namespace SmartParking.Api.DTOs.Responses;

public record WalletResponse(
    Guid Id,
    Guid UserId,
    decimal Balance,
    DateTime CreatedAt
);
