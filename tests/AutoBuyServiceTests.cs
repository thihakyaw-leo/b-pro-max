using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Xunit;

namespace BProMax.Tests;

public class AutoBuyServiceTests
{
    private readonly Mock<IDatabaseService> _mockDb;
    private readonly AutoBuyService _autoBuyService;

    public AutoBuyServiceTests()
    {
        _mockDb = new Mock<IDatabaseService>();
        _autoBuyService = new AutoBuyService(_mockDb.Object, new TestLogger<AutoBuyService>());
    }

    private static AutoBuyRule CreateTestRule(
        int id = 1,
        string agentName = "John Doe",
        decimal triggerAmount = 1000m,
        decimal autoBuyAmount = 500m,
        bool isEnabled = true)
    {
        return new AutoBuyRule
        {
            Id = id,
            AgentName = agentName,
            TriggerAmount = triggerAmount,
            AutoBuyAmount = autoBuyAmount,
            IsEnabled = isEnabled,
            CreatedAt = DateTime.UtcNow,
            ExecutionCount = 0
        };
    }

    [Fact]
    public async Task GetAllRulesAsyncReturnsListFromDatabase()
    {
        // Arrange
        var expected = new List<AutoBuyRule>
        {
            CreateTestRule(1, "John Doe", 1000m, 500m, true),
            CreateTestRule(2, "Jane Smith", 2000m, 1000m, false)
        };
        _mockDb
            .Setup(x => x.QueryListAsync(
                It.IsAny<string>(),
                It.IsAny<Func<NpgsqlDataReader, AutoBuyRule>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _autoBuyService.GetAllRulesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("John Doe", result[0].AgentName);
        Assert.Equal("Jane Smith", result[1].AgentName);
    }

    [Fact]
    public async Task GetRuleByAgentAsyncFoundReturnsRule()
    {
        // Arrange
        var expected = CreateTestRule(1, "John Doe", 1000m, 500m, true);
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("LOWER(agent_name)")),
                It.IsAny<Func<NpgsqlDataReader, AutoBuyRule>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _autoBuyService.GetRuleByAgentAsync("John Doe");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.AgentName);
    }

    [Fact]
    public async Task GetRuleByAgentAsyncNotFoundReturnsNull()
    {
        // Arrange
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.IsAny<string>(),
                It.IsAny<Func<NpgsqlDataReader, AutoBuyRule>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync((AutoBuyRule?)null);

        // Act
        var result = await _autoBuyService.GetRuleByAgentAsync("Unknown");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateRuleAsyncCallsDatabase()
    {
        // Arrange
        var rule = CreateTestRule(0, "New Agent", 1000m, 500m, true);
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("LOWER(agent_name)")),
                It.IsAny<Func<NpgsqlDataReader, AutoBuyRule>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync((AutoBuyRule?)null);
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("RETURNING id")),
                It.IsAny<Func<NpgsqlDataReader, int>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        // Act
        var id = await _autoBuyService.CreateRuleAsync(rule);

        // Assert
        Assert.Equal(1, id);
    }

    [Fact]
    public async Task CreateRuleAsyncDuplicateAgentThrows()
    {
        // Arrange
        var rule = CreateTestRule(0, "John Doe", 1000m, 500m, true);
        var existing = CreateTestRule(1, "John Doe", 1000m, 500m, true);
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("LOWER(agent_name)")),
                It.IsAny<Func<NpgsqlDataReader, AutoBuyRule>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(existing);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _autoBuyService.CreateRuleAsync(rule));
    }

    [Fact]
    public async Task UpdateRuleAsyncMissingRuleReturnsZero()
    {
        // Arrange
        var rule = CreateTestRule(999, "Unknown", 1000m, 500m, true);
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("WHERE id = @id")),
                It.IsAny<Func<NpgsqlDataReader, AutoBuyRule>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync((AutoBuyRule?)null);

        // Act
        var result = await _autoBuyService.UpdateRuleAsync(rule);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task DeleteRuleAsyncCallsDatabase()
    {
        // Arrange
        _mockDb
            .Setup(x => x.ExecuteNonQueryAsync(
                It.Is<string>(s => s.Contains("DELETE FROM auto_buy_rules")),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        // Act
        var result = await _autoBuyService.DeleteRuleAsync(1);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ToggleRuleAsyncCallsDatabase()
    {
        // Arrange
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("NOT is_enabled")),
                It.IsAny<Func<NpgsqlDataReader, bool>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(true);

        // Act
        var result = await _autoBuyService.ToggleRuleAsync(1);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task IncrementExecutionCountAsyncCallsDatabase()
    {
        // Arrange
        _mockDb
            .Setup(x => x.ExecuteNonQueryAsync(
                It.Is<string>(s => s.Contains("execution_count = execution_count + 1")),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        // Act
        var result = await _autoBuyService.IncrementExecutionCountAsync(1);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void CheckTriggerEnabledRuleAboveThresholdReturnsTriggered()
    {
        // Arrange
        var rules = new List<AutoBuyRule>
        {
            CreateTestRule(1, "John Doe", 1000m, 500m, true)
        };
        _mockDb
            .Setup(x => x.QueryListAsync(
                It.IsAny<string>(),
                It.IsAny<Func<NpgsqlDataReader, AutoBuyRule>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(rules);

        // Act
        var (triggered, rule) = _autoBuyService.CheckTrigger("John Doe", 1500m);

        // Assert
        Assert.True(triggered);
        Assert.Equal("John Doe", rule.AgentName);
    }

    [Fact]
    public void CheckTriggerDisabledRuleReturnsNotTriggered()
    {
        // Arrange
        var rules = new List<AutoBuyRule>
        {
            CreateTestRule(1, "John Doe", 1000m, 500m, false)
        };
        _mockDb
            .Setup(x => x.QueryListAsync(
                It.IsAny<string>(),
                It.IsAny<Func<NpgsqlDataReader, AutoBuyRule>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(rules);

        // Act
        var (triggered, rule) = _autoBuyService.CheckTrigger("John Doe", 1500m);

        // Assert
        Assert.False(triggered);
        Assert.Null(rule);
    }

    [Fact]
    public void CheckTriggerBelowThresholdReturnsNotTriggered()
    {
        // Arrange
        var rules = new List<AutoBuyRule>
        {
            CreateTestRule(1, "John Doe", 1000m, 500m, true)
        };
        _mockDb
            .Setup(x => x.QueryListAsync(
                It.IsAny<string>(),
                It.IsAny<Func<NpgsqlDataReader, AutoBuyRule>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(rules);

        // Act
        var (triggered, rule) = _autoBuyService.CheckTrigger("John Doe", 500m);

        // Assert
        Assert.False(triggered);
        Assert.NotNull(rule);
        Assert.Equal("John Doe", rule.AgentName);
    }

    [Fact]
    public void GetEnabledRulesReturnsOnlyEnabledRules()
    {
        // Arrange
        var rules = new List<AutoBuyRule>
        {
            CreateTestRule(1, "John Doe", 1000m, 500m, true),
            CreateTestRule(2, "Jane Smith", 2000m, 1000m, false)
        };
        _mockDb
            .Setup(x => x.QueryListAsync(
                It.IsAny<string>(),
                It.IsAny<Func<NpgsqlDataReader, AutoBuyRule>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(rules);

        // Act
        var result = _autoBuyService.GetEnabledRules();

        // Assert
        Assert.Single(result);
        Assert.Equal("John Doe", result[0].AgentName);
    }
}
