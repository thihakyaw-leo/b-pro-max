namespace BProMax.Services;

public interface IDatabaseMaintenanceService
{
    Task<bool> BackupDatabaseAsync(string backupPath);
    Task<bool> RestoreDatabaseAsync(string backupPath);
    Task<bool> CompactDatabaseAsync();
    Task<bool> CreateNewDatabaseAsync(string databaseName);
}
