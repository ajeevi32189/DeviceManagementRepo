using DeviceManagement.Data;
using DeviceManagementOnly.Dtos;
using DeviceManagementOnly.Models.other;
using Microsoft.EntityFrameworkCore;

namespace DeviceManagementOnly.Services
{
    /// <summary>
    /// Persists the "Excel/CSV column header -> our backend field" mapping the
    /// user sets up once in the UI, so subsequent imports of the same sheet
    /// layout are applied automatically without asking again.
    /// </summary>
    public interface IFieldMappingService
    {
        Task<Dictionary<string, string>> GetMappingAsync(string entityName, Guid? companyId);
        Task SaveMappingAsync(SaveFieldMappingRequestDto dto, string? updatedBy);
    }

    public class FieldMappingService : IFieldMappingService
    {
        private readonly DBContext _db;
        public FieldMappingService(DBContext db) => _db = db;

        public async Task<Dictionary<string, string>> GetMappingAsync(string entityName, Guid? companyId)
        {
            var rows = await _db.ImportFieldMappings
                .AsNoTracking()
                .Where(m => m.EntityName == entityName && m.CompanyId == companyId)
                .ToListAsync();

            return rows.ToDictionary(r => r.SourceHeader, r => r.TargetField, StringComparer.OrdinalIgnoreCase);
        }

        public async Task SaveMappingAsync(SaveFieldMappingRequestDto dto, string? updatedBy)
        {
            var existing = await _db.ImportFieldMappings
                .Where(m => m.EntityName == dto.EntityName && m.CompanyId == dto.CompanyId)
                .ToListAsync();

            foreach (var entry in dto.Mappings)
            {
                var match = existing.FirstOrDefault(m =>
                    m.SourceHeader.Equals(entry.SourceHeader, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    match.TargetField = entry.TargetField;
                    match.UpdatedAt = DateTime.UtcNow;
                    match.UpdatedBy = updatedBy;
                }
                else
                {
                    _db.ImportFieldMappings.Add(new ImportFieldMapping
                    {
                        EntityName = dto.EntityName,
                        CompanyId = dto.CompanyId,
                        SourceHeader = entry.SourceHeader,
                        TargetField = entry.TargetField,
                        CreatedBy = updatedBy,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
