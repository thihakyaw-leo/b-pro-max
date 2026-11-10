using BProMax.Models;

namespace BProMax.Services;

public interface ISalesFilterService
{
    List<SaleRecord> FilterByDateRange(List<SaleRecord> sales, DateTime startDate, DateTime endDate);
    List<SaleRecord> FilterByAgent(List<SaleRecord> sales, string agentName);
    List<SaleRecord> FilterByStatus(List<SaleRecord> sales, string status);
    List<SaleRecord> FilterByAmountRange(List<SaleRecord> sales, decimal minAmount, decimal maxAmount);
    List<SaleRecord> SearchSales(List<SaleRecord> sales, string searchTerm);
    (decimal totalAmount, decimal totalCommission, decimal totalCommission1)
        CalculateSalesTotals(List<SaleRecord> sales);
    List<SaleRecord> SortByDate(List<SaleRecord> sales, bool ascending = true);
    List<SaleRecord> SortByAgent(List<SaleRecord> sales, bool ascending = true);
    List<SaleRecord> SortByAmount(List<SaleRecord> sales, bool ascending = true);
    Task<List<SaleRecord>> GetAllSalesAsync();
}
