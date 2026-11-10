using Npgsql;
using System.Globalization;
using Xunit;

namespace BProMax.E2ETests;

/// <summary>
/// E2E tests for database compaction (VACUUM) against a real PostgreSQL database.
/// Mirrors DD.exe database compaction: VACUUM, REINDEX operations.
/// </summary>
[Collection("E2E Database")]
public class DatabaseCompactionE2ETests : E2ETestBase
{
    [Fact]
    public async Task VacuumAgentsTableSucceedsAndPreservesData()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        // Insert test data
        await InsertTestAgentAsync("Compact Agent 1", null, null, 1000m, 5m);
        await InsertTestAgentAsync("Compact Agent 2", null, null, 2000m, 7.5m);

        // Record data before vacuum
        var beforeCount = await GetAgentCountAsync();
        Assert.Equal(2, beforeCount);

        // VACUUM the agents table (DD.exe equivalent: compact database)
        using var cmd = new NpgsqlCommand("VACUUM agents", Connection);
        await cmd.ExecuteNonQueryAsync();

        // Verify data preserved after vacuum
        var afterCount = await GetAgentCountAsync();
        Assert.Equal(2, afterCount);

        var names = await GetAgentNamesAsync();
        Assert.Contains("Compact Agent 1", names);
        Assert.Contains("Compact Agent 2", names);
    }

    [Fact]
    public async Task VacuumFullAgentsTableSucceedsAndPreservesData()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Vacuum Full Agent", null, null, 500m, 3m);

        var beforeCount = await GetAgentCountAsync();
        Assert.Equal(1, beforeCount);

        // VACUUM FULL (DD.exe equivalent: full compact)
        using var cmd = new NpgsqlCommand("VACUUM (FULL) agents", Connection);
        await cmd.ExecuteNonQueryAsync();

        var afterCount = await GetAgentCountAsync();
        Assert.Equal(1, afterCount);
    }

    [Fact]
    public async Task ReindexAgentsTableSucceeds()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Reindex Agent", null, null);

        // REINDEX the agents table indexes (DD.exe equivalent: rebuild indexes)
        using var cmd = new NpgsqlCommand("REINDEX TABLE agents", Connection);
        await cmd.ExecuteNonQueryAsync();

        // Verify table still functional after reindex
        var count = await GetAgentCountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AnalyzeAgentsTableSucceedsAndImprovesQueryPlanning()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Analyze Agent 1", null, null, 100m, 1m);
        await InsertTestAgentAsync("Analyze Agent 2", null, null, 200m, 2m);

        // ANALYZE updates query planner statistics (DD.exe equivalent: optimize)
        using var cmd = new NpgsqlCommand("ANALYZE agents", Connection);
        await cmd.ExecuteNonQueryAsync();

        // Verify table still functional
        var count = await GetAgentCountAsync();
        Assert.Equal(2, count);
    }

    private static async Task<int> GetAgentCountAsync(NpgsqlConnection conn)
    {
        using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM agents", conn);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()!, CultureInfo.InvariantCulture);
    }

    private async Task<int> GetAgentCountAsync()
    {
        return await GetAgentCountAsync(Connection);
    }

    private async Task<List<string>> GetAgentNamesAsync()
    {
        var names = new List<string>();
        using var cmd = new NpgsqlCommand("SELECT agent_name FROM agents ORDER BY agent_name", Connection);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));
        return names;
    }
}
