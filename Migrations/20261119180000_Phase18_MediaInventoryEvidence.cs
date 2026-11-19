using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;
#nullable disable
namespace BuildingManagementMvc.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261119180000_Phase18_MediaInventoryEvidence")]
public sealed class Phase18_MediaInventoryEvidence : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.LegacyOperationalRecords',N'U') IS NULL
CREATE TABLE dbo.LegacyOperationalRecords(
 LegacyOperationalRecordId uniqueidentifier NOT NULL CONSTRAINT PK_LegacyOperationalRecords PRIMARY KEY,
 SourceSystem nvarchar(80) NOT NULL,
 SourceTable nvarchar(128) NOT NULL,
 SourceKey nvarchar(256) NOT NULL,
 BuildingId nvarchar(64) NULL,
 PayloadJson nvarchar(max) NOT NULL,
 SourceFingerprint varbinary(32) NOT NULL,
 ImportedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_LegacyOperationalRecords_Imported DEFAULT SYSUTCDATETIME(),
 CONSTRAINT UQ_LegacyOperationalRecords_Source UNIQUE(SourceSystem,SourceTable,SourceKey),
 CONSTRAINT CK_LegacyOperationalRecords_Json CHECK(ISJSON(PayloadJson)=1)
);
""");
 }
 protected override void Down(MigrationBuilder migrationBuilder)
 {
  // Intentionally non-destructive; imported legacy evidence must not be dropped on rollback.
 }
}
