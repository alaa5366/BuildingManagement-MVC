# Phase08 – Security and Authorization Review

**Status:** In progress  
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

## Current known risk

The current `LoginThrottle` implementation is process-local for decisions and persists asynchronously. The source explicitly says cross-instance synchronization is unsupported. Phase08 must establish whether the application is intentionally single-instance or requires distributed throttling before accepting this behavior.
