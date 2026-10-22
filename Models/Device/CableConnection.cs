using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Models.Device
{
    // ══════════════════════════════════════════════════════════════
    //  CABLE CONNECTION — 5D View ki har cable.
    //
    //  Demo layout me ~300 cable hain. Poora "deployment chain"
    //  (Transformer → Busway → MPP → PP → UPS → PDU → Row PDU → Rack)
    //  inhi rows se banta hai.
    //
    //  Endpoint do tarah ka ho sakta hai:
    //    1. Farsh ka device — FromPlacementId bhar do, FromEndpoint null.
    //    2. Rack ke andar ka port — FromEndpoint me frontend ka endpoint id
    //       rakho: "<rackDeviceId>#<slotId>@<portIdx>".
    //
    //  Rasta do tarah se de sakte hain (dono ek saath nahi):
    //    TrayPathJson  — tray ke through: [{"trayCode":"PRW","slot":2,"layer":0}, ...]
    //                    Zyadatar cable isi tarah chalti hain.
    //    WaypointsJson — seedhe 3D points, jab koi tray beech me na ho.
    //
    //  NetworkConnection table se farq: wo logical link hai (kaunsa port
    //  kis port se juda hai, speed, VLAN). Ye uska PHYSICAL rasta hai —
    //  kaunsi tray, kaunsa way, kaunsi layer. Dono alag cheez hain,
    //  isliye alag table rakha hai; chaho to NetworkConnectionId se jod do.
    // ══════════════════════════════════════════════════════════════
    public class CableConnection
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid FloorId { get; set; }

        [ForeignKey(nameof(FloorId))]
        public Floor? Floor { get; set; }

        // ── Kahan se ──────────────────────────────────────────────
        public Guid? FromPlacementId { get; set; }
        public Guid? FromDeviceDetailId { get; set; }
        /// <summary>"rackDeviceId#slotId@portIdx" — rack ke andar ka port.</summary>
        [MaxLength(200)] public string? FromEndpoint { get; set; }
        /// <summary>front | rear | left | right | top | bottom — device ka kaunsa face.</summary>
        [MaxLength(10)] public string? FromFace { get; set; }

        // ── Kahan tak ─────────────────────────────────────────────
        public Guid? ToPlacementId { get; set; }
        public Guid? ToDeviceDetailId { get; set; }
        [MaxLength(200)] public string? ToEndpoint { get; set; }
        [MaxLength(10)] public string? ToFace { get; set; }

        /// <summary>power | fiber | copper | network | cooling | backbone.</summary>
        [Required, MaxLength(30)]
        public string CableType { get; set; } = "power";

        /// <summary>5D ka layer panel kis layer me ise dikhaye.</summary>
        [MaxLength(40)] public string? LayerKey { get; set; }

        /// <summary>Tray ke through ka rasta, JSON array.</summary>
        public string? TrayPathJson { get; set; }

        /// <summary>Seedhe 3D points, JSON array (jab tray na ho).</summary>
        public string? WaypointsJson { get; set; }

        [Precision(18, 3)] public decimal? LengthM { get; set; }

        [MaxLength(20)] public string? ColorHex { get; set; }
        [MaxLength(120)] public string? Label { get; set; }

        /// <summary>planned | installed | faulty.</summary>
        [MaxLength(20)] public string Status { get; set; } = "installed";

        /// <summary>Agar isi cable ka logical link NetworkConnection me bhi hai.</summary>
        public Guid? NetworkConnectionId { get; set; }

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
