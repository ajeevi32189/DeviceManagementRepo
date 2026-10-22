using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

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

        // ---- 5D View ke liye naye field ----

        /// <summary>Rack kis taraf ghoom kar khada hai (degrees, 0-360).</summary>
        [Precision(9, 3)] public decimal? RotationAngle { get; set; }

        /// <summary>Cabinet ki chaudai (mm) - aam taur par 600.</summary>
        [Precision(18, 3)] public decimal? WidthMm { get; set; }

        /// <summary>Cabinet ki gehrai (mm) - aam taur par 1000-1200.</summary>
        [Precision(18, 3)] public decimal? DepthMm { get; set; }

        /// <summary>Cabinet ki oonchai (mm). TotalUHeight se bhi nikal sakti hai.</summary>
        [Precision(18, 3)] public decimal? HeightMm { get; set; }

        /// <summary>Kis row me hai - A, B, C... Hot/cold aisle isi se bante hain.</summary>
        [MaxLength(40)] public string? RowName { get; set; }

        /// <summary>Kis aisle me hai.</summary>
        [MaxLength(40)] public string? AisleName { get; set; }

        /// <summary>Rack ka front kis disha me hai - North | South | East | West.</summary>
        [MaxLength(10)] public string? FacingDirection { get; set; }

        /// <summary>rack | netrack - 3D builder kaunsa mesh banaye.</summary>
        [MaxLength(30)] public string? Shape { get; set; }

        /// <summary>5D me rack ka rang.</summary>
        [MaxLength(20)] public string? ColorHex { get; set; }

        /// <summary>3D me rack ke upar dikhne wala tag - RK-B1, NDR-1.</summary>
        [MaxLength(80)] public string? LabelText { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
