using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations;

/// <summary>Adds the uniqueness guarantee required for one entitlement per poll subject.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261120140000_Phase25_PollEligibilityUniqueness")]
public sealed class Phase25_PollEligibilityUniqueness : Migration
{
    /// <summary>Creates the poll eligibility uniqueness index.</summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.PollEligibility', N'U') IS NULL
    THROW 52400, 'PollEligibility table is missing; apply Phase09 first.', 1;
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PollEligibility')
      AND name = N'UX_PollEligibility_Subject'
)
BEGIN
    IF EXISTS
    (
        SELECT BuildingId, PollId, SubjectReference
        FROM dbo.PollEligibility
        GROUP BY BuildingId, PollId, SubjectReference
        HAVING COUNT_BIG(*) > 1
    )
        THROW 52401, 'Duplicate poll eligibility subjects must be reviewed before applying this migration.', 1;

    CREATE UNIQUE INDEX UX_PollEligibility_Subject
        ON dbo.PollEligibility(BuildingId, PollId, SubjectReference);
END;

IF OBJECT_ID(N'dbo.PollBallots', N'U') IS NULL
    THROW 52402, 'PollBallots table is missing; apply Phase09 first.', 1;
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PollBallots')
      AND name = N'IX_PollBallots_Building_Poll_CastAt'
)
    CREATE INDEX IX_PollBallots_Building_Poll_CastAt
        ON dbo.PollBallots(BuildingId, PollId, CastAtUtc);
""");
    }

    /// <summary>Leaves the integrity index in place because it protects consumed voting entitlements.</summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive.
    }
}
