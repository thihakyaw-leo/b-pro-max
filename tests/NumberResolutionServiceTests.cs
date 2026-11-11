using BProMax.Models;
using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

public class NumberResolutionServiceTests
{
    private static readonly string[] ExpectedExact = ["45"];
    private static readonly string[] ExpectedDouble = ["55"];
    private static readonly string[] ExpectedExactAndReverse = ["45", "54"];
    private static readonly string[] ExpectedTens = ["40", "41", "42", "43", "44", "45", "46", "47", "48", "49"];
    private static readonly string[] ExpectedOnes = ["05", "15", "25", "35", "45", "55", "65", "75", "85", "95"];
    private static readonly string[] ExpectedDigitSumFive = ["05", "14", "23", "32", "41", "50", "69", "78", "87", "96"];
    private static readonly string[] ExpectedNakkhat = ["07", "18", "24", "35", "69"];
    private static readonly string[] ExpectedRange = ["05", "06", "07", "08", "09", "10"];
    private static readonly string[] ExpectedRangeEvenOdd = ["01", "03", "05", "07", "09"];
    private static readonly string[] ExpectedEvenDoubles = ["00", "22", "44", "66", "88"];

    private readonly NumberResolutionService _service = new();

    [Fact]
    public void ExactReturnsTheSelectedNumber()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.Exact,
            Selection = "45"
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedExact, result.Numbers.ToArray());
        Assert.Equal(NumberMatchMode.Exact, result.MatchMode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("4")]
    [InlineData("123")]
    [InlineData("4A")]
    [InlineData(" 4")]
    public void ExactRejectsInvalidNumbers(string? selection)
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.Exact,
            Selection = selection
        };

        var success = _service.TryResolve(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void ExactAndReverseReturnsBothOrientations()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.ExactAndReverse,
            Selection = "45"
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedExactAndReverse, result.Numbers.ToArray());
    }

    [Fact]
    public void ExactAndReverseDoesNotDuplicateADouble()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.ExactAndReverse,
            Selection = "55"
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedDouble, result.Numbers.ToArray());
    }

    [Fact]
    public void DigitInEitherPositionReturnsNineteenNumbers()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.DigitInEitherPosition,
            Selection = "5"
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(19, result.Numbers.Count);
        Assert.Equal(19, result.Numbers.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("05", result.Numbers);
        Assert.Contains("55", result.Numbers);
        Assert.Contains("59", result.Numbers);
        Assert.DoesNotContain("46", result.Numbers);
    }

    [Fact]
    public void DigitInEitherPositionCanExcludeDoubles()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.DigitInEitherPosition,
            Selection = "5",
            RuleConfig = new NumberRuleConfig { ExcludeDoubles = true }
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(18, result.Numbers.Count);
        Assert.DoesNotContain("55", result.Numbers);
    }

    [Fact]
    public void DigitInTensPositionReturnsTenNumbers()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.DigitInPosition,
            Selection = "4",
            RuleConfig = new NumberRuleConfig { Position = NumberPosition.Tens }
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedTens, result.Numbers.ToArray());
    }

    [Fact]
    public void DigitInOnesPositionReturnsTenNumbers()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.DigitInPosition,
            Selection = "5",
            RuleConfig = new NumberRuleConfig { Position = NumberPosition.Ones }
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedOnes, result.Numbers.ToArray());
    }

    [Fact]
    public void DigitSumReturnsNumbersWhoseDigitSumEndsInTheSelection()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.DigitSum,
            Selection = "5"
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedDigitSumFive, result.Numbers.ToArray());
    }

    [Fact]
    public void DigitSumWithReverseDoesNotDuplicateTheSet()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.DigitSum,
            Selection = "5",
            IncludeReverse = true
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(10, result.Numbers.Count);
        Assert.Equal(10, result.Numbers.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void DigitInPositionWithReverseAddsTheOppositePosition()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.DigitInPosition,
            Selection = "4",
            IncludeReverse = true
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(19, result.Numbers.Count);
        Assert.Contains("04", result.Numbers);
        Assert.Contains("94", result.Numbers);
    }

    [Fact]
    public void FixedGroupReturnsItsMembers()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.FixedGroup,
            FixedNumbers = ExpectedNakkhat
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedNakkhat, result.Numbers.ToArray());
        Assert.Equal(NumberMatchMode.Exact, result.MatchMode);
    }

    [Fact]
    public void FixedGroupWithReverseReturnsTenNumbers()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.FixedGroup,
            FixedNumbers = ExpectedNakkhat,
            IncludeReverse = true
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(10, result.Numbers.Count);
        Assert.Contains("07", result.Numbers);
        Assert.Contains("70", result.Numbers);
        Assert.Contains("69", result.Numbers);
        Assert.Contains("96", result.Numbers);
    }

    [Fact]
    public void FixedGroupUsesRuleConfigWhenFixedNumbersAreMissing()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.FixedGroup,
            RuleConfig = new NumberRuleConfig { Numbers = ExpectedNakkhat }
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedNakkhat, result.Numbers.ToArray());
    }

    [Fact]
    public void FixedGroupRejectsAnEmptyGroup()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.FixedGroup
        };

        var success = _service.TryResolve(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void FixedGroupRejectsInvalidMembers()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.FixedGroup,
            FixedNumbers = ["07", "8", "24"]
        };

        var success = _service.TryResolve(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void UnorderedEvenPairsReturnsFifteenCanonicalNumbers()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.UnorderedPair,
            RuleConfig = new NumberRuleConfig
            {
                TensParity = NumberParity.Even,
                OnesParity = NumberParity.Even
            }
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(15, result.Numbers.Count);
        Assert.Equal(NumberMatchMode.UnorderedPair, result.MatchMode);
        Assert.Contains("00", result.Numbers);
        Assert.Contains("24", result.Numbers);
        Assert.Contains("68", result.Numbers);
        Assert.DoesNotContain("42", result.Numbers);
        Assert.DoesNotContain("01", result.Numbers);
    }

    [Fact]
    public void UnorderedOddPairsReturnsFifteenCanonicalNumbers()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.UnorderedPair,
            RuleConfig = new NumberRuleConfig
            {
                TensParity = NumberParity.Odd,
                OnesParity = NumberParity.Odd
            }
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(15, result.Numbers.Count);
        Assert.Equal(NumberMatchMode.UnorderedPair, result.MatchMode);
        Assert.Contains("11", result.Numbers);
        Assert.Contains("35", result.Numbers);
        Assert.DoesNotContain("53", result.Numbers);
        Assert.DoesNotContain("00", result.Numbers);
    }

    [Fact]
    public void OrderedMixedParityReturnsTwentyFiveNumbers()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.Parity,
            RuleConfig = new NumberRuleConfig
            {
                TensParity = NumberParity.Even,
                OnesParity = NumberParity.Odd,
                Ordered = true
            }
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(25, result.Numbers.Count);
        Assert.Equal(NumberMatchMode.Exact, result.MatchMode);
        Assert.Contains("01", result.Numbers);
        Assert.Contains("89", result.Numbers);
        Assert.DoesNotContain("10", result.Numbers);
        Assert.DoesNotContain("00", result.Numbers);
    }

    [Fact]
    public void UnorderedMixedParityReturnsTwentyFiveCanonicalPairs()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.Parity,
            RuleConfig = new NumberRuleConfig
            {
                TensParity = NumberParity.Even,
                OnesParity = NumberParity.Odd,
                Ordered = false
            }
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(25, result.Numbers.Count);
        Assert.Equal(NumberMatchMode.UnorderedPair, result.MatchMode);
        Assert.Contains("01", result.Numbers);
        Assert.Contains("12", result.Numbers);
        Assert.DoesNotContain("10", result.Numbers);
    }

    [Fact]
    public void ParityRequiresAtLeastOneParityRule()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.Parity
        };

        var success = _service.TryResolve(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void DoublesReturnsAllTenDoubles()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.Doubles
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(10, result.Numbers.Count);
        Assert.Contains("00", result.Numbers);
        Assert.Contains("99", result.Numbers);
    }

    [Fact]
    public void DoublesCanBeFilteredByParity()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.Doubles,
            RuleConfig = new NumberRuleConfig { TensParity = NumberParity.Even }
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedEvenDoubles, result.Numbers.ToArray());
    }

    [Fact]
    public void RangeReturnsInclusiveNumbers()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.Range,
            RuleConfig = new NumberRuleConfig { Minimum = 5, Maximum = 10 }
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedRange, result.Numbers.ToArray());
    }

    [Fact]
    public void RangeAppliesParityFilters()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.Range,
            RuleConfig = new NumberRuleConfig
            {
                Minimum = 0,
                Maximum = 20,
                TensParity = NumberParity.Even,
                OnesParity = NumberParity.Odd
            }
        };

        var success = _service.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedRangeEvenOdd, result.Numbers.ToArray());
    }

    [Fact]
    public void RangeRejectsInvalidBounds()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = NumberRuleTypes.Range,
            RuleConfig = new NumberRuleConfig { Minimum = 50, Maximum = 10 }
        };

        var success = _service.TryResolve(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void UnknownRuleTypeIsRejected()
    {
        var request = new NumberResolutionRequest
        {
            RuleType = "SomethingElse",
            Selection = "45"
        };

        var success = _service.TryResolve(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }

    [Fact]
    public void EmptyRuleTypeIsRejected()
    {
        var request = new NumberResolutionRequest { Selection = "45" };

        var success = _service.TryResolve(request, out _, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
    }
}
