using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagementOnly.Models.Device
{
    public class DeviceConnectionConfig
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>FK → DeviceDetail (required)</summary>
        public Guid DeviceDetailId { get; set; }

        [ForeignKey(nameof(DeviceDetailId))]
        public DeviceDetail? DeviceDetail { get; set; }

        /// <summary>Modbus, BACnet, MQTT, SNMP, OPCUA, HTTP, ThingsBoard etc.</summary>
        [Required, MaxLength(20)]
        public string Protocol { get; set; } = null!;

        [MaxLength(50)]
        public string? IPAddress { get; set; }

        public int? Port { get; set; }

        [MaxLength(100)]
        public string? ObjectId { get; set; }

        [MaxLength(20)]
        public string? UnitId { get; set; }

        public int PollingIntervalSeconds { get; set; } = 60;

        [MaxLength(100)]
        public string? ThingsBoardDeviceId { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
