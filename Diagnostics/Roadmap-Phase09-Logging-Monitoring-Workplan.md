# Original Roadmap Phase09 — Logging & Monitoring Workplan

**Status:** IMPLEMENTED — local validation pending  
**Branch:** `development/roadmap-phase09-logging-monitoring`  
**Base:** `development/roadmap-phase08-authentication-2fa`  
**Database:** No schema or data changes  
**Production:** Explicitly excluded

## Delivered

- Rolling Serilog file output now renders structured properties, including the server-generated `CorrelationId`.
- Added `HealthCheckResponseWriter` with a minimal JSON contract: overall status, total duration, and check names/statuses only.
- Tagged the SQL database check as readiness-dependent.
- Added `/health/live` (no dependency checks) and `/health/ready` (SQL readiness).
- Retained `/health` as a backward-compatible readiness alias.
- Fatal top-level application failures now set a non-zero process exit code.
- Routed key startup diagnostics through structured Serilog events.
- Added tests for healthy/unhealthy health responses and non-disclosure of exception/connection details.

## Acceptance criteria

- Request log files include structured diagnostic properties and correlation identifiers.
- Liveness remains healthy without invoking SQL checks.
- Readiness and `/health` reflect database health.
- Health JSON never exposes exception messages, stack traces, or connection information.
- Fatal startup/runtime failure returns a non-zero process exit code.
- Full local build and test suite pass.
- Practical TEST smoke checks pass.
- No migrations, SQL changes, or Production modifications.

## Validation record

The assistant has not executed the user's local .NET build or test suite. Record user-confirmed results here only after they are supplied.


## Phase 09.1 — Application Logs Explorer

**Branch:** `development/phase09-1-application-logs-explorer`
**Status:** Implementation committed; local build, focused tests, and UI validation pending.

- Added a separate Application Logs Explorer; it does not replace or mix with the business Audit Trail.
- Reads only the fixed `logs/serilog/serilog-*.txt` pattern, top-level files, and the latest 14 retained files. Request input is never used to construct a file path.
- Streams log files with shared read access so the active rolling file can be inspected while the application is running.
- Supports server-side date, level, message, source-context, correlation-ID, and exception filters with bounded pagination.
- Limits page size to 100 and the reachable page window to 100 pages; the reader retains only the requested page window in memory.
- Redacts common secret assignments, bearer/JWT tokens, connection strings, email addresses, and phone-like values before display.
- Truncates messages and exception continuation text; exposes no absolute file paths.
- Access is restricted to SuperAdmin and Admin accounts explicitly granted `system.logs.view`. The permission is visible in the permission catalog but is not automatically added to standard Admin templates.
- No database schema, migrations, external packages, retention-policy changes, log exports, or Production settings changes.

**Validation still required:** run the new focused `ApplicationLogQueryServiceTests`, build the MVC project, run the full test suite, and verify SuperAdmin access / Admin permission granted and denied in the local TEST environment. Existing Phase09 smoke checks are not being repeated.
