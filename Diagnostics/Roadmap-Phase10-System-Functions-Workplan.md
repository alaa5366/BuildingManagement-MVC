# Original Roadmap Phase10 — System Functions Review Workplan

**Status:** IMPLEMENTED — core build, automated tests, and targeted TEST smoke checks passed; extended smoke checks remain  
**Branch:** `development/roadmap-phase10-system-functions-review`  
**Base:** `development/roadmap-phase09-logging-monitoring` (`16b870a137ffb90e163571b4ca63612e59bccc66`)  
**Database:** No schema/data changes authorized  
**Production:** Explicitly excluded

## Objective

Review the complete user-facing function surface and close verified authorization, route metadata, diagnostic disclosure, and error-disclosure gaps without changing accepted business behavior.

## Delivered

1. Enforced `residents.manage` on all resident-management actions.
2. Enforced `settings.manage.building` on both building-settings actions.
3. Enforced `cameras.view` for admin camera viewing and HLS URL retrieval while retaining resident/super-admin behavior.
4. Enforced role-scoped audit-log permissions on both page and CSV export.
5. Removed the resident claims diagnostic endpoint.
6. Removed duplicate HTTP method attributes and added a regression test.
7. Replaced raw unexpected exception messages in administrative, settings, backup/import, wallet-update, and receipt-OCR responses with generic localized errors.
8. Added regression tests for controller permission metadata, audit-log role/permission decisions, duplicate HTTP method attributes, and removal of the resident diagnostic action.

## Acceptance criteria

- Local `dotnet build` succeeds.
- Full `dotnet test Tests/BuildingManagement.Tests` succeeds with zero failures.
- TEST smoke checks confirm allowed and denied cases for each newly enforced permission.
- Existing resident, super-admin, impersonation, QR access, backup, and financial workflows continue to work.
- No database migration or Production change is required or performed.

## Validation commands

```powershell
git fetch origin
git switch development/roadmap-phase10-system-functions-review
git pull --ff-only origin development/roadmap-phase10-system-functions-review
dotnet build
dotnet test Tests/BuildingManagement.Tests
```

After changing admin permissions in TEST, verify the change on the next authenticated request without signing out or back in. The session revalidator refreshes permission claims and renews the cookie when they differ. This immediate invalidation is process-local; multi-instance deployments need a distributed invalidation mechanism.

## Validation record

- Local build passed.
- Full automated suite passed: 468/468, zero failures, zero skipped.
- User-reported TEST checks passed for resident-management, building-settings, camera permission grant/revoke, Audit Log permissions/scope, resident login with an allow-listed e-mail, and the removed Resident Debug endpoint (HTTP 404).
- Controlled error-response checks and the broader regression smoke list in the TEST checklist were not all individually reported as executed; they remain unverified.

## Not in scope

- Production deployment or Production data.
- Database schema or data migrations.
- Reworking the financial calculation model or changing transaction semantics.
- Replacing the existing QR/quick-login capability-token architecture.
- Adding a third-party monitoring, camera, or notification provider.
