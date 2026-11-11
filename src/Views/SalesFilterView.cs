using System.Globalization;
using BProMax.Helpers;
using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 SalesFilterView (Page). Sales filtering (replaces VB6 frmSaleFilt).
/// </summary>
public sealed partial class SalesFilterView : Page
{
    private readonly ISalesFilterService _salesFilterService;

    private CalendarDatePicker StartDatePicker => this.FindChildByName<CalendarDatePicker>("StartDatePicker")!;
    private CalendarDatePicker EndDatePicker => this.FindChildByName<CalendarDatePicker>("EndDatePicker")!;
    private TextBox AgentNameBox => this.FindChildByName<TextBox>("AgentNameBox")!;
    private ComboBox StatusCombo => this.FindChildByName<ComboBox>("StatusCombo")!;
    private TextBox MinAmountBox => this.FindChildByName<TextBox>("MinAmountBox")!;
    private TextBox MaxAmountBox => this.FindChildByName<TextBox>("MaxAmountBox")!;
    private ListView SalesList => this.FindChildByName<ListView>("SalesList")!;
    private TextBlock TotalAmountText => this.FindChildByName<TextBlock>("TotalAmountText")!;
    private TextBlock TotalCommissionText => this.FindChildByName<TextBlock>("TotalCommissionText")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public SalesFilterView()
    {
        _salesFilterService = App.Services?.GetRequiredService<ISalesFilterService>()
            ?? throw new InvalidOperationException("SalesFilterService not registered.");
    }

    private async void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        try
        {
            var allSales = await _salesFilterService.GetAllSalesAsync();
            var filtered = allSales as List<SaleRecord> ?? allSales.ToList();

            if (StartDatePicker.Date.HasValue)
                filtered = _salesFilterService.FilterByDateRange(filtered, StartDatePicker.Date.Value.DateTime, DateTime.MaxValue);

            if (EndDatePicker.Date.HasValue)
                filtered = _salesFilterService.FilterByDateRange(filtered, DateTime.MinValue, EndDatePicker.Date.Value.DateTime);

            var agentName = AgentNameBox.Text ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(agentName))
                filtered = _salesFilterService.FilterByAgent(filtered, agentName);

            var status = (StatusCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
            if (status != "All")
                filtered = _salesFilterService.FilterByStatus(filtered, status);

            if (decimal.TryParse(MinAmountBox.Text, out var minAmt) && minAmt > 0)
            {
                var maxAmt = decimal.TryParse(MaxAmountBox.Text, out var m) ? m : decimal.MaxValue;
                filtered = _salesFilterService.FilterByAmountRange(filtered, minAmt, maxAmt);
            }

            SalesList.ItemsSource = filtered;

            var totals = _salesFilterService.CalculateSalesTotals(filtered);
            TotalAmountText.Text = totals.totalAmount.ToString("F2", CultureInfo.InvariantCulture);
            TotalCommissionText.Text = (totals.totalCommission + totals.totalCommission1).ToString("F2", CultureInfo.InvariantCulture);

            StatusMessage.Text = $"Showing {filtered.Count} sales.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        StartDatePicker.Date = null;
        EndDatePicker.Date = null;
        AgentNameBox.Text = string.Empty;
        StatusCombo.SelectedIndex = 0;
        MinAmountBox.Text = "0";
        MaxAmountBox.Text = "0";
        SalesList.ItemsSource = null;
        TotalAmountText.Text = "0";
        TotalCommissionText.Text = "0";
        StatusMessage.Text = string.Empty;
    }
}
