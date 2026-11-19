using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations;

/// <summary>Corrects nullable legacy-key uniqueness for normalized operational records.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261119130000_Phase10_OperationalNullableUniqueIndexes")]
public sealed class Phase10_OperationalNullableUniqueIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.MaintenanceRequests', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.MaintenanceRequests') AND name = N'UQ_MaintenanceRequests_Legacy')
        ALTER TABLE dbo.MaintenanceRequests DROP CONSTRAINT UQ_MaintenanceRequests_Legacy;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MaintenanceRequests') AND name = N'UX_MaintenanceRequests_Legacy')
        CREATE UNIQUE INDEX UX_MaintenanceRequests_Legacy
            ON dbo.MaintenanceRequests(BuildingId, LegacyMaintenanceId)
            WHERE LegacyMaintenanceId IS NOT NULL;
END;

IF OBJECT_ID(N'dbo.NotificationMessages', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.NotificationMessages') AND name = N'UQ_NotificationMessages_Legacy')
        ALTER TABLE dbo.NotificationMessages DROP CONSTRAINT UQ_NotificationMessages_Legacy;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.NotificationMessages') AND name = N'UX_NotificationMessages_Legacy')
        CREATE UNIQUE INDEX UX_NotificationMessages_Legacy
            ON dbo.NotificationMessages(LegacyNotificationId)
            WHERE LegacyNotificationId IS NOT NULL;
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive: reverting would reintroduce a uniqueness bug for NULL legacy keys.
    }
}
