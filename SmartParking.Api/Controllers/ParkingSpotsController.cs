using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.DTOs.Requests;
using SmartParking.Api.DTOs.Responses;
using SmartParking.Application.Interfaces;

namespace SmartParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingSpotsController : ControllerBase
{
    private readonly IParkingSpotService _parkingSpotService;
    private readonly ILogger<ParkingSpotsController> _logger;

    public ParkingSpotsController(IParkingSpotService parkingSpotService, ILogger<ParkingSpotsController> logger)
    {
        _parkingSpotService = parkingSpotService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new parking spot
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ParkingSpotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateSpot([FromBody] CreateParkingSpotRequest request)
    {
        _logger.LogInformation("Creating parking spot at location: {Location}", request.Location);

        var spot = _parkingSpotService.CreateSpot(request.Location, request.Type, request.PricePerHour);

        var response = new ParkingSpotResponse(
            spot.Id,
            spot.SpotNumber,
            Enum.Parse<Domain.Enums.SpotType>(spot.SpotType),
            spot.HourlyRate,
            !spot.IsOccupied
        );

        return CreatedAtAction(nameof(GetSpot), new { id = spot.Id }, response);
    }

    /// <summary>
    /// Get parking spot by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ParkingSpotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetSpot(Guid id)
    {
        _logger.LogInformation("Getting parking spot: {SpotId}", id);

        var spot = _parkingSpotService.GetSpot(id);

        var response = new ParkingSpotResponse(
            spot.Id,
            spot.SpotNumber,
            Enum.Parse<Domain.Enums.SpotType>(spot.SpotType),
            spot.HourlyRate,
            !spot.IsOccupied
        );

        return Ok(response);
    }

    /// <summary>
    /// Get all parking spots
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ParkingSpotResponse>), StatusCodes.Status200OK)]
    public IActionResult GetAllSpots()
    {
        _logger.LogInformation("Getting all parking spots");

        var spots = _parkingSpotService.GetAllSpots();

        var response = spots.Select(s => new ParkingSpotResponse(
            s.Id,
            s.SpotNumber,
            Enum.Parse<Domain.Enums.SpotType>(s.SpotType),
            s.HourlyRate,
            !s.IsOccupied
        )).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get all available parking spots
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(List<ParkingSpotResponse>), StatusCodes.Status200OK)]
    public IActionResult GetAvailableSpots()
    {
        _logger.LogInformation("Getting available parking spots");

        var spots = _parkingSpotService.GetAvailableSpots();

        var response = spots.Select(s => new ParkingSpotResponse(
            s.Id,
            s.SpotNumber,
            Enum.Parse<Domain.Enums.SpotType>(s.SpotType),
            s.HourlyRate,
            !s.IsOccupied
        )).ToList();

        return Ok(response);
    }
}
