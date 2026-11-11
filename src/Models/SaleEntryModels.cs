namespace BProMax.Models;

/// <summary>
/// One resolved number supplied by a preview (batch or Quick Add).
/// </summary>
public sealed class SaleEntryNumberRequest
{
    public string Number { get; init; } = string.Empty;
    public decimal Stake { get; init; }
    public bool IsReverse { get; init; }
}

/// <summary>
/// One sale item to create inside a sale transaction.
/// </summary>
public sealed class SaleEntryItemRequest
{
    public string BetTypeCode { get; init; } = string.Empty;
    public string? Selection { get; init; }
    public decimal Amount { get; init; }
    public bool IncludeReverse { get; init; }
    public string? NumberGroupCode { get; init; }

    /// <summary>
    /// Optional pre-resolved stakes from a batch or Quick Add preview. When supplied,
    /// the service validates them and uses them instead of recalculating the split.
    /// </summary>
    public IReadOnlyList<SaleEntryNumberRequest>? NumberStakes { get; init; }
}

/// <summary>
/// A request to create one sale with all of its items in a single transaction.
/// </summary>
public sealed class SaleEntryRequest
{
    public int AgentId { get; init; }
    public long DrawId { get; init; }
    public int? CreatedBy { get; init; }
    public string? CurrencyType { get; init; }
    public decimal? CurrencyRate { get; init; }
    public DateTime? SaleDate { get; init; }
    public IReadOnlyList<SaleEntryItemRequest> Items { get; init; } = Array.Empty<SaleEntryItemRequest>();
}

/// <summary>
/// One created sale item, including its persisted resolved numbers.
/// </summary>
public sealed class SaleEntryItemResult
{
    public long SaleItemId { get; init; }
    public string BetTypeCode { get; init; } = string.Empty;
    public string? NumberGroupCode { get; init; }
    public string BetKey { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal MaxPayout { get; init; }
    public IReadOnlyList<SaleItemNumberRecord> Numbers { get; init; } = Array.Empty<SaleItemNumberRecord>();
}

/// <summary>
/// The created sale header and its item details.
/// </summary>
public sealed class SaleEntryResult
{
    public long SaleId { get; init; }
    public decimal TotalStake { get; init; }
    public decimal Commission { get; init; }
    public decimal Commission1 { get; init; }
    public bool AutoBuyTriggered { get; init; }
    public int? AutoBuyRuleId { get; init; }
    public decimal? AutoBuyAmount { get; init; }
    public IReadOnlyList<SaleEntryItemResult> Items { get; init; } = Array.Empty<SaleEntryItemResult>();
}

/// <summary>
/// The amount split for one sale item before it is persisted.
/// </summary>
public sealed class SaleItemAmountResult
{
    public decimal Total { get; init; }
    public decimal MaxPayout { get; init; }
    public IReadOnlyList<decimal> Stakes { get; init; } = Array.Empty<decimal>();
}
