using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

public class NumberBatchParserTests
{
    private static readonly string[] ExpectedBatch = ["45", "39", "23", "19", "40"];
    private static readonly string[] ExpectedLeadingZeros = ["05", "00", "09"];
    private static readonly string[] ExpectedDuplicates = ["45", "45", "39"];
    private static readonly string[] ExpectedPair = ["45", "39"];

    private readonly NumberBatchParser _parser = new();

    [Theory]
    [InlineData("4539231940", "45, 39, 23, 19, 40")]
    [InlineData("45,39,23,19,40", "45, 39, 23, 19, 40")]
    [InlineData("45 39 23 19 40", "45, 39, 23, 19, 40")]
    [InlineData("45\n39\t23;19|40", "45, 39, 23, 19, 40")]
    [InlineData("45", "45")]
    [InlineData("453", "45, 3")]
    public void FormatGroupsDigitsIntoPairs(string input, string expected)
    {
        Assert.Equal(expected, _parser.Format(input));
    }

    [Fact]
    public void FormatAppendsTrailingSeparatorAfterCompletePair()
    {
        Assert.Equal("45, ", _parser.Format("45", appendTrailingSeparator: true));
        Assert.Equal("45, 3", _parser.Format("453", appendTrailingSeparator: true));
    }

    [Fact]
    public void FormatLeavesInvalidCharactersUntouched()
    {
        Assert.Equal("45A39", _parser.Format("45A39"));
    }

    [Fact]
    public void FormatReturnsEmptyForEmptyInput()
    {
        Assert.Equal(string.Empty, _parser.Format(null));
        Assert.Equal(string.Empty, _parser.Format("   "));
        Assert.Equal(string.Empty, _parser.Format(",,"));
    }

    [Fact]
    public void ParseCommaSeparatedNumbers()
    {
        var result = _parser.Parse("45,39,23,19,40");

        Assert.Equal(ExpectedBatch, result.ToArray());
    }

    [Fact]
    public void ParseSpaceAndNewlineSeparatedNumbers()
    {
        var result = _parser.Parse("45 39\n23\t19;40");

        Assert.Equal(ExpectedBatch, result.ToArray());
    }

    [Fact]
    public void ParseContinuousDigits()
    {
        var result = _parser.Parse("4539231940");

        Assert.Equal(ExpectedBatch, result.ToArray());
    }

    [Fact]
    public void ParsePreservesLeadingZeros()
    {
        var result = _parser.Parse("05,00,09");

        Assert.Equal(ExpectedLeadingZeros, result.ToArray());
    }

    [Fact]
    public void ParsePreservesDuplicates()
    {
        var result = _parser.Parse("45,45,39");

        Assert.Equal(ExpectedDuplicates, result.ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",,,")]
    public void ParseRejectsEmptyInput(string? input)
    {
        Assert.Throws<FormatException>(() => _parser.Parse(input));
    }

    [Theory]
    [InlineData("45,4")]
    [InlineData("45, 123")]
    public void ParseRejectsInvalidTokenLength(string input)
    {
        Assert.Throws<FormatException>(() => _parser.Parse(input));
    }

    [Fact]
    public void ParseRejectsOddNumberOfContinuousDigits()
    {
        Assert.Throws<FormatException>(() => _parser.Parse("45392"));
    }

    [Fact]
    public void ParseRejectsInvalidCharacters()
    {
        Assert.Throws<FormatException>(() => _parser.Parse("45,3A"));
    }

    [Fact]
    public void TryParseReturnsTrueForValidInput()
    {
        var success = _parser.TryParse("45, 39", out var numbers, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(ExpectedPair, numbers.ToArray());
    }

    [Fact]
    public void TryParseReturnsFalseForInvalidInput()
    {
        var success = _parser.TryParse("45, 3", out var numbers, out var validationError);

        Assert.False(success);
        Assert.NotNull(validationError);
        Assert.Empty(numbers);
    }
}
