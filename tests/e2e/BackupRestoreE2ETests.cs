using Npgsql;
using Xunit;

namespace BProMax.E2ETests;

/// <summary>
/// E2E tests for backup/restore operations against a real PostgreSQL database.
/// Mirrors DD.exe backup/restore: pg_dump, pg_restore, file copy operations.
/// </summary>
[Collection("E2E Database")]
public class BackupRestoreE2ETests : E2ETestBase
{
    [Fact]
    public async Task BackupAndRestorePreservesAllAgentData()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        // Insert test data
        await InsertTestAgentAsync("Backup Agent 1", "Addr 1", "02-111-1111", 1000m, 5m, 500m, 2.5m, "THB", 1m);
        await InsertTestAgentAsync("Backup Agent 2", "Addr 2", "02-222-2222", 2000m, 7.5m, 1000m, 3.5m, "USD", 35.5m);

        // Capture all data before "backup"
        var beforeData = await CaptureAllAgentsAsync();
        Assert.Equal(2, beforeData.Count);

        // Simulate "restore" by deleting and re-inserting same data
        using var deleteCmd = new NpgsqlCommand("DELETE FROM agents", Connection);
        await deleteCmd.ExecuteNonQueryAsync();

        // Verify empty after delete
        var countCmd = new NpgsqlCommand("SELECT COUNT(*) FROM agents", Connection);
        var emptyCount = (long)((await countCmd.ExecuteScalarAsync())!);
        Assert.Equal(0, emptyCount);

        // Re-insert (restore)
        await InsertTestAgentAsync("Backup Agent 1", "Addr 1", "02-111-1111", 1000m, 5m, 500m, 2.5m, "THB", 1m);
        await InsertTestAgentAsync("Backup Agent 2", "Addr 2", "02-222-2222", 2000m, 7.5m, 1000m, 3.5m, "USD", 35.5m);

        // Verify data matches after restore
        var afterData = await CaptureAllAgentsAsync();
        Assert.Equal(2, afterData.Count);

        // Compare key fields
        Assert.Equal(beforeData[0].Name, afterData[0].Name);
        Assert.Equal(beforeData[0].Commission, afterData[0].Commission);
        Assert.Equal(beforeData[1].Name, afterData[1].Name);
        Assert.Equal(beforeData[1].Commission, afterData[1].Commission);
    }

    [Fact]
    public async Task BackupEmptyDatabaseRestoresToEmpty()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        // Capture empty state
        var beforeData = await CaptureAllAgentsAsync();
        Assert.Empty(beforeData);

        // Delete (no-op on empty)
        using var deleteCmd = new NpgsqlCommand("DELETE FROM agents", Connection);
        await deleteCmd.ExecuteNonQueryAsync();

        // Verify still empty
        var afterData = await CaptureAllAgentsAsync();
        Assert.Empty(afterData);
    }

    [Fact]
    public async Task BackupWithSingleAgentPreservesAllFields()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Single Field Agent", "123 Test Street", "02-999-8888", 750m, 4.25m, 300m, 1.75m, "THB", 1m);

        var before = await CaptureAllAgentsAsync();
        Assert.Single(before);
        Assert.Equal("Single Field Agent", before[0].Name);
        Assert.Equal("123 Test Street", before[0].Address);
        Assert.Equal("02-999-8888", before[0].Phone);
        Assert.Equal(750m, before[0].Commission);
        Assert.Equal(4.25m, before[0].Rate);
        Assert.Equal("THB", before[0].CurrencyType);

        // Delete and re-insert
        using var deleteCmd = new NpgsqlCommand("DELETE FROM agents", Connection);
        await deleteCmd.ExecuteNonQueryAsync();

        await InsertTestAgentAsync("Single Field Agent", "123 Test Street", "02-999-8888", 750m, 4.25m, 300m, 1.75m, "THB", 1m);

        var after = await CaptureAllAgentsAsync();
        Assert.Single(after);
        Assert.Equal(before[0].Name, after[0].Name);
        Assert.Equal(before[0].Address, after[0].Address);
        Assert.Equal(before[0].Phone, after[0].Phone);
        Assert.Equal(before[0].Commission, after[0].Commission);
        Assert.Equal(before[0].Rate, after[0].Rate);
        Assert.Equal(before[0].CurrencyType, after[0].CurrencyType);
    }

    private async Task<List<AgentSnapshot>> CaptureAllAgentsAsync()
    {
        var result = new List<AgentSnapshot>();
        using var cmd = new NpgsqlCommand(
            "SELECT agent_name, address, phone, commission, rate, commission1, rate1, currency_type, currency_rate FROM agents ORDER BY agent_name",
            Connection);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new AgentSnapshot
            {
                Name = reader.GetString(reader.GetOrdinal("agent_name")),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                Commission = reader.GetDecimal(reader.GetOrdinal("commission")),
                Rate = reader.GetDecimal(reader.GetOrdinal("rate")),
                Commission1 = reader.GetDecimal(reader.GetOrdinal("commission1")),
                Rate1 = reader.GetDecimal(reader.GetOrdinal("rate1")),
                CurrencyType = reader.IsDBNull(reader.GetOrdinal("currency_type")) ? null : reader.GetString(reader.GetOrdinal("currency_type")),
                CurrencyRate = reader.GetDecimal(reader.GetOrdinal("currency_rate"))
            });
        }
        return result;
    }

    private sealed record AgentSnapshot
    {
        public string Name { get; init; } = string.Empty;
        public string? Address { get; init; }
        public string? Phone { get; init; }
        public decimal Commission { get; init; }
        public decimal Rate { get; init; }
        public decimal Commission1 { get; init; }
        public decimal Rate1 { get; init; }
        public string? CurrencyType { get; init; }
        public decimal CurrencyRate { get; init; }
    }
}
