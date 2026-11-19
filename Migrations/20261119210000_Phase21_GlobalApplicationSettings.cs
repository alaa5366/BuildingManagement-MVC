using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;
#nullable disable
namespace BuildingManagementMvc.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261119210000_Phase21_GlobalApplicationSettings")]
public sealed class Phase21_GlobalApplicationSettings : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.GlobalApplicationSettings',N'U') IS NULL
CREATE TABLE dbo.GlobalApplicationSettings(
 SettingsId nvarchar(64) NOT NULL CONSTRAINT PK_GlobalApplicationSettings PRIMARY KEY,
 AppName nvarchar(200) NOT NULL,
 DefaultLanguage nvarchar(16) NOT NULL,
 EnablePolls bit NOT NULL,
 EnableMaintenance bit NOT NULL,
 EnableOcr bit NOT NULL,
 EnableQrAccess bit NOT NULL,
 EnableWhatsApp bit NOT NULL,
 SessionTimeoutDays int NOT NULL,
 MaxPinAttempts int NOT NULL,
 StorageMode nvarchar(32) NOT NULL,
 UpdatedAtUtc datetime2(3) NULL,
 UpdatedByReference nvarchar(160) NULL,
 ImportedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_GlobalApplicationSettings_Imported DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_GlobalApplicationSettings_SessionTimeout CHECK(SessionTimeoutDays BETWEEN 1 AND 365),
 CONSTRAINT CK_GlobalApplicationSettings_PinAttempts CHECK(MaxPinAttempts BETWEEN 1 AND 100)
);
""");
 }
 protected override void Down(MigrationBuilder migrationBuilder)
 {
  // Intentionally non-destructive.
 }
}
