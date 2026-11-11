using BProMax.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

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
            var connectionString = new NpgsqlConnectionStringBuilder(_settings.ConnectionString);
            var env = System.Environment.GetEnvironmentVariables();

            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-Fc -h {connectionString.Host} -p {connectionString.Port} -U {connectionString.Username} -d {connectionString.Database} -f \"{backupPath}\" --no-owner --no-privileges",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Set PGPASSWORD only for this process, not process-wide.
            processStartInfo.Environment["PGPASSWORD"] = connectionString.Password;

            using var process = System.Diagnostics.Process.Start(processStartInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logBackupFailed(_logger, new InvalidOperationException($"pg_dump exited with code {process.ExitCode}"));
                return false;
            }

            // Verify the backup file exists and is non-empty.
            if (!System.IO.File.Exists(backupPath) || new System.IO.FileInfo(backupPath).Length == 0)
            {
                _logBackupFailed(_logger, new InvalidOperationException("Backup file is missing or empty."));
                return false;
            }

            return true;
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
            if (!System.IO.File.Exists(backupPath))
            {
                _logRestoreFailed(_logger, new FileNotFoundException("Backup file not found.", backupPath));
                return false;
            }

            var connectionString = new NpgsqlConnectionStringBuilder(_settings.ConnectionString);
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "pg_restore",
                Arguments = $"-h {connectionString.Host} -p {connectionString.Port} -U {connectionString.Username} -d {connectionString.Database} -c --if-exists --no-owner --no-privileges \"{backupPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            processStartInfo.Environment["PGPASSWORD"] = connectionString.Password;

            using var process = System.Diagnostics.Process.Start(processStartInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logRestoreFailed(_logger, new InvalidOperationException($"pg_restore exited with code {process.ExitCode}"));
                return false;
            }

            return true;
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
            using var conn = new NpgsqlConnection(_settings.ConnectionString);
            await conn.OpenAsync();

            // VACUUM cannot run inside a transaction, so we execute it directly
            using var cmd = new NpgsqlCommand("VACUUM ANALYZE", conn);
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
            // Validate database name before using it in SQL.
            if (!IsValidDatabaseName(databaseName))
            {
                _logCreateDatabaseFailed(_logger, databaseName, new ArgumentException("Invalid database name."));
                return false;
            }

            // Connect to the default 'postgres' database to create a new one
            var connectionString = new NpgsqlConnectionStringBuilder(_settings.ConnectionString);
            connectionString.Database = "postgres";

            using var conn = new NpgsqlConnection(connectionString.ConnectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", conn);
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

    private static bool IsValidDatabaseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        // Reject reserved database names.
        var lower = name.ToLowerInvariant();
        if (lower == "postgres" || lower == "template0" || lower == "template1")
        {
            return false;
        }

        // Allow only alphanumeric, underscore, and hyphen; must start with letter or underscore.
        if (!char.IsLetter(name[0]) && name[0] != '_')
        {
            return false;
        }

        foreach (var c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
            {
                return false;
            }
        }

        // PostgreSQL identifier length limit is 63 bytes.
        if (name.Length > 63)
        {
            return false;
        }

        return true;
    }
}
