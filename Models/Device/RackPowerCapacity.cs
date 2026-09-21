using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagementOnly.Models.Device
{
    public class RackPowerCapacity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>FK → Rack (required)</summary>
        public Guid RackId { get; set; }

        [ForeignKey(nameof(RackId))]
        public Rack? Rack { get; set; }

        [MaxLength(100)]
        public string? PDUName { get; set; }

        [MaxLength(20)]
        public string? PhaseName { get; set; }

        public decimal? RatedCapacityKW { get; set; }
        public decimal? EstimatedLoadKW { get; set; }
        public decimal? MeasuredLoadKW { get; set; }
        public decimal? FailoverReservedKW { get; set; }

        public DateTime? LastSyncedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
