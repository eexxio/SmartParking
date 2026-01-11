namespace SmartParking.Api.DTOs.Responses;

public record PenaltyResponse(
    Guid Id,
    Guid ReservationId,
    decimal Amount,
    string Reason,
    DateTime CreatedAt
);
