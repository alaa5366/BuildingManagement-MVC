# Legacy Permission Scope Catalogue — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Status:** Proposed target mapping from the current `Models/AdminPermissions.cs` catalog, BackendInfo metadata, controller call-site inventory, and Sections 1–31 review. Not an approved authorization policy and not a migration script.

## 1. Governing rule

Legacy permission strings are not sufficient to determine target scope. Every permission must be resolved against:
- the actual subject (account/person);
- the protected action and service operation;
- the resource being accessed;
- the resource's building/apartment ownership;
- effective dates, account status, and explicit deny overrides.

Target authorization must be evaluated by one server-side `AuthorizationEvaluator`. UI visibility, permission claims, and the legacy SuperAdmin bypass are not authoritative. Unknown permission keys, missing scope, unresolved resources, and unknown roles fail closed.

The target role model should represent SuperAdmin as a System-scoped role assignment. Its grants are explicit and auditable. Sensitive permissions such as secret access, restore execution, and global integration-secret access should not be implicitly granted merely because a user has a SuperAdmin label; decide whether they require separate grants, MFA step-up, reason, and/or dual approval.

## 2. Proposed permission-to-scope catalogue

| Legacy key | Current catalogue intent / observed consumer | Proposed target scope | Target resource check | Notes / unresolved issue |
|---|---|---|---|---|
| `buildings.manage` | BuildingsService / BuildingsController: create/edit/delete buildings, floors, apartments, QR code operations | System | Building being created/changed; system-wide list only for authorized system operator | Confirm whether any building-admin operation should be split into a separate building-scoped key. |
| `admins.manage` | AdminManagementService; AdminsController, PermissionsController | System | Target account and explicit building assignments | SuperAdmin-only in current controller inventory. |
| `residents.manage` | Resident/apartment management; AdminResidentsController, AdminAptsController, QR | Building | BuildingId + apartment relationship | BackendInfo currently names AdminAptsController only; update catalogue to include AdminResidentsController. |
| `apartment.disable` | AdminAptsController DisableApartment/EnableApartment | Building | Exact BuildingId + ApartmentId | Separate permission from general resident management. |
| `dvrs.manage` | DvrService / DvrsController CRUD | Building | DVR and linked camera must belong to same building | Do not expose encrypted credentials through broad DTOs. |
| `cameras.view` | CamerasController Live/GetHlsUrl | Building | Camera/building and resident access policy | Legacy filter intentionally allows non-admin authenticated residents to proceed; target must express resident access separately from admin permission. |
| `cameras.manage` | DvrService / CamerasController Add/Edit/Delete camera | Building | Camera and DVR composite building scope | External MediaMTX state requires outbox/retry. |
| `template.import` | ExcelImportService; BackendInfo names BuildingImportController/BuildingWizardController | System or Building (command-specific) | Explicit target BuildingId; import batch bound to actor and target | The referenced controllers are absent in the reviewed branch inventory; do not assign a scope until workflow is restored or catalog entry retired. |
| `wallet.manage` | WalletService / WalletController: deposit confirmation/cancellation, adjustment | Building | Apartment + financial period + deposit belongs to same apartment/building | Split read vs confirm/cancel/adjust capabilities in target; all money commands need idempotency and audit. |
| `expenses.manage` | ExpensesService / ExpensesController | Building | Expense category, apartment/building, period | Split read/create/update/reverse if duties require. |
| `revenues.manage` | RevenuesService / RevenuesController | Building | Revenue category, apartment/building, period | Split read/create/update/reverse if duties require. |
| `categories.manage` | CategoriesService / CategoriesController | System or Building (depends on actual category ownership) | Category must match its owning catalog/building | Current controller permits admin/superadmin; category table scope must be verified before migration. |
| `reports.view` | ReportsService / ReportsController Index/ExportCsv | Building | All report rows constrained to assigned building | Export is a separate data-exfiltration concern; consider a distinct export permission. |
| `polls.manage` | PollsService / PollsController; resident vote flow | Building | Poll belongs to building; resident vote authorization uses eligibility, not admin permission | Do not apply admin manage permission to voter choice. |
| `maintenance.manage` | MaintenanceService / MaintenanceController; resident maintenance actions | Building + Apartment for resident-originated operations | Request's apartment and building; cost linkage to ledger | Split admin workflow management from resident request/comment actions. |
| `notifications.send` | NotificationsService / NotificationsController | Building | Recipient set resolved within building | BackendInfo describes read/mark-read actions, not only send; split dispatch and recipient self-service. |
| `whatsapp.use` | WhatsAppTemplateService / WhatsAppController | Building by default; System only for global provider administration | Building integration configuration and approved recipient set | Separate secret administration from message dispatch; secret values never enter UI DTOs. |
| `audit.view.all` | AuditLogQueryService / AuditLogController | System | Explicit cross-building audit query privilege | Privileged and audited; consider separate export permission. |
| `audit.view.building` | AuditLogQueryService / AuditLogController | Building | Actor's currently assigned building(s) | Must not accept arbitrary BuildingId from query without authorization. |
| `audit.view.own` | AuditLogQueryService / AuditLogController | Personal (subject Person/account) | Own events only; apartment-level visibility requires separate policy | Legacy label/description refers to "my apartment"; resolve whether target means Person, account, or apartment. |
| `system.logs.view` | ApplicationLogQueryService / ApplicationLogsController | System | Sanitized operational logs, privileged monitoring scope | Do not conflate diagnostic logs with business audit. Redaction/retention and download policy apply. |
| `settings.manage.global` | SettingsService / SettingsController Global | System | System settings only | SuperAdmin-only in current inventory; secrets write-only. |
| `settings.manage.building` | SettingsService / SettingsController Building | Building | Explicit target BuildingId | Legacy remediation added permission checks; verify both GET/POST and resource binding in target. |
| `settings.manage.own` | SettingsService / SettingsController Own | Personal | Account -> Person self only | Existing POST path lacked explicit permission enforcement; target must enforce at use-case and endpoint. |
| `tools.manage` | MigrationService/DbMaintenanceService; ToolsController | System | Operator-approved maintenance operation | Referenced controller/services absent in reviewed branch inventory; retire or implement supported workflow. High-risk, step-up/dual-control candidate. |
| `backup.manage` | BackupService/ScheduledBackupService; BackupController | System | Backup artifact and restore target database | Split create/download/restore/delete/retention. Restore and delete should require step-up and explicit confirmation. |
| `sync.manage` | DbSyncService/ReverseSyncService; SyncController | System | Explicit source/target environment and allowed data class | Referenced controller/services absent in reviewed branch inventory; retire or implement with idempotency and source-of-truth. |
| `secret.access` | ImpersonationService / ImpersonationController | System grant + explicit target Building/Apartment scope | Initiator, target account/person, reason, expiry, MFA and live permission revalidation | Current code path lacked explicit secret.access enforcement; never treat this as an ordinary reusable admin permission. |

## 3. Required target permission decomposition

The legacy catalog combines operations that have different risk levels. The target should split high-impact capabilities rather than blindly port each key as one broad grant:

- **Finance:** `wallet.read`, `deposit.confirm`, `deposit.cancel`, `wallet.adjust`, `expense.create/update/reverse`, `revenue.create/update/reverse`, `finance.export`.
- **Residents and access:** `resident.read`, `resident.manage`, `credential.reset`, `apartment.disable`, `apartment.access.grant/revoke`.
- **Cameras:** `camera.view`, `camera.manage`, `dvr.manage`, `camera.credential.reveal` (only if an explicit use case remains).
- **Polls:** `poll.manage`, `poll.open/close/finalize`; voting eligibility/ballot casting is a separate resident use case.
- **Maintenance:** `maintenance.read`, `maintenance.manage`, `maintenance.request`, `maintenance.expense.post/reverse`.
- **Communications:** `notification.dispatch`, `notification.self.read`, `integration.whatsapp.configure`, `integration.whatsapp.send`.
- **Audit/operations:** `audit.read.system/building/personal`, `logs.read`, `backup.create/download/restore/delete`, `system.maintenance.execute`, `data.sync.execute`.
- **Impersonation:** `impersonation.start` with separate target scope, mandatory reason, short expiry, MFA step-up, and durable start/stop/outcome audit.

Names above are examples of a decomposed catalog, not final stable keys. Freeze keys only after command/query contracts and UI workflows are designed.

## 4. Legacy catalog integrity findings

1. `AdminPermissions.All` currently contains 28 keys. `SuperAdminAll` aliases `All`, so it includes `secret.access`, backup, sync, tools, and global logs by default.
2. `TemplateAdminSuper` does not include `system.logs.view`, although `SuperAdminAll` does. Template membership is not a substitute for a documented role policy.
3. `BackendInfo(ManageResidents)` names `AdminAptsController` but omits `AdminResidentsController`, despite the latter having the ManageResidents filter in the reviewed branch.
4. `BackendInfo(ImportTemplate)`, `BackendInfo(ManageTools)`, and `BackendInfo(ManageSync)` reference controllers/services absent from the current branch inventory. Treat them as stale catalog entries until verified or retired.
5. `BackendInfo(SendNotifications)` describes NotificationsController read/mark-read actions, while the key name implies dispatch. Separate resident inbox access from building-wide dispatch.
6. `BackendInfo(ManageMaintenance)` combines administrator management and resident maintenance operations.
7. `ManageWallet` combines read, deposit state transitions, and adjustments; target finance needs command-level authorization.
8. `SecretAccess` is included in the broad SuperAdminAll alias, while the reviewed impersonation path did not show explicit enforcement of this permission. Add a distinct privileged workflow and verify every endpoint.
9. `settings.manage.own` is catalogued but the reviewed self-settings POST path lacked explicit HasPermission enforcement. The target use case must enforce Personal scope regardless of UI.
10. Legacy `AdminPermissionAttribute` and `HasPermissionAttribute` rely on separate role attributes and permission claims; they are not safe target authorization primitives. Replace them with one policy evaluator rather than copying their bypass behavior.

## 5. Owner decisions

The following decisions materially affect permission schema and role templates:

- **PERM-01:** Should SuperAdmin have all operational permissions by default, or should backup restore, system maintenance, secret access, and global secret configuration require separate privileged grants? **Recommendation:** separate grants with MFA step-up; dual approval for restore/secret reveal where practical.
- **PERM-02:** Should building admins receive one broad `wallet.manage` capability, or separate read/confirm/cancel/adjust permissions? **Recommendation:** separate financial duties.
- **PERM-03:** Should apartment-level audit visibility mean the current Person's events or all events for their apartment? **Recommendation:** Personal = own account/person; Apartment requires a separate explicit scope/policy.
- **PERM-04:** Should WhatsApp dispatch permission include configuration/secret administration? **Recommendation:** no; separate them.
- **PERM-05:** Are stale import/tools/sync features to be removed, or do they need to be rebuilt as supported modules? **Recommendation:** do not migrate grants for missing workflows; retain as unresolved until product confirms.

These are owner decisions. The proposed safe defaults can guide design but are not silently approved.

## 6. Safety

This is a documentation-only mapping. It does not alter authorization code, issue grants, change role templates, or migrate `UserPermissions`. No build or tests were run. The final mapping must be reconciled against every endpoint and service method before the target permission catalog is frozen.

## 7. Implementation follow-up — 2026-11-18

The owner approved the recommended defaults through DEC-01 to DEC-46. This resolves the policy choices listed above where they concern defaults, but does not provide endpoint evidence or authorize backfilling an ambiguous legacy assignment.

- `DefaultAuthorizationPermissionCatalog` is now the code-owned allow-list used by the central `AuthorizationEvaluator`. Unknown keys fail closed; sensitive grants require a fresh MFA claim; explicit deny takes precedence.
- `apartment.access.grant.manage` is a new building/apartment-scoped permission that requires fresh MFA. The transactional grant lifecycle checks it before granting or revoking access.
- `ApartmentAccessGrantEntity.PermissionSetKey` values remain inert identifiers. No family, tenant, caretaker, emergency, or temporary profile grants permissions by itself.
- This catalogue remains an inventory for the staged migration. Legacy login/controllers still use legacy permission claims, and no legacy permission assignment has been copied into target role or override tables. Complete each consumer/resource review before any cutover or grant import.
