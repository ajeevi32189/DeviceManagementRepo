using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

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

        // ── 5D View ke liye naye field ─────────────────────────────────
        // Note: PositionX = X (East-West), PositionY = Z (North-South floor
        // plan depth). 3D me Y upar ki taraf hota hai, par ye column pehle se
        // floor-plan ka Z rakh raha tha, isliye naam waisa hi chhoda gaya hai.

        /// <summary>server | ac | dg | electrical | network | storage | ups | chiller | corridor | custom</summary>
        [MaxLength(30)] public string? RoomType { get; set; }

        /// <summary>Room ki oonchai (metres) — deewar kitni oonchi banegi.</summary>
        [Precision(18, 6)] public decimal? Height { get; set; }

        /// <summary>5D me room ka rang — #38bdf8 jaisa.</summary>
        [MaxLength(20)] public string? ColorHex { get; set; }

        /// <summary>Room ka icon — fa-server, fa-snowflake...</summary>
        [MaxLength(60)] public string? Icon { get; set; }

        /// <summary>Is room ka power budget (kW). Facility load isi ka jod hai.</summary>
        [Precision(18, 3)] public decimal? PowerBudgetKW { get; set; }

        /// <summary>User ne jis unit me dimensions daale the (m/ft/mm) — sirf dikhane ke liye.</summary>
        [MaxLength(10)] public string? DimensionUnit { get; set; } = "m";

        /// <summary>Room ke bahar ki khaali patti — {"top":1,"bottom":1,"left":1,"right":1,"unit":"m"}.</summary>
        public string? MarginsJson { get; set; }

        /// <summary>Room ke andar ki khaali patti (device isse bahar nahi ja sakte).</summary>
        public string? InnerMarginsJson { get; set; }

        /// <summary>Room ka display order / sort.</summary>
        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
