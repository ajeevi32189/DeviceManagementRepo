using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    public interface IFileStoreService
    {
        FileStoreResponseDto UploadFile(FileStoreUploadDto dto, string uploadBasePath);
        List<FileStoreResponseDto> GetFilesByEntity(Guid entityTypeId, Guid entityId, string? fileCategory = null);
        FileStoreResponseDto? GetFileById(Guid fileId);
        FileStoreResponseDto? UpdateFile(Guid fileId, FileStoreUpdateDto dto, string uploadBasePath);
        bool SoftDeleteFile(Guid fileId, string deletedBy);
        bool HardDeleteFile(Guid fileId);
    }

    public class FileStoreService : IFileStoreService
    {
        private readonly DBContext _db;

        public FileStoreService(DBContext db)
        {
            _db = db;
        }

        private EntityType GetEntityTypeByName(string entityName)
        {
            return _db.EntityTypes
                .FirstOrDefault(e => e.EntityName == entityName && e.IsActive)
                ?? throw new InvalidOperationException($"EntityType '{entityName}' nahi mila DB me.");
        }

        private (string fileName, string filePath) SaveFileToDisk(
            IFormFile file, string entityTypeName, Guid entityId, string uploadBasePath)
        {
            var folder = Path.Combine(uploadBasePath, entityTypeName.ToLower(), entityId.ToString());
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            var relativePath = Path.Combine("uploads", entityTypeName.ToLower(), entityId.ToString(), uniqueFileName)
                .Replace("\\", "/");

            return (file.FileName, relativePath);
        }

        public FileStoreResponseDto UploadFile(FileStoreUploadDto dto, string uploadBasePath)
        {
            var entityType = _db.EntityTypes.Find(dto.EntityTypeId)
                ?? throw new KeyNotFoundException($"EntityType {dto.EntityTypeId} nahi mila.");

            if (!entityType.IsActive)
                throw new InvalidOperationException($"EntityType '{entityType.EntityName}' inactive hai.");

            var (fileName, filePath) = SaveFileToDisk(dto.File, entityType.EntityName, dto.EntityId, uploadBasePath);

            var fileStore = new FileStore
            {
                EntityTypeId = dto.EntityTypeId,
                EntityId = dto.EntityId,
                FileCategory = dto.FileCategory,
                FileName = fileName,
                FilePath = filePath,
                MimeType = dto.File.ContentType,
                FileSizeBytes = dto.File.Length,
                FileUse = dto.FileUse,
                FileExpiryDate = dto.FileExpiryDate,
                DocumentName = dto.DocumentName,
                DocumentNumber = dto.DocumentNumber,
                DocumentTypeId = dto.DocumentTypeId,
                IssuedById = dto.IssuedById,
                IssuedByDate = dto.IssuedByDate,
                DocumentValidTill = dto.DocumentValidTill,
                CreatedAt = DateTime.UtcNow
            };

            _db.FileStores.Add(fileStore);
            _db.SaveChangesAsync().GetAwaiter().GetResult();

            return MapToDto(fileStore, entityType);
        }

        public List<FileStoreResponseDto> GetFilesByEntity(Guid entityTypeId, Guid entityId, string? fileCategory = null)
        {
            var query = _db.FileStores
                .Include(f => f.EntityType)
                .Include(f => f.DocumentType)
                .Include(f => f.IssuedBy)
                .Where(f => f.EntityTypeId == entityTypeId && f.EntityId == entityId && !f.IsDeleted);

            if (!string.IsNullOrWhiteSpace(fileCategory))
                query = query.Where(f => f.FileCategory == fileCategory);

            return query.OrderByDescending(f => f.CreatedAt)
                .ToList()
                .Select(f => MapToDto(f, f.EntityType)).ToList();
        }

        public FileStoreResponseDto? GetFileById(Guid fileId)
        {
            var file = _db.FileStores
                .Include(f => f.EntityType)
                .Include(f => f.DocumentType)
                .Include(f => f.IssuedBy)
                .FirstOrDefault(f => f.Id == fileId && !f.IsDeleted);

            return file == null ? null : MapToDto(file, file.EntityType);
        }

        public FileStoreResponseDto? UpdateFile(Guid fileId, FileStoreUpdateDto dto, string uploadBasePath)
        {
            var file = _db.FileStores
                .Include(f => f.EntityType)
                .Include(f => f.DocumentType)
                .Include(f => f.IssuedBy)
                .FirstOrDefault(f => f.Id == fileId && !f.IsDeleted);

            if (file == null) return null;

            if (dto.File != null)
            {
                if (!string.IsNullOrWhiteSpace(file.FilePath))
                {
                    var oldPath = Path.Combine(uploadBasePath, file.FilePath.Replace("/", "\\"));
                    if (File.Exists(oldPath)) File.Delete(oldPath);
                }

                var (fileName, filePath) = SaveFileToDisk(dto.File, file.EntityType.EntityName, file.EntityId, uploadBasePath);
                file.FileName = fileName;
                file.FilePath = filePath;
                file.MimeType = dto.File.ContentType;
                file.FileSizeBytes = dto.File.Length;
            }

            file.FileUse = dto.FileUse;
            file.FileExpiryDate = dto.FileExpiryDate;
            file.DocumentName = dto.DocumentName;
            file.DocumentNumber = dto.DocumentNumber;
            file.DocumentTypeId = dto.DocumentTypeId;
            file.IssuedById = dto.IssuedById;
            file.IssuedByDate = dto.IssuedByDate;
            file.DocumentValidTill = dto.DocumentValidTill;
            file.UpdatedAt = DateTime.UtcNow;

            _db.SaveChangesAsync().GetAwaiter().GetResult();
            return MapToDto(file, file.EntityType);
        }

        public bool SoftDeleteFile(Guid fileId, string deletedBy)
        {
            var file = _db.FileStores.Find(fileId);
            if (file == null || file.IsDeleted) return false;

            file.IsDeleted = true;
            file.DeletedBy = deletedBy;
            file.DeletedAt = DateTime.UtcNow;
            _db.SaveChangesAsync().GetAwaiter().GetResult();
            return true;
        }

        public bool HardDeleteFile(Guid fileId)
        {
            var file = _db.FileStores.FirstOrDefault(f => f.Id == fileId);
            if (file == null) return false;

            _db.FileStores.Remove(file);
            _db.SaveChangesAsync().GetAwaiter().GetResult();
            return true;
        }

        private static FileStoreResponseDto MapToDto(FileStore f, EntityType et) => new()
        {
            Id = f.Id,
            EntityTypeId = f.EntityTypeId,
            EntityTypeName = et.EntityName,
            EntityId = f.EntityId,
            FileCategory = f.FileCategory,
            FileName = f.FileName,
            FilePath = f.FilePath,
            MimeType = f.MimeType,
            FileSizeBytes = f.FileSizeBytes,
            FileUse = f.FileUse,
            FileExpiryDate = f.FileExpiryDate,
            DocumentName = f.DocumentName,
            DocumentNumber = f.DocumentNumber,
            DocumentTypeId = f.DocumentTypeId,
            DocumentTypeName = f.DocumentType?.TypeName,
            IssuedById = f.IssuedById,
            IssuedByName = f.IssuedBy?.IssuedByName,
            IssuedByDate = f.IssuedByDate,
            DocumentValidTill = f.DocumentValidTill,
            CreatedAt = f.CreatedAt
        };
    }
}