using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BProMax.Models;

/// <summary>
/// Represents an agent record mapped to the Supabase 'agents' table.
/// Matches DD.exe AgentThai schema.
/// </summary>
[Table("agents")]
public class SupabaseAgent : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public int Id { get; set; }

    [Column("agent_name")]
    public string AgentName { get; set; } = string.Empty;

    [Column("address")]
    public string? Address { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("commission")]
    public decimal Commission { get; set; }

    [Column("rate")]
    public decimal Rate { get; set; }

    [Column("commission1")]
    public decimal Commission1 { get; set; }

    [Column("rate1")]
    public decimal Rate1 { get; set; }

    [Column("currency_type")]
    public string? CurrencyType { get; set; }

    [Column("currency_rate")]
    public decimal CurrencyRate { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
