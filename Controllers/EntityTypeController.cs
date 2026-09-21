using DeviceManagement.Data;
using Microsoft.AspNetCore.Authorization;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EntityTypeController : ControllerBase
    {
        private readonly DBContext _db;
        private readonly DeviceManagementOnly.Common.ImportExport.IExportService _exportService;
        private readonly DeviceManagementOnly.Common.ImportExport.IImportService _importService;

        public EntityTypeController(DBContext db,
            DeviceManagementOnly.Common.ImportExport.IExportService exportService,
            DeviceManagementOnly.Common.ImportExport.IImportService importService)
        {
            _db = db;
            _exportService = exportService;
            _importService = importService;
        }

        [HttpGet("get-all-entity")]
        public IActionResult GetAllEntity()
        {
            var result = _db.EntityTypes
                .OrderBy(e => e.EntityName)
                .Select(e => new EntityTypeResponseDto
                {
                    Id = e.Id,
                    EntityName = e.EntityName,
                    DisplayName = e.DisplayName,
                    IsActive = e.IsActive
                }).ToList();
            return Ok(result);
        }

        [HttpGet("get-entity{id:guid}")]
        public IActionResult GetEntityById(Guid id)
        {
            var et = _db.EntityTypes.Find(id);
            if (et == null) return NotFound();
            return Ok(new EntityTypeResponseDto
            {
                Id = et.Id,
                EntityName = et.EntityName,
                DisplayName = et.DisplayName,
                IsActive = et.IsActive
            });
        }

        [HttpPost("create-entity")]
        public IActionResult CreateEntity([FromBody] EntityTypeCreateDto dto)
        {
            var exists = _db.EntityTypes.Any(e => e.EntityName.ToLower() == dto.EntityName.ToLower());
            if (exists) return BadRequest(new { message = $"'{dto.EntityName}' pehle se exist karta hai." });

            var et = new EntityType
            {
                EntityName = dto.EntityName.Trim(),
                DisplayName = dto.DisplayName?.Trim() ?? dto.EntityName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.EntityTypes.Add(et);
            _db.SaveChangesAsync().GetAwaiter().GetResult();
            return CreatedAtAction(nameof(GetEntityById), new { id = et.Id }, et);
        }

        [HttpPost("update-entity/{id:guid}")]
        public IActionResult UpdateEntity(Guid id, [FromBody] EntityTypeUpdateDto dto)
        {
            var et = _db.EntityTypes.Find(id);
            if (et == null) return NotFound();

            var conflict = _db.EntityTypes.Any(e => e.EntityName.ToLower() == dto.EntityName.ToLower() && e.Id != id);
            if (conflict) return BadRequest(new { message = "Yeh naam kisi aur entity me use ho raha hai." });

            et.EntityName = dto.EntityName.Trim();
            et.DisplayName = dto.DisplayName?.Trim() ?? dto.EntityName;
            et.IsActive = dto.IsActive;
            et.UpdatedAt = DateTime.UtcNow;
            _db.SaveChangesAsync().GetAwaiter().GetResult();
            return NoContent();
        }

        //[HttpDelete("soft-delete/{id:guid}")]
        //public IActionResult Delete(Guid id)
        //{
        //    var et = _db.EntityTypes.Include(e => e.FileStores).FirstOrDefault(e => e.Id == id);
        //    if (et == null) return NotFound();

        //    if (et.FileStores.Any())
        //    {
        //        et.IsActive = false;
        //        et.UpdatedAt = DateTime.UtcNow;
        //        _db.SaveChangesAsync().GetAwaiter().GetResult();
        //        return Ok(new { message = "Files linked hain, soft delete kar diya." });
        //    }

        //    _db.EntityTypes.Remove(et);
        //    _db.SaveChangesAsync().GetAwaiter().GetResult();
        //    return NoContent();
        //}
        // GET: api/EntityType/export?format=excel|csv|pdf
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format = "excel")
        {
            var exportFormat = DeviceManagementOnly.Common.ImportExport.ExportFormatHelper.Parse(format);
            var data = _db.EntityTypes.OrderBy(e => e.EntityName)
                .Select(e => new EntityTypeResponseDto
                {
                    Id = e.Id,
                    EntityName = e.EntityName,
                    DisplayName = e.DisplayName,
                    IsActive = e.IsActive
                }).ToList();
            var file = await _exportService.ExportAsync(data, exportFormat, "EntityTypes");
            return File(file.Content, file.ContentType, file.FileName);
        }

        // POST: api/EntityType/import — Excel/CSV, dynamic mapping, unmatched-.txt
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Please upload an Excel (.xlsx) or CSV (.csv) file." });

            var result = await _importService.ImportAsync<EntityTypeCreateDto>(
                file, "EntityType", null,
                async (dto, rowNumber) =>
                {
                    if (string.IsNullOrWhiteSpace(dto.EntityName))
                        throw new InvalidOperationException("EntityName is required.");

                    var exists = await _db.EntityTypes.AnyAsync(e => e.EntityName.ToLower() == dto.EntityName.ToLower());
                    if (exists) throw new InvalidOperationException($"'{dto.EntityName}' already exists.");

                    _db.EntityTypes.Add(new EntityType
                    {
                        EntityName = dto.EntityName.Trim(),
                        DisplayName = dto.DisplayName?.Trim() ?? dto.EntityName,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();
                });

            return Ok(result);
        }
    }
}