using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using DeviceManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Controllers
{
    // ══════════════════════════════════════════════════════════════
    //  RACK POWER CAPACITY
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RackPowerCapacityController : ControllerBase
    {
        private readonly IRackPowerCapacityService _service;
        private readonly DBContext _db;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;
        public RackPowerCapacityController(IRackPowerCapacityService service, DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _service = service;
            _db = db;
            _exportService = exportService;
            _importService = importService;
        }

        // POST: api/RackPowerCapacity/add-rack-power-capacity
        [HttpPost("add-rack-power-capacity")]
        public async Task<IActionResult> Create([FromBody] RackPowerCapacityCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var createdBy = User.Identity?.Name;
                var result = await _service.CreateAsync(dto, createdBy);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // GET: api/RackPowerCapacity/get-all-rack-power-capacities?RackId=...
        [HttpGet("get-all-rack-power-capacities")]
        public async Task<IActionResult> GetAll([FromQuery] RackPowerCapacityFilterParams p)
        {
            var result = await _service.GetAllAsync(p);
            return Ok(result);
        }

        // GET: api/RackPowerCapacity/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] RackPowerCapacityFilterParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var result = await _service.GetAllAsync(p);
            var file = await _exportService.ExportAsync(result.Data, exportFormat, "RackPowerCapacities");
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Import — Excel (.xlsx) OR CSV (.csv). NO GUID needed — identify the rack via:
        ///   RackName * + RoomName (RoomName disambiguates if RackName repeats across rooms)
        ///   PDUName, PhaseName, RatedCapacityKW, EstimatedLoadKW, MeasuredLoadKW, FailoverReservedKW
        /// Unknown columns are saved to a downloadable .txt instead of being dropped
        /// (see response's UnmatchedFileUrl).
        /// POST /api/RackPowerCapacity/import
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var createdBy = User.Identity?.Name;

            var knownColumns = new[]
            {
                "RackName", "RoomName", "PDUName", "PhaseName",
                "RatedCapacityKW", "EstimatedLoadKW", "MeasuredLoadKW", "FailoverReservedKW"
            };

            var (sheet, headerToField, unmatchedColumns) =
                await _importService.ResolveMappingAsync(file, "RackPowerCapacity", null, knownColumns);

            var racks = await _db.Racks
                .Include(r => r.Room)
                .Select(r => new { r.Id, r.RackName, RoomName = r.Room != null ? r.Room.Name : null })
                .ToListAsync();
            var rackByNameAndRoom = racks
                .Where(r => r.RoomName != null)
                .ToDictionary(r => $"{r.RackName.Trim().ToLower()}|{r.RoomName!.Trim().ToLower()}", r => r.Id, StringComparer.OrdinalIgnoreCase);
            var rackByNameOnly = racks
                .GroupBy(r => r.RackName.Trim().ToLower(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var result = new GenericImportResultDto { TotalRows = sheet.Rows.Count, UnmatchedColumns = unmatchedColumns };
            var unmatchedRows = new List<DeviceManagementOnly.Common.ImportExport.UnmatchedDataWriter.UnmatchedRow>();

            for (int i = 0; i < sheet.Rows.Count; i++)
            {
                var raw = sheet.Rows[i];
                int rowNumber = i + 2;
                string? Get(string col) => raw.TryGetValue(col, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

                try
                {
                    var rackName = Get("RackName");
                    if (string.IsNullOrWhiteSpace(rackName))
                        throw new InvalidOperationException("RackName is required.");

                    Guid rackId;
                    var roomName = Get("RoomName");
                    if (!string.IsNullOrWhiteSpace(roomName) &&
                        rackByNameAndRoom.TryGetValue($"{rackName.ToLower()}|{roomName.ToLower()}", out var rid))
                    {
                        rackId = rid;
                    }
                    else if (rackByNameOnly.TryGetValue(rackName.ToLower(), out var rid2))
                    {
                        rackId = rid2;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Rack '{rackName}'{(roomName != null ? $" in room '{roomName}'" : "")} not found — " +
                            "if multiple racks share this name, also provide RoomName to disambiguate.");
                    }

                    var dto = new RackPowerCapacityCreateDto
                    {
                        RackId = rackId,
                        PDUName = Get("PDUName"),
                        PhaseName = Get("PhaseName"),
                        RatedCapacityKW = decimal.TryParse(Get("RatedCapacityKW"), out var rc) ? rc : null,
                        EstimatedLoadKW = decimal.TryParse(Get("EstimatedLoadKW"), out var el) ? el : null,
                        MeasuredLoadKW = decimal.TryParse(Get("MeasuredLoadKW"), out var ml) ? ml : null,
                        FailoverReservedKW = decimal.TryParse(Get("FailoverReservedKW"), out var fr) ? fr : null
                    };

                    await _service.CreateAsync(dto, createdBy);
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
                    "RackPowerCapacity", unmatchedColumns, unmatchedRows, file.FileName);
                result.Message += $" NOTE: {unmatchedColumns.Count} column(s) don't exist in our backend — saved to a .txt, see UnmatchedFileUrl.";
            }

            return Ok(result);
        }

        // GET: api/RackPowerCapacity/{id}/get-rack-power-capacity
        [HttpGet("{id:guid}/get-rack-power-capacity")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { status = false, message = $"RackPowerCapacity {id} not found." });
            return Ok(result);
        }

        // POST: api/RackPowerCapacity/{id}/update-rack-power-capacity
        [HttpPost("{id:guid}/update-rack-power-capacity")]
        public async Task<IActionResult> Update(Guid id, [FromBody] RackPowerCapacityUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updatedBy = User.Identity?.Name;
                var result = await _service.UpdateAsync(id, dto, updatedBy);
                if (result == null) return NotFound(new { status = false, message = $"RackPowerCapacity {id} not found." });
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // PATCH: api/RackPowerCapacity/{id}/set-active?isActive=false
        [HttpPatch("{id:guid}/set-active")]
        public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool isActive)
        {
            var updatedBy = User.Identity?.Name;
            var ok = await _service.SetActiveStatusAsync(id, isActive, updatedBy);
            if (!ok) return NotFound(new { status = false, message = $"RackPowerCapacity {id} not found." });
            return Ok(new { status = true, message = isActive ? "Activated." : "Deactivated." });
        }
    }
}