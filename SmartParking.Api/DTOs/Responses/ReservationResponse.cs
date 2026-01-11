using SmartParking.Domain.Enums;

namespace SmartParking.Api.DTOs.Responses;

public record ReservationResponse(
    Guid Id,
    Guid UserId,
    Guid SpotId,
    DateTime StartTime,
    DateTime? EndTime,
    ReservationStatus Status,
    DateTime CancellationDeadline,
    DateTime CreatedAt
);
