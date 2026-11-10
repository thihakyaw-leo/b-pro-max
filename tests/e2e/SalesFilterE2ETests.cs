using Npgsql;
using Xunit;

namespace BProMax.E2ETests;

/// <summary>
/// E2E tests for sales filtering against a real PostgreSQL database.
/// Mirrors DD.exe sales filter queries (agent name, date range, currency).
/// </summary>
[Collection("E2E Database")]
public class SalesFilterE2ETests : E2ETestBase
{
    [Fact]
    public async Task FilterAgentsByNameReturnsMatchingAgents()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Alpha Agent", null, null);
        await InsertTestAgentAsync("Beta Agent", null, null);
        await InsertTestAgentAsync("Alpha Beta", null, null);

        using var cmd = new NpgsqlCommand(
            "SELECT agent_name FROM agents WHERE agent_name ILIKE @pattern ORDER BY agent_name", Connection);
        cmd.Parameters.AddWithValue("@pattern", "%Alpha%");
        using var reader = await cmd.ExecuteReaderAsync();

        var names = new List<string>();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));

        Assert.Equal(2, names.Count);
        Assert.Contains("Alpha Agent", names);
        Assert.Contains("Alpha Beta", names);
        Assert.DoesNotContain("Beta Agent", names);
    }

    [Fact]
    public async Task FilterAgentsByCurrencyTypeReturnsMatchingAgents()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("THB Agent", null, null, currencyType: "THB", currencyRate: 1m);
        await InsertTestAgentAsync("USD Agent", null, null, currencyType: "USD", currencyRate: 35.5m);
        await InsertTestAgentAsync("THB Agent 2", null, null, currencyType: "THB", currencyRate: 1m);

        using var cmd = new NpgsqlCommand(
            "SELECT agent_name FROM agents WHERE currency_type = @currency ORDER BY agent_name", Connection);
        cmd.Parameters.AddWithValue("@currency", "THB");
        using var reader = await cmd.ExecuteReaderAsync();

        var names = new List<string>();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));

        Assert.Equal(2, names.Count);
        Assert.All(names, n => Assert.Contains("THB", n));
    }

    [Fact]
    public async Task FilterAgentsByCommissionRangeReturnsMatchingAgents()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Low Commission", null, null, 100m, 0);
        await InsertTestAgentAsync("Medium Commission", null, null, 1000m, 0);
        await InsertTestAgentAsync("High Commission", null, null, 5000m, 0);

        using var cmd = new NpgsqlCommand(
            "SELECT agent_name FROM agents WHERE commission >= @min AND commission <= @max ORDER BY commission", Connection);
        cmd.Parameters.AddWithValue("@min", 500m);
        cmd.Parameters.AddWithValue("@max", 2000m);
        using var reader = await cmd.ExecuteReaderAsync();

        var names = new List<string>();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));

        Assert.Single(names);
        Assert.Equal("Medium Commission", names[0]);
    }

    [Fact]
    public async Task FilterAgentsByPhoneNumberReturnsMatchingAgents()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Agent One", null, "02-123-4567");
        await InsertTestAgentAsync("Agent Two", null, "02-765-4321");
        await InsertTestAgentAsync("Agent Three", null, "08-123-4567");

        using var cmd = new NpgsqlCommand(
            "SELECT agent_name FROM agents WHERE phone LIKE @pattern ORDER BY agent_name", Connection);
        cmd.Parameters.AddWithValue("@pattern", "02-%");
        using var reader = await cmd.ExecuteReaderAsync();

        var names = new List<string>();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));

        Assert.Equal(2, names.Count);
        Assert.Contains("Agent One", names);
        Assert.Contains("Agent Two", names);
        Assert.DoesNotContain("Agent Three", names);
    }
}
