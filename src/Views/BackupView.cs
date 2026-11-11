using BProMax.Helpers;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 BackupView (Page). Database backup/restore (replaces VB6 frmBackUpData).
/// </summary>
public sealed partial class BackupView : Page
{
    private readonly IDatabaseMaintenanceService _maintenanceService;

    private TextBox BackupPathBox => this.FindChildByName<TextBox>("BackupPathBox")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public BackupView()
    {
        _maintenanceService = App.Services?.GetRequiredService<IDatabaseMaintenanceService>()
            ?? throw new InvalidOperationException("DatabaseMaintenanceService not registered.");
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            FileTypeChoices = { { "Backup", new List<string> { ".bak" } } },
            SuggestedFileName = "backup.bak"
        };
        InitializeWithWindow.Initialize(picker, App.MainWindowPtr);

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            BackupPathBox.Text = file.Path;
        }
    }

    private async void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        var path = BackupPathBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage.Text = "Please specify a backup path.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return;
        }

        try
        {
            var ok = await _maintenanceService.BackupDatabaseAsync(path);
            StatusMessage.Text = ok ? "Backup completed successfully." : "Backup failed.";
            StatusMessage.Foreground = (Application.Current.Resources[ok ? "SystemControlSuccessTextForegroundBrush" : "SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(ok ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        var path = BackupPathBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusMessage.Text = "Please specify a backup path.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Restore Database",
            Content = "Are you sure you want to restore from this backup? This will overwrite current data.",
            PrimaryButtonText = "Restore",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            try
            {
                var ok = await _maintenanceService.RestoreDatabaseAsync(path);
                StatusMessage.Text = ok ? "Restore completed successfully." : "Restore failed.";
                StatusMessage.Foreground = (Application.Current.Resources[ok ? "SystemControlSuccessTextForegroundBrush" : "SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(ok ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red);
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Error: {ex.Message}";
                StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }
    }
}
