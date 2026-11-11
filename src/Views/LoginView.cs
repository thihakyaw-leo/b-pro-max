using BProMax.Helpers;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 LoginView (Page). Handles user authentication (replaces VB6 frmLogin).
/// </summary>
public sealed partial class LoginView : Page
{
    private readonly IAuthService _auth;

    private TextBox UsernameBox => this.FindChildByName<TextBox>("UsernameBox")!;
    private PasswordBox PasswordBox => this.FindChildByName<PasswordBox>("PasswordBox")!;
    private Button LoginButton => this.FindChildByName<Button>("LoginButton")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public LoginView()
    {
        _auth = App.Services?.GetRequiredService<IAuthService>()
            ?? throw new InvalidOperationException("AuthService not registered.");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = CheckFirstRunAsync();
    }

    private async System.Threading.Tasks.Task CheckFirstRunAsync()
    {
        try
        {
            bool anyUser = await _auth.AnyUserExistsAsync();
            if (!anyUser)
            {
                // No accounts exist yet: redirect to first-run admin setup.
                _ = this.Frame?.Navigate(typeof(SetupAdminView));
            }
        }
        catch
        {
            // If the database isn't ready yet, stay on login; the error will surface on login attempt.
        }
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        LoginButton.IsEnabled = false;

        try
        {
            var result = await _auth.LoginAsync(UsernameBox.Text ?? string.Empty, PasswordBox.Password);

            if (result.Success)
            {
                StatusMessage.Text = $"Welcome, {_auth.CurrentUser}!";
                StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Green);

                if (App.MainWindowInstance is MainWindow mw)
                {
                    mw.UpdateMenuState();
                    mw.NavigateToAuthenticatedHome();
                }
                else
                {
                    _ = this.Frame?.Navigate(typeof(AgentListView));
                }
            }
            else
            {
                StatusMessage.Text = result.FailureReason switch
                {
                    AuthFailureReason.LockedOut => $"Account locked. Try again after {result.LockedUntil?.ToLocalTime():HH:mm}.",
                    AuthFailureReason.EmptyInput => "Please enter username and password.",
                    _ => "Invalid username or password.",
                };
                StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}
