# Phase11 — Assessment

## Existing baseline

- The application already uses Serilog request logging with log-context enrichment and daily rolling files.
- Audit events are persisted by `AuditLogger` to the existing `AuditLog` table.
- The audit entity has no dedicated correlation column, and changing the schema was intentionally excluded from this phase.
- The audit logger previously serialized caller metadata without a request-level trace key.

## Findings addressed

1. Requests did not have a stable application-owned correlation identifier exposed to the caller.
2. Request logs and audit event details could not be reliably joined using a shared request identifier.
3. Trusting a caller-supplied correlation header would allow spoofed trace identifiers.
4. Normal EF Core tracked updates/deletes had no explicit application-level guard against changing persisted audit rows.

## Design decision

Generate the correlation ID inside the application, ignore any inbound value, expose it in `X-Correlation-ID`, and propagate it through the request trace/logging scope. Store it inside the existing JSON audit Details payload to avoid a schema migration.

## Remaining limitations

- Append-only enforcement covers tracked EF Core SaveChanges operations. It does not prevent privileged direct SQL, ExecuteUpdate/ExecuteDelete, or database referential actions such as the existing building foreign-key SET NULL behavior. SQL permission hardening requires a separately approved database deployment.

- The correlation identifier is scoped to one HTTP request. Background jobs have no HTTP correlation ID unless a future job-specific correlation strategy is introduced.
- AuditLog remains append-only by convention and service usage; database-level immutability/permissions are not asserted by this phase.
- Existing audit records are not backfilled.
