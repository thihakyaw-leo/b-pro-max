using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 AutoBuyView (Page). Auto-purchase rules (replaces VB6 frmAutoBuy).
/// </summary>
public sealed partial class AutoBuyView : Page
{
    private readonly IAutoBuyService _autoBuyService;

    public AutoBuyView()
    {
        InitializeComponent();
        _autoBuyService = App.Services?.GetRequiredService<IAutoBuyService>()
            ?? throw new InvalidOperationException("AutoBuyService not registered.");
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadRulesAsync();
    }

    private async Task LoadRulesAsync()
    {
        var rules = await _autoBuyService.GetAllRulesAsync();
        RulesList.ItemsSource = rules;
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        if (!decimal.TryParse(TriggerAmountBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var triggerAmt) ||
            !decimal.TryParse(AutoBuyAmountBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var autoBuyAmt))
        {
            StatusMessage.Text = "Invalid numeric values.";
            StatusMessage.Foreground = ThemeBrush.Error;
            return;
        }

        var rule = new AutoBuyRule
        {
            AgentName = AgentNameBox.Text ?? string.Empty,
            TriggerAmount = triggerAmt,
            AutoBuyAmount = autoBuyAmt,
            IsEnabled = EnabledCheck.IsChecked == true
        };

        try
        {
            await _autoBuyService.CreateRuleAsync(rule);
            await LoadRulesAsync();
            StatusMessage.Text = "Rule created.";
            StatusMessage.Foreground = ThemeBrush.Success;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage.Text = ex.Message;
            StatusMessage.Foreground = ThemeBrush.Error;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = ThemeBrush.Error;
        }
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        if (!decimal.TryParse(TriggerAmountBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var triggerAmt) ||
            !decimal.TryParse(AutoBuyAmountBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var autoBuyAmt))
        {
            StatusMessage.Text = "Invalid numeric values.";
            StatusMessage.Foreground = ThemeBrush.Error;
            return;
        }

        var agentName = AgentNameBox.Text ?? string.Empty;
        var existing = await _autoBuyService.GetRuleByAgentAsync(agentName);
        if (existing == null)
        {
            StatusMessage.Text = "No rule found for this agent.";
            StatusMessage.Foreground = ThemeBrush.Error;
            return;
        }

        existing.AgentName = agentName;
        existing.TriggerAmount = triggerAmt;
        existing.AutoBuyAmount = autoBuyAmt;
        existing.IsEnabled = EnabledCheck.IsChecked == true;

        try
        {
            await _autoBuyService.UpdateRuleAsync(existing);
            await LoadRulesAsync();
            StatusMessage.Text = "Rule updated.";
            StatusMessage.Foreground = ThemeBrush.Success;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage.Text = ex.Message;
            StatusMessage.Foreground = ThemeBrush.Error;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = ThemeBrush.Error;
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        var agentName = AgentNameBox.Text ?? string.Empty;
        var rule = await _autoBuyService.GetRuleByAgentAsync(agentName);
        if (rule == null)
        {
            StatusMessage.Text = "No rule found for this agent.";
            StatusMessage.Foreground = ThemeBrush.Error;
            return;
        }

        try
        {
            await _autoBuyService.DeleteRuleAsync(rule.Id);
            await LoadRulesAsync();
            StatusMessage.Text = "Rule deleted.";
            StatusMessage.Foreground = ThemeBrush.Success;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = ThemeBrush.Error;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadRulesAsync();
        StatusMessage.Text = string.Empty;
    }

    private async void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int ruleId)
        {
            await _autoBuyService.ToggleRuleAsync(ruleId);
            await LoadRulesAsync();
        }
    }
}
