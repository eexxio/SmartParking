using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;

namespace SmartParking.Application.Interfaces;

/// <summary>
/// Service interface for parking spot business logic
/// </summary>
public interface IParkingSpotService
{
    /// <summary>
    /// Creates a new parking spot
    /// </summary>
    ParkingSpot CreateSpot(string spotNumber, SpotType spotType, decimal hourlyRate);

    /// <summary>
    /// Retrieves a parking spot by its ID
    /// </summary>
    ParkingSpot GetSpot(Guid spotId);

    /// <summary>
    /// Retrieves all parking spots
    /// </summary>
    List<ParkingSpot> GetAllSpots();

    /// <summary>
    /// Retrieves all available (unoccupied) parking spots
    /// </summary>
    List<ParkingSpot> GetAvailableSpots();

    /// <summary>
    /// Retrieves available parking spots by type
    /// </summary>
    List<ParkingSpot> GetAvailableSpotsByType(SpotType spotType);

    /// <summary>
    /// Validates if a spot can be reserved by a user
    /// </summary>
    void ValidateSpotForUser(Guid spotId, bool isEVUser);

    /// <summary>
    /// Marks a parking spot as occupied
    /// </summary>
    void MarkAsOccupied(Guid spotId);

    /// <summary>
    /// Marks a parking spot as available
    /// </summary>
    void MarkAsAvailable(Guid spotId);
}