using System.Runtime.InteropServices;
using BProMax.Helpers;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace BProMax.Views;

/// <summary>
/// Main shell window. Hosts a NavigationView and a Frame for page navigation.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IAuthService _auth;
    private readonly ILicensingService _licensing;

    private NavigationView NavView => this.FindChildByName<NavigationView>("NavView")!;
    private Frame ContentFrame => this.FindChildByName<Frame>("ContentFrame")!;
    private NavigationViewItem NavLogin => this.FindChildByName<NavigationViewItem>("NavLogin")!;
    private NavigationViewItem NavRegister => this.FindChildByName<NavigationViewItem>("NavRegister")!;
    private NavigationViewItem NavLogout => this.FindChildByName<NavigationViewItem>("NavLogout")!;
    private NavigationViewItem NavAgents => this.FindChildByName<NavigationViewItem>("NavAgents")!;
    private NavigationViewItem NavSearch => this.FindChildByName<NavigationViewItem>("NavSearch")!;
    private NavigationViewItem NavSales => this.FindChildByName<NavigationViewItem>("NavSales")!;
    private NavigationViewItem NavDraws => this.FindChildByName<NavigationViewItem>("NavDraws")!;
    private NavigationViewItem NavNumberWisdom => this.FindChildByName<NavigationViewItem>("NavNumberWisdom")!;
    private NavigationViewItem NavAutoBuy => this.FindChildByName<NavigationViewItem>("NavAutoBuy")!;
    private NavigationViewItem NavReports => this.FindChildByName<NavigationViewItem>("NavReports")!;
    private NavigationViewItem NavMaintenance => this.FindChildByName<NavigationViewItem>("NavMaintenance")!;
    private NavigationViewItem NavBackup => this.FindChildByName<NavigationViewItem>("NavBackup")!;
    private NavigationViewItem NavCreateDb => this.FindChildByName<NavigationViewItem>("NavCreateDb")!;
    private NavigationViewItem NavCompact => this.FindChildByName<NavigationViewItem>("NavCompact")!;
    private NavigationViewItem NavSound => this.FindChildByName<NavigationViewItem>("NavSound")!;
    private NavigationViewItem NavTimeLock => this.FindChildByName<NavigationViewItem>("NavTimeLock")!;

    public MainWindow()
    {
        App.MainWindowPtr = WinRT.Interop.WindowNative.GetWindowHandle(this);
        App.MainWindowInstance = this;
        _auth = App.Services?.GetRequiredService<IAuthService>()
            ?? throw new InvalidOperationException("AuthService not registered.");
        _licensing = App.Services?.GetRequiredService<ILicensingService>()
            ?? throw new InvalidOperationException("LicensingService not registered.");

        NavView.ItemInvoked += NavView_ItemInvoked;
        NavView.PaneOpened += (_, _) => UpdateMenuState();

        SizeWindow();

        // Startup navigation: show login page on first launch.
        ContentFrame.Navigate(typeof(LoginView));
        NavView.SelectedItem = NavLogin;
        UpdateMenuState();
    }

    private void SizeWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var scale = GetDpiForWindow(hwnd) / 96.0;

        // Rubric: multi-pane (nav + content) → ~1180 wide × ~700 tall.
        // Width = nav pane (~280) + widest content (~840) + padding (48 + margin) → round up to 20.
        // Height = titlebar (32) + content rows (~620) + spacing + padding (48) → round up to 20.
        const int widthDip = 1180;
        const int heightDip = 700;

        AppWindow.Resize(new SizeInt32((int)(widthDip * scale), (int)(heightDip * scale)));
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var item = args.InvokedItemContainer as NavigationViewItem;

        if (item == NavLogin)
        {
            NavigateTo(typeof(LoginView));
            return;
        }
        if (item == NavRegister)
        {
            NavigateTo(typeof(RegisterView));
            return;
        }
        if (item == NavLogout)
        {
            _ = _auth.LogoutAsync();
            UpdateMenuState();
            NavigateTo(typeof(LoginView));
            return;
        }

        // Feature pages require authentication.
        if (!_auth.IsAuthenticated)
        {
            NavView.SelectedItem = NavLogin;
            ContentFrame.Navigate(typeof(LoginView));
            return;
        }

        if (item == NavAgents) NavigateTo(typeof(AgentListView));
        else if (item == NavSearch) NavigateTo(typeof(AgentSearchView));
        else if (item == NavSales) NavigateTo(typeof(SalesFilterView));
        else if (item == NavDraws) NavigateTo(typeof(DrawsView));
        else if (item == NavNumberWisdom) NavigateTo(typeof(NumberWisdomView));
        else if (item == NavAutoBuy) NavigateTo(typeof(AutoBuyView));
        else if (item == NavReports) NavigateTo(typeof(ReportView));
        else if (item == NavMaintenance) NavigateTo(typeof(MaintenanceView));
        else if (item == NavBackup) NavigateTo(typeof(BackupView));
        else if (item == NavCreateDb) NavigateTo(typeof(CreateDatabaseView));
        else if (item == NavCompact) NavigateTo(typeof(CompactDatabaseView));
        else if (item == NavSound) NavigateTo(typeof(SoundSettingsView));
        else if (item == NavTimeLock) NavigateTo(typeof(TimeLockView));
    }

    private void NavigateTo(Type pageType)
    {
        if (ContentFrame.Content?.GetType() == pageType) return;
        ContentFrame.Navigate(pageType);
    }

    public void NavigateToAuthenticatedHome()
    {
        NavView.SelectedItem = NavAgents;
        ContentFrame.Navigate(typeof(AgentListView));
    }

    public void UpdateMenuState()
    {
        bool authenticated = _auth.IsAuthenticated;
        bool licensed = _licensing.IsRegistered;

        NavAgents.IsEnabled = authenticated;
        NavSearch.IsEnabled = authenticated;
        NavSales.IsEnabled = authenticated;
        NavDraws.IsEnabled = authenticated;
        NavNumberWisdom.IsEnabled = authenticated;
        NavAutoBuy.IsEnabled = authenticated;
        NavReports.IsEnabled = authenticated;
        NavMaintenance.IsEnabled = authenticated;
        NavBackup.IsEnabled = authenticated;
        NavCreateDb.IsEnabled = authenticated;
        NavCompact.IsEnabled = authenticated;
        NavSound.IsEnabled = authenticated;
        NavTimeLock.IsEnabled = authenticated;

        NavLogin.Visibility = authenticated ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
        NavRegister.Visibility = authenticated ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
        NavLogout.Visibility = authenticated ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        NavLogout.IsEnabled = authenticated;
    }
}
