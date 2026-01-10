using System;
using SmartParking.DataAccess.Repositories;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;

namespace SmartParking.Business.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly INotificationService _notificationService;
        private readonly IWalletService _walletService;

        public PaymentService(
            IPaymentRepository paymentRepo,
            INotificationService notificationService,
            IWalletService walletService)
        {
            _paymentRepo = paymentRepo;
            _notificationService = notificationService;
            _walletService = walletService;
        }

        public decimal CalculateAmount(DateTime start, DateTime end, decimal hourlyRate)
        {
            if (end <= start)
                throw new ArgumentException("End time must be after start time");

            var duration = (end - start).TotalHours;
            var amount = (decimal)duration * hourlyRate;

            return Math.Round(amount, 2);
        }

        public void ProcessPayment(Guid reservationId, Guid userId, string userEmail, DateTime startTime, DateTime endTime, decimal hourlyRate)
        {
            decimal amount = CalculateAmount(startTime, endTime, hourlyRate);

            var payment = new Payment(reservationId, amount);
            var paymentId = _paymentRepo.Create(payment);

            try
            {
                _walletService.Withdraw(userId, amount);
                _paymentRepo.UpdateStatus(paymentId, PaymentStatus.Completed);

                _notificationService.SendPaymentReceiptAsync(userEmail, amount).Wait();
            }
            catch (Exception ex)
            {
                _paymentRepo.UpdateStatus(paymentId, PaymentStatus.Failed);

                throw new Exception($"Payment processing failed: {ex.Message}", ex);
            }
        }
    }
}