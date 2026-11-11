using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Exports the 2D reports to CSV, Excel, and PDF.
/// </summary>
public interface ITwoDExportService
{
    Task<byte[]> ExportCsvAsync(
        TwoDReportKind kind,
        DateOnly? fromDate = null,
        DateOnly? toDate = null);

    Task<byte[]> ExportExcelAsync(
        TwoDReportKind kind,
        DateOnly? fromDate = null,
        DateOnly? toDate = null);

    Task<byte[]> ExportPdfAsync(
        TwoDReportKind kind,
        DateOnly? fromDate = null,
        DateOnly? toDate = null);
}
