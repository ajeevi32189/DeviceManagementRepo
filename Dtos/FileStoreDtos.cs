namespace DeviceManagementOnly.Dtos
{
    public class ImageUploadDto
    {
        public IFormFile? File { get; set; }
        /// <summary>"Front", "Back", "Label", "Warranty"</summary>
        public string? FileUse { get; set; }
        public DateTime? FileExpiryDate { get; set; }
    }

    public class DocumentUploadDto
    {
        public IFormFile? File { get; set; }
        public string? DocumentName { get; set; }
        public string? DocumentNumber { get; set; }
        public Guid? DocumentTypeId { get; set; }
        public Guid? IssuedById { get; set; }
        public DateTime? IssuedByDate { get; set; }
        public DateTime? DocumentValidTill { get; set; }
    }

    public class FileStoreUploadDto
    {
        public IFormFile File { get; set; } = null!;
        public Guid EntityTypeId { get; set; }
        public Guid EntityId { get; set; }
        /// <summary>"Image" ya "Document"</summary>
        public string FileCategory { get; set; } = null!;

        // Image fields
        public string? FileUse { get; set; }
        public DateTime? FileExpiryDate { get; set; }

        // Document fields
        public string? DocumentName { get; set; }
        public string? DocumentNumber { get; set; }
        public Guid? DocumentTypeId { get; set; }
        public Guid? IssuedById { get; set; }
        public DateTime? IssuedByDate { get; set; }
        public DateTime? DocumentValidTill { get; set; }
    }

    public class FileStoreUpdateDto
    {
        public IFormFile? File { get; set; }
        public string? FileUse { get; set; }
        public DateTime? FileExpiryDate { get; set; }
        public string? DocumentName { get; set; }
        public string? DocumentNumber { get; set; }
        public Guid? DocumentTypeId { get; set; }
        public Guid? IssuedById { get; set; }
        public DateTime? IssuedByDate { get; set; }
        public DateTime? DocumentValidTill { get; set; }
    }

    public class FileStoreResponseDto
    {
        public Guid Id { get; set; }
        public Guid EntityTypeId { get; set; }
        public string EntityTypeName { get; set; } = null!;
        public Guid EntityId { get; set; }
        public string FileCategory { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string? MimeType { get; set; }
        public long? FileSizeBytes { get; set; }
        // Image
        public string? FileUse { get; set; }
        public DateTime? FileExpiryDate { get; set; }
        // Document
        public string? DocumentName { get; set; }
        public string? DocumentNumber { get; set; }
        public Guid? DocumentTypeId { get; set; }
        public string? DocumentTypeName { get; set; }
        public Guid? IssuedById { get; set; }
        public string? IssuedByName { get; set; }
        public DateTime? IssuedByDate { get; set; }
        public DateTime? DocumentValidTill { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // EntityType DTOs
    public class EntityTypeResponseDto
    {
        public Guid Id { get; set; }
        public string EntityName { get; set; } = null!;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
    }

    public class EntityTypeCreateDto
    {
        public string EntityName { get; set; } = null!;
        public string? DisplayName { get; set; }
    }

    public class EntityTypeUpdateDto
    {
        public string EntityName { get; set; } = null!;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
    }

    // Document Master DTOs
    public class DocumentTypeDto
    {
        public Guid Id { get; set; }
        public string TypeName { get; set; } = null!;
    }

    public class DocumentIssuedByDto
    {
        public Guid Id { get; set; }
        public string IssuedByName { get; set; } = null!;
    }
}