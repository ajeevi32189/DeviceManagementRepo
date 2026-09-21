using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DeviceManagement.Services.Report;
using OfficeOpenXml;

namespace DeviceManagementOnly.Common.ImportExport
{
    /// <summary>
    /// Supported export/import file formats. Every list/report endpoint in the
    /// system should be able to hand back data in any of these.
    /// </summary>
    public enum ExportFormat
    {
        Excel,
        Csv,
        Pdf
    }

    public static class ExportFormatHelper
    {
        public static ExportFormat Parse(string? format)
        {
            var f = (format ?? "excel").Trim().ToLowerInvariant();
            return f switch
            {
                "xlsx" or "excel" or "xls" => ExportFormat.Excel,
                "csv" => ExportFormat.Csv,
                "pdf" => ExportFormat.Pdf,
                _ => throw new InvalidOperationException(
                    $"Unsupported export format '{format}'. Supported: excel, csv, pdf.")
            };
        }

        public static string ContentType(ExportFormat f) => f switch
        {
            ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExportFormat.Csv => "text/csv",
            ExportFormat.Pdf => "application/pdf",
            _ => "application/octet-stream"
        };

        public static string Extension(ExportFormat f) => f switch
        {
            ExportFormat.Excel => "xlsx",
            ExportFormat.Csv => "csv",
            ExportFormat.Pdf => "pdf",
            _ => "bin"
        };
    }

    public record ExportFileResult(byte[] Content, string ContentType, string FileName);

    /// <summary>
    /// Generic export service: give it ANY list of objects (DTOs) and a title,
    /// it will turn it into Excel, CSV or PDF using reflection over the public
    /// readable properties — no per-entity export code needed.
    /// PDF is produced by building a simple table .docx (OpenXml, already used
    /// elsewhere in this project) and converting it with the existing
    /// LibreOffice-based IPdfConverterService — so no new paid/3rd-party PDF
    /// library is required.
    /// </summary>
    public interface IExportService
    {
        Task<ExportFileResult> ExportAsync<T>(IEnumerable<T> data, ExportFormat format, string title);
        Task<ExportFileResult> ExportRawAsync(List<string> headers, List<List<string?>> rows, ExportFormat format, string title);
    }

    public class ExportService : IExportService
    {
        private readonly IPdfConverterService _pdfConverter;
        public ExportService(IPdfConverterService pdfConverter) => _pdfConverter = pdfConverter;

        public async Task<ExportFileResult> ExportAsync<T>(IEnumerable<T> data, ExportFormat format, string title)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToList();

            var headers = props.Select(p => p.Name).ToList();
            var rows = data.Select(item => props
                    .Select(p => FormatValue(p.GetValue(item)))
                    .ToList())
                .ToList();

            return await ExportRawAsync(headers, rows, format, title);
        }

        public async Task<ExportFileResult> ExportRawAsync(List<string> headers, List<List<string?>> rows, ExportFormat format, string title)
        {
            var safeTitle = string.IsNullOrWhiteSpace(title) ? "Export" : title;
            var fileBase = string.Concat(safeTitle.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Replace(' ', '_');

            byte[] content = format switch
            {
                ExportFormat.Excel => BuildExcel(headers, rows, safeTitle),
                ExportFormat.Csv => BuildCsv(headers, rows),
                ExportFormat.Pdf => await BuildPdfAsync(headers, rows, safeTitle),
                _ => throw new InvalidOperationException("Unsupported format")
            };

            var ext = ExportFormatHelper.Extension(format);
            var fileName = $"{fileBase}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{ext}";
            return new ExportFileResult(content, ExportFormatHelper.ContentType(format), fileName);
        }

        private static string? FormatValue(object? value)
        {
            if (value is null) return null;
            if (value is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");
            if (value is bool b) return b ? "Yes" : "No";
            return value.ToString();
        }

        private static byte[] BuildExcel(List<string> headers, List<List<string?>> rows, string sheetTitle)
        {
            // NOTE: license is set once, globally, at app startup in Program.cs via
            // ExcelPackage.License.SetNonCommercialOrganization(...) — the EPPlus 8 API.
            using var package = new ExcelPackage();
            var safeSheetName = sheetTitle.Length > 31 ? sheetTitle[..31] : sheetTitle;
            var ws = package.Workbook.Worksheets.Add(string.IsNullOrWhiteSpace(safeSheetName) ? "Export" : safeSheetName);

            for (int c = 0; c < headers.Count; c++)
            {
                ws.Cells[1, c + 1].Value = headers[c];
                ws.Cells[1, c + 1].Style.Font.Bold = true;
            }

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (int c = 0; c < row.Count; c++)
                {
                    ws.Cells[r + 2, c + 1].Value = row[c];
                }
            }

            if (headers.Count > 0) ws.Cells[1, 1, rows.Count + 1, headers.Count].AutoFitColumns();
            return package.GetAsByteArray();
        }

        private static byte[] BuildCsv(List<string> headers, List<List<string?>> rows)
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, new System.Text.UTF8Encoding(true));
            writer.WriteLine(string.Join(",", headers.Select(CsvEscape)));
            foreach (var row in rows)
                writer.WriteLine(string.Join(",", row.Select(CsvEscape)));
            writer.Flush();
            return ms.ToArray();
        }

        private static string CsvEscape(string? value)
        {
            value ??= string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private async Task<byte[]> BuildPdfAsync(List<string> headers, List<List<string?>> rows, string title)
        {
            var docxBytes = BuildTableDocx(headers, rows, title);
            return await _pdfConverter.ConvertDocxToPdfAsync(docxBytes);
        }

        private static byte[] BuildTableDocx(List<string> headers, List<List<string?>> rows, string title)
        {
            using var stream = new MemoryStream();
            using (var wordDoc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());
                var body = mainPart.Document.Body!;

                body.Append(new Paragraph(new Run(new RunProperties(new Bold(), new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "32" }),
                    new Text(title))));
                body.Append(new Paragraph(new Run(new Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC  |  {rows.Count} records"))));
                body.Append(new Paragraph());

                var table = new Table();
                var tblProps = new TableProperties(
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 4 },
                        new BottomBorder { Val = BorderValues.Single, Size = 4 },
                        new LeftBorder { Val = BorderValues.Single, Size = 4 },
                        new RightBorder { Val = BorderValues.Single, Size = 4 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                    ),
                    new TableWidth { Type = TableWidthUnitValues.Auto }
                );
                table.AppendChild(tblProps);

                var headerRow = new TableRow();
                foreach (var h in headers)
                {
                    headerRow.Append(new TableCell(
                        new TableCellProperties(new Shading { Fill = "D9E2EC" }),
                        new Paragraph(new Run(new RunProperties(new Bold()), new Text(h)))));
                }
                table.Append(headerRow);

                foreach (var row in rows)
                {
                    var tr = new TableRow();
                    foreach (var cell in row)
                    {
                        tr.Append(new TableCell(new Paragraph(new Run(new Text(cell ?? string.Empty)))));
                    }
                    table.Append(tr);
                }

                body.Append(table);
                body.Append(new Paragraph());
                body.Append(new SectionProperties(
                    new PageSize { Width = 16838, Height = 11906, Orient = PageOrientationValues.Landscape },
                    new PageMargin { Top = 720, Bottom = 720, Left = 720, Right = 720 }
                ));

                mainPart.Document.Save();
            }
            return stream.ToArray();
        }
    }
}
