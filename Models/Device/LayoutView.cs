using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagementOnly.Models.Device
{
    // ══════════════════════════════════════════════════════════════
    //  LAYOUT VIEW — 5D ka "saved view" / "compare views".
    //
    //  User camera ko kisi jagah set karke, kuch layer on-off karke
    //  us nazare ko naam de kar save kar leta hai. Abhi ye sirf
    //  browser ki memory me rehta tha aur refresh par chala jaata tha.
    // ══════════════════════════════════════════════════════════════
    public class LayoutView
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid FloorId { get; set; }

        [ForeignKey(nameof(FloorId))]
        public Floor? Floor { get; set; }

        public Guid? DataCenterId { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(500)] public string? Description { get; set; }

        /// <summary>Camera ki jagah — {"position":{...},"target":{...},"zoom":n}.</summary>
        public string? CameraJson { get; set; }

        /// <summary>Kaunsa layer on tha — {"power":true,"cooling":false,...}.</summary>
        public string? LayerStateJson { get; set; }

        /// <summary>Filter / highlight / view mode ka state.</summary>
        public string? FilterStateJson { get; set; }

        /// <summary>Thumbnail FileStore me — compare panel me dikhane ke liye.</summary>
        public Guid? ThumbnailFileId { get; set; }

        /// <summary>false = sirf banane wale ko dikhe.</summary>
        public bool IsShared { get; set; } = true;

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
