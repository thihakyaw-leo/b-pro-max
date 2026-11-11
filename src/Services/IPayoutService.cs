using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Settles a drawn result into payouts and records when those payouts are paid.
/// </summary>
public interface IPayoutService
{
    Task<PayoutSettlementResult> SettleDrawAsync(
        long drawId,
        int? settledBy = null,
        CancellationToken cancellationToken = default);

    Task<int> MarkPayoutPaidAsync(
        long payoutId,
        int paidBy,
        CancellationToken cancellationToken = default);

    Task<List<PayoutRecord>> GetPayoutsByDrawAsync(
        long drawId,
        CancellationToken cancellationToken = default);

    Task<List<PayoutDetailRecord>> GetPayoutDetailsByDrawAsync(
        long drawId,
        CancellationToken cancellationToken = default);
}
