using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Peeveen.Hotel.Ef;

[Index(nameof(RoomId), nameof(Date), IsUnique = true)]
public class OccupiedDate {
	[Key]
	public ulong Id { get; set; }
	// Explicitly declaring this navigation property so I can reference it in the Index attribute.
	public ulong RoomId { get; set; }
	public virtual Room Room { get; set; } = null!;

	public DateTime Date { get; set; }
}
