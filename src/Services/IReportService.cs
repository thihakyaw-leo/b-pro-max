namespace BProMax.Services;

public interface IReportService
{
    Task<byte[]> GenerateAgentReportAsync();
    Task<byte[]> GenerateSalesReportAsync(DateTime startDate, DateTime endDate);
}
