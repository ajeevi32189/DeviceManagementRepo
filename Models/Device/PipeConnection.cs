using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Models.Device
{
    // ══════════════════════════════════════════════════════════════
    //  PIPE CONNECTION — chilled water aur doosri pipe lines.
    //
    //  Cable se alag isliye rakha hai ki pipe tray me nahi chalti,
    //  uska apna route aur diameter hota hai, aur 5D ise alag layer
    //  ("cooling") me dikhata hai.
    // ══════════════════════════════════════════════════════════════
    public class PipeConnection
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid FloorId { get; set; }

        [ForeignKey(nameof(FloorId))]
        public Floor? Floor { get; set; }

        /// <summary>Kahan se — farsh par khada device (placement) ya asset.</summary>
        public Guid? FromPlacementId { get; set; }
        public Guid? FromDeviceDetailId { get; set; }

        /// <summary>Kahan tak.</summary>
        public Guid? ToPlacementId { get; set; }
        public Guid? ToDeviceDetailId { get; set; }

        /// <summary>chilled-water-supply | chilled-water-return | condenser | drain.</summary>
        [Required, MaxLength(40)]
        public string PipeType { get; set; } = "chilled-water-supply";

        [Precision(18, 3)] public decimal? DiameterMm { get; set; }

        /// <summary>Tray codes ki ordered list, JSON array — ["YARD","HDR"].</summary>
        public string? RouteJson { get; set; }

        /// <summary>Haath se diye gaye 3D points, JSON — [{"x":..,"y":..,"z":..}].</summary>
        public string? WaypointsJson { get; set; }

        [Precision(18, 6)] public decimal? FromOffset { get; set; }
        [Precision(18, 6)] public decimal? ToOffset { get; set; }

        [Precision(18, 6)] public decimal ElevationY { get; set; } = -0.62m;

        [Precision(18, 3)] public decimal? LengthM { get; set; }

        [MaxLength(20)] public string? ColorHex { get; set; }
        [MaxLength(120)] public string? Label { get; set; }

        /// <summary>planned | installed | faulty.</summary>
        [MaxLength(20)] public string Status { get; set; } = "installed";

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
