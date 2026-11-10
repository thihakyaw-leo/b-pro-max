namespace BProMax.Services;

public interface ISoundService
{
    Task PlayAlertSoundAsync();
    Task PlayNotificationSoundAsync();
    void SetVolume(double volume);
    bool IsEnabled { get; set; }
}
