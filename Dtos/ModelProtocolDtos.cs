using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  MODEL PROTOCOL POINT DTOs
    //  PointKey change nahi hoti update me (identity field hai) —
    //  naya point chahiye to naya add karo, purana disable/delete karo.
    // ══════════════════════════════════════════════════════════════

    public class ModelProtocolPointCreateDto
    {
        [Required(ErrorMessage = "PointKey is required."), MaxLength(128)]
        public string PointKey { get; set; } = null!;           // e.g. kwh_total

        [MaxLength(255)]
        public string? DisplayName { get; set; }                 // e.g. Total Energy

        [Required(ErrorMessage = "Address is required."), MaxLength(255)]
        public string Address { get; set; } = null!;             // 2999 | OID | analogInput:1

        public int? FunctionCode { get; set; }
        public int? RegisterCount { get; set; } = 1;              // 2 for a 32-bit value

        [MaxLength(32)]
        public string? DataType { get; set; }                     // 16uint | 32uint | float

        public double? Multiplier { get; set; } = 1d;

        [MaxLength(32)]
        public string? Unit { get; set; }                         // V | A | kW | kWh | Hz

        [MaxLength(64)]
        public string? Category { get; set; }                     // voltage | current | energy

        [MaxLength(16)]
        public string? Confidence { get; set; } = "confirmed";    // confirmed | inferred

        public bool IsEnabled { get; set; } = true;
        public int? SortOrder { get; set; }
        public string? Remarks { get; set; }
    }

    public class ModelProtocolPointUpdateDto
    {
        [MaxLength(255)]
        public string? DisplayName { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        public int? FunctionCode { get; set; }
        public int? RegisterCount { get; set; }

        [MaxLength(32)]
        public string? DataType { get; set; }

        public double? Multiplier { get; set; }

        [MaxLength(32)]
        public string? Unit { get; set; }

        [MaxLength(64)]
        public string? Category { get; set; }

        [MaxLength(16)]
        public string? Confidence { get; set; }

        public bool? IsEnabled { get; set; }
        public int? SortOrder { get; set; }
        public string? Remarks { get; set; }
    }

    public class ModelProtocolPointResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public string PointKey { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string Address { get; set; } = null!;
        public int? FunctionCode { get; set; }
        public int? RegisterCount { get; set; }
        public string? DataType { get; set; }
        public double? Multiplier { get; set; }
        public string? Unit { get; set; }
        public string? Category { get; set; }
        public string? Confidence { get; set; }
        public bool IsEnabled { get; set; }
        public int? SortOrder { get; set; }
        public string? Remarks { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  MODEL PROTOCOL PROFILE DTOs
    //  Protocol allowed values: Modbus | SNMP | BACnet (service layer enforces)
    // ══════════════════════════════════════════════════════════════

    public class ModelProtocolProfileCreateDto
    {
        [Required(ErrorMessage = "ModelSpecificationId is required.")]
        public Guid ModelSpecificationId { get; set; }

        [Required(ErrorMessage = "Protocol is required."), MaxLength(32)]
        public string Protocol { get; set; } = null!;             // Modbus | SNMP | BACnet

        public int? DefaultPort { get; set; }                      // 502 / 161 / 47808
        public int? DefaultUnitId { get; set; }                    // Modbus slave id
        public int? FunctionCode { get; set; }                     // 3 = holding, 4 = input

        [MaxLength(8)]
        public string? WordOrder { get; set; } = "BIG";

        [MaxLength(8)]
        public string? ByteOrder { get; set; } = "BIG";

        public byte? AddressBase { get; set; }                      // 1 = datasheet was 1-based
        public int? PollPeriodMs { get; set; } = 10000;

        [MaxLength(512)]
        public string? Source { get; set; }                        // datasheet URL or "auto-discovered"

        public bool IsVerified { get; set; } = false;               // proven against a live unit

        [MaxLength(255)]
        public string? VerifiedBy { get; set; }

        public string? Remarks { get; set; }

        /// <summary>
        /// Optional — points can be created together with the profile in one call
        /// (e.g. pasting a full datasheet-derived map), or added one at a time later
        /// via the add-point endpoint.
        /// </summary>
        public List<ModelProtocolPointCreateDto>? Points { get; set; }
    }

    public class ModelProtocolProfileUpdateDto
    {
        [MaxLength(32)]
        public string? Protocol { get; set; }

        public int? DefaultPort { get; set; }
        public int? DefaultUnitId { get; set; }
        public int? FunctionCode { get; set; }

        [MaxLength(8)]
        public string? WordOrder { get; set; }

        [MaxLength(8)]
        public string? ByteOrder { get; set; }

        public byte? AddressBase { get; set; }
        public int? PollPeriodMs { get; set; }

        [MaxLength(512)]
        public string? Source { get; set; }

        /// <summary>When flipped false → true, VerifiedAt is stamped automatically.</summary>
        public bool? IsVerified { get; set; }

        [MaxLength(255)]
        public string? VerifiedBy { get; set; }

        public string? Remarks { get; set; }
    }

    public class ModelProtocolProfileResponseDto
    {
        public Guid Id { get; set; }
        public Guid ModelSpecificationId { get; set; }
        public string? ModelName { get; set; }
        public string? ModelNumber { get; set; }

        public string Protocol { get; set; } = null!;
        public int? DefaultPort { get; set; }
        public int? DefaultUnitId { get; set; }
        public int? FunctionCode { get; set; }
        public string? WordOrder { get; set; }
        public string? ByteOrder { get; set; }
        public byte? AddressBase { get; set; }
        public int? PollPeriodMs { get; set; }
        public string? Source { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<ModelProtocolPointResponseDto> Points { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════
    //  PROTOCOLSERVICE-FACING DTOs
    //  Matches "if option 1 goes the API route instead" — spec section 10
    // ══════════════════════════════════════════════════════════════

    /// <summary>One row per device ProtocolService needs to attempt discovery on.</summary>
    public class DiscoveryTargetDto
    {
        public Guid DeviceDetailId { get; set; }
        public string? DeviceShortName { get; set; }
        public string? SerialNumber { get; set; }

        public Guid DeviceMasterId { get; set; }
        public Guid? ModelSpecificationId { get; set; }
        public string? ModelName { get; set; }

        /// <summary>Authoritative protocol — read from DeviceMaster.DataFormat.</summary>
        public string? Protocol { get; set; }

        public string? IPAddress { get; set; }
        public string? GatewayIPAddress { get; set; }
        public int? Port { get; set; }
        public int? UnitId { get; set; }
        public string? SnmpCommunity { get; set; }
        public int? BacnetDeviceId { get; set; }

        public string? DiscoveryStatus { get; set; }
        public DateTime? LastDiscoveredAt { get; set; }
        public string? DiscoveryMessage { get; set; }
    }

    public class DiscoveryStatusUpdateDto
    {
        [Required(ErrorMessage = "DiscoveryStatus is required."), MaxLength(32)]
        public string DiscoveryStatus { get; set; } = null!;   // pending | discovered | onboarded | failed | needs_profile

        public string? DiscoveryMessage { get; set; }
    }
}
