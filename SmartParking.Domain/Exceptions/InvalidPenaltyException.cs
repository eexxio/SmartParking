namespace SmartParking.Domain.Exceptions;

public class InvalidPenaltyException : Exception
{
    public InvalidPenaltyException(string message) : base(message) { }
    public InvalidPenaltyException(string message, Exception inner) : base(message, inner) { }
}
