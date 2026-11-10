using BProMax.Configuration;
using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Supabase.Postgrest;
using Xunit;

namespace BProMax.Tests;

public class SupabaseAgentServiceTests
{
    private readonly Mock<IOptions<SupabaseSettings>> _mockOptions;
    private readonly Mock<ILogger<SupabaseAgentService>> _mockLogger;
    private readonly Mock<Client> _mockClient;

    public SupabaseAgentServiceTests()
    {
        _mockOptions = new Mock<IOptions<SupabaseSettings>>();
        _mockLogger = new Mock<ILogger<SupabaseAgentService>>();

        _mockOptions.Setup(x => x.Value).Returns(new SupabaseSettings
        {
            Url = "https://ewwvsuqbfkjbmzueuqbb.supabase.co",
            AnonKey = "sb_publishable_test_key",
            ServiceRoleKey = "sb_secret_test_key"
        });

        _mockClient = new Mock<Client>(
            "https://ewwvsuqbfkjbmzueuqbb.supabase.co",
            new ClientOptions()
        );
    }

    [Fact]
    public void SupabaseSettingsHasCorrectDefaults()
    {
        // Arrange
        var settings = new SupabaseSettings();

        // Act & Assert
        Assert.Equal(string.Empty, settings.Url);
        Assert.Equal(string.Empty, settings.AnonKey);
        Assert.Equal(string.Empty, settings.ServiceRoleKey);
    }

    [Fact]
    public void SupabaseSettingsCanSetValues()
    {
        // Arrange
        var settings = new SupabaseSettings
        {
            Url = "https://test.supabase.co",
            AnonKey = "test-anon-key",
            ServiceRoleKey = "test-service-key"
        };

        // Act & Assert
        Assert.Equal("https://test.supabase.co", settings.Url);
        Assert.Equal("test-anon-key", settings.AnonKey);
        Assert.Equal("test-service-key", settings.ServiceRoleKey);
    }

    [Fact]
    public void SupabaseAgentModelHasCorrectProperties()
    {
        // Arrange & Act
        var agent = new SupabaseAgent
        {
            Id = 1,
            AgentName = "Test Agent",
            Address = "123 Main St",
            Phone = "555-0100",
            Commission = 100m,
            Rate = 10m,
            Commission1 = 50m,
            Rate1 = 5m,
            CurrencyType = "THB",
            CurrencyRate = 1m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, agent.Id);
        Assert.Equal("Test Agent", agent.AgentName);
        Assert.Equal("123 Main St", agent.Address);
        Assert.Equal("555-0100", agent.Phone);
        Assert.Equal(100m, agent.Commission);
        Assert.Equal(10m, agent.Rate);
        Assert.Equal(50m, agent.Commission1);
        Assert.Equal(5m, agent.Rate1);
        Assert.Equal("THB", agent.CurrencyType);
        Assert.Equal(1m, agent.CurrencyRate);
    }

    [Fact]
    public void SupabaseAgentModelDefaultValues()
    {
        // Arrange & Act
        var agent = new SupabaseAgent();

        // Assert
        Assert.Equal(string.Empty, agent.AgentName);
        Assert.Null(agent.Address);
        Assert.Null(agent.Phone);
        Assert.Equal(0m, agent.Commission);
        Assert.Equal(0m, agent.Rate);
        Assert.Equal(0m, agent.Commission1);
        Assert.Equal(0m, agent.Rate1);
        Assert.Null(agent.CurrencyType);
        Assert.Equal(1m, agent.CurrencyRate);
    }
}

