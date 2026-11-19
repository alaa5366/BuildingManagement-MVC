# Phase 13 – Security & Authorization Hardening

**Status:** Assessment / work plan only — implementation not started  
**Branch:** `development/phase13-security-authorization-hardening`  
**Base:** `main` (current accepted repository baseline)  
**Production:** Explicitly excluded  
**Database:** No schema changes or data mutations authorized in this phase without separate approval

## Objective

Harden authentication and authorization based on verified code paths. Preserve existing resident workflows, building/apartment isolation, super-admin behavior, impersonation boundaries, and financial protections. Findings from static review are treated as hypotheses until call sites and tests are traced.

## Confirmed code observations

### 1. AdminPermissionAttribute has a configurable bypass and non-admin pass-through

Current implementation:
- Returns without a decision for unauthenticated principals, relying on a separate `[Authorize]` attribute.
- Returns without denying authenticated principals that are not in role `admin`.
- Returns without checking the permission when `Auth:EnforceAdminPermissions=false`.
- Otherwise requires the named `perm` claim.

This is not safe to change blindly: `CamerasController` uses class-level `[Authorize]` plus `[AdminPermission(ViewCameras)]`, and its comments and action code intentionally support resident camera viewing with building-scope checks. Therefore the non-admin pass-through currently participates in a mixed admin/resident controller design. The controller and every action must be inventoried before defining a fail-closed replacement. A dedicated resident-view policy or separate attribute may be needed rather than turning this filter into a universal role gate.

### 2. Permission catalog versus enforcement call sites

`Models/AdminPermissions.cs` defines the permission catalog and documents backend mappings. Initial repository code search found these five keys only in the catalog (not as active authorization-attribute usages):
- `buildings.manage`
- `apartment.disable`
- `cameras.manage`
- `template.import`
- `notifications.send`

This is a preliminary search result, not yet proof that every corresponding action is unprotected. The mapped controllers/actions and any service-level checks must be inspected individually. In particular, apartment enable/disable currently sits in a controller protected by `ManageResidents`; whether that is intentional or needs a dedicated permission is a product authorization decision.

### 3. HasPermissionAttribute has separate semantics

The current attribute redirects anonymous users, bypasses checks for role `superadmin`, and otherwise accepts a matching `perm` claim without explicitly requiring role `admin`. Its call sites and claim issuance must be reviewed alongside `AdminPermissionAttribute`; do not assume the two attributes are interchangeable.

### 4. PIN and 2FA

Verified source observations:
- Resident login validates a four-character PIN.
- Admin login verifies the stored admin PIN through the shared secret verifier; the legacy migration code treats admin PINs as four digits while super-admin credentials may be a password.
- PINs are stored as BCrypt hashes, with a startup migration for legacy plaintext values.
- Login endpoints use both ASP.NET rate limiting and the account/IP `LoginThrottle`.
- TOTP is currently opt-in for admin/super-admin accounts; recovery codes are generated and stored as hashes.

The four-digit PIN remains a low-entropy credential despite hashing and throttling. Any stronger PIN/password policy must include a safe enrollment/reset and transition plan; do not silently invalidate existing accounts or change credential formats without testing all login and recovery paths. Mandatory 2FA requires a separate review of enrollment, recovery, impersonation, and administrative lockout handling.

### 5. Existing tests

`ControllerAuthorizationTests` checks that controllers/actions have `[Authorize]` or explicit `[AllowAnonymous]`, checks selected controller role restrictions, and checks a small subset of permission attributes. It does not yet provide exhaustive coverage of every declared permission and all custom-filter role combinations.

The prior `Diagnostics/Phase08-Security-Assessment.md` explicitly warns that custom authorization attributes depend on their paired role attributes and that impersonation bypasses ordinary session revalidation. These boundaries must be preserved and revalidated.

## Work plan

### Workstream A — Authorization inventory
1. Enumerate every controller/action and effective `[Authorize]`, `[AllowAnonymous]`, `AdminPermission`, and `HasPermission` combination.
2. Trace all 28 catalog permissions from assignment/template/claims through endpoint and service enforcement.
3. Inspect the five preliminary gaps and map each permission to the exact intended actions.
4. Identify mixed-role endpoints such as resident camera viewing and retain their explicit building/apartment isolation.
5. Trace super-admin and impersonated principal claim issuance and authorization.

### Workstream B — Fail-closed design
1. Remove the unsafe configuration bypass from the final enforcement design, or replace it with a narrowly scoped, deployment-safe emergency mechanism that cannot silently disable authorization.
2. Make authorization behavior explicit by principal type and endpoint intent.
3. Keep role authorization and permission authorization complementary; no custom permission attribute should accidentally substitute for endpoint authentication.
4. Ensure missing/unknown permission claims deny access to protected operations.
5. Preserve intentional resident read-only access only through an explicit, tested policy.

### Workstream C — Permission enforcement
1. Add the correct permission checks to the five mapped feature areas after verifying exact actions and intended roles.
2. Decide whether apartment disable/enable is covered by `residents.manage` or requires `apartment.disable`; document the decision.
3. Keep permission assignment UI/templates and backend enforcement consistent.
4. Verify authorization at the server endpoint, not only by hiding UI controls.

### Workstream D — Credential hardening
1. Trace all resident/admin/super-admin login, PIN change/reset, QR set-PIN, and recovery flows.
2. Assess a stronger credential policy with a backwards-compatible enrollment/reset strategy.
3. Assess mandatory 2FA for privileged admin roles, including recovery and break-glass procedures.
4. Preserve BCrypt, login throttling, anti-enumeration behavior, and session revalidation.
5. Avoid database schema changes unless independently justified and approved.

### Workstream E — Regression tests
Add focused tests for:
- Anonymous, resident, admin, super-admin, and impersonated principals.
- Admin-only endpoints with missing roles and missing permission claims.
- Every declared permission's protected endpoint mapping.
- The five preliminary permission gaps.
- The explicit resident camera-view path and cross-building denial.
- The removal/handling of the configuration bypass.
- PIN length/policy validation, throttling integration, TOTP enrollment/verification/recovery, and account lockout safety.
- Permission refresh/revocation through session revalidation.

## Acceptance criteria

- Every privileged endpoint has explicit authentication, role, and permission semantics.
- All permissions exposed in the admin permission catalog have verified backend enforcement or are explicitly documented as display-only/not applicable.
- No resident can reach admin mutations through a mixed-role controller.
- Intentional resident read-only functionality remains available within the resident's own building.
- No hidden configuration value can silently disable authorization.
- Credential-policy changes do not lock out existing users unexpectedly.
- Build and the full test suite pass locally; relevant SQL Server integration checks are used where behavior depends on SQL Server.
- No production deployment, production data change, or database migration is performed as part of this assessment.

## Execution order

1. Complete the endpoint/permission/credential inventory.
2. Review and approve the proposed authorization semantics before changing filters or role behavior.
3. Implement one bounded workstream at a time.
4. Run targeted tests, then full build/test suite.
5. Review the final diff and document operational impact before merge.

**Current disposition:** branch created and assessment recorded; no application code has been modified.
