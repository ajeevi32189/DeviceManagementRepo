using System.ComponentModel.DataAnnotations;
using System.IO;

namespace DeviceManagementOnly.Models
{
    public class EntityType
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string EntityName { get; set; } = null!;

        [MaxLength(150)]
        public string? DisplayName { get; set; }

        public bool IsActive { get; set; } = true;
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public ICollection<FileStore> FileStores { get; set; } = new List<FileStore>();
    }

    public static class EntityTypeNames
    {
        public const string DeviceCategory = "DeviceCategory";
        public const string DeviceType = "DeviceType";
        public const string DeviceMaster = "DeviceMaster";
        public const string ModelSpecification = "ModelSpecification";
        public const string DeviceDetail = "DeviceDetail";
        public const string DeviceLocation = "DeviceLocation";
    }
}