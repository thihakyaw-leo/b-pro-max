-- Phase M2-FIX-05: Sales table for sales tracking and reporting
CREATE TABLE IF NOT EXISTS sales (
    id             BIGSERIAL PRIMARY KEY,
    agent_id       INTEGER NOT NULL REFERENCES agents(id) ON DELETE RESTRICT,
    sale_date      TIMESTAMPTZ NOT NULL,
    amount         NUMERIC(14,2) NOT NULL DEFAULT 0,
    commission     NUMERIC(14,2) NOT NULL DEFAULT 0,
    commission1    NUMERIC(14,2) NOT NULL DEFAULT 0,
    currency_type  TEXT,
    currency_rate  NUMERIC(12,4) NOT NULL DEFAULT 1,
    status         TEXT NOT NULL DEFAULT 'Pending'
                   CHECK (status IN ('Pending','Paid','Cancelled')),
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_sales_sale_date ON sales(sale_date);
CREATE INDEX IF NOT EXISTS idx_sales_agent_id ON sales(agent_id);
CREATE INDEX IF NOT EXISTS idx_sales_status ON sales(status);

COMMENT ON TABLE sales IS 'Sales records for tracking and reporting';
COMMENT ON COLUMN sales.id IS 'Unique sale identifier';
COMMENT ON COLUMN sales.agent_id IS 'Reference to agents table';
COMMENT ON COLUMN sales.sale_date IS 'Date and time of the sale';
COMMENT ON COLUMN sales.amount IS 'Sale amount';
COMMENT ON COLUMN sales.commission IS 'Commission amount';
COMMENT ON COLUMN sales.commission1 IS 'Secondary commission amount';
COMMENT ON COLUMN sales.currency_type IS 'Currency type code';
COMMENT ON COLUMN sales.currency_rate IS 'Currency exchange rate';
COMMENT ON COLUMN sales.status IS 'Sale status: Pending, Paid, or Cancelled';
