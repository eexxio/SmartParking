using Moq;
using Moq.Protected;
using SmartParking.Application.Configuration;
using SmartParking.Application.Services;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SmartParking.Tests
{
    public class NotificationServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<NotificationService>> _mockLogger;
        private readonly NotificationSettings _settings;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _mockLogger = new Mock<ILogger<NotificationService>>();

            _settings = new NotificationSettings
            {
                IsSimulationMode = false,
                ApiKey = "MOCK_KEY",
                ApiUrl = "http://test-api.com"
            };

            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            _service = new NotificationService(httpClient, _settings, _mockLogger.Object);
        }

        [Fact]
        public async Task SendPaymentConfirmation_ValidData_SendsRequest()
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                });

            // Act
            await _service.SendPaymentConfirmationAsync("tesh.alexandru@gmail.com", 100m);

            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString() == "http://test-api.com/"),
                ItExpr.IsAny<CancellationToken>()
            );
        }
        [Fact]
        public async Task SendPaymentConfirmation_ApiFailure_HandlesGracefully_AndRetries()
        {
            // Arrange
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            // ACT & ASSERT
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
                await _service.SendPaymentConfirmationAsync("tesh.alexandru@gmail.com", 100m)
            );

            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(3),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task SendReservationConfirmation_SimulationMode_DoesNotCallHttp()
        {
            var simSettings = new NotificationSettings { IsSimulationMode = true };
            var service = new NotificationService(new HttpClient(), simSettings, _mockLogger.Object);

            // Act
            await service.SendReservationConfirmationAsync("tesh.alexandru@gmail.com", "A1");

        }
    }
}