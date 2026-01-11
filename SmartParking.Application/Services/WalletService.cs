using Microsoft.Extensions.Logging;
using SmartParking.Application.Interfaces;
using SmartParking.Infrastructure.Interfaces;
using SmartParking.Domain.Exceptions;

namespace SmartParking.Application.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;
    private readonly ILogger<WalletService> _logger;

    public WalletService(
        IWalletRepository walletRepository,
        ILogger<WalletService> logger)
    {
        _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public decimal GetBalance(Guid userId)
    {
        try
        {
            _logger.LogInformation("Retrieving balance for user: {UserId}", userId);

            var wallet = _walletRepository.GetByUserId(userId);
            if (wallet == null)
            {
                _logger.LogWarning("Wallet not found for user: {UserId}", userId);
                throw new InvalidOperationException($"Wallet not found for user {userId}");
            }

            _logger.LogInformation("Balance retrieved for user {UserId}: {Balance}", userId, wallet.Balance);
            return wallet.Balance;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error retrieving balance for user: {UserId}", userId);
            throw;
        }
    }

    public void Deposit(Guid userId, decimal amount)
    {
        try
        {
            _logger.LogInformation("Attempting to deposit {Amount} for user: {UserId}", amount, userId);

            if (amount <= 0)
            {
                _logger.LogWarning("Deposit failed: Invalid amount {Amount} for user {UserId}", amount, userId);
                throw new ArgumentException("Deposit amount must be greater than zero", nameof(amount));
            }

            var wallet = _walletRepository.GetByUserId(userId);
            if (wallet == null)
            {
                _logger.LogWarning("Deposit failed: Wallet not found for user: {UserId}", userId);
                throw new InvalidOperationException($"Wallet not found for user {userId}");
            }

            wallet.Deposit(amount);
            _walletRepository.UpdateBalance(wallet.Id, wallet.Balance);

            _logger.LogInformation("Deposit successful: {Amount} added to user {UserId}. New balance: {Balance}", amount, userId, wallet.Balance);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during deposit of {Amount} for user: {UserId}", amount, userId);
            throw;
        }
    }

    public void Withdraw(Guid userId, decimal amount)
    {
        try
        {
            _logger.LogInformation("Attempting to withdraw {Amount} for user: {UserId}", amount, userId);

            if (amount <= 0)
            {
                _logger.LogWarning("Withdrawal failed: Invalid amount {Amount} for user {UserId}", amount, userId);
                throw new ArgumentException("Withdrawal amount must be greater than zero", nameof(amount));
            }

            var wallet = _walletRepository.GetByUserId(userId);
            if (wallet == null)
            {
                _logger.LogWarning("Withdrawal failed: Wallet not found for user: {UserId}", userId);
                throw new InvalidOperationException($"Wallet not found for user {userId}");
            }

            if (!wallet.CanWithdraw(amount))
            {
                _logger.LogWarning("Withdrawal failed: Insufficient balance. User {UserId} tried to withdraw {Amount} but has {Balance}", userId, amount, wallet.Balance);
                throw new InsufficientBalanceException($"Insufficient balance. Available: {wallet.Balance}, Required: {amount}");
            }

            wallet.Withdraw(amount);
            _walletRepository.UpdateBalance(wallet.Id, wallet.Balance);

            _logger.LogInformation("Withdrawal successful: {Amount} deducted from user {UserId}. New balance: {Balance}", amount, userId, wallet.Balance);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InsufficientBalanceException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during withdrawal of {Amount} for user: {UserId}", amount, userId);
            throw;
        }
    }

    public bool CanAfford(Guid userId, decimal amount)
    {
        try
        {
            _logger.LogInformation("Checking if user {UserId} can afford {Amount}", userId, amount);

            var wallet = _walletRepository.GetByUserId(userId);
            if (wallet == null)
            {
                _logger.LogWarning("Wallet not found for user: {UserId}", userId);
                return false;
            }

            var canAfford = wallet.CanWithdraw(amount);
            _logger.LogInformation("User {UserId} can afford {Amount}: {CanAfford}", userId, amount, canAfford);
            return canAfford;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking affordability for user: {UserId}", userId);
            throw;
        }
    }
}
