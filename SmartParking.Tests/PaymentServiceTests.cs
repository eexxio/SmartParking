using Moq;
using SmartParking.Business;
using SmartParking.Business.Interfaces;
using SmartParking.Business.Services;
using SmartParking.DataAccess.Repositories;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using SmartParking.Domain.Exceptions;
using System;
using System.Collections.Generic;
using Xunit;

namespace SmartParking.Tests
{
    public class PaymentServiceTests
    {
        private readonly Mock<IPaymentRepository> _mockRepo;
        private readonly Mock<INotificationService> _mockNotify;
        private readonly Mock<IWalletService> _mockWallet;
        private readonly Mock<IReservationService> _mockReservation;
        private readonly Mock<IParkingSpotService> _mockSpot;

        private readonly PaymentService _service;

        public PaymentServiceTests()
        {
            _mockRepo = new Mock<IPaymentRepository>();
            _mockNotify = new Mock<INotificationService>();
            _mockWallet = new Mock<IWalletService>();
            _mockReservation = new Mock<IReservationService>();
            _mockSpot = new Mock<IParkingSpotService>();

            _service = new PaymentService(
                _mockRepo.Object,
                _mockNotify.Object,
                _mockWallet.Object,
                _mockReservation.Object,
                _mockSpot.Object
            );
        }

        [Fact]
        public void CalculatePaymentAmount_ValidReservation_ReturnsCorrectAmount()
        {
            var resId = Guid.NewGuid();
            SetupReservation(resId, durationHours: 2);
            SetupSpot(10m);

            decimal amount = _service.CalculatePaymentAmount(resId);

            Assert.Equal(20m, amount); // 2 * 10 = 20
        }

        [Fact]
        public void CalculatePaymentAmount_1Hour_CalculatesCorrectly()
        {
            var resId = Guid.NewGuid();
            SetupReservation(resId, durationHours: 1);
            SetupSpot(15m);

            decimal amount = _service.CalculatePaymentAmount(resId);

            Assert.Equal(15m, amount);
        }

        [Fact]
        public void CalculatePaymentAmount_3Point5Hours_CalculatesCorrectly()
        {
            var resId = Guid.NewGuid();
            SetupReservation(resId, durationHours: 3.5);
            SetupSpot(10m);

            decimal amount = _service.CalculatePaymentAmount(resId);

            Assert.Equal(35m, amount);
        }

        [Fact]
        public void CalculatePaymentAmount_NoEndTime_ThrowsException()
        {
            var resId = Guid.NewGuid();
            SetupReservation(resId, durationHours: 1, isOngoing: true);

            Assert.Throws<InvalidPaymentException>(() => _service.CalculatePaymentAmount(resId));
        }

        [Fact]
        public void CalculatePaymentAmount_ReservationNotFound_ThrowsException()
        {
            _mockReservation.Setup(r => r.GetReservation(It.IsAny<Guid>())).Returns((Reservation?)null);
            Assert.Throws<PaymentNotFoundException>(() => _service.CalculatePaymentAmount(Guid.NewGuid()));
        }

        [Fact]
        public void CalculatePaymentAmount_SpotNotFound_ThrowsException()
        {
            var resId = Guid.NewGuid();
            SetupReservation(resId, 1);
            _mockSpot.Setup(s => s.GetSpot(It.IsAny<Guid>())).Returns((ParkingSpot)null);

            Assert.Throws<PaymentProcessingException>(() => _service.CalculatePaymentAmount(resId));
        }

        [Fact]
        public void CalculatePaymentAmount_SameStartAndEndTime_ThrowsException()
        {
            var resId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var spotId = Guid.NewGuid();
            var startTime = DateTime.UtcNow;

            Assert.Throws<InvalidReservationException>(() =>
            {
                new Reservation(
                    resId, userId, spotId,
                    startTime, startTime, 
                    ReservationStatus.Completed,
                    startTime.AddMinutes(15),
                    startTime
                );
            });
        }

        [Fact]
        public void CalculatePaymentAmount_ZeroDuration_ReturnsZero()
        {
            var resId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var spotId = Guid.NewGuid();

            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddSeconds(1);

            var reservation = new Reservation(
                resId, userId, spotId, startTime, endTime,
                ReservationStatus.Completed, startTime.AddMinutes(15), startTime
            );

            _mockReservation.Setup(r => r.GetReservation(resId)).Returns(reservation);
            _mockSpot.Setup(s => s.GetSpot(spotId)).Returns(
                new ParkingSpot(Guid.NewGuid(), "A-001", "Regular", 10m, false, DateTime.UtcNow));

            decimal amount = _service.CalculatePaymentAmount(resId);

            Assert.Equal(0m, amount);
        }

        [Fact]
        public void CalculatePaymentAmount_HalfHour_CalculatesCorrectly()
        {
            var resId = Guid.NewGuid();
            SetupReservation(resId, 0.5);
            SetupSpot(20m); 

            decimal amount = _service.CalculatePaymentAmount(resId);

            Assert.Equal(10m, amount); 
        }

        [Fact]
        public void CalculatePaymentAmount_24Hours_CalculatesCorrectly()
        {
            var resId = Guid.NewGuid();
            SetupReservation(resId, 24); 
            SetupSpot(5m); 

            decimal amount = _service.CalculatePaymentAmount(resId);

            Assert.Equal(120m, amount);
        }

        [Fact]
        public void ProcessPayment_SufficientBalance_Success()
        {
            var resId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var payId = Guid.NewGuid();

            SetupReservation(resId, 1, userId);
            SetupSpot(10m);
            _mockWallet.Setup(w => w.CanAfford(userId, 10m)).Returns(true);
            _mockRepo.Setup(r => r.Create(It.IsAny<Payment>())).Returns(payId);

            _service.ProcessPayment(resId);

            _mockRepo.Verify(r => r.UpdateStatus(payId, PaymentStatus.Completed), Times.Once);
        }

        [Fact]
        public void ProcessPayment_InsufficientBalance_ThrowsException()
        {
            var resId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            SetupReservation(resId, 1, userId);
            SetupSpot(10m);
            _mockWallet.Setup(w => w.CanAfford(userId, 10m)).Returns(false);

            Assert.Throws<InsufficientBalanceException>(() => _service.ProcessPayment(resId));
        }

        [Fact]
        public void ProcessPayment_DeductsFromWallet()
        {
            var resId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            SetupReservation(resId, 2, userId);
            SetupSpot(50m);
            _mockWallet.Setup(w => w.CanAfford(userId, 100m)).Returns(true);

            _service.ProcessPayment(resId);

            _mockWallet.Verify(w => w.Withdraw(userId, 100m), Times.Once);
        }

        [Fact]
        public void ProcessPayment_CreatesPaymentRecord()
        {
            var resId = Guid.NewGuid();
            SetupReservation(resId, 1);
            SetupSpot(10m);
            _mockWallet.Setup(w => w.CanAfford(It.IsAny<Guid>(), It.IsAny<decimal>())).Returns(true);

            _service.ProcessPayment(resId);

            _mockRepo.Verify(r => r.Create(It.Is<Payment>(p => p.ReservationId == resId && p.Amount == 10m)), Times.Once);
        }

        [Fact]
        public void ProcessPayment_SendsNotification()
        {
            var resId = Guid.NewGuid();
            SetupReservation(resId, 1);
            SetupSpot(10m);
            _mockWallet.Setup(w => w.CanAfford(It.IsAny<Guid>(), It.IsAny<decimal>())).Returns(true);

            _service.ProcessPayment(resId);

            _mockNotify.Verify(n => n.SendPaymentConfirmationAsync(It.IsAny<string>(), 10m), Times.Once);
        }

        [Fact]
        public void ProcessPayment_WalletWithdrawFails_UpdatesStatusToFailed()
        {
            var resId = Guid.NewGuid();
            var payId = Guid.NewGuid();
            SetupReservation(resId, 1);
            SetupSpot(10m);
            _mockWallet.Setup(w => w.CanAfford(It.IsAny<Guid>(), It.IsAny<decimal>())).Returns(true);
            _mockRepo.Setup(r => r.Create(It.IsAny<Payment>())).Returns(payId);

            _mockWallet.Setup(w => w.Withdraw(It.IsAny<Guid>(), It.IsAny<decimal>())).Throws(new Exception("Bank Error"));

            Assert.Throws<PaymentProcessingException>(() => _service.ProcessPayment(resId));

            _mockRepo.Verify(r => r.UpdateStatus(payId, PaymentStatus.Failed), Times.Once);
        }

        [Fact]
        public void ProcessPayment_NotificationFails_UpdatesStatusToFailed()
        {
            var resId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            SetupReservation(resId, 1, userId);
            SetupSpot(10m);
            _mockWallet.Setup(w => w.CanAfford(userId, 10m)).Returns(true);
            _mockRepo.Setup(r => r.Create(It.IsAny<Payment>())).Returns(paymentId);

            _mockNotify
                .Setup(n => n.SendPaymentConfirmationAsync(It.IsAny<string>(), It.IsAny<decimal>()))
                .ThrowsAsync(new Exception("Email service down"));

            Assert.Throws<PaymentProcessingException>(() => _service.ProcessPayment(resId));

            _mockRepo.Verify(r => r.UpdateStatus(paymentId, PaymentStatus.Failed), Times.Once);
        }

        [Fact]
        public void GetPaymentByReservation_ExistingPayment_ReturnsPayment()
        {
            var resId = Guid.NewGuid();
            var expectedPayment = new Payment(resId, 50m);
            _mockRepo.Setup(r => r.GetByReservationId(resId)).Returns(expectedPayment);

            var result = _service.GetPaymentByReservation(resId);

            Assert.NotNull(result);
            Assert.Equal(resId, result.ReservationId);
        }

        [Fact]
        public void GetPaymentByReservation_NoPayment_ReturnsNull()
        {
            _mockRepo.Setup(r => r.GetByReservationId(It.IsAny<Guid>())).Returns((Payment?)null);
            var result = _service.GetPaymentByReservation(Guid.NewGuid());
            Assert.Null(result);
        }

        [Fact]
        public void GetUserPayments_MultiplePayments_ReturnsAll()
        {
            var userId = Guid.NewGuid();
            var payments = new List<Payment>
            {
                new Payment(Guid.NewGuid(), 50m),
                new Payment(Guid.NewGuid(), 100m),
                new Payment(Guid.NewGuid(), 75m)
            };

            _mockRepo.Setup(r => r.GetByUserId(userId)).Returns(payments);

            var result = _service.GetUserPayments(userId);

            Assert.Equal(3, result.Count);
            Assert.Equal(50m, result[0].Amount);
            Assert.Equal(100m, result[1].Amount);
            Assert.Equal(75m, result[2].Amount);
        }

        [Fact]
        public void GetUserPayments_NoPayments_ReturnsEmptyList()
        {
            var userId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetByUserId(userId)).Returns(new List<Payment>());

            var result = _service.GetUserPayments(userId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // ============================================
        // HELPER METHODS
        // ============================================

        private void SetupReservation(Guid resId, double durationHours, Guid? userId = null, bool isOngoing = false)
        {
            var uId = userId ?? Guid.NewGuid();
            var sId = Guid.NewGuid();
            var startTime = DateTime.UtcNow.AddHours(-durationHours);
            var endTime = isOngoing ? (DateTime?)null : DateTime.UtcNow;

            var fakeRes = new Reservation(
                resId,
                uId,
                sId,
                startTime,
                endTime,
                isOngoing ? ReservationStatus.Pending : ReservationStatus.Completed,
                startTime.AddMinutes(15),
                startTime
            );

            _mockReservation.Setup(r => r.GetReservation(resId)).Returns(fakeRes);
        }

        private void SetupSpot(decimal rate)
        {
            var fakeSpot = new ParkingSpot(Guid.NewGuid(), "A-001", "Regular", rate, false, DateTime.UtcNow);
            _mockSpot.Setup(s => s.GetSpot(It.IsAny<Guid>())).Returns(fakeSpot);
        }
    }
}