using SmartParking.Domain.Enums;

namespace SmartParking.Api.DTOs.Responses;

public record ParkingSpotResponse(
    Guid Id,
    string Location,
    SpotType Type,
    decimal PricePerHour,
    bool IsAvailable
);
