using BProMax.Configuration;
using BProMax.Services;
using BProMax.Views;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;

namespace BProMax;

/// <summary>
/// WinUI 3 application composition root. Configures DI, configuration, and the main window.
/// Replaces the former static App.cs / Program.cs console entry point.
/// </summary>
public sealed partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    public static IConfiguration? Configuration { get; private set; }
    public static IntPtr MainWindowPtr { get; internal set; }
    public static MainWindow? MainWindowInstance { get; internal set; }

    public App()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Load .env file first (real credentials stay out of source control)
        Env.Load();

        // Build configuration: .env values > appsettings.json > environment variables
        var envConfig = new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING"),
        };

        Configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddInMemoryCollection(envConfig.Where(kv => kv.Value != null)
                .Select(kv => new KeyValuePair<string, string?>(kv.Key!, kv.Value)))
            .AddEnvironmentVariables()
            .Build();

        // Configure dependency injection
        var services = new ServiceCollection();
        if (Configuration != null)
        {
            services.AddSingleton(Configuration);
            services.Configure<AppSettings>(Configuration.GetSection("App"));
            services.Configure<DatabaseSettings>(Configuration.GetSection("Database"));
            services.Configure<SoundSettings>(Configuration.GetSection("Sound"));
            services.Configure<RegionalSettings>(Configuration.GetSection("Regional"));
            services.AddLogging(builder => builder.AddConsole());
        }

        // Services (local PostgreSQL via Npgsql)
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<ISettingsStore, SettingsStore>();
        services.AddSingleton<IAgentService, AgentService>();
        services.AddSingleton<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddSingleton<ICommissionService, CommissionService>();
        services.AddSingleton<ISalesFilterService, SalesFilterService>();
        services.AddSingleton<ISalesService, SalesService>();
        services.AddSingleton<IAutoBuyService, AutoBuyService>();
        services.AddSingleton<INumberBatchParser, NumberBatchParser>();
        services.AddSingleton<IBatchSaleBuilder, BatchSaleBuilder>();
        services.AddSingleton<INumberResolutionService, NumberResolutionService>();
        services.AddSingleton<IPayoutCalculator, PayoutCalculator>();
        services.AddSingleton<ISaleItemAmountCalculator, SaleItemAmountCalculator>();
        services.AddSingleton<ISalePanelLineFactory, SalePanelLineFactory>();
        services.AddSingleton<IBetTypeService, BetTypeService>();
        services.AddSingleton<IDrawService, DrawService>();
        services.AddSingleton<ISaleEntryService, SaleEntryService>();
        services.AddSingleton<IPayoutService, PayoutService>();
        services.AddSingleton<ITwoDReportService, TwoDReportService>();
        services.AddSingleton<ITwoDExportService, TwoDExportService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IExcelExportService, ExcelExportService>();
        services.AddSingleton<ICsvExportService, CsvExportService>();
        services.AddSingleton<IDatabaseMaintenanceService, DatabaseMaintenanceService>();
        services.AddSingleton<ISoundService, SoundService>();
        services.AddSingleton<IRegionalSettingsService, RegionalSettingsService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ILicensingService, LicensingService>();
        services.AddSingleton<ITimeLockService, TimeLockService>();

        Services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Initialize database (migration + seed) without blocking the UI thread.
        _ = InitializeDatabaseAsync();

        var window = new MainWindow();
        window.Activate();
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            var migration = Services?.GetRequiredService<IDatabaseMigrationService>();
            if (migration != null)
            {
                await migration.EnsureTablesCreatedAsync();
                var (tableExists, agentCount) = await migration.GetStatusAsync();

                if (!tableExists)
                {
                    if (App.Logger is { } logger)
                    {
                        _agentsTableNotFoundLog(logger, null);
                    }
                    return;
                }

                if (agentCount == 0)
                {
                    await migration.SeedDataAsync();
                }
            }
        }
        catch (Exception ex)
        {
            if (App.Logger is { } logger)
            {
                _databaseInitFailedLog(logger, ex);
            }
        }
    }

    private static readonly Action<ILogger, Exception?> _agentsTableNotFoundLog =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1, "AgentsTableNotFound"), "'agents' table not found. App runs in limited mode until table is created.");

    private static readonly Action<ILogger, Exception> _databaseInitFailedLog =
        LoggerMessage.Define(LogLevel.Warning, new EventId(2, "DatabaseInitializationFailed"), "Database initialization failed; app continues in limited mode.");

    private static ILogger? Logger =>
        Services?.GetRequiredService<ILogger<App>>();
}
