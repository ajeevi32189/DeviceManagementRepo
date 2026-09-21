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
    //  DEVICE CONNECTION CONFIG
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceConnectionConfigController : ControllerBase
    {
        private readonly IDeviceConnectionConfigService _service;
        private readonly DBContext _db;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;
        public DeviceConnectionConfigController(IDeviceConnectionConfigService service, DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _service = service;
            _db = db;
            _exportService = exportService;
            _importService = importService;
        }

        // POST: api/DeviceConnectionConfig/add-device-connection-config
        [HttpPost("add-device-connection-config")]
        public async Task<IActionResult> Create([FromBody] DeviceConnectionConfigCreateDto dto)
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

        // GET: api/DeviceConnectionConfig/get-all-device-connection-configs?DeviceDetailId=...&Protocol=...&IsActive=...
        [HttpGet("get-all-device-connection-configs")]
        public async Task<IActionResult> GetAll([FromQuery] DeviceConnectionConfigFilterParams p)
        {
            var result = await _service.GetAllAsync(p);
            return Ok(result);
        }

        // GET: api/DeviceConnectionConfig/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] DeviceConnectionConfigFilterParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var result = await _service.GetAllAsync(p);
            var file = await _exportService.ExportAsync(result.Data, exportFormat, "DeviceConnectionConfigs");
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Import — Excel (.xlsx) OR CSV (.csv). NO GUID needed — identify the device via:
        ///   DeviceSerialNumber *  → matched against DeviceDetail.SerialNumber
        ///   Protocol *, IPAddress, Port, ObjectId, UnitId, PollingIntervalSeconds, ThingsBoardDeviceId
        /// Unknown columns are saved to a downloadable .txt instead of being dropped
        /// (see response's UnmatchedFileUrl).
        /// POST /api/DeviceConnectionConfig/import
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var createdBy = User.Identity?.Name;

            var knownColumns = new[]
            {
                "DeviceSerialNumber", "Protocol", "IPAddress", "Port", "ObjectId",
                "UnitId", "PollingIntervalSeconds", "ThingsBoardDeviceId"
            };

            var (sheet, headerToField, unmatchedColumns) =
                await _importService.ResolveMappingAsync(file, "DeviceConnectionConfig", null, knownColumns);

            var deviceBySerial = await _db.DeviceDetails
                .Where(d => d.SerialNumber != null)
                .Select(d => new { d.Id, d.SerialNumber })
                .ToDictionaryAsync(d => d.SerialNumber!.Trim().ToLower(), d => d.Id, StringComparer.OrdinalIgnoreCase);

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

                    var protocol = Get("Protocol");
                    if (string.IsNullOrWhiteSpace(protocol))
                        throw new InvalidOperationException("Protocol is required.");

                    var dto = new DeviceConnectionConfigCreateDto
                    {
                        DeviceDetailId = deviceDetailId,
                        Protocol = protocol,
                        IPAddress = Get("IPAddress"),
                        Port = int.TryParse(Get("Port"), out var port) ? port : null,
                        ObjectId = Get("ObjectId"),
                        UnitId = Get("UnitId"),
                        PollingIntervalSeconds = int.TryParse(Get("PollingIntervalSeconds"), out var poll) ? poll : 60,
                        ThingsBoardDeviceId = Get("ThingsBoardDeviceId")
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
                    "DeviceConnectionConfig", unmatchedColumns, unmatchedRows, file.FileName);
                result.Message += $" NOTE: {unmatchedColumns.Count} column(s) don't exist in our backend — saved to a .txt, see UnmatchedFileUrl.";
            }

            return Ok(result);
        }

        // GET: api/DeviceConnectionConfig/{id}/get-device-connection-config
        [HttpGet("{id:guid}/get-device-connection-config")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { status = false, message = $"DeviceConnectionConfig {id} not found." });
            return Ok(result);
        }

        // POST: api/DeviceConnectionConfig/{id}/update-device-connection-config
        [HttpPost("{id:guid}/update-device-connection-config")]
        public async Task<IActionResult> Update(Guid id, [FromBody] DeviceConnectionConfigUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updatedBy = User.Identity?.Name;
                var result = await _service.UpdateAsync(id, dto, updatedBy);
                if (result == null) return NotFound(new { status = false, message = $"DeviceConnectionConfig {id} not found." });
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // PATCH: api/DeviceConnectionConfig/{id}/set-active?isActive=false
        [HttpPatch("{id:guid}/set-active")]
        public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool isActive)
        {
            var updatedBy = User.Identity?.Name;
            var ok = await _service.SetActiveStatusAsync(id, isActive, updatedBy);
            if (!ok) return NotFound(new { status = false, message = $"DeviceConnectionConfig {id} not found." });
            return Ok(new { status = true, message = isActive ? "Activated." : "Deactivated." });
        }
    }
}