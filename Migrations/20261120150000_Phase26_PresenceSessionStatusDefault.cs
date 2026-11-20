using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261120150000_Phase26_PresenceSessionStatusDefault")]
public sealed class Phase26_PresenceSessionStatusDefault : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // EF marks Status as database-generated with default 0. The original operational
        // schema omitted the SQL default, causing heartbeat inserts to omit Status and fail.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.PresenceSessions', N'U') IS NOT NULL
               AND NOT EXISTS (
                   SELECT 1
                   FROM sys.default_constraints AS dc
                   INNER JOIN sys.columns AS c
                       ON c.object_id = dc.parent_object_id
                      AND c.column_id = dc.parent_column_id
                   WHERE dc.parent_object_id = OBJECT_ID(N'dbo.PresenceSessions')
                     AND c.name = N'Status'
               )
            BEGIN
                ALTER TABLE dbo.PresenceSessions
                    ADD CONSTRAINT DF_PresenceSessions_Status_Phase26 DEFAULT (0) FOR Status;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.PresenceSessions', N'U') IS NOT NULL
               AND EXISTS (
                   SELECT 1 FROM sys.default_constraints
                   WHERE parent_object_id = OBJECT_ID(N'dbo.PresenceSessions')
                     AND name = N'DF_PresenceSessions_Status_Phase26'
               )
            BEGIN
                ALTER TABLE dbo.PresenceSessions
                    DROP CONSTRAINT DF_PresenceSessions_Status_Phase26;
            END;
            """);
    }
}
