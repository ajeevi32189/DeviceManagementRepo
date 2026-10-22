using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Models.Device
{
    // ══════════════════════════════════════════════════════════════
    //  FLOOR TILE — raised floor ki perforated / solid tile.
    //
    //  Cold aisle ki thandi hawa inhi perforated tile se aati hai,
    //  aur 5D ka airflow + hotspot map inhi ki jagah dekh kar banta
    //  hai. Demo me 48 perforated tile hain.
    // ══════════════════════════════════════════════════════════════
    public class FloorTile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid FloorId { get; set; }

        [ForeignKey(nameof(FloorId))]
        public Floor? Floor { get; set; }

        public Guid? RoomId { get; set; }

        /// <summary>perforated | solid | grill | cutout.</summary>
        [Required, MaxLength(20)]
        public string TileType { get; set; } = "perforated";

        [Precision(18, 6)] public decimal PositionX { get; set; }
        [Precision(18, 6)] public decimal PositionZ { get; set; }

        /// <summary>Tile ka side (metres) — aam taur par 0.6.</summary>
        [Precision(18, 6)] public decimal SizeM { get; set; } = 0.6m;

        /// <summary>Us tile se nikalne wali hawa (CFM) — airflow model ke liye.</summary>
        [Precision(18, 3)] public decimal? AirflowCfm { get; set; }

        /// <summary>Kis aisle ki tile hai — cold | hot | none.</summary>
        [MaxLength(10)] public string? AisleType { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
