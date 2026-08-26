using DeviceManagementOnly.Common;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Hubs;
using DeviceManagementOnly.Models.Device;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  ROOM — FloorId FK validate hota hai (Floor exist + active),
    //  aur Reserved+InternalUse area > total RoomArea nahi ho sakta
    //  (yeh cross-field business rule hai, DTO Range attribute se
    //  cover nahi hoti, isliye service layer me check kiya).
    //
    //  GEOMETRY (move/resize/rotate): UpdateGeometryAsync sirf position/
    //  size/rotation fields save karta hai (lightweight — frontend isko
    //  drag/resize KHATAM hone par ek hi baar call kare, har frame pe nahi),
    //  aur save hote hi RoomLayoutHub se us floor ke saare connected
    //  clients ko real-time broadcast kar deta hai.
    // ══════════════════════════════════════════════════════════════

    public interface IRoomService
    {
        Task<RoomResponseDto> CreateAsync(RoomCreateDto dto, string? createdBy);
        Task<PagedResult<RoomResponseDto>> GetAllAsync(RoomFilterParams p);
        Task<RoomResponseDto?> GetByIdAsync(Guid id);
        Task<RoomResponseDto?> UpdateAsync(Guid id, RoomUpdateDto dto, string? updatedBy);
        Task<RoomResponseDto?> UpdateGeometryAsync(Guid id, RoomGeometryUpdateDto dto, string? updatedBy);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy);
    }

    public class RoomService : IRoomService
    {
        private readonly DBContext _db;
        private readonly IHubContext<RoomLayoutHub> _hub;

        public RoomService(DBContext db, IHubContext<RoomLayoutHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        private void ValidateArea(decimal? roomArea, decimal? reserved, decimal? internalUse)
        {
            if (roomArea.HasValue)
            {
                var used = (reserved ?? 0) + (internalUse ?? 0);
                if (used > roomArea.Value)
                    throw new InvalidOperationException(
                $"Reserved + Internal use area ({used}) cannot exceed the Room Area ({roomArea}) sq. ft.");
            }
        }

        public async Task<RoomResponseDto> CreateAsync(RoomCreateDto dto, string? createdBy)
        {
            // ── VALIDATION: FloorId exist + active hona chahiye ──
            var floor = await _db.Floors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == dto.FloorId);
            if (floor is null)
                throw new InvalidOperationException($"Floor with Id={dto.FloorId} was not found.");
            if (!floor.IsActive)
                throw new InvalidOperationException($"Floor '{floor.Name}' is inactive. Rooms cannot be added to an inactive floor.");

            ValidateArea(dto.RoomAreaSqFt, dto.ReservedAreaSqFt, dto.InternalUseAreaSqFt);

            var room = new Room
            {
                FloorId = dto.FloorId,
                Name = dto.Name,
                RoomAreaSqFt = dto.RoomAreaSqFt,
                ReservedAreaSqFt = dto.ReservedAreaSqFt,
                InternalUseAreaSqFt = dto.InternalUseAreaSqFt,
                PositionX = dto.PositionX,
                PositionY = dto.PositionY,
                Width = dto.Width,
                Length = dto.Length,
                RotationAngle = dto.RotationAngle,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.Rooms.Add(room);
            await _db.SaveChangesAsync();
            return await MapAsync(room);
        }

        public async Task<PagedResult<RoomResponseDto>> GetAllAsync(RoomFilterParams p)
        {
            var query = _db.Rooms.AsNoTracking().Where(x => x.IsActive);

            // ── FILTER: FloorId (query string ?FloorId=...) ──
            if (p.FloorId.HasValue)
                query = query.Where(x => x.FloorId == p.FloorId.Value);

            // ── FILTER: free-text search on Name ──
            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(s));
            }

            query = query.OrderBy(x => x.Name);
            var paged = await query.ToPagedAsync(p);

            // ── OPTIMIZATION (N+1 fix): pehle har room ke liye alag Floor-name
            //    query lagti thi (MapAsync ke andar). Ab ek hi query se saare
            //    distinct FloorIds ke naam ek dictionary me le lete hain ──
            var floorIds = paged.Data.Select(r => r.FloorId).Distinct().ToList();
            var floorNames = await _db.Floors.AsNoTracking()
                .Where(f => floorIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => f.Name);

            var data = paged.Data.Select(r => Map(r, floorNames.GetValueOrDefault(r.FloorId))).ToList();
            return PagedResult<RoomResponseDto>.Create(data, paged.TotalRecords, p);
        }

        public async Task<RoomResponseDto?> GetByIdAsync(Guid id)
        {
            var room = await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return room is null ? null : await MapAsync(room);
        }

        public async Task<RoomResponseDto?> UpdateAsync(Guid id, RoomUpdateDto dto, string? updatedBy)
        {
            var room = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == id);
            if (room is null) return null;

            if (dto.FloorId.HasValue)
            {
                var floorExists = await _db.Floors.AnyAsync(f => f.Id == dto.FloorId.Value);
                if (!floorExists)
                    throw new InvalidOperationException($"Floor Id={dto.FloorId} nahi mila.");
                room.FloorId = dto.FloorId.Value;
            }

            if (dto.Name != null) room.Name = dto.Name;
            if (dto.RoomAreaSqFt.HasValue) room.RoomAreaSqFt = dto.RoomAreaSqFt;
            if (dto.ReservedAreaSqFt.HasValue) room.ReservedAreaSqFt = dto.ReservedAreaSqFt;
            if (dto.InternalUseAreaSqFt.HasValue) room.InternalUseAreaSqFt = dto.InternalUseAreaSqFt;
            if (dto.PositionX.HasValue) room.PositionX = dto.PositionX;
            if (dto.PositionY.HasValue) room.PositionY = dto.PositionY;
            if (dto.Width.HasValue) room.Width = dto.Width;
            if (dto.Length.HasValue) room.Length = dto.Length;
            if (dto.RotationAngle.HasValue) room.RotationAngle = dto.RotationAngle;

            ValidateArea(room.RoomAreaSqFt, room.ReservedAreaSqFt, room.InternalUseAreaSqFt);

            if (dto.IsActive.HasValue) room.IsActive = dto.IsActive.Value;

            room.UpdatedAt = DateTime.UtcNow;
            room.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return await MapAsync(room);
        }

        // ══════════════════════════════════════════════════════════════
        //  GEOMETRY UPDATE — drag (move) / resize / rotate khatam hone par
        //  frontend se EK baar call hoga. DB me save + connected clients ko
        //  SignalR se turant broadcast.
        // ══════════════════════════════════════════════════════════════
        public async Task<RoomResponseDto?> UpdateGeometryAsync(Guid id, RoomGeometryUpdateDto dto, string? updatedBy)
        {
            var room = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == id);
            if (room is null) return null;

            if (dto.PositionX.HasValue) room.PositionX = dto.PositionX;
            if (dto.PositionY.HasValue) room.PositionY = dto.PositionY;
            if (dto.Width.HasValue) room.Width = dto.Width;
            if (dto.Length.HasValue) room.Length = dto.Length;
            if (dto.RotationAngle.HasValue) room.RotationAngle = dto.RotationAngle;

            room.UpdatedAt = DateTime.UtcNow;
            room.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            var result = await MapAsync(room);

            // ── Multi-user real-time sync: isi floor ko dekh rahe baaki
            //    connected clients ko turant naya position/size/rotation bhej do ──
            await _hub.Clients.Group($"floor-{room.FloorId}")
                .SendAsync("RoomGeometryUpdated", new
                {
                    roomId = room.Id,
                    floorId = room.FloorId,
                    positionX = room.PositionX,
                    positionY = room.PositionY,
                    width = room.Width,
                    length = room.Length,
                    rotationAngle = room.RotationAngle,
                    updatedBy
                });

            return result;
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy)
        {
            var room = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == id);
            if (room is null) return false;

            room.IsActive = isActive;
            room.UpdatedAt = DateTime.UtcNow;
            room.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<RoomResponseDto> MapAsync(Room r)
        {
            var floorName = await _db.Floors.AsNoTracking()
                .Where(f => f.Id == r.FloorId).Select(f => f.Name).FirstOrDefaultAsync();
            return Map(r, floorName);
        }

        // ── entityName caller se milta hai (bulk-fetched), koi DB call nahi ──
        private static RoomResponseDto Map(Room r, string? floorName) => new()
        {
            Id = r.Id,
            FloorId = r.FloorId,
            FloorName = floorName,
            Name = r.Name,
            RoomAreaSqFt = r.RoomAreaSqFt,
            ReservedAreaSqFt = r.ReservedAreaSqFt,
            InternalUseAreaSqFt = r.InternalUseAreaSqFt,
            PositionX = r.PositionX,
            PositionY = r.PositionY,
            Width = r.Width,
            Length = r.Length,
            RotationAngle = r.RotationAngle,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}