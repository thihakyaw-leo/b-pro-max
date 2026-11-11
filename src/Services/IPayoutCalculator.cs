using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Calculates the payout of one sale item for a given draw result.
/// </summary>
public interface IPayoutCalculator
{
    /// <summary>
    /// Calculates a payout. Returns false and a user-facing message when the request is invalid.
    /// </summary>
    bool TryCalculate(
        PayoutCalculationRequest request,
        out PayoutCalculationResult result,
        out string? validationError);
}
