using System.Runtime.Versioning;
using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BProMax;

/// <summary>
/// Simple console menu for testing core functionality before WinUI 3 migration.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class InteractiveMenu
{
    private readonly IServiceProvider? _services;

    public InteractiveMenu(IServiceProvider? services)
    {
        _services = services;
    }

    public async Task RunAsync()
    {
        // Check if console input is available (dotnet run may redirect stdin)
        bool hasConsole = !Console.IsInputRedirected;

        if (!hasConsole)
        {
            Console.WriteLine("Console input is redirected. Interactive menu not available.");
            Console.WriteLine("Run directly from terminal for full menu access.");
            return;
        }

        while (true)
        {
            Console.WriteLine("Main Menu:");
            Console.WriteLine("  1. List all agents");
            Console.WriteLine("  2. Search agents by name");
            Console.WriteLine("  3. Add new agent");
            Console.WriteLine("  4. Calculate commission");
            Console.WriteLine("  5. Test sound alert");
            Console.WriteLine("  6. Show config");
            Console.WriteLine("  0. Exit");
            Console.Write("Select option: ");

            var input = Console.ReadLine();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    await ListAgentsAsync();
                    break;
                case "2":
                    await SearchAgentsAsync();
                    break;
                case "3":
                    await AddAgentAsync();
                    break;
                case "4":
                    await CalcCommissionAsync();
                    break;
                case "5":
                    await TestSoundAsync();
                    break;
                case "6":
                    ShowConfig();
                    break;
                case "0":
                    Console.WriteLine("Goodbye!");
                    return;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.WriteLine();
        }
    }

    private async Task ListAgentsAsync()
    {
        var agentService = _services?.GetRequiredService<IAgentService>();
        if (agentService == null) return;

        var agents = await agentService.GetAllAgentsAsync();
        Console.WriteLine($"Agents ({agents.Count}):");
        foreach (var a in agents)
        {
            Console.WriteLine($"  [{a.Id}] {a.AgentName} - {a.Phone} - Comm: {a.Commission} - Rate: {a.Rate}");
        }
    }

    private async Task SearchAgentsAsync()
    {
        var agentService = _services?.GetRequiredService<IAgentService>();
        if (agentService == null) return;

        Console.Write("Enter search term: ");
        var term = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(term)) return;

        var agents = await agentService.SearchAgentsAsync(term);
        Console.WriteLine($"Found {agents.Count} agent(s):");
        foreach (var a in agents)
        {
            Console.WriteLine($"  [{a.Id}] {a.AgentName} - {a.Phone}");
        }
    }

    private async Task AddAgentAsync()
    {
        var agentService = _services?.GetRequiredService<IAgentService>();
        if (agentService == null) return;

        Console.Write("Agent name: ");
        var name = Console.ReadLine();
        Console.Write("Address: ");
        var address = Console.ReadLine();
        Console.Write("Phone: ");
        var phone = Console.ReadLine();
        Console.Write("Commission: ");
        decimal commission = decimal.TryParse(Console.ReadLine(), out var c) ? c : 0;
        Console.Write("Rate: ");
        decimal rate = decimal.TryParse(Console.ReadLine(), out var r) ? r : 0;

        var agent = new Agent
        {
            AgentName = name ?? string.Empty,
            Address = address,
            Phone = phone,
            Commission = commission,
            Rate = rate
        };

        var id = await agentService.CreateAgentAsync(agent);
        Console.WriteLine($"Agent created with ID: {id}");
    }

    private Task CalcCommissionAsync()
    {
        var commissionService = _services?.GetRequiredService<ICommissionService>();
        if (commissionService == null) return Task.CompletedTask;

        Console.Write("Amount: ");
        decimal amount = decimal.TryParse(Console.ReadLine(), out var a) ? a : 0;
        Console.Write("Rate: ");
        decimal rate = decimal.TryParse(Console.ReadLine(), out var r) ? r : 0;

        var commission = commissionService.CalculateCommission(amount, rate);
        Console.WriteLine($"Commission: {commission}");
        return Task.CompletedTask;
    }

    private async Task TestSoundAsync()
    {
        var soundService = _services?.GetRequiredService<ISoundService>();
        if (soundService == null) return;

        Console.WriteLine("Playing alert sound...");
        await soundService.PlayAlertSoundAsync();
        Console.WriteLine("Sound played.");
    }

    private void ShowConfig()
    {
        var config = App.Configuration;
        if (config == null) return;

        Console.WriteLine($"App Name: {config["App:Name"]}");
        Console.WriteLine($"App Version: {config["App:Version"]}");
        Console.WriteLine($"Database Host: {config["Database:ConnectionString"]}");
        Console.WriteLine($"Sound Enabled: {config["Sound:Enabled"]}");
    }
}
