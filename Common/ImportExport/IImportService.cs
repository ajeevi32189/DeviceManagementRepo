using System.Reflection;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;

namespace DeviceManagementOnly.Common.ImportExport
{
    /// <summary>
    /// Generic, reusable Excel/CSV import pipeline.
    ///
    /// Flow per file:
    ///   1. Read the file into headers + rows (SpreadsheetReader — works for both .xlsx and .csv).
    ///   2. For every header, decide which backend field it maps to:
    ///        a) a previously-saved mapping for this entity (IFieldMappingService) wins first,
    ///        b) otherwise an automatic case/spacing-insensitive name match against
    ///           the target DTO's public settable properties,
    ///        c) otherwise the header is "unmatched".
    ///   3. Matched columns are reflection-set onto a new TCreateDto per row, then
    ///      handed to the caller's saveRowAsync (which does the real business logic
    ///      / EF save, exactly like the existing per-entity import code does).
    ///   4. Any column (or, for a row, any populated value under an unmatched
    ///      column) our backend has no field for is NOT dropped — it is written
    ///      to a downloadable .txt file via UnmatchedDataWriter.
    /// </summary>
    public interface IImportService
    {
        Task<GenericImportResultDto> ImportAsync<TCreateDto>(
            IFormFile file,
            string entityName,
            Guid? companyId,
            Func<TCreateDto, int, Task> saveRowAsync)
            where TCreateDto : new();

        /// <summary>Low-level: just parses + resolves the column mapping, without saving anything.
        /// Useful for import types (e.g. multi-lookup DeviceDetail import) that need custom
        /// per-row business logic but still want mapping + unmatched-column detection.</summary>
        Task<(SpreadsheetReader.ParsedSheet Sheet, Dictionary<string, string> HeaderToField, List<string> UnmatchedColumns)>
            ResolveMappingAsync(IFormFile file, string entityName, Guid? companyId, IEnumerable<string> knownBackendFields);

        Task<string> SaveUnmatchedFileAsync(string entityName, List<string> unmatchedColumns,
            List<UnmatchedDataWriter.UnmatchedRow> unmatchedRows, string sourceFileName);
    }

    public class ImportService : IImportService
    {
        private readonly IFieldMappingService _mappingService;
        private readonly IWebHostEnvironment _env;

        public ImportService(IFieldMappingService mappingService, IWebHostEnvironment env)
        {
            _mappingService = mappingService;
            _env = env;
        }

        public async Task<GenericImportResultDto> ImportAsync<TCreateDto>(
            IFormFile file,
            string entityName,
            Guid? companyId,
            Func<TCreateDto, int, Task> saveRowAsync)
            where TCreateDto : new()
        {
            var props = typeof(TCreateDto)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToList();

            var (sheet, headerToField, unmatchedColumns) =
                await ResolveMappingAsync(file, entityName, companyId, props.Select(p => p.Name));

            var result = new GenericImportResultDto { TotalRows = sheet.Rows.Count, UnmatchedColumns = unmatchedColumns };
            var unmatchedRows = new List<UnmatchedDataWriter.UnmatchedRow>();

            for (int i = 0; i < sheet.Rows.Count; i++)
            {
                int rowNumber = i + 2; // header is row 1
                var row = sheet.Rows[i];
                var dto = new TCreateDto();

                try
                {
                    RowMapper.PopulateFromRow(dto, sheet.Headers, row, headerToField);
                    await saveRowAsync(dto, rowNumber);
                    result.Inserted++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add(new ImportRowError { RowNumber = rowNumber, Message = ex.Message });
                }

                // collect unmatched-column values for this row (if any non-empty)
                if (unmatchedColumns.Count > 0)
                {
                    var extra = new Dictionary<string, string>();
                    foreach (var col in unmatchedColumns)
                    {
                        if (row.TryGetValue(col, out var val) && !string.IsNullOrWhiteSpace(val))
                            extra[col] = val;
                    }
                    if (extra.Count > 0)
                        unmatchedRows.Add(new UnmatchedDataWriter.UnmatchedRow { RowNumber = rowNumber, Values = extra });
                }
            }

            if (unmatchedRows.Count > 0)
            {
                result.HasUnmatchedData = true;
                result.UnmatchedFileUrl = await SaveUnmatchedFileAsync(entityName, unmatchedColumns, unmatchedRows, file.FileName);
            }

            result.Message = result.Failed == 0
                ? $"Import complete: {result.Inserted}/{result.TotalRows} row(s) saved."
                : $"Import finished with issues: {result.Inserted} saved, {result.Failed} failed out of {result.TotalRows}.";

            if (result.HasUnmatchedData)
                result.Message += $" NOTE: {unmatchedColumns.Count} column(s) in your file don't exist in our backend " +
                                   $"({string.Join(", ", unmatchedColumns)}) — their values were saved to a .txt file, see UnmatchedFileUrl.";

            return result;
        }

        public async Task<(SpreadsheetReader.ParsedSheet Sheet, Dictionary<string, string> HeaderToField, List<string> UnmatchedColumns)>
            ResolveMappingAsync(IFormFile file, string entityName, Guid? companyId, IEnumerable<string> knownBackendFields)
        {
            var sheet = await SpreadsheetReader.ReadAsync(file);
            var savedMapping = await _mappingService.GetMappingAsync(entityName, companyId);

            var fieldsByNormalized = knownBackendFields
                .GroupBy(Normalize)
                .ToDictionary(g => g.Key, g => g.First());

            var headerToField = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var unmatched = new List<string>();

            foreach (var header in sheet.Headers)
            {
                if (savedMapping.TryGetValue(header, out var mappedField))
                {
                    headerToField[header] = mappedField;
                    continue;
                }

                if (fieldsByNormalized.TryGetValue(Normalize(header), out var autoField))
                {
                    headerToField[header] = autoField;
                    continue;
                }

                unmatched.Add(header);
            }

            return (sheet, headerToField, unmatched);
        }

        public Task<string> SaveUnmatchedFileAsync(string entityName, List<string> unmatchedColumns,
            List<UnmatchedDataWriter.UnmatchedRow> unmatchedRows, string sourceFileName)
        {
            var bytes = UnmatchedDataWriter.Build(entityName, unmatchedColumns, unmatchedRows, sourceFileName);

            var folder = Path.Combine(_env.ContentRootPath, "uploads", "importlogs");
            Directory.CreateDirectory(folder);
            var fileName = $"{entityName}_unmatched_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.txt";
            var fullPath = Path.Combine(folder, fileName);
            File.WriteAllBytes(fullPath, bytes);

            return Task.FromResult($"/uploads/importlogs/{fileName}");
        }

        private static string Normalize(string s) =>
            new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}
