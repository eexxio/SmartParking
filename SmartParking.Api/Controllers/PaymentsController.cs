using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.DTOs.Responses;
using SmartParking.Application.Interfaces;

namespace SmartParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Calculate payment amount for a reservation
    /// </summary>
    [HttpGet("calculate/{reservationId}")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult CalculatePayment(Guid reservationId)
    {
        _logger.LogInformation("Calculating payment for reservation: {ReservationId}", reservationId);

        var amount = _paymentService.CalculatePaymentAmount(reservationId);

        return Ok(new { reservationId, amount });
    }

    /// <summary>
    /// Process payment for a reservation
    /// </summary>
    [HttpPost("{reservationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ProcessPayment(Guid reservationId)
    {
        _logger.LogInformation("Processing payment for reservation: {ReservationId}", reservationId);

        _paymentService.ProcessPayment(reservationId);

        return NoContent();
    }

    /// <summary>
    /// Get payment by reservation ID
    /// </summary>
    [HttpGet("reservation/{reservationId}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetPaymentByReservation(Guid reservationId)
    {
        _logger.LogInformation("Getting payment for reservation: {ReservationId}", reservationId);

        var payment = _paymentService.GetPaymentByReservation(reservationId);

        var response = new PaymentResponse(
            payment.Id,
            payment.ReservationId,
            payment.Amount,
            payment.PaymentStatus,
            payment.CreatedAt
        );

        return Ok(response);
    }

    /// <summary>
    /// Get payment history for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<PaymentResponse>), StatusCodes.Status200OK)]
    public IActionResult GetUserPayments(Guid userId)
    {
        _logger.LogInformation("Getting payment history for user: {UserId}", userId);

        var payments = _paymentService.GetUserPayments(userId);

        var response = payments.Select(p => new PaymentResponse(
            p.Id,
            p.ReservationId,
            p.Amount,
            p.PaymentStatus,
            p.CreatedAt
        )).ToList();

        return Ok(response);
    }
}
