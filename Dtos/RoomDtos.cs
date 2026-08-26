using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  ROOM DTOs
    // ══════════════════════════════════════════════════════════════

    public class RoomCreateDto
    {
        [Required(ErrorMessage = "FloorId required hai.")]
        public Guid FloorId { get; set; }

        [Required(ErrorMessage = "Room name required hai."), MaxLength(100)]
        public string Name { get; set; } = null!;

        [Range(0, double.MaxValue, ErrorMessage = "RoomAreaSqFt negative nahi ho sakta.")]
        public decimal? RoomAreaSqFt { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "ReservedAreaSqFt negative nahi ho sakta.")]
        public decimal? ReservedAreaSqFt { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "InternalUseAreaSqFt negative nahi ho sakta.")]
        public decimal? InternalUseAreaSqFt { get; set; }

        // ── Optional at creation time — 3D view baad me bhi place kar sakta hai ──
        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Width negative nahi ho sakta.")]
        public decimal? Width { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Length negative nahi ho sakta.")]
        public decimal? Length { get; set; }

        [Range(0, 360, ErrorMessage = "RotationAngle 0-360 ke beech hona chahiye.")]
        public decimal? RotationAngle { get; set; }
    }

    public class RoomUpdateDto
    {
        public Guid? FloorId { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? RoomAreaSqFt { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ReservedAreaSqFt { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? InternalUseAreaSqFt { get; set; }

        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Width { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Length { get; set; }

        [Range(0, 360)]
        public decimal? RotationAngle { get; set; }

        public bool? IsActive { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  ROOM GEOMETRY — sirf move/resize/rotate ke liye lightweight DTO.
    //  Ye endpoint baar-baar (drag/resize khatam hone par) call hoga,
    //  isliye poore RoomUpdateDto ki jagah alag/halka DTO rakha hai.
    // ══════════════════════════════════════════════════════════════
    public class RoomGeometryUpdateDto
    {
        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Width negative nahi ho sakta.")]
        public decimal? Width { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Length negative nahi ho sakta.")]
        public decimal? Length { get; set; }

        [Range(0, 360, ErrorMessage = "RotationAngle 0-360 ke beech hona chahiye.")]
        public decimal? RotationAngle { get; set; }
    }

    public class RoomResponseDto
    {
        public Guid Id { get; set; }
        public Guid FloorId { get; set; }
        public string? FloorName { get; set; }

        public string Name { get; set; } = null!;
        public decimal? RoomAreaSqFt { get; set; }
        public decimal? ReservedAreaSqFt { get; set; }
        public decimal? InternalUseAreaSqFt { get; set; }

        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }
        public decimal? RotationAngle { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
