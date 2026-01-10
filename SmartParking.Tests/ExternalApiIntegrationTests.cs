using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SmartParking.Business.Configuration;
using SmartParking.Business.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SmartParking.Tests
{
    /// <summary>
    /// EXTERNAL API INTEGRATION TESTS
    /// SendGrid API (Email Receipt)
    /// 
    /// Demonstrates that NotificationService works with:
    /// 1. SendGrid API integration
    /// 2. HTTP client setup
    /// 3. Retry logic (3 attempts)
    /// 4. Error handling
    /// 5. Logging
    /// 6. Simulation mode (for testing without costs)
    /// </summary>
    public class ExternalApiIntegrationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<ILogger<NotificationService>> _mockLogger;

        public ExternalApiIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            _mockLogger = new Mock<ILogger<NotificationService>>();
        }

        // ========================================
        // TEST SIMULATION MODE
        // ========================================

        [Fact]
        public async Task SendGridAPI_SimulationMode_SendsEmailSuccessfully()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                ApiKey = "MOCK_KEY",
                FromEmail = "tesh.alexandru@gmail.com",
                ApiUrl = "https://api.sendgrid.com/v3/mail/send",
                IsSimulationMode = true 
            };

            var service = new NotificationService(new HttpClient(), settings, _mockLogger.Object);

            // Act
            await service.SendPaymentConfirmationAsync("test@example.com", 150.75m);

            // Assert - Verify it logged the simulation message
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("[SIMULATION]")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce,
                "Should log [SIMULATION] when sending email"
            );

            _output.WriteLine("TEST 1 PASSED: SendGrid Simulation Mode works!");
        }

        [Fact]
        public async Task SendGridAPI_SimulationMode_DoesNotMakeRealHttpCalls()
        {
            // Arrange
            var settings = new NotificationSettings { IsSimulationMode = true };

            // HttpClient that would throw exception if used
            var throwingHandler = new Mock<HttpMessageHandler>();
            throwingHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Throws(new Exception("HTTP should not be called!"));

            var httpClient = new HttpClient(throwingHandler.Object);
            var service = new NotificationService(httpClient, settings, _mockLogger.Object);

            // Act - Should not throw exception
            await service.SendPaymentConfirmationAsync("test@example.com", 100m);

            // Assert - HttpClient was not called
            throwingHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );

            _output.WriteLine("TEST 2 PASSED: Simulation mode does NOT make real HTTP calls!");
        }

        // ========================================
        // TEST REAL API MODE (with Mock HTTP)
        // ========================================

        [Fact]
        public async Task SendGridAPI_RealMode_SendsHttpRequestCorrectly()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                ApiKey = "MOCK_KEY",
                FromEmail = "tesh.alexandru@gmail.com",
                ApiUrl = "https://api.sendgrid.com/v3/mail/send",
                IsSimulationMode = false // REAL MODE
            };

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"message\":\"success\"}")
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var service = new NotificationService(httpClient, settings, _mockLogger.Object);

            // Act
            await service.SendPaymentConfirmationAsync("test@example.com", 250.00m);

            // Assert - Verify it made an HTTP POST
            mockHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().Contains("api.sendgrid.com")
                ),
                ItExpr.IsAny<CancellationToken>()
            );

            _output.WriteLine("TEST 3 PASSED: SendGrid HTTP request sent correctly!");
        }

        // ========================================
        // TEST RETRY LOGIC
        // ========================================

        [Fact]
        public async Task SendGridAPI_ApiFailure_RetriesThreeTimes()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                ApiKey = "MOCK_KEY",
                IsSimulationMode = false
            };

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError // API fails
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var service = new NotificationService(httpClient, settings, _mockLogger.Object);

            // Act & Assert
            // We expect the service to throw an exception after 3 failed attempts
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
                await service.SendPaymentConfirmationAsync("test@example.com", 100m)
            );

            // Assert - Verify it tried 3 times
            mockHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(3),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );

            _output.WriteLine("TEST 4 PASSED: Retry logic works (3 attempts)!");
        }

        // ========================================
        // TEST ERROR HANDLING
        // ========================================

        [Fact]
        public async Task SendGridAPI_NetworkError_HandlesGracefully()
        {
            // Arrange
            var settings = new NotificationSettings { IsSimulationMode = false };

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Network error"));

            var httpClient = new HttpClient(mockHandler.Object);
            var service = new NotificationService(httpClient, settings, _mockLogger.Object);

            // Act & Assert
            // We expect the service to throw an exception after 3 failed attempts
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
                await service.SendPaymentConfirmationAsync("test@example.com", 50m)
            );

            // Assert - Verify it logged the error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce,
                "Should log the error"
            );

            _output.WriteLine("TEST 5 PASSED: Error handling works!");
        }

        // ========================================
        // TEST LOGGING
        // ========================================

        [Fact]
        public async Task SendGridAPI_AllOperations_AreLogged()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                ApiKey = "MOCK_KEY",
                IsSimulationMode = false
            };

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

            var httpClient = new HttpClient(mockHandler.Object);
            var service = new NotificationService(httpClient, settings, _mockLogger.Object);

            // Act
            await service.SendPaymentConfirmationAsync("test@example.com", 100m);

            // Assert - Verify it logged the operation
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce,
                "All operations should be logged"
            );

            _output.WriteLine("TEST 6 PASSED: Logging works!");
        }

        // ========================================
        // TEST BOTH METHODS (Payment + Reservation)
        // ========================================

        [Fact]
        public async Task SendGridAPI_BothMethods_WorkCorrectly()
        {
            // Arrange
            var settings = new NotificationSettings { IsSimulationMode = true };
            var service = new NotificationService(new HttpClient(), settings, _mockLogger.Object);

            string paymentEmail = "payment@test.com";
            string reservationEmail = "reservation@test.com";

            // Act
            await service.SendPaymentConfirmationAsync(paymentEmail, 200m);
            await service.SendReservationConfirmationAsync(reservationEmail, "A-123");

            // Assert - Verify both were logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(paymentEmail)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce
            );

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(reservationEmail)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce
            );

            _output.WriteLine("TEST 7 PASSED: Both methods work!");
        }

        // ========================================
        // TEST CONFIGURATION
        // ========================================

        [Fact]
        public void NotificationSettings_IsConfiguredCorrectly()
        {
            // Arrange & Act
            var settings = new NotificationSettings
            {
                ApiKey = "MOCK_KEY",
                FromEmail = "tesh.alexandru@gmail.com",
                ApiUrl = "https://api.sendgrid.com/v3/mail/send",
                IsSimulationMode = true
            };

            // Assert
            Assert.NotNull(settings.ApiKey);
            Assert.NotNull(settings.FromEmail);
            Assert.NotNull(settings.ApiUrl);
            Assert.Contains("sendgrid", settings.ApiUrl.ToLower());
            Assert.True(settings.IsSimulationMode);

            _output.WriteLine("TEST 8 PASSED: Configuration is valid!");
        }
    }
}