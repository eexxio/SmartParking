namespace SmartParking.Application.Interfaces;

/// <summary>
/// Service interface for weather information
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Gets current weather information for a location
    /// </summary>
    Task<WeatherInfo?> GetCurrentWeatherAsync(string location);
}

/// <summary>
/// Weather information data model
/// </summary>
public class WeatherInfo
{
    public string Location { get; set; } = string.Empty;
    public decimal Temperature { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public decimal WindSpeed { get; set; }
}
