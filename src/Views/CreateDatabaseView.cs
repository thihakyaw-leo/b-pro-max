using BProMax.Helpers;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 CreateDatabaseView (Page). New database creation (replaces VB6 frmCreateNewDatabase).
/// </summary>
public sealed partial class CreateDatabaseView : Page
{
    private readonly IDatabaseMaintenanceService _maintenanceService;

    private TextBox DatabaseNameBox => this.FindChildByName<TextBox>("DatabaseNameBox")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public CreateDatabaseView()
    {
        _maintenanceService = App.Services?.GetRequiredService<IDatabaseMaintenanceService>()
            ?? throw new InvalidOperationException("DatabaseMaintenanceService not registered.");
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        var dbName = DatabaseNameBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(dbName))
        {
            StatusMessage.Text = "Please enter a database name.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return;
        }

        try
        {
            var ok = await _maintenanceService.CreateNewDatabaseAsync(dbName);
            StatusMessage.Text = ok ? $"Database '{dbName}' created." : "Database creation failed.";
            StatusMessage.Foreground = (Application.Current.Resources[ok ? "SystemControlSuccessTextForegroundBrush" : "SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(ok ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }
}
