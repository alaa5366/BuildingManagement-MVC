using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;
#nullable disable
namespace BuildingManagementMvc.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261119200000_Phase20_IdentityProfileEvidence")]
public sealed class Phase20_IdentityProfileEvidence : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.LegacyOperationalRecords',N'U') IS NULL
 THROW 51020,'Phase18 LegacyOperationalRecords table is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LegacyOperationalRecords') AND name=N'IX_LegacyOperationalRecords_IdentitySource')
 CREATE INDEX IX_LegacyOperationalRecords_IdentitySource ON dbo.LegacyOperationalRecords(SourceTable,BuildingId,SourceKey);
""");
 }
 protected override void Down(MigrationBuilder migrationBuilder)
 {
  // Intentionally non-destructive; identity evidence is retained.
 }
}
