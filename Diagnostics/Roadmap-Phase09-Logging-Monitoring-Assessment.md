# Original Roadmap Phase09 — Logging & Monitoring Assessment

**Branch:** `development/roadmap-phase09-logging-monitoring`  
**Baseline:** `development/roadmap-phase08-authentication-2fa`  
**Review type:** Source inspection and implementation assessment  
**Database / Production:** Not accessed

## Existing capabilities confirmed

- Serilog host integration and bootstrap logging.
- Daily rolling files under `logs/serilog`, 14-file retention, 10 MB size limit.
- ASP.NET request-completion events with method, path, status and elapsed time.
- Server-generated per-request correlation ID in `X-Correlation-ID`, request trace, logging scope and audit metadata.
- Append-only protection for tracked EF Core audit updates/deletes.
- Existing SQL database health check.
- Background backup scheduler emits lifecycle and failure logs.

## Findings and remediation

1. **Structured properties were not persisted to file.** The file output template omitted `{Properties:j}`, so correlation and request diagnostic properties were absent from the text file. Fixed by rendering structured properties in the rolling-file template.
2. **Liveness and readiness were conflated.** Added `/health/live` with no dependency checks and `/health/ready` with the SQL readiness check. The existing `/health` path remains a readiness alias for compatibility.
3. **Health response contract was implicit.** Added a minimal JSON response writer exposing aggregate status, duration, and check name/status only. It does not serialize descriptions, exception messages, stack traces, or health-check data.
4. **Fatal termination exit status was ambiguous.** The top-level fatal exception handler now sets `Environment.ExitCode = 1`.
5. **Some startup diagnostics bypassed Serilog.** Storage selection, legacy-secret migration results/failures, and font registration failures now use structured Serilog events.

## Scope decisions

- Reuse existing Serilog and health-check packages; no external monitoring vendor.
- Preserve daily rolling, 10 MB cap, 14-file retention, log directory, and request severity rules.
- Preserve `/health` behavior as readiness.
- No database schema/data changes, migrations, new audit columns, or Production access.
- No hosted metrics backend, alerting service, distributed tracing exporter, log shipping, or dashboard; these require deployment/platform decisions and credentials outside this repository.

## Validation status

Implementation and focused health-response tests are committed. Local build, full test suite, and practical TEST smoke checks remain pending user execution.
