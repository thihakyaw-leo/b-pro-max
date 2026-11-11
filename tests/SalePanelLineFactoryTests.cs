using BProMax.Models;
using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

public class SalePanelLineFactoryTests
{
    private readonly SalePanelLineFactory _factory = new(
        new NumberResolutionService(),
        new SaleItemAmountCalculator());

    [Fact]
    public void DirectLineShowsTheBaseNumber()
    {
        var request = new SaleEntryItemRequest
        {
            BetTypeCode = "DIRECT",
            Selection = "45",
            Amount = 1_000m
        };

        var success = _factory.TryCreate(
            request,
            SeedBetType("DIRECT"),
            null,
            out var line,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal("(base)", line.Label);
        Assert.Equal("45", line.NumbersText);
        Assert.Equal(1_000m, line.Amount);
        Assert.Equal("1,000.00 each", line.PerNumberText);
        Assert.Equal(80_000m, line.MaxPayout);
    }

    [Fact]
    public void RLineShowsBothOrientationsAndSplitStakes()
    {
        var request = new SaleEntryItemRequest
        {
            BetTypeCode = "R",
            Selection = "45",
            Amount = 1_000m,
            IncludeReverse = true
        };

        var success = _factory.TryCreate(
            request,
            SeedBetType("R"),
            null,
            out var line,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal("R", line.Label);
        Assert.Equal("45, 54", line.NumbersText);
        Assert.Equal("500.00 each", line.PerNumberText);
        Assert.Equal(40_000m, line.MaxPayout);
    }

    [Fact]
    public void ApaLineShowsNineteenNumbers()
    {
        var request = new SaleEntryItemRequest
        {
            BetTypeCode = "APA",
            Selection = "5",
            Amount = 1_900m
        };

        var success = _factory.TryCreate(
            request,
            SeedBetType("APA"),
            null,
            out var line,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal("ပါ", line.Label);
        Assert.Equal(19, line.NumbersText.Split(',').Length);
        Assert.Equal("100.00 each", line.PerNumberText);
        Assert.Equal(8_000m, line.MaxPayout);
    }

    [Fact]
    public void QuickAddGroupLineShowsTheReverseNumbers()
    {
        var group = TwoDSeedData.NumberGroups.Single(item => item.Code == "NAKKHAT");
        var request = new SaleEntryItemRequest
        {
            BetTypeCode = "NAKKHAT",
            NumberGroupCode = "NAKKHAT",
            Amount = 1_000m,
            IncludeReverse = true
        };

        var success = _factory.TryCreate(
            request,
            SeedBetType("NAKKHAT"),
            group,
            out var line,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal("နက္ခတ်", line.Label);
        Assert.Equal(10, line.NumbersText.Split(',').Length);
        Assert.Equal("100.00 each", line.PerNumberText);
        Assert.Equal(8_000m, line.MaxPayout);
    }

    [Fact]
    public void ParityLineShowsTheRemainderRange()
    {
        var request = new SaleEntryItemRequest
        {
            BetTypeCode = "EVEN_EVEN",
            Amount = 1_000m
        };

        var success = _factory.TryCreate(
            request,
            SeedBetType("EVEN_EVEN"),
            null,
            out var line,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal("စုံစုံ", line.Label);
        Assert.Equal(15, line.NumbersText.Split(',').Length);
        Assert.Equal("66.66–66.67", line.PerNumberText);
        Assert.Equal(5_333.60m, line.MaxPayout);
    }

    [Fact]
    public void ExplicitBatchStakesAreAccepted()
    {
        var request = new SaleEntryItemRequest
        {
            BetTypeCode = "DIRECT",
            Selection = "45",
            Amount = 1_000m,
            NumberStakes = [new SaleEntryNumberRequest { Number = "45", Stake = 1_000m }]
        };

        var success = _factory.TryCreate(
            request,
            SeedBetType("DIRECT"),
            null,
            out var line,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(1_000m, line.Amount);
        Assert.Equal(80_000m, line.MaxPayout);
    }

    [Fact]
    public void ExplicitStakesMustSumToTheItemAmount()
    {
        var request = new SaleEntryItemRequest
        {
            BetTypeCode = "DIRECT",
            Selection = "45",
            Amount = 1_000m,
            NumberStakes = [new SaleEntryNumberRequest { Number = "45", Stake = 500m }]
        };

        var success = _factory.TryCreate(
            request,
            SeedBetType("DIRECT"),
            null,
            out _,
            out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void InvalidSelectionIsRejected()
    {
        var request = new SaleEntryItemRequest
        {
            BetTypeCode = "DIRECT",
            Selection = "4",
            Amount = 1_000m
        };

        var success = _factory.TryCreate(
            request,
            SeedBetType("DIRECT"),
            null,
            out _,
            out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    private static BetTypeRecord SeedBetType(string code)
    {
        return TwoDSeedData.BetTypes.Single(type => type.Code == code);
    }
}
