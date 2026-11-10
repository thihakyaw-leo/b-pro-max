using BProMax.Models;

namespace BProMax.Services;

public interface IAgentService
{
    Task<List<Agent>> GetAllAgentsAsync();
    Task<Agent?> GetAgentByIdAsync(int id);
    Task<Agent?> GetAgentByNameAsync(string name);
    Task<List<Agent>> SearchAgentsAsync(string searchTerm);
    Task<int> CreateAgentAsync(Agent agent);
    Task<int> UpdateAgentAsync(Agent agent);
    Task<int> DeleteAgentAsync(int id);
    Task<int> DeleteAgentByNameAsync(string name);
}
