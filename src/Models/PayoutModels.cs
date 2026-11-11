namespace BProMax.Models;

/// <summary>
/// One resolved number and the stake that was sold on it.
/// </summary>
public sealed class PayoutNumberStake
{
    public string Number { get; init; } = string.Empty;
    public decimal Stake { get; init; }
    public bool IsReverse { get; init; }
}

/// <summary>
/// A request to calculate the payout of one sale item against a draw result.
/// </summary>
public sealed class PayoutCalculationRequest
{
    public string ResultNumber { get; init; } = string.Empty;
    public NumberMatchMode MatchMode { get; init; } = NumberMatchMode.Exact;
    public PayoutMode PayoutMode { get; init; } = PayoutMode.PerWinningNumber;
    public decimal PayoutMultiplier { get; init; } = 80m;

    /// <summary>The item amount, used only by <see cref="PayoutMode.TotalIfHit"/>.</summary>
    public decimal ItemAmount { get; init; }

    public IReadOnlyList<PayoutNumberStake> Numbers { get; init; } = Array.Empty<PayoutNumberStake>();
}

/// <summary>
/// A winning resolved number and its calculated payout.
/// </summary>
public sealed class PayoutCalculationLine
{
    public string Number { get; init; } = string.Empty;
    public decimal Stake { get; init; }
    public bool IsReverse { get; init; }
    public decimal Payout { get; init; }
}

/// <summary>
/// The payout result for one sale item.
/// </summary>
public sealed class PayoutCalculationResult
{
    public bool IsWinner { get; init; }
    public decimal TotalPayout { get; init; }
    public IReadOnlyList<PayoutCalculationLine> WinningNumbers { get; init; } = Array.Empty<PayoutCalculationLine>();
}

/// <summary>
/// The result of settling one draw.
/// </summary>
public sealed class PayoutSettlementResult
{
    public long DrawId { get; init; }
    public string DrawNumber { get; init; } = string.Empty;
    public int ItemsSettled { get; init; }
    public int WinningItems { get; init; }
    public decimal TotalPayout { get; init; }
    public IReadOnlyList<PayoutRecord> Payouts { get; init; } = Array.Empty<PayoutRecord>();
}

/// <summary>
/// A payout row enriched with the agent, bet type, and resolved number for display.
/// </summary>
public sealed class PayoutDetailRecord
{
    public long PayoutId { get; init; }
    public long SaleId { get; init; }
    public long SaleItemId { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public string BetLabel { get; init; } = string.Empty;
    public string? Number { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? PaidAt { get; init; }
    public int? PaidBy { get; init; }
}
