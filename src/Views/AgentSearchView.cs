using BProMax.Helpers;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 AgentSearchView (Page). Agent search (replaces VB6 frmSearch).
/// </summary>
public sealed partial class AgentSearchView : Page
{
    private readonly IAgentService _agentService;

    private TextBox SearchBox => this.FindChildByName<TextBox>("SearchBox")!;
    private ListView SearchResults => this.FindChildByName<ListView>("SearchResults")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public AgentSearchView()
    {
        _agentService = App.Services?.GetRequiredService<IAgentService>()
            ?? throw new InvalidOperationException("AgentService not registered.");
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var term = SearchBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(term))
        {
            StatusMessage.Text = "Please enter a search term.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return;
        }

        try
        {
            var agents = await _agentService.SearchAgentsAsync(term);
            SearchResults.ItemsSource = agents;
            StatusMessage.Text = $"Found {agents.Count} agents.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Microsoft.UI.Xaml.Media.Brush) ?? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error: {ex.Message}";
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }
}
