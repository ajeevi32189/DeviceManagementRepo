using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Models.Device
{
    // ══════════════════════════════════════════════════════════════
    //  DATA CENTRE — 5D View ka sabse bahar wala container.
    //
    //  Hierarchy:  DataCenter → Floor → Room → Rack → Device
    //
    //  5D View building ki chaar deewar, parapet, door aur floor grid
    //  isi row se banata hai. Pehle ye sab frontend me hardcoded tha
    //  (AddDataCenter.jsx), ab yahan se aata hai.
    //
    //  Dimensions do tarah se rakhi gayi hain:
    //    LengthM / BreadthM   — hamesha METRES me (3D engine isi ko padhta hai)
    //    LengthUnit / BreadthUnit — user ne jo unit me daala tha (m/ft/mm/yd),
    //                           sirf dobara dikhane ke liye. Convert karke
    //                           metres me store karna service ka kaam hai.
    // ══════════════════════════════════════════════════════════════
    public class DataCenter
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        /// <summary>Short code — DEMO-IDEAL-00 jaisa. Unique rakha gaya hai.</summary>
        [Required, MaxLength(50)]
        public string Code { get; set; } = null!;

        // ── Building footprint (3D engine ke liye — hamesha metres) ──
        [Precision(18, 6)] public decimal? LengthM { get; set; }    // East–West  (dcLength)
        [Precision(18, 6)] public decimal? BreadthM { get; set; }   // North–South (dcBreadth)
        [Precision(18, 6)] public decimal? HeightM { get; set; }    // slab to ceiling

        [MaxLength(10)] public string? LengthUnit { get; set; } = "m";
        [MaxLength(10)] public string? BreadthUnit { get; set; } = "m";

        // ── Perimeter margins (building wall se andar ki khaali patti) ──
        [Precision(18, 6)] public decimal? MarginTop { get; set; }
        [Precision(18, 6)] public decimal? MarginBottom { get; set; }
        [Precision(18, 6)] public decimal? MarginLeft { get; set; }
        [Precision(18, 6)] public decimal? MarginRight { get; set; }
        [MaxLength(10)] public string? MarginUnit { get; set; } = "m";

        /// <summary>North | South | East | West — building ka main door kis deewar par hai.</summary>
        [MaxLength(10)] public string? DoorDirection { get; set; }

        /// <summary>left | center | right — us deewar par door kahan hai.</summary>
        [MaxLength(10)] public string? DoorPosition { get; set; }

        // ── Capacity (dashboard aur capacity planner ke liye) ──
        [Precision(18, 3)] public decimal? TotalKW { get; set; }
        [MaxLength(10)] public string? TotalKWUnit { get; set; } = "kW";
        [Precision(18, 3)] public decimal? PowerCapacityKW { get; set; }
        [Precision(18, 3)] public decimal? CoolingCapacityKW { get; set; }
        public int? TotalRacks { get; set; }
        public int? UtilizedRacks { get; set; }

        // ── Where it is (Company API ke masters ke GUID — FK nahi, sirf reference) ──
        [MaxLength(200)] public string? LocationName { get; set; }
        [MaxLength(300)] public string? StreetAddress { get; set; }
        [MaxLength(20)] public string? PinCode { get; set; }
        public Guid? CountryId { get; set; }
        public Guid? StateId { get; set; }
        public Guid? CityId { get; set; }
        public Guid? SiteId { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? ContactPersonId { get; set; }
        [Precision(10, 7)] public decimal? Latitude { get; set; }
        [Precision(10, 7)] public decimal? Longitude { get; set; }

        /// <summary>Active | Maintenance | Warning | Inactive</summary>
        [MaxLength(30)] public string Status { get; set; } = "Active";

        /// <summary>DXF se banaya gaya ho to original file ka FileStore reference.</summary>
        public Guid? DxfFileId { get; set; }

        /// <summary>manual | dxf — layout kaise bana tha.</summary>
        [MaxLength(20)] public string? SetupMode { get; set; } = "manual";

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
