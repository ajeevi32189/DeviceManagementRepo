using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Models.other
{
    public class UnitMaster
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string UnitName { get; set; } = null!;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
