using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagementOnly.Models.Device
{
    /// <summary>
    /// Rack + U-position based device location (data-center style).
    /// Naya alag model hai — existing DeviceLocation.cs (address/lat-long
    /// wala) se koi lena dena nahi, dono independent hain.
    /// </summary>
    public class DeviceRackLocation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>FK → DeviceDetail (required)</summary>
        public Guid DeviceDetailId { get; set; }

        [ForeignKey(nameof(DeviceDetailId))]
        public DeviceDetail? DeviceDetail { get; set; }

        /// <summary>FK → Rack (optional — device rack ke bahar bhi ho sakta hai)</summary>
        public Guid? RackId { get; set; }

        [ForeignKey(nameof(RackId))]
        public Rack? Rack { get; set; }

        public int? StartUPosition { get; set; }
        public int? UOccupied { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(20)]
        public string? PinCode { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // ---- 5D View / Rack elevation ke liye naye field ----

        /// <summary>front | rear - rack ke kis mooh par laga hai.</summary>
        [MaxLength(10)] public string? Face { get; set; } = "front";

        /// <summary>
        /// Is slot ke ports ka naksha, JSON array.
        /// 5D me har slot par 12 port hote hain aur cable ka endpoint
        /// "rackDeviceId#slotId@portIdx" hota hai - wahi yahan rakha jaata hai.
        /// </summary>
        public string? PortsJson { get; set; }

        /// <summary>Rack face par is device ki tasveer (FileStore).</summary>
        public Guid? FaceImageFileId { get; set; }

        /// <summary>Slot ka rang - rack elevation me.</summary>
        [MaxLength(20)] public string? ColorHex { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
