# Phase09 – Session Revocation and Impersonation Hardening

**Status:** IN PROGRESS — code review and implementation plan committed  
**Branch:** `development/phase09-session-revocation-hardening`  
**Base:** `development/phase08-security-authorization-review`  
**Database:** No schema or data changes authorized  
**Production:** Explicitly excluded

## Objective

Reduce the time during which a disabled, removed, or de-authorized account can continue using an authenticated or impersonated session. Preserve the existing support workflow (admin/super-admin entering resident or building-admin context) and avoid breaking legitimate nested impersonation.

## Verified starting points

1. `SessionRevalidator.ValidateAsync` immediately accepts every principal with `impersonated=true`. It does not validate the identity that initiated impersonation.
2. `BuildResidentClaims` creates a synthetic resident identity (`NameIdentifier = imp-{apartmentId}`) and stores the initiating account only in `impersonatedBy`. This synthetic identity cannot be checked as a normal resident row.
3. `EnterBuildingAdmin` creates an admin-role principal for the super-admin and adds `originalRole=superadmin`; the current revalidator accepts this path without a database lookup.
4. Normal admin and resident sessions use a two-minute cache and a fifteen-minute last-known-good fallback during database errors.
5. The impersonation stack is protected by Data Protection and time-limited to two hours; the authentication cookie for impersonated sessions is non-persistent and non-refreshable with a two-hour expiry.
6. The stack snapshot includes the original UID, role, building IDs and permissions. The current impersonated principal is not cryptographically linked to a specific stack entry beyond the protected stack cookie and its claims.

## Phase09 scope

### A. Model the initiating identity explicitly

- Define the exact claims needed to identify the initiating account and its role for each supported transition.
- Distinguish the acting/impersonated principal from the initiating principal.
- Validate the initiating account as active and still authorized for the target building/apartment.
- Treat missing, malformed, or inconsistent impersonation metadata as invalid; do not silently fall back to unconditional acceptance.
- Preserve nested impersonation and correct restoration through the protected backup stack.

### B. Define revocation policy

- Decide and document whether impersonated sessions must be revoked immediately on account disablement/role change or may follow a bounded cache.
- Decide how super-admin eligibility is revoked: configured allow-list only, database role/status, or both. Do not assume the email allow-list is a complete account lifecycle check.
- Decide outage behavior explicitly. Do not extend a stale last-known-good authorization indefinitely.
- Keep ordinary resident/admin session behavior unchanged unless a separately tested improvement is justified.

### C. Tests required before implementation is accepted

- Active admin can enter a resident context and exit to the same admin/building/permissions.
- Disabled, inactive, deleted, or removed-from-building initiating admin cannot continue an impersonated session after the documented revocation window.
- Super-admin initiated resident and building-admin impersonation validate correctly.
- Nested impersonation restores the correct identity at each level.
- Missing/tampered/mismatched impersonation claims are rejected.
- Expired or unreadable stack cannot restore an identity.
- Database outage follows the documented fail-open/fail-closed boundary.
- Existing authorization, cookie, anti-forgery, and impersonation tests remain green.

## Non-goals

- No distributed session store or multi-instance deployment redesign.
- No changes to login throttling.
- No CSP/header policy changes.
- No SQL schema/data changes.
- No production configuration, database, or deployment access.

## Safety and implementation guardrails

- Do not remove the impersonation bypass until the initiating identity is represented and validated for every impersonation path; doing so would reject synthetic resident identities and may break super-admin workflows.
- Do not trust a role, UID, or building claim solely because it exists in the authentication cookie. Validate it against the current account state.
- Do not use the backup-stack cookie as the only authorization source; it is a restoration mechanism, not a replacement for current account authorization.
- Any change to revocation windows must include tests and an explicit availability/security trade-off.
- Prefer focused service-level tests plus integration coverage of cookie validation and controller transitions.
- Build and full test suite must be run locally before Phase09 acceptance.

## Validation record

No implementation or runtime validation has been claimed in this initial planning commit. The next implementation commit must include focused regression tests. User-confirmed local build/test results will be recorded only after they are supplied.

## Acceptance criteria

- Every impersonated session is linked to a validated initiating identity.
- Account disablement/removal and authorization changes take effect within the agreed, tested revocation window.
- Super-admin, admin, resident, and nested impersonation flows remain correct.
- Malformed or inconsistent impersonation state fails closed.
- Build and full test suite pass.
- No production or database changes occur.
