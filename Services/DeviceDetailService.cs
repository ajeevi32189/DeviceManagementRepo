using DeviceManagementOnly.Common;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models;
using DeviceManagementOnly.Models.Device;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QRCoder;

namespace DeviceManagementOnly.Services
{
    // ══════════════════════════════════════════════════════════════
    //  DEVICE DETAIL
    // ══════════════════════════════════════════════════════════════

    public interface IDeviceDetailService
    {
        Task<DeviceDetailResponseDto> CreateDeviceDetailAsync(DeviceDetailCreateDto dto);
        Task<PagedResult<DeviceDetailResponseDto>> GetAllDeviceDetailAsync(DeviceDetailFilterParams p);
        Task<PagedResult<DeviceDetailResponseDto>> GetDeviceDetailByMasterAsync(Guid deviceMasterId, PaginationParams p);
        Task<PagedResult<DeviceDetailResponseDto>> GetDeviceDetailByCategoryAsync(Guid categoryId, PaginationParams p);
        Task<PagedResult<DeviceDetailResponseDto>> GetDeviceDetailByTypeAsync(Guid deviceTypeId, PaginationParams p);
        Task<PagedResult<DeviceDetailResponseDto>> GetDeviceDetailByCompanyAsync(Guid companyId, PaginationParams p);
        Task<string> GenerateQRCodeForDeviceAsync(Guid deviceDetailId);
        Task<DeviceTrackingResponseDto> SetupDeviceTrackingAsync(Guid deviceDetailId, DeviceTrackingInputDto dto);
        Task<DeviceDetailResponseDto?> GetDeviceDetailByIdAsync(Guid id);
        Task<DeviceDetailResponseDto?> UpdateDeviceDetailAsync(Guid id, DeviceDetailUpdateDto dto);
        Task<DeviceDetailImportResponse> ImportDeviceDetailExcelAsync(IFormFile file);
        // Task<bool> DeleteAsync(Guid id);
    }

    public class DeviceDetailService : IDeviceDetailService
    {
        private readonly DBContext _db;
        public DeviceDetailService(DBContext db) => _db = db;

        public async Task<DeviceDetailResponseDto> CreateDeviceDetailAsync(DeviceDetailCreateDto dto)
        {
            var master = await _db.DeviceMasters.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == dto.DeviceMasterId);

            if (master is null)
                throw new InvalidOperationException($"DeviceMaster Id={dto.DeviceMasterId} does not exist.");

            var entity = new DeviceDetail
            {
                DeviceMasterId = dto.DeviceMasterId,
                IMEI = dto.IMEI,
                MACAddress = dto.MACAddress,
                TagNumber = dto.TagNumber,
                ShortName = dto.ShortName,
                LongName = dto.LongName,
                SerialNumber = dto.SerialNumber,
                PurchaseDate = dto.PurchaseDate,
                WarrantyExpiry = dto.WarrantyExpiry,
                PurchaseCost = dto.PurchaseCost,
                Remarks = dto.Remarks,
                IPAddress = dto.IPAddress,
                SIMNumber = dto.SIMNumber,
                ParentDeviceDetailId = dto.ParentDeviceDetailId,
                CreatedAt = DateTime.UtcNow
            };

            _db.DeviceDetails.Add(entity);
            await _db.SaveChangesAsync();
            return MapWithMaster(entity, master);
        }

        public async Task<PagedResult<DeviceDetailResponseDto>> GetAllDeviceDetailAsync(DeviceDetailFilterParams p)
        {
            var query =
                from dd in _db.DeviceDetails.AsNoTracking()
                join dm in _db.DeviceMasters.AsNoTracking()
                    on dd.DeviceMasterId equals dm.Id
                join dt in _db.DeviceTypes.AsNoTracking()
                    on dm.DeviceTypeId equals dt.Id into dtJoin
                from dt in dtJoin.DefaultIfEmpty()
                join dc in _db.DeviceCategories.AsNoTracking()
                    on dm.DeviceCategoryId equals dc.Id into dcJoin
                from dc in dcJoin.DefaultIfEmpty()
                select new
                {
                    Detail = dd,
                    Master = dm,
                    DeviceTypeName = dt != null ? dt.Name : null,
                    DeviceCategoryName = dc != null ? dc.Name : null
                };

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var search = p.Search.Trim();
                query = query.Where(x =>
                    (x.Detail.IMEI != null && x.Detail.IMEI.Contains(search)) ||
                    (x.Detail.SerialNumber != null && x.Detail.SerialNumber.Contains(search)) ||
                    (x.Detail.ShortName != null && x.Detail.ShortName.Contains(search)) ||
                    (x.Detail.LongName != null && x.Detail.LongName.Contains(search)) ||
                    (x.Detail.TagNumber != null && x.Detail.TagNumber.Contains(search)) ||
                    (x.Detail.MACAddress != null && x.Detail.MACAddress.Contains(search)) ||
                    (x.Detail.IPAddress != null && x.Detail.IPAddress.Contains(search)) ||
                    (x.Master.DeviceShortName != null && x.Master.DeviceShortName.Contains(search)) ||
                    (x.DeviceTypeName != null && x.DeviceTypeName.Contains(search)) ||
                    (x.DeviceCategoryName != null && x.DeviceCategoryName.Contains(search)));
            }

            if (p.DeviceMasterId.HasValue) query = query.Where(x => x.Detail.DeviceMasterId == p.DeviceMasterId.Value);
            if (p.CompanyId.HasValue) query = query.Where(x => x.Master.CompanyId == p.CompanyId.Value);
            if (p.DeviceCategoryId.HasValue) query = query.Where(x => x.Master.DeviceCategoryId == p.DeviceCategoryId.Value);
            if (p.DeviceTypeId.HasValue) query = query.Where(x => x.Master.DeviceTypeId == p.DeviceTypeId.Value);

            query = query.OrderBy(x => x.Detail.CreatedAt);

            int totalRecords;
            List<DeviceDetailResponseDto> data;

            if (p.IsPaginated)
            {
                totalRecords = await query.CountAsync();
                var raw = await query.Skip((p.Page!.Value - 1) * p.PageSize!.Value).Take(p.PageSize.Value).ToListAsync();
                data = raw.Select(x =>
                {
                    var dto = MapWithMaster(x.Detail, x.Master);
                    dto.DeviceTypeName = x.DeviceTypeName;
                    dto.DeviceCategoryName = x.DeviceCategoryName;
                    return dto;
                }).ToList();
            }
            else
            {
                var raw = await query.ToListAsync();
                totalRecords = raw.Count;
                data = raw.Select(x =>
                {
                    var dto = MapWithMaster(x.Detail, x.Master);
                    dto.DeviceTypeName = x.DeviceTypeName;
                    dto.DeviceCategoryName = x.DeviceCategoryName;
                    return dto;
                }).ToList();
            }

            return PagedResult<DeviceDetailResponseDto>.Create(data, totalRecords, p);
        }

        public async Task<PagedResult<DeviceDetailResponseDto>> GetDeviceDetailByMasterAsync(Guid deviceMasterId, PaginationParams p)
        {
            var master = await _db.DeviceMasters.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == deviceMasterId && !m.IsDeleted);
            if (master is null)
                return PagedResult<DeviceDetailResponseDto>.Create(Enumerable.Empty<DeviceDetailResponseDto>(), 0, p);

            var query = _db.DeviceDetails.AsNoTracking()
                .Where(d => d.DeviceMasterId == deviceMasterId).OrderBy(d => d.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<DeviceDetailResponseDto>.Create(paged.Data.Select(d => MapWithMaster(d, master)), paged.TotalRecords, p);
        }

        public async Task<PagedResult<DeviceDetailResponseDto>> GetDeviceDetailByCategoryAsync(Guid categoryId, PaginationParams p)
        {
            var query = _db.DeviceDetails.AsNoTracking()
                .Join(_db.DeviceMasters.AsNoTracking(), d => d.DeviceMasterId, m => m.Id, (d, m) => new { Detail = d, Master = m })
                .Where(x => x.Master.DeviceCategoryId == categoryId && !x.Detail.IsDeleted)
                .OrderBy(x => x.Detail.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<DeviceDetailResponseDto>.Create(paged.Data.Select(x => MapWithMaster(x.Detail, x.Master)), paged.TotalRecords, p);
        }

        public async Task<PagedResult<DeviceDetailResponseDto>> GetDeviceDetailByTypeAsync(Guid deviceTypeId, PaginationParams p)
        {
            var query = _db.DeviceDetails.AsNoTracking()
                .Join(_db.DeviceMasters.AsNoTracking(), d => d.DeviceMasterId, m => m.Id, (d, m) => new { Detail = d, Master = m })
                .Where(x => x.Master.DeviceTypeId == deviceTypeId && !x.Detail.IsDeleted)
                .OrderBy(x => x.Detail.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<DeviceDetailResponseDto>.Create(paged.Data.Select(x => MapWithMaster(x.Detail, x.Master)), paged.TotalRecords, p);
        }

        public async Task<PagedResult<DeviceDetailResponseDto>> GetDeviceDetailByCompanyAsync(Guid companyId, PaginationParams p)
        {
            var query = _db.DeviceDetails.AsNoTracking()
                .Join(_db.DeviceMasters.AsNoTracking(), d => d.DeviceMasterId, m => m.Id, (d, m) => new { Detail = d, Master = m })
                .Where(x => x.Master.CompanyId == companyId && !x.Detail.IsDeleted)
                .OrderBy(x => x.Detail.CreatedAt);
            var paged = await query.ToPagedAsync(p);
            return PagedResult<DeviceDetailResponseDto>.Create(paged.Data.Select(x => MapWithMaster(x.Detail, x.Master)), paged.TotalRecords, p);
        }

        public async Task<DeviceDetailResponseDto?> GetDeviceDetailByIdAsync(Guid id)
        {
            var result = await _db.DeviceDetails.AsNoTracking()
                .Join(_db.DeviceMasters.AsNoTracking(), d => d.DeviceMasterId, m => m.Id, (d, m) => new { Detail = d, Master = m })
                .FirstOrDefaultAsync(x => x.Detail.Id == id && !x.Detail.IsDeleted);
            return result is null ? null : MapWithMaster(result.Detail, result.Master);
        }

        public async Task<DeviceDetailResponseDto?> UpdateDeviceDetailAsync(Guid id, DeviceDetailUpdateDto dto)
        {
            var e = await _db.DeviceDetails.FindAsync(id);
            if (e is null) return null;

            if (e.DeviceMasterId != dto.DeviceMasterId)
            {
                var masterExists = await _db.DeviceMasters.AnyAsync(m => m.Id == dto.DeviceMasterId);
                if (!masterExists)
                    throw new InvalidOperationException($"DeviceMaster Id={dto.DeviceMasterId} does not exist.");
            }

            e.DeviceMasterId = dto.DeviceMasterId; e.IMEI = dto.IMEI;
            e.MACAddress = dto.MACAddress; e.TagNumber = dto.TagNumber;
            e.ShortName = dto.ShortName; e.LongName = dto.LongName;
            e.SerialNumber = dto.SerialNumber; e.PurchaseDate = dto.PurchaseDate;
            e.WarrantyExpiry = dto.WarrantyExpiry; e.PurchaseCost = dto.PurchaseCost;
            e.Remarks = dto.Remarks; e.IPAddress = dto.IPAddress;
            e.SIMNumber = dto.SIMNumber; e.RFIDTagNumber = dto.RFIDTagNumber;
            e.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var master = await _db.DeviceMasters.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == e.DeviceMasterId);
            return MapWithMaster(e, master);
        }

        public async Task<string> GenerateQRCodeForDeviceAsync(Guid deviceDetailId)
        {
            var detailInfo = await (
                from detail in _db.DeviceDetails
                join device in _db.DeviceMasters on detail.DeviceMasterId equals device.Id
                where detail.Id == deviceDetailId && !detail.IsDeleted
                select new { DetailRecord = detail, DeviceName = device.DeviceShortName }
            ).FirstOrDefaultAsync();

            if (detailInfo == null) throw new Exception("Device Detail record not found.");

            string devName = detailInfo.DeviceName?.Replace(" ", "") ?? "Dev";
            string serialNum = !string.IsNullOrEmpty(detailInfo.DetailRecord.SerialNumber)
                ? detailInfo.DetailRecord.SerialNumber.Replace(" ", "") : "NoSN";
            string rfidNum = !string.IsNullOrEmpty(detailInfo.DetailRecord.RFIDTagNumber)
                ? detailInfo.DetailRecord.RFIDTagNumber.Replace(" ", "") : "NoRF";

            string qrString = $"DeviceDetailId:{detailInfo.DetailRecord.Id}|DeviceName:{devName}|SerialNumber:{serialNum}|RFIDNumber:{rfidNum}";

            detailInfo.DetailRecord.BarcodeNumber = qrString;
            detailInfo.DetailRecord.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(20);
            return "data:image/png;base64," + Convert.ToBase64String(qrBytes);
        }

        public async Task<DeviceTrackingResponseDto> SetupDeviceTrackingAsync(Guid deviceDetailId, DeviceTrackingInputDto dto)
        {
            var detail = await _db.DeviceDetails.FirstOrDefaultAsync(d => d.Id == deviceDetailId && !d.IsDeleted);
            if (detail == null) throw new Exception("Device Detail record not found.");

            var deviceData = await (
                from device in _db.DeviceMasters
                join model in _db.ModelSpecifications on device.ModelSpecificationId equals model.Id into modelJoin
                from specification in modelJoin.DefaultIfEmpty()
                where device.Id == detail.DeviceMasterId && !device.IsDeleted
                select new
                {
                    Device = device,
                    ModelNumber = specification != null ? specification.ModelNumber : "NoModel"
                }
            ).FirstOrDefaultAsync();

            if (deviceData == null) throw new Exception("Associated Active Device Master not found.");

            if (!string.IsNullOrEmpty(dto.RFIDTagNumber))
            {
                var rfidExists = await _db.DeviceDetails.AnyAsync(d =>
                    d.RFIDTagNumber == dto.RFIDTagNumber && d.Id != deviceDetailId && !d.IsDeleted);
                if (rfidExists) throw new Exception("This RFID Tag is already assigned to another device!");
            }

            detail.SerialNumber = dto.SerialNumber;
            detail.RFIDTagNumber = dto.RFIDTagNumber;

            string devName = deviceData.Device.DeviceShortName?.Replace(" ", "") ?? "Device";
            string modelNum = (deviceData.ModelNumber ?? "").Replace(" ", "");
            string serialNum = !string.IsNullOrEmpty(dto.SerialNumber) ? dto.SerialNumber.Replace(" ", "") : "NoSerial";
            string rfidNum = !string.IsNullOrEmpty(dto.RFIDTagNumber) ? dto.RFIDTagNumber.Replace(" ", "") : "NoRFID";
            string companyId = deviceData.Device.CompanyId?.ToString() ?? "NoCompany";

            string qrText = $"DD:{detail.Id}|DeviceName:{devName}|CompanyId:{companyId}|ModelNumber:{modelNum}|SerialNumber:{serialNum}|RFIDNumber:{rfidNum}";
            detail.BarcodeNumber = qrText;
            detail.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new DeviceTrackingResponseDto
            {
                Message = "Data updated successfully. Generate QR on frontend.",
                DeviceMasterId = detail.DeviceMasterId,
                DeviceShortName = deviceData.Device.DeviceShortName,
                CompanyId = deviceData.Device.CompanyId,
                ModelNumber = deviceData.ModelNumber,
                SerialNumber = detail.SerialNumber,
                RFIDTagNumber = detail.RFIDTagNumber,
                BarcodeText = qrText
            };
        }

        public async Task<DeviceDetailImportResponse> ImportDeviceDetailExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) throw new Exception("Please upload a valid Excel file.");

            var response = new DeviceDetailImportResponse();
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            if (worksheet.Dimension == null) throw new Exception("Excel is empty.");

            int rowCount = worksheet.Dimension.Rows;
            var rows = new List<DeviceDetailImportRow>(rowCount - 1);
            for (int row = 2; row <= rowCount; row++)
            {
                rows.Add(new DeviceDetailImportRow
                {
                    CategoryName = worksheet.Cells[row, 1].Text?.Trim(),
                    DeviceTypeName = worksheet.Cells[row, 2].Text?.Trim(),
                    ModelName = worksheet.Cells[row, 3].Text?.Trim(),
                    ModelNumber = worksheet.Cells[row, 4].Text?.Trim(),
                    DeviceShortName = worksheet.Cells[row, 5].Text?.Trim(),
                    DeviceLongName = worksheet.Cells[row, 6].Text?.Trim(),
                    IMEI = worksheet.Cells[row, 7].Text?.Trim(),
                    MACAddress = worksheet.Cells[row, 8].Text?.Trim(),
                    TagNumber = worksheet.Cells[row, 9].Text?.Trim(),
                    SerialNumber = worksheet.Cells[row, 10].Text?.Trim(),
                    PurchaseDate = DateTime.TryParse(worksheet.Cells[row, 11].Text, out var pd) ? pd : null,
                    WarrantyExpiry = DateTime.TryParse(worksheet.Cells[row, 12].Text, out var wd) ? wd : null,
                    PurchaseCost = decimal.TryParse(worksheet.Cells[row, 13].Text, out var pc) ? pc : null,
                    IPAddress = worksheet.Cells[row, 14].Text?.Trim(),
                    SIMNumber = worksheet.Cells[row, 15].Text?.Trim(),
                    Remarks = worksheet.Cells[row, 16].Text?.Trim()
                });
            }

            // Note: CompanyId is an external Guid reference — not looked up in this service.
            // Rows must provide CompanyId in the import or set it as Guid.Empty / null.
            var categoryNames = rows.Select(r => r.CategoryName).Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var existingCategories = await _db.DeviceCategories
                .Where(c => !c.IsDeleted && categoryNames.Contains(c.Name)).ToListAsync();
            var categoriesByName = existingCategories.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            var toCreateCategories = categoryNames.Where(n => !categoriesByName.ContainsKey(n))
                .Select(n => new DeviceCategory { Name = n, IsActive = true, CreatedAt = DateTime.UtcNow }).ToList();

            if (toCreateCategories.Any())
            {
                _db.DeviceCategories.AddRange(toCreateCategories);
                await _db.SaveChangesAsync();
                foreach (var c in toCreateCategories) categoriesByName[c.Name] = c;
                response.CategoriesCreated.AddRange(toCreateCategories.Select(c => c.Name));
            }

            var deviceTypesByCategoryAndName = new Dictionary<(Guid categoryId, string name), DeviceType>();
            foreach (var kv in rows.Where(r => !string.IsNullOrWhiteSpace(r.CategoryName) && !string.IsNullOrWhiteSpace(r.DeviceTypeName))
                .GroupBy(r => r.CategoryName!.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                if (!categoriesByName.TryGetValue(kv.Key, out var cat)) continue;
                var names = kv.Select(r => r.DeviceTypeName!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var existing = await _db.DeviceTypes.Where(dt => dt.DeviceCategoryId == cat.Id && names.Contains(dt.Name)).ToListAsync();
                foreach (var et in existing) deviceTypesByCategoryAndName[(cat.Id, et.Name)] = et;

                var missing = names.Where(n => !existing.Any(e => string.Equals(e.Name, n, StringComparison.OrdinalIgnoreCase))).ToList();
                if (missing.Any())
                {
                    var toCreate = missing.Select(n => new DeviceType { Name = n, DeviceCategoryId = cat.Id, CreatedAt = DateTime.UtcNow }).ToList();
                    _db.DeviceTypes.AddRange(toCreate);
                    await _db.SaveChangesAsync();
                    response.DeviceTypesCreated.AddRange(toCreate.Select(t => t.Name));
                    foreach (var t in toCreate) deviceTypesByCategoryAndName[(cat.Id, t.Name)] = t;
                }
            }

            var imeis = rows.Select(r => r.IMEI).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var serials = rows.Select(r => r.SerialNumber).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var existingImeis = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existingSerials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (imeis.Any() || serials.Any())
            {
                var existingDetails = await _db.DeviceDetails
                    .Where(d => (d.IMEI != null && imeis.Contains(d.IMEI)) || (d.SerialNumber != null && serials.Contains(d.SerialNumber)))
                    .Select(d => new { d.IMEI, d.SerialNumber }).ToListAsync();
                foreach (var ed in existingDetails)
                {
                    if (!string.IsNullOrWhiteSpace(ed.IMEI)) existingImeis.Add(ed.IMEI!);
                    if (!string.IsNullOrWhiteSpace(ed.SerialNumber)) existingSerials.Add(ed.SerialNumber!);
                }
            }

            var detailsToAdd = new List<DeviceDetail>();
            var mastersByKey = new Dictionary<(Guid catId, Guid typeId, string shortName), DeviceMaster>();

            // ── OPTIMIZATION (N+1 fix): jaise upar Categories/DeviceTypes ke
            //    liye kiya, waise hi ModelSpecifications aur DeviceMasters bhi
            //    bulk me ek-ek query se pre-fetch karo — loop ke andar phir
            //    koi DB round-trip nahi lagega, sirf in-memory dictionary check ──
            var knownDeviceTypeIds = deviceTypesByCategoryAndName.Values.Select(t => t.Id).Distinct().ToList();

            // Note: ModelSpecification.DeviceTypeId aur DeviceMaster.DeviceTypeId/DeviceCategoryId
            // models me nullable (Guid?) hain, isliye List<Guid>.Contains() ke saath direct
            // compare nahi hota — .Value / ?? Guid.Empty se explicitly unwrap karna padta hai.
            var modelSpecByKey = (await _db.ModelSpecifications
                    .Where(m => !m.IsDeleted && m.DeviceTypeId.HasValue && knownDeviceTypeIds.Contains(m.DeviceTypeId.Value))
                    .ToListAsync())
                .ToDictionary(m => (m.DeviceTypeId ?? Guid.Empty, m.Name ?? ""), m => m);

            foreach (var em in await _db.DeviceMasters
                .Where(m => !m.IsDeleted && m.DeviceTypeId.HasValue && knownDeviceTypeIds.Contains(m.DeviceTypeId.Value))
                .ToListAsync())
            {
                mastersByKey[(em.DeviceCategoryId ?? Guid.Empty, em.DeviceTypeId ?? Guid.Empty, em.DeviceShortName ?? "")] = em;
            }

            // Naye records yahin collect honge, DB me ek hi baar (batch) me jaayenge
            var newDeviceTypes = new List<DeviceType>();
            var newModelSpecs = new List<ModelSpecification>();
            var newMasters = new List<DeviceMaster>();

            foreach (var row in rows)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(row.CategoryName))
                    { response.SkippedDevices.Add($"{row.IMEI ?? row.SerialNumber ?? "Unknown"} - Category missing"); continue; }

                    if (!categoriesByName.TryGetValue(row.CategoryName!, out var category))
                    { response.SkippedDevices.Add($"{row.CategoryName} - Category not found"); continue; }

                    var typeKey = (category.Id, row.DeviceTypeName?.Trim() ?? "");
                    if (!deviceTypesByCategoryAndName.TryGetValue(typeKey, out var deviceType))
                    {
                        // Id client-side generate hoti hai (model me Guid.NewGuid() default),
                        // isliye abhi reference bana sakte hain, DB insert loop ke baad batch me hoga
                        deviceType = new DeviceType { Name = row.DeviceTypeName ?? "Unknown", DeviceCategoryId = category.Id, CreatedAt = DateTime.UtcNow };
                        deviceTypesByCategoryAndName[typeKey] = deviceType;
                        newDeviceTypes.Add(deviceType);
                        response.DeviceTypesCreated.Add(deviceType.Name);
                    }

                    // Model spec — in-memory dictionary check (pehle yahan har row pe DB call thi)
                    var modelName = row.ModelName?.Trim() ?? "";
                    var modelKey = (deviceType.Id, modelName);
                    if (!modelSpecByKey.TryGetValue(modelKey, out var modelSpec))
                    {
                        modelSpec = new ModelSpecification
                        {
                            Name = modelName,
                            ModelNumber = row.ModelNumber?.Trim(),
                            DeviceTypeId = deviceType.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        modelSpecByKey[modelKey] = modelSpec;
                        newModelSpecs.Add(modelSpec);
                        response.ModelsCreated.Add(modelSpec.Name);
                    }

                    var masterKey = (category.Id, deviceType.Id, row.DeviceShortName?.Trim() ?? "");
                    if (!mastersByKey.TryGetValue(masterKey, out var master))
                    {
                        master = new DeviceMaster
                        {
                            DeviceCategoryId = category.Id,
                            DeviceTypeId = deviceType.Id,
                            ModelSpecificationId = modelSpec.Id,
                            DeviceShortName = row.DeviceShortName,
                            DeviceLongName = row.DeviceLongName,
                            CreatedAt = DateTime.UtcNow
                        };
                        mastersByKey[masterKey] = master;
                        newMasters.Add(master);
                        response.DeviceMastersCreated.Add(master.DeviceShortName ?? "");
                    }

                    if (!string.IsNullOrWhiteSpace(row.IMEI) && existingImeis.Contains(row.IMEI!))
                    { response.SkippedDevices.Add($"{row.IMEI} - Already Exists"); continue; }
                    if (!string.IsNullOrWhiteSpace(row.SerialNumber) && existingSerials.Contains(row.SerialNumber!))
                    { response.SkippedDevices.Add($"{row.SerialNumber} - Already Exists"); continue; }

                    var detail = new DeviceDetail
                    {
                        DeviceMasterId = master.Id,
                        IMEI = row.IMEI,
                        MACAddress = row.MACAddress,
                        TagNumber = row.TagNumber,
                        ShortName = row.DeviceShortName,
                        LongName = row.DeviceLongName,
                        SerialNumber = row.SerialNumber,
                        PurchaseDate = row.PurchaseDate,
                        WarrantyExpiry = row.WarrantyExpiry,
                        PurchaseCost = row.PurchaseCost,
                        IPAddress = row.IPAddress,
                        SIMNumber = row.SIMNumber,
                        Remarks = row.Remarks,
                        CreatedAt = DateTime.UtcNow
                    };
                    detailsToAdd.Add(detail);
                    if (!string.IsNullOrWhiteSpace(detail.IMEI)) existingImeis.Add(detail.IMEI!);
                    if (!string.IsNullOrWhiteSpace(detail.SerialNumber)) existingSerials.Add(detail.SerialNumber!);
                    response.DevicesImported.Add($"{detail.ShortName} ({detail.IMEI})");
                }
                catch (Exception ex)
                {
                    response.SkippedDevices.Add(ex.Message);
                }
            }

            // ── OPTIMIZATION (N+1 fix): pehle DeviceType/ModelSpecification/DeviceMaster
            //    har naye row pe alag SaveChangesAsync() call karte the (teen extra DB
            //    round-trips per new row). Ab sab ek hi SaveChanges me jaate hain ──
            if (newDeviceTypes.Any()) await _db.DeviceTypes.AddRangeAsync(newDeviceTypes);
            if (newModelSpecs.Any()) await _db.ModelSpecifications.AddRangeAsync(newModelSpecs);
            if (newMasters.Any()) await _db.DeviceMasters.AddRangeAsync(newMasters);
            if (newDeviceTypes.Any() || newModelSpecs.Any() || newMasters.Any())
                await _db.SaveChangesAsync();

            const int batchSize = 200;
            for (int i = 0; i < detailsToAdd.Count; i += batchSize)
            {
                _db.DeviceDetails.AddRange(detailsToAdd.Skip(i).Take(batchSize));
                await _db.SaveChangesAsync();
            }

            return response;
        }

        //public async Task<bool> DeleteAsync(Guid id)
        //{
        //    var e = await _db.DeviceDetails.FindAsync(id);
        //    if (e is null) return false;
        //    e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow;
        //    await _db.SaveChangesAsync();
        //    return true;
        //}

        private static DeviceDetailResponseDto MapWithMaster(DeviceDetail d, DeviceMaster? m) => new()
        {
            Id = d.Id,
            DeviceMasterId = d.DeviceMasterId,
            ParentDeviceDetailId = d.ParentDeviceDetailId,
            DeviceCategoryId = m?.DeviceCategoryId,
            DeviceTypeId = m?.DeviceTypeId,
            CompanyId = m?.CompanyId,
            IMEI = d.IMEI,
            MACAddress = d.MACAddress,
            TagNumber = d.TagNumber,
            ShortName = d.ShortName,
            LongName = d.LongName,
            SerialNumber = d.SerialNumber,
            PurchaseDate = d.PurchaseDate,
            WarrantyExpiry = d.WarrantyExpiry,
            PurchaseCost = d.PurchaseCost,
            Remarks = d.Remarks,
            IPAddress = d.IPAddress,
            SIMNumber = d.SIMNumber,
            RFIDTagNumber = d.RFIDTagNumber,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        };

        private class DeviceDetailImportRow
        {
            public string? CategoryName { get; set; }
            public string? DeviceTypeName { get; set; }
            public string? ModelName { get; set; }
            public string? ModelNumber { get; set; }
            public string? DeviceShortName { get; set; }
            public string? DeviceLongName { get; set; }
            public string? IMEI { get; set; }
            public string? MACAddress { get; set; }
            public string? TagNumber { get; set; }
            public string? SerialNumber { get; set; }
            public DateTime? PurchaseDate { get; set; }
            public DateTime? WarrantyExpiry { get; set; }
            public decimal? PurchaseCost { get; set; }
            public string? IPAddress { get; set; }
            public string? SIMNumber { get; set; }
            public string? Remarks { get; set; }
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  DEVICE ASSOCIATION (Sub-Device Parent-Child Management)
    // ══════════════════════════════════════════════════════════════

    public interface IDeviceAssociationService
    {
        Task<List<DeviceDropdownDto>> GetAvailableDevicesAsync(Guid excludeParentId);
        Task<List<AssociateResultDto>> AssociateDevicesAsync(Guid parentId, List<Guid> childIds);
        Task<DeviceDetailResponseDto> UnassociateDevicesAsync(Guid childId);
        Task<List<SubDeviceDto>> GetSubDevicesAsync(Guid parentId);
        Task<DeviceDetailResponseDto?> GetWithSubDevicesAsync(Guid deviceDetailId);
    }

    public class DeviceAssociationService : IDeviceAssociationService
    {
        private readonly DBContext _db;
        public DeviceAssociationService(DBContext db) => _db = db;

        public async Task<List<DeviceDropdownDto>> GetAvailableDevicesAsync(Guid excludeParentId)
        {
            return await (
                from dd in _db.DeviceDetails.AsNoTracking()
                join dm in _db.DeviceMasters.AsNoTracking() on dd.DeviceMasterId equals dm.Id
                where !dd.IsDeleted && dd.ParentDeviceDetailId == null && dd.Id != excludeParentId
                select new DeviceDropdownDto
                {
                    Id = dd.Id,
                    ShortName = dd.ShortName,
                    SerialNumber = dd.SerialNumber,
                    DeviceMasterName = dm.DeviceShortName,
                    IsAlreadyAssigned = false
                }
            ).ToListAsync();
        }

        public async Task<List<AssociateResultDto>> AssociateDevicesAsync(Guid parentId, List<Guid> childIds)
        {
            var results = new List<AssociateResultDto>();
            if (childIds == null || !childIds.Any()) return results;

            var parent = await _db.DeviceDetails.FirstOrDefaultAsync(d => d.Id == parentId && !d.IsDeleted);
            if (parent is null)
            {
                foreach (var id in childIds)
                    results.Add(new AssociateResultDto { ChildDeviceDetailId = id, Success = false, Message = $"Parent device (Id={parentId}) not found." });
                return results;
            }

            var childDevices = await _db.DeviceDetails.Where(d => childIds.Contains(d.Id) && !d.IsDeleted).ToListAsync();
            var foundIds = childDevices.Select(d => d.Id).ToHashSet();

            foreach (var childId in childIds)
            {
                if (!foundIds.Contains(childId)) { results.Add(new AssociateResultDto { ChildDeviceDetailId = childId, Success = false, Message = $"Device (Id={childId}) not found or deleted." }); continue; }

                var child = childDevices.First(d => d.Id == childId);
                if (parentId == childId) { results.Add(new AssociateResultDto { ChildDeviceDetailId = childId, Success = false, Message = "A device cannot be its own sub-device." }); continue; }
                if (child.ParentDeviceDetailId != null) { results.Add(new AssociateResultDto { ChildDeviceDetailId = childId, Success = false, Message = $"Already a sub-device of Id={child.ParentDeviceDetailId}. Unassociate first." }); continue; }

                try { await CheckCircularAsync(parentId, childId); }
                catch (InvalidOperationException ex) { results.Add(new AssociateResultDto { ChildDeviceDetailId = childId, Success = false, Message = ex.Message }); continue; }

                child.ParentDeviceDetailId = parentId;
                child.UpdatedAt = DateTime.UtcNow;
                results.Add(new AssociateResultDto
                {
                    ChildDeviceDetailId = childId,
                    Success = true,
                    Message = $"Device Id={childId} successfully associated.",
                    Data = new SubDeviceDto { Id = child.Id, ParentDeviceDetailId = parentId, ShortName = child.ShortName, SerialNumber = child.SerialNumber, IMEI = child.IMEI, DeviceMasterId = child.DeviceMasterId, CreatedAt = child.CreatedAt }
                });
            }

            await _db.SaveChangesAsync();
            return results;
        }

        public async Task<DeviceDetailResponseDto> UnassociateDevicesAsync(Guid childId)
        {
            var child = await _db.DeviceDetails.FirstOrDefaultAsync(d => d.Id == childId && !d.IsDeleted);
            if (child is null) throw new InvalidOperationException($"Device (Id={childId}) not found.");
            if (child.ParentDeviceDetailId is null) throw new InvalidOperationException("This is not a sub-device of any device.");

            child.ParentDeviceDetailId = null;
            child.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await BuildResponseAsync(child) ?? throw new Exception("Could not build device response.");
        }

        public async Task<List<SubDeviceDto>> GetSubDevicesAsync(Guid parentId)
        {
            return await (
                from dd in _db.DeviceDetails.AsNoTracking()
                join dm in _db.DeviceMasters.AsNoTracking() on dd.DeviceMasterId equals dm.Id
                where dd.ParentDeviceDetailId == parentId && !dd.IsDeleted
                select new SubDeviceDto
                {
                    Id = dd.Id,
                    ParentDeviceDetailId = dd.ParentDeviceDetailId,
                    ShortName = dd.ShortName,
                    SerialNumber = dd.SerialNumber,
                    IMEI = dd.IMEI,
                    DeviceMasterName = dm.DeviceShortName,
                    DeviceMasterId = dd.DeviceMasterId,
                    CreatedAt = dd.CreatedAt
                }
            ).ToListAsync();
        }

        public async Task<DeviceDetailResponseDto?> GetWithSubDevicesAsync(Guid deviceDetailId)
        {
            var device = await _db.DeviceDetails.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == deviceDetailId && !d.IsDeleted);
            if (device is null) return null;
            return await BuildResponseAsync(device);
        }

        private async Task CheckCircularAsync(Guid parentId, Guid childId)
        {
            var visited = new HashSet<Guid>();
            var queue = new Queue<Guid>();
            queue.Enqueue(childId);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (!visited.Add(cur)) continue;
                if (cur == parentId) throw new InvalidOperationException("Circular reference detected!");
                var children = await _db.DeviceDetails.AsNoTracking()
                    .Where(d => d.ParentDeviceDetailId == cur && !d.IsDeleted)
                    .Select(d => d.Id).ToListAsync();
                foreach (var c in children) queue.Enqueue(c);
            }
        }

        private async Task<DeviceDetailResponseDto?> BuildResponseAsync(DeviceDetail device)
        {
            var master = await _db.DeviceMasters.AsNoTracking().FirstOrDefaultAsync(m => m.Id == device.DeviceMasterId);
            DeviceDetail? parent = null;
            if (device.ParentDeviceDetailId.HasValue)
                parent = await _db.DeviceDetails.AsNoTracking().FirstOrDefaultAsync(d => d.Id == device.ParentDeviceDetailId.Value);

            var subDevices = await (
                from dd in _db.DeviceDetails.AsNoTracking()
                join dm in _db.DeviceMasters.AsNoTracking() on dd.DeviceMasterId equals dm.Id
                where dd.ParentDeviceDetailId == device.Id && !dd.IsDeleted
                select new SubDeviceDto
                {
                    Id = dd.Id,
                    ParentDeviceDetailId = dd.ParentDeviceDetailId,
                    ShortName = dd.ShortName,
                    SerialNumber = dd.SerialNumber,
                    IMEI = dd.IMEI,
                    DeviceMasterName = dm.DeviceShortName,
                    DeviceMasterId = dd.DeviceMasterId,
                    CreatedAt = dd.CreatedAt
                }
            ).ToListAsync();

            return new DeviceDetailResponseDto
            {
                Id = device.Id,
                DeviceMasterId = device.DeviceMasterId,
                ParentDeviceDetailId = device.ParentDeviceDetailId,
                ParentDeviceShortName = parent?.ShortName,
                ParentSerialNumber = parent?.SerialNumber,
                DeviceCategoryId = master?.DeviceCategoryId,
                DeviceTypeId = master?.DeviceTypeId,
                CompanyId = master?.CompanyId,
                IMEI = device.IMEI,
                MACAddress = device.MACAddress,
                TagNumber = device.TagNumber,
                ShortName = device.ShortName,
                LongName = device.LongName,
                SerialNumber = device.SerialNumber,
                PurchaseDate = device.PurchaseDate,
                WarrantyExpiry = device.WarrantyExpiry,
                PurchaseCost = device.PurchaseCost,
                Remarks = device.Remarks,
                IPAddress = device.IPAddress,
                SIMNumber = device.SIMNumber,
                RFIDTagNumber = device.RFIDTagNumber,
                SubDevices = subDevices,
                CreatedAt = device.CreatedAt,
                UpdatedAt = device.UpdatedAt
            };
        }
    }
}