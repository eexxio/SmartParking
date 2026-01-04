-- =============================================
-- MEMBER 2: Parking Spot Stored Procedures
-- =============================================

USE SmartParkingDB;
GO

PRINT '========================================';
PRINT 'Creating Parking Spot Stored Procedures (Member 2)...';
PRINT '========================================';

-- =============================================
-- sp_CreateParkingSpot
-- Creates a new parking spot
-- =============================================
IF OBJECT_ID('sp_CreateParkingSpot', 'P') IS NOT NULL
    DROP PROCEDURE sp_CreateParkingSpot;
GO

CREATE PROCEDURE sp_CreateParkingSpot
    @SpotNumber NVARCHAR(10),
    @SpotType NVARCHAR(20),
    @HourlyRate DECIMAL(10, 2),
    @SpotId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Validate input
        IF LEN(@SpotNumber) < 5
            THROW 50101, 'Spot number must be at least 5 characters long', 1;

        IF @SpotType NOT IN ('Regular', 'EV')
            THROW 50102, 'Spot type must be Regular or EV', 1;

        IF @HourlyRate <= 0
            THROW 50103, 'Hourly rate must be greater than 0', 1;

        -- Check if spot number already exists
        IF EXISTS (SELECT 1 FROM ParkingSpots WHERE SpotNumber = @SpotNumber)
            THROW 50104, 'Spot number already exists', 1;

        -- Create parking spot
        SET @SpotId = NEWID();
        INSERT INTO ParkingSpots (Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt)
        VALUES (@SpotId, @SpotNumber, @SpotType, 0, @HourlyRate, GETDATE());
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_CreateParkingSpot created';

-- =============================================
-- sp_GetParkingSpotById
-- Retrieves parking spot by ID
-- =============================================
IF OBJECT_ID('sp_GetParkingSpotById', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetParkingSpotById;
GO

CREATE PROCEDURE sp_GetParkingSpotById
    @SpotId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt
    FROM ParkingSpots
    WHERE Id = @SpotId;
END
GO
PRINT '✓ sp_GetParkingSpotById created';

-- =============================================
-- sp_GetAllParkingSpots
-- Retrieves all parking spots
-- =============================================
IF OBJECT_ID('sp_GetAllParkingSpots', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetAllParkingSpots;
GO

CREATE PROCEDURE sp_GetAllParkingSpots
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt
    FROM ParkingSpots
    ORDER BY SpotNumber;
END
GO
PRINT '✓ sp_GetAllParkingSpots created';

-- =============================================
-- sp_GetAvailableParkingSpots
-- Retrieves all available (unoccupied) parking spots
-- =============================================
IF OBJECT_ID('sp_GetAvailableParkingSpots', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetAvailableParkingSpots;
GO

CREATE PROCEDURE sp_GetAvailableParkingSpots
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt
    FROM ParkingSpots
    WHERE IsOccupied = 0
    ORDER BY SpotType, SpotNumber;
END
GO
PRINT '✓ sp_GetAvailableParkingSpots created';

-- =============================================
-- sp_GetAvailableSpotsByType
-- Retrieves available parking spots by type
-- =============================================
IF OBJECT_ID('sp_GetAvailableSpotsByType', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetAvailableSpotsByType;
GO

CREATE PROCEDURE sp_GetAvailableSpotsByType
    @SpotType NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Validate spot type
        IF @SpotType NOT IN ('Regular', 'EV')
            THROW 50102, 'Spot type must be Regular or EV', 1;

        SELECT Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt
        FROM ParkingSpots
        WHERE IsOccupied = 0 AND SpotType = @SpotType
        ORDER BY SpotNumber;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_GetAvailableSpotsByType created';

-- =============================================
-- sp_UpdateSpotOccupancy
-- Updates the occupancy status of a parking spot
-- =============================================
IF OBJECT_ID('sp_UpdateSpotOccupancy', 'P') IS NOT NULL
    DROP PROCEDURE sp_UpdateSpotOccupancy;
GO

CREATE PROCEDURE sp_UpdateSpotOccupancy
    @SpotId UNIQUEIDENTIFIER,
    @IsOccupied BIT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Check if spot exists
        IF NOT EXISTS (SELECT 1 FROM ParkingSpots WHERE Id = @SpotId)
            THROW 50105, 'Parking spot not found', 1;

        -- Update occupancy
        UPDATE ParkingSpots
        SET IsOccupied = @IsOccupied
        WHERE Id = @SpotId;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_UpdateSpotOccupancy created';

-- =============================================
-- sp_ValidateSpotForUser
-- Validates if a user can reserve a specific spot type
-- Returns 1 if valid, throws error if invalid
-- =============================================
IF OBJECT_ID('sp_ValidateSpotForUser', 'P') IS NOT NULL
    DROP PROCEDURE sp_ValidateSpotForUser;
GO

CREATE PROCEDURE sp_ValidateSpotForUser
    @SpotId UNIQUEIDENTIFIER,
    @IsEVUser BIT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @SpotType NVARCHAR(20);
        DECLARE @IsOccupied BIT;

        -- Get spot details
        SELECT @SpotType = SpotType, @IsOccupied = IsOccupied
        FROM ParkingSpots
        WHERE Id = @SpotId;

        -- Check if spot exists
        IF @SpotType IS NULL
            THROW 50105, 'Parking spot not found', 1;

        -- Check if spot is available
        IF @IsOccupied = 1
            THROW 50106, 'Parking spot is already occupied', 1;

        -- Validation logic:
        -- EV users can use both Regular and EV spots
        -- Non-EV users can ONLY use Regular spots
        IF @SpotType = 'EV' AND @IsEVUser = 0
            THROW 50107, 'Non-EV users cannot reserve EV spots', 1;

        -- Return success (1 means valid)
        SELECT 1 AS IsValid;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_ValidateSpotForUser created';

-- =============================================
-- sp_GetSpotStatistics
-- Returns statistics about parking spots
-- =============================================
IF OBJECT_ID('sp_GetSpotStatistics', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetSpotStatistics;
GO

CREATE PROCEDURE sp_GetSpotStatistics
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*) AS TotalSpots,
        SUM(CASE WHEN IsOccupied = 1 THEN 1 ELSE 0 END) AS OccupiedSpots,
        SUM(CASE WHEN IsOccupied = 0 THEN 1 ELSE 0 END) AS AvailableSpots,
        SUM(CASE WHEN SpotType = 'EV' THEN 1 ELSE 0 END) AS TotalEVSpots,
        SUM(CASE WHEN SpotType = 'EV' AND IsOccupied = 0 THEN 1 ELSE 0 END) AS AvailableEVSpots,
        SUM(CASE WHEN SpotType = 'Regular' THEN 1 ELSE 0 END) AS TotalRegularSpots,
        SUM(CASE WHEN SpotType = 'Regular' AND IsOccupied = 0 THEN 1 ELSE 0 END) AS AvailableRegularSpots
    FROM ParkingSpots;
END
GO
PRINT '✓ sp_GetSpotStatistics created';

PRINT '========================================';
PRINT 'Member 2 Stored Procedures Created Successfully!';
PRINT '========================================';
GO
