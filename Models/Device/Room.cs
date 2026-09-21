using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagementOnly.Models.Device
{
    public class Room
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>FK → Floor (required)</summary>
        public Guid FloorId { get; set; }

        [ForeignKey(nameof(FloorId))]
        public Floor? Floor { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        public decimal? RoomAreaSqFt { get; set; }
        public decimal? ReservedAreaSqFt { get; set; }
        public decimal? InternalUseAreaSqFt { get; set; }

        // ── 3D floor-plan layout (drag-to-move / drag-to-resize / rotate) ──
        /// <summary>Room's origin (top-left) X coordinate on the floor plan.</summary>
        public decimal? PositionX { get; set; }
        /// <summary>Room's origin (top-left) Y coordinate on the floor plan.</summary>
        public decimal? PositionY { get; set; }
        /// <summary>Room's width on the floor plan (resize handle).</summary>
        public decimal? Width { get; set; }
        /// <summary>Room's length/depth on the floor plan (resize handle).</summary>
        public decimal? Length { get; set; }
        /// <summary>Rotation angle in degrees (0-360).</summary>
        public decimal? RotationAngle { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
