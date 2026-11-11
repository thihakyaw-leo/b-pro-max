using BProMax.Models;
using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

public class SaleItemAmountCalculatorTests
{
    private readonly SaleItemAmountCalculator _calculator = new();

    [Fact]
    public void PerNumberUsesTheAmountForEachNumber()
    {
        var success = _calculator.TryCalculate(
            1_000m,
            BetAmountModes.PerNumber,
            5,
            80m,
            PayoutMode.PerWinningNumber,
            out var result,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(5_000m, result.Total);
        Assert.Equal(80_000m, result.MaxPayout);
        Assert.All(result.Stakes, stake => Assert.Equal(1_000m, stake));
    }

    [Fact]
    public void PerNumberSingleNumberMatchesTheDirectExample()
    {
        var success = _calculator.TryCalculate(
            1_000m,
            BetAmountModes.PerNumber,
            1,
            80m,
            PayoutMode.PerWinningNumber,
            out var result,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(1_000m, result.Total);
        Assert.Equal(80_000m, result.MaxPayout);
        Assert.Equal(1_000m, Assert.Single(result.Stakes));
    }

    [Fact]
    public void TotalSplitDividesAcrossAllNumbers()
    {
        var success = _calculator.TryCalculate(
            1_000m,
            BetAmountModes.TotalSplit,
            5,
            80m,
            PayoutMode.PerWinningNumber,
            out var result,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(1_000m, result.Total);
        Assert.Equal(16_000m, result.MaxPayout);
        Assert.All(result.Stakes, stake => Assert.Equal(200m, stake));
    }

    [Fact]
    public void TotalSplitDistributesTheRemainder()
    {
        var success = _calculator.TryCalculate(
            1_000m,
            BetAmountModes.TotalSplit,
            3,
            80m,
            PayoutMode.PerWinningNumber,
            out var result,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(333.34m, result.Stakes[0]);
        Assert.Equal(333.33m, result.Stakes[1]);
        Assert.Equal(333.33m, result.Stakes[2]);
        Assert.Equal(1_000m, result.Stakes.Sum());
        Assert.Equal(26_667.20m, result.MaxPayout);
    }

    [Fact]
    public void TotalSplitForParitySetsUsesTheLargestStake()
    {
        var success = _calculator.TryCalculate(
            1_000m,
            BetAmountModes.TotalSplit,
            15,
            80m,
            PayoutMode.PerWinningNumber,
            out var result,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(15, result.Stakes.Count);
        Assert.Equal(1_000m, result.Stakes.Sum());
        Assert.Equal(66.67m, result.Stakes[0]);
        Assert.Equal(66.66m, result.Stakes[14]);
        Assert.Equal(5_333.60m, result.MaxPayout);
    }

    [Fact]
    public void TotalModeKeepsTheAmountAsOneStake()
    {
        var success = _calculator.TryCalculate(
            1_000m,
            BetAmountModes.Total,
            15,
            80m,
            PayoutMode.TotalIfHit,
            out var result,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(1_000m, result.Total);
        Assert.Equal(80_000m, result.MaxPayout);
        Assert.All(result.Stakes, stake => Assert.Equal(0m, stake));
    }

    [Fact]
    public void TotalIfHitUsesTheWholeItemAmount()
    {
        var success = _calculator.TryCalculate(
            1_000m,
            BetAmountModes.PerNumber,
            5,
            80m,
            PayoutMode.TotalIfHit,
            out var result,
            out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(5_000m, result.Total);
        Assert.Equal(400_000m, result.MaxPayout);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsNonPositiveAmount(decimal amount)
    {
        var success = _calculator.TryCalculate(
            amount,
            BetAmountModes.TotalSplit,
            5,
            80m,
            PayoutMode.PerWinningNumber,
            out _,
            out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void RejectsAmountWithMoreThanTwoDecimals()
    {
        var success = _calculator.TryCalculate(
            1_000.001m,
            BetAmountModes.TotalSplit,
            5,
            80m,
            PayoutMode.PerWinningNumber,
            out _,
            out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void RejectsZeroNumbers()
    {
        var success = _calculator.TryCalculate(
            1_000m,
            BetAmountModes.TotalSplit,
            0,
            80m,
            PayoutMode.PerWinningNumber,
            out _,
            out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void RejectsUnknownAmountMode()
    {
        var success = _calculator.TryCalculate(
            1_000m,
            "Unknown",
            5,
            80m,
            PayoutMode.PerWinningNumber,
            out _,
            out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }
}
