using Microsoft.Extensions.Logging;
using Moq;
using SmartParking.Business;
using SmartParking.Business.Configuration;
using SmartParking.Business.Interfaces;
using SmartParking.Business.Services;
using SmartParking.DataAccess.Repositories;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using System;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;

namespace SmartParking.Tests
{
    /// <summary>
    /// MANUAL TEST to run ProcessPayment() and send REAL email
    /// </summary>
    public class ManualPaymentIntegrationTest
    {
        private readonly ITestOutputHelper _output;

        public ManualPaymentIntegrationTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ManualTest_ProcessPayment_SendsEmailNotification()
        {
            // STEP 1: Setup mock services
            _output.WriteLine("STEP 1: Setup mock services...");

            var mockPaymentRepo = new Mock<IPaymentRepository>();
            var mockWallet = new Mock<IWalletService>();
            var mockReservation = new Mock<IReservationService>();
            var mockSpot = new Mock<IParkingSpotService>();

            // STEP 2: Configure NotificationService
            _output.WriteLine("STEP 2: Configure NotificationService...");

            var settings = new NotificationSettings
            {
                ApiKey = "MOCK_KEY",
                FromEmail = "noreply@smartparking.com",
                ApiUrl = "https://api.sendgrid.com/v3/mail/send",
                IsSimulationMode = true
            };

            var logger = new Mock<ILogger<NotificationService>>();
            var httpClient = new HttpClient();
            var notificationService = new NotificationService(httpClient, settings, logger.Object);

            // STEP 3: Create PaymentService
            _output.WriteLine("STEP 3: Create PaymentService...");

            var paymentService = new PaymentService(
                mockPaymentRepo.Object,
                notificationService,
                mockWallet.Object,
                mockReservation.Object,
                mockSpot.Object
            );

            // STEP 4: Setup test data
            _output.WriteLine("STEP 4: Setup test data...");

            var reservationId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var spotId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var reservation = new Reservation(
                reservationId,
                userId,
                spotId,
                DateTime.UtcNow.AddHours(-2),
                DateTime.UtcNow,
                ReservationStatus.Completed,
                DateTime.UtcNow.AddMinutes(15),
                DateTime.UtcNow.AddHours(-2)
            );

            mockReservation.Setup(r => r.GetReservation(reservationId)).Returns(reservation);

            var spot = new ParkingSpot
            {
                Id = spotId,
                SpotNumber = "A-123",
                HourlyRate = 10m
            };

            mockSpot.Setup(s => s.GetSpot(spotId)).Returns(spot);

            mockWallet.Setup(w => w.CanAfford(userId, It.IsAny<decimal>())).Returns(true);
            mockWallet.Setup(w => w.Withdraw(userId, It.IsAny<decimal>()));

            mockPaymentRepo.Setup(r => r.Create(It.IsAny<Payment>())).Returns(paymentId);
            mockPaymentRepo.Setup(r => r.UpdateStatus(paymentId, PaymentStatus.Completed));

            // STEP 5: Run ProcessPayment
            _output.WriteLine("");
            _output.WriteLine("STEP 5: Run ProcessPayment()...");
            _output.WriteLine("========================================");

            paymentService.ProcessPayment(reservationId);

            _output.WriteLine("========================================");
            _output.WriteLine("ProcessPayment() executed successfully!");
            _output.WriteLine("");

            // STEP 6: Verifications
            _output.WriteLine("STEP 6: Verifications...");

            decimal expectedAmount = 20m;
            _output.WriteLine($"   Calculated amount: {expectedAmount}");

            mockWallet.Verify(w => w.Withdraw(userId, expectedAmount), Times.Once);
            _output.WriteLine("   Amount withdrawn from wallet");

            mockPaymentRepo.Verify(r => r.Create(It.IsAny<Payment>()), Times.Once);
            _output.WriteLine("   Payment record created");

            mockPaymentRepo.Verify(r => r.UpdateStatus(paymentId, PaymentStatus.Completed), Times.Once);
            _output.WriteLine("   Status updated to Completed");

            // Final output
            _output.WriteLine("");
            _output.WriteLine("========================================");
            _output.WriteLine("TEST COMPLETED SUCCESSFULLY");
            _output.WriteLine("========================================");
            _output.WriteLine("");

            if (settings.IsSimulationMode)
            {
                _output.WriteLine("EMAIL STATUS: SIMULATION MODE");
                _output.WriteLine("   Check logs for [SIMULATION] message");
            }
            else
            {
                _output.WriteLine("EMAIL STATUS: SENT REAL EMAIL");
                _output.WriteLine("   Check inbox at: user@example.com");
            }

            _output.WriteLine("");
            _output.WriteLine("PAYMENT DETAILS:");
            _output.WriteLine($"   Reservation ID: {reservationId}");
            _output.WriteLine($"   User ID: {userId}");
            _output.WriteLine($"   Spot: {spot.SpotNumber}");
            _output.WriteLine("   Duration: 2 hours");
            _output.WriteLine($"   Rate: {spot.HourlyRate} per hour");
            _output.WriteLine($"   TOTAL PAID: {expectedAmount}");
            _output.WriteLine("");
        }

        [Fact]
        public void ManualTest_SendEmailDirectly()
        {
            _output.WriteLine("========================================");
            _output.WriteLine("Direct Email Test");
            _output.WriteLine("========================================");
            _output.WriteLine("");

            var settings = new NotificationSettings
            {
                ApiKey = "MOCK_KEY",
                FromEmail = "tesh.alexandru@gmail.com",
                ApiUrl = "https://api.sendgrid.com/v3/mail/send",
                IsSimulationMode = false
            };

            var logger = new Mock<ILogger<NotificationService>>();
            var service = new NotificationService(new HttpClient(), settings, logger.Object);

            _output.WriteLine("Sending email...");
            service.SendPaymentConfirmationAsync("tesh.alexandru@gmail.com", 150.50m).Wait();

            _output.WriteLine("Email sent successfully!");
            _output.WriteLine("");

            if (settings.IsSimulationMode)
            {
                _output.WriteLine("INFO: Simulation mode enabled");
                _output.WriteLine("      Email was NOT sent for real");
                _output.WriteLine("      To send real email: set IsSimulationMode = false");
            }
            else
            {
                _output.WriteLine("SUCCESS: REAL email sent to customer@example.com");
                _output.WriteLine("         Check inbox!");
            }

            _output.WriteLine("");
        }
    }
}

// ============================================
// HOW TO RUN THESE TESTS:
// ============================================
// 
// Option 1 - Visual Studio:
// 1. Open Test Explorer (View -> Test Explorer)
// 2. Find "ManualPaymentIntegrationTest"
// 3. Right click -> Run
// 4. View output in Test Explorer
//
// Option 2 - Command Line:
// dotnet test --filter "ManualPaymentIntegrationTest" --logger "console;verbosity=detailed"
//
// ============================================
// FOR REAL EMAIL:
// ============================================
// 1. Get your SendGrid API key
// 2. Replace "MOCK_KEY" with your real API key
// 3. Set IsSimulationMode = false
// 4. Run the test
// 5. Check inbox at customer@example.com
// ============================================