using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 AgentListView (Page). Lists all agents (replaces VB6 frmAgentList).
/// </summary>
public sealed partial class AgentListView : Page
{
    private readonly IAgentService _agentService;

    public AgentListView()
    {
        InitializeComponent();
        _agentService = App.Services?.GetRequiredService<IAgentService>()
            ?? throw new InvalidOperationException("AgentService not registered.");
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadAgentsAsync();
    }

    private async Task LoadAgentsAsync()
    {
        try
        {
            var agents = await _agentService.GetAllAgentsAsync();
            AgentList.ItemsSource = agents;
            StatusMessage.Text = $"Loaded {agents.Count} agents.";
            StatusMessage.Foreground = ThemeBrush.Success;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = ThemeBrush.Error;
        }
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var term = SearchBox.Text ?? string.Empty;
        try
        {
            var agents = string.IsNullOrWhiteSpace(term)
                ? await _agentService.GetAllAgentsAsync()
                : await _agentService.SearchAgentsAsync(term);
            AgentList.ItemsSource = agents;
            StatusMessage.Text = $"Found {agents.Count} agents.";
            StatusMessage.Foreground = ThemeBrush.Success;
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        await LoadAgentsAsync();
    }

    private void NewAgentButton_Click(object sender, RoutedEventArgs e)
    {
        _ = this.Frame?.Navigate(typeof(AgentDetailView));
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int agentId)
        {
            _ = this.Frame?.Navigate(typeof(AgentDetailView), agentId);
        }
    }
}
