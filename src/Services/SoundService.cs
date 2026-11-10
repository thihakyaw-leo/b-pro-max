using System.Runtime.Versioning;
using BProMax.Configuration;
using Microsoft.Extensions.Options;

namespace BProMax.Services;

[SupportedOSPlatform("windows")]
public class SoundService : ISoundService
{
    private readonly SoundSettings _settings;

    public SoundService(IOptions<SoundSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool IsEnabled
    {
        get => _settings.Enabled;
        set => _settings.Enabled = value;
    }

    public async Task PlayAlertSoundAsync()
    {
        if (!IsEnabled) return;
        try
        {
            Console.Beep(800, 200); // System alert beep
            await Task.CompletedTask;
        }
        catch
        {
            // Sound not available, continue silently
        }
    }

    public async Task PlayNotificationSoundAsync()
    {
        if (!IsEnabled) return;
        try
        {
            Console.Beep(600, 150); // Notification beep
            await Task.CompletedTask;
        }
        catch
        {
            // Sound not available, continue silently
        }
    }

    public void SetVolume(double volume)
    {
        _settings.Volume = Math.Clamp(volume, 0, 1);
    }
}
