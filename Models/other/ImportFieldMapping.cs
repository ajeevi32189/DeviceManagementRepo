using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Models.other
{
    /// <summary>
    /// Remembers how a source file's column header ("Excel field") maps to a
    /// backend property ("our field") for a given entity/import type, so the
    /// user doesn't have to re-map the same sheet layout every time they import.
    /// One row per (EntityName, CompanyId, SourceHeader).
    /// </summary>
    public class ImportFieldMapping
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Logical import type, e.g. "DeviceDetail", "Floor", "Room".</summary>
        [Required, MaxLength(100)]
        public string EntityName { get; set; } = null!;

        /// <summary>Optional — mappings can be per-company or global (null = global default).</summary>
        public Guid? CompanyId { get; set; }

        /// <summary>The column header exactly as it appears in the uploaded Excel/CSV.</summary>
        [Required, MaxLength(200)]
        public string SourceHeader { get; set; } = null!;

        /// <summary>The backend property name it should be saved into.</summary>
        [Required, MaxLength(200)]
        public string TargetField { get; set; } = null!;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
