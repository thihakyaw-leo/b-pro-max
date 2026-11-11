-- Phase M2-FIX-06: Auto-buy rules table for persistent auto-purchase rules
CREATE TABLE IF NOT EXISTS auto_buy_rules (
    id               SERIAL PRIMARY KEY,
    agent_name       TEXT NOT NULL,
    trigger_amount   NUMERIC(14,2) NOT NULL,
    auto_buy_amount  NUMERIC(14,2) NOT NULL,
    is_enabled       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_executed    TIMESTAMPTZ NULL,
    execution_count  INTEGER NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_auto_buy_agent
    ON auto_buy_rules (LOWER(agent_name));

COMMENT ON TABLE auto_buy_rules IS 'Auto-buy rules for automatic purchasing';
COMMENT ON COLUMN auto_buy_rules.agent_name IS 'Agent name this rule applies to';
COMMENT ON COLUMN auto_buy_rules.trigger_amount IS 'Amount that triggers auto-buy';
COMMENT ON COLUMN auto_buy_rules.auto_buy_amount IS 'Amount to auto-buy when triggered';
COMMENT ON COLUMN auto_buy_rules.is_enabled IS 'Whether the rule is active';
COMMENT ON COLUMN auto_buy_rules.last_executed IS 'Last time the rule was executed';
COMMENT ON COLUMN auto_buy_rules.execution_count IS 'Number of times the rule has been executed';
