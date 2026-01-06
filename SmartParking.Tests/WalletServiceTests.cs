using Microsoft.Extensions.Logging;
using Moq;
using SmartParking.Business;
using SmartParking.DataAccess;
using SmartParking.Domain;
using SmartParking.Domain.Exceptions;

namespace SmartParking.Tests;

public class WalletServiceTests
{
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly Mock<ILogger<WalletService>> _loggerMock;
    private readonly WalletService _walletService;

    public WalletServiceTests()
    {
        _walletRepositoryMock = new Mock<IWalletRepository>();
        _loggerMock = new Mock<ILogger<WalletService>>();
        _walletService = new WalletService(
            _walletRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void GetBalance_ExistingWallet_ReturnsBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedBalance = 150.50m;
        var wallet = new UserWallet(userId, expectedBalance);

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(wallet);

        // Act
        var result = _walletService.GetBalance(userId);

        // Assert
        Assert.Equal(expectedBalance, result);
        _walletRepositoryMock.Verify(r => r.GetByUserId(userId), Times.Once);
    }

    [Fact]
    public void GetBalance_NonExistingWallet_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns((UserWallet?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _walletService.GetBalance(userId));

        Assert.Contains("Wallet not found", exception.Message);
    }

    [Fact]
    public void Deposit_ValidAmount_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var initialBalance = 100m;
        var depositAmount = 50m;
        var wallet = new UserWallet(userId, initialBalance);

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(wallet);
        _walletRepositoryMock.Setup(r => r.UpdateBalance(It.IsAny<Guid>(), It.IsAny<decimal>()));

        // Act
        _walletService.Deposit(userId, depositAmount);

        // Assert
        Assert.Equal(150m, wallet.Balance);
        _walletRepositoryMock.Verify(r => r.UpdateBalance(wallet.Id, 150m), Times.Once);
    }

    [Fact]
    public void Deposit_NegativeAmount_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var negativeAmount = -50m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _walletService.Deposit(userId, negativeAmount));

        Assert.Contains("must be greater than zero", exception.Message);
        _walletRepositoryMock.Verify(r => r.UpdateBalance(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public void Deposit_ZeroAmount_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var zeroAmount = 0m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _walletService.Deposit(userId, zeroAmount));

        Assert.Contains("must be greater than zero", exception.Message);
        _walletRepositoryMock.Verify(r => r.UpdateBalance(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public void Deposit_NonExistingWallet_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var depositAmount = 50m;

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns((UserWallet?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _walletService.Deposit(userId, depositAmount));

        Assert.Contains("Wallet not found", exception.Message);
    }

    [Fact]
    public void Withdraw_SufficientBalance_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var initialBalance = 100m;
        var withdrawAmount = 30m;
        var wallet = new UserWallet(userId, initialBalance);

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(wallet);
        _walletRepositoryMock.Setup(r => r.UpdateBalance(It.IsAny<Guid>(), It.IsAny<decimal>()));

        // Act
        _walletService.Withdraw(userId, withdrawAmount);

        // Assert
        Assert.Equal(70m, wallet.Balance);
        _walletRepositoryMock.Verify(r => r.UpdateBalance(wallet.Id, 70m), Times.Once);
    }

    [Fact]
    public void Withdraw_InsufficientBalance_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var initialBalance = 50m;
        var withdrawAmount = 100m;
        var wallet = new UserWallet(userId, initialBalance);

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(wallet);

        // Act & Assert
        var exception = Assert.Throws<InsufficientBalanceException>(() =>
            _walletService.Withdraw(userId, withdrawAmount));

        Assert.Contains("Insufficient balance", exception.Message);
        _walletRepositoryMock.Verify(r => r.UpdateBalance(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public void Withdraw_NegativeAmount_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var negativeAmount = -20m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _walletService.Withdraw(userId, negativeAmount));

        Assert.Contains("must be greater than zero", exception.Message);
        _walletRepositoryMock.Verify(r => r.UpdateBalance(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public void Withdraw_ZeroAmount_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var zeroAmount = 0m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _walletService.Withdraw(userId, zeroAmount));

        Assert.Contains("must be greater than zero", exception.Message);
        _walletRepositoryMock.Verify(r => r.UpdateBalance(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public void Withdraw_NonExistingWallet_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var withdrawAmount = 50m;

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns((UserWallet?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _walletService.Withdraw(userId, withdrawAmount));

        Assert.Contains("Wallet not found", exception.Message);
    }

    [Fact]
    public void CanAfford_SufficientBalance_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var balance = 100m;
        var requiredAmount = 50m;
        var wallet = new UserWallet(userId, balance);

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(wallet);

        // Act
        var result = _walletService.CanAfford(userId, requiredAmount);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanAfford_InsufficientBalance_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var balance = 30m;
        var requiredAmount = 50m;
        var wallet = new UserWallet(userId, balance);

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(wallet);

        // Act
        var result = _walletService.CanAfford(userId, requiredAmount);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanAfford_ExactBalance_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var balance = 50m;
        var requiredAmount = 50m;
        var wallet = new UserWallet(userId, balance);

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(wallet);

        // Act
        var result = _walletService.CanAfford(userId, requiredAmount);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanAfford_NonExistingWallet_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requiredAmount = 50m;

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns((UserWallet?)null);

        // Act
        var result = _walletService.CanAfford(userId, requiredAmount);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanAfford_NegativeAmount_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var balance = 100m;
        var negativeAmount = -50m;
        var wallet = new UserWallet(userId, balance);

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(wallet);

        // Act
        var result = _walletService.CanAfford(userId, negativeAmount);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Deposit_UpdatesBalanceCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var initialBalance = 75.25m;
        var depositAmount = 24.75m;
        var wallet = new UserWallet(userId, initialBalance);

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(wallet);
        _walletRepositoryMock.Setup(r => r.UpdateBalance(It.IsAny<Guid>(), It.IsAny<decimal>()));

        // Act
        _walletService.Deposit(userId, depositAmount);

        // Assert
        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_UpdatesBalanceCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var initialBalance = 200.50m;
        var withdrawAmount = 50.25m;
        var wallet = new UserWallet(userId, initialBalance);

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(wallet);
        _walletRepositoryMock.Setup(r => r.UpdateBalance(It.IsAny<Guid>(), It.IsAny<decimal>()));

        // Act
        _walletService.Withdraw(userId, withdrawAmount);

        // Assert
        Assert.Equal(150.25m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_ExactBalance_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var initialBalance = 100m;
        var withdrawAmount = 100m;
        var wallet = new UserWallet(userId, initialBalance);

        _walletRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(wallet);
        _walletRepositoryMock.Setup(r => r.UpdateBalance(It.IsAny<Guid>(), It.IsAny<decimal>()));

        // Act
        _walletService.Withdraw(userId, withdrawAmount);

        // Assert
        Assert.Equal(0m, wallet.Balance);
        _walletRepositoryMock.Verify(r => r.UpdateBalance(wallet.Id, 0m), Times.Once);
    }
}
