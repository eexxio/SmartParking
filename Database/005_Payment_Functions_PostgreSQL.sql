-- =============================================
-- MEMBER 4: Payment Functions (PostgreSQL)
-- =============================================

RAISE NOTICE '========================================';
RAISE NOTICE 'Creating Payment Functions (Member 4)...';
RAISE NOTICE '========================================';

-- =============================================
-- sp_create_payment
-- Creates a new payment record
-- =============================================
CREATE OR REPLACE FUNCTION sp_create_payment(
    p_reservation_id UUID,
    p_amount NUMERIC(10,2),
    OUT payment_id UUID
) AS $$
BEGIN
    -- Validate amount
    IF p_amount <= 0 THEN
        RAISE EXCEPTION 'Payment amount must be greater than 0' USING ERRCODE = '45301';
    END IF;

    -- Check if reservation exists
    IF NOT EXISTS (SELECT 1 FROM Reservations WHERE Id = p_reservation_id) THEN
        RAISE EXCEPTION 'Reservation not found' USING ERRCODE = '45302';
    END IF;

    -- Check if payment already exists for this reservation
    IF EXISTS (SELECT 1 FROM Payments WHERE ReservationId = p_reservation_id AND PaymentStatus = 'Completed') THEN
        RAISE EXCEPTION 'Payment already completed for this reservation' USING ERRCODE = '45303';
    END IF;

    -- Create payment
    payment_id := gen_random_uuid();
    INSERT INTO Payments (Id, ReservationId, Amount, PaymentStatus, CreatedAt)
    VALUES (payment_id, p_reservation_id, p_amount, 'Pending', CURRENT_TIMESTAMP);
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_create_payment created';

-- =============================================
-- sp_get_payment_by_id
-- Retrieves payment by ID
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_payment_by_id(
    p_payment_id UUID
) RETURNS TABLE (
    id UUID,
    reservation_id UUID,
    amount NUMERIC(10,2),
    payment_status VARCHAR,
    created_at TIMESTAMP,
    user_id UUID,
    spot_id UUID,
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    user_email VARCHAR,
    user_name VARCHAR,
    spot_number VARCHAR,
    hourly_rate NUMERIC(10,2)
) AS $$
BEGIN
    RETURN QUERY
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
        u.Email AS user_email,
        u.FullName AS user_name,
        ps.SpotNumber AS spot_number,
        ps.HourlyRate AS hourly_rate
    FROM Payments p
    INNER JOIN Reservations r ON p.ReservationId = r.Id
    INNER JOIN Users u ON r.UserId = u.Id
    INNER JOIN ParkingSpots ps ON r.SpotId = ps.Id
    WHERE p.Id = p_payment_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_payment_by_id created';

-- =============================================
-- sp_get_payment_by_reservation_id
-- Retrieves payment by reservation ID
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_payment_by_reservation_id(
    p_reservation_id UUID
) RETURNS TABLE (
    id UUID,
    reservation_id UUID,
    amount NUMERIC(10,2),
    payment_status VARCHAR,
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT Id, ReservationId, Amount, PaymentStatus, CreatedAt
    FROM Payments
    WHERE ReservationId = p_reservation_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_payment_by_reservation_id created';

-- =============================================
-- sp_get_payments_by_user_id
-- Retrieves all payments for a user
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_payments_by_user_id(
    p_user_id UUID
) RETURNS TABLE (
    id UUID,
    reservation_id UUID,
    amount NUMERIC(10,2),
    payment_status VARCHAR,
    created_at TIMESTAMP,
    spot_id UUID,
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    spot_number VARCHAR,
    hourly_rate NUMERIC(10,2)
) AS $$
BEGIN
    RETURN QUERY
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
    WHERE r.UserId = p_user_id
    ORDER BY p.CreatedAt DESC;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_payments_by_user_id created';

-- =============================================
-- sp_update_payment_status
-- Updates payment status
-- =============================================
CREATE OR REPLACE FUNCTION sp_update_payment_status(
    p_payment_id UUID,
    p_payment_status VARCHAR
) RETURNS VOID AS $$
BEGIN
    -- Validate payment status
    IF p_payment_status NOT IN ('Pending', 'Completed', 'Failed') THEN
        RAISE EXCEPTION 'Invalid payment status' USING ERRCODE = '45304';
    END IF;

    -- Check if payment exists
    IF NOT EXISTS (SELECT 1 FROM Payments WHERE Id = p_payment_id) THEN
        RAISE EXCEPTION 'Payment not found' USING ERRCODE = '45305';
    END IF;

    -- Update status
    UPDATE Payments
    SET PaymentStatus = p_payment_status
    WHERE Id = p_payment_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_update_payment_status created';

-- =============================================
-- sp_calculate_payment_amount
-- Calculates payment amount based on reservation duration
-- =============================================
CREATE OR REPLACE FUNCTION sp_calculate_payment_amount(
    p_reservation_id UUID,
    OUT amount NUMERIC(10,2)
) AS $$
DECLARE
    v_start_time TIMESTAMP;
    v_end_time TIMESTAMP;
    v_hourly_rate NUMERIC(10,2);
    v_duration_hours NUMERIC(10,2);
BEGIN
    -- Get reservation details
    SELECT r.StartTime, r.EndTime, ps.HourlyRate
    INTO v_start_time, v_end_time, v_hourly_rate
    FROM Reservations r
    INNER JOIN ParkingSpots ps ON r.SpotId = ps.Id
    WHERE r.Id = p_reservation_id;

    -- Check if reservation exists
    IF v_start_time IS NULL THEN
        RAISE EXCEPTION 'Reservation not found' USING ERRCODE = '45302';
    END IF;

    -- Check if reservation has ended
    IF v_end_time IS NULL THEN
        RAISE EXCEPTION 'Reservation has not ended yet' USING ERRCODE = '45306';
    END IF;

    -- Calculate duration in hours (convert minutes to hours)
    v_duration_hours := EXTRACT(EPOCH FROM (v_end_time - v_start_time)) / 3600.0;

    -- Calculate amount (rounded to 2 decimal places)
    amount := ROUND(v_hourly_rate * v_duration_hours, 2);

    -- Ensure minimum charge (at least the hourly rate)
    IF amount < v_hourly_rate THEN
        amount := v_hourly_rate;
    END IF;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_calculate_payment_amount created';

-- =============================================
-- sp_process_payment
-- Processes a payment (creates payment and deducts from wallet)
-- =============================================
CREATE OR REPLACE FUNCTION sp_process_payment(
    p_reservation_id UUID,
    OUT payment_id UUID
) AS $$
DECLARE
    v_amount NUMERIC(10,2);
    v_user_id UUID;
    v_current_balance NUMERIC(10,2);
BEGIN
    -- Calculate payment amount
    SELECT sp_calculate_payment_amount(p_reservation_id) INTO v_amount;

    -- Get user ID from reservation
    SELECT UserId INTO v_user_id
    FROM Reservations
    WHERE Id = p_reservation_id;

    -- Get current balance
    SELECT Balance INTO v_current_balance
    FROM UserWallets
    WHERE UserId = v_user_id;

    IF v_current_balance IS NULL THEN
        RAISE EXCEPTION 'User wallet not found' USING ERRCODE = '45307';
    END IF;

    -- Check sufficient balance
    IF v_current_balance < v_amount THEN
        RAISE EXCEPTION 'Insufficient balance' USING ERRCODE = '45308';
    END IF;

    -- Create payment record
    SELECT sp_create_payment(p_reservation_id, v_amount) INTO payment_id;

    -- Deduct from wallet
    PERFORM sp_deduct_from_wallet(v_user_id, v_amount);

    -- Update payment status to Completed
    UPDATE Payments
    SET PaymentStatus = 'Completed'
    WHERE Id = payment_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_process_payment created';

-- =============================================
-- sp_get_payment_statistics
-- Returns payment statistics
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_payment_statistics()
RETURNS TABLE (
    total_payments BIGINT,
    completed_payments BIGINT,
    failed_payments BIGINT,
    pending_payments BIGINT,
    total_revenue NUMERIC(10,2),
    average_payment NUMERIC(10,2)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        COUNT(*) AS total_payments,
        SUM(CASE WHEN PaymentStatus = 'Completed' THEN 1 ELSE 0 END) AS completed_payments,
        SUM(CASE WHEN PaymentStatus = 'Failed' THEN 1 ELSE 0 END) AS failed_payments,
        SUM(CASE WHEN PaymentStatus = 'Pending' THEN 1 ELSE 0 END) AS pending_payments,
        COALESCE(SUM(CASE WHEN PaymentStatus = 'Completed' THEN Amount ELSE 0 END), 0) AS total_revenue,
        AVG(CASE WHEN PaymentStatus = 'Completed' THEN Amount ELSE NULL END) AS average_payment
    FROM Payments;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_payment_statistics created';

RAISE NOTICE '========================================';
RAISE NOTICE 'Member 4 Functions Created Successfully!';
RAISE NOTICE '========================================';
