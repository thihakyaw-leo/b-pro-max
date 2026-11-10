using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BProMax.Services;
using BProMax.Configuration;
using System.Runtime.Versioning;

namespace BProMax;

/// <summary>
/// Application composition root. Configures dependency injection and settings.
/// In a future WinUI 3 migration, this logic moves into App.xaml.cs.
/// </summary>
[SupportedOSPlatform("windows")]
public static class App
{
    public static IServiceProvider? Services { get; private set; }
    public static IConfiguration? Configuration { get; private set; }

    public static void Initialize(string[] args)
    {
        // Load .env file first (real credentials stay out of source control)
        Env.Load();
        // Build configuration: .env values > appsettings.json > environment variables
        // Map .env variables to configuration sections:
        //   SUPABASE_URL -> Supabase:Url
        //   SUPABASE_PUBLISHABLE_KEY -> Supabase:AnonKey
        //   SUPABASE_SECRET_KEY -> Supabase:ServiceRoleKey
        var envConfig = new Dictionary<string, string?>
        {
            ["Supabase:Url"] = Environment.GetEnvironmentVariable("SUPABASE_URL"),
            ["Supabase:AnonKey"] = Environment.GetEnvironmentVariable("SUPABASE_PUBLISHABLE_KEY"),
            ["Supabase:ServiceRoleKey"] = Environment.GetEnvironmentVariable("SUPABASE_SECRET_KEY"),
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
        ConfigureServices(services, Configuration);
        Services = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration? configuration)
    {
        if (configuration != null)
        {
            services.AddSingleton(configuration);
            services.Configure<AppSettings>(configuration.GetSection("App"));
            services.Configure<DatabaseSettings>(configuration.GetSection("Database"));
            services.Configure<SupabaseSettings>(configuration.GetSection("Supabase"));
            services.Configure<SoundSettings>(configuration.GetSection("Sound"));
            services.Configure<TimeLockSettings>(configuration.GetSection("TimeLock"));
            services.AddLogging(builder => builder.AddConsole());
        }

        // Services
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<ISupabaseAgentService, SupabaseAgentService>();
        services.AddSingleton<IAgentService, AgentServiceAdapter>();
        services.AddSingleton<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddSingleton<ICommissionService, CommissionService>();
        services.AddSingleton<ISalesFilterService, SalesFilterService>();
        services.AddSingleton<IAutoBuyService, AutoBuyService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IExcelExportService, ExcelExportService>();
        services.AddSingleton<ICsvExportService, CsvExportService>();
        services.AddSingleton<IDatabaseMaintenanceService, DatabaseMaintenanceService>();
        services.AddSingleton<ISoundService, SoundService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<ILicensingService, LicensingService>();
        services.AddSingleton<ITimeLockService, TimeLockService>();
    }
}
