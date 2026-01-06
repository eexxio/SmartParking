using Microsoft.Extensions.Logging;
using SmartParking.DataAccess;
using SmartParking.Domain;
using SmartParking.Domain.Exceptions;

namespace SmartParking.Business;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IWalletRepository walletRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public User RegisterUser(string email, string fullName, bool isEVUser)
    {
        try
        {
            _logger.LogInformation("Attempting to register user with email: {Email}", email);

            var existingUser = _userRepository.GetByEmail(email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", email);
                throw new InvalidUserDataException($"Email {email} is already registered");
            }

            var user = new User(email, fullName, isEVUser);
            var createdUser = _userRepository.Create(user);

            var wallet = new UserWallet(createdUser.Id, 100.00m);
            _walletRepository.Create(wallet);

            _logger.LogInformation("User registered successfully with ID: {UserId}, Email: {Email}", createdUser.Id, createdUser.Email);

            return createdUser;
        }
        catch (InvalidUserDataException)
        {
            throw;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Validation error during user registration");
            throw new InvalidUserDataException(ex.Message, ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Validation error during user registration");
            throw new InvalidUserDataException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration for email: {Email}", email);
            throw;
        }
    }

    public User GetUser(Guid userId)
    {
        try
        {
            _logger.LogInformation("Retrieving user with ID: {UserId}", userId);

            var user = _userRepository.GetById(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", userId);
                throw new UserNotFoundException($"User with ID {userId} not found");
            }

            _logger.LogInformation("User retrieved successfully: {UserId}", userId);
            return user;
        }
        catch (UserNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID: {UserId}", userId);
            throw;
        }
    }

    public void UpdateUser(Guid userId, string fullName, bool isEVUser)
    {
        try
        {
            _logger.LogInformation("Updating user with ID: {UserId}", userId);

            var user = _userRepository.GetById(userId);
            if (user == null)
            {
                _logger.LogWarning("Update failed: User not found with ID: {UserId}", userId);
                throw new UserNotFoundException($"User with ID {userId} not found");
            }

            user.Update(fullName, isEVUser);
            _userRepository.Update(user);

            _logger.LogInformation("User updated successfully: {UserId}", userId);
        }
        catch (UserNotFoundException)
        {
            throw;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Validation error during user update");
            throw new InvalidUserDataException(ex.Message, ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Validation error during user update");
            throw new InvalidUserDataException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID: {UserId}", userId);
            throw;
        }
    }
}
