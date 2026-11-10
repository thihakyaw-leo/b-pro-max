namespace BProMax.Services;

/// <summary>
/// Interface for CSV export functionality.
/// </summary>
public interface ICsvExportService
{
    Task<byte[]> ExportAgentsToCsvAsync();
    Task<byte[]> ExportSalesToCsvAsync();
}
