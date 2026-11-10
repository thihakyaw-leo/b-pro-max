namespace BProMax.Services;

public interface IExcelExportService
{
    Task<byte[]> ExportAgentsToExcelAsync();
    Task<byte[]> ExportSalesToExcelAsync();
}
