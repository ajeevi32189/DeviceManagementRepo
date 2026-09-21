using DeviceManagementOnly.Common;
using Microsoft.AspNetCore.Authorization;
using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.Device;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace DeviceManagementOnly.Controllers
{
    // ══════════════════════════════════════════════════════════════
    // DEVICE CATEGORY
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceCategoryController : ControllerBase
    {
        private readonly IDeviceCategoryService _service;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        public DeviceCategoryController(IDeviceCategoryService service, DeviceManagementOnly.Common.ImportExport.IExportService exportService)
        {
            _service = service;
            _exportService = exportService;
        }

        [HttpPost("create-deviceCategory")]
        public async Task<IActionResult> CreateDeviceCategory([FromBody] DeviceCategoryCreateDto dto)
        {
            var result = await _service.CreateDeviceCategoryAsync(dto);
            return Ok(result);
        }

        [HttpGet("get-all-category")]
        public async Task<IActionResult> GetAllDeviceCategory([FromQuery] PaginationParams p) =>
            Ok(await _service.GetAllDeviceCategoryAsync(p));

        [HttpGet("get-category/{id:guid}")]
        public async Task<IActionResult> GetDeviceCategoryById(Guid id)
        {
            var result = await _service.GetDeviceCategoryByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("update-category{id:guid}")]
        public async Task<IActionResult> UpdateDeviceCategory(Guid id, [FromBody] DeviceCategoryUpdateDto dto)
        {
            var result = await _service.UpdateDeviceCategoryAsync(id, dto);
            return result == null ? NotFound() : Ok(result);
        }

        //[HttpDelete("soft-delete/{id:guid}")]
        //public async Task<IActionResult> Delete(Guid id)
        //{
        //    var result = await _service.DeleteAsync(id);
        //    return result ? Ok(new { message = "Deleted successfully." }) : NotFound();
        //}

        /// <summary>
        /// Excel Import — columns: Name, Remarks
        /// POST /api/devicecategory/import
        /// </summary>
        [HttpPost("import-deviceCategory")]
        public async Task<IActionResult> ImportExcelDeviceCategory([FromForm] DeviceCategoryImportDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { message = "Please upload an Excel file." });

            var result = await _service.ImportDeviceCategoryExcelAsync(dto.File);
            return Ok(result);
        }

        // GET: api/DeviceCategory/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] PaginationParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var result = await _service.GetAllDeviceCategoryAsync(p);
            var file = await _exportService.ExportAsync(result.Data, exportFormat, "DeviceCategories");
            return File(file.Content, file.ContentType, file.FileName);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // DEVICE TYPE
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceTypeController : ControllerBase
    {
        private readonly IDeviceTypeService _service;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        public DeviceTypeController(IDeviceTypeService service, DeviceManagementOnly.Common.ImportExport.IExportService exportService)
        {
            _service = service;
            _exportService = exportService;
        }

        [HttpPost("create-devicetype")]
        public async Task<IActionResult> CreateDeviceType([FromBody] DeviceTypeCreateDto dto)
        {
            var result = await _service.CreateDeviceTypeAsync(dto);
            return Ok(result);
        }

        [HttpGet("get-all-devicetype")]
        public async Task<IActionResult> GetAllDeviceType([FromQuery] PaginationParams p) =>
            Ok(await _service.GetAllDeviceTypeAsync(p));

        [HttpGet("get-category/{categoryId:guid}")]
        public async Task<IActionResult> GetDeviceTypeByCategory(Guid categoryId, [FromQuery] PaginationParams p) =>
            Ok(await _service.GetDeviceTypeByCategoryAsync(categoryId, p));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDeviceTypeById(Guid id)
        {
            var result = await _service.GetDeviceTypeByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("update-type/{id:guid}")]
        public async Task<IActionResult> UpdateDeviceType(Guid id, [FromBody] DeviceTypeUpdateDto dto)
        {
            var result = await _service.UpdateDeviceTypeAsync(id, dto);
            return result == null ? NotFound() : Ok(result);
        }

        //[HttpDelete("soft-delete/{id:guid}")]
        //public async Task<IActionResult> Delete(Guid id)
        //{
        //    var result = await _service.DeleteAsync(id);
        //    return result ? Ok(new { message = "Deleted successfully." }) : NotFound();
        //}

        /// <summary>
        /// Excel Import — columns: DeviceTypeName, CategoryName, Protocol, SupportedFeature, Remarks
        /// POST /api/devicetype/import-devicetype
        /// </summary>
        [HttpPost("import-devicetype")]
        public async Task<IActionResult> ImportExcelDeviceType([FromForm] DeviceTypeImportDto dto)
        {
            try
            {
                if (dto.File == null || dto.File.Length == 0)
                    return BadRequest(new { message = "Please upload a valid Excel file." });

                var result = await _service.ImportDeviceTypeExcelAsync(dto.File);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/DeviceType/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] PaginationParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var result = await _service.GetAllDeviceTypeAsync(p);
            var file = await _exportService.ExportAsync(result.Data, exportFormat, "DeviceTypes");
            return File(file.Content, file.ContentType, file.FileName);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // MODEL CATEGORY
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ModelCategoryController : ControllerBase
    {
        private readonly IModelCategoryService _service;
        private readonly DBContext _db;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;

        public ModelCategoryController(IModelCategoryService service, DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _service = service;
            _db = db;
            _exportService = exportService;
            _importService = importService;
        }

        [HttpPost("create-model")]
        public async Task<IActionResult> CreateModelCategory([FromBody] ModelCategoryCreateDto dto)
        {
            var result = await _service.CreateModelCategoryAsync(dto);
            return Ok(result);
        }

        [HttpGet("get-all-model")]
        public async Task<IActionResult> GetAllModelCategory([FromQuery] PaginationParams p) =>
            Ok(await _service.GetAllModelCategoryAsync(p));

        // GET: api/ModelCategory/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] PaginationParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var result = await _service.GetAllModelCategoryAsync(p);
            var file = await _exportService.ExportAsync(result.Data, exportFormat, "ModelCategories");
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpGet("get-model/{id:guid}")]
        public async Task<IActionResult> GetModelCategoryById(Guid id)
        {
            var result = await _service.GetModelCategoryByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("update-model/{id:guid}")]
        public async Task<IActionResult> UpdateModelCategory(Guid id, [FromBody] ModelCategoryUpdateDto dto)
        {
            var result = await _service.UpdateModelCategoryAsync(id, dto);
            return result == null ? NotFound() : Ok(result);
        }

        //[HttpDelete("soft-delete/{id:guid}")]
        //public async Task<IActionResult> Delete(Guid id)
        //{
        //    var result = await _service.DeleteAsync(id);
        //    return result ? Ok(new { message = "Deleted successfully." }) : NotFound();
        //}

        /// <summary>
        /// Import — Excel (.xlsx) OR CSV (.csv). Columns: Name (or CategoryName), Remarks.
        /// Unknown columns don't error out — their values are saved to a downloadable .txt
        /// (see response's UnmatchedFileUrl). Column matching also honors any mapping
        /// saved earlier via /api/FieldMapping/save for entityName="ModelCategory".
        /// POST /api/modelcategory/import-modelcategories
        /// </summary>
        [HttpPost("import-modelcategories")]
        public async Task<IActionResult> ImportModelCategories(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var result = await _importService.ImportAsync<ModelCategoryCreateDto>(
                file, "ModelCategory", null,
                async (dto, rowNumber) =>
                {
                    if (string.IsNullOrWhiteSpace(dto.Name))
                        throw new InvalidOperationException("Name is required.");

                    var exists = await _db.ModelCategories.AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower() && !c.IsDeleted);
                    if (exists) throw new InvalidOperationException($"'{dto.Name}' already exists.");

                    _db.ModelCategories.Add(new ModelCategory { Name = dto.Name, Remarks = dto.Remarks, IsActive = true, CreatedAt = DateTime.UtcNow });
                    await _db.SaveChangesAsync();
                });

            return Ok(result);
        }

        private static string? GetColByHeader(ExcelWorksheet ws, int row, string header)
        {
            if (ws.Dimension == null) return null;
            for (int col = 1; col <= ws.Dimension.End.Column; col++)
                if (ws.Cells[1, col].Text.Trim().Equals(header, StringComparison.OrdinalIgnoreCase))
                    return ws.Cells[row, col].Text.Trim();
            return null;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // MODEL SPECIFICATION
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ModelSpecificationController : ControllerBase
    {
        private readonly IModelSpecificationService _service;
        private readonly DBContext _db;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;

        public ModelSpecificationController(IModelSpecificationService service, DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService)
        {
            _service = service;
            _db = db;
            _exportService = exportService;
        }

        [HttpPost("create-modelspecification")]
        public async Task<IActionResult> CreateModelSpecification([FromBody] ModelSpecificationCreateDto dto)
        {
            var result = await _service.CreateModelSpecificationAsync(dto);
            return Ok(result);
        }

        [HttpGet("get-all-modelspecification")]
        public async Task<IActionResult> GetAllModelSpecification([FromQuery] ModelSpecificationFilterParams p) =>
            Ok(await _service.GetAllModelSpecificationAsync(p));

        // GET: api/ModelSpecification/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] ModelSpecificationFilterParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var result = await _service.GetAllModelSpecificationAsync(p);
            var file = await _exportService.ExportAsync(result.Data, exportFormat, "ModelSpecifications");
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpGet("modelspecification-by-device-type/{deviceTypeId:guid}")]
        public async Task<IActionResult> GetModelSpecificationByDeviceType(Guid deviceTypeId, [FromQuery] PaginationParams p) =>
            Ok(await _service.GetModelSpecificationByDeviceTypeAsync(deviceTypeId, p));

        [HttpGet("modelspecification-by-company/{companyId:guid}")]
        public async Task<IActionResult> GetModelSpecificationByCompany(Guid companyId, [FromQuery] PaginationParams p) =>
            Ok(await _service.GetModelSpecificationByCompanyAsync(companyId, p));

        [HttpGet("get-modelspecification/{id:guid}")]
        public async Task<IActionResult> GetModelSpecificationById(Guid id)
        {
            var result = await _service.GetModelSpecificationByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("update-modelspecification/{id:guid}")]
        public async Task<IActionResult> UpdateModelSpecification(Guid id, [FromBody] ModelSpecificationUpdateDto dto)
        {
            var result = await _service.UpdateModelSpecificationAsync(id, dto);
            return result == null ? NotFound() : Ok(result);
        }

        //[HttpPatch("{id:guid}/soft-delete")]
        //public async Task<IActionResult> SoftDelete(Guid id, [FromQuery] string? deletedBy)
        //{
        //    var result = await _service.SoftDeleteAsync(id, deletedBy);
        //    return result ? Ok(new { message = "Soft deleted." }) : NotFound();
        //}

        /// <summary>
        /// Excel Import — columns: Name, ModelNumber, DeviceTypeName, CategoryName, ParameterName,
        ///   ParameterValue, Unit, Capacity, Processor, RAM, Storage, FirmwareVersion, Protocol,
        ///   WarrantyDuration, EndOfLife, Intro, Uses, Feature
        /// POST /api/modelspecification/import-specifications
        /// </summary>
        [HttpPost("import-specifications")]
        public async Task<IActionResult> ImportModelSpecifications(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Please upload a valid Excel file." });

                int newCats = 0, skipCats = 0, newSpecs = 0, skipSpecs = 0, newParams = 0, skipParams = 0;
                var skipDetails = new List<string>();

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);
                var ws = package.Workbook.Worksheets[0];
                if (ws?.Dimension == null) return BadRequest(new { message = "Excel is empty." });

                int rowCount = ws.Dimension.End.Row;

                for (int row = 2; row <= rowCount; row++)
                {
                    var modelName = Col(ws, row, "Name");
                    if (string.IsNullOrWhiteSpace(modelName)) { skipDetails.Add($"Row {row}: Name empty."); continue; }

                    // Resolve CategoryName → ModelCategory (auto-create if missing)
                    var catName = Col(ws, row, "CategoryName");
                    Guid? categoryId = null;
                    if (!string.IsNullOrWhiteSpace(catName))
                    {
                        var cat = await _db.ModelCategories.FirstOrDefaultAsync(c => c.Name == catName && !c.IsDeleted);
                        if (cat == null)
                        {
                            cat = new ModelCategory { Name = catName, IsActive = true, CreatedAt = DateTime.UtcNow };
                            _db.ModelCategories.Add(cat); await _db.SaveChangesAsync(); newCats++;
                        }
                        else skipCats++;
                        categoryId = cat.Id;
                    }

                    // Resolve DeviceTypeName → DeviceType
                    var dtName = Col(ws, row, "DeviceTypeName");
                    Guid? deviceTypeId = null;
                    if (!string.IsNullOrWhiteSpace(dtName))
                    {
                        var dt = await _db.DeviceTypes.FirstOrDefaultAsync(d => d.Name == dtName && !d.IsDeleted);
                        deviceTypeId = dt?.Id;
                    }

                    // Upsert ModelSpecification
                    var spec = await _db.ModelSpecifications.FirstOrDefaultAsync(s => s.Name.ToLower() == modelName.ToLower() && !s.IsDeleted);
                    if (spec == null)
                    {
                        spec = new ModelSpecification
                        {
                            Name = modelName,
                            ModelNumber = Col(ws, row, "ModelNumber"),
                            DeviceTypeId = deviceTypeId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.ModelSpecifications.Add(spec); await _db.SaveChangesAsync(); newSpecs++;
                    }
                    else skipSpecs++;

                    // Add ModelParameter if ParameterName present
                    var paramName = Col(ws, row, "ParameterName");
                    if (!string.IsNullOrWhiteSpace(paramName))
                    {
                        if (categoryId == null) { skipDetails.Add($"Row {row}: Param '{paramName}' skipped — no CategoryName."); skipParams++; continue; }

                        bool paramExists = await _db.ModelParameters.AnyAsync(p =>
                            p.ParameterName.ToLower() == paramName.ToLower() && p.DeviceModelId == spec.Id && !p.IsDeleted);

                        if (!paramExists)
                        {
                            _db.ModelParameters.Add(new ModelParameter
                            {
                                DeviceModelId = spec.Id,
                                ModelCategoryId = categoryId.Value,
                                ParameterName = paramName,
                                ParameterValue = Col(ws, row, "ParameterValue") ?? "N/A",
                                Unit = Col(ws, row, "Unit"),
                                Capacity = Col(ws, row, "Capacity"),
                                Processor = Col(ws, row, "Processor"),
                                RAM = Col(ws, row, "RAM"),
                                Storage = Col(ws, row, "Storage"),
                                FirmwareVersion = Col(ws, row, "FirmwareVersion"),
                                Protocol = Col(ws, row, "Protocol"),
                                WarrantyDuration = Col(ws, row, "WarrantyDuration"),
                                EndOfLife = Col(ws, row, "EndOfLife"),
                                Intro = Col(ws, row, "Intro"),
                                Uses = Col(ws, row, "Uses"),
                                Feature = Col(ws, row, "Feature"),
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = "Import"
                            });
                            newParams++;
                        }
                        else { skipDetails.Add($"Row {row}: Param '{paramName}' already exists."); skipParams++; }
                    }
                }

                await _db.SaveChangesAsync();
                return Ok(new
                {
                    message = "Import completed.",
                    Categories = new { Inserted = newCats, Skipped = skipCats },
                    Specifications = new { Inserted = newSpecs, Skipped = skipSpecs },
                    Parameters = new { Inserted = newParams, Skipped = skipParams },
                    skipDetails = skipDetails.Take(50)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        private static string? Col(ExcelWorksheet ws, int row, string header)
        {
            if (ws.Dimension == null) return null;
            for (int c = 1; c <= ws.Dimension.End.Column; c++)
                if (ws.Cells[1, c].Text.Trim().Equals(header, StringComparison.OrdinalIgnoreCase))
                    return ws.Cells[row, c].Text.Trim();
            return null;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // MODEL PARAMETER
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ModelParameterController : ControllerBase
    {
        private readonly IModelParameterService _service;
        private readonly DBContext _db;

       
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;

        public ModelParameterController(IModelParameterService service, DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _service = service;
            _db = db;
            _exportService = exportService;
            _importService = importService;
        }

        [HttpPost("create-modelparameter")]
        public async Task<IActionResult> CreateModelParameter([FromBody] ModelParameterCreateDto dto)
        {
            var result = await _service.CreateModelParameterAsync(dto);
            return Ok(result);
        }

        [HttpGet("model/{modelId:guid}")]
        public async Task<IActionResult> GetModelParameterByModel(Guid modelId) =>
            Ok(await _service.GetModelParameterByModelAsync(modelId));

        [HttpGet("get-parameter{id:guid}")]
        public async Task<IActionResult> GetModelParameterById(Guid id)
        {
            var result = await _service.GetModelParameterByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPut("update-parameter{id:guid}")]
        public async Task<IActionResult> UpdateModelParameter(Guid id, [FromBody] ModelParameterUpdateDto dto)
        {
            var result = await _service.UpdateModelParameterAsync(id, dto);
            return result == null ? NotFound() : Ok(result);
        }

        //[HttpDelete("soft-delete/{id:guid}")]
        //public async Task<IActionResult> Delete(Guid id)
        //{
        //    var result = await _service.DeleteAsync(id);
        //    return result ? Ok(new { message = "Deleted." }) : NotFound();
        //}

        // GET: api/ModelParameter/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "excel")
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            var data = await _service.GetAllModelParameterAsync();
            var file = await _exportService.ExportAsync(data, exportFormat, "ModelParameters");
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Import — Excel (.xlsx) OR CSV (.csv). Columns: ModelName, CategoryName, ParameterName,
        /// ParameterValue, Unit, Capacity, Processor, RAM, Storage, FirmwareVersion, Protocol,
        /// WarrantyDuration, EndOfLife, Intro, Uses, Feature. Unknown columns are saved to a
        /// downloadable .txt instead of being dropped (see response's UnmatchedFileUrl).
        /// POST /api/modelparameter/import-parameters
        /// </summary>
        [HttpPost("import-parameters")]
        public async Task<IActionResult> ImportModelParameters(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var knownColumns = new[]
            {
                "ModelName", "CategoryName", "ParameterName", "ParameterValue", "Unit", "Capacity",
                "Processor", "RAM", "Storage", "FirmwareVersion", "Protocol", "WarrantyDuration",
                "EndOfLife", "Intro", "Uses", "Feature"
            };

            var (sheet, headerToField, unmatchedColumns) =
                await _importService.ResolveMappingAsync(file, "ModelParameter", null, knownColumns);

            int imported = 0;
            var unmatchedRows = new List<DeviceManagementOnly.Common.ImportExport.UnmatchedDataWriter.UnmatchedRow>();

            for (int i = 0; i < sheet.Rows.Count; i++)
            {
                var raw = sheet.Rows[i];
                int rowNumber = i + 2;
                string? Get(string col) => raw.TryGetValue(col, out var v) ? v : null;

                var paramName = Get("ParameterName")?.Trim();
                if (string.IsNullOrWhiteSpace(paramName)) continue;

                var modelName = Get("ModelName")?.Trim();
                ModelSpecification? spec = null;
                if (!string.IsNullOrWhiteSpace(modelName))
                    spec = await _db.ModelSpecifications.FirstOrDefaultAsync(s => s.Name == modelName && !s.IsDeleted);
                if (spec == null) continue;

                var catName = Get("CategoryName")?.Trim();
                ModelCategory? cat = null;
                if (!string.IsNullOrWhiteSpace(catName))
                    cat = await _db.ModelCategories.FirstOrDefaultAsync(c => c.Name == catName && !c.IsDeleted);
                if (cat == null) continue;

                bool exists = await _db.ModelParameters.AnyAsync(p =>
                    p.ParameterName.ToLower() == paramName.ToLower() && p.DeviceModelId == spec.Id && !p.IsDeleted);

                if (!exists)
                {
                    _db.ModelParameters.Add(new ModelParameter
                    {
                        DeviceModelId = spec.Id,
                        ModelCategoryId = cat.Id,
                        ParameterName = paramName,
                        ParameterValue = Get("ParameterValue")?.Trim() ?? "N/A",
                        Unit = Get("Unit")?.Trim(),
                        Capacity = Get("Capacity")?.Trim(),
                        Processor = Get("Processor")?.Trim(),
                        RAM = Get("RAM")?.Trim(),
                        Storage = Get("Storage")?.Trim(),
                        FirmwareVersion = Get("FirmwareVersion")?.Trim(),
                        Protocol = Get("Protocol")?.Trim(),
                        WarrantyDuration = Get("WarrantyDuration")?.Trim(),
                        EndOfLife = Get("EndOfLife")?.Trim(),
                        Intro = Get("Intro")?.Trim(),
                        Uses = Get("Uses")?.Trim(),
                        Feature = Get("Feature")?.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "Import"
                    });
                    imported++;
                }

                if (unmatchedColumns.Count > 0)
                {
                    var extra = new Dictionary<string, string>();
                    foreach (var col in unmatchedColumns)
                        if (raw.TryGetValue(col, out var val) && !string.IsNullOrWhiteSpace(val)) extra[col] = val;
                    if (extra.Count > 0)
                        unmatchedRows.Add(new DeviceManagementOnly.Common.ImportExport.UnmatchedDataWriter.UnmatchedRow
                        { RowNumber = rowNumber, Values = extra });
                }
            }

            await _db.SaveChangesAsync();

            var result = new DeviceManagementOnly.Dtos.GenericImportResultDto
            {
                TotalRows = sheet.Rows.Count,
                Inserted = imported,
                UnmatchedColumns = unmatchedColumns,
                Message = $"{imported} parameter(s) imported successfully."
            };

            if (unmatchedRows.Count > 0)
            {
                result.HasUnmatchedData = true;
                result.UnmatchedFileUrl = await _importService.SaveUnmatchedFileAsync(
                    "ModelParameter", unmatchedColumns, unmatchedRows, file.FileName);
                result.Message += $" NOTE: {unmatchedColumns.Count} column(s) don't exist in our backend — saved to a .txt, see UnmatchedFileUrl.";
            }

            return Ok(result);
        }

        private static string? Col(ExcelWorksheet ws, int row, string header)
        {
            if (ws.Dimension == null) return null;
            for (int c = 1; c <= ws.Dimension.End.Column; c++)
                if (ws.Cells[1, c].Text.Trim().Equals(header, StringComparison.OrdinalIgnoreCase))
                    return ws.Cells[row, c].Text.Trim();
            return null;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // DEVICE MASTER
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceMasterController : ControllerBase
    {
        private readonly IDeviceMasterService _service;
        private readonly ILogger<DeviceMasterController> _logger;
        private readonly DBContext _db;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;

        public DeviceMasterController(IDeviceMasterService service, ILogger<DeviceMasterController> logger, DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _service = service;
            _logger = logger;
            _db = db;
            _exportService = exportService;
            _importService = importService;
        }

        [HttpPost("create-device")]
        public async Task<IActionResult> CreateDeviceMaster([FromBody] DeviceMasterCreateDto dto)
        {
            try
            {
                var result = await _service.CreateDeviceMasterAsync(dto);
                return CreatedAtAction(nameof(GetDeviceMasterById), new { id = result.Id }, result);
            }
            catch (Exception ex) { _logger.LogError(ex, "Create DeviceMaster"); return StatusCode(500, "Something went wrong"); }
        }

        [HttpGet("get-all-device")]
        public async Task<IActionResult> GetAllDeviceMaster([FromQuery] DeviceMasterFilterParams p)
        {
            try { return Ok(await _service.GetAllDeviceMasterAsync(p)); }
            catch (Exception ex) { _logger.LogError(ex, "GetAll DeviceMaster"); return StatusCode(500, "Something went wrong"); }
        }

        // GET: api/DeviceMaster/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] DeviceMasterFilterParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var result = await _service.GetAllDeviceMasterAsync(p);
            var file = await _exportService.ExportAsync(result.Data, exportFormat, "DeviceMasters");
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Import — Excel (.xlsx) OR CSV (.csv). NO GUIDs needed for DeviceType/Category/Model —
        /// identify via readable names:
        ///   DeviceTypeName, CategoryName, ModelName  (all optional, but at least one recommended)
        ///   CompanyId  — still a raw GUID (external reference from the Company microservice,
        ///                can't be looked up in this DB) — leave blank if not applicable
        ///   DeviceShortName, DeviceLongName, Remarks, DataFormat
        /// Unknown columns are saved to a downloadable .txt instead of being dropped
        /// (see response's UnmatchedFileUrl).
        /// POST /api/DeviceMaster/import
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var knownColumns = new[]
            {
                "DeviceTypeName", "CategoryName", "ModelName", "CompanyId",
                "DeviceShortName", "DeviceLongName", "Remarks", "DataFormat"
            };

            var (sheet, headerToField, unmatchedColumns) =
                await _importService.ResolveMappingAsync(file, "DeviceMaster", null, knownColumns);

            var typeByName = await _db.DeviceTypes
                .Where(t => !t.IsDeleted)
                .Select(t => new { t.Id, t.Name })
                .ToDictionaryAsync(t => t.Name.Trim().ToLower(), t => t.Id, StringComparer.OrdinalIgnoreCase);

            var categoryByName = await _db.DeviceCategories
                .Where(c => !c.IsDeleted)
                .Select(c => new { c.Id, c.Name })
                .ToDictionaryAsync(c => c.Name.Trim().ToLower(), c => c.Id, StringComparer.OrdinalIgnoreCase);

            var modelByName = await _db.ModelSpecifications
                .Where(m => !m.IsDeleted)
                .Select(m => new { m.Id, m.Name })
                .ToDictionaryAsync(m => m.Name.Trim().ToLower(), m => m.Id, StringComparer.OrdinalIgnoreCase);

            var result = new GenericImportResultDto { TotalRows = sheet.Rows.Count, UnmatchedColumns = unmatchedColumns };
            var unmatchedRows = new List<DeviceManagementOnly.Common.ImportExport.UnmatchedDataWriter.UnmatchedRow>();

            for (int i = 0; i < sheet.Rows.Count; i++)
            {
                var raw = sheet.Rows[i];
                int rowNumber = i + 2;
                string? Get(string col) => raw.TryGetValue(col, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

                try
                {
                    Guid? deviceTypeId = null;
                    var typeName = Get("DeviceTypeName");
                    if (!string.IsNullOrWhiteSpace(typeName))
                    {
                        if (!typeByName.TryGetValue(typeName.ToLower(), out var tid))
                            throw new InvalidOperationException($"DeviceType '{typeName}' not found.");
                        deviceTypeId = tid;
                    }

                    Guid? categoryId = null;
                    var categoryName = Get("CategoryName");
                    if (!string.IsNullOrWhiteSpace(categoryName))
                    {
                        if (!categoryByName.TryGetValue(categoryName.ToLower(), out var cid))
                            throw new InvalidOperationException($"DeviceCategory '{categoryName}' not found.");
                        categoryId = cid;
                    }

                    Guid? modelId = null;
                    var modelName = Get("ModelName");
                    if (!string.IsNullOrWhiteSpace(modelName))
                    {
                        if (!modelByName.TryGetValue(modelName.ToLower(), out var mid))
                            throw new InvalidOperationException($"ModelSpecification '{modelName}' not found.");
                        modelId = mid;
                    }

                    var companyIdText = Get("CompanyId");
                    Guid? companyId = Guid.TryParse(companyIdText, out var cid2) ? cid2 : null;
                    if (!string.IsNullOrWhiteSpace(companyIdText) && companyId == null)
                        throw new InvalidOperationException($"CompanyId '{companyIdText}' is not a valid GUID.");

                    var dto = new DeviceMasterCreateDto
                    {
                        DeviceTypeId = deviceTypeId,
                        DeviceCategoryId = categoryId,
                        ModelSpecificationId = modelId,
                        CompanyId = companyId,
                        DeviceShortName = Get("DeviceShortName"),
                        DeviceLongName = Get("DeviceLongName"),
                        Remarks = Get("Remarks"),
                        DataFormat = Get("DataFormat")
                    };

                    await _service.CreateDeviceMasterAsync(dto);
                    result.Inserted++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add(new ImportRowError { RowNumber = rowNumber, Message = ex.Message });
                }

                if (unmatchedColumns.Count > 0)
                {
                    var extra = new Dictionary<string, string>();
                    foreach (var col in unmatchedColumns)
                        if (raw.TryGetValue(col, out var val) && !string.IsNullOrWhiteSpace(val)) extra[col] = val;
                    if (extra.Count > 0)
                        unmatchedRows.Add(new DeviceManagementOnly.Common.ImportExport.UnmatchedDataWriter.UnmatchedRow
                        { RowNumber = rowNumber, Values = extra });
                }
            }

            result.Message = result.Failed == 0
                ? $"Import complete: {result.Inserted}/{result.TotalRows} row(s) saved."
                : $"Import finished with issues: {result.Inserted} saved, {result.Failed} failed out of {result.TotalRows}.";

            if (unmatchedRows.Count > 0)
            {
                result.HasUnmatchedData = true;
                result.UnmatchedFileUrl = await _importService.SaveUnmatchedFileAsync(
                    "DeviceMaster", unmatchedColumns, unmatchedRows, file.FileName);
                result.Message += $" NOTE: {unmatchedColumns.Count} column(s) don't exist in our backend — saved to a .txt, see UnmatchedFileUrl.";
            }

            return Ok(result);
        }

        [HttpGet("get-device/{id:guid}")]
        public async Task<IActionResult> GetDeviceMasterById(Guid id)
        {
            var data = await _service.GetDeviceMasterByIdAsync(id);
            return data == null ? NotFound($"DeviceMaster {id} not found") : Ok(data);
        }

        /// <summary>
        /// Get all DeviceMasters by external CompanyId (Guid from Company microservice)
        /// GET /api/devicemaster/company/{companyId}
        /// </summary>
        [HttpGet("company/{companyId:guid}")]
        public async Task<IActionResult> GetDeviceMasterByCompany(Guid companyId, [FromQuery] PaginationParams p)
        {
            try { return Ok(await _service.GetDeviceMasterByCompanyAsync(companyId, p)); }
            catch (Exception ex) { _logger.LogError(ex, "GetByCompany DeviceMaster"); return StatusCode(500, "Something went wrong"); }
        }

        [HttpGet("category/{categoryId:guid}")]
        public async Task<IActionResult> GetDeviceMasterByCategory(Guid categoryId, [FromQuery] PaginationParams p)
        {
            try { return Ok(await _service.GetDeviceMasterByCategoryAsync(categoryId, p)); }
            catch (Exception ex) { _logger.LogError(ex, "GetByCategory DeviceMaster"); return StatusCode(500, "Something went wrong"); }
        }

        [HttpPost("update-device/{id:guid}")]
        public async Task<IActionResult> UpdateDeviceMaster(Guid id, [FromBody] DeviceMasterUpdateDto dto)
        {
            var updated = await _service.UpdateDeviceMasterAsync(id, dto);
            return updated == null ? NotFound($"DeviceMaster {id} not found") : Ok(updated);
        }

        //[HttpDelete("soft-delete/{id:guid}")]
        //public async Task<IActionResult> Delete(Guid id)
        //{
        //    var deleted = await _service.DeleteAsync(id);
        //    return deleted ? Ok("Deleted successfully.") : NotFound($"DeviceMaster {id} not found");
        //}
    }

    // ══════════════════════════════════════════════════════════════
    // DEVICE DETAIL
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceDetailController : ControllerBase
    {
        private readonly IDeviceDetailService _service;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        public DeviceDetailController(IDeviceDetailService service, DeviceManagementOnly.Common.ImportExport.IExportService exportService)
        {
            _service = service;
            _exportService = exportService;
        }

        [HttpPost("create-devicedetail")]
        public async Task<IActionResult> CreateDeviceDetail([FromBody] DeviceDetailCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.CreateDeviceDetailAsync(dto);
                return CreatedAtAction(nameof(GetDeviceDetailById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("get-all-devicedetail")]
        public async Task<IActionResult> GetAllDeviceDetail([FromQuery] DeviceDetailFilterParams p) =>
            Ok(await _service.GetAllDeviceDetailAsync(p));

        [HttpGet("by-master/{deviceMasterId:guid}")]
        public async Task<IActionResult> GetDeviceDetailByMaster(Guid deviceMasterId, [FromQuery] PaginationParams p) =>
            Ok(await _service.GetDeviceDetailByMasterAsync(deviceMasterId, p));

        [HttpGet("by-category/{categoryId:guid}")]
        public async Task<IActionResult> GetDeviceByCategory(Guid categoryId, [FromQuery] PaginationParams p) =>
            Ok(await _service.GetDeviceDetailByCategoryAsync(categoryId, p));

        [HttpGet("by-type/{deviceTypeId:guid}")]
        public async Task<IActionResult> GetDeviceDetailByType(Guid deviceTypeId, [FromQuery] PaginationParams p) =>
            Ok(await _service.GetDeviceDetailByTypeAsync(deviceTypeId, p));

        [HttpGet("by-company/{companyId:guid}")]
        public async Task<IActionResult> GetDeviceDetailByCompany(Guid companyId, [FromQuery] PaginationParams p) =>
            Ok(await _service.GetDeviceDetailByCompanyAsync(companyId, p));

        [HttpGet("get-devicedetail{id:guid}")]
        public async Task<IActionResult> GetDeviceDetailById(Guid id)
        {
            var result = await _service.GetDeviceDetailByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("update-devicedetail{id:guid}")]
        public async Task<IActionResult> UpdateDeviceDetail(Guid id, [FromBody] DeviceDetailUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.UpdateDeviceDetailAsync(id, dto);
            return result is null ? NotFound() : Ok(result);
        }

        //[HttpDelete("soft-delete/{id:guid}")]
        //public async Task<IActionResult> Delete(Guid id)
        //{
        //    var deleted = await _service.DeleteAsync(id);
        //    return deleted ? NoContent() : NotFound();
        //}

        /// <summary>
        /// Bulk Excel import for physical devices.
        /// POST /api/devicedetail/import
        /// Columns: CategoryName, DeviceTypeName, ModelName, ModelNumber, DeviceShortName,
        ///          DeviceLongName, IMEI, MACAddress, TagNumber, SerialNumber, PurchaseDate,
        ///          WarrantyExpiry, PurchaseCost, IPAddress, SIMNumber, Remarks
        /// </summary>
        /// <summary>
        /// Bulk import — accepts BOTH .xlsx and .csv. Column headers are matched
        /// against our backend fields automatically (or via a mapping saved earlier
        /// through /api/FieldMapping/save). Any column we don't have a field for is
        /// not dropped — check the response's HasUnmatchedData / UnmatchedFileUrl.
        /// </summary>
        [HttpPost("import-devicedetail")]
        public async Task<IActionResult> ImportExcelDeviceDetail([FromForm] DeviceDetailImportDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var result = await _service.ImportDeviceDetailExcelAsync(dto.File);
            return Ok(result);
        }

        /// <summary>
        /// GET api/DeviceDetail/export-devicedetail?format=excel|csv|pdf
        /// Exports the full device-detail list in the requested format.
        /// </summary>
        [HttpGet("export-devicedetail")]
        public async Task<IActionResult> ExportDeviceDetail([FromQuery] string format = "excel", [FromQuery] DeviceDetailFilterParams? p = null)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            var filter = p ?? new DeviceDetailFilterParams();
            filter.Page = null; filter.PageSize = null; // export = full list, never paginated
            var data = await _service.GetAllDeviceDetailAsync(filter);
            var file = await _exportService.ExportAsync(data.Data, exportFormat, "DeviceDetails");
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpGet("generate-qr/{deviceDetailId:guid}")]
        public async Task<IActionResult> GenerateQR(Guid deviceDetailId)
        {
            try
            {
                var qrBase64 = await _service.GenerateQRCodeForDeviceAsync(deviceDetailId);
                return Ok(new { message = "QR Code Generated Successfully", QRCodeImage = qrBase64 });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("setup-tracking/{deviceDetailId:guid}")]
        public async Task<IActionResult> SetupTracking(Guid deviceDetailId, [FromBody] DeviceTrackingInputDto dto)
        {
            try
            {
                var result = await _service.SetupDeviceTrackingAsync(deviceDetailId, dto);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }
    }

    // ══════════════════════════════════════════════════════════════
    // DEVICE ASSOCIATION (Sub-Device)
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceAssociationController : ControllerBase
    {
        private readonly IDeviceAssociationService _svc;
        public DeviceAssociationController(IDeviceAssociationService svc) => _svc = svc;

        [HttpGet("available/{parentId:guid}")]
        public async Task<IActionResult> GetAvailableDevices(Guid parentId)
        {
            var result = await _svc.GetAvailableDevicesAsync(parentId);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("{parentId:guid}/associate")]
        public async Task<IActionResult> AssociateDevices(Guid parentId, [FromBody] AssociateDeviceDto dto)
        {
            if (dto.ChildDeviceDetailIds == null || !dto.ChildDeviceDetailIds.Any())
                return BadRequest(new { success = false, message = "ChildDeviceDetailIds should not be empty." });

            var results = await _svc.AssociateDevicesAsync(parentId, dto.ChildDeviceDetailIds);
            return Ok(new
            {
                success = true,
                message = $"{results.Count(r => r.Success)} associated, {results.Count(r => !r.Success)} failed.",
                data = results
            });
        }

        [HttpPatch("{childId:guid}/unassociate")]
        public async Task<IActionResult> UnassociateDevices(Guid childId)
        {
            try
            {
                var result = await _svc.UnassociateDevicesAsync(childId);
                return Ok(new { success = true, message = $"Device {childId} unassociated.", data = result });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        }

        [HttpGet("{parentId:guid}/subdevices")]
        public async Task<IActionResult> GetSubDevices(Guid parentId)
        {
            var result = await _svc.GetSubDevicesAsync(parentId);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("{deviceDetailId:guid}/tree")]
        public async Task<IActionResult> GetDeviceTree(Guid deviceDetailId)
        {
            var result = await _svc.GetWithSubDevicesAsync(deviceDetailId);
            return result is null
                ? NotFound(new { success = false, message = "Device not found." })
                : Ok(new { success = true, data = result });
        }
    }

    // ══════════════════════════════════════════════════════════════
    // DEVICE MODEL MAPPING
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceModelController : ControllerBase
    {
        private readonly IDeviceModelService _svc;
        private readonly ICurrentCompanyService _currentCompanyService;
        private readonly DBContext _db;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;

        public DeviceModelController(IDeviceModelService svc, ICurrentCompanyService currentCompanyService, DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _svc = svc;
            _currentCompanyService = currentCompanyService;
            _db = db;
            _exportService = exportService;
            _importService = importService;
        }

        [HttpPost("assign/{deviceTypeId:guid}")]
        public async Task<IActionResult> AssignDeviceModels(Guid deviceTypeId, [FromBody] List<Guid> modelSpecificationIds)
        {
            if (modelSpecificationIds == null || !modelSpecificationIds.Any())
                return BadRequest(new { message = "modelSpecificationIds cannot be empty." });

            var result = await _svc.AssignModelsToDeviceAsync(deviceTypeId, modelSpecificationIds);
            return Ok(result);
        }

        [HttpGet("device-model-details")]
        public async Task<IActionResult> GetDeviceModelDetails()
        {
            var details = await _svc.GetDeviceModelMappingsAsync();
            return Ok(details);
        }

        // GET: api/DeviceModel/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "excel")
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            var data = await _svc.GetDeviceModelMappingsAsync();
            var file = await _exportService.ExportAsync(data, exportFormat, "DeviceModelMappings");
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Import — Excel (.xlsx) OR CSV (.csv). Columns: DeviceTypeName, ModelName
        /// (name-based, no need to know GUIDs). Each row assigns one model-specification
        /// to one device-type. Unknown columns are saved to a downloadable .txt instead
        /// of being dropped — see response's UnmatchedFileUrl.
        /// POST /api/DeviceModel/import
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var knownColumns = new[] { "DeviceTypeName", "ModelName" };
            var (sheet, headerToField, unmatchedColumns) =
                await _importService.ResolveMappingAsync(file, "DeviceModel", null, knownColumns);

            // group rows by DeviceTypeName so we can call AssignModelsToDeviceAsync once per device type
            var grouped = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var unmatchedRows = new List<DeviceManagementOnly.Common.ImportExport.UnmatchedDataWriter.UnmatchedRow>();
            int totalRows = sheet.Rows.Count;

            for (int i = 0; i < sheet.Rows.Count; i++)
            {
                var raw = sheet.Rows[i];
                int rowNumber = i + 2;
                var deviceTypeName = raw.TryGetValue("DeviceTypeName", out var dt) ? dt?.Trim() : null;
                var modelName = raw.TryGetValue("ModelName", out var mn) ? mn?.Trim() : null;

                if (!string.IsNullOrWhiteSpace(deviceTypeName) && !string.IsNullOrWhiteSpace(modelName))
                {
                    if (!grouped.TryGetValue(deviceTypeName, out var list))
                        grouped[deviceTypeName] = list = new List<string>();
                    list.Add(modelName);
                }

                if (unmatchedColumns.Count > 0)
                {
                    var extra = new Dictionary<string, string>();
                    foreach (var col in unmatchedColumns)
                        if (raw.TryGetValue(col, out var val) && !string.IsNullOrWhiteSpace(val)) extra[col] = val;
                    if (extra.Count > 0)
                        unmatchedRows.Add(new DeviceManagementOnly.Common.ImportExport.UnmatchedDataWriter.UnmatchedRow
                        { RowNumber = rowNumber, Values = extra });
                }
            }

            int assigned = 0, failed = 0;
            var errors = new List<string>();

            foreach (var (deviceTypeName, modelNames) in grouped)
            {
                var deviceType = await _db.DeviceTypes
                    .Where(d => d.Name.ToLower() == deviceTypeName.ToLower() && !d.IsDeleted)
                    .Select(d => (Guid?)d.Id)
                    .FirstOrDefaultAsync();

                if (deviceType == null) { failed++; errors.Add($"DeviceType '{deviceTypeName}' not found."); continue; }

                var modelIds = await _db.ModelSpecifications
                    .Where(s => modelNames.Select(n => n.ToLower()).Contains(s.Name.ToLower()) && !s.IsDeleted)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (modelIds.Count == 0) { failed++; errors.Add($"No matching models found for '{deviceTypeName}'."); continue; }

                var result = await _svc.AssignModelsToDeviceAsync(deviceType.Value, modelIds);
                assigned += result.SkippedOrInvalidIds == null ? modelIds.Count : modelIds.Count - result.SkippedOrInvalidIds.Count;
            }

            var response = new DeviceManagementOnly.Dtos.GenericImportResultDto
            {
                TotalRows = totalRows,
                Inserted = assigned,
                Failed = failed,
                UnmatchedColumns = unmatchedColumns,
                Message = $"{assigned} model assignment(s) processed across {grouped.Count} device type(s)."
            };
            foreach (var e in errors) response.Errors.Add(new DeviceManagementOnly.Dtos.ImportRowError { RowNumber = 0, Message = e });

            if (unmatchedRows.Count > 0)
            {
                response.HasUnmatchedData = true;
                response.UnmatchedFileUrl = await _importService.SaveUnmatchedFileAsync(
                    "DeviceModel", unmatchedColumns, unmatchedRows, file.FileName);
                response.Message += $" NOTE: {unmatchedColumns.Count} column(s) don't exist in our backend — saved to a .txt, see UnmatchedFileUrl.";
            }

            return Ok(response);
        }

        [HttpGet("models-by-devicetype/{deviceTypeId:guid}")]
        public async Task<IActionResult> GetModelsByDeviceType(Guid deviceTypeId)
        {
            var result = await _svc.GetModelsByDeviceTypeIdAsync(deviceTypeId);
            if (result == null || !result.Any())
                return NotFound(new { message = $"No models found for DeviceType {deviceTypeId}." });
            return Ok(result);
        }

        [HttpGet("by-company/{companyId:guid}")]
        public async Task<IActionResult> GetModelDevicesByCompany(Guid companyId)
        {
            var result = await _svc.GetDevicesByCompanyIdAsync(companyId);
            if (result == null || !result.Any())
                return NotFound(new { message = $"No devices found for Company {companyId}." });
            return Ok(result);
        }

        [HttpGet("by-category/{categoryId:guid}")]
        public async Task<IActionResult> GetModelDevicesByCategory(Guid categoryId)
        {
            var result = await _svc.GetDevicesByCategoryIdAsync(categoryId);
            if (result == null || !result.Any())
                return NotFound(new { message = $"No devices found for Category {categoryId}." });
            return Ok(result);
        }

        [HttpGet("my-devices")]
        public async Task<IActionResult> GetMyCompanyDevices()
        {
            var companyId = _currentCompanyService.GetCurrentCompanyId();
            if (companyId == null)
                return BadRequest(new
                {
                    status = false,
                    message = "CompanyId not found in token.Firstly login and attach token."
                });

            var result = await _svc.GetMyCompanyDevicesWithModelAsync(companyId.Value);

            return Ok(new
            {
                status = true,
                // companyId = companyId.Value,
                count = result.Count,
                data = result
            });
        }
    }

    // ══════════════════════════════════════════════════════════════
    // DEVICE BULK FILE IMPORT (Multi-sheet Excel → DeviceMaster)
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceFileController : ControllerBase
    {
        private readonly ILogger<DeviceFileController> _logger;
        private readonly DBContext _db;

        public DeviceFileController(ILogger<DeviceFileController> logger, DBContext db)
        {
            _logger = logger;
            _db = db;
        }

        public class ParsedRow
        {
            public int RowIndex { get; set; }
            public string SheetName { get; set; }
            public string OemName { get; set; }
            public string Category { get; set; }
            public string DeviceType { get; set; }
            public string ModelName { get; set; }
            public string ModelNumber { get; set; }
            public string DeviceShortName { get; set; }
            public string DeviceLongName { get; set; }
            public string IMEI { get; set; }
            public string MACAddress { get; set; }
            public string TagNumber { get; set; }
            public string SerialNumber { get; set; }
            public string PurchaseDate { get; set; }
            public string WarrantyExpiry { get; set; }
            public string PurchaseCost { get; set; }
            public string IPAddress { get; set; }
            public string SIMNumber { get; set; }
            public string Remarks { get; set; }
        }

        public sealed class RowResult
        {
            public int RowIndex { get; init; }
            public string SheetName { get; init; } = string.Empty;
            public string OemName { get; init; } = string.Empty;
            public string Category { get; init; } = string.Empty;
            public string ModelName { get; init; } = string.Empty;
            public string Status { get; init; } = string.Empty;
            public string? Reason { get; init; }
            public Guid? DeviceMasterId { get; init; }
        }

        public sealed class BulkImportResponse
        {
            public int Created { get; init; }
            public int Skipped { get; init; }
            public int Errors { get; init; }
            public string Message { get; init; } = string.Empty;
            public IEnumerable<RowResult> Rows { get; init; } = [];
        }

        /// <summary>
        /// Multi-sheet Excel bulk import → DeviceMaster.
        /// CompanyId = external Guid from Company microservice.
        /// POST /api/devicefile/import-bulk
        /// Supported column headers (any sheet): OEM/OEM->Name/Company/CompanyName,
        ///   Category/DeviceCategory, Model/Model->Name/ModelName,
        ///   ModelNumber, Protocol, FirmwareVersion, WarrantyDuration, EndOfLife
        /// </summary>
        [HttpPost("import-bulk")]
        public async Task<IActionResult> ImportBulkDevice(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls") return BadRequest("Only .xlsx / .xls files accepted.");

            List<ParsedRow> rows;
            try { rows = ParseAllSheets(file); }
            catch (Exception ex) { _logger.LogError(ex, "Parse failed"); return BadRequest($"Parse error: {ex.Message}"); }

            if (rows.Count == 0) return BadRequest("No data rows found.");

            var categoryCache = await _db.DeviceCategories
                .Where(c => !c.IsDeleted)
                .ToDictionaryAsync(c => c.Name.ToLower().Trim(), c => c.Id, StringComparer.OrdinalIgnoreCase);

            var modelCache = await _db.ModelSpecifications
                .Where(m => !m.IsDeleted)
                .ToDictionaryAsync(m => m.Name.ToLower().Trim(), m => m.Id, StringComparer.OrdinalIgnoreCase);

            var existingMasters = await _db.DeviceMasters
                .Where(d => !d.IsDeleted)
                .Select(d => new { d.DeviceCategoryId, d.ModelSpecificationId })
                .ToListAsync();

            // 🔧 FIX 1: null|null wali "fake duplicate" key ko masterSet me include hi mat karo
            var masterSet = existingMasters
                .Where(d => d.DeviceCategoryId != null || d.ModelSpecificationId != null)
                .Select(d => $"{d.DeviceCategoryId}|{d.ModelSpecificationId}")
                .ToHashSet();

            var results = new List<RowResult>();
            int created = 0, skipped = 0, errors = 0;

            foreach (var row in rows)
            {
                try
                {
                    Guid? categoryId = null;
                    if (!string.IsNullOrWhiteSpace(row.Category))
                        categoryId = await UpsertCategoryAsync(row.Category, categoryCache);

                    Guid? modelId = null;
                    if (!string.IsNullOrWhiteSpace(row.ModelName))
                        modelId = await UpsertModelSpecAsync(row, modelCache);

                    // 🔧 FIX 2: agar dono null hain toh yeh kabhi "duplicate" nahi maana jayega
                    bool isMeaningfulKey = categoryId != null || modelId != null;
                    var key = $"{categoryId}|{modelId}";

                    if (isMeaningfulKey && masterSet.Contains(key))
                    {
                        results.Add(new RowResult { RowIndex = row.RowIndex, SheetName = row.SheetName, OemName = row.OemName, Category = row.Category, ModelName = row.ModelName, Status = "skipped", Reason = "Duplicate DeviceMaster" });
                        skipped++; continue;
                    }

                    var dm = new DeviceMaster
                    {
                        DeviceCategoryId = categoryId,
                        ModelSpecificationId = modelId,
                        DeviceShortName = string.IsNullOrWhiteSpace(row.ModelName) ? row.OemName : row.ModelName,
                        DeviceLongName = string.Join(" | ", new[] { row.OemName, row.Category, row.ModelName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _db.DeviceMasters.Add(dm);
                    await _db.SaveChangesAsync();

                    if (isMeaningfulKey) masterSet.Add(key);

                    results.Add(new RowResult { RowIndex = row.RowIndex, SheetName = row.SheetName, OemName = row.OemName, Category = row.Category, ModelName = row.ModelName, Status = "created", DeviceMasterId = dm.Id });
                    created++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Row {Row} error", row.RowIndex);
                    results.Add(new RowResult { RowIndex = row.RowIndex, SheetName = row.SheetName, OemName = row.OemName, Category = row.Category, ModelName = row.ModelName, Status = "error", Reason = ex.Message });
                    errors++;
                }
            }

            return Ok(new BulkImportResponse { Created = created, Skipped = skipped, Errors = errors, Message = $"{created} created, {skipped} skipped, {errors} errors.", Rows = results });
        }

        private async Task<Guid> UpsertCategoryAsync(string name, Dictionary<string, Guid> cache)
        {
            var key = name.ToLower().Trim();
            if (cache.TryGetValue(key, out Guid id)) return id;
            var cat = new DeviceCategory { Name = name.Trim(), IsActive = true, CreatedAt = DateTime.UtcNow };
            _db.DeviceCategories.Add(cat); await _db.SaveChangesAsync();
            cache[key] = cat.Id; return cat.Id;
        }

        private async Task<Guid> UpsertModelSpecAsync(ParsedRow row, Dictionary<string, Guid> cache)
        {
            var key = row.ModelName.ToLower().Trim();
            if (cache.TryGetValue(key, out Guid id)) return id;
            var spec = new ModelSpecification { Name = row.ModelName.Trim(), ModelNumber = string.IsNullOrWhiteSpace(row.ModelNumber) ? null : row.ModelNumber.Trim(), CreatedAt = DateTime.UtcNow };
            _db.ModelSpecifications.Add(spec); await _db.SaveChangesAsync();
            cache[key] = spec.Id; return spec.Id;
        }

        private static List<ParsedRow> ParseAllSheets(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var pkg = new ExcelPackage(stream);
            var result = new List<ParsedRow>();

            foreach (var ws in pkg.Workbook.Worksheets)
            {
                if (ws.Dimension == null) continue;
                int cols = ws.Dimension.Columns, rows = ws.Dimension.Rows;
                var h = BuildHeaderMap(ws, cols);

                for (int row = 2; row <= rows; row++)
                {
                    if (IsEmptyRow(ws, row, cols)) continue;
                    result.Add(new ParsedRow
                    {
                        RowIndex = row,
                        SheetName = ws.Name,
                        OemName = First(ws, row, h, "CompanyName", "OEM", "OEM Name", "Company", "Company Name"),
                        Category = First(ws, row, h, "DeviceCategoryName", "Category", "DeviceCategory", "Device Category"),
                        DeviceType = First(ws, row, h, "DeviceTypeName", "DeviceType", "Device Type"),
                        ModelName = First(ws, row, h, "ModelSpecificationName", "Model", "Model Name", "ModelName", "ModelSpecification"),
                        ModelNumber = First(ws, row, h, "ModelNumber", "Model Number", "Model No"),
                        DeviceShortName = First(ws, row, h, "DeviceShortName", "Short Name"),
                        DeviceLongName = First(ws, row, h, "DeviceLongName", "Long Name"),
                        IMEI = First(ws, row, h, "IMEI"),
                        MACAddress = First(ws, row, h, "MACAddress", "MAC Address"),
                        TagNumber = First(ws, row, h, "TagNumber", "Tag Number"),
                        SerialNumber = First(ws, row, h, "SerialNumber", "Serial Number", "Serial No"),
                        PurchaseDate = First(ws, row, h, "PurchaseDate", "Purchase Date"),
                        WarrantyExpiry = First(ws, row, h, "WarrantyExpiry", "Warranty Expiry", "WarrantyDuration"),
                        PurchaseCost = First(ws, row, h, "PurchaseCost", "Purchase Cost"),
                        IPAddress = First(ws, row, h, "IPAddress", "IP Address"),
                        SIMNumber = First(ws, row, h, "SIMNumber", "SIM Number"),
                        Remarks = First(ws, row, h, "Remarks")
                    });
                }
            }
            return result;
        }
        private static string First(ExcelWorksheet ws, int row, Dictionary<string, int> h, params string[] aliases)
        {
            foreach (var a in aliases)
                if (h.TryGetValue(a, out int col)) { var v = ws.Cells[row, col].Text.Trim(); if (!string.IsNullOrWhiteSpace(v)) return v; }
            return string.Empty;
        }

        private static Dictionary<string, int> BuildHeaderMap(ExcelWorksheet ws, int cols)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= cols; c++) { var h = ws.Cells[1, c].Text.Trim(); if (!string.IsNullOrWhiteSpace(h)) map.TryAdd(h, c); }
            return map;
        }

        private static bool IsEmptyRow(ExcelWorksheet ws, int row, int cols)
        {
            for (int c = 1; c <= cols; c++) if (!string.IsNullOrWhiteSpace(ws.Cells[row, c].Text)) return false;
            return true;
        }
    }
    // ══════════════════════════════════════════════════════════════
    // UNIT MASTER
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UnitMasterController : ControllerBase
    {
        private readonly IUnitMasterService _service;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;
        public UnitMasterController(IUnitMasterService service,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _service = service;
            _exportService = exportService;
            _importService = importService;
        }

        [HttpPost("create-unit")]
        public async Task<IActionResult> CreateUnit([FromBody] UnitMasterCreateDto dto)
        {
            var result = await _service.CreateUnitMasterAsync(dto);
            return Ok(result);
        }

        [HttpGet("get-all-units")]
        public async Task<IActionResult> GetAllUnits([FromQuery] PaginationParams p) =>
            Ok(await _service.GetAllUnitMasterAsync(p));

        // GET: api/UnitMaster/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] PaginationParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var result = await _service.GetAllUnitMasterAsync(p);
            var file = await _exportService.ExportAsync(result.Data, exportFormat, "UnitMasters");
            return File(file.Content, file.ContentType, file.FileName);
        }

        // POST: api/UnitMaster/import — Excel/CSV, dynamic mapping, unmatched-.txt
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var result = await _importService.ImportAsync<UnitMasterCreateDto>(
                file, "UnitMaster", null,
                async (dto, rowNumber) => await _service.CreateUnitMasterAsync(dto));

            return Ok(result);
        }

        [HttpGet("get-unit{id:guid}")]
        public async Task<IActionResult> GetUnitById(Guid id)
        {
            var result = await _service.GetUnitMasterByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("update-unit/{id:guid}")]
        public async Task<IActionResult> UpdateUnit(Guid id, [FromBody] UnitMasterUpdateDto dto)
        {
            var result = await _service.UpdateUnitMasterAsync(id, dto);
            return result == null ? NotFound() : Ok(result);
        }

        //[HttpDelete("soft-delete/{id:guid}")]
        //public async Task<IActionResult> Delete(Guid id)
        //{
        //    var result = await _service.DeleteAsync(id);
        //    return result ? Ok(new { message = "Deleted." }) : NotFound();
        //}
    }
}