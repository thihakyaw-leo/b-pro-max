using BProMax.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BProMax.Services;

public class AgentService : IAgentService
{
    private readonly IDatabaseService _db;
    private readonly ILogger<AgentService> _logger;

    public AgentService(IDatabaseService db, ILogger<AgentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Agent>> GetAllAgentsAsync()
    {
        const string sql = "SELECT * FROM agents ORDER BY agent_name";
        return await _db.QueryListAsync(sql, MapAgent);
    }

    public async Task<Agent?> GetAgentByIdAsync(int id)
    {
        const string sql = "SELECT * FROM agents WHERE id = @id";
        return await _db.QuerySingleAsync(sql, MapAgent, new NpgsqlParameter("@id", id));
    }

    public async Task<Agent?> GetAgentByNameAsync(string name)
    {
        const string sql = "SELECT * FROM agents WHERE agent_name = @name";
        return await _db.QuerySingleAsync(sql, MapAgent, new NpgsqlParameter("@name", name));
    }

    public async Task<List<Agent>> SearchAgentsAsync(string searchTerm)
    {
        const string sql = "SELECT * FROM agents WHERE agent_name ILIKE @search ORDER BY agent_name";
        return await _db.QueryListAsync(sql, MapAgent, new NpgsqlParameter("@search", $"%{searchTerm}%"));
    }

    public async Task<int> CreateAgentAsync(Agent agent)
    {
        const string sql = @"
            INSERT INTO agents (agent_name, address, phone, commission, rate, commission1, rate1, currency_type, currency_rate, updated_at)
            VALUES (@agent_name, @address, @phone, @commission, @rate, @commission1, @rate1, @currency_type, @currency_rate, @updated_at)
            RETURNING id";
        var result = await _db.QuerySingleAsync(sql, MapNewId,
            new NpgsqlParameter("@agent_name", agent.AgentName),
            new NpgsqlParameter("@address", agent.Address ?? (object)DBNull.Value),
            new NpgsqlParameter("@phone", agent.Phone ?? (object)DBNull.Value),
            new NpgsqlParameter("@commission", agent.Commission),
            new NpgsqlParameter("@rate", agent.Rate),
            new NpgsqlParameter("@commission1", agent.Commission1),
            new NpgsqlParameter("@rate1", agent.Rate1),
            new NpgsqlParameter("@currency_type", agent.CurrencyType ?? (object)DBNull.Value),
            new NpgsqlParameter("@currency_rate", agent.CurrencyRate),
            new NpgsqlParameter("@updated_at", DateTime.UtcNow));
        return result;
    }

    public async Task<int> UpdateAgentAsync(Agent agent)
    {
        const string sql = @"
            UPDATE agents 
            SET agent_name = @agent_name, address = @address, phone = @phone, commission = @commission, rate = @rate,
                commission1 = @commission1, rate1 = @rate1, currency_type = @currency_type, 
                currency_rate = @currency_rate, updated_at = @updated_at
            WHERE id = @id";
        return await _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@id", agent.Id),
            new NpgsqlParameter("@agent_name", agent.AgentName),
            new NpgsqlParameter("@address", agent.Address ?? (object)DBNull.Value),
            new NpgsqlParameter("@phone", agent.Phone ?? (object)DBNull.Value),
            new NpgsqlParameter("@commission", agent.Commission),
            new NpgsqlParameter("@rate", agent.Rate),
            new NpgsqlParameter("@commission1", agent.Commission1),
            new NpgsqlParameter("@rate1", agent.Rate1),
            new NpgsqlParameter("@currency_type", agent.CurrencyType ?? (object)DBNull.Value),
            new NpgsqlParameter("@currency_rate", agent.CurrencyRate),
            new NpgsqlParameter("@updated_at", DateTime.UtcNow));
    }

    public async Task<int> DeleteAgentAsync(int id)
    {
        const string sql = "DELETE FROM agents WHERE id = @id";
        return await _db.ExecuteNonQueryAsync(sql, new NpgsqlParameter("@id", id));
    }

    public async Task<int> DeleteAgentByNameAsync(string name)
    {
        const string sql = "DELETE FROM agents WHERE agent_name = @name";
        return await _db.ExecuteNonQueryAsync(sql, new NpgsqlParameter("@name", name));
    }

    private static Agent MapAgent(NpgsqlDataReader reader)
    {
        return new Agent
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            AgentName = reader.GetString(reader.GetOrdinal("agent_name")),
            Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
            Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
            Commission = reader.GetDecimal(reader.GetOrdinal("commission")),
            Rate = reader.GetDecimal(reader.GetOrdinal("rate")),
            Commission1 = reader.GetDecimal(reader.GetOrdinal("commission1")),
            Rate1 = reader.GetDecimal(reader.GetOrdinal("rate1")),
            CurrencyType = reader.IsDBNull(reader.GetOrdinal("currency_type")) ? null : reader.GetString(reader.GetOrdinal("currency_type")),
            CurrencyRate = reader.GetDecimal(reader.GetOrdinal("currency_rate")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    private static int MapNewId(NpgsqlDataReader reader)
    {
        return reader.GetInt32(0);
    }
}
