using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace BProMax.E2ETests;

/// <summary>
/// End-to-end tests for the complete sell workflow through <see cref="SaleEntryService"/>
/// plus sales-history filtering via <see cref="ISalesFilterService"/>.
/// Mirrors DD.exe sale panel workflow: agent/draw selection → add items → save sale →
/// verify in sales history.
/// </summary>
[Collection("E2E Database")]
public class SellWorkflowE2ETests : E2ETestBase
{
    [Fact]
    public async Task CompleteSellWorkflowSavesSaleAndAppearsInHistory()
    {
        if (SkipIfUnavailable()) return;
        await CleanSaleTablesAsync();
        await CleanAutoBuyRulesTableAsync();

        // --- Setup: agent, draw, bet types, number group, auto-buy rule ---
        var agentId = await InsertTestAgentAsync("Sell Workflow Agent", commission: 5000m, rate: 5m);
        var drawId = await InsertTestDrawAsync();
        await InsertTestBetTypeAsync("EXACT", "Exact", "Exact",
            ruleType: NumberRuleTypes.Exact);
        await InsertTestBetTypeAsync("NAKKHAT", "နက္ခတ်", "နက္ခတ်",
            ruleType: NumberRuleTypes.FixedGroup,
            ruleConfig: "{\"Numbers\":[\"11\",\"22\",\"33\"]}");
        var groupId = await InsertTestNumberGroupAsync("GRP1", "Group 1", "Group 1",
            allowReverse: true, members: new List<string> { "11", "22", "33" });

        var db = CreateDatabaseService();
        var autoBuyService = new AutoBuyService(db, NullLogger<AutoBuyService>.Instance);
        await autoBuyService.CreateRuleAsync(new AutoBuyRule
        {
            AgentName = "Sell Workflow Agent",
            TriggerAmount = 5000m,
            AutoBuyAmount = 2000m,
            IsEnabled = true
        });

        var saleEntryService = new SaleEntryService(
            db,
            new NumberResolutionService(),
            new SaleItemAmountCalculator(),
            new CommissionService());

        // --- Action: create a sale with multiple items ---
        var result = await saleEntryService.CreateSaleAsync(new SaleEntryRequest
        {
            AgentId = agentId,
            DrawId = drawId,
            CreatedBy = 1,
            CurrencyType = "THB",
            CurrencyRate = 1m,
            Items = new[]
            {
                // Single exact bet: amount per number
                new SaleEntryItemRequest
                {
                    BetTypeCode = "EXACT",
                    Amount = 3000m,
                    Selection = "42",
                    IncludeReverse = false
                },
                // Quick Add group: amount split across group members
                new SaleEntryItemRequest
                {
                    BetTypeCode = "NAKKHAT",
                    NumberGroupCode = "GRP1",
                    Amount = 6000m,
                    IncludeReverse = false
                }
            }
        });

        // --- Assert: sale result ---
        Assert.True(result.SaleId > 0, "SaleId should be positive");
        Assert.Equal(9000m, result.TotalStake); // 3000 + 6000
        Assert.Equal(450m, result.Commission);   // 9000 * 5 / 100
        Assert.True(result.AutoBuyTriggered, "Auto-buy should trigger (9000 >= 5000)");
        Assert.Equal(2000m, result.AutoBuyAmount);
        Assert.Equal(2, result.Items.Count);

        // Item 1: EXACT on 42, 3000 total stake
        var item1 = Assert.Single(result.Items, i => i.BetTypeCode == "EXACT");
        Assert.Equal(3000m, item1.Amount);
        Assert.Single(item1.Numbers);
        Assert.Equal("42", item1.Numbers[0].Number);
        Assert.Equal(3000m, item1.Numbers[0].Stake);
        Assert.False(item1.Numbers[0].IsReverse);
        Assert.Equal(80 * 3000m, item1.MaxPayout);

        // Item 2: NAKKHAT group, 6000 split across 3 numbers = 2000 each
        var item2 = Assert.Single(result.Items, i => i.BetTypeCode == "NAKKHAT");
        Assert.Equal(6000m, item2.Amount);
        Assert.Equal(3, item2.Numbers.Count);
        foreach (var number in item2.Numbers)
        {
            Assert.Equal(2000m, number.Stake);
        }
        Assert.Equal(80 * 2000m, item2.MaxPayout);

        // --- Assert: sale appears in sales history ---
        var salesService = new SalesService(db, NullLogger<SalesService>.Instance);
        var filterService = new SalesFilterService(salesService);

        var allSales = await filterService.GetAllSalesAsync();
        var savedSale = Assert.Single(allSales, s => s.Id == result.SaleId);
        Assert.Equal(agentId, savedSale.AgentId);
        Assert.Equal(9000m, savedSale.Amount);
        Assert.Equal(450m, savedSale.Commission);
        Assert.Equal("Pending", savedSale.Status);

        // --- Assert: filtering works ---
        var byAgent = filterService.FilterByAgent(allSales, "Sell Workflow Agent");
        Assert.Single(byAgent);

        var byStatus = filterService.FilterByStatus(allSales, "Pending");
        Assert.Single(byStatus);

        var byAmount = filterService.FilterByAmountRange(allSales, 8000m, 10000m);
        Assert.Single(byAmount);

        var today = DateTime.UtcNow.Date;
        var byDate = filterService.FilterByDateRange(allSales, today, today.AddDays(1));
        Assert.Single(byDate);

        var totals = filterService.CalculateSalesTotals(allSales);
        Assert.Equal(9000m, totals.totalAmount);
        Assert.Equal(450m, totals.totalCommission);
    }

    [Fact]
    public async Task SellWorkflowRejectsClosedDraw()
    {
        if (SkipIfUnavailable()) return;
        await CleanSaleTablesAsync();

        var agentId = await InsertTestAgentAsync("Closed Draw Agent", commission: 0, rate: 5m);
        var drawId = await InsertTestDrawAsync(status: DrawStatuses.Closed);
        await InsertTestBetTypeAsync("EXACT", "Exact", "Exact",
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
    public async Task SellWorkflowBatchWithPreResolvedStakes()
    {
        if (SkipIfUnavailable()) return;
        await CleanSaleTablesAsync();

        var agentId = await InsertTestAgentAsync("Batch Stakes Agent", commission: 0, rate: 5m);
        var drawId = await InsertTestDrawAsync();
        await InsertTestBetTypeAsync("EXACT", "Exact", "Exact",
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
        var number = Assert.Single(item.Numbers);
        Assert.Equal("42", number.Number);
        Assert.Equal(3000m, number.Stake); // 1000 + 2000 summed
        Assert.Equal(80 * 3000m, item.MaxPayout);
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
