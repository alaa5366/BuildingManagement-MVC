# Phase08 – Security and Authorization Assessment

**Status:** Assessment in progress — single-instance deployment confirmed  
**Branch:** `development/phase08-security-authorization-review`  
**Base:** `development/phase07-application-data-integrity`  
**Database scope:** TEST only; diagnostics are read-only unless separately approved  
**Production:** Explicitly excluded

## Objective

Review authentication, authorization, session revalidation, login throttling, QR token consumption, request protections, secret handling, and backup/file boundaries. The goal is to verify actual code paths and tests, not merely restate the security guide.

## Initial code-review findings

### 1. LoginThrottle is not distributed across application instances

`Services/LoginThrottle.cs` uses a process-local lock and `IMemoryCache` for increments and lock decisions. It asynchronously mirrors counters to SQL with a `MERGE`; its own XML remarks explicitly state that multi-instance synchronization is unsupported and each instance loads state at startup. This means simultaneous failures handled by different instances can race, and a newly incremented counter is not guaranteed to be visible to another instance before its next decision.

**Disposition:** High-priority investigation. Determine whether deployment is single-instance by design. If multi-instance operation is supported, design a database-atomic increment/lock implementation and tests before changing behavior.

### 2. Session revalidation has intentional staleness windows

`Services/SessionRevalidator.cs` caches valid/invalid results for two minutes and can use a last-known-good result for up to fifteen minutes during database errors. This trades immediate revocation for temporary availability.

**Disposition:** Document and test the intended security/availability boundary. Do not shorten or remove the fallback without understanding production availability requirements.

### 3. Impersonation bypasses ordinary session revalidation

`SessionRevalidator.ValidateAsync` returns valid immediately for a principal carrying `impersonated=true`; it also has explicit `originalRole=superadmin` handling. These may be valid design choices, but they require verification that impersonation creation, expiry, exit, and authorization are independently constrained.

**Disposition:** Trace the complete impersonation flow and its tests before changing the bypass.

### 4. Permission attributes have different semantics

`AdminPermissionAttribute` returns without denying when the authenticated principal is not in the `admin` role; `HasPermissionAttribute` checks a `perm` claim without an explicit admin-role check (except super-admin bypass). Their safety therefore depends on controller/action role attributes and claim issuance.

**Disposition:** Inventory every use and validate effective endpoint policies. Do not change attributes globally until all call sites are reviewed.

## Confirmed existing protections to preserve

- Global MVC anti-forgery validation.
- Controller authorization reflection tests.
- Production cookies use `SecurePolicy.Always`; cookies are HttpOnly.
- Login rate-limiter policies and persistent login-throttle intent.
- Quick QR token use-count update is conditional/atomic in SQL.
- Secret configuration is validated through `ConfigSecrets.Require`.
- Backup restore is constrained to a generated temporary archive and uses a database transaction.

## Detailed review results

### Authorization call-site inventory

The repository search identified these controller-level uses of `AdminPermissionAttribute`:
- WhatsApp, QR, Reports, Polls, Revenues, Expenses, DVRs, Maintenance, Wallet, and Admin Apartments.
- Each inspected declaration pairs the attribute with `[Authorize(Roles = ...)]`. The DVR controller allows `superadmin,admin`; the others in this inventory are admin-only.

The `HasPermissionAttribute` call sites found are:
- Permissions, Admins, Settings (global actions), Categories, and Backup.
- Each inspected call site is paired with `[Authorize]` role restrictions. Categories permits admin/superadmin; the remaining listed controllers/actions are superadmin-only.

This pairing is important: both custom attributes are intentionally not standalone role gates. Keep the role attribute in place whenever either custom permission attribute is used. Existing controller authorization tests cover missing `[Authorize]` / `[AllowAnonymous]` and selected sensitive controller roles, but they do not yet assert every custom-attribute pairing.

### Impersonation lifecycle

- Entry and exit actions are POST-only, have anti-forgery validation, and require explicit role authorization.
- The backup stack is protected by ASP.NET Data Protection with a two-hour time limit and a maximum depth of five.
- Impersonated authentication cookies are non-persistent, non-refreshable, and explicitly expire after two hours.
- `SessionRevalidator` bypasses DB revalidation for any principal with `impersonated=true`. This is bounded by the two-hour impersonation cookie lifetime in the controller flow, but disabling the original account or changing permissions during that interval does not immediately terminate the impersonated session.
- The stack cookie uses HttpOnly and SameSite=Lax; its Secure flag follows `Request.IsHttps`. Production HTTPS is expected, but proxy/forwarded-proto configuration must be verified in deployment settings.

**Disposition:** No immediate change yet. Add regression coverage for two-hour expiry, stack integrity, and authorization pairing before modifying this established workflow.

### Login-throttle persistence ordering and deployment boundary

The deployment is confirmed as **one application instance**. Therefore the in-memory counter and its process-local lock are the live enforcement authority; a distributed cache or distributed atomic counter is not required for the stated topology.

The original implementation queued independent fire-and-forget SQL `MERGE` and DELETE tasks. Even in one process, those operations could finish out of order (for example, a delayed failure snapshot could be written after a successful-login reset). The implementation now appends persistence operations to a single task chain while holding the same lock used for each in-memory state transition. This preserves the order of increments and resets in the SQL recovery mirror.

**Remaining boundary:** SQL persistence is asynchronous best-effort recovery state. A process crash can still lose writes that have not completed; the database copy is not the live lock authority and does not provide distributed enforcement. Multi-instance deployment is not supported by this design and requires a separate architecture change.

## Phase08 work sequence

1. Inventory authorization attributes and controller/action combinations.
2. Trace claims issued for resident, admin, super-admin, and impersonated sessions.
3. Trace impersonation start/exit/expiry and session revalidation.
4. Confirmed single-instance deployment; serialize login-throttle persistence writes and document the restart-recovery boundary.
5. Add focused regression tests for confirmed defects only.
6. Update security documentation with verified behavior and operational constraints.
7. Run local build and full test suite; use TEST-only SQL checks if a database change is justified.

## Safety boundary

No schema changes, data changes, or production operations are authorized by this assessment. Any required migration or destructive action must be separately reviewed and explicitly approved.
