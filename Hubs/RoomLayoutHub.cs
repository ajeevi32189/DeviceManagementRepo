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
    }
}
