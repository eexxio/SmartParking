-- =============================================
-- MEMBER 1: User & Wallet Functions (PostgreSQL)
-- =============================================

RAISE NOTICE '========================================';
RAISE NOTICE 'Creating User & Wallet Functions (Member 1)...';
RAISE NOTICE '========================================';

-- =============================================
-- sp_create_user
-- Creates a new user and automatically creates their wallet
-- =============================================
CREATE OR REPLACE FUNCTION sp_create_user(
    p_email VARCHAR(255),
    p_full_name VARCHAR(100),
    p_is_ev_user BOOLEAN DEFAULT FALSE,
    p_initial_balance NUMERIC(10,2) DEFAULT 100.00,
    OUT user_id UUID
) AS $$
DECLARE
    v_wallet_id UUID;
BEGIN
    -- Validate input
    IF LENGTH(p_email) < 5 THEN
        RAISE EXCEPTION 'Email must be at least 5 characters long' USING ERRCODE = '45001';
    END IF;

    IF LENGTH(p_full_name) < 5 THEN
        RAISE EXCEPTION 'Full name must be at least 5 characters long' USING ERRCODE = '45002';
    END IF;

    IF p_email !~ '.*@.*\..*' THEN
        RAISE EXCEPTION 'Email format is invalid' USING ERRCODE = '45003';
    END IF;

    -- Check if email already exists
    IF EXISTS (SELECT 1 FROM Users WHERE Email = p_email) THEN
        RAISE EXCEPTION 'Email already exists' USING ERRCODE = '45004';
    END IF;

    -- Create user
    user_id := gen_random_uuid();
    INSERT INTO Users (Id, Email, FullName, IsEVUser, IsActive, CreatedAt)
    VALUES (user_id, p_email, p_full_name, p_is_ev_user, TRUE, CURRENT_TIMESTAMP);

    -- Create wallet automatically
    v_wallet_id := gen_random_uuid();
    INSERT INTO UserWallets (Id, UserId, Balance, UpdatedAt)
    VALUES (v_wallet_id, user_id, p_initial_balance, CURRENT_TIMESTAMP);

EXCEPTION
    WHEN OTHERS THEN
        RAISE;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_create_user created';

-- =============================================
-- sp_get_user_by_id
-- Retrieves user by ID
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_user_by_id(
    p_user_id UUID
) RETURNS TABLE (
    id UUID,
    email VARCHAR,
    full_name VARCHAR,
    is_ev_user BOOLEAN,
    is_active BOOLEAN,
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT Id, Email, FullName, IsEVUser, IsActive, CreatedAt
    FROM Users
    WHERE Id = p_user_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_user_by_id created';

-- =============================================
-- sp_get_user_by_email
-- Retrieves user by email
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_user_by_email(
    p_email VARCHAR
) RETURNS TABLE (
    id UUID,
    email VARCHAR,
    full_name VARCHAR,
    is_ev_user BOOLEAN,
    is_active BOOLEAN,
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT Id, Email, FullName, IsEVUser, IsActive, CreatedAt
    FROM Users
    WHERE Email = p_email;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_user_by_email created';

-- =============================================
-- sp_update_user
-- Updates user information
-- =============================================
CREATE OR REPLACE FUNCTION sp_update_user(
    p_user_id UUID,
    p_full_name VARCHAR(100),
    p_is_ev_user BOOLEAN
) RETURNS VOID AS $$
BEGIN
    -- Validate input
    IF LENGTH(p_full_name) < 5 THEN
        RAISE EXCEPTION 'Full name must be at least 5 characters long' USING ERRCODE = '45002';
    END IF;

    -- Check if user exists
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = p_user_id) THEN
        RAISE EXCEPTION 'User not found' USING ERRCODE = '45005';
    END IF;

    UPDATE Users
    SET FullName = p_full_name,
        IsEVUser = p_is_ev_user
    WHERE Id = p_user_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_update_user created';

-- =============================================
-- sp_get_all_users
-- Retrieves all active users
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_all_users()
RETURNS TABLE (
    id UUID,
    email VARCHAR,
    full_name VARCHAR,
    is_ev_user BOOLEAN,
    is_active BOOLEAN,
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT Id, Email, FullName, IsEVUser, IsActive, CreatedAt
    FROM Users
    WHERE IsActive = TRUE
    ORDER BY CreatedAt DESC;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_all_users created';

-- =============================================
-- sp_get_wallet_by_user_id
-- Retrieves wallet information for a user
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_wallet_by_user_id(
    p_user_id UUID
) RETURNS TABLE (
    id UUID,
    user_id UUID,
    balance NUMERIC(10,2),
    updated_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT Id, UserId, Balance, UpdatedAt
    FROM UserWallets
    WHERE UserId = p_user_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_wallet_by_user_id created';

-- =============================================
-- sp_add_to_wallet
-- Adds funds to user wallet (deposit)
-- =============================================
CREATE OR REPLACE FUNCTION sp_add_to_wallet(
    p_user_id UUID,
    p_amount NUMERIC(10,2)
) RETURNS VOID AS $$
BEGIN
    -- Validate amount
    IF p_amount <= 0 THEN
        RAISE EXCEPTION 'Amount must be greater than 0' USING ERRCODE = '45006';
    END IF;

    -- Check if wallet exists
    IF NOT EXISTS (SELECT 1 FROM UserWallets WHERE UserId = p_user_id) THEN
        RAISE EXCEPTION 'Wallet not found for this user' USING ERRCODE = '45007';
    END IF;

    -- Add to balance
    UPDATE UserWallets
    SET Balance = Balance + p_amount,
        UpdatedAt = CURRENT_TIMESTAMP
    WHERE UserId = p_user_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_add_to_wallet created';

-- =============================================
-- sp_deduct_from_wallet
-- Deducts funds from user wallet
-- =============================================
CREATE OR REPLACE FUNCTION sp_deduct_from_wallet(
    p_user_id UUID,
    p_amount NUMERIC(10,2)
) RETURNS VOID AS $$
DECLARE
    v_current_balance NUMERIC(10,2);
BEGIN
    -- Validate amount
    IF p_amount <= 0 THEN
        RAISE EXCEPTION 'Amount must be greater than 0' USING ERRCODE = '45006';
    END IF;

    -- Get current balance
    SELECT Balance INTO v_current_balance
    FROM UserWallets
    WHERE UserId = p_user_id;

    IF v_current_balance IS NULL THEN
        RAISE EXCEPTION 'Wallet not found for this user' USING ERRCODE = '45007';
    END IF;

    -- Check sufficient balance
    IF v_current_balance < p_amount THEN
        RAISE EXCEPTION 'Insufficient balance' USING ERRCODE = '45008';
    END IF;

    -- Deduct from balance
    UPDATE UserWallets
    SET Balance = Balance - p_amount,
        UpdatedAt = CURRENT_TIMESTAMP
    WHERE UserId = p_user_id;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_deduct_from_wallet created';

-- =============================================
-- sp_get_wallet_balance
-- Gets current wallet balance
-- =============================================
CREATE OR REPLACE FUNCTION sp_get_wallet_balance(
    p_user_id UUID,
    OUT balance NUMERIC(10,2)
) AS $$
BEGIN
    SELECT Balance INTO balance
    FROM UserWallets
    WHERE UserId = p_user_id;

    IF balance IS NULL THEN
        RAISE EXCEPTION 'Wallet not found for this user' USING ERRCODE = '45007';
    END IF;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '✓ sp_get_wallet_balance created';

RAISE NOTICE '========================================';
RAISE NOTICE 'Member 1 Functions Created Successfully!';
RAISE NOTICE '========================================';
