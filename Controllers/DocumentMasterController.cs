using DeviceManagement.Data;
using Microsoft.AspNetCore.Authorization;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementOnly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentMasterController : ControllerBase
    {
        private readonly DBContext _db;
        public DocumentMasterController(DBContext db) => _db = db;

        // ── Document Types ─────────────────────────────────────────

        [HttpGet("document-types")]
        public IActionResult GetDocumentTypes()
        {
            var result = _db.DocumentTypes
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.TypeName)
                .Select(d => new DocumentTypeDto { Id = d.Id, TypeName = d.TypeName })
                .ToList();
            return Ok(result);
        }

        [HttpPost("create-documenttypes")]
        public IActionResult AddDocumentType([FromBody] DocumentTypeDto dto)
        {
            var exists = _db.DocumentTypes.Any(d => d.TypeName.ToLower() == dto.TypeName.ToLower() && !d.IsDeleted);
            if (exists) return BadRequest(new { message = "Yeh DocumentType pehle se exist karta hai." });

            var doc = new DocumentType { TypeName = dto.TypeName.Trim() };
            _db.DocumentTypes.Add(doc);
            _db.SaveChangesAsync().GetAwaiter().GetResult();
            return Ok(doc);
        }

        [HttpDelete("document-types/{id:guid}")]
        public IActionResult DeleteDocumentType(Guid id)
        {
            var doc = _db.DocumentTypes.Find(id);
            if (doc == null) return NotFound();
            doc.IsDeleted = true;
            doc.DeletedAt = DateTime.UtcNow;
            _db.SaveChangesAsync().GetAwaiter().GetResult();
            return NoContent();
        }

        // ── Issued By ──────────────────────────────────────────────

        [HttpGet("issued-by")]
        public IActionResult GetIssuedBy()
        {
            var result = _db.DocumentIssuedBies
                .OrderBy(i => i.IssuedByName)
                .Select(i => new DocumentIssuedByDto { Id = i.Id, IssuedByName = i.IssuedByName })
                .ToList();
            return Ok(result);
        }

        [HttpPost("issued-by")]
        public IActionResult AddIssuedBy([FromBody] DocumentIssuedByDto dto)
        {
            var item = new DocumentIssuedBy { IssuedByName = dto.IssuedByName.Trim() };
            _db.DocumentIssuedBies.Add(item);
            _db.SaveChangesAsync().GetAwaiter().GetResult();
            return Ok(item);
        }
    }
}