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

    public LoginView()
    {
        InitializeComponent();
        _auth = App.Services?.GetRequiredService<IAuthService>()
            ?? throw new InvalidOperationException("AuthService not registered.");

        // Support Enter key in username/password fields to trigger login.
        UsernameBox.KeyDown += OnUsernameBoxKeyDown;
        PasswordBox.KeyDown += OnPasswordBoxKeyDown;
        CreateAdminButton.Click += CreateAdminButton_Click;
    }

    private void OnUsernameBoxKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            Login_Click(this, new Microsoft.UI.Xaml.RoutedEventArgs());
        }
    }

    private void OnPasswordBoxKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            Login_Click(this, new Microsoft.UI.Xaml.RoutedEventArgs());
        }
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
                // No accounts exist yet: show "Create admin account" button.
                CreateAdminButton.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                LoginButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
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
                StatusMessage.Foreground = ThemeBrush.Success;

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
                StatusMessage.Foreground = ThemeBrush.Error;
            }
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void CreateAdminButton_Click(object sender, RoutedEventArgs e)
    {
        _ = this.Frame?.Navigate(typeof(SetupAdminView));
    }
}
