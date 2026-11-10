using BProMax;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Versioning;

// b-pro-max — Modern upgrade of DD.exe (VB6 lottery/agent management system)
// Tech stack: .NET 8 + C# + Supabase (REST API) + EPPlus + JSON config
// UI: Console application now; WinUI 3 XAML views to be added in Visual Studio

namespace BProMax;

internal sealed class Program
{
    [SupportedOSPlatform("windows")]
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  b-pro-max — Lottery Agent Management");
        Console.WriteLine("  Modern upgrade of DD.exe (VB6)");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // Initialize application (DI, configuration)
        App.Initialize(args);

        // Test Supabase connection (REST API via HTTPS, not direct PostgreSQL)
        var supabase = App.Services?.GetRequiredService<ISupabaseAgentService>();
        if (supabase != null)
        {
            Console.Write("Testing Supabase connection... ");
            bool connected = await supabase.TestConnectionAsync();
            Console.WriteLine(connected ? "OK" : "FAILED");
            Console.WriteLine();
        }

        // Run database migration check
        var migration = App.Services?.GetRequiredService<IDatabaseMigrationService>();
        if (migration != null)
        {
            var (tableExists, agentCount) = await migration.GetStatusAsync();

            if (!tableExists)
            {
                Console.WriteLine("WARNING: 'agents' table not found in Supabase.");
                Console.WriteLine();
                Console.WriteLine("To create it, run this SQL in your Supabase SQL Editor:");
                Console.WriteLine("(https://app.supabase.com/ -> SQL Editor -> New query)");
                Console.WriteLine();
                Console.WriteLine(DatabaseMigrationService.CreateAgentsTableSql);
                Console.WriteLine();
                Console.WriteLine("The app will continue in limited mode until the table is created.");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($"'agents' table found. Current agents: {agentCount}");

                if (agentCount == 0)
                {
                    Console.WriteLine("Seeding test data...");
                    await migration.SeedDataAsync();

                    var (_, newCount) = await migration.GetStatusAsync();
                    Console.WriteLine($"Seeded {newCount} test agents.");
                }
                Console.WriteLine();
            }
        }

        // Run the interactive menu
        var menu = new InteractiveMenu(App.Services);
        await menu.RunAsync();

        return 0;
    }
}
