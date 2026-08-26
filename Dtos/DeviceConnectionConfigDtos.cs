using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  DEVICE CONNECTION CONFIG DTOs
    //  Protocol allowed values service layer me enforce hote hain
    //  (Modbus, BACnet, MQTT, SNMP, OPCUA, HTTP, ThingsBoard)
    // ══════════════════════════════════════════════════════════════

    public class DeviceConnectionConfigCreateDto
    {
        [Required(ErrorMessage = "DeviceDetailId is required.")]
        public Guid DeviceDetailId { get; set; }

        [Required(ErrorMessage = "Protocol is required."), MaxLength(20)]
        public string Protocol { get; set; } = null!;

        [MaxLength(50)]
        public string? IPAddress { get; set; }

        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
        public int? Port { get; set; }

        [MaxLength(100)]
        public string? ObjectId { get; set; }

        [MaxLength(20)]
        public string? UnitId { get; set; }

        [Range(1, 86400, ErrorMessage = "PollingIntervalSeconds must be between 1 and 86400 (24 hours).")]
        public int PollingIntervalSeconds { get; set; } = 60;

        [MaxLength(100)]
        public string? ThingsBoardDeviceId { get; set; }
    }

    public class DeviceConnectionConfigUpdateDto
    {
        public Guid? DeviceDetailId { get; set; }

        [MaxLength(20)]
        public string? Protocol { get; set; }

        [MaxLength(50)]
        public string? IPAddress { get; set; }

        [Range(1, 65535)]
        public int? Port { get; set; }

        [MaxLength(100)]
        public string? ObjectId { get; set; }

        [MaxLength(20)]
        public string? UnitId { get; set; }

        [Range(1, 86400)]
        public int? PollingIntervalSeconds { get; set; }

        [MaxLength(100)]
        public string? ThingsBoardDeviceId { get; set; }

        public bool? IsActive { get; set; }
    }

    public class DeviceConnectionConfigResponseDto
    {
        public Guid Id { get; set; }
        public Guid DeviceDetailId { get; set; }
        public string? DeviceShortName { get; set; }

        public string Protocol { get; set; } = null!;
        public string? IPAddress { get; set; }
        public int? Port { get; set; }
        public string? ObjectId { get; set; }
        public string? UnitId { get; set; }
        public int PollingIntervalSeconds { get; set; }
        public string? ThingsBoardDeviceId { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
