-- =============================================
-- MEMBER 4: Payment Stored Procedures
-- =============================================

USE SmartParkingDB;
GO

PRINT '========================================';
PRINT 'Creating Payment Stored Procedures (Member 4)...';
PRINT '========================================';

-- =============================================
-- sp_CreatePayment
-- Creates a new payment record
-- =============================================
IF OBJECT_ID('sp_CreatePayment', 'P') IS NOT NULL
    DROP PROCEDURE sp_CreatePayment;
GO

CREATE PROCEDURE sp_CreatePayment
    @ReservationId UNIQUEIDENTIFIER,
    @Amount DECIMAL(10, 2),
    @PaymentId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Validate amount
        IF @Amount <= 0
            THROW 50301, 'Payment amount must be greater than 0', 1;

        -- Check if reservation exists
        IF NOT EXISTS (SELECT 1 FROM Reservations WHERE Id = @ReservationId)
            THROW 50302, 'Reservation not found', 1;

        -- Check if payment already exists for this reservation
        IF EXISTS (SELECT 1 FROM Payments WHERE ReservationId = @ReservationId AND PaymentStatus = 'Completed')
            THROW 50303, 'Payment already completed for this reservation', 1;

        -- Create payment
        SET @PaymentId = NEWID();
        INSERT INTO Payments (Id, ReservationId, Amount, PaymentStatus, CreatedAt)
        VALUES (@PaymentId, @ReservationId, @Amount, 'Pending', GETDATE());
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_CreatePayment created';

-- =============================================
-- sp_GetPaymentById
-- Retrieves payment by ID
-- =============================================
IF OBJECT_ID('sp_GetPaymentById', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetPaymentById;
GO

CREATE PROCEDURE sp_GetPaymentById
    @PaymentId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.ReservationId,
        p.Amount,
        p.PaymentStatus,
        p.CreatedAt,
        r.UserId,
        r.SpotId,
        r.StartTime,
        r.EndTime,
        u.Email AS UserEmail,
        u.FullName AS UserName,
        ps.SpotNumber,
        ps.HourlyRate
    FROM Payments p
    INNER JOIN Reservations r ON p.ReservationId = r.Id
    INNER JOIN Users u ON r.UserId = u.Id
    INNER JOIN ParkingSpots ps ON r.SpotId = ps.Id
    WHERE p.Id = @PaymentId;
END
GO
PRINT '✓ sp_GetPaymentById created';

-- =============================================
-- sp_GetPaymentByReservationId
-- Retrieves payment by reservation ID
-- =============================================
IF OBJECT_ID('sp_GetPaymentByReservationId', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetPaymentByReservationId;
GO

CREATE PROCEDURE sp_GetPaymentByReservationId
    @ReservationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.ReservationId,
        p.Amount,
        p.PaymentStatus,
        p.CreatedAt
    FROM Payments p
    WHERE p.ReservationId = @ReservationId;
END
GO
PRINT '✓ sp_GetPaymentByReservationId created';

-- =============================================
-- sp_GetPaymentsByUserId
-- Retrieves all payments for a user
-- =============================================
IF OBJECT_ID('sp_GetPaymentsByUserId', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetPaymentsByUserId;
GO

CREATE PROCEDURE sp_GetPaymentsByUserId
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.ReservationId,
        p.Amount,
        p.PaymentStatus,
        p.CreatedAt,
        r.SpotId,
        r.StartTime,
        r.EndTime,
        ps.SpotNumber,
        ps.HourlyRate
    FROM Payments p
    INNER JOIN Reservations r ON p.ReservationId = r.Id
    INNER JOIN ParkingSpots ps ON r.SpotId = ps.Id
    WHERE r.UserId = @UserId
    ORDER BY p.CreatedAt DESC;
END
GO
PRINT '✓ sp_GetPaymentsByUserId created';

-- =============================================
-- sp_UpdatePaymentStatus
-- Updates payment status
-- =============================================
IF OBJECT_ID('sp_UpdatePaymentStatus', 'P') IS NOT NULL
    DROP PROCEDURE sp_UpdatePaymentStatus;
GO

CREATE PROCEDURE sp_UpdatePaymentStatus
    @PaymentId UNIQUEIDENTIFIER,
    @PaymentStatus NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Validate payment status
        IF @PaymentStatus NOT IN ('Pending', 'Completed', 'Failed')
            THROW 50304, 'Invalid payment status', 1;

        -- Check if payment exists
        IF NOT EXISTS (SELECT 1 FROM Payments WHERE Id = @PaymentId)
            THROW 50305, 'Payment not found', 1;

        -- Update status
        UPDATE Payments
        SET PaymentStatus = @PaymentStatus
        WHERE Id = @PaymentId;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_UpdatePaymentStatus created';

-- =============================================
-- sp_CalculatePaymentAmount
-- Calculates payment amount based on reservation duration
-- =============================================
IF OBJECT_ID('sp_CalculatePaymentAmount', 'P') IS NOT NULL
    DROP PROCEDURE sp_CalculatePaymentAmount;
GO

CREATE PROCEDURE sp_CalculatePaymentAmount
    @ReservationId UNIQUEIDENTIFIER,
    @Amount DECIMAL(10, 2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @StartTime DATETIME;
        DECLARE @EndTime DATETIME;
        DECLARE @HourlyRate DECIMAL(10, 2);
        DECLARE @DurationHours DECIMAL(10, 2);

        -- Get reservation details
        SELECT
            @StartTime = r.StartTime,
            @EndTime = r.EndTime,
            @HourlyRate = ps.HourlyRate
        FROM Reservations r
        INNER JOIN ParkingSpots ps ON r.SpotId = ps.Id
        WHERE r.Id = @ReservationId;

        -- Check if reservation exists
        IF @StartTime IS NULL
            THROW 50302, 'Reservation not found', 1;

        -- Check if reservation has ended
        IF @EndTime IS NULL
            THROW 50306, 'Reservation has not ended yet', 1;

        -- Calculate duration in hours
        SET @DurationHours = DATEDIFF(MINUTE, @StartTime, @EndTime) / 60.0;

        -- Calculate amount (rounded to 2 decimal places)
        SET @Amount = ROUND(@HourlyRate * @DurationHours, 2);

        -- Ensure minimum charge (at least the hourly rate)
        IF @Amount < @HourlyRate
            SET @Amount = @HourlyRate;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO
PRINT '✓ sp_CalculatePaymentAmount created';

-- =============================================
-- sp_ProcessPayment
-- Processes a payment (creates payment and deducts from wallet)
-- =============================================
IF OBJECT_ID('sp_ProcessPayment', 'P') IS NOT NULL
    DROP PROCEDURE sp_ProcessPayment;
GO

CREATE PROCEDURE sp_ProcessPayment
    @ReservationId UNIQUEIDENTIFIER,
    @PaymentId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Amount DECIMAL(10, 2);
        DECLARE @UserId UNIQUEIDENTIFIER;
        DECLARE @CurrentBalance DECIMAL(10, 2);

        -- Calculate payment amount
        EXEC sp_CalculatePaymentAmount @ReservationId, @Amount OUTPUT;

        -- Get user ID from reservation
        SELECT @UserId = UserId
        FROM Reservations
        WHERE Id = @ReservationId;

        -- Get current balance
        SELECT @CurrentBalance = Balance
        FROM UserWallets
        WHERE UserId = @UserId;

        IF @CurrentBalance IS NULL
            THROW 50307, 'User wallet not found', 1;

        -- Check sufficient balance
        IF @CurrentBalance < @Amount
            THROW 50308, 'Insufficient balance', 1;

        -- Create payment record
        EXEC sp_CreatePayment @ReservationId, @Amount, @PaymentId OUTPUT;

        -- Deduct from wallet
        EXEC sp_DeductFromWallet @UserId, @Amount;

        -- Update payment status to Completed
        UPDATE Payments
        SET PaymentStatus = 'Completed'
        WHERE Id = @PaymentId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Update payment status to Failed if it was created
        IF @PaymentId IS NOT NULL
        BEGIN
            UPDATE Payments
            SET PaymentStatus = 'Failed'
            WHERE Id = @PaymentId;
        END

        THROW;
    END CATCH
END
GO
PRINT '✓ sp_ProcessPayment created';

-- =============================================
-- sp_GetPaymentStatistics
-- Returns payment statistics
-- =============================================
IF OBJECT_ID('sp_GetPaymentStatistics', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetPaymentStatistics;
GO

CREATE PROCEDURE sp_GetPaymentStatistics
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*) AS TotalPayments,
        SUM(CASE WHEN PaymentStatus = 'Completed' THEN 1 ELSE 0 END) AS CompletedPayments,
        SUM(CASE WHEN PaymentStatus = 'Failed' THEN 1 ELSE 0 END) AS FailedPayments,
        SUM(CASE WHEN PaymentStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingPayments,
        SUM(CASE WHEN PaymentStatus = 'Completed' THEN Amount ELSE 0 END) AS TotalRevenue,
        AVG(CASE WHEN PaymentStatus = 'Completed' THEN Amount ELSE NULL END) AS AveragePayment
    FROM Payments;
END
GO
PRINT '✓ sp_GetPaymentStatistics created';

PRINT '========================================';
PRINT 'Member 4 Stored Procedures Created Successfully!';
PRINT '========================================';
GO
