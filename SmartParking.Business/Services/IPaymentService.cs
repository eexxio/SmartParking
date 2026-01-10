using System;
using SmartParking.Domain.Entities;

namespace SmartParking.Business.Services
{
    public interface IPaymentService
    {
        decimal CalculateAmount(DateTime start, DateTime end, decimal hourlyRate);
        void ProcessPayment(Guid reservationId, Guid userId, string userEmail, DateTime startTime, DateTime endTime, decimal hourlyRate);
    }
}