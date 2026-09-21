using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementOnly.Controllers
{
    // ══════════════════════════════════════════════════════════════
    //  FIELD MAPPING — one generic endpoint set used by every module's
    //  "map my Excel columns to your fields" import UI.
    //
    //  Flow the frontend should follow:
    //    1. User picks a file to import. Frontend calls the entity's
    //       normal import endpoint (e.g. POST /api/DeviceDetail/import)
    //       once — the response's UnmatchedColumns tells the UI which
    //       headers from the file could not be auto-matched.
    //    2. UI shows a mapping screen: for each unmatched header, user
    //       picks which backend field it corresponds to (or "ignore").
    //    3. UI calls POST /api/FieldMapping/save once. From then on,
    //       the same header always auto-maps — the user is not asked again.
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FieldMappingController : ControllerBase
    {
        private readonly IFieldMappingService _mappingService;
        public FieldMappingController(IFieldMappingService mappingService) => _mappingService = mappingService;

        /// <summary>
        /// GET api/FieldMapping/get?entityName=DeviceDetail&companyId=...
        /// Returns whatever mapping is already saved for this entity/company.
        /// </summary>
        [HttpGet("get")]
        public async Task<IActionResult> Get([FromQuery] string entityName, [FromQuery] Guid? companyId)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return BadRequest(new { message = "entityName is required." });

            var map = await _mappingService.GetMappingAsync(entityName, companyId);
            return Ok(new FieldMappingResponseDto
            {
                EntityName = entityName,
                CompanyId = companyId,
                Mappings = map.Select(kv => new FieldMappingEntryDto { SourceHeader = kv.Key, TargetField = kv.Value }).ToList()
            });
        }

        /// <summary>
        /// POST api/FieldMapping/save
        /// Body: { entityName, companyId?, mappings: [{ sourceHeader, targetField }, ...] }
        /// Saves/updates the mapping so future imports of the same sheet layout
        /// apply it automatically.
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] SaveFieldMappingRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.EntityName))
                return BadRequest(new { message = "entityName is required." });

            var updatedBy = User.Identity?.Name;
            await _mappingService.SaveMappingAsync(dto, updatedBy);
            return Ok(new { status = true, message = "Mapping saved. Future imports of this entity will use it automatically." });
        }
    }
}
