using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  RACK POWER CAPACITY DTOs
    // ══════════════════════════════════════════════════════════════

    public class RackPowerCapacityCreateDto
    {
        [Required(ErrorMessage = "RackId required hai.")]
        public Guid RackId { get; set; }

        [MaxLength(100)]
        public string? PDUName { get; set; }

        [MaxLength(20)]
        public string? PhaseName { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "RatedCapacityKW negative nahi ho sakta.")]
        public decimal? RatedCapacityKW { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "EstimatedLoadKW negative nahi ho sakta.")]
        public decimal? EstimatedLoadKW { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "MeasuredLoadKW negative nahi ho sakta.")]
        public decimal? MeasuredLoadKW { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "FailoverReservedKW negative nahi ho sakta.")]
        public decimal? FailoverReservedKW { get; set; }
    }

    public class RackPowerCapacityUpdateDto
    {
        public Guid? RackId { get; set; }

        [MaxLength(100)]
        public string? PDUName { get; set; }

        [MaxLength(20)]
        public string? PhaseName { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? RatedCapacityKW { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EstimatedLoadKW { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MeasuredLoadKW { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? FailoverReservedKW { get; set; }

        public bool? IsActive { get; set; }
    }

    public class RackPowerCapacityResponseDto
    {
        public Guid Id { get; set; }
        public Guid RackId { get; set; }
        public string? RackName { get; set; }

        public string? PDUName { get; set; }
        public string? PhaseName { get; set; }
        public decimal? RatedCapacityKW { get; set; }
        public decimal? EstimatedLoadKW { get; set; }
        public decimal? MeasuredLoadKW { get; set; }
        public decimal? FailoverReservedKW { get; set; }
        public DateTime? LastSyncedAt { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
