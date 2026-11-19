using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119220000_Phase34_SystemReviewedIdentityEvidence")]
public sealed class Phase34_SystemReviewedIdentityEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE dbo.LegacyIdentityMap DROP CONSTRAINT CK_LegacyIdentityMap_ResolvedReview;
ALTER TABLE dbo.LegacyIdentityMap DROP CONSTRAINT CK_LegacyIdentityMap_ReviewStamp;

ALTER TABLE dbo.LegacyIdentityMap WITH CHECK ADD CONSTRAINT CK_LegacyIdentityMap_ResolvedReview
CHECK
(
    [MappingStatus] NOT IN (1,2)
    OR
    (
        [ReviewedAtUtc] IS NOT NULL
        AND
        (
            [ReviewedByAccountId] IS NOT NULL
            OR [MatchMethod] IN (N'user-confirmed-separate-population', N'system-exact-source-identity')
        )
        AND [ReviewReason] IS NOT NULL
        AND LEN(LTRIM(RTRIM([ReviewReason]))) > 0
        AND [EvidenceReference] IS NOT NULL
        AND LEN(LTRIM(RTRIM([EvidenceReference]))) > 0
    )
);

ALTER TABLE dbo.LegacyIdentityMap WITH CHECK ADD CONSTRAINT CK_LegacyIdentityMap_ReviewStamp
CHECK
(
    ([ReviewedAtUtc] IS NULL AND [ReviewedByAccountId] IS NULL)
    OR
    (
        [ReviewedAtUtc] IS NOT NULL
        AND
        (
            [ReviewedByAccountId] IS NOT NULL
            OR [MatchMethod] IN (N'user-confirmed-separate-population', N'system-exact-source-identity')
        )
    )
);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive. The expanded review provenance rule is retained.
    }
}
