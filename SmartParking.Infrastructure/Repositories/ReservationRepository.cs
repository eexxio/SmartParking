using Npgsql;
using SmartParking.Infrastructure.Interfaces;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using SmartParking.Domain.Exceptions;
using System.Data;

namespace SmartParking.Infrastructure.Repositories;

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
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_validate_spot_for_user(@p_spot_id, @p_is_ev_user)", connection);

            command.Parameters.AddWithValue("p_spot_id", spotId);
            command.Parameters.AddWithValue("p_is_ev_user", isEVUser);

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (PostgresException ex) when (ex.SqlState is "45101" or "45102" or "45103" or "45104" or "45105" or "45106" or "45107")
        {
            throw new InvalidReservationException(ex.Message, ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error validating spot: {ex.Message}", ex);
        }
    }

    public Reservation Create(Guid userId, Guid spotId, int cancellationTimeoutMinutes = 15)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(
                "SELECT * FROM sp_create_reservation(@p_user_id, @p_spot_id, @p_cancellation_timeout_minutes)",
                connection);

            command.Parameters.AddWithValue("p_user_id", userId);
            command.Parameters.AddWithValue("p_spot_id", spotId);
            command.Parameters.AddWithValue("p_cancellation_timeout_minutes", cancellationTimeoutMinutes);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                var reservationId = reader.GetGuid(reader.GetOrdinal("reservation_id"));
                return GetById(reservationId)
                       ?? throw new InvalidOperationException("Reservation created but could not be loaded.");
            }

            throw new InvalidOperationException("Failed to create reservation");
        }
        catch (PostgresException ex) when (ex.SqlState is "45201" or "45202" or "45203")
        {
            throw new InvalidReservationException(ex.Message, ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error creating reservation: {ex.Message}", ex);
        }
    }

    public Reservation? GetById(Guid reservationId)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_reservation_by_id(@p_reservation_id)", connection);

            command.Parameters.AddWithValue("p_reservation_id", reservationId);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return MapReservation(reader);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error getting reservation: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Reservation> GetByUserId(Guid userId)
    {
        try
        {
            var list = new List<Reservation>();

            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_reservations_by_user_id(@p_user_id)", connection);

            command.Parameters.AddWithValue("p_user_id", userId);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(MapReservation(reader));
            }

            return list;
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error getting reservations by user: {ex.Message}", ex);
        }
    }

    public void Confirm(Guid reservationId)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT sp_confirm_reservation(@p_reservation_id)", connection);

            command.Parameters.AddWithValue("p_reservation_id", reservationId);

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (PostgresException ex) when (ex.SqlState == "45204")
        {
            throw new ReservationNotFoundException(ex.Message, ex);
        }
        catch (PostgresException ex) when (ex.SqlState == "45205")
        {
            throw new InvalidReservationException(ex.Message, ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error confirming reservation: {ex.Message}", ex);
        }
    }

    public bool Cancel(Guid reservationId)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_cancel_reservation(@p_reservation_id)", connection);

            command.Parameters.AddWithValue("p_reservation_id", reservationId);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return reader.GetBoolean(reader.GetOrdinal("is_late"));
            }

            throw new InvalidOperationException("Failed to cancel reservation");
        }
        catch (PostgresException ex) when (ex.SqlState == "45204")
        {
            throw new ReservationNotFoundException(ex.Message, ex);
        }
        catch (PostgresException ex) when (ex.SqlState == "45206")
        {
            throw new InvalidReservationException(ex.Message, ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error cancelling reservation: {ex.Message}", ex);
        }
    }

    public void Complete(Guid reservationId)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT sp_complete_reservation(@p_reservation_id)", connection);

            command.Parameters.AddWithValue("p_reservation_id", reservationId);

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (PostgresException ex) when (ex.SqlState == "45204")
        {
            throw new ReservationNotFoundException(ex.Message, ex);
        }
        catch (PostgresException ex) when (ex.SqlState == "45207")
        {
            throw new InvalidReservationException(ex.Message, ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error completing reservation: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Reservation> GetExpiredPendingReservations()
    {
        try
        {
            var list = new List<Reservation>();

            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_expired_pending_reservations()", connection);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                // SP returns fewer columns (no EndTime, CreatedAt). We read what we have and fill safe defaults.
                var id = reader.GetGuid(reader.GetOrdinal("id"));
                var userId = reader.GetGuid(reader.GetOrdinal("user_id"));
                var spotId = reader.GetGuid(reader.GetOrdinal("spot_id"));
                var startTime = reader.GetDateTime(reader.GetOrdinal("start_time"));
                var deadline = reader.GetDateTime(reader.GetOrdinal("cancellation_deadline"));

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
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error getting expired reservations: {ex.Message}", ex);
        }
    }

    private static Reservation MapReservation(NpgsqlDataReader reader)
    {
        var id = reader.GetGuid(reader.GetOrdinal("id"));
        var userId = reader.GetGuid(reader.GetOrdinal("user_id"));
        var spotId = reader.GetGuid(reader.GetOrdinal("spot_id"));
        var startTime = reader.GetDateTime(reader.GetOrdinal("start_time"));

        DateTime? endTime = null;
        var endOrdinal = reader.GetOrdinal("end_time");
        if (!reader.IsDBNull(endOrdinal))
        {
            endTime = reader.GetDateTime(endOrdinal);
        }

        var statusStr = reader.GetString(reader.GetOrdinal("status"));
        var status = statusStr switch
        {
            "Pending" => ReservationStatus.Pending,
            "Confirmed" => ReservationStatus.Confirmed,
            "Cancelled" => ReservationStatus.Cancelled,
            "Completed" => ReservationStatus.Completed,
            _ => throw new InvalidOperationException($"Unknown Status from DB: {statusStr}")
        };

        var deadline = reader.GetDateTime(reader.GetOrdinal("cancellation_deadline"));
        var createdAt = reader.GetDateTime(reader.GetOrdinal("created_at"));

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
