-- =============================================
-- Seed Data Script (PostgreSQL)
-- Populates database with test data for all members
-- =============================================

RAISE NOTICE '========================================';
RAISE NOTICE 'Seeding Database with Test Data...';
RAISE NOTICE '========================================';

-- =============================================
-- Seed Users & Wallets (Member 1)
-- =============================================

-- Create test users (using SELECT INTO to get the returned IDs)
SELECT sp_create_user('john.doe@email.com', 'John Doe', FALSE, 150.00) AS user1_id \gset
SELECT sp_create_user('jane.smith@email.com', 'Jane Smith', TRUE, 200.00) AS user2_id \gset
SELECT sp_create_user('michael.johnson@email.com', 'Michael Johnson', TRUE, 100.00) AS user3_id \gset
SELECT sp_create_user('sarah.williams@email.com', 'Sarah Williams', FALSE, 175.00) AS user4_id \gset
SELECT sp_create_user('david.brown@email.com', 'David Brown', FALSE, 125.00) AS user5_id \gset

RAISE NOTICE '✓ Created 5 test users (2 EV users, 3 regular users)';

-- =============================================
-- Seed Parking Spots (Member 2)
-- =============================================

-- Create parking spots (7 Regular, 3 EV)
SELECT sp_create_parking_spot('A-001-REG', 'Regular', 5.00) AS spot1_id \gset
SELECT sp_create_parking_spot('A-002-REG', 'Regular', 5.00) AS spot2_id \gset
SELECT sp_create_parking_spot('A-003-REG', 'Regular', 5.00) AS spot3_id \gset
SELECT sp_create_parking_spot('B-001-REG', 'Regular', 7.50) AS spot4_id \gset
SELECT sp_create_parking_spot('B-002-REG', 'Regular', 7.50) AS spot5_id \gset
SELECT sp_create_parking_spot('B-003-REG', 'Regular', 7.50) AS spot6_id \gset
SELECT sp_create_parking_spot('C-001-REG', 'Regular', 10.00) AS spot7_id \gset
SELECT sp_create_parking_spot('EV-01-CHG', 'EV', 12.00) AS spot8_id \gset
SELECT sp_create_parking_spot('EV-02-CHG', 'EV', 12.00) AS spot9_id \gset
SELECT sp_create_parking_spot('EV-03-CHG', 'EV', 12.00) AS spot10_id \gset

RAISE NOTICE '✓ Created 10 parking spots (7 Regular, 3 EV)';

-- =============================================
-- Seed Sample Reservations (Member 3)
-- =============================================

-- Use the :variable_name syntax in psql for variables
-- Note: For actual execution, you'll need to run these commands in psql

-- Create a completed reservation for User1
-- \set user1_id (SELECT sp_create_user('john.doe@email.com', 'John Doe', FALSE, 150.00))
-- But since we're using SQL functions, let's create reservations differently

DO $$
DECLARE
    v_user1_id UUID;
    v_user2_id UUID;
    v_spot1_id UUID;
    v_spot8_id UUID;
    v_reservation1_id UUID;
    v_reservation2_id UUID;
BEGIN
    -- Get user IDs
    SELECT Id INTO v_user1_id FROM Users WHERE Email = 'john.doe@email.com';
    SELECT Id INTO v_user2_id FROM Users WHERE Email = 'jane.smith@email.com';

    -- Get spot IDs
    SELECT Id INTO v_spot1_id FROM ParkingSpots WHERE SpotNumber = 'A-001-REG';
    SELECT Id INTO v_spot8_id FROM ParkingSpots WHERE SpotNumber = 'EV-01-CHG';

    -- Create a completed reservation for User1
    PERFORM sp_create_reservation(v_user1_id, v_spot1_id, 15);

    -- Get the reservation we just created
    SELECT Id INTO v_reservation1_id
    FROM Reservations
    WHERE UserId = v_user1_id AND SpotId = v_spot1_id
    ORDER BY CreatedAt DESC LIMIT 1;

    -- Confirm it
    PERFORM sp_confirm_reservation(v_reservation1_id);

    -- Simulate completion after 2 hours
    UPDATE Reservations
    SET EndTime = StartTime + INTERVAL '2 hours',
        Status = 'Completed'
    WHERE Id = v_reservation1_id;

    -- Free the spot
    UPDATE ParkingSpots SET IsOccupied = FALSE WHERE Id = v_spot1_id;

    RAISE NOTICE '✓ Created sample completed reservation';

    -- Create a pending reservation for User2 (EV user)
    PERFORM sp_create_reservation(v_user2_id, v_spot8_id, 15);

    RAISE NOTICE '✓ Created sample pending reservation (EV spot)';

    -- Process payment for completed reservation
    PERFORM sp_process_payment(v_reservation1_id);

    RAISE NOTICE '✓ Created sample payment for completed reservation';
END $$;

-- =============================================
-- Display Seed Data Summary
-- =============================================

RAISE NOTICE '';
RAISE NOTICE '========================================';
RAISE NOTICE 'Seed Data Summary:';
RAISE NOTICE '========================================';

-- Users summary
SELECT
    COUNT(*) AS "TotalUsers",
    SUM(CASE WHEN IsEVUser = TRUE THEN 1 ELSE 0 END) AS "EVUsers",
    SUM(CASE WHEN IsEVUser = FALSE THEN 1 ELSE 0 END) AS "RegularUsers"
FROM Users;

-- Wallets summary
SELECT
    COUNT(*) AS "TotalWallets",
    SUM(Balance) AS "TotalBalance",
    AVG(Balance) AS "AverageBalance",
    MIN(Balance) AS "MinBalance",
    MAX(Balance) AS "MaxBalance"
FROM UserWallets;

-- Parking spots summary
SELECT
    COUNT(*) AS "TotalSpots",
    SUM(CASE WHEN SpotType = 'EV' THEN 1 ELSE 0 END) AS "EVSpots",
    SUM(CASE WHEN SpotType = 'Regular' THEN 1 ELSE 0 END) AS "RegularSpots",
    SUM(CASE WHEN IsOccupied = TRUE THEN 1 ELSE 0 END) AS "OccupiedSpots",
    SUM(CASE WHEN IsOccupied = FALSE THEN 1 ELSE 0 END) AS "AvailableSpots"
FROM ParkingSpots;

-- Reservations summary
SELECT
    COUNT(*) AS "TotalReservations",
    SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS "PendingReservations",
    SUM(CASE WHEN Status = 'Confirmed' THEN 1 ELSE 0 END) AS "ConfirmedReservations",
    SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS "CompletedReservations",
    SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END) AS "CancelledReservations"
FROM Reservations;

-- Payments summary
SELECT
    COUNT(*) AS "TotalPayments",
    SUM(CASE WHEN PaymentStatus = 'Completed' THEN 1 ELSE 0 END) AS "CompletedPayments",
    COALESCE(SUM(CASE WHEN PaymentStatus = 'Completed' THEN Amount ELSE 0 END), 0) AS "TotalRevenue"
FROM Payments;

RAISE NOTICE '';
RAISE NOTICE '========================================';
RAISE NOTICE 'Database seeding completed successfully!';
RAISE NOTICE 'You can now start testing all modules.';
RAISE NOTICE '========================================';
