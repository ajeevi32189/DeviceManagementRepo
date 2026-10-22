using System.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementOnly.Controllers
{
    // ══════════════════════════════════════════════════════════════
    //  LAYOUT — 5D View ka API.
    //
    //  Khulne ka tareeka (frontend ka kram):
    //    1. GET  floor/{id}                -> poora scene (ek hi call)
    //    2. SignalR: JoinFloor(floorId)    -> doosron ke badlav live
    //    3. Edit karna ho to:
    //         POST floor/{id}/acquire-edit  -> lease le lo
    //         ... user drag/drop karta rahe ...
    //         PATCH placement/{pid}/transform  (har mouseup par ek baar)
    //         POST floor/{id}/heartbeat-edit   (har 2-3 minute)
    //         POST floor/{id}/save             (Save button)
    //         POST floor/{id}/release-edit     (edit mode band / tab band)
    //    4. Layout final ho jaye to:
    //         POST floor/{id}/lock { isLocked: true }
    //
    //  HTTP codes jo frontend ko samajhne hain:
    //    423 Locked   -> layout lock hai, pehle unlock karo
    //    409 Conflict -> ya to koi aur edit kar raha hai, ya aapke
    //                    paas purana revision hai (dobara load karo)
    // ══════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LayoutController : ControllerBase
    {
        private readonly ILayoutService _service;
        private readonly ILogger<LayoutController> _log;

        public LayoutController(ILayoutService service, ILogger<LayoutController> log)
        {
            _service = service;
            _log = log;
        }

        private string? Me => User.Identity?.Name;

        /// <summary>Lock/conflict wale message ko sahi HTTP code me badalta hai.</summary>
        private IActionResult MapError(InvalidOperationException ex)
        {
            var m = ex.Message ?? string.Empty;
            if (m.Contains("locked hai", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status423Locked, new { status = false, code = "LAYOUT_LOCKED", message = m });
            if (m.Contains("edit kar raha", StringComparison.OrdinalIgnoreCase)
             || m.Contains("khatam ho chuka", StringComparison.OrdinalIgnoreCase)
             || m.Contains("ab aapke paas nahi", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { status = false, code = "EDIT_SESSION", message = m });
            return BadRequest(new { status = false, message = m });
        }

        // ══════════════════════════════════════════════════════════
        //  SCENE
        // ══════════════════════════════════════════════════════════

        /// <summary>Poora 5D drishya — ek hi call me. View khulte hi yahi lagti hai.</summary>
        // GET: api/Layout/floor/{floorId}
        [HttpGet("floor/{floorId:guid}")]
        public async Task<IActionResult> GetScene(Guid floorId)
        {
            var scene = await _service.GetSceneAsync(floorId, Me);
            if (scene is null) return NotFound(new { status = false, message = $"Floor {floorId} nahi mila." });
            return Ok(scene);
        }

        /// <summary>Poora drishya save. Jo list nahi bheji, usko haath nahi lagta.</summary>
        // POST: api/Layout/floor/{floorId}/save
        [HttpPost("floor/{floorId:guid}/save")]
        public async Task<IActionResult> SaveScene(Guid floorId, [FromBody] LayoutSaveDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.SaveSceneAsync(floorId, dto, Me);
                return Ok(result);
            }
            catch (DBConcurrencyException ex)
            {
                return Conflict(new { status = false, code = "STALE_REVISION", message = ex.Message });
            }
            catch (InvalidOperationException ex) { return MapError(ex); }
        }

        /// <summary>
        /// Ek device hilana — drag khatam hone par ek baar.
        /// Har frame par mat bhejiye; mouseup/touchend par ek hi call.
        /// </summary>
        // PATCH: api/Layout/placement/{placementId}/transform
        [HttpPatch("placement/{placementId:guid}/transform")]
        public async Task<IActionResult> Transform(Guid placementId, [FromBody] PlacementTransformDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.TransformPlacementAsync(placementId, dto, Me);
                if (result is null) return NotFound(new { status = false, message = $"Placement {placementId} nahi mila." });
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return MapError(ex); }
        }

        // ══════════════════════════════════════════════════════════
        //  LOCK / EDIT MODE
        // ══════════════════════════════════════════════════════════

        /// <summary>Abhi layout ki kya haalat hai — view, edit ya locked.</summary>
        // GET: api/Layout/floor/{floorId}/lock?sessionId=...
        [HttpGet("floor/{floorId:guid}/lock")]
        public async Task<IActionResult> GetLock(Guid floorId, [FromQuery] string? sessionId)
        {
            var result = await _service.GetLockAsync(floorId, Me, sessionId);
            if (result is null) return NotFound(new { status = false, message = $"Floor {floorId} nahi mila." });
            return Ok(result);
        }

        /// <summary>
        /// Pakka lock lagao ya hatao.
        /// Lock lagte hi chalti hui edit session bhi khatam ho jaati hai.
        /// </summary>
        // POST: api/Layout/floor/{floorId}/lock
        [HttpPost("floor/{floorId:guid}/lock")]
        public async Task<IActionResult> SetLock(Guid floorId, [FromBody] LayoutLockRequestDto dto)
        {
            try { return Ok(await _service.SetLockAsync(floorId, dto, Me)); }
            catch (InvalidOperationException ex) { return MapError(ex); }
        }

        /// <summary>Edit mode on — lease le lo. Iske bina koi save nahi chalega.</summary>
        // POST: api/Layout/floor/{floorId}/acquire-edit
        [HttpPost("floor/{floorId:guid}/acquire-edit")]
        public async Task<IActionResult> AcquireEdit(Guid floorId, [FromBody] EditSessionRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _service.AcquireEditAsync(floorId, dto, Me)); }
            catch (InvalidOperationException ex) { return MapError(ex); }
        }

        /// <summary>Lease ko zinda rakho — frontend har 2-3 minute me bheje.</summary>
        // POST: api/Layout/floor/{floorId}/heartbeat-edit
        [HttpPost("floor/{floorId:guid}/heartbeat-edit")]
        public async Task<IActionResult> Heartbeat(Guid floorId, [FromBody] EditSessionRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _service.HeartbeatEditAsync(floorId, dto, Me)); }
            catch (InvalidOperationException ex) { return MapError(ex); }
        }

        /// <summary>Edit mode band — lease chhod do, taaki agla user le sake.</summary>
        // POST: api/Layout/floor/{floorId}/release-edit
        [HttpPost("floor/{floorId:guid}/release-edit")]
        public async Task<IActionResult> ReleaseEdit(Guid floorId, [FromBody] EditSessionRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return Ok(await _service.ReleaseEditAsync(floorId, dto.EditSessionId, Me));
        }

        // ══════════════════════════════════════════════════════════
        //  SAVED VIEWS (compare views)
        // ══════════════════════════════════════════════════════════

        // GET: api/Layout/floor/{floorId}/views
        [HttpGet("floor/{floorId:guid}/views")]
        public async Task<IActionResult> GetViews(Guid floorId)
            => Ok(await _service.GetViewsAsync(floorId, Me));

        // POST: api/Layout/floor/{floorId}/views
        [HttpPost("floor/{floorId:guid}/views")]
        public async Task<IActionResult> CreateView(Guid floorId, [FromBody] LayoutViewCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _service.CreateViewAsync(floorId, dto, Me)); }
            catch (InvalidOperationException ex) { return MapError(ex); }
        }

        // DELETE: api/Layout/views/{viewId}
        [HttpDelete("views/{viewId:guid}")]
        public async Task<IActionResult> DeleteView(Guid viewId)
        {
            var ok = await _service.DeleteViewAsync(viewId, Me);
            if (!ok) return NotFound(new { status = false, message = $"View {viewId} nahi mila." });
            return Ok(new { status = true, message = "View hata diya gaya." });
        }
    }
}
