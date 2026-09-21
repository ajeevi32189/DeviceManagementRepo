using System.Text;

namespace DeviceManagementOnly.Common.ImportExport
{
    /// <summary>
    /// When an imported Excel/CSV/PDF-extracted/DXF file has a column or piece
    /// of data that has no matching field in our backend, we must not silently
    /// drop it. This writes a plain .txt file the user can download, listing —
    /// row by row — exactly which extra data existed in the source file but
    /// could not be saved because our backend doesn't have that field.
    /// </summary>
    public static class UnmatchedDataWriter
    {
        public class UnmatchedRow
        {
            public int RowNumber { get; set; }
            /// <summary>column header -> value, for headers with no backend field.</summary>
            public Dictionary<string, string> Values { get; set; } = new();
        }

        public static byte[] Build(string entityName, List<string> unmatchedColumns, List<UnmatchedRow> rows, string sourceFileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("UNMATCHED DATA REPORT");
            sb.AppendLine(new string('=', 60));
            sb.AppendLine($"Entity        : {entityName}");
            sb.AppendLine($"Source file   : {sourceFileName}");
            sb.AppendLine($"Generated (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("The following column(s) from your file do not exist as a field in the");
            sb.AppendLine("backend for this entity, so their values were NOT saved to the database.");
            sb.AppendLine("They are listed below so nothing is lost — you can review them here or");
            sb.AppendLine("ask for these fields to be added to the backend.");
            sb.AppendLine();
            sb.AppendLine("Unmatched column(s):");
            foreach (var col in unmatchedColumns)
                sb.AppendLine($"  - {col}");
            sb.AppendLine();
            sb.AppendLine(new string('-', 60));
            sb.AppendLine("Row-by-row values:");
            sb.AppendLine(new string('-', 60));

            foreach (var row in rows)
            {
                if (row.Values.Count == 0) continue;
                sb.AppendLine($"Row {row.RowNumber}:");
                foreach (var kv in row.Values)
                    sb.AppendLine($"    {kv.Key} = {kv.Value}");
                sb.AppendLine();
            }

            return new UTF8Encoding(true).GetBytes(sb.ToString());
        }
    }
}
