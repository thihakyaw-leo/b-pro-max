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

    public CreateDatabaseView()
    {
        InitializeComponent();
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
            StatusMessage.Foreground = ThemeBrush.Error;
            return;
        }

        try
        {
            var ok = await _maintenanceService.CreateNewDatabaseAsync(dbName);
            StatusMessage.Text = ok ? $"Database '{dbName}' created." : "Database creation failed.";
            StatusMessage.Foreground = ok ? ThemeBrush.Success : ThemeBrush.Error;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = ThemeBrush.Error;
        }
    }
}
