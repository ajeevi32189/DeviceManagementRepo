using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Models.Device
{
    public class DeviceMaster
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? DeviceTypeId { get; set; }          // FK → DeviceType

        public Guid? DeviceCategoryId { get; set; }      // FK → DeviceCategory

        public Guid? ModelSpecificationId { get; set; }  // FK → ModelSpecification

        public string? DeviceShortName { get; set; }

        public string? DeviceLongName { get; set; }

        /// <summary>
        /// External reference to Company in the Company microservice.
        /// Stored as Guid; no FK constraint since Company table is not in this service.
        /// </summary>
        public Guid? CompanyId { get; set; }

        public string? Remarks { get; set; }

        public string? DataFormat { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
