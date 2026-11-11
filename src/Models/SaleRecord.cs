namespace BProMax.Models;

/// <summary>
/// Sales record, migrated from DD.exe sales tracking.
/// </summary>
public class SaleRecord
{
    public long Id { get; set; }
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Commission { get; set; }
    public decimal Commission1 { get; set; }
    public string? CurrencyType { get; set; }
    public decimal CurrencyRate { get; set; } = 1;
    public string Status { get; set; } = "Pending"; // "Pending", "Paid", "Cancelled"
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
