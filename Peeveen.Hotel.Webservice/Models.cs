using System.Text.Json.Serialization;

namespace Peeveen.Hotel.Webservice;

public record CreateHotelRequest(
	[property:JsonPropertyName("name")]
	string Name,
	[property:JsonPropertyName("rooms")]
	IReadOnlyDictionary<uint, uint> Rooms
);
public record CreateRoomRequest(
	[property:JsonPropertyName("number")]
	uint Number,
	[property:JsonPropertyName("capacity")]
	uint Capacity
) : IRoom;
public record GetAvailableRoomsRequest(
	[property:JsonPropertyName("hotelId")]
	ulong HotelId,
	[property:JsonPropertyName("minCapacity")]
	uint MinCapacity,
	[property:JsonPropertyName("from")]
	DateTime From,
	[property:JsonPropertyName("to")]
	DateTime To
);
public record CreateBookingRequest(
	[property:JsonPropertyName("hotelId")]
	ulong HotelId,
	[property:JsonPropertyName("roomNumber")]
	uint RoomNumber,
	[property:JsonPropertyName("from")]
	DateTime From,
	[property:JsonPropertyName("to")]
	DateTime To
);
