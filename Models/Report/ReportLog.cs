using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagement.Models.Report
{
    /// <summary>
    /// Audit record of every calculator report that was generated and emailed.
    /// Mirrors the existing AuditLog pattern used elsewhere in this project.
    /// </summary>
    [Table("ReportLogs")]
    public class ReportLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string? CalculatorId { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string? CalculatorName { get; set; } = string.Empty;

        [Required, MaxLength(320)]
        public string? RecipientEmail { get; set; } = string.Empty;

        /// <summary>Raw JSON snapshot of inputs + result at the time the report was sent.</summary>
        [Column(TypeName = "json")]
        public string? PayloadJson { get; set; } = "{}";

        [Required, MaxLength(20)]
        public string ?Status { get; set; } = "Pending";   // Pending | Sent | Failed

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        /// <summary>Who triggered this report — set from the JWT if the endpoint requires auth.</summary>
        [MaxLength(200)]
        public string? RequestedBy { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? SentAtUtc { get; set; }
    }
}
