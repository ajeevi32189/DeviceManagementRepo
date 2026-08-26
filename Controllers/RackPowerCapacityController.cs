using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public RackPowerCapacityController(IRackPowerCapacityService service) => _service = service;

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
