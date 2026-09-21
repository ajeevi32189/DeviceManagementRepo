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
    //  NETWORK CONNECTION
    //  Note: is table me IsActive nahi, sirf Status (Active/Inactive/
    //  Faulty) hai — isliye set-active ki jagah set-status endpoint hai.
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NetworkConnectionController : ControllerBase
    {
        private readonly INetworkConnectionService _service;
        private readonly DBContext _db;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;
        public NetworkConnectionController(INetworkConnectionService service, DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _service = service;
            _db = db;
            _exportService = exportService;
            _importService = importService;
        }

        // POST: api/NetworkConnection/add-network-connection
        [HttpPost("add-network-connection")]
        public async Task<IActionResult> Create([FromBody] NetworkConnectionCreateDto dto)
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

        // GET: api/NetworkConnection/get-all-network-connections?SourceDeviceDetailId=...&TargetDeviceDetailId=...&DeviceDetailId=...&Status=...&CableType=...
        [HttpGet("get-all-network-connections")]
        public async Task<IActionResult> GetAll([FromQuery] NetworkConnectionFilterParams p)
        {
            var result = await _service.GetAllAsync(p);
            return Ok(result);
        }

        // GET: api/NetworkConnection/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] NetworkConnectionFilterParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var result = await _service.GetAllAsync(p);
            var file = await _exportService.ExportAsync(result.Data, exportFormat, "NetworkConnections");
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Import — Excel (.xlsx) OR CSV (.csv). NO GUIDs needed — identify both
        /// devices via SerialNumber:
        ///   SourceDeviceSerialNumber *, SourcePortName
        ///   TargetDeviceSerialNumber *, TargetPortName
        ///   CableType, CableColorCode, CableLabel, Status
        /// Unknown columns are saved to a downloadable .txt instead of being dropped
        /// (see response's UnmatchedFileUrl).
        /// POST /api/NetworkConnection/import
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var createdBy = User.Identity?.Name;

            var knownColumns = new[]
            {
                "SourceDeviceSerialNumber", "SourcePortName", "TargetDeviceSerialNumber", "TargetPortName",
                "CableType", "CableColorCode", "CableLabel", "Status"
            };

            var (sheet, headerToField, unmatchedColumns) =
                await _importService.ResolveMappingAsync(file, "NetworkConnection", null, knownColumns);

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
                    var sourceSerial = Get("SourceDeviceSerialNumber");
                    if (string.IsNullOrWhiteSpace(sourceSerial))
                        throw new InvalidOperationException("SourceDeviceSerialNumber is required.");
                    if (!deviceBySerial.TryGetValue(sourceSerial.ToLower(), out var sourceId))
                        throw new InvalidOperationException($"No device found with SerialNumber '{sourceSerial}' (source).");

                    var targetSerial = Get("TargetDeviceSerialNumber");
                    if (string.IsNullOrWhiteSpace(targetSerial))
                        throw new InvalidOperationException("TargetDeviceSerialNumber is required.");
                    if (!deviceBySerial.TryGetValue(targetSerial.ToLower(), out var targetId))
                        throw new InvalidOperationException($"No device found with SerialNumber '{targetSerial}' (target).");

                    var dto = new NetworkConnectionCreateDto
                    {
                        SourceDeviceDetailId = sourceId,
                        SourcePortName = Get("SourcePortName"),
                        TargetDeviceDetailId = targetId,
                        TargetPortName = Get("TargetPortName"),
                        CableType = Get("CableType"),
                        CableColorCode = Get("CableColorCode"),
                        CableLabel = Get("CableLabel"),
                        Status = Get("Status") ?? "Active"
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
                    "NetworkConnection", unmatchedColumns, unmatchedRows, file.FileName);
                result.Message += $" NOTE: {unmatchedColumns.Count} column(s) don't exist in our backend — saved to a .txt, see UnmatchedFileUrl.";
            }

            return Ok(result);
        }

        // GET: api/NetworkConnection/{id}/get-network-connection
        [HttpGet("{id:guid}/get-network-connection")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { status = false, message = $"NetworkConnection {id} not found." });
            return Ok(result);
        }

        // POST: api/NetworkConnection/{id}/update-network-connection
        [HttpPost("{id:guid}/update-network-connection")]
        public async Task<IActionResult> Update(Guid id, [FromBody] NetworkConnectionUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updatedBy = User.Identity?.Name;
                var result = await _service.UpdateAsync(id, dto, updatedBy);
                if (result == null) return NotFound(new { status = false, message = $"NetworkConnection {id} not found." });
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }
    }
}