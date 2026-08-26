using DeviceManagementOnly.Common;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.Device;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  DEVICE CONNECTION CONFIG — DeviceDetailId FK validate hota hai,
    //  Protocol fixed list se check hota hai, aur ek DeviceDetail ke
    //  liye same (IPAddress + Port) ka duplicate active config nahi
    //  ban sakta (conflict/clash avoid karne ke liye).
    // ══════════════════════════════════════════════════════════════

    public interface IDeviceConnectionConfigService
    {
        Task<DeviceConnectionConfigResponseDto> CreateAsync(DeviceConnectionConfigCreateDto dto, string? createdBy);
        Task<PagedResult<DeviceConnectionConfigResponseDto>> GetAllAsync(DeviceConnectionConfigFilterParams p);
        Task<DeviceConnectionConfigResponseDto?> GetByIdAsync(Guid id);
        Task<DeviceConnectionConfigResponseDto?> UpdateAsync(Guid id, DeviceConnectionConfigUpdateDto dto, string? updatedBy);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy);
    }

    public class DeviceConnectionConfigService : IDeviceConnectionConfigService
    {
        private static readonly string[] AllowedProtocols =
            { "Modbus", "BACnet", "MQTT", "SNMP", "OPCUA", "HTTP", "ThingsBoard" };

        private readonly DBContext _db;
        public DeviceConnectionConfigService(DBContext db) => _db = db;

        private void ValidateProtocol(string protocol)
        {
            if (!AllowedProtocols.Contains(protocol, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Protocol '{protocol}' valid nahi hai. Allowed: {string.Join(", ", AllowedProtocols)}");
        }

        private async Task ValidateNoDuplicateEndpointAsync(Guid deviceDetailId, string? ip, int? port, Guid? excludeId)
        {
            if (string.IsNullOrWhiteSpace(ip) || !port.HasValue) return;

            var clash = await _db.DeviceConnectionConfigs.AsNoTracking().AnyAsync(x =>
                x.DeviceDetailId == deviceDetailId &&
                x.IsActive &&
                x.IPAddress == ip &&
                x.Port == port.Value &&
                x.Id != excludeId);

            if (clash)
                throw new InvalidOperationException(
                    $"Is device ke liye IPAddress={ip}, Port={port} wala connection config pehle se maujood hai.");
        }

        public async Task<DeviceConnectionConfigResponseDto> CreateAsync(DeviceConnectionConfigCreateDto dto, string? createdBy)
        {
            // ── VALIDATION: DeviceDetail exist ──
            var deviceExists = await _db.DeviceDetails.AsNoTracking().AnyAsync(d => d.Id == dto.DeviceDetailId);
            if (!deviceExists)
                throw new InvalidOperationException($"DeviceDetail Id={dto.DeviceDetailId} nahi mila.");

            ValidateProtocol(dto.Protocol);
            await ValidateNoDuplicateEndpointAsync(dto.DeviceDetailId, dto.IPAddress, dto.Port, null);

            var entity = new DeviceConnectionConfig
            {
                DeviceDetailId = dto.DeviceDetailId,
                Protocol = dto.Protocol,
                IPAddress = dto.IPAddress,
                Port = dto.Port,
                ObjectId = dto.ObjectId,
                UnitId = dto.UnitId,
                PollingIntervalSeconds = dto.PollingIntervalSeconds,
                ThingsBoardDeviceId = dto.ThingsBoardDeviceId,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.DeviceConnectionConfigs.Add(entity);
            await _db.SaveChangesAsync();
            return await MapAsync(entity);
        }

        public async Task<PagedResult<DeviceConnectionConfigResponseDto>> GetAllAsync(DeviceConnectionConfigFilterParams p)
        {
            var query = _db.DeviceConnectionConfigs.AsNoTracking().AsQueryable();

            // ── FILTER: IsActive (default true, lekin explicitly false bhi maang sakte hain) ──
            query = p.IsActive.HasValue
                ? query.Where(x => x.IsActive == p.IsActive.Value)
                : query.Where(x => x.IsActive);

            // ── FILTER: DeviceDetailId, Protocol ──
            if (p.DeviceDetailId.HasValue)
                query = query.Where(x => x.DeviceDetailId == p.DeviceDetailId.Value);
            if (!string.IsNullOrWhiteSpace(p.Protocol))
                query = query.Where(x => x.Protocol == p.Protocol);

            // ── FILTER: Search on IPAddress/ObjectId/ThingsBoardDeviceId ──
            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x =>
                    (x.IPAddress != null && x.IPAddress.ToLower().Contains(s)) ||
                    (x.ObjectId != null && x.ObjectId.ToLower().Contains(s)) ||
                    (x.ThingsBoardDeviceId != null && x.ThingsBoardDeviceId.ToLower().Contains(s)));
            }

            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);

            // ── OPTIMIZATION (N+1 fix): ek hi bulk query se saare Device names ──
            var deviceIds = paged.Data.Select(x => x.DeviceDetailId).Distinct().ToList();
            var deviceNames = await _db.DeviceDetails.AsNoTracking()
                .Where(d => deviceIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.ShortName);

            var data = paged.Data.Select(e => Map(e, deviceNames.GetValueOrDefault(e.DeviceDetailId))).ToList();
            return PagedResult<DeviceConnectionConfigResponseDto>.Create(data, paged.TotalRecords, p);
        }

        public async Task<DeviceConnectionConfigResponseDto?> GetByIdAsync(Guid id)
        {
            var e = await _db.DeviceConnectionConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return e is null ? null : await MapAsync(e);
        }

        public async Task<DeviceConnectionConfigResponseDto?> UpdateAsync(Guid id, DeviceConnectionConfigUpdateDto dto, string? updatedBy)
        {
            var e = await _db.DeviceConnectionConfigs.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return null;

            if (dto.DeviceDetailId.HasValue)
            {
                var deviceExists = await _db.DeviceDetails.AnyAsync(d => d.Id == dto.DeviceDetailId.Value);
                if (!deviceExists) throw new InvalidOperationException($"DeviceDetail Id={dto.DeviceDetailId} nahi mila.");
                e.DeviceDetailId = dto.DeviceDetailId.Value;
            }

            if (dto.Protocol != null)
            {
                ValidateProtocol(dto.Protocol);
                e.Protocol = dto.Protocol;
            }

            if (dto.IPAddress != null) e.IPAddress = dto.IPAddress;
            if (dto.Port.HasValue) e.Port = dto.Port;

            await ValidateNoDuplicateEndpointAsync(e.DeviceDetailId, e.IPAddress, e.Port, e.Id);

            if (dto.ObjectId != null) e.ObjectId = dto.ObjectId;
            if (dto.UnitId != null) e.UnitId = dto.UnitId;
            if (dto.PollingIntervalSeconds.HasValue) e.PollingIntervalSeconds = dto.PollingIntervalSeconds.Value;
            if (dto.ThingsBoardDeviceId != null) e.ThingsBoardDeviceId = dto.ThingsBoardDeviceId;
            if (dto.IsActive.HasValue) e.IsActive = dto.IsActive.Value;

            e.UpdatedAt = DateTime.UtcNow;
            e.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return await MapAsync(e);
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy)
        {
            var e = await _db.DeviceConnectionConfigs.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return false;

            e.IsActive = isActive;
            e.UpdatedAt = DateTime.UtcNow;
            e.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<DeviceConnectionConfigResponseDto> MapAsync(DeviceConnectionConfig e)
        {
            var deviceName = await _db.DeviceDetails.AsNoTracking()
                .Where(d => d.Id == e.DeviceDetailId).Select(d => d.ShortName).FirstOrDefaultAsync();
            return Map(e, deviceName);
        }

        private static DeviceConnectionConfigResponseDto Map(DeviceConnectionConfig e, string? deviceName) => new()
        {
            Id = e.Id,
            DeviceDetailId = e.DeviceDetailId,
            DeviceShortName = deviceName,
            Protocol = e.Protocol,
            IPAddress = e.IPAddress,
            Port = e.Port,
            ObjectId = e.ObjectId,
            UnitId = e.UnitId,
            PollingIntervalSeconds = e.PollingIntervalSeconds,
            ThingsBoardDeviceId = e.ThingsBoardDeviceId,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}