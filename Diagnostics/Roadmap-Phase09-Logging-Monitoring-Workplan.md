# Original Roadmap Phase09 — Logging & Monitoring Workplan

**Status:** Implementation in progress  
**Branch:** `development/roadmap-phase09-logging-monitoring`  
**Base:** `development/roadmap-phase08-authentication-2fa`  
**Database:** No schema or data changes  
**Production:** Explicitly excluded

## Objectives

1. Make structured Serilog properties, especially request correlation IDs, visible in rolling file logs.
2. Separate process liveness from database readiness.
3. Return a stable, privacy-safe JSON response from health endpoints.
4. Ensure fatal application termination is reflected by a non-zero process exit code.
5. Add focused automated tests for the health response contract and information disclosure.

## Implementation tasks

- Update the rolling file output template to include structured properties.
- Add a dedicated health response writer that exposes status and check names/statuses only.
- Tag the database health check as readiness-dependent.
- Map `/health/live` to a no-dependency liveness check.
- Map `/health/ready` to readiness checks and retain `/health` as its backward-compatible alias.
- Set `Environment.ExitCode = 1` when the top-level fatal exception handler catches an exception.
- Add tests for healthy/unhealthy JSON output and verify exception details are not disclosed.

## Acceptance criteria

- Request log files include `CorrelationId` and other structured diagnostic properties.
- A healthy liveness endpoint does not attempt a database check.
- Readiness and the legacy `/health` endpoint report database health.
- Health response JSON contains no exception message, stack trace, or connection information.
- Fatal startup/runtime failure returns a non-zero process exit code.
- Full local build and test suite pass.
- User performs the practical TEST smoke checks before phase acceptance.
- No migrations, SQL changes, or Production modifications.
