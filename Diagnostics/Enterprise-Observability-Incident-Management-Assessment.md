# Enterprise Observability & Incident Management Center
## Unified Phase Assessment and Implementation Specification

**Status:** Approved scope; implementation in progress (unified assessment committed; first evidence-based component filter implemented)  
**Working branch:** `development/phase14-enterprise-logs-azure-ui`  
**UI direction:** Azure Portal-inspired Light Enterprise, Arabic RTL and English LTR  
**Primary rule:** All observability, diagnostics, monitoring, incident-management, and related fixes are delivered in this one phase and this one branch. Do not create separate phases or branches for sub-features.

---

## 1. Objective

Evolve the existing Application Logs page into a practical, secure, read-only-by-default Enterprise Observability & Incident Management Center for BuildingManagement-MVC. The center must help administrators answer:

1. What happened?
2. Where did it happen (Controller, Service, View, Middleware, Repository, database, background task, or external dependency)?
3. Who/what was affected, where supported by trustworthy telemetry?
4. Why did it happen, and which events belong to the same request or incident?
5. Is the system healthy, degrading, or recovering?
6. Has the incident been assigned, mitigated, and resolved?

The implementation must be incremental inside this same branch, with one consolidated assessment/backlog and continuous build/test/review.

## 2. Existing implementation verified on this branch

The current implementation is a useful foundation, not a blank slate:

- `ApplicationLogsController` is authorized for `superadmin,admin`, additionally guarded by `AdminPermission(ViewApplicationLogs)`, disables response caching, and applies the `lookup` rate-limit policy.
- Current GET filters: UTC date range, level, free-text search, CorrelationId, SourceContext, exception presence, page, and page size.
- `ApplicationLogEntry` currently exposes Timestamp, Level, Message, SourceContext, CorrelationId, Exception, Details, FileName, and HasException.
- `ApplicationLogQueryService` reads retained Serilog rolling text files from `logs/serilog`, currently bounded to the most recent 14 files; it sanitizes sensitive values and filesystem paths, limits page sizes, supports cancellation, and returns paged results.
- The current view model is intentionally small: query + paged results.
- Serilog writes structured properties to daily rolling files, with 14-file retention and a 10 MB file size limit.
- The application already registers a database health check and has a separate AuditLog service/query path.
- The Azure-inspired Logs Explorer UI and scoped CSS are already present on this branch.

**Important current data gaps:** the current log entry/query contract does not yet expose dedicated RequestPath, HTTP method/status, duration, user/role, component type, component name, SQL operation timing, deployment version, or job execution metadata. Do not present filters or charts for these fields as operational until reliable instrumentation and parsing/storage exist.

## 3. Functional scope — all included in this single phase

### 3.1 Monitoring Overview
- System status summary and configurable time window.
- Request/event volume, Error/Fatal count and rate, Warning count, exception count, slow requests, and health-check status where measurable.
- Time-series charts and severity/component distributions.
- Top failing components and slowest endpoints only when telemetry supports them.
- Clear time zone labeling and consistent filter/time-window behavior.
- Distinguish current-page counts from whole-query/system totals.

### 3.2 Application Logs Explorer
- Preserve and improve current date, level, text, source, correlation, exception, and paging filters.
- Add quick time ranges (15 minutes, 1 hour, 24 hours, 7 days) where the query engine can honor them.
- Advanced filters for component type/name, request path, HTTP method/status, user/role, duration, and exception type after instrumentation is available.
- Selectable columns, sorting, saved searches, copy actions, CSV/JSON export, and clear empty/error states.
- Keep search bounded, rate-limited, cancellation-aware, and safe against expensive broad scans.

### 3.3 Code-Level Investigation
- Component categories: Controller, Service, View, Middleware, Repository/Data Access, Background Job, Model/Validation, External Dependency, and Other.
- Component name, namespace, method, exception type, source file/line (only when available), and related request.
- Infer a component only from trustworthy SourceContext, exception stack trace, explicit instrumentation, or known route metadata.
- Do not claim that a View caused a failure merely because the request rendered a page; instrument view rendering only if a reliable and low-overhead approach is justified.
- Component drill-down must preserve the time range and relevant filters.

### 3.4 Event Investigation and Request Tracing
- Event detail panel with sanitized message, exception, stack trace, structured properties, source, timestamp, and file.
- Correlation/request timeline with preceding and following events.
- Request metadata: path, method, status, duration, trace/request IDs, and outcome.
- User journey/request grouping only where IDs and privacy-safe identifiers support it.
- Never expose credentials, tokens, cookies, connection strings, secrets, private resident data, or raw sensitive payloads.

### 3.5 Performance Monitoring
- Request duration and slow-request thresholds.
- Controller/action and service operation timing using low-overhead instrumentation.
- Database operation timing and failure/timeout counts where EF Core/SQL instrumentation can provide reliable data.
- Percentiles (p50/p95/p99) only from a sufficiently representative sample; label sample size and time window.
- Compare trends and detect regressions without inventing baselines.

### 3.6 Database Monitoring
- Database connectivity and readiness.
- Query/command duration, timeout, failure, and deadlock indicators when observable.
- Connection-pool exhaustion indicators only if a reliable source exists.
- EF Core operation telemetry and SQL dependency correlation.
- No query text or parameters containing personal or secret data in the UI by default.
- No intrusive diagnostic query or production database mutation.

### 3.7 System Health and Dependencies
- Reuse existing ASP.NET Core Health Checks and extend with meaningful dependency checks.
- Application readiness/liveness distinction where appropriate.
- Database and configured external dependency health.
- CPU, memory, disk, and process metrics only where hosting environment and permissions support reliable collection.
- Show last check time, duration, state, and sanitized failure reason.
- Do not assume access to host-level metrics on restricted/shared hosting.

### 3.8 Background Jobs and Scheduled Tasks
- Inventory known hosted/background services and scheduled tasks.
- Execution history, start/end time, duration, outcome, retry count, and last successful run where instrumented.
- Detect missed/late runs only when a schedule is explicitly known.
- Include the existing backup scheduler as an initial candidate after code inspection.
- Do not add a new job framework solely for dashboard appearance.

### 3.9 Proactive Detection and Error Fingerprinting
- Group equivalent errors by stable fingerprint (exception type + normalized message + stable stack/source signature).
- Detect first-seen errors, repeated failures, spikes, and sustained degradation.
- Suppress duplicate alerts within configurable windows while preserving occurrence counts.
- Make thresholds configurable and explain the rule that triggered each signal.
- Avoid high-cardinality fingerprints and redact sensitive content before fingerprinting.

### 3.10 Impact Analysis
- Estimate affected requests/users/components from available telemetry.
- Add building/apartment impact only where safe, authorized, and explicitly captured by instrumentation.
- Clearly label estimates and sample coverage.
- Never infer affected residents or buildings from unrelated log text.

### 3.11 Incident Management
- Incident lifecycle: New, Investigating, Mitigated, Resolved, and Reopened.
- Severity/priority, title, summary, owner/assignee, timestamps, notes, linked fingerprints/events, root cause, mitigation, and resolution.
- Group related occurrences without duplicating incidents.
- Audit every incident state/assignment change.
- Keep incident persistence isolated and permission-protected; schema/migration changes require review and tests.
- No automatic destructive remediation.

### 3.12 Alerts and Notifications
- Configurable rules for critical errors, error spikes, slow requests, failed health checks, and missed jobs.
- Threshold, evaluation window, severity, cooldown/deduplication, enabled state, and recipient/channel configuration.
- In-app notifications first; external channels only if an approved provider is configured.
- Alert delivery history, acknowledgement, resolution, and failure visibility.
- Prevent notification storms and do not send external notifications without explicit configuration.

### 3.13 Security Monitoring and Audit Integration
- Security events: failed authentication, authorization denials, rate-limit rejections, suspicious repeated access, and privileged changes where logged.
- Keep Application Logs, Security Events, and Audit Trail logically distinct.
- Provide correlation/navigation between them without merging their data stores or weakening their independent authorization.
- Review access to the observability center and audit sensitive administrative actions.
- Preserve existing role, building-scope, and permission semantics.

### 3.14 Configuration, Deployment, and Release Correlation
- Show application version/build/environment/start time when available.
- Correlate incident onset with known deployment/release markers.
- Track approved operational configuration changes without recording secret values.
- Feature-flag state only if the application actually uses a supported feature-flag source.
- Do not infer deployment causality from temporal proximity alone.

### 3.15 Diagnostic Toolkit
- Read-only checks for database connectivity, health checks, configured dependencies, required storage paths, and background-service state.
- Diagnostic bundle/report with timestamp, environment, version, check results, and relevant sanitized event references.
- Bound execution time and rate; require appropriate permission.
- No shell execution, arbitrary SQL, filesystem mutation, restart, or remediation actions from the UI.

### 3.16 Log Quality, Retention, and Storage
- Validate structured properties, correlation coverage, exception completeness, and event quality.
- Track log file count/size and read/enumeration failures.
- Retention policy visibility and safe cleanup only after explicit policy, dry-run, and tests.
- Export with permission checks, bounded date range/size, safe CSV escaping, and sensitive-data redaction.
- Preserve current retention behavior until a reviewed replacement is implemented.
- Consider structured JSON/compact event storage only after comparing compatibility, migration, performance, and operational cost.

### 3.17 Reports and Administration
- Daily/weekly operational summaries and incident reports.
- Saved searches and configurable dashboard time windows.
- Arabic/English localization, RTL/LTR, responsive layout, accessibility, and consistent empty/loading/error states.
- Granular permissions for viewing logs, viewing security events, running diagnostics, managing alerts, and managing incidents.
- Export and administrative actions separately authorized and audited.

### 3.18 BuildingManagement domain coverage
Assess and instrument the following areas where useful and technically supported:
- Buildings, Residents, Apartments, Cameras.
- Authentication, sessions, 2FA, authorization, and impersonation.
- Presence/Heartbeat.
- AuditLog.
- Notifications.
- Backup scheduler and other hosted services.
Instrumentation must preserve domain authorization, building scoping, privacy, and existing behavior.

## 4. Delivery order inside this one phase

These are implementation workstreams, not separate phases or branches. Keep one branch and one consolidated assessment.

1. **Foundation and correctness:** inspect current code/tests; preserve the current explorer; add missing tests; validate masking, paging, cancellation, rate limiting, and authorization.
2. **Telemetry contract:** define bounded, versioned event metadata and add request/component/error metadata using ASP.NET Core logging/diagnostics and supported EF Core hooks.
3. **Investigation UX:** component filters, event detail, correlation timeline, request metadata, saved searches, safe export.
4. **Overview and performance:** accurate metrics/charts and slow-operation analysis from collected telemetry.
5. **Health and jobs:** extend health checks and instrument actual hosted/scheduled work.
6. **Fingerprinting and incidents:** grouping, lifecycle, ownership, notes, root-cause/resolution.
7. **Alerts and security:** deduplicated rules, notifications, security-event views, Audit integration.
8. **Operations and reporting:** diagnostic bundles, retention/storage governance, deployment correlation, reports, UX/accessibility polish.

A workstream may be reordered based on dependencies, but it remains in this same phase.

## 5. Engineering constraints and non-goals

- No production database access, migration execution, deployment, or production configuration change as part of development.
- No merge to `main` without explicit user approval.
- No additional phase branches for features in this scope.
- No fabricated telemetry, metrics, health states, component attribution, or impact counts.
- No automatic remediation or arbitrary command execution.
- No weakening of authorization, rate limits, anti-forgery, or data masking.
- No mixing Application Logs with Audit Trail records.
- No sensitive values in logs, diagnostics, fingerprints, exports, or incident reports.
- Avoid expensive full-file scans on every dashboard widget; use bounded queries, caching with clear freshness, indexing/aggregation, or a suitable storage design after measurement.
- Preserve backward compatibility with existing Serilog rolling files during any telemetry evolution.
- All user-visible text must be localized in Arabic and English.
- Any new database schema requires explicit migration review, tests, and user approval before production rollout.

## 6. Testing and acceptance criteria

### Functional
- Every visible filter is backed by real query semantics and tested.
- Component attribution is traceable to a documented source of metadata.
- Event detail and correlation views show only related events and preserve authorization.
- Dashboard totals have documented scope (current page, filtered result, or whole-system aggregate).
- Incident transitions, assignments, notes, deduplication, and audit history work consistently.
- Alerts honor thresholds, cooldowns, and deduplication.

### Security and privacy
- Existing role and permission checks remain enforced.
- Unauthorized users cannot access logs, diagnostics, incidents, alerts, or exports.
- Sensitive-data masking is tested against messages, exception text, structured properties, and exports.
- No secret-bearing configuration or personal data is exposed.
- CSV/JSON export is bounded and safe.

### Reliability and performance
- Cancellation, I/O errors, missing files, malformed lines, and partial telemetry are handled safely.
- Dashboard and search operations have bounded cost and do not block normal application requests.
- Instrumentation overhead is measured and controlled.
- No unbounded in-memory retention or high-cardinality metric dimensions.

### Quality gates
- `dotnet build` succeeds.
- `dotnet test Tests/BuildingManagement.Tests` succeeds.
- New tests cover query parsing/filtering, component metadata, sanitization, correlation, health checks, incidents, alert deduplication, and permissions as features are implemented.
- Review Git diff and manually inspect the UI in Arabic RTL and English LTR.
- No merge or production deployment until the user reviews and approves.

## 7. Current implementation status

| Capability | Current status |
|---|---|
| Azure-inspired Application Logs UI | Present on this branch; requires local visual review |
| File-based Serilog reader | Present |
| Date/level/text/source/correlation/exception filters | Present |
| Paging, cancellation, bounded retained-file scan | Present |
| Sensitive-data/path sanitization | Present; continue security testing |
| Request path/method/status/duration | Not yet in current entry/query contract |
| Component type filter | Implemented from explicit SourceContext classification (Controller, Service, View, Middleware, Repository, Database, Background Job, External Dependency, Other); unknown metadata is not guessed |
| Event detail/correlation timeline | Not yet implemented as a dedicated investigation experience |
| Monitoring overview aggregates/charts | Not yet implemented |
| Existing database health check | Registered; dashboard integration/expansion pending |
| Performance/EF Core telemetry | Requires assessment and instrumentation |
| Background-job execution history | Requires instrumentation |
| Fingerprinting/incidents/alerts | Not yet implemented |
| Security events/Audit correlation | Separate Audit infrastructure exists; integration requires design |
| Export/saved searches/retention administration | Requires implementation and security review |

## 8. Immediate next actions

1. Run the branch locally and capture the Azure UI in Arabic and English.
2. Run build and the complete test suite; fix any regressions before expanding.
3. Inspect the remainder of `ApplicationLogQueryService`, Serilog configuration, middleware pipeline, health checks, AuditLog query model, and hosted services.
4. Produce a code-backed telemetry inventory: which fields are already emitted, parsed, masked, and queryable.
5. Extend Code-Level Investigation from the implemented SourceContext-based component filter; add request/method/line-level attribution only after reliable instrumentation is designed.
6. Update this same assessment as decisions, gaps, implementation status, and test evidence change.

---

**Decision record:** The user explicitly approved one unified, comprehensive phase containing all observability ideas, improvements, bug fixes, and related enhancements. Keep all work in `development/phase14-enterprise-logs-azure-ui`; do not split it into multiple phases or branches.
