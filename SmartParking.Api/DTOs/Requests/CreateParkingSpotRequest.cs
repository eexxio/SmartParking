using System.ComponentModel.DataAnnotations;
using SmartParking.Domain.Enums;

namespace SmartParking.Api.DTOs.Requests;

public record CreateParkingSpotRequest(
    [Required][MinLength(1)] string Location,
    [Required] SpotType Type,
    [Range(0.01, double.MaxValue)] decimal PricePerHour
);
