namespace Peeveen.Hotel.Test;

internal class MockStore : IHotelStore {
	public Task<ulong> CreateHotelAsync(string name, IReadOnlyDictionary<uint,uint> rooms) =>
		Task.FromResult(1UL);
	public Task<IReadOnlyList<IHotel>> GetHotelAsync(string nameOrPartOfName) =>
		Task.FromResult<IReadOnlyList<IHotel>>([]);
	public Task<IReadOnlyList<IRoom>> GetAvailableRoomsAsync(ulong hotelId, uint minCapacity, DateTime from, DateTime to) =>
		Task.FromResult<IReadOnlyList<IRoom>>([]);
	public Task<ulong> CreateBookingAsync(ulong hotelId, uint roomNumber, DateTime from, DateTime to) =>
		Task.FromResult(1UL);
	public Task<IBooking?> GetBookingAsync(ulong bookingId) =>
		Task.FromResult<IBooking?>(null);
}