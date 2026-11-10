namespace BProMax.Services;

public interface IAutoBuyService
{
    List<AutoBuyRule> GetAllRules();
    AutoBuyRule? GetRuleById(int id);
    AutoBuyRule? GetRuleByAgent(string agentName);
    int CreateRule(AutoBuyRule rule);
    int UpdateRule(AutoBuyRule rule);
    int DeleteRule(int id);
    int ToggleRule(int id);
    List<AutoBuyRule> GetEnabledRules();
    (bool triggered, AutoBuyRule rule) CheckTrigger(string agentName, decimal currentAmount);
    int IncrementExecutionCount(int ruleId);
}
