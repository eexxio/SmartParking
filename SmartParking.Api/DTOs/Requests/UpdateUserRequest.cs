using System.ComponentModel.DataAnnotations;

namespace SmartParking.Api.DTOs.Requests;

public record UpdateUserRequest(
    [Required][MinLength(2)] string FullName,
    bool IsEVUser
);
