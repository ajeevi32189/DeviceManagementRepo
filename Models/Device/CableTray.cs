using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Models.Device
{
    // ══════════════════════════════════════════════════════════════
    //  CABLE TRAY — farsh ke neeche / chhat ke upar chalne wali tray.
    //
    //  5D View har cable ko tray ke andar ek "way" (slot) me daal kar
    //  chalata hai, taaki 300 cable aapas me na ulajhein. Tray ek
    //  seedhi line hai:
    //      Axis = "x"  → line Z par fix hai, X me chalti hai
    //      Axis = "z"  → line X par fix hai, Z me chalti hai
    //
    //  TrayCode frontend ke chhote naam hain (PRW, PRE, HDR, TRUNK,
    //  ASP, BSP, YARD...) — routing rules inhi code se likhe gaye hain.
    // ══════════════════════════════════════════════════════════════
    public class CableTray
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid FloorId { get; set; }

        [ForeignKey(nameof(FloorId))]
        public Floor? Floor { get; set; }

        public Guid? RoomId { get; set; }

        /// <summary>Chhota code — PRW, PRE, HDR, TRUNK, ASP, BSP, YARD.</summary>
        [Required, MaxLength(40)]
        public string TrayCode { get; set; } = null!;

        [MaxLength(120)] public string? Name { get; set; }

        /// <summary>x | z — tray kis axis me chalti hai.</summary>
        [Required, MaxLength(2)]
        public string Axis { get; set; } = "x";

        /// <summary>Jis axis me nahi chalti, us axis ka fix coordinate.</summary>
        [Precision(18, 6)] public decimal Line { get; set; }

        /// <summary>Run ka shuruaat ka coordinate (Axis wale axis par).</summary>
        [Precision(18, 6)] public decimal FromCoord { get; set; }

        /// <summary>Run ka aakhir ka coordinate.</summary>
        [Precision(18, 6)] public decimal ToCoord { get; set; }

        [Precision(18, 6)] public decimal WidthM { get; set; } = 0.3m;

        /// <summary>Tray me kitne way (cable slots) hain — demo me 7–9.</summary>
        public int Slots { get; set; } = 7;

        /// <summary>Do way ke beech ki doori (metres).</summary>
        [Precision(18, 6)] public decimal PitchM { get; set; } = 0.12m;

        /// <summary>+1 ya -1 — slot numbering kis taraf se shuru ho.</summary>
        public int NearSide { get; set; } = 1;

        /// <summary>underfloor | plenum | ceiling | roof.</summary>
        [MaxLength(20)] public string Level { get; set; } = "underfloor";

        /// <summary>Tray ki ooncha-neecha jagah (metres, floor = 0).</summary>
        [Precision(18, 6)] public decimal ElevationY { get; set; } = -0.34m;

        /// <summary>power | data | cooling | mixed.</summary>
        [MaxLength(20)] public string TrayType { get; set; } = "mixed";

        [MaxLength(20)] public string? ColorHex { get; set; }
        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
