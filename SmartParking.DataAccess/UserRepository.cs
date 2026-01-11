using Npgsql;
using SmartParking.Domain;
using SmartParking.Domain.Exceptions;
using System.Data;

namespace SmartParking.DataAccess;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public User Create(User user)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(
                "SELECT * FROM sp_create_user(@p_email, @p_full_name, @p_is_ev_user, @p_initial_balance)",
                connection);

            command.Parameters.AddWithValue("p_email", user.Email);
            command.Parameters.AddWithValue("p_full_name", user.FullName);
            command.Parameters.AddWithValue("p_is_ev_user", user.IsEVUser);
            command.Parameters.AddWithValue("p_initial_balance", 100.00m);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                var userId = reader.GetGuid(reader.GetOrdinal("user_id"));
                return new User(userId, user.Email, user.FullName, user.IsEVUser, DateTime.UtcNow, true);
            }

            throw new InvalidOperationException("Failed to create user");
        }
        catch (PostgresException ex) when (ex.SqlState == "45004")
        {
            throw new InvalidUserDataException($"Email already exists: {user.Email}", ex);
        }
        catch (PostgresException ex) when (ex.SqlState is "45001" or "45002" or "45003")
        {
            throw new InvalidUserDataException(ex.Message, ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"Database error occurred while creating user: {ex.Message}", ex);
        }
    }

    public User? GetById(Guid id)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_user_by_id(@p_user_id)", connection);

            command.Parameters.AddWithValue("p_user_id", id);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return MapUserFromReader(reader);
            }

            return null;
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"Database error occurred while retrieving user: {ex.Message}", ex);
        }
    }

    public User? GetByEmail(string email)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT * FROM sp_get_user_by_email(@p_email)", connection);

            command.Parameters.AddWithValue("p_email", email);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return MapUserFromReader(reader);
            }

            return null;
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"Database error occurred while retrieving user by email: {ex.Message}", ex);
        }
    }

    public void Update(User user)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("SELECT sp_update_user(@p_user_id, @p_full_name, @p_is_ev_user)", connection);

            command.Parameters.AddWithValue("p_user_id", user.Id);
            command.Parameters.AddWithValue("p_full_name", user.FullName);
            command.Parameters.AddWithValue("p_is_ev_user", user.IsEVUser);

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (PostgresException ex) when (ex.SqlState is "45002" or "45005")
        {
            throw new InvalidUserDataException(ex.Message, ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"Database error occurred while updating user: {ex.Message}", ex);
        }
    }

    public void Delete(Guid id)
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand("DELETE FROM Users WHERE Id = @p_user_id", connection);

            command.Parameters.AddWithValue("p_user_id", id);

            connection.Open();
            var rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                throw new UserNotFoundException($"User with ID {id} not found");
            }
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"Database error occurred while deleting user: {ex.Message}", ex);
        }
    }

    private static User MapUserFromReader(NpgsqlDataReader reader)
    {
        return new User(
            id: reader.GetGuid(reader.GetOrdinal("id")),
            email: reader.GetString(reader.GetOrdinal("email")),
            fullName: reader.GetString(reader.GetOrdinal("full_name")),
            isEVUser: reader.GetBoolean(reader.GetOrdinal("is_ev_user")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("created_at")),
            isActive: reader.GetBoolean(reader.GetOrdinal("is_active"))
        );
    }
}
