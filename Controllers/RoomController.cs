using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public RoomController(IRoomService service) => _service = service;

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
    }
}
