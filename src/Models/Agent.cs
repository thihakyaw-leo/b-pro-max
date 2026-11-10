namespace BProMax.Models;

/// <summary>
/// Represents an agent record, migrated from DD.exe AgentThai table.
/// </summary>
public class Agent
{
    public int Id { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public decimal Commission { get; set; }
    public decimal Rate { get; set; }
    public decimal Commission1 { get; set; }
    public decimal Rate1 { get; set; }
    public string? CurrencyType { get; set; }
    public decimal CurrencyRate { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
