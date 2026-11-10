using Npgsql;
using Xunit;

namespace BProMax.E2ETests;

/// <summary>
/// E2E tests for agent CRUD against a real PostgreSQL database.
/// Mirrors DD.exe AgentThai table operations:
///   SELECT * FROM agents
///   SELECT * FROM agents WHERE agent_name = @name
///   SELECT * FROM agents ORDER BY agent_name
///   DELETE FROM agents WHERE agent_name = @name
/// </summary>
[Collection("E2E Database")]
public class AgentCrudE2ETests : E2ETestBase
{
    [Fact]
    public async Task CreateReadUpdateDeleteAgentMatchesDDExeBehavior()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        // CREATE — Insert agent (DD.exe: Insert Into AgentThai values (...))
        var id = await InsertTestAgentAsync("John Doe", "123 Main Street, Bangkok", "02-123-4567", 1000m, 5m, 500m, 2.5m, "THB", 1m);
        Assert.True(id > 0);

        // READ ALL — DD.exe: Select * From AgentThai Order By AgentName
        using var cmd1 = new NpgsqlCommand("SELECT * FROM agents ORDER BY agent_name", Connection);
        using var reader1 = await cmd1.ExecuteReaderAsync();
        Assert.True(await reader1.ReadAsync());
        Assert.Equal("John Doe", reader1.GetString(reader1.GetOrdinal("agent_name")));
        Assert.Equal("123 Main Street, Bangkok", reader1.GetString(reader1.GetOrdinal("address")));
        Assert.Equal("02-123-4567", reader1.GetString(reader1.GetOrdinal("phone")));
        Assert.Equal(1000m, reader1.GetDecimal(reader1.GetOrdinal("commission")));
        Assert.Equal(5m, reader1.GetDecimal(reader1.GetOrdinal("rate")));

        // READ BY NAME — DD.exe: Select * From AgentThai Where AgentName='...'
        using var cmd2 = new NpgsqlCommand("SELECT * FROM agents WHERE agent_name = @name", Connection);
        cmd2.Parameters.AddWithValue("@name", "John Doe");
        using var reader2 = await cmd2.ExecuteReaderAsync();
        Assert.True(await reader2.ReadAsync());
        Assert.Equal(id, reader2.GetInt32(reader2.GetOrdinal("id")));

        // UPDATE — DD.exe: Update AgentThai Set ...
        using var cmd3 = new NpgsqlCommand("UPDATE agents SET commission = @commission WHERE id = @id", Connection);
        cmd3.Parameters.AddWithValue("@commission", 1500m);
        cmd3.Parameters.AddWithValue("@id", id);
        var affected = await cmd3.ExecuteNonQueryAsync();
        Assert.Equal(1, affected);

        // Verify update
        using var cmd4 = new NpgsqlCommand("SELECT commission FROM agents WHERE id = @id", Connection);
        cmd4.Parameters.AddWithValue("@id", id);
        var updatedCommission = (decimal)((await cmd4.ExecuteScalarAsync())!);
        Assert.Equal(1500m, updatedCommission);

        // DELETE BY NAME — DD.exe: Delete From AgentThai Where AgentName='...'
        using var cmd5 = new NpgsqlCommand("DELETE FROM agents WHERE agent_name = @name", Connection);
        cmd5.Parameters.AddWithValue("@name", "John Doe");
        var deleted = await cmd5.ExecuteNonQueryAsync();
        Assert.Equal(1, deleted);

        // Verify deletion
        using var cmd6 = new NpgsqlCommand("SELECT COUNT(*) FROM agents WHERE agent_name = @name", Connection);
        cmd6.Parameters.AddWithValue("@name", "John Doe");
        var count = (long)((await cmd6.ExecuteScalarAsync())!);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetAllAgentsReturnsSortedByName()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Zara Smith", null, null, 0, 0);
        await InsertTestAgentAsync("Alice Brown", null, null, 0, 0);
        await InsertTestAgentAsync("John Doe", null, null, 0, 0);

        using var cmd = new NpgsqlCommand("SELECT agent_name FROM agents ORDER BY agent_name", Connection);
        using var reader = await cmd.ExecuteReaderAsync();

        var names = new List<string>();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));

        Assert.Equal(3, names.Count);
        Assert.Equal("Alice Brown", names[0]);
        Assert.Equal("John Doe", names[1]);
        Assert.Equal("Zara Smith", names[2]);
    }

    [Fact]
    public async Task SearchAgentsByNameReturnsMatchingResults()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("John Doe", null, null);
        await InsertTestAgentAsync("Johnny Appleseed", null, null);
        await InsertTestAgentAsync("Jane Smith", null, null);

        using var cmd = new NpgsqlCommand("SELECT agent_name FROM agents WHERE agent_name ILIKE @search ORDER BY agent_name", Connection);
        cmd.Parameters.AddWithValue("@search", "%John%");
        using var reader = await cmd.ExecuteReaderAsync();

        var names = new List<string>();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));

        Assert.Equal(2, names.Count);
        Assert.Contains("John Doe", names);
        Assert.Contains("Johnny Appleseed", names);
        Assert.DoesNotContain("Jane Smith", names);
    }

    [Fact]
    public async Task DeleteAgentByIdReturnsAffectedRowCount()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        var id = await InsertTestAgentAsync("Temp Agent", null, null);

        using var cmd = new NpgsqlCommand("DELETE FROM agents WHERE id = @id", Connection);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        Assert.Equal(1, affected);

        using var cmd2 = new NpgsqlCommand("SELECT COUNT(*) FROM agents", Connection);
        var count = (long)((await cmd2.ExecuteScalarAsync())!);
        Assert.Equal(0, count);
    }
}
