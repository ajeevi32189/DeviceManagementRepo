using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  5D VIEW LAYOUT DTOs
    //
    //  Soch samajh kar ek hi "scene" call rakhi gayi hai.
    //  5D View ek saath 97 device, 300 cable, 46 rack aur 48 tile
    //  dikhata hai. Agar har cheez ki alag API call hoti to view
    //  khulne me 400 request lagti aur 20-30 second lag jaate.
    //  Isliye:
    //      GET  /api/Layout/floor/{floorId}        -> poora scene, ek call
    //      POST /api/Layout/floor/{floorId}/save   -> poora scene, ek call
    //      PATCH .../placement/{id}/transform      -> ek device hilaya (halka)
    //
    //  Naap ki ek hi bhasha: SAB kuch METRES me hai (device ke apne
    //  size ko chhod kar, wo mm me hai kyunki datasheet mm me hoti hai).
    //  Coordinates building ke centre se nape jaate hain:
    //      X -> East-West, Z -> North-South, Y -> upar (0 = finished floor)
    // ══════════════════════════════════════════════════════════════

    // ───────────────────────────────────────────── DATA CENTRE
    public class DataCenterCreateDto
    {
        [Required, MaxLength(150)] public string Name { get; set; } = null!;
        [Required, MaxLength(50)] public string Code { get; set; } = null!;

        [Range(0, double.MaxValue)] public decimal? LengthM { get; set; }
        [Range(0, double.MaxValue)] public decimal? BreadthM { get; set; }
        [Range(0, double.MaxValue)] public decimal? HeightM { get; set; }
        [MaxLength(10)] public string? LengthUnit { get; set; }
        [MaxLength(10)] public string? BreadthUnit { get; set; }

        public decimal? MarginTop { get; set; }
        public decimal? MarginBottom { get; set; }
        public decimal? MarginLeft { get; set; }
        public decimal? MarginRight { get; set; }
        [MaxLength(10)] public string? MarginUnit { get; set; }

        [MaxLength(10)] public string? DoorDirection { get; set; }
        [MaxLength(10)] public string? DoorPosition { get; set; }

        public decimal? TotalKW { get; set; }
        [MaxLength(10)] public string? TotalKWUnit { get; set; }
        public decimal? PowerCapacityKW { get; set; }
        public decimal? CoolingCapacityKW { get; set; }
        public int? TotalRacks { get; set; }
        public int? UtilizedRacks { get; set; }

        [MaxLength(200)] public string? LocationName { get; set; }
        [MaxLength(300)] public string? StreetAddress { get; set; }
        [MaxLength(20)] public string? PinCode { get; set; }
        public Guid? CountryId { get; set; }
        public Guid? StateId { get; set; }
        public Guid? CityId { get; set; }
        public Guid? SiteId { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? ContactPersonId { get; set; }

        [Range(-90, 90)] public decimal? Latitude { get; set; }
        [Range(-180, 180)] public decimal? Longitude { get; set; }

        [MaxLength(30)] public string? Status { get; set; }
        [MaxLength(20)] public string? SetupMode { get; set; }
        public Guid? DxfFileId { get; set; }
    }

    public class DataCenterUpdateDto : DataCenterCreateDto
    {
        public bool? IsActive { get; set; }
    }

    public class DataCenterResponseDto : DataCenterCreateDto
    {
        public Guid Id { get; set; }
        public bool IsActive { get; set; }
        public int FloorCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ───────────────────────────────────────────── FLOOR (scene header)
    public class LayoutFloorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid? DataCenterId { get; set; }
        public decimal? WidthM { get; set; }
        public decimal? LengthM { get; set; }
        public decimal? HeightM { get; set; }
        public decimal? RaisedFloorHeightM { get; set; }
        public decimal? TileSizeM { get; set; }
        public int? LevelNumber { get; set; }
    }

    // ───────────────────────────────────────────── ROOM
    public class LayoutRoomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? RoomType { get; set; }
        public decimal? PositionX { get; set; }
        /// <summary>Floor-plan ka Z. Column ka naam purane karan se PositionY hai.</summary>
        public decimal? PositionZ { get; set; }
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }
        public decimal? Height { get; set; }
        public decimal? RotationAngle { get; set; }
        public string? ColorHex { get; set; }
        public string? Icon { get; set; }
        public decimal? PowerBudgetKW { get; set; }
        public string? DimensionUnit { get; set; }
        public string? MarginsJson { get; set; }
        public string? InnerMarginsJson { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    // ───────────────────────────────────────────── RACK
    public class LayoutRackDto
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public string RackName { get; set; } = null!;
        public int TotalUHeight { get; set; }
        public string? Status { get; set; }
        public decimal? PositionX { get; set; }
        public decimal? PositionZ { get; set; }
        public decimal? RotationAngle { get; set; }
        public decimal? WidthMm { get; set; }
        public decimal? DepthMm { get; set; }
        public decimal? HeightMm { get; set; }
        public string? RowName { get; set; }
        public string? AisleName { get; set; }
        public string? FacingDirection { get; set; }
        public string? Shape { get; set; }
        public string? ColorHex { get; set; }
        public string? LabelText { get; set; }
        public bool IsActive { get; set; }

        // Rack ka power — 5D ke power layer ke liye saath hi bhej dete hain
        public decimal? RatedCapacityKW { get; set; }
        public decimal? EstimatedLoadKW { get; set; }
        public decimal? MeasuredLoadKW { get; set; }
    }

    // ───────────────────────────────────────────── RACK SLOT (rack ke andar)
    public class RackSlotDto
    {
        public Guid Id { get; set; }
        public Guid? RackId { get; set; }
        public Guid DeviceDetailId { get; set; }
        public string? DeviceName { get; set; }
        public int? StartUPosition { get; set; }
        public int? UOccupied { get; set; }
        /// <summary>front | rear</summary>
        public string? Face { get; set; }
        public string? PortsJson { get; set; }
        public Guid? FaceImageFileId { get; set; }
        public string? ColorHex { get; set; }
        public bool IsActive { get; set; }
    }

    // ───────────────────────────────────────────── FLOOR DEVICE
    public class FloorPlacementDto
    {
        public Guid Id { get; set; }
        public Guid? DeviceDetailId { get; set; }
        public Guid? RoomId { get; set; }
        public Guid? DeviceTypeId { get; set; }

        public decimal PositionX { get; set; }
        public decimal PositionZ { get; set; }
        public decimal PositionY { get; set; }
        public decimal RotationAngle { get; set; }
        public string Level { get; set; } = "floor";

        public decimal? LengthMm { get; set; }
        public decimal? WidthMm { get; set; }
        public decimal? HeightMm { get; set; }

        public string? Shape { get; set; }
        public string? ColorHex { get; set; }
        public string? IconName { get; set; }
        public string? LabelText { get; set; }
        public string? RowName { get; set; }
        public string? AisleName { get; set; }

        public bool IsPinned { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsProvisional { get; set; }
        public string? PlaceholderName { get; set; }
        public int SortOrder { get; set; }
        public string? MetaJson { get; set; }
        public bool IsActive { get; set; } = true;

        // Read-only — asset ka naam, sirf dikhane ke liye
        public string? DeviceName { get; set; }
        public string? DeviceTypeName { get; set; }
        public string? CategoryName { get; set; }
    }

    /// <summary>Sirf hilane ke liye halka DTO — drag khatam hone par ek baar.</summary>
    public class PlacementTransformDto
    {
        public decimal? PositionX { get; set; }
        public decimal? PositionZ { get; set; }
        public decimal? PositionY { get; set; }
        [Range(0, 360)] public decimal? RotationAngle { get; set; }
        public Guid? RoomId { get; set; }
        [MaxLength(20)] public string? Level { get; set; }
        /// <summary>Edit lease ki pehchan — bina iske lock wale floor par save nahi hoga.</summary>
        [MaxLength(100)] public string? EditSessionId { get; set; }
    }

    // ───────────────────────────────────────────── TRAY / CABLE / PIPE / TILE
    public class CableTrayDto
    {
        public Guid Id { get; set; }
        public Guid? RoomId { get; set; }
        public string TrayCode { get; set; } = null!;
        public string? Name { get; set; }
        public string Axis { get; set; } = "x";
        public decimal Line { get; set; }
        public decimal FromCoord { get; set; }
        public decimal ToCoord { get; set; }
        public decimal WidthM { get; set; }
        public int Slots { get; set; }
        public decimal PitchM { get; set; }
        public int NearSide { get; set; }
        public string Level { get; set; } = "underfloor";
        public decimal ElevationY { get; set; }
        public string TrayType { get; set; } = "mixed";
        public string? ColorHex { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CableDto
    {
        public Guid Id { get; set; }
        public Guid? FromPlacementId { get; set; }
        public Guid? FromDeviceDetailId { get; set; }
        public string? FromEndpoint { get; set; }
        public string? FromFace { get; set; }
        public Guid? ToPlacementId { get; set; }
        public Guid? ToDeviceDetailId { get; set; }
        public string? ToEndpoint { get; set; }
        public string? ToFace { get; set; }
        public string CableType { get; set; } = "power";
        public string? LayerKey { get; set; }
        public string? TrayPathJson { get; set; }
        public string? WaypointsJson { get; set; }
        public decimal? LengthM { get; set; }
        public string? ColorHex { get; set; }
        public string? Label { get; set; }
        public string Status { get; set; } = "installed";
        public Guid? NetworkConnectionId { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PipeDto
    {
        public Guid Id { get; set; }
        public Guid? FromPlacementId { get; set; }
        public Guid? FromDeviceDetailId { get; set; }
        public Guid? ToPlacementId { get; set; }
        public Guid? ToDeviceDetailId { get; set; }
        public string PipeType { get; set; } = "chilled-water-supply";
        public decimal? DiameterMm { get; set; }
        public string? RouteJson { get; set; }
        public string? WaypointsJson { get; set; }
        public decimal? FromOffset { get; set; }
        public decimal? ToOffset { get; set; }
        public decimal ElevationY { get; set; }
        public decimal? LengthM { get; set; }
        public string? ColorHex { get; set; }
        public string? Label { get; set; }
        public string Status { get; set; } = "installed";
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class FloorTileDto
    {
        public Guid Id { get; set; }
        public Guid? RoomId { get; set; }
        public string TileType { get; set; } = "perforated";
        public decimal PositionX { get; set; }
        public decimal PositionZ { get; set; }
        public decimal SizeM { get; set; }
        public decimal? AirflowCfm { get; set; }
        public string? AisleType { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Device type ka 3D roop — shape, default naap, rang, garmi.
    /// Scene ke saath isliye bhejte hain ki frontend ko alag call na karni pade.
    /// </summary>
    public class DeviceTypeShapeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid DeviceCategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Shape { get; set; }
        public string? Icon { get; set; }
        public string? IconColor { get; set; }
        public string? LayerKey { get; set; }
        public decimal? DefaultLengthMm { get; set; }
        public decimal? DefaultWidthMm { get; set; }
        public decimal? DefaultHeightMm { get; set; }
        public decimal? HeatKW { get; set; }
        public decimal? CoolingKW { get; set; }
    }

    // ───────────────────────────────────────────── LOCK / EDIT MODE
    public class LayoutLockDto
    {
        public Guid FloorId { get; set; }
        public Guid? DataCenterId { get; set; }

        /// <summary>true = layout freeze. Kuch bhi hilega nahi.</summary>
        public bool IsLocked { get; set; }
        public string? LockedBy { get; set; }
        public DateTime? LockedAt { get; set; }
        public string? LockNote { get; set; }

        /// <summary>Abhi kaun edit kar raha hai (NULL = koi nahi).</summary>
        public string? EditingBy { get; set; }
        public string? EditingSessionId { get; set; }
        public DateTime? EditingSince { get; set; }
        public DateTime? EditingExpiresAt { get; set; }

        /// <summary>Jo abhi call kar raha hai, kya wahi edit kar raha hai.</summary>
        public bool IsEditableByMe { get; set; }

        /// <summary>Frontend ko seedha batane ke liye — view | edit | locked.</summary>
        public string Mode { get; set; } = "view";

        public int Revision { get; set; }
        public DateTime? LastSavedAt { get; set; }
        public string? LastSavedBy { get; set; }
    }

    public class LayoutLockRequestDto
    {
        /// <summary>Lock laga rahe hain ya hata rahe hain.</summary>
        public bool IsLocked { get; set; }
        [MaxLength(300)] public string? LockNote { get; set; }
    }

    public class EditSessionRequestDto
    {
        /// <summary>Browser tab ki pehchan — frontend banata hai (GUID chalega).</summary>
        [Required, MaxLength(100)] public string EditSessionId { get; set; } = null!;

        /// <summary>Lease kitne minute ki. Default 10, zyada se zyada 60.</summary>
        [Range(1, 60)] public int? LeaseMinutes { get; set; }

        /// <summary>
        /// true = doosre ka chhoda hua (expire ho chuka) lease zabardasti le lo.
        /// Lease zinda ho to phir bhi mana kar diya jayega.
        /// </summary>
        public bool Force { get; set; } = false;
    }

    // ───────────────────────────────────────────── SCENE (poora)
    public class LayoutCountsDto
    {
        public int Rooms { get; set; }
        public int Racks { get; set; }
        public int RackSlots { get; set; }
        public int FloorDevices { get; set; }
        public int Trays { get; set; }
        public int Cables { get; set; }
        public int Pipes { get; set; }
        public int Tiles { get; set; }
    }

    public class LayoutSceneDto
    {
        public DataCenterResponseDto? DataCenter { get; set; }
        public LayoutFloorDto Floor { get; set; } = new();
        public List<LayoutRoomDto> Rooms { get; set; } = new();
        public List<LayoutRackDto> Racks { get; set; } = new();
        public List<RackSlotDto> RackSlots { get; set; } = new();
        public List<FloorPlacementDto> FloorDevices { get; set; } = new();
        public List<CableTrayDto> Trays { get; set; } = new();
        public List<CableDto> Cables { get; set; } = new();
        public List<PipeDto> Pipes { get; set; } = new();
        public List<FloorTileDto> Tiles { get; set; } = new();
        public List<DeviceTypeShapeDto> DeviceTypes { get; set; } = new();
        public LayoutLockDto Lock { get; set; } = new();
        public LayoutCountsDto Counts { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Poora scene save. Har list OPTIONAL hai:
    ///   null  -> us cheez ko haath mat lagao
    ///   list  -> jo bheja hai wahi sach hai (jiska Id khaali hai wo naya,
    ///            jiska Id hai wo update; aur jo DeletedIds me hai wo hatao)
    /// </summary>
    public class LayoutSaveDto
    {
        /// <summary>Jo revision frontend ke paas tha. Mel na khaye to 409 milega.</summary>
        public int? ExpectedRevision { get; set; }

        /// <summary>Edit lease ki pehchan — iske bina save mana ho jayega.</summary>
        [Required, MaxLength(100)] public string EditSessionId { get; set; } = null!;

        public LayoutFloorDto? Floor { get; set; }
        public List<LayoutRoomDto>? Rooms { get; set; }
        public List<LayoutRackDto>? Racks { get; set; }
        public List<RackSlotDto>? RackSlots { get; set; }
        public List<FloorPlacementDto>? FloorDevices { get; set; }
        public List<CableTrayDto>? Trays { get; set; }
        public List<CableDto>? Cables { get; set; }
        public List<PipeDto>? Pipes { get; set; }
        public List<FloorTileDto>? Tiles { get; set; }

        public List<Guid>? DeletedRoomIds { get; set; }
        public List<Guid>? DeletedRackIds { get; set; }
        public List<Guid>? DeletedRackSlotIds { get; set; }
        public List<Guid>? DeletedFloorDeviceIds { get; set; }
        public List<Guid>? DeletedTrayIds { get; set; }
        public List<Guid>? DeletedCableIds { get; set; }
        public List<Guid>? DeletedPipeIds { get; set; }
        public List<Guid>? DeletedTileIds { get; set; }
    }

    public class LayoutSaveResultDto
    {
        public bool Success { get; set; }
        public int Revision { get; set; }
        public LayoutCountsDto Created { get; set; } = new();
        public LayoutCountsDto Updated { get; set; } = new();
        public LayoutCountsDto Deleted { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
        public long ElapsedMs { get; set; }
    }

    // ───────────────────────────────────────────── SAVED VIEWS
    public class LayoutViewCreateDto
    {
        [Required, MaxLength(150)] public string Name { get; set; } = null!;
        [MaxLength(500)] public string? Description { get; set; }
        public string? CameraJson { get; set; }
        public string? LayerStateJson { get; set; }
        public string? FilterStateJson { get; set; }
        public Guid? ThumbnailFileId { get; set; }
        public bool IsShared { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public class LayoutViewResponseDto : LayoutViewCreateDto
    {
        public Guid Id { get; set; }
        public Guid FloorId { get; set; }
        public Guid? DataCenterId { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
