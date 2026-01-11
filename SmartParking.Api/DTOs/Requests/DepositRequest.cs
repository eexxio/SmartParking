using System.ComponentModel.DataAnnotations;

namespace SmartParking.Api.DTOs.Requests;

public record DepositRequest(
    [Required][Range(0.01, 10000)] decimal Amount
);
