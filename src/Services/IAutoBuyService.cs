namespace BProMax.Services;

public interface IAutoBuyService
{
    Task<List<AutoBuyRule>> GetAllRulesAsync();
    Task<AutoBuyRule?> GetRuleByIdAsync(int id);
    Task<AutoBuyRule?> GetRuleByAgentAsync(string agentName);
    Task<int> CreateRuleAsync(AutoBuyRule rule);
    Task<int> UpdateRuleAsync(AutoBuyRule rule);
    Task<int> DeleteRuleAsync(int id);
    Task<int> ToggleRuleAsync(int id);
    List<AutoBuyRule> GetEnabledRules();
    (bool triggered, AutoBuyRule rule) CheckTrigger(string agentName, decimal currentAmount);
    Task<int> IncrementExecutionCountAsync(int ruleId);
}
