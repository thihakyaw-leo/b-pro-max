using Npgsql;
using Xunit;

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
}
