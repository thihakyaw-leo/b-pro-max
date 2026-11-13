using System.Globalization;
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

    public TimeLockView()
    {
        InitializeComponent();
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

        if (!int.TryParse(StartHourBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var startHour) ||
            !int.TryParse(EndHourBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var endHour))
        {
            StatusMessage.Text = "Invalid hour values. Please enter whole numbers 0–23.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return;
        }

        if (startHour < 0 || startHour > 23 || endHour < 0 || endHour > 23)
        {
            StatusMessage.Text = "Hours must be between 0 and 23.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return;
        }

        try
        {
            _timeLockService.IsEnabled = EnabledCheck.IsChecked == true;
            _timeLockService.StartHour = startHour;
            _timeLockService.EndHour = endHour;

            if (_timeLockService.IsEnabled && _timeLockService.StartHour >= _timeLockService.EndHour)
            {
                StatusMessage.Text = "Start hour must be less than end hour.";
                StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                return;
            }

            StatusMessage.Text = "Time lock settings saved.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            StatusMessage.Text = ex.Message;
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage.Text = ex.Message;
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private async void CheckButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        var within = await _timeLockService.RequestAccessAsync("Time lock check");
        StatusMessage.Text = within ? "Access allowed at this time." : "Access denied at this time.";
        StatusMessage.Foreground = (Application.Current.Resources[within ? "SystemControlSuccessTextForegroundBrush" : "SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(within ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red);
    }
}
