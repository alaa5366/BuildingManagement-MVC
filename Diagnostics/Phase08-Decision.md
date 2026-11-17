# Phase08 – Security Review Decision

**Branch:** `development/phase08-security-authorization-review`  
**Base:** `development/phase07-application-data-integrity`  
**Status:** ACCEPTED — local build and full test suite passed  
**Database:** No schema or data changes  
**Production:** Not accessed

## Accepted deployment assumptions

- The application runs as a single instance.
- Login-throttle decisions remain process-local.
- SQL persistence is a best-effort restart-recovery mirror. Writes and resets are serialized in the order of their in-memory state transitions.
- Multi-instance hosting is not supported by this implementation; it requires a separate distributed design.

## Changes committed

- Serialized `LoginThrottle` SQL persistence operations to prevent stale asynchronous snapshots from overwriting newer counts or a later reset.
- Added a concurrent-failure regression test for the in-memory account lock threshold.
- Expanded the security assessment with verified authorization pairings, impersonation lifecycle boundaries, anti-forgery exceptions, rate limits, QR consumption, backup restore boundaries, cookie/Data Protection settings, and CSP hardening notes.

## Security findings retained for future hardening

1. Impersonated principals bypass ordinary session revalidation for the lifetime of the two-hour impersonation authentication cookie. Account disablement or permission changes during that period do not immediately revoke the impersonated session.
2. Admin/resident session revalidation uses a two-minute cache and can use a last-known-good result for up to fifteen minutes during database errors.
3. The only `IgnoreAntiforgeryToken` call found is the authenticated presence heartbeat. Its effect is limited to updating online-presence status; retain the exception only while that remains true.
4. QR device-fingerprint support exists in the service signature but is not wired into the current controller flow; do not rely on it as an active control.
5. CSP permits inline scripts/styles and broad resource sources. Tightening requires a separate compatibility review.
6. Forwarded-header trust must be configured for the actual production proxy addresses.

## Validation completed

User-confirmed local validation on the Phase08 branch:

- `dotnet build`: succeeded.
- `dotnet test Tests/BuildingManagement.Tests`: 408 passed, 0 failed, 0 skipped.
- No EF migration or SQL script was required.
- Production database and deployment were not accessed.

**Decision:** Phase08 is accepted and closed.
