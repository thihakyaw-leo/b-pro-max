using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

public class CommissionServiceCurrencyTests
{
    private readonly CommissionService _commissionService;

    public CommissionServiceCurrencyTests()
    {
        _commissionService = new CommissionService();
    }

    [Fact]
    public void ConvertCurrencySameCurrencyReturnsOriginalAmount()
    {
        // Arrange
        decimal amount = 1000;

        // Act
        var result = _commissionService.ConvertCurrency(amount, "THB", "THB", 1);

        // Assert
        Assert.Equal(1000m, result);
    }

    [Fact]
    public void ConvertCurrencyDifferentCurrencyMultipliesByRate()
    {
        // Arrange
        decimal amount = 1000;
        decimal exchangeRate = 35.5m; // THB to USD approx

        // Act
        var result = _commissionService.ConvertCurrency(amount, "THB", "USD", exchangeRate);

        // Assert
        Assert.Equal(35500m, result);
    }

    [Fact]
    public void CalculateCommissionInCurrencyAppliesExchangeRate()
    {
        // Arrange
        decimal amount = 10000;
        decimal rate = 10; // 10%
        decimal exchangeRate = 35.5m;

        // Act
        var result = _commissionService.CalculateCommissionInCurrency(amount, rate, exchangeRate, "USD");

        // Assert
        // Commission = 10000 * 10 / 100 = 1000
        // In USD = 1000 * 35.5 = 35500
        Assert.Equal(35500m, result);
    }

    [Fact]
    public void CalculateNetCommissionInCurrencyCalculatesCorrectly()
    {
        // Arrange
        decimal amount = 10000;
        decimal rate = 10;
        decimal rate1 = 5;
        decimal exchangeRate = 35.5m;

        // Act
        var result = _commissionService.CalculateNetCommissionInCurrency(amount, rate, rate1, exchangeRate, "USD");

        // Assert
        // Commission = 1000, Commission1 = 500, Net = 1500
        // In USD = 1500 * 35.5 = 53250
        Assert.Equal(53250m, result);
    }

    [Fact]
    public void CalculateTotalsWithCurrencyReturnsCorrectTotals()
    {
        // Arrange
        var amounts = new List<decimal> { 10000, 20000 };
        var rates = new List<decimal> { 10, 10 };
        var rates1 = new List<decimal> { 5, 5 };
        decimal exchangeRate = 35.5m;

        // Act
        var (totalCommission, totalCommission1, totalNet) =
            _commissionService.CalculateTotalsWithCurrency(amounts, rates, rates1, exchangeRate, "USD");

        // Assert
        // Commission: (1000 + 2000) * 35.5 = 106500
        // Commission1: (500 + 1000) * 35.5 = 53250
        // Net: 159750
        Assert.Equal(106500m, totalCommission);
        Assert.Equal(53250m, totalCommission1);
        Assert.Equal(159750m, totalNet);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(50, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(101, false)]
    public void IsValidRateValidatesRange(decimal rate, bool expected)
    {
        // Act
        var result = _commissionService.IsValidRate(rate);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatCommissionAddsCurrencySymbol()
    {
        // Arrange
        decimal amount = 1234.56m;

        // Act
        var result = _commissionService.FormatCommission(amount);

        // Assert
        Assert.Equal("฿1,234.56", result);
    }

    [Fact]
    public void FormatCommissionCustomCurrencySymbol()
    {
        // Arrange
        decimal amount = 100;

        // Act
        var result = _commissionService.FormatCommission(amount, "$");

        // Assert
        Assert.Equal("$100.00", result);
    }
}

