using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Xunit;

namespace BProMax.Tests;

public class AgentServiceTests
{
    private readonly Mock<IDatabaseService> _mockDb;
    private readonly AgentService _agentService;

    public AgentServiceTests()
    {
        _mockDb = new Mock<IDatabaseService>();
        _agentService = new AgentService(_mockDb.Object, new TestLogger<AgentService>());
    }

    [Fact]
    public async Task GetAllAgentsAsyncReturnsListOfAgents()
    {
        // Arrange
        var expected = new List<Agent>
        {
            new Agent { Id = 1, AgentName = "John Doe", Phone = "123-456-7890", Commission = 100, Rate = 10 },
            new Agent { Id = 2, AgentName = "Jane Smith", Phone = "098-765-4321", Commission = 200, Rate = 15 }
        };
        _mockDb.Setup(x => x.QueryListAsync(It.IsAny<string>(), It.IsAny<Func<NpgsqlDataReader, Agent>>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _agentService.GetAllAgentsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("John Doe", result[0].AgentName);
        Assert.Equal("Jane Smith", result[1].AgentName);
    }

    [Fact]
    public async Task GetAgentByNameAsyncFoundReturnsAgent()
    {
        // Arrange
        var expected = new Agent { Id = 1, AgentName = "John Doe", Phone = "123-456-7890" };
        _mockDb.Setup(x => x.QuerySingleAsync(It.Is<string>(s => s.Contains("agent_name = @name")), It.IsAny<Func<NpgsqlDataReader, Agent>>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _agentService.GetAgentByNameAsync("John Doe");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.AgentName);
    }

    [Fact]
    public async Task GetAgentByNameAsyncNotFoundReturnsNull()
    {
        // Arrange
        _mockDb.Setup(x => x.QuerySingleAsync(It.IsAny<string>(), It.IsAny<Func<NpgsqlDataReader, Agent>>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync((Agent?)null);

        // Act
        var result = await _agentService.GetAgentByNameAsync("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchAgentsAsyncReturnsMatchingAgents()
    {
        // Arrange
        var expected = new List<Agent>
        {
            new Agent { Id = 1, AgentName = "John Doe" },
            new Agent { Id = 2, AgentName = "Johnny Appleseed" }
        };
        _mockDb.Setup(x => x.QueryListAsync(It.Is<string>(s => s.Contains("ILIKE")), It.IsAny<Func<NpgsqlDataReader, Agent>>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _agentService.SearchAgentsAsync("John");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Contains("John", a.AgentName));
    }

    [Fact]
    public async Task CreateAgentAsyncReturnsNewId()
    {
        // Arrange
        var agent = new Agent { AgentName = "New Agent", Commission = 50, Rate = 5 };
        _mockDb.Setup(x => x.QuerySingleAsync(It.Is<string>(s => s.Contains("RETURNING id")), It.IsAny<Func<NpgsqlDataReader, int>>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(42);

        // Act
        var result = await _agentService.CreateAgentAsync(agent);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task UpdateAgentAsyncReturnsAffectedRows()
    {
        // Arrange
        var agent = new Agent { Id = 1, AgentName = "Updated", Commission = 75 };
        _mockDb.Setup(x => x.ExecuteNonQueryAsync(It.Is<string>(s => s.Contains("UPDATE agents")), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        // Act
        var result = await _agentService.UpdateAgentAsync(agent);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task DeleteAgentAsyncReturnsAffectedRows()
    {
        // Arrange
        _mockDb.Setup(x => x.ExecuteNonQueryAsync(It.Is<string>(s => s.Contains("DELETE FROM agents WHERE id = @id")), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        // Act
        var result = await _agentService.DeleteAgentAsync(1);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task DeleteAgentByNameAsyncReturnsAffectedRows()
    {
        // Arrange
        _mockDb.Setup(x => x.ExecuteNonQueryAsync(It.Is<string>(s => s.Contains("DELETE FROM agents WHERE agent_name = @name")), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        // Act
        var result = await _agentService.DeleteAgentByNameAsync("John Doe");

        // Assert
        Assert.Equal(1, result);
    }
}

public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

