using Npgsql;
using SmartParking.Infrastructure.Interfaces;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Exceptions;
using System.Data;

namespace SmartParking.Infrastructure.Repositories;

/// <summary>
/// Repository for parking spot data access using PostgreSQL functions
/// </summary>
public class ParkingSpotRepository : IParkingSpotRepository
{
    private readonly string _connectionString;

    public ParkingSpotRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Creates a new parking spot using sp_create_parking_spot
    /// </summary>
    public ParkingSpot Create(string spotNumber, string spotType, decimal hourlyRate)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(
                "SELECT * FROM sp_create_parking_spot(@p_spot_number, @p_spot_type, @p_hourly_rate)",
                connection);

            command.Parameters.AddWithValue("p_spot_number", spotNumber);
            command.Parameters.AddWithValue("p_spot_type", spotType);
            command.Parameters.AddWithValue("p_hourly_rate", hourlyRate);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                var spotId = reader.GetGuid(reader.GetOrdinal("spot_id"));
                return GetById(spotId) ?? throw new InvalidOperationException("Spot created but could not be loaded.");
            }

            throw new InvalidOperationException("Failed to create parking spot");
        }
        catch (PostgresException ex) when (ex.SqlState is "45101" or "45102" or "45103" or "45104")
        {
            throw new InvalidSpotDataException(ex.Message, ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error creating spot: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves a parking spot by ID using sp_get_parking_spot_by_id
    /// </summary>
    public ParkingSpot? GetById(Guid spotId)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_parking_spot_by_id(@p_spot_id)", connection);

            command.Parameters.AddWithValue("p_spot_id", spotId);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return new ParkingSpot(
                    id: reader.GetGuid(reader.GetOrdinal("id")),
                    spotNumber: reader.GetString(reader.GetOrdinal("spot_number")),
                    spotType: reader.GetString(reader.GetOrdinal("spot_type")),
                    hourlyRate: reader.GetDecimal(reader.GetOrdinal("hourly_rate")),
                    isOccupied: reader.GetBoolean(reader.GetOrdinal("is_occupied")),
                    createdAt: reader.GetDateTime(reader.GetOrdinal("created_at"))
                );
            }

            return null;
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error getting spot by ID: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves all parking spots using sp_get_all_parking_spots
    /// </summary>
    public List<ParkingSpot> GetAll()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_all_parking_spots()", connection);

            connection.Open();
            using var reader = command.ExecuteReader();

            var spots = new List<ParkingSpot>();
            while (reader.Read())
            {
                spots.Add(new ParkingSpot(
                    id: reader.GetGuid(reader.GetOrdinal("id")),
                    spotNumber: reader.GetString(reader.GetOrdinal("spot_number")),
                    spotType: reader.GetString(reader.GetOrdinal("spot_type")),
                    hourlyRate: reader.GetDecimal(reader.GetOrdinal("hourly_rate")),
                    isOccupied: reader.GetBoolean(reader.GetOrdinal("is_occupied")),
                    createdAt: reader.GetDateTime(reader.GetOrdinal("created_at"))
                ));
            }

            return spots;
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error getting all spots: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves all available parking spots using sp_get_available_parking_spots
    /// </summary>
    public List<ParkingSpot> GetAvailableSpots()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_available_parking_spots()", connection);

            connection.Open();
            using var reader = command.ExecuteReader();

            var spots = new List<ParkingSpot>();
            while (reader.Read())
            {
                spots.Add(new ParkingSpot(
                    id: reader.GetGuid(reader.GetOrdinal("id")),
                    spotNumber: reader.GetString(reader.GetOrdinal("spot_number")),
                    spotType: reader.GetString(reader.GetOrdinal("spot_type")),
                    hourlyRate: reader.GetDecimal(reader.GetOrdinal("hourly_rate")),
                    isOccupied: reader.GetBoolean(reader.GetOrdinal("is_occupied")),
                    createdAt: reader.GetDateTime(reader.GetOrdinal("created_at"))
                ));
            }

            return spots;
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error getting available spots: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves available parking spots by type using sp_get_available_spots_by_type
    /// </summary>
    public List<ParkingSpot> GetAvailableSpotsByType(string spotType)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_available_spots_by_type(@p_spot_type)", connection);

            command.Parameters.AddWithValue("p_spot_type", spotType);

            connection.Open();
            using var reader = command.ExecuteReader();

            var spots = new List<ParkingSpot>();
            while (reader.Read())
            {
                spots.Add(new ParkingSpot(
                    id: reader.GetGuid(reader.GetOrdinal("id")),
                    spotNumber: reader.GetString(reader.GetOrdinal("spot_number")),
                    spotType: reader.GetString(reader.GetOrdinal("spot_type")),
                    hourlyRate: reader.GetDecimal(reader.GetOrdinal("hourly_rate")),
                    isOccupied: reader.GetBoolean(reader.GetOrdinal("is_occupied")),
                    createdAt: reader.GetDateTime(reader.GetOrdinal("created_at"))
                ));
            }

            return spots;
        }
        catch (PostgresException ex) when (ex.SqlState == "45102")
        {
            throw new InvalidSpotTypeException(ex.Message, ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error getting available spots by type: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates the occupancy status of a parking spot using sp_update_spot_occupancy
    /// </summary>
    public void UpdateOccupancy(Guid spotId, bool isOccupied)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT sp_update_spot_occupancy(@p_spot_id, @p_is_occupied)", connection);

            command.Parameters.AddWithValue("p_spot_id", spotId);
            command.Parameters.AddWithValue("p_is_occupied", isOccupied);

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (PostgresException ex) when (ex.SqlState == "45105")
        {
            throw new InvalidSpotDataException("Parking spot not found", ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error updating spot occupancy: {ex.Message}", ex);
        }
    }
}
