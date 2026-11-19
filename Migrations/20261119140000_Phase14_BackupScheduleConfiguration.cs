using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119140000_Phase14_BackupScheduleConfiguration")]
public sealed class Phase14_BackupScheduleConfiguration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.BackupScheduleConfigurations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BackupScheduleConfigurations (
        ConfigurationId uniqueidentifier NOT NULL CONSTRAINT PK_BackupScheduleConfigurations PRIMARY KEY,
        LegacySettingId nvarchar(20) NOT NULL,
        IsEnabled bit NOT NULL,
        Frequency nvarchar(20) NOT NULL,
        ScheduledHour int NOT NULL,
        DayOfWeek int NULL,
        DayOfMonth int NULL,
        CollectionsJson nvarchar(max) NOT NULL,
        MaxBackupsToKeep int NOT NULL,
        LastRunAtUtc datetime2(3) NULL,
        LastRunStatus nvarchar(30) NULL,
        UpdatedAtUtc datetime2(3) NULL,
        UpdatedByReference nvarchar(128) NULL,
        ImportedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_BackupScheduleConfigurations_Imported DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_BackupScheduleConfigurations_Legacy UNIQUE(LegacySettingId),
        CONSTRAINT CK_BackupScheduleConfigurations_Hour CHECK(ScheduledHour BETWEEN 0 AND 23),
        CONSTRAINT CK_BackupScheduleConfigurations_DayOfWeek CHECK(DayOfWeek IS NULL OR DayOfWeek BETWEEN 0 AND 6),
        CONSTRAINT CK_BackupScheduleConfigurations_DayOfMonth CHECK(DayOfMonth IS NULL OR DayOfMonth BETWEEN 1 AND 31),
        CONSTRAINT CK_BackupScheduleConfigurations_Retention CHECK(MaxBackupsToKeep BETWEEN 1 AND 30)
    );
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive: imported schedule configuration is retained on rollback.
    }
}
