using DeviceManagementOnly.Common;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.Device;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  RACK POWER CAPACITY — RackId FK validate hota hai.
    //  Business rule: EstimatedLoadKW / MeasuredLoadKW, RatedCapacityKW
    //  se zyada nahi hone chahiye (over-provisioning warning bache).
    // ══════════════════════════════════════════════════════════════

    public interface IRackPowerCapacityService
    {
        Task<RackPowerCapacityResponseDto> CreateAsync(RackPowerCapacityCreateDto dto, string? createdBy);
        Task<PagedResult<RackPowerCapacityResponseDto>> GetAllAsync(RackPowerCapacityFilterParams p);
        Task<RackPowerCapacityResponseDto?> GetByIdAsync(Guid id);
        Task<RackPowerCapacityResponseDto?> UpdateAsync(Guid id, RackPowerCapacityUpdateDto dto, string? updatedBy);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy);
    }

    public class RackPowerCapacityService : IRackPowerCapacityService
    {
        private readonly DBContext _db;
        public RackPowerCapacityService(DBContext db) => _db = db;

        private void ValidateLoad(decimal? rated, decimal? estimated, decimal? measured)
        {
            if (rated.HasValue)
            {
                if (estimated.HasValue && estimated.Value > rated.Value)
                    throw new InvalidOperationException($"EstimatedLoadKW ({estimated}) RatedCapacityKW ({rated}) se zyada nahi ho sakta.");
                if (measured.HasValue && measured.Value > rated.Value)
                    throw new InvalidOperationException($"MeasuredLoadKW ({measured}) RatedCapacityKW ({rated}) se zyada nahi ho sakta.");
            }
        }

        public async Task<RackPowerCapacityResponseDto> CreateAsync(RackPowerCapacityCreateDto dto, string? createdBy)
        {
            // ── VALIDATION: RackId exist ──
            var rack = await _db.Racks.AsNoTracking().FirstOrDefaultAsync(r => r.Id == dto.RackId);
            if (rack is null)
                throw new InvalidOperationException($"Rack Id={dto.RackId} nahi mila.");

            ValidateLoad(dto.RatedCapacityKW, dto.EstimatedLoadKW, dto.MeasuredLoadKW);

            var entity = new RackPowerCapacity
            {
                RackId = dto.RackId,
                PDUName = dto.PDUName,
                PhaseName = dto.PhaseName,
                RatedCapacityKW = dto.RatedCapacityKW,
                EstimatedLoadKW = dto.EstimatedLoadKW,
                MeasuredLoadKW = dto.MeasuredLoadKW,
                FailoverReservedKW = dto.FailoverReservedKW,
                LastSyncedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.RackPowerCapacities.Add(entity);
            await _db.SaveChangesAsync();
            return await MapAsync(entity);
        }

        public async Task<PagedResult<RackPowerCapacityResponseDto>> GetAllAsync(RackPowerCapacityFilterParams p)
        {
            var query = _db.RackPowerCapacities.AsNoTracking().Where(x => x.IsActive);

            // ── FILTER: RackId ──
            if (p.RackId.HasValue)
                query = query.Where(x => x.RackId == p.RackId.Value);

            // ── FILTER: Search on PDUName/PhaseName ──
            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x =>
                    (x.PDUName != null && x.PDUName.ToLower().Contains(s)) ||
                    (x.PhaseName != null && x.PhaseName.ToLower().Contains(s)));
            }

            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);

            // ── OPTIMIZATION (N+1 fix): ek hi bulk query se saare Rack names ──
            var rackIds = paged.Data.Select(x => x.RackId).Distinct().ToList();
            var rackNames = await _db.Racks.AsNoTracking()
                .Where(r => rackIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.RackName);

            var data = paged.Data.Select(e => Map(e, rackNames.GetValueOrDefault(e.RackId))).ToList();
            return PagedResult<RackPowerCapacityResponseDto>.Create(data, paged.TotalRecords, p);
        }

        public async Task<RackPowerCapacityResponseDto?> GetByIdAsync(Guid id)
        {
            var e = await _db.RackPowerCapacities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return e is null ? null : await MapAsync(e);
        }

        public async Task<RackPowerCapacityResponseDto?> UpdateAsync(Guid id, RackPowerCapacityUpdateDto dto, string? updatedBy)
        {
            var e = await _db.RackPowerCapacities.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return null;

            if (dto.RackId.HasValue)
            {
                var rackExists = await _db.Racks.AnyAsync(r => r.Id == dto.RackId.Value);
                if (!rackExists) throw new InvalidOperationException($"Rack Id={dto.RackId} nahi mila.");
                e.RackId = dto.RackId.Value;
            }

            if (dto.PDUName != null) e.PDUName = dto.PDUName;
            if (dto.PhaseName != null) e.PhaseName = dto.PhaseName;
            if (dto.RatedCapacityKW.HasValue) e.RatedCapacityKW = dto.RatedCapacityKW;
            if (dto.EstimatedLoadKW.HasValue) e.EstimatedLoadKW = dto.EstimatedLoadKW;
            if (dto.MeasuredLoadKW.HasValue) e.MeasuredLoadKW = dto.MeasuredLoadKW;
            if (dto.FailoverReservedKW.HasValue) e.FailoverReservedKW = dto.FailoverReservedKW;

            ValidateLoad(e.RatedCapacityKW, e.EstimatedLoadKW, e.MeasuredLoadKW);

            if (dto.IsActive.HasValue) e.IsActive = dto.IsActive.Value;

            e.LastSyncedAt = DateTime.UtcNow;
            e.UpdatedAt = DateTime.UtcNow;
            e.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return await MapAsync(e);
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy)
        {
            var e = await _db.RackPowerCapacities.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return false;

            e.IsActive = isActive;
            e.UpdatedAt = DateTime.UtcNow;
            e.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<RackPowerCapacityResponseDto> MapAsync(RackPowerCapacity e)
        {
            var rackName = await _db.Racks.AsNoTracking()
                .Where(r => r.Id == e.RackId).Select(r => r.RackName).FirstOrDefaultAsync();
            return Map(e, rackName);
        }

        private static RackPowerCapacityResponseDto Map(RackPowerCapacity e, string? rackName) => new()
        {
            Id = e.Id,
            RackId = e.RackId,
            RackName = rackName,
            PDUName = e.PDUName,
            PhaseName = e.PhaseName,
            RatedCapacityKW = e.RatedCapacityKW,
            EstimatedLoadKW = e.EstimatedLoadKW,
            MeasuredLoadKW = e.MeasuredLoadKW,
            FailoverReservedKW = e.FailoverReservedKW,
            LastSyncedAt = e.LastSyncedAt,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}