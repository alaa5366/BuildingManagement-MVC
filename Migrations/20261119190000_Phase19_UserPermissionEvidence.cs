using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;
#nullable disable
namespace BuildingManagementMvc.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261119190000_Phase19_UserPermissionEvidence")]
public sealed class Phase19_UserPermissionEvidence : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.LegacyOperationalRecords',N'U') IS NULL
 THROW 51019,'Phase18 LegacyOperationalRecords table is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LegacyOperationalRecords') AND name=N'IX_LegacyOperationalRecords_Permission')
 CREATE INDEX IX_LegacyOperationalRecords_Permission ON dbo.LegacyOperationalRecords(SourceTable,SourceKey);
""");
 }
 protected override void Down(MigrationBuilder migrationBuilder)
 {
  // Intentionally non-destructive.
 }
}
