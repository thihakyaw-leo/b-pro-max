namespace BProMax.Services;

public interface ICommissionService
{
    decimal CalculateCommission(decimal amount, decimal rate);
    decimal CalculateCommission1(decimal amount, decimal rate1);
    decimal CalculateNetCommission(decimal commission, decimal commission1);
    (decimal totalCommission, decimal totalCommission1) CalculateTotals(
        List<decimal> amounts, List<decimal> rates, List<decimal> rates1);

    // Currency conversion
    decimal ConvertCurrency(decimal amount, string fromCurrency, string toCurrency, decimal exchangeRate);
    decimal CalculateCommissionInCurrency(decimal amount, decimal rate, decimal exchangeRate, string targetCurrency);
    decimal CalculateCommission1InCurrency(decimal amount, decimal rate1, decimal exchangeRate, string targetCurrency);
    decimal CalculateNetCommissionInCurrency(
        decimal amount, decimal rate, decimal rate1, decimal exchangeRate, string targetCurrency);
    (decimal totalCommission, decimal totalCommission1, decimal totalNetCommission)
        CalculateTotalsWithCurrency(
            List<decimal> amounts, List<decimal> rates, List<decimal> rates1,
            decimal exchangeRate, string targetCurrency);

    // Validation and formatting
    bool IsValidRate(decimal rate);
    string FormatCommission(decimal amount, string currencySymbol = "฿");
}
