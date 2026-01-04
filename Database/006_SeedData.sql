-- =============================================
-- Seed Data Script
-- Populates database with test data for all members
-- =============================================

USE SmartParkingDB;
GO

PRINT '========================================';
PRINT 'Seeding Database with Test Data...';
PRINT '========================================';

-- =============================================
-- Seed Users & Wallets (Member 1)
-- =============================================
DECLARE @User1Id UNIQUEIDENTIFIER;
DECLARE @User2Id UNIQUEIDENTIFIER;
DECLARE @User3Id UNIQUEIDENTIFIER;
DECLARE @User4Id UNIQUEIDENTIFIER;
DECLARE @User5Id UNIQUEIDENTIFIER;

-- Create test users
EXEC sp_CreateUser 'john.doe@email.com', 'John Doe', 0, 150.00, @User1Id OUTPUT;
EXEC sp_CreateUser 'jane.smith@email.com', 'Jane Smith', 1, 200.00, @User2Id OUTPUT;
EXEC sp_CreateUser 'michael.johnson@email.com', 'Michael Johnson', 1, 100.00, @User3Id OUTPUT;
EXEC sp_CreateUser 'sarah.williams@email.com', 'Sarah Williams', 0, 175.00, @User4Id OUTPUT;
EXEC sp_CreateUser 'david.brown@email.com', 'David Brown', 0, 125.00, @User5Id OUTPUT;

PRINT '✓ Created 5 test users (2 EV users, 3 regular users)';

-- =============================================
-- Seed Parking Spots (Member 2)
-- =============================================
DECLARE @Spot1Id UNIQUEIDENTIFIER;
DECLARE @Spot2Id UNIQUEIDENTIFIER;
DECLARE @Spot3Id UNIQUEIDENTIFIER;
DECLARE @Spot4Id UNIQUEIDENTIFIER;
DECLARE @Spot5Id UNIQUEIDENTIFIER;
DECLARE @Spot6Id UNIQUEIDENTIFIER;
DECLARE @Spot7Id UNIQUEIDENTIFIER;
DECLARE @Spot8Id UNIQUEIDENTIFIER;
DECLARE @Spot9Id UNIQUEIDENTIFIER;
DECLARE @Spot10Id UNIQUEIDENTIFIER;

-- Create parking spots (7 Regular, 3 EV)
EXEC sp_CreateParkingSpot 'A-001-REG', 'Regular', 5.00, @Spot1Id OUTPUT;
EXEC sp_CreateParkingSpot 'A-002-REG', 'Regular', 5.00, @Spot2Id OUTPUT;
EXEC sp_CreateParkingSpot 'A-003-REG', 'Regular', 5.00, @Spot3Id OUTPUT;
EXEC sp_CreateParkingSpot 'B-001-REG', 'Regular', 7.50, @Spot4Id OUTPUT;
EXEC sp_CreateParkingSpot 'B-002-REG', 'Regular', 7.50, @Spot5Id OUTPUT;
EXEC sp_CreateParkingSpot 'B-003-REG', 'Regular', 7.50, @Spot6Id OUTPUT;
EXEC sp_CreateParkingSpot 'C-001-REG', 'Regular', 10.00, @Spot7Id OUTPUT;
EXEC sp_CreateParkingSpot 'EV-01-CHG', 'EV', 12.00, @Spot8Id OUTPUT;
EXEC sp_CreateParkingSpot 'EV-02-CHG', 'EV', 12.00, @Spot9Id OUTPUT;
EXEC sp_CreateParkingSpot 'EV-03-CHG', 'EV', 12.00, @Spot10Id OUTPUT;

PRINT '✓ Created 10 parking spots (7 Regular, 3 EV)';

-- =============================================
-- Seed Sample Reservations (Member 3)
-- =============================================
DECLARE @Reservation1Id UNIQUEIDENTIFIER;
DECLARE @Reservation2Id UNIQUEIDENTIFIER;

-- Create a completed reservation for User1
EXEC sp_CreateReservation @User1Id, @Spot1Id, 15, @Reservation1Id OUTPUT;
EXEC sp_ConfirmReservation @Reservation1Id;

-- Simulate completion after 2 hours
UPDATE Reservations
SET EndTime = DATEADD(HOUR, 2, StartTime),
    Status = 'Completed'
WHERE Id = @Reservation1Id;

-- Free the spot
UPDATE ParkingSpots SET IsOccupied = 0 WHERE Id = @Spot1Id;

PRINT '✓ Created sample completed reservation';

-- Create a pending reservation for User2 (EV user)
EXEC sp_CreateReservation @User2Id, @Spot8Id, 15, @Reservation2Id OUTPUT;

PRINT '✓ Created sample pending reservation (EV spot)';

-- =============================================
-- Seed Sample Payment (Member 4)
-- =============================================
DECLARE @Payment1Id UNIQUEIDENTIFIER;

-- Create payment for the completed reservation
EXEC sp_ProcessPayment @Reservation1Id, @Payment1Id OUTPUT;

PRINT '✓ Created sample payment for completed reservation';

-- =============================================
-- Display Seed Data Summary
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'Seed Data Summary:';
PRINT '========================================';

-- Users summary
SELECT
    COUNT(*) AS TotalUsers,
    SUM(CASE WHEN IsEVUser = 1 THEN 1 ELSE 0 END) AS EVUsers,
    SUM(CASE WHEN IsEVUser = 0 THEN 1 ELSE 0 END) AS RegularUsers
FROM Users;

-- Wallets summary
SELECT
    COUNT(*) AS TotalWallets,
    SUM(Balance) AS TotalBalance,
    AVG(Balance) AS AverageBalance,
    MIN(Balance) AS MinBalance,
    MAX(Balance) AS MaxBalance
FROM UserWallets;

-- Parking spots summary
SELECT
    COUNT(*) AS TotalSpots,
    SUM(CASE WHEN SpotType = 'EV' THEN 1 ELSE 0 END) AS EVSpots,
    SUM(CASE WHEN SpotType = 'Regular' THEN 1 ELSE 0 END) AS RegularSpots,
    SUM(CASE WHEN IsOccupied = 1 THEN 1 ELSE 0 END) AS OccupiedSpots,
    SUM(CASE WHEN IsOccupied = 0 THEN 1 ELSE 0 END) AS AvailableSpots
FROM ParkingSpots;

-- Reservations summary
SELECT
    COUNT(*) AS TotalReservations,
    SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS PendingReservations,
    SUM(CASE WHEN Status = 'Confirmed' THEN 1 ELSE 0 END) AS ConfirmedReservations,
    SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedReservations,
    SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledReservations
FROM Reservations;

-- Payments summary
SELECT
    COUNT(*) AS TotalPayments,
    SUM(CASE WHEN PaymentStatus = 'Completed' THEN 1 ELSE 0 END) AS CompletedPayments,
    SUM(CASE WHEN PaymentStatus = 'Completed' THEN Amount ELSE 0 END) AS TotalRevenue
FROM Payments;

PRINT '';
PRINT '========================================';
PRINT 'Database seeding completed successfully!';
PRINT 'You can now start testing all modules.';
PRINT '========================================';
GO
