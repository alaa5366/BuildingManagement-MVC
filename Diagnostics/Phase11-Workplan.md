# Phase11 — Audit Trail & Operational Observability

**Status:** IMPLEMENTED — local build/test validation pending  
**Branch:** `development/phase11-audit-observability`  
**Base:** `development/phase10-backup-restore-security`  
**Database:** No schema, migration, or data changes  
**Production:** Explicitly excluded

## Objectives

- Give every HTTP request a server-generated correlation identifier.
- Make the identifier available in the response, ASP.NET request trace identifier, structured logging scope, and current diagnostic activity.
- Attach the same identifier to audit metadata created during the request, without changing the AuditLog table schema.
- Keep client-supplied correlation headers untrusted and ignore them.

## Delivered

- Added `CorrelationIdMiddleware`; it generates a fresh 32-character hexadecimal ID per request and returns it as `X-Correlation-ID`.
- The middleware deliberately ignores incoming `X-Correlation-ID` values to prevent client-controlled identifiers from being treated as trusted server trace IDs.
- The identifier is set on `HttpContext.TraceIdentifier`, the current `Activity` tag `correlation.id`, and a logging scope property `CorrelationId`.
- `AuditLogger` adds the active request correlation ID to the JSON details object. Existing metadata properties remain at the root; scalar/array metadata is retained under `value`.
- Serilog request completion events explicitly include the same `CorrelationId` property.
- `AppDbContext` rejects tracked audit-row updates and deletes through normal EF Core SaveChanges paths; inserts remain supported for normal events and backup restore.
- Background operations without an active HTTP correlation continue to serialize metadata as before.
- Added five focused tests: server-owned response IDs, per-request uniqueness, audit metadata correlation, rejection of tracked audit updates, and rejection of tracked audit deletes.

## Guardrails

- No database migration or SQL script.
- No changes to audit table structure, query filtering, tenant scoping, or existing action names.
- The append-only guard is application-layer protection for tracked EF Core writes, not a substitute for restrictive SQL permissions. Direct SQL, bulk operations that bypass SaveChanges, and the existing building foreign-key SET NULL action remain outside this guard.
- No client IP, user-agent, credentials, cookies, or request body added to audit metadata.
- No production access or changes.

## Validation

Run the full local build and test suite. Phase11 remains pending acceptance until user-confirmed results are available.
