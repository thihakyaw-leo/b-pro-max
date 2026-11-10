using System.Globalization;
using BProMax.Helpers;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 TimeLockView (Page). Time-based access control (replaces VB6 frmTimeLock).
/// </summary>
public sealed partial class TimeLockView : Page
{
    private readonly ITimeLockService _timeLockService;

    private CheckBox EnabledCheck => this.FindChildByName<CheckBox>("EnabledCheck")!;
    private TextBox StartHourBox => this.FindChildByName<TextBox>("StartHourBox")!;
    private TextBox EndHourBox => this.FindChildByName<TextBox>("EndHourBox")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public TimeLockView()
    {
        _timeLockService = App.Services?.GetRequiredService<ITimeLockService>()
            ?? throw new InvalidOperationException("TimeLockService not registered.");
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        EnabledCheck.IsChecked = _timeLockService.IsEnabled;
        StartHourBox.Text = _timeLockService.StartHour.ToString(CultureInfo.InvariantCulture);
        EndHourBox.Text = _timeLockService.EndHour.ToString(CultureInfo.InvariantCulture);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        if (!int.TryParse(StartHourBox.Text, out var startHour) ||
            !int.TryParse(EndHourBox.Text, out var endHour))
        {
            StatusMessage.Text = "Invalid hour values.";
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return;
        }

        _timeLockService.IsEnabled = EnabledCheck.IsChecked == true;
        _timeLockService.StartHour = startHour;
        _timeLockService.EndHour = endHour;

        StatusMessage.Text = "Time lock settings saved.";
        StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
    }

    private void CheckButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        var within = _timeLockService.IsWithinAllowedTime();
        StatusMessage.Text = within ? "Access allowed at this time." : "Access denied at this time.";
        StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            within ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red);
    }
}
