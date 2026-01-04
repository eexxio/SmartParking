-- =============================================
-- Smart Parking System - Complete Database Schema
-- Team Project - All Members
-- =============================================

USE master;
GO

-- Drop database if exists (for clean setup)
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'SmartParkingDB')
BEGIN
    ALTER DATABASE SmartParkingDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SmartParkingDB;
END
GO

-- Create database
CREATE DATABASE SmartParkingDB;
GO

USE SmartParkingDB;
GO

PRINT '========================================';
PRINT 'Creating Tables...';
PRINT '========================================';

-- =============================================
-- Table: Users (Member 1)
-- Stores user account information
-- =============================================
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(255) NOT NULL UNIQUE,
    FullName NVARCHAR(100) NOT NULL,
    IsEVUser BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    -- Validation constraints
    CONSTRAINT CHK_Users_Email_Length CHECK (LEN(Email) >= 5),
    CONSTRAINT CHK_Users_FullName_Length CHECK (LEN(FullName) >= 5),
    CONSTRAINT CHK_Users_Email_Format CHECK (Email LIKE '%@%.%')
);
GO

CREATE NONCLUSTERED INDEX IX_Users_Email ON Users(Email);
GO
PRINT '✓ Users table created';

-- =============================================
-- Table: UserWallets (Member 1)
-- Stores user balance (app coins)
-- =============================================
CREATE TABLE UserWallets (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    Balance DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_UserWallets_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_UserWallets_Balance CHECK (Balance >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_UserWallets_UserId ON UserWallets(UserId);
GO
PRINT '✓ UserWallets table created';

-- =============================================
-- Table: ParkingSpots (Member 2)
-- Stores parking spot information
-- =============================================
CREATE TABLE ParkingSpots (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SpotNumber NVARCHAR(10) NOT NULL UNIQUE,
    SpotType NVARCHAR(20) NOT NULL,
    IsOccupied BIT NOT NULL DEFAULT 0,
    HourlyRate DECIMAL(10, 2) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT CHK_ParkingSpots_SpotNumber_Length CHECK (LEN(SpotNumber) >= 5),
    CONSTRAINT CHK_ParkingSpots_SpotType CHECK (SpotType IN ('Regular', 'EV')),
    CONSTRAINT CHK_ParkingSpots_HourlyRate CHECK (HourlyRate > 0)
);
GO

CREATE NONCLUSTERED INDEX IX_ParkingSpots_IsOccupied ON ParkingSpots(IsOccupied);
CREATE NONCLUSTERED INDEX IX_ParkingSpots_SpotType ON ParkingSpots(SpotType);
GO
PRINT '✓ ParkingSpots table created';

-- =============================================
-- Table: Reservations (Member 3)
-- Stores parking reservations
-- =============================================
CREATE TABLE Reservations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    SpotId UNIQUEIDENTIFIER NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CancellationDeadline DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Reservations_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Reservations_ParkingSpots FOREIGN KEY (SpotId)
        REFERENCES ParkingSpots(Id),
    CONSTRAINT CHK_Reservations_Status CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled', 'Completed')),
    CONSTRAINT CHK_Reservations_EndTime CHECK (EndTime IS NULL OR EndTime > StartTime)
);
GO

CREATE NONCLUSTERED INDEX IX_Reservations_UserId ON Reservations(UserId);
CREATE NONCLUSTERED INDEX IX_Reservations_SpotId ON Reservations(SpotId);
CREATE NONCLUSTERED INDEX IX_Reservations_Status ON Reservations(Status);
CREATE NONCLUSTERED INDEX IX_Reservations_CancellationDeadline ON Reservations(CancellationDeadline);
GO
PRINT '✓ Reservations table created';

-- =============================================
-- Table: Payments (Member 4)
-- Stores payment transactions
-- =============================================
CREATE TABLE Payments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReservationId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(10, 2) NOT NULL,
    PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Payments_Reservations FOREIGN KEY (ReservationId)
        REFERENCES Reservations(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT CHK_Payments_Status CHECK (PaymentStatus IN ('Pending', 'Completed', 'Failed'))
);
GO

CREATE NONCLUSTERED INDEX IX_Payments_ReservationId ON Payments(ReservationId);
CREATE NONCLUSTERED INDEX IX_Payments_PaymentStatus ON Payments(PaymentStatus);
GO
PRINT '✓ Payments table created';

-- =============================================
-- Table: Penalties (Member 3)
-- Stores penalty records
-- =============================================
CREATE TABLE Penalties (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReservationId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(10, 2) NOT NULL,
    Reason NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Penalties_Reservations FOREIGN KEY (ReservationId)
        REFERENCES Reservations(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_Penalties_Amount CHECK (Amount > 0),
    CONSTRAINT CHK_Penalties_Reason_Length CHECK (LEN(Reason) >= 5)
);
GO

CREATE NONCLUSTERED INDEX IX_Penalties_ReservationId ON Penalties(ReservationId);
GO
PRINT '✓ Penalties table created';

PRINT '========================================';
PRINT 'Database schema created successfully!';
PRINT 'Tables: Users, UserWallets, ParkingSpots, Reservations, Payments, Penalties';
PRINT '========================================';
GO
