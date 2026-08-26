using DeviceManagementOnly.Common;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementOnly.Controllers
{
    // ══════════════════════════════════════════════════════════════
    //  DEVICE LOCATION
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceLocationController : ControllerBase
    {
        private readonly IDeviceLocationService _service;
        private readonly IWebHostEnvironment _env;

        public DeviceLocationController(IDeviceLocationService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        private string UploadBasePath => Path.Combine(_env.ContentRootPath, "uploads");

        // POST: api/DeviceLocation/add-device-location
        [HttpPost("add-device-location")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateDeviceLocation([FromForm] DeviceLocationCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.CreateDeviceLocationAsync(dto, UploadBasePath);
                return CreatedAtAction(nameof(GetDeviceLocationById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { status = false, message = ex.Message }); }
        }

        // GET: api/DeviceLocation/get-all-device-locations
        [HttpGet("get-all-device-locations")]
        public async Task<IActionResult> GetAllDeviceLocation([FromQuery] PaginationParams p)
        {
            try
            {
                var result = await _service.GetAllDeviceLocationAsync(p);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        // GET: api/DeviceLocation/{id}/get-device-location
        [HttpGet("{id:guid}/get-device-location")]
        public async Task<IActionResult> GetDeviceLocationById(Guid id)
        {
            var result = await _service.GetDeviceLocationByIdAsync(id);
            if (result == null) return NotFound(new { status = false, message = $"DeviceLocation {id} not found." });
            return Ok(result);
        }

        // GET: api/DeviceLocation/{deviceDetailId}/get-by-device
        [HttpGet("{deviceDetailId:guid}/get-by-device")]
        public async Task<IActionResult> GetByDevice(Guid deviceDetailId, [FromQuery] PaginationParams p)
        {
            var result = await _service.GetDeviceLocationByDeviceAsync(deviceDetailId, p);
            return Ok(result);
        }

        // PUT: api/DeviceLocation/{id}/update-device-location
        [HttpPost("{id:guid}/update-device-location")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateDeviceLocation(Guid id, [FromForm] DeviceLocationUpdateDto dto)
        {
            var result = await _service.UpdateDeviceLocationAsync(id, dto, UploadBasePath);
            if (result == null) return NotFound(new { status = false, message = $"DeviceLocation {id} not found." });
            return Ok(result);
        }

        // PATCH: api/DeviceLocation/{id}/set-active?isActive=false
        //[HttpPatch("{id:guid}/set-active")]
        //public async Task<IActionResult> SetActive(Guid id, [FromQuery] bool isActive)
        //{
        //    var updatedBy = User.Identity?.Name;
        //    var ok = await _service.SetActiveStatusAsync(id, isActive, updatedBy);
        //    if (!ok) return NotFound(new { status = false, message = $"DeviceLocation {id} not found." });
        //    return Ok(new { status = true, message = isActive ? "Activated." : "Deactivated." });
        //}
    }
}