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
/// WinUI 3 NumberWisdomView (Page). Dealer administration for bet types and fixed groups.
/// </summary>
public sealed partial class NumberWisdomView : Page
{
    private readonly IBetTypeService _betTypeService;
    private readonly INumberBatchParser _batchParser;

    private TextBlock BetTypeHeader => this.FindChildByName<TextBlock>("BetTypeHeader")!;
    private TextBlock BetTypeCodeText => this.FindChildByName<TextBlock>("BetTypeCodeText")!;
    private TextBlock BetTypeLabelText => this.FindChildByName<TextBlock>("BetTypeLabelText")!;
    private TextBlock BetTypeEnglishText => this.FindChildByName<TextBlock>("BetTypeEnglishText")!;
    private TextBox BetTypeMultiplierBox => this.FindChildByName<TextBox>("BetTypeMultiplierBox")!;
    private TextBox BetTypeSortOrderBox => this.FindChildByName<TextBox>("BetTypeSortOrderBox")!;
    private CheckBox BetTypeActiveCheck => this.FindChildByName<CheckBox>("BetTypeActiveCheck")!;
    private Button SaveBetTypeButton => this.FindChildByName<Button>("SaveBetTypeButton")!;
    private ListView BetTypesList => this.FindChildByName<ListView>("BetTypesList")!;

    private TextBlock GroupHeader => this.FindChildByName<TextBlock>("GroupHeader")!;
    private TextBlock GroupCodeText => this.FindChildByName<TextBlock>("GroupCodeText")!;
    private TextBlock GroupLabelText => this.FindChildByName<TextBlock>("GroupLabelText")!;
    private TextBlock GroupEnglishText => this.FindChildByName<TextBlock>("GroupEnglishText")!;
    private TextBox GroupMembersBox => this.FindChildByName<TextBox>("GroupMembersBox")!;
    private CheckBox GroupAllowReverseCheck => this.FindChildByName<CheckBox>("GroupAllowReverseCheck")!;
    private TextBox GroupSortOrderBox => this.FindChildByName<TextBox>("GroupSortOrderBox")!;
    private CheckBox GroupActiveCheck => this.FindChildByName<CheckBox>("GroupActiveCheck")!;
    private Button SaveGroupButton => this.FindChildByName<Button>("SaveGroupButton")!;
    private ListView GroupsList => this.FindChildByName<ListView>("GroupsList")!;

    private TextBlock StatusMessage => this.FindChildByName<TextBlock>("StatusMessage")!;

    public NumberWisdomView()
    {
        _betTypeService = App.Services?.GetRequiredService<IBetTypeService>()
            ?? throw new InvalidOperationException("BetTypeService not registered.");
        _batchParser = App.Services?.GetRequiredService<INumberBatchParser>()
            ?? throw new InvalidOperationException("NumberBatchParser not registered.");

        SaveBetTypeButton.IsEnabled = false;
        SaveGroupButton.IsEnabled = false;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        try
        {
            await LoadBetTypesAsync();
            await LoadGroupsAsync();
            ShowStatus("Number Wisdom loaded.", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", isError: true);
        }
    }

    private async Task LoadBetTypesAsync(int? selectId = null)
    {
        var selectedId = selectId ?? (BetTypesList.SelectedItem as BetTypeRecord)?.Id;
        var betTypes = await _betTypeService.GetBetTypesAsync(activeOnly: false);
        BetTypesList.ItemsSource = betTypes;

        var selected = selectedId.HasValue
            ? betTypes.FirstOrDefault(betType => betType.Id == selectedId.Value)
            : null;
        if (selected is not null)
        {
            BetTypesList.SelectedItem = selected;
        }
        else if (betTypes.Count > 0)
        {
            BetTypesList.SelectedIndex = 0;
        }
        else
        {
            ClearBetTypeForm();
        }
    }

    private async Task LoadGroupsAsync(int? selectId = null)
    {
        var selectedId = selectId ?? (GroupsList.SelectedItem as NumberGroupRecord)?.Id;
        var groups = await _betTypeService.GetNumberGroupsAsync(activeOnly: false);
        GroupsList.ItemsSource = groups;

        var selected = selectedId.HasValue
            ? groups.FirstOrDefault(group => group.Id == selectedId.Value)
            : null;
        if (selected is not null)
        {
            GroupsList.SelectedItem = selected;
        }
        else if (groups.Count > 0)
        {
            GroupsList.SelectedIndex = 0;
        }
        else
        {
            ClearGroupForm();
        }
    }

    private void BetTypesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BetTypesList.SelectedItem is not BetTypeRecord betType)
        {
            ClearBetTypeForm();
            return;
        }

        BetTypeHeader.Text = betType.NameMy;
        BetTypeCodeText.Text = betType.Code;
        BetTypeLabelText.Text = betType.NameMy;
        BetTypeEnglishText.Text = betType.NameEn;
        BetTypeMultiplierBox.Text = betType.PayoutMultiplier.ToString("0.####", CultureInfo.InvariantCulture);
        BetTypeSortOrderBox.Text = betType.SortOrder.ToString(CultureInfo.InvariantCulture);
        BetTypeActiveCheck.IsChecked = betType.IsActive;
        SaveBetTypeButton.IsEnabled = true;
    }

    private void GroupsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GroupsList.SelectedItem is not NumberGroupRecord group)
        {
            ClearGroupForm();
            return;
        }

        GroupHeader.Text = group.NameMy;
        GroupCodeText.Text = group.Code;
        GroupLabelText.Text = group.NameMy;
        GroupEnglishText.Text = group.NameEn;
        GroupMembersBox.Text = string.Join(", ", group.Members);
        GroupAllowReverseCheck.IsChecked = group.AllowReverse;
        GroupSortOrderBox.Text = group.SortOrder.ToString(CultureInfo.InvariantCulture);
        GroupActiveCheck.IsChecked = group.IsActive;
        SaveGroupButton.IsEnabled = true;
    }

    private async void SaveBetTypeButton_Click(object sender, RoutedEventArgs e)
    {
        if (BetTypesList.SelectedItem is not BetTypeRecord betType)
        {
            ShowStatus("Select a bet type.", isError: true);
            return;
        }

        if (!decimal.TryParse(
                BetTypeMultiplierBox.Text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var multiplier)
            || multiplier <= 0)
        {
            ShowStatus("Enter a payout multiplier greater than zero.", isError: true);
            return;
        }

        if (!int.TryParse(
                BetTypeSortOrderBox.Text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var sortOrder)
            || sortOrder < 0)
        {
            ShowStatus("Enter a sort order of zero or greater.", isError: true);
            return;
        }

        SaveBetTypeButton.IsEnabled = false;
        try
        {
            var rows = await _betTypeService.UpdateBetTypeAsync(
                betType.Id,
                multiplier,
                BetTypeActiveCheck.IsChecked == true,
                sortOrder);

            await LoadBetTypesAsync(betType.Id);
            ShowStatus(
                rows > 0
                    ? $"{betType.NameMy} saved: {multiplier.ToString("0.####", CultureInfo.InvariantCulture)}x."
                    : "The bet type was not found.",
                isError: rows == 0);
        }
        catch (Exception ex)
        {
            ShowStatus($"Save failed: {ex.Message}", isError: true);
        }
        finally
        {
            SaveBetTypeButton.IsEnabled = true;
        }
    }

    private async void SaveGroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (GroupsList.SelectedItem is not NumberGroupRecord group)
        {
            ShowStatus("Select a fixed group.", isError: true);
            return;
        }

        if (!_batchParser.TryParse(GroupMembersBox.Text, out var members, out var parseError))
        {
            ShowStatus(parseError ?? "Enter the group members.", isError: true);
            return;
        }

        if (members.Distinct(StringComparer.Ordinal).Count() != members.Count)
        {
            ShowStatus("A group cannot contain duplicate numbers.", isError: true);
            return;
        }

        if (!int.TryParse(
                GroupSortOrderBox.Text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var sortOrder)
            || sortOrder < 0)
        {
            ShowStatus("Enter a sort order of zero or greater.", isError: true);
            return;
        }

        SaveGroupButton.IsEnabled = false;
        try
        {
            await _betTypeService.UpdateNumberGroupAsync(
                group.Id,
                GroupAllowReverseCheck.IsChecked == true,
                GroupActiveCheck.IsChecked == true,
                sortOrder,
                members);

            await LoadGroupsAsync(group.Id);
            ShowStatus($"{group.NameMy} saved with {members.Count} member(s).", isError: false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Save failed: {ex.Message}", isError: true);
        }
        finally
        {
            SaveGroupButton.IsEnabled = true;
        }
    }

    private void ClearBetTypeForm()
    {
        BetTypeHeader.Text = "Select a bet type";
        BetTypeCodeText.Text = string.Empty;
        BetTypeLabelText.Text = string.Empty;
        BetTypeEnglishText.Text = string.Empty;
        BetTypeMultiplierBox.Text = string.Empty;
        BetTypeSortOrderBox.Text = string.Empty;
        BetTypeActiveCheck.IsChecked = false;
        SaveBetTypeButton.IsEnabled = false;
    }

    private void ClearGroupForm()
    {
        GroupHeader.Text = "Select a fixed group";
        GroupCodeText.Text = string.Empty;
        GroupLabelText.Text = string.Empty;
        GroupEnglishText.Text = string.Empty;
        GroupMembersBox.Text = string.Empty;
        GroupAllowReverseCheck.IsChecked = false;
        GroupSortOrderBox.Text = string.Empty;
        GroupActiveCheck.IsChecked = false;
        SaveGroupButton.IsEnabled = false;
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
