using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagementOnly.Models.Device
{
    public class DeviceDetail
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DeviceMasterId { get; set; }  // FK → DeviceMaster

        // Self-reference for sub-devices
        public Guid? ParentDeviceDetailId { get; set; }

        [ForeignKey(nameof(ParentDeviceDetailId))]
        public DeviceDetail? ParentDevice { get; set; }

        public ICollection<DeviceDetail> SubDevices { get; set; } = new List<DeviceDetail>();

        // Device Identification
        public string? IMEI { get; set; }
        public string? MACAddress { get; set; }
        public string? TagNumber { get; set; }
        public string? ShortName { get; set; }
        public string? LongName { get; set; }
        public string? SerialNumber { get; set; }

        // Purchase Details
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public decimal? PurchaseCost { get; set; }
        public string? Remarks { get; set; }

        // Network Details
        public string? IPAddress { get; set; }
        public string? SIMNumber { get; set; }
        public string? BarcodeNumber { get; set; }
        public string? RFIDTagNumber { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
