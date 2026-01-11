using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.DTOs.Requests;
using SmartParking.Api.DTOs.Responses;
using SmartParking.Application.Interfaces;

namespace SmartParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(IReservationService reservationService, ILogger<ReservationsController> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new reservation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult CreateReservation([FromBody] CreateReservationRequest request)
    {
        _logger.LogInformation("Creating reservation for user {UserId} and spot {SpotId}", request.UserId, request.SpotId);

        var reservation = _reservationService.CreateReservation(
            request.UserId,
            request.SpotId,
            request.CancellationTimeoutMinutes
        );

        var response = new ReservationResponse(
            reservation.Id,
            reservation.UserId,
            reservation.SpotId,
            reservation.StartTime,
            reservation.EndTime,
            reservation.Status,
            reservation.CancellationDeadline,
            reservation.CreatedAt
        );

        return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, response);
    }

    /// <summary>
    /// Get reservation by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetReservation(Guid id)
    {
        _logger.LogInformation("Getting reservation: {ReservationId}", id);

        var reservation = _reservationService.GetReservation(id);

        var response = new ReservationResponse(
            reservation.Id,
            reservation.UserId,
            reservation.SpotId,
            reservation.StartTime,
            reservation.EndTime,
            reservation.Status,
            reservation.CancellationDeadline,
            reservation.CreatedAt
        );

        return Ok(response);
    }

    /// <summary>
    /// Get reservations for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<ReservationResponse>), StatusCodes.Status200OK)]
    public IActionResult GetUserReservations(Guid userId)
    {
        _logger.LogInformation("Getting reservations for user: {UserId}", userId);

        var reservations = _reservationService.GetUserReservations(userId);

        var response = reservations.Select(r => new ReservationResponse(
            r.Id,
            r.UserId,
            r.SpotId,
            r.StartTime,
            r.EndTime,
            r.Status,
            r.CancellationDeadline,
            r.CreatedAt
        )).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Confirm a reservation
    /// </summary>
    [HttpPatch("{id}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ConfirmReservation(Guid id)
    {
        _logger.LogInformation("Confirming reservation: {ReservationId}", id);

        _reservationService.ConfirmReservation(id);

        return NoContent();
    }

    /// <summary>
    /// Cancel a reservation
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult CancelReservation(Guid id)
    {
        _logger.LogInformation("Cancelling reservation: {ReservationId}", id);

        _reservationService.CancelReservation(id);

        return NoContent();
    }

    /// <summary>
    /// Complete a reservation
    /// </summary>
    [HttpPatch("{id}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult CompleteReservation(Guid id)
    {
        _logger.LogInformation("Completing reservation: {ReservationId}", id);

        _reservationService.CompleteReservation(id);

        return NoContent();
    }
}
