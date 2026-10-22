using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Models.Device
{
    public class DeviceType
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = null!;

        public Guid DeviceCategoryId { get; set; }

        /// <summary>
        /// NOT used by ProtocolService for discovery — this is only a general/default hint at
        /// the category level (e.g. "Energy Meter") and can disagree with a specific model's
        /// real protocol. DeviceMaster.DataFormat is the authoritative source. Left as-is for
        /// now; not touched in the Protocol Discovery Schema change.
        /// </summary>
        public string? Protocol { get; set; }

        public string? SupportedFeature { get; set; }

        public string? Icon { get; set; }
        public string? IconColor { get; set; }
        // ---- 5D View ke liye naye field ----

        /// <summary>
        /// 3D mesh builder ki key. Iske bina har device plain box dikhega.
        /// Values: busway, generator, transformer, mpp, pp, ups, pdu, battery,
        /// crac, chiller, rack, netrack, corerouter, firewall, distswitch,
        /// torswitch, wan, isplink.
        /// </summary>
        [MaxLength(50)] public string? Shape { get; set; }

        /// <summary>Is type ke device ki default lambai (mm).</summary>
        [Precision(18, 3)] public decimal? DefaultLengthMm { get; set; }

        /// <summary>Default chaudai (mm).</summary>
        [Precision(18, 3)] public decimal? DefaultWidthMm { get; set; }

        /// <summary>Default oonchai (mm).</summary>
        [Precision(18, 3)] public decimal? DefaultHeightMm { get; set; }

        /// <summary>Ye type kitni garmi chhodta hai (kW) - hotspot map iska use karta hai.</summary>
        [Precision(18, 3)] public decimal? HeatKW { get; set; }

        /// <summary>Cooling device ho to kitni garmi kheenchta hai (kW).</summary>
        [Precision(18, 3)] public decimal? CoolingKW { get; set; }

        /// <summary>5D layer panel me kis layer me aaye - power|cooling|network|compute|security|safety.</summary>
        [MaxLength(40)] public string? LayerKey { get; set; }

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}