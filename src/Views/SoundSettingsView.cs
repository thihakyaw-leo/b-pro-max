using System.Globalization;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 SoundSettingsView (Page). Sound configuration (replaces VB6 frmSoundSetting).
/// </summary>
public sealed partial class SoundSettingsView : Page
{
    private const string VolumeSettingKey = "Sound:Volume";

    private readonly ISoundService _soundService;
    private readonly ISettingsStore _settings;

    public SoundSettingsView()
    {
        InitializeComponent();
        _soundService = App.Services?.GetRequiredService<ISoundService>()
            ?? throw new InvalidOperationException("SoundService not registered.");
        _settings = App.Services?.GetRequiredService<ISettingsStore>()
            ?? throw new InvalidOperationException("SettingsStore not registered.");
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        EnabledCheck.IsChecked = _soundService.IsEnabled;

        // Load persisted volume; default to 0.5.
        var volumeStr = await _settings.GetAsync(VolumeSettingKey);
        double volume = 0.5;
        if (!string.IsNullOrEmpty(volumeStr) && double.TryParse(volumeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            volume = Math.Clamp(parsed, 0, 1);
        }
        VolumeSlider.Value = volume;
    }

    private async void TestAlertButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        try
        {
            await _soundService.PlayAlertSoundAsync();
            StatusMessage.Text = "Alert sound played.";
            StatusMessage.Foreground = ThemeBrush.Success;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = ThemeBrush.Error;
        }
    }

    private async void TestNotificationButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        try
        {
            await _soundService.PlayNotificationSoundAsync();
            StatusMessage.Text = "Notification sound played.";
            StatusMessage.Foreground = ThemeBrush.Success;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = ThemeBrush.Error;
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        _soundService.IsEnabled = EnabledCheck.IsChecked == true;
        var volume = VolumeSlider.Value;
        _soundService.SetVolume(volume);

        // Persist volume.
        await _settings.SetAsync(VolumeSettingKey, volume.ToString(CultureInfo.InvariantCulture));

        StatusMessage.Text = "Sound settings saved.";
        StatusMessage.Foreground = ThemeBrush.Success;
    }
}
