using Microsoft.Extensions.Logging;
using Moq;
using SmartParking.Business.Services;
using SmartParking.DataAccess.Interfaces;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using SmartParking.Domain.Exceptions;
using Xunit;

namespace SmartParking.Tests;

public class ParkingSpotServiceTests
{
    private readonly Mock<IParkingSpotRepository> _repositoryMock;
    private readonly Mock<ILogger<ParkingSpotService>> _loggerMock;
    private readonly ParkingSpotService _service;

    public ParkingSpotServiceTests()
    {
        _repositoryMock = new Mock<IParkingSpotRepository>();
        _loggerMock = new Mock<ILogger<ParkingSpotService>>();
        _service = new ParkingSpotService(_repositoryMock.Object, _loggerMock.Object);
    }

    #region Helper Methods

    private static ParkingSpot CreateSpot(Guid id, string spotNumber, string spotType, decimal hourlyRate, bool isOccupied)
        => new(id, spotNumber, spotType, hourlyRate, isOccupied, DateTime.UtcNow);

    #endregion

    #region Part 1: Basic Operations (13 tests)

    [Fact]
    public void CreateSpot_ValidData_Success()
    {
        // Arrange
        var spotNumber = "A-001";
        var spotType = SpotType.Regular;
        var hourlyRate = 10.5m;
        var expectedSpot = CreateSpot(Guid.NewGuid(), spotNumber, "Regular", hourlyRate, false);

        _repositoryMock.Setup(r => r.Create(spotNumber, "Regular", hourlyRate))
                      .Returns(expectedSpot);

        // Act
        var result = _service.CreateSpot(spotNumber, spotType, hourlyRate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(spotNumber, result.SpotNumber);
        Assert.Equal("Regular", result.SpotType);
        Assert.Equal(hourlyRate, result.HourlyRate);
        _repositoryMock.Verify(r => r.Create(spotNumber, "Regular", hourlyRate), Times.Once);
    }

    [Fact]
    public void CreateSpot_ShortSpotNumber_ThrowsException()
    {
        // Arrange
        var shortSpotNumber = "A-01"; // Only 4 characters
        var spotType = SpotType.Regular;
        var hourlyRate = 10.0m;

        // Act & Assert
        var exception = Assert.Throws<InvalidSpotDataException>(() =>
            _service.CreateSpot(shortSpotNumber, spotType, hourlyRate));

        Assert.Contains("at least 5 characters", exception.Message);
        _repositoryMock.Verify(r => r.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public void CreateSpot_NegativeRate_ThrowsException()
    {
        // Arrange
        var spotNumber = "A-001";
        var spotType = SpotType.Regular;
        var negativeRate = -5.0m;

        // Act & Assert
        var exception = Assert.Throws<InvalidSpotDataException>(() =>
            _service.CreateSpot(spotNumber, spotType, negativeRate));

        Assert.Contains("greater than 0", exception.Message);
        _repositoryMock.Verify(r => r.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public void CreateSpot_ZeroRate_ThrowsException()
    {
        // Arrange
        var spotNumber = "A-001";
        var spotType = SpotType.Regular;
        var zeroRate = 0m;

        // Act & Assert
        var exception = Assert.Throws<InvalidSpotDataException>(() =>
            _service.CreateSpot(spotNumber, spotType, zeroRate));

        Assert.Contains("greater than 0", exception.Message);
        _repositoryMock.Verify(r => r.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public void GetSpot_ExistingId_ReturnsSpot()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var expectedSpot = CreateSpot(spotId, "A-001", "Regular", 10.0m, false);

        _repositoryMock.Setup(r => r.GetById(spotId))
                      .Returns(expectedSpot);

        // Act
        var result = _service.GetSpot(spotId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(spotId, result.Id);
        Assert.Equal("A-001", result.SpotNumber);
        _repositoryMock.Verify(r => r.GetById(spotId), Times.Once);
    }

    [Fact]
    public void GetSpot_NonExistingId_ThrowsException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetById(nonExistingId))
                      .Returns((ParkingSpot?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidSpotDataException>(() =>
            _service.GetSpot(nonExistingId));

        Assert.Contains("not found", exception.Message);
        _repositoryMock.Verify(r => r.GetById(nonExistingId), Times.Once);
    }

    [Fact]
    public void GetAllSpots_ReturnsAllSpots()
    {
        // Arrange
        var spots = new List<ParkingSpot>
        {
            CreateSpot(Guid.NewGuid(), "A-001", "Regular", 10.0m, false),
            CreateSpot(Guid.NewGuid(), "A-002", "EV", 15.0m, false),
            CreateSpot(Guid.NewGuid(), "B-001", "Regular", 10.0m, true)
        };

        _repositoryMock.Setup(r => r.GetAll())
                      .Returns(spots);

        // Act
        var result = _service.GetAllSpots();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
    }

    [Fact]
    public void GetAvailableSpots_ReturnsOnlyUnoccupied()
    {
        // Arrange
        var availableSpots = new List<ParkingSpot>
        {
            CreateSpot(Guid.NewGuid(), "A-001", "Regular", 10.0m, false),
            CreateSpot(Guid.NewGuid(), "A-002", "EV", 15.0m, false)
        };

        _repositoryMock.Setup(r => r.GetAvailableSpots())
                      .Returns(availableSpots);

        // Act
        var result = _service.GetAvailableSpots();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, spot => Assert.False(spot.IsOccupied));
        _repositoryMock.Verify(r => r.GetAvailableSpots(), Times.Once);
    }

    [Fact]
    public void GetAvailableSpotsByType_EV_ReturnsOnlyEVSpots()
    {
        // Arrange
        var evSpots = new List<ParkingSpot>
        {
            CreateSpot(Guid.NewGuid(), "A-001", "EV", 15.0m, false),
            CreateSpot(Guid.NewGuid(), "A-002", "EV", 15.0m, false)
        };

        _repositoryMock.Setup(r => r.GetAvailableSpotsByType("EV"))
                      .Returns(evSpots);

        // Act
        var result = _service.GetAvailableSpotsByType(SpotType.EV);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, spot => Assert.Equal("EV", spot.SpotType));
        _repositoryMock.Verify(r => r.GetAvailableSpotsByType("EV"), Times.Once);
    }

    [Fact]
    public void GetAvailableSpotsByType_Regular_ReturnsOnlyRegularSpots()
    {
        // Arrange
        var regularSpots = new List<ParkingSpot>
        {
            CreateSpot(Guid.NewGuid(), "B-001", "Regular", 10.0m, false),
            CreateSpot(Guid.NewGuid(), "B-002", "Regular", 10.0m, false)
        };

        _repositoryMock.Setup(r => r.GetAvailableSpotsByType("Regular"))
                      .Returns(regularSpots);

        // Act
        var result = _service.GetAvailableSpotsByType(SpotType.Regular);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, spot => Assert.Equal("Regular", spot.SpotType));
        _repositoryMock.Verify(r => r.GetAvailableSpotsByType("Regular"), Times.Once);
    }

    [Fact]
    public void MarkAsOccupied_AvailableSpot_Success()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var spot = CreateSpot(spotId, "A-001", "Regular", 10.0m, false);

        _repositoryMock.Setup(r => r.GetById(spotId))
                      .Returns(spot);
        _repositoryMock.Setup(r => r.UpdateOccupancy(spotId, true));

        // Act
        _service.MarkAsOccupied(spotId);

        // Assert
        _repositoryMock.Verify(r => r.UpdateOccupancy(spotId, true), Times.Once);
    }

    [Fact]
    public void MarkAsOccupied_AlreadyOccupied_ThrowsException()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var occupiedSpot = CreateSpot(spotId, "A-001", "Regular", 10.0m, true);

        _repositoryMock.Setup(r => r.GetById(spotId))
                      .Returns(occupiedSpot);

        // Act & Assert
        var exception = Assert.Throws<SpotNotAvailableException>(() =>
            _service.MarkAsOccupied(spotId));

        Assert.Contains("already occupied", exception.Message);
        _repositoryMock.Verify(r => r.UpdateOccupancy(It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void MarkAsAvailable_OccupiedSpot_Success()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var spot = CreateSpot(spotId, "A-001", "Regular", 10.0m, true);

        _repositoryMock.Setup(r => r.GetById(spotId))
                      .Returns(spot);
        _repositoryMock.Setup(r => r.UpdateOccupancy(spotId, false));

        // Act
        _service.MarkAsAvailable(spotId);

        // Assert
        _repositoryMock.Verify(r => r.UpdateOccupancy(spotId, false), Times.Once);
    }

    #endregion

    #region Part 2: User Validation (12 tests)

    [Fact]
    public void ValidateSpotForUser_EVSpot_NonEVUser_ThrowsException()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var evSpot = CreateSpot(spotId, "A-001", "EV", 15.0m, false);

        _repositoryMock.Setup(r => r.GetById(spotId))
                      .Returns(evSpot);

        // Act & Assert
        var exception = Assert.Throws<InvalidSpotTypeException>(() =>
            _service.ValidateSpotForUser(spotId, isEVUser: false));

        Assert.Contains("Non-EV users cannot reserve EV spots", exception.Message);
    }

    [Fact]
    public void ValidateSpotForUser_EVSpot_EVUser_Success()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var evSpot = CreateSpot(spotId, "A-001", "EV", 15.0m, false);

        _repositoryMock.Setup(r => r.GetById(spotId))
                      .Returns(evSpot);

        // Act - Should not throw
        _service.ValidateSpotForUser(spotId, isEVUser: true);

        // Assert - Verify GetById was called
        _repositoryMock.Verify(r => r.GetById(spotId), Times.Once);
    }

    [Fact]
    public void ValidateSpotForUser_RegularSpot_EVUser_Success()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var regularSpot = CreateSpot(spotId, "B-001", "Regular", 10.0m, false);

        _repositoryMock.Setup(r => r.GetById(spotId))
                      .Returns(regularSpot);

        // Act - Should not throw
        _service.ValidateSpotForUser(spotId, isEVUser: true);

        // Assert - Verify GetById was called
        _repositoryMock.Verify(r => r.GetById(spotId), Times.Once);
    }

    [Fact]
    public void ValidateSpotForUser_RegularSpot_NonEVUser_Success()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var regularSpot = CreateSpot(spotId, "B-001", "Regular", 10.0m, false);

        _repositoryMock.Setup(r => r.GetById(spotId))
                      .Returns(regularSpot);

        // Act - Should not throw
        _service.ValidateSpotForUser(spotId, isEVUser: false);

        // Assert - Verify GetById was called
        _repositoryMock.Verify(r => r.GetById(spotId), Times.Once);
    }

    [Fact]
    public void ValidateSpotForUser_OccupiedSpot_ThrowsException()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var occupiedSpot = CreateSpot(spotId, "A-001", "Regular", 10.0m, true);

        _repositoryMock.Setup(r => r.GetById(spotId))
                      .Returns(occupiedSpot);

        // Act & Assert
        var exception = Assert.Throws<SpotNotAvailableException>(() =>
            _service.ValidateSpotForUser(spotId, isEVUser: false));

        Assert.Contains("already occupied", exception.Message);
    }

    [Fact]
    public void ValidateSpotForUser_AvailableSpot_Success()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var availableSpot = CreateSpot(spotId, "A-001", "Regular", 10.0m, false);

        _repositoryMock.Setup(r => r.GetById(spotId))
                      .Returns(availableSpot);

        // Act - Should not throw
        _service.ValidateSpotForUser(spotId, isEVUser: false);

        // Assert - Verify GetById was called
        _repositoryMock.Verify(r => r.GetById(spotId), Times.Once);
    }

    [Fact]
    public void CreateSpot_DuplicateSpotNumber_ThrowsException()
    {
        // Arrange
        var spotNumber = "A-001";
        var spotType = SpotType.Regular;
        var hourlyRate = 10.0m;

        _repositoryMock.Setup(r => r.Create(spotNumber, "Regular", hourlyRate))
                      .Throws(new InvalidSpotDataException("Spot number already exists"));

        // Act & Assert
        var exception = Assert.Throws<InvalidSpotDataException>(() =>
            _service.CreateSpot(spotNumber, spotType, hourlyRate));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void GetAvailableSpots_AllOccupied_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAvailableSpots())
                      .Returns(new List<ParkingSpot>());

        // Act
        var result = _service.GetAvailableSpots();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _repositoryMock.Verify(r => r.GetAvailableSpots(), Times.Once);
    }

    [Fact]
    public void GetAvailableSpots_MixedOccupancy_ReturnsCorrectSpots()
    {
        // Arrange
        var availableSpots = new List<ParkingSpot>
        {
            CreateSpot(Guid.NewGuid(), "A-001", "Regular", 10.0m, false),
            CreateSpot(Guid.NewGuid(), "A-003", "EV", 15.0m, false)
        };

        _repositoryMock.Setup(r => r.GetAvailableSpots())
                      .Returns(availableSpots);

        // Act
        var result = _service.GetAvailableSpots();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, spot => Assert.False(spot.IsOccupied));
    }

    [Fact]
    public void UpdateOccupancy_TogglesCorrectly()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var spot = CreateSpot(spotId, "A-001", "Regular", 10.0m, false);

        _repositoryMock.Setup(r => r.GetById(spotId))
                      .Returns(spot);
        _repositoryMock.Setup(r => r.UpdateOccupancy(spotId, true));

        // Act
        _service.MarkAsOccupied(spotId);

        // Assert
        _repositoryMock.Verify(r => r.UpdateOccupancy(spotId, true), Times.Once);
    }

    [Fact]
    public void GetAvailableSpotsByType_NoSpotsOfType_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAvailableSpotsByType("EV"))
                      .Returns(new List<ParkingSpot>());

        // Act
        var result = _service.GetAvailableSpotsByType(SpotType.EV);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _repositoryMock.Verify(r => r.GetAvailableSpotsByType("EV"), Times.Once);
    }

    [Fact]
    public void CreateSpot_BoundarySpotNumber_5Chars_Success()
    {
        // Arrange
        var spotNumber = "A-001"; // Exactly 5 characters
        var spotType = SpotType.Regular;
        var hourlyRate = 10.0m;
        var expectedSpot = CreateSpot(Guid.NewGuid(), spotNumber, "Regular", hourlyRate, false);

        _repositoryMock.Setup(r => r.Create(spotNumber, "Regular", hourlyRate))
                      .Returns(expectedSpot);

        // Act
        var result = _service.CreateSpot(spotNumber, spotType, hourlyRate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(spotNumber, result.SpotNumber);
        _repositoryMock.Verify(r => r.Create(spotNumber, "Regular", hourlyRate), Times.Once);
    }

    #endregion
}
