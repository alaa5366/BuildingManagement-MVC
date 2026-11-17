# Phase08 – Security and Authorization Assessment

**Status:** Assessment in progress  
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

## Phase08 work sequence

1. Inventory authorization attributes and controller/action combinations.
2. Trace claims issued for resident, admin, super-admin, and impersonated sessions.
3. Trace impersonation start/exit/expiry and session revalidation.
4. Verify login-throttle persistence and cross-instance behavior; decide supported deployment topology.
5. Add focused regression tests for confirmed defects only.
6. Update security documentation with verified behavior and operational constraints.
7. Run local build and full test suite; use TEST-only SQL checks if a database change is justified.

## Safety boundary

No schema changes, data changes, or production operations are authorized by this assessment. Any required migration or destructive action must be separately reviewed and explicitly approved.
