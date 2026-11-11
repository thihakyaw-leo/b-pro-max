using BProMax.Models;

namespace BProMax.Services;

public class SalesFilterService : ISalesFilterService
{
    private readonly ISalesService _salesService;

    public SalesFilterService(ISalesService salesService)
    {
        _salesService = salesService;
    }

    public List<SaleRecord> FilterByDateRange(List<SaleRecord> sales, DateTime startDate, DateTime endDate)
    {
        return sales.Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate).ToList();
    }

    public List<SaleRecord> FilterByAgent(List<SaleRecord> sales, string agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
            return sales;

        return sales.Where(s =>
            s.AgentName.Contains(agentName, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public List<SaleRecord> FilterByStatus(List<SaleRecord> sales, string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return sales;

        return sales.Where(s =>
            s.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public List<SaleRecord> FilterByAmountRange(List<SaleRecord> sales, decimal minAmount, decimal maxAmount)
    {
        return sales.Where(s => s.Amount >= minAmount && s.Amount <= maxAmount).ToList();
    }

    public List<SaleRecord> SearchSales(List<SaleRecord> sales, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return sales;

        return sales.Where(s =>
            s.AgentName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            s.Status.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public (decimal totalAmount, decimal totalCommission, decimal totalCommission1)
        CalculateSalesTotals(List<SaleRecord> sales)
    {
        var totalAmount = Math.Round(sales.Sum(s => s.Amount), 2);
        var totalCommission = Math.Round(sales.Sum(s => s.Commission), 2);
        var totalCommission1 = Math.Round(sales.Sum(s => s.Commission1), 2);
        return (totalAmount, totalCommission, totalCommission1);
    }

    public List<SaleRecord> SortByDate(List<SaleRecord> sales, bool ascending = true)
    {
        return ascending
            ? sales.OrderBy(s => s.SaleDate).ToList()
            : sales.OrderByDescending(s => s.SaleDate).ToList();
    }

    public List<SaleRecord> SortByAgent(List<SaleRecord> sales, bool ascending = true)
    {
        return ascending
            ? sales.OrderBy(s => s.AgentName).ToList()
            : sales.OrderByDescending(s => s.AgentName).ToList();
    }

    public List<SaleRecord> SortByAmount(List<SaleRecord> sales, bool ascending = true)
    {
        return ascending
            ? sales.OrderBy(s => s.Amount).ToList()
            : sales.OrderByDescending(s => s.Amount).ToList();
    }

    public async Task<List<SaleRecord>> GetAllSalesAsync()
    {
        return await _salesService.GetAllSalesAsync();
    }
}
