using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.Device;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    // Extra DTOs needed by DeviceModelService (Company-free versions)
    public class CompanyDevicesDto
    {
        public Guid DeviceId { get; set; }
        public string? DeviceShortName { get; set; }
        public string? DeviceLongName { get; set; }
        public Guid? DeviceTypeId { get; set; }
        public string? DeviceTypeName { get; set; }
        /// <summary>External Guid reference to Company microservice.</summary>
        public Guid? CompanyId { get; set; }
        public string? Remarks { get; set; }
    }

    public class MyCompanyDeviceDto
    {
        public Guid DeviceId { get; set; }
        public string? DeviceShortName { get; set; }
        public string? DeviceLongName { get; set; }

        public Guid? DeviceTypeId { get; set; }
        public string? DeviceTypeName { get; set; }

        public Guid? DeviceCategoryId { get; set; }
        public string? CategoryName { get; set; }

        public Guid? ModelSpecificationId { get; set; }
        public string? ModelName { get; set; }
        public string? ModelNumber { get; set; }

        /// <summary>External Guid reference to Company microservice.</summary>
        public Guid? CompanyId { get; set; }
        public string? Remarks { get; set; }
    }
    public class CategoryDevicesDto
    {
        public Guid DeviceId { get; set; }
        public string? DeviceShortName { get; set; }
        public string? DeviceLongName { get; set; }
        public Guid? DeviceCategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? DeviceTypeName { get; set; }
        /// <summary>External Guid reference to Company microservice.</summary>
        public Guid? CompanyId { get; set; }
    }

    public interface IDeviceModelService
    {
        Task<BulkAssignmentResponseDto> AssignModelsToDeviceAsync(Guid deviceTypeId, List<Guid> modelSpecificationIds);
        Task<List<DeviceModelDetailsDto>> GetDeviceModelMappingsAsync();
        Task<List<ModelDetailsDto>> GetModelsByDeviceTypeIdAsync(Guid deviceTypeId);
        Task<List<CompanyDevicesDto>> GetDevicesByCompanyIdAsync(Guid companyId);
        Task<List<CategoryDevicesDto>> GetDevicesByCategoryIdAsync(Guid categoryId);
        Task<List<MyCompanyDeviceDto>> GetMyCompanyDevicesWithModelAsync(Guid companyId);
    }

    public class DeviceModelService : IDeviceModelService
    {
        private readonly DBContext _db;
        public DeviceModelService(DBContext db) => _db = db;

        public async Task<BulkAssignmentResponseDto> AssignModelsToDeviceAsync(Guid deviceTypeId, List<Guid> modelSpecificationIds)
        {
            var response = new BulkAssignmentResponseDto { DeviceTypeId = deviceTypeId };

            var deviceExists = await _db.DeviceTypes.AnyAsync(d => d.Id == deviceTypeId && !d.IsDeleted);
            if (!deviceExists)
            {
                response.Message = $"DeviceType {deviceTypeId} does not exist or is deleted.";
                return response;
            }

            var validModelIds = await _db.ModelSpecifications
                .Where(s => modelSpecificationIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => s.Id).ToListAsync();

            response.SkippedOrInvalidIds = modelSpecificationIds.Except(validModelIds).ToList();

            var existingMappings = await _db.DeviceModelMappings
                .Where(m => m.DeviceTypeId == deviceTypeId)
                .Select(m => m.ModelSpecificationId).ToListAsync();

            var toInsert = new List<DeviceModelMapping>();
            foreach (var modelId in validModelIds)
            {
                if (!existingMappings.Contains(modelId))
                {
                    toInsert.Add(new DeviceModelMapping { DeviceTypeId = deviceTypeId, ModelSpecificationId = modelId });
                    response.SuccessfullyAssignedIds.Add(modelId);
                }
                else
                {
                    if (!response.SkippedOrInvalidIds.Contains(modelId))
                        response.SkippedOrInvalidIds.Add(modelId);
                }
            }

            if (toInsert.Any()) { _db.DeviceModelMappings.AddRange(toInsert); await _db.SaveChangesAsync(); }

            response.Message = "Bulk assignment completed.";
            return response;
        }

        public async Task<List<ModelDetailsDto>> GetModelsByDeviceTypeIdAsync(Guid deviceTypeId)
        {
            return await (from mapping in _db.DeviceModelMappings
                          join model in _db.ModelSpecifications on mapping.ModelSpecificationId equals model.Id
                          where mapping.DeviceTypeId == deviceTypeId && !model.IsDeleted
                          select new ModelDetailsDto
                          {
                              ModelId = model.Id, ModelName = model.Name,
                              ModelNumber = model.ModelNumber, DeviceTypeId = mapping.DeviceTypeId
                          }).ToListAsync();
        }

        public async Task<List<DeviceModelDetailsDto>> GetDeviceModelMappingsAsync()
        {
            return await (from mapping in _db.DeviceModelMappings
                          join devType in _db.DeviceTypes on mapping.DeviceTypeId equals devType.Id
                          join device in _db.DeviceMasters on mapping.DeviceTypeId equals device.DeviceTypeId into devJoin
                          from device in devJoin.DefaultIfEmpty()
                          join model in _db.ModelSpecifications on mapping.ModelSpecificationId equals model.Id
                          where !model.IsDeleted
                          select new DeviceModelDetailsDto
                          {
                              MappingId = mapping.Id, DeviceTypeId = mapping.DeviceTypeId,
                              DeviceTypeName = devType.Name,
                              DeviceShortName = device != null ? device.DeviceShortName : null,
                              DeviceLongName = device != null ? device.DeviceLongName : null,
                              ModelSpecificationId = mapping.ModelSpecificationId, ModelName = model.Name
                          }).ToListAsync();
        }

        public async Task<List<CompanyDevicesDto>> GetDevicesByCompanyIdAsync(Guid companyId)
        {
            return await (from device in _db.DeviceMasters
                          join devType in _db.DeviceTypes on device.DeviceTypeId equals devType.Id into typeJoin
                          from subType in typeJoin.DefaultIfEmpty()
                          where device.CompanyId == companyId && !device.IsDeleted
                          select new CompanyDevicesDto
                          {
                              DeviceId = device.Id, DeviceShortName = device.DeviceShortName,
                              DeviceLongName = device.DeviceLongName,
                              DeviceTypeId = device.DeviceTypeId,
                              DeviceTypeName = subType != null ? subType.Name : null,
                              CompanyId = device.CompanyId, Remarks = device.Remarks
                          }).ToListAsync();
        }

        public async Task<List<CategoryDevicesDto>> GetDevicesByCategoryIdAsync(Guid categoryId)
        {
            return await (from device in _db.DeviceMasters
                          join category in _db.DeviceCategories on device.DeviceCategoryId equals category.Id
                          join devType in _db.DeviceTypes on device.DeviceTypeId equals devType.Id into typeJoin
                          from subType in typeJoin.DefaultIfEmpty()
                          where device.DeviceCategoryId == categoryId && !device.IsDeleted && !category.IsDeleted
                          select new CategoryDevicesDto
                          {
                              DeviceId = device.Id, DeviceShortName = device.DeviceShortName,
                              DeviceLongName = device.DeviceLongName,
                              DeviceCategoryId = category.Id, CategoryName = category.Name,
                              DeviceTypeName = subType != null ? subType.Name : null,
                              CompanyId = device.CompanyId
                          }).ToListAsync();
        }
        public async Task<List<MyCompanyDeviceDto>> GetMyCompanyDevicesWithModelAsync(Guid companyId)
        {
            return await (from device in _db.DeviceMasters
                          join devType in _db.DeviceTypes on device.DeviceTypeId equals devType.Id into typeJoin
                          from subType in typeJoin.DefaultIfEmpty()
                          join devCat in _db.DeviceCategories on device.DeviceCategoryId equals devCat.Id into catJoin
                          from subCat in catJoin.DefaultIfEmpty()
                          join model in _db.ModelSpecifications on device.ModelSpecificationId equals model.Id into modelJoin
                          from subModel in modelJoin.DefaultIfEmpty()
                          where device.CompanyId == companyId && !device.IsDeleted
                          select new MyCompanyDeviceDto
                          {
                              DeviceId = device.Id,
                              DeviceShortName = device.DeviceShortName,
                              DeviceLongName = device.DeviceLongName,
                              DeviceTypeId = device.DeviceTypeId,
                              DeviceTypeName = subType != null ? subType.Name : null,
                              DeviceCategoryId = device.DeviceCategoryId,
                              CategoryName = subCat != null ? subCat.Name : null,
                              ModelSpecificationId = device.ModelSpecificationId,
                              ModelName = subModel != null ? subModel.Name : null,
                              ModelNumber = subModel != null ? subModel.ModelNumber : null,
                              CompanyId = device.CompanyId,
                              Remarks = device.Remarks
                          }).ToListAsync();
        }
    }
}
