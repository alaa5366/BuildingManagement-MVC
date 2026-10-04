// =====================================================================
//  AppDbContext - مطابق لسكربت BuildingManagement-Schema.sql
//
//  الاستخدام: Database-First. السكربت هو المصدر الأساسي للسكيما
//  (فيه الـ CHECK constraints والـ defaults)، والـ DbContext ده بيعمل Mapping
//  بس. ماتشغّلش dotnet ef migrations فوق القاعدة دي من غير ما تراجع.
// =====================================================================
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<UserPermissionEntity> UserPermissions => Set<UserPermissionEntity>();
    public DbSet<BuildingEntity> Buildings => Set<BuildingEntity>();
    public DbSet<BuildingSettingsEntity> BuildingSettings => Set<BuildingSettingsEntity>();
    public DbSet<BuildingAdminEntity> BuildingAdmins => Set<BuildingAdminEntity>();
    public DbSet<FloorEntity> Floors => Set<FloorEntity>();
    public DbSet<ApartmentEntity> Apartments => Set<ApartmentEntity>();
    public DbSet<FinancialCategoryEntity> FinancialCategories => Set<FinancialCategoryEntity>();
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();
    public DbSet<RevenueEntity> Revenues => Set<RevenueEntity>();
    public DbSet<MonthlyApartmentShareEntity> MonthlyApartmentShares => Set<MonthlyApartmentShareEntity>();
    public DbSet<DepositEntity> Deposits => Set<DepositEntity>();
    public DbSet<DepositEditEntity> DepositEditHistory => Set<DepositEditEntity>();
    public DbSet<WalletAdjustmentEntity> WalletAdjustments => Set<WalletAdjustmentEntity>();
    public DbSet<ReceiptEntity> Receipts => Set<ReceiptEntity>();
    public DbSet<AuditLogEntity> AuditLog => Set<AuditLogEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<PollEntity> Polls => Set<PollEntity>();
    public DbSet<PollOptionEntity> PollOptions => Set<PollOptionEntity>();
    public DbSet<PollVoteEntity> PollVotes => Set<PollVoteEntity>();
    public DbSet<MaintenanceRecordEntity> MaintenanceRecords => Set<MaintenanceRecordEntity>();
    public DbSet<QrTokenEntity> QrTokens => Set<QrTokenEntity>();
    public DbSet<PresenceEntity> Presence => Set<PresenceEntity>();
    public DbSet<GlobalSettingsEntity> GlobalSettings => Set<GlobalSettingsEntity>();
    public DbSet<ScheduledBackupSettingsEntity> ScheduledBackupSettings => Set<ScheduledBackupSettingsEntity>();
    public DbSet<BackupHistoryEntity> BackupHistory => Set<BackupHistoryEntity>();
    public DbSet<ApartmentWalletMovementsView> ApartmentWalletMovements => Set<ApartmentWalletMovementsView>();
    public DbSet<SyncPendingChangeEntity> SyncPendingChanges => Set<SyncPendingChangeEntity>();

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

        mb.Entity<ReceiptEntity>(e =>
        {
            e.ToTable("Receipts");
            e.HasKey(x => x.ReceiptId);
            e.Property(x => x.ReceiptId).ValueGeneratedOnAdd();
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.SetNull);
        });

        // ---------------- السجل والإشعارات ----------------
        mb.Entity<AuditLogEntity>(e =>
        {
            e.ToTable("AuditLog");
            e.HasKey(x => x.AuditId);
            e.Property(x => x.AuditId).ValueGeneratedOnAdd();
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.BuildingId, x.Ts });
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
        // في OnModelCreating:
        mb.Entity<SyncPendingChangeEntity>(e =>
        {
            e.ToTable("SyncPendingChanges");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.Applied, x.CreatedAt });
            e.HasIndex(x => new { x.EntityType, x.EntityId });
        });
    }
}
