using Microsoft.Extensions.Logging;
using Moq;
using SmartParking.Application.Interfaces;
using SmartParking.Application.Services;         
using SmartParking.Infrastructure.Interfaces;     
using SmartParking.Domain;                   
using SmartParking.Domain.Entities;          
using SmartParking.Domain.Enums;            
using SmartParking.Domain.Exceptions;       

namespace SmartParking.Tests;

public class ReservationServiceTests
{
    private readonly Mock<IReservationRepository> _reservationRepositoryMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IPenaltyService> _penaltyServiceMock;
    private readonly Mock<ILogger<ReservationService>> _loggerMock;

    private readonly ReservationService _reservationService;

    public ReservationServiceTests()
    {
        _reservationRepositoryMock = new Mock<IReservationRepository>();
        _userServiceMock = new Mock<IUserService>();
        _penaltyServiceMock = new Mock<IPenaltyService>();
        _loggerMock = new Mock<ILogger<ReservationService>>();

        _reservationService = new ReservationService(
            _reservationRepositoryMock.Object,
            _userServiceMock.Object,
            _penaltyServiceMock.Object,
            _loggerMock.Object);
    }

    private static User CreateUser(Guid id, bool isEVUser)
        => new User(id, "test@email.com", "Test User", isEVUser, DateTime.UtcNow, true);

    private static Reservation CreateReservation(Guid reservationId, Guid userId, Guid spotId, ReservationStatus status)
        => new Reservation(
            id: reservationId,
            userId: userId,
            spotId: spotId,
            startTime: DateTime.UtcNow.AddMinutes(-2),
            endTime: null,
            status: status,
            cancellationDeadline: DateTime.UtcNow.AddMinutes(10),
            createdAt: DateTime.UtcNow.AddMinutes(-2)
        );

    [Fact]
    public void CreateReservation_ValidData_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var spotId = Guid.NewGuid();

        var user = CreateUser(userId, isEVUser: false);

        _userServiceMock.Setup(s => s.GetUser(userId)).Returns(user);

        var expectedReservation = CreateReservation(Guid.NewGuid(), userId, spotId, ReservationStatus.Pending);

        _reservationRepositoryMock.Setup(r => r.Create(userId, spotId, 15))
            .Returns(expectedReservation);

        // Act
        var result = _reservationService.CreateReservation(userId, spotId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedReservation.Id, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(spotId, result.SpotId);

        _reservationRepositoryMock.Verify(r => r.ValidateSpotForUser(spotId, false), Times.Once);
        _reservationRepositoryMock.Verify(r => r.Create(userId, spotId, 15), Times.Once);
    }

    [Fact]
    public void CreateReservation_EVUser_PassesTrueToValidateSpotForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var spotId = Guid.NewGuid();

        var user = CreateUser(userId, isEVUser: true);
        _userServiceMock.Setup(s => s.GetUser(userId)).Returns(user);

        _reservationRepositoryMock.Setup(r => r.Create(userId, spotId, 15))
            .Returns(CreateReservation(Guid.NewGuid(), userId, spotId, ReservationStatus.Pending));

        // Act
        _reservationService.CreateReservation(userId, spotId);

        // Assert
        _reservationRepositoryMock.Verify(r => r.ValidateSpotForUser(spotId, true), Times.Once);
    }

    [Fact]
    public void CreateReservation_WhenValidateSpotThrows_ThrowsSameException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var spotId = Guid.NewGuid();

        _userServiceMock.Setup(s => s.GetUser(userId))
            .Returns(CreateUser(userId, isEVUser: false));

        _reservationRepositoryMock.Setup(r => r.ValidateSpotForUser(spotId, false))
            .Throws(new InvalidReservationException("Spot invalid"));

        // Act & Assert
        Assert.Throws<InvalidReservationException>(() =>
            _reservationService.CreateReservation(userId, spotId));

        _reservationRepositoryMock.Verify(r => r.Create(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void ConfirmReservation_ValidId_CallsRepository()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        // Act
        _reservationService.ConfirmReservation(reservationId);

        // Assert
        _reservationRepositoryMock.Verify(r => r.Confirm(reservationId), Times.Once);
    }

    [Fact]
    public void CancelReservation_NotLate_DoesNotApplyPenalty()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.Cancel(reservationId)).Returns(false);

        // Act
        _reservationService.CancelReservation(reservationId);

        // Assert
        _reservationRepositoryMock.Verify(r => r.Cancel(reservationId), Times.Once);
        _penaltyServiceMock.Verify(p => p.ApplyPenalty(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CancelReservation_Late_AppliesPenalty()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.Cancel(reservationId)).Returns(true);

        // Act
        _reservationService.CancelReservation(reservationId);

        // Assert
        _penaltyServiceMock.Verify(p => p.ApplyPenalty(reservationId, 10.00m, "Late cancellation"), Times.Once);
    }

    [Fact]
    public void CompleteReservation_ValidId_CallsRepository()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        // Act
        _reservationService.CompleteReservation(reservationId);

        // Assert
        _reservationRepositoryMock.Verify(r => r.Complete(reservationId), Times.Once);
    }

    [Fact]
    public void GetUserReservations_ReturnsRepositoryList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var list = new List<Reservation>
        {
            CreateReservation(Guid.NewGuid(), userId, Guid.NewGuid(), ReservationStatus.Pending),
            CreateReservation(Guid.NewGuid(), userId, Guid.NewGuid(), ReservationStatus.Completed)
        };

        _reservationRepositoryMock.Setup(r => r.GetByUserId(userId)).Returns(list);

        // Act
        var result = _reservationService.GetUserReservations(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _reservationRepositoryMock.Verify(r => r.GetByUserId(userId), Times.Once);
    }

    [Fact]
    public void CheckAndApplyTimeoutPenalties_NoExpired_Returns0()
    {
        // Arrange
        _reservationRepositoryMock.Setup(r => r.GetExpiredPendingReservations())
            .Returns(new List<Reservation>());

        // Act
        var processed = _reservationService.CheckAndApplyTimeoutPenalties();

        // Assert
        Assert.Equal(0, processed);
        _reservationRepositoryMock.Verify(r => r.GetExpiredPendingReservations(), Times.Once);
        _reservationRepositoryMock.Verify(r => r.Cancel(It.IsAny<Guid>()), Times.Never);
        _penaltyServiceMock.Verify(p => p.ApplyPenalty(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CheckAndApplyTimeoutPenalties_WithExpired_CancelsAllAndPenalizesLateOnes()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var r1 = CreateReservation(Guid.NewGuid(), userId, Guid.NewGuid(), ReservationStatus.Pending);
        var r2 = CreateReservation(Guid.NewGuid(), userId, Guid.NewGuid(), ReservationStatus.Pending);

        _reservationRepositoryMock.Setup(r => r.GetExpiredPendingReservations())
            .Returns(new List<Reservation> { r1, r2 });

        // r1 late, r2 not late
        _reservationRepositoryMock.SetupSequence(r => r.Cancel(It.IsAny<Guid>()))
            .Returns(true)
            .Returns(false);

        // Act
        var processed = _reservationService.CheckAndApplyTimeoutPenalties();

        // Assert
        Assert.Equal(2, processed);

        _reservationRepositoryMock.Verify(r => r.Cancel(r1.Id), Times.Once);
        _reservationRepositoryMock.Verify(r => r.Cancel(r2.Id), Times.Once);

        _penaltyServiceMock.Verify(p => p.ApplyPenalty(r1.Id, 10.00m, "Reservation timeout"), Times.Once);
        _penaltyServiceMock.Verify(p => p.ApplyPenalty(r2.Id, 10.00m, "Reservation timeout"), Times.Never);
    }
    [Fact]
    public void CreateReservation_CustomTimeout_PassesCorrectValueToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var spotId = Guid.NewGuid();
        var timeout = 3;

        _userServiceMock.Setup(s => s.GetUser(userId))
            .Returns(CreateUser(userId, isEVUser: false));

        _reservationRepositoryMock.Setup(r => r.Create(userId, spotId, timeout))
            .Returns(CreateReservation(Guid.NewGuid(), userId, spotId, ReservationStatus.Pending));

        // Act
        _reservationService.CreateReservation(userId, spotId, timeout);

        // Assert
        _reservationRepositoryMock.Verify(r => r.Create(userId, spotId, timeout), Times.Once);
    }

    [Fact]
    public void CreateReservation_WhenUserServiceThrows_PropagatesException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var spotId = Guid.NewGuid();

        _userServiceMock.Setup(s => s.GetUser(userId))
            .Throws(new UserNotFoundException($"User {userId} not found"));

        // Act & Assert
        Assert.Throws<UserNotFoundException>(() =>
            _reservationService.CreateReservation(userId, spotId));

        _reservationRepositoryMock.Verify(r => r.ValidateSpotForUser(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        _reservationRepositoryMock.Verify(r => r.Create(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void ConfirmReservation_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.Confirm(reservationId))
            .Throws(new ReservationNotFoundException("Not found"));

        // Act & Assert
        Assert.Throws<ReservationNotFoundException>(() =>
            _reservationService.ConfirmReservation(reservationId));
    }

    [Fact]
    public void CancelReservation_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.Cancel(reservationId))
            .Throws(new ReservationNotFoundException("Not found"));

        // Act & Assert
        Assert.Throws<ReservationNotFoundException>(() =>
            _reservationService.CancelReservation(reservationId));

        _penaltyServiceMock.Verify(p => p.ApplyPenalty(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CompleteReservation_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.Complete(reservationId))
            .Throws(new ReservationNotFoundException("Not found"));

        // Act & Assert
        Assert.Throws<ReservationNotFoundException>(() =>
            _reservationService.CompleteReservation(reservationId));
    }

    [Fact]
    public void CancelReservation_WhenRepositoryThrows_PropagatesException_AndNoPenaltyApplied()
    {
        // Arrange
        var reservationId = Guid.NewGuid();

        _reservationRepositoryMock.Setup(r => r.Cancel(reservationId))
            .Throws(new ReservationNotFoundException("Not found"));

        // Act & Assert
        Assert.Throws<ReservationNotFoundException>(() =>
            _reservationService.CancelReservation(reservationId));

        _penaltyServiceMock.Verify(p => p.ApplyPenalty(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

}
