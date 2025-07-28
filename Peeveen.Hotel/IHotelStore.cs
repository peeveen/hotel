namespace Peeveen.Hotel;

public interface IHotelStore {
	Task<ulong> CreateHotelAsync(string name, IReadOnlyDictionary<uint, uint> rooms);
	Task<IReadOnlyList<IHotel>> GetHotelAsync(string nameOrPartOfName);
	Task<IReadOnlyList<IRoom>> GetAvailableRoomsAsync(ulong hotelId, uint minCapacity, DateTime from, DateTime to);
	Task<ulong> CreateBookingAsync(ulong hotelId, uint roomNumber, DateTime from, DateTime to);
	Task<IBooking?> GetBookingAsync(ulong bookingId);
}
