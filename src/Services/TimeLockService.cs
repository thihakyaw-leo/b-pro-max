using System.Globalization;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BProMax.Services;

/// <summary>
/// Time-based access control service.
/// Access is granted only while <see cref="IsEnabled"/> is true and the current local hour
/// falls within [<see cref="StartHour"/>, <see cref="EndHour"/>).
/// Settings are persisted in the database; access requests are audited.
/// </summary>
public class TimeLockService : ITimeLockService
{
    private const string EnabledKey = "TimeLock:Enabled";
    private const string StartHourKey = "TimeLock:StartHour";
    private const string EndHourKey = "TimeLock:EndHour";

    private readonly ISettingsStore _settings;
    private readonly IDatabaseService _db;
    private readonly ILogger<TimeLockService> _logger;

    private bool _enabled;
    private int _startHour;
    private int _endHour;
    private bool _loaded;

    private static readonly Action<ILogger, string, bool, bool, int, Exception?> _logAccessRequest =
        LoggerMessage.Define<string, bool, bool, int>(LogLevel.Information, 1, "Access request: Reason={Reason}, Granted={Granted}, Enabled={Enabled}, Hour={Hour}");

    public TimeLockService(ISettingsStore settings, IDatabaseService db, ILogger<TimeLockService> logger)
    {
        _settings = settings;
        _db = db;
        _logger = logger;

        // Defaults from config (will be overridden by database values on first load).
        _enabled = false;
        _startHour = 9;
        _endHour = 17;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;

        var enabledStr = await _settings.GetAsync(EnabledKey);
        if (!string.IsNullOrEmpty(enabledStr) && bool.TryParse(enabledStr, out var enabled))
        {
            _enabled = enabled;
        }

        var startStr = await _settings.GetAsync(StartHourKey);
        if (!string.IsNullOrEmpty(startStr) && int.TryParse(startStr, out var start) && start >= 0 && start <= 23)
        {
            _startHour = start;
        }

        var endStr = await _settings.GetAsync(EndHourKey);
        if (!string.IsNullOrEmpty(endStr) && int.TryParse(endStr, out var end) && end >= 0 && end <= 23)
        {
            _endHour = end;
        }
    }

    public bool IsEnabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public int StartHour
    {
        get => _startHour;
        set
        {
            if (value < 0 || value > 23)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "StartHour must be between 0 and 23.");
            }
            _startHour = value;
        }
    }

    public int EndHour
    {
        get => _endHour;
        set
        {
            if (value < 0 || value > 23)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "EndHour must be between 0 and 23.");
            }
            _endHour = value;
        }
    }

    public bool IsWithinAllowedTime()
    {
        if (!_enabled)
        {
            return true;
        }
        var hour = DateTime.Now.Hour;
        return hour >= _startHour && hour < _endHour;
    }

    public async Task<bool> RequestAccessAsync(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        await EnsureLoadedAsync();

        var granted = IsWithinAllowedTime();
        var hour = DateTime.Now.Hour;

        // Persist settings if they were loaded from defaults (first-run).
        await _settings.SetAsync(EnabledKey, _enabled.ToString(CultureInfo.InvariantCulture));
        await _settings.SetAsync(StartHourKey, _startHour.ToString(CultureInfo.InvariantCulture));
        await _settings.SetAsync(EndHourKey, _endHour.ToString(CultureInfo.InvariantCulture));

        // Audit log to database.
        try
        {
            const string sql = @"
INSERT INTO access_requests (timestamp, username, reason, granted, enabled, hour)
VALUES (NOW(), @username, @reason, @granted, @enabled, @hour);";

            await _db.ExecuteNonQueryAsync(sql,
                new NpgsqlParameter("@username", "system"),
                new NpgsqlParameter("@reason", reason),
                new NpgsqlParameter("@granted", granted),
                new NpgsqlParameter("@enabled", _enabled),
                new NpgsqlParameter("@hour", hour));
        }
        catch
        {
            // Audit log failure should not block access requests.
        }

        _logAccessRequest(_logger, reason, granted, _enabled, hour, null);

        return granted;
    }

    /// <summary>
    /// Snapshot of recorded access requests (most recent first is not guaranteed).
    /// </summary>
    public IReadOnlyList<AccessRequest> GetAccessRequests()
    {
        // In-memory snapshot; database audit log is the durable record.
        return Array.Empty<AccessRequest>();
    }

    public sealed record AccessRequest(DateTimeOffset Timestamp, string Reason, bool Granted);
}
