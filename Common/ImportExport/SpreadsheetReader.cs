using OfficeOpenXml;

namespace DeviceManagementOnly.Common.ImportExport
{
    /// <summary>
    /// Reads .xlsx/.xls (via EPPlus) or .csv (hand-written parser — no extra
    /// NuGet dependency needed) into a uniform shape: a header row + rows of
    /// header -> cell-text dictionaries. This is what makes header-based
    /// (rather than fixed-column-position) import possible, which in turn is
    /// what makes field-mapping and "unknown column" detection possible.
    /// </summary>
    public static class SpreadsheetReader
    {
        public class ParsedSheet
        {
            public List<string> Headers { get; set; } = new();
            /// <summary>Each row: header -> raw cell text (already trimmed).</summary>
            public List<Dictionary<string, string>> Rows { get; set; } = new();
        }

        public static async Task<ParsedSheet> ReadAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Please upload a valid file.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            return ext switch
            {
                ".csv" => ReadCsv(ms),
                ".xlsx" or ".xls" => ReadExcel(ms),
                _ => throw new InvalidOperationException(
                    $"Unsupported import file type '{ext}'. Please upload .xlsx or .csv.")
            };
        }

        private static ParsedSheet ReadExcel(MemoryStream ms)
        {
            // NOTE: license is set once, globally, at app startup in Program.cs via
            // ExcelPackage.License.SetNonCommercialOrganization(...) — the EPPlus 8 API.
            // (Do NOT set the old ExcelPackage.LicenseContext here — it's obsolete in
            // EPPlus 8+ and silently fails the real license check.)
            using var package = new ExcelPackage(ms);
            var ws = package.Workbook.Worksheets[0];
            var sheet = new ParsedSheet();
            if (ws.Dimension == null) return sheet;

            int cols = ws.Dimension.Columns;
            int rowsCount = ws.Dimension.Rows;

            for (int c = 1; c <= cols; c++)
            {
                var header = ws.Cells[1, c].Text?.Trim();
                sheet.Headers.Add(string.IsNullOrWhiteSpace(header) ? $"Column{c}" : header);
            }

            for (int r = 2; r <= rowsCount; r++)
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool anyValue = false;
                for (int c = 1; c <= cols; c++)
                {
                    var text = ws.Cells[r, c].Text?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(text)) anyValue = true;
                    dict[sheet.Headers[c - 1]] = text;
                }
                if (anyValue) sheet.Rows.Add(dict);
            }

            return sheet;
        }

        private static ParsedSheet ReadCsv(MemoryStream ms)
        {
            var lines = ParseCsvLines(ms);
            var sheet = new ParsedSheet();
            if (lines.Count == 0) return sheet;

            sheet.Headers = lines[0].Select((h, i) => string.IsNullOrWhiteSpace(h) ? $"Column{i + 1}" : h.Trim()).ToList();

            for (int r = 1; r < lines.Count; r++)
            {
                var line = lines[r];
                if (line.Count == 1 && string.IsNullOrWhiteSpace(line[0])) continue; // skip blank line

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < sheet.Headers.Count; c++)
                {
                    dict[sheet.Headers[c]] = c < line.Count ? line[c].Trim() : string.Empty;
                }
                sheet.Rows.Add(dict);
            }

            return sheet;
        }

        /// <summary>
        /// Minimal RFC-4180-ish CSV parser: handles quoted fields, embedded
        /// commas/newlines inside quotes, and "" as an escaped quote.
        /// </summary>
        private static List<List<string>> ParseCsvLines(MemoryStream ms)
        {
            using var reader = new StreamReader(ms, detectEncodingFromByteOrderMarks: true);
            var text = reader.ReadToEnd();

            var lines = new List<List<string>>();
            var currentRow = new List<string>();
            var field = new System.Text.StringBuilder();
            bool inQuotes = false;

            void EndField() { currentRow.Add(field.ToString()); field.Clear(); }
            void EndRow() { EndField(); lines.Add(currentRow); currentRow = new List<string>(); }

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(ch);
                    continue;
                }

                switch (ch)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        EndField();
                        break;
                    case '\r':
                        break; // ignore, \n handles line end
                    case '\n':
                        EndRow();
                        break;
                    default:
                        field.Append(ch);
                        break;
                }
            }
            // trailing field/row (file may not end with newline)
            if (field.Length > 0 || currentRow.Count > 0) EndRow();

            return lines;
        }
    }
}
