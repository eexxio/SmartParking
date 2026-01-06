namespace SmartParking.Domain.Exceptions;

public class InsufficientBalanceException : Exception
{
    public InsufficientBalanceException() : base()
    {
    }

    public InsufficientBalanceException(string message) : base(message)
    {
    }

    public InsufficientBalanceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
