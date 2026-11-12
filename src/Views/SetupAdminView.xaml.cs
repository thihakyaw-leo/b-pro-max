using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 SetupAdminView (Page). First-run administrator account setup.
/// Created entirely in code (no XAML) because XAML compilation is skipped on this Linux SDK.
/// </summary>
public sealed partial class SetupAdminView : Page
{
    private readonly IAuthService _auth;

    private static Brush ErrorBrush => (Brush)Application.Current.Resources["SystemControlErrorTextForegroundBrush"];
    private static Brush SuccessBrush => (Brush)Application.Current.Resources["SystemControlSuccessTextForegroundBrush"];

    private TextBox UsernameBox { get; set; } = null!;
    private PasswordBox PasswordBox { get; set; } = null!;
    private PasswordBox ConfirmPasswordBox { get; set; } = null!;
    private Button CreateButton { get; set; } = null!;
    private TextBlock StatusMessage { get; set; } = null!;

    public SetupAdminView()
    {
        _auth = App.Services?.GetRequiredService<IAuthService>()
            ?? throw new InvalidOperationException("AuthService not registered.");

        BuildUI();
    }

    private void BuildUI()
    {
        var root = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 400,
            Padding = new Thickness(24)
        };

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = "b-pro-max",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };
        title.SetValue(Grid.RowProperty, 0);
        title.Style = (Style)Application.Current.Resources["TitleTextBlockStyle"];
        root.Children.Add(title);

        var subtitle = new TextBlock
        {
            Text = "Create Administrator Account",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 24),
        };
        subtitle.Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"];
        subtitle.SetValue(Grid.RowProperty, 1);
        root.Children.Add(subtitle);

        var usernameLabel = new TextBlock { Text = "Username", Margin = new Thickness(0, 0, 0, 6) };
        usernameLabel.SetValue(Grid.RowProperty, 2);
        root.Children.Add(usernameLabel);

        UsernameBox = new TextBox
        {
            PlaceholderText = "Enter administrator username",
            Margin = new Thickness(0, 22, 0, 0),
        };
        UsernameBox.SetValue(Grid.RowProperty, 2);
        AutomationProperties.SetAutomationId(UsernameBox, "UsernameBox");
        AutomationProperties.SetName(UsernameBox, "Username");
        AutomationProperties.SetLabeledBy(UsernameBox, usernameLabel);
        root.Children.Add(UsernameBox);

        var passwordLabel = new TextBlock { Text = "Password", Margin = new Thickness(0, 0, 0, 6) };
        passwordLabel.SetValue(Grid.RowProperty, 3);
        root.Children.Add(passwordLabel);

        PasswordBox = new PasswordBox
        {
            PlaceholderText = "Enter administrator password",
            Margin = new Thickness(0, 22, 0, 0),
        };
        PasswordBox.SetValue(Grid.RowProperty, 3);
        AutomationProperties.SetAutomationId(PasswordBox, "PasswordBox");
        AutomationProperties.SetName(PasswordBox, "Password");
        AutomationProperties.SetLabeledBy(PasswordBox, passwordLabel);
        root.Children.Add(PasswordBox);

        var confirmLabel = new TextBlock { Text = "Confirm Password", Margin = new Thickness(0, 0, 0, 6) };
        confirmLabel.SetValue(Grid.RowProperty, 4);
        root.Children.Add(confirmLabel);

        ConfirmPasswordBox = new PasswordBox
        {
            PlaceholderText = "Confirm administrator password",
            Margin = new Thickness(0, 22, 0, 24),
        };
        ConfirmPasswordBox.SetValue(Grid.RowProperty, 4);
        AutomationProperties.SetAutomationId(ConfirmPasswordBox, "ConfirmPasswordBox");
        AutomationProperties.SetName(ConfirmPasswordBox, "Confirm password");
        AutomationProperties.SetLabeledBy(ConfirmPasswordBox, confirmLabel);
        root.Children.Add(ConfirmPasswordBox);

        CreateButton = new Button
        {
            Content = "Create Account",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 40,
            Margin = new Thickness(0, 10, 0, 0),
        };
        CreateButton.Click += CreateButton_Click;
        CreateButton.SetValue(Grid.RowProperty, 5);
        AutomationProperties.SetAutomationId(CreateButton, "CreateButton");
        AutomationProperties.SetName(CreateButton, "Create Account");
        root.Children.Add(CreateButton);

        StatusMessage = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0),
        };
        StatusMessage.SetValue(Grid.RowProperty, 6);
        AutomationProperties.SetAutomationId(StatusMessage, "StatusMessage");
        AutomationProperties.SetName(StatusMessage, "Status message");
        root.Children.Add(StatusMessage);

        this.Content = root;
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        CreateButton.IsEnabled = false;

        try
        {
            var username = UsernameBox.Text ?? string.Empty;
            var password = PasswordBox.Password;
            var confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                StatusMessage.Text = "Username is required.";
                StatusMessage.Foreground = ErrorBrush;
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                StatusMessage.Text = "Password is required.";
                StatusMessage.Foreground = ErrorBrush;
                return;
            }
            if (password != confirm)
            {
                StatusMessage.Text = "Passwords do not match.";
                StatusMessage.Foreground = ErrorBrush;
                return;
            }

            try
            {
                await _auth.RegisterCredentialAsync(username, password);
                StatusMessage.Text = "Account created successfully. Please log in.";
                StatusMessage.Foreground = SuccessBrush;

                _ = this.Frame?.Navigate(typeof(LoginView));
            }
            catch (InvalidOperationException ex)
            {
                StatusMessage.Text = ex.Message;
                StatusMessage.Foreground = ErrorBrush;
            }
        }
        finally
        {
            CreateButton.IsEnabled = true;
        }
    }
}
