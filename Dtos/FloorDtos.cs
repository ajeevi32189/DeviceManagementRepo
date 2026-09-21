using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  FLOOR DTOs
    // ══════════════════════════════════════════════════════════════

    public class FloorCreateDto
    {
        [Required(ErrorMessage = "Floor name required hai."), MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? SiteName { get; set; }

        [MaxLength(500)]
        public string? FloorPlanFileUrl { get; set; }
    }

    public class FloorUpdateDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? SiteName { get; set; }

        [MaxLength(500)]
        public string? FloorPlanFileUrl { get; set; }

        public bool? IsActive { get; set; }
    }

    public class FloorResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? SiteName { get; set; }
        public string? FloorPlanFileUrl { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
