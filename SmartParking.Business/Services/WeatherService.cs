using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartParking.Business.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SmartParking.Business.Services;

/// <summary>
/// Service for retrieving weather information from OpenWeatherMap API
/// </summary>
public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly string _apiKey;
    private readonly string _apiUrl;
    private readonly string _defaultLocation;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 500;

    public WeatherService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["WeatherSettings:ApiKey"]
            ?? throw new InvalidOperationException("WeatherSettings:ApiKey is not configured");
        _apiUrl = configuration["WeatherSettings:ApiUrl"]
            ?? "https://api.openweathermap.org/data/2.5/weather";
        _defaultLocation = configuration["WeatherSettings:DefaultLocation"] ?? "Bucharest,RO";
    }

    /// <summary>
    /// Gets current weather information for a location with retry logic
    /// </summary>
    public async Task<WeatherInfo?> GetCurrentWeatherAsync(string location)
    {
        location = string.IsNullOrWhiteSpace(location) ? _defaultLocation : location;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Fetching weather for location: {Location} (Attempt {Attempt}/{MaxRetries})",
                    location, attempt, MaxRetries);

                var url = $"{_apiUrl}?q={location}&appid={_apiKey}&units=metric";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Weather API returned status code: {StatusCode}", response.StatusCode);

                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(RetryDelayMs);
                        continue;
                    }

                    return null;
                }

                var weatherResponse = await response.Content.ReadFromJsonAsync<OpenWeatherMapResponse>();

                if (weatherResponse == null)
                {
                    _logger.LogWarning("Failed to deserialize weather response");
                    return null;
                }

                var weatherInfo = new WeatherInfo
                {
                    Location = weatherResponse.Name,
                    Temperature = weatherResponse.Main.Temp,
                    Description = weatherResponse.Weather.FirstOrDefault()?.Description ?? "N/A",
                    Icon = weatherResponse.Weather.FirstOrDefault()?.Icon ?? "",
                    Humidity = weatherResponse.Main.Humidity,
                    WindSpeed = weatherResponse.Wind.Speed
                };

                _logger.LogInformation("Weather retrieved successfully for {Location}: {Temperature}°C, {Description}",
                    weatherInfo.Location, weatherInfo.Temperature, weatherInfo.Description);

                return weatherInfo;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching weather (Attempt {Attempt}/{MaxRetries})", attempt, MaxRetries);

                if (attempt < MaxRetries)
                {
                    await Task.Delay(RetryDelayMs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching weather for {Location}", location);
                return null;
            }
        }

        _logger.LogError("Failed to fetch weather after {MaxRetries} attempts", MaxRetries);
        return null;
    }

    #region OpenWeatherMap API Response Models

    private class OpenWeatherMapResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("main")]
        public MainData Main { get; set; } = new();

        [JsonPropertyName("weather")]
        public List<WeatherData> Weather { get; set; } = new();

        [JsonPropertyName("wind")]
        public WindData Wind { get; set; } = new();
    }

    private class MainData
    {
        [JsonPropertyName("temp")]
        public decimal Temp { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }
    }

    private class WeatherData
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;
    }

    private class WindData
    {
        [JsonPropertyName("speed")]
        public decimal Speed { get; set; }
    }

    #endregion
}
