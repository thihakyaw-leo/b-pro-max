-- Phase M3-FIX-10: Access request audit log for time-lock enforcement
CREATE TABLE IF NOT EXISTS access_requests (
    id          SERIAL PRIMARY KEY,
    timestamp   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    username    TEXT NOT NULL,
    reason      TEXT NOT NULL,
    granted     BOOLEAN NOT NULL,
    enabled     BOOLEAN NOT NULL,
    hour        INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_access_requests_timestamp ON access_requests(timestamp);
CREATE INDEX IF NOT EXISTS idx_access_requests_username ON access_requests(username);

COMMENT ON TABLE access_requests IS 'Audit log for time-lock access requests';
COMMENT ON COLUMN access_requests.username IS 'User requesting access';
COMMENT ON COLUMN access_requests.reason IS 'Reason for access request';
COMMENT ON COLUMN access_requests.granted IS 'Whether access was granted';
COMMENT ON COLUMN access_requests.enabled IS 'Whether time lock was enabled';
COMMENT ON COLUMN access_requests.hour IS 'Hour of the request (0-23)';
