using System;
using System.Collections.Generic;
using SmartParking.Business.Interfaces;
using SmartParking.DataAccess.Repositories;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using SmartParking.Domain.Exceptions;

namespace SmartParking.Business.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly INotificationService _notificationService;
        private readonly IWalletService _walletService;
        private readonly IReservationService _reservationService;
        private readonly IParkingSpotService _spotService;

        public PaymentService(
            IPaymentRepository paymentRepo,
            INotificationService notificationService,
            IWalletService walletService,
            IReservationService reservationService,
            IParkingSpotService spotService)
        {
            _paymentRepo = paymentRepo;
            _notificationService = notificationService;
            _walletService = walletService;
            _reservationService = reservationService;
            _spotService = spotService;
        }

        public decimal CalculatePaymentAmount(Guid reservationId)
        {
            var reservation = _reservationService.GetReservation(reservationId);

            if (reservation == null)
                throw new PaymentNotFoundException($"Reservation {reservationId} not found.");

            if (reservation.EndTime == null || reservation.EndTime == DateTime.MinValue)
                throw new InvalidPaymentException("Reservation is ongoing. Cannot calculate final price.");

            var duration = (reservation.EndTime.Value - reservation.StartTime).TotalHours;
            if (duration < 0) duration = 0;

            var spot = _spotService.GetSpot(reservation.SpotId);

            if (spot == null)
                throw new PaymentProcessingException($"Spot {reservation.SpotId} not found.");

            decimal amount = spot.HourlyRate * (decimal)duration;
            return Math.Round(amount, 2);
        }

        public void ProcessPayment(Guid reservationId)
        {
            decimal amount = CalculatePaymentAmount(reservationId);

            var reservation = _reservationService.GetReservation(reservationId);
            var userId = reservation.UserId;

            if (!_walletService.CanAfford(userId, amount))
            {
                throw new InsufficientBalanceException($"User {userId} has insufficient balance.");
            }

            var payment = new Payment(reservationId, amount);
            var paymentId = _paymentRepo.Create(payment);

            try
            {
                _walletService.Withdraw(userId, amount);
                _paymentRepo.UpdateStatus(paymentId, PaymentStatus.Completed);

                _notificationService.SendPaymentConfirmationAsync("user@example.com", amount).Wait();
            }
            catch (Exception ex)
            {
                _paymentRepo.UpdateStatus(paymentId, PaymentStatus.Failed);
                throw new PaymentProcessingException("Payment failed during processing.", ex);
            }
        }

        public Payment GetPaymentByReservation(Guid reservationId)
        {
            return _paymentRepo.GetByReservationId(reservationId);
        }

        public List<Payment> GetUserPayments(Guid userId)
        {
            return _paymentRepo.GetByUserId(userId);
        }
    }
}
