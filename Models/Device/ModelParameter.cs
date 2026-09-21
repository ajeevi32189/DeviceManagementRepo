using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Models.Device
{
    public class ModelParameter
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DeviceModelId { get; set; }    // FK → ModelSpecification

        public Guid ModelCategoryId { get; set; }  // FK → ModelCategory

        public string ParameterName { get; set; } = null!;

        public string ParameterValue { get; set; } = null!;

        public string? Unit { get; set; }
        public string? Capacity { get; set; }
        public string? Processor { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public string? Remarks { get; set; }

        public string? FirmwareVersion { get; set; }
        public string? Protocol { get; set; }
        public string? WarrantyDuration { get; set; }
        public string? EndOfLife { get; set; }

        public string? Intro { get; set; }
        public string? Uses { get; set; }
        public string? Feature { get; set; }

        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
