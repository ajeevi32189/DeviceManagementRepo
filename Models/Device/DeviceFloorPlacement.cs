using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Models.Device
{
    // ══════════════════════════════════════════════════════════════
    //  DEVICE FLOOR PLACEMENT — farsh par khade device ki jagah.
    //
    //  Ye 5D View ka SABSE ZAROORI naya table hai.
    //
    //  Farq samajh lijiye:
    //    DeviceRackLocation  → device rack ke ANDAR hai (kaunse U par)
    //    DeviceLocation      → device ka DAAK ka pata (city, pincode)
    //    DeviceFloorPlacement→ device farsh par KHADA hai (X/Z/rotation)
    //
    //  5D ke demo me 97 device farsh par khade hain — transformer, busway,
    //  DG set, MPP, PP, UPS, PDU cabinet, battery bank, row PDU, PAC,
    //  chiller, WAN, ISP, core router, firewall, dist switch, ToR, aur
    //  khud rack bhi. In sab ki jagah yahin rakhi jaati hai.
    //
    //  Coordinate system (3D engine jaisa hi):
    //    X → East–West      (building ke centre se, metres me)
    //    Z → North–South    (building ke centre se, metres me)
    //    Y → elevation      (0 = finished floor; roof ke liye +; plenum ke liye −)
    //
    //  DeviceDetailId NULLABLE kyun hai:
    //    5D me Toolbox se tile drag karke drop karte hi device dikhna
    //    chahiye — us waqt uska asset record abhi bana hi nahi hota.
    //    Aise placement ko IsProvisional = true rakha jaata hai, aur
    //    baad me "Assign asset" se DeviceDetailId bhar diya jaata hai.
    // ══════════════════════════════════════════════════════════════
    public class DeviceFloorPlacement
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Asset record. NULL = abhi sirf drop hua hai, asset nahi bana (IsProvisional).</summary>
        public Guid? DeviceDetailId { get; set; }

        [ForeignKey(nameof(DeviceDetailId))]
        public DeviceDetail? DeviceDetail { get; set; }

        /// <summary>FK → Floor (required — har placement kisi na kisi floor par hai).</summary>
        public Guid FloorId { get; set; }

        [ForeignKey(nameof(FloorId))]
        public Floor? Floor { get; set; }

        /// <summary>FK → Room. NULL = kisi room ke andar nahi (yard, roof, corridor ke bahar).</summary>
        public Guid? RoomId { get; set; }

        [ForeignKey(nameof(RoomId))]
        public Room? Room { get; set; }

        /// <summary>Device type — shape aur default size yahin se aate hain.</summary>
        public Guid? DeviceTypeId { get; set; }

        // ── Jagah ─────────────────────────────────────────────────
        [Precision(18, 6)] public decimal PositionX { get; set; }
        [Precision(18, 6)] public decimal PositionZ { get; set; }
        /// <summary>Elevation. 0 = floor, 4.2 = roof deck, negative = plenum.</summary>
        [Precision(18, 6)] public decimal PositionY { get; set; } = 0m;

        /// <summary>Degrees, 0–360. Rack rows aur PAC facing isi se bante hain.</summary>
        [Precision(9, 3)] public decimal RotationAngle { get; set; } = 0m;

        /// <summary>floor | plenum | ceiling | roof | yard — kis tal par khada hai.</summary>
        [MaxLength(20)] public string Level { get; set; } = "floor";

        // ── Dikhne ka tarika (DeviceType ke default ko yahan override kar sakte hain) ──
        [Precision(18, 3)] public decimal? LengthMm { get; set; }
        [Precision(18, 3)] public decimal? WidthMm { get; set; }
        [Precision(18, 3)] public decimal? HeightMm { get; set; }

        /// <summary>3D mesh builder ki key — busway, generator, ups, pdu, crac, rack, netrack...</summary>
        [MaxLength(50)] public string? Shape { get; set; }

        [MaxLength(20)] public string? ColorHex { get; set; }
        [MaxLength(60)] public string? IconName { get; set; }

        /// <summary>3D me device ke upar dikhne wala tag — UPS1, PAC-1W, RPDU-1A.</summary>
        [MaxLength(80)] public string? LabelText { get; set; }

        /// <summary>Rack row ka naam (A–F) — hot/cold aisle isi se bante hain.</summary>
        [MaxLength(40)] public string? RowName { get; set; }

        /// <summary>Aisle number ya naam.</summary>
        [MaxLength(40)] public string? AisleName { get; set; }

        // ── Lock / visibility ─────────────────────────────────────
        /// <summary>Sirf is ek device ko pin kar dena (poora layout lock kiye bina).</summary>
        public bool IsPinned { get; set; } = false;

        public bool IsVisible { get; set; } = true;

        /// <summary>Drop to ho gaya par asset record abhi nahi bana.</summary>
        public bool IsProvisional { get; set; } = false;

        /// <summary>Provisional placement ka naam — asset banne tak.</summary>
        [MaxLength(150)] public string? PlaceholderName { get; set; }

        public int SortOrder { get; set; } = 0;

        /// <summary>Frontend jo extra cheez rakhna chahe (JSON). Backend isme kuch nahi padhta.</summary>
        public string? MetaJson { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
