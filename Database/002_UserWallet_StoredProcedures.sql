-- =============================================
-- MEMBER 1: User & Wallet Stored Procedures
-- =============================================

USE SmartParkingDB;
GO

PRINT '========================================';
PRINT 'Creating User & Wallet Stored Procedures (Member 1)...';
PRINT '========================================';

-- =============================================
-- sp_CreateUser
-- Creates a new user and automatically creates their wallet
-- =============================================
IF OBJECT_ID('sp_CreateUser', 'P') IS NOT NULL
    DROP PROCEDURE sp_CreateUser;
GO

CREATE PROCEDURE sp_CreateUser
    @Email NVARCHAR(255),
    @FullName NVARCHAR(100),
    @IsEVUser BIT = 0,
    @InitialBalance DECIMAL(10, 2) = 100.00,
    @UserId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validate input
        IF LEN(@Email) < 5
            THROW 50001, 'Email must be at least 5 characters long', 1;

        IF LEN(@FullName) < 5
            THROW 50002, 'Full name must be at least 5 characters long', 1;

        IF @Email NOT LIKE '%@%.%'
            THROW 50003, 'Email format is invalid', 1;

        -- Check if email already exists
        IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
            THROW 50004, 'Email already exists', 1;

        -- Create user
        SET @UserId = NEWID();
        INSERT INTO Users (Id, Email, FullName, IsEVUser, IsActive, CreatedAt)
        VALUES (@UserId, @Email, @FullName, @IsEVUser, 1, GETDATE());

        -- Create wallet automatically
        INSERT INTO UserWallets (Id, UserId, Balance, UpdatedAt)
        VALUES (NEWID(), @UserId, @InitialBalance, GETDATE());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_CreateUser created';

-- =============================================
-- sp_GetUserById
-- Retrieves user by ID
-- =============================================
IF OBJECT_ID('sp_GetUserById', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetUserById;
GO

CREATE PROCEDURE sp_GetUserById
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Email, FullName, IsEVUser, IsActive, CreatedAt
    FROM Users
    WHERE Id = @UserId;
END
GO
PRINT '✓ sp_GetUserById created';

-- =============================================
-- sp_GetUserByEmail
-- Retrieves user by email
-- =============================================
IF OBJECT_ID('sp_GetUserByEmail', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetUserByEmail;
GO

CREATE PROCEDURE sp_GetUserByEmail
    @Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Email, FullName, IsEVUser, IsActive, CreatedAt
    FROM Users
    WHERE Email = @Email;
END
GO
PRINT '✓ sp_GetUserByEmail created';

-- =============================================
-- sp_UpdateUser
-- Updates user information
-- =============================================
IF OBJECT_ID('sp_UpdateUser', 'P') IS NOT NULL
    DROP PROCEDURE sp_UpdateUser;
GO

CREATE PROCEDURE sp_UpdateUser
    @UserId UNIQUEIDENTIFIER,
    @FullName NVARCHAR(100),
    @IsEVUser BIT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Validate input
        IF LEN(@FullName) < 5
            THROW 50002, 'Full name must be at least 5 characters long', 1;

        -- Check if user exists
        IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = @UserId)
            THROW 50005, 'User not found', 1;

        UPDATE Users
        SET FullName = @FullName,
            IsEVUser = @IsEVUser
        WHERE Id = @UserId;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_UpdateUser created';

-- =============================================
-- sp_GetAllUsers
-- Retrieves all active users
-- =============================================
IF OBJECT_ID('sp_GetAllUsers', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetAllUsers;
GO

CREATE PROCEDURE sp_GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Email, FullName, IsEVUser, IsActive, CreatedAt
    FROM Users
    WHERE IsActive = 1
    ORDER BY CreatedAt DESC;
END
GO
PRINT '✓ sp_GetAllUsers created';

-- =============================================
-- sp_GetWalletByUserId
-- Retrieves wallet information for a user
-- =============================================
IF OBJECT_ID('sp_GetWalletByUserId', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetWalletByUserId;
GO

CREATE PROCEDURE sp_GetWalletByUserId
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, UserId, Balance, UpdatedAt
    FROM UserWallets
    WHERE UserId = @UserId;
END
GO
PRINT '✓ sp_GetWalletByUserId created';

-- =============================================
-- sp_AddToWallet
-- Adds funds to user wallet (deposit)
-- =============================================
IF OBJECT_ID('sp_AddToWallet', 'P') IS NOT NULL
    DROP PROCEDURE sp_AddToWallet;
GO

CREATE PROCEDURE sp_AddToWallet
    @UserId UNIQUEIDENTIFIER,
    @Amount DECIMAL(10, 2)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validate amount
        IF @Amount <= 0
            THROW 50006, 'Amount must be greater than 0', 1;

        -- Check if wallet exists
        IF NOT EXISTS (SELECT 1 FROM UserWallets WHERE UserId = @UserId)
            THROW 50007, 'Wallet not found for this user', 1;

        -- Add to balance
        UPDATE UserWallets
        SET Balance = Balance + @Amount,
            UpdatedAt = GETDATE()
        WHERE UserId = @UserId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_AddToWallet created';

-- =============================================
-- sp_DeductFromWallet
-- Deducts funds from user wallet
-- =============================================
IF OBJECT_ID('sp_DeductFromWallet', 'P') IS NOT NULL
    DROP PROCEDURE sp_DeductFromWallet;
GO

CREATE PROCEDURE sp_DeductFromWallet
    @UserId UNIQUEIDENTIFIER,
    @Amount DECIMAL(10, 2)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentBalance DECIMAL(10, 2);

        -- Validate amount
        IF @Amount <= 0
            THROW 50006, 'Amount must be greater than 0', 1;

        -- Get current balance
        SELECT @CurrentBalance = Balance
        FROM UserWallets
        WHERE UserId = @UserId;

        IF @CurrentBalance IS NULL
            THROW 50007, 'Wallet not found for this user', 1;

        -- Check sufficient balance
        IF @CurrentBalance < @Amount
            THROW 50008, 'Insufficient balance', 1;

        -- Deduct from balance
        UPDATE UserWallets
        SET Balance = Balance - @Amount,
            UpdatedAt = GETDATE()
        WHERE UserId = @UserId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_DeductFromWallet created';

-- =============================================
-- sp_GetWalletBalance
-- Gets current wallet balance
-- =============================================
IF OBJECT_ID('sp_GetWalletBalance', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetWalletBalance;
GO

CREATE PROCEDURE sp_GetWalletBalance
    @UserId UNIQUEIDENTIFIER,
    @Balance DECIMAL(10, 2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @Balance = Balance
    FROM UserWallets
    WHERE UserId = @UserId;

    IF @Balance IS NULL
        THROW 50007, 'Wallet not found for this user', 1;
END
GO
PRINT '✓ sp_GetWalletBalance created';

PRINT '========================================';
PRINT 'Member 1 Stored Procedures Created Successfully!';
PRINT '========================================';
GO
