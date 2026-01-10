namespace SmartParking.Domain.Exceptions;

public class ReservationNotFoundException : Exception
{
    public ReservationNotFoundException(string message) : base(message) { }
    public ReservationNotFoundException(string message, Exception inner) : base(message, inner) { }
}
