using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Peeveen.Hotel.Test")]

namespace Peeveen.Hotel;

public class HotelApi(IHotelStore hotelStore) {
	internal const string ImpossibleBookingErrorMessage = "The 'to' date must be at least one day later than the 'from' date.";
	internal const string PastBookingErrorMessage = "The booking dates must not be in the past.";
	internal const string NonUtcDateErrorMessage = "The booking dates must be in UTC.";

	internal const string FailedToCreateHotelErrorMessage = "Failed to create hotel.";
	internal const string FailedToSearchHotelsErrorMessage = "Failed to search hotels.";
	internal const string FailedToGetAvailableRoomsErrorMessage = "Failed to get available rooms.";
	internal const string FailedToCreateBookingErrorMessage = "Failed to create booking.";
	internal const string FailedToGetBookingErrorMessage = "Failed to get booking.";

	public const int MinimumHotelNameLength = 5;
	public const int MinimumSearchStringLength = 3;
	public const int RequiredRoomCount = 6;

	internal static readonly string NotEnoughSearchTextErrorMessage = $"The search text must be at least {MinimumSearchStringLength} characters.";
	internal static readonly string InvalidHotelNameErrorMessage = $"The hotel name must be at least {MinimumHotelNameLength} characters.";
	internal static readonly string InvalidRoomCountErrorMessage = $"The hotel must have exactly {RequiredRoomCount} rooms.";

	public async Task<ulong> CreateHotelAsync(string name, IReadOnlyDictionary<uint, uint> rooms) {
		try {
			name = name.Trim();
			if (name.Length < MinimumHotelNameLength)
				throw new ArgumentException(InvalidHotelNameErrorMessage, nameof(name));
			if (rooms.Count() != RequiredRoomCount)
				throw new ArgumentException(InvalidRoomCountErrorMessage, nameof(rooms));
			return await hotelStore.CreateHotelAsync(name, rooms);
		} catch (Exception e) {
			throw new HotelException(FailedToCreateHotelErrorMessage, e);
		}
	}

	public async Task<IReadOnlyList<IHotel>> GetHotelAsync(string nameOrPartOfName) {
		try {
			nameOrPartOfName = nameOrPartOfName.Trim();
			if (nameOrPartOfName.Length < MinimumSearchStringLength)
				throw new ArgumentException(NotEnoughSearchTextErrorMessage, nameof(nameOrPartOfName));
			return await hotelStore.GetHotelAsync(nameOrPartOfName);
		} catch (Exception e) {
			throw new HotelException(FailedToSearchHotelsErrorMessage, e);
		}
	}

	public async Task<IReadOnlyList<IRoom>> GetAvailableRoomsAsync(ulong hotelId, uint minCapacity, DateTime from, DateTime to) {
		try {
			(from, to) = NormalizeDates(from, to);
			return await hotelStore.GetAvailableRoomsAsync(hotelId, minCapacity, from, to);
		} catch (Exception e) {
			throw new HotelException(FailedToGetAvailableRoomsErrorMessage, e);
		}
	}
	public async Task<ulong> CreateBookingAsync(ulong hotelId, uint roomNumber, DateTime from, DateTime to) {
		try {
			(from, to) = NormalizeDates(from, to);
			return await hotelStore.CreateBookingAsync(hotelId, roomNumber, from, to);
		} catch (Exception e) {
			throw new HotelException(FailedToCreateBookingErrorMessage, e);
		}
	}
	public async Task<IBooking?> GetBookingAsync(ulong bookingId) {
		try {
			return await hotelStore.GetBookingAsync(bookingId);
		} catch (Exception e) {
			throw new HotelException(FailedToGetBookingErrorMessage, e);
		}
	}

	internal static (DateTime, DateTime) NormalizeDates(DateTime from, DateTime to) {
		if (from.Kind != DateTimeKind.Utc)
			throw new ArgumentException(NonUtcDateErrorMessage, nameof(from));
		if (to.Kind != DateTimeKind.Utc)
			throw new ArgumentException(NonUtcDateErrorMessage, nameof(to));
		var fromDateOnly = from.Date;
		var toDateOnly = to.Date;
		if (toDateOnly <= fromDateOnly)
			throw new ArgumentException(ImpossibleBookingErrorMessage, nameof(from));
		if (fromDateOnly <= DateTime.UtcNow)
			throw new ArgumentException(PastBookingErrorMessage, nameof(to));
		return (fromDateOnly, toDateOnly);
	}
}