using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261120140000_Phase25_LegacyPermissionCatalogCompatibility")]
public sealed class Phase25_LegacyPermissionCatalogCompatibility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Preserve existing building-role grants while translating retired broad finance
        // permissions into the explicit read/post/reverse permissions now enforced by MVC.
        migrationBuilder.Sql("""
            DECLARE @PermissionMap TABLE (LegacyKey nvarchar(128) NOT NULL, CurrentKey nvarchar(128) NOT NULL);
            INSERT INTO @PermissionMap (LegacyKey, CurrentKey) VALUES
                (N'expenses.manage', N'expense.read'),
                (N'expenses.manage', N'expense.post'),
                (N'expenses.manage', N'expense.reverse'),
                (N'revenues.manage', N'revenue.read'),
                (N'revenues.manage', N'revenue.post'),
                (N'revenues.manage', N'revenue.reverse'),
                (N'wallet.manage', N'wallet.read'),
                (N'wallet.manage', N'deposit.read'),
                (N'wallet.manage', N'deposit.confirm'),
                (N'wallet.manage', N'deposit.cancel'),
                (N'wallet.manage', N'deposit.update'),
                (N'wallet.manage', N'wallet.adjust'),
                (N'whatsapp.use', N'integration.whatsapp.send'),
                (N'whatsapp.use', N'integration.whatsapp.configure');

            INSERT INTO dbo.RolePermissions (RoleKey, PermissionKey, Effect)
            SELECT legacy.RoleKey, mapping.CurrentKey, legacy.Effect
            FROM dbo.RolePermissions AS legacy
            INNER JOIN @PermissionMap AS mapping ON mapping.LegacyKey = legacy.PermissionKey
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.RolePermissions AS currentGrant
                WHERE currentGrant.RoleKey = legacy.RoleKey
                  AND currentGrant.PermissionKey = mapping.CurrentKey
            );

            -- These code-owned system permissions were introduced after some SuperAdmin
            -- accounts were provisioned; add only missing grants and never override denies.
            INSERT INTO dbo.RolePermissions (RoleKey, PermissionKey, Effect)
            SELECT N'superadmin', permissions.PermissionKey, CONVERT(tinyint, 1)
            FROM (VALUES
                (N'categories.manage'),
                (N'deposit.update'),
                (N'integration.whatsapp.send')
            ) AS permissions(PermissionKey)
            WHERE EXISTS (
                SELECT 1 FROM dbo.RoleDefinitions
                WHERE RoleKey = N'superadmin' AND IsActive = 1
            )
            AND NOT EXISTS (
                SELECT 1 FROM dbo.RolePermissions existing
                WHERE existing.RoleKey = N'superadmin'
                  AND existing.PermissionKey = permissions.PermissionKey
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally retain additive grants: a grant may have existed before this
        // migration and removing it here could revoke an administrator's prior access.
    }
}
