using BProMax.Helpers;
using BProMax.Models;
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

    private TextBox AgentNameBox => this.FindChildByName<TextBox>("AgentNameBox")!;
    private TextBox TriggerAmountBox => this.FindChildByName<TextBox>("TriggerAmountBox")!;
    private TextBox AutoBuyAmountBox => this.FindChildByName<TextBox>("AutoBuyAmountBox")!;
    private CheckBox EnabledCheck => this.FindChildByName<CheckBox>("EnabledCheck")!;
    private ListView RulesList => this.FindChildByName<ListView>("RulesList")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public AutoBuyView()
    {
        _autoBuyService = App.Services?.GetRequiredService<IAutoBuyService>()
            ?? throw new InvalidOperationException("AutoBuyService not registered.");
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        LoadRules();
    }

    private void LoadRules()
    {
        var rules = _autoBuyService.GetAllRules();
        RulesList.ItemsSource = rules;
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        if (!decimal.TryParse(TriggerAmountBox.Text, out var triggerAmt) ||
            !decimal.TryParse(AutoBuyAmountBox.Text, out var autoBuyAmt))
        {
            StatusMessage.Text = "Invalid numeric values.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
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
            _autoBuyService.CreateRule(rule);
            LoadRules();
            StatusMessage.Text = "Rule created.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        if (!decimal.TryParse(TriggerAmountBox.Text, out var triggerAmt) ||
            !decimal.TryParse(AutoBuyAmountBox.Text, out var autoBuyAmt))
        {
            StatusMessage.Text = "Invalid numeric values.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return;
        }

        var agentName = AgentNameBox.Text ?? string.Empty;
        var existing = _autoBuyService.GetRuleByAgent(agentName);
        if (existing == null)
        {
            StatusMessage.Text = "No rule found for this agent.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return;
        }

        existing.AgentName = agentName;
        existing.TriggerAmount = triggerAmt;
        existing.AutoBuyAmount = autoBuyAmt;
        existing.IsEnabled = EnabledCheck.IsChecked == true;

        try
        {
            _autoBuyService.UpdateRule(existing);
            LoadRules();
            StatusMessage.Text = "Rule updated.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        var agentName = AgentNameBox.Text ?? string.Empty;
        var rule = _autoBuyService.GetRuleByAgent(agentName);
        if (rule == null)
        {
            StatusMessage.Text = "No rule found for this agent.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return;
        }

        try
        {
            _autoBuyService.DeleteRule(rule.Id);
            LoadRules();
            StatusMessage.Text = "Rule deleted.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadRules();
        StatusMessage.Text = string.Empty;
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int ruleId)
        {
            _autoBuyService.ToggleRule(ruleId);
            LoadRules();
        }
    }
}
