using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.DTOs.Requests;
using SmartParking.Api.DTOs.Responses;
using SmartParking.Application.Interfaces;

namespace SmartParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RegisterUser([FromBody] CreateUserRequest request)
    {
        _logger.LogInformation("Registering new user: {Email}", request.Email);

        var user = _userService.RegisterUser(request.Email, request.FullName, request.IsEVUser);

        var response = new UserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.IsEVUser,
            user.CreatedAt,
            user.IsActive
        );

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetUser(Guid id)
    {
        _logger.LogInformation("Getting user: {UserId}", id);

        var user = _userService.GetUser(id);

        var response = new UserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.IsEVUser,
            user.CreatedAt,
            user.IsActive
        );

        return Ok(response);
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        _logger.LogInformation("Updating user: {UserId}", id);

        _userService.UpdateUser(id, request.FullName, request.IsEVUser);

        return NoContent();
    }
}
