using BProMax.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BProMax.Services;

/// <summary>
/// Database migration service for creating tables and seeding data against a local PostgreSQL database.
/// </summary>
public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IDatabaseService _db;
    private readonly IAgentService _agentService;
    private readonly ILogger<DatabaseMigrationService> _logger;
    private bool _migrationChecked;

    private static readonly Action<ILogger, Exception?> _logTableCreated =
        LoggerMessage.Define(LogLevel.Information, 1, "'agents' table created or already exists in local database.");
    private static readonly Action<ILogger, Exception?> _logTableMissing =
        LoggerMessage.Define(LogLevel.Warning, 2, "'agents' table was not found in local database.");
    private static readonly Action<ILogger, int, Exception?> _logSeedSkipped =
        LoggerMessage.Define<int>(LogLevel.Information, 3, "Seed skipped: {Count} agent(s) already exist.");
    private static readonly Action<ILogger, int, Exception?> _logSeeded =
        LoggerMessage.Define<int>(LogLevel.Information, 4, "Seeded {Count} test agents into local database.");
    private static readonly Action<ILogger, Exception?> _logMigrationFailed =
        LoggerMessage.Define(LogLevel.Warning, 5, "Local database migration check failed.");

    private const string CreateAgentsTableSql = @"
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
";

    private const string CreateUsersTableSql = @"
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
";

    private const string CreateSalesTableSql = @"
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
";

    private const string CreateAutoBuyRulesTableSql = @"
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
";

    private const string CreateAppSettingsTableSql = @"
CREATE TABLE IF NOT EXISTS app_settings (
    key        TEXT PRIMARY KEY,
    value      TEXT NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
";

    private const string CreateAccessRequestsTableSql = @"
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
";

    private const string CreateDrawsTableSql = """
CREATE TABLE IF NOT EXISTS draws (
    id           BIGSERIAL PRIMARY KEY,
    draw_date    DATE NOT NULL,
    draw_time    TIME NOT NULL,
    draw_number  CHAR(2) NULL
                 CHECK (draw_number IS NULL OR draw_number ~ '^[0-9]{2}$'),
    status       TEXT NOT NULL DEFAULT 'Open'
                 CHECK (status IN ('Open','Closed','Drawn','Settled','Cancelled')),
    created_by   INTEGER NULL REFERENCES users(id),
    settled_by   INTEGER NULL REFERENCES users(id),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (draw_date, draw_time)
);

CREATE INDEX IF NOT EXISTS idx_draws_status_date ON draws(status, draw_date);
""";

    private const string ExtendDrawsTableSql = """
ALTER TABLE draws
    ADD COLUMN IF NOT EXISTS settled_by INTEGER NULL REFERENCES users(id);
""";

    private const string CreateBetTypesTableSql = """
CREATE TABLE IF NOT EXISTS bet_types (
    id                 SERIAL PRIMARY KEY,
    code               TEXT NOT NULL UNIQUE,
    name_en            TEXT NOT NULL,
    name_my            TEXT NOT NULL,
    rule_type          TEXT NOT NULL,
    rule_config        JSONB NOT NULL DEFAULT '{}'::jsonb,
    amount_mode        TEXT NOT NULL
                       CHECK (amount_mode IN ('PerNumber','TotalSplit','Total')),
    payout_mode        TEXT NOT NULL
                       CHECK (payout_mode IN ('PerWinningNumber','TotalIfHit')),
    payout_multiplier  NUMERIC(10,4) NOT NULL CHECK (payout_multiplier > 0),
    is_active          BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order         INTEGER NOT NULL DEFAULT 0,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
""";

    private const string CreateNumberGroupsTableSql = """
CREATE TABLE IF NOT EXISTS number_groups (
    id          SERIAL PRIMARY KEY,
    code        TEXT NOT NULL UNIQUE,
    name_en     TEXT NOT NULL,
    name_my     TEXT NOT NULL,
    allow_r     BOOLEAN NOT NULL DEFAULT TRUE,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order  INTEGER NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS number_group_members (
    group_id    INTEGER NOT NULL REFERENCES number_groups(id) ON DELETE CASCADE,
    number      CHAR(2) NOT NULL CHECK (number ~ '^[0-9]{2}$'),
    sort_order  INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (group_id, number)
);

CREATE INDEX IF NOT EXISTS idx_number_group_members_number
    ON number_group_members(number);
""";

    private const string ExtendSalesTableSql = """
ALTER TABLE sales
    ADD COLUMN IF NOT EXISTS draw_id BIGINT NULL REFERENCES draws(id),
    ADD COLUMN IF NOT EXISTS created_by INTEGER NULL REFERENCES users(id),
    ADD COLUMN IF NOT EXISTS settlement_status TEXT NOT NULL DEFAULT 'Open',
    ADD COLUMN IF NOT EXISTS settled_at TIMESTAMPTZ NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_sales_settlement_status'
    ) THEN
        ALTER TABLE sales
            ADD CONSTRAINT chk_sales_settlement_status
            CHECK (settlement_status IN ('Open','Settled','Cancelled'));
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_sales_draw_id ON sales(draw_id);
""";

    private const string CreateSaleItemsTableSql = """
CREATE TABLE IF NOT EXISTS sale_items (
    id                BIGSERIAL PRIMARY KEY,
    sale_id           BIGINT NOT NULL REFERENCES sales(id) ON DELETE CASCADE,
    draw_id           BIGINT NOT NULL REFERENCES draws(id),
    bet_type_id       INTEGER NOT NULL REFERENCES bet_types(id),
    number_group_id   INTEGER NULL REFERENCES number_groups(id),
    bet_key           TEXT NOT NULL,
    amount            NUMERIC(14,2) NOT NULL CHECK (amount > 0),
    payout_multiplier NUMERIC(10,4) NOT NULL CHECK (payout_multiplier > 0),
    payout_mode       TEXT NOT NULL
                      CHECK (payout_mode IN ('PerWinningNumber','TotalIfHit')),
    amount_mode       TEXT NOT NULL
                      CHECK (amount_mode IN ('PerNumber','TotalSplit','Total')),
    match_mode        TEXT NOT NULL DEFAULT 'Exact'
                      CHECK (match_mode IN ('Exact','UnorderedPair')),
    status            TEXT NOT NULL DEFAULT 'Pending'
                      CHECK (status IN ('Pending','Won','Lost','Cancelled')),
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_sale_items_sale ON sale_items(sale_id);
CREATE INDEX IF NOT EXISTS idx_sale_items_draw ON sale_items(draw_id);
""";

    private const string ExtendSaleItemsTableSql = """
ALTER TABLE sale_items
    ADD COLUMN IF NOT EXISTS match_mode TEXT NOT NULL DEFAULT 'Exact';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_sale_items_match_mode'
    ) THEN
        ALTER TABLE sale_items
            ADD CONSTRAINT chk_sale_items_match_mode
            CHECK (match_mode IN ('Exact','UnorderedPair'));
    END IF;
END $$;
""";

    private const string CreateSaleItemNumbersTableSql = """
CREATE TABLE IF NOT EXISTS sale_item_numbers (
    id              BIGSERIAL PRIMARY KEY,
    sale_item_id    BIGINT NOT NULL REFERENCES sale_items(id) ON DELETE CASCADE,
    number          CHAR(2) NOT NULL CHECK (number ~ '^[0-9]{2}$'),
    stake           NUMERIC(14,2) NOT NULL CHECK (stake >= 0),
    is_reverse      BOOLEAN NOT NULL DEFAULT FALSE,
    status          TEXT NOT NULL DEFAULT 'Pending'
                    CHECK (status IN ('Pending','Won','Lost','Cancelled')),
    payout_amount   NUMERIC(14,2) NOT NULL DEFAULT 0,
    UNIQUE (sale_item_id, number)
);

CREATE INDEX IF NOT EXISTS idx_sale_item_numbers_number
    ON sale_item_numbers(number);
""";

    private const string CreatePayoutsTableSql = """
CREATE TABLE IF NOT EXISTS payouts (
    id                   BIGSERIAL PRIMARY KEY,
    draw_id              BIGINT NOT NULL REFERENCES draws(id),
    sale_id              BIGINT NOT NULL REFERENCES sales(id),
    sale_item_id         BIGINT NOT NULL REFERENCES sale_items(id),
    sale_item_number_id  BIGINT NULL REFERENCES sale_item_numbers(id),
    amount               NUMERIC(14,2) NOT NULL CHECK (amount > 0),
    status               TEXT NOT NULL DEFAULT 'Pending'
                         CHECK (status IN ('Pending','Paid','Cancelled')),
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    paid_at              TIMESTAMPTZ NULL,
    paid_by              INTEGER NULL REFERENCES users(id)
);

CREATE INDEX IF NOT EXISTS idx_payouts_draw_status ON payouts(draw_id, status);
""";

    private const string InsertBetTypeSql = """
INSERT INTO bet_types
    (code, name_en, name_my, rule_type, rule_config, amount_mode,
     payout_mode, payout_multiplier, is_active, sort_order)
VALUES
    (@code, @name_en, @name_my, @rule_type, CAST(@rule_config AS jsonb), @amount_mode,
     @payout_mode, @payout_multiplier, @is_active, @sort_order)
ON CONFLICT (code) DO NOTHING;
""";

    private const string InsertNumberGroupSql = """
INSERT INTO number_groups (code, name_en, name_my, allow_r, is_active, sort_order)
VALUES (@code, @name_en, @name_my, @allow_r, @is_active, @sort_order)
ON CONFLICT (code) DO NOTHING;
""";

    private const string InsertNumberGroupMemberSql = """
INSERT INTO number_group_members (group_id, number, sort_order)
SELECT g.id, @number, @sort_order
FROM number_groups g
WHERE g.code = @group_code
ON CONFLICT (group_id, number) DO NOTHING;
""";

    public DatabaseMigrationService(
        IDatabaseService db,
        IAgentService agentService,
        ILogger<DatabaseMigrationService> logger)
    {
        _db = db;
        _agentService = agentService;
        _logger = logger;
    }

    public async Task<bool> AgentsTableExistsAsync()
    {
        const string sql = @"
SELECT EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'agents'
);";
        try
        {
            var exists = await _db.QuerySingleAsync(sql, r => r.GetBoolean(0));
            return exists;
        }
        catch
        {
            return false;
        }
    }

    public async Task EnsureTablesCreatedAsync()
    {
        if (_migrationChecked) return;
        _migrationChecked = true;

        try
        {
            await _db.ExecuteNonQueryAsync(CreateAgentsTableSql);
            await _db.ExecuteNonQueryAsync(CreateUsersTableSql);
            await _db.ExecuteNonQueryAsync(CreateSalesTableSql);
            await _db.ExecuteNonQueryAsync(CreateAutoBuyRulesTableSql);
            await _db.ExecuteNonQueryAsync(CreateAppSettingsTableSql);
            await _db.ExecuteNonQueryAsync(CreateAccessRequestsTableSql);
            await _db.ExecuteNonQueryAsync(CreateDrawsTableSql);
            await _db.ExecuteNonQueryAsync(ExtendDrawsTableSql);
            await _db.ExecuteNonQueryAsync(CreateBetTypesTableSql);
            await _db.ExecuteNonQueryAsync(CreateNumberGroupsTableSql);
            await _db.ExecuteNonQueryAsync(ExtendSalesTableSql);
            await _db.ExecuteNonQueryAsync(CreateSaleItemsTableSql);
            await _db.ExecuteNonQueryAsync(ExtendSaleItemsTableSql);
            await _db.ExecuteNonQueryAsync(CreateSaleItemNumbersTableSql);
            await _db.ExecuteNonQueryAsync(CreatePayoutsTableSql);
            await SeedTwoDDataAsync();
            _logTableCreated(_logger, null);
        }
        catch (Exception ex)
        {
            _logMigrationFailed(_logger, ex);
        }
    }

    private async Task SeedTwoDDataAsync()
    {
        foreach (var betType in TwoDSeedData.BetTypes)
        {
            await _db.ExecuteNonQueryAsync(
                InsertBetTypeSql,
                new NpgsqlParameter("@code", betType.Code),
                new NpgsqlParameter("@name_en", betType.NameEn),
                new NpgsqlParameter("@name_my", betType.NameMy),
                new NpgsqlParameter("@rule_type", betType.RuleType),
                new NpgsqlParameter("@rule_config", betType.RuleConfigJson),
                new NpgsqlParameter("@amount_mode", betType.AmountMode),
                new NpgsqlParameter("@payout_mode", betType.PayoutMode),
                new NpgsqlParameter("@payout_multiplier", betType.PayoutMultiplier),
                new NpgsqlParameter("@is_active", betType.IsActive),
                new NpgsqlParameter("@sort_order", betType.SortOrder));
        }

        foreach (var group in TwoDSeedData.NumberGroups)
        {
            await _db.ExecuteNonQueryAsync(
                InsertNumberGroupSql,
                new NpgsqlParameter("@code", group.Code),
                new NpgsqlParameter("@name_en", group.NameEn),
                new NpgsqlParameter("@name_my", group.NameMy),
                new NpgsqlParameter("@allow_r", group.AllowReverse),
                new NpgsqlParameter("@is_active", group.IsActive),
                new NpgsqlParameter("@sort_order", group.SortOrder));

            for (var index = 0; index < group.Members.Count; index++)
            {
                await _db.ExecuteNonQueryAsync(
                    InsertNumberGroupMemberSql,
                    new NpgsqlParameter("@group_code", group.Code),
                    new NpgsqlParameter("@number", group.Members[index]),
                    new NpgsqlParameter("@sort_order", index + 1));
            }
        }
    }

    public async Task SeedDataAsync()
    {
        var exists = await AgentsTableExistsAsync();
        if (!exists)
        {
            _logTableMissing(_logger, null);
            return;
        }

        var agents = await _agentService.GetAllAgentsAsync();
        if (agents.Count > 0)
        {
            _logSeedSkipped(_logger, agents.Count, null);
            return;
        }

        var seedAgents = new[]
        {
            new Agent
            {
                AgentName = "John Doe",
                Address = "123 Main Street, Bangkok",
                Phone = "02-123-4567",
                Commission = 1000m,
                Rate = 5m,
                Commission1 = 500m,
                Rate1 = 2.5m,
                CurrencyType = "THB",
                CurrencyRate = 1m
            },
            new Agent
            {
                AgentName = "Jane Smith",
                Address = "456 Sukhumvit Rd, Bangkok",
                Phone = "02-765-4321",
                Commission = 1500m,
                Rate = 7.5m,
                Commission1 = 750m,
                Rate1 = 3m,
                CurrencyType = "THB",
                CurrencyRate = 1m
            },
            new Agent
            {
                AgentName = "Bob Wilson",
                Address = "789 Silom Rd, Bangkok",
                Phone = "02-555-0100",
                Commission = 800m,
                Rate = 4m,
                Commission1 = 400m,
                Rate1 = 2m,
                CurrencyType = "USD",
                CurrencyRate = 35.5m
            }
        };

        foreach (var agent in seedAgents)
        {
            await _agentService.CreateAgentAsync(agent);
        }

        _logSeeded(_logger, seedAgents.Length, null);
    }

    public async Task<(bool tableExists, int agentCount)> GetStatusAsync()
    {
        var exists = await AgentsTableExistsAsync();
        if (!exists) return (false, 0);

        const string countSql = "SELECT COUNT(*)::int FROM agents";
        var count = await _db.QuerySingleAsync(countSql, r => r.GetInt32(0));
        return (true, count);
    }
}
