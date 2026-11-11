using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Calculates the total stake and per-number stakes for one sale item.
/// </summary>
public interface ISaleItemAmountCalculator
{
    bool TryCalculate(
        decimal amount,
        string amountMode,
        int numberCount,
        decimal payoutMultiplier,
        PayoutMode payoutMode,
        out SaleItemAmountResult result,
        out string? validationError);
}
