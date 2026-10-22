using DeviceManagement.Data;
using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.Device;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  DATA CENTRE — 5D View ka sabse bahar wala container.
    //
    //  Pehle ye poora frontend me static pada tha (AddDataCenter.jsx).
    //  Building ki lambai-chaudai, margin, door ki disha — 3D ki
    //  chaar deewar inhi se banti hain, isliye DB me aana zaroori tha.
    //
    //  Naap: user kisi bhi unit me de (m/ft/mm/yd), hum hamesha METRES
    //  me store karte hain. Unit sirf isliye rakhte hain ki user ko
    //  wahi dobara dikhe jo usne daala tha.
    // ══════════════════════════════════════════════════════════════

    public interface IDataCenterService
    {
        Task<DataCenterResponseDto> CreateAsync(DataCenterCreateDto dto, string? user);
        Task<PagedResult<DataCenterResponseDto>> GetAllAsync(DataCenterFilterParams p);
        Task<DataCenterResponseDto?> GetByIdAsync(Guid id);
        Task<DataCenterResponseDto?> UpdateAsync(Guid id, DataCenterUpdateDto dto, string? user);
        Task<bool> SetActiveAsync(Guid id, bool isActive, string? user);
    }

    public class DataCenterService : IDataCenterService
    {
        private readonly DBContext _db;
        public DataCenterService(DBContext db) => _db = db;

        public async Task<DataCenterResponseDto> CreateAsync(DataCenterCreateDto dto, string? user)
        {
            var code = dto.Code.Trim();
            var clash = await _db.DataCenters.AsNoTracking()
                .AnyAsync(d => d.Code == code && !d.IsDeleted);
            if (clash)
                throw new InvalidOperationException($"Data centre code '{code}' pehle se maujood hai.");

            var d = new DataCenter
            {
                Name = dto.Name.Trim(),
                Code = code,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };
            Apply(d, dto);
            _db.DataCenters.Add(d);
            await _db.SaveChangesAsync();
            return await MapAsync(d);
        }

        public async Task<PagedResult<DataCenterResponseDto>> GetAllAsync(DataCenterFilterParams p)
        {
            var q = _db.DataCenters.AsNoTracking().Where(d => !d.IsDeleted && d.IsActive);

            if (p.SiteId.HasValue) q = q.Where(d => d.SiteId == p.SiteId);
            if (p.TenantId.HasValue) q = q.Where(d => d.TenantId == p.TenantId);
            if (p.CountryId.HasValue) q = q.Where(d => d.CountryId == p.CountryId);
            if (!string.IsNullOrWhiteSpace(p.Status)) q = q.Where(d => d.Status == p.Status);

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                q = q.Where(d => d.Name.ToLower().Contains(s)
                              || d.Code.ToLower().Contains(s)
                              || (d.LocationName != null && d.LocationName.ToLower().Contains(s)));
            }

            q = q.OrderBy(d => d.Name);
            var paged = await q.ToPagedAsync(p);

            var ids = paged.Data.Select(d => d.Id).ToList();
            var floorCounts = await _db.Floors.AsNoTracking()
                .Where(f => f.DataCenterId != null && ids.Contains(f.DataCenterId.Value))
                .GroupBy(f => f.DataCenterId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var data = paged.Data.Select(d => Map(d, floorCounts.GetValueOrDefault(d.Id))).ToList();
            return PagedResult<DataCenterResponseDto>.Create(data, paged.TotalRecords, p);
        }

        public async Task<DataCenterResponseDto?> GetByIdAsync(Guid id)
        {
            var d = await _db.DataCenters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return d is null ? null : await MapAsync(d);
        }

        public async Task<DataCenterResponseDto?> UpdateAsync(Guid id, DataCenterUpdateDto dto, string? user)
        {
            var d = await _db.DataCenters.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (d is null) return null;

            var code = dto.Code.Trim();
            if (!string.Equals(code, d.Code, StringComparison.OrdinalIgnoreCase))
            {
                var clash = await _db.DataCenters.AsNoTracking()
                    .AnyAsync(x => x.Code == code && x.Id != id && !x.IsDeleted);
                if (clash)
                    throw new InvalidOperationException($"Data centre code '{code}' pehle se maujood hai.");
            }

            d.Name = dto.Name.Trim();
            d.Code = code;
            Apply(d, dto);
            if (dto.IsActive.HasValue) d.IsActive = dto.IsActive.Value;
            d.UpdatedAt = DateTime.UtcNow;
            d.UpdatedBy = user;

            await _db.SaveChangesAsync();
            return await MapAsync(d);
        }

        public async Task<bool> SetActiveAsync(Guid id, bool isActive, string? user)
        {
            var d = await _db.DataCenters.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (d is null) return false;
            d.IsActive = isActive;
            d.UpdatedAt = DateTime.UtcNow;
            d.UpdatedBy = user;
            await _db.SaveChangesAsync();
            return true;
        }

        // ── helpers ────────────────────────────────────────────────
        private static void Apply(DataCenter d, DataCenterCreateDto s)
        {
            d.LengthM = s.LengthM; d.BreadthM = s.BreadthM; d.HeightM = s.HeightM;
            d.LengthUnit = s.LengthUnit ?? "m"; d.BreadthUnit = s.BreadthUnit ?? "m";
            d.MarginTop = s.MarginTop; d.MarginBottom = s.MarginBottom;
            d.MarginLeft = s.MarginLeft; d.MarginRight = s.MarginRight;
            d.MarginUnit = s.MarginUnit ?? "m";
            d.DoorDirection = s.DoorDirection; d.DoorPosition = s.DoorPosition;
            d.TotalKW = s.TotalKW; d.TotalKWUnit = s.TotalKWUnit ?? "kW";
            d.PowerCapacityKW = s.PowerCapacityKW; d.CoolingCapacityKW = s.CoolingCapacityKW;
            d.TotalRacks = s.TotalRacks; d.UtilizedRacks = s.UtilizedRacks;
            d.LocationName = s.LocationName; d.StreetAddress = s.StreetAddress; d.PinCode = s.PinCode;
            d.CountryId = s.CountryId; d.StateId = s.StateId; d.CityId = s.CityId;
            d.SiteId = s.SiteId; d.TenantId = s.TenantId; d.ContactPersonId = s.ContactPersonId;
            d.Latitude = s.Latitude; d.Longitude = s.Longitude;
            d.Status = string.IsNullOrWhiteSpace(s.Status) ? d.Status : s.Status!;
            d.SetupMode = s.SetupMode ?? d.SetupMode;
            d.DxfFileId = s.DxfFileId ?? d.DxfFileId;
        }

        private async Task<DataCenterResponseDto> MapAsync(DataCenter d)
        {
            var count = await _db.Floors.AsNoTracking().CountAsync(f => f.DataCenterId == d.Id);
            return Map(d, count);
        }

        private static DataCenterResponseDto Map(DataCenter d, int floorCount) => new()
        {
            Id = d.Id, Name = d.Name, Code = d.Code,
            LengthM = d.LengthM, BreadthM = d.BreadthM, HeightM = d.HeightM,
            LengthUnit = d.LengthUnit, BreadthUnit = d.BreadthUnit,
            MarginTop = d.MarginTop, MarginBottom = d.MarginBottom,
            MarginLeft = d.MarginLeft, MarginRight = d.MarginRight, MarginUnit = d.MarginUnit,
            DoorDirection = d.DoorDirection, DoorPosition = d.DoorPosition,
            TotalKW = d.TotalKW, TotalKWUnit = d.TotalKWUnit,
            PowerCapacityKW = d.PowerCapacityKW, CoolingCapacityKW = d.CoolingCapacityKW,
            TotalRacks = d.TotalRacks, UtilizedRacks = d.UtilizedRacks,
            LocationName = d.LocationName, StreetAddress = d.StreetAddress, PinCode = d.PinCode,
            CountryId = d.CountryId, StateId = d.StateId, CityId = d.CityId,
            SiteId = d.SiteId, TenantId = d.TenantId, ContactPersonId = d.ContactPersonId,
            Latitude = d.Latitude, Longitude = d.Longitude,
            Status = d.Status, SetupMode = d.SetupMode, DxfFileId = d.DxfFileId,
            IsActive = d.IsActive, FloorCount = floorCount,
            CreatedAt = d.CreatedAt, UpdatedAt = d.UpdatedAt
        };
    }
}
