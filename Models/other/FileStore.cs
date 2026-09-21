using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Models
{
    public class FileStore
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // ── Polymorphic Reference ─────────────────────────────────
        [Required]
        public Guid EntityTypeId { get; set; }
        public EntityType EntityType { get; set; } = null!;

        /// <summary>
        /// Actual entity ka PK — DeviceDetail.Id, DeviceMaster.Id etc.
        /// DB-level FK nahi — logical reference hai
        /// </summary>
        [Required]
        public Guid EntityId { get; set; }

        // ── File Category ─────────────────────────────────────────
        /// <summary>"Image" ya "Document"</summary>
        [Required, MaxLength(20)]
        public string FileCategory { get; set; } = null!;

        // ── Common Fields ─────────────────────────────────────────
        [Required, MaxLength(500)]
        public string FileName { get; set; } = null!;

        [Required, MaxLength(1000)]
        public string FilePath { get; set; } = null!;

        [MaxLength(100)]
        public string? MimeType { get; set; }

        public long? FileSizeBytes { get; set; }

        // ── Image-only Fields ─────────────────────────────────────
        [MaxLength(50)]
        public string? FileUse { get; set; }        // "Front", "Back", "Label"
        public DateTime? FileExpiryDate { get; set; }

        // ── Document-only Fields ──────────────────────────────────
        [MaxLength(300)]
        public string? DocumentName { get; set; }

        [MaxLength(100)]
        public string? DocumentNumber { get; set; }

        public Guid? DocumentTypeId { get; set; }
        public DocumentType? DocumentType { get; set; }

        public Guid? IssuedById { get; set; }
        public DocumentIssuedBy? IssuedBy { get; set; }

        public DateTime? IssuedByDate { get; set; }
        public DateTime? DocumentValidTill { get; set; }

        // ── Soft Delete + Audit ───────────────────────────────────
        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public static class FileCategoryConstants
    {
        public const string Image = "Image";
        public const string Document = "Document";
    }
}