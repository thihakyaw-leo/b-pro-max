using Microsoft.Extensions.Logging;
using Npgsql;

namespace BProMax.Services;

public class AutoBuyRule
{
    public int Id { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public decimal TriggerAmount { get; set; }
    public decimal AutoBuyAmount { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastExecuted { get; set; }
    public int ExecutionCount { get; set; }
}

public class AutoBuyService : IAutoBuyService
{
    private readonly IDatabaseService _db;
    private readonly ILogger<AutoBuyService> _logger;

    public AutoBuyService(IDatabaseService db, ILogger<AutoBuyService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<AutoBuyRule>> GetAllRulesAsync()
    {
        const string sql = @"
SELECT id, agent_name, trigger_amount, auto_buy_amount, is_enabled,
       created_at, last_executed, execution_count
FROM auto_buy_rules
ORDER BY agent_name;";

        return await _db.QueryListAsync(sql, MapRule);
    }

    public Task<AutoBuyRule?> GetRuleByIdAsync(int id)
    {
        const string sql = @"
SELECT id, agent_name, trigger_amount, auto_buy_amount, is_enabled,
       created_at, last_executed, execution_count
FROM auto_buy_rules
WHERE id = @id;";

        return _db.QuerySingleAsync(sql, MapRule,
            new NpgsqlParameter("@id", id));
    }

    public Task<AutoBuyRule?> GetRuleByAgentAsync(string agentName)
    {
        const string sql = @"
SELECT id, agent_name, trigger_amount, auto_buy_amount, is_enabled,
       created_at, last_executed, execution_count
FROM auto_buy_rules
WHERE LOWER(agent_name) = LOWER(@agentName);";

        return _db.QuerySingleAsync(sql, MapRule,
            new NpgsqlParameter("@agentName", agentName));
    }

    public async Task<int> CreateRuleAsync(AutoBuyRule rule)
    {
        var existing = await GetRuleByAgentAsync(rule.AgentName);
        if (existing != null)
        {
            throw new InvalidOperationException($"Auto-buy rule for '{rule.AgentName}' already exists.");
        }

        const string sql = @"
INSERT INTO auto_buy_rules (agent_name, trigger_amount, auto_buy_amount, is_enabled, created_at)
VALUES (@agent_name, @trigger_amount, @auto_buy_amount, @is_enabled, NOW())
RETURNING id;";

        return await _db.QuerySingleAsync(sql, r => r.GetInt32(0),
            new NpgsqlParameter("@agent_name", rule.AgentName),
            new NpgsqlParameter("@trigger_amount", rule.TriggerAmount),
            new NpgsqlParameter("@auto_buy_amount", rule.AutoBuyAmount),
            new NpgsqlParameter("@is_enabled", rule.IsEnabled));
    }

    public async Task<int> UpdateRuleAsync(AutoBuyRule rule)
    {
        var existing = await GetRuleByIdAsync(rule.Id);
        if (existing == null) return 0;

        // Check for duplicate agent name (different id)
        var byAgent = await GetRuleByAgentAsync(rule.AgentName);
        if (byAgent != null && byAgent.Id != rule.Id)
        {
            throw new InvalidOperationException($"Auto-buy rule for '{rule.AgentName}' already exists.");
        }

        const string sql = @"
UPDATE auto_buy_rules
SET agent_name = @agent_name, trigger_amount = @trigger_amount,
    auto_buy_amount = @auto_buy_amount, is_enabled = @is_enabled
WHERE id = @id;";

        return await _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@id", rule.Id),
            new NpgsqlParameter("@agent_name", rule.AgentName),
            new NpgsqlParameter("@trigger_amount", rule.TriggerAmount),
            new NpgsqlParameter("@auto_buy_amount", rule.AutoBuyAmount),
            new NpgsqlParameter("@is_enabled", rule.IsEnabled));
    }

    public async Task<int> DeleteRuleAsync(int id)
    {
        const string sql = "DELETE FROM auto_buy_rules WHERE id = @id;";
        return await _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@id", id));
    }

    public async Task<int> ToggleRuleAsync(int id)
    {
        const string sql = @"
UPDATE auto_buy_rules
SET is_enabled = NOT is_enabled
WHERE id = @id
RETURNING is_enabled;";

        var result = await _db.QuerySingleAsync(sql, r => r.GetBoolean(0),
            new NpgsqlParameter("@id", id));
        return result ? 1 : 0;
    }

    public async Task<List<AutoBuyRule>> GetEnabledRulesAsync()
    {
        var rules = await GetAllRulesAsync();
        return rules.Where(r => r.IsEnabled).OrderBy(r => r.AgentName).ToList();
    }

    public async Task<(bool triggered, AutoBuyRule rule)> CheckTriggerAsync(string agentName, decimal currentAmount)
    {
        var rules = await GetAllRulesAsync();
        var rule = rules.FirstOrDefault(r =>
            r.AgentName.Equals(agentName, StringComparison.OrdinalIgnoreCase) && r.IsEnabled);

        if (rule == null)
        {
            return (false, null!);
        }

        var triggered = currentAmount >= rule.TriggerAmount;
        return (triggered, rule);
    }

    public async Task<int> IncrementExecutionCountAsync(int ruleId)
    {
        const string sql = @"
UPDATE auto_buy_rules
SET execution_count = execution_count + 1,
    last_executed = NOW()
WHERE id = @id;";

        return await _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@id", ruleId));
    }

    private static AutoBuyRule MapRule(NpgsqlDataReader reader)
    {
        return new AutoBuyRule
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            AgentName = reader.GetString(reader.GetOrdinal("agent_name")),
            TriggerAmount = reader.GetDecimal(reader.GetOrdinal("trigger_amount")),
            AutoBuyAmount = reader.GetDecimal(reader.GetOrdinal("auto_buy_amount")),
            IsEnabled = reader.GetBoolean(reader.GetOrdinal("is_enabled")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            LastExecuted = reader.IsDBNull(reader.GetOrdinal("last_executed"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("last_executed")),
            ExecutionCount = reader.GetInt32(reader.GetOrdinal("execution_count"))
        };
    }
}
