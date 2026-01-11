-- =============================================
-- MEMBER 3: Reservation & Penalty Functions (PostgreSQL)
-- =============================================

RAISE NOTICE '========================================';
RAISE NOTICE 'Creating Reservation & Penalty Functions (Member 3)...';
RAISE NOTICE '========================================';

-- =============================================
-- sp_create_reservation
-- Creates a new reservation
-- =============================================
CREATE OR REPLACE FUNCTION sp_create_reservation(
    p_user_id UUID,
    p_spot_id UUID,
    p_cancellation_timeout_minutes INTEGER DEFAULT 15,
    OUT reservation_id UUID
) AS $$
BEGIN
    -- Check if user exists
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = p_user_id) THEN
        RAISE EXCEPTION 'User not found' USING ERRCODE = '45201';
    END IF;

    -- Check if spot exists
    IF NOT EXISTS (SELECT 1 FROM ParkingSpots WHERE Id = p_spot_id) THEN
        RAISE EXCEPTION 'Parking spot not found' USING ERRCODE = '45202';
    END IF;

    -- Check if spot is available
    IF EXISTS (SELECT 1 FROM ParkingSpots WHERE Id = p_spot_id AND IsOccupied = TRUE) THEN
        RAISE EXCEPTION 'Parking spot is already occupied' USING ERRCODE = '45203';
    END IF;

    -- Create reservation
    reservation_id := gen_random_uuid();
    INSERT INTO Reservations (Id, UserId, SpotId, StartTime, EndTime, Status, CancellationDeadline, CreatedAt)
    VALUES (
        reservation_id,
        p_user_id,
        p_spot_id,
        CURRENT_TIMESTAMP,
        NULL,
        'Pending',
        CURRENT_TIMESTAMP + (p_cancellation_timeout_minutes || ' minutes')::INTERVAL,
        CURRENT_TIMESTAMP
    );

    -- Mark spot as occupied
    UPDATE ParkingSpots
    SET IsOccupied = TRUE
    WHERE Id = p_spot_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_create_reservation created';

-- =============================================
-- sp_get_reservation_by_id
-- Retrieves reservation by ID
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_reservation_by_id(
    p_reservation_id UUID
) RETURNS TABLE (
    id UUID,
    user_id UUID,
    spot_id UUID,
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    status VARCHAR,
    cancellation_deadline TIMESTAMP,
    created_at TIMESTAMP,
    user_email VARCHAR,
    user_name VARCHAR,
    is_ev_user BOOLEAN,
    spot_number VARCHAR,
    spot_type VARCHAR,
    hourly_rate NUMERIC(10,2)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        r.Id,
        r.UserId,
        r.SpotId,
        r.StartTime,
        r.EndTime,
        r.Status,
        r.CancellationDeadline,
        r.CreatedAt,
        u.Email AS user_email,
        u.FullName AS user_name,
        u.IsEVUser AS is_ev_user,
        ps.SpotNumber AS spot_number,
        ps.SpotType AS spot_type,
        ps.HourlyRate AS hourly_rate
    FROM Reservations r
    INNER JOIN Users u ON r.UserId = u.Id
    INNER JOIN ParkingSpots ps ON r.SpotId = ps.Id
    WHERE r.Id = p_reservation_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_reservation_by_id created';

-- =============================================
-- sp_get_reservations_by_user_id
-- Retrieves all reservations for a user
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_reservations_by_user_id(
    p_user_id UUID
) RETURNS TABLE (
    id UUID,
    user_id UUID,
    spot_id UUID,
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    status VARCHAR,
    cancellation_deadline TIMESTAMP,
    created_at TIMESTAMP,
    spot_number VARCHAR,
    spot_type VARCHAR,
    hourly_rate NUMERIC(10,2)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        r.Id,
        r.UserId,
        r.SpotId,
        r.StartTime,
        r.EndTime,
        r.Status,
        r.CancellationDeadline,
        r.CreatedAt,
        ps.SpotNumber AS spot_number,
        ps.SpotType AS spot_type,
        ps.HourlyRate AS hourly_rate
    FROM Reservations r
    INNER JOIN ParkingSpots ps ON r.SpotId = ps.Id
    WHERE r.UserId = p_user_id
    ORDER BY r.CreatedAt DESC;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_reservations_by_user_id created';

-- =============================================
-- sp_confirm_reservation
-- Confirms a pending reservation
-- =============================================
CREATE OR REPLACE FUNCTION sp_confirm_reservation(
    p_reservation_id UUID
) RETURNS VOID AS $$
DECLARE
    v_current_status VARCHAR;
BEGIN
    -- Get current status
    SELECT Status INTO v_current_status
    FROM Reservations
    WHERE Id = p_reservation_id;

    -- Check if reservation exists
    IF v_current_status IS NULL THEN
        RAISE EXCEPTION 'Reservation not found' USING ERRCODE = '45204';
    END IF;

    -- Check if reservation is pending
    IF v_current_status != 'Pending' THEN
        RAISE EXCEPTION 'Only pending reservations can be confirmed' USING ERRCODE = '45205';
    END IF;

    -- Update status to Confirmed
    UPDATE Reservations
    SET Status = 'Confirmed'
    WHERE Id = p_reservation_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_confirm_reservation created';

-- =============================================
-- sp_cancel_reservation
-- Cancels a reservation
-- =============================================
CREATE OR REPLACE FUNCTION sp_cancel_reservation(
    p_reservation_id UUID,
    OUT is_late BOOLEAN
) AS $$
DECLARE
    v_current_status VARCHAR;
    v_spot_id UUID;
    v_cancellation_deadline TIMESTAMP;
BEGIN
    -- Get reservation details
    SELECT Status, SpotId, CancellationDeadline INTO v_current_status, v_spot_id, v_cancellation_deadline
    FROM Reservations
    WHERE Id = p_reservation_id;

    -- Check if reservation exists
    IF v_current_status IS NULL THEN
        RAISE EXCEPTION 'Reservation not found' USING ERRCODE = '45204';
    END IF;

    -- Check if reservation can be cancelled
    IF v_current_status IN ('Cancelled', 'Completed') THEN
        RAISE EXCEPTION 'Reservation is already cancelled or completed' USING ERRCODE = '45206';
    END IF;

    -- Check if cancellation is late
    is_late := CASE WHEN CURRENT_TIMESTAMP > v_cancellation_deadline THEN TRUE ELSE FALSE END;

    -- Update reservation status
    UPDATE Reservations
    SET Status = 'Cancelled'
    WHERE Id = p_reservation_id;

    -- Free the parking spot
    UPDATE ParkingSpots
    SET IsOccupied = FALSE
    WHERE Id = v_spot_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_cancel_reservation created';

-- =============================================
-- sp_complete_reservation
-- Completes a reservation (user finished parking)
-- =============================================
CREATE OR REPLACE FUNCTION sp_complete_reservation(
    p_reservation_id UUID
) RETURNS VOID AS $$
DECLARE
    v_current_status VARCHAR;
    v_spot_id UUID;
BEGIN
    -- Get reservation details
    SELECT Status, SpotId INTO v_current_status, v_spot_id
    FROM Reservations
    WHERE Id = p_reservation_id;

    -- Check if reservation exists
    IF v_current_status IS NULL THEN
        RAISE EXCEPTION 'Reservation not found' USING ERRCODE = '45204';
    END IF;

    -- Check if reservation can be completed
    IF v_current_status NOT IN ('Pending', 'Confirmed') THEN
        RAISE EXCEPTION 'Only pending or confirmed reservations can be completed' USING ERRCODE = '45207';
    END IF;

    -- Update reservation
    UPDATE Reservations
    SET Status = 'Completed',
        EndTime = CURRENT_TIMESTAMP
    WHERE Id = p_reservation_id;

    -- Free the parking spot
    UPDATE ParkingSpots
    SET IsOccupied = FALSE
    WHERE Id = v_spot_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_complete_reservation created';

-- =============================================
-- sp_get_expired_pending_reservations
-- Gets all pending reservations past their cancellation deadline
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_expired_pending_reservations()
RETURNS TABLE (
    id UUID,
    user_id UUID,
    spot_id UUID,
    start_time TIMESTAMP,
    status VARCHAR,
    cancellation_deadline TIMESTAMP,
    email VARCHAR,
    spot_number VARCHAR
) AS $$
BEGIN
    RETURN QUERY
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
      AND r.CancellationDeadline < CURRENT_TIMESTAMP;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_expired_pending_reservations created';

-- =============================================
-- sp_create_penalty
-- Creates a penalty record
-- =============================================
CREATE OR REPLACE FUNCTION sp_create_penalty(
    p_reservation_id UUID,
    p_amount NUMERIC(10,2),
    p_reason VARCHAR,
    OUT penalty_id UUID
) AS $$
BEGIN
    -- Validate input
    IF p_amount <= 0 THEN
        RAISE EXCEPTION 'Penalty amount must be greater than 0' USING ERRCODE = '45208';
    END IF;

    IF LENGTH(p_reason) < 5 THEN
        RAISE EXCEPTION 'Penalty reason must be at least 5 characters long' USING ERRCODE = '45209';
    END IF;

    -- Check if reservation exists
    IF NOT EXISTS (SELECT 1 FROM Reservations WHERE Id = p_reservation_id) THEN
        RAISE EXCEPTION 'Reservation not found' USING ERRCODE = '45204';
    END IF;

    -- Create penalty
    penalty_id := gen_random_uuid();
    INSERT INTO Penalties (Id, ReservationId, Amount, Reason, CreatedAt)
    VALUES (penalty_id, p_reservation_id, p_amount, p_reason, CURRENT_TIMESTAMP);
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_create_penalty created';

-- =============================================
-- sp_get_penalties_by_reservation_id
-- Retrieves all penalties for a reservation
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_penalties_by_reservation_id(
    p_reservation_id UUID
) RETURNS TABLE (
    id UUID,
    reservation_id UUID,
    amount NUMERIC(10,2),
    reason VARCHAR,
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT Id, ReservationId, Amount, Reason, CreatedAt
    FROM Penalties
    WHERE ReservationId = p_reservation_id
    ORDER BY CreatedAt DESC;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_penalties_by_reservation_id created';

-- =============================================
-- sp_get_penalties_by_user_id
-- Retrieves all penalties for a user
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_penalties_by_user_id(
    p_user_id UUID
) RETURNS TABLE (
    id UUID,
    reservation_id UUID,
    amount NUMERIC(10,2),
    reason VARCHAR,
    created_at TIMESTAMP,
    spot_id UUID,
    spot_number VARCHAR
) AS $$
BEGIN
    RETURN QUERY
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
    WHERE r.UserId = p_user_id
    ORDER BY p.CreatedAt DESC;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_penalties_by_user_id created';

RAISE NOTICE '========================================';
RAISE NOTICE 'Member 3 Functions Created Successfully!';
RAISE NOTICE '========================================';
