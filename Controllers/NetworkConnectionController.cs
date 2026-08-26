using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public NetworkConnectionController(INetworkConnectionService service) => _service = service;

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
