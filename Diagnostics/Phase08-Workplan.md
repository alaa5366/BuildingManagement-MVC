# Phase08 – Security and Authorization Review

**Status:** Code review and targeted changes committed; awaiting local build/test validation  
**Branch:** `development/phase08-security-authorization-review`

## Scope

A code-first security review of authorization policy consistency, authentication claims, impersonation, session revocation, login throttling, QR one-time use, request protections, and backup/file handling.

## Acceptance criteria

- Every controller action has an effective, tested authorization policy.
- Every permission attribute call site is checked against the roles that can reach it.
- Resident/admin/super-admin claims and impersonation transitions are traced end to end.
- Session cache and last-known-good behavior are documented with explicit revocation windows.
- Login-throttle behavior is either proven safe for the supported topology or corrected with concurrency tests.
- No security change weakens existing anti-forgery, cookie, token, backup, or secret protections.
- Build and full test suite pass after changes.
- No production database or deployment is touched.

## Confirmed deployment boundary

The application is deployed as **one instance**. `LoginThrottle` therefore continues to use its process-local cache as the live enforcement authority. Persistence operations are now serialized in the same order as in-memory increments and resets. The SQL copy is restart-recovery state only; it is not distributed enforcement and can lose pending writes if the process stops abruptly.

## Remaining validation

1. Pull this branch locally.
2. Run `dotnet build`.
3. Run `dotnet test Tests/BuildingManagement.Tests`.
4. Confirm the new concurrent login-throttle test passes and report the final totals.
5. No SQL migration or database operation is required for Phase08 changes.
