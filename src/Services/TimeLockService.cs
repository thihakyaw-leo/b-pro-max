using Microsoft.Extensions.Options;
using BProMax.Configuration;

namespace BProMax.Services;

public class TimeLockService : ITimeLockService
{
    private readonly TimeLockSettings _settings;

    public TimeLockService(IOptions<TimeLockSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool IsEnabled
    {
        get => _settings.Enabled;
        set => _settings.Enabled = value;
    }

    public int StartHour
    {
        get => _settings.StartHour;
        set => _settings.StartHour = value;
    }

    public int EndHour
    {
        get => _settings.EndHour;
        set => _settings.EndHour = value;
    }

    public bool IsWithinAllowedTime()
    {
        if (!IsEnabled) return true;
        var hour = DateTime.Now.Hour;
        return hour >= StartHour && hour < EndHour;
    }

    public Task<bool> RequestAccessAsync(string reason)
    {
        // TODO: Implement access request logging
        return Task.FromResult(true);
    }
}
