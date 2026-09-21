using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementOnly.Controllers
{
    // ══════════════════════════════════════════════════════════════
    //  FLOOR
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FloorController : ControllerBase
    {
        private readonly IFloorService _service;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;

        public FloorController(IFloorService service,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _service = service;
            _exportService = exportService;
            _importService = importService;
        }

        // POST: api/Floor/add-floor
        [HttpPost("add-floor")]
        public async Task<IActionResult> Create([FromBody] FloorCreateDto dto)
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

        // GET: api/Floor/get-all-floors
        [HttpGet("get-all-floors")]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParams p)
        {
            var result = await _service.GetAllAsync(p);
            return Ok(result);
        }

        // GET: api/Floor/{id}/get-floor
        [HttpGet("{id:guid}/get-floor")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { status = false, message = $"Floor {id} not found." });
            return Ok(result);
        }

        // POST: api/Floor/{id}/update-floor
        [HttpPost("{id:guid}/update-floor")]
        public async Task<IActionResult> Update(Guid id, [FromBody] FloorUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updatedBy = User.Identity?.Name;
                var result = await _service.UpdateAsync(id, dto, updatedBy);
                if (result == null) return NotFound(new { status = false, message = $"Floor {id} not found." });
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // PATCH: api/Floor/{id}/set-active?isActive=false
        [HttpPatch("{id:guid}/set-active")]
        public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool isActive)
        {
            var updatedBy = User.Identity?.Name;
            var ok = await _service.SetActiveStatusAsync(id, isActive, updatedBy);
            if (!ok) return NotFound(new { status = false, message = $"Floor {id} not found." });
            return Ok(new { status = true, message = isActive ? "Activated." : "Deactivated." });
        }

        // ══════════════════════════════════════════════════════════
        //  EXPORT — Excel / CSV / PDF
        //  GET api/Floor/export?format=excel|csv|pdf
        // ══════════════════════════════════════════════════════════
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format, [FromQuery] PaginationParams p)
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            p.Page = null; p.PageSize = null;
            var data = await _service.GetAllAsync(p);
            var file = await _exportService.ExportAsync(data.Data, exportFormat, "Floors");
            return File(file.Content, file.ContentType, file.FileName);
        }

        // ══════════════════════════════════════════════════════════
        //  IMPORT — Excel / CSV, dynamic column mapping + unmatched-.txt
        //  POST api/Floor/import  (multipart file)
        // ══════════════════════════════════════════════════════════
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var createdBy = User.Identity?.Name;
            var result = await _importService.ImportAsync<FloorCreateDto>(
                file, "Floor", null,
                async (dto, rowNumber) => await _service.CreateAsync(dto, createdBy));

            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════
        //  DXF floor-plan import/export lives on the Room resource
        //  (geometry is stored per-Room) — see:
        //    POST api/Room/import-dxf?floorId=...
        //    GET  api/Room/export-dxf?floorId=...
        // ══════════════════════════════════════════════════════════
    }
}
