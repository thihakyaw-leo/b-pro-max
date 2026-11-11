namespace BProMax.Models;

/// <summary>
/// One row in the current sale preview.
/// </summary>
public sealed class SalePanelLine
{
    public string BetTypeCode { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string BetKey { get; init; } = string.Empty;
    public string NumbersText { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string PerNumberText { get; init; } = string.Empty;
    public decimal MaxPayout { get; init; }
    public SaleEntryItemRequest Request { get; init; } = new();
}
