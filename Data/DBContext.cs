using DeviceManagement.Models.Report;
using DeviceManagementOnly.Models;
using DeviceManagementOnly.Models.Device;
using DeviceManagementOnly.Models.other;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DeviceManagement.Data
{
    public class DBContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private class PendingAuditEntry
        {
            public AuditLog Log { get; set; } = null!;
            public EntityEntry? EntityEntry { get; set; }
        }

        public DBContext(
            DbContextOptions<DBContext> options,
            IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // ── Device Group ──────────────────────────────────────────
        public DbSet<DeviceCategory> DeviceCategories { get; set; }
        public DbSet<DeviceType> DeviceTypes { get; set; }
        public DbSet<DeviceMaster> DeviceMasters { get; set; }

        // ── Model / Specification Group ──────────────────────────
        public DbSet<ModelCategory> ModelCategories { get; set; }
        public DbSet<ModelSpecification> ModelSpecifications { get; set; }
        public DbSet<ModelParameter> ModelParameters { get; set; }
        public DbSet<DeviceModelMapping> DeviceModelMappings { get; set; }

        // ── Device Detail (physical asset) ───────────────────────
        public DbSet<DeviceDetail> DeviceDetails { get; set; }
        public DbSet<DeviceLocation> DeviceLocations { get; set; }

        // ── Unit ──────────────────────────────────────────────────
        public DbSet<UnitMaster> UnitMasters { get; set; }

        // ── Audit ─────────────────────────────────────────────────
        public DbSet<AuditLog> AuditLogs { get; set; }
        //
        // Models add karo
        public DbSet<EntityType> EntityTypes { get; set; }
        public DbSet<FileStore> FileStores { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<DocumentIssuedBy> DocumentIssuedBies { get; set; }

        // ── Facility / Rack Group (Floor → Room → Rack) ──────────────
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Rack> Racks { get; set; }
        public DbSet<RackPowerCapacity> RackPowerCapacities { get; set; }

        // ── Device Rack Location (rack + U-position based, separate
        //    from address/lat-long based DeviceLocations) ────────────
        public DbSet<DeviceRackLocation> DeviceRackLocations { get; set; }

        // ── Device Connectivity ───────────────────────────────────────
        public DbSet<DeviceConnectionConfig> DeviceConnectionConfigs { get; set; }
        public DbSet<NetworkConnection> NetworkConnections { get; set; }

        //report
        public DbSet<ReportLog> ReportLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // DeviceType → DeviceCategory
            modelBuilder.Entity<DeviceType>()
                .HasOne<DeviceCategory>()
                .WithMany()
                .HasForeignKey(dt => dt.DeviceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // DeviceMaster → DeviceType
            modelBuilder.Entity<DeviceMaster>()
                .HasOne<DeviceType>()
                .WithMany()
                .HasForeignKey(d => d.DeviceTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            // DeviceMaster → DeviceCategory
            modelBuilder.Entity<DeviceMaster>()
                .HasOne<DeviceCategory>()
                .WithMany()
                .HasForeignKey(d => d.DeviceCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // DeviceMaster → ModelSpecification
            modelBuilder.Entity<DeviceMaster>()
                .HasOne<ModelSpecification>()
                .WithMany()
                .HasForeignKey(d => d.ModelSpecificationId)
                .OnDelete(DeleteBehavior.SetNull);

            // ModelSpecification → DeviceType
            modelBuilder.Entity<ModelSpecification>()
                .HasOne<DeviceType>()
                .WithMany()
                .HasForeignKey(ms => ms.DeviceTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            // ModelParameter → ModelSpecification (cascade delete)
            modelBuilder.Entity<ModelParameter>()
                .HasOne<ModelSpecification>()
                .WithMany()
                .HasForeignKey(mp => mp.DeviceModelId)
                .OnDelete(DeleteBehavior.Cascade);

            // ModelParameter → ModelCategory
            modelBuilder.Entity<ModelParameter>()
                .HasOne<ModelCategory>()
                .WithMany()
                .HasForeignKey(mp => mp.ModelCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // DeviceModelMapping → DeviceType
            modelBuilder.Entity<DeviceModelMapping>()
                .HasOne<DeviceType>()
                .WithMany()
                .HasForeignKey(m => m.DeviceTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // DeviceModelMapping → ModelSpecification
            modelBuilder.Entity<DeviceModelMapping>()
                .HasOne<ModelSpecification>()
                .WithMany()
                .HasForeignKey(m => m.ModelSpecificationId)
                .OnDelete(DeleteBehavior.Cascade);

            // DeviceDetail → DeviceMaster (cascade)
            modelBuilder.Entity<DeviceDetail>()
                .HasOne<DeviceMaster>()
                .WithMany()
                .HasForeignKey(d => d.DeviceMasterId)
                .OnDelete(DeleteBehavior.Cascade);

            // DeviceDetail self-reference (parent-child)
            modelBuilder.Entity<DeviceDetail>()
                .HasOne(d => d.ParentDevice)
                .WithMany(d => d.SubDevices)
                .HasForeignKey(d => d.ParentDeviceDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            // IMEI globally unique (nullable)
            modelBuilder.Entity<DeviceDetail>()
                .HasIndex(d => d.IMEI)
                .IsUnique()
                .HasFilter("IMEI IS NOT NULL");

            // SerialNumber globally unique (nullable)
            modelBuilder.Entity<DeviceDetail>()
                .HasIndex(d => d.SerialNumber)
                .IsUnique()
                .HasFilter("SerialNumber IS NOT NULL");

            // FileStore → EntityType
            modelBuilder.Entity<FileStore>()
                .HasOne(f => f.EntityType)
                .WithMany(e => e.FileStores)
                .HasForeignKey(f => f.EntityTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // FileStore → DocumentType
            modelBuilder.Entity<FileStore>()
                .HasOne(f => f.DocumentType)
                .WithMany()
                .HasForeignKey(f => f.DocumentTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            // FileStore → DocumentIssuedBy
            modelBuilder.Entity<FileStore>()
                .HasOne(f => f.IssuedBy)
                .WithMany()
                .HasForeignKey(f => f.IssuedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Index for fast queries
            modelBuilder.Entity<FileStore>()
                .HasIndex(f => new { f.EntityTypeId, f.EntityId, f.FileCategory });

            // EntityType unique name
            modelBuilder.Entity<EntityType>()
                .HasIndex(e => e.EntityName)
                .IsUnique();

            // ── Facility / Rack Group ─────────────────────────────────

            // Room → Floor (cascade — floor delete ho toh rooms bhi jayenge)
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Floor)
                .WithMany()
                .HasForeignKey(r => r.FloorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Rack → Room (cascade)
            modelBuilder.Entity<Rack>()
                .HasOne(r => r.Room)
                .WithMany()
                .HasForeignKey(r => r.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // RackPowerCapacity → Rack (cascade)
            modelBuilder.Entity<RackPowerCapacity>()
                .HasOne(rp => rp.Rack)
                .WithMany()
                .HasForeignKey(rp => rp.RackId)
                .OnDelete(DeleteBehavior.Cascade);

            // DeviceRackLocation → DeviceDetail (cascade — device delete ho toh location bhi)
            modelBuilder.Entity<DeviceRackLocation>()
                .HasOne(dl => dl.DeviceDetail)
                .WithMany()
                .HasForeignKey(dl => dl.DeviceDetailId)
                .OnDelete(DeleteBehavior.Cascade);

            // DeviceRackLocation → Rack (SetNull — rack delete ho toh location udhar se hat jaye, device na jaye)
            modelBuilder.Entity<DeviceRackLocation>()
                .HasOne(dl => dl.Rack)
                .WithMany()
                .HasForeignKey(dl => dl.RackId)
                .OnDelete(DeleteBehavior.SetNull);

            // DeviceConnectionConfig → DeviceDetail (cascade)
            modelBuilder.Entity<DeviceConnectionConfig>()
                .HasOne(c => c.DeviceDetail)
                .WithMany()
                .HasForeignKey(c => c.DeviceDetailId)
                .OnDelete(DeleteBehavior.Cascade);

            // NetworkConnection → DeviceDetail (Source) — Restrict taaki EF ka
            // "multiple cascade paths" error na aaye (Source aur Target dono
            // DeviceDetail ko point karte hain)
            modelBuilder.Entity<NetworkConnection>()
                .HasOne(n => n.SourceDeviceDetail)
                .WithMany()
                .HasForeignKey(n => n.SourceDeviceDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            // NetworkConnection → DeviceDetail (Target) — Restrict (same reason)
            modelBuilder.Entity<NetworkConnection>()
                .HasOne(n => n.TargetDeviceDetail)
                .WithMany()
                .HasForeignKey(n => n.TargetDeviceDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Data — EntityTypes
            modelBuilder.Entity<EntityType>().HasData(
                new EntityType { Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), EntityName = "DeviceCategory", DisplayName = "Device Category", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new EntityType { Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), EntityName = "DeviceType", DisplayName = "Device Type", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new EntityType { Id = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), EntityName = "DeviceMaster", DisplayName = "Device Master", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new EntityType { Id = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), EntityName = "ModelSpecification", DisplayName = "Model Specification", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new EntityType { Id = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), EntityName = "DeviceDetail", DisplayName = "Device Detail", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }

        // ── Audit Logging ─────────────────────────────────────────────

        // Entity me CreatedBy/UpdatedBy/CreatedAt/UpdatedAt columns hain toh
        // unhe automatically fill kar do — agar entity me woh property nahi hai
        // toh chup-chaap skip ho jayega, error nahi aayega.
        private void SetAuditFields(EntityEntry entry, string userName)
        {
            if (entry.State == EntityState.Added)
            {
                SetPropertyIfExists(entry, "CreatedBy", userName);
                SetPropertyIfExists(entry, "CreatedAt", DateTime.UtcNow);
            }
            else if (entry.State == EntityState.Modified)
            {
                SetPropertyIfExists(entry, "UpdatedBy", userName);
                SetPropertyIfExists(entry, "UpdatedAt", DateTime.UtcNow);
            }
        }

        private void SetPropertyIfExists(EntityEntry entry, string propName, object value)
        {
            var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propName);
            if (prop != null)
                prop.CurrentValue = value;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var pendingAudits = new List<PendingAuditEntry>();
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog) continue;
                if (entry.State == EntityState.Unchanged || entry.State == EntityState.Detached) continue;

                // ── Yahan CreatedBy/UpdatedBy set ho jaata hai ──────────
                SetAuditFields(entry, userName);

                var action = entry.State switch
                {
                    EntityState.Added => "INSERT",
                    EntityState.Modified => "UPDATE",
                    EntityState.Deleted => "DELETE",
                    _ => "UNKNOWN"
                };

                var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;

                Guid? recordId = null;
                var pk = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                if (pk?.CurrentValue is Guid g) recordId = g;

                if (entry.State == EntityState.Added)
                {
                    // INSERT me PK abhi generate nahi hua — defer karo
                    var log = new AuditLog
                    {
                        TableName = tableName,
                        ActionType = action,
                        ChangedBy = userName,
                        ChangedAt = DateTime.UtcNow
                    };
                    pendingAudits.Add(new PendingAuditEntry { Log = log, EntityEntry = entry });
                }
                else
                {
                    foreach (var prop in entry.Properties.Where(p => p.IsModified || entry.State == EntityState.Deleted))
                    {
                        var log = new AuditLog
                        {
                            TableName = tableName,
                            RecordId = recordId?.ToString(),
                            ActionType = action,
                            ColumnName = prop.Metadata.Name,
                            OldValue = prop.OriginalValue?.ToString(),
                            NewValue = prop.CurrentValue?.ToString(),
                            ChangedBy = userName,
                            ChangedAt = DateTime.UtcNow
                        };
                        AuditLogs.Add(log);
                    }
                }
            }

            var result = await base.SaveChangesAsync(cancellationToken);

            // INSERT ke baad PK mil jaati hai — ab set karo
            foreach (var pending in pendingAudits)
            {
                if (pending.EntityEntry != null)
                {
                    var pk = pending.EntityEntry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                    if (pk?.CurrentValue is Guid g) pending.Log.RecordId = g.ToString();
                }
                AuditLogs.Add(pending.Log);
            }

            if (pendingAudits.Any())
                await base.SaveChangesAsync(cancellationToken);

            return result;
        }

        public override int SaveChanges()
        {
            return SaveChangesAsync().GetAwaiter().GetResult();
        }
    }
}