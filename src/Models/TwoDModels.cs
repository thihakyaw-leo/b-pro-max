using System.Globalization;

namespace BProMax.Models;

/// <summary>
/// The lifecycle status of a draw.
/// </summary>
public static class DrawStatuses
{
    public const string Open = "Open";
    public const string Closed = "Closed";
    public const string Drawn = "Drawn";
    public const string Settled = "Settled";
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// The settlement status of a sale header.
/// </summary>
public static class SaleSettlementStatuses
{
    public const string Open = "Open";
    public const string Settled = "Settled";
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// The status of one sale item or one resolved number inside a sale item.
/// </summary>
public static class SaleItemStatuses
{
    public const string Pending = "Pending";
    public const string Won = "Won";
    public const string Lost = "Lost";
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// The status of a payout row.
/// </summary>
public static class PayoutStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// How the entered amount is interpreted for a bet type.
/// </summary>
public static class BetAmountModes
{
    public const string PerNumber = "PerNumber";
    public const string TotalSplit = "TotalSplit";
    public const string Total = "Total";
}

/// <summary>
/// A draw header. One two-digit result belongs to one draw.
/// </summary>
public sealed class DrawRecord
{
    public long Id { get; set; }
    public DateOnly DrawDate { get; set; }
    public TimeOnly DrawTime { get; set; }
    public string? DrawNumber { get; set; }
    public string Status { get; set; } = DrawStatuses.Open;
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>A stable display label for pickers, for example "2026-11-11 12:00".</summary>
    public string Display => string.Create(
        CultureInfo.InvariantCulture,
        $"{DrawDate:yyyy-MM-dd} {DrawTime:HH\\:mm}");
}

/// <summary>
/// A dealer-configurable bet type.
/// </summary>
public sealed class BetTypeRecord
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameMy { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;

    /// <summary>JSON stored in <c>bet_types.rule_config</c>.</summary>
    public string RuleConfigJson { get; set; } = "{}";

    public string AmountMode { get; set; } = BetAmountModes.TotalSplit;
    public string PayoutMode { get; set; } = nameof(BProMax.Models.PayoutMode.PerWinningNumber);
    public decimal PayoutMultiplier { get; set; } = 80m;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// A fixed number group sold through Quick Add (for example နက္ခတ် or ပါဝါ).
/// </summary>
public sealed class NumberGroupRecord
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameMy { get; set; } = string.Empty;
    public bool AllowReverse { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> Members { get; set; } = [];
}

/// <summary>
/// One line inside a sale: a bet type plus its exact amount and configuration snapshot.
/// </summary>
public sealed class SaleItemRecord
{
    public long Id { get; set; }
    public long SaleId { get; set; }
    public long DrawId { get; set; }
    public int BetTypeId { get; set; }
    public int? NumberGroupId { get; set; }
    public string BetKey { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PayoutMultiplier { get; set; }
    public string PayoutMode { get; set; } = nameof(BProMax.Models.PayoutMode.PerWinningNumber);
    public string AmountMode { get; set; } = BetAmountModes.TotalSplit;
    public string MatchMode { get; set; } = nameof(NumberMatchMode.Exact);
    public string Status { get; set; } = SaleItemStatuses.Pending;
    public DateTime CreatedAt { get; set; }
    public List<SaleItemNumberRecord> Numbers { get; set; } = [];
}

/// <summary>
/// One resolved two-digit number inside a sale item and the stake sold on it.
/// </summary>
public sealed class SaleItemNumberRecord
{
    public long Id { get; set; }
    public long SaleItemId { get; set; }
    public string Number { get; set; } = string.Empty;
    public decimal Stake { get; set; }
    public bool IsReverse { get; set; }
    public string Status { get; set; } = SaleItemStatuses.Pending;
    public decimal PayoutAmount { get; set; }
}

/// <summary>
/// A payout generated when a draw result matches a resolved number.
/// </summary>
public sealed class PayoutRecord
{
    public long Id { get; set; }
    public long DrawId { get; set; }
    public long SaleId { get; set; }
    public long SaleItemId { get; set; }
    public long? SaleItemNumberId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = PayoutStatuses.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public int? PaidBy { get; set; }
}
