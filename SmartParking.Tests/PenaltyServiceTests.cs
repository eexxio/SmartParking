using Microsoft.Extensions.Logging;
using Moq;
using SmartParking.Business;                 
using SmartParking.Business.Interfaces;       
using SmartParking.Business.Services;         
using SmartParking.DataAccess.Interfaces;     
using SmartParking.Domain.Entities;           
using SmartParking.Domain.Enums;             
using SmartParking.Domain.Exceptions;         

namespace SmartParking.Tests;

public class PenaltyServiceTests
{
    private readonly Mock<IPenaltyRepository> _penaltyRepositoryMock;
    private readonly Mock<IReservationRepository> _reservationRepositoryMock;
    private readonly Mock<IWalletService> _walletServiceMock;
    private readonly Mock<ILogger<PenaltyService>> _loggerMock;

    private readonly PenaltyService _penaltyService;

    public PenaltyServiceTests()
    {
        _penaltyRepositoryMock = new Mock<IPenaltyRepository>();
        _reservationRepositoryMock = new Mock<IReservationRepository>();
        _walletServiceMock = new Mock<IWalletService>();
        _loggerMock = new Mock<ILogger<PenaltyService>>();

        _penaltyService = new PenaltyService(
            _penaltyRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _walletServiceMock.Object,
            _loggerMock.Object);
    }

    private static Reservation CreateReservation(Guid reservationId, Guid userId, Guid spotId)
        => new Reservation(
            id: reservationId,
            userId: userId,
            spotId: spotId,
            startTime: DateTime.UtcNow.AddMinutes(-5),
            endTime: null,
            status: ReservationStatus.Pending,
            cancellationDeadline: DateTime.UtcNow.AddMinutes(10),
            createdAt: DateTime.UtcNow.AddMinutes(-5)
        );

    [Fact]
    public void ApplyPenalty_ValidData_CreatesPenalty_AndWithdrawsFromWallet()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var spotId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.GetById(reservationId))
            .Returns(CreateReservation(reservationId, userId, spotId));

        _penaltyRepositoryMock.Setup(r => r.Create(It.IsAny<Penalty>()))
            .Returns((Penalty p) => p);

        // Act
        var result = _penaltyService.ApplyPenalty(reservationId, 10.00m, "Late cancellation");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(reservationId, result.ReservationId);
        Assert.Equal(10.00m, result.Amount);

        _penaltyRepositoryMock.Verify(r => r.Create(It.IsAny<Penalty>()), Times.Once);
        _walletServiceMock.Verify(w => w.Withdraw(userId, 10.00m), Times.Once);
    }

    [Fact]
    public void ApplyPenalty_ReservationNotFound_ThrowsException()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.GetById(reservationId))
            .Returns((Reservation?)null);

        // Act & Assert
        Assert.Throws<ReservationNotFoundException>(() =>
            _penaltyService.ApplyPenalty(reservationId, 10.00m, "Late cancellation"));

        _penaltyRepositoryMock.Verify(r => r.Create(It.IsAny<Penalty>()), Times.Never);
        _walletServiceMock.Verify(w => w.Withdraw(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public void ApplyPenalty_InvalidReason_ThrowsException()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.GetById(reservationId))
            .Returns(CreateReservation(reservationId, userId, Guid.NewGuid()));

        // Act & Assert
        Assert.Throws<InvalidPenaltyException>(() =>
            _penaltyService.ApplyPenalty(reservationId, 10.00m, "bad"));

        _penaltyRepositoryMock.Verify(r => r.Create(It.IsAny<Penalty>()), Times.Never);
        _walletServiceMock.Verify(w => w.Withdraw(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public void ApplyPenalty_InvalidAmount_ThrowsException()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.GetById(reservationId))
            .Returns(CreateReservation(reservationId, userId, Guid.NewGuid()));

        // Act & Assert
        Assert.Throws<InvalidPenaltyException>(() =>
            _penaltyService.ApplyPenalty(reservationId, 0m, "Valid reason"));

        _penaltyRepositoryMock.Verify(r => r.Create(It.IsAny<Penalty>()), Times.Never);
    }

    [Fact]
    public void ApplyPenalty_WhenWalletWithdrawThrows_PropagatesException()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.GetById(reservationId))
            .Returns(CreateReservation(reservationId, userId, Guid.NewGuid()));

        _penaltyRepositoryMock.Setup(r => r.Create(It.IsAny<Penalty>()))
            .Returns((Penalty p) => p);

        _walletServiceMock.Setup(w => w.Withdraw(userId, 10.00m))
            .Throws(new InsufficientBalanceException("Insufficient balance"));

        // Act & Assert
        Assert.Throws<InsufficientBalanceException>(() =>
            _penaltyService.ApplyPenalty(reservationId, 10.00m, "Late cancellation"));

        _penaltyRepositoryMock.Verify(r => r.Create(It.IsAny<Penalty>()), Times.Once);
    }

    [Fact]
    public void GetUserPenalties_ReturnsRepositoryList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var list = new List<Penalty>
        {
            new Penalty(Guid.NewGuid(), 10m, "Late cancellation"),
            new Penalty(Guid.NewGuid(), 5m, "Timeout penalty")
        };

        _penaltyRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(list);

        // Act
        var result = _penaltyService.GetUserPenalties(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _penaltyRepositoryMock.Verify(r => r.GetByUserId(userId), Times.Once);
    }

    [Fact]
    public void GetReservationPenalties_ReturnsRepositoryList()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        var list = new List<Penalty>
        {
            new Penalty(reservationId, 10m, "Late cancellation")
        };

        _penaltyRepositoryMock.Setup(r => r.GetByReservationId(reservationId)).Returns(list);

        // Act
        var result = _penaltyService.GetReservationPenalties(reservationId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        _penaltyRepositoryMock.Verify(r => r.GetByReservationId(reservationId), Times.Once);
    }
    [Fact]
    public void ApplyPenalty_EmptyReason_ThrowsInvalidPenaltyException()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.GetById(reservationId))
            .Returns(CreateReservation(reservationId, userId, Guid.NewGuid()));

        // Act & Assert
        Assert.Throws<InvalidPenaltyException>(() =>
            _penaltyService.ApplyPenalty(reservationId, 10m, ""));

        _penaltyRepositoryMock.Verify(r => r.Create(It.IsAny<Penalty>()), Times.Never);
        _walletServiceMock.Verify(w => w.Withdraw(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public void GetUserPenalties_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _penaltyRepositoryMock.Setup(r => r.GetByUserId(userId))
            .Throws(new InvalidOperationException("DB error"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _penaltyService.GetUserPenalties(userId));
    }

    [Fact]
    public void GetReservationPenalties_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _penaltyRepositoryMock.Setup(r => r.GetByReservationId(reservationId))
            .Throws(new InvalidOperationException("DB error"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _penaltyService.GetReservationPenalties(reservationId));
    }
    [Fact]
    public void GetReservationPenalties_NoPenalties_ReturnsEmptyList()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _penaltyRepositoryMock.Setup(r => r.GetByReservationId(reservationId))
            .Returns(new List<Penalty>());

        // Act
        var result = _penaltyService.GetReservationPenalties(reservationId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _penaltyRepositoryMock.Verify(r => r.GetByReservationId(reservationId), Times.Once);
    }



}
