using System.Globalization;
using BProMax.Helpers;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 RegionalSettingsView (Page). Regional and locale settings (date, number, currency, font).
/// </summary>
public sealed partial class RegionalSettingsView : Page
{
    private readonly IRegionalSettingsService _regionalSettings;
    private readonly ISettingsStore _settingsStore;

    private ComboBox CultureCombo => this.FindChildByName<ComboBox>("CultureCombo")!;
    private TextBox DateFormatBox => this.FindChildByName<TextBox>("DateFormatBox")!;
    private TextBox DateTimeFormatBox => this.FindChildByName<TextBox>("DateTimeFormatBox")!;
    private TextBox CurrencySymbolBox => this.FindChildByName<TextBox>("CurrencySymbolBox")!;
    private TextBox CurrencyCodeBox => this.FindChildByName<TextBox>("CurrencyCodeBox")!;
    private TextBox DecimalDigitsBox => this.FindChildByName<TextBox>("DecimalDigitsBox")!;
    private ComboBox FirstDayOfWeekCombo => this.FindChildByName<ComboBox>("FirstDayOfWeekCombo")!;
    private CheckBox Use24HourCheck => this.FindChildByName<CheckBox>("Use24HourCheck")!;
    private TextBox FontFamilyBox => this.FindChildByName<TextBox>("FontFamilyBox")!;
    private Button SaveButton => this.FindChildByName<Button>("SaveButton")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public RegionalSettingsView()
    {
        _regionalSettings = App.Services?.GetRequiredService<IRegionalSettingsService>()
            ?? throw new InvalidOperationException("RegionalSettingsService not registered.");
        _settingsStore = App.Services?.GetRequiredService<ISettingsStore>()
            ?? throw new InvalidOperationException("SettingsStore not registered.");
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Load persisted settings from the database, falling back to config defaults.
        await _regionalSettings.LoadFromStoreAsync();

        PopulateUIFromService();
    }

    private void PopulateUIFromService()
    {
        CultureCombo.SelectedIndex = _regionalSettings.Culture switch
        {
            "my-MM" => 1,
            "th-TH" => 2,
            _ => 0
        };

        DateFormatBox.Text = _regionalSettings.DateFormat;
        DateTimeFormatBox.Text = _regionalSettings.DateTimeFormat;
        CurrencySymbolBox.Text = _regionalSettings.CurrencySymbol;
        CurrencyCodeBox.Text = _regionalSettings.CurrencyCode;
        DecimalDigitsBox.Text = _regionalSettings.DecimalDigits.ToString(CultureInfo.InvariantCulture);
        FirstDayOfWeekCombo.SelectedIndex = _regionalSettings.FirstDayOfWeek == 0 ? 0 : 1;
        Use24HourCheck.IsChecked = _regionalSettings.Use24HourTime;
        FontFamilyBox.Text = _regionalSettings.FontFamily;
    }

    private void ApplyFontToWindow()
    {
        if (App.MainWindowInstance is MainWindow mw && !string.IsNullOrWhiteSpace(_regionalSettings.FontFamily))
        {
            mw.ApplyFontFamily(_regionalSettings.FontFamily);
        }
    }

    private void ApplyMyanmarButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        _regionalSettings.ApplyMyanmarPreset();
        PopulateUIFromService();
        ApplyFontToWindow();

        StatusMessage.Text = "Myanmar preset applied. Click Save to persist.";
        StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Green);
    }

    private void ApplyFontButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;

        _regionalSettings.ApplyMyanmarUnicodeFont();
        FontFamilyBox.Text = _regionalSettings.FontFamily;
        ApplyFontToWindow();

        StatusMessage.Text = "Pyidaungsu font applied. Click Save to persist.";
        StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Green);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage.Text = string.Empty;
        SaveButton.IsEnabled = false;

        try
        {
            // Read and validate UI values.
            var culture = CultureCombo.SelectedItem is ComboBoxItem cbi ? cbi.Content as string ?? "en-US" : "en-US";
            var dateFormat = DateFormatBox.Text ?? string.Empty;
            var dateTimeFormat = DateTimeFormatBox.Text ?? string.Empty;
            var currencySymbol = CurrencySymbolBox.Text ?? string.Empty;
            var currencyCode = CurrencyCodeBox.Text ?? string.Empty;

            if (!int.TryParse(DecimalDigitsBox.Text ?? string.Empty, NumberStyles.None, CultureInfo.InvariantCulture, out var decimalDigits) ||
                decimalDigits < 0 || decimalDigits > 10)
            {
                StatusMessage.Text = "Decimal digits must be a whole number between 0 and 10.";
                StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Red);
                return;
            }

            var firstDayOfWeek = FirstDayOfWeekCombo.SelectedIndex == 0 ? 0 : 1;
            var use24HourTime = Use24HourCheck.IsChecked == true;
            var fontFamily = FontFamilyBox.Text ?? "Segoe UI";

            // Apply to service.
            _regionalSettings.Culture = culture;
            _regionalSettings.DateFormat = dateFormat;
            _regionalSettings.DateTimeFormat = dateTimeFormat;
            _regionalSettings.CurrencySymbol = currencySymbol;
            _regionalSettings.CurrencyCode = currencyCode;
            _regionalSettings.DecimalDigits = decimalDigits;
            _regionalSettings.FirstDayOfWeek = firstDayOfWeek;
            _regionalSettings.Use24HourTime = use24HourTime;
            _regionalSettings.FontFamily = fontFamily;

            // Persist to database.
            await _regionalSettings.SaveToStoreAsync();

            // Apply font immediately.
            ApplyFontToWindow();

            StatusMessage.Text = "Regional settings saved.";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        catch (Exception ex)
        {
            StatusMessage.Text = $"Error saving settings: {ex.Message}";
            StatusMessage.Foreground = (Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Brush) ?? new SolidColorBrush(Microsoft.UI.Colors.Red);
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }
}
