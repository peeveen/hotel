using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Peeveen.Hotel.Ef;

public class Hotel : IHotel {
	[Key]
	public ulong Id { get; set; }

	public required string Name { get; set; }

	[JsonIgnore]
	public virtual required ICollection<Room> Rooms { get; set; }

	[NotMapped]
	IReadOnlyDictionary<uint, uint> IHotel.Rooms =>
		Rooms.ToDictionary(r => r.Number, r => r.Capacity);
}
