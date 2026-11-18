# Target Architecture and Class Refactoring Roadmap

**Date:** 2026-11-18  
**Branch:** `development/database-redesign-enterprise`  
**Status:** Proposed target design derived from the 31-section source review and restored-database evidence. This is an architecture baseline, not implementation authorization.

## 1. Architectural direction

The target is a modular monolith with explicit module boundaries, one deployable application initially, and the option to extract selected modules later without redesigning their contracts.

The redesign is not a controller-by-controller patch. Controllers, services, entities, views, jobs, and database access paths are all eligible for replacement. Existing behavior is retained only when it is an approved business rule or a required compatibility contract.

### Non-negotiable principles

1. One human is represented by one Person; a Person has zero or one interactive UserAccount.
2. Ownership, occupancy, apartment access, account status, and authorization are separate concepts with effective dates and provenance.
3. Authorization is evaluated server-side against account status, role grants, scope, resource, explicit overrides, and validity dates. UI visibility and claims alone never authorize an operation.
4. Financial history is immutable after posting. Corrections use reversal/void/adjustment workflows with actor, reason, timestamp, and correlation.
5. Identity, permission, financial, security, and administrative mutations are auditable and recoverable.
6. Secret voting stores eligibility evidence separately from ballot choice.
7. External side effects are coordinated through durable outbox/inbox patterns and idempotent handlers.
8. Read paths are read-only; commands own state transitions.
9. Every query is scoped, bounded, projected, and cancellation-aware.
10. Migration is additive, restartable, observable, reconcilable, and rehearsed against a disposable restored database.

## 2. Solution and dependency shape

Recommended logical projects/namespaces (whether or not physically split into projects in the first implementation):

- **Web** — MVC controllers, filters, view models, endpoint composition, HTTP concerns.
- **Application** — use cases, commands/queries, validators, authorization requirements, orchestration, DTOs.
- **Domain** — aggregates, value objects, invariants, domain events, state-transition rules. No EF, HTTP, filesystem, or provider dependencies.
- **Infrastructure** — EF Core persistence, SQL repositories, cryptography adapters, file/object storage, mail/WhatsApp, OCR, camera/media adapters, scheduler and external integrations.
- **Contracts** — stable request/response contracts shared only where needed; avoid turning this into a dumping ground.
- **Tests** — domain, application, persistence integration, web authorization, migration/reconciliation, and end-to-end acceptance suites.

Dependency rule: Web -> Application -> Domain. Infrastructure implements Application/Domain-facing ports and is wired at the composition root. Domain never references Application, Web, EF Core, SQL Server, or vendor SDKs.

Avoid a generic repository over every DbSet. Use repositories only around aggregate persistence and complex query ports. Use dedicated query handlers/read models for reporting and dashboards.

## 3. Target modules and ownership boundaries

### 3.1 Identity and Accounts
Owns Person, UserAccount, credentials, account lifecycle, identity proofing, legacy identity provenance, and account recovery.

Candidate application services:
- `PersonRegistrationService`
- `IdentityResolutionService`
- `UserAccountLifecycleService`
- `CredentialService`
- `AccountRecoveryService`
- `LegacyIdentityMappingService`

Candidate ports:
- `IPersonRepository`, `IUserAccountRepository`, `ICredentialStore`, `ILegacyIdentityMapRepository`.

Rules: no matching/merging solely by name, phone, email, apartment, or shared contact. Preserve source-row keys and unresolved cases. Enforce one account per Person transactionally and with a database uniqueness constraint.

### 3.2 Authentication, MFA and Sessions
Owns sign-in flows, credential verification, MFA enrollment/challenge/recovery, session validation, revocation, security stamps, and rate-limit policy.

Candidate services:
- `AuthenticationOrchestrator`
- `PasswordOrPinPolicy` (separate credential policies; do not trim secrets)
- `MfaEnrollmentService`
- `TotpVerificationService`
- `RecoveryCodeService`
- `SessionValidationService`
- `SessionRevocationService`

Admin and SuperAdmin MFA is mandatory through a staged enrollment/exception/bootstrap process. Residents may use optional MFA. A verified second factor is a server-validated session fact, not a user-controlled claim. Unknown roles fail closed. Revocation and permission changes invalidate sessions within an explicitly approved maximum window.

### 3.3 Authorization and Access
Owns role definitions, role permissions, scoped role assignments, direct allow/deny overrides, resource-scope evaluation, and audit of grants/revocations.

Candidate services:
- `AuthorizationEvaluator`
- `RoleAssignmentService`
- `PermissionOverrideService`
- `AccessScopeResolver`
- `AuthorizationPolicyCatalog`

Scopes: System, Building, Apartment, Personal. Deny wins over allow. Effective dates, actor, reason, source, and revocation are retained. Legacy flat UserPermissions must be mapped only after each permission's real consuming-resource scope is reviewed. Do not copy flat grants into every building.

### 3.4 Property, Ownership, Occupancy and Resident Access
Owns Buildings, Apartments, effective-dated ownership shares, occupancy periods, primary occupant designation, and explicit apartment access grants.

Candidate services:
- `PropertyManagementService`
- `OwnershipRegistryService`
- `OccupancyLifecycleService`
- `ApartmentAccessService`

Ownership does not imply occupancy; occupancy does not imply legal ownership; neither automatically grants an interactive account. Enforce non-overlapping primary-occupancy intervals and valid share totals using transactional validation. Keep historical relationships; do not cascade-delete them.

### 3.5 Building Administration
Owns building operational settings and building-scoped administrator assignments, but not global account identity or permission evaluation.

Candidate services:
- `BuildingAdministrationService`
- `BuildingAdminAssignmentService`
- `BuildingSettingsService`

Grant, replace, and revoke are distinct commands. Never interpret an omitted BuildingId as a revocation unless the command explicitly means full replacement. All assignment changes invalidate affected sessions and produce durable audit records.

### 3.6 Apartment Wallet and Financial Ledger
Owns deposits, expenses, revenues, monthly fees/shares, wallet movements, adjustments, reversals, payment evidence, and reconciliation.

Candidate services:
- `DepositCommandService`
- `ExpenseCommandService`
- `RevenueCommandService`
- `WalletLedgerService`
- `MonthlyAllocationService`
- `FinancialReconciliationService`
- `FinancialPeriodService`

Use explicit state machines (Pending -> Confirmed or Cancelled; posted entries -> Reversed/Void through compensating entries). Enforce idempotency keys, optimistic concurrency, strict month parsing, target-building/category validation, and transactional balance updates. Never physically delete posted financial records. Preserve legacy wallet formulas and source rows until a parallel-run reconciliation proves parity. Define rounding/remainder allocation deterministically.

Reports must distinguish cash movement, wallet liability/balance, income, expense, carryover, pending deposits, and period totals. A report's label must match its formula and time semantics.

### 3.7 Polls and Secret Ballots
Owns poll lifecycle, eligibility snapshots, delegated apartment representation, ballot issuance, anonymous ballot storage, and tallying.

Candidate services:
- `PollLifecycleService`
- `VotingEligibilityService`
- `VotingDelegationService`
- `BallotIssuanceService`
- `SecretBallotBox`
- `PollTallyService`

Identity/eligibility and ballot choice must be stored separately. Ballot storage must not contain ApartmentId, PersonId, account ID, or a reversible link to the voter. Prevent duplicate voting with a one-way/nullifier mechanism scoped to poll and eligibility subject. Snapshot eligibility at poll opening or at a documented cutoff. Do not expose selected option in audit logs. Define a controlled tally/finalization process and retained evidence.

### 3.8 Maintenance and Service Requests
Owns request lifecycle, assignment, comments, attachments, expense linkage, and closure.

Candidate services:
- `MaintenanceRequestService`
- `MaintenanceWorkflowService`
- `MaintenanceExpenseLinkService`

Define apartment visibility policy explicitly. If a maintenance cost is posted to finance, retain a durable source link and use reversal/update workflows; edits/deletes must not silently desynchronize the ledger.

### 3.9 Notifications and Presence
Owns recipient resolution, delivery, read state, preferences, and presence semantics.

Candidate services:
- `NotificationDispatchService`
- `NotificationPreferenceService`
- `PresenceSessionService`

Decide whether notifications/read state and presence are per Person, account, or apartment; do not infer one from another. Bound retention and pagination by recipient. Presence counts must state whether they count people, accounts, devices, or apartments.

### 3.10 Cameras and Media
Owns camera/DVR metadata, credential vault references, access policy, and external media provisioning/decommissioning.

Candidate services:
- `CameraRegistryService`
- `DvrCredentialVault`
- `MediaProvisioningOrchestrator`
- `MediaDecommissioningWorker`

Never return entities containing encrypted credentials from broad query methods. Decrypt only in a narrow authorized operation. Model external provisioning/deletion as retryable state transitions with outbox jobs; partial failures must remain visible and recoverable. Media server authorization must be independently enforced.

### 3.11 Files, Receipts, OCR and Exports
Owns secure upload, malware/content validation, private storage, download authorization, OCR assistance, and export generation.

Candidate services:
- `PrivateDocumentStorage`
- `ReceiptUploadService`
- `ReceiptDownloadAuthorizationService`
- `ReceiptOcrService`
- `ExportGenerationService`

Treat OCR as untrusted form-fill assistance only. Payment confirmation requires an independent authorized financial command. Validate decoded file format, dimensions/pixel count, size, and processing time; apply per-account/IP limits. Use private object storage and short-lived authorized downloads. External uploads require compensation/reconciliation if the database transaction fails.

### 3.12 Backup, Restore and Data Operations
Owns backup lifecycle, encryption, artifact metadata, restore validation, retention, and operator actions.

Candidate services:
- `BackupOrchestrator`
- `BackupArtifactStore`
- `BackupIntegrityVerifier`
- `RestoreDrillRunner`
- `BackupRetentionService`

Return typed outcomes. Never report success when a job returned early or failed internally. Reconcile file artifacts and history records. Protect downloads with authorization, no-store, audit, and integrity verification. Scheduler overlap protection must be durable if deployment becomes multi-instance.

### 3.13 Audit, Logs and Observability
Owns durable business/security audit, application diagnostics, correlation, health signals, and retention policy. These are separate streams with distinct access controls.

Candidate services:
- `AuditWriter` (transactional outbox-backed)
- `AuditQueryService`
- `ApplicationLogQueryService`
- `CorrelationContext`
- `HealthCheckPolicy`

Audit records capture actor, subject, action, scope/resource, outcome, reason, correlation, timestamp, and safe before/after metadata. Never log credentials, tokens, OCR raw text, ballot choice, or decrypted secrets. Operational log access must be scoped and bounded. Health endpoints expose only the minimum required information to authorized monitoring networks.

### 3.14 Settings and Integrations
Owns system/building/user preferences and external integration configuration.

Candidate services:
- `SystemSettingsService`
- `BuildingPreferencesService`
- `UserPreferencesService`
- `IntegrationSecretStore`
- `IntegrationHealthService`

Separate ordinary settings from secrets. Secret values are write-only in UI/API, encrypted at rest with managed key rotation, and never returned in view models. GET operations must not create or mutate settings rows.

## 4. Web and UI redesign

Controllers become thin HTTP adapters:
- bind and validate input;
- invoke one application use case;
- map typed outcomes to HTTP responses;
- enforce antiforgery and transport-level protections;
- never contain financial calculations, identity matching, authorization scope logic, or external integration orchestration.

Views use dedicated page/view models, never EF entities or secret-bearing domain objects. All sensitive downloads use explicit authorization and no-store headers. Avoid loading full building aggregates for reports, dashboards, audit, or resident pages. Use paged query handlers and projections.

Suggested UI areas: System Administration, Building Operations, People & Accounts, Property Relationships, Finance & Wallet, Polls & Ballots, Maintenance, Communications, Cameras, Documents, Backups, Audit & Monitoring, and Personal Settings.

## 5. Persistence and data conventions

- Use the existing verified key conventions only after comparing live metadata, EF model, migrations, and SQL artifacts. Do not assume proposed key types are final.
- Use UTC timestamps for system events; preserve business-local dates/months explicitly.
- Use decimal precision and currency rules consistently; no floating-point money.
- Use rowversion/concurrency tokens on mutable aggregates and conditional state transitions.
- Apply restrictive FKs for identity, financial, audit, ownership, occupancy, ballot, and security history.
- Add indexes from measured query patterns and query plans, not speculative duplication.
- Use explicit query projections and bounded pagination; avoid unbounded Include graphs.
- Use SQL views only through one authoritative deployment mechanism (EF migration or versioned SQL deployment), not an undocumented split-brain between FullSchema and migrations.
- Keep SQL-specific operations behind Infrastructure adapters and test them against SQL Server.

## 6. Cross-cutting application patterns

### Commands and queries
Commands mutate one aggregate or coordinate a clearly bounded transaction. Queries return purpose-built read models and never mutate state. Validation is centralized at the application boundary and reinforced by domain invariants/database constraints.

### Transactions and external side effects
Use a database transaction for related state changes and an outbox record. Background workers process outbox messages idempotently. External operations (Cloudinary/media/WhatsApp/email) must have retry, dead-letter, and compensation/reconciliation behavior.

### Error model
Use typed outcomes for expected business states (not found, conflict, invalid transition, forbidden, validation failure, retryable integration failure). Do not convert failures into empty success results.

### Security
Default deny. Explicit role + scoped permission + resource check. Secrets are write-only. Apply antiforgery to state-changing browser requests, no-store to sensitive responses, rate limits to authentication/QR/OCR/export endpoints, and structured redaction before logging.

### Performance
Set query budgets for high-traffic paths. Paginate at database level, use stable ordering, project only needed columns, cache only with explicit invalidation/revocation semantics, and measure before indexing.

## 7. Migration and coexistence strategy

1. Freeze the reviewed legacy schema and source evidence on a restored disposable database.
2. Reconcile all metadata: live inventory, EF model/snapshot/history, FullSchema SQL, runtime queries, views, triggers, and deployment scripts.
3. Approve the target ERD, data dictionary, source-to-target mapping, constraints, and unresolved business decisions.
4. Add new schema only after explicit Gate A authorization. Do not rename/drop legacy columns in this stage.
5. Build an idempotent, restartable migration pipeline with checkpointing, dry-run, row-level outcome codes, and durable `LegacyIdentityMap`.
6. Run identity resolution as a review queue. No fuzzy auto-merges. Keep unresolved records intact.
7. Migrate accounts, role grants, overrides, ownership, and occupancy in separately reconciled batches.
8. Run old and new read models in parallel and compare financial totals, wallet movements, report periods, permissions, and access decisions.
9. Introduce compatibility adapters so old UI/API paths can call new application use cases during staged cutover.
10. Cut over one module at a time behind feature flags, with explicit rollback criteria and an operator runbook.
11. Retire legacy writes only after parity, audit completeness, restore rehearsal, and business sign-off.
12. Drop legacy structures only in a separately approved final decommission phase.

### Rollback requirements
- Restore rehearsal from a known backup.
- Migration checkpoint and restart test.
- Before/after aggregate counts and financial totals.
- Explicit reversal/compensation for any dual-write window.
- Feature-flag rollback that does not discard new authoritative records.
- No rollback plan may depend on recreating deleted legacy data.

## 8. Delivery phases and exit criteria

| Phase | Scope | Exit criteria |
|---|---|---|
| A0 | Evidence and architecture freeze | All 31 review sections mapped to target requirements; schema/code/live metadata discrepancies listed; business decisions tracked |
| A1 | Target model and migration contract | ERD, data dictionary, identity mapping, permissions, finance reconciliation, secret ballot, retention, and key conventions approved |
| A2 | Platform foundations | DI/composition, validation, typed results, clock, current actor, correlation, transaction/outbox, audit, encryption, test infrastructure |
| A3 | Identity and access | Person/account, credentials, MFA, session revocation, scoped authorization; legacy compatibility and negative tests |
| A4 | Property relationships | ownership, occupancy, primary occupant, access grants; temporal overlap and share validations |
| A5 | Finance | ledger commands, idempotency, concurrency, reversals, allocations, reconciliation, report read models |
| A6 | Resident/building operations | maintenance, notifications, presence, settings, profile/preferences |
| A7 | Polls and secret ballot | eligibility snapshot, delegation, anonymous ballot box, duplicate prevention, tally and secrecy tests |
| A8 | Integrations and documents | private receipts, OCR limits, camera/media orchestration, integration secret vault |
| A9 | Backup and operations | backup typed results, retention reconciliation, restore drills, health policy, audit/log explorer |
| A10 | UI and compatibility cutover | new controllers/view models/views, feature flags, module-by-module rollout, accessibility and regression |
| A11 | Legacy retirement | zero unexplained reconciliation differences, approved retention, rollback window closed, explicit decommission approval |

No phase is considered complete solely because code compiles. Each phase requires automated tests, security tests, data reconciliation where relevant, operational documentation, and explicit acceptance.

## 9. Mandatory tests

- Domain invariant tests for account cardinality, ownership shares, temporal occupancy, state transitions, financial reversals, and ballot separation.
- Authorization matrix tests across roles, scopes, resources, dates, explicit deny, disabled accounts, unknown roles, and stale sessions.
- Authentication tests for MFA enrollment enforcement, replay prevention, recovery code one-time use, PIN/password whitespace, throttling, and revocation.
- Persistence integration tests on SQL Server for constraints, rowversion, transaction rollback, filtered indexes, and query behavior.
- Financial parity tests comparing legacy and target calculations at building/apartment/month level, including rounding and adjustments.
- Migration restart/idempotency tests, unresolved identity cases, duplicate contacts, missing owners, and partial-batch recovery.
- Secret canary tests ensuring no secrets appear in logs, HTML, exports, exceptions, or ordinary DTOs.
- File/OCR tests for spoofed signatures, malformed/oversized images, decompression bombs, unauthorized download, and orphan cleanup.
- Backup tests for typed failure, corrupted artifact, missing file/history mismatch, retention, and full restore.
- Web tests for antiforgery, authorization filters, no-store, rate limits, and direct endpoint access.

## 10. Blocking decisions and evidence still required

These are not to be guessed in code:

1. Person identity proof and merge approval roles; treatment of shared family phone numbers.
2. Whether a Person without a UserAccount may own/occupy and how they receive notices.
3. Legal ownership evidence, ownership share validation, and effective-date semantics.
4. Apartment access delegation and emergency/temporary access rules.
5. Exact scope of each legacy permission key and treatment of ambiguous account-wide grants.
6. Voting subject (Person vs Apartment), representative delegation, eligibility cutoff, ballot retention, and audit model.
7. Wallet accounting basis, opening balances, period close, rounding allocation, and treatment of legacy variances.
8. Financial correction authority and whether any posted transaction can ever be erased under a separate legal process.
9. Maintenance visibility and resident-to-resident privacy.
10. Notification ownership/read state and presence counting semantics.
11. Receipt retention, privacy, and private storage provider.
12. Camera/media authorization and decommission retry policy.
13. Backup retention, encryption key ownership, restore operator approvals, and artifact deletion semantics.
14. Audit retention, access, tamper-evidence, and privacy obligations.
15. Deployment topology: single instance or future scale-out, shared cache/session/data-protection key requirements.
16. Authoritative SQL view deployment and reconciliation of EF migrations vs FullSchema.

## 11. Current findings mapped to target work

- Review Sections 1–18: identity, authorization, schema, retention, credentials, MFA, and migration foundations -> A1/A3/A4.
- Section 19: reports/wallet/read-path semantics -> A5.
- Sections 20–22: backups, exports, invoice/QR/document delivery -> A8/A9/A10.
- Sections 23–24: resident lifecycle, polls, maintenance, notifications, presence, cameras/media -> A4/A6/A7/A8.
- Section 25: financial commands and reconciliation -> A5.
- Section 26: settings, integration secrets, impersonation -> A2/A3/A6/A8/A9.
- Section 27: startup mutation and SQL mirror -> A2/A3/A1; startup mutation must be removed or feature-gated before runtime validation against preserved legacy data.
- Section 28: scheduler, logs, health, sync and backup orchestration -> A2/A9.
- Sections 29–30: QR/authentication/MFA/PIN/receipt/OCR/import validation -> A3/A8/A10.
- Section 31: consolidated priorities -> phase gates and acceptance criteria above.

## 12. Explicit non-authorization statement

This document does not authorize:
- running Gate A DDL;
- creating or applying EF Core migrations;
- modifying any database or source application code;
- backfilling, merging, or deleting identity records;
- changing financial records or wallet balances;
- enabling production cutover or retiring legacy tables.

The architecture, decision register, implementation backlog, and first cross-document consistency review are now published. The next safe activity is to complete the remaining column/constraint parity matrix, permission-to-resource scope catalogue, and read-only deployment metadata reconciliation, then prepare (but do not execute) the disposable-restore Gate A rehearsal package. Owner decisions remain separately tracked. Gate A remains **NOT AUTHORIZED** until the owner explicitly approves it after reviewing the complete evidence and rehearsal plan.
