using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 ReportView (Page). Reporting with PDF/Excel/CSV export (replaces VB6 frmReport / frmF5Report).
/// </summary>
public sealed partial class ReportView : Page
{
    private readonly IReportService _reportService;
    private readonly IExcelExportService _excelExportService;
    private readonly ICsvExportService _csvExportService;
    private readonly ITwoDExportService _twoDExportService;

    public ReportView()
    {
        InitializeComponent();
        _reportService = App.Services?.GetRequiredService<IReportService>()
            ?? throw new InvalidOperationException("ReportService not registered.");
        _excelExportService = App.Services?.GetRequiredService<IExcelExportService>()
            ?? throw new InvalidOperationException("ExcelExportService not registered.");
        _csvExportService = App.Services?.GetRequiredService<ICsvExportService>()
            ?? throw new InvalidOperationException("CsvExportService not registered.");
        _twoDExportService = App.Services?.GetRequiredService<ITwoDExportService>()
            ?? throw new InvalidOperationException("TwoDExportService not registered.");
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        GenerateButton.IsEnabled = false;

        try
        {
            var reportType = ((ComboBoxItem)ReportTypeCombo.SelectedItem)?.Content?.ToString() ?? "Agents";
            var format = ((ComboBoxItem)ExportFormatCombo.SelectedItem)?.Content?.ToString() ?? "PDF";

            byte[] data;
            string extension;
            string description;

            if (reportType == "Agents")
            {
                switch (format)
                {
                    case "PDF":
                        data = await _reportService.GenerateAgentReportAsync();
                        extension = ".pdf";
                        description = "Agent report";
                        break;
                    case "Excel":
                        data = await _excelExportService.ExportAgentsToExcelAsync();
                        extension = ".xlsx";
                        description = "Agent Excel export";
                        break;
                    case "CSV":
                        data = await _csvExportService.ExportAgentsToCsvAsync();
                        extension = ".csv";
                        description = "Agent CSV export";
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown format: {format}");
                }
            }
            else if (reportType == "Sales") // Sales
            {
                var start = StartDatePicker.Date?.DateTime ?? DateTime.Today.AddDays(-30);
                var end = EndDatePicker.Date?.DateTime ?? DateTime.Today;

                switch (format)
                {
                    case "PDF":
                        data = await _reportService.GenerateSalesReportAsync(start, end);
                        extension = ".pdf";
                        description = "Sales report";
                        break;
                    case "Excel":
                        data = await _excelExportService.ExportSalesToExcelAsync();
                        extension = ".xlsx";
                        description = "Sales Excel export";
                        break;
                    case "CSV":
                        data = await _csvExportService.ExportSalesToCsvAsync();
                        extension = ".csv";
                        description = "Sales CSV export";
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown format: {format}");
                }
            }
            else
            {
                var kind = reportType switch
                {
                    "2D Sales by Draw" => TwoDReportKind.SalesByDraw,
                    "2D Sales by Number" => TwoDReportKind.SalesByNumber,
                    "2D Group Performance" => TwoDReportKind.GroupPerformance,
                    "2D Payout Register" => TwoDReportKind.PayoutRegister,
                    _ => throw new InvalidOperationException($"Unknown report type: {reportType}")
                };

                var fromDate = StartDatePicker.Date.HasValue
                    ? DateOnly.FromDateTime(StartDatePicker.Date.Value.DateTime)
                    : (DateOnly?)null;
                var toDate = EndDatePicker.Date.HasValue
                    ? DateOnly.FromDateTime(EndDatePicker.Date.Value.DateTime)
                    : (DateOnly?)null;

                switch (format)
                {
                    case "PDF":
                        data = await _twoDExportService.ExportPdfAsync(kind, fromDate, toDate);
                        extension = ".pdf";
                        break;
                    case "Excel":
                        data = await _twoDExportService.ExportExcelAsync(kind, fromDate, toDate);
                        extension = ".xlsx";
                        break;
                    case "CSV":
                        data = await _twoDExportService.ExportCsvAsync(kind, fromDate, toDate);
                        extension = ".csv";
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown format: {format}");
                }

                description = reportType;
            }

            StatusMessage.Text = $"{description} generated ({data.Length} bytes).";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Green);

            // Store the generated data for export
            _lastGeneratedData = data;
            _lastGeneratedExtension = extension;
            _lastGeneratedDescription = description;

            ExportButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Red);
        }
        finally
        {
            GenerateButton.IsEnabled = true;
        }
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        if (_lastGeneratedData == null)
        {
            StatusMessage.Text = "Please generate a report first.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlWarningTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Orange);
            return;
        }

        try
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"{_lastGeneratedDescription.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}{_lastGeneratedExtension}"
            };
            savePicker.FileTypeChoices.Add(_lastGeneratedDescription, new List<string> { _lastGeneratedExtension });
            InitializeWithWindow.Initialize(savePicker, App.MainWindowPtr);

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteBytesAsync(file, _lastGeneratedData);
                StatusMessage.Text = $"Exported to {file.Path}";
                StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Green);
            }
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Export error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private byte[]? _lastGeneratedData;
    private string _lastGeneratedExtension = string.Empty;
    private string _lastGeneratedDescription = string.Empty;
}
