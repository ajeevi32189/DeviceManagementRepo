using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DeviceManagementOnly.Hubs
{
    // ══════════════════════════════════════════════════════════════
    //  ROOM LAYOUT HUB — multi-user real-time sync
    //
    //  Flow:
    //  1. 3D view khulte hi frontend connect karta hai aur
    //     JoinFloor(floorId) call karta hai → us floor ke group me add ho jaata hai.
    //  2. Jab koi user room move/resize/rotate karta hai, RoomService
    //     geometry DB me save karne ke baad isi hub se us floor-group ko
    //     "RoomGeometryUpdated" event bhejta hai.
    //  3. Baaki sab connected clients (jo usi floor ko dekh rahe hain)
    //     ko live update mil jaata hai — unhe dobara REST call nahi karni padti.
    //
    //  Route (Program.cs me mapped): /hubs/room-layout
    // ══════════════════════════════════════════════════════════════

    [Authorize]
    public class RoomLayoutHub : Hub
    {
        private static string FloorGroup(Guid floorId) => $"floor-{floorId}";

        /// <summary>Frontend 3D view floor open karte hi ye call kare.</summary>
        public async Task JoinFloor(Guid floorId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, FloorGroup(floorId));
        }

        /// <summary>Floor view band karte waqt (ya doosre floor pe jaate waqt) ye call kare.</summary>
        public async Task LeaveFloor(Guid floorId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, FloorGroup(floorId));
        }

        // ══════════════════════════════════════════════════════════
        //  5D VIEW ke events — server se client ko ye naam aate hain.
        //  Frontend inhi par listener lagaye:
        //
        //    RoomGeometryUpdated  { ... }   room move/resize/rotate hua
        //    PlacementMoved       { ... }   farsh ka koi device hila
        //    LayoutSaved          { floorId, revision, savedBy, savedAt }
        //                                   kisi ne poora layout save kiya --
        //                                   aapka revision purana ho gaya,
        //                                   dobara load kar lijiye
        //    LayoutLockChanged    { ...LayoutLockDto }
        //                                   lock laga ya hata -- 5D ko turant
        //                                   view-only ya editable kar dijiye
        //    LayoutEditStarted    { ...LayoutLockDto }
        //                                   kisi ne edit mode on kiya --
        //                                   baaki sabko "X edit kar raha hai"
        //    LayoutEditEnded      { ...LayoutLockDto }
        //                                   edit mode band -- ab koi bhi le sakta hai
        //
        //  Dhyan rahe: lease par expiry hai. Agar koi tab band karke chala
        //  jaaye to hub ko pata nahi chalta, par lease khud time nikalne par
        //  chhoot jaati hai -- isliye layout kabhi hamesha ke liye block nahi
        //  hota.
        // ══════════════════════════════════════════════════════════

        /// <summary>Connection zinda hai — client chaahe to ping kar sakta hai.</summary>
        public Task<string> Ping() => Task.FromResult("pong");
    }
}
