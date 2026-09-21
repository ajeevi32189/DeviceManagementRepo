using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementOnly.Controllers
{
    // ══════════════════════════════════════════════════════════════
    //  PROTOCOL DISCOVERY — endpoints for ProtocolService
    //  Exact routes from the spec's "if option 1 goes the API route
    //  instead" section, in case ProtocolService talks over HTTP
    //  rather than reading the MySQL tables directly.
    //  NOTE: still behind [Authorize] — ProtocolService needs a valid
    //  JWT (a service account, same as any other client of this API).
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Authorize]
    public class ProtocolDiscoveryController : ControllerBase
    {
        private readonly IModelProtocolService _service;
        public ProtocolDiscoveryController(IModelProtocolService service) => _service = service;

        // GET /api/devices/discovery-targets?Protocol=Modbus&DiscoveryStatus=pending
        // → devices with model identity + connection config, for a discovery run
        [HttpGet("/api/devices/discovery-targets")]
        public async Task<IActionResult> GetDiscoveryTargets([FromQuery] DiscoveryTargetFilterParams p)
        {
            var result = await _service.GetDiscoveryTargetsAsync(p);
            return Ok(result);
        }

        // GET /api/models/{modelSpecificationId}/protocol-profile?protocol=Modbus
        // → profile with its points
        [HttpGet("/api/models/{modelSpecificationId:guid}/protocol-profile")]
        public async Task<IActionResult> GetProtocolProfile(Guid modelSpecificationId, [FromQuery] string protocol)
        {
            var result = await _service.GetProfileByModelAndProtocolAsync(modelSpecificationId, protocol);
            if (result == null) return NotFound(new { status = false, message = $"'{protocol}' profile is model ke liye nahi mila." });
            return Ok(result);
        }

        // POST /api/models/{modelSpecificationId}/protocol-profile
        // → create a profile (with its points) after an engineer confirms a discovered map
        [HttpPost("/api/models/{modelSpecificationId:guid}/protocol-profile")]
        public async Task<IActionResult> CreateProtocolProfile(Guid modelSpecificationId, [FromBody] ModelProtocolProfileCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.ModelSpecificationId = modelSpecificationId;  // route se authoritative, body ignore
            try
            {
                var createdBy = User.Identity?.Name ?? "ProtocolService";
                var result = await _service.CreateProfileAsync(dto, createdBy);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // PATCH /api/devices/{deviceDetailId}/discovery-status
        // → write back status, timestamp and message after a discovery attempt
        [HttpPatch("/api/devices/{deviceDetailId:guid}/discovery-status")]
        public async Task<IActionResult> UpdateDiscoveryStatus(Guid deviceDetailId, [FromBody] DiscoveryStatusUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var ok = await _service.UpdateDiscoveryStatusAsync(deviceDetailId, dto);
            if (!ok) return NotFound(new { status = false, message = $"DeviceDetail {deviceDetailId} not found." });
            return Ok(new { status = true, message = "Discovery status update ho gaya." });
        }
    }
}
