using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Reads the dealer-configurable bet types and fixed number groups.
/// </summary>
public interface IBetTypeService
{
    Task<List<BetTypeRecord>> GetBetTypesAsync(bool activeOnly = true);
    Task<BetTypeRecord?> GetBetTypeByCodeAsync(string code);
    Task<List<NumberGroupRecord>> GetNumberGroupsAsync(bool activeOnly = true);
    Task<NumberGroupRecord?> GetNumberGroupByCodeAsync(string code);

    /// <summary>
    /// Updates the dealer-configurable fields of a bet type. Labels and codes remain locked.
    /// </summary>
    Task<int> UpdateBetTypeAsync(
        int id,
        decimal payoutMultiplier,
        bool isActive,
        int sortOrder);

    /// <summary>
    /// Updates the dealer-configurable fields of a fixed number group and replaces its
    /// members in one transaction. Labels and codes remain locked.
    /// </summary>
    Task<int> UpdateNumberGroupAsync(
        int id,
        bool allowReverse,
        bool isActive,
        int sortOrder,
        IReadOnlyList<string> members);
}
