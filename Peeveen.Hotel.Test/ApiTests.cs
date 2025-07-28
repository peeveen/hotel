namespace Peeveen.Hotel.Test;

public class ApiTests {
	[Fact]
	public void TestDateValidation() {
		var today = DateTime.UtcNow.Date;

		var pastAction = () => HotelApi.NormalizeDates(today.AddDays(-2), today.AddDays(2));
		pastAction.Should().Throw<ArgumentException>()
			.WithMessage($"{HotelApi.PastBookingErrorMessage}*");
		var impossibleAction = () => HotelApi.NormalizeDates(today.AddDays(20), today.AddDays(10));
		impossibleAction.Should().Throw<ArgumentException>()
			.WithMessage($"{HotelApi.ImpossibleBookingErrorMessage}*");
		var (normFrom, normTo) = HotelApi.NormalizeDates(today.AddDays(20), today.AddDays(30));
		normFrom.Should().Be(today.AddDays(20).Date);
		normTo.Should().Be(today.AddDays(30).Date);
		var nonUtcFromAction = () => HotelApi.NormalizeDates(DateTime.Now, DateTime.UtcNow);
		nonUtcFromAction.Should().Throw<ArgumentException>()
			.WithMessage($"{HotelApi.NonUtcDateErrorMessage}*");
		var nonUtcToAction = () => HotelApi.NormalizeDates(DateTime.UtcNow, DateTime.Now);
		nonUtcToAction.Should().Throw<ArgumentException>()
			.WithMessage($"{HotelApi.NonUtcDateErrorMessage}*");
	}

	[Fact]
	public async Task TestApiValidation() {
		var hotelApi = new HotelApi(new MockStore());

		var noRooms = new Dictionary<uint, uint> { };
		var sixRooms = Enumerable.Range(1, 6).ToDictionary(i => (uint)i, i => (uint)i);

		var tooShortHotelName = GenerateString(HotelApi.MinimumHotelNameLength - 1);
		var justLongEnoughHotelName = GenerateString(HotelApi.MinimumHotelNameLength);

		var tooShortNameAction = () => hotelApi.CreateHotelAsync(tooShortHotelName, sixRooms);
		(await tooShortNameAction.Should().ThrowAsync<HotelException>())
			.WithMessage(HotelApi.FailedToCreateHotelErrorMessage)
			.WithInnerException<ArgumentException>()
			.WithMessage($"{HotelApi.InvalidHotelNameErrorMessage}*")
			.WithParameterName("name");

		var noRoomsAction = () => hotelApi.CreateHotelAsync(justLongEnoughHotelName, noRooms);
		(await noRoomsAction.Should().ThrowAsync<HotelException>())
			.WithMessage(HotelApi.FailedToCreateHotelErrorMessage)
			.WithInnerException<ArgumentException>()
			.WithMessage($"{HotelApi.InvalidRoomCountErrorMessage}*")
			.WithParameterName("rooms");

		var justLongEnoughHotelNameAction = () => hotelApi.CreateHotelAsync(justLongEnoughHotelName, sixRooms);
		await justLongEnoughHotelNameAction.Should().NotThrowAsync();

		var tooShortSearchString = GenerateString(HotelApi.MinimumSearchStringLength - 1);
		var justLongEnoughSearchString = GenerateString(HotelApi.MinimumSearchStringLength);

		var tooShortSearchAction = () => hotelApi.GetHotelAsync(tooShortSearchString);
		(await tooShortSearchAction.Should().ThrowAsync<HotelException>())
			.WithMessage(HotelApi.FailedToSearchHotelsErrorMessage)
			.WithInnerException<ArgumentException>()
			.WithMessage($"{HotelApi.NotEnoughSearchTextErrorMessage}*")
			.WithParameterName("nameOrPartOfName");

		var justLongEnoughSearchAction = () => hotelApi.GetHotelAsync(justLongEnoughSearchString);
		await justLongEnoughSearchAction.Should().NotThrowAsync();
	}

	private static string GenerateString(int length) {
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		return new string([.. Enumerable.Repeat(chars, length).Select(s => s[Random.Shared.Next(s.Length)])]);
	}
}