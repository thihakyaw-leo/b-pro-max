using BProMax.Helpers;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 RegisterView (Page). Handles software registration/licensing (replaces VB6 frmRegister).
/// </summary>
public sealed partial class RegisterView : Page
{
    private readonly ILicensingService _licensing;

    private TextBox LicenseKeyBox => this.FindChildByName<TextBox>("LicenseKeyBox")!;
    private TextBox CompanyBox => this.FindChildByName<TextBox>("CompanyBox")!;
    private TextBox EmailBox => this.FindChildByName<TextBox>("EmailBox")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public RegisterView()
    {
        _licensing = App.Services?.GetRequiredService<ILicensingService>()
            ?? throw new InvalidOperationException("LicensingService not registered.");
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        string key = LicenseKeyBox.Text ?? string.Empty;
        bool valid = await _licensing.ValidateLicenseAsync(key);
        if (valid)
        {
            await _licensing.RegisterAsync(key);
            StatusMessage.Text = "Registration successful.";
            StatusMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
            if (Window.Current is MainWindow mw) mw.UpdateMenuState();
            _ = this.Frame?.Navigate(typeof(LoginView));
        }
        else
        {
            StatusMessage.Text = "Invalid license key.";
            StatusMessage.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        _ = this.Frame?.Navigate(typeof(LoginView));
    }
}
