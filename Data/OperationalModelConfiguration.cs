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
