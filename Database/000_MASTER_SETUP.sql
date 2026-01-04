-- =============================================
-- MASTER SETUP SCRIPT
-- Smart Parking System - Complete Database Setup
-- Run this script to set up the entire database
-- =============================================

PRINT '========================================';
PRINT 'Smart Parking System - Database Setup';
PRINT 'This will create the complete database with all tables, stored procedures, and test data';
PRINT '========================================';
PRINT '';

-- Step 1: Create Tables
PRINT 'Step 1/6: Creating database and tables...';
:r 001_CreateTables.sql
PRINT '';

-- Step 2: Create User & Wallet Stored Procedures (Member 1)
PRINT 'Step 2/6: Creating User & Wallet stored procedures...';
:r 002_UserWallet_StoredProcedures.sql
PRINT '';

-- Step 3: Create ParkingSpot Stored Procedures (Member 2)
PRINT 'Step 3/6: Creating ParkingSpot stored procedures...';
:r 003_ParkingSpot_StoredProcedures.sql
PRINT '';

-- Step 4: Create Reservation & Penalty Stored Procedures (Member 3)
PRINT 'Step 4/6: Creating Reservation & Penalty stored procedures...';
:r 004_Reservation_Penalty_StoredProcedures.sql
PRINT '';

-- Step 5: Create Payment Stored Procedures (Member 4)
PRINT 'Step 5/6: Creating Payment stored procedures...';
:r 005_Payment_StoredProcedures.sql
PRINT '';

-- Step 6: Seed Test Data
PRINT 'Step 6/6: Seeding test data...';
:r 006_SeedData.sql
PRINT '';

PRINT '========================================';
PRINT 'DATABASE SETUP COMPLETED SUCCESSFULLY!';
PRINT '';
PRINT 'Database: SmartParkingDB';
PRINT 'Tables: 6 (Users, UserWallets, ParkingSpots, Reservations, Payments, Penalties)';
PRINT 'Stored Procedures: 30+';
PRINT 'Test Data: 5 users, 10 parking spots, sample reservations';
PRINT '';
PRINT 'You can now start developing your C# application!';
PRINT '========================================';
GO
