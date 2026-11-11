using BProMax.Helpers;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 ReportView (Page). Reporting (replaces VB6 frmReport / frmF5Report).
/// </summary>
public sealed partial class ReportView : Page
{
    private readonly IReportService _reportService;

    private CalendarDatePicker StartDatePicker => this.FindChildByName<CalendarDatePicker>("StartDatePicker")!;
    private CalendarDatePicker EndDatePicker => this.FindChildByName<CalendarDatePicker>("EndDatePicker")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public ReportView()
    {
        _reportService = App.Services?.GetRequiredService<IReportService>()
            ?? throw new InvalidOperationException("ReportService not registered.");
    }

    private async void AgentReportButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        try
        {
            var pdf = await _reportService.GenerateAgentReportAsync();
            StatusMessage.Text = $"Agent report generated ({pdf.Length} bytes).";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private async void SalesReportButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        var start = StartDatePicker.Date?.DateTime ?? DateTime.Today.AddDays(-30);
        var end = EndDatePicker.Date?.DateTime ?? DateTime.Today;

        try
        {
            var pdf = await _reportService.GenerateSalesReportAsync(start, end);
            StatusMessage.Text = $"Sales report generated ({pdf.Length} bytes).";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = "Export functionality not yet implemented.";
        StatusMessage.Foreground = (Application.Current.Resources["SystemControlWarningTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange);
    }
}
