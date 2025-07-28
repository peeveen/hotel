namespace Peeveen.Hotel;

public interface IBooking {
	ulong Id { get; }
	ulong HotelId { get; }
	uint RoomNumber { get; }
	DateTime From { get; }
	DateTime To { get; }
}
