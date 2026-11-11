using BProMax.Models;
using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

public class BatchSaleBuilderTests
{
    private readonly BatchSaleBuilder _builder = new(new NumberBatchParser());

    [Fact]
    public void PerBaseNumberWithoutReverse()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "45,39,23,19,40",
            Amount = 1_000m
        };

        var success = _builder.TryBuild(request, out var preview, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(5, preview.Lines.Count);
        Assert.Equal(5_000m, preview.TotalStake);
        Assert.All(preview.Lines, line =>
        {
            Assert.Single(line.Numbers);
            Assert.Equal(1_000m, line.Numbers[0].Stake);
            Assert.Equal(80_000m, line.MaxPayout);
        });
    }

    [Fact]
    public void PerBaseNumberWithReverse()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "45,39,23,19,40",
            Amount = 1_000m,
            IncludeReverse = true
        };

        var success = _builder.TryBuild(request, out var preview, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(5, preview.Lines.Count);
        Assert.Equal(5_000m, preview.TotalStake);
        Assert.All(preview.Lines, line =>
        {
            Assert.Equal(2, line.Numbers.Count);
            Assert.Equal(500m, line.Numbers[0].Stake);
            Assert.Equal(500m, line.Numbers[1].Stake);
            Assert.True(line.Numbers[1].IsReverse);
            Assert.Equal(40_000m, line.MaxPayout);
        });
    }

    [Fact]
    public void TotalSplitWithoutReverse()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "45,39,23,19,40",
            Amount = 1_000m,
            AmountMode = BatchAmountMode.TotalSplit
        };

        var success = _builder.TryBuild(request, out var preview, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(1_000m, preview.TotalStake);
        Assert.All(preview.Lines, line =>
        {
            Assert.Single(line.Numbers);
            Assert.Equal(200m, line.Numbers[0].Stake);
            Assert.Equal(16_000m, line.MaxPayout);
        });
    }

    [Fact]
    public void TotalSplitWithReverse()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "45,39,23,19,40",
            Amount = 1_000m,
            IncludeReverse = true,
            AmountMode = BatchAmountMode.TotalSplit
        };

        var success = _builder.TryBuild(request, out var preview, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(1_000m, preview.TotalStake);
        Assert.All(preview.Lines, line =>
        {
            Assert.Equal(2, line.Numbers.Count);
            Assert.Equal(100m, line.Numbers[0].Stake);
            Assert.Equal(100m, line.Numbers[1].Stake);
            Assert.Equal(8_000m, line.MaxPayout);
        });
    }

    [Fact]
    public void ReverseOfDoubleDoesNotCreateASecondNumber()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "55",
            Amount = 1_000m,
            IncludeReverse = true
        };

        var success = _builder.TryBuild(request, out var preview, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        var line = Assert.Single(preview.Lines);
        Assert.Single(line.Numbers);
        Assert.Equal("55", line.Numbers[0].Number);
        Assert.Equal(1_000m, line.Numbers[0].Stake);
        Assert.Equal(80_000m, line.MaxPayout);
    }

    [Fact]
    public void ReversePairIsMergedWhenBothOrientationsAreEntered()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "45,54",
            Amount = 1_000m,
            IncludeReverse = true
        };

        var success = _builder.TryBuild(request, out var preview, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        var line = Assert.Single(preview.Lines);
        Assert.Equal(2_000m, line.Amount);
        Assert.Equal(2, line.Numbers.Count);
        Assert.Equal(1_000m, line.Numbers[0].Stake);
        Assert.Equal(1_000m, line.Numbers[1].Stake);
        Assert.Equal(80_000m, line.MaxPayout);
        Assert.Single(preview.Warnings);
    }

    [Fact]
    public void DuplicateDirectNumberIsMerged()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "45,45",
            Amount = 1_000m
        };

        var success = _builder.TryBuild(request, out var preview, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        var line = Assert.Single(preview.Lines);
        Assert.Equal(2_000m, line.Amount);
        Assert.Equal(2_000m, line.Numbers[0].Stake);
        Assert.Equal(160_000m, line.MaxPayout);
        Assert.Single(preview.Warnings);
    }

    [Fact]
    public void TotalSplitDistributesRemainderToFirstLines()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "45,39,23",
            Amount = 1_000m,
            AmountMode = BatchAmountMode.TotalSplit
        };

        var success = _builder.TryBuild(request, out var preview, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(3, preview.Lines.Count);
        Assert.Equal(333.34m, preview.Lines[0].Amount);
        Assert.Equal(333.33m, preview.Lines[1].Amount);
        Assert.Equal(333.33m, preview.Lines[2].Amount);
        Assert.Equal(1_000m, preview.TotalStake);
    }

    [Theory]
    [InlineData("45", 0)]
    [InlineData("45", -1)]
    public void RejectsNonPositiveAmount(string input, decimal amount)
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = input,
            Amount = amount
        };

        var success = _builder.TryBuild(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void RejectsInvalidNumber()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "45,4",
            Amount = 1_000m
        };

        var success = _builder.TryBuild(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void RejectsBatchLargerThanMaximum()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "45,39,23",
            Amount = 1_000m,
            MaxNumbers = 2
        };

        var success = _builder.TryBuild(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void RejectsAmountWithMoreThanTwoDecimals()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "45",
            Amount = 1_000.001m
        };

        var success = _builder.TryBuild(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void RejectsNonPositiveMultiplier()
    {
        var request = new BatchSaleRequest
        {
            NumbersInput = "45",
            Amount = 1_000m,
            PayoutMultiplier = 0m
        };

        var success = _builder.TryBuild(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }
}
