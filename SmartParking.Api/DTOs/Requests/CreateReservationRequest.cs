using System.ComponentModel.DataAnnotations;

namespace SmartParking.Api.DTOs.Requests;

public record CreateReservationRequest(
    [Required] Guid UserId,
    [Required] Guid SpotId,
    [Range(1, 60)] int CancellationTimeoutMinutes = 15
);
