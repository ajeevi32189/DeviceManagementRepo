using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementOnly.Controllers
{
    // ══════════════════════════════════════════════════════════════
    //  MODEL PROTOCOL PROFILE / POINT
    //  Device Management side screens use these to view/create/edit
    //  protocol profiles per model (e.g. the TINYPRO-6 register map).
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ModelProtocolController : ControllerBase
    {
        private readonly IModelProtocolService _service;
        public ModelProtocolController(IModelProtocolService service) => _service = service;

        // POST: api/ModelProtocol/profiles/add-profile
        [HttpPost("profiles/add-profile")]
        public async Task<IActionResult> CreateProfile([FromBody] ModelProtocolProfileCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var createdBy = User.Identity?.Name;
                var result = await _service.CreateProfileAsync(dto, createdBy);
                return CreatedAtAction(nameof(GetProfileById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // GET: api/ModelProtocol/profiles/get-all-profiles?ModelSpecificationId=...&Protocol=...&IsVerified=...
        [HttpGet("profiles/get-all-profiles")]
        public async Task<IActionResult> GetAllProfiles([FromQuery] ModelProtocolProfileFilterParams p)
        {
            var result = await _service.GetAllProfilesAsync(p);
            return Ok(result);
        }

        // GET: api/ModelProtocol/profiles/{id}/get-profile
        [HttpGet("profiles/{id:guid}/get-profile")]
        public async Task<IActionResult> GetProfileById(Guid id)
        {
            var result = await _service.GetProfileByIdAsync(id);
            if (result == null) return NotFound(new { status = false, message = $"ModelProtocolProfile {id} not found." });
            return Ok(result);
        }

        // GET: api/ModelProtocol/profiles/by-model?modelSpecificationId=...&protocol=Modbus
        [HttpGet("profiles/by-model")]
        public async Task<IActionResult> GetProfileByModel([FromQuery] Guid modelSpecificationId, [FromQuery] string protocol)
        {
            var result = await _service.GetProfileByModelAndProtocolAsync(modelSpecificationId, protocol);
            if (result == null) return NotFound(new { status = false, message = $"'{protocol}' profile is model ke liye nahi mila." });
            return Ok(result);
        }

        // POST: api/ModelProtocol/profiles/{id}/update-profile
        [HttpPost("profiles/{id:guid}/update-profile")]
        public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] ModelProtocolProfileUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var updatedBy = User.Identity?.Name;
                var result = await _service.UpdateProfileAsync(id, dto, updatedBy);
                if (result == null) return NotFound(new { status = false, message = $"ModelProtocolProfile {id} not found." });
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // DELETE: api/ModelProtocol/profiles/{id}/delete-profile
        // Soft-deletes the profile AND all its points.
        [HttpDelete("profiles/{id:guid}/delete-profile")]
        public async Task<IActionResult> DeleteProfile(Guid id)
        {
            var deletedBy = User.Identity?.Name;
            var ok = await _service.DeleteProfileAsync(id, deletedBy);
            if (!ok) return NotFound(new { status = false, message = $"ModelProtocolProfile {id} not found." });
            return Ok(new { status = true, message = "Profile aur uske points delete ho gaye." });
        }

        // POST: api/ModelProtocol/profiles/{profileId}/points/add-point
        [HttpPost("profiles/{profileId:guid}/points/add-point")]
        public async Task<IActionResult> AddPoint(Guid profileId, [FromBody] ModelProtocolPointCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.AddPointAsync(profileId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // POST: api/ModelProtocol/points/{pointId}/update-point
        [HttpPost("points/{pointId:guid}/update-point")]
        public async Task<IActionResult> UpdatePoint(Guid pointId, [FromBody] ModelProtocolPointUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.UpdatePointAsync(pointId, dto);
            if (result == null) return NotFound(new { status = false, message = $"ModelProtocolPoint {pointId} not found." });
            return Ok(result);
        }

        // DELETE: api/ModelProtocol/points/{pointId}/delete-point
        [HttpDelete("points/{pointId:guid}/delete-point")]
        public async Task<IActionResult> DeletePoint(Guid pointId)
        {
            var deletedBy = User.Identity?.Name;
            var ok = await _service.DeletePointAsync(pointId, deletedBy);
            if (!ok) return NotFound(new { status = false, message = $"ModelProtocolPoint {pointId} not found." });
            return Ok(new { status = true, message = "Point delete ho gaya." });
        }
    }
}
