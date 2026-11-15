// =====================================================================
//  AppDbContext - مطابق لسكربت BuildingManagement-Schema.sql
// =====================================================================
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>
/// Entity Framework Core database context for the building management schema.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Gets the users.
    /// </summary>
    public DbSet<UserEntity> Users => Set<UserEntity>();
    /// <summary>
    /// Gets the user permissions.
    /// </summary>
    public DbSet<UserPermissionEntity> UserPermissions => Set<UserPermissionEntity>();
    /// <summary>
    /// Gets the buildings.
    /// </summary>
    public DbSet<BuildingEntity> Buildings => Set<BuildingEntity>();
    /// <summary>
    /// Gets the building settings.
    /// </summary>
    public DbSet<BuildingSettingsEntity> BuildingSettings => Set<BuildingSettingsEntity>();
    /// <summary>
    /// Gets the building admins.
    /// </summary>
    public DbSet<BuildingAdminEntity> BuildingAdmins => Set<BuildingAdminEntity>();
    /// <summary>
    /// Gets the floors.
    /// </summary>
    public DbSet<FloorEntity> Floors => Set<FloorEntity>();
    /// <summary>
    /// Gets the apartments.
    /// </summary>
    public DbSet<ApartmentEntity> Apartments => Set<ApartmentEntity>();
    /// <summary>
    /// Gets the financial categories.
    /// </summary>
    public DbSet<FinancialCategoryEntity> FinancialCategories => Set<FinancialCategoryEntity>();
    /// <summary>
    /// Gets the expenses.
    /// </summary>
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();
    /// <summary>
    /// Gets the revenues.
    /// </summary>
    public DbSet<RevenueEntity> Revenues => Set<RevenueEntity>();
    /// <summary>
    /// Gets the monthly apartment shares.
    /// </summary>
    public DbSet<MonthlyApartmentShareEntity> MonthlyApartmentShares => Set<MonthlyApartmentShareEntity>();
    /// <summary>
    /// Gets the deposits.
    /// </summary>
    public DbSet<DepositEntity> Deposits => Set<DepositEntity>();
    /// <summary>
    /// Gets the deposit edit history.
    /// </summary>
    public DbSet<DepositEditEntity> DepositEditHistory => Set<DepositEditEntity>();
    /// <summary>
    /// Gets the wallet adjustments.
    /// </summary>
    public DbSet<WalletAdjustmentEntity> WalletAdjustments => Set<WalletAdjustmentEntity>();
    // ❌ محذوف: DbSet<ReceiptEntity> Receipts
    /// <summary>
    /// Gets the audit log.
    /// </summary>
    public DbSet<AuditLogEntity> AuditLog => Set<AuditLogEntity>();
    /// <summary>
    /// Gets the notifications.
    /// </summary>
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    /// <summary>
    /// Gets the polls.
    /// </summary>
    public DbSet<PollEntity> Polls => Set<PollEntity>();
    /// <summary>
    /// Gets the poll options.
    /// </summary>
    public DbSet<PollOptionEntity> PollOptions => Set<PollOptionEntity>();
    /// <summary>
    /// Gets the poll votes.
    /// </summary>
    public DbSet<PollVoteEntity> PollVotes => Set<PollVoteEntity>();
    /// <summary>
    /// Gets the maintenance records.
    /// </summary>
    public DbSet<MaintenanceRecordEntity> MaintenanceRecords => Set<MaintenanceRecordEntity>();
    /// <summary>
    /// Gets the QR tokens.
    /// </summary>
    public DbSet<QrTokenEntity> QrTokens => Set<QrTokenEntity>();
    /// <summary>
    /// Gets the QR usages.
    /// </summary>
    public DbSet<QrUsageEntity> QrUsages => Set<QrUsageEntity>();   // ✅ جديد
    /// <summary>
    /// Gets the presence.
    /// </summary>
    public DbSet<PresenceEntity> Presence => Set<PresenceEntity>();
    /// <summary>
    /// Gets the global settings.
    /// </summary>
    public DbSet<GlobalSettingsEntity> GlobalSettings => Set<GlobalSettingsEntity>();
    /// <summary>
    /// Gets the scheduled backup settings.
    /// </summary>
    public DbSet<ScheduledBackupSettingsEntity> ScheduledBackupSettings => Set<ScheduledBackupSettingsEntity>();
    /// <summary>
    /// Gets the backup history.
    /// </summary>
    public DbSet<BackupHistoryEntity> BackupHistory => Set<BackupHistoryEntity>();
    /// <summary>
    /// Gets the apartment wallet movements.
    /// </summary>
    public DbSet<ApartmentWalletMovementsView> ApartmentWalletMovements => Set<ApartmentWalletMovementsView>();
    /// <summary>
    /// Gets the sync pending changes.
    /// </summary>
    public DbSet<SyncPendingChangeEntity> SyncPendingChanges => Set<SyncPendingChangeEntity>();
    /// <summary>
    /// Gets the dvrs.
    /// </summary>
    public DbSet<DvrEntity> Dvrs => Set<DvrEntity>();
    /// <summary>
    /// Gets the cameras.
    /// </summary>
    public DbSet<CameraEntity> Cameras => Set<CameraEntity>();
    /// <summary>
    /// Gets the residents.
    /// </summary>
    public DbSet<ResidentEntity> Residents => Set<ResidentEntity>();
    protected override void ConfigureConventions(ModelConfigurationBuilder cb)
    {
        cb.Properties<decimal>().HavePrecision(18, 2);
        cb.Properties<decimal?>().HavePrecision(18, 2);
        cb.Properties<DateTime>().HaveColumnType("datetime2(0)");
        cb.Properties<DateTime?>().HaveColumnType("datetime2(0)");
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ---------------- المستخدمين ----------------
        mb.Entity<UserEntity>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Uid);
        });

        mb.Entity<UserPermissionEntity>(e =>
        {
            e.ToTable("UserPermissions");
            e.HasKey(x => new { x.Uid, x.Permission });
            e.HasOne(x => x.User).WithMany(u => u.Permissions)
                .HasForeignKey(x => x.Uid).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- العمارات ----------------
        mb.Entity<BuildingEntity>(e =>
        {
            e.ToTable("Buildings");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.BuildingNumber).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        mb.Entity<BuildingSettingsEntity>(e =>
        {
            e.ToTable("BuildingSettings");
            e.HasKey(x => x.BuildingId);
            e.HasOne(x => x.Building).WithOne(b => b.Settings)
                .HasForeignKey<BuildingSettingsEntity>(x => x.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<BuildingAdminEntity>(e =>
        {
            e.ToTable("BuildingAdmins");
            e.HasKey(x => new { x.BuildingId, x.AdminUid });
            e.HasOne(x => x.Building).WithMany(b => b.Admins)
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.BuildingAdmins)
                .HasForeignKey(x => x.AdminUid).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<FloorEntity>(e =>
        {
            e.ToTable("Floors");
            e.HasKey(x => new { x.BuildingId, x.Id });
            e.HasOne(x => x.Building).WithMany(b => b.Floors)
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ApartmentEntity>(e =>
        {
            e.ToTable("Apartments");
            e.HasKey(x => new { x.BuildingId, x.Id });
            e.HasIndex(x => new { x.BuildingId, x.Number }).IsUnique();
            e.HasOne(x => x.Building).WithMany(b => b.Apartments)
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Floor).WithMany(f => f.Apartments)
                .HasForeignKey(x => new { x.BuildingId, x.FloorId }).OnDelete(DeleteBehavior.NoAction);
        });

        // ---------------- المالية ----------------
        mb.Entity<FinancialCategoryEntity>(e =>
        {
            e.ToTable("FinancialCategories");
            e.HasKey(x => new { x.BuildingId, x.Kind, x.Id });
            e.HasOne(x => x.Building).WithMany(b => b.Categories)
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ExpenseEntity>(e =>
        {
            e.ToTable("Expenses");
            e.HasKey(x => new { x.BuildingId, x.Id });
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany()
                .HasForeignKey(x => new { x.BuildingId, x.CategoryKind, x.CategoryId }).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<RevenueEntity>(e =>
        {
            e.ToTable("Revenues");
            e.HasKey(x => new { x.BuildingId, x.Id });
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany()
                .HasForeignKey(x => new { x.BuildingId, x.CategoryKind, x.CategoryId }).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<MonthlyApartmentShareEntity>(e =>
        {
            e.ToTable("MonthlyApartmentShares");
            e.HasKey(x => new { x.BuildingId, x.MonthKey, x.ApartmentId });
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Apartment).WithMany()
                .HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<DepositEntity>(e =>
        {
            e.ToTable("Deposits");
            e.HasKey(x => new { x.BuildingId, x.Id });
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Apartment).WithMany()
                .HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<DepositEditEntity>(e =>
        {
            e.ToTable("DepositEditHistory");
            e.HasKey(x => x.EditId);
            e.Property(x => x.EditId).ValueGeneratedOnAdd();
            e.HasOne(x => x.Deposit).WithMany(d => d.EditHistory)
                .HasForeignKey(x => new { x.BuildingId, x.DepositId }).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<WalletAdjustmentEntity>(e =>
        {
            e.ToTable("WalletAdjustments");
            e.HasKey(x => new { x.BuildingId, x.Id });
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Apartment).WithMany()
                .HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
        });

        // ❌ محذوف: mb.Entity<ReceiptEntity>(...)

        // ---------------- السجل والإشعارات ----------------
        mb.Entity<AuditLogEntity>(e =>
        {
            e.ToTable("AuditLog");
            e.HasKey(x => x.AuditId);
            e.Property(x => x.AuditId).ValueGeneratedOnAdd();
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.BuildingId, x.Ts });
            e.HasIndex(x => new { x.ApartmentId, x.Ts });
            e.Property(x => x.ApartmentId).HasMaxLength(64);
            e.Property(x => x.Severity).HasMaxLength(20).HasDefaultValue("info");
        });

        mb.Entity<NotificationEntity>(e =>
        {
            e.ToTable("Notifications");
            e.HasKey(x => new { x.BuildingId, x.Id });
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RecipientApartment).WithMany()
                .HasForeignKey(x => new { x.BuildingId, x.RecipientApartmentId })
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ---------------- التصويت ----------------
        mb.Entity<PollEntity>(e =>
        {
            e.ToTable("Polls");
            e.HasKey(x => new { x.BuildingId, x.Id });
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<PollOptionEntity>(e =>
        {
            e.ToTable("PollOptions");
            e.HasKey(x => new { x.BuildingId, x.PollId, x.Id });
            e.HasOne(x => x.Poll).WithMany(p => p.Options)
                .HasForeignKey(x => new { x.BuildingId, x.PollId }).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<PollVoteEntity>(e =>
        {
            e.ToTable("PollVotes");
            e.HasKey(x => new { x.BuildingId, x.PollId, x.ApartmentId });
            e.HasOne(x => x.Option).WithMany(o => o.Votes)
                .HasForeignKey(x => new { x.BuildingId, x.PollId, x.OptionId })
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Apartment).WithMany()
                .HasForeignKey(x => new { x.BuildingId, x.ApartmentId })
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ---------------- الصيانة ----------------
        mb.Entity<MaintenanceRecordEntity>(e =>
        {
            e.ToTable("MaintenanceRecords");
            e.HasKey(x => new { x.BuildingId, x.Id });
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- QR والتواجد ----------------
        mb.Entity<QrTokenEntity>(e =>
        {
            e.ToTable("QrTokens");
            e.HasKey(x => x.Uid);
            e.Property(x => x.ExpiresAt).HasColumnType("datetime2(3)");
            e.Property(x => x.IssuedAt).HasColumnType("datetime2(3)");
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Apartment).WithMany()
                .HasForeignKey(x => new { x.BuildingId, x.ApartmentId }).OnDelete(DeleteBehavior.NoAction);
        });

        // ✅ جديد: QrUsages
        mb.Entity<QrUsageEntity>(e =>
        {
            e.ToTable("QrUsages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(128);
            e.Property(x => x.TokenId).HasMaxLength(128);
            e.HasIndex(x => x.TokenId);
            e.HasIndex(x => x.CreatedAt);
        });

        mb.Entity<PresenceEntity>(e =>
        {
            e.ToTable("Presence");
            e.HasKey(x => x.Uid);
            e.Property(x => x.LastSeen).HasColumnType("datetime2(3)");
        });

        // ---------------- الإعدادات والنسخ الاحتياطي ----------------
        mb.Entity<GlobalSettingsEntity>(e =>
        {
            e.ToTable("GlobalSettings");
            e.HasKey(x => x.Id);
        });

        mb.Entity<ScheduledBackupSettingsEntity>(e =>
        {
            e.ToTable("ScheduledBackupSettings");
            e.HasKey(x => x.Id);
        });

        mb.Entity<BackupHistoryEntity>(e =>
        {
            e.ToTable("BackupHistory");
            e.HasKey(x => x.Id);
        });

        // ---------------- View ----------------
        mb.Entity<ApartmentWalletMovementsView>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_ApartmentWalletMovements");
        });

        mb.Entity<SyncPendingChangeEntity>(e =>
        {
            e.ToTable("SyncPendingChanges");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.Applied, x.CreatedAt });
            e.HasIndex(x => new { x.EntityType, x.EntityId });
        });

        mb.Entity<DvrEntity>(e =>
        {
            e.ToTable("Dvrs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.BuildingId);
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<CameraEntity>(e =>
        {
            e.ToTable("Cameras");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.DvrId, x.Channel }).IsUnique();
            e.HasIndex(x => x.BuildingId);
            e.HasOne(x => x.Dvr).WithMany(d => d.Cameras)
                .HasForeignKey(x => x.DvrId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        // ═══════════════════════════════════════════════════════════
        // ✅ Residents — جدول الساكنين الجديد
        // ═══════════════════════════════════════════════════════════
        mb.Entity<ResidentEntity>(e =>
        {
            e.ToTable("Residents");
            e.HasKey(x => x.Id);

            e.HasIndex(x => x.BuildingId);
            e.HasIndex(x => x.ApartmentId);
            e.HasIndex(x => x.Phone);
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => new { x.BuildingId, x.Phone });

            // Foreign Key للـ Apartment
            e.HasOne(x => x.Apartment)
                .WithMany()
                .HasForeignKey(x => new { x.BuildingId, x.ApartmentId })
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}