namespace SmartParking.Domain.Exceptions;

public class SpotNotAvailableException : Exception
{
    public SpotNotAvailableException(string message) : base(message) { }

    public SpotNotAvailableException(string message, Exception innerException)
        : base(message, innerException) { }
}
