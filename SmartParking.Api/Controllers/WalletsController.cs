using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.DTOs.Requests;
using SmartParking.Application.Interfaces;

namespace SmartParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ILogger<WalletsController> _logger;

    public WalletsController(IWalletService walletService, ILogger<WalletsController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    /// <summary>
    /// Get wallet balance for a user
    /// </summary>
    [HttpGet("{userId}/balance")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetBalance(Guid userId)
    {
        _logger.LogInformation("Getting balance for user: {UserId}", userId);

        var balance = _walletService.GetBalance(userId);

        return Ok(new { userId, balance });
    }

    /// <summary>
    /// Deposit money into user wallet
    /// </summary>
    [HttpPost("{userId}/deposit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Deposit(Guid userId, [FromBody] DepositRequest request)
    {
        _logger.LogInformation("Depositing {Amount} for user: {UserId}", request.Amount, userId);

        _walletService.Deposit(userId, request.Amount);

        return NoContent();
    }

    /// <summary>
    /// Withdraw money from user wallet
    /// </summary>
    [HttpPost("{userId}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Withdraw(Guid userId, [FromBody] WithdrawRequest request)
    {
        _logger.LogInformation("Withdrawing {Amount} for user: {UserId}", request.Amount, userId);

        _walletService.Withdraw(userId, request.Amount);

        return NoContent();
    }
}
