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
    //  DEVICE RACK LOCATION
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceRackLocationController : ControllerBase
    {
        private readonly IDeviceRackLocationService _service;
        private readonly DBContext _db;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;
        public DeviceRackLocationController(IDeviceRackLocationService service, DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _service = service;
            _db = db;
            _exportService = exportService;
            _importService = importService;
        }

        // POST: api/DeviceRackLocation/add-device-rack-location
        [HttpPost("add-device-rack-location")]
        public async Task<IActionResult> Create([FromBody] DeviceRackLocationCreateDto dto)
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

        // GET: api/DeviceRackLocation/get-all-device-rack-locations?DeviceDetailId=...&RackId=...&Country=...&State=...&City=...
        [HttpGet("get-all-device-rack-locations")]
        public async Task<IActionResult> GetAll([FromQuery] DeviceRackLocationFilterParams p)
        {
            var result = await _service.GetAllAsync(p);
            return Ok(result);
        }

        // GET: api/DeviceRackLocation/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] DeviceRackLocationFilterParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var result = await _service.GetAllAsync(p);
            var file = await _exportService.ExportAsync(result.Data, exportFormat, "DeviceRackLocations");
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Import — Excel (.xlsx) OR CSV (.csv). NO GUIDs needed — identify records
        /// by human-readable columns:
        ///   DeviceSerialNumber *  → matched against DeviceDetail.SerialNumber
        ///   RackName + RoomName   → matched against Rack.RackName + its Room.Name (optional)
        ///   StartUPosition, UOccupied, Address, Country, State, City, PinCode, Latitude, Longitude
        /// Unknown columns are saved to a downloadable .txt instead of being dropped
        /// (see response's UnmatchedFileUrl).
        /// POST /api/DeviceRackLocation/import
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var createdBy = User.Identity?.Name;

            var knownColumns = new[]
            {
                "DeviceSerialNumber", "RackName", "RoomName", "StartUPosition", "UOccupied",
                "Address", "Country", "State", "City", "PinCode", "Latitude", "Longitude"
            };

            var (sheet, headerToField, unmatchedColumns) =
                await _importService.ResolveMappingAsync(file, "DeviceRackLocation", null, knownColumns);

            // ── Pehle saare DeviceDetail (SerialNumber) aur Rack (RackName+RoomName)
            //    ek hi query se memory me le lo — loop ke andar koi DB call nahi ──
            var deviceBySerial = await _db.DeviceDetails
                .Where(d => d.SerialNumber != null)
                .Select(d => new { d.Id, d.SerialNumber })
                .ToDictionaryAsync(d => d.SerialNumber!.Trim().ToLower(), d => d.Id, StringComparer.OrdinalIgnoreCase);

            var rackByNameAndRoom = await _db.Racks
                .Include(r => r.Room)
                .Select(r => new { r.Id, r.RackName, RoomName = r.Room != null ? r.Room.Name : null })
                .ToListAsync();
            var rackLookup = rackByNameAndRoom
                .Where(r => r.RoomName != null)
                .ToDictionary(r => $"{r.RackName.Trim().ToLower()}|{r.RoomName!.Trim().ToLower()}", r => r.Id, StringComparer.OrdinalIgnoreCase);
            // fallback: rack name alone (agar RoomName excel me nahi diya, aur naam globally unique hai)
            var rackByNameOnly = rackByNameAndRoom
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
                    var serial = Get("DeviceSerialNumber");
                    if (string.IsNullOrWhiteSpace(serial))
                        throw new InvalidOperationException("DeviceSerialNumber is required.");

                    if (!deviceBySerial.TryGetValue(serial.ToLower(), out var deviceDetailId))
                        throw new InvalidOperationException($"No device found with SerialNumber '{serial}'.");

                    Guid? rackId = null;
                    var rackName = Get("RackName");
                    if (!string.IsNullOrWhiteSpace(rackName))
                    {
                        var roomName = Get("RoomName");
                        if (!string.IsNullOrWhiteSpace(roomName) &&
                            rackLookup.TryGetValue($"{rackName.ToLower()}|{roomName.ToLower()}", out var rid))
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
                    }

                    var dto = new DeviceRackLocationCreateDto
                    {
                        DeviceDetailId = deviceDetailId,
                        RackId = rackId,
                        StartUPosition = int.TryParse(Get("StartUPosition"), out var su) ? su : null,
                        UOccupied = int.TryParse(Get("UOccupied"), out var uo) ? uo : null,
                        Address = Get("Address"),
                        Country = Get("Country"),
                        State = Get("State"),
                        City = Get("City"),
                        PinCode = Get("PinCode"),
                        Latitude = decimal.TryParse(Get("Latitude"), out var lat) ? lat : null,
                        Longitude = decimal.TryParse(Get("Longitude"), out var lng) ? lng : null
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
                    "DeviceRackLocation", unmatchedColumns, unmatchedRows, file.FileName);
                result.Message += $" NOTE: {unmatchedColumns.Count} column(s) don't exist in our backend — saved to a .txt, see UnmatchedFileUrl.";
            }

            return Ok(result);
        }

        // GET: api/DeviceRackLocation/{id}/get-device-rack-location
        [HttpGet("{id:guid}/get-device-rack-location")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { status = false, message = $"DeviceRackLocation {id} not found." });
            return Ok(result);
        }

        // POST: api/DeviceRackLocation/{id}/update-device-rack-location
        [HttpPost("{id:guid}/update-device-rack-location")]
        public async Task<IActionResult> Update(Guid id, [FromBody] DeviceRackLocationUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updatedBy = User.Identity?.Name;
                var result = await _service.UpdateAsync(id, dto, updatedBy);
                if (result == null) return NotFound(new { status = false, message = $"DeviceRackLocation {id} not found." });
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // PATCH: api/DeviceRackLocation/{id}/set-active?isActive=false
        [HttpPatch("{id:guid}/set-active")]
        public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool isActive)
        {
            var updatedBy = User.Identity?.Name;
            var ok = await _service.SetActiveStatusAsync(id, isActive, updatedBy);
            if (!ok) return NotFound(new { status = false, message = $"DeviceRackLocation {id} not found." });
            return Ok(new { status = true, message = isActive ? "Activated." : "Deactivated." });
        }
    }
}