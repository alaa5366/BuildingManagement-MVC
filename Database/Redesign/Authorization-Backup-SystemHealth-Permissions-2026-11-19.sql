/*
    Authorization catalog grant update — 2026-11-19
    Apply only after a verified database backup and after the identity foundation exists.
    This script grants the new system-monitoring and action-specific backup permissions
    to the existing SuperAdmin role only. It does not change Admin or account-level grants.
    Idempotent: existing grants are left unchanged.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.RoleDefinitions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
    THROW 51119, 'Target authorization tables are missing; apply the identity foundation first.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.RoleDefinitions WHERE RoleKey = N'superadmin' AND IsActive = 1)
    THROW 51120, 'An active superadmin role definition is required.', 1;

BEGIN TRANSACTION;

INSERT dbo.RolePermissions (RoleKey, PermissionKey, Effect)
SELECT N'superadmin', p.PermissionKey, 1
FROM (VALUES
    (N'backup.view'),
    (N'backup.create'),
    (N'backup.download'),
    (N'backup.restore'),
    (N'backup.delete'),
    (N'system.health.view')
) AS p(PermissionKey)
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.RolePermissions existing
    WHERE existing.RoleKey = N'superadmin'
      AND existing.PermissionKey = p.PermissionKey
);

COMMIT TRANSACTION;
