using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

[assembly: InternalsVisibleTo("Peeveen.Hotel.Ef.Test")]

namespace Peeveen.Hotel.Ef;

public class EfHotelStore(HotelContext hotelContext) : IHotelStore {
	private static List<OccupiedDate> GetOccupiedDates(ulong roomId, DateTime from, DateTime to) {
		var occupiedDates = new List<OccupiedDate>();
		// Checkout day does not count as "occupied".
		while (from < to) {
			occupiedDates.Add(new OccupiedDate() {
				Date = from,
				RoomId = roomId
			});
			from = from.AddDays(1);
		}
		return occupiedDates;
	}

	public async Task<ulong> CreateHotelAsync(string name, IReadOnlyDictionary<uint, uint> rooms) {
		var newHotel = new Hotel() {
			Name = name,
			Rooms = [.. rooms.Select(kvp => new Room() {
				Number = kvp.Key,
				Capacity = kvp.Value,
				OccupiedDates = []
			})]
		};
		await hotelContext.Hotels.AddAsync(newHotel);
		await hotelContext.SaveChangesAsync();
		return newHotel.Id;
	}

	public async Task<ulong> CreateBookingAsync(ulong hotelId, uint roomNumber, DateTime from, DateTime to) {
		// We need the hotel ID.
		var hotel = await hotelContext.Hotels
			.Where(h => h.Id == hotelId)
			.SingleAsync();

		// We also want to update the room occupied dates, so we need the room.
		var room = await hotelContext.Rooms
			.Where(r => r.HotelId == hotelId && r.Number == roomNumber)
			.SingleAsync();

		var newBooking = new Booking() {
			Hotel = hotel,
			RoomNumber = roomNumber,
			From = from,
			To = to
		};
		var originalOccupiedDates = room.OccupiedDates.ToList();
		using var transaction = await hotelContext.Database.BeginTransactionAsync();
		try {
			await hotelContext.Bookings.AddAsync(newBooking);

			var occupiedDates = GetOccupiedDates(room.Id, from, to);
			foreach (var occupiedDate in occupiedDates)
				room.OccupiedDates.Add(occupiedDate);

			await hotelContext.SaveChangesAsync();
			await transaction.CommitAsync();
		} catch {
			await transaction.RollbackAsync();
			throw;
		}
		return newBooking.Id;
	}

	public async Task<IReadOnlyList<IRoom>> GetAvailableRoomsAsync(ulong hotelId, uint minCapacity, DateTime from, DateTime to) =>
		await hotelContext.Rooms
			.Where(
				r => r.HotelId == hotelId &&
				r.Capacity >= minCapacity &&
				r.OccupiedDates.All(d => d.Date < from || d.Date >= to)
			).ToListAsync<IRoom>();

	public Task<IBooking?> GetBookingAsync(ulong bookingId) =>
		hotelContext.Bookings
			.Where(b => b.Id == bookingId)
			.SingleOrDefaultAsync<IBooking>();

	public async Task<IReadOnlyList<IHotel>> GetHotelAsync(string nameOrPartOfName) =>
		await hotelContext.Hotels
			.Include(h => h.Rooms)
			.Where(h => EF.Functions.Like(h.Name, $"%{nameOrPartOfName}%"))
			.ToListAsync<IHotel>();
}