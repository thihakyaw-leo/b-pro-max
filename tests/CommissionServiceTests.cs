using BProMax.Services;
using BProMax.Models;
using Xunit;

namespace BProMax.Tests;

public class CommissionServiceTests
{
    private readonly CommissionService _commissionService;

    public CommissionServiceTests()
    {
        _commissionService = new CommissionService();
    }

    [Fact]
    public void CalculateCommissionWithPositiveAmountAndRateReturnsCorrectValue()
    {
        // Arrange
        decimal amount = 1000;
        decimal rate = 10;

        // Act
        var result = _commissionService.CalculateCommission(amount, rate);

        // Assert
        Assert.Equal(100m, result);
    }

    [Fact]
    public void CalculateCommissionWithZeroAmountReturnsZero()
    {
        // Arrange
        decimal amount = 0;
        decimal rate = 10;

        // Act
        var result = _commissionService.CalculateCommission(amount, rate);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateCommissionWithZeroRateReturnsZero()
    {
        // Arrange
        decimal amount = 1000;
        decimal rate = 0;

        // Act
        var result = _commissionService.CalculateCommission(amount, rate);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateCommission1WithPositiveAmountAndRateReturnsCorrectValue()
    {
        // Arrange
        decimal amount = 500;
        decimal rate1 = 5;

        // Act
        var result = _commissionService.CalculateCommission1(amount, rate1);

        // Assert
        Assert.Equal(25m, result);
    }

    [Fact]
    public void CalculateNetCommissionAddsCommissionAndCommission1()
    {
        // Arrange
        decimal commission = 100;
        decimal commission1 = 25;

        // Act
        var result = _commissionService.CalculateNetCommission(commission, commission1);

        // Assert
        Assert.Equal(125m, result);
    }

    [Fact]
    public void CalculateTotalsWithMultipleAgentsReturnsSummedValues()
    {
        // Arrange
        var amounts = new List<decimal> { 1000, 2000, 3000 };
        var rates = new List<decimal> { 10, 10, 10 };
        var rates1 = new List<decimal> { 5, 5, 5 };

        // Act
        var (totalCommission, totalCommission1) = _commissionService.CalculateTotals(amounts, rates, rates1);

        // Assert
        Assert.Equal(600m, totalCommission);
        Assert.Equal(300m, totalCommission1);
    }

    [Fact]
    public void CalculateTotalsWithEmptyListsReturnsZero()
    {
        // Arrange
        var amounts = new List<decimal>();
        var rates = new List<decimal>();
        var rates1 = new List<decimal>();

        // Act
        var (totalCommission, totalCommission1) = _commissionService.CalculateTotals(amounts, rates, rates1);

        // Assert
        Assert.Equal(0m, totalCommission);
        Assert.Equal(0m, totalCommission1);
    }
}

