namespace BProMax.E2ETests;

using BProMax.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

/// <summary>
/// E2E tests for auto-buy rules (SC-07): rule creation, trigger evaluation,
/// enabled/disabled toggle, and execution-count tracking.
/// Mirrors DD.exe auto-purchase behavior.
/// </summary>
[Collection("E2E Database")]
public class AutoBuyE2ETests : E2ETestBase
{
    [Fact]
    public async Task CreateGetUpdateDeleteRuleMatchesServiceBehavior()
    {
        if (SkipIfUnavailable()) return;
        await CleanAutoBuyRulesTableAsync();

        var autoBuyService = new AutoBuyService(CreateDatabaseService(), NullLogger<AutoBuyService>.Instance);

        // CREATE
        var rule = new AutoBuyRule
        {
            AgentName = "John Doe",
            TriggerAmount = 10000m,
            AutoBuyAmount = 5000m,
            IsEnabled = true
        };
        var id = await autoBuyService.CreateRuleAsync(rule);
        Assert.True(id > 0);
        rule.Id = id;

        // GET BY ID
        var fetched = await autoBuyService.GetRuleByIdAsync(id);
        Assert.NotNull(fetched);
        Assert.Equal("John Doe", fetched.AgentName);
        Assert.Equal(10000m, fetched.TriggerAmount);
        Assert.Equal(5000m, fetched.AutoBuyAmount);
        Assert.True(fetched.IsEnabled);

        // GET BY AGENT NAME (case-insensitive)
        var byAgent = await autoBuyService.GetRuleByAgentAsync("john doe");
        Assert.NotNull(byAgent);
        Assert.Equal(id, byAgent.Id);

        // DUPLICATE AGENT NAME throws
        var duplicate = new AutoBuyRule
        {
            AgentName = "John Doe",
            TriggerAmount = 20000m,
            AutoBuyAmount = 10000m,
            IsEnabled = true
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            autoBuyService.CreateRuleAsync(duplicate));

        // UPDATE
        rule.TriggerAmount = 15000m;
        var updated = await autoBuyService.UpdateRuleAsync(rule);
        Assert.Equal(1, updated);
        var fetchedAgain = await autoBuyService.GetRuleByIdAsync(id);
        Assert.NotNull(fetchedAgain);
        Assert.Equal(15000m, fetchedAgain.TriggerAmount);

        // TOGGLE
        var toggled = await autoBuyService.ToggleRuleAsync(id);
        Assert.Equal(1, toggled);
        var disabled = await autoBuyService.GetRuleByIdAsync(id);
        Assert.NotNull(disabled);
        Assert.False(disabled.IsEnabled);

        // GET ALL
        var allRules = await autoBuyService.GetAllRulesAsync();
        Assert.Single(allRules);

        // DELETE
        var deleted = await autoBuyService.DeleteRuleAsync(id);
        Assert.Equal(1, deleted);
        var gone = await autoBuyService.GetRuleByIdAsync(id);
        Assert.Null(gone);
    }

    [Fact]
    public async Task CheckTriggerEnabledRuleTriggersWhenAmountExceedsThreshold()
    {
        if (SkipIfUnavailable()) return;
        await CleanAutoBuyRulesTableAsync();

        var autoBuyService = new AutoBuyService(CreateDatabaseService(), NullLogger<AutoBuyService>.Instance);

        var rule = new AutoBuyRule
        {
            AgentName = "Alice",
            TriggerAmount = 10000m,
            AutoBuyAmount = 5000m,
            IsEnabled = true
        };
        await autoBuyService.CreateRuleAsync(rule);

        // Amount >= trigger → triggered
        var (triggered, matchedRule) = await autoBuyService.CheckTriggerAsync("alice", 10000m);
        Assert.True(triggered);
        Assert.Equal("Alice", matchedRule.AgentName);

        // Amount < trigger → not triggered
        var (notTriggered, _) = await autoBuyService.CheckTriggerAsync("alice", 9999m);
        Assert.False(notTriggered);

        // Disabled rule → not triggered
        var ruleForAlice = await autoBuyService.GetRuleByAgentAsync("Alice");
        Assert.NotNull(ruleForAlice);
        await autoBuyService.ToggleRuleAsync(ruleForAlice.Id);
        var (disabledTriggered, _) = await autoBuyService.CheckTriggerAsync("alice", 50000m);
        Assert.False(disabledTriggered);

        // Unknown agent → not triggered
        var (unknownTriggered, _) = await autoBuyService.CheckTriggerAsync("Nobody", 50000m);
        Assert.False(unknownTriggered);
    }

    [Fact]
    public async Task IncrementExecutionCountUpdatesCountAndLastExecuted()
    {
        if (SkipIfUnavailable()) return;
        await CleanAutoBuyRulesTableAsync();

        var autoBuyService = new AutoBuyService(CreateDatabaseService(), NullLogger<AutoBuyService>.Instance);

        var rule = new AutoBuyRule
        {
            AgentName = "Bob",
            TriggerAmount = 1000m,
            AutoBuyAmount = 500m,
            IsEnabled = true
        };
        var id = await autoBuyService.CreateRuleAsync(rule);

        var fetched = await autoBuyService.GetRuleByIdAsync(id);
        Assert.NotNull(fetched);
        Assert.Equal(0, fetched.ExecutionCount);
        Assert.Null(fetched.LastExecuted);

        var incremented = await autoBuyService.IncrementExecutionCountAsync(id);
        Assert.Equal(1, incremented);

        fetched = await autoBuyService.GetRuleByIdAsync(id);
        Assert.NotNull(fetched);
        Assert.Equal(1, fetched.ExecutionCount);
        Assert.NotNull(fetched.LastExecuted);
    }

    private async Task CleanAutoBuyRulesTableAsync()
    {
        if (!IsAvailable) return;
        using var cmd = new NpgsqlCommand("DELETE FROM auto_buy_rules", Connection);
        await cmd.ExecuteNonQueryAsync();
    }
}
