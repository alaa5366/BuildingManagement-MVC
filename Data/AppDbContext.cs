﻿// =====================================================================
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
        if (ChangeTracker.Entries<AuditEventEntity>()
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


    /// <summary>Gets buildings.</summary>
    public DbSet<BuildingEntity> Buildings => Set<BuildingEntity>();
    /// <summary>Gets building settings.</summary>
    public DbSet<BuildingSettingsEntity> BuildingSettings => Set<BuildingSettingsEntity>();
    /// <summary>Gets floors.</summary>
    public DbSet<FloorEntity> Floors => Set<FloorEntity>();
    /// <summary>Gets apartments.</summary>
    public DbSet<ApartmentEntity> Apartments => Set<ApartmentEntity>();
    /// <summary>Gets financial categories.</summary>
    public DbSet<FinancialCategoryEntity> FinancialCategories => Set<FinancialCategoryEntity>();
    /// <summary>Gets saved WhatsApp templates.</summary>
    public DbSet<SavedWhatsAppTemplateEntity> SavedWhatsAppTemplates => Set<SavedWhatsAppTemplateEntity>();

    /// <summary>Gets target poll definitions.</summary>
    public DbSet<PollEntity> PollDefinitions => Set<PollEntity>();
    /// <summary>Gets target poll choice options.</summary>
    public DbSet<PollOptionEntity> PollChoiceOptions => Set<PollOptionEntity>();
    public DbSet<PollVotingPolicyEntity> PollVotingPolicies => Set<PollVotingPolicyEntity>();
    public DbSet<PollEligibilityEntity> PollEligibility => Set<PollEligibilityEntity>();
    public DbSet<PollBallotEntity> PollBallots => Set<PollBallotEntity>();
    public DbSet<PollLifecycleEventEntity> PollLifecycleEvents => Set<PollLifecycleEventEntity>();

    public DbSet<MaintenanceRequestEntity> MaintenanceRequests => Set<MaintenanceRequestEntity>();
    public DbSet<MaintenanceAssignmentEntity> MaintenanceAssignments => Set<MaintenanceAssignmentEntity>();
    public DbSet<MaintenanceCommentEntity> MaintenanceComments => Set<MaintenanceCommentEntity>();
    public DbSet<MaintenanceAttachmentEntity> MaintenanceAttachments => Set<MaintenanceAttachmentEntity>();
    public DbSet<MaintenanceStatusEventEntity> MaintenanceStatusEvents => Set<MaintenanceStatusEventEntity>();
    public DbSet<MaintenanceLedgerLinkEntity> MaintenanceLedgerLinks => Set<MaintenanceLedgerLinkEntity>();
    public DbSet<QrCapabilityEntity> QrCapabilities => Set<QrCapabilityEntity>();
    public DbSet<QrCapabilityUseEntity> QrCapabilityUses => Set<QrCapabilityUseEntity>();
    public DbSet<GlobalApplicationSettingsEntity> GlobalApplicationSettings => Set<GlobalApplicationSettingsEntity>();
    public DbSet<ProtectedIntegrationSecretEntity> ProtectedIntegrationSecrets => Set<ProtectedIntegrationSecretEntity>();
    public DbSet<ApartmentWalletMovementsView> ApartmentWalletMovements => Set<ApartmentWalletMovementsView>();

    public DbSet<PresenceSessionEntity> PresenceSessions => Set<PresenceSessionEntity>();
    public DbSet<PresenceHeartbeatEntity> PresenceHeartbeats => Set<PresenceHeartbeatEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();
    public DbSet<BackupJobEntity> BackupJobs => Set<BackupJobEntity>();
    public DbSet<BackupArtifactEntity> BackupArtifacts => Set<BackupArtifactEntity>();
    public DbSet<BackupRetentionEventEntity> BackupRetentionEvents => Set<BackupRetentionEventEntity>();
    public DbSet<RestoreOperationEntity> RestoreOperations => Set<RestoreOperationEntity>();
    public DbSet<BackupScheduleConfigurationEntity> BackupScheduleConfigurations => Set<BackupScheduleConfigurationEntity>();
    public DbSet<MediaCredentialReferenceEntity> MediaCredentialReferences => Set<MediaCredentialReferenceEntity>();
    public DbSet<MediaProvisioningOperationEntity> MediaProvisioningOperations => Set<MediaProvisioningOperationEntity>();
    public DbSet<MediaAccessSessionEntity> MediaAccessSessions => Set<MediaAccessSessionEntity>();
    public DbSet<MediaDeviceEntity> MediaDevices => Set<MediaDeviceEntity>();
    public DbSet<MediaDeviceChannelEntity> MediaDeviceChannels => Set<MediaDeviceChannelEntity>();
    public DbSet<NotificationMessageEntity> NotificationMessages => Set<NotificationMessageEntity>();
    public DbSet<NotificationRecipientEntity> NotificationRecipients => Set<NotificationRecipientEntity>();
    public DbSet<NotificationDeliveryEntity> NotificationDeliveries => Set<NotificationDeliveryEntity>();
    public DbSet<NotificationReadStateEntity> NotificationReadStates => Set<NotificationReadStateEntity>();
    public DbSet<NotificationPreferenceEntity> NotificationPreferences => Set<NotificationPreferenceEntity>();

    // Canonical identity and authorization foundation.
    public DbSet<PersonEntity> Persons => Set<PersonEntity>();
    public DbSet<UserAccountEntity> UserAccounts => Set<UserAccountEntity>();
    public DbSet<UserPreferenceEntity> UserPreferences => Set<UserPreferenceEntity>();
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

        mb.Entity<BuildingEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<BuildingSettingsEntity>().Property(x => x.BuildingId).HasMaxLength(64);

        mb.Entity<FloorEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<FloorEntity>().Property(x => x.Id).HasMaxLength(64);

        mb.Entity<ApartmentEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<ApartmentEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<ApartmentEntity>().Property(x => x.FloorId).HasMaxLength(64);

        mb.Entity<FinancialCategoryEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<FinancialCategoryEntity>().Property(x => x.Id).HasMaxLength(64);
        mb.Entity<FinancialCategoryEntity>().Property(x => x.Kind).HasMaxLength(10);

        mb.Entity<SavedWhatsAppTemplateEntity>().Property(x => x.BuildingId).HasMaxLength(64);

        mb.Entity<PollEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<PollEntity>().Property(x => x.Id).HasMaxLength(64);

        mb.Entity<PollOptionEntity>().Property(x => x.BuildingId).HasMaxLength(64);
        mb.Entity<PollOptionEntity>().Property(x => x.PollId).HasMaxLength(64);
        mb.Entity<PollOptionEntity>().Property(x => x.Id).HasMaxLength(64);

        // ---------------- المستخدمين ----------------
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





// â‌Œ ظ…ط­ط°ظˆظپ: mb.Entity<ReceiptEntity>(...)

        // ---------------- السجل والإشعارات ----------------

// ---------------- التصويت ----------------
        mb.Entity<PollEntity>(e =>
        {
            e.ToTable("PollDefinitions");
            e.HasKey(x => new { x.BuildingId, x.Id });
            e.Property(x => x.BuildingId).HasMaxLength(64);
            e.Property(x => x.Id).HasMaxLength(128);
            e.Property(x => x.Question).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.CreatedBy).HasMaxLength(160);
            e.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
            e.Property(x => x.Deadline).HasColumnType("datetime2(3)");
            e.HasOne<BuildingEntity>().WithMany()
                .HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<PollOptionEntity>(e =>
        {
            e.ToTable("PollChoiceOptions");
            e.HasKey(x => new { x.BuildingId, x.PollId, x.Id });
            e.Property(x => x.BuildingId).HasMaxLength(64);
            e.Property(x => x.PollId).HasMaxLength(128);
            e.Property(x => x.Id).HasMaxLength(128);
            e.Property(x => x.Text).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Poll).WithMany(p => p.Options)
                .HasForeignKey(x => new { x.BuildingId, x.PollId }).OnDelete(DeleteBehavior.Cascade);
        });
// ---------------- الصيانة ----------------
// ---------------- QR والتواجد ----------------
// âœ… ط¬ط¯ظٹط¯: QrUsages

// ---------------- الإعدادات والنسخ الاحتياطي ----------------


// ---------------- View ----------------
        mb.Entity<ApartmentWalletMovementsView>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_ApartmentWalletMovements");
        });
// ═══════════════════════════════════════════════════════════
        // ═══════════════════════════════════════════════════════════
}
}
