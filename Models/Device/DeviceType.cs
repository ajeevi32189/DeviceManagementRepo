using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Models.Device
{
    public class DeviceType
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = null!;

        public Guid DeviceCategoryId { get; set; }

        /// <summary>
        /// NOT used by ProtocolService for discovery — this is only a general/default hint at
        /// the category level (e.g. "Energy Meter") and can disagree with a specific model's
        /// real protocol. DeviceMaster.DataFormat is the authoritative source. Left as-is for
        /// now; not touched in the Protocol Discovery Schema change.
        /// </summary>
        public string? Protocol { get; set; }

        public string? SupportedFeature { get; set; }

        public string? Icon { get; set; }
        public string? IconColor { get; set; }
        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}