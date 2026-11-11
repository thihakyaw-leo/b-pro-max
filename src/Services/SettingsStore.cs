using Microsoft.Extensions.Logging;
using Npgsql;

namespace BProMax.Services;

/// <summary>
/// Database-backed implementation of <see cref="ISettingsStore"/>.
/// </summary>
public class SettingsStore : ISettingsStore
{
    private readonly IDatabaseService _db;
    private readonly ILogger<SettingsStore> _logger;

    public SettingsStore(IDatabaseService db, ILogger<SettingsStore> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key)
    {
        const string sql = "SELECT value FROM app_settings WHERE key = @key;";
        try
        {
            return await _db.QuerySingleAsync(sql, r => r.IsDBNull(0) ? null : r.GetString(0),
                new NpgsqlParameter("@key", key));
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        const string sql = @"
INSERT INTO app_settings (key, value, updated_at)
VALUES (@key, @value, NOW())
ON CONFLICT (key)
DO UPDATE SET value = @value, updated_at = NOW();";

        await _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@key", key),
            new NpgsqlParameter("@value", value));
    }

    public async Task DeleteAsync(string key)
    {
        const string sql = "DELETE FROM app_settings WHERE key = @key;";
        await _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@key", key));
    }
}
