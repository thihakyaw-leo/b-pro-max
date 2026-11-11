-- Phase M3-FIX-09: Application settings table for persistent configuration
CREATE TABLE IF NOT EXISTS app_settings (
    key        TEXT PRIMARY KEY,
    value      TEXT NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE app_settings IS 'Application settings persisted in database';
COMMENT ON COLUMN app_settings.key IS 'Setting key';
COMMENT ON COLUMN app_settings.value IS 'Setting value';
COMMENT ON COLUMN app_settings.updated_at IS 'Last update timestamp';
