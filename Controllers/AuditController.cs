using DeviceManagementOnly.Common;
using Microsoft.AspNetCore.Authorization;
using DeviceManagement.Data;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementOnly.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly DBContext _db;

        public AuditController(DBContext db)
        {
            _db = db;
        }

        // GET /api/audit/history/{tableName}?Page=1&PageSize=10
        [HttpGet("history/{tableName}")]
        public async Task<IActionResult> GetTableHistory(
            string tableName,
            [FromQuery] PaginationParams p)
        {
            var query = _db.AuditLogs
                .Where(x => x.TableName!.ToLower() == tableName.ToLower())
                .OrderByDescending(x => x.ChangedAt);

            var result = await query.ToPagedAsync(p);
            return Ok(result);
        }

        // GET /api/audit/history/{tableName}/{recordId}?Page=1&PageSize=10
        [HttpGet("history/{tableName}/{recordId}")]
        public async Task<IActionResult> GetHistory(
            string tableName,
            string recordId,
            [FromQuery] PaginationParams p)
        {
            var query = _db.AuditLogs
                .Where(x =>
                    x.TableName!.ToLower() == tableName.ToLower()
                    && x.RecordId == recordId)
                .OrderByDescending(x => x.ChangedAt);

            var result = await query.ToPagedAsync(p);
            return Ok(result);
        }

        // POST /api/audit/mark-as-final/{auditLogId}
        [HttpPost("mark-as-final/{auditLogId}")]
        public async Task<IActionResult> MarkAsFinal(Guid auditLogId)
        {
            var log = await _db.AuditLogs.FindAsync(auditLogId);

            if (log == null)
                return NotFound();

            log.IsFinal = true;
            log.FinalizedAt = DateTime.UtcNow;
            log.FinalizedBy = User.Identity?.Name ?? "System";
            await _db.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = "Audit finalized successfully."
            });
        }
    }
}