using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119230000_Phase20_AggregateAuditQueryIndex")]
public sealed class Phase20_AggregateAuditQueryIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuditEvents') AND name = N'IX_AuditEvents_Building_Category_Resource_Time')
    CREATE INDEX IX_AuditEvents_Building_Category_Resource_Time
        ON dbo.AuditEvents(BuildingId, EventCategory, ResourceType, OccurredAtUtc DESC, AuditEventId DESC)
        INCLUDE (ResourceReference, ActorReference, Outcome);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuditEvents') AND name = N'IX_AuditEvents_Building_Category_Resource_Time')
    DROP INDEX IX_AuditEvents_Building_Category_Resource_Time ON dbo.AuditEvents;
""");
    }
}
