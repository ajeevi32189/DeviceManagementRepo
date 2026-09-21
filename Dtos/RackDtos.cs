using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  RACK DTOs
    //  Status ke liye fixed allowed values — service layer me check hota hai
    //  (yahan bhi RegularExpression laga sakte the, lekin list badalne pe
    //  service ke ek hi jagah update karna easy rahega)
    // ══════════════════════════════════════════════════════════════

    public class RackCreateDto
    {
        [Required(ErrorMessage = "RoomId required hai.")]
        public Guid RoomId { get; set; }

        [Required(ErrorMessage = "RackName required hai."), MaxLength(100)]
        public string RackName { get; set; } = null!;

        [Range(1, 60, ErrorMessage = "TotalUHeight 1 se 60 ke beech hona chahiye.")]
        public int TotalUHeight { get; set; } = 42;

        [MaxLength(20)]
        public string? Status { get; set; } = "Empty";

        [Range(0, double.MaxValue)]
        public decimal? MaxWeightCapacityKg { get; set; }

        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }
    }

    public class RackUpdateDto
    {
        public Guid? RoomId { get; set; }

        [MaxLength(100)]
        public string? RackName { get; set; }

        [Range(1, 60)]
        public int? TotalUHeight { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxWeightCapacityKg { get; set; }

        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }

        public bool? IsActive { get; set; }
    }

    public class RackResponseDto
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public string? RoomName { get; set; }

        public string RackName { get; set; } = null!;
        public int TotalUHeight { get; set; }
        public string Status { get; set; } = null!;
        public decimal? MaxWeightCapacityKg { get; set; }
        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }

        /// <summary>Kitni U-height already occupied hai (DeviceRackLocation se calculate)</summary>
        public int UOccupiedTotal { get; set; }
        public int UAvailable { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
