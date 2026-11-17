namespace BProMax.Services;

public interface IExcelExportService
{
    Task<byte[]> ExportAgentsToExcelAsync();
    Task<byte[]> ExportSalesToExcelAsync(DateTime start, DateTime endDate);
}
