using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagementOnly.Models.Device
{
    public class NetworkConnection
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>FK → DeviceDetail (source device, required)</summary>
        public Guid SourceDeviceDetailId { get; set; }

        [ForeignKey(nameof(SourceDeviceDetailId))]
        public DeviceDetail? SourceDeviceDetail { get; set; }

        [MaxLength(50)]
        public string? SourcePortName { get; set; }

        /// <summary>FK → DeviceDetail (target device, required)</summary>
        public Guid TargetDeviceDetailId { get; set; }

        [ForeignKey(nameof(TargetDeviceDetailId))]
        public DeviceDetail? TargetDeviceDetail { get; set; }

        [MaxLength(50)]
        public string? TargetPortName { get; set; }

        [MaxLength(50)]
        public string? CableType { get; set; }

        [MaxLength(30)]
        public string? CableColorCode { get; set; }

        [MaxLength(100)]
        public string? CableLabel { get; set; }

        /// <summary>Active, Inactive, Faulty</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
