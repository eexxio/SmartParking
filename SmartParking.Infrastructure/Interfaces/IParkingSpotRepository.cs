using SmartParking.Domain.Entities;

namespace SmartParking.Infrastructure.Interfaces;

/// <summary>
/// Repository interface for parking spot data access
/// </summary>
public interface IParkingSpotRepository
{
    /// <summary>
    /// Creates a new parking spot
    /// </summary>
    ParkingSpot Create(string spotNumber, string spotType, decimal hourlyRate);

    /// <summary>
    /// Retrieves a parking spot by its ID
    /// </summary>
    ParkingSpot? GetById(Guid spotId);

    /// <summary>
    /// Retrieves all parking spots
    /// </summary>
    List<ParkingSpot> GetAll();

    /// <summary>
    /// Retrieves all available (unoccupied) parking spots
    /// </summary>
    List<ParkingSpot> GetAvailableSpots();

    /// <summary>
    /// Retrieves available parking spots by type (Regular or EV)
    /// </summary>
    List<ParkingSpot> GetAvailableSpotsByType(string spotType);

    /// <summary>
    /// Updates the occupancy status of a parking spot
    /// </summary>
    void UpdateOccupancy(Guid spotId, bool isOccupied);
}
