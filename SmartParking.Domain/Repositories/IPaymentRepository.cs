using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using System;

namespace SmartParking.DataAccess.Repositories
{
    public interface IPaymentRepository
    {
        Guid Create(Payment payment);
        Payment GetById(Guid id);
        Payment GetByReservationId(Guid reservationId);
        void UpdateStatus(Guid id, PaymentStatus status);
    }
}