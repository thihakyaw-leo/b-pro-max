using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Builds the 2D sales and payout reports.
/// </summary>
public interface ITwoDReportService
{
    Task<List<DrawSalesReportRow>> GetSalesByDrawAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<List<NumberSalesReportRow>> GetSalesByNumberAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        long? drawId = null,
        CancellationToken cancellationToken = default);

    Task<List<GroupSalesReportRow>> GetGroupPerformanceAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<List<PayoutRegisterRow>> GetPayoutRegisterAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? status = null,
        CancellationToken cancellationToken = default);
}
