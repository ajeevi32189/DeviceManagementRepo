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
    //  ROOM
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _service;
        private readonly DBContext _db;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;

        public RoomController(IRoomService service, DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _service = service;
            _db = db;
            _exportService = exportService;
            _importService = importService;
        }

        // POST: api/Room/add-room
        [HttpPost("add-room")]
        public async Task<IActionResult> Create([FromBody] RoomCreateDto dto)
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

        // GET: api/Room/get-all-rooms?FloorId=...&Search=...&Page=1&PageSize=20
        [HttpGet("get-all-rooms")]
        public async Task<IActionResult> GetAll([FromQuery] RoomFilterParams p)
        {
            var result = await _service.GetAllAsync(p);
            return Ok(result);
        }

        // GET: api/Room/{id}/get-room
        [HttpGet("{id:guid}/get-room")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { status = false, message = $"Room {id} not found." });
            return Ok(result);
        }

        // POST: api/Room/{id}/update-room
        [HttpPost("{id:guid}/update-room")]
        public async Task<IActionResult> Update(Guid id, [FromBody] RoomUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updatedBy = User.Identity?.Name;
                var result = await _service.UpdateAsync(id, dto, updatedBy);
                if (result == null) return NotFound(new { status = false, message = $"Room {id} not found." });
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // PATCH: api/Room/{id}/update-geometry
        // 3D view isko drag(move)/resize/rotate KHATAM hone par (mouseup/touchend)
        // ek hi baar call kare — har frame pe nahi, warna load badh jaayega.
        [HttpPatch("{id:guid}/update-geometry")]
        public async Task<IActionResult> UpdateGeometry(Guid id, [FromBody] RoomGeometryUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var updatedBy = User.Identity?.Name;
            var result = await _service.UpdateGeometryAsync(id, dto, updatedBy);
            if (result == null) return NotFound(new { status = false, message = $"Room {id} not found." });
            return Ok(result);
        }

        // PATCH: api/Room/{id}/set-active?isActive=false
        [HttpPatch("{id:guid}/set-active")]
        public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool isActive)
        {
            var updatedBy = User.Identity?.Name;
            var ok = await _service.SetActiveStatusAsync(id, isActive, updatedBy);
            if (!ok) return NotFound(new { status = false, message = $"Room {id} not found." });
            return Ok(new { status = true, message = isActive ? "Activated." : "Deactivated." });
        }

        // ══════════════════════════════════════════════════════════
        //  EXPORT — Excel / CSV / PDF
        //  GET api/Room/export?format=excel|csv|pdf&FloorId=...
        // ══════════════════════════════════════════════════════════
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] RoomFilterParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var data = await _service.GetAllAsync(p);
            var file = await _exportService.ExportAsync(data.Data, exportFormat, "Rooms");
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Import — Excel (.xlsx) OR CSV (.csv). NO GUID chahiye — Floor ko
        /// naam se identify karo:
        ///   FloorName *  → matched against Floor.Name (case-insensitive)
        ///   Name *       → Room ka naam
        ///   RoomAreaSqFt, ReservedAreaSqFt, InternalUseAreaSqFt,
        ///   PositionX, PositionY, Width, Length, RotationAngle
        /// Ek hi file me multiple floors ke rooms import ho sakte hain
        /// (har row apna FloorName leke aata hai).
        /// POST /api/Room/import
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var createdBy = User.Identity?.Name;

            var knownColumns = new[]
            {
                "FloorName", "Name", "RoomAreaSqFt", "ReservedAreaSqFt", "InternalUseAreaSqFt",
                "PositionX", "PositionY", "Width", "Length", "RotationAngle"
            };

            var (sheet, headerToField, unmatchedColumns) =
                await _importService.ResolveMappingAsync(file, "Room", null, knownColumns);

            // ── Saare Floors ek hi query se memory me le lo (loop ke andar DB call nahi) ──
            var allFloors = await _db.Floors
                .Select(f => new { f.Id, f.Name })
                .ToListAsync();

            var floorLookup = allFloors
                .GroupBy(f => f.Name.Trim().ToLower(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var ambiguousFloorNames = allFloors
                .GroupBy(f => f.Name.Trim().ToLower(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var result = new GenericImportResultDto { TotalRows = sheet.Rows.Count, UnmatchedColumns = unmatchedColumns };
            var unmatchedRows = new List<DeviceManagementOnly.Common.ImportExport.UnmatchedDataWriter.UnmatchedRow>();

            for (int i = 0; i < sheet.Rows.Count; i++)
            {
                var raw = sheet.Rows[i];
                int rowNumber = i + 2;

                string? Get(string col) => raw.TryGetValue(col, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

                try
                {
                    var floorName = Get("FloorName");
                    if (string.IsNullOrWhiteSpace(floorName))
                        throw new InvalidOperationException("FloorName is required.");

                    if (ambiguousFloorNames.Contains(floorName.ToLower()))
                        throw new InvalidOperationException(
                            $"Multiple floors named '{floorName}' found — Floor names unique honi chahiye, ya batao to building-wise disambiguation add kar dete hain.");

                    if (!floorLookup.TryGetValue(floorName.ToLower(), out var floorId))
                        throw new InvalidOperationException($"No floor found with name '{floorName}'.");

                    var name = Get("Name");
                    if (string.IsNullOrWhiteSpace(name))
                        throw new InvalidOperationException("Name is required.");

                    var dto = new RoomCreateDto
                    {
                        FloorId = floorId,
                        Name = name,
                        RoomAreaSqFt = decimal.TryParse(Get("RoomAreaSqFt"), out var ra) ? ra : null,
                        ReservedAreaSqFt = decimal.TryParse(Get("ReservedAreaSqFt"), out var rsa) ? rsa : null,
                        InternalUseAreaSqFt = decimal.TryParse(Get("InternalUseAreaSqFt"), out var iua) ? iua : null,
                        PositionX = decimal.TryParse(Get("PositionX"), out var px) ? px : null,
                        PositionY = decimal.TryParse(Get("PositionY"), out var py) ? py : null,
                        Width = decimal.TryParse(Get("Width"), out var w) ? w : null,
                        Length = decimal.TryParse(Get("Length"), out var l) ? l : null,
                        RotationAngle = decimal.TryParse(Get("RotationAngle"), out var rot) ? rot : null
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
                    "Room", unmatchedColumns, unmatchedRows, file.FileName);
                result.Message += $" NOTE: {unmatchedColumns.Count} column(s) don't exist in our backend — saved to a .txt, see UnmatchedFileUrl.";
            }

            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════
        //  DXF — floor-plan CAD import/export (room rectangles)
        // ══════════════════════════════════════════════════════════

        // POST api/Room/import-dxf?floorName=Ground Floor  (multipart .dxf file)
        // Ab Guid nahi, Floor.Name (readable) leta hai.
        [HttpPost("import-dxf")]
        public async Task<IActionResult> ImportDxf([FromQuery] string floorName, IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(floorName))
                return BadRequest(new { status = false, message = "floorName is required." });
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload a .dxf file." });

            try
            {
                var matches = await _db.Floors
                    .Where(f => f.Name.ToLower() == floorName.Trim().ToLower())
                    .Select(f => f.Id)
                    .ToListAsync();

                if (matches.Count == 0)
                    return BadRequest(new { status = false, message = $"No floor found with name '{floorName}'." });
                if (matches.Count > 1)
                    return BadRequest(new { status = false, message = $"Multiple floors named '{floorName}' found — Floor names unique honi chahiye." });

                var updatedBy = User.Identity?.Name;
                var result = await _service.ImportDxfAsync(matches[0], file, updatedBy);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // GET api/Room/export-dxf?floorId=...
        // Ye export hai (existing floor select karke download karna), isliye
        // frontend dropdown se already-known FloorId aayega — GUID yahin rehne diya hai.
        // Agar isko bhi floorName se karwana ho, bata dena.
        [HttpGet("export-dxf")]
        public async Task<IActionResult> ExportDxf([FromQuery] Guid floorId)
        {
            try
            {
                var bytes = await _service.ExportDxfAsync(floorId);
                return File(bytes, "application/dxf", $"FloorPlan_{floorId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.dxf");
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }
    }
}