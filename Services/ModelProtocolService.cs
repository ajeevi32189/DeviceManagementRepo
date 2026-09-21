using DeviceManagementOnly.Common;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.Device;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  MODEL PROTOCOL SERVICE
    //  - ModelProtocolProfile / ModelProtocolPoint CRUD (soft-delete)
    //  - Uniqueness of (ModelSpecificationId, Protocol) and (ProfileId, PointKey)
    //    is enforced HERE, not by a DB unique index — a soft-deleted row still
    //    occupies the key in MySQL (no filtered/partial unique index support),
    //    so we only check among rows where IsDeleted == false.
    //  - Deleting a profile soft-deletes its points too (DB-level ON DELETE
    //    CASCADE only fires on a real DELETE statement, not a soft-delete flag).
    //  - Discovery-target / discovery-status endpoints for ProtocolService,
    //    matching the spec's "if option 1 goes the API route instead" design.
    //  - Protocol authority: DeviceMaster.DataFormat (NOT DeviceType.Protocol —
    //    see comments on those models).
    // ══════════════════════════════════════════════════════════════

    public interface IModelProtocolService
    {
        // Profiles
        Task<ModelProtocolProfileResponseDto> CreateProfileAsync(ModelProtocolProfileCreateDto dto, string? createdBy);
        Task<PagedResult<ModelProtocolProfileResponseDto>> GetAllProfilesAsync(ModelProtocolProfileFilterParams p);
        Task<ModelProtocolProfileResponseDto?> GetProfileByIdAsync(Guid id);
        Task<ModelProtocolProfileResponseDto?> GetProfileByModelAndProtocolAsync(Guid modelSpecificationId, string protocol);
        Task<ModelProtocolProfileResponseDto?> UpdateProfileAsync(Guid id, ModelProtocolProfileUpdateDto dto, string? updatedBy);
        Task<bool> DeleteProfileAsync(Guid id, string? deletedBy);

        // Points
        Task<ModelProtocolPointResponseDto> AddPointAsync(Guid profileId, ModelProtocolPointCreateDto dto);
        Task<ModelProtocolPointResponseDto?> UpdatePointAsync(Guid pointId, ModelProtocolPointUpdateDto dto);
        Task<bool> DeletePointAsync(Guid pointId, string? deletedBy);

        // ProtocolService-facing
        Task<PagedResult<DiscoveryTargetDto>> GetDiscoveryTargetsAsync(DiscoveryTargetFilterParams p);
        Task<bool> UpdateDiscoveryStatusAsync(Guid deviceDetailId, DiscoveryStatusUpdateDto dto);
    }

    public class ModelProtocolService : IModelProtocolService
    {
        private static readonly string[] AllowedProtocols = { "Modbus", "SNMP", "BACnet" };

        private readonly DBContext _db;
        public ModelProtocolService(DBContext db) => _db = db;

        private static void ValidateProtocol(string protocol)
        {
            if (!AllowedProtocols.Contains(protocol, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Protocol '{protocol}' valid nahi hai. Allowed: {string.Join(", ", AllowedProtocols)}");
        }

        // ───────────────────────── PROFILES ─────────────────────────

        public async Task<ModelProtocolProfileResponseDto> CreateProfileAsync(ModelProtocolProfileCreateDto dto, string? createdBy)
        {
            ValidateProtocol(dto.Protocol);

            var modelExists = await _db.ModelSpecifications.AsNoTracking()
                .AnyAsync(m => m.Id == dto.ModelSpecificationId && !m.IsDeleted);
            if (!modelExists)
                throw new InvalidOperationException($"ModelSpecification Id={dto.ModelSpecificationId} nahi mila.");

            var duplicate = await _db.ModelProtocolProfiles.AsNoTracking().AnyAsync(x =>
                x.ModelSpecificationId == dto.ModelSpecificationId &&
                x.Protocol == dto.Protocol &&
                !x.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException(
                    $"Is model ke liye '{dto.Protocol}' protocol ka profile pehle se maujood hai.");

            var entity = new ModelProtocolProfile
            {
                ModelSpecificationId = dto.ModelSpecificationId,
                Protocol = dto.Protocol,
                DefaultPort = dto.DefaultPort,
                DefaultUnitId = dto.DefaultUnitId,
                FunctionCode = dto.FunctionCode,
                WordOrder = dto.WordOrder,
                ByteOrder = dto.ByteOrder,
                AddressBase = dto.AddressBase,
                PollPeriodMs = dto.PollPeriodMs,
                Source = dto.Source,
                IsVerified = dto.IsVerified,
                VerifiedAt = dto.IsVerified ? DateTime.UtcNow : null,
                VerifiedBy = dto.IsVerified ? dto.VerifiedBy : null,
                Remarks = dto.Remarks,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            if (dto.Points is { Count: > 0 })
            {
                var keys = dto.Points.Select(p => p.PointKey.Trim().ToLower()).ToList();
                var dupKeys = keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                if (dupKeys.Count > 0)
                    throw new InvalidOperationException($"Duplicate PointKey isi request me: {string.Join(", ", dupKeys)}");

                foreach (var p in dto.Points)
                {
                    entity.Points.Add(new ModelProtocolPoint
                    {
                        PointKey = p.PointKey,
                        DisplayName = p.DisplayName,
                        Address = p.Address,
                        FunctionCode = p.FunctionCode,
                        RegisterCount = p.RegisterCount ?? 1,
                        DataType = p.DataType,
                        Multiplier = p.Multiplier ?? 1d,
                        Unit = p.Unit,
                        Category = p.Category,
                        Confidence = p.Confidence ?? "confirmed",
                        IsEnabled = p.IsEnabled,
                        SortOrder = p.SortOrder,
                        Remarks = p.Remarks,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            _db.ModelProtocolProfiles.Add(entity);
            await _db.SaveChangesAsync();
            return await MapProfileAsync(entity.Id) ?? throw new InvalidOperationException("Profile save ke baad read nahi ho paya.");
        }

        public async Task<PagedResult<ModelProtocolProfileResponseDto>> GetAllProfilesAsync(ModelProtocolProfileFilterParams p)
        {
            var query = _db.ModelProtocolProfiles.AsNoTracking().Where(x => !x.IsDeleted);

            if (p.ModelSpecificationId.HasValue)
                query = query.Where(x => x.ModelSpecificationId == p.ModelSpecificationId.Value);
            if (!string.IsNullOrWhiteSpace(p.Protocol))
                query = query.Where(x => x.Protocol == p.Protocol);
            if (p.IsVerified.HasValue)
                query = query.Where(x => x.IsVerified == p.IsVerified.Value);

            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);

            var ids = paged.Data.Select(x => x.Id).ToList();
            var data = new List<ModelProtocolProfileResponseDto>();
            foreach (var id in ids)
            {
                var mapped = await MapProfileAsync(id);
                if (mapped != null) data.Add(mapped);
            }

            return PagedResult<ModelProtocolProfileResponseDto>.Create(data, paged.TotalRecords, p);
        }

        public async Task<ModelProtocolProfileResponseDto?> GetProfileByIdAsync(Guid id) => await MapProfileAsync(id);

        public async Task<ModelProtocolProfileResponseDto?> GetProfileByModelAndProtocolAsync(Guid modelSpecificationId, string protocol)
        {
            var entity = await _db.ModelProtocolProfiles.AsNoTracking().FirstOrDefaultAsync(x =>
                x.ModelSpecificationId == modelSpecificationId &&
                x.Protocol == protocol &&
                !x.IsDeleted);

            return entity == null ? null : await MapProfileAsync(entity.Id);
        }

        public async Task<ModelProtocolProfileResponseDto?> UpdateProfileAsync(Guid id, ModelProtocolProfileUpdateDto dto, string? updatedBy)
        {
            var e = await _db.ModelProtocolProfiles.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (e is null) return null;

            if (dto.Protocol != null)
            {
                ValidateProtocol(dto.Protocol);
                var duplicate = await _db.ModelProtocolProfiles.AsNoTracking().AnyAsync(x =>
                    x.ModelSpecificationId == e.ModelSpecificationId &&
                    x.Protocol == dto.Protocol &&
                    x.Id != id &&
                    !x.IsDeleted);
                if (duplicate)
                    throw new InvalidOperationException($"Is model ke liye '{dto.Protocol}' protocol ka profile pehle se maujood hai.");
                e.Protocol = dto.Protocol;
            }

            if (dto.DefaultPort.HasValue) e.DefaultPort = dto.DefaultPort;
            if (dto.DefaultUnitId.HasValue) e.DefaultUnitId = dto.DefaultUnitId;
            if (dto.FunctionCode.HasValue) e.FunctionCode = dto.FunctionCode;
            if (dto.WordOrder != null) e.WordOrder = dto.WordOrder;
            if (dto.ByteOrder != null) e.ByteOrder = dto.ByteOrder;
            if (dto.AddressBase.HasValue) e.AddressBase = dto.AddressBase;
            if (dto.PollPeriodMs.HasValue) e.PollPeriodMs = dto.PollPeriodMs;
            if (dto.Source != null) e.Source = dto.Source;
            if (dto.Remarks != null) e.Remarks = dto.Remarks;
            if (dto.VerifiedBy != null) e.VerifiedBy = dto.VerifiedBy;

            if (dto.IsVerified.HasValue && dto.IsVerified.Value != e.IsVerified)
            {
                e.IsVerified = dto.IsVerified.Value;
                e.VerifiedAt = dto.IsVerified.Value ? DateTime.UtcNow : null;
            }

            e.UpdatedAt = DateTime.UtcNow;
            e.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return await MapProfileAsync(e.Id);
        }

        public async Task<bool> DeleteProfileAsync(Guid id, string? deletedBy)
        {
            var e = await _db.ModelProtocolProfiles.Include(x => x.Points)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (e is null) return false;

            var now = DateTime.UtcNow;
            e.IsDeleted = true;
            e.DeletedAt = now;
            e.DeletedBy = deletedBy;

            // Soft-delete cascade — DB ON DELETE CASCADE only fires on a real DELETE,
            // so we mirror it here manually for the soft-delete flag.
            foreach (var point in e.Points.Where(pt => !pt.IsDeleted))
            {
                point.IsDeleted = true;
                point.DeletedAt = now;
                point.DeletedBy = deletedBy;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        // ───────────────────────── POINTS ─────────────────────────

        public async Task<ModelProtocolPointResponseDto> AddPointAsync(Guid profileId, ModelProtocolPointCreateDto dto)
        {
            var profileExists = await _db.ModelProtocolProfiles.AsNoTracking()
                .AnyAsync(x => x.Id == profileId && !x.IsDeleted);
            if (!profileExists)
                throw new InvalidOperationException($"ModelProtocolProfile Id={profileId} nahi mila.");

            var duplicate = await _db.ModelProtocolPoints.AsNoTracking().AnyAsync(x =>
                x.ProfileId == profileId &&
                x.PointKey == dto.PointKey &&
                !x.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException($"Is profile me PointKey '{dto.PointKey}' pehle se maujood hai.");

            var entity = new ModelProtocolPoint
            {
                ProfileId = profileId,
                PointKey = dto.PointKey,
                DisplayName = dto.DisplayName,
                Address = dto.Address,
                FunctionCode = dto.FunctionCode,
                RegisterCount = dto.RegisterCount ?? 1,
                DataType = dto.DataType,
                Multiplier = dto.Multiplier ?? 1d,
                Unit = dto.Unit,
                Category = dto.Category,
                Confidence = dto.Confidence ?? "confirmed",
                IsEnabled = dto.IsEnabled,
                SortOrder = dto.SortOrder,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow
            };

            _db.ModelProtocolPoints.Add(entity);
            await _db.SaveChangesAsync();
            return MapPoint(entity);
        }

        public async Task<ModelProtocolPointResponseDto?> UpdatePointAsync(Guid pointId, ModelProtocolPointUpdateDto dto)
        {
            var e = await _db.ModelProtocolPoints.FirstOrDefaultAsync(x => x.Id == pointId && !x.IsDeleted);
            if (e is null) return null;

            if (dto.DisplayName != null) e.DisplayName = dto.DisplayName;
            if (dto.Address != null) e.Address = dto.Address;
            if (dto.FunctionCode.HasValue) e.FunctionCode = dto.FunctionCode;
            if (dto.RegisterCount.HasValue) e.RegisterCount = dto.RegisterCount;
            if (dto.DataType != null) e.DataType = dto.DataType;
            if (dto.Multiplier.HasValue) e.Multiplier = dto.Multiplier;
            if (dto.Unit != null) e.Unit = dto.Unit;
            if (dto.Category != null) e.Category = dto.Category;
            if (dto.Confidence != null) e.Confidence = dto.Confidence;
            if (dto.IsEnabled.HasValue) e.IsEnabled = dto.IsEnabled.Value;
            if (dto.SortOrder.HasValue) e.SortOrder = dto.SortOrder;
            if (dto.Remarks != null) e.Remarks = dto.Remarks;

            e.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return MapPoint(e);
        }

        public async Task<bool> DeletePointAsync(Guid pointId, string? deletedBy)
        {
            var e = await _db.ModelProtocolPoints.FirstOrDefaultAsync(x => x.Id == pointId && !x.IsDeleted);
            if (e is null) return false;

            e.IsDeleted = true;
            e.DeletedAt = DateTime.UtcNow;
            e.DeletedBy = deletedBy;

            await _db.SaveChangesAsync();
            return true;
        }

        // ───────────────────── PROTOCOLSERVICE-FACING ─────────────────────

        public async Task<PagedResult<DiscoveryTargetDto>> GetDiscoveryTargetsAsync(DiscoveryTargetFilterParams p)
        {
            var query =
                from d in _db.DeviceDetails.AsNoTracking().Where(x => !x.IsDeleted)
                join m in _db.DeviceMasters.AsNoTracking() on d.DeviceMasterId equals m.Id
                join ms in _db.ModelSpecifications.AsNoTracking() on m.ModelSpecificationId equals (Guid?)ms.Id into msJoin
                from ms in msJoin.DefaultIfEmpty()
                select new DiscoveryTargetDto
                {
                    DeviceDetailId = d.Id,
                    DeviceShortName = d.ShortName,
                    SerialNumber = d.SerialNumber,
                    DeviceMasterId = m.Id,
                    ModelSpecificationId = m.ModelSpecificationId,
                    ModelName = ms != null ? ms.Name : null,
                    Protocol = m.DataFormat,               // DeviceMaster.DataFormat = authoritative protocol
                    IPAddress = d.IPAddress,
                    GatewayIPAddress = d.GatewayIPAddress,
                    Port = d.Port,
                    UnitId = d.UnitId,
                    SnmpCommunity = d.SnmpCommunity,
                    BacnetDeviceId = d.BacnetDeviceId,
                    DiscoveryStatus = d.DiscoveryStatus,
                    LastDiscoveredAt = d.LastDiscoveredAt,
                    DiscoveryMessage = d.DiscoveryMessage
                };

            if (!string.IsNullOrWhiteSpace(p.Protocol))
                query = query.Where(x => x.Protocol == p.Protocol);
            if (!string.IsNullOrWhiteSpace(p.DiscoveryStatus))
                query = query.Where(x => x.DiscoveryStatus == p.DiscoveryStatus);

            query = query.OrderBy(x => x.DeviceDetailId);
            return await query.ToPagedAsync(p);
        }

        public async Task<bool> UpdateDiscoveryStatusAsync(Guid deviceDetailId, DiscoveryStatusUpdateDto dto)
        {
            var e = await _db.DeviceDetails.FirstOrDefaultAsync(x => x.Id == deviceDetailId && !x.IsDeleted);
            if (e is null) return false;

            e.DiscoveryStatus = dto.DiscoveryStatus;
            e.DiscoveryMessage = dto.DiscoveryMessage;
            e.LastDiscoveredAt = DateTime.UtcNow;
            e.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        // ───────────────────────── MAPPING ─────────────────────────

        private async Task<ModelProtocolProfileResponseDto?> MapProfileAsync(Guid profileId)
        {
            var e = await _db.ModelProtocolProfiles.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == profileId && !x.IsDeleted);
            if (e is null) return null;

            var model = await _db.ModelSpecifications.AsNoTracking()
                .Where(m => m.Id == e.ModelSpecificationId)
                .Select(m => new { m.Name, m.ModelNumber })
                .FirstOrDefaultAsync();

            var points = await _db.ModelProtocolPoints.AsNoTracking()
                .Where(x => x.ProfileId == profileId && !x.IsDeleted)
                .OrderBy(x => x.SortOrder ?? int.MaxValue).ThenBy(x => x.PointKey)
                .ToListAsync();

            return new ModelProtocolProfileResponseDto
            {
                Id = e.Id,
                ModelSpecificationId = e.ModelSpecificationId,
                ModelName = model?.Name,
                ModelNumber = model?.ModelNumber,
                Protocol = e.Protocol,
                DefaultPort = e.DefaultPort,
                DefaultUnitId = e.DefaultUnitId,
                FunctionCode = e.FunctionCode,
                WordOrder = e.WordOrder,
                ByteOrder = e.ByteOrder,
                AddressBase = e.AddressBase,
                PollPeriodMs = e.PollPeriodMs,
                Source = e.Source,
                IsVerified = e.IsVerified,
                VerifiedAt = e.VerifiedAt,
                VerifiedBy = e.VerifiedBy,
                Remarks = e.Remarks,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                Points = points.Select(MapPoint).ToList()
            };
        }

        private static ModelProtocolPointResponseDto MapPoint(ModelProtocolPoint e) => new()
        {
            Id = e.Id,
            ProfileId = e.ProfileId,
            PointKey = e.PointKey,
            DisplayName = e.DisplayName,
            Address = e.Address,
            FunctionCode = e.FunctionCode,
            RegisterCount = e.RegisterCount,
            DataType = e.DataType,
            Multiplier = e.Multiplier,
            Unit = e.Unit,
            Category = e.Category,
            Confidence = e.Confidence,
            IsEnabled = e.IsEnabled,
            SortOrder = e.SortOrder,
            Remarks = e.Remarks
        };
    }
}
