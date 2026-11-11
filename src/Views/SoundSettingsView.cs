using BProMax.Helpers;
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
    private readonly ISoundService _soundService;

    private CheckBox EnabledCheck => this.FindChildByName<CheckBox>("EnabledCheck")!;
    private Slider VolumeSlider => this.FindChildByName<Slider>("VolumeSlider")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public SoundSettingsView()
    {
        _soundService = App.Services?.GetRequiredService<ISoundService>()
            ?? throw new InvalidOperationException("SoundService not registered.");
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        EnabledCheck.IsChecked = _soundService.IsEnabled;
        VolumeSlider.Value = _soundService.IsEnabled ? 0.5 : 0;
    }

    private async void TestAlertButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        try
        {
            await _soundService.PlayAlertSoundAsync();
            StatusMessage.Text = "Alert sound played.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private async void TestNotificationButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        try
        {
            await _soundService.PlayNotificationSoundAsync();
            StatusMessage.Text = "Notification sound played.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        _soundService.IsEnabled = EnabledCheck.IsChecked == true;
        _soundService.SetVolume(VolumeSlider.Value);
        StatusMessage.Text = "Sound settings saved.";
        StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
    }
}
