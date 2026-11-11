namespace BProMax.Models;

/// <summary>
/// How a resolved number is matched against the draw result.
/// </summary>
public enum NumberMatchMode
{
    /// <summary>The number must match exactly.</summary>
    Exact,

    /// <summary>The digits are unordered; 24 matches a result of 42.</summary>
    UnorderedPair
}

/// <summary>
/// A digit position used by <see cref="NumberRuleTypes.DigitInPosition"/>.
/// </summary>
public enum NumberPosition
{
    /// <summary>The first (tens) digit.</summary>
    Tens,

    /// <summary>The second (ones) digit.</summary>
    Ones
}

/// <summary>
/// The parity of a digit or pair of digits.
/// </summary>
public enum NumberParity
{
    Even,
    Odd
}

/// <summary>
/// Which numbers a bet type resolves to.
/// </summary>
public enum PayoutMode
{
    /// <summary>The stake on each matching resolved number wins the multiplier.</summary>
    PerWinningNumber,

    /// <summary>The whole item amount wins the multiplier when any resolved number matches.</summary>
    TotalIfHit
}

/// <summary>
/// The supported rule types for v1. These values are stored in <c>bet_types.rule_type</c>.
/// </summary>
public static class NumberRuleTypes
{
    public const string Exact = "Exact";
    public const string ExactAndReverse = "ExactAndReverse";
    public const string DigitInEitherPosition = "DigitInEitherPosition";
    public const string DigitInPosition = "DigitInPosition";
    public const string DigitSum = "DigitSum";
    public const string FixedGroup = "FixedGroup";
    public const string UnorderedPair = "UnorderedPair";
    public const string Doubles = "Doubles";
    public const string Parity = "Parity";
    public const string Range = "Range";
}

/// <summary>
/// Dealer-configurable options for a number rule.
/// </summary>
public sealed class NumberRuleConfig
{
    /// <summary>The digit position for <see cref="NumberRuleTypes.DigitInPosition"/>.</summary>
    public NumberPosition Position { get; init; } = NumberPosition.Tens;

    /// <summary>Required parity of the tens digit, or <c>null</c> for any digit.</summary>
    public NumberParity? TensParity { get; init; }

    /// <summary>Required parity of the ones digit, or <c>null</c> for any digit.</summary>
    public NumberParity? OnesParity { get; init; }

    /// <summary>
    /// When false, a pair such as 24 and 42 is stored once in ascending order and matched unordered.
    /// </summary>
    public bool Ordered { get; init; } = true;

    /// <summary>Members used by <see cref="NumberRuleTypes.FixedGroup"/>.</summary>
    public IReadOnlyList<string> Numbers { get; init; } = Array.Empty<string>();

    /// <summary>Inclusive lower bound for <see cref="NumberRuleTypes.Range"/>.</summary>
    public int Minimum { get; init; }

    /// <summary>Inclusive upper bound for <see cref="NumberRuleTypes.Range"/>.</summary>
    public int Maximum { get; init; } = 99;

    /// <summary>Exclude numbers whose two digits are equal.</summary>
    public bool ExcludeDoubles { get; init; }
}

/// <summary>
/// A request to resolve a sale-type selection into two-digit numbers.
/// </summary>
public sealed class NumberResolutionRequest
{
    public string RuleType { get; init; } = string.Empty;
    public NumberRuleConfig RuleConfig { get; init; } = new();
    public string? Selection { get; init; }
    public IReadOnlyList<string>? FixedNumbers { get; init; }
    public bool IncludeReverse { get; init; }
}

/// <summary>
/// The resolved numbers and the match rule that must be used when settling.
/// </summary>
public sealed class NumberResolutionResult
{
    public IReadOnlyList<string> Numbers { get; init; } = Array.Empty<string>();
    public NumberMatchMode MatchMode { get; init; } = NumberMatchMode.Exact;
}
