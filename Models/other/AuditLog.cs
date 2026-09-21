using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Models.other
{
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? TableName { get; set; }
        public string? RecordId { get; set; }   // Guid stored as string for flexibility
        public string? ActionType { get; set; }
        public string? ColumnName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? Remarks { get; set; }
        public bool IsFinal { get; set; } = false;
        public DateTime? FinalizedAt { get; set; }
        public string? FinalizedBy { get; set; }
    }
}
