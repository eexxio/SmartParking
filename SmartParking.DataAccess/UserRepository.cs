using Microsoft.Data.SqlClient;
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
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_CreateUser", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@IsEVUser", user.IsEVUser);
            command.Parameters.AddWithValue("@InitialBalance", 100.00m);

            var userIdParam = new SqlParameter("@UserId", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(userIdParam);

            connection.Open();
            command.ExecuteNonQuery();

            var userId = (Guid)userIdParam.Value;
            return new User(userId, user.Email, user.FullName, user.IsEVUser, DateTime.UtcNow, true);
        }
        catch (SqlException ex) when (ex.Number == 50004)
        {
            throw new InvalidUserDataException($"Email already exists: {user.Email}", ex);
        }
        catch (SqlException ex) when (ex.Number >= 50001 && ex.Number <= 50003)
        {
            throw new InvalidUserDataException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error occurred while creating user: {ex.Message}", ex);
        }
    }

    public User? GetById(Guid id)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetUserById", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@UserId", id);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return MapUserFromReader(reader);
            }

            return null;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error occurred while retrieving user: {ex.Message}", ex);
        }
    }

    public User? GetByEmail(string email)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetUserByEmail", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@Email", email);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return MapUserFromReader(reader);
            }

            return null;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error occurred while retrieving user by email: {ex.Message}", ex);
        }
    }

    public void Update(User user)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_UpdateUser", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@UserId", user.Id);
            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@IsEVUser", user.IsEVUser);
            command.Parameters.AddWithValue("@IsActive", user.IsActive);

            connection.Open();
            var rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                throw new UserNotFoundException($"User with ID {user.Id} not found");
            }
        }
        catch (SqlException ex) when (ex.Number >= 50001 && ex.Number <= 50003)
        {
            throw new InvalidUserDataException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error occurred while updating user: {ex.Message}", ex);
        }
    }

    public void Delete(Guid id)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("DELETE FROM Users WHERE Id = @UserId", connection);
            command.CommandType = CommandType.Text;

            command.Parameters.AddWithValue("@UserId", id);

            connection.Open();
            var rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                throw new UserNotFoundException($"User with ID {id} not found");
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error occurred while deleting user: {ex.Message}", ex);
        }
    }

    private static User MapUserFromReader(SqlDataReader reader)
    {
        return new User(
            id: reader.GetGuid(reader.GetOrdinal("Id")),
            email: reader.GetString(reader.GetOrdinal("Email")),
            fullName: reader.GetString(reader.GetOrdinal("FullName")),
            isEVUser: reader.GetBoolean(reader.GetOrdinal("IsEVUser")),
            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            isActive: reader.GetBoolean(reader.GetOrdinal("IsActive"))
        );
    }
}
