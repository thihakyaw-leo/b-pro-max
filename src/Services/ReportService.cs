using System.Globalization;
using BProMax.Models;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace BProMax.Services;

/// <summary>
/// Report service using PdfSharpCore for PDF generation.
/// Replaces Crystal Reports from DD.exe.
/// </summary>
public class ReportService : IReportService
{
    private readonly IAgentService _agentService;
    private readonly ISalesFilterService _salesFilterService;
    private readonly ICommissionService _commissionService;
    private readonly ILogger<ReportService> _logger;

    private static readonly Action<ILogger, Exception?> _logFailedToGenerateAgentReport =
        LoggerMessage.Define(LogLevel.Error, 1, "Failed to generate agent report");
    private static readonly Action<ILogger, Exception?> _logFailedToGenerateSalesReport =
        LoggerMessage.Define(LogLevel.Error, 2, "Failed to generate sales report");

    public ReportService(
        IAgentService agentService,
        ISalesFilterService salesFilterService,
        ICommissionService commissionService,
        ILogger<ReportService> logger)
    {
        _agentService = agentService;
        _salesFilterService = salesFilterService;
        _commissionService = commissionService;
        _logger = logger;
    }

    public async Task<byte[]> GenerateAgentReportAsync()
    {
        try
        {
            var agents = await _agentService.GetAllAgentsAsync();
            return GenerateAgentReportPdf(agents);
        }
        catch (Exception ex)
        {
            _logFailedToGenerateAgentReport(_logger, ex);
            throw;
        }
    }

    public async Task<byte[]> GenerateSalesReportAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var sales = await _salesFilterService.GetAllSalesAsync();
            var filteredSales = _salesFilterService.FilterByDateRange(sales, startDate, endDate);
            return GenerateSalesReportPdf(filteredSales, startDate, endDate);
        }
        catch (Exception ex)
        {
            _logFailedToGenerateSalesReport(_logger, ex);
            throw;
        }
    }

    private static byte[] GenerateAgentReportPdf(List<Agent> agents)
    {
        using var document = new PdfDocument();
        document.Info.Title = "Agent Report - b-pro-max";
        document.Info.Subject = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        document.Info.Author = "b-pro-max";

        var page = document.AddPage();
        page.Width = 595;  // A4 width in points
        page.Height = 842; // A4 height in points
        var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Verdana", 12, XFontStyle.Bold);
        var fontSmall = new XFont("Verdana", 10, XFontStyle.Regular);

        // Title
        gfx.DrawString("Agent Report - b-pro-max", font, XBrushes.Black, new XRect(40, 40, page.Width - 80, 30), XStringFormats.TopCenter);
        gfx.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", fontSmall, XBrushes.Black, new XRect(40, 70, page.Width - 80, 20), XStringFormats.TopCenter);

        // Table headers
        var y = 100;
        var xStart = 40;
        var colWidths = new[] { 100.0, 150.0, 100.0, 80.0, 60.0, 100.0 };
        var headers = new[] { "Agent Name", "Address", "Phone", "Commission", "Rate", "Currency Type" };

        gfx.DrawRectangle(XPens.Black, XBrushes.LightGray, xStart, y, colWidths.Sum(), 20);
        for (int i = 0; i < headers.Length; i++)
        {
            gfx.DrawString(headers[i], font, XBrushes.Black, new XRect(xStart + colWidths.Take(i).Sum(), y + 2, colWidths[i], 16), XStringFormats.TopLeft);
        }

        y += 20;

        // Data rows
        foreach (var agent in agents)
        {
            gfx.DrawRectangle(XPens.LightGray, null, xStart, y, colWidths.Sum(), 20);
            gfx.DrawString(agent.AgentName, fontSmall, XBrushes.Black, new XRect(xStart, y + 2, colWidths[0], 16), XStringFormats.TopLeft);
            gfx.DrawString(agent.Address ?? string.Empty, fontSmall, XBrushes.Black, new XRect(xStart + colWidths[0], y + 2, colWidths[1], 16), XStringFormats.TopLeft);
            gfx.DrawString(agent.Phone ?? string.Empty, fontSmall, XBrushes.Black, new XRect(xStart + colWidths[0] + colWidths[1], y + 2, colWidths[2], 16), XStringFormats.TopLeft);
            gfx.DrawString(agent.Commission.ToString("N2", CultureInfo.InvariantCulture), fontSmall, XBrushes.Black, new XRect(xStart + colWidths[0] + colWidths[1] + colWidths[2], y + 2, colWidths[3], 16), XStringFormats.TopLeft);
            gfx.DrawString(agent.Rate.ToString("N2", CultureInfo.InvariantCulture), fontSmall, XBrushes.Black, new XRect(xStart + colWidths[0] + colWidths[1] + colWidths[2] + colWidths[3], y + 2, colWidths[4], 16), XStringFormats.TopLeft);
            gfx.DrawString(agent.CurrencyType ?? string.Empty, fontSmall, XBrushes.Black, new XRect(xStart + colWidths[0] + colWidths[1] + colWidths[2] + colWidths[3] + colWidths[4], y + 2, colWidths[5], 16), XStringFormats.TopLeft);
            y += 20;
        }

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    private static byte[] GenerateSalesReportPdf(List<SaleRecord> sales, DateTime startDate, DateTime endDate)
    {
        using var document = new PdfDocument();
        document.Info.Title = "Sales Report - b-pro-max";
        document.Info.Subject = $"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd} - Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        document.Info.Author = "b-pro-max";

        var page = document.AddPage();
        page.Width = 595;  // A4 width in points
        page.Height = 842; // A4 height in points
        var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Verdana", 12, XFontStyle.Bold);
        var fontSmall = new XFont("Verdana", 10, XFontStyle.Regular);

        // Title
        gfx.DrawString("Sales Report - b-pro-max", font, XBrushes.Black, new XRect(40, 40, page.Width - 80, 30), XStringFormats.TopCenter);
        gfx.DrawString($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}", fontSmall, XBrushes.Black, new XRect(40, 70, page.Width - 80, 20), XStringFormats.TopCenter);
        gfx.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", fontSmall, XBrushes.Black, new XRect(40, 90, page.Width - 80, 20), XStringFormats.TopCenter);

        // Table headers
        var y = 120;
        var xStart = 40;
        var colWidths = new[] { 120.0, 80.0, 80.0, 80.0, 80.0, 80.0 };
        var headers = new[] { "Agent Name", "Sale Date", "Amount", "Commission", "Commission1", "Status" };

        gfx.DrawRectangle(XPens.Black, XBrushes.LightGray, xStart, y, colWidths.Sum(), 20);
        for (int i = 0; i < headers.Length; i++)
        {
            gfx.DrawString(headers[i], font, XBrushes.Black, new XRect(xStart + colWidths.Take(i).Sum(), y + 2, colWidths[i], 16), XStringFormats.TopLeft);
        }

        y += 20;

        // Data rows
        foreach (var sale in sales)
        {
            gfx.DrawRectangle(XPens.LightGray, null, xStart, y, colWidths.Sum(), 20);
            gfx.DrawString(sale.AgentName, fontSmall, XBrushes.Black, new XRect(xStart, y + 2, colWidths[0], 16), XStringFormats.TopLeft);
            gfx.DrawString(sale.SaleDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), fontSmall, XBrushes.Black, new XRect(xStart + colWidths[0], y + 2, colWidths[1], 16), XStringFormats.TopLeft);
            gfx.DrawString(sale.Amount.ToString("N2", CultureInfo.InvariantCulture), fontSmall, XBrushes.Black, new XRect(xStart + colWidths[0] + colWidths[1], y + 2, colWidths[2], 16), XStringFormats.TopLeft);
            gfx.DrawString(sale.Commission.ToString("N2", CultureInfo.InvariantCulture), fontSmall, XBrushes.Black, new XRect(xStart + colWidths[0] + colWidths[1] + colWidths[2], y + 2, colWidths[3], 16), XStringFormats.TopLeft);
            gfx.DrawString(sale.Commission1.ToString("N2", CultureInfo.InvariantCulture), fontSmall, XBrushes.Black, new XRect(xStart + colWidths[0] + colWidths[1] + colWidths[2] + colWidths[3], y + 2, colWidths[4], 16), XStringFormats.TopLeft);
            gfx.DrawString(sale.Status, fontSmall, XBrushes.Black, new XRect(xStart + colWidths[0] + colWidths[1] + colWidths[2] + colWidths[3] + colWidths[4], y + 2, colWidths[5], 16), XStringFormats.TopLeft);
            y += 20;
        }

        // Totals
        var totals = CalculateTotals(sales);
        gfx.DrawString($"Totals - Amount: {totals.totalAmount:N2}, Commission: {totals.totalCommission:N2}, Commission1: {totals.totalCommission1:N2}", font, XBrushes.Black, new XRect(40, y + 10, page.Width - 80, 20), XStringFormats.TopLeft);

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    private static (decimal totalAmount, decimal totalCommission, decimal totalCommission1) CalculateTotals(List<SaleRecord> sales)
    {
        return (
            sales.Sum(s => s.Amount),
            sales.Sum(s => s.Commission),
            sales.Sum(s => s.Commission1)
        );
    }
}
