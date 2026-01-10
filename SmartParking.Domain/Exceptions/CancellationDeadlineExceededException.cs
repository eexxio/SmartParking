namespace SmartParking.Domain.Exceptions;

public class CancellationDeadlineExceededException : Exception
{
    public CancellationDeadlineExceededException(string message) : base(message) { }
    public CancellationDeadlineExceededException(string message, Exception inner) : base(message, inner) { }
}
