using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Manages the draw lifecycle: Open → Closed → Drawn → Settled or Cancelled.
/// </summary>
public interface IDrawService
{
    Task<List<DrawRecord>> GetDrawsAsync();
    Task<List<DrawRecord>> GetOpenDrawsAsync();
    Task<DrawRecord?> GetDrawByIdAsync(long id);
    Task<long> CreateDrawAsync(DateOnly drawDate, TimeOnly drawTime, int? createdBy);
    Task<int> CloseDrawAsync(long id);
    Task<int> SetResultAsync(long id, string drawNumber);
    Task<int> CancelDrawAsync(long id);
}
