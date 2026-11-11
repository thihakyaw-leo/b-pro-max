using Npgsql;
using Xunit;
using BProMax.Services;
using BProMax.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace BProMax.E2ETests;

/// <summary>
/// Base class for E2E tests that connect to a real PostgreSQL database.
/// Tests are skipped when no test database is available.
/// </summary>
public abstract class E2ETestBase : IAsyncLifetime
{
    protected NpgsqlConnection Connection { get; private set; } = null!;

    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=bpro_max_test;Username=postgres;Password=postgres";

    public virtual async Task InitializeAsync()
    {
        try
        {
            Connection = new NpgsqlConnection(TestConnectionString);
            await Connection.OpenAsync();
            await EnsureSchemaAsync();
        }
        catch (Exception)
        {
            // Database not available — tests will be skipped
            Connection = null!;
        }
    }

    public virtual async Task DisposeAsync()
    {
        if (Connection != null)
        {
            await Connection.CloseAsync();
            Connection.Dispose();
        }
    }

    protected bool IsAvailable => Connection != null;

    protected bool SkipIfUnavailable()
    {
        return !IsAvailable;
    }

    private static async Task EnsureSchemaAsync(NpgsqlConnection conn)
    {
        // Create agents table if it doesn't exist (matches Scripts/001_agents.sql)
        const string sql = @"
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

CREATE TABLE IF NOT EXISTS draws (
    id BIGSERIAL PRIMARY KEY,
    draw_date DATE NOT NULL,
    draw_time TIME NOT NULL,
    draw_number TEXT,
    status TEXT NOT NULL DEFAULT 'Open',
    created_by INTEGER,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_draws_status ON draws(status);
CREATE INDEX IF NOT EXISTS idx_draws_draw_date ON draws(draw_date);

CREATE TABLE IF NOT EXISTS sales (
    id BIGSERIAL PRIMARY KEY,
    agent_id INTEGER NOT NULL REFERENCES agents(id) ON DELETE RESTRICT,
    sale_date TIMESTAMPTZ NOT NULL,
    amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    commission NUMERIC(14,2) NOT NULL DEFAULT 0,
    commission1 NUMERIC(14,2) NOT NULL DEFAULT 0,
    currency_type TEXT,
    currency_rate NUMERIC(12,4) NOT NULL DEFAULT 1,
    status TEXT NOT NULL DEFAULT 'Pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_sales_sale_date ON sales(sale_date);
CREATE INDEX IF NOT EXISTS idx_sales_agent_id ON sales(agent_id);

CREATE TABLE IF NOT EXISTS auto_buy_rules (
    id SERIAL PRIMARY KEY,
    agent_name TEXT NOT NULL,
    trigger_amount NUMERIC(14,2) NOT NULL,
    auto_buy_amount NUMERIC(14,2) NOT NULL,
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_executed TIMESTAMPTZ NULL,
    execution_count INTEGER NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_auto_buy_agent ON auto_buy_rules (LOWER(agent_name));

-- SaleEntryService needs these additional columns/tables that are not yet in the SQL migration scripts.
ALTER TABLE sales ADD COLUMN IF NOT EXISTS draw_id BIGINT;
ALTER TABLE sales ADD COLUMN IF NOT EXISTS created_by INTEGER;
ALTER TABLE sales ADD COLUMN IF NOT EXISTS settlement_status TEXT DEFAULT 'Open';

CREATE TABLE IF NOT EXISTS bet_types (
    id SERIAL PRIMARY KEY,
    code TEXT NOT NULL UNIQUE,
    name_en TEXT NOT NULL,
    name_my TEXT NOT NULL,
    rule_type TEXT NOT NULL,
    rule_config TEXT NOT NULL DEFAULT '{}',
    amount_mode TEXT NOT NULL DEFAULT 'TotalSplit',
    payout_mode TEXT NOT NULL DEFAULT 'PerWinningNumber',
    payout_multiplier NUMERIC(12,2) NOT NULL DEFAULT 80,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS number_groups (
    id SERIAL PRIMARY KEY,
    code TEXT NOT NULL UNIQUE,
    name_en TEXT NOT NULL,
    name_my TEXT NOT NULL,
    allow_r BOOLEAN NOT NULL DEFAULT TRUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS number_group_members (
    id SERIAL PRIMARY KEY,
    group_id INTEGER NOT NULL REFERENCES number_groups(id) ON DELETE CASCADE,
    number TEXT NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    UNIQUE(group_id, number)
);

CREATE TABLE IF NOT EXISTS sale_items (
    id BIGSERIAL PRIMARY KEY,
    sale_id BIGINT NOT NULL REFERENCES sales(id) ON DELETE CASCADE,
    draw_id BIGINT NOT NULL,
    bet_type_id INTEGER NOT NULL,
    number_group_id INTEGER REFERENCES number_groups(id) ON DELETE SET NULL,
    bet_key TEXT NOT NULL,
    amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    payout_multiplier NUMERIC(12,2) NOT NULL DEFAULT 80,
    payout_mode TEXT NOT NULL DEFAULT 'PerWinningNumber',
    amount_mode TEXT NOT NULL DEFAULT 'TotalSplit',
    match_mode TEXT NOT NULL DEFAULT 'Exact',
    status TEXT NOT NULL DEFAULT 'Pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS sale_item_numbers (
    id BIGSERIAL PRIMARY KEY,
    sale_item_id BIGINT NOT NULL REFERENCES sale_items(id) ON DELETE CASCADE,
    number TEXT NOT NULL,
    stake NUMERIC(14,2) NOT NULL DEFAULT 0,
    is_reverse BOOLEAN NOT NULL DEFAULT FALSE
);
";
        using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    protected static async Task EnsureSchemaAsync()
    {
        using var conn = new NpgsqlConnection(TestConnectionString);
        await conn.OpenAsync();
        await EnsureSchemaAsync(conn);
    }

    protected async Task CleanAgentsTableAsync()
    {
        if (!IsAvailable) return;
        using var cmd = new NpgsqlCommand("DELETE FROM agents", Connection);
        await cmd.ExecuteNonQueryAsync();
    }

    protected async Task<int> InsertTestAgentAsync(string name, string? address = null, string? phone = null,
        decimal commission = 0, decimal rate = 0, decimal commission1 = 0, decimal rate1 = 0,
        string? currencyType = null, decimal currencyRate = 1)
    {
        if (!IsAvailable) return 0;
        const string sql = @"
            INSERT INTO agents (agent_name, address, phone, commission, rate, commission1, rate1, currency_type, currency_rate, updated_at)
            VALUES (@agent_name, @address, @phone, @commission, @rate, @commission1, @rate1, @currency_type, @currency_rate, @updated_at)
            RETURNING id";
        using var cmd = new NpgsqlCommand(sql, Connection);
        cmd.Parameters.AddWithValue("@agent_name", name);
        cmd.Parameters.AddWithValue("@address", (object?)address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@phone", (object?)phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@commission", commission);
        cmd.Parameters.AddWithValue("@rate", rate);
        cmd.Parameters.AddWithValue("@commission1", commission1);
        cmd.Parameters.AddWithValue("@rate1", rate1);
        cmd.Parameters.AddWithValue("@currency_type", (object?)currencyType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@currency_rate", currencyRate);
        cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
        return (int)((await cmd.ExecuteScalarAsync())!);
    }

    #region Service factory

    protected IDatabaseService CreateDatabaseService()
    {
        var options = Options.Create(new DatabaseSettings
        {
            ConnectionString = TestConnectionString
        });
        return new DatabaseService(options, NullLogger<DatabaseService>.Instance);
    }

    #endregion

    #region Draw helpers

    protected async Task<long> InsertTestDrawAsync(DateOnly? drawDate = null, TimeOnly? drawTime = null,
        string status = DrawStatuses.Open, string? drawNumber = null, int? createdBy = null)
    {
        if (!IsAvailable) return 0;
        const string sql = @"
            INSERT INTO draws (draw_date, draw_time, draw_number, status, created_by, updated_at)
            VALUES (@draw_date, @draw_time, @draw_number, @status, @created_by, @updated_at)
            RETURNING id";
        using var cmd = new NpgsqlCommand(sql, Connection);
        cmd.Parameters.AddWithValue("@draw_date", drawDate ?? DateOnly.FromDateTime(DateTime.UtcNow));
        cmd.Parameters.AddWithValue("@draw_time", drawTime ?? TimeOnly.FromDateTime(DateTime.UtcNow));
        cmd.Parameters.AddWithValue("@draw_number", (object?)drawNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@created_by", (object?)createdBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
        return (long)((await cmd.ExecuteScalarAsync())!);
    }

    #endregion

    #region Bet type helpers

    protected async Task<int> InsertTestBetTypeAsync(string code, string nameEn, string nameMy,
        string ruleType = NumberRuleTypes.Exact, string ruleConfig = "{}",
        string amountMode = BetAmountModes.TotalSplit, string payoutMode = "PerWinningNumber",
        decimal payoutMultiplier = 80m, bool isActive = true, int sortOrder = 0)
    {
        if (!IsAvailable) return 0;
        const string sql = @"
            INSERT INTO bet_types (code, name_en, name_my, rule_type, rule_config, amount_mode,
                payout_mode, payout_multiplier, is_active, sort_order, updated_at)
            VALUES (@code, @name_en, @name_my, @rule_type, @rule_config, @amount_mode,
                @payout_mode, @payout_multiplier, @is_active, @sort_order, @updated_at)
            RETURNING id";
        using var cmd = new NpgsqlCommand(sql, Connection);
        cmd.Parameters.AddWithValue("@code", code);
        cmd.Parameters.AddWithValue("@name_en", nameEn);
        cmd.Parameters.AddWithValue("@name_my", nameMy);
        cmd.Parameters.AddWithValue("@rule_type", ruleType);
        cmd.Parameters.AddWithValue("@rule_config", ruleConfig);
        cmd.Parameters.AddWithValue("@amount_mode", amountMode);
        cmd.Parameters.AddWithValue("@payout_mode", payoutMode);
        cmd.Parameters.AddWithValue("@payout_multiplier", payoutMultiplier);
        cmd.Parameters.AddWithValue("@is_active", isActive);
        cmd.Parameters.AddWithValue("@sort_order", sortOrder);
        cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
        return (int)((await cmd.ExecuteScalarAsync())!);
    }

    #endregion

    #region Number group helpers

    protected async Task<int> InsertTestNumberGroupAsync(string code, string nameEn, string nameMy,
        bool allowReverse = true, bool isActive = true, int sortOrder = 0,
        IReadOnlyList<string>? members = null)
    {
        if (!IsAvailable) return 0;
        const string sql = @"
            INSERT INTO number_groups (code, name_en, name_my, allow_r, is_active, sort_order, updated_at)
            VALUES (@code, @name_en, @name_my, @allow_r, @is_active, @sort_order, @updated_at)
            RETURNING id";
        using var cmd = new NpgsqlCommand(sql, Connection);
        cmd.Parameters.AddWithValue("@code", code);
        cmd.Parameters.AddWithValue("@name_en", nameEn);
        cmd.Parameters.AddWithValue("@name_my", nameMy);
        cmd.Parameters.AddWithValue("@allow_r", allowReverse);
        cmd.Parameters.AddWithValue("@is_active", isActive);
        cmd.Parameters.AddWithValue("@sort_order", sortOrder);
        cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
        var groupId = (int)((await cmd.ExecuteScalarAsync())!);

        if (members is not null)
        {
            var index = 0;
            foreach (var number in members)
            {
                using var memberCmd = new NpgsqlCommand(
                    "INSERT INTO number_group_members (group_id, number, sort_order) VALUES (@group_id, @number, @sort_order)", Connection);
                memberCmd.Parameters.AddWithValue("@group_id", groupId);
                memberCmd.Parameters.AddWithValue("@number", number);
                memberCmd.Parameters.AddWithValue("@sort_order", index++);
                await memberCmd.ExecuteNonQueryAsync();
            }
        }

        return groupId;
    }

    #endregion
}
