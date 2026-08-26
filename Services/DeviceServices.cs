using DeviceManagementOnly.Common;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.Device;
using DeviceManagementOnly.Models.other;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  DEVICE CATEGORY
    // ══════════════════════════════════════════════════════════════

    public interface IDeviceCategoryService
    {
        Task<DeviceCategoryResponseDto> CreateDeviceCategoryAsync(DeviceCategoryCreateDto dto);
        Task<PagedResult<DeviceCategoryResponseDto>> GetAllDeviceCategoryAsync(PaginationParams p);
        Task<DeviceCategoryResponseDto?> GetDeviceCategoryByIdAsync(Guid id);
        Task<DeviceCategoryResponseDto?> UpdateDeviceCategoryAsync(Guid id, DeviceCategoryUpdateDto dto);
        //Task<bool> DeleteAsync(Guid id);
        Task<DeviceCategoryImportResponse> ImportDeviceCategoryExcelAsync(IFormFile file);
    }

    public class DeviceCategoryService : IDeviceCategoryService
    {
        private readonly DBContext _db;
        public DeviceCategoryService(DBContext db) => _db = db;

        public async Task<DeviceCategoryResponseDto> CreateDeviceCategoryAsync(DeviceCategoryCreateDto dto)
        {
            var e = new DeviceCategory
            {
                Name = dto.Name,
                IsActive = dto.IsActive,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow
            };
            _db.DeviceCategories.Add(e);
            await _db.SaveChangesAsync();
            return Map(e);
        }

        public async Task<PagedResult<DeviceCategoryResponseDto>> GetAllDeviceCategoryAsync(PaginationParams p)
        {
            var query = _db.DeviceCategories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var search = p.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(search) ||
                    (x.Remarks != null && x.Remarks.ToLower().Contains(search)) ||
                    (search == "active" && x.IsActive) ||
                    (search == "inactive" && !x.IsActive));
            }

            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<DeviceCategoryResponseDto>.Create(paged.Data.Select(Map), paged.TotalRecords, p);
        }

        public async Task<DeviceCategoryResponseDto?> GetDeviceCategoryByIdAsync(Guid id)
        {
            var e = await _db.DeviceCategories.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return e is null ? null : Map(e);
        }

        public async Task<DeviceCategoryResponseDto?> UpdateDeviceCategoryAsync(Guid id, DeviceCategoryUpdateDto dto)
        {
            var e = await _db.DeviceCategories.FindAsync(id);
            if (e is null) return null;
            e.Name = dto.Name; e.IsActive = dto.IsActive;
            e.Remarks = dto.Remarks; e.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Map(e);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var e = await _db.DeviceCategories.FindAsync(id);
            if (e is null) return false;
            e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<DeviceCategoryImportResponse> ImportDeviceCategoryExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("Please upload a valid Excel file.");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet == null || worksheet.Dimension == null)
                throw new Exception("Excel file is empty.");

            var imported = new List<string>();
            var skipped = new List<string>();
            var toAdd = new List<DeviceCategory>();
            int rowCount = worksheet.Dimension.Rows;

            // ── OPTIMIZATION (N+1 fix): pehle saare active names ek hi query se
            //    HashSet me le lo, loop ke andar DB call zero ho jaayegi ──
            var existingNames = (await _db.DeviceCategories
     .Where(x => !x.IsDeleted)
     .Select(x => x.Name.ToLower())
     .ToListAsync())      // ✅ pehle DB se List<string> laao
     .ToHashSet();

            for (int row = 2; row <= rowCount; row++)
            {
                var name = worksheet.Cells[row, 1].Text?.Trim();
                var remarks = worksheet.Cells[row, 2].Text?.Trim();

                if (string.IsNullOrWhiteSpace(name)) continue;

                var nameLower = name.ToLower();

                if (existingNames.Contains(nameLower)) { skipped.Add(name); continue; }

                toAdd.Add(new DeviceCategory
                {
                    Name = name,
                    Remarks = remarks,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                imported.Add(name);
                existingNames.Add(nameLower); // same excel ke andar duplicate rows bhi skip honge
            }

            if (toAdd.Any())
            {
                await _db.DeviceCategories.AddRangeAsync(toAdd);
                await _db.SaveChangesAsync();
            }

            return new DeviceCategoryImportResponse
            {
                TotalImported = imported.Count,
                TotalSkipped = skipped.Count,
                ImportedCategories = imported,
                SkippedCategories = skipped
            };
        }

        private static DeviceCategoryResponseDto Map(DeviceCategory e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            IsActive = e.IsActive,
            Remarks = e.Remarks,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }

    // ══════════════════════════════════════════════════════════════
    //  DEVICE TYPE
    // ══════════════════════════════════════════════════════════════

    public interface IDeviceTypeService
    {
        Task<DeviceTypeResponseDto> CreateDeviceTypeAsync(DeviceTypeCreateDto dto);
        Task<PagedResult<DeviceTypeResponseDto>> GetAllDeviceTypeAsync(PaginationParams p);
        Task<PagedResult<DeviceTypeResponseDto>> GetDeviceTypeByCategoryAsync(Guid categoryId, PaginationParams p);
        Task<DeviceTypeResponseDto?> GetDeviceTypeByIdAsync(Guid id);
        Task<DeviceTypeResponseDto?> UpdateDeviceTypeAsync(Guid id, DeviceTypeUpdateDto dto);
        Task<DeviceTypeImportResponse> ImportDeviceTypeExcelAsync(IFormFile file);
        //Task<bool> DeleteAsync(Guid id);
    }

    public class DeviceTypeService : IDeviceTypeService
    {
        private readonly DBContext _db;
        public DeviceTypeService(DBContext db) => _db = db;

        public async Task<DeviceTypeResponseDto> CreateDeviceTypeAsync(DeviceTypeCreateDto dto)
        {
            var e = new DeviceType
            {
                Name = dto.Name,
                DeviceCategoryId = dto.DeviceCategoryId,
                Protocol = dto.Protocol,
                SupportedFeature = dto.SupportedFeature,
                Icon = dto.Icon,
                IconColor = dto.IconColor,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow
            };
            _db.DeviceTypes.Add(e);
            await _db.SaveChangesAsync();
            return Map(e);
        }

        public async Task<PagedResult<DeviceTypeResponseDto>> GetAllDeviceTypeAsync(PaginationParams p)
        {
            var query = _db.DeviceTypes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var search = p.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(search) ||
                    (x.Protocol != null && x.Protocol.ToLower().Contains(search)) ||
                    (x.SupportedFeature != null && x.SupportedFeature.ToLower().Contains(search)) ||
                    (x.Remarks != null && x.Remarks.ToLower().Contains(search)) ||
                    _db.DeviceCategories.Any(dc => dc.Id == x.DeviceCategoryId &&
                        dc.Name.ToLower().Contains(search)));
            }

            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<DeviceTypeResponseDto>.Create(paged.Data.Select(Map), paged.TotalRecords, p);
        }

        public async Task<PagedResult<DeviceTypeResponseDto>> GetDeviceTypeByCategoryAsync(Guid categoryId, PaginationParams p)
        {
            var query = _db.DeviceTypes.AsNoTracking()
                .Where(x => x.DeviceCategoryId == categoryId && !x.IsDeleted)
                .OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<DeviceTypeResponseDto>.Create(paged.Data.Select(Map), paged.TotalRecords, p);
        }

        public async Task<DeviceTypeResponseDto?> GetDeviceTypeByIdAsync(Guid id)
        {
            var e = await _db.DeviceTypes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return e is null ? null : Map(e);
        }

        public async Task<DeviceTypeResponseDto?> UpdateDeviceTypeAsync(Guid id, DeviceTypeUpdateDto dto)
        {
            var e = await _db.DeviceTypes.FindAsync(id);
            if (e is null) return null;
            e.Name = dto.Name; e.DeviceCategoryId = dto.DeviceCategoryId;
            e.Protocol = dto.Protocol; e.SupportedFeature = dto.SupportedFeature;
            e.Icon = dto.Icon; e.IconColor = dto.IconColor;
            e.Remarks = dto.Remarks; e.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Map(e);
        }

        //public async Task<bool> DeleteAsync(Guid id)
        //{
        //    var e = await _db.DeviceTypes.FindAsync(id);
        //    if (e is null) return false;
        //    e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow;
        //    await _db.SaveChangesAsync();
        //    return true;
        //}

        public async Task<DeviceTypeImportResponse> ImportDeviceTypeExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("Please upload a valid Excel file.");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet?.Dimension == null) throw new Exception("Excel file is empty.");

            int rowCount = worksheet.Dimension.Rows;
            var imported = new List<string>();
            var skipped = new List<string>();
            var createdCategories = new List<string>();
            var toAdd = new List<DeviceType>();
            var categoriesToAdd = new List<DeviceCategory>();

            // ── OPTIMIZATION (N+1 fix): 2 baar hi DB hit karo, poori loop
            //    ke liye — pehle saari active categories aur saare active
            //    device-types ek-ek query se memory me le lo ──
            var categoryByName = await _db.DeviceCategories
                .Where(x => !x.IsDeleted)
                .ToDictionaryAsync(x => x.Name.ToLower(), x => x);

            var existingTypeKeys = (await _db.DeviceTypes
                .Where(x => !x.IsDeleted)
                .Select(x => new { Name = x.Name.ToLower(), x.DeviceCategoryId })
                .ToListAsync())
                .Select(x => (x.Name, x.DeviceCategoryId))
                .ToHashSet();

            for (int row = 2; row <= rowCount; row++)
            {
                var typeName = worksheet.Cells[row, 1].Text?.Trim();
                var categoryName = worksheet.Cells[row, 2].Text?.Trim();
                var protocol = worksheet.Cells[row, 3].Text?.Trim();
                var supportedFeature = worksheet.Cells[row, 4].Text?.Trim();
                var remarks = worksheet.Cells[row, 5].Text?.Trim();

                if (string.IsNullOrWhiteSpace(typeName)) continue;
                if (string.IsNullOrWhiteSpace(categoryName)) { skipped.Add($"{typeName} (Category Missing)"); continue; }

                var categoryNameLower = categoryName.ToLower();

                // ── Instant in-memory lookup, koi DB call nahi ──
                if (!categoryByName.TryGetValue(categoryNameLower, out var category))
                {
                    // Id client-side hi generate ho jaati hai (Guid.NewGuid() model me default hai),
                    // isliye FK reference abhi assign kar sakte hain, DB insert baad me batch me hoga
                    category = new DeviceCategory { Name = categoryName, IsActive = true, CreatedAt = DateTime.UtcNow };
                    categoryByName[categoryNameLower] = category;
                    categoriesToAdd.Add(category);
                    createdCategories.Add(categoryName);
                }

                var typeKey = (typeName.ToLower(), category.Id);

                if (existingTypeKeys.Contains(typeKey)) { skipped.Add(typeName); continue; }

                toAdd.Add(new DeviceType
                {
                    Name = typeName,
                    DeviceCategoryId = category.Id,
                    Protocol = protocol,
                    SupportedFeature = supportedFeature,
                    Remarks = remarks,
                    CreatedAt = DateTime.UtcNow
                });
                imported.Add(typeName);
                existingTypeKeys.Add(typeKey); // same excel ke andar duplicate rows bhi skip honge
            }

            // ── Saari nayi categories + device types EK hi SaveChanges me insert ──
            if (categoriesToAdd.Any()) await _db.DeviceCategories.AddRangeAsync(categoriesToAdd);
            if (toAdd.Any()) await _db.DeviceTypes.AddRangeAsync(toAdd);
            if (categoriesToAdd.Any() || toAdd.Any()) await _db.SaveChangesAsync();

            return new DeviceTypeImportResponse
            {
                TotalImported = imported.Count,
                TotalSkipped = skipped.Count,
                ImportedDeviceTypes = imported,
                SkippedDeviceTypes = skipped,
                CreatedCategories = createdCategories
            };
        }

        private static DeviceTypeResponseDto Map(DeviceType e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            DeviceCategoryId = e.DeviceCategoryId,
            Protocol = e.Protocol,
            SupportedFeature = e.SupportedFeature,
            Remarks = e.Remarks,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }

    // ══════════════════════════════════════════════════════════════
    //  MODEL CATEGORY
    // ══════════════════════════════════════════════════════════════

    public interface IModelCategoryService
    {
        Task<ModelCategoryResponseDto> CreateModelCategoryAsync(ModelCategoryCreateDto dto);
        Task<PagedResult<ModelCategoryResponseDto>> GetAllModelCategoryAsync(PaginationParams p);
        Task<ModelCategoryResponseDto?> GetModelCategoryByIdAsync(Guid id);
        Task<ModelCategoryResponseDto?> UpdateModelCategoryAsync(Guid id, ModelCategoryUpdateDto dto);
        //  Task<bool> DeleteAsync(Guid id);
    }

    public class ModelCategoryService : IModelCategoryService
    {
        private readonly DBContext _db;
        public ModelCategoryService(DBContext db) => _db = db;

        public async Task<ModelCategoryResponseDto> CreateModelCategoryAsync(ModelCategoryCreateDto dto)
        {
            var e = new ModelCategory { Name = dto.Name, Remarks = dto.Remarks, IsActive = dto.IsActive, CreatedAt = DateTime.UtcNow };
            _db.ModelCategories.Add(e);
            await _db.SaveChangesAsync();
            return Map(e);
        }

        public async Task<PagedResult<ModelCategoryResponseDto>> GetAllModelCategoryAsync(PaginationParams p)
        {
            var query = _db.ModelCategories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(s) ||
                    (x.Remarks != null && x.Remarks.ToLower().Contains(s)) ||
                    (s == "active" && x.IsActive) || (s == "inactive" && !x.IsActive));
            }

            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<ModelCategoryResponseDto>.Create(paged.Data.Select(Map), paged.TotalRecords, p);
        }

        public async Task<ModelCategoryResponseDto?> GetModelCategoryByIdAsync(Guid id)
        {
            var e = await _db.ModelCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return e is null ? null : Map(e);
        }

        public async Task<ModelCategoryResponseDto?> UpdateModelCategoryAsync(Guid id, ModelCategoryUpdateDto dto)
        {
            var e = await _db.ModelCategories.FindAsync(id);
            if (e is null) return null;
            e.Name = dto.Name; e.Remarks = dto.Remarks; e.IsActive = dto.IsActive; e.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Map(e);
        }

        //public async Task<bool> DeleteAsync(Guid id)
        //{
        //    var e = await _db.ModelCategories.FindAsync(id);
        //    if (e is null) return false;
        //    e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow;
        //    await _db.SaveChangesAsync();
        //    return true;
        //}

        private static ModelCategoryResponseDto Map(ModelCategory e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            Remarks = e.Remarks,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }

    // ══════════════════════════════════════════════════════════════
    //  MODEL SPECIFICATION
    // ══════════════════════════════════════════════════════════════

    public interface IModelSpecificationService
    {
        Task<ModelSpecificationResponseDto> CreateModelSpecificationAsync(ModelSpecificationCreateDto dto);
        Task<PagedResult<ModelSpecificationResponseDto>> GetAllModelSpecificationAsync(ModelSpecificationFilterParams p);
        Task<PagedResult<ModelSpecificationResponseDto>> GetModelSpecificationByDeviceTypeAsync(Guid deviceTypeId, PaginationParams p);
        Task<PagedResult<ModelSpecificationResponseDto>> GetModelSpecificationByCompanyAsync(Guid companyId, PaginationParams p);
        Task<ModelSpecificationResponseDto?> GetModelSpecificationByIdAsync(Guid id);
        Task<ModelSpecificationResponseDto?> UpdateModelSpecificationAsync(Guid id, ModelSpecificationUpdateDto dto);
        // Task<bool> SoftDeleteAsync(Guid id, string? deletedBy);
    }

    public class ModelSpecificationService : IModelSpecificationService
    {
        private readonly DBContext _db;
        public ModelSpecificationService(DBContext db) => _db = db;

        public async Task<ModelSpecificationResponseDto> CreateModelSpecificationAsync(ModelSpecificationCreateDto dto)
        {
            var spec = new ModelSpecification
            {
                Name = dto.Name,
                DeviceTypeId = dto.DeviceTypeId,
                ModelNumber = dto.ModelNumber,
                Remarks = dto.Remarks,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            _db.ModelSpecifications.Add(spec);
            await _db.SaveChangesAsync();

            foreach (var p in dto.Parameters)
            {
                _db.ModelParameters.Add(new ModelParameter
                {
                    DeviceModelId = spec.Id,
                    ModelCategoryId = p.ModelCategoryId,
                    ParameterName = p.ParameterName,
                    ParameterValue = p.ParameterValue,
                    Unit = p.Unit,
                    Capacity = p.Capacity,
                    Processor = p.Processor,
                    RAM = p.RAM,
                    Storage = p.Storage,
                    FirmwareVersion = p.FirmwareVersion,
                    Feature = p.Feature,
                    Intro = p.Intro,
                    Uses = p.Uses,
                    EndOfLife = p.EndOfLife,
                    WarrantyDuration = p.WarrantyDuration,
                    Protocol = p.Protocol,
                    Remarks = p.Remarks,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Map(spec, new List<Guid>());
        }

        public async Task<PagedResult<ModelSpecificationResponseDto>> GetAllModelSpecificationAsync(ModelSpecificationFilterParams p)
        {
            var query = _db.ModelSpecifications.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Name.ToLower().Contains(s) ||
                    (x.ModelNumber != null && x.ModelNumber.ToLower().Contains(s)) ||
                    _db.DeviceTypes.Any(dt => dt.Id == x.DeviceTypeId && dt.Name.ToLower().Contains(s)));
            }

            if (p.DeviceTypeId.HasValue)
                query = query.Where(x => x.DeviceTypeId == p.DeviceTypeId.Value);

            // ── FIX: ModelSpecification pe CompanyId field nahi hai, isliye
            //    yeh filter commented pada tha. Ab DeviceMaster ke through
            //    sahi se filter karta hai: sirf wahi specs jinka kam se kam
            //    ek DeviceMaster is company ke liye bana hai ──
            if (p.CompanyId.HasValue)
            {
                var specIdsForCompany = _db.DeviceMasters.AsNoTracking()
                    .Where(dm => dm.CompanyId == p.CompanyId.Value && !dm.IsDeleted && dm.ModelSpecificationId != null)
                    .Select(dm => dm.ModelSpecificationId!.Value);
                query = query.Where(x => specIdsForCompany.Contains(x.Id));
            }

            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);

            // ── OPTIMIZATION (N+1 fix): har spec ke liye alag DeviceMasters
            //    query lagane ke bajaye, ek hi query se saare distinct
            //    spec→company mappings ek dictionary me le lete hain ──
            var companyMap = await GetCompanyIdsBySpecAsync(paged.Data.Select(x => x.Id).ToList());

            return PagedResult<ModelSpecificationResponseDto>.Create(
                paged.Data.Select(x => Map(x, companyMap.GetValueOrDefault(x.Id, new List<Guid>()))),
                paged.TotalRecords, p);
        }

        public async Task<PagedResult<ModelSpecificationResponseDto>> GetModelSpecificationByDeviceTypeAsync(Guid deviceTypeId, PaginationParams p)
        {
            var query = _db.ModelSpecifications.AsNoTracking()
                .Where(x => x.DeviceTypeId == deviceTypeId && !x.IsDeleted)
                .OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            var companyMap = await GetCompanyIdsBySpecAsync(paged.Data.Select(x => x.Id).ToList());
            return PagedResult<ModelSpecificationResponseDto>.Create(
                paged.Data.Select(x => Map(x, companyMap.GetValueOrDefault(x.Id, new List<Guid>()))),
                paged.TotalRecords, p);
        }

        // ── FIX: pehle yeh method companyId param ko istemal hi nahi karta
        //    tha (kyunki ModelSpecification pe CompanyId field hai hi nahi) —
        //    ab DeviceMaster ke through sahi se filter karta hai: sirf
        //    wahi specs jo is company ke kisi DeviceMaster me use hui hain ──
        public async Task<PagedResult<ModelSpecificationResponseDto>> GetModelSpecificationByCompanyAsync(Guid companyId, PaginationParams p)
        {
            var specIdsForCompany = _db.DeviceMasters.AsNoTracking()
                .Where(dm => dm.CompanyId == companyId && !dm.IsDeleted && dm.ModelSpecificationId != null)
                .Select(dm => dm.ModelSpecificationId!.Value)
                .Distinct();

            var query = _db.ModelSpecifications.AsNoTracking()
                .Where(x => !x.IsDeleted && specIdsForCompany.Contains(x.Id))
                .OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            var companyMap = await GetCompanyIdsBySpecAsync(paged.Data.Select(x => x.Id).ToList());
            return PagedResult<ModelSpecificationResponseDto>.Create(
                paged.Data.Select(x => Map(x, companyMap.GetValueOrDefault(x.Id, new List<Guid>()))),
                paged.TotalRecords, p);
        }

        public async Task<ModelSpecificationResponseDto?> GetModelSpecificationByIdAsync(Guid id)
        {
            var e = await _db.ModelSpecifications.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (e is null) return null;
            var companyMap = await GetCompanyIdsBySpecAsync(new List<Guid> { id });
            return Map(e, companyMap.GetValueOrDefault(id, new List<Guid>()));
        }

        public async Task<ModelSpecificationResponseDto?> UpdateModelSpecificationAsync(Guid id, ModelSpecificationUpdateDto dto)
        {
            var e = await _db.ModelSpecifications.FindAsync(id);
            if (e is null || e.IsDeleted) return null;
            e.Name = dto.Name; e.DeviceTypeId = dto.DeviceTypeId;
            e.ModelNumber = dto.ModelNumber; e.Remarks = dto.Remarks;
            e.UpdatedBy = dto.UpdatedBy; e.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            var companyMap = await GetCompanyIdsBySpecAsync(new List<Guid> { id });
            return Map(e, companyMap.GetValueOrDefault(id, new List<Guid>()));
        }

        //public async Task<bool> SoftDeleteAsync(Guid id, string? deletedBy)
        //{
        //    var e = await _db.ModelSpecifications.FindAsync(id);
        //    if (e is null || e.IsDeleted) return false;
        //    e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedBy = deletedBy;
        //    await _db.SaveChangesAsync();
        //    return true;
        //}

        // ── Bulk lookup: given spec Ids, DeviceMasters se distinct
        //    CompanyIds nikaal ke ek dictionary bana deta hai (1 query,
        //    N+1 nahi) ──
        private async Task<Dictionary<Guid, List<Guid>>> GetCompanyIdsBySpecAsync(List<Guid> specIds)
        {
            if (specIds.Count == 0) return new Dictionary<Guid, List<Guid>>();

            var rows = await _db.DeviceMasters.AsNoTracking()
                .Where(dm => dm.ModelSpecificationId != null
                             && specIds.Contains(dm.ModelSpecificationId!.Value)
                             && !dm.IsDeleted
                             && dm.CompanyId != null)
                .Select(dm => new { SpecId = dm.ModelSpecificationId!.Value, dm.CompanyId })
                .Distinct()
                .ToListAsync();

            return rows
                .GroupBy(r => r.SpecId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.CompanyId!.Value).ToList());
        }

        private static ModelSpecificationResponseDto Map(ModelSpecification e, List<Guid> companyIds) => new()
        {
            Id = e.Id,
            Name = e.Name,
            DeviceTypeId = e.DeviceTypeId,
            ModelNumber = e.ModelNumber,
            Remarks = e.Remarks,
            CompanyIds = companyIds,
            IsDeleted = e.IsDeleted,
            CreatedBy = e.CreatedBy,
            UpdatedBy = e.UpdatedBy,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }

    // ══════════════════════════════════════════════════════════════
    //  MODEL PARAMETER
    // ══════════════════════════════════════════════════════════════

    public interface IModelParameterService
    {
        Task<ModelParameterResponseDto> CreateModelParameterAsync(ModelParameterCreateDto dto);
        Task<IEnumerable<ModelParameterResponseDto>> GetModelParameterByModelAsync(Guid modelSpecId);
        Task<ModelParameterResponseDto?> GetModelParameterByIdAsync(Guid id);
        Task<ModelParameterResponseDto?> UpdateModelParameterAsync(Guid id, ModelParameterUpdateDto dto);
        // Task<bool> DeleteAsync(Guid id);
    }

    public class ModelParameterService : IModelParameterService
    {
        private readonly DBContext _db;
        public ModelParameterService(DBContext db) => _db = db;

        public async Task<ModelParameterResponseDto> CreateModelParameterAsync(ModelParameterCreateDto dto)
        {
            var e = new ModelParameter
            {
                DeviceModelId = dto.DeviceModelId,
                ModelCategoryId = dto.ModelCategoryId,
                ParameterName = dto.ParameterName,
                ParameterValue = dto.ParameterValue,
                Unit = dto.Unit,
                Capacity = dto.Capacity,
                Processor = dto.Processor,
                RAM = dto.RAM,
                Storage = dto.Storage,
                Remarks = dto.Remarks,
                FirmwareVersion = dto.FirmwareVersion,
                Protocol = dto.Protocol,
                WarrantyDuration = dto.WarrantyDuration,
                EndOfLife = dto.EndOfLife,
                Intro = dto.Intro,
                Uses = dto.Uses,
                Feature = dto.Feature,
                CreatedAt = DateTime.UtcNow
            };
            _db.ModelParameters.Add(e);
            await _db.SaveChangesAsync();
            return await MapWithCategory(e);
        }

        public async Task<IEnumerable<ModelParameterResponseDto>> GetModelParameterByModelAsync(Guid modelSpecId)
        {
            var list = await _db.ModelParameters.AsNoTracking()
                .Where(x => x.DeviceModelId == modelSpecId && !x.IsDeleted).ToListAsync();
            var result = new List<ModelParameterResponseDto>();
            foreach (var p in list) result.Add(await MapWithCategory(p));
            return result;
        }

        public async Task<ModelParameterResponseDto?> GetModelParameterByIdAsync(Guid id)
        {
            var e = await _db.ModelParameters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return e is null ? null : await MapWithCategory(e);
        }

        public async Task<ModelParameterResponseDto?> UpdateModelParameterAsync(Guid id, ModelParameterUpdateDto dto)
        {
            var e = await _db.ModelParameters.FindAsync(id);
            if (e is null) return null;
            e.ModelCategoryId = dto.ModelCategoryId; e.ParameterName = dto.ParameterName;
            e.ParameterValue = dto.ParameterValue; e.Unit = dto.Unit; e.Capacity = dto.Capacity;
            e.RAM = dto.RAM; e.EndOfLife = dto.EndOfLife; e.Processor = dto.Processor;
            e.Storage = dto.Storage; e.Remarks = dto.Remarks; e.Feature = dto.Feature;
            e.FirmwareVersion = dto.FirmwareVersion; e.Protocol = dto.Protocol;
            e.WarrantyDuration = dto.WarrantyDuration; e.Intro = dto.Intro; e.Uses = dto.Uses;
            e.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await MapWithCategory(e);
        }

        //public async Task<bool> DeleteAsync(Guid id)
        //{
        //    var e = await _db.ModelParameters.FindAsync(id);
        //    if (e is null) return false;
        //    e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow;
        //    await _db.SaveChangesAsync();
        //    return true;
        //}

        private async Task<ModelParameterResponseDto> MapWithCategory(ModelParameter e)
        {
            var cat = await _db.ModelCategories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == e.ModelCategoryId);
            return new ModelParameterResponseDto
            {
                Id = e.Id,
                DeviceModelId = e.DeviceModelId,
                ModelCategoryId = e.ModelCategoryId,
                ModelCategoryName = cat?.Name ?? "",
                ParameterName = e.ParameterName,
                ParameterValue = e.ParameterValue,
                Unit = e.Unit,
                FirmwareVersion = e.FirmwareVersion,
                Protocol = e.Protocol,
                WarrantyDuration = e.WarrantyDuration,
                EndOfLife = e.EndOfLife,
                Intro = e.Intro,
                Uses = e.Uses,
                Feature = e.Feature,
                RAM = e.RAM,
                Storage = e.Storage,
                Processor = e.Processor,
                Capacity = e.Capacity,
                Remarks = e.Remarks
            };
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  DEVICE MASTER
    // ══════════════════════════════════════════════════════════════

    public interface IDeviceMasterService
    {
        Task<DeviceMasterResponseDto> CreateDeviceMasterAsync(DeviceMasterCreateDto dto);
        Task<PagedResult<DeviceMasterResponseDto>> GetAllDeviceMasterAsync(DeviceMasterFilterParams p);
        Task<PagedResult<DeviceMasterResponseDto>> GetDeviceMasterByCompanyAsync(Guid companyId, PaginationParams p);
        Task<PagedResult<DeviceMasterResponseDto>> GetDeviceMasterByCategoryAsync(Guid categoryId, PaginationParams p);
        Task<DeviceMasterResponseDto?> GetDeviceMasterByIdAsync(Guid id);
        Task<DeviceMasterResponseDto?> UpdateDeviceMasterAsync(Guid id, DeviceMasterUpdateDto dto);
        //  Task<bool> DeleteAsync(Guid id);
    }

    public class DeviceMasterService : IDeviceMasterService
    {
        private readonly DBContext _db;
        public DeviceMasterService(DBContext db) => _db = db;

        public async Task<DeviceMasterResponseDto> CreateDeviceMasterAsync(DeviceMasterCreateDto dto)
        {
            var e = new DeviceMaster
            {
                DeviceTypeId = dto.DeviceTypeId,
                DeviceCategoryId = dto.DeviceCategoryId,
                CompanyId = dto.CompanyId,
                ModelSpecificationId = dto.ModelSpecificationId,
                DeviceShortName = dto.DeviceShortName,
                DeviceLongName = dto.DeviceLongName,
                Remarks = dto.Remarks,
                DataFormat = dto.DataFormat,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };
            _db.DeviceMasters.Add(e);
            await _db.SaveChangesAsync();
            return Map(e);
        }

        public async Task<PagedResult<DeviceMasterResponseDto>> GetAllDeviceMasterAsync(DeviceMasterFilterParams p)
        {
            var query = _db.DeviceMasters.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x =>
                    (x.DeviceShortName != null && x.DeviceShortName.ToLower().Contains(s)) ||
                    (x.DeviceLongName != null && x.DeviceLongName.ToLower().Contains(s)) ||
                    (x.Remarks != null && x.Remarks.ToLower().Contains(s)) ||
                    _db.DeviceTypes.Any(dt => dt.Id == x.DeviceTypeId && dt.Name.ToLower().Contains(s)) ||
                    _db.DeviceCategories.Any(dc => dc.Id == x.DeviceCategoryId && dc.Name.ToLower().Contains(s)) ||
                    _db.ModelSpecifications.Any(ms => ms.Id == x.ModelSpecificationId && !ms.IsDeleted && ms.Name.ToLower().Contains(s)));
            }

            if (p.DeviceCategoryId.HasValue) query = query.Where(x => x.DeviceCategoryId == p.DeviceCategoryId);
            if (p.DeviceTypeId.HasValue) query = query.Where(x => x.DeviceTypeId == p.DeviceTypeId);
            if (p.CompanyId.HasValue) query = query.Where(x => x.CompanyId == p.CompanyId);

            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<DeviceMasterResponseDto>.Create(paged.Data.Select(Map), paged.TotalRecords, p);
        }

        public async Task<PagedResult<DeviceMasterResponseDto>> GetDeviceMasterByCompanyAsync(Guid companyId, PaginationParams p)
        {
            var query = _db.DeviceMasters.AsNoTracking()
                .Where(x => x.CompanyId == companyId && !x.IsDeleted).OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<DeviceMasterResponseDto>.Create(paged.Data.Select(Map), paged.TotalRecords, p);
        }

        public async Task<PagedResult<DeviceMasterResponseDto>> GetDeviceMasterByCategoryAsync(Guid categoryId, PaginationParams p)
        {
            var query = _db.DeviceMasters.AsNoTracking()
                .Where(x => x.DeviceCategoryId == categoryId && !x.IsDeleted).OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<DeviceMasterResponseDto>.Create(paged.Data.Select(Map), paged.TotalRecords, p);
        }

        public async Task<DeviceMasterResponseDto?> GetDeviceMasterByIdAsync(Guid id)
        {
            var e = await _db.DeviceMasters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return e is null ? null : Map(e);
        }

        public async Task<DeviceMasterResponseDto?> UpdateDeviceMasterAsync(Guid id, DeviceMasterUpdateDto dto)
        {
            var e = await _db.DeviceMasters.FindAsync(id);
            if (e is null) return null;
            e.DeviceTypeId = dto.DeviceTypeId; e.DeviceCategoryId = dto.DeviceCategoryId;
            e.ModelSpecificationId = dto.ModelSpecificationId; e.CompanyId = dto.CompanyId;
            e.DeviceShortName = dto.DeviceShortName; e.DeviceLongName = dto.DeviceLongName;
            e.Remarks = dto.Remarks; e.DataFormat = dto.DataFormat;
            e.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Map(e);
        }

        //public async Task<bool> DeleteAsync(Guid id)
        //{
        //    var e = await _db.DeviceMasters.FindAsync(id);
        //    if (e is null) return false;
        //    e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow;
        //    await _db.SaveChangesAsync();
        //    return true;
        //}

        private static DeviceMasterResponseDto Map(DeviceMaster e) => new()
        {
            Id = e.Id,
            DeviceTypeId = e.DeviceTypeId,
            DeviceCategoryId = e.DeviceCategoryId,
            ModelSpecificationId = e.ModelSpecificationId,
            CompanyId = e.CompanyId,
            DeviceShortName = e.DeviceShortName,
            DeviceLongName = e.DeviceLongName,
            Remarks = e.Remarks,
            DataFormat = e.DataFormat,
            CreatedBy = e.CreatedBy,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }

    // ══════════════════════════════════════════════════════════════
    //  UNIT MASTER
    // ══════════════════════════════════════════════════════════════

    public interface IUnitMasterService
    {
        Task<UnitMasterResponseDto> CreateUnitMasterAsync(UnitMasterCreateDto dto);
        Task<PagedResult<UnitMasterResponseDto>> GetAllUnitMasterAsync(PaginationParams p);
        Task<UnitMasterResponseDto?> GetUnitMasterByIdAsync(Guid id);
        Task<UnitMasterResponseDto?> UpdateUnitMasterAsync(Guid id, UnitMasterUpdateDto dto);
        // Task<bool> DeleteAsync(Guid id);
    }

    public class UnitMasterService : IUnitMasterService
    {
        private readonly DBContext _db;
        public UnitMasterService(DBContext db) => _db = db;

        public async Task<UnitMasterResponseDto> CreateUnitMasterAsync(UnitMasterCreateDto dto)
        {
            var e = new UnitMaster { UnitName = dto.UnitName, CreatedAt = DateTime.UtcNow };
            _db.UnitMasters.Add(e);
            await _db.SaveChangesAsync();
            return Map(e);
        }

        public async Task<PagedResult<UnitMasterResponseDto>> GetAllUnitMasterAsync(PaginationParams p)
        {
            var query = _db.UnitMasters.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var s = p.Search.Trim().ToLower();
                query = query.Where(x => x.UnitName.ToLower().Contains(s));
            }
            query = query.OrderBy(x => x.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<UnitMasterResponseDto>.Create(paged.Data.Select(Map), paged.TotalRecords, p);
        }

        public async Task<UnitMasterResponseDto?> GetUnitMasterByIdAsync(Guid id)
        {
            var e = await _db.UnitMasters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return e is null ? null : Map(e);
        }

        public async Task<UnitMasterResponseDto?> UpdateUnitMasterAsync(Guid id, UnitMasterUpdateDto dto)
        {
            var e = await _db.UnitMasters.FindAsync(id);
            if (e is null) return null;
            e.UnitName = dto.UnitName; e.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Map(e);
        }

        //public async Task<bool> DeleteAsync(Guid id)
        //{
        //    var e = await _db.UnitMasters.FindAsync(id);
        //    if (e is null) return false;
        //    _db.UnitMasters.Remove(e);
        //    await _db.SaveChangesAsync();
        //    return true;
        //}

        private static UnitMasterResponseDto Map(UnitMaster e) => new()
        {
            Id = e.Id,
            UnitName = e.UnitName,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}