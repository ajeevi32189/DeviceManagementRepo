using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  NETWORK CONNECTION DTOs
    //  Source != Target check service layer me hota hai (cross-field
    //  validation DataAnnotations se clean nahi hoti)
    // ══════════════════════════════════════════════════════════════

    public class NetworkConnectionCreateDto
    {
        [Required(ErrorMessage = "SourceDeviceDetailId required hai.")]
        public Guid SourceDeviceDetailId { get; set; }

        [MaxLength(50)]
        public string? SourcePortName { get; set; }

        [Required(ErrorMessage = "TargetDeviceDetailId required hai.")]
        public Guid TargetDeviceDetailId { get; set; }

        [MaxLength(50)]
        public string? TargetPortName { get; set; }

        [MaxLength(50)]
        public string? CableType { get; set; }

        [MaxLength(30)]
        public string? CableColorCode { get; set; }

        [MaxLength(100)]
        public string? CableLabel { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; } = "Active";
    }

    public class NetworkConnectionUpdateDto
    {
        public Guid? SourceDeviceDetailId { get; set; }

        [MaxLength(50)]
        public string? SourcePortName { get; set; }

        public Guid? TargetDeviceDetailId { get; set; }

        [MaxLength(50)]
        public string? TargetPortName { get; set; }

        [MaxLength(50)]
        public string? CableType { get; set; }

        [MaxLength(30)]
        public string? CableColorCode { get; set; }

        [MaxLength(100)]
        public string? CableLabel { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }
    }

    public class NetworkConnectionResponseDto
    {
        public Guid Id { get; set; }

        public Guid SourceDeviceDetailId { get; set; }
        public string? SourceDeviceName { get; set; }
        public string? SourcePortName { get; set; }

        public Guid TargetDeviceDetailId { get; set; }
        public string? TargetDeviceName { get; set; }
        public string? TargetPortName { get; set; }

        public string? CableType { get; set; }
        public string? CableColorCode { get; set; }
        public string? CableLabel { get; set; }
        public string Status { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
