using System.Globalization;
using BProMax.Models;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace BProMax.Services;

/// <summary>
/// Exports the 2D reports to CSV, Excel, and PDF.
/// </summary>
public class TwoDExportService : ITwoDExportService
{
    private readonly ITwoDReportService _reportService;

    public TwoDExportService(ITwoDReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<byte[]> ExportCsvAsync(
        TwoDReportKind kind,
        DateOnly? fromDate = null,
        DateOnly? toDate = null)
    {
        var report = await BuildReportAsync(kind, fromDate, toDate);
        return GenerateCsv(report.Headers, report.Rows);
    }

    public async Task<byte[]> ExportExcelAsync(
        TwoDReportKind kind,
        DateOnly? fromDate = null,
        DateOnly? toDate = null)
    {
        var report = await BuildReportAsync(kind, fromDate, toDate);
        return GenerateExcel(report.SheetName, report.Headers, report.Rows);
    }

    public async Task<byte[]> ExportPdfAsync(
        TwoDReportKind kind,
        DateOnly? fromDate = null,
        DateOnly? toDate = null)
    {
        var report = await BuildReportAsync(kind, fromDate, toDate);
        return GeneratePdf(report.Title, report.Headers, report.Rows, fromDate, toDate);
    }

    private async Task<ReportData> BuildReportAsync(TwoDReportKind kind, DateOnly? fromDate, DateOnly? toDate)
    {
        switch (kind)
        {
            case TwoDReportKind.SalesByDraw:
                {
                    var rows = await _reportService.GetSalesByDrawAsync(fromDate, toDate);
                    return new ReportData(
                        "Sales by Draw",
                        "two-d-sales-by-draw",
                        "Sales by Draw",
                        ["Draw ID", "Date", "Time", "Result", "Status", "Sales", "Stake", "Commission", "Payout", "Net"],
                        rows.Select(row => new object[]
                        {
                        row.DrawId,
                        row.DrawDate,
                        row.DrawTime,
                        row.DrawNumber ?? string.Empty,
                        row.Status,
                        row.SaleCount,
                        row.TotalStake,
                        row.TotalCommission,
                        row.TotalPayout,
                        row.Net
                        }).ToList());
                }

            case TwoDReportKind.SalesByNumber:
                {
                    var rows = await _reportService.GetSalesByNumberAsync(fromDate, toDate);
                    return new ReportData(
                        "Sales by Number",
                        "two-d-sales-by-number",
                        "Sales by Number",
                        ["Number", "Lines", "Stake", "Winning Lines", "Payout"],
                        rows.Select(row => new object[]
                        {
                        row.Number,
                        row.LineCount,
                        row.TotalStake,
                        row.WinningLineCount,
                        row.TotalPayout
                        }).ToList());
                }

            case TwoDReportKind.GroupPerformance:
                {
                    var rows = await _reportService.GetGroupPerformanceAsync(fromDate, toDate);
                    return new ReportData(
                        "Group Performance",
                        "two-d-group-performance",
                        "Group Performance",
                        ["Group", "Name", "Base Lines", "Base Stake", "Reverse Lines", "Reverse Stake", "Winning Lines", "Payout"],
                        rows.Select(row => new object[]
                        {
                        row.GroupCode,
                        row.GroupName,
                        row.BaseLineCount,
                        row.BaseStake,
                        row.ReverseLineCount,
                        row.ReverseStake,
                        row.WinningLineCount,
                        row.TotalPayout
                        }).ToList());
                }

            case TwoDReportKind.PayoutRegister:
                {
                    var rows = await _reportService.GetPayoutRegisterAsync(fromDate, toDate);
                    return new ReportData(
                        "Payout Register",
                        "two-d-payout-register",
                        "Payout Register",
                        ["Payout ID", "Date", "Time", "Result", "Agent", "Type", "Group", "Number", "Stake", "Amount", "Status", "Paid At"],
                        rows.Select(row => new object[]
                        {
                        row.PayoutId,
                        row.DrawDate,
                        row.DrawTime,
                        row.DrawNumber ?? string.Empty,
                        row.AgentName,
                        row.BetTypeName,
                        row.GroupName ?? string.Empty,
                        row.Number ?? string.Empty,
                        row.Stake ?? (object)string.Empty,
                        row.Amount,
                        row.Status,
                        row.PaidAt ?? (object)string.Empty
                        }).ToList());
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown 2D report kind.");
        }
    }

    private static byte[] GenerateCsv(string[] headers, IReadOnlyList<object[]> rows)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        foreach (var header in headers)
        {
            csv.WriteField(header);
        }

        csv.NextRecord();

        foreach (var row in rows)
        {
            foreach (var value in row)
            {
                csv.WriteField(FormatValue(value));
            }

            csv.NextRecord();
        }

        writer.Flush();
        return memoryStream.ToArray();
    }

    private static byte[] GenerateExcel(string sheetName, string[] headers, IReadOnlyList<object[]> rows)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        for (var column = 0; column < headers.Length; column++)
        {
            worksheet.Cells[1, column + 1].Value = headers[column];
        }

        using (var headerRange = worksheet.Cells[1, 1, 1, headers.Length])
        {
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 51, 102));
            headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        var excelRow = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < row.Length; column++)
            {
                var value = row[column];
                if (value is string text)
                {
                    worksheet.Cells[excelRow, column + 1].Value = text;
                }
                else if (value is not null)
                {
                    worksheet.Cells[excelRow, column + 1].Value = value;
                }
            }

            excelRow++;
        }

        if (worksheet.Dimension is not null)
        {
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        return package.GetAsByteArray();
    }

    private static byte[] GeneratePdf(
        string title,
        string[] headers,
        IReadOnlyList<object[]> rows,
        DateOnly? fromDate,
        DateOnly? toDate)
    {
        using var document = new PdfDocument();
        document.Info.Title = $"{title} - b-pro-max";
        document.Info.Author = "b-pro-max";

        var page = AddLandscapePage(document);
        var gfx = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Verdana", 13, XFontStyle.Bold);
        var font = new XFont("Verdana", 6.5, XFontStyle.Regular);
        var headerFont = new XFont("Verdana", 7, XFontStyle.Bold);

        var margin = 30.0;
        var y = 30.0;
        gfx.DrawString(title, titleFont, XBrushes.Black, new XRect(margin, y, page.Width - (margin * 2), 20), XStringFormats.TopLeft);
        y += 20;

        var period = fromDate.HasValue || toDate.HasValue
            ? $"Period: {FormatValue(fromDate)} to {FormatValue(toDate)}   "
            : string.Empty;
        gfx.DrawString(
            $"{period}Generated: {DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}",
            font,
            XBrushes.Black,
            new XRect(margin, y, page.Width - (margin * 2), 14),
            XStringFormats.TopLeft);
        y += 22;

        var tableWidth = page.Width - (margin * 2);
        var columnWidth = tableWidth / headers.Length;
        const double rowHeight = 14;

        DrawPdfRow(gfx, headers, headerFont, margin, y, columnWidth, rowHeight, true);
        y += rowHeight;

        foreach (var row in rows)
        {
            if (y + rowHeight > page.Height - margin)
            {
                gfx.Dispose();
                page = AddLandscapePage(document);
                gfx = XGraphics.FromPdfPage(page);
                y = margin;
                DrawPdfRow(gfx, headers, headerFont, margin, y, columnWidth, rowHeight, true);
                y += rowHeight;
            }

            var values = row.Select(FormatValue).ToArray();
            DrawPdfRow(gfx, values, font, margin, y, columnWidth, rowHeight, false);
            y += rowHeight;
        }

        gfx.Dispose();
        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    private static PdfPage AddLandscapePage(PdfDocument document)
    {
        var page = document.AddPage();
        page.Width = 842;
        page.Height = 595;
        return page;
    }

    private static void DrawPdfRow(
        XGraphics gfx,
        string[] values,
        XFont font,
        double x,
        double y,
        double columnWidth,
        double rowHeight,
        bool header)
    {
        gfx.DrawRectangle(XPens.LightGray, header ? XBrushes.LightGray : null, x, y, columnWidth * values.Length, rowHeight);
        for (var column = 0; column < values.Length; column++)
        {
            gfx.DrawString(
                TrimForPdf(values[column]),
                font,
                XBrushes.Black,
                new XRect(x + (column * columnWidth) + 2, y + 2, columnWidth - 4, rowHeight - 2),
                XStringFormats.TopLeft);
        }
    }

    private static string TrimForPdf(string value)
    {
        const int maximumLength = 20;
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string text => text,
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("HH\\:mm", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            decimal amount => amount.ToString("N2", CultureInfo.InvariantCulture),
            int number => number.ToString(CultureInfo.InvariantCulture),
            long number => number.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private sealed class ReportData
    {
        public ReportData(string sheetName, string fileStem, string title, string[] headers, List<object[]> rows)
        {
            SheetName = sheetName;
            FileStem = fileStem;
            Title = title;
            Headers = headers;
            Rows = rows;
        }

        public string SheetName { get; }

        public string FileStem { get; }

        public string Title { get; }

        public string[] Headers { get; }

        public List<object[]> Rows { get; }
    }
}
