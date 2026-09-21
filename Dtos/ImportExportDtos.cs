namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  FIELD MAPPING (Excel/CSV header  ↔  backend field)
    // ══════════════════════════════════════════════════════════════

    public class FieldMappingEntryDto
    {
        /// <summary>Column header as it appears in the uploaded file.</summary>
        public string SourceHeader { get; set; } = null!;
        /// <summary>Backend property name to save it into.</summary>
        public string TargetField { get; set; } = null!;
    }

    public class SaveFieldMappingRequestDto
    {
        public string EntityName { get; set; } = null!;
        public Guid? CompanyId { get; set; }
        public List<FieldMappingEntryDto> Mappings { get; set; } = new();
    }

    public class FieldMappingResponseDto
    {
        public string EntityName { get; set; } = null!;
        public Guid? CompanyId { get; set; }
        /// <summary>Backend fields available for this entity — for building the mapping UI dropdown.</summary>
        public List<string> AvailableTargetFields { get; set; } = new();
        public List<FieldMappingEntryDto> Mappings { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════
    //  GENERIC IMPORT RESULT
    // ══════════════════════════════════════════════════════════════

    public class ImportRowError
    {
        public int RowNumber { get; set; }
        public string Message { get; set; } = null!;
    }

    public class GenericImportResultDto
    {
        public int TotalRows { get; set; }
        public int Inserted { get; set; }
        public int Failed { get; set; }
        public List<ImportRowError> Errors { get; set; } = new();

        /// <summary>
        /// True if the uploaded file had columns that don't exist on our backend
        /// entity. Those values were NOT dropped — they were written out to a
        /// downloadable .txt file (see UnmatchedFileUrl) so nothing is silently lost.
        /// </summary>
        public bool HasUnmatchedData { get; set; }
        public List<string> UnmatchedColumns { get; set; } = new();
        public string? UnmatchedFileUrl { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
