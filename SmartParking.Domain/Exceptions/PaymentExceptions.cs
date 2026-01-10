using System;

namespace SmartParking.Domain.Exceptions
{
    public class InvalidPaymentException : Exception
    {
        public InvalidPaymentException(string message) : base(message) { }
    }

    public class PaymentProcessingException : Exception
    {
        public PaymentProcessingException(string message) : base(message) { }
        public PaymentProcessingException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class PaymentNotFoundException : Exception
    {
        public PaymentNotFoundException(Guid id)
            : base($"Payment with ID {id} was not found.") { }

        public PaymentNotFoundException(string message) : base(message) { }
    }
}