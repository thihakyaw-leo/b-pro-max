using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Builds a display row for the Sale Panel without touching the database.
/// </summary>
public interface ISalePanelLineFactory
{
    bool TryCreate(
        SaleEntryItemRequest request,
        BetTypeRecord betType,
        NumberGroupRecord? group,
        out SalePanelLine line,
        out string? validationError);
}
