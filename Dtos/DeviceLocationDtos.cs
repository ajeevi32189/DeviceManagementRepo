namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  DEVICE LOCATION DTOs
    //  (Company API ke Location wale pattern jaisa hi — Images/Documents
    //  ke liye already maujood ImageUploadDto/DocumentUploadDto reuse kiye)
    // ══════════════════════════════════════════════════════════════

    public class DeviceLocationCreateDto
    {
        public Guid DeviceDetailId { get; set; }

        public string? LocationType { get; set; }   // Primary, Secondary, Site, Warehouse

        public string AddressLine1 { get; set; } = null!;
        public string? AddressLine2 { get; set; }

        public Guid? CountryId { get; set; }
        public Guid? StateId { get; set; }
        public Guid? CityId { get; set; }

        public int? PinCode { get; set; }
        public string? Landmark { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public Guid? TimeZoneId { get; set; }
        public string? LocationContactNumber { get; set; }

        // File uploads
        public List<ImageUploadDto>? Images { get; set; } = new();
        public List<DocumentUploadDto>? Documents { get; set; } = new();
    }

    public class DeviceLocationUpdateDto
    {
        public Guid DeviceDetailId { get; set; }

        public string? LocationType { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }

        public Guid? CountryId { get; set; }
        public Guid? StateId { get; set; }
        public Guid? CityId { get; set; }

        public int? PinCode { get; set; }
        public string? Landmark { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public Guid? TimeZoneId { get; set; }
        public string? LocationContactNumber { get; set; }

        public bool? IsActive { get; set; }

        // Naye files add karne ke liye
        public List<ImageUploadDto>? Images { get; set; } = new();
        public List<DocumentUploadDto>? Documents { get; set; } = new();
    }

    public class DeviceLocationResponseDto
    {
        public Guid Id { get; set; }
        public Guid DeviceDetailId { get; set; }

        public string? LocationType { get; set; }

        public string AddressLine1 { get; set; } = null!;
        public string? AddressLine2 { get; set; }

        public Guid? CountryId { get; set; }
        public Guid? StateId { get; set; }
        public Guid? CityId { get; set; }

        public int? PinCode { get; set; }
        public string? Landmark { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public Guid? TimeZoneId { get; set; }
        public string? LocationContactNumber { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<FileStoreResponseDto> Images { get; set; } = new();
        public List<FileStoreResponseDto> Documents { get; set; } = new();
    }
}