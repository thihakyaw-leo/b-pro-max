namespace BProMax.Models;

/// <summary>
/// One draw in the sales-by-draw report.
/// </summary>
public sealed class DrawSalesReportRow
{
    public long DrawId { get; init; }
    public DateOnly DrawDate { get; init; }
    public TimeOnly DrawTime { get; init; }
    public string? DrawNumber { get; init; }
    public string Status { get; init; } = string.Empty;
    public int SaleCount { get; init; }
    public decimal TotalStake { get; init; }
    public decimal TotalCommission { get; init; }
    public decimal TotalPayout { get; init; }
    public decimal Net => decimal.Round(
        TotalStake - TotalCommission - TotalPayout,
        2,
        MidpointRounding.AwayFromZero);
}

/// <summary>
/// One resolved number in the sales-by-number report.
/// </summary>
public sealed class NumberSalesReportRow
{
    public string Number { get; init; } = string.Empty;
    public int LineCount { get; init; }
    public decimal TotalStake { get; init; }
    public int WinningLineCount { get; init; }
    public decimal TotalPayout { get; init; }
}

/// <summary>
/// One fixed number group in the group-performance report.
/// </summary>
public sealed class GroupSalesReportRow
{
    public string GroupCode { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public int BaseLineCount { get; init; }
    public decimal BaseStake { get; init; }
    public int ReverseLineCount { get; init; }
    public decimal ReverseStake { get; init; }
    public int WinningLineCount { get; init; }
    public decimal TotalPayout { get; init; }
}

/// <summary>
/// One payout row in the payout register.
/// </summary>
public sealed class PayoutRegisterRow
{
    public long PayoutId { get; init; }
    public DateOnly DrawDate { get; init; }
    public TimeOnly DrawTime { get; init; }
    public string? DrawNumber { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public string BetTypeCode { get; init; } = string.Empty;
    public string BetTypeName { get; init; } = string.Empty;
    public string? GroupName { get; init; }
    public string? Number { get; init; }
    public decimal? Stake { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
}

/// <summary>
/// The 2D reports that can be exported.
/// </summary>
public enum TwoDReportKind
{
    SalesByDraw,
    SalesByNumber,
    GroupPerformance,
    PayoutRegister
}
