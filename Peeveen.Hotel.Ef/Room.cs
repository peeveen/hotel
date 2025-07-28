using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Peeveen.Hotel.Ef;

[Index(nameof(HotelId), nameof(Number), IsUnique = true)]
public class Room : IRoom {
	[Key]
	[JsonIgnore]
	public ulong Id { get; set; }

	// Explicitly declaring this navigation property so I can reference it in the Index attribute.
	[JsonIgnore]
	public ulong HotelId { get; set; }
	[JsonIgnore]
	public virtual Hotel Hotel { get; set; } = null!;

	[JsonIgnore]
	public virtual ICollection<OccupiedDate> OccupiedDates { get; set; } = [];

	public required uint Number { get; set; }
	public required uint Capacity { get; set; }
}
