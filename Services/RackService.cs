using DeviceManagementOnly.Common;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.Device;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  RACK — RoomId FK validate hota hai. Status field ke liye fixed
    //  allowed list yahin service me hai (Empty/PartiallyOccupied/Full/
    //  Reserved/Faulty) — DTO me sirf MaxLength check tha.
    //  Filter: RoomId, FloorId (Room ke through), Status.
    // ══════════════════════════════════════════════════════════════

    public interface IRackService
    {
        Task<RackResponseDto> CreateAsync(RackCreateDto dto, string? createdBy);
        Task<PagedResult<RackResponseDto>> GetAllAsync(RackFilterParams p);
        Task<RackResponseDto?> GetByIdAsync(Guid id);
        Task<RackResponseDto?> UpdateAsync(Guid id, RackUpdateDto dto, string? updatedBy);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy);
    }

    public class RackService : IRackService
    {
        private static readonly string[] AllowedStatuses =
            { "Empty", "PartiallyOccupied", "Full", "Reserved", "Faulty" };

        private readonly DBContext _db;
        public RackService(DBContext db) => _db = db;

        private void ValidateStatus(string? status)
        {
            if (status != null && !AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Status '{status}' valid nahi hai. Allowed: {string.Join(", ", AllowedStatuses)}");
        }

        public async Task<RackResponseDto> CreateAsync(RackCreateDto dto, string? createdBy)
        {
            // ── VALIDATION: RoomId exist + active ──
            var room = await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == dto.RoomId);
            if (room is null)
                throw new InvalidOperationException($"Room Id={dto.RoomId} nahi mila.");
            if (!room.IsActive)
                throw new InvalidOperationException($"Room '{room.Name}' inactive hai, isme rack add nahi ho sakta.");

            ValidateStatus(dto.Status);

            var rack = new Rack
            {
                RoomId = dto.RoomId,
                RackName = dto.RackName,
                TotalUHeight = dto.TotalUHeight,
                Status = dto.Status ?? "Empty",
                MaxWeightCapacityKg = dto.MaxWeightCapacityKg,
                PositionX = dto.PositionX,
                PositionY = dto.PositionY,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.Racks.Add(rack);
            await _db.SaveChangesAsync();
            return await MapAsync(rack);
        }

        public async Task<PagedResult<RackResponseDto>> GetAllAsync(RackFilterParams p)
        {
            var query = _db.Racks.AsNoTracking().Where(x => x.IsActive);

            // ── FILTER: RoomId ──
            if (p.RoomId.HasValue)
                query = query.Where(x => x.RoomId == p.RoomId.Value);

            // ── FILTER: FloorId — Room ke through indirect filter ──
            if (p.FloorId.HasValue)
            {
                var roomIds1 = _db.Rooms.AsNoTracking()
                    .Where(r => r.FloorId == p.FloorId.Value)
                    .Select(r => r.Id);
                query = query.Where(x => roomIds1.Contains(x.RoomId));
            }

            // ── FILTER: Status (exact match) ──
            if (!string.IsNullOrWhiteSpace(p.Status))
                query = query.Where(x => x.Status == p.Status);

            // ── FILTER: Search on RackName ──
            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x => x.RackName.ToLower().Contains(s));
            }

            query = query.OrderBy(x => x.RackName);
            var paged = await query.ToPagedAsync(p);

            // ── OPTIMIZATION (N+1 fix): pehle har rack ke liye 2 alag queries
            //    lagti thi (Room name + occupied-U sum). Ab dono ek-ek bulk
            //    query se dictionary me le lete hain, loop me 0 DB calls ──
            var rackIds = paged.Data.Select(r => r.Id).ToList();
            var roomIds = paged.Data.Select(r => r.RoomId).Distinct().ToList();

            var roomNames = await _db.Rooms.AsNoTracking()
                .Where(x => roomIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var occupiedByRack = await _db.DeviceRackLocations.AsNoTracking()
                .Where(x => x.RackId != null && rackIds.Contains(x.RackId.Value) && x.IsActive)
                .GroupBy(x => x.RackId!.Value)
                .Select(g => new { RackId = g.Key, Total = g.Sum(x => x.UOccupied ?? 0) })
                .ToDictionaryAsync(x => x.RackId, x => x.Total);

            var data = paged.Data
                .Select(r => Map(r, roomNames.GetValueOrDefault(r.RoomId), occupiedByRack.GetValueOrDefault(r.Id)))
                .ToList();

            return PagedResult<RackResponseDto>.Create(data, paged.TotalRecords, p);
        }

        public async Task<RackResponseDto?> GetByIdAsync(Guid id)
        {
            var rack = await _db.Racks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return rack is null ? null : await MapAsync(rack);
        }

        public async Task<RackResponseDto?> UpdateAsync(Guid id, RackUpdateDto dto, string? updatedBy)
        {
            var rack = await _db.Racks.FirstOrDefaultAsync(x => x.Id == id);
            if (rack is null) return null;

            if (dto.RoomId.HasValue)
            {
                var roomExists = await _db.Rooms.AnyAsync(r => r.Id == dto.RoomId.Value);
                if (!roomExists) throw new InvalidOperationException($"Room Id={dto.RoomId} nahi mila.");
                rack.RoomId = dto.RoomId.Value;
            }

            if (dto.RackName != null) rack.RackName = dto.RackName;
            if (dto.TotalUHeight.HasValue) rack.TotalUHeight = dto.TotalUHeight.Value;

            if (dto.Status != null)
            {
                ValidateStatus(dto.Status);
                rack.Status = dto.Status;
            }

            if (dto.MaxWeightCapacityKg.HasValue) rack.MaxWeightCapacityKg = dto.MaxWeightCapacityKg;
            if (dto.PositionX.HasValue) rack.PositionX = dto.PositionX;
            if (dto.PositionY.HasValue) rack.PositionY = dto.PositionY;
            if (dto.IsActive.HasValue) rack.IsActive = dto.IsActive.Value;

            rack.UpdatedAt = DateTime.UtcNow;
            rack.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return await MapAsync(rack);
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy)
        {
            var rack = await _db.Racks.FirstOrDefaultAsync(x => x.Id == id);
            if (rack is null) return false;

            rack.IsActive = isActive;
            rack.UpdatedAt = DateTime.UtcNow;
            rack.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<RackResponseDto> MapAsync(Rack r)
        {
            var roomName = await _db.Rooms.AsNoTracking()
                .Where(x => x.Id == r.RoomId).Select(x => x.Name).FirstOrDefaultAsync();

            var occupied = await _db.DeviceRackLocations.AsNoTracking()
                .Where(x => x.RackId == r.Id && x.IsActive)
                .SumAsync(x => x.UOccupied ?? 0);

            return Map(r, roomName, occupied);
        }

        // ── roomName aur occupied caller se milte hain (bulk-fetched), koi DB call nahi ──
        private static RackResponseDto Map(Rack r, string? roomName, int occupied) => new()
        {
            Id = r.Id,
            RoomId = r.RoomId,
            RoomName = roomName,
            RackName = r.RackName,
            TotalUHeight = r.TotalUHeight,
            Status = r.Status,
            MaxWeightCapacityKg = r.MaxWeightCapacityKg,
            PositionX = r.PositionX,
            PositionY = r.PositionY,
            UOccupiedTotal = occupied,
            UAvailable = r.TotalUHeight - occupied,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}