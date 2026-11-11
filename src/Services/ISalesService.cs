using BProMax.Models;

namespace BProMax.Services;

public interface ISalesService
{
    Task<List<SaleRecord>> GetAllSalesAsync();
    Task<SaleRecord?> GetSaleByIdAsync(long id);
    Task<SaleRecord?> GetSaleByAgentAsync(int agentId);
    Task<long> CreateSaleAsync(SaleRecord sale);
    Task<int> UpdateSaleAsync(SaleRecord sale);
    Task<int> DeleteSaleAsync(long id);
}
