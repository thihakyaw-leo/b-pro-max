using BProMax.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;

namespace BProMax.Services;

public class DatabaseService : IDatabaseService
{
    private readonly DatabaseSettings _settings;
    private readonly ILogger<DatabaseService> _logger;

    private static readonly Action<ILogger, Exception?> _logConnectionTestFailed =
        LoggerMessage.Define(LogLevel.Error, 1, "Database connection test failed");

    public DatabaseService(IOptions<DatabaseSettings> settings, ILogger<DatabaseService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_settings.ConnectionString);
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logConnectionTestFailed(_logger, ex);
            return false;
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, params NpgsqlParameter[] parameters)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(sql, conn);
        if (parameters != null)
            cmd.Parameters.AddRange(parameters);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<NpgsqlDataReader> ExecuteReaderAsync(string sql, params NpgsqlParameter[] parameters)
    {
        var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(sql, conn);
        if (parameters != null)
            cmd.Parameters.AddRange(parameters);
        return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
    }

    public async Task<T?> QuerySingleAsync<T>(string sql, Func<NpgsqlDataReader, T> map, params NpgsqlParameter[] parameters)
    {
        using var reader = await ExecuteReaderAsync(sql, parameters);
        if (await reader.ReadAsync())
            return map(reader);
        return default;
    }

    public async Task<List<T>> QueryListAsync<T>(string sql, Func<NpgsqlDataReader, T> map, params NpgsqlParameter[] parameters)
    {
        var results = new List<T>();
        using var reader = await ExecuteReaderAsync(sql, parameters);
        while (await reader.ReadAsync())
            results.Add(map(reader));
        return results;
    }

}
