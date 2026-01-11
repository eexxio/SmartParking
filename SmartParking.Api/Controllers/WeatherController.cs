using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.DTOs.Responses;
using SmartParking.Application.Interfaces;

namespace SmartParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    /// <summary>
    /// Get current weather for a location
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWeather([FromQuery] string location = "Bucharest,RO")
    {
        _logger.LogInformation("Getting weather for location: {Location}", location);

        var weather = await _weatherService.GetCurrentWeatherAsync(location);

        if (weather == null)
        {
            return NotFound(new { error = $"Weather data not found for location: {location}" });
        }

        var response = new WeatherResponse(
            weather.Location,
            (double)weather.Temperature,
            weather.Description,
            weather.Humidity,
            (double)weather.WindSpeed
        );

        return Ok(response);
    }
}
