using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 MaintenanceView (Page). Data maintenance dashboard (replaces VB6 frmMaintenance).
/// </summary>
public sealed partial class MaintenanceView : Page
{
    private readonly IDatabaseMigrationService _migrationService;
    private readonly IDatabaseMaintenanceService _maintenanceService;

    public MaintenanceView()
    {
        InitializeComponent();
        _migrationService = App.Services?.GetRequiredService<IDatabaseMigrationService>()
            ?? throw new InvalidOperationException("DatabaseMigrationService not registered.");
        _maintenanceService = App.Services?.GetRequiredService<IDatabaseMaintenanceService>()
            ?? throw new InvalidOperationException("DatabaseMaintenanceService not registered.");
    }

    private async void CheckStatusButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        try
        {
            var (tableExists, agentCount) = await _migrationService.GetStatusAsync();
            StatusMessage.Text = tableExists
                ? $"Database ready: {agentCount} agents."
                : "Database not initialized.";
            StatusMessage.Foreground = ThemeBrush.Success;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = ThemeBrush.Error;
        }
    }

    private async void MigrateButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        try
        {
            await _migrationService.EnsureTablesCreatedAsync();
            await _migrationService.SeedDataAsync();
            StatusMessage.Text = "Migration and seeding completed.";
            StatusMessage.Foreground = ThemeBrush.Success;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = ThemeBrush.Error;
        }
    }
}
