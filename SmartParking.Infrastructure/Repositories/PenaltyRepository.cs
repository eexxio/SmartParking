using Npgsql;
using SmartParking.Infrastructure.Interfaces;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Exceptions;
using System.Data;

namespace SmartParking.Infrastructure.Repositories;

public class PenaltyRepository : IPenaltyRepository
{
    private readonly string _connectionString;

    public PenaltyRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public Penalty Create(Penalty penalty)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(
                "SELECT * FROM sp_create_penalty(@p_reservation_id, @p_amount, @p_reason)",
                connection);

            command.Parameters.AddWithValue("p_reservation_id", penalty.ReservationId);
            command.Parameters.AddWithValue("p_amount", penalty.Amount);
            command.Parameters.AddWithValue("p_reason", penalty.Reason);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                var penaltyId = reader.GetGuid(reader.GetOrdinal("penalty_id"));
                return new Penalty(
                    id: penaltyId,
                    reservationId: penalty.ReservationId,
                    amount: penalty.Amount,
                    reason: penalty.Reason,
                    createdAt: DateTime.UtcNow);
            }

            throw new InvalidOperationException("Failed to create penalty");
        }
        catch (PostgresException ex) when (ex.SqlState == "45204")
        {
            throw new ReservationNotFoundException(ex.Message, ex);
        }
        catch (PostgresException ex) when (ex.SqlState is "45208" or "45209")
        {
            throw new InvalidPenaltyException(ex.Message, ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error creating penalty: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Penalty> GetByReservationId(Guid reservationId)
    {
        try
        {
            var list = new List<Penalty>();

            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_penalties_by_reservation_id(@p_reservation_id)", connection);

            command.Parameters.AddWithValue("p_reservation_id", reservationId);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(MapPenalty(reader));
            }

            return list;
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error getting penalties: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Penalty> GetByUserId(Guid userId)
    {
        try
        {
            var list = new List<Penalty>();

            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_penalties_by_user_id(@p_user_id)", connection);

            command.Parameters.AddWithValue("p_user_id", userId);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(MapPenalty(reader));
            }

            return list;
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"DB error getting penalties by user: {ex.Message}", ex);
        }
    }

    private static Penalty MapPenalty(NpgsqlDataReader reader)
    {
        return new Penalty(
            id: reader.GetGuid(reader.GetOrdinal("id")),
            reservationId: reader.GetGuid(reader.GetOrdinal("reservation_id")),
            amount: reader.GetDecimal(reader.GetOrdinal("amount")),
            reason: reader.GetString(reader.GetOrdinal("reason")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")));
    }
}
