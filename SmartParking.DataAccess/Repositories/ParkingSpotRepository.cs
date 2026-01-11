using Microsoft.Data.SqlClient;
using SmartParking.DataAccess.Interfaces;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Exceptions;
using System.Data;

namespace SmartParking.DataAccess.Repositories;

/// <summary>
/// Repository for parking spot data access using stored procedures
/// </summary>
public class ParkingSpotRepository : IParkingSpotRepository
{
    private readonly string _connectionString;

    public ParkingSpotRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Creates a new parking spot using sp_CreateParkingSpot
    /// </summary>
    public ParkingSpot Create(string spotNumber, string spotType, decimal hourlyRate)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_CreateParkingSpot", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@SpotNumber", spotNumber);
            command.Parameters.AddWithValue("@SpotType", spotType);
            command.Parameters.AddWithValue("@HourlyRate", hourlyRate);

            var spotIdParam = new SqlParameter("@SpotId", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(spotIdParam);

            connection.Open();
            command.ExecuteNonQuery();

            var spotId = (Guid)spotIdParam.Value;
            return GetById(spotId) ?? throw new InvalidOperationException("Spot created but could not be loaded.");
        }
        catch (SqlException ex) when (ex.Number >= 50101 && ex.Number <= 50107)
        {
            throw new InvalidSpotDataException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error creating spot: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves a parking spot by ID using sp_GetParkingSpotById
    /// </summary>
    public ParkingSpot? GetById(Guid spotId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetParkingSpotById", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@SpotId", spotId);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return new ParkingSpot(
                    id: reader.GetGuid(reader.GetOrdinal("Id")),
                    spotNumber: reader.GetString(reader.GetOrdinal("SpotNumber")),
                    spotType: reader.GetString(reader.GetOrdinal("SpotType")),
                    hourlyRate: reader.GetDecimal(reader.GetOrdinal("HourlyRate")),
                    isOccupied: reader.GetBoolean(reader.GetOrdinal("IsOccupied")),
                    createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                );
            }

            return null;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error getting spot by ID: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves all parking spots using sp_GetAllParkingSpots
    /// </summary>
    public List<ParkingSpot> GetAll()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAllParkingSpots", connection);
            command.CommandType = CommandType.StoredProcedure;

            connection.Open();
            using var reader = command.ExecuteReader();

            var spots = new List<ParkingSpot>();
            while (reader.Read())
            {
                spots.Add(new ParkingSpot(
                    id: reader.GetGuid(reader.GetOrdinal("Id")),
                    spotNumber: reader.GetString(reader.GetOrdinal("SpotNumber")),
                    spotType: reader.GetString(reader.GetOrdinal("SpotType")),
                    hourlyRate: reader.GetDecimal(reader.GetOrdinal("HourlyRate")),
                    isOccupied: reader.GetBoolean(reader.GetOrdinal("IsOccupied")),
                    createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                ));
            }

            return spots;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error getting all spots: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves all available parking spots using sp_GetAvailableParkingSpots
    /// </summary>
    public List<ParkingSpot> GetAvailableSpots()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAvailableParkingSpots", connection);
            command.CommandType = CommandType.StoredProcedure;

            connection.Open();
            using var reader = command.ExecuteReader();

            var spots = new List<ParkingSpot>();
            while (reader.Read())
            {
                spots.Add(new ParkingSpot(
                    id: reader.GetGuid(reader.GetOrdinal("Id")),
                    spotNumber: reader.GetString(reader.GetOrdinal("SpotNumber")),
                    spotType: reader.GetString(reader.GetOrdinal("SpotType")),
                    hourlyRate: reader.GetDecimal(reader.GetOrdinal("HourlyRate")),
                    isOccupied: reader.GetBoolean(reader.GetOrdinal("IsOccupied")),
                    createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                ));
            }

            return spots;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error getting available spots: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves available parking spots by type using sp_GetAvailableSpotsByType
    /// </summary>
    public List<ParkingSpot> GetAvailableSpotsByType(string spotType)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetAvailableSpotsByType", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@SpotType", spotType);

            connection.Open();
            using var reader = command.ExecuteReader();

            var spots = new List<ParkingSpot>();
            while (reader.Read())
            {
                spots.Add(new ParkingSpot(
                    id: reader.GetGuid(reader.GetOrdinal("Id")),
                    spotNumber: reader.GetString(reader.GetOrdinal("SpotNumber")),
                    spotType: reader.GetString(reader.GetOrdinal("SpotType")),
                    hourlyRate: reader.GetDecimal(reader.GetOrdinal("HourlyRate")),
                    isOccupied: reader.GetBoolean(reader.GetOrdinal("IsOccupied")),
                    createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                ));
            }

            return spots;
        }
        catch (SqlException ex) when (ex.Number == 50102)
        {
            throw new InvalidSpotTypeException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error getting available spots by type: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates the occupancy status of a parking spot using sp_UpdateSpotOccupancy
    /// </summary>
    public void UpdateOccupancy(Guid spotId, bool isOccupied)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_UpdateSpotOccupancy", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@SpotId", spotId);
            command.Parameters.AddWithValue("@IsOccupied", isOccupied);

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (SqlException ex) when (ex.Number == 50105)
        {
            throw new InvalidSpotDataException("Parking spot not found", ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error updating spot occupancy: {ex.Message}", ex);
        }
    }
}
