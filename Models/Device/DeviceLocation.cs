using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Models.Device
{
    public class DeviceLocation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>FK → DeviceDetail (required)</summary>
        public Guid DeviceDetailId { get; set; }

        [MaxLength(50)]
        public string? LocationType { get; set; }   // e.g. Primary, Secondary, Site, Warehouse

        [Required, MaxLength(500)]
        public string AddressLine1 { get; set; } = null!;

        public string? AddressLine2 { get; set; }

        /// <summary>External Guid refs — no FK constraint (from master-data service)</summary>
        public Guid? CountryId { get; set; }
        public Guid? StateId { get; set; }
        public Guid? CityId { get; set; }

        public int? PinCode { get; set; }

        public string? Landmark { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public Guid? TimeZoneId { get; set; }

        [MaxLength(20)]
        public string? LocationContactNumber { get; set; }

        [MaxLength(50)]
        public string? EntityType { get; set; }

        /// <summary>Soft-disable instead of delete</summary>
        public bool IsActive { get; set; } = true;

      
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

 
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

      
    }
}