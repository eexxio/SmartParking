using Microsoft.Data.SqlClient;
using SmartParking.DataAccess.Interfaces;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using SmartParking.Domain.Exceptions;
using System.Data;

namespace SmartParking.DataAccess.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly string _connectionString;

    public ReservationRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public void ValidateSpotForUser(Guid spotId, bool isEVUser)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_ValidateSpotForUser", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@SpotId", spotId);
            command.Parameters.AddWithValue("@IsEVUser", isEVUser);

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (SqlException ex) when (ex.Number >= 50101 && ex.Number <= 50199)
        {
            throw new InvalidReservationException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error validating spot: {ex.Message}", ex);
        }
    }

    public Reservation Create(Guid userId, Guid spotId, int cancellationTimeoutMinutes = 15)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_CreateReservation", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@SpotId", spotId);
            command.Parameters.AddWithValue("@CancellationTimeoutMinutes", cancellationTimeoutMinutes);

            var reservationIdParam = new SqlParameter("@ReservationId", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(reservationIdParam);

            connection.Open();
            command.ExecuteNonQuery();

            var reservationId = (Guid)reservationIdParam.Value;

            return GetById(reservationId)
                   ?? throw new InvalidOperationException("Reservation created but could not be loaded.");
        }
        catch (SqlException ex) when (ex.Number is 50201 or 50202 or 50203)
        {
            throw new InvalidReservationException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error creating reservation: {ex.Message}", ex);
        }
    }

    public Reservation? GetById(Guid reservationId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetReservationById", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@ReservationId", reservationId);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return MapReservation(reader);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error getting reservation: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Reservation> GetByUserId(Guid userId)
    {
        try
        {
            var list = new List<Reservation>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetReservationsByUserId", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@UserId", userId);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(MapReservation(reader));
            }

            return list;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error getting reservations by user: {ex.Message}", ex);
        }
    }

    public void Confirm(Guid reservationId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_ConfirmReservation", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@ReservationId", reservationId);

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (SqlException ex) when (ex.Number == 50204)
        {
            throw new ReservationNotFoundException(ex.Message, ex);
        }
        catch (SqlException ex) when (ex.Number == 50205)
        {
            throw new InvalidReservationException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error confirming reservation: {ex.Message}", ex);
        }
    }

    public bool Cancel(Guid reservationId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_CancelReservation", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@ReservationId", reservationId);

            var isLateParam = new SqlParameter("@IsLate", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(isLateParam);

            connection.Open();
            command.ExecuteNonQuery();

            return isLateParam.Value != DBNull.Value && (bool)isLateParam.Value;
        }
        catch (SqlException ex) when (ex.Number == 50204)
        {
            throw new ReservationNotFoundException(ex.Message, ex);
        }
        catch (SqlException ex) when (ex.Number == 50206)
        {
            throw new InvalidReservationException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error cancelling reservation: {ex.Message}", ex);
        }
    }

    public void Complete(Guid reservationId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_CompleteReservation", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@ReservationId", reservationId);

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (SqlException ex) when (ex.Number == 50204)
        {
            throw new ReservationNotFoundException(ex.Message, ex);
        }
        catch (SqlException ex) when (ex.Number == 50207)
        {
            throw new InvalidReservationException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error completing reservation: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Reservation> GetExpiredPendingReservations()
    {
        try
        {
            var list = new List<Reservation>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetExpiredPendingReservations", connection);
            command.CommandType = CommandType.StoredProcedure;

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                // SP returns fewer columns (no EndTime, CreatedAt). We read what we have and fill safe defaults.
                var id = reader.GetGuid(reader.GetOrdinal("Id"));
                var userId = reader.GetGuid(reader.GetOrdinal("UserId"));
                var spotId = reader.GetGuid(reader.GetOrdinal("SpotId"));
                var startTime = reader.GetDateTime(reader.GetOrdinal("StartTime"));
                var deadline = reader.GetDateTime(reader.GetOrdinal("CancellationDeadline"));

                list.Add(new Reservation(
                    id: id,
                    userId: userId,
                    spotId: spotId,
                    startTime: startTime,
                    endTime: null,
                    status: ReservationStatus.Pending,
                    cancellationDeadline: deadline,
                    createdAt: startTime));
            }

            return list;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error getting expired reservations: {ex.Message}", ex);
        }
    }

    private static Reservation MapReservation(SqlDataReader reader)
    {
        var id = reader.GetGuid(reader.GetOrdinal("Id"));
        var userId = reader.GetGuid(reader.GetOrdinal("UserId"));
        var spotId = reader.GetGuid(reader.GetOrdinal("SpotId"));
        var startTime = reader.GetDateTime(reader.GetOrdinal("StartTime"));

        DateTime? endTime = null;
        var endOrdinal = reader.GetOrdinal("EndTime");
        if (!reader.IsDBNull(endOrdinal))
        {
            endTime = reader.GetDateTime(endOrdinal);
        }

        var statusStr = reader.GetString(reader.GetOrdinal("Status"));
        var status = statusStr switch
        {
            "Pending" => ReservationStatus.Pending,
            "Confirmed" => ReservationStatus.Confirmed,
            "Cancelled" => ReservationStatus.Cancelled,
            "Completed" => ReservationStatus.Completed,
            _ => throw new InvalidOperationException($"Unknown Status from DB: {statusStr}")
        };

        var deadline = reader.GetDateTime(reader.GetOrdinal("CancellationDeadline"));
        var createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

        return new Reservation(
            id: id,
            userId: userId,
            spotId: spotId,
            startTime: startTime,
            endTime: endTime,
            status: status,
            cancellationDeadline: deadline,
            createdAt: createdAt);
    }
}
