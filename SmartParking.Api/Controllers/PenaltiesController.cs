using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.DTOs.Responses;
using SmartParking.Application.Interfaces;

namespace SmartParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PenaltiesController : ControllerBase
{
    private readonly IPenaltyService _penaltyService;
    private readonly ILogger<PenaltiesController> _logger;

    public PenaltiesController(IPenaltyService penaltyService, ILogger<PenaltiesController> logger)
    {
        _penaltyService = penaltyService;
        _logger = logger;
    }

    /// <summary>
    /// Get penalties for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<PenaltyResponse>), StatusCodes.Status200OK)]
    public IActionResult GetUserPenalties(Guid userId)
    {
        _logger.LogInformation("Getting penalties for user: {UserId}", userId);

        var penalties = _penaltyService.GetUserPenalties(userId);

        var response = penalties.Select(p => new PenaltyResponse(
            p.Id,
            p.ReservationId,
            p.Amount,
            p.Reason,
            p.CreatedAt
        )).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get penalties for a reservation
    /// </summary>
    [HttpGet("reservation/{reservationId}")]
    [ProducesResponseType(typeof(List<PenaltyResponse>), StatusCodes.Status200OK)]
    public IActionResult GetReservationPenalties(Guid reservationId)
    {
        _logger.LogInformation("Getting penalties for reservation: {ReservationId}", reservationId);

        var penalties = _penaltyService.GetReservationPenalties(reservationId);

        var response = penalties.Select(p => new PenaltyResponse(
            p.Id,
            p.ReservationId,
            p.Amount,
            p.Reason,
            p.CreatedAt
        )).ToList();

        return Ok(response);
    }
}
