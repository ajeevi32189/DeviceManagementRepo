using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Models.Device
{
    /// <summary>
    /// One row per model per protocol (Modbus / SNMP / BACnet) — the transport settings that
    /// apply to every physical unit of that model. For SNMP/BACnet most fields stay null since
    /// those protocols are self-describing. For Modbus this row is effectively the device driver.
    /// </summary>
    public class ModelProtocolProfile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ModelSpecificationId { get; set; }  // FK → ModelSpecification

        public string Protocol { get; set; } = null!;    // Modbus | SNMP | BACnet

        public int? DefaultPort { get; set; }             // 502 / 161 / 47808
        public int? DefaultUnitId { get; set; }            // Modbus slave id
        public int? FunctionCode { get; set; }             // 3 = holding, 4 = input
        public string? WordOrder { get; set; }             // BIG = high word first
        public string? ByteOrder { get; set; }
        public byte? AddressBase { get; set; }             // 1 = datasheet was 1-based
        public int? PollPeriodMs { get; set; }

        public string? Source { get; set; }                // datasheet URL or "auto-discovered"
        public bool IsVerified { get; set; } = false;       // proven against a live unit
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // Navigation
        public ModelSpecification? ModelSpecification { get; set; }
        public ICollection<ModelProtocolPoint> Points { get; set; } = new List<ModelProtocolPoint>();
    }
}