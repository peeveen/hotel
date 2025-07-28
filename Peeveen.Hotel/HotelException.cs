namespace Peeveen.Hotel;

public class HotelException : Exception {
	internal HotelException(string message) : base(message) { }
	internal HotelException(string message, Exception innerException) : base(message, innerException) { }
}