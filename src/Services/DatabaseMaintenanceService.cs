using BProMax.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BProMax.Services;

public class DatabaseMaintenanceService : IDatabaseMaintenanceService
{
    private readonly DatabaseSettings _settings;
    private readonly ILogger<DatabaseMaintenanceService> _logger;

    private static readonly Action<ILogger, Exception?> _logBackupFailed =
        LoggerMessage.Define(LogLevel.Error, 1, "Database backup failed");
    private static readonly Action<ILogger, Exception?> _logRestoreFailed =
        LoggerMessage.Define(LogLevel.Error, 2, "Database restore failed");
    private static readonly Action<ILogger, Exception?> _logCompactionCompleted =
        LoggerMessage.Define(LogLevel.Information, 3, "Database compaction completed");
    private static readonly Action<ILogger, Exception?> _logCompactionFailed =
        LoggerMessage.Define(LogLevel.Error, 4, "Database compaction failed");
    private static readonly Action<ILogger, string, Exception?> _logDatabaseCreated =
        LoggerMessage.Define<string>(LogLevel.Information, 5, "Database {DatabaseName} created");
    private static readonly Action<ILogger, string, Exception?> _logCreateDatabaseFailed =
        LoggerMessage.Define<string>(LogLevel.Error, 6, "Failed to create database {DatabaseName}");

    public DatabaseMaintenanceService(IOptions<DatabaseSettings> settings, ILogger<DatabaseMaintenanceService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> BackupDatabaseAsync(string backupPath)
    {
        try
        {
            var connectionString = new Npgsql.NpgsqlConnectionStringBuilder(_settings.ConnectionString);
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-h {connectionString.Host} -p {connectionString.Port} -U {connectionString.Username} -d {connectionString.Database} -f \"{backupPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Set PGPASSWORD environment variable for authentication
            System.Environment.SetEnvironmentVariable("PGPASSWORD", connectionString.Password);

            using var process = System.Diagnostics.Process.Start(processStartInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logBackupFailed(_logger, ex);
            return false;
        }
    }

    public async Task<bool> RestoreDatabaseAsync(string backupPath)
    {
        try
        {
            var connectionString = new Npgsql.NpgsqlConnectionStringBuilder(_settings.ConnectionString);
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "pg_restore",
                Arguments = $"-h {connectionString.Host} -p {connectionString.Port} -U {connectionString.Username} -d {connectionString.Database} -c --if-exists \"{backupPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            System.Environment.SetEnvironmentVariable("PGPASSWORD", connectionString.Password);

            using var process = System.Diagnostics.Process.Start(processStartInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logRestoreFailed(_logger, ex);
            return false;
        }
    }

    public async Task<bool> CompactDatabaseAsync()
    {
        try
        {
            using var conn = new Npgsql.NpgsqlConnection(_settings.ConnectionString);
            await conn.OpenAsync();

            // VACUUM cannot run inside a transaction, so we execute it directly
            using var cmd = new Npgsql.NpgsqlCommand("VACUUM ANALYZE", conn);
            await cmd.ExecuteNonQueryAsync();

            _logCompactionCompleted(_logger, null);
            return true;
        }
        catch (Exception ex)
        {
            _logCompactionFailed(_logger, ex);
            return false;
        }
    }

    public async Task<bool> CreateNewDatabaseAsync(string databaseName)
    {
        try
        {
            // Connect to the default 'postgres' database to create a new one
            var connectionString = new Npgsql.NpgsqlConnectionStringBuilder(_settings.ConnectionString);
            connectionString.Database = "postgres";

            using var conn = new Npgsql.NpgsqlConnection(connectionString.ConnectionString);
            await conn.OpenAsync();

            using var cmd = new Npgsql.NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", conn);
            await cmd.ExecuteNonQueryAsync();

            _logDatabaseCreated(_logger, databaseName, null);
            return true;
        }
        catch (Exception ex)
        {
            _logCreateDatabaseFailed(_logger, databaseName, ex);
            return false;
        }
    }
}
