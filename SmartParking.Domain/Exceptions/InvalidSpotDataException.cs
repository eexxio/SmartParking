namespace SmartParking.Domain.Exceptions;

public class InvalidSpotDataException : Exception
{
    public InvalidSpotDataException(string message) : base(message) { }

    public InvalidSpotDataException(string message, Exception innerException)
        : base(message, innerException) { }
}
