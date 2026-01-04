-- =============================================
-- MEMBER 3: Reservation & Penalty Stored Procedures
-- =============================================

USE SmartParkingDB;
GO

PRINT '========================================';
PRINT 'Creating Reservation & Penalty Stored Procedures (Member 3)...';
PRINT '========================================';

-- =============================================
-- sp_CreateReservation
-- Creates a new reservation
-- =============================================
IF OBJECT_ID('sp_CreateReservation', 'P') IS NOT NULL
    DROP PROCEDURE sp_CreateReservation;
GO

CREATE PROCEDURE sp_CreateReservation
    @UserId UNIQUEIDENTIFIER,
    @SpotId UNIQUEIDENTIFIER,
    @CancellationTimeoutMinutes INT = 15,
    @ReservationId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Check if user exists
        IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = @UserId)
            THROW 50201, 'User not found', 1;

        -- Check if spot exists
        IF NOT EXISTS (SELECT 1 FROM ParkingSpots WHERE Id = @SpotId)
            THROW 50202, 'Parking spot not found', 1;

        -- Check if spot is available
        IF EXISTS (SELECT 1 FROM ParkingSpots WHERE Id = @SpotId AND IsOccupied = 1)
            THROW 50203, 'Parking spot is already occupied', 1;

        -- Create reservation
        SET @ReservationId = NEWID();
        INSERT INTO Reservations (Id, UserId, SpotId, StartTime, EndTime, Status, CancellationDeadline, CreatedAt)
        VALUES (
            @ReservationId,
            @UserId,
            @SpotId,
            GETDATE(),
            NULL,
            'Pending',
            DATEADD(MINUTE, @CancellationTimeoutMinutes, GETDATE()),
            GETDATE()
        );

        -- Mark spot as occupied
        UPDATE ParkingSpots
        SET IsOccupied = 1
        WHERE Id = @SpotId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_CreateReservation created';

-- =============================================
-- sp_GetReservationById
-- Retrieves reservation by ID
-- =============================================
IF OBJECT_ID('sp_GetReservationById', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetReservationById;
GO

CREATE PROCEDURE sp_GetReservationById
    @ReservationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id,
        r.UserId,
        r.SpotId,
        r.StartTime,
        r.EndTime,
        r.Status,
        r.CancellationDeadline,
        r.CreatedAt,
        u.Email AS UserEmail,
        u.FullName AS UserName,
        u.IsEVUser,
        ps.SpotNumber,
        ps.SpotType,
        ps.HourlyRate
    FROM Reservations r
    INNER JOIN Users u ON r.UserId = u.Id
    INNER JOIN ParkingSpots ps ON r.SpotId = ps.Id
    WHERE r.Id = @ReservationId;
END
GO
PRINT '✓ sp_GetReservationById created';

-- =============================================
-- sp_GetReservationsByUserId
-- Retrieves all reservations for a user
-- =============================================
IF OBJECT_ID('sp_GetReservationsByUserId', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetReservationsByUserId;
GO

CREATE PROCEDURE sp_GetReservationsByUserId
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id,
        r.UserId,
        r.SpotId,
        r.StartTime,
        r.EndTime,
        r.Status,
        r.CancellationDeadline,
        r.CreatedAt,
        ps.SpotNumber,
        ps.SpotType,
        ps.HourlyRate
    FROM Reservations r
    INNER JOIN ParkingSpots ps ON r.SpotId = ps.Id
    WHERE r.UserId = @UserId
    ORDER BY r.CreatedAt DESC;
END
GO
PRINT '✓ sp_GetReservationsByUserId created';

-- =============================================
-- sp_ConfirmReservation
-- Confirms a pending reservation
-- =============================================
IF OBJECT_ID('sp_ConfirmReservation', 'P') IS NOT NULL
    DROP PROCEDURE sp_ConfirmReservation;
GO

CREATE PROCEDURE sp_ConfirmReservation
    @ReservationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @CurrentStatus NVARCHAR(20);

        -- Get current status
        SELECT @CurrentStatus = Status
        FROM Reservations
        WHERE Id = @ReservationId;

        -- Check if reservation exists
        IF @CurrentStatus IS NULL
            THROW 50204, 'Reservation not found', 1;

        -- Check if reservation is pending
        IF @CurrentStatus != 'Pending'
            THROW 50205, 'Only pending reservations can be confirmed', 1;

        -- Update status to Confirmed
        UPDATE Reservations
        SET Status = 'Confirmed'
        WHERE Id = @ReservationId;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_ConfirmReservation created';

-- =============================================
-- sp_CancelReservation
-- Cancels a reservation
-- =============================================
IF OBJECT_ID('sp_CancelReservation', 'P') IS NOT NULL
    DROP PROCEDURE sp_CancelReservation;
GO

CREATE PROCEDURE sp_CancelReservation
    @ReservationId UNIQUEIDENTIFIER,
    @IsLate BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(20);
        DECLARE @SpotId UNIQUEIDENTIFIER;
        DECLARE @CancellationDeadline DATETIME;

        -- Get reservation details
        SELECT
            @CurrentStatus = Status,
            @SpotId = SpotId,
            @CancellationDeadline = CancellationDeadline
        FROM Reservations
        WHERE Id = @ReservationId;

        -- Check if reservation exists
        IF @CurrentStatus IS NULL
            THROW 50204, 'Reservation not found', 1;

        -- Check if reservation can be cancelled
        IF @CurrentStatus IN ('Cancelled', 'Completed')
            THROW 50206, 'Reservation is already cancelled or completed', 1;

        -- Check if cancellation is late
        SET @IsLate = CASE WHEN GETDATE() > @CancellationDeadline THEN 1 ELSE 0 END;

        -- Update reservation status
        UPDATE Reservations
        SET Status = 'Cancelled'
        WHERE Id = @ReservationId;

        -- Free the parking spot
        UPDATE ParkingSpots
        SET IsOccupied = 0
        WHERE Id = @SpotId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_CancelReservation created';

-- =============================================
-- sp_CompleteReservation
-- Completes a reservation (user finished parking)
-- =============================================
IF OBJECT_ID('sp_CompleteReservation', 'P') IS NOT NULL
    DROP PROCEDURE sp_CompleteReservation;
GO

CREATE PROCEDURE sp_CompleteReservation
    @ReservationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(20);
        DECLARE @SpotId UNIQUEIDENTIFIER;
        DECLARE @StartTime DATETIME;

        -- Get reservation details
        SELECT
            @CurrentStatus = Status,
            @SpotId = SpotId,
            @StartTime = StartTime
        FROM Reservations
        WHERE Id = @ReservationId;

        -- Check if reservation exists
        IF @CurrentStatus IS NULL
            THROW 50204, 'Reservation not found', 1;

        -- Check if reservation can be completed
        IF @CurrentStatus NOT IN ('Pending', 'Confirmed')
            THROW 50207, 'Only pending or confirmed reservations can be completed', 1;

        -- Update reservation
        UPDATE Reservations
        SET Status = 'Completed',
            EndTime = GETDATE()
        WHERE Id = @ReservationId;

        -- Free the parking spot
        UPDATE ParkingSpots
        SET IsOccupied = 0
        WHERE Id = @SpotId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_CompleteReservation created';

-- =============================================
-- sp_GetExpiredPendingReservations
-- Gets all pending reservations past their cancellation deadline
-- =============================================
IF OBJECT_ID('sp_GetExpiredPendingReservations', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetExpiredPendingReservations;
GO

CREATE PROCEDURE sp_GetExpiredPendingReservations
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Id,
        r.UserId,
        r.SpotId,
        r.StartTime,
        r.Status,
        r.CancellationDeadline,
        u.Email,
        ps.SpotNumber
    FROM Reservations r
    INNER JOIN Users u ON r.UserId = u.Id
    INNER JOIN ParkingSpots ps ON r.SpotId = ps.Id
    WHERE r.Status = 'Pending'
      AND r.CancellationDeadline < GETDATE();
END
GO
PRINT '✓ sp_GetExpiredPendingReservations created';

-- =============================================
-- sp_CreatePenalty
-- Creates a penalty record
-- =============================================
IF OBJECT_ID('sp_CreatePenalty', 'P') IS NOT NULL
    DROP PROCEDURE sp_CreatePenalty;
GO

CREATE PROCEDURE sp_CreatePenalty
    @ReservationId UNIQUEIDENTIFIER,
    @Amount DECIMAL(10, 2),
    @Reason NVARCHAR(255),
    @PenaltyId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Validate input
        IF @Amount <= 0
            THROW 50208, 'Penalty amount must be greater than 0', 1;

        IF LEN(@Reason) < 5
            THROW 50209, 'Penalty reason must be at least 5 characters long', 1;

        -- Check if reservation exists
        IF NOT EXISTS (SELECT 1 FROM Reservations WHERE Id = @ReservationId)
            THROW 50204, 'Reservation not found', 1;

        -- Create penalty
        SET @PenaltyId = NEWID();
        INSERT INTO Penalties (Id, ReservationId, Amount, Reason, CreatedAt)
        VALUES (@PenaltyId, @ReservationId, @Amount, @Reason, GETDATE());
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_CreatePenalty created';

-- =============================================
-- sp_GetPenaltiesByReservationId
-- Retrieves all penalties for a reservation
-- =============================================
IF OBJECT_ID('sp_GetPenaltiesByReservationId', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetPenaltiesByReservationId;
GO

CREATE PROCEDURE sp_GetPenaltiesByReservationId
    @ReservationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, ReservationId, Amount, Reason, CreatedAt
    FROM Penalties
    WHERE ReservationId = @ReservationId
    ORDER BY CreatedAt DESC;
END
GO
PRINT '✓ sp_GetPenaltiesByReservationId created';

-- =============================================
-- sp_GetPenaltiesByUserId
-- Retrieves all penalties for a user
-- =============================================
IF OBJECT_ID('sp_GetPenaltiesByUserId', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetPenaltiesByUserId;
GO

CREATE PROCEDURE sp_GetPenaltiesByUserId
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.ReservationId,
        p.Amount,
        p.Reason,
        p.CreatedAt,
        r.SpotId,
        ps.SpotNumber
    FROM Penalties p
    INNER JOIN Reservations r ON p.ReservationId = r.Id
    INNER JOIN ParkingSpots ps ON r.SpotId = ps.Id
    WHERE r.UserId = @UserId
    ORDER BY p.CreatedAt DESC;
END
GO
PRINT '✓ sp_GetPenaltiesByUserId created';

PRINT '========================================';
PRINT 'Member 3 Stored Procedures Created Successfully!';
PRINT '========================================';
GO
