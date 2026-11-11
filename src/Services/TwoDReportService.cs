using BProMax.Models;
using Npgsql;
using NpgsqlTypes;

namespace BProMax.Services;

/// <summary>
/// Builds the 2D sales and payout reports from the persisted sale snapshots.
/// </summary>
public class TwoDReportService : ITwoDReportService
{
    private readonly IDatabaseService _db;

    public TwoDReportService(IDatabaseService db)
    {
        _db = db;
    }

    public async Task<List<DrawSalesReportRow>> GetSalesByDrawAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
SELECT d.id, d.draw_date, d.draw_time, d.draw_number, d.status,
       COUNT(DISTINCT s.id)::int AS sale_count,
       COALESCE(SUM(s.amount), 0) AS total_stake,
       COALESCE(SUM(s.commission + s.commission1), 0) AS total_commission,
       COALESCE((
           SELECT SUM(p.amount)
           FROM payouts p
           WHERE p.draw_id = d.id AND p.status <> 'Cancelled'
       ), 0) AS total_payout
FROM draws d
LEFT JOIN sales s ON s.draw_id = d.id AND s.status <> 'Cancelled'
WHERE (@from IS NULL OR d.draw_date >= @from)
  AND (@to IS NULL OR d.draw_date <= @to)
GROUP BY d.id, d.draw_date, d.draw_time, d.draw_number, d.status
ORDER BY d.draw_date DESC, d.draw_time DESC;
""";

        return await _db.QueryListAsync(sql, MapDrawSales, DateParameter("@from", fromDate), DateParameter("@to", toDate));
    }

    public async Task<List<NumberSalesReportRow>> GetSalesByNumberAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        long? drawId = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
SELECT sin.number,
       COUNT(*)::int AS line_count,
       COALESCE(SUM(sin.stake), 0) AS total_stake,
       COUNT(*) FILTER (WHERE sin.status = 'Won')::int AS winning_line_count,
       COALESCE(SUM(sin.payout_amount), 0) AS total_payout
FROM sale_item_numbers sin
JOIN sale_items si ON si.id = sin.sale_item_id
JOIN sales s ON s.id = si.sale_id
JOIN draws d ON d.id = si.draw_id
WHERE s.status <> 'Cancelled'
  AND (@from IS NULL OR d.draw_date >= @from)
  AND (@to IS NULL OR d.draw_date <= @to)
  AND (@draw_id IS NULL OR si.draw_id = @draw_id)
GROUP BY sin.number
ORDER BY sin.number;
""";

        return await _db.QueryListAsync(
            sql,
            MapNumberSales,
            DateParameter("@from", fromDate),
            DateParameter("@to", toDate),
            new NpgsqlParameter("@draw_id", NpgsqlDbType.Bigint)
            {
                Value = drawId.HasValue ? drawId.Value : DBNull.Value
            });
    }

    public async Task<List<GroupSalesReportRow>> GetGroupPerformanceAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
SELECT g.code, g.name_my,
       COUNT(*) FILTER (WHERE sin.is_reverse = FALSE)::int AS base_line_count,
       COALESCE(SUM(sin.stake) FILTER (WHERE sin.is_reverse = FALSE), 0) AS base_stake,
       COUNT(*) FILTER (WHERE sin.is_reverse = TRUE)::int AS reverse_line_count,
       COALESCE(SUM(sin.stake) FILTER (WHERE sin.is_reverse = TRUE), 0) AS reverse_stake,
       COUNT(*) FILTER (WHERE sin.status = 'Won')::int AS winning_line_count,
       COALESCE(SUM(sin.payout_amount), 0) AS total_payout
FROM sale_items si
JOIN number_groups g ON g.id = si.number_group_id
JOIN sale_item_numbers sin ON sin.sale_item_id = si.id
JOIN sales s ON s.id = si.sale_id
JOIN draws d ON d.id = si.draw_id
WHERE s.status <> 'Cancelled'
  AND (@from IS NULL OR d.draw_date >= @from)
  AND (@to IS NULL OR d.draw_date <= @to)
GROUP BY g.code, g.name_my
ORDER BY g.code;
""";

        return await _db.QueryListAsync(sql, MapGroupSales, DateParameter("@from", fromDate), DateParameter("@to", toDate));
    }

    public async Task<List<PayoutRegisterRow>> GetPayoutRegisterAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
SELECT p.id, d.draw_date, d.draw_time, d.draw_number, a.agent_name,
       bt.code, bt.name_en, g.name_my, sin.number, sin.stake,
       p.amount, p.status, p.paid_at
FROM payouts p
JOIN draws d ON d.id = p.draw_id
JOIN sales s ON s.id = p.sale_id
JOIN agents a ON a.id = s.agent_id
JOIN sale_items si ON si.id = p.sale_item_id
JOIN bet_types bt ON bt.id = si.bet_type_id
LEFT JOIN number_groups g ON g.id = si.number_group_id
LEFT JOIN sale_item_numbers sin ON sin.id = p.sale_item_number_id
WHERE (@from IS NULL OR d.draw_date >= @from)
  AND (@to IS NULL OR d.draw_date <= @to)
  AND (@status IS NULL OR p.status = @status)
ORDER BY p.id;
""";

        return await _db.QueryListAsync(
            sql,
            MapPayout,
            DateParameter("@from", fromDate),
            DateParameter("@to", toDate),
            new NpgsqlParameter("@status", NpgsqlDbType.Text)
            {
                Value = string.IsNullOrWhiteSpace(status) ? DBNull.Value : status
            });
    }

    private static NpgsqlParameter DateParameter(string name, DateOnly? value)
    {
        return new NpgsqlParameter(name, NpgsqlDbType.Date)
        {
            Value = value.HasValue ? value.Value : DBNull.Value
        };
    }

    private static DrawSalesReportRow MapDrawSales(NpgsqlDataReader reader)
    {
        return new DrawSalesReportRow
        {
            DrawId = reader.GetInt64(reader.GetOrdinal("id")),
            DrawDate = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("draw_date")),
            DrawTime = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("draw_time")),
            DrawNumber = reader.IsDBNull(reader.GetOrdinal("draw_number"))
                ? null
                : reader.GetString(reader.GetOrdinal("draw_number")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            SaleCount = reader.GetInt32(reader.GetOrdinal("sale_count")),
            TotalStake = reader.GetDecimal(reader.GetOrdinal("total_stake")),
            TotalCommission = reader.GetDecimal(reader.GetOrdinal("total_commission")),
            TotalPayout = reader.GetDecimal(reader.GetOrdinal("total_payout"))
        };
    }

    private static NumberSalesReportRow MapNumberSales(NpgsqlDataReader reader)
    {
        return new NumberSalesReportRow
        {
            Number = reader.GetString(reader.GetOrdinal("number")),
            LineCount = reader.GetInt32(reader.GetOrdinal("line_count")),
            TotalStake = reader.GetDecimal(reader.GetOrdinal("total_stake")),
            WinningLineCount = reader.GetInt32(reader.GetOrdinal("winning_line_count")),
            TotalPayout = reader.GetDecimal(reader.GetOrdinal("total_payout"))
        };
    }

    private static GroupSalesReportRow MapGroupSales(NpgsqlDataReader reader)
    {
        return new GroupSalesReportRow
        {
            GroupCode = reader.GetString(reader.GetOrdinal("code")),
            GroupName = reader.GetString(reader.GetOrdinal("name_my")),
            BaseLineCount = reader.GetInt32(reader.GetOrdinal("base_line_count")),
            BaseStake = reader.GetDecimal(reader.GetOrdinal("base_stake")),
            ReverseLineCount = reader.GetInt32(reader.GetOrdinal("reverse_line_count")),
            ReverseStake = reader.GetDecimal(reader.GetOrdinal("reverse_stake")),
            WinningLineCount = reader.GetInt32(reader.GetOrdinal("winning_line_count")),
            TotalPayout = reader.GetDecimal(reader.GetOrdinal("total_payout"))
        };
    }

    private static PayoutRegisterRow MapPayout(NpgsqlDataReader reader)
    {
        return new PayoutRegisterRow
        {
            PayoutId = reader.GetInt64(reader.GetOrdinal("id")),
            DrawDate = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("draw_date")),
            DrawTime = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("draw_time")),
            DrawNumber = reader.IsDBNull(reader.GetOrdinal("draw_number"))
                ? null
                : reader.GetString(reader.GetOrdinal("draw_number")),
            AgentName = reader.GetString(reader.GetOrdinal("agent_name")),
            BetTypeCode = reader.GetString(reader.GetOrdinal("code")),
            BetTypeName = reader.GetString(reader.GetOrdinal("name_en")),
            GroupName = reader.IsDBNull(reader.GetOrdinal("name_my"))
                ? null
                : reader.GetString(reader.GetOrdinal("name_my")),
            Number = reader.IsDBNull(reader.GetOrdinal("number"))
                ? null
                : reader.GetString(reader.GetOrdinal("number")),
            Stake = reader.IsDBNull(reader.GetOrdinal("stake"))
                ? null
                : reader.GetDecimal(reader.GetOrdinal("stake")),
            Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            PaidAt = reader.IsDBNull(reader.GetOrdinal("paid_at"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("paid_at"))
        };
    }
}
