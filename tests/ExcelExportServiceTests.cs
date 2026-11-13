using System.Globalization;
using BProMax.Models;
using BProMax.Services;
using Moq;
using OfficeOpenXml;
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
        var mockSalesService = new Mock<ISalesService>();
        var service = new ExcelExportService(mockAgentService.Object, mockSalesService.Object);

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
        var mockSalesService = new Mock<ISalesService>();
        mockSalesService.Setup(s => s.GetAllSalesAsync())
            .ReturnsAsync(new List<SaleRecord>());
        var mockAgentService = new Mock<IAgentService>();
        var service = new ExcelExportService(mockAgentService.Object, mockSalesService.Object);

        // Act
        var result = await service.ExportSalesToExcelAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExportSalesToExcelAsyncFiltersByDateRange()
    {
        // Arrange
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 31);
        var sales = new List<SaleRecord>
        {
            new SaleRecord { Id = 1, AgentName = "A", SaleDate = new DateTime(2026, 1, 15), Amount = 100m, Status = "Paid" },
            new SaleRecord { Id = 2, AgentName = "B", SaleDate = new DateTime(2026, 2, 1), Amount = 200m, Status = "Pending" }, // out of range
            new SaleRecord { Id = 3, AgentName = "C", SaleDate = new DateTime(2026, 1, 20), Amount = 300m, Status = "Cancelled" },
        };
        var mockSalesService = new Mock<ISalesService>();
        mockSalesService.Setup(s => s.GetAllSalesAsync()).ReturnsAsync(sales);
        var mockAgentService = new Mock<IAgentService>();
        var service = new ExcelExportService(mockAgentService.Object, mockSalesService.Object);

        // Act
        var result = await service.ExportSalesToExcelAsync(start, end);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        using var package = new ExcelPackage(new MemoryStream(result));
        var worksheet = package.Workbook.Worksheets["Sales"];
        Assert.NotNull(worksheet);

        var rowCount = 0;
        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var cell = worksheet.Cells[row, 1].Value;
            if (cell == null || string.IsNullOrWhiteSpace(cell.ToString()))
                break;
            rowCount++;
        }

        Assert.Equal(2, rowCount);
    }
}

