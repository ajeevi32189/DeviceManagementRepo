namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  DEVICE CATEGORY
    // ══════════════════════════════════════════════════════════════

    public class DeviceCategoryCreateDto
    {
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public string? Remarks { get; set; }
    }

    public class DeviceCategoryUpdateDto
    {
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public string? Remarks { get; set; }
    }

    public class DeviceCategoryResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class DeviceCategoryImportDto
    {
        public IFormFile File { get; set; } = null!;
    }

    public class DeviceCategoryImportResponse
    {
        public int TotalImported { get; set; }
        public int TotalSkipped { get; set; }
        public List<string> ImportedCategories { get; set; } = new();
        public List<string> SkippedCategories { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════
    //  DEVICE TYPE
    // ══════════════════════════════════════════════════════════════

    public class DeviceTypeCreateDto
    {
        public string Name { get; set; } = null!;
        public Guid DeviceCategoryId { get; set; }
        public string? Protocol { get; set; }
        public string? SupportedFeature { get; set; }
        public string? Icon { get; set; }
        public string? IconColor { get; set; }
        public string? Remarks { get; set; }
    }

    public class DeviceTypeUpdateDto
    {
        public string Name { get; set; } = null!;
        public Guid DeviceCategoryId { get; set; }
        public string? Protocol { get; set; }
        public string? SupportedFeature { get; set; }
        public string? Icon { get; set; }
        public string? IconColor { get; set; }
        public string? Remarks { get; set; }
    }

    public class DeviceTypeResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid DeviceCategoryId { get; set; }
        public string? Protocol { get; set; }
        public string? SupportedFeature { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class DeviceTypeImportDto
    {
        public IFormFile File { get; set; } = null!;
    }

    public class DeviceTypeImportResponse
    {
        public int TotalImported { get; set; }
        public int TotalSkipped { get; set; }
        public List<string> ImportedDeviceTypes { get; set; } = new();
        public List<string> SkippedDeviceTypes { get; set; } = new();
        public List<string> CreatedCategories { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════
    //  MODEL CATEGORY
    // ══════════════════════════════════════════════════════════════

    public class ModelCategoryCreateDto
    {
        public string Name { get; set; } = null!;
        public string? Remarks { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ModelCategoryUpdateDto
    {
        public string Name { get; set; } = null!;
        public string? Remarks { get; set; }
        public bool IsActive { get; set; }
    }

    public class ModelCategoryResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Remarks { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  MODEL SPECIFICATION
    // ══════════════════════════════════════════════════════════════

    public class ModelSpecificationCreateDto
    {
        public string Name { get; set; } = null!;
        public Guid? DeviceTypeId { get; set; }
        public string? ModelNumber { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public List<ModelParameterEmbedDto> Parameters { get; set; } = new();
    }

    public class ModelSpecificationUpdateDto
    {
        public string Name { get; set; } = null!;
        public Guid? DeviceTypeId { get; set; }
        public string? ModelNumber { get; set; }
        public string? Remarks { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class ModelSpecificationResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid? DeviceTypeId { get; set; }
        public string? ModelNumber { get; set; }
        public string? Remarks { get; set; }

        // ── Isme direct field nahi hai — DeviceMaster.ModelSpecificationId ke
        //    through join karke nikaali jaati hai (ek spec multiple companies
        //    use kar sakti hain, isliye list) ──
        public List<Guid> CompanyIds { get; set; } = new();

        public bool IsDeleted { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  MODEL PARAMETER
    // ══════════════════════════════════════════════════════════════

    public class ModelParameterEmbedDto
    {
        public Guid ModelCategoryId { get; set; }
        public string ParameterName { get; set; } = null!;
        public string ParameterValue { get; set; } = null!;
        public string? Unit { get; set; }
        public string? Capacity { get; set; }
        public string? Processor { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public string? Intro { get; set; }
        public string? Uses { get; set; }
        public string? Feature { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? Protocol { get; set; }
        public string? WarrantyDuration { get; set; }
        public string? EndOfLife { get; set; }
        public string? Remarks { get; set; }
    }

    public class ModelParameterCreateDto
    {
        public Guid DeviceModelId { get; set; }
        public Guid ModelCategoryId { get; set; }
        public string ParameterName { get; set; } = null!;
        public string ParameterValue { get; set; } = null!;
        public string? Unit { get; set; }
        public string? Capacity { get; set; }
        public string? Processor { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public string? Intro { get; set; }
        public string? Uses { get; set; }
        public string? Feature { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? Protocol { get; set; }
        public string? WarrantyDuration { get; set; }
        public string? EndOfLife { get; set; }
        public string? Remarks { get; set; }
    }

    public class ModelParameterUpdateDto
    {
        public Guid ModelCategoryId { get; set; }
        public string ParameterName { get; set; } = null!;
        public string ParameterValue { get; set; } = null!;
        public string? Unit { get; set; }
        public string? Capacity { get; set; }
        public string? Processor { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public string? Intro { get; set; }
        public string? Uses { get; set; }
        public string? Feature { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? Protocol { get; set; }
        public string? WarrantyDuration { get; set; }
        public string? EndOfLife { get; set; }
        public string? Remarks { get; set; }
    }

    public class ModelParameterResponseDto
    {
        public Guid Id { get; set; }
        public Guid DeviceModelId { get; set; }
        public Guid ModelCategoryId { get; set; }
        public string ModelCategoryName { get; set; } = null!;
        public string ParameterName { get; set; } = null!;
        public string ParameterValue { get; set; } = null!;
        public string? Unit { get; set; }
        public string? Capacity { get; set; }
        public string? Processor { get; set; }
        public string? RAM { get; set; }
        public string? Storage { get; set; }
        public string? Intro { get; set; }
        public string? Uses { get; set; }
        public string? Feature { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? Protocol { get; set; }
        public string? WarrantyDuration { get; set; }
        public string? EndOfLife { get; set; }
        public string? Remarks { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  DEVICE MASTER
    // ══════════════════════════════════════════════════════════════

    public class DeviceMasterCreateDto
    {
        public Guid? DeviceTypeId { get; set; }
        public Guid? DeviceCategoryId { get; set; }
        /// <summary>External reference — Guid from Company microservice.</summary>
        public Guid? CompanyId { get; set; }
        public Guid? ModelSpecificationId { get; set; }
        public string? DeviceShortName { get; set; }
        public string? DeviceLongName { get; set; }
        public string? Remarks { get; set; }
        public string? DataFormat { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class DeviceMasterUpdateDto
    {
        public Guid? DeviceTypeId { get; set; }
        public Guid? DeviceCategoryId { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? ModelSpecificationId { get; set; }
        public string? DeviceShortName { get; set; }
        public string? DeviceLongName { get; set; }
        public string? Remarks { get; set; }
        public string? DataFormat { get; set; }
    }

    public class DeviceMasterResponseDto
    {
        public Guid Id { get; set; }
        public Guid? DeviceTypeId { get; set; }
        public Guid? DeviceCategoryId { get; set; }
        public Guid? ModelSpecificationId { get; set; }
        public Guid? CompanyId { get; set; }
        public string? DeviceShortName { get; set; }
        public string? DeviceLongName { get; set; }
        public string? Remarks { get; set; }
        public string? DataFormat { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  DEVICE DETAIL
    // ══════════════════════════════════════════════════════════════

    public class DeviceDetailCreateDto
    {
        public Guid DeviceMasterId { get; set; }
        public Guid? ParentDeviceDetailId { get; set; }
        public string? IMEI { get; set; }
        public string? MACAddress { get; set; }
        public string? TagNumber { get; set; }
        public string? ShortName { get; set; }
        public string? LongName { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public decimal? PurchaseCost { get; set; }
        public string? Remarks { get; set; }
        public string? IPAddress { get; set; }
        public string? SIMNumber { get; set; }
    }

    public class DeviceDetailUpdateDto
    {
        public Guid DeviceMasterId { get; set; }
        public string? IMEI { get; set; }
        public string? MACAddress { get; set; }
        public string? TagNumber { get; set; }
        public string? ShortName { get; set; }
        public string? LongName { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public decimal? PurchaseCost { get; set; }
        public string? Remarks { get; set; }
        public string? IPAddress { get; set; }
        public string? SIMNumber { get; set; }
        public string? RFIDTagNumber { get; set; }
    }

    public class DeviceDetailResponseDto
    {
        public Guid Id { get; set; }
        public Guid DeviceMasterId { get; set; }
        public Guid? ParentDeviceDetailId { get; set; }
        public string? ParentDeviceShortName { get; set; }
        public string? ParentSerialNumber { get; set; }

        // Joined from DeviceMaster
        public Guid? DeviceCategoryId { get; set; }
        public Guid? DeviceTypeId { get; set; }
        public string? DeviceTypeName { get; set; }
        public string? DeviceCategoryName { get; set; }
        public Guid? CompanyId { get; set; }   // External Guid reference

        // Device Identification
        public string? IMEI { get; set; }
        public string? MACAddress { get; set; }
        public string? TagNumber { get; set; }
        public string? ShortName { get; set; }
        public string? LongName { get; set; }
        public string? SerialNumber { get; set; }

        // Purchase Details
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public decimal? PurchaseCost { get; set; }
        public string? Remarks { get; set; }

        // Network Details
        public string? IPAddress { get; set; }
        public string? SIMNumber { get; set; }
        public string? RFIDTagNumber { get; set; }

        public List<SubDeviceDto> SubDevices { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SubDeviceDto
    {
        public Guid Id { get; set; }
        public Guid? ParentDeviceDetailId { get; set; }
        public string? ShortName { get; set; }
        public string? SerialNumber { get; set; }
        public string? IMEI { get; set; }
        public string? DeviceMasterName { get; set; }
        public Guid DeviceMasterId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AssociateDeviceDto
    {
        public List<Guid> ChildDeviceDetailIds { get; set; } = new();
    }

    public class AssociateResultDto
    {
        public Guid ChildDeviceDetailId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public SubDeviceDto? Data { get; set; }
    }

    public class DeviceDropdownDto
    {
        public Guid Id { get; set; }
        public string? ShortName { get; set; }
        public string? SerialNumber { get; set; }
        public string? DeviceMasterName { get; set; }
        public bool IsAlreadyAssigned { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  DEVICE MODEL MAPPING
    // ══════════════════════════════════════════════════════════════

    public class BulkAssignmentResponseDto
    {
        public string Message { get; set; } = null!;
        public Guid DeviceTypeId { get; set; }
        public List<Guid> SuccessfullyAssignedIds { get; set; } = new();
        public List<Guid> SkippedOrInvalidIds { get; set; } = new();
    }

    public class DeviceModelDetailsDto
    {
        public Guid MappingId { get; set; }
        public Guid DeviceTypeId { get; set; }
        public string? DeviceTypeName { get; set; }
        public string? DeviceShortName { get; set; }
        public string? DeviceLongName { get; set; }
        public Guid ModelSpecificationId { get; set; }
        public string? ModelName { get; set; }
    }

    public class ModelDetailsDto
    {
        public Guid ModelId { get; set; }
        public string? ModelName { get; set; }
        public string? ModelNumber { get; set; }
        public Guid DeviceTypeId { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  UNIT MASTER
    // ══════════════════════════════════════════════════════════════

    public class UnitMasterCreateDto
    {
        public string UnitName { get; set; } = null!;
    }

    public class UnitMasterUpdateDto
    {
        public string UnitName { get; set; } = null!;
    }

    public class UnitMasterResponseDto
    {
        public Guid Id { get; set; }
        public string UnitName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  TRACKING
    // ══════════════════════════════════════════════════════════════

    public class DeviceTrackingInputDto
    {
        public string? SerialNumber { get; set; }
        public string? RFIDTagNumber { get; set; }
    }

    public class DeviceTrackingResponseDto
    {
        public string Message { get; set; } = null!;
        public Guid DeviceMasterId { get; set; }
        public string? RFIDTagNumber { get; set; }
        public string? BarcodeText { get; set; }
        public string? DeviceShortName { get; set; }
        public Guid? CompanyId { get; set; }
        public string? ModelNumber { get; set; }
        public string? SerialNumber { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  EXCEL IMPORT
    // ══════════════════════════════════════════════════════════════

    public class DeviceDetailImportDto
    {
        public IFormFile File { get; set; } = null!;
    }

    public class DeviceDetailImportResponse
    {
        public List<string> CategoriesCreated { get; set; } = new();
        public List<string> DeviceTypesCreated { get; set; } = new();
        public List<string> ModelsCreated { get; set; } = new();
        public List<string> DeviceMastersCreated { get; set; } = new();
        public List<string> DevicesImported { get; set; } = new();
        public List<string> SkippedDevices { get; set; } = new();
    }



    //public class DeviceLocationCreateDto
    //{
    //    public string? LocationType { get; set; }
    //    public string AddressLine1 { get; set; } = null!;
    //    public string? AddressLine2 { get; set; }
    //    public Guid? CountryId { get; set; }
    //    public Guid? StateId { get; set; }
    //    public Guid? CityId { get; set; }
    //    public int? PinCode { get; set; }
    //    public string? Landmark { get; set; }
    //    public decimal? Latitude { get; set; }
    //    public decimal? Longitude { get; set; }
    //    public Guid? TimeZoneId { get; set; }
    //    public string? LocationContactNumber { get; set; }
    //    public string? EntityType { get; set; }

    //}

    ///// <summary>
    ///// POST /api/devicelocation/update/{id}
    ///// PUT ke jagah POST — same body, location update karo
    ///// </summary>
    //public class DeviceLocationUpdateDto
    //{
    //    public string? LocationType { get; set; }
    //    public string AddressLine1 { get; set; } = null!;
    //    public string? AddressLine2 { get; set; }

    //    public Guid? StateId { get; set; }
    //    public Guid? CityId { get; set; }
    //    public int? PinCode { get; set; }
    //    public string? Landmark { get; set; }
    //    public decimal? Latitude { get; set; }
    //    public decimal? Longitude { get; set; }
    //    public Guid? TimeZoneId { get; set; }
    //    public string? LocationContactNumber { get; set; }
    //    public string? EntityType { get; set; }

    //}

    //public class DeviceLocationResponseDto
    //{
    //    public Guid Id { get; set; }
    //    public Guid DeviceDetailId { get; set; }
    //    public string? LocationType { get; set; }
    //    public string AddressLine1 { get; set; } = null!;
    //    public string? AddressLine2 { get; set; }
    //    public Guid? CountryId { get; set; }
    //    public Guid? StateId { get; set; }
    //    public Guid? CityId { get; set; }
    //    public int? PinCode { get; set; }
    //    public string? Landmark { get; set; }
    //    public decimal? Latitude { get; set; }
    //    public decimal? Longitude { get; set; }
    //    public Guid? TimeZoneId { get; set; }
    //    public string? LocationContactNumber { get; set; }
    //    public string? EntityType { get; set; }
    //    public bool IsActive { get; set; }

    //    public DateTime CreatedAt { get; set; }

    //    public DateTime? UpdatedAt { get; set; }
    //}

}
