using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>EF mappings for durable operational presence records.</summary>
internal static class OperationalModelConfiguration
{
    internal static void Configure(ModelBuilder mb)
    {
        mb.Entity<SavedWhatsAppTemplateEntity>(e =>
        {
            e.ToTable("SavedWhatsAppTemplates", t =>
                t.HasCheckConstraint("CK_SavedWhatsAppTemplates_Name", "LEN(LTRIM(RTRIM([Name]))) > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Message).HasMaxLength(4000).IsRequired();
            e.Property(x => x.CreatedByUid).HasMaxLength(128);
            e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => new { x.BuildingId, x.IsActive, x.Name })
                .HasDatabaseName("IX_SavedWhatsAppTemplates_Building_Active_Name");
        });

        mb.Entity<GlobalApplicationSettingsEntity>(e =>
        {
            e.ToTable("GlobalApplicationSettings", table =>
            {
                table.HasCheckConstraint("CK_GlobalApplicationSettings_SessionTimeout", "[SessionTimeoutDays] BETWEEN 1 AND 365");
                table.HasCheckConstraint("CK_GlobalApplicationSettings_PinAttempts", "[MaxPinAttempts] BETWEEN 1 AND 100");
            });
            e.HasKey(item => item.SettingsId);
            e.Property(item => item.SettingsId).HasMaxLength(64).ValueGeneratedNever();
            e.Property(item => item.AppName).HasMaxLength(200).IsRequired();
            e.Property(item => item.DefaultLanguage).HasMaxLength(16).IsRequired();
            e.Property(item => item.StorageMode).HasMaxLength(32).IsRequired();
            e.Property(item => item.SessionTimeoutDays).HasDefaultValue(7);
            e.Property(item => item.MaxPinAttempts).HasDefaultValue(5);
            e.Property(item => item.UpdatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.UpdatedByReference).HasMaxLength(160);
            e.Property(item => item.ImportedAtUtc).HasColumnType("datetime2(3)")
                .HasDefaultValueSql("SYSUTCDATETIME()");
        });

        mb.Entity<ProtectedIntegrationSecretEntity>(e =>
        {
            e.ToTable("ProtectedIntegrationSecrets");
            e.HasKey(item => item.SecretId);
            e.Property(item => item.SecretId).ValueGeneratedNever();
            e.Property(item => item.ScopeReference).HasMaxLength(160).IsRequired();
            e.Property(item => item.Provider).HasMaxLength(100).IsRequired();
            e.Property(item => item.SecretName).HasMaxLength(160).IsRequired();
            e.Property(item => item.Ciphertext).IsRequired();
            e.Property(item => item.KeyVersion).HasMaxLength(100).IsRequired();
            e.Property(item => item.CreatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.RotatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.RevokedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(item => new { item.ScopeType, item.ScopeReference, item.Provider, item.SecretName })
                .IsUnique().HasDatabaseName("UQ_ProtectedIntegrationSecrets_Scope");
        });

        mb.Entity<BackupJobEntity>(e =>
        {
            e.ToTable("BackupJobs");
            e.HasKey(item => item.BackupJobId);
            e.Property(item => item.BackupJobId).ValueGeneratedNever();
            e.Property(item => item.RequestedByReference).HasMaxLength(160);
            e.Property(item => item.DatabaseReference).HasMaxLength(200).IsRequired();
            e.Property(item => item.SafeErrorCode).HasMaxLength(100);
            e.Property(item => item.MetadataJson).HasColumnType("nvarchar(max)");
            e.Property(item => item.StartedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.CompletedAtUtc).HasColumnType("datetime2(3)");
        });

        mb.Entity<BackupArtifactEntity>(e =>
        {
            e.ToTable("BackupArtifacts", table => table.HasCheckConstraint("CK_BackupArtifacts_Size", "[SizeBytes] >= 0"));
            e.HasKey(item => item.BackupArtifactId);
            e.Property(item => item.BackupArtifactId).ValueGeneratedNever();
            e.Property(item => item.StorageReference).HasMaxLength(500).IsRequired();
            e.Property(item => item.Sha256).HasColumnType("varbinary(32)").IsRequired();
            e.Property(item => item.SizeBytes).IsRequired();
            e.Property(item => item.CreatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.RetainUntilUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.EncryptionKeyVersion).HasMaxLength(100);
        });

        mb.Entity<BackupRetentionEventEntity>(e =>
        {
            e.ToTable("BackupRetentionEvents");
            e.HasKey(item => item.RetentionEventId);
            e.Property(item => item.RetentionEventId).ValueGeneratedNever();
            e.Property(item => item.OccurredAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.ActorReference).HasMaxLength(160);
            e.Property(item => item.Reason).HasMaxLength(1000);
            e.HasOne<BackupArtifactEntity>().WithMany()
                .HasForeignKey(item => item.BackupArtifactId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<RestoreOperationEntity>(e =>
        {
            e.ToTable("RestoreOperations");
            e.HasKey(item => item.RestoreOperationId);
            e.Property(item => item.RestoreOperationId).ValueGeneratedNever();
            e.Property(item => item.TargetDatabase).HasMaxLength(200).IsRequired();
            e.Property(item => item.ApprovedByReference).HasMaxLength(160);
            e.Property(item => item.SafeErrorCode).HasMaxLength(100);
            e.Property(item => item.RequestedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.ApprovedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.StartedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.CompletedAtUtc).HasColumnType("datetime2(3)");
            e.HasOne<BackupArtifactEntity>().WithMany()
                .HasForeignKey(item => item.BackupArtifactId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<BackupScheduleConfigurationEntity>(e =>
        {
            e.ToTable("BackupScheduleConfigurations", table =>
            {
                table.HasCheckConstraint("CK_BackupScheduleConfigurations_Hour", "[ScheduledHour] BETWEEN 0 AND 23");
                table.HasCheckConstraint("CK_BackupScheduleConfigurations_DayOfWeek", "[DayOfWeek] IS NULL OR [DayOfWeek] BETWEEN 0 AND 6");
                table.HasCheckConstraint("CK_BackupScheduleConfigurations_DayOfMonth", "[DayOfMonth] IS NULL OR [DayOfMonth] BETWEEN 1 AND 31");
                table.HasCheckConstraint("CK_BackupScheduleConfigurations_Retention", "[MaxBackupsToKeep] BETWEEN 1 AND 30");
            });
            e.HasKey(item => item.ConfigurationId);
            e.Property(item => item.ConfigurationId).ValueGeneratedNever();
            e.Property(item => item.LegacySettingId).HasMaxLength(20).IsRequired();
            e.Property(item => item.Frequency).HasMaxLength(20).IsRequired();
            e.Property(item => item.CollectionsJson).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(item => item.LastRunStatus).HasMaxLength(30);
            e.Property(item => item.UpdatedByReference).HasMaxLength(128);
            e.Property(item => item.LastRunAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.UpdatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.ImportedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(item => item.LegacySettingId).IsUnique()
                .HasDatabaseName("UQ_BackupScheduleConfigurations_Legacy");
        });

        mb.Entity<MediaCredentialReferenceEntity>(e =>
        {
            e.ToTable("MediaCredentialReferences");
            e.HasKey(item => item.CredentialReferenceId);
            e.Property(item => item.CredentialReferenceId).ValueGeneratedNever();
            e.Property(item => item.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(item => item.DvrLegacyId).HasMaxLength(128);
            e.Property(item => item.VaultProvider).HasMaxLength(80).IsRequired();
            e.Property(item => item.VaultSecretReference).HasMaxLength(500).IsRequired();
            e.Property(item => item.KeyVersion).HasMaxLength(100).IsRequired();
            e.Property(item => item.CreatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.RotatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.RevokedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(item => item.DvrLegacyId).IsUnique()
                .HasDatabaseName("UQ_MediaCredentialReferences_Legacy");
        });

        mb.Entity<MediaProvisioningOperationEntity>(e =>
        {
            e.ToTable("MediaProvisioningOperations");
            e.HasKey(item => item.OperationId);
            e.Property(item => item.OperationId).ValueGeneratedNever();
            e.Property(item => item.DvrLegacyId).HasMaxLength(128);
            e.Property(item => item.SafeErrorCode).HasMaxLength(100);
            e.Property(item => item.RequestedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.CompletedAtUtc).HasColumnType("datetime2(3)");
            e.HasOne<MediaCredentialReferenceEntity>().WithMany()
                .HasForeignKey(item => item.CredentialReferenceId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<MediaAccessSessionEntity>(e =>
        {
            e.ToTable("MediaAccessSessions");
            e.HasKey(item => item.MediaAccessSessionId);
            e.Property(item => item.MediaAccessSessionId).ValueGeneratedNever();
            e.Property(item => item.CameraLegacyId).HasMaxLength(128);
            e.Property(item => item.SessionReference).HasMaxLength(200).IsRequired();
            e.Property(item => item.IssuedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.ExpiresAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.RevokedAtUtc).HasColumnType("datetime2(3)");
        });

        mb.Entity<MediaDeviceEntity>(e =>
        {
            e.ToTable("MediaDevices");
            e.HasKey(item => item.MediaDeviceId);
            e.Property(item => item.MediaDeviceId).ValueGeneratedNever();
            e.Property(item => item.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(item => item.Name).HasMaxLength(160).IsRequired();
            e.Property(item => item.HostAddress).HasMaxLength(255).IsRequired();
            e.Property(item => item.Brand).HasMaxLength(80).IsRequired();
            e.Property(item => item.Username).HasMaxLength(160).IsRequired();
            e.Property(item => item.CreatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.UpdatedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(item => new { item.BuildingId, item.HostAddress, item.Port })
                .IsUnique().HasDatabaseName("UQ_MediaDevices_Building_Host_Port");
        });

        mb.Entity<MediaDeviceChannelEntity>(e =>
        {
            e.ToTable("MediaDeviceChannels");
            e.HasKey(item => item.ChannelId);
            e.Property(item => item.ChannelId).ValueGeneratedNever();
            e.Property(item => item.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(item => item.Name).HasMaxLength(160).IsRequired();
            e.Property(item => item.RtspPath).HasMaxLength(500).IsRequired();
            e.Property(item => item.CreatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.UpdatedAtUtc).HasColumnType("datetime2(3)");
            e.HasOne<MediaDeviceEntity>().WithMany()
                .HasForeignKey(item => item.MediaDeviceId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(item => new { item.MediaDeviceId, item.ChannelNumber })
                .IsUnique().HasDatabaseName("UQ_MediaDeviceChannels_Device_Channel");
        });

        mb.Entity<AuditEventEntity>(e =>
        {
            e.ToTable("AuditEvents");
            e.HasKey(x => x.AuditEventId);
            e.Property(x => x.AuditEventId).ValueGeneratedNever();
            e.Property(x => x.ActorReference).HasMaxLength(160);
            e.Property(x => x.BuildingId).HasMaxLength(64);
            e.Property(x => x.EventCategory).HasMaxLength(80).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(120).IsRequired();
            e.Property(x => x.ResourceType).HasMaxLength(100);
            e.Property(x => x.ResourceReference).HasMaxLength(200);
            e.Property(x => x.CorrelationId).HasMaxLength(100);
            e.HasIndex(x => new { x.CorrelationId, x.OccurredAtUtc, x.AuditEventId })
                .IsDescending(false, true, true)
                .HasDatabaseName("IX_AuditEvents_Correlation_Time");
            e.Property(x => x.OccurredAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.Outcome).HasDefaultValue((byte)0);
            e.HasIndex(x => new { x.OccurredAtUtc, x.AuditEventId })
                .HasDatabaseName("IX_AuditEvents_OccurredAt");
            e.HasIndex(x => new { x.ResourceType, x.ResourceReference, x.OccurredAtUtc })
                .HasDatabaseName("IX_AuditEvents_Resource");
            e.HasIndex(x => new { x.BuildingId, x.EventCategory, x.ResourceType, x.OccurredAtUtc, x.AuditEventId })
                .IsDescending(false, false, false, true, true)
                .HasDatabaseName("IX_AuditEvents_Building_Category_Resource_Time");
        });

        mb.Entity<QrCapabilityEntity>(e =>
        {
            e.ToTable("QrCapabilities");
            e.HasKey(item => item.CapabilityId);
            e.Property(item => item.CapabilityId).ValueGeneratedNever();
            e.Property(item => item.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(item => item.ApartmentId).HasMaxLength(64);
            e.Property(item => item.TargetReference).HasMaxLength(160).IsRequired();
            e.Property(item => item.TokenHash).HasColumnType("varbinary(64)").IsRequired();
            e.Property(item => item.IssuedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.ExpiresAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.RevokedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.RevokedByReference).HasMaxLength(160);
            e.HasIndex(item => item.TokenHash).IsUnique().HasDatabaseName("UQ_QrCapabilities_Token");
            e.HasIndex(item => new { item.BuildingId, item.ApartmentId, item.ExpiresAtUtc })
                .HasDatabaseName("IX_QrCapabilities_Target_Expiry");
        });

        mb.Entity<QrCapabilityUseEntity>(e =>
        {
            e.ToTable("QrCapabilityUses");
            e.HasKey(item => item.UseId);
            e.Property(item => item.UseId).ValueGeneratedNever();
            e.Property(item => item.UsedAtUtc).HasColumnType("datetime2(3)");
            e.Property(item => item.UseReference).HasMaxLength(160);
            e.HasOne<QrCapabilityEntity>().WithMany()
                .HasForeignKey(item => item.CapabilityId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(item => new { item.CapabilityId, item.UsedAtUtc })
                .HasDatabaseName("IX_QrCapabilityUses_Capability_UsedAt");
        });

        mb.Entity<PresenceSessionEntity>(e =>
        {
            e.ToTable("PresenceSessions");
            e.HasKey(x => x.PresenceSessionId);
            e.Property(x => x.PresenceSessionId).ValueGeneratedNever();
            e.Property(x => x.LegacyUid).HasMaxLength(128);
            e.Property(x => x.BuildingId).HasMaxLength(64);
            e.Property(x => x.ApartmentId).HasMaxLength(64);
            e.Property(x => x.DeviceReference).HasMaxLength(200);
            e.Property(x => x.Status).HasDefaultValue((byte)0);
            e.Property(x => x.StartedAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.LastSeenAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.EndedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.BuildingId, x.ApartmentId, x.LastSeenAtUtc })
                .HasDatabaseName("IX_PresenceSessions_Building_Apartment_LastSeen");
            e.HasIndex(x => new { x.LegacyUid, x.Status, x.LastSeenAtUtc })
                .HasDatabaseName("IX_PresenceSessions_LegacyUid_Status_LastSeen");
            e.HasMany(x => x.Heartbeats).WithOne(x => x.Session)
                .HasForeignKey(x => x.PresenceSessionId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<PresenceHeartbeatEntity>(e =>
        {
            e.ToTable("PresenceHeartbeats");
            e.HasKey(x => x.HeartbeatId);
            e.Property(x => x.HeartbeatId).UseIdentityColumn();
            e.Property(x => x.ObservedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.PresenceSessionId, x.ObservedAtUtc })
                .HasDatabaseName("IX_PresenceHeartbeats_Session_Observed");
        });

        mb.Entity<MaintenanceRequestEntity>(e =>
        {
            e.ToTable("MaintenanceRequests", t =>
            {
                t.HasCheckConstraint("CK_MaintenanceRequests_Status", "[Status] BETWEEN 0 AND 8");
                t.HasCheckConstraint("CK_MaintenanceRequests_Priority", "[Priority] BETWEEN 0 AND 4");
            });
            e.HasKey(x => x.RequestId);
            e.Property(x => x.RequestId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.ApartmentId).HasMaxLength(64);
            e.Property(x => x.LegacyMaintenanceId).HasMaxLength(128);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Category).HasMaxLength(80);
            e.Property(x => x.RequestedByReference).HasMaxLength(160);
            e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.ClosedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.BuildingId, x.CreatedAtUtc }).HasDatabaseName("IX_MaintenanceRequests_Building_Created");
            e.HasIndex(x => new { x.BuildingId, x.LegacyMaintenanceId }).IsUnique().HasFilter("[LegacyMaintenanceId] IS NOT NULL").HasDatabaseName("UX_MaintenanceRequests_Legacy");
            e.HasMany(x => x.Assignments).WithOne(x => x.Request).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Comments).WithOne(x => x.Request).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Attachments).WithOne(x => x.Request).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.StatusEvents).WithOne(x => x.Request).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.LedgerLinks).WithOne(x => x.Request).HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<MaintenanceAssignmentEntity>(e =>
        {
            e.ToTable("MaintenanceAssignments");
            e.HasKey(x => x.AssignmentId);
            e.Property(x => x.AssignmentId).ValueGeneratedNever();
            e.Property(x => x.AssigneeReference).HasMaxLength(160).IsRequired();
            e.Property(x => x.VendorName).HasMaxLength(200);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.AssignedAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.CompletedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.RequestId, x.AssignedAtUtc });
        });

        mb.Entity<MaintenanceCommentEntity>(e =>
        {
            e.ToTable("MaintenanceComments");
            e.HasKey(x => x.CommentId);
            e.Property(x => x.CommentId).ValueGeneratedNever();
            e.Property(x => x.AuthorReference).HasMaxLength(160);
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.RequestId, x.CreatedAtUtc });
        });

        mb.Entity<MaintenanceAttachmentEntity>(e =>
        {
            e.ToTable("MaintenanceAttachments");
            e.HasKey(x => x.AttachmentId);
            e.Property(x => x.AttachmentId).ValueGeneratedNever();
            e.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(120);
            e.Property(x => x.Sha256).HasColumnType("varbinary(32)");
            e.Property(x => x.UploadedByReference).HasMaxLength(160);
            e.Property(x => x.UploadedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.RequestId, x.UploadedAtUtc });
        });

        mb.Entity<MaintenanceStatusEventEntity>(e =>
        {
            e.ToTable("MaintenanceStatusEvents");
            e.HasKey(x => x.StatusEventId);
            e.Property(x => x.StatusEventId).ValueGeneratedNever();
            e.Property(x => x.ActorReference).HasMaxLength(160);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.OccurredAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.RequestId, x.OccurredAtUtc });
        });

        mb.Entity<MaintenanceLedgerLinkEntity>(e =>
        {
            e.ToTable("MaintenanceLedgerLinks");
            e.HasKey(x => x.LinkId);
            e.Property(x => x.LinkId).ValueGeneratedNever();
            e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.RequestId, x.LedgerTransactionId }).IsUnique();
        });

        mb.Entity<PollVotingPolicyEntity>(e =>
        {
            e.ToTable("PollVotingPolicies", t => t.HasCheckConstraint("CK_PollVotingPolicies_Mode", "[VotingMode] IN (0,1,2) AND [EligibilityMode] IN (0,1,2)"));
            e.HasKey(x => x.PolicyId);
            e.Property(x => x.PolicyId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.PollId).HasMaxLength(64).IsRequired();
            e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.HasAlternateKey(x => new { x.BuildingId, x.PollId, x.PolicyVersion }).HasName("UQ_PollVotingPolicies_PollVersion");
        });

        mb.Entity<PollEligibilityEntity>(e =>
        {
            e.ToTable("PollEligibility", t => t.HasCheckConstraint("CK_PollEligibility_Expiry", "[ExpiresAtUtc] > [IssuedAtUtc]"));
            e.HasKey(x => x.EligibilityId);
            e.Property(x => x.EligibilityId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.PollId).HasMaxLength(64).IsRequired();
            e.Property(x => x.SubjectReference).HasMaxLength(160).IsRequired();
            e.Property(x => x.CredentialHash).HasColumnType("varbinary(64)");
            e.Property(x => x.IssuedAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.ConsumedAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.RevokedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.BuildingId, x.PollId, x.SubjectReference }).IsUnique()
                .HasDatabaseName("UX_PollEligibility_Subject");
        });

        mb.Entity<PollBallotEntity>(e =>
        {
            e.ToTable("PollBallots");
            e.HasKey(x => x.BallotId);
            e.Property(x => x.BallotId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.PollId).HasMaxLength(64).IsRequired();
            e.Property(x => x.BallotTokenHash).HasColumnType("varbinary(64)").IsRequired();
            e.Property(x => x.ChoicePayload).HasColumnType("varbinary(max)").IsRequired();
            e.Property(x => x.CastAtUtc).HasColumnType("datetime2(3)");
            e.HasAlternateKey(x => x.BallotTokenHash).HasName("UQ_PollBallots_Token");
            e.HasIndex(x => new { x.BuildingId, x.PollId, x.CastAtUtc }).HasDatabaseName("IX_PollBallots_Building_Poll_CastAt");
        });

        mb.Entity<PollLifecycleEventEntity>(e =>
        {
            e.ToTable("PollLifecycleEvents");
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64).IsRequired();
            e.Property(x => x.PollId).HasMaxLength(64).IsRequired();
            e.Property(x => x.ActorReference).HasMaxLength(160);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
            e.Property(x => x.OccurredAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.BuildingId, x.PollId, x.OccurredAtUtc }).HasDatabaseName("IX_PollLifecycleEvents_LegacyPoll");
        });

        mb.Entity<NotificationMessageEntity>(e =>
        {
            e.ToTable("NotificationMessages");
            e.HasKey(x => x.MessageId);
            e.Property(x => x.MessageId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64);
            e.Property(x => x.LegacyNotificationId).HasMaxLength(128);
            e.Property(x => x.Title).HasMaxLength(250);
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Origin).HasMaxLength(80).IsRequired();
            e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.BuildingId, x.CreatedAtUtc, x.MessageId })
                .IsDescending(false, true, true)
                .HasDatabaseName("IX_NotificationMessages_Building_Created");
            e.HasIndex(x => x.LegacyNotificationId).IsUnique()
                .HasFilter("[LegacyNotificationId] IS NOT NULL")
                .HasDatabaseName("UX_NotificationMessages_Legacy");
            e.HasMany(x => x.Recipients).WithOne(x => x.Message)
                .HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<NotificationRecipientEntity>(e =>
        {
            e.ToTable("NotificationRecipients");
            e.HasKey(x => x.RecipientId);
            e.Property(x => x.RecipientId).ValueGeneratedNever();
            e.Property(x => x.RecipientReference).HasMaxLength(160).IsRequired();
            e.Property(x => x.ApartmentId).HasMaxLength(64);
            e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.RecipientReference, x.CreatedAtUtc });
            e.HasOne(x => x.ReadState).WithOne(x => x.Recipient)
                .HasForeignKey<NotificationReadStateEntity>(x => x.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<NotificationDeliveryEntity>(e =>
        {
            e.ToTable("NotificationDeliveries");
            e.HasKey(x => x.DeliveryId);
            e.Property(x => x.DeliveryId).ValueGeneratedNever();
            e.Property(x => x.Provider).HasMaxLength(80).IsRequired();
            e.Property(x => x.ProviderReference).HasMaxLength(200);
            e.Property(x => x.SafeErrorCode).HasMaxLength(100);
            e.Property(x => x.AttemptedAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.CompletedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.RecipientId, x.AttemptNumber }).IsUnique();
        });

        mb.Entity<NotificationReadStateEntity>(e =>
        {
            e.ToTable("NotificationReadStates");
            e.HasKey(x => x.ReadStateId);
            e.Property(x => x.ReadStateId).ValueGeneratedNever();
            e.Property(x => x.ReadAtUtc).HasColumnType("datetime2(3)");
            e.Property(x => x.DismissedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => x.RecipientId).IsUnique();
        });

        mb.Entity<NotificationPreferenceEntity>(e =>
        {
            e.ToTable("NotificationPreferences");
            e.HasKey(x => x.PreferenceId);
            e.Property(x => x.PreferenceId).ValueGeneratedNever();
            e.Property(x => x.SubjectReference).HasMaxLength(160).IsRequired();
            e.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.SubjectType, x.SubjectReference, x.Channel }).IsUnique();
        });
    }
}
