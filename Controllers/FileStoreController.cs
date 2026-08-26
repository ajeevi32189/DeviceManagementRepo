using DeviceManagementOnly.Dtos;
using Microsoft.AspNetCore.Authorization;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementOnly.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileStoreController : ControllerBase
    {
        private readonly IFileStoreService _fileStoreService;
        private readonly IWebHostEnvironment _env;

        public FileStoreController(IFileStoreService fileStoreService, IWebHostEnvironment env)
        {
            _fileStoreService = fileStoreService;
            _env = env;
        }

        private string UploadBasePath => Path.Combine(_env.ContentRootPath, "uploads");

        [HttpPost("upload-file")]
        [Consumes("multipart/form-data")]
        public IActionResult Upload([FromForm] FileStoreUploadDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("File required hai.");

            if (dto.FileCategory != "Image" && dto.FileCategory != "Document")
                return BadRequest("FileCategory sirf 'Image' ya 'Document' hona chahiye.");

            var result = _fileStoreService.UploadFile(dto, UploadBasePath);
            return Ok(result);
        }

        [HttpGet("entity/{entityTypeId:guid}/{entityId:guid}")]
        public IActionResult GetFileByEntity(Guid entityTypeId, Guid entityId, [FromQuery] string? fileCategory = null)
        {
            var result = _fileStoreService.GetFilesByEntity(entityTypeId, entityId, fileCategory);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public IActionResult GetFileById(Guid id)
        {
            var result = _fileStoreService.GetFileById(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("update-file{id:guid}")]
        [Consumes("multipart/form-data")]
        public IActionResult UpdateFile(Guid id, [FromForm] FileStoreUpdateDto dto)
        {
            var result = _fileStoreService.UpdateFile(id, dto, UploadBasePath);
            return result == null ? NotFound() : Ok(result);
        }

        //[HttpDelete("soft-delete/{id:guid}")]
        //public IActionResult SoftDelete(Guid id, [FromQuery] string deletedBy = "System")
        //{
        //    var success = _fileStoreService.SoftDeleteFile(id, deletedBy);
        //    return success ? NoContent() : NotFound();
        //}

        //[HttpDelete("hard-delete/{id:guid}")]
        //public IActionResult HardDelete(Guid id)
        //{
        //    var success = _fileStoreService.HardDeleteFile(id);
        //    return success ? Ok("File deleted.") : NotFound();
        //}
    }
}