using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BProMax.Tests;

public class ReportServiceTests
{
    [Fact]
    public async Task GenerateAgentReportAsyncReturnsByteArray()
    {
        // Arrange
        var mockAgentService = new Mock<IAgentService>();
        mockAgentService.Setup(s => s.GetAllAgentsAsync())
            .ReturnsAsync(new List<Agent>
            {
                new Agent { Id = 1, AgentName = "Test Agent", Commission = 100m, Rate = 10m }
            });
        var mockSalesFilterService = new Mock<ISalesFilterService>();
        var mockCommissionService = new Mock<ICommissionService>();
        var mockLogger = new Mock<ILogger<ReportService>>();
        var service = new ReportService(mockAgentService.Object, mockSalesFilterService.Object, mockCommissionService.Object, mockLogger.Object);

        // Act
        var result = await service.GenerateAgentReportAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateSalesReportAsyncReturnsByteArray()
    {
        // Arrange
        var mockAgentService = new Mock<IAgentService>();
        var mockSalesFilterService = new Mock<ISalesFilterService>();
        mockSalesFilterService.Setup(s => s.GetAllSalesAsync())
            .ReturnsAsync(new List<SaleRecord>());
        mockSalesFilterService.Setup(s => s.FilterByDateRange(It.IsAny<List<SaleRecord>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new List<SaleRecord>());
        var mockCommissionService = new Mock<ICommissionService>();
        var mockLogger = new Mock<ILogger<ReportService>>();
        var service = new ReportService(mockAgentService.Object, mockSalesFilterService.Object, mockCommissionService.Object, mockLogger.Object);

        // Act
        var result = await service.GenerateSalesReportAsync(DateTime.Now.AddDays(-7), DateTime.Now);

        // Assert
        Assert.NotNull(result);
    }
}

