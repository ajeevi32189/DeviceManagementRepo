using DeviceManagementOnly.Common;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.Device;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  DEVICE RACK LOCATION — DeviceDetailId required (device exist
    //  check), RackId optional (agar diya toh Rack exist check + U
    //  position overlap check: same rack me do devices same U-slot
    //  occupy nahi kar sakte, aur StartUPosition+UOccupied,
    //  Rack.TotalUHeight se bahar nahi jaana chahiye).
    // ══════════════════════════════════════════════════════════════

    public interface IDeviceRackLocationService
    {
        Task<DeviceRackLocationResponseDto> CreateAsync(DeviceRackLocationCreateDto dto, string? createdBy);
        Task<PagedResult<DeviceRackLocationResponseDto>> GetAllAsync(DeviceRackLocationFilterParams p);
        Task<DeviceRackLocationResponseDto?> GetByIdAsync(Guid id);
        Task<DeviceRackLocationResponseDto?> UpdateAsync(Guid id, DeviceRackLocationUpdateDto dto, string? updatedBy);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy);
    }

    public class DeviceRackLocationService : IDeviceRackLocationService
    {
        private readonly DBContext _db;
        public DeviceRackLocationService(DBContext db) => _db = db;

        // ── Rack ke andar U-slot overlap check ─────────────────────
        private async Task ValidateRackPlacementAsync(Guid rackId, int? startU, int? uOccupied, Guid? excludeLocationId)
        {
            var rack = await _db.Racks.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rackId);
            if (rack is null)
                throw new InvalidOperationException($"Rack Id={rackId} nahi mila.");

            if (!startU.HasValue || !uOccupied.HasValue) return; // U-position optional hai

            var endU = startU.Value + uOccupied.Value - 1;
            if (endU > rack.TotalUHeight)
                throw new InvalidOperationException(
                    $"StartUPosition({startU}) + UOccupied({uOccupied}) = {endU + 1} rack ki TotalUHeight ({rack.TotalUHeight}) se bahar ja raha hai.");

            var others = await _db.DeviceRackLocations.AsNoTracking()
                .Where(x => x.RackId == rackId && x.IsActive && x.Id != excludeLocationId
                    && x.StartUPosition != null && x.UOccupied != null)
                .ToListAsync();

            foreach (var o in others)
            {
                var oEnd = o.StartUPosition!.Value + o.UOccupied!.Value - 1;
                var overlap = startU.Value <= oEnd && endU >= o.StartUPosition.Value;
                if (overlap)
                    throw new InvalidOperationException(
                        $"Rack me U{startU}-U{endU} pehle se occupied hai (U{o.StartUPosition}-U{oEnd}).");
            }
        }

        public async Task<DeviceRackLocationResponseDto> CreateAsync(DeviceRackLocationCreateDto dto, string? createdBy)
        {
            // ── VALIDATION: DeviceDetail exist ──
            var deviceExists = await _db.DeviceDetails.AsNoTracking().AnyAsync(d => d.Id == dto.DeviceDetailId);
            if (!deviceExists)
                throw new InvalidOperationException($"DeviceDetail Id={dto.DeviceDetailId} nahi mila.");

            if (dto.RackId.HasValue)
                await ValidateRackPlacementAsync(dto.RackId.Value, dto.StartUPosition, dto.UOccupied, null);

            var entity = new DeviceRackLocation
            {
                DeviceDetailId = dto.DeviceDetailId,
                RackId = dto.RackId,
                StartUPosition = dto.StartUPosition,
                UOccupied = dto.UOccupied,
                Address = dto.Address,
                Country = dto.Country,
                State = dto.State,
                City = dto.City,
                PinCode = dto.PinCode,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.DeviceRackLocations.Add(entity);
            await _db.SaveChangesAsync();
            return await MapAsync(entity);
        }

        public async Task<PagedResult<DeviceRackLocationResponseDto>> GetAllAsync(DeviceRackLocationFilterParams p)
        {
            var query = _db.DeviceRackLocations.AsNoTracking().Where(x => x.IsActive);

            // ── FILTER: DeviceDetailId, RackId, Country, State, City ──
            if (p.DeviceDetailId.HasValue)
                query = query.Where(x => x.DeviceDetailId == p.DeviceDetailId.Value);
            if (p.RackId.HasValue)
                query = query.Where(x => x.RackId == p.RackId.Value);
            if (!string.IsNullOrWhiteSpace(p.Country))
                query = query.Where(x => x.Country != null && x.Country == p.Country);
            if (!string.IsNullOrWhiteSpace(p.State))
                query = query.Where(x => x.State != null && x.State == p.State);
            if (!string.IsNullOrWhiteSpace(p.City))
                query = query.Where(x => x.City != null && x.City == p.City);

            // ── FILTER: Search on Address/PinCode ──
            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x =>
                    (x.Address != null && x.Address.ToLower().Contains(s)) ||
                    (x.PinCode != null && x.PinCode.Contains(s)));
            }

            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);

            // ── OPTIMIZATION (N+1 fix): pehle har row ke liye Device-name aur
            //    Rack-name ki alag queries lagti thi. Ab dono ek-ek bulk query
            //    se dictionary me le lete hain ──
            var deviceIds = paged.Data.Select(x => x.DeviceDetailId).Distinct().ToList();
            var rackIds = paged.Data.Where(x => x.RackId.HasValue).Select(x => x.RackId!.Value).Distinct().ToList();

            var deviceNames = await _db.DeviceDetails.AsNoTracking()
                .Where(d => deviceIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.ShortName);

            var rackNames = await _db.Racks.AsNoTracking()
                .Where(r => rackIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.RackName);

            var data = paged.Data.Select(e => Map(
                e,
                deviceNames.GetValueOrDefault(e.DeviceDetailId),
                e.RackId.HasValue ? rackNames.GetValueOrDefault(e.RackId.Value) : null)).ToList();

            return PagedResult<DeviceRackLocationResponseDto>.Create(data, paged.TotalRecords, p);
        }

        public async Task<DeviceRackLocationResponseDto?> GetByIdAsync(Guid id)
        {
            var e = await _db.DeviceRackLocations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return e is null ? null : await MapAsync(e);
        }

        public async Task<DeviceRackLocationResponseDto?> UpdateAsync(Guid id, DeviceRackLocationUpdateDto dto, string? updatedBy)
        {
            var e = await _db.DeviceRackLocations.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return null;

            if (dto.DeviceDetailId.HasValue)
            {
                var deviceExists = await _db.DeviceDetails.AnyAsync(d => d.Id == dto.DeviceDetailId.Value);
                if (!deviceExists) throw new InvalidOperationException($"DeviceDetail Id={dto.DeviceDetailId} nahi mila.");
                e.DeviceDetailId = dto.DeviceDetailId.Value;
            }

            var newRackId = dto.RackId ?? e.RackId;
            var newStartU = dto.StartUPosition ?? e.StartUPosition;
            var newUOccupied = dto.UOccupied ?? e.UOccupied;

            if (newRackId.HasValue)
                await ValidateRackPlacementAsync(newRackId.Value, newStartU, newUOccupied, e.Id);

            e.RackId = newRackId;
            e.StartUPosition = newStartU;
            e.UOccupied = newUOccupied;

            if (dto.Address != null) e.Address = dto.Address;
            if (dto.Country != null) e.Country = dto.Country;
            if (dto.State != null) e.State = dto.State;
            if (dto.City != null) e.City = dto.City;
            if (dto.PinCode != null) e.PinCode = dto.PinCode;
            if (dto.Latitude.HasValue) e.Latitude = dto.Latitude;
            if (dto.Longitude.HasValue) e.Longitude = dto.Longitude;
            if (dto.IsActive.HasValue) e.IsActive = dto.IsActive.Value;

            e.UpdatedAt = DateTime.UtcNow;
            e.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return await MapAsync(e);
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy)
        {
            var e = await _db.DeviceRackLocations.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return false;

            e.IsActive = isActive;
            e.UpdatedAt = DateTime.UtcNow;
            e.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<DeviceRackLocationResponseDto> MapAsync(DeviceRackLocation e)
        {
            var deviceName = await _db.DeviceDetails.AsNoTracking()
                .Where(d => d.Id == e.DeviceDetailId).Select(d => d.ShortName).FirstOrDefaultAsync();

            string? rackName = null;
            if (e.RackId.HasValue)
                rackName = await _db.Racks.AsNoTracking()
                    .Where(r => r.Id == e.RackId.Value).Select(r => r.RackName).FirstOrDefaultAsync();

            return Map(e, deviceName, rackName);
        }

        private static DeviceRackLocationResponseDto Map(DeviceRackLocation e, string? deviceName, string? rackName) => new()
        {
            Id = e.Id,
            DeviceDetailId = e.DeviceDetailId,
            DeviceShortName = deviceName,
            RackId = e.RackId,
            RackName = rackName,
            StartUPosition = e.StartUPosition,
            UOccupied = e.UOccupied,
            Address = e.Address,
            Country = e.Country,
            State = e.State,
            City = e.City,
            PinCode = e.PinCode,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}