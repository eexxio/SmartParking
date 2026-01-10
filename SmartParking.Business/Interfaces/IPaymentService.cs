using System;
using System.Collections.Generic;
using SmartParking.Domain.Entities;

namespace SmartParking.Business.Interfaces
{
    public interface IPaymentService
    {
        decimal CalculatePaymentAmount(Guid reservationId);
        void ProcessPayment(Guid reservationId);
        Payment GetPaymentByReservation(Guid reservationId);
        List<Payment> GetUserPayments(Guid userId);
    }
}