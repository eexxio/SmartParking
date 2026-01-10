using Moq;
using SmartParking.Business;
using SmartParking.Business.Services;
using SmartParking.DataAccess.Repositories;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using System;
using Xunit;

namespace SmartParking.Tests
{
    public class PaymentServiceTests
    {
        private readonly Mock<IPaymentRepository> _mockRepo;
        private readonly Mock<INotificationService> _mockNotify;
        private readonly Mock<IWalletService> _mockWallet;
        private readonly PaymentService _service;

        public PaymentServiceTests()
        {
            _mockRepo = new Mock<IPaymentRepository>();
            _mockNotify = new Mock<INotificationService>();
            _mockWallet = new Mock<IWalletService>();

            _service = new PaymentService(
                _mockRepo.Object,
                _mockNotify.Object,
                _mockWallet.Object
            );
        }

        [Fact]
        public void CalculateAmount_ValidDuration_ReturnsCorrectCost()
        {
            var start = DateTime.Parse("10:00");
            var end = DateTime.Parse("12:00");
            decimal rate = 10m;

            decimal result = _service.CalculateAmount(start, end, rate);

            Assert.Equal(20m, result);
        }

        [Fact]
        public void CalculateAmount_InvalidTime_ThrowsException()
        {
            var start = DateTime.Parse("12:00");
            var end = DateTime.Parse("10:00");

            Assert.Throws<ArgumentException>(() => _service.CalculateAmount(start, end, 10m));
        }

        [Fact]
        public void ProcessPayment_Success_UpdatesStatusAndSendsEmail()
        {
            var resId = Guid.NewGuid();
            var payId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockRepo.Setup(r => r.Create(It.IsAny<Payment>())).Returns(payId);

            _service.ProcessPayment(resId, userId, "test@email.com", DateTime.Now, DateTime.Now.AddHours(1), 10m);

            _mockRepo.Verify(r => r.Create(It.IsAny<Payment>()), Times.Once);
            _mockWallet.Verify(w => w.Withdraw(userId, It.IsAny<decimal>()), Times.Once);
            _mockRepo.Verify(r => r.UpdateStatus(payId, PaymentStatus.Completed), Times.Once);
            _mockNotify.Verify(n => n.SendPaymentReceiptAsync("test@email.com", 10m), Times.Once);
        }
    }
}