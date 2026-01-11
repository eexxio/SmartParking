using SmartParking.Domain.Enums;

namespace SmartParking.Api.DTOs.Responses;

public record PaymentResponse(
    Guid Id,
    Guid ReservationId,
    decimal Amount,
    PaymentStatus PaymentStatus,
    DateTime CreatedAt
);
