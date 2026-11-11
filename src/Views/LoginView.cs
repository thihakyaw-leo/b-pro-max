using BProMax.Helpers;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 LoginView (Page). Handles user authentication (replaces VB6 frmLogin).
/// </summary>
public sealed partial class LoginView : Page
{
    private readonly IAuthService _auth;

    private TextBox UsernameBox => this.FindChildByName<TextBox>("UsernameBox")!;
    private PasswordBox PasswordBox => this.FindChildByName<PasswordBox>("PasswordBox")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public LoginView()
    {
        _auth = App.Services?.GetRequiredService<IAuthService>()
            ?? throw new InvalidOperationException("AuthService not registered.");
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        bool ok = await _auth.LoginAsync(UsernameBox.Text ?? string.Empty, PasswordBox.Password);
        if (ok)
        {
            StatusMessage.Text = $"Welcome, {_auth.CurrentUser}!";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Green);
            if (App.MainWindowInstance is MainWindow mw) mw.UpdateMenuState();
            _ = this.Frame?.Navigate(typeof(AgentListView));
        }
        else
        {
            StatusMessage.Text = "Invalid username or password.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }
}
