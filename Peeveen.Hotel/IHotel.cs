namespace Peeveen.Hotel;

public interface IHotel {
	ulong Id { get; }
	string Name { get; }
	IReadOnlyDictionary<uint, uint> Rooms { get; }
}
