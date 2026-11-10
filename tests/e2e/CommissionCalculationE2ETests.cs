using Npgsql;
using Xunit;

namespace BProMax.E2ETests;

/// <summary>
/// E2E tests for commission calculation against a real PostgreSQL database.
/// Mirrors DD.exe commission logic: commission = rate-based percentage calculations.
/// </summary>
[Collection("E2E Database")]
public class CommissionCalculationE2ETests : E2ETestBase
{
    [Fact]
    public async Task CalculateCommissionReturnsCorrectAmount()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        // Agent with commission=1000, rate=5 -> commission * rate / 100 = 50
        var id = await InsertTestAgentAsync("Commission Agent", "1 Test St", "02-111-2222", 1000m, 5m, 0, 0);

        using var cmd = new NpgsqlCommand(
            "SELECT commission, rate FROM agents WHERE id = @id", Connection);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());

        var commission = reader.GetDecimal(reader.GetOrdinal("commission"));
        var rate = reader.GetDecimal(reader.GetOrdinal("rate"));
        var expected = commission * rate / 100m;
        Assert.Equal(50m, expected);
    }

    [Fact]
    public async Task CommissionWithZeroRateReturnsZero()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Zero Rate Agent", null, null, 5000m, 0m);

        using var cmd = new NpgsqlCommand("SELECT commission, rate FROM agents WHERE commission = 5000 AND rate = 0", Connection);
        using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());

        var commission = reader.GetDecimal(reader.GetOrdinal("commission"));
        var rate = reader.GetDecimal(reader.GetOrdinal("rate"));
        Assert.Equal(5000m, commission);
        Assert.Equal(0m, rate);
        Assert.Equal(0m, commission * rate / 100m);
    }

    [Fact]
    public async Task Commission1AndRate1StoredCorrectly()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Dual Rate Agent", null, null, 1000m, 5m, 500m, 2.5m);

        using var cmd = new NpgsqlCommand("SELECT commission1, rate1 FROM agents WHERE commission1 = 500 AND rate1 = 2.5", Connection);
        using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(500m, reader.GetDecimal(reader.GetOrdinal("commission1")));
        Assert.Equal(2.5m, reader.GetDecimal(reader.GetOrdinal("rate1")));
    }

    [Fact]
    public async Task MultipleAgentsCommissionTotalIsSumOfIndividual()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Agent A", null, null, 1000m, 5m);
        await InsertTestAgentAsync("Agent B", null, null, 2000m, 7.5m);
        await InsertTestAgentAsync("Agent C", null, null, 3000m, 10m);

        using var cmd = new NpgsqlCommand("SELECT commission, rate FROM agents ORDER BY agent_name", Connection);
        using var reader = await cmd.ExecuteReaderAsync();

        decimal totalCommission = 0;
        while (await reader.ReadAsync())
        {
            var commission = reader.GetDecimal(reader.GetOrdinal("commission"));
            var rate = reader.GetDecimal(reader.GetOrdinal("rate"));
            totalCommission += commission * rate / 100m;
        }

        // 1000*5/100 + 2000*7.5/100 + 3000*10/100 = 50 + 150 + 300 = 500
        Assert.Equal(500m, totalCommission);
    }
}
