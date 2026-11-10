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
    private readonly List<AutoBuyRule> _rules = new();
    private int _nextId = 1;

    public List<AutoBuyRule> GetAllRules() => _rules.OrderBy(r => r.AgentName).ToList();

    public AutoBuyRule? GetRuleById(int id) => _rules.FirstOrDefault(r => r.Id == id);

    public AutoBuyRule? GetRuleByAgent(string agentName) =>
        _rules.FirstOrDefault(r => r.AgentName.Equals(agentName, StringComparison.OrdinalIgnoreCase));

    public int CreateRule(AutoBuyRule rule)
    {
        rule.Id = _nextId++;
        rule.CreatedAt = DateTime.UtcNow;
        _rules.Add(rule);
        return rule.Id;
    }

    public int UpdateRule(AutoBuyRule rule)
    {
        var existing = GetRuleById(rule.Id);
        if (existing == null) return 0;

        existing.AgentName = rule.AgentName;
        existing.TriggerAmount = rule.TriggerAmount;
        existing.AutoBuyAmount = rule.AutoBuyAmount;
        existing.IsEnabled = rule.IsEnabled;
        return 1;
    }

    public int DeleteRule(int id)
    {
        var rule = GetRuleById(id);
        if (rule == null) return 0;
        _rules.Remove(rule);
        return 1;
    }

    public int ToggleRule(int id)
    {
        var rule = GetRuleById(id);
        if (rule == null) return 0;
        rule.IsEnabled = !rule.IsEnabled;
        return 1;
    }

    public List<AutoBuyRule> GetEnabledRules() =>
        _rules.Where(r => r.IsEnabled).OrderBy(r => r.AgentName).ToList();

    public (bool triggered, AutoBuyRule rule) CheckTrigger(string agentName, decimal currentAmount)
    {
        var rule = GetRuleByAgent(agentName);
        if (rule == null || !rule.IsEnabled)
            return (false, null!);

        var triggered = currentAmount >= rule.TriggerAmount;
        return (triggered, rule);
    }

    public int IncrementExecutionCount(int ruleId)
    {
        var rule = GetRuleById(ruleId);
        if (rule == null) return 0;
        rule.ExecutionCount++;
        rule.LastExecuted = DateTime.UtcNow;
        return 1;
    }
}
