using SmartParking.Domain.Exceptions;

namespace SmartParking.Domain.Entities;

public class ParkingSpot
{
    public Guid Id { get; private set; }
    public string SpotNumber { get; private set; }
    public string SpotType { get; private set; }
    public decimal HourlyRate { get; private set; }
    public bool IsOccupied { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Constructor for creating a new parking spot
    /// </summary>
    public ParkingSpot(string spotNumber, string spotType, decimal hourlyRate)
    {
        ValidateSpotNumber(spotNumber);
        ValidateSpotType(spotType);
        ValidateHourlyRate(hourlyRate);

        Id = Guid.NewGuid();
        SpotNumber = spotNumber;
        SpotType = spotType;
        HourlyRate = hourlyRate;
        IsOccupied = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for repository reconstruction
    /// </summary>
    public ParkingSpot(Guid id, string spotNumber, string spotType, decimal hourlyRate, bool isOccupied, DateTime createdAt)
    {
        ValidateSpotNumber(spotNumber);
        ValidateSpotType(spotType);
        ValidateHourlyRate(hourlyRate);

        Id = id;
        SpotNumber = spotNumber;
        SpotType = spotType;
        HourlyRate = hourlyRate;
        IsOccupied = isOccupied;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Parameterless constructor for ORM/serialization
    /// </summary>
    public ParkingSpot()
    {
        SpotNumber = string.Empty;
        SpotType = string.Empty;
    }

    public void MarkAsOccupied()
    {
        if (IsOccupied)
        {
            throw new SpotNotAvailableException($"Spot {SpotNumber} is already occupied");
        }
        IsOccupied = true;
    }

    public void MarkAsAvailable()
    {
        IsOccupied = false;
    }

    private static void ValidateSpotNumber(string spotNumber)
    {
        if (string.IsNullOrWhiteSpace(spotNumber))
        {
            throw new InvalidSpotDataException("Spot number is required");
        }

        if (spotNumber.Length < 5)
        {
            throw new InvalidSpotDataException("Spot number must be at least 5 characters long");
        }
    }

    private static void ValidateSpotType(string spotType)
    {
        if (string.IsNullOrWhiteSpace(spotType))
        {
            throw new InvalidSpotDataException("Spot type is required");
        }

        if (spotType != "Regular" && spotType != "EV")
        {
            throw new InvalidSpotTypeException($"Invalid spot type: {spotType}. Must be 'Regular' or 'EV'");
        }
    }

    private static void ValidateHourlyRate(decimal hourlyRate)
    {
        if (hourlyRate <= 0)
        {
            throw new InvalidSpotDataException("Hourly rate must be greater than 0");
        }
    }
}