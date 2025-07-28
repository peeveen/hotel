using Microsoft.EntityFrameworkCore;
using YamlDotNet.Serialization;

namespace Peeveen.Hotel.Ef.Test;

public abstract class DatabaseTests {
	protected abstract HotelContext CreateHotelContext();
	private readonly IDeserializer _yamlDeserializer = new DeserializerBuilder().Build();
	private const string DbUpdateExceptionMessage = "An error occurred while saving the entity changes. See the inner exception for details.";

	private async Task WithContext(Func<HotelContext, Task> action) {
		using var context = CreateHotelContext();
		context.Database.EnsureCreated();
		await action(context);
		await context.Database.CloseConnectionAsync();
	}

	private async Task<T> WithContext<T>(Func<HotelContext, Task<T>> action) {
		using var context = CreateHotelContext();
		context.Database.EnsureCreated();
		var result = await action(context);
		await context.Database.CloseConnectionAsync();
		return result;
	}

	private async Task<T> GetTestData<T>(string filename) {
		// Down from bin/debug/net9.0 to the test project folder ...
		var path = Path.Combine("..", "..", "..", "testData", filename);
		return _yamlDeserializer.Deserialize<T>(await File.ReadAllTextAsync(path));
	}

	private async Task<Hotel[]> InitializeHotels(string filename) {
		// Ensure DB is empty before we start.
		await WithContext(async context => {
			var hotels = await context.Hotels.ToListAsync();
			hotels.Should().BeEmpty();
		});

		// Read hotels from YAML file.
		var hotelsToAdd = await GetTestData<Hotel[]>(filename);

		// Add hotels.
		await WithContext(async context => {
			foreach (var hotelToAdd in hotelsToAdd)
				context.Hotels.Add(hotelToAdd);
			await context.SaveChangesAsync();
		});

		return hotelsToAdd;
	}

	[Fact]
	public async Task TestAddingThenSearchingHotels() {
		var hotels = await InitializeHotels("testAddingThenSearchingHotels.yml");

		await WithContext(async context => {
			var dbHotels = await context.Hotels.Include(h => h.Rooms).ToListAsync();
			// Added hotels should match source data, but with actual IDs.
			dbHotels.Should().HaveCount(hotels.Length);
			hotels.Zip(dbHotels).Should().AllSatisfy(zippedHotel => {
				var sourceHotel = zippedHotel.First;
				var dbHotel = zippedHotel.Second;
				dbHotel.Id.Should().BeGreaterThan(0);
				dbHotel.Name.Should().Be(sourceHotel.Name);
				dbHotel.Rooms.Count.Should().Be(sourceHotel.Rooms.Count);
				var zippedRooms = sourceHotel.Rooms.Zip(dbHotel.Rooms);
				sourceHotel.Rooms.Zip(dbHotel.Rooms).Should().AllSatisfy(zippedRoom => {
					var sourceRoom = zippedRoom.First;
					var dbRoom = zippedRoom.Second;
					dbRoom.Number.Should().Be(sourceRoom.Number);
					dbRoom.Capacity.Should().Be(sourceRoom.Capacity);
					dbRoom.Hotel.Id.Should().Be(sourceRoom.Hotel.Id).And.Be(dbHotel.Id);
				});
			});

			var store = new EfHotelStore(context);
			// Should match "Test Hotel 1" and "Test Hotel 2"
			var searchResults = await store.GetHotelAsync("test hotel");
			searchResults.Should().HaveCount(2);
			// Should only match "Test Hotel 1"
			searchResults = await store.GetHotelAsync("test hotel 1");
			searchResults.Should().HaveCount(1);
			// Should not match anything.
			searchResults = await store.GetHotelAsync("flimflam");
			searchResults.Should().HaveCount(0);
		});
	}

	[Fact]
	public async Task TestBooking() {
		await InitializeHotels("testBooking.yml");

		var today = DateTime.UtcNow.Date;
		var startDate = today.AddDays(5);
		var endDate = startDate.AddDays(7);

		var (hotelCaliforniaId, suitableHotelCaliforniaRoomCount, firstSuitableHotelCaliforniaRoomNumber, bookingId) = await WithContext(async context => {
			var store = new EfHotelStore(context);
			var hotelCalifornia = (await store.GetHotelAsync("Hotel calif")).Single();

			// Get available double rooms.
			var requiredCapacity = 2u;
			var suitableHotelCaliforniaRoomCount = hotelCalifornia.Rooms.Count(r => r.Value >= requiredCapacity);
			var availableHotelCaliforniaRooms = await store.GetAvailableRoomsAsync(hotelCalifornia.Id, requiredCapacity, startDate, endDate);
			availableHotelCaliforniaRooms.Should().HaveCount(suitableHotelCaliforniaRoomCount);

			// Create a booking in one of those rooms.
			var firstSuitableHotelCaliforniaRoomNumber = availableHotelCaliforniaRooms[0].Number;
			var bookingId = await store.CreateBookingAsync(hotelCalifornia.Id, firstSuitableHotelCaliforniaRoomNumber, startDate, endDate);
			bookingId.Should().BeGreaterThan(0);
			return (hotelCalifornia.Id, suitableHotelCaliforniaRoomCount, firstSuitableHotelCaliforniaRoomNumber, bookingId);
		});

		// Now check available rooms again. Should only be two for the requested dates.
		// Ensure we get the same result if the requested date range partially or wholly overlaps the existing booking.
		var tasks = CreateOffsetTasks((f, g) =>
			WithContext(async context => {
				var store = new EfHotelStore(context);
				return await store.GetAvailableRoomsAsync(hotelCaliforniaId, 2, startDate.AddDays(f), endDate.AddDays(g));
			})
		);
		var results = await Task.WhenAll(tasks);
		results.Should().AllSatisfy(r => r.Should().HaveCount(suitableHotelCaliforniaRoomCount - 1));

		// Attempt to create clashing bookings. Ensure that the DB constraints prevent it.
		var clashTasks = CreateOffsetTasks<Func<Task<ulong>>>((f, g) => () =>
			WithContext(async context => {
				var store = new EfHotelStore(context);
				return await store.CreateBookingAsync(hotelCaliforniaId, firstSuitableHotelCaliforniaRoomNumber, startDate.AddDays(f), endDate.AddDays(g));
			})
		);
		clashTasks.Should().AllSatisfy(t => t.Should().ThrowAsync<DbUpdateException>()
			.WithMessage(DbUpdateExceptionMessage)
		);

		// We should be able to create a booking in the same room that starts on the END date of the previous booking
		// since a room does not count as "occupied" on checkout day.
		var adjacentBookingId = await WithContext(async context => {
			var store = new EfHotelStore(context);
			var adjacentBookingId = await store.CreateBookingAsync(hotelCaliforniaId, firstSuitableHotelCaliforniaRoomNumber, endDate, endDate.AddDays(5));
			adjacentBookingId.Should().BeGreaterThan(0);
			return adjacentBookingId;
		});

		await WithContext(async context => {
			var store = new EfHotelStore(context);
			// We should now be able to retrieve our booking information.
			// Verify that they are correct.
			var originalBooking = await store.GetBookingAsync(bookingId);
			originalBooking.Should().NotBeNull();
			originalBooking.HotelId.Should().Be(hotelCaliforniaId);
			originalBooking.RoomNumber.Should().Be(firstSuitableHotelCaliforniaRoomNumber);
			originalBooking.From.Should().Be(startDate);
			originalBooking.To.Should().Be(endDate);
			var adjacentBooking = await store.GetBookingAsync(adjacentBookingId);
			adjacentBooking.Should().NotBeNull();
			adjacentBooking.HotelId.Should().Be(hotelCaliforniaId);
			adjacentBooking.RoomNumber.Should().Be(firstSuitableHotelCaliforniaRoomNumber);
			adjacentBooking.From.Should().Be(endDate);
			adjacentBooking.To.Should().Be(endDate.AddDays(5));
		});

		await WithContext(async context => {
			var store = new EfHotelStore(context);
			// Also make sure that we get null for non-existent booking numbers.
			var nonExistentBooking = await store.GetBookingAsync(93847886);
			nonExistentBooking.Should().BeNull();
		});
	}

	[Fact]
	public async Task TestAddingHotelWithDuplicateRoomNumbers() {
		var addHotelsFn = () => InitializeHotels("testAddingHotelWithDuplicateRoomNumbers.yml");
		await addHotelsFn.Should().ThrowAsync<DbUpdateException>()
			.WithMessage(DbUpdateExceptionMessage);
	}

	private static IEnumerable<T> CreateOffsetTasks<T>(Func<int, int, T> fn, uint maximumOffsetMagnitude = 1) {
		var start = -(int)maximumOffsetMagnitude;
		var count = ((int)maximumOffsetMagnitude * 2) + 1;
		return Enumerable.Range(start, count).SelectMany(f =>
			Enumerable.Range(start, count).Select(g =>
				fn(f, g)
			)
		);
	}
}