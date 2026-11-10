using System.Globalization;
using BProMax.Configuration;
using BProMax.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Supabase.Postgrest;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Models;
using static Supabase.Postgrest.Constants;

namespace BProMax.Services;

/// <summary>
/// Supabase REST API implementation of agent management using Supabase.Postgrest v4.0.0.
/// Connects to Supabase-hosted PostgreSQL via REST API.
/// </summary>
public class SupabaseAgentService : ISupabaseAgentService
{
    private readonly SupabaseSettings _settings;
    private readonly ILogger<SupabaseAgentService> _logger;
    private readonly Client _client;

    private static readonly Action<ILogger, string, int, bool, Exception?> _logSupabaseConfig =
        LoggerMessage.Define<string, int, bool>(LogLevel.Information, 1, "Supabase Config: Url={Url}, AnonKey={AnonKeyLength} chars, Url set={UrlSet}");
    private static readonly Action<ILogger, Exception?> _logFailedToGetAllAgents =
        LoggerMessage.Define(LogLevel.Error, 2, "Failed to get all agents from Supabase");
    private static readonly Action<ILogger, int, Exception?> _logFailedToGetAgentById =
        LoggerMessage.Define<int>(LogLevel.Error, 3, "Failed to get agent by ID {AgentId} from Supabase");
    private static readonly Action<ILogger, string, Exception?> _logFailedToGetAgentByName =
        LoggerMessage.Define<string>(LogLevel.Error, 4, "Failed to get agent by name {AgentName} from Supabase");
    private static readonly Action<ILogger, Exception?> _logFailedToSearchAgents =
        LoggerMessage.Define(LogLevel.Error, 5, "Failed to search agents from Supabase");
    private static readonly Action<ILogger, Exception?> _logFailedToCreateAgent =
        LoggerMessage.Define(LogLevel.Error, 6, "Failed to create agent in Supabase");
    private static readonly Action<ILogger, int, Exception?> _logFailedToUpdateAgent =
        LoggerMessage.Define<int>(LogLevel.Error, 7, "Failed to update agent {AgentId} in Supabase");
    private static readonly Action<ILogger, int, Exception?> _logFailedToDeleteAgent =
        LoggerMessage.Define<int>(LogLevel.Error, 8, "Failed to delete agent {AgentId} from Supabase");
    private static readonly Action<ILogger, string, Exception?> _logFailedToDeleteAgentByName =
        LoggerMessage.Define<string>(LogLevel.Error, 9, "Failed to delete agent {AgentName} from Supabase");
    private static readonly Action<ILogger, string, Exception?> _logSupabaseTableNotFound =
        LoggerMessage.Define<string>(LogLevel.Warning, 10, "Supabase API reachable, but 'agents' table not found in PostgREST schema cache. {Message}");
    private static readonly Action<ILogger, Exception?> _logSupabaseConnectionTestFailed =
        LoggerMessage.Define(LogLevel.Error, 11, "Supabase connection test failed");

    public SupabaseAgentService(IOptions<SupabaseSettings> settings, ILogger<SupabaseAgentService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Fallback: if config is empty, read directly from .env environment variables
        if (string.IsNullOrEmpty(_settings.Url))
        {
            _settings.Url = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
        }
        if (string.IsNullOrEmpty(_settings.AnonKey))
        {
            _settings.AnonKey = Environment.GetEnvironmentVariable("SUPABASE_PUBLISHABLE_KEY")
                ?? Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY")
                ?? "";
        }
        if (string.IsNullOrEmpty(_settings.ServiceRoleKey))
        {
            _settings.ServiceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SECRET_KEY")
                ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
                ?? "";
        }

        // Log what we found (for debugging)
        _logSupabaseConfig(_logger, _settings.Url, _settings.AnonKey.Length, !string.IsNullOrEmpty(_settings.Url), null);

        // Initialize Supabase client with API key in headers
        var clientOptions = new ClientOptions
        {
            Schema = "public",
            Headers = new Dictionary<string, string>
            {
                ["apikey"] = _settings.AnonKey,
                ["Authorization"] = $"Bearer {_settings.AnonKey}"
            }
        };
        _client = new Client(_settings.Url, clientOptions);
    }

    public async Task<List<SupabaseAgent>> GetAllAgentsAsync()
    {
        try
        {
            var response = await _client.Table<SupabaseAgent>()
                .Order("agent_name", Ordering.Ascending, NullPosition.First)
                .Get(CancellationToken.None);

            return response.Models ?? new List<SupabaseAgent>();
        }
        catch (Exception ex)
        {
            _logFailedToGetAllAgents(_logger, ex);
            throw;
        }
    }

    public async Task<SupabaseAgent?> GetAgentByIdAsync(int id)
    {
        try
        {
            var response = await _client.Table<SupabaseAgent>()
                .Filter("id", Operator.Equals, id.ToString(CultureInfo.InvariantCulture))
                .Single(CancellationToken.None);

            return response;
        }
        catch (PostgrestException ex) when (ex.Message.Contains("no rows", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        catch (Exception ex)
        {
            _logFailedToGetAgentById(_logger, id, ex);
            throw;
        }
    }

    public async Task<SupabaseAgent?> GetAgentByNameAsync(string name)
    {
        try
        {
            var response = await _client.Table<SupabaseAgent>()
                .Filter("agent_name", Operator.Equals, name)
                .Single(CancellationToken.None);

            return response;
        }
        catch (PostgrestException ex) when (ex.Message.Contains("no rows", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        catch (Exception ex)
        {
            _logFailedToGetAgentByName(_logger, name, ex);
            throw;
        }
    }

    public async Task<List<SupabaseAgent>> SearchAgentsAsync(string searchTerm)
    {
        try
        {
            var response = await _client.Table<SupabaseAgent>()
                .Filter("agent_name", Operator.ILike, $"%{searchTerm}%")
                .Order("agent_name", Ordering.Ascending, NullPosition.First)
                .Get(CancellationToken.None);

            return response.Models ?? new List<SupabaseAgent>();
        }
        catch (Exception ex)
        {
            _logFailedToSearchAgents(_logger, ex);
            throw;
        }
    }

    public async Task<int> CreateAgentAsync(SupabaseAgent agent)
    {
        try
        {
            var response = await _client.Table<SupabaseAgent>()
                .Insert(agent, new QueryOptions(), CancellationToken.None);

            return response.Model?.Id ?? 0;
        }
        catch (Exception ex)
        {
            _logFailedToCreateAgent(_logger, ex);
            throw;
        }
    }

    public async Task<int> UpdateAgentAsync(SupabaseAgent agent)
    {
        try
        {
            await _client.Table<SupabaseAgent>()
                .Filter("id", Operator.Equals, agent.Id.ToString(CultureInfo.InvariantCulture))
                .Update(new QueryOptions(), CancellationToken.None);

            return 1;
        }
        catch (Exception ex)
        {
            _logFailedToUpdateAgent(_logger, agent.Id, ex);
            throw;
        }
    }

    public async Task<int> DeleteAgentAsync(int id)
    {
        try
        {
            await _client.Table<SupabaseAgent>()
                .Filter("id", Operator.Equals, id.ToString(CultureInfo.InvariantCulture))
                .Delete(new QueryOptions(), CancellationToken.None);

            return 1;
        }
        catch (Exception ex)
        {
            _logFailedToDeleteAgent(_logger, id, ex);
            throw;
        }
    }

    public async Task<int> DeleteAgentByNameAsync(string name)
    {
        try
        {
            await _client.Table<SupabaseAgent>()
                .Filter("agent_name", Operator.Equals, name)
                .Delete(new QueryOptions(), CancellationToken.None);

            return 1;
        }
        catch (Exception ex)
        {
            _logFailedToDeleteAgentByName(_logger, name, ex);
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // Try to fetch a single row to verify connection
            var response = await _client.Table<SupabaseAgent>()
                .Select("id")
                .Limit(1, "")
                .Get(CancellationToken.None);

            return true;
        }
        catch (PostgrestException ex)
        {
            var msg = ex.Message;
            // "requested path is invalid" / PGRST205 means table exists but schema cache stale
            if (msg.Contains("requested path is invalid", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("schema cache", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("PGRST205", StringComparison.OrdinalIgnoreCase))
            {
                _logSupabaseTableNotFound(_logger,
                    "If the table exists, run: NOTIFY pgrst, 'reload schema'; in Supabase SQL Editor.", null);
                return true;
            }
            _logSupabaseConnectionTestFailed(_logger, ex);
            return false;
        }
        catch (Exception ex)
        {
            _logSupabaseConnectionTestFailed(_logger, ex);
            return false;
        }
    }
}
