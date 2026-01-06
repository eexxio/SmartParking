using Microsoft.Extensions.Logging;
using Moq;
using SmartParking.Business;
using SmartParking.DataAccess;
using SmartParking.Domain;
using SmartParking.Domain.Exceptions;

namespace SmartParking.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IWalletRepository> _walletRepositoryMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _walletRepositoryMock = new Mock<IWalletRepository>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _userService = new UserService(
            _userRepositoryMock.Object,
            _walletRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void RegisterUser_ValidData_Success()
    {
        // Arrange
        var email = "test@example.com";
        var fullName = "John Doe";
        var isEVUser = true;

        _userRepositoryMock.Setup(r => r.GetByEmail(email)).Returns((User?)null);
        _userRepositoryMock.Setup(r => r.Create(It.IsAny<User>()))
            .Returns((User u) => u);
        _walletRepositoryMock.Setup(r => r.Create(It.IsAny<UserWallet>()))
            .Returns((UserWallet w) => w);

        // Act
        var result = _userService.RegisterUser(email, fullName, isEVUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Equal(fullName, result.FullName);
        Assert.Equal(isEVUser, result.IsEVUser);
        _userRepositoryMock.Verify(r => r.Create(It.IsAny<User>()), Times.Once);
        _walletRepositoryMock.Verify(r => r.Create(It.Is<UserWallet>(w => w.Balance == 100.00m)), Times.Once);
    }

    [Fact]
    public void RegisterUser_InvalidEmail_ThrowsException()
    {
        // Arrange
        var invalidEmail = "invalid-email";
        var fullName = "John Doe";

        // Act & Assert
        var exception = Assert.Throws<InvalidUserDataException>(() =>
            _userService.RegisterUser(invalidEmail, fullName, false));

        Assert.Contains("Invalid email format", exception.Message);
        _userRepositoryMock.Verify(r => r.Create(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public void RegisterUser_ShortName_ThrowsException()
    {
        // Arrange
        var email = "test@example.com";
        var shortName = "John"; // Less than 5 characters

        // Act & Assert
        var exception = Assert.Throws<InvalidUserDataException>(() =>
            _userService.RegisterUser(email, shortName, false));

        Assert.Contains("at least 5 characters", exception.Message);
        _userRepositoryMock.Verify(r => r.Create(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public void RegisterUser_DuplicateEmail_ThrowsException()
    {
        // Arrange
        var email = "existing@example.com";
        var fullName = "John Doe";
        var existingUser = new User(email, fullName, false);

        _userRepositoryMock.Setup(r => r.GetByEmail(email)).Returns(existingUser);

        // Act & Assert
        var exception = Assert.Throws<InvalidUserDataException>(() =>
            _userService.RegisterUser(email, fullName, false));

        Assert.Contains("already registered", exception.Message);
        _userRepositoryMock.Verify(r => r.Create(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public void RegisterUser_NullEmail_ThrowsException()
    {
        // Arrange
        string? email = null;
        var fullName = "John Doe";

        // Act & Assert
        var exception = Assert.Throws<InvalidUserDataException>(() =>
            _userService.RegisterUser(email!, fullName, false));

        Assert.Contains("Email is required", exception.Message);
    }

    [Fact]
    public void RegisterUser_EmptyEmail_ThrowsException()
    {
        // Arrange
        var email = "";
        var fullName = "John Doe";

        // Act & Assert
        var exception = Assert.Throws<InvalidUserDataException>(() =>
            _userService.RegisterUser(email, fullName, false));

        Assert.Contains("Email is required", exception.Message);
    }

    [Fact]
    public void RegisterUser_NullName_ThrowsException()
    {
        // Arrange
        var email = "test@example.com";
        string? fullName = null;

        // Act & Assert
        var exception = Assert.Throws<InvalidUserDataException>(() =>
            _userService.RegisterUser(email, fullName!, false));

        Assert.Contains("Full name is required", exception.Message);
    }

    [Fact]
    public void RegisterUser_EmptyName_ThrowsException()
    {
        // Arrange
        var email = "test@example.com";
        var fullName = "";

        // Act & Assert
        var exception = Assert.Throws<InvalidUserDataException>(() =>
            _userService.RegisterUser(email, fullName, false));

        Assert.Contains("Full name is required", exception.Message);
    }

    [Fact]
    public void RegisterUser_CreatesWalletWithInitialBalance()
    {
        // Arrange
        var email = "test@example.com";
        var fullName = "John Doe";

        _userRepositoryMock.Setup(r => r.GetByEmail(email)).Returns((User?)null);
        _userRepositoryMock.Setup(r => r.Create(It.IsAny<User>()))
            .Returns((User u) => u);
        _walletRepositoryMock.Setup(r => r.Create(It.IsAny<UserWallet>()))
            .Returns((UserWallet w) => w);

        // Act
        var result = _userService.RegisterUser(email, fullName, false);

        // Assert
        _walletRepositoryMock.Verify(r => r.Create(
            It.Is<UserWallet>(w => w.Balance == 100.00m && w.UserId == result.Id)),
            Times.Once);
    }

    [Fact]
    public void GetUser_ExistingId_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new User("test@example.com", "John Doe", false);

        _userRepositoryMock.Setup(r => r.GetById(userId)).Returns(expectedUser);

        // Act
        var result = _userService.GetUser(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUser.Email, result.Email);
        _userRepositoryMock.Verify(r => r.GetById(userId), Times.Once);
    }

    [Fact]
    public void GetUser_NonExistingId_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepositoryMock.Setup(r => r.GetById(userId)).Returns((User?)null);

        // Act & Assert
        var exception = Assert.Throws<UserNotFoundException>(() =>
            _userService.GetUser(userId));

        Assert.Contains(userId.ToString(), exception.Message);
    }

    [Fact]
    public void UpdateUser_ValidData_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User(userId, "test@example.com", "John Doe", false, DateTime.UtcNow, true);
        var newFullName = "Jane Smith";
        var newIsEVUser = true;

        _userRepositoryMock.Setup(r => r.GetById(userId)).Returns(existingUser);
        _userRepositoryMock.Setup(r => r.Update(It.IsAny<User>()));

        // Act
        _userService.UpdateUser(userId, newFullName, newIsEVUser);

        // Assert
        _userRepositoryMock.Verify(r => r.Update(It.Is<User>(u =>
            u.FullName == newFullName && u.IsEVUser == newIsEVUser)), Times.Once);
    }

    [Fact]
    public void UpdateUser_NonExistingUser_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newFullName = "Jane Smith";

        _userRepositoryMock.Setup(r => r.GetById(userId)).Returns((User?)null);

        // Act & Assert
        var exception = Assert.Throws<UserNotFoundException>(() =>
            _userService.UpdateUser(userId, newFullName, true));

        Assert.Contains(userId.ToString(), exception.Message);
    }

    [Fact]
    public void UpdateUser_ShortName_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User(userId, "test@example.com", "John Doe", false, DateTime.UtcNow, true);
        var shortName = "Jane"; // Less than 5 characters

        _userRepositoryMock.Setup(r => r.GetById(userId)).Returns(existingUser);

        // Act & Assert
        var exception = Assert.Throws<InvalidUserDataException>(() =>
            _userService.UpdateUser(userId, shortName, true));

        Assert.Contains("at least 5 characters", exception.Message);
        _userRepositoryMock.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public void UpdateUser_NullName_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User(userId, "test@example.com", "John Doe", false, DateTime.UtcNow, true);
        string? nullName = null;

        _userRepositoryMock.Setup(r => r.GetById(userId)).Returns(existingUser);

        // Act & Assert
        var exception = Assert.Throws<InvalidUserDataException>(() =>
            _userService.UpdateUser(userId, nullName!, true));

        Assert.Contains("Full name is required", exception.Message);
        _userRepositoryMock.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }
}
