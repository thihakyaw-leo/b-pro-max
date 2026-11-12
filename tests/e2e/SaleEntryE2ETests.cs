namespace BProMax.E2ETests;

using System.Collections.Generic;
using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

/// <summary>
/// E2E tests for sale entry (SC-05): single bet type, batch/Quick Add with
/// pre-resolved number stakes, and auto-buy trigger integration.
/// Mirrors DD.exe sale panel workflow.
/// </summary>
[Collection("E2E Database")]
public class SaleEntryE2ETests : E2ETestBase
{
    private static readonly string[] QuickAddMembers = { "11", "22", "33" };
    [Fact]
    public async Task SingleSaleEntryCreatesSaleItemsAndNumbers()
    {
        if (SkipIfUnavailable()) return;
        await CleanSaleTablesAsync();

        var agentId = await InsertTestAgentAsync("Sale Agent", commission: 0, rate: 5m, rate1: 2.5m);
        var drawId = await InsertTestDrawAsync();
        var betTypeId = await InsertTestBetTypeAsync("EXACT", "Exact", "Exact",
            ruleType: NumberRuleTypes.Exact);

        var saleEntryService = new SaleEntryService(
            CreateDatabaseService(),
            new NumberResolutionService(),
            new SaleItemAmountCalculator(),
            new CommissionService());

        var result = await saleEntryService.CreateSaleAsync(new SaleEntryRequest
        {
            AgentId = agentId,
            DrawId = drawId,
            CreatedBy = 1,
            CurrencyType = "THB",
            CurrencyRate = 1m,
            Items = new[]
            {
                new SaleEntryItemRequest
                {
                    BetTypeCode = "EXACT",
                    Amount = 1000m,
                    Selection = "42",
                    IncludeReverse = false
                }
            }
        });

        Assert.True(result.SaleId > 0);
        Assert.Equal(1000m, result.TotalStake);
        Assert.Equal(50m, result.Commission);      // 1000 * 5 / 100
        Assert.Equal(25m, result.Commission1);     // 1000 * 2.5 / 100
        Assert.Single(result.Items);

        var item = result.Items[0];
        Assert.Equal("EXACT", item.BetTypeCode);
        Assert.Equal(1000m, item.Amount);
        Assert.Single(item.Numbers);
        Assert.Equal("42", item.Numbers[0].Number);
        Assert.Equal(1000m, item.Numbers[0].Stake);
        Assert.False(item.Numbers[0].IsReverse);
    }

    [Fact]
    public async Task BatchSaleEntryWithNumberStakesUsesPreResolvedStakes()
    {
        if (SkipIfUnavailable()) return;
        await CleanSaleTablesAsync();

        var agentId = await InsertTestAgentAsync("Batch Agent", commission: 0, rate: 5m);
        var drawId = await InsertTestDrawAsync();
        var betTypeId = await InsertTestBetTypeAsync("EXACT", "Exact", "Exact",
            ruleType: NumberRuleTypes.Exact);

        var saleEntryService = new SaleEntryService(
            CreateDatabaseService(),
            new NumberResolutionService(),
            new SaleItemAmountCalculator(),
            new CommissionService());

        var result = await saleEntryService.CreateSaleAsync(new SaleEntryRequest
        {
            AgentId = agentId,
            DrawId = drawId,
            Items = new[]
            {
                new SaleEntryItemRequest
                {
                    BetTypeCode = "EXACT",
                    Amount = 3000m,
                    Selection = "42",
                    IncludeReverse = false,
                    NumberStakes = new List<SaleEntryNumberRequest>
                    {
                        new() { Number = "42", Stake = 1000m, IsReverse = false },
                        new() { Number = "42", Stake = 2000m, IsReverse = false }
                    }
                }
            }
        });

        Assert.True(result.SaleId > 0);
        Assert.Equal(3000m, result.TotalStake);

        var item = Assert.Single(result.Items);
        Assert.Equal(3000m, item.Amount);

        // Stakes from NumberStakes are summed per number
        var number = Assert.Single(item.Numbers);
        Assert.Equal("42", number.Number);
        Assert.Equal(3000m, number.Stake); // 1000 + 2000
        Assert.Equal(80 * 3000m, item.MaxPayout); // stake * multiplier
    }

    [Fact]
    public async Task QuickAddNumberGroupResolvesFixedGroupMembers()
    {
        if (SkipIfUnavailable()) return;
        await CleanSaleTablesAsync();

        var agentId = await InsertTestAgentAsync("Quick Add Agent", commission: 0, rate: 5m);
        var drawId = await InsertTestDrawAsync();
        var betTypeId = await InsertTestBetTypeAsync("FIXED", "Fixed Group", "Fixed Group",
            ruleType: NumberRuleTypes.FixedGroup,
            ruleConfig: "{\"Numbers\":[\"11\",\"22\",\"33\"]}");
        var groupId = await InsertTestNumberGroupAsync("GRP1", "Group 1", "Group 1",
            allowReverse: true, members: QuickAddMembers);

        var saleEntryService = new SaleEntryService(
            CreateDatabaseService(),
            new NumberResolutionService(),
            new SaleItemAmountCalculator(),
            new CommissionService());

        var result = await saleEntryService.CreateSaleAsync(new SaleEntryRequest
        {
            AgentId = agentId,
            DrawId = drawId,
            Items = new[]
            {
                new SaleEntryItemRequest
                {
                    BetTypeCode = "FIXED",
                    Amount = 6000m,
                    NumberGroupCode = "GRP1",
                    IncludeReverse = false
                }
            }
        });

        Assert.True(result.SaleId > 0);

        // TotalSplit: 6000 / 3 numbers = 2000 each
        var item = Assert.Single(result.Items);
        Assert.Equal(3, item.Numbers.Count);
        foreach (var number in item.Numbers)
        {
            Assert.Equal(2000m, number.Stake);
        }
    }

    [Fact]
    public async Task SaleEntryRequiresOpenDraw()
    {
        if (SkipIfUnavailable()) return;
        await CleanSaleTablesAsync();

        var agentId = await InsertTestAgentAsync("Closed Draw Agent", commission: 0, rate: 5m);
        var drawId = await InsertTestDrawAsync(status: DrawStatuses.Closed);
        var betTypeId = await InsertTestBetTypeAsync("EXACT", "Exact", "Exact",
            ruleType: NumberRuleTypes.Exact);

        var saleEntryService = new SaleEntryService(
            CreateDatabaseService(),
            new NumberResolutionService(),
            new SaleItemAmountCalculator(),
            new CommissionService());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            saleEntryService.CreateSaleAsync(new SaleEntryRequest
            {
                AgentId = agentId,
                DrawId = drawId,
                Items = new[]
                {
                    new SaleEntryItemRequest
                    {
                        BetTypeCode = "EXACT",
                        Amount = 1000m,
                        Selection = "42"
                    }
                }
            }));
    }

    [Fact]
    public async Task AutoBuyTriggeredWhenStakeExceedsRuleThreshold()
    {
        if (SkipIfUnavailable()) return;
        await CleanSaleTablesAsync();
        await CleanAutoBuyRulesTableAsync();

        var agentId = await InsertTestAgentAsync("AutoBuy Agent", commission: 0, rate: 5m);
        var drawId = await InsertTestDrawAsync();
        var betTypeId = await InsertTestBetTypeAsync("EXACT", "Exact", "Exact",
            ruleType: NumberRuleTypes.Exact);

        // Create an auto-buy rule for this agent with trigger at 5000
        var autoBuyService = new AutoBuyService(CreateDatabaseService(), NullLogger<AutoBuyService>.Instance);
        await autoBuyService.CreateRuleAsync(new AutoBuyRule
        {
            AgentName = "AutoBuy Agent",
            TriggerAmount = 5000m,
            AutoBuyAmount = 2000m,
            IsEnabled = true
        });

        var saleEntryService = new SaleEntryService(
            CreateDatabaseService(),
            new NumberResolutionService(),
            new SaleItemAmountCalculator(),
            new CommissionService());

        var result = await saleEntryService.CreateSaleAsync(new SaleEntryRequest
        {
            AgentId = agentId,
            DrawId = drawId,
            Items = new[]
            {
                new SaleEntryItemRequest
                {
                    BetTypeCode = "EXACT",
                    Amount = 8000m,
                    Selection = "42"
                }
            }
        });

        Assert.True(result.AutoBuyTriggered);
        Assert.Equal(2000m, result.AutoBuyAmount);
    }

    private async Task CleanSaleTablesAsync()
    {
        if (!IsAvailable) return;
        using var cmd = new NpgsqlCommand("DELETE FROM sale_item_numbers; DELETE FROM sale_items; DELETE FROM sales;", Connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task CleanAutoBuyRulesTableAsync()
    {
        if (!IsAvailable) return;
        using var cmd = new NpgsqlCommand("DELETE FROM auto_buy_rules", Connection);
        await cmd.ExecuteNonQueryAsync();
    }
}
