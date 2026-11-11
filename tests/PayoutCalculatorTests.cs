using BProMax.Models;
using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

public class PayoutCalculatorTests
{
    private static readonly PayoutNumberStake[] Direct45 =
    [
        new PayoutNumberStake { Number = "45", Stake = 1_000m }
    ];

    private static readonly PayoutNumberStake[] Reverse45 =
    [
        new PayoutNumberStake { Number = "45", Stake = 500m },
        new PayoutNumberStake { Number = "54", Stake = 500m, IsReverse = true }
    ];

    private static readonly PayoutNumberStake[] EvenPair =
    [
        new PayoutNumberStake { Number = "24", Stake = 66.67m }
    ];

    private readonly PayoutCalculator _calculator = new();

    [Fact]
    public void DirectNumberWinsAtTheConfiguredMultiplier()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "45",
            Numbers = Direct45
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.True(result.IsWinner);
        Assert.Equal(80_000m, result.TotalPayout);
        var line = Assert.Single(result.WinningNumbers);
        Assert.Equal("45", line.Number);
        Assert.Equal(1_000m, line.Stake);
        Assert.Equal(80_000m, line.Payout);
    }

    [Fact]
    public void DirectNumberLosesWhenTheResultDiffers()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "54",
            Numbers = Direct45
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.False(result.IsWinner);
        Assert.Equal(0m, result.TotalPayout);
        Assert.Empty(result.WinningNumbers);
    }

    [Fact]
    public void ReverseWinsAndKeepsTheReverseFlag()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "54",
            Numbers = Reverse45
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.True(result.IsWinner);
        Assert.Equal(40_000m, result.TotalPayout);
        var line = Assert.Single(result.WinningNumbers);
        Assert.Equal("54", line.Number);
        Assert.True(line.IsReverse);
    }

    [Fact]
    public void UnorderedPairMatchesTheOppositeOrientation()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "42",
            MatchMode = NumberMatchMode.UnorderedPair,
            Numbers = EvenPair
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.True(result.IsWinner);
        Assert.Equal(5_333.60m, result.TotalPayout);
        Assert.Equal("24", Assert.Single(result.WinningNumbers).Number);
    }

    [Fact]
    public void ExactModeDoesNotMatchTheOppositeOrientation()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "42",
            MatchMode = NumberMatchMode.Exact,
            Numbers = EvenPair
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.False(result.IsWinner);
        Assert.Equal(0m, result.TotalPayout);
    }

    [Fact]
    public void UnorderedPairSumsBothStoredOrientations()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "42",
            MatchMode = NumberMatchMode.UnorderedPair,
            Numbers =
            [
                new PayoutNumberStake { Number = "24", Stake = 100m },
                new PayoutNumberStake { Number = "42", Stake = 100m }
            ]
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.True(result.IsWinner);
        Assert.Equal(2, result.WinningNumbers.Count);
        Assert.Equal(16_000m, result.TotalPayout);
    }

    [Fact]
    public void DuplicateExactNumbersAreSummed()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "45",
            Numbers =
            [
                new PayoutNumberStake { Number = "45", Stake = 100m },
                new PayoutNumberStake { Number = "45", Stake = 100m }
            ]
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(16_000m, result.TotalPayout);
    }

    [Fact]
    public void MultiplierCanBeLessThanTwo()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "45",
            PayoutMultiplier = 1.6m,
            Numbers = Direct45
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(1_600m, result.TotalPayout);
    }

    [Fact]
    public void PayoutIsRoundedToTwoDecimals()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "45",
            Numbers = [new PayoutNumberStake { Number = "45", Stake = 33.333m }]
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(2_666.64m, result.TotalPayout);
    }

    [Fact]
    public void TotalIfHitPaysTheItemAmountOnce()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "42",
            MatchMode = NumberMatchMode.UnorderedPair,
            PayoutMode = PayoutMode.TotalIfHit,
            ItemAmount = 1_000m,
            Numbers =
            [
                new PayoutNumberStake { Number = "24", Stake = 66.67m },
                new PayoutNumberStake { Number = "68", Stake = 66.67m }
            ]
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.True(result.IsWinner);
        Assert.Equal(80_000m, result.TotalPayout);
        Assert.Single(result.WinningNumbers);
    }

    [Fact]
    public void TotalIfHitLosesWhenNoNumberMatches()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "42",
            MatchMode = NumberMatchMode.UnorderedPair,
            PayoutMode = PayoutMode.TotalIfHit,
            ItemAmount = 1_000m,
            Numbers = [new PayoutNumberStake { Number = "68", Stake = 66.67m }]
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.False(result.IsWinner);
        Assert.Equal(0m, result.TotalPayout);
    }

    [Fact]
    public void TotalIfHitRequiresAnItemAmount()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "45",
            PayoutMode = PayoutMode.TotalIfHit,
            Numbers = Direct45
        };

        var success = _calculator.TryCalculate(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("4")]
    [InlineData("4A")]
    public void RejectsInvalidResultNumber(string? resultNumber)
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = resultNumber!,
            Numbers = Direct45
        };

        var success = _calculator.TryCalculate(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-80)]
    public void RejectsNonPositiveMultiplier(decimal multiplier)
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "45",
            PayoutMultiplier = multiplier,
            Numbers = Direct45
        };

        var success = _calculator.TryCalculate(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void RejectsNegativeStake()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "45",
            Numbers = [new PayoutNumberStake { Number = "45", Stake = -1m }]
        };

        var success = _calculator.TryCalculate(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void RejectsInvalidResolvedNumber()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "45",
            Numbers = [new PayoutNumberStake { Number = "4", Stake = 100m }]
        };

        var success = _calculator.TryCalculate(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void EmptyNumbersIsALoss()
    {
        var request = new PayoutCalculationRequest
        {
            ResultNumber = "45"
        };

        var success = _calculator.TryCalculate(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.False(result.IsWinner);
        Assert.Equal(0m, result.TotalPayout);
    }
}
