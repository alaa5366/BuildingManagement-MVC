using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261120010000_Phase22_AuditCorrelationIndex")]
public sealed class Phase22_AuditCorrelationIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuditEvents') AND name = N'IX_AuditEvents_Correlation_Time')
    CREATE INDEX IX_AuditEvents_Correlation_Time
        ON dbo.AuditEvents(CorrelationId, OccurredAtUtc DESC, AuditEventId DESC)
        INCLUDE (BuildingId, EventCategory, EventType, Outcome);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuditEvents') AND name = N'IX_AuditEvents_Correlation_Time')
    DROP INDEX IX_AuditEvents_Correlation_Time ON dbo.AuditEvents;
""");
    }
}
