using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;
#nullable disable
namespace BuildingManagementMvc.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261119160000_Phase16_QRCapabilityBackfill")]
public sealed class Phase16_QRCapabilityBackfill : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.QrCapabilities',N'U') IS NULL OR OBJECT_ID(N'dbo.QrCapabilityUses',N'U') IS NULL
 THROW 51016,'Phase09 QR target schema is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.QrCapabilities') AND name=N'IX_QrCapabilities_LegacyTokenHash')
 CREATE INDEX IX_QrCapabilities_LegacyTokenHash ON dbo.QrCapabilities(TokenHash);
""");
 }
 protected override void Down(MigrationBuilder migrationBuilder)
 {
  // Intentionally non-destructive.
 }
}
