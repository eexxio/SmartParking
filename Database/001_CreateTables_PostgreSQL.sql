-- =============================================
-- Smart Parking System - Complete Database Schema (PostgreSQL)
-- Team Project - All Members
-- =============================================

-- Drop database if exists (for clean setup)
DROP DATABASE IF EXISTS "SmartParkingDB";

-- Create database
CREATE DATABASE "SmartParkingDB"
  WITH ENCODING='UTF8'
  TEMPLATE=template0;

\c "SmartParkingDB";

RAISE NOTICE '========================================';
RAISE NOTICE 'Creating Tables...';
RAISE NOTICE '========================================';

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================
-- Table: Users (Member 1)
-- Stores user account information
-- =============================================
CREATE TABLE Users (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Email VARCHAR(255) NOT NULL UNIQUE,
    FullName VARCHAR(100) NOT NULL,
    IsEVUser BOOLEAN NOT NULL DEFAULT FALSE,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Validation constraints
    CONSTRAINT CHK_Users_Email_Length CHECK (LENGTH(Email) >= 5),
    CONSTRAINT CHK_Users_FullName_Length CHECK (LENGTH(FullName) >= 5),
    CONSTRAINT CHK_Users_Email_Format CHECK (Email ~ '.*@.*\..*')
);

CREATE INDEX IX_Users_Email ON Users(Email);
RAISE NOTICE '✓ Users table created';

-- =============================================
-- Table: UserWallets (Member 1)
-- Stores user balance (app coins)
-- =============================================
CREATE TABLE UserWallets (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId UUID NOT NULL UNIQUE,
    Balance NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_UserWallets_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_UserWallets_Balance CHECK (Balance >= 0)
);

CREATE INDEX IX_UserWallets_UserId ON UserWallets(UserId);
RAISE NOTICE '✓ UserWallets table created';

-- =============================================
-- Table: ParkingSpots (Member 2)
-- Stores parking spot information
-- =============================================
CREATE TABLE ParkingSpots (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    SpotNumber VARCHAR(10) NOT NULL UNIQUE,
    SpotType VARCHAR(20) NOT NULL,
    IsOccupied BOOLEAN NOT NULL DEFAULT FALSE,
    HourlyRate NUMERIC(10, 2) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT CHK_ParkingSpots_SpotNumber_Length CHECK (LENGTH(SpotNumber) >= 5),
    CONSTRAINT CHK_ParkingSpots_SpotType CHECK (SpotType IN ('Regular', 'EV')),
    CONSTRAINT CHK_ParkingSpots_HourlyRate CHECK (HourlyRate > 0)
);

CREATE INDEX IX_ParkingSpots_IsOccupied ON ParkingSpots(IsOccupied);
CREATE INDEX IX_ParkingSpots_SpotType ON ParkingSpots(SpotType);
RAISE NOTICE '✓ ParkingSpots table created';

-- =============================================
-- Table: Reservations (Member 3)
-- Stores parking reservations
-- =============================================
CREATE TABLE Reservations (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId UUID NOT NULL,
    SpotId UUID NOT NULL,
    StartTime TIMESTAMP NOT NULL,
    EndTime TIMESTAMP NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    CancellationDeadline TIMESTAMP NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_Reservations_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Reservations_ParkingSpots FOREIGN KEY (SpotId)
        REFERENCES ParkingSpots(Id),
    CONSTRAINT CHK_Reservations_Status CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled', 'Completed')),
    CONSTRAINT CHK_Reservations_EndTime CHECK (EndTime IS NULL OR EndTime > StartTime)
);

CREATE INDEX IX_Reservations_UserId ON Reservations(UserId);
CREATE INDEX IX_Reservations_SpotId ON Reservations(SpotId);
CREATE INDEX IX_Reservations_Status ON Reservations(Status);
CREATE INDEX IX_Reservations_CancellationDeadline ON Reservations(CancellationDeadline);
RAISE NOTICE '✓ Reservations table created';

-- =============================================
-- Table: Payments (Member 4)
-- Stores payment transactions
-- =============================================
CREATE TABLE Payments (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ReservationId UUID NOT NULL,
    Amount NUMERIC(10, 2) NOT NULL,
    PaymentStatus VARCHAR(20) NOT NULL DEFAULT 'Pending',
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_Payments_Reservations FOREIGN KEY (ReservationId)
        REFERENCES Reservations(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT CHK_Payments_Status CHECK (PaymentStatus IN ('Pending', 'Completed', 'Failed'))
);

CREATE INDEX IX_Payments_ReservationId ON Payments(ReservationId);
CREATE INDEX IX_Payments_PaymentStatus ON Payments(PaymentStatus);
RAISE NOTICE '✓ Payments table created';

-- =============================================
-- Table: Penalties (Member 3)
-- Stores penalty records
-- =============================================
CREATE TABLE Penalties (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ReservationId UUID NOT NULL,
    Amount NUMERIC(10, 2) NOT NULL,
    Reason VARCHAR(255) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_Penalties_Reservations FOREIGN KEY (ReservationId)
        REFERENCES Reservations(Id) ON DELETE CASCADE,
    CONSTRAINT CHK_Penalties_Amount CHECK (Amount > 0),
    CONSTRAINT CHK_Penalties_Reason_Length CHECK (LENGTH(Reason) >= 5)
);

CREATE INDEX IX_Penalties_ReservationId ON Penalties(ReservationId);
RAISE NOTICE '✓ Penalties table created';

RAISE NOTICE '========================================';
RAISE NOTICE 'Database schema created successfully!';
RAISE NOTICE 'Tables: Users, UserWallets, ParkingSpots, Reservations, Payments, Penalties';
RAISE NOTICE '========================================';
