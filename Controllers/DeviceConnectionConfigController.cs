using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public DeviceConnectionConfigController(IDeviceConnectionConfigService service) => _service = service;

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
