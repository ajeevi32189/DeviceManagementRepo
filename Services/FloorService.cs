using DeviceManagementOnly.Common;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.Device;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  FLOOR — root of Floor → Room → Rack chain. Koi parent FK nahi,
    //  isliye sirf PaginationParams.Search (Name/SiteName pe) filter hai.
    // ══════════════════════════════════════════════════════════════

    public interface IFloorService
    {
        Task<FloorResponseDto> CreateAsync(FloorCreateDto dto, string? createdBy);
        Task<PagedResult<FloorResponseDto>> GetAllAsync(PaginationParams p);
        Task<FloorResponseDto?> GetByIdAsync(Guid id);
        Task<FloorResponseDto?> UpdateAsync(Guid id, FloorUpdateDto dto, string? updatedBy);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy);
    }

    public class FloorService : IFloorService
    {
        private readonly DBContext _db;
        public FloorService(DBContext db) => _db = db;

        public async Task<FloorResponseDto> CreateAsync(FloorCreateDto dto, string? createdBy)
        {
            var floor = new Floor
            {
                Name = dto.Name,
                SiteName = dto.SiteName,
                FloorPlanFileUrl = dto.FloorPlanFileUrl,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.Floors.Add(floor);
            await _db.SaveChangesAsync();
            return Map(floor);
        }

        public async Task<PagedResult<FloorResponseDto>> GetAllAsync(PaginationParams p)
        {
            // ── FILTER: sirf active floors, aur Search Name/SiteName dono pe ──
            var query = _db.Floors.AsNoTracking().Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(s) ||
                    (x.SiteName != null && x.SiteName.ToLower().Contains(s)));
            }

            query = query.OrderBy(x => x.Name);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<FloorResponseDto>.Create(paged.Data.Select(Map), paged.TotalRecords, p);
        }

        public async Task<FloorResponseDto?> GetByIdAsync(Guid id)
        {
            var floor = await _db.Floors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return floor is null ? null : Map(floor);
        }

        public async Task<FloorResponseDto?> UpdateAsync(Guid id, FloorUpdateDto dto, string? updatedBy)
        {
            var floor = await _db.Floors.FirstOrDefaultAsync(x => x.Id == id);
            if (floor is null) return null;

            if (dto.Name != null) floor.Name = dto.Name;
            if (dto.SiteName != null) floor.SiteName = dto.SiteName;
            if (dto.FloorPlanFileUrl != null) floor.FloorPlanFileUrl = dto.FloorPlanFileUrl;
            if (dto.IsActive.HasValue) floor.IsActive = dto.IsActive.Value;

            floor.UpdatedAt = DateTime.UtcNow;
            floor.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return Map(floor);
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy)
        {
            var floor = await _db.Floors.FirstOrDefaultAsync(x => x.Id == id);
            if (floor is null) return false;

            floor.IsActive = isActive;
            floor.UpdatedAt = DateTime.UtcNow;
            floor.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return true;
        }

        private static FloorResponseDto Map(Floor f) => new()
        {
            Id = f.Id,
            Name = f.Name,
            SiteName = f.SiteName,
            FloorPlanFileUrl = f.FloorPlanFileUrl,
            IsActive = f.IsActive,
            CreatedAt = f.CreatedAt,
            UpdatedAt = f.UpdatedAt
        };
    }
}
