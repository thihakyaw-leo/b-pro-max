using BProMax.Configuration;
using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BProMax.Services;

/// <summary>
/// Database migration service for creating tables and seeding data.
/// Uses Supabase REST API for data operations; provides SQL guidance for DDL.
/// </summary>
public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly ISupabaseAgentService _supabaseService;
    private readonly ILogger<DatabaseMigrationService> _logger;
    private bool _migrationChecked;

    private static readonly Action<ILogger, string, Exception?> _logTableNotFound =
        LoggerMessage.Define<string>(LogLevel.Warning, 1, "The 'agents' table was not found in Supabase. Please run the following SQL in your Supabase SQL Editor:\n{Sql}");
    private static readonly Action<ILogger, Exception?> _logTableExists =
        LoggerMessage.Define(LogLevel.Information, 2, "'agents' table exists in Supabase.");
    private static readonly Action<ILogger, Exception?> _logCannotSeed =
        LoggerMessage.Define(LogLevel.Warning, 3, "Cannot seed data: 'agents' table does not exist. Run migration first.");
    private static readonly Action<ILogger, int, Exception?> _logSeedSkipped =
        LoggerMessage.Define<int>(LogLevel.Information, 4, "Seed skipped: {Count} agent(s) already exist.");
    private static readonly Action<ILogger, int, Exception?> _logSeeded =
        LoggerMessage.Define<int>(LogLevel.Information, 5, "Seeded {Count} test agents.");

    public DatabaseMigrationService(
        ISupabaseAgentService supabaseService,
        ILogger<DatabaseMigrationService> logger)
    {
        _supabaseService = supabaseService;
        _logger = logger;
    }

    /// <summary>
    /// SQL to create the agents table. Run this in Supabase SQL Editor.
    /// </summary>
    public const string CreateAgentsTableSql = @"
CREATE TABLE IF NOT EXISTS agents (
    id SERIAL PRIMARY KEY,
    agent_name TEXT NOT NULL,
    address TEXT,
    phone TEXT,
    commission NUMERIC(12,2) DEFAULT 0,
    rate NUMERIC(12,2) DEFAULT 0,
    commission1 NUMERIC(12,2) DEFAULT 0,
    rate1 NUMERIC(12,2) DEFAULT 0,
    currency_type TEXT,
    currency_rate NUMERIC(12,4) DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_agents_agent_name ON agents(agent_name);
CREATE INDEX IF NOT EXISTS idx_agents_created_at ON agents(created_at);
";

    public async Task<bool> AgentsTableExistsAsync()
    {
        try
        {
            // Try to get all agents - if table exists, this succeeds
            await _supabaseService.GetAllAgentsAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task EnsureTablesCreatedAsync()
    {
        if (_migrationChecked) return;
        _migrationChecked = true;

        var exists = await AgentsTableExistsAsync();
        if (!exists)
        {
            _logTableNotFound(_logger, CreateAgentsTableSql, null);
        }
        else
        {
            _logTableExists(_logger, null);
        }
    }

    public async Task SeedDataAsync()
    {
        var exists = await AgentsTableExistsAsync();
        if (!exists)
        {
            _logCannotSeed(_logger, null);
            return;
        }

        // Check if already have data
        var agents = await _supabaseService.GetAllAgentsAsync();
        if (agents.Count > 0)
        {
            _logSeedSkipped(_logger, agents.Count, null);
            return;
        }

        // Seed test agents
        var seedAgents = new[]
        {
            new SupabaseAgent
            {
                AgentName = "John Doe",
                Address = "123 Main Street, Bangkok",
                Phone = "02-123-4567",
                Commission = 1000m,
                Rate = 5m,
                Commission1 = 500m,
                Rate1 = 2.5m,
                CurrencyType = "THB",
                CurrencyRate = 1m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SupabaseAgent
            {
                AgentName = "Jane Smith",
                Address = "456 Sukhumvit Rd, Bangkok",
                Phone = "02-765-4321",
                Commission = 1500m,
                Rate = 7.5m,
                Commission1 = 750m,
                Rate1 = 3m,
                CurrencyType = "THB",
                CurrencyRate = 1m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SupabaseAgent
            {
                AgentName = "Bob Wilson",
                Address = "789 Silom Rd, Bangkok",
                Phone = "02-555-0100",
                Commission = 800m,
                Rate = 4m,
                Commission1 = 400m,
                Rate1 = 2m,
                CurrencyType = "USD",
                CurrencyRate = 35.5m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var agent in seedAgents)
        {
            await _supabaseService.CreateAgentAsync(agent);
        }

        _logSeeded(_logger, seedAgents.Length, null);
    }

    public async Task<(bool tableExists, int agentCount)> GetStatusAsync()
    {
        var exists = await AgentsTableExistsAsync();
        if (!exists) return (false, 0);

        var agents = await _supabaseService.GetAllAgentsAsync();
        return (true, agents.Count);
    }
}
