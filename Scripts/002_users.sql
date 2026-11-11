-- Phase M1-FIX-01: Users table for persistent authentication
CREATE TABLE IF NOT EXISTS users (
    id              SERIAL PRIMARY KEY,
    username        TEXT NOT NULL,
    password_hash   BYTEA NOT NULL,
    salt            BYTEA NOT NULL,
    iterations      INTEGER NOT NULL DEFAULT 210000,
    failed_attempts INTEGER NOT NULL DEFAULT 0,
    locked_until    TIMESTAMPTZ NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_username
    ON users (LOWER(username));

CREATE INDEX IF NOT EXISTS idx_users_locked_until
    ON users (locked_until)
    WHERE locked_until IS NOT NULL;

COMMENT ON TABLE users IS 'User accounts for authentication (migrated from in-memory CredentialStore)';
COMMENT ON COLUMN users.username IS 'Unique login username (case-insensitive)';
COMMENT ON COLUMN users.password_hash IS 'PBKDF2-HMAC-SHA256 hash of password + salt';
COMMENT ON COLUMN users.salt IS 'Random salt per user';
COMMENT ON COLUMN users.iterations IS 'PBKDF2 iteration count';
COMMENT ON COLUMN users.failed_attempts IS 'Consecutive failed login attempts';
COMMENT ON COLUMN users.locked_until IS 'Lockout expiration timestamp (NULL = not locked)';
