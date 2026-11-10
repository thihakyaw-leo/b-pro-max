namespace BProMax.Services;

public interface IDatabaseMigrationService
{
    Task<bool> AgentsTableExistsAsync();
    Task EnsureTablesCreatedAsync();
    Task SeedDataAsync();
    Task<(bool tableExists, int agentCount)> GetStatusAsync();
}
