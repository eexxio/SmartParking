using Npgsql;
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
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(
                "INSERT INTO UserWallets (Id, UserId, Balance, UpdatedAt) VALUES (@p_id, @p_user_id, @p_balance, @p_updated_at)",
                connection);

            command.Parameters.AddWithValue("p_id", wallet.Id);
            command.Parameters.AddWithValue("p_user_id", wallet.UserId);
            command.Parameters.AddWithValue("p_balance", wallet.Balance);
            command.Parameters.AddWithValue("p_updated_at", wallet.UpdatedAt);

            connection.Open();
            command.ExecuteNonQuery();

            return wallet;
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"Database error occurred while creating wallet: {ex.Message}", ex);
        }
    }

    public UserWallet? GetByUserId(Guid userId)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_wallet_by_user_id(@p_user_id)", connection);

            command.Parameters.AddWithValue("p_user_id", userId);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return MapWalletFromReader(reader);
            }

            return null;
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"Database error occurred while retrieving wallet: {ex.Message}", ex);
        }
    }

    public void UpdateBalance(Guid walletId, decimal newBalance)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(
                "UPDATE UserWallets SET Balance = @p_balance, UpdatedAt = @p_updated_at WHERE Id = @p_wallet_id",
                connection);

            command.Parameters.AddWithValue("p_wallet_id", walletId);
            command.Parameters.AddWithValue("p_balance", newBalance);
            command.Parameters.AddWithValue("p_updated_at", DateTime.UtcNow);

            connection.Open();
            var rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Wallet with ID {walletId} not found");
            }
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"Database error occurred while updating wallet balance: {ex.Message}", ex);
        }
    }

    private static UserWallet MapWalletFromReader(NpgsqlDataReader reader)
    {
        return new UserWallet(
            id: reader.GetGuid(reader.GetOrdinal("id")),
            userId: reader.GetGuid(reader.GetOrdinal("user_id")),
            balance: reader.GetDecimal(reader.GetOrdinal("balance")),
            updatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at"))
        );
    }
}
