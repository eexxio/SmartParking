namespace SmartParking.Domain.Exceptions;

public class InvalidSpotTypeException : Exception
{
    public InvalidSpotTypeException(string message) : base(message) { }

    public InvalidSpotTypeException(string message, Exception innerException)
        : base(message, innerException) { }
}
