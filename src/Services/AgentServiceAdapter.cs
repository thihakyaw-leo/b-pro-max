using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.Logging;

namespace BProMax.Services;

/// <summary>
/// Adapter that wraps ISupabaseAgentService to implement IAgentService,
/// converting between SupabaseAgent and Agent domain models.
/// </summary>
public class AgentServiceAdapter : IAgentService
{
    private readonly ISupabaseAgentService _supabaseService;
    private readonly ILogger<AgentServiceAdapter> _logger;

    public AgentServiceAdapter(ISupabaseAgentService supabaseService, ILogger<AgentServiceAdapter> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    public async Task<List<Agent>> GetAllAgentsAsync()
    {
        var supabaseAgents = await _supabaseService.GetAllAgentsAsync();
        return supabaseAgents.Select(MapToAgent).ToList();
    }

    public async Task<Agent?> GetAgentByIdAsync(int id)
    {
        var supabaseAgent = await _supabaseService.GetAgentByIdAsync(id);
        return supabaseAgent == null ? null : MapToAgent(supabaseAgent);
    }

    public async Task<Agent?> GetAgentByNameAsync(string name)
    {
        var supabaseAgent = await _supabaseService.GetAgentByNameAsync(name);
        return supabaseAgent == null ? null : MapToAgent(supabaseAgent);
    }

    public async Task<List<Agent>> SearchAgentsAsync(string searchTerm)
    {
        var supabaseAgents = await _supabaseService.SearchAgentsAsync(searchTerm);
        return supabaseAgents.Select(MapToAgent).ToList();
    }

    public async Task<int> CreateAgentAsync(Agent agent)
    {
        var supabaseAgent = MapToSupabaseAgent(agent);
        return await _supabaseService.CreateAgentAsync(supabaseAgent);
    }

    public async Task<int> UpdateAgentAsync(Agent agent)
    {
        var supabaseAgent = MapToSupabaseAgent(agent);
        return await _supabaseService.UpdateAgentAsync(supabaseAgent);
    }

    public async Task<int> DeleteAgentAsync(int id)
    {
        return await _supabaseService.DeleteAgentAsync(id);
    }

    public async Task<int> DeleteAgentByNameAsync(string name)
    {
        return await _supabaseService.DeleteAgentByNameAsync(name);
    }

    private static Agent MapToAgent(SupabaseAgent s)
    {
        return new Agent
        {
            Id = s.Id,
            AgentName = s.AgentName,
            Address = s.Address,
            Phone = s.Phone,
            Commission = s.Commission,
            Rate = s.Rate,
            Commission1 = s.Commission1,
            Rate1 = s.Rate1,
            CurrencyType = s.CurrencyType,
            CurrencyRate = s.CurrencyRate,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }

    private static SupabaseAgent MapToSupabaseAgent(Agent a)
    {
        return new SupabaseAgent
        {
            Id = a.Id,
            AgentName = a.AgentName,
            Address = a.Address,
            Phone = a.Phone,
            Commission = a.Commission,
            Rate = a.Rate,
            Commission1 = a.Commission1,
            Rate1 = a.Rate1,
            CurrencyType = a.CurrencyType,
            CurrencyRate = a.CurrencyRate,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }
}
