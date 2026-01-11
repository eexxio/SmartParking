using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using System.Collections.Generic;

namespace SmartParking.Infrastructure.Interfaces
{
    public interface IPaymentRepository
    {
        Guid Create(Payment payment);
        Payment GetById(Guid id);
        Payment GetByReservationId(Guid reservationId);
        List<Payment> GetByUserId(Guid userId);
        void UpdateStatus(Guid id, PaymentStatus status);
    }
}