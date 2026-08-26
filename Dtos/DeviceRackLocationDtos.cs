using System.ComponentModel.DataAnnotations;

namespace DeviceManagementOnly.Dtos
{
    // ══════════════════════════════════════════════════════════════
    //  DEVICE RACK LOCATION DTOs
    //  (Rack + U-position based — DeviceLocationDtos.cs se separate)
    // ══════════════════════════════════════════════════════════════

    public class DeviceRackLocationCreateDto
    {
        [Required(ErrorMessage = "DeviceDetailId required hai.")]
        public Guid DeviceDetailId { get; set; }

        public Guid? RackId { get; set; }

        [Range(1, 60, ErrorMessage = "StartUPosition 1 se 60 ke beech hona chahiye.")]
        public int? StartUPosition { get; set; }

        [Range(1, 60, ErrorMessage = "UOccupied 1 se 60 ke beech hona chahiye.")]
        public int? UOccupied { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(20)]
        public string? PinCode { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude -90 se 90 ke beech honi chahiye.")]
        public decimal? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude -180 se 180 ke beech honi chahiye.")]
        public decimal? Longitude { get; set; }
    }

    public class DeviceRackLocationUpdateDto
    {
        public Guid? DeviceDetailId { get; set; }
        public Guid? RackId { get; set; }

        [Range(1, 60)]
        public int? StartUPosition { get; set; }

        [Range(1, 60)]
        public int? UOccupied { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(20)]
        public string? PinCode { get; set; }

        [Range(-90, 90)]
        public decimal? Latitude { get; set; }

        [Range(-180, 180)]
        public decimal? Longitude { get; set; }

        public bool? IsActive { get; set; }
    }

    public class DeviceRackLocationResponseDto
    {
        public Guid Id { get; set; }
        public Guid DeviceDetailId { get; set; }
        public string? DeviceShortName { get; set; }

        public Guid? RackId { get; set; }
        public string? RackName { get; set; }

        public int? StartUPosition { get; set; }
        public int? UOccupied { get; set; }

        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? PinCode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
