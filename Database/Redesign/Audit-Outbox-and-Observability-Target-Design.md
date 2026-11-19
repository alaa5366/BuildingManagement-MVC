# Audit, Outbox and Observability Target Design

**Date:** 2026-11-18  
**Branch:** development/database-redesign-enterprise  
**Status:** Target platform design. No audit/log schema or runtime behavior changed.

## 1. Separate three information streams

### Business and security audit
Records who performed a business/security action, against which resource, under what scope, with what outcome and reason. It is durable evidence, append-only, access-controlled, and retained under an approved policy.

### Application diagnostics
Records operational events used to diagnose errors, performance, dependencies, and runtime behavior. It is not a substitute for business audit and may have shorter retention. It must be redacted and access-restricted.

### Integration delivery history
Records durable outbox/inbox processing, provider attempts, retry state, and external correlation IDs. It is not a general application log and must not contain secret payloads.

Never merge these streams into one table or give them identical access policies merely because they all contain timestamps and messages.

## 2. Proposed audit event contract

Logical fields:
- AuditEventId;
- OccurredAtUtc;
- ActorAccountId and resolved ActorPersonId when applicable;
- ActorType (human account, system job, integration);
- SubjectAccountId/PersonId where relevant;
- ActionKey and outcome;
- ResourceType, ResourceId, BuildingId, ApartmentId;
- PermissionKey / scope decision reference where appropriate;
- ReasonCode and safe ReasonText;
- CorrelationId and causation/source event reference;
- SourceChannel;
- safe before/after change summary;
- schema/version marker.

Do not store:
- passwords, PINs, hashes, TOTP secrets, recovery codes, API tokens, decrypted DVR passwords;
- ballot choices or identity-to-ballot correlation;
- raw OCR text or receipt image content;
- full sensitive request/response bodies;
- unnecessary personal/financial details.

Sensitive before/after values should be represented as field names and redacted change indicators, not raw secret values.

## 3. Atomicity model

For critical state changes:
1. Validate actor, resource, permission, invariants and concurrency.
2. Execute the business state transition in a database transaction.
3. Insert the audit event (or durable audit outbox envelope) in the same transaction.
4. Insert external side-effect outbox messages in the same transaction.
5. Commit once.
6. Publish/process outbox messages asynchronously and idempotently.
7. Record attempt count, next retry, last safe error code, completion or dead-letter state.

If the business transaction fails, no success audit event or external dispatch should be published. If an external provider fails after commit, the durable operation remains visible as pending/retryable and must not be falsely reported as completed.

For operations whose audit must be guaranteed, audit failure fails the command transaction. For low-risk diagnostics, diagnostic writer failure must not roll back business data; these are different requirements.

## 4. Outbox/inbox contract

Outbox message fields:
- MessageId;
- Type/SchemaVersion;
- OccurredAtUtc;
- AggregateType/AggregateId;
- CorrelationId/CausationId;
- safe payload;
- Status, Attempts, NextAttemptAtUtc;
- LockedUntilUtc/LeaseOwner where needed;
- LastErrorCode (sanitized);
- ProcessedAtUtc.

Inbox/deduplication stores the external message/provider idempotency key and processing outcome.

Rules:
- at-least-once delivery with idempotent handlers;
- no assumption of exactly-once external side effects;
- bounded retries with exponential backoff and jitter;
- dead-letter with operator replay workflow;
- provider calls outside long-running SQL transactions;
- compensation/reconciliation for orphaned uploads or partial media provisioning;
- distributed lease only when deployment is multi-instance or overlapping workers are possible.

## 5. Audit taxonomy and mandatory audited operations

At minimum:
- Person/account create, link, separate, disable, archive, recovery and credential reset;
- role assignment/revocation and permission override;
- MFA enrollment/disable/recovery and session revocation;
- impersonation start/stop/failed attempt and target;
- ownership/occupancy/access grant changes;
- deposit create/confirm/cancel/reverse;
- expense/revenue post/reverse and wallet adjustment;
- period close/reopen and allocation;
- poll create/open/close/finalize/annul, but never ballot choice;
- document upload/download/expiry and receipt access;
- camera/DVR credential/configuration/media access;
- integration secret rotate/test and message dispatch;
- backup create/verify/download/restore/delete and retention;
- import batch preview/approve/commit/cancel;
- privileged tools and data sync.

Every audited action has an outcome (success, denied, validation failure, conflict, partial/retryable failure) and safe reason code.

## 6. Query and access model

- System-wide audit requires explicit System scope.
- Building audit is filtered by currently authorized building scope in SQL.
- Personal audit is filtered by Person/account subject.
- Apartment audit is a separate explicit scope; do not infer it from the legacy audit.view.own label.
- Export is a distinct permission from interactive viewing for high-sensitivity streams.
- Application logs require a separate privileged capability and sanitized projection.
- Pagination is stable (timestamp + unique ID), bounded, and filtered in SQL.
- Sensitive audit queries and exports are themselves audited.
- Read models never expose raw payloads or secrets.

## 7. Operational logs and observability

- Structured logs with correlation/trace IDs and stable event codes.
- Redaction before serialization and a second defense-in-depth filter at storage/query.
- Do not log raw query strings containing QR tokens or capability credentials.
- Do not log full request bodies for authentication, financial, receipt, import, or secret operations.
- Avoid host/IP/user-agent retention beyond the documented operational need.
- Application log explorer uses indexed/centralized storage if the deployment grows; local rolling files are not a cross-instance source of truth.
- Query budgets and retention are explicit.
- Health endpoints expose minimal liveness publicly only to trusted infrastructure; readiness/dependency detail is restricted to monitoring networks.
- Metrics must not contain high-cardinality personal identifiers or secrets.

## 8. Retention and tamper evidence

Owner decisions DEC-26/DEC-37 must define:
- retention by audit class (security, finance, identity, poll, operational);
- legal hold behavior;
- who can query/export;
- immutable/WORM or tamper-evidence requirement;
- backup retention and encryption;
- deletion/anonymization rules for personal fields while preserving required financial/security evidence.

Do not delete audit rows when an account or Person is archived. Use restrictive references, privacy-safe pseudonymization where legally required, and a separate approved retention process.

## 9. Tests

- Business mutation and audit insert commit/rollback atomically.
- Audit writer failure follows the specified critical/noncritical policy.
- Outbox dispatch retries safely and does not duplicate provider side effects.
- Dead-letter replay is idempotent.
- No secrets, raw OCR, or ballot choices appear in logs/audit.
- Resource-scope filters cannot leak cross-building events.
- Audit export is separately authorized and audited.
- Retention/hold rules are enforced and tested.
- Health endpoints do not disclose database/server secrets.
- Correlation survives MVC -> Application -> Infrastructure -> background worker.
- Multi-instance invalidation/leases tested if topology requires them.

## 10. Owner decisions

- DEC-26/DEC-37: retention, legal hold, tamper evidence and privileged audit access.
- DEC-27: single vs multi-instance deployment, affecting distributed outbox leases, cache/session invalidation and centralized log storage.
- Recommendation: keep business/security audit durable and transactional from the first redesigned module; keep diagnostic logs separate and redacted.

## 11. Safety status

This is a target design only. No tables, indexes, log configuration, audit code, or outbox worker were created or changed. No tests/build were run. Gate A remains NOT AUTHORIZED.
