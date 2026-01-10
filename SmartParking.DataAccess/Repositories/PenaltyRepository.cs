using Microsoft.Data.SqlClient;
using SmartParking.DataAccess.Interfaces;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Exceptions;
using System.Data;

namespace SmartParking.DataAccess.Repositories;

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
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_CreatePenalty", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@ReservationId", penalty.ReservationId);
            command.Parameters.AddWithValue("@Amount", penalty.Amount);
            command.Parameters.AddWithValue("@Reason", penalty.Reason);

            var penaltyIdParam = new SqlParameter("@PenaltyId", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(penaltyIdParam);

            connection.Open();
            command.ExecuteNonQuery();

            var penaltyId = (Guid)penaltyIdParam.Value;

            return new Penalty(
                id: penaltyId,
                reservationId: penalty.ReservationId,
                amount: penalty.Amount,
                reason: penalty.Reason,
                createdAt: DateTime.UtcNow);
        }
        catch (SqlException ex) when (ex.Number == 50204)
        {
            throw new ReservationNotFoundException(ex.Message, ex);
        }
        catch (SqlException ex) when (ex.Number is 50208 or 50209)
        {
            throw new InvalidPenaltyException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error creating penalty: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Penalty> GetByReservationId(Guid reservationId)
    {
        try
        {
            var list = new List<Penalty>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetPenaltiesByReservationId", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@ReservationId", reservationId);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(MapPenalty(reader));
            }

            return list;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error getting penalties: {ex.Message}", ex);
        }
    }

    public IReadOnlyList<Penalty> GetByUserId(Guid userId)
    {
        try
        {
            var list = new List<Penalty>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetPenaltiesByUserId", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@UserId", userId);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(MapPenalty(reader));
            }

            return list;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"DB error getting penalties by user: {ex.Message}", ex);
        }
    }

    private static Penalty MapPenalty(SqlDataReader reader)
    {
        return new Penalty(
            id: reader.GetGuid(reader.GetOrdinal("Id")),
            reservationId: reader.GetGuid(reader.GetOrdinal("ReservationId")),
            amount: reader.GetDecimal(reader.GetOrdinal("Amount")),
            reason: reader.GetString(reader.GetOrdinal("Reason")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")));
    }
}
