using SmartParking.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace SmartParking.Api.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            UserNotFoundException => HttpStatusCode.NotFound,
            InvalidUserDataException => HttpStatusCode.BadRequest,
            InvalidSpotDataException => HttpStatusCode.BadRequest,
            SpotNotAvailableException => HttpStatusCode.Conflict,
            InvalidReservationException => HttpStatusCode.BadRequest,
            ReservationNotFoundException => HttpStatusCode.NotFound,
            InsufficientBalanceException => HttpStatusCode.PaymentRequired,
            InvalidPenaltyException => HttpStatusCode.BadRequest,
            PaymentNotFoundException => HttpStatusCode.NotFound,
            InvalidPaymentException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        var response = new
        {
            error = exception.Message,
            statusCode = (int)statusCode,
            timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
