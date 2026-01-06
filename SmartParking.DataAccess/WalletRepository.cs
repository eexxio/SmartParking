using Microsoft.Data.SqlClient;
using SmartParking.Domain;
using SmartParking.Domain.Exceptions;
using System.Data;

namespace SmartParking.DataAccess;

public class WalletRepository : IWalletRepository
{
    private readonly string _connectionString;

    public WalletRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public UserWallet Create(UserWallet wallet)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("INSERT INTO UserWallets (Id, UserId, Balance, UpdatedAt) VALUES (@Id, @UserId, @Balance, @UpdatedAt)", connection);
            command.CommandType = CommandType.Text;

            command.Parameters.AddWithValue("@Id", wallet.Id);
            command.Parameters.AddWithValue("@UserId", wallet.UserId);
            command.Parameters.AddWithValue("@Balance", wallet.Balance);
            command.Parameters.AddWithValue("@UpdatedAt", wallet.UpdatedAt);

            connection.Open();
            command.ExecuteNonQuery();

            return wallet;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error occurred while creating wallet: {ex.Message}", ex);
        }
    }

    public UserWallet? GetByUserId(Guid userId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetWalletByUserId", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@UserId", userId);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return MapWalletFromReader(reader);
            }

            return null;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error occurred while retrieving wallet: {ex.Message}", ex);
        }
    }

    public void UpdateBalance(Guid walletId, decimal newBalance)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE UserWallets SET Balance = @Balance, UpdatedAt = @UpdatedAt WHERE Id = @WalletId", connection);
            command.CommandType = CommandType.Text;

            command.Parameters.AddWithValue("@WalletId", walletId);
            command.Parameters.AddWithValue("@Balance", newBalance);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

            connection.Open();
            var rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Wallet with ID {walletId} not found");
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error occurred while updating wallet balance: {ex.Message}", ex);
        }
    }

    private static UserWallet MapWalletFromReader(SqlDataReader reader)
    {
        return new UserWallet(
            id: reader.GetGuid(reader.GetOrdinal("Id")),
            userId: reader.GetGuid(reader.GetOrdinal("UserId")),
            balance: reader.GetDecimal(reader.GetOrdinal("Balance")),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        );
    }
}
