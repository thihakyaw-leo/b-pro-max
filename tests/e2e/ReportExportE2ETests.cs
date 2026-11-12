namespace BProMax.E2ETests;

using BProMax.Models;
using BProMax.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

/// <summary>
/// E2E tests for report export (SC-06): PDF agent/sales reports via ReportService,
/// Excel agent export via ExcelExportService, CSV agent/sales export via CsvExportService.
/// Mirrors DD.exe reporting functionality.
/// </summary>
[Collection("E2E Database")]
public class ReportExportE2ETests : E2ETestBase
{
    [Fact]
    public async Task GenerateAgentReportReturnsNonEmptyPdf()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        var reportService = BuildReportService();

        var pdf = await reportService.GenerateAgentReportAsync();

        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);
        // PDF header magic bytes
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [Fact]
    public async Task GenerateAgentReportIncludesInsertedAgents()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Report Agent 1", commission: 100m, rate: 5m);
        await InsertTestAgentAsync("Report Agent 2", commission: 200m, rate: 7.5m);

        var reportService = BuildReportService();

        var pdf = await reportService.GenerateAgentReportAsync();

        Assert.NotEmpty(pdf);
        var text = System.Text.Encoding.UTF8.GetString(pdf);
        Assert.Contains("Report Agent 1", text);
        Assert.Contains("Report Agent 2", text);
    }

    [Fact]
    public async Task GenerateSalesReportReturnsNonEmptyPdf()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        var reportService = BuildReportService();

        var pdf = await reportService.GenerateSalesReportAsync(
            DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(1));

        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [Fact]
    public async Task ExportAgentsToExcelReturnsNonEmptyBytes()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("Excel Agent 1", commission: 100m, rate: 5m);
        await InsertTestAgentAsync("Excel Agent 2", commission: 200m, rate: 7.5m);

        var excelService = BuildExcelService();

        var xlsx = await excelService.ExportAgentsToExcelAsync();

        Assert.NotNull(xlsx);
        Assert.NotEmpty(xlsx);
        // XLSX is a ZIP file — check the ZIP magic bytes
        Assert.Equal(0x50, xlsx[0]); // 'P'
        Assert.Equal(0x4B, xlsx[1]); // 'K'
    }

    [Fact]
    public async Task ExportAgentsToCsvReturnsNonEmptyBytes()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        await InsertTestAgentAsync("CSV Agent 1", commission: 100m, rate: 5m);

        var csvService = BuildCsvService();

        var csv = await csvService.ExportAgentsToCsvAsync();

        Assert.NotNull(csv);
        Assert.NotEmpty(csv);
        var text = System.Text.Encoding.UTF8.GetString(csv);
        Assert.Contains("CSV Agent 1", text);
        Assert.Contains("Agent Name", text);
    }

    [Fact]
    public async Task ExportSalesToCsvReturnsBytesEvenWithNoSales()
    {
        if (SkipIfUnavailable()) return;
        await CleanAgentsTableAsync();

        var csvService = BuildCsvService();

        var csv = await csvService.ExportSalesToCsvAsync();

        Assert.NotNull(csv);
        Assert.NotEmpty(csv);
    }

    private ReportService BuildReportService()
    {
        var db = CreateDatabaseService();
        var agentService = new AgentService(db, NullLogger<AgentService>.Instance);
        var salesService = new SalesService(db, NullLogger<SalesService>.Instance);
        var salesFilterService = new SalesFilterService(salesService);
        var commissionService = new CommissionService();
        return new ReportService(
            agentService,
            salesFilterService,
            commissionService,
            NullLogger<ReportService>.Instance);
    }

    private ExcelExportService BuildExcelService()
    {
        var db = CreateDatabaseService();
        var agentService = new AgentService(db, NullLogger<AgentService>.Instance);
        return new ExcelExportService(agentService);
    }

    private CsvExportService BuildCsvService()
    {
        var db = CreateDatabaseService();
        var agentService = new AgentService(db, NullLogger<AgentService>.Instance);
        var salesService = new SalesService(db, NullLogger<SalesService>.Instance);
        var salesFilterService = new SalesFilterService(salesService);
        return new CsvExportService(agentService, salesFilterService);
    }
}
