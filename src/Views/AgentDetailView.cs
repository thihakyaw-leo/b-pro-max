using System.Globalization;
using BProMax.Helpers;
using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 AgentDetailView (Page). Agent create/edit form (replaces VB6 frmCustomer).
/// </summary>
public sealed partial class AgentDetailView : Page
{
    private readonly IAgentService _agentService;
    private int? _agentId;

    private TextBlock IdLabel => this.FindChildByName<TextBlock>("IdLabel")!;
    private TextBox AgentNameBox => this.FindChildByName<TextBox>("AgentNameBox")!;
    private TextBox AddressBox => this.FindChildByName<TextBox>("AddressBox")!;
    private TextBox PhoneBox => this.FindChildByName<TextBox>("PhoneBox")!;
    private TextBox CommissionBox => this.FindChildByName<TextBox>("CommissionBox")!;
    private TextBox RateBox => this.FindChildByName<TextBox>("RateBox")!;
    private TextBox Commission1Box => this.FindChildByName<TextBox>("Commission1Box")!;
    private TextBox Rate1Box => this.FindChildByName<TextBox>("Rate1Box")!;
    private TextBox CurrencyTypeBox => this.FindChildByName<TextBox>("CurrencyTypeBox")!;
    private TextBox CurrencyRateBox => this.FindChildByName<TextBox>("CurrencyRateBox")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public AgentDetailView()
    {
        _agentService = App.Services?.GetRequiredService<IAgentService>()
            ?? throw new InvalidOperationException("AgentService not registered.");
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is int agentId)
        {
            _agentId = agentId;
            IdLabel.Text = $"Edit Agent (ID: {agentId})";
            IdLabel.Visibility = Visibility.Visible;
            await LoadAgentAsync(agentId);
        }
        else
        {
            _agentId = null;
            IdLabel.Text = "New Agent";
            IdLabel.Visibility = Visibility.Visible;
        }
    }

    private async Task LoadAgentAsync(int id)
    {
        try
        {
            var agent = await _agentService.GetAgentByIdAsync(id);
            if (agent == null)
            {
                StatusMessage.Text = "Agent not found.";
                StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                return;
            }

            AgentNameBox.Text = agent.AgentName;
            AddressBox.Text = agent.Address ?? string.Empty;
            PhoneBox.Text = agent.Phone ?? string.Empty;
            CommissionBox.Text = agent.Commission.ToString(CultureInfo.InvariantCulture);
            RateBox.Text = agent.Rate.ToString(CultureInfo.InvariantCulture);
            Commission1Box.Text = agent.Commission1.ToString(CultureInfo.InvariantCulture);
            Rate1Box.Text = agent.Rate1.ToString(CultureInfo.InvariantCulture);
            CurrencyTypeBox.Text = agent.CurrencyType ?? string.Empty;
            CurrencyRateBox.Text = agent.CurrencyRate.ToString(CultureInfo.InvariantCulture);

            StatusMessage.Text = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error loading agent: {ex.Message}";
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        if (!decimal.TryParse(CommissionBox.Text, out var commission) ||
            !decimal.TryParse(RateBox.Text, out var rate) ||
            !decimal.TryParse(Commission1Box.Text, out var commission1) ||
            !decimal.TryParse(Rate1Box.Text, out var rate1) ||
            !decimal.TryParse(CurrencyRateBox.Text, out var currencyRate))
        {
            StatusMessage.Text = "Invalid numeric values.";
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return;
        }

        var agent = new Agent
        {
            AgentName = AgentNameBox.Text ?? string.Empty,
            Address = AddressBox.Text,
            Phone = PhoneBox.Text,
            Commission = commission,
            Rate = rate,
            Commission1 = commission1,
            Rate1 = rate1,
            CurrencyType = CurrencyTypeBox.Text,
            CurrencyRate = currencyRate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            if (_agentId.HasValue)
            {
                agent.Id = _agentId.Value;
                await _agentService.UpdateAgentAsync(agent);
                StatusMessage.Text = "Agent updated.";
            }
            else
            {
                await _agentService.CreateAgentAsync(agent);
                StatusMessage.Text = "Agent created.";
            }

            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
            _ = this.Frame?.Navigate(typeof(AgentListView));
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error saving agent: {ex.Message}";
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_agentId.HasValue) return;

        var dialog = new ContentDialog
        {
            Title = "Delete Agent",
            Content = "Are you sure you want to delete this agent?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            try
            {
                await _agentService.DeleteAgentAsync(_agentId.Value);
                StatusMessage.Text = "Agent deleted.";
                StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                _ = this.Frame?.Navigate(typeof(AgentListView));
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Error deleting agent: {ex.Message}";
                StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _ = this.Frame?.Navigate(typeof(AgentListView));
    }
}
