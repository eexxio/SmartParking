namespace SmartParking.Domain.Exceptions;

public class InvalidUserDataException : Exception
{
    public InvalidUserDataException() : base()
    {
    }

    public InvalidUserDataException(string message) : base(message)
    {
    }

    public InvalidUserDataException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
