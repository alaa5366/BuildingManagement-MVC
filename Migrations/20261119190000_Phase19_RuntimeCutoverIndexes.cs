using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119190000_Phase19_RuntimeCutoverIndexes")]
public sealed class Phase19_RuntimeCutoverIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.PresenceSessions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PresenceSessions') AND name = N'IX_PresenceSessions_Building_Apartment_LastSeen')
    CREATE INDEX IX_PresenceSessions_Building_Apartment_LastSeen
        ON dbo.PresenceSessions(BuildingId, ApartmentId, LastSeenAtUtc DESC);

IF OBJECT_ID(N'dbo.PresenceSessions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PresenceSessions') AND name = N'IX_PresenceSessions_LegacyUid_Status_LastSeen')
    CREATE INDEX IX_PresenceSessions_LegacyUid_Status_LastSeen
        ON dbo.PresenceSessions(LegacyUid, Status, LastSeenAtUtc DESC);

IF OBJECT_ID(N'dbo.PresenceHeartbeats', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PresenceHeartbeats') AND name = N'IX_PresenceHeartbeats_Session_Observed')
    CREATE INDEX IX_PresenceHeartbeats_Session_Observed
        ON dbo.PresenceHeartbeats(PresenceSessionId, ObservedAtUtc DESC);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.PresenceHeartbeats', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PresenceHeartbeats') AND name = N'IX_PresenceHeartbeats_Session_Observed')
    DROP INDEX IX_PresenceHeartbeats_Session_Observed ON dbo.PresenceHeartbeats;
IF OBJECT_ID(N'dbo.PresenceSessions', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PresenceSessions') AND name = N'IX_PresenceSessions_LegacyUid_Status_LastSeen')
    DROP INDEX IX_PresenceSessions_LegacyUid_Status_LastSeen ON dbo.PresenceSessions;
IF OBJECT_ID(N'dbo.PresenceSessions', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PresenceSessions') AND name = N'IX_PresenceSessions_Building_Apartment_LastSeen')
    DROP INDEX IX_PresenceSessions_Building_Apartment_LastSeen ON dbo.PresenceSessions;
""");
    }
}
