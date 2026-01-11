using Microsoft.Extensions.Logging;
using SmartParking.Business.Interfaces;
using SmartParking.DataAccess.Interfaces;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using SmartParking.Domain.Exceptions;

namespace SmartParking.Business.Services;

/// <summary>
/// Service for parking spot business logic
/// </summary>
public class ParkingSpotService : IParkingSpotService
{
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly ILogger<ParkingSpotService> _logger;

    public ParkingSpotService(
        IParkingSpotRepository parkingSpotRepository,
        ILogger<ParkingSpotService> logger)
    {
        _parkingSpotRepository = parkingSpotRepository ?? throw new ArgumentNullException(nameof(parkingSpotRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new parking spot
    /// </summary>
    public ParkingSpot CreateSpot(string spotNumber, SpotType spotType, decimal hourlyRate)
    {
        try
        {
            _logger.LogInformation("Creating parking spot. SpotNumber={SpotNumber}, SpotType={SpotType}, HourlyRate={HourlyRate}",
                spotNumber, spotType, hourlyRate);

            // Validate input data
            if (string.IsNullOrWhiteSpace(spotNumber))
            {
                throw new InvalidSpotDataException("Spot number is required");
            }

            if (spotNumber.Length < 5)
            {
                throw new InvalidSpotDataException("Spot number must be at least 5 characters long");
            }

            if (hourlyRate <= 0)
            {
                throw new InvalidSpotDataException("Hourly rate must be greater than 0");
            }

            // Convert enum to string for repository
            string spotTypeString = spotType == SpotType.EV ? "EV" : "Regular";

            var spot = _parkingSpotRepository.Create(spotNumber, spotTypeString, hourlyRate);

            _logger.LogInformation("Parking spot created successfully. SpotId={SpotId}, SpotNumber={SpotNumber}",
                spot.Id, spot.SpotNumber);

            return spot;
        }
        catch (InvalidSpotDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating parking spot. SpotNumber={SpotNumber}", spotNumber);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a parking spot by its ID
    /// </summary>
    public ParkingSpot GetSpot(Guid spotId)
    {
        try
        {
            _logger.LogInformation("Getting parking spot. SpotId={SpotId}", spotId);

            var spot = _parkingSpotRepository.GetById(spotId);

            if (spot == null)
            {
                throw new InvalidSpotDataException($"Parking spot with ID {spotId} not found");
            }

            _logger.LogInformation("Parking spot retrieved successfully. SpotId={SpotId}, SpotNumber={SpotNumber}",
                spot.Id, spot.SpotNumber);

            return spot;
        }
        catch (InvalidSpotDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parking spot. SpotId={SpotId}", spotId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all parking spots
    /// </summary>
    public List<ParkingSpot> GetAllSpots()
    {
        try
        {
            _logger.LogInformation("Getting all parking spots");

            var spots = _parkingSpotRepository.GetAll();

            _logger.LogInformation("Retrieved {Count} parking spots", spots.Count);

            return spots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all parking spots");
            throw;
        }
    }

    /// <summary>
    /// Retrieves all available (unoccupied) parking spots
    /// </summary>
    public List<ParkingSpot> GetAvailableSpots()
    {
        try
        {
            _logger.LogInformation("Getting available parking spots");

            var spots = _parkingSpotRepository.GetAvailableSpots();

            _logger.LogInformation("Retrieved {Count} available parking spots", spots.Count);

            return spots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available parking spots");
            throw;
        }
    }

    /// <summary>
    /// Retrieves available parking spots by type
    /// </summary>
    public List<ParkingSpot> GetAvailableSpotsByType(SpotType spotType)
    {
        try
        {
            _logger.LogInformation("Getting available parking spots by type. SpotType={SpotType}", spotType);

            // Convert enum to string for repository
            string spotTypeString = spotType == SpotType.EV ? "EV" : "Regular";

            var spots = _parkingSpotRepository.GetAvailableSpotsByType(spotTypeString);

            _logger.LogInformation("Retrieved {Count} available {SpotType} parking spots", spots.Count, spotType);

            return spots;
        }
        catch (InvalidSpotTypeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available parking spots by type. SpotType={SpotType}", spotType);
            throw;
        }
    }

    /// <summary>
    /// Validates if a spot can be reserved by a user
    /// EV users can use both Regular and EV spots
    /// Non-EV users can ONLY use Regular spots
    /// </summary>
    public void ValidateSpotForUser(Guid spotId, bool isEVUser)
    {
        try
        {
            _logger.LogInformation("Validating spot for user. SpotId={SpotId}, IsEVUser={IsEVUser}", spotId, isEVUser);

            var spot = GetSpot(spotId);

            // Check if spot is available
            if (spot.IsOccupied)
            {
                throw new SpotNotAvailableException($"Spot {spot.SpotNumber} is already occupied");
            }

            // Validation logic:
            // - EV users can use both Regular and EV spots
            // - Non-EV users can ONLY use Regular spots
            if (spot.SpotType == "EV" && !isEVUser)
            {
                throw new InvalidSpotTypeException("Non-EV users cannot reserve EV spots");
            }

            _logger.LogInformation("Spot validation successful for {SpotId}", spotId);
        }
        catch (InvalidSpotDataException)
        {
            throw;
        }
        catch (SpotNotAvailableException)
        {
            throw;
        }
        catch (InvalidSpotTypeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating spot for user. SpotId={SpotId}", spotId);
            throw;
        }
    }

    /// <summary>
    /// Marks a parking spot as occupied
    /// </summary>
    public void MarkAsOccupied(Guid spotId)
    {
        try
        {
            _logger.LogInformation("Marking spot as occupied. SpotId={SpotId}", spotId);

            var spot = GetSpot(spotId);

            if (spot.IsOccupied)
            {
                throw new SpotNotAvailableException($"Spot {spot.SpotNumber} is already occupied");
            }

            _parkingSpotRepository.UpdateOccupancy(spotId, true);

            _logger.LogInformation("Spot marked as occupied. SpotId={SpotId}", spotId);
        }
        catch (InvalidSpotDataException)
        {
            throw;
        }
        catch (SpotNotAvailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking spot as occupied. SpotId={SpotId}", spotId);
            throw;
        }
    }

    /// <summary>
    /// Marks a parking spot as available
    /// </summary>
    public void MarkAsAvailable(Guid spotId)
    {
        try
        {
            _logger.LogInformation("Marking spot as available. SpotId={SpotId}", spotId);

            // Verify spot exists
            var spot = GetSpot(spotId);

            _parkingSpotRepository.UpdateOccupancy(spotId, false);

            _logger.LogInformation("Spot marked as available. SpotId={SpotId}", spotId);
        }
        catch (InvalidSpotDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking spot as available. SpotId={SpotId}", spotId);
            throw;
        }
    }
}
