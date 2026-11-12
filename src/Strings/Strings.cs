using System.Globalization;
using System.Reflection;
using System.Resources;

namespace BProMax.Strings;

/// <summary>
/// Provides access to localized string resources from Strings/Resources.resx.
/// Used in code-behind where x:Uid / ResourceLoader is unavailable (PRI generation is disabled).
/// </summary>
public static class StringResources
{
    private static readonly ResourceManager _resourceManager = new(
        "BProMax.Strings.Resources",
        Assembly.GetExecutingAssembly());

    public static string Get(string name) =>
        _resourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? $"[{name}]";

    public static string Format(string name, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(name), args);

    // Application title
    public static string AppTitle => Get(nameof(AppTitle));

    // MainWindow navigation
    public static string NavLogin => Get(nameof(NavLogin));
    public static string NavAgents => Get(nameof(NavAgents));
    public static string NavSearchAgents => Get(nameof(NavSearchAgents));
    public static string NavSales => Get(nameof(NavSales));
    public static string NavDraws => Get(nameof(NavDraws));
    public static string NavNumberWisdom => Get(nameof(NavNumberWisdom));
    public static string NavAutoBuy => Get(nameof(NavAutoBuy));
    public static string NavReports => Get(nameof(NavReports));
    public static string NavMaintenance => Get(nameof(NavMaintenance));
    public static string NavBackup => Get(nameof(NavBackup));
    public static string NavCreateDatabase => Get(nameof(NavCreateDatabase));
    public static string NavCompactDatabase => Get(nameof(NavCompactDatabase));
    public static string NavSoundSettings => Get(nameof(NavSoundSettings));
    public static string NavTimeLock => Get(nameof(NavTimeLock));
    public static string NavRegister => Get(nameof(NavRegister));
    public static string NavLogout => Get(nameof(NavLogout));

    // LoginView
    public static string LoginTitle => Get(nameof(LoginTitle));
    public static string LoginUsername => Get(nameof(LoginUsername));
    public static string LoginUsernamePlaceholder => Get(nameof(LoginUsernamePlaceholder));
    public static string LoginPassword => Get(nameof(LoginPassword));
    public static string LoginPasswordPlaceholder => Get(nameof(LoginPasswordPlaceholder));
    public static string LoginButton => Get(nameof(LoginButton));

    // Common buttons
    public static string BtnSave => Get(nameof(BtnSave));
    public static string BtnCancel => Get(nameof(BtnCancel));
    public static string BtnDelete => Get(nameof(BtnDelete));
    public static string BtnRefresh => Get(nameof(BtnRefresh));
    public static string BtnSearch => Get(nameof(BtnSearch));
    public static string BtnCreate => Get(nameof(BtnCreate));
    public static string BtnUpdate => Get(nameof(BtnUpdate));

    // Status messages
    public static string MsgInvalidInput => Get(nameof(MsgInvalidInput));
    public static string MsgRequired => Get(nameof(MsgRequired));
    public static string MsgError => Get(nameof(MsgError));

    // AutoBuyView
    public static string AutoBuyTitle => Get(nameof(AutoBuyTitle));
    public static string AutoBuyAgentNameLabel => Get(nameof(AutoBuyAgentNameLabel));
    public static string AutoBuyTriggerAmountLabel => Get(nameof(AutoBuyTriggerAmountLabel));
    public static string AutoBuyAutoBuyAmountLabel => Get(nameof(AutoBuyAutoBuyAmountLabel));
    public static string AutoBuyEnabledLabel => Get(nameof(AutoBuyEnabledLabel));
    public static string AutoBuyRuleCreated => Get(nameof(AutoBuyRuleCreated));
    public static string AutoBuyRuleUpdated => Get(nameof(AutoBuyRuleUpdated));
    public static string AutoBuyRuleDeleted => Get(nameof(AutoBuyRuleDeleted));
    public static string AutoBuyNoRuleFound => Get(nameof(AutoBuyNoRuleFound));

    // DrawsView
    public static string DrawsTitle => Get(nameof(DrawsTitle));
    public static string DrawsStatusFlow => Get(nameof(DrawsStatusFlow));
    public static string DrawDateLabel => Get(nameof(DrawDateLabel));
    public static string DrawTimeLabel => Get(nameof(DrawTimeLabel));
    public static string DrawResultLabel => Get(nameof(DrawResultLabel));
    public static string BtnCreateDraw => Get(nameof(BtnCreateDraw));
    public static string BtnCloseDraw => Get(nameof(BtnCloseDraw));
    public static string BtnSetResult => Get(nameof(BtnSetResult));
    public static string BtnSettleDraw => Get(nameof(BtnSettleDraw));
    public static string BtnCancelDraw => Get(nameof(BtnCancelDraw));
    public static string BtnMarkPayoutPaid => Get(nameof(BtnMarkPayoutPaid));
}
