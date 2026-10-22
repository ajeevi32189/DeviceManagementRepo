using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementOnly.Controllers
{
    // ══════════════════════════════════════════════════════════════
    //  DATA CENTRE — 5D View ka building record.
    //  Baaki masters ke jaisa hi dhancha: add / get-all / get / update / set-active.
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DataCenterController : ControllerBase
    {
        private readonly IDataCenterService _service;
        public DataCenterController(IDataCenterService service) => _service = service;

        private string? Me => User.Identity?.Name;

        // POST: api/DataCenter/add-datacenter
        [HttpPost("add-datacenter")]
        public async Task<IActionResult> Create([FromBody] DataCenterCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.CreateAsync(dto, Me);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // GET: api/DataCenter/get-all-datacenters?Search=&Page=1&PageSize=20
        [HttpGet("get-all-datacenters")]
        public async Task<IActionResult> GetAll([FromQuery] DataCenterFilterParams p)
            => Ok(await _service.GetAllAsync(p));

        // GET: api/DataCenter/{id}/get-datacenter
        [HttpGet("{id:guid}/get-datacenter")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new { status = false, message = $"Data centre {id} nahi mila." });
            return Ok(result);
        }

        // POST: api/DataCenter/{id}/update-datacenter
        [HttpPost("{id:guid}/update-datacenter")]
        public async Task<IActionResult> Update(Guid id, [FromBody] DataCenterUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.UpdateAsync(id, dto, Me);
                if (result is null) return NotFound(new { status = false, message = $"Data centre {id} nahi mila." });
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // PATCH: api/DataCenter/{id}/set-active?isActive=false
        [HttpPatch("{id:guid}/set-active")]
        public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool isActive)
        {
            var ok = await _service.SetActiveAsync(id, isActive, Me);
            if (!ok) return NotFound(new { status = false, message = $"Data centre {id} nahi mila." });
            return Ok(new { status = true, message = isActive ? "Activated." : "Deactivated." });
        }
    }
}
