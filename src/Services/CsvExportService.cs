using System.Globalization;
using BProMax.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace BProMax.Services;

/// <summary>
/// CSV export service using CsvHelper.
/// Provides CSV export functionality for agents and sales data.
/// </summary>
public class CsvExportService : ICsvExportService
{
    private readonly IAgentService _agentService;
    private readonly ISalesFilterService _salesFilterService;

    public CsvExportService(IAgentService agentService, ISalesFilterService salesFilterService)
    {
        _agentService = agentService;
        _salesFilterService = salesFilterService;
    }

    public async Task<byte[]> ExportAgentsToCsvAsync()
    {
        var agents = await _agentService.GetAllAgentsAsync();
        return GenerateAgentsCsv(agents);
    }

    public async Task<byte[]> ExportSalesToCsvAsync()
    {
        var sales = await _salesFilterService.GetAllSalesAsync();
        return GenerateSalesCsv(sales);
    }

    private static byte[] GenerateAgentsCsv(List<Agent> agents)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture));

        csv.WriteField("Id");
        csv.WriteField("AgentName");
        csv.WriteField("Address");
        csv.WriteField("Phone");
        csv.WriteField("Commission");
        csv.WriteField("Rate");
        csv.WriteField("Commission1");
        csv.WriteField("Rate1");
        csv.WriteField("CurrencyType");
        csv.WriteField("CurrencyRate");
        csv.WriteField("CreatedAt");
        csv.WriteField("UpdatedAt");
        csv.NextRecord();

        foreach (var agent in agents)
        {
            csv.WriteField(agent.Id);
            csv.WriteField(agent.AgentName);
            csv.WriteField(agent.Address ?? string.Empty);
            csv.WriteField(agent.Phone ?? string.Empty);
            csv.WriteField(agent.Commission);
            csv.WriteField(agent.Rate);
            csv.WriteField(agent.Commission1);
            csv.WriteField(agent.Rate1);
            csv.WriteField(agent.CurrencyType ?? string.Empty);
            csv.WriteField(agent.CurrencyRate);
            csv.WriteField(agent.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            csv.WriteField(agent.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            csv.NextRecord();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }

    private static byte[] GenerateSalesCsv(List<SaleRecord> sales)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture));

        csv.WriteField("Id");
        csv.WriteField("AgentName");
        csv.WriteField("SaleDate");
        csv.WriteField("Amount");
        csv.WriteField("Commission");
        csv.WriteField("Commission1");
        csv.WriteField("CurrencyType");
        csv.WriteField("CurrencyRate");
        csv.WriteField("Status");
        csv.NextRecord();

        foreach (var sale in sales)
        {
            csv.WriteField(sale.Id);
            csv.WriteField(sale.AgentName);
            csv.WriteField(sale.SaleDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            csv.WriteField(sale.Amount);
            csv.WriteField(sale.Commission);
            csv.WriteField(sale.Commission1);
            csv.WriteField(sale.CurrencyType ?? string.Empty);
            csv.WriteField(sale.CurrencyRate);
            csv.WriteField(sale.Status);
            csv.NextRecord();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }
}
