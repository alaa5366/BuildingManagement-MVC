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

    private void EnsureAppendOnlyAndPostedFinanceIsImmutable()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries<AuditLogEntity>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<AuditEventEntity>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "Business audit events are append-only and cannot be updated or deleted by the application.");
        }

        if (ChangeTracker.Entries<InvoiceIssueEntity>()
            .Any(entry => entry.State == EntityState.Deleted ||
                entry.State == EntityState.Modified &&
                !string.IsNullOrWhiteSpace(entry.Property(x => x.SnapshotJson).OriginalValue)))
            throw new InvalidOperationException("Issued invoices are immutable; reprints must use the stored invoice snapshot.");

        if (ChangeTracker.Entries<LedgerEntryEntity>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Ledger entries are immutable; corrections require reversal transactions.");

        if (ChangeTracker.Entries<LedgerTransactionEntity>()
            .Any(entry => (entry.State is EntityState.Modified or EntityState.Deleted) &&
                          entry.Property(x => x.Status).OriginalValue == LedgerTransactionStatus.Posted))
            throw new InvalidOperationException("Posted ledger transactions are immutable; corrections require reversal transactions.");
    }

    /// <summary>
    /// Saves changes while enforcing append-only audit records.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureAppendOnlyAndPostedFinanceIsImmutable();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Saves changes asynchronously while enforcing append-only audit records.
    /// </summary>
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureAppendOnlyAndPostedFinanceIsImmutable();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

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
    /// <summary>Saved building-scoped WhatsApp message templates.</summary>
    public DbSet<SavedWhatsAppTemplateEntity> SavedWhatsAppTemplates => Set<SavedWhatsAppTemplateEntity>();
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
    public DbSet<MaintenanceRequestEntity> MaintenanceRequests => Set<MaintenanceRequestEntity>();
    public DbSet<MaintenanceAssignmentEntity> MaintenanceAssignments => Set<MaintenanceAssignmentEntity>();
    public DbSet<MaintenanceCommentEntity> MaintenanceComments => Set<MaintenanceCommentEntity>();
    public DbSet<MaintenanceAttachmentEntity> MaintenanceAttachments => Set<MaintenanceAttachmentEntity>();
    public DbSet<MaintenanceStatusEventEntity> MaintenanceStatusEvents => Set<MaintenanceStatusEventEntity>();
    public DbSet<MaintenanceLedgerLinkEntity> MaintenanceLedgerLinks => Set<MaintenanceLedgerLinkEntity>();
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

    public DbSet<PresenceSessionEntity> PresenceSessions => Set<PresenceSessionEntity>();
    public DbSet<PresenceHeartbeatEntity> PresenceHeartbeats => Set<PresenceHeartbeatEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();
    public DbSet<NotificationMessageEntity> NotificationMessages => Set<NotificationMessageEntity>();
    public DbSet<NotificationRecipientEntity> NotificationRecipients => Set<NotificationRecipientEntity>();
    public DbSet<NotificationDeliveryEntity> NotificationDeliveries => Set<NotificationDeliveryEntity>();
    public DbSet<NotificationReadStateEntity> NotificationReadStates => Set<NotificationReadStateEntity>();
    public DbSet<NotificationPreferenceEntity> NotificationPreferences => Set<NotificationPreferenceEntity>();

    // Canonical identity and authorization foundation (Gate A).
    public DbSet<PersonEntity> Persons => Set<PersonEntity>();
    public DbSet<UserAccountEntity> UserAccounts => Set<UserAccountEntity>();
    public DbSet<UserCredentialEntity> UserCredentials => Set<UserCredentialEntity>();
    public DbSet<UserTwoFactorSettingEntity> UserTwoFactorSettings => Set<UserTwoFactorSettingEntity>();
    public DbSet<UserRecoveryCodeEntity> UserRecoveryCodes => Set<UserRecoveryCodeEntity>();
    public DbSet<RoleDefinitionEntity> RoleDefinitions => Set<RoleDefinitionEntity>();
    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();
    public DbSet<UserRoleAssignmentEntity> UserRoleAssignments => Set<UserRoleAssignmentEntity>();
    public DbSet<ApartmentOwnershipEntity> ApartmentOwnerships => Set<ApartmentOwnershipEntity>();
    public DbSet<ApartmentOccupancyEntity> ApartmentOccupancies => Set<ApartmentOccupancyEntity>();
    public DbSet<ApartmentAccessGrantEntity> ApartmentAccessGrants => Set<ApartmentAccessGrantEntity>();
    public DbSet<AccountPermissionOverrideEntity> AccountPermissionOverrides => Set<AccountPermissionOverrideEntity>();
    public DbSet<LegacyIdentityMapEntity> LegacyIdentityMaps => Set<LegacyIdentityMapEntity>();
    public DbSet<PersonPhoneChangeRequestEntity> PersonPhoneChangeRequests => Set<PersonPhoneChangeRequestEntity>();

    public DbSet<FinancialPeriodEntity> FinancialPeriods => Set<FinancialPeriodEntity>();
    public DbSet<PaymentDepositEntity> PaymentDeposits => Set<PaymentDepositEntity>();
    public DbSet<LedgerTransactionEntity> LedgerTransactions => Set<LedgerTransactionEntity>();
    public DbSet<LedgerEntryEntity> LedgerEntries => Set<LedgerEntryEntity>();
    public DbSet<FinancialSourceLinkEntity> FinancialSourceLinks => Set<FinancialSourceLinkEntity>();
    public DbSet<FinancialAllocationBatchEntity> FinancialAllocationBatches => Set<FinancialAllocationBatchEntity>();
    public DbSet<FinancialAllocationLineEntity> FinancialAllocationLines => Set<FinancialAllocationLineEntity>();
    public DbSet<FinancialReconciliationRunEntity> FinancialReconciliationRuns => Set<FinancialReconciliationRunEntity>();
    public DbSet<FinancialReconciliationDifferenceEntity> FinancialReconciliationDifferences => Set<FinancialReconciliationDifferenceEntity>();
    public DbSet<InvoiceIssueEntity> InvoiceIssues => Set<InvoiceIssueEntity>();
    public DbSet<LanguageEntity> Languages => Set<LanguageEntity>();
    public DbSet<TranslationEntryEntity> TranslationEntries => Set<TranslationEntryEntity>();
    public DbSet<TranslationValueEntity> TranslationValues => Set<TranslationValueEntity>();
    protected override void ConfigureConventions(ModelConfigurationBuilder cb)
    {
        cb.Properties<decimal>().HavePrecision(18, 2);
        cb.Properties<decimal?>().HavePrecision(18, 2);
        cb.Properties<DateTime>().HaveColumnType("datetime2(0)");
        cb.Properties<DateTime?>().HaveColumnType("datetime2(0)");
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        IdentityModelConfiguration.Configure(mb);
        mb.Entity<LanguageEntity>(e => { e.ToTable("Languages"); e.HasKey(x => x.CultureCode); e.Property(x => x.CultureCode).HasMaxLength(20); e.Property(x => x.EnglishName).HasMaxLength(100).IsRequired(); e.Property(x => x.NativeName).HasMaxLength(100).IsRequired(); e.Property(x => x.Direction).HasMaxLength(12).IsRequired(); e.Property(x => x.FallbackCulture).HasMaxLength(20); e.Property(x => x.Flag).HasMaxLength(8); e.Property(x => x.UpdatedBy).HasMaxLength(128); });
        mb.Entity<TranslationEntryEntity>(e => { e.ToTable("TranslationEntries"); e.HasKey(x => x.Key); e.Property(x => x.Key).HasMaxLength(200); e.Property(x => x.Category).HasMaxLength(40).IsRequired(); e.Property(x => x.SourceGroup).HasMaxLength(160).IsRequired(); e.Property(x => x.Description).HasMaxLength(500); e.Property(x => x.UpdatedBy).HasMaxLength(128); e.Property(x => x.RowVersion).IsRowVersion(); e.HasIndex(x => new { x.Category, x.SourceGroup }); });
        mb.Entity<TranslationValueEntity>(e => { e.ToTable("TranslationValues"); e.HasKey(x => new { x.Key, x.CultureCode }); e.Property(x => x.Key).HasMaxLength(200); e.Property(x => x.CultureCode).HasMaxLength(20); e.Property(x => x.Value).HasMaxLength(4000).IsRequired(); e.Property(x => x.UpdatedBy).HasMaxLength(128); e.HasOne<TranslationEntryEntity>().WithMany().HasForeignKey(x => x.Key).OnDelete(DeleteBehavior.Cascade); e.HasOne<LanguageEntity>().WithMany().HasForeignKey(x => x.CultureCode).OnDelete(DeleteBehavior.Restrict); });
        FinanceModelConfiguration.Configure(mb);
        OperationalModelConfiguration.Configure(mb);

        // -----------------------------------------------------------------
        // Phase 04: Explicit SQL Server lengths for PK/FK and key columns.
        // Keep every FK component aligned with its principal key column.
        // -----------------------------------------------------------------
        mb.Entity<UserEntity>().Property(x => x.Uid).HasMaxLength(128);
        mb.Entity<UserEntity>().Property(x => x.TwoFactorEnabled).HasDefaultValue(false);
        mb.Entity<UserEntity>().Property(x => x.TwoFactorSecretEncrypted).HasMaxLength(512);
        mb.Entity<UserEntity>().Property(x => x.TwoFactorRecoveryCodesJson).HasMaxLength(4000);

        mb.Entity<UserPermissionEntity>().Property(x => x.Uid).HasMaxLength(128);
        mb.Entity<UserPermissionEntity>().Property(x => x.Permission).HasMaxLength(60);

        mb.Entity<BuildingEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<BuildingSettingsEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<BuildingAdminEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<BuildingAdminEntity>().Property(x => x.AdminUid).HasMaxLength(128);

        mb.Entity<FloorEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<FloorEntity>().Property(x => x.Id).HasMaxLength(64);

        mb.Entity<ApartmentEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<ApartmentEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<ApartmentEntity>().Property(x => x.FloorId).HasMaxLength(64);

        mb.Entity<FinancialCategoryEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<FinancialCategoryEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<FinancialCategoryEntity>().Property(x => x.Kind).HasMaxLength(10);

        mb.Entity<ExpenseEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<ExpenseEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<ExpenseEntity>().Property(x => x.CategoryId).HasMaxLength(64);
        mb.Entity<ExpenseEntity>().Property(x => x.CategoryKind).HasMaxLength(10);
        mb.Entity<ExpenseEntity>().Property(x => x.MonthKey).HasColumnType("char(7)").HasMaxLength(7);

        mb.Entity<RevenueEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<RevenueEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<RevenueEntity>().Property(x => x.CategoryId).HasMaxLength(64);
        mb.Entity<RevenueEntity>().Property(x => x.CategoryKind).HasMaxLength(10);
        mb.Entity<RevenueEntity>().Property(x => x.MonthKey).HasColumnType("char(7)").HasMaxLength(7);

        mb.Entity<MonthlyApartmentShareEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<MonthlyApartmentShareEntity>().Property(x => x.ApartmentId).HasMaxLength(64);
        mb.Entity<MonthlyApartmentShareEntity>().Property(x => x.MonthKey).HasColumnType("char(7)").HasMaxLength(7);

        mb.Entity<DepositEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<DepositEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<DepositEntity>().Property(x => x.ApartmentId).HasMaxLength(64);
        mb.Entity<DepositEntity>().Property(x => x.MonthKey).HasColumnType("char(7)").HasMaxLength(7);

        mb.Entity<DepositEditEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<DepositEditEntity>().Property(x => x.DepositId).HasMaxLength(64);

        mb.Entity<WalletAdjustmentEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<WalletAdjustmentEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<WalletAdjustmentEntity>().Property(x => x.ApartmentId).HasMaxLength(64);
        mb.Entity<WalletAdjustmentEntity>().Property(x => x.MonthKey).HasColumnType("char(7)").HasMaxLength(7);

        mb.Entity<AuditLogEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<AuditLogEntity>().Property(x => x.ApartmentId).HasMaxLength(64);
        mb.Entity<AuditLogEntity>().Property(x => x.MonthKey).HasMaxLength(7);

        mb.Entity<SavedWhatsAppTemplateEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<NotificationEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<NotificationEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<NotificationEntity>().Property(x => x.RecipientApartmentId).HasMaxLength(64);

        mb.Entity<PollEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<PollEntity>().Property(x => x.Id).HasMaxLength(64);

        mb.Entity<PollOptionEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<PollOptionEntity>().Property(x => x.PollId).HasMaxLength(64);
        mb.Entity<PollOptionEntity>().Property(x => x.Id).HasMaxLength(64);

        mb.Entity<PollVoteEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<PollVoteEntity>().Property(x => x.PollId).HasMaxLength(64);
        mb.Entity<PollVoteEntity>().Property(x => x.ApartmentId).HasMaxLength(64);
        mb.Entity<PollVoteEntity>().Property(x => x.OptionId).HasMaxLength(64);

        mb.Entity<MaintenanceRecordEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<MaintenanceRecordEntity>().Property(x => x.Id).HasMaxLength(64);

        mb.Entity<QrTokenEntity>().Property(x => x.Uid).HasMaxLength(128);
        mb.Entity<QrTokenEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<QrTokenEntity>().Property(x => x.ApartmentId).HasMaxLength(64);

        mb.Entity<QrUsageEntity>().Property(x => x.Id).HasMaxLength(128);
        mb.Entity<QrUsageEntity>().Property(x => x.TokenId).HasMaxLength(128);

        mb.Entity<PresenceEntity>().Property(x => x.Uid).HasMaxLength(128);
        mb.Entity<PresenceEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<PresenceEntity>().Property(x => x.ApartmentId).HasMaxLength(64);

        mb.Entity<GlobalSettingsEntity>().Property(x => x.Id).HasMaxLength(20);
        mb.Entity<ScheduledBackupSettingsEntity>().Property(x => x.Id).HasMaxLength(20);
        mb.Entity<BackupHistoryEntity>().Property(x => x.Id).HasMaxLength(64);

        mb.Entity<DvrEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<DvrEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<CameraEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<CameraEntity>().Property(x => x.DvrId).HasMaxLength(64);
        mb.Entity<CameraEntity>().Property(x => x.BuildingId).HasMaxLength(64);

        mb.Entity<ResidentEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<ResidentEntity>().Property(x => x.Uid).HasMaxLength(128);
        mb.Entity<ResidentEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<ResidentEntity>().Property(x => x.ApartmentId).HasMaxLength(64);

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

            // BuildingId is already the leading key in the composite indexes below.
            // Keep indexes aligned with actual query patterns; avoid redundant single-column indexes.
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
