using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>EF mappings for durable operational presence records.</summary>
internal static class OperationalModelConfiguration
{
    internal static void Configure(ModelBuilder mb)
    {
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
    }
}
