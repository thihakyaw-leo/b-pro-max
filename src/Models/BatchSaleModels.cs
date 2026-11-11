namespace BProMax.Models;

/// <summary>
/// How the amount entered for a batch is distributed.
/// </summary>
public enum BatchAmountMode
{
    /// <summary>The amount applies to each base number; R splits each base number's amount.</summary>
    PerBaseNumber,

    /// <summary>The amount is the total stake and is divided across the whole batch.</summary>
    TotalSplit
}

/// <summary>
/// A request to preview a batch of direct or R numbers before saving.
/// </summary>
public sealed class BatchSaleRequest
{
    public string NumbersInput { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public bool IncludeReverse { get; init; }
    public BatchAmountMode AmountMode { get; init; } = BatchAmountMode.PerBaseNumber;
    public decimal PayoutMultiplier { get; init; } = 80m;
    public int MaxNumbers { get; init; } = 100;
}

/// <summary>
/// One resolved number inside a batch line, with its exact stake.
/// </summary>
public sealed class BatchSaleNumber
{
    public string Number { get; init; } = string.Empty;
    public decimal Stake { get; init; }
    public bool IsReverse { get; init; }
}

/// <summary>
/// One base number from a batch and its resolved number(s).
/// </summary>
public sealed class BatchSaleLine
{
    public string BaseNumber { get; init; } = string.Empty;
    public IReadOnlyList<BatchSaleNumber> Numbers { get; init; } = Array.Empty<BatchSaleNumber>();
    public decimal Amount { get; init; }
    public decimal MaxPayout { get; init; }
}

/// <summary>
/// Validated preview of a batch entry.
/// </summary>
public sealed class BatchSalePreview
{
    public IReadOnlyList<BatchSaleLine> Lines { get; init; } = Array.Empty<BatchSaleLine>();
    public decimal TotalStake { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
