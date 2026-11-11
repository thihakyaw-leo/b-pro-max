using BProMax.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BProMax.Services;

public class SalesService : ISalesService
{
    private readonly IDatabaseService _db;
    private readonly ILogger<SalesService> _logger;

    public SalesService(IDatabaseService db, ILogger<SalesService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<SaleRecord>> GetAllSalesAsync()
    {
        const string sql = @"
SELECT s.id, s.agent_id, s.sale_date, s.amount, s.commission, s.commission1,
       s.currency_type, s.currency_rate, s.status, s.created_at, s.updated_at,
       a.agent_name
FROM sales s
JOIN agents a ON a.id = s.agent_id
ORDER BY s.sale_date DESC;";

        return await _db.QueryListAsync(sql, MapSaleRecord);
    }

    public async Task<SaleRecord?> GetSaleByIdAsync(long id)
    {
        const string sql = @"
SELECT s.id, s.agent_id, s.sale_date, s.amount, s.commission, s.commission1,
       s.currency_type, s.currency_rate, s.status, s.created_at, s.updated_at,
       a.agent_name
FROM sales s
JOIN agents a ON a.id = s.agent_id
WHERE s.id = @id;";

        return await _db.QuerySingleAsync(sql, MapSaleRecord,
            new NpgsqlParameter("@id", id));
    }

    public async Task<SaleRecord?> GetSaleByAgentAsync(int agentId)
    {
        const string sql = @"
SELECT s.id, s.agent_id, s.sale_date, s.amount, s.commission, s.commission1,
       s.currency_type, s.currency_rate, s.status, s.created_at, s.updated_at,
       a.agent_name
FROM sales s
JOIN agents a ON a.id = s.agent_id
WHERE s.agent_id = @agentId
ORDER BY s.sale_date DESC;";

        return await _db.QuerySingleAsync(sql, MapSaleRecord,
            new NpgsqlParameter("@agentId", agentId));
    }

    public async Task<long> CreateSaleAsync(SaleRecord sale)
    {
        const string sql = @"
INSERT INTO sales (agent_id, sale_date, amount, commission, commission1,
                   currency_type, currency_rate, status, updated_at)
VALUES (@agent_id, @sale_date, @amount, @commission, @commission1,
        @currency_type, @currency_rate, @status, NOW())
RETURNING id;";

        return await _db.QuerySingleAsync(sql, r => r.GetInt64(0),
            new NpgsqlParameter("@agent_id", sale.AgentId),
            new NpgsqlParameter("@sale_date", sale.SaleDate),
            new NpgsqlParameter("@amount", sale.Amount),
            new NpgsqlParameter("@commission", sale.Commission),
            new NpgsqlParameter("@commission1", sale.Commission1),
            new NpgsqlParameter("@currency_type", sale.CurrencyType ?? (object)DBNull.Value),
            new NpgsqlParameter("@currency_rate", sale.CurrencyRate),
            new NpgsqlParameter("@status", sale.Status));
    }

    public async Task<int> UpdateSaleAsync(SaleRecord sale)
    {
        const string sql = @"
UPDATE sales
SET agent_id = @agent_id, sale_date = @sale_date, amount = @amount,
    commission = @commission, commission1 = @commission1,
    currency_type = @currency_type, currency_rate = @currency_rate,
    status = @status, updated_at = NOW()
WHERE id = @id;";

        return await _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@id", sale.Id),
            new NpgsqlParameter("@agent_id", sale.AgentId),
            new NpgsqlParameter("@sale_date", sale.SaleDate),
            new NpgsqlParameter("@amount", sale.Amount),
            new NpgsqlParameter("@commission", sale.Commission),
            new NpgsqlParameter("@commission1", sale.Commission1),
            new NpgsqlParameter("@currency_type", sale.CurrencyType ?? (object)DBNull.Value),
            new NpgsqlParameter("@currency_rate", sale.CurrencyRate),
            new NpgsqlParameter("@status", sale.Status));
    }

    public async Task<int> DeleteSaleAsync(long id)
    {
        const string sql = "DELETE FROM sales WHERE id = @id;";
        return await _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@id", id));
    }

    private static SaleRecord MapSaleRecord(NpgsqlDataReader reader)
    {
        return new SaleRecord
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            AgentId = reader.GetInt32(reader.GetOrdinal("agent_id")),
            SaleDate = reader.GetDateTime(reader.GetOrdinal("sale_date")),
            Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
            Commission = reader.GetDecimal(reader.GetOrdinal("commission")),
            Commission1 = reader.GetDecimal(reader.GetOrdinal("commission1")),
            CurrencyType = reader.IsDBNull(reader.GetOrdinal("currency_type"))
                ? null
                : reader.GetString(reader.GetOrdinal("currency_type")),
            CurrencyRate = reader.GetDecimal(reader.GetOrdinal("currency_rate")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
            AgentName = reader.IsDBNull(reader.GetOrdinal("agent_name"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("agent_name"))
        };
    }
}
