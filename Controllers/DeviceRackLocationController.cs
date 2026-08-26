using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public DeviceRackLocationController(IDeviceRackLocationService service) => _service = service;

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
