using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>EF mappings for durable operational presence records.</summary>
internal static class OperationalModelConfiguration
{
    internal static void Configure(ModelBuilder mb)
    {
        mb.Entity<PresenceSessionEntity>(e =>
        {
            e.ToTable("PresenceSessions", t =>
                t.HasCheckConstraint("CK_PresenceSessions_Status", "[Status] IN (0,1,2)"));
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
            e.HasIndex(x => new { x.BuildingId, x.ApartmentId, x.LastSeenAtUtc });
            e.HasIndex(x => new { x.LegacyUid, x.Status, x.LastSeenAtUtc });
            e.HasMany(x => x.Heartbeats).WithOne(x => x.Session)
                .HasForeignKey(x => x.PresenceSessionId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<PresenceHeartbeatEntity>(e =>
        {
            e.ToTable("PresenceHeartbeats");
            e.HasKey(x => x.HeartbeatId);
            e.Property(x => x.HeartbeatId).UseIdentityColumn();
            e.Property(x => x.ObservedAtUtc).HasColumnType("datetime2(3)");
            e.HasIndex(x => new { x.PresenceSessionId, x.ObservedAtUtc });
        });
    }
}
