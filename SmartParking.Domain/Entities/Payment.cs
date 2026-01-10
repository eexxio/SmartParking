using System;
using SmartParking.Domain.Enums;

namespace SmartParking.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid ReservationId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        public Payment(Guid reservationId, decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            Id = Guid.NewGuid();
            ReservationId = reservationId;
            Amount = amount;
            PaymentStatus = PaymentStatus.Pending;
            CreatedAt = DateTime.Now;
        }
        public Payment() { }
    }
}