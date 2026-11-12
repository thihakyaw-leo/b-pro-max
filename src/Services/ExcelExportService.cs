using System.Globalization;
using BProMax.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BProMax.Services;

/// <summary>
/// Excel export service using EPPlus.
/// Replaces Crystal Reports Excel export from DD.exe.
/// </summary>
public class ExcelExportService : IExcelExportService
{
    private readonly IAgentService _agentService;

    public ExcelExportService(IAgentService agentService)
    {
        _agentService = agentService;
    }

    public async Task<byte[]> ExportAgentsToExcelAsync()
    {
        var agents = await _agentService.GetAllAgentsAsync();
        return GenerateAgentsExcel(agents);
    }

    public Task<byte[]> ExportSalesToExcelAsync()
    {
        // TODO: Implement when sales data source exists
        var sales = new List<SaleRecord>();
        return Task.FromResult(GenerateSalesExcel(sales));
    }

    private static byte[] GenerateAgentsExcel(List<Agent> agents)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Agents");

        // Header row
        worksheet.Cells[1, 1].Value = "ID";
        worksheet.Cells[1, 2].Value = "Agent Name";
        worksheet.Cells[1, 3].Value = "Address";
        worksheet.Cells[1, 4].Value = "Phone";
        worksheet.Cells[1, 5].Value = "Commission";
        worksheet.Cells[1, 6].Value = "Rate";
        worksheet.Cells[1, 7].Value = "Commission1";
        worksheet.Cells[1, 8].Value = "Rate1";
        worksheet.Cells[1, 9].Value = "Currency Type";
        worksheet.Cells[1, 10].Value = "Currency Rate";
        worksheet.Cells[1, 11].Value = "Created At";
        worksheet.Cells[1, 12].Value = "Updated At";

        // Style header
        using var headerRange = worksheet.Cells[1, 1, 1, 12];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 51, 102));
        headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);

        // Data rows
        int row = 2;
        foreach (var agent in agents)
        {
            worksheet.Cells[row, 1].Value = agent.Id;
            worksheet.Cells[row, 2].Value = agent.AgentName;
            worksheet.Cells[row, 3].Value = agent.Address ?? string.Empty;
            worksheet.Cells[row, 4].Value = agent.Phone ?? string.Empty;
            worksheet.Cells[row, 5].Value = agent.Commission;
            worksheet.Cells[row, 6].Value = agent.Rate;
            worksheet.Cells[row, 7].Value = agent.Commission1;
            worksheet.Cells[row, 8].Value = agent.Rate1;
            worksheet.Cells[row, 9].Value = agent.CurrencyType ?? string.Empty;
            worksheet.Cells[row, 10].Value = agent.CurrencyRate;
            worksheet.Cells[row, 11].Value = agent.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            worksheet.Cells[row, 12].Value = agent.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            row++;
        }

        // Auto-fit columns
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        return package.GetAsByteArray();
    }

    private static byte[] GenerateSalesExcel(List<SaleRecord> sales)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sales");

        // Header row
        worksheet.Cells[1, 1].Value = "ID";
        worksheet.Cells[1, 2].Value = "Agent Name";
        worksheet.Cells[1, 3].Value = "Sale Date";
        worksheet.Cells[1, 4].Value = "Amount";
        worksheet.Cells[1, 5].Value = "Commission";
        worksheet.Cells[1, 6].Value = "Commission1";
        worksheet.Cells[1, 7].Value = "Currency Type";
        worksheet.Cells[1, 8].Value = "Currency Rate";
        worksheet.Cells[1, 9].Value = "Status";

        // Style header
        using var headerRange = worksheet.Cells[1, 1, 1, 9];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 51, 102));
        headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);

        // Data rows
        int row = 2;
        foreach (var sale in sales)
        {
            worksheet.Cells[row, 1].Value = sale.Id;
            worksheet.Cells[row, 2].Value = sale.AgentName;
            worksheet.Cells[row, 3].Value = sale.SaleDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            worksheet.Cells[row, 4].Value = sale.Amount;
            worksheet.Cells[row, 5].Value = sale.Commission;
            worksheet.Cells[row, 6].Value = sale.Commission1;
            worksheet.Cells[row, 7].Value = sale.CurrencyType ?? string.Empty;
            worksheet.Cells[row, 8].Value = sale.CurrencyRate;
            worksheet.Cells[row, 9].Value = sale.Status;
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        return package.GetAsByteArray();
    }
}
