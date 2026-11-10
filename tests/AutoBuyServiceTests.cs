using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

public class AutoBuyServiceTests
{
    private readonly AutoBuyService _autoBuyService;

    public AutoBuyServiceTests()
    {
        _autoBuyService = new AutoBuyService();
    }

    [Fact]
    public void CreateRuleAddsAndReturnsId()
    {
        // Arrange
        var rule = new AutoBuyRule
        {
            AgentName = "John Doe",
            TriggerAmount = 1000,
            AutoBuyAmount = 500,
            IsEnabled = true
        };

        // Act
        var id = _autoBuyService.CreateRule(rule);

        // Assert
        Assert.Equal(1, id);
        Assert.Single(_autoBuyService.GetAllRules());
    }

    [Fact]
    public void GetRuleByIdReturnsCorrectRule()
    {
        // Arrange
        var rule = new AutoBuyRule { AgentName = "John Doe", TriggerAmount = 1000, AutoBuyAmount = 500 };
        var id = _autoBuyService.CreateRule(rule);

        // Act
        var result = _autoBuyService.GetRuleById(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result!.AgentName);
    }

    [Fact]
    public void GetRuleByIdInvalidIdReturnsNull()
    {
        // Act
        var result = _autoBuyService.GetRuleById(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRuleByAgentReturnsMatchingRule()
    {
        // Arrange
        _autoBuyService.CreateRule(new AutoBuyRule { AgentName = "John Doe", TriggerAmount = 1000, AutoBuyAmount = 500 });
        _autoBuyService.CreateRule(new AutoBuyRule { AgentName = "Jane Smith", TriggerAmount = 2000, AutoBuyAmount = 1000 });

        // Act
        var result = _autoBuyService.GetRuleByAgent("john doe");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result!.AgentName);
    }

    [Fact]
    public void UpdateRuleUpdatesExistingRule()
    {
        // Arrange
        var id = _autoBuyService.CreateRule(new AutoBuyRule
        {
            AgentName = "John Doe",
            TriggerAmount = 1000,
            AutoBuyAmount = 500,
            IsEnabled = true
        });

        // Act
        var updated = new AutoBuyRule
        {
            Id = id,
            AgentName = "John Doe",
            TriggerAmount = 2000,
            AutoBuyAmount = 1000,
            IsEnabled = false
        };
        var rowsAffected = _autoBuyService.UpdateRule(updated);
        var rule = _autoBuyService.GetRuleById(id);

        // Assert
        Assert.Equal(1, rowsAffected);
        Assert.Equal(2000, rule!.TriggerAmount);
        Assert.Equal(1000, rule.AutoBuyAmount);
        Assert.False(rule.IsEnabled);
    }

    [Fact]
    public void DeleteRuleRemovesRule()
    {
        // Arrange
        var id = _autoBuyService.CreateRule(new AutoBuyRule
        { AgentName = "John Doe", TriggerAmount = 1000, AutoBuyAmount = 500 });

        // Act
        var rowsAffected = _autoBuyService.DeleteRule(id);
        var result = _autoBuyService.GetRuleById(id);

        // Assert
        Assert.Equal(1, rowsAffected);
        Assert.Null(result);
        Assert.Empty(_autoBuyService.GetAllRules());
    }

    [Fact]
    public void ToggleRuleChangesEnabledState()
    {
        // Arrange
        var id = _autoBuyService.CreateRule(new AutoBuyRule
        { AgentName = "John Doe", TriggerAmount = 1000, AutoBuyAmount = 500, IsEnabled = true });

        // Act
        var rowsAffected = _autoBuyService.ToggleRule(id);
        var rule = _autoBuyService.GetRuleById(id);

        // Assert
        Assert.Equal(1, rowsAffected);
        Assert.False(rule!.IsEnabled);
    }

    [Fact]
    public void GetEnabledRulesReturnsOnlyEnabled()
    {
        // Arrange
        _autoBuyService.CreateRule(new AutoBuyRule { AgentName = "John", TriggerAmount = 1000, AutoBuyAmount = 500, IsEnabled = true });
        _autoBuyService.CreateRule(new AutoBuyRule { AgentName = "Jane", TriggerAmount = 2000, AutoBuyAmount = 1000, IsEnabled = false });

        // Act
        var result = _autoBuyService.GetEnabledRules();

        // Assert
        Assert.Single(result);
        Assert.Equal("John", result[0].AgentName);
    }

    [Fact]
    public void CheckTriggerWhenAmountExceedsThresholdReturnsTriggered()
    {
        // Arrange
        _autoBuyService.CreateRule(new AutoBuyRule
        { AgentName = "John Doe", TriggerAmount = 1000, AutoBuyAmount = 500, IsEnabled = true });

        // Act
        var (triggered, rule) = _autoBuyService.CheckTrigger("John Doe", 1500);

        // Assert
        Assert.True(triggered);
        Assert.Equal("John Doe", rule.AgentName);
    }

    [Fact]
    public void CheckTriggerWhenAmountBelowThresholdReturnsNotTriggered()
    {
        // Arrange
        _autoBuyService.CreateRule(new AutoBuyRule
        { AgentName = "John Doe", TriggerAmount = 1000, AutoBuyAmount = 500, IsEnabled = true });

        // Act
        var (triggered, rule) = _autoBuyService.CheckTrigger("John Doe", 500);

        // Assert
        Assert.False(triggered);
    }

    [Fact]
    public void CheckTriggerDisabledRuleReturnsNotTriggered()
    {
        // Arrange
        _autoBuyService.CreateRule(new AutoBuyRule
        { AgentName = "John Doe", TriggerAmount = 1000, AutoBuyAmount = 500, IsEnabled = false });

        // Act
        var (triggered, rule) = _autoBuyService.CheckTrigger("John Doe", 1500);

        // Assert
        Assert.False(triggered);
    }

    [Fact]
    public void IncrementExecutionCountIncrementsAndSetsLastExecuted()
    {
        // Arrange
        var id = _autoBuyService.CreateRule(new AutoBuyRule
        { AgentName = "John Doe", TriggerAmount = 1000, AutoBuyAmount = 500, IsEnabled = true });

        // Act
        var rowsAffected = _autoBuyService.IncrementExecutionCount(id);
        var rule = _autoBuyService.GetRuleById(id);

        // Assert
        Assert.Equal(1, rowsAffected);
        Assert.Equal(1, rule!.ExecutionCount);
        Assert.NotNull(rule.LastExecuted);
    }
}

