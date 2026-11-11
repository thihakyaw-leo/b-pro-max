using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Resolves a bet type and its selection into the exact two-digit numbers that are sold.
/// </summary>
public interface INumberResolutionService
{
    /// <summary>
    /// Resolves a request. Returns false and a user-facing message when the selection or
    /// rule configuration is invalid.
    /// </summary>
    bool TryResolve(
        NumberResolutionRequest request,
        out NumberResolutionResult result,
        out string? validationError);
}
