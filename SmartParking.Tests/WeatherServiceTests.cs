using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SmartParking.Application.Interfaces;
using SmartParking.Application.Services;
using System.Net;
using System.Net.Http;
using Xunit;

namespace SmartParking.Tests;

public class WeatherServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<WeatherService>> _loggerMock;
    private readonly WeatherService _service;

    public WeatherServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<WeatherService>>();

        // Setup configuration
        _configurationMock.Setup(c => c["WeatherSettings:ApiKey"])
                         .Returns("test_api_key");
        _configurationMock.Setup(c => c["WeatherSettings:ApiUrl"])
                         .Returns("https://api.openweathermap.org/data/2.5/weather");
        _configurationMock.Setup(c => c["WeatherSettings:DefaultLocation"])
                         .Returns("Bucharest,RO");

        _service = new WeatherService(_httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCurrentWeather_ValidLocation_ReturnsWeatherInfo()
    {
        // Arrange
        var location = "Bucharest,RO";
        var jsonResponse = @"{
            ""name"": ""Bucharest"",
            ""main"": {
                ""temp"": 15.5,
                ""humidity"": 65
            },
            ""weather"": [
                {
                    ""description"": ""clear sky"",
                    ""icon"": ""01d""
                }
            ],
            ""wind"": {
                ""speed"": 3.5
            }
        }";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _service.GetCurrentWeatherAsync(location);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bucharest", result.Location);
        Assert.Equal(15.5m, result.Temperature);
        Assert.Equal("clear sky", result.Description);
        Assert.Equal("01d", result.Icon);
        Assert.Equal(65, result.Humidity);
        Assert.Equal(3.5m, result.WindSpeed);
    }

    [Fact]
    public async Task GetCurrentWeather_ApiFailure_HandlesGracefully()
    {
        // Arrange
        var location = "InvalidCity";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"message\":\"city not found\"}")
            });

        // Act
        var result = await _service.GetCurrentWeatherAsync(location);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentWeather_InvalidApiKey_ThrowsOrReturnsNull()
    {
        // Arrange
        var location = "Bucharest";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"message\":\"Invalid API key\"}")
            });

        // Act
        var result = await _service.GetCurrentWeatherAsync(location);

        // Assert
        Assert.Null(result);
    }
}
