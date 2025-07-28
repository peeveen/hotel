using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Peeveen.Hotel.Ef;

public class Booking : IBooking {
	[Key]
	public ulong Id { get; set; }

	// Explicitly declaring this navigation property so I can reference it in the PrimaryKey attribute.
	public ulong HotelId { get; set; }

	[JsonIgnore]
	public virtual Hotel Hotel { get; set; } = null!;

	public uint RoomNumber { get; set; }

	public DateTime From { get; set; }
	public DateTime To { get; set; }
}
