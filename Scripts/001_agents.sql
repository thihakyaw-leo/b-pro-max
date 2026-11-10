-- Phase 0-04: Core agent table (matches DD.exe AgentThai schema)
CREATE TABLE IF NOT EXISTS agents (
    id SERIAL PRIMARY KEY,
    agent_name TEXT NOT NULL,
    address TEXT,
    phone TEXT,
    commission NUMERIC(12,2) DEFAULT 0,
    rate NUMERIC(12,2) DEFAULT 0,
    commission1 NUMERIC(12,2) DEFAULT 0,
    rate1 NUMERIC(12,2) DEFAULT 0,
    currency_type TEXT,
    currency_rate NUMERIC(12,4) DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_agents_agent_name ON agents(agent_name);
CREATE INDEX IF NOT EXISTS idx_agents_created_at ON agents(created_at);

-- Comments for documentation
COMMENT ON TABLE agents IS 'Agent master table - migrated from DD.exe AgentThai';
COMMENT ON COLUMN agents.agent_name IS 'Agent full name';
COMMENT ON COLUMN agents.address IS 'Agent address';
COMMENT ON COLUMN agents.phone IS 'Agent phone number';
COMMENT ON COLUMN agents.commission IS 'Commission amount';
COMMENT ON COLUMN agents.rate IS 'Commission rate';
COMMENT ON COLUMN agents.commission1 IS 'Secondary commission';
COMMENT ON COLUMN agents.rate1 IS 'Secondary rate';
COMMENT ON COLUMN agents.currency_type IS 'Currency type code';
COMMENT ON COLUMN agents.currency_rate IS 'Currency exchange rate';
