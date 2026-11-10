namespace BProMax.Services;

public class CommissionService : ICommissionService
{
    public decimal CalculateCommission(decimal amount, decimal rate)
    {
        return Math.Round(amount * rate / 100, 2);
    }

    public decimal CalculateCommission1(decimal amount, decimal rate1)
    {
        return Math.Round(amount * rate1 / 100, 2);
    }

    public decimal CalculateNetCommission(decimal commission, decimal commission1)
    {
        return Math.Round(commission + commission1, 2);
    }

    public (decimal totalCommission, decimal totalCommission1) CalculateTotals(
        List<decimal> amounts, List<decimal> rates, List<decimal> rates1)
    {
        decimal totalCommission = 0;
        decimal totalCommission1 = 0;

        for (int i = 0; i < amounts.Count; i++)
        {
            totalCommission += CalculateCommission(amounts[i], rates[i]);
            totalCommission1 += CalculateCommission1(amounts[i], rates1[i]);
        }

        return (Math.Round(totalCommission, 2), Math.Round(totalCommission1, 2));
    }

    /// <summary>
    /// Convert an amount from one currency to another using the exchange rate.
    /// Matches DD.exe currency_type / currency_rate fields.
    /// </summary>
    public decimal ConvertCurrency(decimal amount, string fromCurrency, string toCurrency, decimal exchangeRate)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return Math.Round(amount, 2);

        // Multiply by exchange rate to convert to target currency
        return Math.Round(amount * exchangeRate, 2);
    }

    /// <summary>
    /// Calculate commission in a target currency, applying exchange rate.
    /// </summary>
    public decimal CalculateCommissionInCurrency(decimal amount, decimal rate, decimal exchangeRate, string targetCurrency)
    {
        var commission = CalculateCommission(amount, rate);
        return ConvertCurrency(commission, "THB", targetCurrency, exchangeRate);
    }

    /// <summary>
    /// Calculate commission1 in a target currency, applying exchange rate.
    /// </summary>
    public decimal CalculateCommission1InCurrency(decimal amount, decimal rate1, decimal exchangeRate, string targetCurrency)
    {
        var commission1 = CalculateCommission1(amount, rate1);
        return ConvertCurrency(commission1, "THB", targetCurrency, exchangeRate);
    }

    /// <summary>
    /// Calculate net commission in a target currency.
    /// </summary>
    public decimal CalculateNetCommissionInCurrency(
        decimal amount, decimal rate, decimal rate1, decimal exchangeRate, string targetCurrency)
    {
        var commission = CalculateCommissionInCurrency(amount, rate, exchangeRate, targetCurrency);
        var commission1 = CalculateCommission1InCurrency(amount, rate1, exchangeRate, targetCurrency);
        return CalculateNetCommission(commission, commission1);
    }

    /// <summary>
    /// Calculate totals for multiple agents with currency conversion.
    /// </summary>
    public (decimal totalCommission, decimal totalCommission1, decimal totalNetCommission)
        CalculateTotalsWithCurrency(
            List<decimal> amounts, List<decimal> rates, List<decimal> rates1,
            decimal exchangeRate, string targetCurrency)
    {
        decimal totalCommission = 0;
        decimal totalCommission1 = 0;

        for (int i = 0; i < amounts.Count; i++)
        {
            totalCommission += CalculateCommissionInCurrency(amounts[i], rates[i], exchangeRate, targetCurrency);
            totalCommission1 += CalculateCommission1InCurrency(amounts[i], rates1[i], exchangeRate, targetCurrency);
        }

        var totalNet = CalculateNetCommission(totalCommission, totalCommission1);
        return (Math.Round(totalCommission, 2), Math.Round(totalCommission1, 2), Math.Round(totalNet, 2));
    }

    /// <summary>
    /// Validate that commission rate is within acceptable range (0-100%).
    /// </summary>
    public bool IsValidRate(decimal rate)
    {
        return rate >= 0 && rate <= 100;
    }

    /// <summary>
    /// Format commission amount for display with currency symbol.
    /// </summary>
    public string FormatCommission(decimal amount, string currencySymbol = "฿")
    {
        return $"{currencySymbol}{amount:N2}";
    }
}
