using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Creates a sale, its items, and its resolved numbers in one database transaction.
/// </summary>
public interface ISaleEntryService
{
    Task<SaleEntryResult> CreateSaleAsync(SaleEntryRequest request, CancellationToken cancellationToken = default);
}
