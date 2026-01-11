-- =============================================
-- MEMBER 2: Parking Spot Functions (PostgreSQL)
-- =============================================

RAISE NOTICE '========================================';
RAISE NOTICE 'Creating Parking Spot Functions (Member 2)...';
RAISE NOTICE '========================================';

-- =============================================
-- sp_create_parking_spot
-- Creates a new parking spot
-- =============================================
CREATE OR REPLACE FUNCTION sp_create_parking_spot(
    p_spot_number VARCHAR(10),
    p_spot_type VARCHAR(20),
    p_hourly_rate NUMERIC(10,2),
    OUT spot_id UUID
) AS $$
BEGIN
    -- Validate input
    IF LENGTH(p_spot_number) < 5 THEN
        RAISE EXCEPTION 'Spot number must be at least 5 characters long' USING ERRCODE = '45101';
    END IF;

    IF p_spot_type NOT IN ('Regular', 'EV') THEN
        RAISE EXCEPTION 'Spot type must be Regular or EV' USING ERRCODE = '45102';
    END IF;

    IF p_hourly_rate <= 0 THEN
        RAISE EXCEPTION 'Hourly rate must be greater than 0' USING ERRCODE = '45103';
    END IF;

    -- Check if spot number already exists
    IF EXISTS (SELECT 1 FROM ParkingSpots WHERE SpotNumber = p_spot_number) THEN
        RAISE EXCEPTION 'Spot number already exists' USING ERRCODE = '45104';
    END IF;

    -- Create parking spot
    spot_id := gen_random_uuid();
    INSERT INTO ParkingSpots (Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt)
    VALUES (spot_id, p_spot_number, p_spot_type, FALSE, p_hourly_rate, CURRENT_TIMESTAMP);
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_create_parking_spot created';

-- =============================================
-- sp_get_parking_spot_by_id
-- Retrieves parking spot by ID
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_parking_spot_by_id(
    p_spot_id UUID
) RETURNS TABLE (
    id UUID,
    spot_number VARCHAR,
    spot_type VARCHAR,
    is_occupied BOOLEAN,
    hourly_rate NUMERIC(10,2),
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt
    FROM ParkingSpots
    WHERE Id = p_spot_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_parking_spot_by_id created';

-- =============================================
-- sp_get_all_parking_spots
-- Retrieves all parking spots
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_all_parking_spots()
RETURNS TABLE (
    id UUID,
    spot_number VARCHAR,
    spot_type VARCHAR,
    is_occupied BOOLEAN,
    hourly_rate NUMERIC(10,2),
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt
    FROM ParkingSpots
    ORDER BY SpotNumber;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_all_parking_spots created';

-- =============================================
-- sp_get_available_parking_spots
-- Retrieves all available (unoccupied) parking spots
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_available_parking_spots()
RETURNS TABLE (
    id UUID,
    spot_number VARCHAR,
    spot_type VARCHAR,
    is_occupied BOOLEAN,
    hourly_rate NUMERIC(10,2),
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt
    FROM ParkingSpots
    WHERE IsOccupied = FALSE
    ORDER BY SpotType, SpotNumber;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_available_parking_spots created';

-- =============================================
-- sp_get_available_spots_by_type
-- Retrieves available parking spots by type
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_available_spots_by_type(
    p_spot_type VARCHAR
) RETURNS TABLE (
    id UUID,
    spot_number VARCHAR,
    spot_type VARCHAR,
    is_occupied BOOLEAN,
    hourly_rate NUMERIC(10,2),
    created_at TIMESTAMP
) AS $$
BEGIN
    -- Validate spot type
    IF p_spot_type NOT IN ('Regular', 'EV') THEN
        RAISE EXCEPTION 'Spot type must be Regular or EV' USING ERRCODE = '45102';
    END IF;

    RETURN QUERY
    SELECT Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt
    FROM ParkingSpots
    WHERE IsOccupied = FALSE AND SpotType = p_spot_type
    ORDER BY SpotNumber;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_available_spots_by_type created';

-- =============================================
-- sp_update_spot_occupancy
-- Updates the occupancy status of a parking spot
-- =============================================
CREATE OR REPLACE FUNCTION sp_update_spot_occupancy(
    p_spot_id UUID,
    p_is_occupied BOOLEAN
) RETURNS VOID AS $$
BEGIN
    -- Check if spot exists
    IF NOT EXISTS (SELECT 1 FROM ParkingSpots WHERE Id = p_spot_id) THEN
        RAISE EXCEPTION 'Parking spot not found' USING ERRCODE = '45105';
    END IF;

    -- Update occupancy
    UPDATE ParkingSpots
    SET IsOccupied = p_is_occupied
    WHERE Id = p_spot_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_update_spot_occupancy created';

-- =============================================
-- sp_validate_spot_for_user
-- Validates if a user can reserve a specific spot type
-- Returns 1 if valid, throws error if invalid
-- =============================================
CREATE OR REPLACE FUNCTION sp_validate_spot_for_user(
    p_spot_id UUID,
    p_is_ev_user BOOLEAN
) RETURNS TABLE (
    is_valid INTEGER
) AS $$
DECLARE
    v_spot_type VARCHAR;
    v_is_occupied BOOLEAN;
BEGIN
    -- Get spot details
    SELECT SpotType, IsOccupied INTO v_spot_type, v_is_occupied
    FROM ParkingSpots
    WHERE Id = p_spot_id;

    -- Check if spot exists
    IF v_spot_type IS NULL THEN
        RAISE EXCEPTION 'Parking spot not found' USING ERRCODE = '45105';
    END IF;

    -- Check if spot is available
    IF v_is_occupied = TRUE THEN
        RAISE EXCEPTION 'Parking spot is already occupied' USING ERRCODE = '45106';
    END IF;

    -- Validation logic:
    -- EV users can use both Regular and EV spots
    -- Non-EV users can ONLY use Regular spots
    IF v_spot_type = 'EV' AND p_is_ev_user = FALSE THEN
        RAISE EXCEPTION 'Non-EV users cannot reserve EV spots' USING ERRCODE = '45107';
    END IF;

    -- Return success (1 means valid)
    RETURN QUERY SELECT 1::INTEGER;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_validate_spot_for_user created';

-- =============================================
-- sp_get_spot_statistics
-- Returns statistics about parking spots
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_spot_statistics()
RETURNS TABLE (
    total_spots BIGINT,
    occupied_spots BIGINT,
    available_spots BIGINT,
    total_ev_spots BIGINT,
    available_ev_spots BIGINT,
    total_regular_spots BIGINT,
    available_regular_spots BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        COUNT(*) AS total_spots,
        SUM(CASE WHEN IsOccupied = TRUE THEN 1 ELSE 0 END) AS occupied_spots,
        SUM(CASE WHEN IsOccupied = FALSE THEN 1 ELSE 0 END) AS available_spots,
        SUM(CASE WHEN SpotType = 'EV' THEN 1 ELSE 0 END) AS total_ev_spots,
        SUM(CASE WHEN SpotType = 'EV' AND IsOccupied = FALSE THEN 1 ELSE 0 END) AS available_ev_spots,
        SUM(CASE WHEN SpotType = 'Regular' THEN 1 ELSE 0 END) AS total_regular_spots,
        SUM(CASE WHEN SpotType = 'Regular' AND IsOccupied = FALSE THEN 1 ELSE 0 END) AS available_regular_spots
    FROM ParkingSpots;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_spot_statistics created';

RAISE NOTICE '========================================';
RAISE NOTICE 'Member 2 Functions Created Successfully!';
RAISE NOTICE '========================================';
