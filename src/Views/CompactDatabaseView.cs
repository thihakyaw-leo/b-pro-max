using BProMax.Helpers;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 CompactDatabaseView (Page). Database compaction/repair (replaces VB6 frmCompactMsg).
/// </summary>
public sealed partial class CompactDatabaseView : Page
{
    private readonly IDatabaseMaintenanceService _maintenanceService;

    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public CompactDatabaseView()
    {
        _maintenanceService = App.Services?.GetRequiredService<IDatabaseMaintenanceService>()
            ?? throw new InvalidOperationException("DatabaseMaintenanceService not registered.");
    }

    private async void CompactButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        try
        {
            var ok = await _maintenanceService.CompactDatabaseAsync();
            StatusMessage.Text = ok ? "Database compacted successfully." : "Compaction failed.";
            StatusMessage.Foreground = (Application.Current.Resources[ok ? "SystemControlSuccessTextForegroundBrush" : "SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(ok ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }
}
