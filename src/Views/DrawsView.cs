using System.Globalization;
using BProMax.Helpers;
using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace BProMax.Views;

/// <summary>
/// WinUI 3 DrawsView (Page). Creates draws, records results, settles payouts, and marks payouts paid.
/// </summary>
public sealed partial class DrawsView : Page
{
    private readonly IDrawService _drawService;
    private readonly IPayoutService _payoutService;
    private readonly IAuthService _authService;

    private CalendarDatePicker DrawDatePicker => this.FindChildByName<CalendarDatePicker>("DrawDatePicker")!;
    private TextBox DrawTimeBox => this.FindChildByName<TextBox>("DrawTimeBox")!;
    private ListView DrawsList => this.FindChildByName<ListView>("DrawsList")!;
    private TextBox ResultBox => this.FindChildByName<TextBox>("ResultBox")!;
    private ListView PayoutsList => this.FindChildByName<ListView>("PayoutsList")!;
    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public DrawsView()
    {
        _drawService = App.Services?.GetRequiredService<IDrawService>()
            ?? throw new InvalidOperationException("DrawService not registered.");
        _payoutService = App.Services?.GetRequiredService<IPayoutService>()
            ?? throw new InvalidOperationException("PayoutService not registered.");
        _authService = App.Services?.GetRequiredService<IAuthService>()
            ?? throw new InvalidOperationException("AuthService not registered.");
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        DrawDatePicker.Date = DateTimeOffset.Now;
        try
        {
            await LoadDrawsAsync();
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", isError: true);
        }
    }

    private async Task LoadDrawsAsync(long? selectDrawId = null)
    {
        var selectedId = selectDrawId ?? (DrawsList.SelectedItem as DrawRecord)?.Id;
        var draws = await _drawService.GetDrawsAsync();
        DrawsList.ItemsSource = draws;

        var selected = selectedId.HasValue
            ? draws.FirstOrDefault(draw => draw.Id == selectedId.Value)
            : null;
        if (selected is not null)
        {
            DrawsList.SelectedItem = selected;
        }
        else if (draws.Count > 0)
        {
            DrawsList.SelectedIndex = 0;
        }
        else
        {
            PayoutsList.ItemsSource = null;
        }
    }

    private async void DrawsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            await LoadPayoutsAsync();
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", isError: true);
        }
    }

    private async Task LoadPayoutsAsync()
    {
        if (DrawsList.SelectedItem is not DrawRecord draw)
        {
            PayoutsList.ItemsSource = null;
            return;
        }

        PayoutsList.ItemsSource = await _payoutService.GetPayoutDetailsByDrawAsync(draw.Id);
    }

    private async void CreateDrawButton_Click(object sender, RoutedEventArgs e)
    {
        if (!DrawDatePicker.Date.HasValue)
        {
            ShowStatus("Select a draw date.", isError: true);
            return;
        }

        if (!TimeOnly.TryParseExact(
                DrawTimeBox.Text?.Trim(),
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var drawTime))
        {
            ShowStatus("Enter the draw time as HH:mm.", isError: true);
            return;
        }

        try
        {
            var drawDate = DateOnly.FromDateTime(DrawDatePicker.Date.Value.DateTime);
            var userId = await _authService.GetCurrentUserIdAsync();
            var drawId = await _drawService.CreateDrawAsync(drawDate, drawTime, userId);
            await LoadDrawsAsync(drawId);
            ShowStatus($"Draw {drawId} created.", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Create failed: {ex.Message}", isError: true);
        }
    }

    private async void CloseDrawButton_Click(object sender, RoutedEventArgs e)
    {
        if (DrawsList.SelectedItem is not DrawRecord draw)
        {
            ShowStatus("Select a draw.", isError: true);
            return;
        }

        try
        {
            var rows = await _drawService.CloseDrawAsync(draw.Id);
            await LoadDrawsAsync(draw.Id);
            ShowStatus(
                rows > 0 ? $"Draw {draw.Id} closed." : "Only an open draw can be closed.",
                isError: rows == 0);
        }
        catch (Exception ex)
        {
            ShowStatus($"Close failed: {ex.Message}", isError: true);
        }
    }

    private async void SetResultButton_Click(object sender, RoutedEventArgs e)
    {
        if (DrawsList.SelectedItem is not DrawRecord draw)
        {
            ShowStatus("Select a draw.", isError: true);
            return;
        }

        var result = ResultBox.Text?.Trim();
        if (result is not { Length: 2 }
            || !char.IsAsciiDigit(result[0])
            || !char.IsAsciiDigit(result[1]))
        {
            ShowStatus("The result must be exactly two digits from 00 to 99.", isError: true);
            return;
        }

        try
        {
            var rows = await _drawService.SetResultAsync(draw.Id, result);
            await LoadDrawsAsync(draw.Id);
            ShowStatus(
                rows > 0 ? $"Draw {draw.Id} result set to {result}." : "Only a closed draw can receive a result.",
                isError: rows == 0);
        }
        catch (Exception ex)
        {
            ShowStatus($"Set result failed: {ex.Message}", isError: true);
        }
    }

    private async void SettleDrawButton_Click(object sender, RoutedEventArgs e)
    {
        if (DrawsList.SelectedItem is not DrawRecord draw)
        {
            ShowStatus("Select a draw.", isError: true);
            return;
        }

        try
        {
            var userId = await _authService.GetCurrentUserIdAsync();
            var result = await _payoutService.SettleDrawAsync(draw.Id, userId);
            await LoadDrawsAsync(draw.Id);
            await LoadPayoutsAsync();
            ShowStatus(
                $"Draw {result.DrawNumber} settled: {result.ItemsSettled} item(s), "
                + $"{result.WinningItems} winner(s), total {FormatMoney(result.TotalPayout)}.",
                isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Settle failed: {ex.Message}", isError: true);
        }
    }

    private async void CancelDrawButton_Click(object sender, RoutedEventArgs e)
    {
        if (DrawsList.SelectedItem is not DrawRecord draw)
        {
            ShowStatus("Select a draw.", isError: true);
            return;
        }

        try
        {
            var rows = await _drawService.CancelDrawAsync(draw.Id);
            await LoadDrawsAsync(draw.Id);
            ShowStatus(
                rows > 0 ? $"Draw {draw.Id} cancelled." : "This draw cannot be cancelled.",
                isError: rows == 0);
        }
        catch (Exception ex)
        {
            ShowStatus($"Cancel failed: {ex.Message}", isError: true);
        }
    }

    private async void MarkPayoutPaidButton_Click(object sender, RoutedEventArgs e)
    {
        if (PayoutsList.SelectedItem is not PayoutDetailRecord payout)
        {
            ShowStatus("Select a payout.", isError: true);
            return;
        }

        var userId = await _authService.GetCurrentUserIdAsync();
        if (userId is null)
        {
            ShowStatus("The signed-in user could not be resolved.", isError: true);
            return;
        }

        try
        {
            var rows = await _payoutService.MarkPayoutPaidAsync(payout.PayoutId, userId.Value);
            await LoadPayoutsAsync();
            ShowStatus(
                rows > 0 ? $"Payout {payout.PayoutId} marked paid." : "Only a pending payout can be paid.",
                isError: rows == 0);
        }
        catch (Exception ex)
        {
            ShowStatus($"Mark paid failed: {ex.Message}", isError: true);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadDrawsAsync();
            await LoadPayoutsAsync();
            ShowStatus("Draws refreshed.", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", isError: true);
        }
    }

    private static string FormatMoney(decimal amount)
    {
        return amount.ToString("N2", CultureInfo.InvariantCulture);
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage.Text = message;
        StatusMessage.Foreground = isError
            ? Application.Current.Resources["SystemControlErrorTextForegroundBrush"] as Brush
                ?? new SolidColorBrush(Microsoft.UI.Colors.Red)
            : Application.Current.Resources["SystemControlSuccessTextForegroundBrush"] as Brush
                ?? new SolidColorBrush(Microsoft.UI.Colors.Green);
    }
}
