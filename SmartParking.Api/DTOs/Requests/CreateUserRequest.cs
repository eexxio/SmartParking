using System.ComponentModel.DataAnnotations;

namespace SmartParking.Api.DTOs.Requests;

public record CreateUserRequest(
    [Required][EmailAddress] string Email,
    [Required][MinLength(2)] string FullName,
    bool IsEVUser
);
