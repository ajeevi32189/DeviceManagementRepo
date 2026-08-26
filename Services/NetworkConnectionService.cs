using DeviceManagementOnly.Common;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.Device;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  NETWORK CONNECTION — Source aur Target dono DeviceDetail
    //  exist hone chahiye, Source != Target (device khud se cable
    //  connect nahi kar sakta), aur ek device port pe ek hi Active
    //  cable ho sakti hai (source port clash check).
    // ══════════════════════════════════════════════════════════════

    public interface INetworkConnectionService
    {
        Task<NetworkConnectionResponseDto> CreateAsync(NetworkConnectionCreateDto dto, string? createdBy);
        Task<PagedResult<NetworkConnectionResponseDto>> GetAllAsync(NetworkConnectionFilterParams p);
        Task<NetworkConnectionResponseDto?> GetByIdAsync(Guid id);
        Task<NetworkConnectionResponseDto?> UpdateAsync(Guid id, NetworkConnectionUpdateDto dto, string? updatedBy);
    }

    public class NetworkConnectionService : INetworkConnectionService
    {
        private static readonly string[] AllowedStatuses = { "Active", "Inactive", "Faulty" };

        private readonly DBContext _db;
        public NetworkConnectionService(DBContext db) => _db = db;

        private void ValidateStatus(string? status)
        {
            if (status != null && !AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Status '{status}' valid nahi hai. Allowed: {string.Join(", ", AllowedStatuses)}");
        }

        private async Task ValidatePortClashAsync(Guid deviceId, string? portName, Guid? excludeId)
        {
            if (string.IsNullOrWhiteSpace(portName)) return;

            var clash = await _db.NetworkConnections.AsNoTracking().AnyAsync(x =>
                x.Status == "Active" && x.Id != excludeId &&
                ((x.SourceDeviceDetailId == deviceId && x.SourcePortName == portName) ||
                 (x.TargetDeviceDetailId == deviceId && x.TargetPortName == portName)));

            if (clash)
                throw new InvalidOperationException(
                    $"Device={deviceId} ka port '{portName}' pehle se ek active connection me use ho raha hai.");
        }

        public async Task<NetworkConnectionResponseDto> CreateAsync(NetworkConnectionCreateDto dto, string? createdBy)
        {
            if (dto.SourceDeviceDetailId == dto.TargetDeviceDetailId)
                throw new InvalidOperationException("SourceDeviceDetailId aur TargetDeviceDetailId same nahi ho sakte.");

            // ── VALIDATION: dono devices exist ──
            var sourceExists = await _db.DeviceDetails.AsNoTracking().AnyAsync(d => d.Id == dto.SourceDeviceDetailId);
            if (!sourceExists) throw new InvalidOperationException($"Source DeviceDetail Id={dto.SourceDeviceDetailId} nahi mila.");

            var targetExists = await _db.DeviceDetails.AsNoTracking().AnyAsync(d => d.Id == dto.TargetDeviceDetailId);
            if (!targetExists) throw new InvalidOperationException($"Target DeviceDetail Id={dto.TargetDeviceDetailId} nahi mila.");

            ValidateStatus(dto.Status);

            var status = dto.Status ?? "Active";
            if (status == "Active")
            {
                await ValidatePortClashAsync(dto.SourceDeviceDetailId, dto.SourcePortName, null);
                await ValidatePortClashAsync(dto.TargetDeviceDetailId, dto.TargetPortName, null);
            }

            var entity = new NetworkConnection
            {
                SourceDeviceDetailId = dto.SourceDeviceDetailId,
                SourcePortName = dto.SourcePortName,
                TargetDeviceDetailId = dto.TargetDeviceDetailId,
                TargetPortName = dto.TargetPortName,
                CableType = dto.CableType,
                CableColorCode = dto.CableColorCode,
                CableLabel = dto.CableLabel,
                Status = status,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.NetworkConnections.Add(entity);
            await _db.SaveChangesAsync();
            return await MapAsync(entity);
        }

        public async Task<PagedResult<NetworkConnectionResponseDto>> GetAllAsync(NetworkConnectionFilterParams p)
        {
            var query = _db.NetworkConnections.AsNoTracking().AsQueryable();

            // ── FILTER: Source, Target, ya dono side (DeviceDetailId) ──
            if (p.SourceDeviceDetailId.HasValue)
                query = query.Where(x => x.SourceDeviceDetailId == p.SourceDeviceDetailId.Value);
            if (p.TargetDeviceDetailId.HasValue)
                query = query.Where(x => x.TargetDeviceDetailId == p.TargetDeviceDetailId.Value);
            if (p.DeviceDetailId.HasValue)
                query = query.Where(x =>
                    x.SourceDeviceDetailId == p.DeviceDetailId.Value ||
                    x.TargetDeviceDetailId == p.DeviceDetailId.Value);

            // ── FILTER: Status, CableType ──
            if (!string.IsNullOrWhiteSpace(p.Status))
                query = query.Where(x => x.Status == p.Status);
            if (!string.IsNullOrWhiteSpace(p.CableType))
                query = query.Where(x => x.CableType == p.CableType);

            // ── FILTER: Search on CableLabel/PortNames ──
            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x =>
                    (x.CableLabel != null && x.CableLabel.ToLower().Contains(s)) ||
                    (x.SourcePortName != null && x.SourcePortName.ToLower().Contains(s)) ||
                    (x.TargetPortName != null && x.TargetPortName.ToLower().Contains(s)));
            }

            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);

            // ── OPTIMIZATION (N+1 fix): pehle har row ke liye Source + Target
            //    device names ki 2 alag queries lagti thi. Ab dono IDs mila ke
            //    ek hi bulk query se dictionary bana lete hain ──
            var deviceIds = paged.Data
                .SelectMany(x => new[] { x.SourceDeviceDetailId, x.TargetDeviceDetailId })
                .Distinct().ToList();

            var deviceNames = await _db.DeviceDetails.AsNoTracking()
                .Where(d => deviceIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.ShortName);

            var data = paged.Data.Select(e => Map(
                e,
                deviceNames.GetValueOrDefault(e.SourceDeviceDetailId),
                deviceNames.GetValueOrDefault(e.TargetDeviceDetailId))).ToList();

            return PagedResult<NetworkConnectionResponseDto>.Create(data, paged.TotalRecords, p);
        }

        public async Task<NetworkConnectionResponseDto?> GetByIdAsync(Guid id)
        {
            var e = await _db.NetworkConnections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return e is null ? null : await MapAsync(e);
        }

        public async Task<NetworkConnectionResponseDto?> UpdateAsync(Guid id, NetworkConnectionUpdateDto dto, string? updatedBy)
        {
            var e = await _db.NetworkConnections.FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return null;

            var newSource = dto.SourceDeviceDetailId ?? e.SourceDeviceDetailId;
            var newTarget = dto.TargetDeviceDetailId ?? e.TargetDeviceDetailId;

            if (newSource == newTarget)
                throw new InvalidOperationException("SourceDeviceDetailId aur TargetDeviceDetailId same nahi ho sakte.");

            if (dto.SourceDeviceDetailId.HasValue)
            {
                var exists = await _db.DeviceDetails.AnyAsync(d => d.Id == dto.SourceDeviceDetailId.Value);
                if (!exists) throw new InvalidOperationException($"Source DeviceDetail Id={dto.SourceDeviceDetailId} nahi mila.");
            }
            if (dto.TargetDeviceDetailId.HasValue)
            {
                var exists = await _db.DeviceDetails.AnyAsync(d => d.Id == dto.TargetDeviceDetailId.Value);
                if (!exists) throw new InvalidOperationException($"Target DeviceDetail Id={dto.TargetDeviceDetailId} nahi mila.");
            }

            e.SourceDeviceDetailId = newSource;
            e.TargetDeviceDetailId = newTarget;

            if (dto.SourcePortName != null) e.SourcePortName = dto.SourcePortName;
            if (dto.TargetPortName != null) e.TargetPortName = dto.TargetPortName;
            if (dto.CableType != null) e.CableType = dto.CableType;
            if (dto.CableColorCode != null) e.CableColorCode = dto.CableColorCode;
            if (dto.CableLabel != null) e.CableLabel = dto.CableLabel;

            if (dto.Status != null)
            {
                ValidateStatus(dto.Status);
                e.Status = dto.Status;
            }

            if (e.Status == "Active")
            {
                await ValidatePortClashAsync(e.SourceDeviceDetailId, e.SourcePortName, e.Id);
                await ValidatePortClashAsync(e.TargetDeviceDetailId, e.TargetPortName, e.Id);
            }

            e.UpdatedAt = DateTime.UtcNow;
            e.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return await MapAsync(e);
        }

        private async Task<NetworkConnectionResponseDto> MapAsync(NetworkConnection e)
        {
            var sourceName = await _db.DeviceDetails.AsNoTracking()
                .Where(d => d.Id == e.SourceDeviceDetailId).Select(d => d.ShortName).FirstOrDefaultAsync();
            var targetName = await _db.DeviceDetails.AsNoTracking()
                .Where(d => d.Id == e.TargetDeviceDetailId).Select(d => d.ShortName).FirstOrDefaultAsync();

            return Map(e, sourceName, targetName);
        }

        private static NetworkConnectionResponseDto Map(NetworkConnection e, string? sourceName, string? targetName) => new()
        {
            Id = e.Id,
            SourceDeviceDetailId = e.SourceDeviceDetailId,
            SourceDeviceName = sourceName,
            SourcePortName = e.SourcePortName,
            TargetDeviceDetailId = e.TargetDeviceDetailId,
            TargetDeviceName = targetName,
            TargetPortName = e.TargetPortName,
            CableType = e.CableType,
            CableColorCode = e.CableColorCode,
            CableLabel = e.CableLabel,
            Status = e.Status,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}