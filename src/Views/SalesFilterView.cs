using System.Collections.ObjectModel;
using System.Globalization;
using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 SalesFilterView (Page). The Sale Panel creates 2D sales; the lower section
/// keeps the sales history filters (replaces VB6 frmSaleFilt).
/// </summary>
public sealed partial class SalesFilterView : Page
{
    private readonly ISalesFilterService _salesFilterService;
    private readonly IAgentService _agentService;
    private readonly IDrawService _drawService;
    private readonly IBetTypeService _betTypeService;
    private readonly ISalePanelLineFactory _lineFactory;
    private readonly ISaleEntryService _saleEntryService;
    private readonly INumberBatchParser _batchParser;
    private readonly IBatchSaleBuilder _batchBuilder;
    private readonly ICommissionService _commissionService;
    private readonly ILogger<SalesFilterView> _logger;
    private readonly ObservableCollection<SalePanelLine> _saleLines = [];

    private List<BetTypeRecord> _betTypes = [];
    private List<NumberGroupRecord> _groups = [];
    private bool _formattingBatch;
    private string _previousBatchText = string.Empty;

    public SalesFilterView()
    {
        InitializeComponent();
        _salesFilterService = App.Services?.GetRequiredService<ISalesFilterService>()
            ?? throw new InvalidOperationException("SalesFilterService not registered.");
        _agentService = App.Services?.GetRequiredService<IAgentService>()
            ?? throw new InvalidOperationException("AgentService not registered.");
        _drawService = App.Services?.GetRequiredService<IDrawService>()
            ?? throw new InvalidOperationException("DrawService not registered.");
        _betTypeService = App.Services?.GetRequiredService<IBetTypeService>()
            ?? throw new InvalidOperationException("BetTypeService not registered.");
        _lineFactory = App.Services?.GetRequiredService<ISalePanelLineFactory>()
            ?? throw new InvalidOperationException("SalePanelLineFactory not registered.");
        _saleEntryService = App.Services?.GetRequiredService<ISaleEntryService>()
            ?? throw new InvalidOperationException("SaleEntryService not registered.");
        _batchParser = App.Services?.GetRequiredService<INumberBatchParser>()
            ?? throw new InvalidOperationException("NumberBatchParser not registered.");
        _batchBuilder = App.Services?.GetRequiredService<IBatchSaleBuilder>()
            ?? throw new InvalidOperationException("BatchSaleBuilder not registered.");
        _commissionService = App.Services?.GetRequiredService<ICommissionService>()
            ?? throw new InvalidOperationException("CommissionService not registered.");
        _logger = App.Services?.GetRequiredService<ILogger<SalesFilterView>>()
            ?? throw new InvalidOperationException("ILogger<SalesFilterView> not registered.");

        CurrentSaleList.ItemsSource = _saleLines;
        UpdateCurrentSaleTotals();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        try
        {
            await LoadReferenceDataAsync();
            await LoadSalesAsync();
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", isError: true);
        }
    }

    private async Task LoadReferenceDataAsync()
    {
        _groups = await _betTypeService.GetNumberGroupsAsync();
        _betTypes = await _betTypeService.GetBetTypesAsync();

        var agents = await _agentService.GetAllAgentsAsync();
        AgentCombo.ItemsSource = agents;
        if (agents.Count > 0)
        {
            AgentCombo.SelectedIndex = 0;
        }

        var draws = await _drawService.GetOpenDrawsAsync();
        DrawCombo.ItemsSource = draws;
        if (draws.Count > 0)
        {
            DrawCombo.SelectedIndex = 0;
        }

        var saleTypes = _betTypes
            .Where(type => !string.Equals(type.Code, "DIRECT", StringComparison.Ordinal))
            .OrderBy(type => type.SortOrder)
            .ToList();
        SaleTypeCombo.ItemsSource = saleTypes;
        if (saleTypes.Count > 0)
        {
            SaleTypeCombo.SelectedIndex = 0;
        }

        QuickAddGroupCombo.ItemsSource = _groups;
        if (_groups.Count > 0)
        {
            QuickAddGroupCombo.SelectedIndex = 0;
        }

        _referenceDataLoadedLog(_logger,
            $"agents={agents.Count}, draws={draws.Count}, saleTypes={saleTypes.Count}, groups={_groups.Count}",
            null);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadReferenceDataAsync();
            ShowStatus("Agents, open draws, and sale types refreshed.", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", isError: true);
        }
    }

    private void BatchNumbersBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_formattingBatch)
        {
            return;
        }

        var box = (TextBox)sender;
        var current = box.Text ?? string.Empty;
        var caretAtEnd = box.SelectionStart >= current.Length;

        if (current.Length < _previousBatchText.Length)
        {
            // Let backspace and deletions work; the next typed digit reformats.
            _previousBatchText = current;
            return;
        }

        if (current.Length == 0)
        {
            _previousBatchText = current;
            return;
        }

        foreach (var character in current)
        {
            if (!char.IsAsciiDigit(character) && !IsBatchSeparator(character))
            {
                RestoreBatchText(box);
                ShowStatus("Numbers may contain only digits and separators.", isError: true);
                return;
            }
        }

        if (!caretAtEnd)
        {
            _previousBatchText = current;
            return;
        }

        var formatted = _batchParser.Format(current, appendTrailingSeparator: EndsWithCompletePair(current));
        if (!string.Equals(formatted, current, StringComparison.Ordinal))
        {
            _formattingBatch = true;
            box.Text = formatted;
            box.SelectionStart = formatted.Length;
            _formattingBatch = false;
        }

        _previousBatchText = box.Text ?? string.Empty;
    }

    private void RestoreBatchText(TextBox box)
    {
        _formattingBatch = true;
        box.Text = _previousBatchText;
        box.SelectionStart = box.Text.Length;
        _formattingBatch = false;
    }

    private static bool EndsWithCompletePair(string text)
    {
        if (text.Length == 0 || !char.IsAsciiDigit(text[^1]))
        {
            return false;
        }

        var digits = 0;
        foreach (var character in text)
        {
            if (char.IsAsciiDigit(character))
            {
                digits++;
            }
        }

        return digits > 0 && digits % 2 == 0;
    }

    private static bool IsBatchSeparator(char character)
    {
        return character is ',' or ';' or '|' or ' ' or '\t' or '\r' or '\n';
    }

    private void AddBatchButton_Click(object sender, RoutedEventArgs e)
    {
        var direct = _betTypes.FirstOrDefault(type => string.Equals(type.Code, "DIRECT", StringComparison.Ordinal));
        if (direct is null)
        {
            ShowStatus("The direct sale type is not configured.", isError: true);
            return;
        }

        if (!TryParseAmount(BatchAmountBox.Text, out var amount))
        {
            ShowStatus("Enter a valid batch amount.", isError: true);
            return;
        }

        var includeReverse = BatchReverseCheck.IsChecked == true;
        var reverseType = _betTypes.FirstOrDefault(type => string.Equals(type.Code, "R", StringComparison.Ordinal));
        if (includeReverse && reverseType is null)
        {
            ShowStatus("The R sale type is not configured.", isError: true);
            return;
        }

        var request = new BatchSaleRequest
        {
            NumbersInput = BatchNumbersBox.Text ?? string.Empty,
            Amount = amount,
            IncludeReverse = includeReverse,
            AmountMode = BatchAmountModeCombo.SelectedIndex == 1
                ? BatchAmountMode.TotalSplit
                : BatchAmountMode.PerBaseNumber,
            PayoutMultiplier = direct.PayoutMultiplier
        };

        if (!_batchBuilder.TryBuild(request, out var preview, out var validationError))
        {
            ShowStatus(validationError ?? "The batch is invalid.", isError: true);
            return;
        }

        var betType = includeReverse ? reverseType! : direct;
        var lines = new List<SalePanelLine>(preview.Lines.Count);
        foreach (var batchLine in preview.Lines)
        {
            var itemRequest = new SaleEntryItemRequest
            {
                BetTypeCode = betType.Code,
                Selection = batchLine.BaseNumber,
                Amount = batchLine.Amount,
                IncludeReverse = includeReverse,
                NumberStakes = batchLine.Numbers
                    .Select(number => new SaleEntryNumberRequest
                    {
                        Number = number.Number,
                        Stake = number.Stake,
                        IsReverse = number.IsReverse
                    })
                    .ToArray()
            };

            if (!_lineFactory.TryCreate(itemRequest, betType, null, out var panelLine, out validationError))
            {
                ShowStatus(validationError ?? "The batch is invalid.", isError: true);
                return;
            }

            lines.Add(panelLine);
        }

        foreach (var line in lines)
        {
            _saleLines.Add(line);
        }

        UpdateCurrentSaleTotals();
        BatchNumbersBox.Text = string.Empty;
        _previousBatchText = string.Empty;

        var warning = preview.Warnings.Count > 0
            ? $" {preview.Warnings.Count} duplicate(s) merged."
            : string.Empty;
        ShowStatus(
            $"Added {lines.Count} line(s), total {FormatMoney(preview.TotalStake)}.{warning}",
            isError: false);

        _batchAddedLog(_logger, lines.Count, preview.TotalStake.ToString("N2", CultureInfo.InvariantCulture), null);
    }

    private void SaleTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var betType = SaleTypeCombo.SelectedItem as BetTypeRecord;
        if (betType is null)
        {
            return;
        }

        var needsSelection = NeedsSelection(betType.RuleType);
        SelectionRow.Visibility = needsSelection ? Visibility.Visible : Visibility.Collapsed;
        SelectionBox.PlaceholderText = betType.RuleType switch
        {
            NumberRuleTypes.DigitInEitherPosition or NumberRuleTypes.DigitInPosition or NumberRuleTypes.DigitSum
                => "0-9",
            _ => "00-99"
        };

        var group = FindGroup(betType.Code);
        ManualReverseCheck.IsEnabled = group?.AllowReverse == true;
        ManualReverseCheck.IsChecked = false;
    }

    private void QuickAddGroupCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var group = QuickAddGroupCombo.SelectedItem as NumberGroupRecord;

        QuickAddReverseCheck.IsEnabled = group?.AllowReverse == true;
        QuickAddReverseCheck.IsChecked = false;
    }

    private bool TryAddSaleLine(SaleEntryItemRequest request, BetTypeRecord betType, NumberGroupRecord? group, string invalidMessage, out SalePanelLine line)
    {
        if (!_lineFactory.TryCreate(request, betType, group, out line, out var validationError))
        {
            ShowStatus(validationError ?? invalidMessage, isError: true);
            return false;
        }

        _saleLines.Add(line);
        UpdateCurrentSaleTotals();
        _lineAddedLog(_logger, line.Label, null);
        return true;
    }

    private void AddSaleItemButton_Click(object sender, RoutedEventArgs e)
    {
        var betType = SaleTypeCombo.SelectedItem as BetTypeRecord;
        if (betType is null)
        {
            ShowStatus("Select a sale type.", isError: true);
            return;
        }

        if (!TryParseAmount(ManualAmountBox.Text, out var amount))
        {
            ShowStatus("Enter a valid sale amount.", isError: true);
            return;
        }

        var group = FindGroup(betType.Code);
        var includeReverse = group is not null && ManualReverseCheck.IsChecked == true;
        var selection = NeedsSelection(betType.RuleType)
            ? SelectionBox.Text?.Trim()
            : null;

        var request = new SaleEntryItemRequest
        {
            BetTypeCode = betType.Code,
            Selection = selection,
            Amount = amount,
            IncludeReverse = includeReverse,
            NumberGroupCode = group?.Code
        };

        if (!TryAddSaleLine(request, betType, group, "The sale line is invalid.", out var line))
        {
            return;
        }

        SelectionBox.Text = string.Empty;
        ShowStatus($"Added {line.Label} ({FormatMoney(line.Amount)}).", isError: false);
    }

    private void QuickAddButton_Click(object sender, RoutedEventArgs e)
    {
        var group = QuickAddGroupCombo.SelectedItem as NumberGroupRecord;
        if (group is null)
        {
            ShowStatus("Select a Quick Add group.", isError: true);
            return;
        }

        var betType = _betTypes.FirstOrDefault(type =>
            string.Equals(type.Code, group.Code, StringComparison.Ordinal));
        if (betType is null)
        {
            ShowStatus($"The {group.NameMy} sale type is not configured.", isError: true);
            return;
        }

        if (!TryParseAmount(QuickAddAmountBox.Text, out var amount))
        {
            ShowStatus("Enter a valid Quick Add amount.", isError: true);
            return;
        }

        var includeReverse = QuickAddReverseCheck.IsChecked == true;
        if (includeReverse && !group.AllowReverse)
        {
            ShowStatus($"R is disabled for {group.NameMy}.", isError: true);
            return;
        }

        var request = new SaleEntryItemRequest
        {
            BetTypeCode = betType.Code,
            NumberGroupCode = group.Code,
            Amount = amount,
            IncludeReverse = includeReverse
        };

        if (!TryAddSaleLine(request, betType, group, "The Quick Add line is invalid.", out var line))
        {
            return;
        }

        ShowStatus($"Added {line.Label} ({FormatMoney(line.Amount)}).", isError: false);
    }

    private void RemoveLine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is SalePanelLine line)
        {
            _saleLines.Remove(line);
            UpdateCurrentSaleTotals();
        }
    }

    private async void SaveSaleButton_Click(object sender, RoutedEventArgs e)
    {
        var agent = AgentCombo.SelectedItem as Agent;
        if (agent is null)
        {
            ShowStatus("Select an agent.", isError: true);
            return;
        }

        if (DrawCombo.SelectedItem is not DrawRecord draw)
        {
            ShowStatus("Select an open draw.", isError: true);
            return;
        }

        if (_saleLines.Count == 0)
        {
            ShowStatus("Add at least one sale line before saving.", isError: true);
            return;
        }

        SaveSaleButton.IsEnabled = false;
        try
        {
            var request = new SaleEntryRequest
            {
                AgentId = agent.Id,
                DrawId = draw.Id,
                CurrencyType = agent.CurrencyType,
                CurrencyRate = agent.CurrencyRate,
                Items = _saleLines.Select(line => line.Request).ToArray()
            };

            var result = await _saleEntryService.CreateSaleAsync(request);
            var autoBuy = result.AutoBuyTriggered
                ? $" Auto-buy triggered ({FormatMoney(result.AutoBuyAmount ?? 0m)})."
                : string.Empty;
            ShowStatus(
                $"Sale {result.SaleId} saved: stake {FormatMoney(result.TotalStake)}, "
                + $"commission {FormatMoney(result.Commission + result.Commission1)}.{autoBuy}",
                isError: false);

            _saleSavedLog(_logger, result.SaleId.ToString(CultureInfo.InvariantCulture), result.TotalStake, result.Commission + result.Commission1, null);

            _saleLines.Clear();
            UpdateCurrentSaleTotals();
            await LoadSalesAsync();
        }
        catch (Exception ex)
        {
            _saleSaveFailedLog(_logger, agent.Id.ToString(CultureInfo.InvariantCulture), ex);
            ShowStatus($"Save failed: {ex.Message}", isError: true);
        }
        finally
        {
            SaveSaleButton.IsEnabled = true;
        }
    }

    private async void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadSalesAsync();
            ShowStatus($"Showing {SalesList.Items.Count} sales.", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", isError: true);
        }
    }

    private async Task LoadSalesAsync()
    {
        var allSales = await _salesFilterService.GetAllSalesAsync();
        var filtered = allSales as List<SaleRecord> ?? allSales.ToList();

        if (StartDatePicker.Date.HasValue)
        {
            filtered = _salesFilterService.FilterByDateRange(
                filtered,
                StartDatePicker.Date.Value.DateTime,
                DateTime.MaxValue);
        }

        if (EndDatePicker.Date.HasValue)
        {
            filtered = _salesFilterService.FilterByDateRange(
                filtered,
                DateTime.MinValue,
                EndDatePicker.Date.Value.DateTime);
        }

        var agentName = AgentNameBox.Text ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(agentName))
        {
            filtered = _salesFilterService.FilterByAgent(filtered, agentName);
        }

        var status = (StatusCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        if (!string.Equals(status, "All", StringComparison.Ordinal))
        {
            filtered = _salesFilterService.FilterByStatus(filtered, status);
        }

        if (TryParseAmount(MinAmountBox.Text, out var minAmount) && minAmount > 0)
        {
            var maxAmount = TryParseAmount(MaxAmountBox.Text, out var parsedMax)
                ? parsedMax
                : decimal.MaxValue;
            filtered = _salesFilterService.FilterByAmountRange(filtered, minAmount, maxAmount);
        }

        SalesList.ItemsSource = filtered;

        var totals = _salesFilterService.CalculateSalesTotals(filtered);
        TotalAmountText.Text = totals.totalAmount.ToString("N2", CultureInfo.InvariantCulture);
        TotalCommissionText.Text =
            (totals.totalCommission + totals.totalCommission1).ToString("N2", CultureInfo.InvariantCulture);

        _salesLoadedLog(_logger, filtered.Count, null);
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

    private void ClearSaleButton_Click(object sender, RoutedEventArgs e)
    {
        _saleLines.Clear();
        UpdateCurrentSaleTotals();
        ShowStatus("Current sale cleared.", isError: false);
    }

    private void AgentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCurrentSaleTotals();
    }

    private void UpdateCurrentSaleTotals()
    {
        var totalStake = _saleLines.Sum(line => line.Amount);
        var maxPayout = _saleLines.Sum(line => line.MaxPayout);
        var commission = 0m;

        if (AgentCombo?.SelectedItem is Agent agent)
        {
            commission = _commissionService.CalculateCommission(totalStake, agent.Rate)
                + _commissionService.CalculateCommission1(totalStake, agent.Rate1);
        }

        CurrentSaleTotalText.Text = FormatMoney(totalStake);
        CurrentSaleCommissionText.Text = FormatMoney(commission);
        CurrentSaleMaxPayoutText.Text = FormatMoney(maxPayout);
    }

    private NumberGroupRecord? FindGroup(string code)
    {
        return _groups.FirstOrDefault(group =>
            string.Equals(group.Code, code, StringComparison.Ordinal));
    }

    private static bool NeedsSelection(string ruleType)
    {
        return ruleType is
            NumberRuleTypes.Exact
            or NumberRuleTypes.ExactAndReverse
            or NumberRuleTypes.DigitInEitherPosition
            or NumberRuleTypes.DigitInPosition
            or NumberRuleTypes.DigitSum;
    }

    private static bool TryParseAmount(string? text, out decimal amount)
    {
        return decimal.TryParse(
            text,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out amount);
    }

    private static string FormatMoney(decimal amount)
    {
        return amount.ToString("N2", CultureInfo.InvariantCulture);
    }

    private static readonly Action<ILogger, string, decimal, decimal, Exception?> _saleSavedLog =
        LoggerMessage.Define<string, decimal, decimal>(LogLevel.Information, new EventId(1, "SaleSaved"), "Sale {SaleId} saved: stake {Stake}, commission {Commission}.");

    private static readonly Action<ILogger, string, Exception?> _saleSaveFailedLog =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2, "SaleSaveFailed"), "Sale save failed for agent {AgentId}.");

    private static readonly Action<ILogger, int, string, Exception?> _batchAddedLog =
        LoggerMessage.Define<int, string>(LogLevel.Information, new EventId(3, "BatchAdded"), "Batch added: {LineCount} line(s), total {TotalStake}.");

    private static readonly Action<ILogger, string, Exception?> _lineAddedLog =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(4, "SaleLineAdded"), "Sale line added: {LineLabel}.");

    private static readonly Action<ILogger, int, Exception?> _salesLoadedLog =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(5, "SalesLoaded"), "Loaded {SalesCount} sales.");

    private static readonly Action<ILogger, string, Exception?> _referenceDataLoadedLog =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(6, "ReferenceDataLoaded"), "Reference data loaded: {Details}.");

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage.Text = message;
        StatusMessage.Foreground = isError
            ? ThemeBrush.Error
            : ThemeBrush.Success;
    }
}
