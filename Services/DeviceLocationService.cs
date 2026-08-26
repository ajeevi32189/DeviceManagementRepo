using DeviceManagementOnly.Common;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models;
using DeviceManagementOnly.Models.Device;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  DEVICE LOCATION
    //  Company API ke LocationService jaisa hi logic — bas yahan
    //  IFileStoreService.UploadFile() use kiya hai jo file ko disk pe
    //  bhi save karta hai (Company wale me sirf DB record banta tha,
    //  FilePath khaali reh jaata tha — yahan woh gap nahi hai).
    // ══════════════════════════════════════════════════════════════

    public interface IDeviceLocationService
    {
        Task<DeviceLocationResponseDto> CreateDeviceLocationAsync(DeviceLocationCreateDto dto, string uploadBasePath);
        Task<PagedResult<DeviceLocationResponseDto>> GetAllDeviceLocationAsync(PaginationParams p);
        Task<PagedResult<DeviceLocationResponseDto>> GetDeviceLocationByDeviceAsync(Guid deviceDetailId, PaginationParams p);
        Task<DeviceLocationResponseDto?> GetDeviceLocationByIdAsync(Guid id);
        Task<DeviceLocationResponseDto?> UpdateDeviceLocationAsync(Guid id, DeviceLocationUpdateDto dto, string uploadBasePath);
        Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy);
    }

    public class DeviceLocationService : IDeviceLocationService
    {
        private readonly DBContext _db;
        private readonly IFileStoreService _fileStoreService;

        public DeviceLocationService(DBContext db, IFileStoreService fileStoreService)
        {
            _db = db;
            _fileStoreService = fileStoreService;
        }

        private async Task<Guid> GetDeviceLocationEntityTypeIdAsync()
        {
            var id = await _db.EntityTypes
                .Where(x => x.EntityName == EntityTypeNames.DeviceLocation)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (id == Guid.Empty)
                throw new InvalidOperationException(
                    $"EntityType '{EntityTypeNames.DeviceLocation}' not exist in DB. Firstly create entity type for DeviceLocation.");

            return id;
        }

        public async Task<DeviceLocationResponseDto> CreateDeviceLocationAsync(DeviceLocationCreateDto dto, string uploadBasePath)
        {
            var deviceDetail = await _db.DeviceDetails.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == dto.DeviceDetailId && !d.IsDeleted);

            if (deviceDetail is null)
                throw new InvalidOperationException($"DeviceDetail Id={dto.DeviceDetailId} not found.");

            var location = new DeviceLocation
            {
                DeviceDetailId = dto.DeviceDetailId,
                LocationType = dto.LocationType,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                CountryId = dto.CountryId,
                StateId = dto.StateId,
                CityId = dto.CityId,
                PinCode = dto.PinCode,
                Landmark = dto.Landmark,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                TimeZoneId = dto.TimeZoneId,
                LocationContactNumber = dto.LocationContactNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.DeviceLocations.Add(location);
            await _db.SaveChangesAsync();

            await UploadFilesAsync(location.Id, dto.Images, dto.Documents, uploadBasePath);

            return await MapAsync(location);
        }

        public async Task<DeviceLocationResponseDto?> UpdateDeviceLocationAsync(Guid id, DeviceLocationUpdateDto dto, string uploadBasePath)
        {
            var location = await _db.DeviceLocations.FirstOrDefaultAsync(x => x.Id == id);
            if (location is null) return null;

            location.DeviceDetailId = dto.DeviceDetailId;
            location.LocationType = dto.LocationType;
            location.AddressLine1 = dto.AddressLine1 ?? location.AddressLine1;
            location.AddressLine2 = dto.AddressLine2;
            location.CountryId = dto.CountryId;
            location.StateId = dto.StateId;
            location.CityId = dto.CityId;
            location.PinCode = dto.PinCode;
            location.Landmark = dto.Landmark;
            location.Latitude = dto.Latitude;
            location.Longitude = dto.Longitude;
            location.TimeZoneId = dto.TimeZoneId;
            location.LocationContactNumber = dto.LocationContactNumber;
            if (dto.IsActive.HasValue) location.IsActive = dto.IsActive.Value;
            location.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await UploadFilesAsync(location.Id, dto.Images, dto.Documents, uploadBasePath);

            return await MapAsync(location);
        }

        public async Task<PagedResult<DeviceLocationResponseDto>> GetAllDeviceLocationAsync(PaginationParams p)
        {
            var query = _db.DeviceLocations.AsNoTracking().Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x =>
                    (x.LocationType != null && x.LocationType.ToLower().Contains(s)) ||
                    x.AddressLine1.ToLower().Contains(s) ||
                    (x.AddressLine2 != null && x.AddressLine2.ToLower().Contains(s)) ||
                    (x.Landmark != null && x.Landmark.ToLower().Contains(s)) ||
                    (x.LocationContactNumber != null && x.LocationContactNumber.Contains(s)));
            }

            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);

            // ── OPTIMIZATION (N+1 fix): entityTypeId hamesha same rehta hai
            //    (DeviceLocation ke liye), pehle har row pe alag DB call lagti
            //    thi (MapAsync ke andar) — ab ek hi baar fetch karke pass karte hain ──
            var entityTypeId = await GetDeviceLocationEntityTypeIdAsync();

            var data = new List<DeviceLocationResponseDto>();
            foreach (var loc in paged.Data)
                data.Add(MapWithEntityType(loc, entityTypeId));

            return PagedResult<DeviceLocationResponseDto>.Create(data, paged.TotalRecords, p);
        }

        public async Task<PagedResult<DeviceLocationResponseDto>> GetDeviceLocationByDeviceAsync(Guid deviceDetailId, PaginationParams p)
        {
            var query = _db.DeviceLocations.AsNoTracking()
                .Where(x => x.DeviceDetailId == deviceDetailId && x.IsActive)
                .OrderBy(x => x.CreatedAt);

            var paged = await query.ToPagedAsync(p);

            // ── OPTIMIZATION (N+1 fix): same wajah — ek hi baar fetch karo ──
            var entityTypeId = await GetDeviceLocationEntityTypeIdAsync();

            var data = new List<DeviceLocationResponseDto>();
            foreach (var loc in paged.Data)
                data.Add(MapWithEntityType(loc, entityTypeId));

            return PagedResult<DeviceLocationResponseDto>.Create(data, paged.TotalRecords, p);
        }

        public async Task<DeviceLocationResponseDto?> GetDeviceLocationByIdAsync(Guid id)
        {
            var location = await _db.DeviceLocations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (location is null) return null;
            return await MapAsync(location);
        }

        public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string? updatedBy)
        {
            var location = await _db.DeviceLocations.FirstOrDefaultAsync(x => x.Id == id);
            if (location is null) return false;

            location.IsActive = isActive;
            location.UpdatedAt = DateTime.UtcNow;
            location.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return true;
        }

        // ── File Upload helper — same IFileStoreService jo Device/Company files ke liye use hota hai ──
        private async Task UploadFilesAsync(Guid deviceLocationId, List<ImageUploadDto>? images, List<DocumentUploadDto>? documents, string uploadBasePath)
        {
            var entityTypeId = await GetDeviceLocationEntityTypeIdAsync();

            if (images != null)
            {
                foreach (var img in images)
                {
                    if (img.File == null) continue;
                    _fileStoreService.UploadFile(new FileStoreUploadDto
                    {
                        File = img.File,
                        EntityTypeId = entityTypeId,
                        EntityId = deviceLocationId,
                        FileCategory = FileCategoryConstants.Image,
                        FileUse = img.FileUse,
                        FileExpiryDate = img.FileExpiryDate
                    }, uploadBasePath);
                }
            }

            if (documents != null)
            {
                foreach (var doc in documents)
                {
                    if (doc.File == null) continue;
                    _fileStoreService.UploadFile(new FileStoreUploadDto
                    {
                        File = doc.File,
                        EntityTypeId = entityTypeId,
                        EntityId = deviceLocationId,
                        FileCategory = FileCategoryConstants.Document,
                        DocumentName = doc.DocumentName,
                        DocumentNumber = doc.DocumentNumber,
                        DocumentTypeId = doc.DocumentTypeId,
                        IssuedById = doc.IssuedById,
                        IssuedByDate = doc.IssuedByDate,
                        DocumentValidTill = doc.DocumentValidTill
                    }, uploadBasePath);
                }
            }
        }

        private async Task<DeviceLocationResponseDto> MapAsync(DeviceLocation e)
        {
            var entityTypeId = await GetDeviceLocationEntityTypeIdAsync();
            return MapWithEntityType(e, entityTypeId);
        }

        // ── entityTypeId caller se milta hai (ek hi baar fetch hota hai loop se pehle),
        //    isliye ye method koi DB call nahi karta — sirf mapping ──
        private DeviceLocationResponseDto MapWithEntityType(DeviceLocation e, Guid entityTypeId)
        {
            var files = _fileStoreService.GetFilesByEntity(entityTypeId, e.Id);

            return new DeviceLocationResponseDto
            {
                Id = e.Id,
                DeviceDetailId = e.DeviceDetailId,
                LocationType = e.LocationType,
                AddressLine1 = e.AddressLine1,
                AddressLine2 = e.AddressLine2,
                CountryId = e.CountryId,
                StateId = e.StateId,
                CityId = e.CityId,
                PinCode = e.PinCode,
                Landmark = e.Landmark,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                TimeZoneId = e.TimeZoneId,
                LocationContactNumber = e.LocationContactNumber,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                Images = files.Where(f => f.FileCategory == FileCategoryConstants.Image).ToList(),
                Documents = files.Where(f => f.FileCategory == FileCategoryConstants.Document).ToList()
            };
        }
    }
}