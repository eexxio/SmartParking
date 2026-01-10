namespace SmartParking.Domain.Exceptions;

public class InvalidReservationException : Exception
{
    public InvalidReservationException(string message) : base(message) { }
    public InvalidReservationException(string message, Exception inner) : base(message, inner) { }
}
