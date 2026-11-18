# Domain Glossary and Application Use-Case Catalogue

**Date:** 2026-11-18  
**Branch:** `development/database-redesign-enterprise`  
**Status:** Target contract draft. This document defines language and use-case boundaries; it does not authorize code or database changes.

## 1. Shared domain language

| Term | Canonical meaning | Explicitly not the same as |
|---|---|---|
| Person | One human identity record, with reviewed identity evidence and profile history | Login, resident row, apartment, phone number |
| UserAccount | Interactive principal used to authenticate and hold scoped roles/permission overrides | Person, ownership, occupancy |
| Credential | Versioned authentication factor/verifier associated with an account | Account, session, recovery evidence |
| Session | Revocable authenticated interaction context with account, MFA assurance, security stamp, and expiry | Permanent authorization grant |
| RoleDefinition | Named bundle of permissions | A person or an account |
| RoleAssignment | Time-bounded grant of a role to an account within a defined scope | Global role string in a cookie |
| Permission | Named capability over a resource/action | UI menu item or claim string |
| PermissionOverride | Account-specific Allow/Deny exception at a defined scope and time | Role-wide permission |
| Scope | System, Building, Apartment, or Personal boundary within which an authorization decision is evaluated | A role or user type |
| Ownership | Evidence-backed legal/economic interest in an apartment over an effective interval | Occupancy, primary resident, login |
| Occupancy | A person's time-bounded residence/occupancy relationship to an apartment | Ownership or account status |
| AccessGrant | Explicit temporary or limited right to access an apartment resource | Ownership or occupancy |
| Wallet | Apartment-scoped financial position derived from immutable ledger movements and approved rules | A person balance or a report subtotal |
| LedgerEntry | Immutable posted financial movement with source, actor, period, and reversal linkage | Editable current balance |
| Deposit | Payment evidence/state workflow that may become a posted financial movement | OCR extraction or uploaded receipt |
| Allocation | Deterministic distribution of a financial amount among eligible apartments | Arbitrary per-row rounding |
| FinancialPeriod | Explicit business accounting period with open/closed state and timezone policy | Server current month |
| Poll | A governed voting process with frozen policy and lifecycle | A list of options |
| Eligibility | Evidence that a subject may participate in a poll | Ballot or selected option |
| Delegation | Time-bounded authority for a representative to act for a subject in a specific poll | Ownership transfer |
| Ballot | Anonymous choice record separated from voter eligibility identity | A vote audit record with voter ID |
| AuditEvent | Durable record of business/security action and outcome | Diagnostic log line |
| ApplicationLog | Operational diagnostic event with redaction and retention | Tamper-evident business audit |
| OutboxMessage | Durable request to perform an external/asynchronous side effect | Best-effort in-memory callback |
| IntegrationSecret | Encrypted, versioned credential for an external provider | Ordinary settings or view-model field |
| DocumentAsset | Private file object with ownership, classification, retention, and download policy | Public URL |
| BackupArtifact | Integrity-verifiable protected backup plus reconciled metadata | A successful button click |
| CapabilityToken | Short-lived signed/opaque capability bound to a target, use policy, expiry, and revocation state | Permanent identity or session |
| ImportBatch | Staged, versioned, actor/target-bound import with row-level validation and commit outcome | Direct bulk insertion or unreviewed file upload |
| Reconciliation | Comparison of source/target records and balances with explicit variance dispositions | A count-only migration report |
| LegacyIdentityMap | Durable reviewed source-row-to-target identity provenance | Temporary migration log |

## 2. Aggregate ownership and invariants

| Aggregate / consistency boundary | Root | Owned records / references | Core invariants |
|---|---|---|---|
| Person | Person | Profile evidence, archive state | No duplicate identity merge without review; account cardinality <= 1 |
| UserAccount | UserAccount | Credentials, MFA settings, role assignments, permission overrides, session/security state | Active account only; credentials versioned; role grants scoped and effective; session revocation respected |
| Apartment | BuildingId + ApartmentId | Lifecycle and references to ownership/occupancy/access periods | All child resources same-building; cannot delete referenced financial/property history |
| Ownership period | Ownership record + apartment consistency boundary | Person, share, evidence, effective interval | Valid share; overlap and aggregate completeness checked transactionally |
| Occupancy period | Occupancy record + apartment consistency boundary | Person, type, primary status, effective interval | At most one active primary; no invalid overlap per approved policy |
| Apartment access grant | Grant | Person, apartment, permission profile, issuer, validity/revocation | Explicit issuer, bounded scope, valid interval, revocation audit |
| Financial account / wallet | Apartment + financial period | Deposits, expenses, revenues, allocations, adjustments, ledger entries | Atomic posting, idempotency, concurrency, immutable history, compensating reversal |
| Poll | Poll | Policy snapshot, options, delegations, eligibility, ballots, lifecycle events | Freeze before open; eligibility single-use; ballot cannot be linked to identity |
| Maintenance request | MaintenanceRequest | Comments, assignments, attachments, financial source link | Explicit visibility; expense posting/reversal remains linked |
| Notification | Notification/recipient | Delivery attempts, recipient read state | Recipient type explicit; bounded reads; idempotent delivery |
| Media resource | Camera/DVR | Credential reference, provisioning state, external resource ID | Authorized access, no secret DTO, retryable lifecycle |
| Document | DocumentAsset | Blob reference, scan/validation metadata, retention, access grants | Private storage; verified content; authorized no-store delivery |
| Backup operation | BackupJob/Artifact | State transitions, integrity metadata, history | Typed outcome; file/history reconciliation; restore audit |
| Audit stream | AuditEvent | Actor, subject, action, resource, outcome, correlation | Durable, append-only, privacy-safe, restricted access |

Aggregate boundaries are logical consistency boundaries. They do not require one giant EF Core aggregate or a repository per table. Cross-aggregate workflows use explicit application transactions and outbox messages where appropriate.

## 3. Application command catalogue

Commands are the only application entry points allowed to mutate business state. Each command requires a validated actor context, correlation ID, authorization decision, cancellation token, and typed outcome. The exact DTO field list is finalized during module implementation.

### 3.1 Identity and accounts
- `RegisterPerson` — create a person from verified evidence; no implicit account.
- `ResolveLegacyIdentity` — review one or more source rows and record Link/Separate/Reject/ManualReview disposition.
- `CreateUserAccount` — create the single account for a Person; reject if one already exists.
- `ChangeAccountStatus` — activate, lock, disable, or archive according to lifecycle rules.
- `UpdatePersonProfile` — update profile fields with source provenance and conflict handling.
- `CloseLegacyIdentityMapping` — immutable reviewed disposition; never erase source evidence.

### 3.2 Authentication and sessions
- `AuthenticateWithCredential` — verify credential without normalizing secret content.
- `BeginMfaEnrollment` / `ConfirmMfaEnrollment` — staged enrollment and verified activation.
- `VerifySecondFactor` — replay-safe TOTP or approved factor verification.
- `ConsumeRecoveryCode` — atomic one-time recovery code use.
- `ResetCredential` — policy-validated reset with session/security-stamp invalidation.
- `RevokeAccountSessions` — revoke all or selected sessions with reason and actor.

### 3.3 Authorization
- `AssignRole` / `RevokeRole` — scoped, effective-dated role assignment.
- `GrantPermissionOverride` / `RevokePermissionOverride` — account-level Allow/Deny exception.
- `EvaluateAccess` — read-only authorization decision for a specific actor, action, and resource.
- `ReviewPermissionMigration` — classify legacy permission keys and scope before mapping.

### 3.4 Property and relationships
- `RegisterOwnershipPeriod` / `CloseOwnershipPeriod` — evidence-backed ownership intervals and shares.
- `RegisterOccupancy` / `EndOccupancy` — dated occupancy and primary occupant transitions.
- `GrantApartmentAccess` / `RevokeApartmentAccess` — explicit access, expiry, and reason.
- `ChangeApartmentLifecycle` — close/reopen/disable with financial and relationship checks.

### 3.5 Finance
- `CreateDeposit` — create pending deposit/payment evidence record.
- `ConfirmDeposit` / `CancelDeposit` — conditional state transition with idempotency and actor.
- `PostExpense` / `PostRevenue` — validate category/building/period and create immutable ledger movement.
- `AdjustWallet` — controlled adjustment with reason and authority.
- `ReverseLedgerEntry` — compensating movement referencing original posting.
- `AllocatePeriodCharge` — deterministic apartment allocation with documented rounding.
- `CloseFinancialPeriod` / `ReopenFinancialPeriod` — controlled period transition.
- `ReconcileLegacyWallet` — produce variance report; never auto-correct legacy rows.

### 3.6 QR and capability tokens
- `IssueQrCapability` — create a target-bound token with signed authorization fields, expiry, use limit, and revocation metadata.
- `ConfirmQrAccess` — verify token and target, apply policy, consume once, and establish a dedicated guest/resident session.
- `RevokeQrCapability` — revoke outstanding capabilities and invalidate derived access.
- `ValidateCapabilityUse` — read-only validation without consuming the token.

### 3.7 Import and migration workbench
- `CreateImportBatch` — bind file digest, actor, target scope, template version, and expiry.
- `ValidateImportBatch` — parse and validate all rows without creating domain records.
- `PreviewImportBatch` — show safe row-level outcomes and conflicts.
- `CommitImportBatch` — commit approved valid rows idempotently with checkpointing.
- `CancelImportBatch` — cancel uncommitted staged work and preserve audit evidence.

### 3.8 Polling
- `CreatePoll` / `AmendDraftPoll` — draft-only configuration.
- `FreezePollPolicy` / `OpenPoll` — snapshot eligibility policy and options.
- `IssueEligibilityCredential` — create one-time opaque participation credential.
- `CastBallot` — atomically consume eligibility and write a ballot without a voter-choice link.
- `ClosePoll` / `FinalizePoll` — close, validate tally, and publish approved results.
- `AnnulPoll` / `ReopenPoll` — controlled whole-poll remedy; never edit individual choices.

### 3.9 Operations and integrations
- `CreateMaintenanceRequest` / `UpdateMaintenanceRequest` / `CloseMaintenanceRequest`.
- `PostMaintenanceCost` / `ReverseMaintenanceCost`.
- `DispatchNotification` / `MarkNotificationRead` / `UpdateNotificationPreference`.
- `StartMediaProvisioning` / `RetryMediaProvisioning` / `DecommissionMediaResource`.
- `UploadPrivateDocument` / `AuthorizeDocumentDownload` / `ExpireDocument`.
- `ExtractReceiptFields` — OCR suggestion only; cannot confirm a payment.
- `RotateIntegrationSecret` / `TestIntegrationConnection` — write-only secret workflow.
- `CreateBackup` / `VerifyBackup` / `RestoreBackup` / `DeleteBackupArtifact` — typed state transitions and operator audit.

## 4. Query catalogue

Queries are read-only, purpose-specific projections. They must not return tracked domain aggregates to MVC views.

### Identity and access
- `GetPersonSummary`, `GetAccountSecurityStatus`, `GetCredentialHistory` (redacted).
- `GetEffectiveRoles`, `GetEffectivePermissions`, `ExplainAuthorizationDecision`.
- `GetLegacyIdentityReviewQueue`, `GetLegacyIdentityMappingDetail`.

### Property
- `GetBuildingSummary`, `GetApartmentSummary`, `GetOwnershipTimeline`, `GetOccupancyTimeline`, `GetEffectiveApartmentAccess`.

### Finance
- `GetApartmentWalletStatement`, `GetBuildingFinancialSummary`, `GetDepositQueue`, `GetLedgerEntryDetail`, `GetPeriodAllocationPreview`, `GetFinancialReconciliationReport`.
- Every financial query requires building/apartment scope and an explicit period/date policy.

### Polls and operations
- `GetPollDraft`, `GetPollPublicStatus`, `GetEligibilityStatus` (without choice), `GetPollTally`.
- `GetMaintenanceQueue`, `GetResidentMaintenanceRequests`, `GetNotificationInbox`, `GetPresenceSummary`.
- `GetCameraDirectory` (safe DTO only), `GetAuthorizedMediaSession`.

### Documents and platform
- `GetDocumentMetadata`, `GetBackupHistory`, `GetBackupArtifactStatus`, `GetAuditPage`, `GetApplicationLogPage`, `GetSystemHealthSummary`.

All list queries require stable ordering, bounded pagination, cancellation, explicit projection, and tenant/resource filters applied in SQL before materialization.

## 5. Typed application outcomes

Expected outcomes should be represented explicitly rather than hidden in empty lists or booleans:

- `Succeeded` with the created/updated identifier or read model.
- `ValidationFailed` with field-safe validation codes.
- `NotFound` without leaking cross-scope resource existence.
- `Forbidden` with no sensitive resource details.
- `Conflict` for stale rowversion, duplicate account, already-consumed eligibility, or invalid concurrent state transition.
- `InvalidTransition` for unsupported lifecycle action.
- `RequiresStepUp` for MFA/reauthentication.
- `RequiresApproval` for dual-control operations.
- `RetryableFailure` for recoverable integration/storage dependency failure.
- `PermanentFailure` for invalid external request or non-recoverable validation.
- `PartiallyCompleted` only when the workflow explicitly models partial external state and includes a reconciliation handle.

Controllers map these outcomes to HTTP/UI behavior. They must not convert a failed job into success text.

## 6. Cross-cutting request context

Every command should receive:
- `ActorAccountId` and resolved `PersonId` (if a human account);
- actor assurance level (MFA verified, factor, verification time);
- requested permission/action and resolved resource scope;
- correlation/trace ID;
- UTC clock abstraction;
- idempotency key for retryable financial/external commands;
- source channel (web, scheduled job, import, integration);
- cancellation token.

System jobs use a dedicated non-human actor identity and explicit job permissions. They must not impersonate a human or store NULL actor attribution where the operation requires accountability.

## 7. Design constraints to carry into implementation

1. No controller calls Infrastructure repositories directly.
2. No domain service depends on HttpContext, claims, EF Core, or vendor SDKs.
3. No generic `SaveEverything` aggregate operation for all business modules.
4. No financial mutation inside a query handler.
5. No permission decision based only on a role string or stale claim.
6. No credential normalization beyond the identifier; never trim or case-normalize secret values.
7. No file URL is considered private merely because it is difficult to guess.
8. No OCR result is a payment confirmation.
9. No external side effect without idempotency, retry, and compensation/reconciliation.
10. No successful command result until the durable state transition is committed.
11. No audit payload contains secrets, ballot choices, raw OCR text, or unnecessary personal data.
12. No module is cut over before parity, negative authorization tests, restore/rollback evidence, and owner acceptance.

## 8. Open owner decisions carried forward

This contract catalogue does not resolve DEC-01 through DEC-38 or PERM-01 through PERM-05. The most important blockers remain ownership completeness, voting eligibility/delegation, financial rounding and period semantics, maintenance visibility, preferences/notification recipient semantics, retention, ballot assurance, and privileged SuperAdmin permissions.

## 9. Verification status

This is a target design artifact based on the reviewed source and diagnostics. No code, EF model, database, or runtime path was changed. No build or tests were run. Gate A remains **NOT AUTHORIZED**.
