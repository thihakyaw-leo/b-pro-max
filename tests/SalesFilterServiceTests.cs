using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BProMax.Tests;

public class SalesFilterServiceTests
{
    private readonly SalesFilterService _salesFilterService;
    private readonly Mock<ISalesService> _mockSalesService;

    public SalesFilterServiceTests()
    {
        _mockSalesService = new Mock<ISalesService>();
        _salesFilterService = new SalesFilterService(_mockSalesService.Object);
    }

    [Fact]
    public void FilterByDateRangeReturnsSalesInRange()
    {
        // Arrange
        var sales = new List<SaleRecord>
        {
            new SaleRecord { Id = 1, SaleDate = new DateTime(2026, 1, 15), AgentName = "Alice", Amount = 100 },
            new SaleRecord { Id = 2, SaleDate = new DateTime(2026, 2, 10), AgentName = "Bob", Amount = 200 },
            new SaleRecord { Id = 3, SaleDate = new DateTime(2026, 3, 5), AgentName = "Charlie", Amount = 300 }
        };

        // Act
        var result = _salesFilterService.FilterByDateRange(sales, new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        // Assert
        Assert.Single(result);
        Assert.Equal("Bob", result[0].AgentName);
    }

    [Fact]
    public void FilterByAgentReturnsMatchingAgent()
    {
        // Arrange
        var sales = new List<SaleRecord>
        {
            new SaleRecord { Id = 1, AgentName = "John Doe", Amount = 100 },
            new SaleRecord { Id = 2, AgentName = "john smith", Amount = 200 },
            new SaleRecord { Id = 3, AgentName = "Jane Doe", Amount = 300 }
        };

        // Act
        var result = _salesFilterService.FilterByAgent(sales, "john");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Contains("john", s.AgentName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FilterByStatusReturnsMatchingStatus()
    {
        // Arrange
        var sales = new List<SaleRecord>
        {
            new SaleRecord { Id = 1, Status = "Paid", Amount = 100 },
            new SaleRecord { Id = 2, Status = "Pending", Amount = 200 },
            new SaleRecord { Id = 3, Status = "PAID", Amount = 300 }
        };

        // Act
        var result = _salesFilterService.FilterByStatus(sales, "paid");

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FilterByAmountRangeReturnsSalesInRange()
    {
        // Arrange
        var sales = new List<SaleRecord>
        {
            new SaleRecord { Id = 1, Amount = 50 },
            new SaleRecord { Id = 2, Amount = 150 },
            new SaleRecord { Id = 3, Amount = 300 }
        };

        // Act
        var result = _salesFilterService.FilterByAmountRange(sales, 100, 250);

        // Assert
        Assert.Single(result);
        Assert.Equal(150, result[0].Amount);
    }

    [Fact]
    public void SearchSalesReturnsMatchingResults()
    {
        // Arrange
        var sales = new List<SaleRecord>
        {
            new SaleRecord { Id = 1, AgentName = "Alice Smith", Status = "Paid" },
            new SaleRecord { Id = 2, AgentName = "Bob Jones", Status = "Pending" },
            new SaleRecord { Id = 3, AgentName = "Charlie Brown", Status = "Paid" }
        };

        // Act
        var result = _salesFilterService.SearchSales(sales, "paid");

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void CalculateSalesTotalsReturnsCorrectTotals()
    {
        // Arrange
        var sales = new List<SaleRecord>
        {
            new SaleRecord { Amount = 100, Commission = 10, Commission1 = 5 },
            new SaleRecord { Amount = 200, Commission = 20, Commission1 = 10 },
            new SaleRecord { Amount = 300, Commission = 30, Commission1 = 15 }
        };

        // Act
        var (totalAmount, totalCommission, totalCommission1) =
            _salesFilterService.CalculateSalesTotals(sales);

        // Assert
        Assert.Equal(600m, totalAmount);
        Assert.Equal(60m, totalCommission);
        Assert.Equal(30m, totalCommission1);
    }

    [Fact]
    public void SortByDateAscendingReturnsChronologicalOrder()
    {
        // Arrange
        var sales = new List<SaleRecord>
        {
            new SaleRecord { Id = 1, SaleDate = new DateTime(2026, 3, 1) },
            new SaleRecord { Id = 2, SaleDate = new DateTime(2026, 1, 1) },
            new SaleRecord { Id = 3, SaleDate = new DateTime(2026, 2, 1) }
        };

        // Act
        var result = _salesFilterService.SortByDate(sales, ascending: true);

        // Assert
        Assert.Equal(new DateTime(2026, 1, 1), result[0].SaleDate);
        Assert.Equal(new DateTime(2026, 3, 1), result[2].SaleDate);
    }

    [Fact]
    public void SortByAgentDescendingReturnsReverseAlphabetical()
    {
        // Arrange
        var sales = new List<SaleRecord>
        {
            new SaleRecord { AgentName = "Alice" },
            new SaleRecord { AgentName = "Charlie" },
            new SaleRecord { AgentName = "Bob" }
        };

        // Act
        var result = _salesFilterService.SortByAgent(sales, ascending: false);

        // Assert
        Assert.Equal("Charlie", result[0].AgentName);
        Assert.Equal("Alice", result[2].AgentName);
    }

    [Fact]
    public async Task GetAllSalesAsyncDelegatesToSalesService()
    {
        // Arrange
        var expected = new List<SaleRecord>
        {
            new SaleRecord { Id = 1, AgentName = "Alice", Amount = 100 }
        };
        _mockSalesService
            .Setup(x => x.GetAllSalesAsync())
            .ReturnsAsync(expected);

        // Act
        var result = await _salesFilterService.GetAllSalesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Alice", result[0].AgentName);
        _mockSalesService.Verify(x => x.GetAllSalesAsync(), Times.Once);
    }
}
