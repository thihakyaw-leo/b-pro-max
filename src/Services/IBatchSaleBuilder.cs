using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Builds a validated preview for a batch of direct or R numbers.
/// </summary>
public interface IBatchSaleBuilder
{
    /// <summary>
    /// Builds a preview without touching the database.
    /// </summary>
    /// <param name="request">The batch entry to validate.</param>
    /// <param name="preview">The resolved lines when validation succeeds.</param>
    /// <param name="validationError">A user-facing message when validation fails.</param>
    bool TryBuild(BatchSaleRequest request, out BatchSalePreview preview, out string? validationError);
}
