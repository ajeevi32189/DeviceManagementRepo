using System.Data;
using System.Diagnostics;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Hubs;
using DeviceManagementOnly.Models.Device;
using DeviceManagementOnly.Models.other;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  LAYOUT SERVICE — 5D View ka backend.
    //
    //  Teen cheezein sambhalta hai:
    //
    //  1. SCENE (padhna / likhna)
    //     Poora drishya ek hi call me. 5D me 97 device, 300 cable,
    //     46 rack aur 48 tile ek saath hote hain — ek-ek ki alag call
    //     karne par view khulne me 20-30 second lag jaate, isliye
    //     sab kuch ek request me.
    //
    //  2. LOCK / EDIT MODE
    //     - IsLocked  : pakka lock. Layout final hone ke baad laga do,
    //                   phir kuch hilta nahi.
    //     - Edit lease: asthayi pakad. Edit mode me jaate hi user lease
    //                   le leta hai; tab tak baaki sabko view-only
    //                   dikhta hai. Lease par expiry hai taaki koi tab
    //                   band karke chala jaaye to layout hamesha ke liye
    //                   block na ho.
    //
    //  3. BROADCAST
    //     Har badlav ke baad usi floor ke doosre khule browsers ko
    //     SignalR se khabar chali jaati hai.
    //
    //  Naap ki bhasha: sab metres me (device ke apne naap mm me, kyunki
    //  datasheet mm me hoti hai). Coordinates building ke centre se:
    //  X = East-West, Z = North-South, Y = upar (0 = finished floor).
    // ══════════════════════════════════════════════════════════════

    public interface ILayoutService
    {
        Task<LayoutSceneDto?> GetSceneAsync(Guid floorId, string? user);
        Task<LayoutSaveResultDto> SaveSceneAsync(Guid floorId, LayoutSaveDto dto, string? user);
        Task<FloorPlacementDto?> TransformPlacementAsync(Guid placementId, PlacementTransformDto dto, string? user);

        Task<LayoutLockDto?> GetLockAsync(Guid floorId, string? user, string? sessionId = null);
        Task<LayoutLockDto> SetLockAsync(Guid floorId, LayoutLockRequestDto dto, string? user);
        Task<LayoutLockDto> AcquireEditAsync(Guid floorId, EditSessionRequestDto dto, string? user);
        Task<LayoutLockDto> HeartbeatEditAsync(Guid floorId, EditSessionRequestDto dto, string? user);
        Task<LayoutLockDto> ReleaseEditAsync(Guid floorId, string editSessionId, string? user);

        Task<List<LayoutViewResponseDto>> GetViewsAsync(Guid floorId, string? user);
        Task<LayoutViewResponseDto> CreateViewAsync(Guid floorId, LayoutViewCreateDto dto, string? user);
        Task<bool> DeleteViewAsync(Guid viewId, string? user);
    }

    public class LayoutService : ILayoutService
    {
        private readonly DBContext _db;
        private readonly IHubContext<RoomLayoutHub> _hub;
        private readonly ILogger<LayoutService> _log;

        private const int DefaultLeaseMinutes = 10;

        public LayoutService(DBContext db, IHubContext<RoomLayoutHub> hub, ILogger<LayoutService> log)
        {
            _db = db;
            _hub = hub;
            _log = log;
        }

        private static string FloorGroup(Guid floorId) => $"floor-{floorId}";

        // ══════════════════════════════════════════════════════════
        //  LOCK — sabse pehle, kyunki baaki sab isi se guzarta hai
        // ══════════════════════════════════════════════════════════

        /// <summary>Floor ka lock row laao; na ho to bana do.</summary>
        private async Task<LayoutLock> GetOrCreateLockAsync(Guid floorId, string? user)
        {
            var row = await _db.LayoutLocks.FirstOrDefaultAsync(l => l.FloorId == floorId);
            if (row != null) return row;

            var floor = await _db.Floors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == floorId);
            row = new LayoutLock
            {
                FloorId = floorId,
                DataCenterId = floor?.DataCenterId,
                IsLocked = false,
                Revision = 0,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };
            _db.LayoutLocks.Add(row);
            await _db.SaveChangesAsync();
            return row;
        }

        /// <summary>Lease zinda hai ya time nikal chuka.</summary>
        private static bool LeaseIsLive(LayoutLock l) =>
            !string.IsNullOrEmpty(l.EditingSessionId)
            && l.EditingExpiresAt.HasValue
            && l.EditingExpiresAt.Value > DateTime.UtcNow;

        private static LayoutLockDto MapLock(LayoutLock l, string? user, string? sessionId)
        {
            var live = LeaseIsLive(l);
            var mine = live && sessionId != null && l.EditingSessionId == sessionId;

            return new LayoutLockDto
            {
                FloorId = l.FloorId,
                DataCenterId = l.DataCenterId,
                IsLocked = l.IsLocked,
                LockedBy = l.LockedBy,
                LockedAt = l.LockedAt,
                LockNote = l.LockNote,
                EditingBy = live ? l.EditingBy : null,
                EditingSessionId = live ? l.EditingSessionId : null,
                EditingSince = live ? l.EditingSince : null,
                EditingExpiresAt = live ? l.EditingExpiresAt : null,
                IsEditableByMe = !l.IsLocked && mine,
                Mode = l.IsLocked ? "locked" : (mine ? "edit" : "view"),
                Revision = l.Revision,
                LastSavedAt = l.LastSavedAt,
                LastSavedBy = l.LastSavedBy
            };
        }

        /// <summary>
        /// Likhne se pehle ki jaanch. Teen soorat me mana:
        ///   - layout pakka lock hai
        ///   - koi aur edit kar raha hai
        ///   - caller ne lease li hi nahi
        /// </summary>
        private async Task<LayoutLock> EnsureEditableAsync(Guid floorId, string? editSessionId, string? user)
        {
            var l = await GetOrCreateLockAsync(floorId, user);

            if (l.IsLocked)
                throw new InvalidOperationException(
                    $"Layout locked hai (by {l.LockedBy ?? "unknown"} on {l.LockedAt:dd-MMM-yyyy HH:mm} UTC). " +
                    "Pehle unlock kijiye, tabhi kuch badla ja sakta hai.");

            if (string.IsNullOrWhiteSpace(editSessionId))
                throw new InvalidOperationException(
                    "EditSessionId nahi mila. Pehle edit mode on kijiye (acquire-edit), tabhi layout save hoga.");

            if (!LeaseIsLive(l))
                throw new InvalidOperationException(
                    "Edit session khatam ho chuka hai. Edit mode dobara on kijiye.");

            if (l.EditingSessionId != editSessionId)
                throw new InvalidOperationException(
                    $"'{l.EditingBy ?? "koi aur"}' abhi is layout ko edit kar raha hai. " +
                    "Unke khatam karne ka intezaar kijiye.");

            return l;
        }

        public async Task<LayoutLockDto?> GetLockAsync(Guid floorId, string? user, string? sessionId = null)
        {
            var exists = await _db.Floors.AsNoTracking().AnyAsync(f => f.Id == floorId);
            if (!exists) return null;
            var l = await GetOrCreateLockAsync(floorId, user);
            return MapLock(l, user, sessionId);
        }

        public async Task<LayoutLockDto> SetLockAsync(Guid floorId, LayoutLockRequestDto dto, string? user)
        {
            var l = await GetOrCreateLockAsync(floorId, user);
            var now = DateTime.UtcNow;

            if (dto.IsLocked)
            {
                l.IsLocked = true;
                l.LockedBy = user;
                l.LockedAt = now;
                l.LockNote = dto.LockNote;
                // Lock lagte hi chalti hui edit lease bhi khatam
                l.EditingBy = null;
                l.EditingSessionId = null;
                l.EditingSince = null;
                l.EditingExpiresAt = null;
            }
            else
            {
                l.IsLocked = false;
                l.UnlockedBy = user;
                l.UnlockedAt = now;
            }

            l.UpdatedAt = now;
            l.UpdatedBy = user;
            await _db.SaveChangesAsync();

            var result = MapLock(l, user, null);
            await _hub.Clients.Group(FloorGroup(floorId)).SendAsync("LayoutLockChanged", result);
            _log.LogInformation("Layout {Floor} {Action} by {User}", floorId, dto.IsLocked ? "LOCKED" : "UNLOCKED", user);
            return result;
        }

        public async Task<LayoutLockDto> AcquireEditAsync(Guid floorId, EditSessionRequestDto dto, string? user)
        {
            var l = await GetOrCreateLockAsync(floorId, user);

            if (l.IsLocked)
                throw new InvalidOperationException(
                    $"Layout locked hai (by {l.LockedBy ?? "unknown"}). Edit mode on nahi ho sakta.");

            // Koi aur zinda lease rakhta hai to mana — jab tak Force na ho
            if (LeaseIsLive(l) && l.EditingSessionId != dto.EditSessionId && !dto.Force)
                throw new InvalidOperationException(
                    $"'{l.EditingBy ?? "koi aur"}' abhi edit kar raha hai " +
                    $"(lease {l.EditingExpiresAt:HH:mm} UTC tak). Thodi der baad dekhiye.");

            // Force bhi zinda lease nahi tod sakta — sirf chhoota hua lease le sakta hai
            if (LeaseIsLive(l) && l.EditingSessionId != dto.EditSessionId && dto.Force)
                throw new InvalidOperationException(
                    $"'{l.EditingBy}' ka edit session abhi chal raha hai, zabardasti nahi liya ja sakta.");

            var minutes = dto.LeaseMinutes.GetValueOrDefault(DefaultLeaseMinutes);
            var now = DateTime.UtcNow;
            l.EditingBy = user;
            l.EditingSessionId = dto.EditSessionId;
            l.EditingSince = now;
            l.EditingExpiresAt = now.AddMinutes(minutes);
            l.UpdatedAt = now;
            l.UpdatedBy = user;
            await _db.SaveChangesAsync();

            var result = MapLock(l, user, dto.EditSessionId);
            await _hub.Clients.Group(FloorGroup(floorId)).SendAsync("LayoutEditStarted", result);
            return result;
        }

        public async Task<LayoutLockDto> HeartbeatEditAsync(Guid floorId, EditSessionRequestDto dto, string? user)
        {
            var l = await GetOrCreateLockAsync(floorId, user);

            if (l.EditingSessionId != dto.EditSessionId)
                throw new InvalidOperationException("Ye edit session ab aapke paas nahi hai. Edit mode dobara on kijiye.");

            var minutes = dto.LeaseMinutes.GetValueOrDefault(DefaultLeaseMinutes);
            l.EditingExpiresAt = DateTime.UtcNow.AddMinutes(minutes);
            await _db.SaveChangesAsync();
            return MapLock(l, user, dto.EditSessionId);
        }

        public async Task<LayoutLockDto> ReleaseEditAsync(Guid floorId, string editSessionId, string? user)
        {
            var l = await GetOrCreateLockAsync(floorId, user);

            // Jisne li thi wahi chhod sakta hai (ya lease waise bhi mar chuki ho)
            if (l.EditingSessionId == editSessionId || !LeaseIsLive(l))
            {
                l.EditingBy = null;
                l.EditingSessionId = null;
                l.EditingSince = null;
                l.EditingExpiresAt = null;
                l.UpdatedAt = DateTime.UtcNow;
                l.UpdatedBy = user;
                await _db.SaveChangesAsync();
            }

            var result = MapLock(l, user, null);
            await _hub.Clients.Group(FloorGroup(floorId)).SendAsync("LayoutEditEnded", result);
            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  SCENE — padhna
        // ══════════════════════════════════════════════════════════

        public async Task<LayoutSceneDto?> GetSceneAsync(Guid floorId, string? user)
        {
            var sw = Stopwatch.StartNew();

            var floor = await _db.Floors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == floorId);
            if (floor is null) return null;

            var scene = new LayoutSceneDto
            {
                Floor = new LayoutFloorDto
                {
                    Id = floor.Id,
                    Name = floor.Name,
                    DataCenterId = floor.DataCenterId,
                    WidthM = floor.WidthM,
                    LengthM = floor.LengthM,
                    HeightM = floor.HeightM,
                    RaisedFloorHeightM = floor.RaisedFloorHeightM,
                    TileSizeM = floor.TileSizeM,
                    LevelNumber = floor.LevelNumber
                }
            };

            // ── Building ──
            if (floor.DataCenterId.HasValue)
            {
                var dc = await _db.DataCenters.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == floor.DataCenterId.Value && !d.IsDeleted);
                if (dc != null) scene.DataCenter = MapDataCenter(dc, 0);
            }

            // ── Rooms ──
            var rooms = await _db.Rooms.AsNoTracking()
                .Where(r => r.FloorId == floorId && r.IsActive)
                .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
                .ToListAsync();

            scene.Rooms = rooms.Select(r => new LayoutRoomDto
            {
                Id = r.Id,
                Name = r.Name,
                RoomType = r.RoomType,
                PositionX = r.PositionX,
                PositionZ = r.PositionY,          // column purana hai, matlab Z hai
                Width = r.Width,
                Length = r.Length,
                Height = r.Height,
                RotationAngle = r.RotationAngle,
                ColorHex = r.ColorHex,
                Icon = r.Icon,
                PowerBudgetKW = r.PowerBudgetKW,
                DimensionUnit = r.DimensionUnit,
                MarginsJson = r.MarginsJson,
                InnerMarginsJson = r.InnerMarginsJson,
                SortOrder = r.SortOrder,
                IsActive = r.IsActive
            }).ToList();

            var roomIds = rooms.Select(r => r.Id).ToList();

            // ── Racks (+ power, ek hi query me jod kar) ──
            var racks = await _db.Racks.AsNoTracking()
                .Where(k => roomIds.Contains(k.RoomId) && k.IsActive)
                .OrderBy(k => k.RowName).ThenBy(k => k.RackName)
                .ToListAsync();

            var rackIds = racks.Select(k => k.Id).ToList();

            var power = await _db.RackPowerCapacities.AsNoTracking()
                .Where(p => rackIds.Contains(p.RackId) && p.IsActive)
                .GroupBy(p => p.RackId)
                .Select(g => new
                {
                    RackId = g.Key,
                    Rated = g.Sum(x => x.RatedCapacityKW ?? 0m),
                    Estimated = g.Sum(x => x.EstimatedLoadKW ?? 0m),
                    Measured = g.Sum(x => x.MeasuredLoadKW ?? 0m)
                })
                .ToDictionaryAsync(x => x.RackId);

            scene.Racks = racks.Select(k =>
            {
                power.TryGetValue(k.Id, out var p);
                return new LayoutRackDto
                {
                    Id = k.Id,
                    RoomId = k.RoomId,
                    RackName = k.RackName,
                    TotalUHeight = k.TotalUHeight,
                    Status = k.Status,
                    PositionX = k.PositionX,
                    PositionZ = k.PositionY,      // yahan bhi Y column me Z hai
                    RotationAngle = k.RotationAngle,
                    WidthMm = k.WidthMm,
                    DepthMm = k.DepthMm,
                    HeightMm = k.HeightMm,
                    RowName = k.RowName,
                    AisleName = k.AisleName,
                    FacingDirection = k.FacingDirection,
                    Shape = k.Shape,
                    ColorHex = k.ColorHex,
                    LabelText = k.LabelText,
                    IsActive = k.IsActive,
                    RatedCapacityKW = p?.Rated,
                    EstimatedLoadKW = p?.Estimated,
                    MeasuredLoadKW = p?.Measured
                };
            }).ToList();

            // ── Rack ke andar ke device (slots) ──
            scene.RackSlots = await (
                from loc in _db.DeviceRackLocations.AsNoTracking()
                join dd in _db.DeviceDetails.AsNoTracking() on loc.DeviceDetailId equals dd.Id into ddj
                from dd in ddj.DefaultIfEmpty()
                where loc.RackId != null && rackIds.Contains(loc.RackId.Value) && loc.IsActive
                orderby loc.StartUPosition
                select new RackSlotDto
                {
                    Id = loc.Id,
                    RackId = loc.RackId,
                    DeviceDetailId = loc.DeviceDetailId,
                    DeviceName = dd != null ? (dd.ShortName ?? dd.LongName ?? dd.TagNumber) : null,
                    StartUPosition = loc.StartUPosition,
                    UOccupied = loc.UOccupied,
                    Face = loc.Face,
                    PortsJson = loc.PortsJson,
                    FaceImageFileId = loc.FaceImageFileId,
                    ColorHex = loc.ColorHex,
                    IsActive = loc.IsActive
                }).ToListAsync();

            // ── Farsh par khade device ──
            scene.FloorDevices = await (
                from pl in _db.DeviceFloorPlacements.AsNoTracking()
                join dd in _db.DeviceDetails.AsNoTracking() on pl.DeviceDetailId equals (Guid?)dd.Id into ddj
                from dd in ddj.DefaultIfEmpty()
                join dt in _db.DeviceTypes.AsNoTracking() on pl.DeviceTypeId equals (Guid?)dt.Id into dtj
                from dt in dtj.DefaultIfEmpty()
                where pl.FloorId == floorId && pl.IsActive
                orderby pl.SortOrder
                select new FloorPlacementDto
                {
                    Id = pl.Id,
                    DeviceDetailId = pl.DeviceDetailId,
                    RoomId = pl.RoomId,
                    DeviceTypeId = pl.DeviceTypeId,
                    PositionX = pl.PositionX,
                    PositionZ = pl.PositionZ,
                    PositionY = pl.PositionY,
                    RotationAngle = pl.RotationAngle,
                    Level = pl.Level,
                    LengthMm = pl.LengthMm ?? (dt != null ? dt.DefaultLengthMm : null),
                    WidthMm = pl.WidthMm ?? (dt != null ? dt.DefaultWidthMm : null),
                    HeightMm = pl.HeightMm ?? (dt != null ? dt.DefaultHeightMm : null),
                    Shape = pl.Shape ?? (dt != null ? dt.Shape : null),
                    ColorHex = pl.ColorHex ?? (dt != null ? dt.IconColor : null),
                    IconName = pl.IconName ?? (dt != null ? dt.Icon : null),
                    LabelText = pl.LabelText,
                    RowName = pl.RowName,
                    AisleName = pl.AisleName,
                    IsPinned = pl.IsPinned,
                    IsVisible = pl.IsVisible,
                    IsProvisional = pl.IsProvisional,
                    PlaceholderName = pl.PlaceholderName,
                    SortOrder = pl.SortOrder,
                    MetaJson = pl.MetaJson,
                    IsActive = pl.IsActive,
                    DeviceName = dd != null ? (dd.ShortName ?? dd.LongName ?? dd.TagNumber) : pl.PlaceholderName,
                    DeviceTypeName = dt != null ? dt.Name : null
                }).ToListAsync();

            // ── Tray / cable / pipe / tile ──
            scene.Trays = await _db.CableTrays.AsNoTracking()
                .Where(t => t.FloorId == floorId && t.IsActive)
                .OrderBy(t => t.SortOrder).ThenBy(t => t.TrayCode)
                .Select(t => new CableTrayDto
                {
                    Id = t.Id, RoomId = t.RoomId, TrayCode = t.TrayCode, Name = t.Name,
                    Axis = t.Axis, Line = t.Line, FromCoord = t.FromCoord, ToCoord = t.ToCoord,
                    WidthM = t.WidthM, Slots = t.Slots, PitchM = t.PitchM, NearSide = t.NearSide,
                    Level = t.Level, ElevationY = t.ElevationY, TrayType = t.TrayType,
                    ColorHex = t.ColorHex, SortOrder = t.SortOrder, IsActive = t.IsActive
                }).ToListAsync();

            scene.Cables = await _db.CableConnections.AsNoTracking()
                .Where(c => c.FloorId == floorId && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new CableDto
                {
                    Id = c.Id,
                    FromPlacementId = c.FromPlacementId, FromDeviceDetailId = c.FromDeviceDetailId,
                    FromEndpoint = c.FromEndpoint, FromFace = c.FromFace,
                    ToPlacementId = c.ToPlacementId, ToDeviceDetailId = c.ToDeviceDetailId,
                    ToEndpoint = c.ToEndpoint, ToFace = c.ToFace,
                    CableType = c.CableType, LayerKey = c.LayerKey,
                    TrayPathJson = c.TrayPathJson, WaypointsJson = c.WaypointsJson,
                    LengthM = c.LengthM, ColorHex = c.ColorHex, Label = c.Label,
                    Status = c.Status, NetworkConnectionId = c.NetworkConnectionId,
                    SortOrder = c.SortOrder, IsActive = c.IsActive
                }).ToListAsync();

            scene.Pipes = await _db.PipeConnections.AsNoTracking()
                .Where(p => p.FloorId == floorId && p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => new PipeDto
                {
                    Id = p.Id,
                    FromPlacementId = p.FromPlacementId, FromDeviceDetailId = p.FromDeviceDetailId,
                    ToPlacementId = p.ToPlacementId, ToDeviceDetailId = p.ToDeviceDetailId,
                    PipeType = p.PipeType, DiameterMm = p.DiameterMm,
                    RouteJson = p.RouteJson, WaypointsJson = p.WaypointsJson,
                    FromOffset = p.FromOffset, ToOffset = p.ToOffset,
                    ElevationY = p.ElevationY, LengthM = p.LengthM,
                    ColorHex = p.ColorHex, Label = p.Label, Status = p.Status,
                    SortOrder = p.SortOrder, IsActive = p.IsActive
                }).ToListAsync();

            scene.Tiles = await _db.FloorTiles.AsNoTracking()
                .Where(t => t.FloorId == floorId && t.IsActive)
                .Select(t => new FloorTileDto
                {
                    Id = t.Id, RoomId = t.RoomId, TileType = t.TileType,
                    PositionX = t.PositionX, PositionZ = t.PositionZ, SizeM = t.SizeM,
                    AirflowCfm = t.AirflowCfm, AisleType = t.AisleType, IsActive = t.IsActive
                }).ToListAsync();

            // ── Device types (shape + default naap) — frontend ko alag call na karni pade ──
            scene.DeviceTypes = await (
                from dt in _db.DeviceTypes.AsNoTracking()
                join dc in _db.DeviceCategories.AsNoTracking() on dt.DeviceCategoryId equals dc.Id into dcj
                from dc in dcj.DefaultIfEmpty()
                where !dt.IsDeleted
                orderby dt.Name
                select new DeviceTypeShapeDto
                {
                    Id = dt.Id, Name = dt.Name, DeviceCategoryId = dt.DeviceCategoryId,
                    CategoryName = dc != null ? dc.Name : null,
                    Shape = dt.Shape, Icon = dt.Icon, IconColor = dt.IconColor, LayerKey = dt.LayerKey,
                    DefaultLengthMm = dt.DefaultLengthMm, DefaultWidthMm = dt.DefaultWidthMm,
                    DefaultHeightMm = dt.DefaultHeightMm, HeatKW = dt.HeatKW, CoolingKW = dt.CoolingKW
                }).ToListAsync();

            // ── Lock ki haalat ──
            var lockRow = await GetOrCreateLockAsync(floorId, user);
            scene.Lock = MapLock(lockRow, user, null);

            scene.Counts = new LayoutCountsDto
            {
                Rooms = scene.Rooms.Count,
                Racks = scene.Racks.Count,
                RackSlots = scene.RackSlots.Count,
                FloorDevices = scene.FloorDevices.Count,
                Trays = scene.Trays.Count,
                Cables = scene.Cables.Count,
                Pipes = scene.Pipes.Count,
                Tiles = scene.Tiles.Count
            };
            scene.GeneratedAt = DateTime.UtcNow;

            sw.Stop();
            _log.LogInformation(
                "5D scene loaded floor={Floor} in {Ms} ms — {Rooms} rooms, {Racks} racks, {Dev} floor devices, {Cables} cables",
                floorId, sw.ElapsedMilliseconds, scene.Counts.Rooms, scene.Counts.Racks,
                scene.Counts.FloorDevices, scene.Counts.Cables);

            return scene;
        }

        private static DataCenterResponseDto MapDataCenter(DataCenter d, int floorCount) => new()
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

        // ══════════════════════════════════════════════════════════
        //  SCENE — likhna
        //
        //  Ek hi transaction me poora drishya. Teen baat dhyan me:
        //
        //  1. Id frontend banata hai.
        //     3D editor me cable ko placement ka id chahiye hota hai
        //     SAVE SE PEHLE (cable kis device se kahan ja rahi hai).
        //     Isliye naye row ka GUID frontend hi banata hai aur hum
        //     usi id se row banate hain. Id khaali aaye to hum bana denge.
        //
        //  2. Delete matlab soft delete.
        //     IsActive = false. Purana layout kabhi mitta nahi, taaki
        //     "pehle kya tha" dekha ja sake.
        //
        //  3. Audit band.
        //     Har column ka audit row banane se ek save par lakhon rows
        //     ban jaate. Isliye SuppressAudit on karke ek summary row
        //     likhte hain.
        // ══════════════════════════════════════════════════════════

        public async Task<LayoutSaveResultDto> SaveSceneAsync(Guid floorId, LayoutSaveDto dto, string? user)
        {
            var sw = Stopwatch.StartNew();

            var floor = await _db.Floors.FirstOrDefaultAsync(f => f.Id == floorId)
                ?? throw new InvalidOperationException($"Floor {floorId} nahi mila.");

            var lockRow = await EnsureEditableAsync(floorId, dto.EditSessionId, user);

            if (dto.ExpectedRevision.HasValue && dto.ExpectedRevision.Value != lockRow.Revision)
                throw new DBConcurrencyException(
                    $"Layout beech me kisi aur ne badal diya hai (aapke paas revision {dto.ExpectedRevision}, " +
                    $"server par {lockRow.Revision}). Pehle dobara load kijiye, phir save.");

            var res = new LayoutSaveResultDto { Revision = lockRow.Revision };
            var now = DateTime.UtcNow;

            var roomIdsOnFloor = await _db.Rooms.Where(r => r.FloorId == floorId).Select(r => r.Id).ToListAsync();

            await using var tx = await _db.Database.BeginTransactionAsync();
            _db.SuppressAudit = true;
            try
            {
                // ── Floor header ──
                if (dto.Floor != null)
                {
                    floor.WidthM = dto.Floor.WidthM ?? floor.WidthM;
                    floor.LengthM = dto.Floor.LengthM ?? floor.LengthM;
                    floor.HeightM = dto.Floor.HeightM ?? floor.HeightM;
                    floor.RaisedFloorHeightM = dto.Floor.RaisedFloorHeightM ?? floor.RaisedFloorHeightM;
                    floor.TileSizeM = dto.Floor.TileSizeM ?? floor.TileSizeM;
                    floor.LevelNumber = dto.Floor.LevelNumber ?? floor.LevelNumber;
                    if (dto.Floor.DataCenterId.HasValue) floor.DataCenterId = dto.Floor.DataCenterId;
                    floor.UpdatedAt = now;
                    floor.UpdatedBy = user;
                }

                var savedRoomIds = new List<Guid>();

                // -- ROOMS --
                if (dto.Rooms != null)
                {
                    var existing = await _db.Rooms.Where(r => r.FloorId == floorId).ToDictionaryAsync(r => r.Id);
                    foreach (var d in dto.Rooms)
                    {
                        var id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id;
                        savedRoomIds.Add(id);
                        if (!existing.TryGetValue(id, out var row))
                        {
                            row = new Room { Id = id, FloorId = floorId, Name = d.Name, CreatedBy = user, CreatedAt = now };
                            _db.Rooms.Add(row);
                            res.Created.Rooms++;
                        }
                        else res.Updated.Rooms++;

                        row.Name = d.Name;
                        row.RoomType = d.RoomType;
                        row.PositionX = d.PositionX;
                        row.PositionY = d.PositionZ;      // Z yahin rakha jaata hai
                        row.Width = d.Width;
                        row.Length = d.Length;
                        row.Height = d.Height;
                        row.RotationAngle = d.RotationAngle;
                        row.ColorHex = d.ColorHex;
                        row.Icon = d.Icon;
                        row.PowerBudgetKW = d.PowerBudgetKW;
                        row.DimensionUnit = d.DimensionUnit;
                        row.MarginsJson = d.MarginsJson;
                        row.InnerMarginsJson = d.InnerMarginsJson;
                        row.SortOrder = d.SortOrder;
                        row.IsActive = true;
                        row.UpdatedAt = now;
                        row.UpdatedBy = user;
                    }
                }
                res.Deleted.Rooms = await SoftDeleteRoomsAsync(dto.DeletedRoomIds, floorId, user, now);

                // Abhi jo rooms bane/update hue, unke id bhi list me daal do -
                // warna usi save me aaya hua naya rack "room nahi mila" kehkar
                // chhoot jaata.
                if (savedRoomIds.Count > 0)
                    roomIdsOnFloor = roomIdsOnFloor.Union(savedRoomIds).Distinct().ToList();

                // ── RACKS ──
                if (dto.Racks != null)
                {
                    var existing = await _db.Racks
                        .Where(k => roomIdsOnFloor.Contains(k.RoomId))
                        .ToDictionaryAsync(k => k.Id);

                    foreach (var d in dto.Racks)
                    {
                        if (!roomIdsOnFloor.Contains(d.RoomId))
                        {
                            res.Warnings.Add($"Rack '{d.RackName}' chhoda gaya — RoomId {d.RoomId} is floor par nahi hai.");
                            continue;
                        }
                        var id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id;
                        if (!existing.TryGetValue(id, out var row))
                        {
                            row = new Rack { Id = id, RoomId = d.RoomId, RackName = d.RackName, CreatedBy = user, CreatedAt = now };
                            _db.Racks.Add(row);
                            res.Created.Racks++;
                        }
                        else res.Updated.Racks++;

                        row.RoomId = d.RoomId;
                        row.RackName = d.RackName;
                        row.TotalUHeight = d.TotalUHeight <= 0 ? 42 : d.TotalUHeight;
                        row.Status = string.IsNullOrWhiteSpace(d.Status) ? row.Status : d.Status!;
                        row.PositionX = d.PositionX;
                        row.PositionY = d.PositionZ;
                        row.RotationAngle = d.RotationAngle;
                        row.WidthMm = d.WidthMm;
                        row.DepthMm = d.DepthMm;
                        row.HeightMm = d.HeightMm;
                        row.RowName = d.RowName;
                        row.AisleName = d.AisleName;
                        row.FacingDirection = d.FacingDirection;
                        row.Shape = d.Shape;
                        row.ColorHex = d.ColorHex;
                        row.LabelText = d.LabelText;
                        row.IsActive = true;
                        row.UpdatedAt = now;
                        row.UpdatedBy = user;
                    }
                }
                res.Deleted.Racks = await SoftDeleteAsync<Rack>(dto.DeletedRackIds, user, now);

                // ── RACK SLOTS (rack ke andar ke device) ──
                if (dto.RackSlots != null)
                {
                    var ids = dto.RackSlots.Select(x => x.Id).Where(x => x != Guid.Empty).ToList();
                    var existing = await _db.DeviceRackLocations.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);

                    foreach (var d in dto.RackSlots)
                    {
                        var id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id;
                        if (!existing.TryGetValue(id, out var row))
                        {
                            row = new DeviceRackLocation
                            {
                                Id = id,
                                DeviceDetailId = d.DeviceDetailId,
                                CreatedBy = user,
                                CreatedAt = now
                            };
                            _db.DeviceRackLocations.Add(row);
                            res.Created.RackSlots++;
                        }
                        else res.Updated.RackSlots++;

                        row.DeviceDetailId = d.DeviceDetailId;
                        row.RackId = d.RackId;
                        row.StartUPosition = d.StartUPosition;
                        row.UOccupied = d.UOccupied;
                        row.Face = d.Face ?? "front";
                        row.PortsJson = d.PortsJson;
                        row.FaceImageFileId = d.FaceImageFileId;
                        row.ColorHex = d.ColorHex;
                        row.IsActive = true;
                        row.UpdatedAt = now;
                        row.UpdatedBy = user;
                    }
                }
                res.Deleted.RackSlots = await SoftDeleteAsync<DeviceRackLocation>(dto.DeletedRackSlotIds, user, now);

                // ── FARSH PAR KHADE DEVICE ──
                if (dto.FloorDevices != null)
                {
                    var existing = await _db.DeviceFloorPlacements
                        .Where(p => p.FloorId == floorId).ToDictionaryAsync(p => p.Id);

                    foreach (var d in dto.FloorDevices)
                    {
                        var id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id;
                        if (!existing.TryGetValue(id, out var row))
                        {
                            row = new DeviceFloorPlacement { Id = id, FloorId = floorId, CreatedBy = user, CreatedAt = now };
                            _db.DeviceFloorPlacements.Add(row);
                            res.Created.FloorDevices++;
                        }
                        else res.Updated.FloorDevices++;

                        row.DeviceDetailId = d.DeviceDetailId;
                        row.RoomId = d.RoomId;
                        row.DeviceTypeId = d.DeviceTypeId;
                        row.PositionX = d.PositionX;
                        row.PositionZ = d.PositionZ;
                        row.PositionY = d.PositionY;
                        row.RotationAngle = d.RotationAngle;
                        row.Level = string.IsNullOrWhiteSpace(d.Level) ? "floor" : d.Level;
                        row.LengthMm = d.LengthMm;
                        row.WidthMm = d.WidthMm;
                        row.HeightMm = d.HeightMm;
                        row.Shape = d.Shape;
                        row.ColorHex = d.ColorHex;
                        row.IconName = d.IconName;
                        row.LabelText = d.LabelText;
                        row.RowName = d.RowName;
                        row.AisleName = d.AisleName;
                        row.IsPinned = d.IsPinned;
                        row.IsVisible = d.IsVisible;
                        row.IsProvisional = d.DeviceDetailId == null;
                        row.PlaceholderName = d.PlaceholderName;
                        row.SortOrder = d.SortOrder;
                        row.MetaJson = d.MetaJson;
                        row.IsActive = true;
                        row.UpdatedAt = now;
                        row.UpdatedBy = user;
                    }
                }
                res.Deleted.FloorDevices = await SoftDeleteAsync<DeviceFloorPlacement>(dto.DeletedFloorDeviceIds, user, now);

                // ── TRAYS ──
                if (dto.Trays != null)
                {
                    var existing = await _db.CableTrays.Where(t => t.FloorId == floorId).ToDictionaryAsync(t => t.Id);
                    foreach (var d in dto.Trays)
                    {
                        var id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id;
                        if (!existing.TryGetValue(id, out var row))
                        {
                            row = new CableTray { Id = id, FloorId = floorId, TrayCode = d.TrayCode, CreatedBy = user, CreatedAt = now };
                            _db.CableTrays.Add(row);
                            res.Created.Trays++;
                        }
                        else res.Updated.Trays++;

                        row.RoomId = d.RoomId;
                        row.TrayCode = d.TrayCode;
                        row.Name = d.Name;
                        row.Axis = string.IsNullOrWhiteSpace(d.Axis) ? "x" : d.Axis;
                        row.Line = d.Line;
                        row.FromCoord = d.FromCoord;
                        row.ToCoord = d.ToCoord;
                        row.WidthM = d.WidthM <= 0 ? 0.3m : d.WidthM;
                        row.Slots = d.Slots <= 0 ? 7 : d.Slots;
                        row.PitchM = d.PitchM <= 0 ? 0.12m : d.PitchM;
                        row.NearSide = d.NearSide == 0 ? 1 : d.NearSide;
                        row.Level = string.IsNullOrWhiteSpace(d.Level) ? "underfloor" : d.Level;
                        row.ElevationY = d.ElevationY;
                        row.TrayType = string.IsNullOrWhiteSpace(d.TrayType) ? "mixed" : d.TrayType;
                        row.ColorHex = d.ColorHex;
                        row.SortOrder = d.SortOrder;
                        row.IsActive = true;
                        row.UpdatedAt = now;
                        row.UpdatedBy = user;
                    }
                }
                res.Deleted.Trays = await SoftDeleteAsync<CableTray>(dto.DeletedTrayIds, user, now);

                // ── CABLES ──
                if (dto.Cables != null)
                {
                    var existing = await _db.CableConnections.Where(c => c.FloorId == floorId).ToDictionaryAsync(c => c.Id);
                    foreach (var d in dto.Cables)
                    {
                        var id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id;
                        if (!existing.TryGetValue(id, out var row))
                        {
                            row = new CableConnection { Id = id, FloorId = floorId, CreatedBy = user, CreatedAt = now };
                            _db.CableConnections.Add(row);
                            res.Created.Cables++;
                        }
                        else res.Updated.Cables++;

                        row.FromPlacementId = d.FromPlacementId;
                        row.FromDeviceDetailId = d.FromDeviceDetailId;
                        row.FromEndpoint = d.FromEndpoint;
                        row.FromFace = d.FromFace;
                        row.ToPlacementId = d.ToPlacementId;
                        row.ToDeviceDetailId = d.ToDeviceDetailId;
                        row.ToEndpoint = d.ToEndpoint;
                        row.ToFace = d.ToFace;
                        row.CableType = string.IsNullOrWhiteSpace(d.CableType) ? "power" : d.CableType;
                        row.LayerKey = d.LayerKey;
                        row.TrayPathJson = d.TrayPathJson;
                        row.WaypointsJson = d.WaypointsJson;
                        row.LengthM = d.LengthM;
                        row.ColorHex = d.ColorHex;
                        row.Label = d.Label;
                        row.Status = string.IsNullOrWhiteSpace(d.Status) ? "installed" : d.Status;
                        row.NetworkConnectionId = d.NetworkConnectionId;
                        row.SortOrder = d.SortOrder;
                        row.IsActive = true;
                        row.UpdatedAt = now;
                        row.UpdatedBy = user;
                    }
                }
                res.Deleted.Cables = await SoftDeleteAsync<CableConnection>(dto.DeletedCableIds, user, now);

                // ── PIPES ──
                if (dto.Pipes != null)
                {
                    var existing = await _db.PipeConnections.Where(p => p.FloorId == floorId).ToDictionaryAsync(p => p.Id);
                    foreach (var d in dto.Pipes)
                    {
                        var id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id;
                        if (!existing.TryGetValue(id, out var row))
                        {
                            row = new PipeConnection { Id = id, FloorId = floorId, CreatedBy = user, CreatedAt = now };
                            _db.PipeConnections.Add(row);
                            res.Created.Pipes++;
                        }
                        else res.Updated.Pipes++;

                        row.FromPlacementId = d.FromPlacementId;
                        row.FromDeviceDetailId = d.FromDeviceDetailId;
                        row.ToPlacementId = d.ToPlacementId;
                        row.ToDeviceDetailId = d.ToDeviceDetailId;
                        row.PipeType = string.IsNullOrWhiteSpace(d.PipeType) ? "chilled-water-supply" : d.PipeType;
                        row.DiameterMm = d.DiameterMm;
                        row.RouteJson = d.RouteJson;
                        row.WaypointsJson = d.WaypointsJson;
                        row.FromOffset = d.FromOffset;
                        row.ToOffset = d.ToOffset;
                        row.ElevationY = d.ElevationY;
                        row.LengthM = d.LengthM;
                        row.ColorHex = d.ColorHex;
                        row.Label = d.Label;
                        row.Status = string.IsNullOrWhiteSpace(d.Status) ? "installed" : d.Status;
                        row.SortOrder = d.SortOrder;
                        row.IsActive = true;
                        row.UpdatedAt = now;
                        row.UpdatedBy = user;
                    }
                }
                res.Deleted.Pipes = await SoftDeleteAsync<PipeConnection>(dto.DeletedPipeIds, user, now);

                // ── TILES ──
                if (dto.Tiles != null)
                {
                    var existing = await _db.FloorTiles.Where(t => t.FloorId == floorId).ToDictionaryAsync(t => t.Id);
                    foreach (var d in dto.Tiles)
                    {
                        var id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id;
                        if (!existing.TryGetValue(id, out var row))
                        {
                            row = new FloorTile { Id = id, FloorId = floorId, CreatedBy = user, CreatedAt = now };
                            _db.FloorTiles.Add(row);
                            res.Created.Tiles++;
                        }
                        else res.Updated.Tiles++;

                        row.RoomId = d.RoomId;
                        row.TileType = string.IsNullOrWhiteSpace(d.TileType) ? "perforated" : d.TileType;
                        row.PositionX = d.PositionX;
                        row.PositionZ = d.PositionZ;
                        row.SizeM = d.SizeM <= 0 ? 0.6m : d.SizeM;
                        row.AirflowCfm = d.AirflowCfm;
                        row.AisleType = d.AisleType;
                        row.IsActive = true;
                        row.UpdatedAt = now;
                        row.UpdatedBy = user;
                    }
                }
                res.Deleted.Tiles = await SoftDeleteAsync<FloorTile>(dto.DeletedTileIds, user, now);

                // ── Revision aage badhao ──
                lockRow.Revision += 1;
                lockRow.LastSavedAt = now;
                lockRow.LastSavedBy = user;
                lockRow.UpdatedAt = now;
                lockRow.UpdatedBy = user;

                await _db.SaveChangesAsync();

                // ── Ek summary audit row (poore column-wise audit ki jagah) ──
                _db.AuditLogs.Add(new AuditLog
                {
                    TableName = "layout_scene",
                    RecordId = floorId.ToString(),
                    ActionType = "LAYOUT_SAVE",
                    ColumnName = "revision",
                    OldValue = (lockRow.Revision - 1).ToString(),
                    NewValue = lockRow.Revision.ToString(),
                    ChangedBy = user,
                    ChangedAt = now,
                    Remarks =
                        $"rooms +{res.Created.Rooms}/~{res.Updated.Rooms}/-{res.Deleted.Rooms}; " +
                        $"racks +{res.Created.Racks}/~{res.Updated.Racks}/-{res.Deleted.Racks}; " +
                        $"rackSlots +{res.Created.RackSlots}/~{res.Updated.RackSlots}/-{res.Deleted.RackSlots}; " +
                        $"floorDevices +{res.Created.FloorDevices}/~{res.Updated.FloorDevices}/-{res.Deleted.FloorDevices}; " +
                        $"trays +{res.Created.Trays}/~{res.Updated.Trays}/-{res.Deleted.Trays}; " +
                        $"cables +{res.Created.Cables}/~{res.Updated.Cables}/-{res.Deleted.Cables}; " +
                        $"pipes +{res.Created.Pipes}/~{res.Updated.Pipes}/-{res.Deleted.Pipes}; " +
                        $"tiles +{res.Created.Tiles}/~{res.Updated.Tiles}/-{res.Deleted.Tiles}"
                });
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
            finally
            {
                _db.SuppressAudit = false;
            }

            sw.Stop();
            res.Success = true;
            res.Revision = lockRow.Revision;
            res.SavedAt = now;
            res.ElapsedMs = sw.ElapsedMilliseconds;
            res.Message = $"Layout save ho gaya (revision {res.Revision}).";

            await _hub.Clients.Group(FloorGroup(floorId))
                .SendAsync("LayoutSaved", new { floorId, revision = res.Revision, savedBy = user, savedAt = now });

            _log.LogInformation("5D layout saved floor={Floor} rev={Rev} in {Ms} ms by {User}",
                floorId, res.Revision, sw.ElapsedMilliseconds, user);

            return res;
        }

        /// <summary>IsActive = false kar do. Row kabhi mitte nahi.</summary>
        private async Task<int> SoftDeleteAsync<T>(List<Guid>? ids, string? user, DateTime now) where T : class
        {
            if (ids == null || ids.Count == 0) return 0;
            var rows = await _db.Set<T>().Where(e => ids.Contains(EF.Property<Guid>(e, "Id"))).ToListAsync();
            foreach (var r in rows)
            {
                _db.Entry(r).Property("IsActive").CurrentValue = false;
                _db.Entry(r).Property("UpdatedAt").CurrentValue = now;
                _db.Entry(r).Property("UpdatedBy").CurrentValue = user;
            }
            return rows.Count;
        }

        /// <summary>Room hatao to uske rack aur us room ke device bhi hata do.</summary>
        private async Task<int> SoftDeleteRoomsAsync(List<Guid>? ids, Guid floorId, string? user, DateTime now)
        {
            if (ids == null || ids.Count == 0) return 0;

            var rooms = await _db.Rooms.Where(r => ids.Contains(r.Id) && r.FloorId == floorId).ToListAsync();
            foreach (var r in rooms) { r.IsActive = false; r.UpdatedAt = now; r.UpdatedBy = user; }

            var racks = await _db.Racks.Where(k => ids.Contains(k.RoomId)).ToListAsync();
            foreach (var k in racks) { k.IsActive = false; k.UpdatedAt = now; k.UpdatedBy = user; }

            var placed = await _db.DeviceFloorPlacements
                .Where(p => p.RoomId != null && ids.Contains(p.RoomId.Value)).ToListAsync();
            foreach (var p in placed) { p.IsActive = false; p.UpdatedAt = now; p.UpdatedBy = user; }

            return rooms.Count;
        }

        // ══════════════════════════════════════════════════════════
        //  EK DEVICE HILANA — halka rasta
        //
        //  Drag khatam hone par sirf ek device ki jagah badalni hoti
        //  hai. Uske liye poora scene bhejna bekaar hai, isliye ye
        //  chhota endpoint. Frontend ise mouseup par ek baar kare —
        //  har frame par nahi.
        // ══════════════════════════════════════════════════════════
        public async Task<FloorPlacementDto?> TransformPlacementAsync(Guid placementId, PlacementTransformDto dto, string? user)
        {
            var row = await _db.DeviceFloorPlacements.FirstOrDefaultAsync(p => p.Id == placementId);
            if (row is null) return null;

            await EnsureEditableAsync(row.FloorId, dto.EditSessionId, user);

            if (row.IsPinned)
                throw new InvalidOperationException("Ye device pin kiya hua hai. Pehle unpin kijiye.");

            if (dto.PositionX.HasValue) row.PositionX = dto.PositionX.Value;
            if (dto.PositionZ.HasValue) row.PositionZ = dto.PositionZ.Value;
            if (dto.PositionY.HasValue) row.PositionY = dto.PositionY.Value;
            if (dto.RotationAngle.HasValue) row.RotationAngle = dto.RotationAngle.Value;
            if (dto.RoomId.HasValue) row.RoomId = dto.RoomId.Value;
            if (!string.IsNullOrWhiteSpace(dto.Level)) row.Level = dto.Level!;

            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedBy = user;
            await _db.SaveChangesAsync();

            var result = new FloorPlacementDto
            {
                Id = row.Id,
                DeviceDetailId = row.DeviceDetailId,
                RoomId = row.RoomId,
                DeviceTypeId = row.DeviceTypeId,
                PositionX = row.PositionX,
                PositionZ = row.PositionZ,
                PositionY = row.PositionY,
                RotationAngle = row.RotationAngle,
                Level = row.Level,
                LengthMm = row.LengthMm,
                WidthMm = row.WidthMm,
                HeightMm = row.HeightMm,
                Shape = row.Shape,
                ColorHex = row.ColorHex,
                IconName = row.IconName,
                LabelText = row.LabelText,
                RowName = row.RowName,
                AisleName = row.AisleName,
                IsPinned = row.IsPinned,
                IsVisible = row.IsVisible,
                IsProvisional = row.IsProvisional,
                PlaceholderName = row.PlaceholderName,
                SortOrder = row.SortOrder,
                MetaJson = row.MetaJson,
                IsActive = row.IsActive
            };

            await _hub.Clients.Group(FloorGroup(row.FloorId))
                .SendAsync("PlacementMoved", result);

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  SAVED VIEWS
        // ══════════════════════════════════════════════════════════

        public async Task<List<LayoutViewResponseDto>> GetViewsAsync(Guid floorId, string? user)
        {
            return await _db.LayoutViews.AsNoTracking()
                .Where(v => v.FloorId == floorId && v.IsActive
                            && (v.IsShared || v.CreatedBy == user))
                .OrderBy(v => v.SortOrder).ThenBy(v => v.Name)
                .Select(v => new LayoutViewResponseDto
                {
                    Id = v.Id, FloorId = v.FloorId, DataCenterId = v.DataCenterId,
                    Name = v.Name, Description = v.Description,
                    CameraJson = v.CameraJson, LayerStateJson = v.LayerStateJson,
                    FilterStateJson = v.FilterStateJson, ThumbnailFileId = v.ThumbnailFileId,
                    IsShared = v.IsShared, SortOrder = v.SortOrder,
                    CreatedBy = v.CreatedBy, CreatedAt = v.CreatedAt, UpdatedAt = v.UpdatedAt
                }).ToListAsync();
        }

        public async Task<LayoutViewResponseDto> CreateViewAsync(Guid floorId, LayoutViewCreateDto dto, string? user)
        {
            var floor = await _db.Floors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == floorId)
                ?? throw new InvalidOperationException($"Floor {floorId} nahi mila.");

            var v = new LayoutView
            {
                FloorId = floorId,
                DataCenterId = floor.DataCenterId,
                Name = dto.Name,
                Description = dto.Description,
                CameraJson = dto.CameraJson,
                LayerStateJson = dto.LayerStateJson,
                FilterStateJson = dto.FilterStateJson,
                ThumbnailFileId = dto.ThumbnailFileId,
                IsShared = dto.IsShared,
                SortOrder = dto.SortOrder,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };
            _db.LayoutViews.Add(v);
            await _db.SaveChangesAsync();

            return new LayoutViewResponseDto
            {
                Id = v.Id, FloorId = v.FloorId, DataCenterId = v.DataCenterId,
                Name = v.Name, Description = v.Description,
                CameraJson = v.CameraJson, LayerStateJson = v.LayerStateJson,
                FilterStateJson = v.FilterStateJson, ThumbnailFileId = v.ThumbnailFileId,
                IsShared = v.IsShared, SortOrder = v.SortOrder,
                CreatedBy = v.CreatedBy, CreatedAt = v.CreatedAt
            };
        }

        public async Task<bool> DeleteViewAsync(Guid viewId, string? user)
        {
            var v = await _db.LayoutViews.FirstOrDefaultAsync(x => x.Id == viewId);
            if (v is null) return false;
            v.IsActive = false;
            v.UpdatedAt = DateTime.UtcNow;
            v.UpdatedBy = user;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
