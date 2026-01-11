namespace SmartParking.Api.DTOs.Responses;

public record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    bool IsEVUser,
    DateTime CreatedAt,
    bool IsActive
);
