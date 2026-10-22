using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Models.Device
{
    public class Floor
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? SiteName { get; set; }

        [MaxLength(500)]
        public string? FloorPlanFileUrl { get; set; }

        /// <summary>Soft-disable instead of delete</summary>
        // ---- 5D View ke liye naye field ----

        /// <summary>FK - DataCenter. Purani rows ke liye NULL chalega.</summary>
        public Guid? DataCenterId { get; set; }

        /// <summary>Floor ki chaudai (metres) - East-West.</summary>
        [Precision(18, 6)] public decimal? WidthM { get; set; }

        /// <summary>Floor ki lambai (metres) - North-South.</summary>
        [Precision(18, 6)] public decimal? LengthM { get; set; }

        /// <summary>Slab se chhat tak ki oonchai (metres).</summary>
        [Precision(18, 6)] public decimal? HeightM { get; set; }

        /// <summary>Raised floor ki oonchai (metres) - plenum ki gehrai.</summary>
        [Precision(18, 6)] public decimal? RaisedFloorHeightM { get; set; }

        /// <summary>Building me kaunsa tal - 0 = ground, 1 = first...</summary>
        public int? LevelNumber { get; set; }

        /// <summary>Floor tile ka size (metres) - grid isi se banta hai.</summary>
        [Precision(18, 6)] public decimal? TileSizeM { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
