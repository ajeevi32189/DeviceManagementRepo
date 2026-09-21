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
    //  RACK
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RackController : ControllerBase
    {
        private readonly IRackService _service;
        private readonly DBContext _db;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;
        public RackController(IRackService service, DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _service = service;
            _db = db;
            _exportService = exportService;
            _importService = importService;
        }

        // POST: api/Rack/add-rack
        [HttpPost("add-rack")]
        public async Task<IActionResult> Create([FromBody] RackCreateDto dto)
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

        // GET: api/Rack/get-all-racks?RoomId=...&FloorId=...&Status=...&Search=...
        [HttpGet("get-all-racks")]
        public async Task<IActionResult> GetAll([FromQuery] RackFilterParams p)
        {
            var result = await _service.GetAllAsync(p);
            return Ok(result);
        }

        // GET: api/Rack/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] RackFilterParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var result = await _service.GetAllAsync(p);
            var file = await _exportService.ExportAsync(result.Data, exportFormat, "Racks");
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Import — Excel (.xlsx) OR CSV (.csv). NO GUID needed — identify the room via:
        ///   RoomName * + FloorName (FloorName disambiguates if RoomName repeats across floors)
        ///   RackName *, TotalUHeight, Status, MaxWeightCapacityKg, PositionX, PositionY
        /// Unknown columns are saved to a downloadable .txt instead of being dropped
        /// (see response's UnmatchedFileUrl).
        /// POST /api/Rack/import
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var createdBy = User.Identity?.Name;

            var knownColumns = new[]
            {
                "RoomName", "FloorName", "RackName", "TotalUHeight", "Status",
                "MaxWeightCapacityKg", "PositionX", "PositionY"
            };

            var (sheet, headerToField, unmatchedColumns) =
                await _importService.ResolveMappingAsync(file, "Rack", null, knownColumns);

            var rooms = await _db.Rooms
                .Include(r => r.Floor)
                .Select(r => new { r.Id, r.Name, FloorName = r.Floor != null ? r.Floor.Name : null })
                .ToListAsync();
            var roomByNameAndFloor = rooms
                .Where(r => r.FloorName != null)
                .ToDictionary(r => $"{r.Name.Trim().ToLower()}|{r.FloorName!.Trim().ToLower()}", r => r.Id, StringComparer.OrdinalIgnoreCase);
            var roomByNameOnly = rooms
                .GroupBy(r => r.Name.Trim().ToLower(), StringComparer.OrdinalIgnoreCase)
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
                    var roomName = Get("RoomName");
                    if (string.IsNullOrWhiteSpace(roomName))
                        throw new InvalidOperationException("RoomName is required.");

                    Guid roomId;
                    var floorName = Get("FloorName");
                    if (!string.IsNullOrWhiteSpace(floorName) &&
                        roomByNameAndFloor.TryGetValue($"{roomName.ToLower()}|{floorName.ToLower()}", out var rid))
                    {
                        roomId = rid;
                    }
                    else if (roomByNameOnly.TryGetValue(roomName.ToLower(), out var rid2))
                    {
                        roomId = rid2;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Room '{roomName}'{(floorName != null ? $" on floor '{floorName}'" : "")} not found — " +
                            "if multiple rooms share this name, also provide FloorName to disambiguate.");
                    }

                    var rackName = Get("RackName");
                    if (string.IsNullOrWhiteSpace(rackName))
                        throw new InvalidOperationException("RackName is required.");

                    var dto = new RackCreateDto
                    {
                        RoomId = roomId,
                        RackName = rackName,
                        TotalUHeight = int.TryParse(Get("TotalUHeight"), out var uh) ? uh : 42,
                        Status = Get("Status") ?? "Empty",
                        MaxWeightCapacityKg = decimal.TryParse(Get("MaxWeightCapacityKg"), out var wt) ? wt : null,
                        PositionX = decimal.TryParse(Get("PositionX"), out var px) ? px : null,
                        PositionY = decimal.TryParse(Get("PositionY"), out var py) ? py : null
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
                    "Rack", unmatchedColumns, unmatchedRows, file.FileName);
                result.Message += $" NOTE: {unmatchedColumns.Count} column(s) don't exist in our backend — saved to a .txt, see UnmatchedFileUrl.";
            }

            return Ok(result);
        }

        // GET: api/Rack/{id}/get-rack
        [HttpGet("{id:guid}/get-rack")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { status = false, message = $"Rack {id} not found." });
            return Ok(result);
        }

        // POST: api/Rack/{id}/update-rack
        [HttpPost("{id:guid}/update-rack")]
        public async Task<IActionResult> Update(Guid id, [FromBody] RackUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updatedBy = User.Identity?.Name;
                var result = await _service.UpdateAsync(id, dto, updatedBy);
                if (result == null) return NotFound(new { status = false, message = $"Rack {id} not found." });
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // PATCH: api/Rack/{id}/set-active?isActive=false
        [HttpPatch("{id:guid}/set-active")]
        public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool isActive)
        {
            var updatedBy = User.Identity?.Name;
            var ok = await _service.SetActiveStatusAsync(id, isActive, updatedBy);
            if (!ok) return NotFound(new { status = false, message = $"Rack {id} not found." });
            return Ok(new { status = true, message = isActive ? "Activated." : "Deactivated." });
        }
    }
}