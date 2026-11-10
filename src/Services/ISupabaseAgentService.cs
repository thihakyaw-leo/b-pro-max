using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Service for managing agents via Supabase REST API.
/// Replaces the direct Npgsql DatabaseService for Supabase-hosted data.
/// </summary>
public interface ISupabaseAgentService
{
    Task<List<SupabaseAgent>> GetAllAgentsAsync();
    Task<SupabaseAgent?> GetAgentByIdAsync(int id);
    Task<SupabaseAgent?> GetAgentByNameAsync(string name);
    Task<List<SupabaseAgent>> SearchAgentsAsync(string searchTerm);
    Task<int> CreateAgentAsync(SupabaseAgent agent);
    Task<int> UpdateAgentAsync(SupabaseAgent agent);
    Task<int> DeleteAgentAsync(int id);
    Task<int> DeleteAgentByNameAsync(string name);
    Task<bool> TestConnectionAsync();
}
