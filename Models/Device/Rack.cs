using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagementOnly.Models.Device
{
    public class Rack
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>FK → Room (required)</summary>
        public Guid RoomId { get; set; }

        [ForeignKey(nameof(RoomId))]
        public Room? Room { get; set; }

        [Required, MaxLength(100)]
        public string RackName { get; set; } = null!;

        public int TotalUHeight { get; set; } = 42;

        /// <summary>Empty, PartiallyOccupied, Full, Reserved, Faulty etc.</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Empty";

        public decimal? MaxWeightCapacityKg { get; set; }

        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
