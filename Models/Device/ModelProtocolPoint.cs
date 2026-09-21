using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Models.Device
{
    /// <summary>
    /// Each readable value inside a ModelProtocolProfile. Address is a string so the same table
    /// serves all three protocols: a Modbus register number, an SNMP OID, or a BACnet object
    /// reference such as analogInput:1.
    /// </summary>
    public class ModelProtocolPoint
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProfileId { get; set; }  // FK → ModelProtocolProfile

        public string PointKey { get; set; } = null!;      // e.g. kwh_total
        public string? DisplayName { get; set; }             // e.g. Total Energy
        public string Address { get; set; } = null!;       // 2999 | OID | analogInput:1
        public int? FunctionCode { get; set; }
        public int? RegisterCount { get; set; } = 1;         // 2 for a 32-bit value
        public string? DataType { get; set; }                // 16uint | 32uint | float
        public double? Multiplier { get; set; } = 1d;         // raw * multiplier = real value
        public string? Unit { get; set; }                    // V | A | kW | kWh | Hz
        public string? Category { get; set; }                // voltage | current | energy
        public string? Confidence { get; set; } = "confirmed"; // confirmed | inferred

        public bool IsEnabled { get; set; } = true;
        public int? SortOrder { get; set; }
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // Navigation
        public ModelProtocolProfile? Profile { get; set; }
    }
}