using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Models.Device
{
    public class DeviceModelMapping
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DeviceTypeId { get; set; }         // FK → DeviceType

        public Guid ModelSpecificationId { get; set; } // FK → ModelSpecification

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
