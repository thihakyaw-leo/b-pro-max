using BProMax.Models;
using BProMax.Services;
using Moq;
using Xunit;

namespace BProMax.Tests;

public class ExcelExportServiceTests
{
    [Fact]
    public async Task ExportAgentsToExcelAsyncReturnsByteArray()
    {
        // Arrange
        var mockAgentService = new Mock<IAgentService>();
        mockAgentService.Setup(s => s.GetAllAgentsAsync())
            .ReturnsAsync(new List<Agent>
            {
                new Agent { Id = 1, AgentName = "Test Agent", Commission = 100m, Rate = 10m }
            });
        var service = new ExcelExportService(mockAgentService.Object);

        // Act
        var result = await service.ExportAgentsToExcelAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task ExportSalesToExcelAsyncReturnsByteArray()
    {
        // Arrange
        var mockAgentService = new Mock<IAgentService>();
        mockAgentService.Setup(s => s.GetAllAgentsAsync())
            .ReturnsAsync(new List<Agent>());
        var service = new ExcelExportService(mockAgentService.Object);

        // Act
        var result = await service.ExportSalesToExcelAsync();

        // Assert
        Assert.NotNull(result);
    }
}

