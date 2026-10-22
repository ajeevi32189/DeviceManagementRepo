using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeviceManagementOnly.Models.Device
{
    // ══════════════════════════════════════════════════════════════
    //  LAYOUT LOCK — 5D View ka "edit mode / lock mode" switch.
    //
    //  Do alag cheezein ek hi row me rakhi hain:
    //
    //  1. IsLocked — PAKKA lock.
    //     Jab layout final ho jaaye to lock kar do. Uske baad koi
    //     room/device/cable hil nahi sakta — 5D sirf dekhne aur live
    //     data ke liye khulta hai. Unlock karne ka haq sirf us role
    //     ke paas hai jise permission di gayi hai.
    //
    //  2. Edit lease (EditingBy / EditingSessionId / EditingExpiresAt) —
    //     ASTHAYI pakad.
    //     Jab koi user edit mode me jaata hai to wo lease le leta hai.
    //     Us waqt doosre users ko 5D read-only dikhta hai aur upar
    //     likha aata hai "Rahul abhi edit kar raha hai". Lease par
    //     expiry isliye hai ki agar koi tab band karke chala jaaye to
    //     layout hamesha ke liye block na ho jaaye — time nikalte hi
    //     apne aap chhoot jaati hai.
    //
    //  Har Floor ke liye ek hi row rehti hai (FloorId par unique index).
    // ══════════════════════════════════════════════════════════════
    public class LayoutLock
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>FK → Floor. Har floor ka ek hi lock record hota hai.</summary>
        public Guid FloorId { get; set; }

        [ForeignKey(nameof(FloorId))]
        public Floor? Floor { get; set; }

        public Guid? DataCenterId { get; set; }

        // ── 1. Pakka lock ─────────────────────────────────────────
        /// <summary>true = layout freeze. Koi move/add/delete nahi chalega.</summary>
        public bool IsLocked { get; set; } = false;

        [MaxLength(150)] public string? LockedBy { get; set; }
        public DateTime? LockedAt { get; set; }

        /// <summary>Kyun lock kiya — "Go-live layout, approved 20-Oct".</summary>
        [MaxLength(300)] public string? LockNote { get; set; }

        [MaxLength(150)] public string? UnlockedBy { get; set; }
        public DateTime? UnlockedAt { get; set; }

        // ── 2. Edit lease ─────────────────────────────────────────
        /// <summary>Abhi kaun edit kar raha hai. NULL = koi nahi.</summary>
        [MaxLength(150)] public string? EditingBy { get; set; }

        /// <summary>Us browser tab ki pehchan — wahi tab lease chhoda sakta hai.</summary>
        [MaxLength(100)] public string? EditingSessionId { get; set; }

        public DateTime? EditingSince { get; set; }

        /// <summary>Is waqt ke baad lease apne aap khatam. Heartbeat se aage badhti rehti hai.</summary>
        public DateTime? EditingExpiresAt { get; set; }

        /// <summary>Layout kitni baar badla — frontend stale save pakadne ke liye bhejta hai.</summary>
        public int Revision { get; set; } = 0;

        public DateTime? LastSavedAt { get; set; }
        [MaxLength(150)] public string? LastSavedBy { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
