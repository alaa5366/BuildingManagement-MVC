# Original Roadmap Phase10 — TEST Smoke Checklist

Run only against local/TEST. Do not use Production. No migration is required.

## 1. Build and automated tests

```powershell
dotnet build
dotnet test Tests/BuildingManagement.Tests
```

Expected: both succeed; zero failed tests.

## 2. Resident-management permission

Test with an admin account in a TEST building:
- Admin without `residents.manage`: resident list/details/create/edit/disable/enable/delete and PIN-reset actions return 403.
- Admin with `residents.manage`: the same actions work within the admin's own building.
- Super-admin behavior remains unchanged.
- Admin cannot access a resident belonging to another building.

## 3. Building-settings permission

- Admin without `settings.manage.building`: GET and POST `Settings/Building` return 403.
- Admin with the permission: GET and POST work for the admin's own building.
- Admin cannot read or update another building by changing `buildingId`.
- Super-admin can still select and update a building.

## 4. Camera-view permission

- Admin without `cameras.view`: `Cameras/Live` and `Cameras/GetHlsUrl` return 403.
- Admin with `cameras.view`: camera wall and HLS URL retrieval work only for the admin's building.
- Resident can still view cameras for the resident's own building.
- Resident cannot retrieve another building's HLS URL.
- Super-admin behavior remains unchanged.

## 5. Audit-log permissions and scope

- Resident with `audit.view.own`: can view/export only their own apartment's audit entries.
- Resident without `audit.view.own`: page and CSV export return 403.
- Admin with `audit.view.building`: can view/export entries for their own building only.
- Admin without `audit.view.building`: page and CSV export return 403.
- Super-admin can still filter across buildings.
- Attempted cross-building filters by admin/resident do not widen the result scope.

## 6. Diagnostic endpoint and route metadata

- `/ResidentHome/Debug` is no longer mapped.
- Automated tests confirm no duplicate HTTP method attributes on controller actions.
- Resident dashboard and resident polls page still load.
- QR access confirmation still works with a valid, unused token.

## 7. Safe error responses

Use controlled invalid inputs in TEST only:
- Backup/template/import failures show a generic localized error, not an exception type, local path, SQL detail, or stack trace.
- Settings and admin management failures do not expose raw exception messages.
- Receipt OCR failure returns a generic JSON error.
- Wallet pending-deposit validation still shows its known business validation message; unexpected failures remain generic.

## 8. Regression smoke

- Admin and resident login.
- Admin/super-admin 2FA and recovery-code flows.
- Impersonation enter/exit and session validation.
- Resident quick login and one-time QR access.
- Resident deposit submission; admin confirmation/cancellation/update of a pending deposit.
- Reports, invoices, maintenance, polls, notifications, and backup/restore pages.

## Safety

- Do not change Production.
- Do not apply migrations or SQL scripts for this phase.
- Do not manipulate Production accounts, payments, or backup files.
- Permission grants/revocations must be reflected on the next authenticated request without requiring logout/login.
- Verify both directions: grant `cameras.view` and confirm access becomes available; revoke it and confirm the next request returns 403.
- Verify the same behavior for `residents.manage` and `settings.manage.building`.
- If session-principal validation throws unexpectedly, the cookie must be rejected rather than continuing with stale claims.
- Automated regression tests cover the per-admin permission-cache version invalidation.
