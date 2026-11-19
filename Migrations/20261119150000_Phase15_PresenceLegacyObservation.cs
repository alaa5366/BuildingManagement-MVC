using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119150000_Phase15_PresenceLegacyObservation")]
public sealed class Phase15_PresenceLegacyObservation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.PresenceSessions', N'U') IS NULL
    THROW 51015, 'Phase09 PresenceSessions table is missing.', 1;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.PresenceSessions')
      AND name = N'StartedAtUtc' AND is_nullable = 0
)
    ALTER TABLE dbo.PresenceSessions ALTER COLUMN StartedAtUtc datetime2(3) NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PresenceSessions')
      AND name = N'IX_PresenceSessions_LegacyUid'
)
    CREATE INDEX IX_PresenceSessions_LegacyUid
        ON dbo.PresenceSessions(LegacyUid);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive: do not make previously unknown session start times mandatory.
    }
}
