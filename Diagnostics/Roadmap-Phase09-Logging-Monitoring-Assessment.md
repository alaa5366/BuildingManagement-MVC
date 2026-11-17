# Original Roadmap Phase09 — Logging & Monitoring Assessment

**Branch:** `development/roadmap-phase09-logging-monitoring`  
**Baseline:** `development/roadmap-phase08-authentication-2fa`  
**Review type:** Read-only source inspection before implementation  
**Database / Production:** Not accessed

## Existing capabilities confirmed

- Serilog is configured as the ASP.NET Core host logger.
- Bootstrap logging captures startup failures.
- Daily rolling file logs are written under `logs/serilog`, with a 14-file retention limit and 10 MB size limit.
- ASP.NET request-completion logging records method, path, status code, and elapsed time.
- Requests receive a server-generated correlation ID in `X-Correlation-ID`; the ID is also attached to the request trace and logging scope.
- Audit events are persisted to the existing `AuditLog` table and include the request correlation ID in the JSON details when called within an HTTP request.
- Audit rows are protected from tracked EF Core updates and deletes.
- A database health check is registered and exposed at `/health`.
- The background backup scheduler writes start/stop and failure events through `ILogger`.

## Gaps and risks confirmed

1. The rolling-file output template omits `{Properties:j}`. Consequently, structured properties such as `CorrelationId`, `RequestHost`, `RemoteIp`, and `UserAgent` are not rendered into the text log file, weakening practical correlation and diagnostics.
2. The single `/health` endpoint combines process availability with database readiness. Orchestrators and operators cannot distinguish a live process from an application that is not ready to serve database-dependent traffic.
3. The default health-check response writer does not provide a stable, intentionally minimized JSON contract. A custom response should expose only aggregate/component status and must not serialize exception details, connection strings, or other internals.
4. The application-level fatal exception handler logs the startup/runtime termination but does not explicitly set a non-zero process exit code when the top-level catch handles the exception.
5. There are no focused tests for the health response contract or its non-disclosure behavior.

## Decisions for this phase

- Keep the existing Serilog package and file sink; do not introduce a new logging vendor or external service.
- Preserve daily rolling, size cap, 14-file retention, existing log directory, and existing request event levels.
- Render structured properties in file output so correlation data is actually searchable.
- Add separate liveness and readiness endpoints. Liveness must not depend on SQL; readiness must execute the registered database check.
- Preserve `/health` as a compatibility alias for readiness.
- Return a minimal JSON health document containing only overall status and check names/statuses. Never expose exception messages or connection details.
- Set a non-zero process exit code when the top-level exception handler catches a fatal application failure.
- No database schema changes, no new audit columns, no new external monitoring dependency, and no Production access.

## Scope boundary

This phase improves the existing operational logging and health-check foundation. It does not add a hosted metrics backend, alerting service, distributed tracing exporter, log shipping, or a monitoring dashboard; those require deployment/platform choices and credentials outside this repository.
