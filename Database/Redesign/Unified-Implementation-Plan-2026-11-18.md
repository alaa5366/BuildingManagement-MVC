# Unified Enterprise Identity, Authorization, and Data-Lifecycle Implementation Plan

**Repository:** `alaa5366/BuildingManagement-MVC`  
**Branch:** `development/database-redesign-enterprise`  
**Approval recorded:** 2026-11-18 — approval to consolidate the completed discovery into one coordinated implementation program.  
**Execution status:** Identity/authorization schema rehearsal completed through Phase07; resident login can authenticate from target identity tables. Full legacy-domain backfill has not started.
**Database boundary:** Only `BuildingManagement_RestoreTest` has been changed. Production is excluded.

## 1. Purpose and delivery rule

Deliver one coordinated redesign of identity, authorization, property relationships, credentials/MFA, voting, financial continuity, audit, retention, and operations. Do not create unrelated feature phases or independently migrate one domain while leaving another with contradictory identity or security rules.

The work is organized as **one delivery program with ordered work packages**. Work packages can be implemented and reviewed together where dependencies permit, but their safety checkpoints are mandatory. The later owner authorization recorded in `Owner-Approval-Recommended-Defaults-2026-11-18.md` supersedes this document's original plan-only approval for application changes and disposable-database rehearsal. Production deployment and data-bearing cutover remain separately gated.

## 2. Non-negotiable target rules

1. A human is represented by one `Person`; a human Person has zero or one interactive `UserAccount`. One account belongs to one Person.
2. Ownership, occupancy, apartment access, account status, and authorization are independent, dated concepts.
3. A Person may own/occupy/access multiple apartments and hold roles in multiple buildings. An apartment may have multiple owners and occupants. A primary occupant is optional and time-bounded.
4. SuperAdmin is a System-scoped role. It is not a separate identity type and cannot be granted at Building/Apartment/Personal scope.
5. Authorization is evaluated centrally against account state, role grants, direct allow/deny overrides, effective dates, requested action, and the actual resource scope. UI hiding and claims alone are not authorization.
6. Apartment financial balances and ledger history remain apartment-scoped. No financial recalculation, netting, or carryover rewrite is implicit in identity migration.
7. Secret voting separates eligibility/identity evidence from ballot choice. No directly joinable voter-to-choice key is permitted in the target ballot store.
8. Posted financial, ownership, occupancy, access, security, voting, and audit history is retained. Ordinary user actions close/revoke/archive; irreversible purge is exceptional and separately governed.
9. Legacy data is preserved with source provenance. No fuzzy identity merge by name, phone, email, WhatsApp, or apartment. Unresolved rows remain unresolved.
10. Migration is additive, restartable, observable, reconcilable, and reversible until explicit cutover approval.
11. No data-changing migration runs automatically at web-application startup.
12. Every security-sensitive mutation is transactional with durable audit/outbox evidence, concurrency protection, and session/security-version revocation as applicable.

## 3. Consolidated delivery work packages

### WP0 — Policy freeze and source-of-truth reconciliation

**Deliverables**
- Approved versioned ERD and data dictionary, including exact SQL types, key lengths, collations, indexes, constraints, and relationship delete behavior.
- Decision register for all unresolved business policies listed in section 4.
- Reconciled inventory of live disposable SQL metadata, EF model/snapshot/migrations, checked-in FullSchema, views, triggers, SQL jobs, reports, external integrations, exports, and mobile/client dependencies.
- Explicit data lifecycle/retention matrix by domain.
- Permission catalogue and action/resource/scope matrix.

**Exit criteria**
- No contradictory schema definitions or unclassified legacy dependencies.
- ApartmentAccessGrants GrantType and versioned PermissionSetKey catalogs finalized; grants remain fail-closed until a permission evaluator and explicit profile policy are implemented. Revoke lifecycle remains open.
- Ownership/occupancy interval and share rules approved.
- Financial carryover/rounding/reconciliation and wallet-view dependencies resolved.
- No unresolved policy silently inferred from current code.

### WP1 — Additive identity and relationship foundation

**Deliverables**
- EF entities/configuration and reviewed EF migration for Persons, UserAccounts, UserCredentials, UserTwoFactorSettings, UserRecoveryCodes, RoleDefinitions, RolePermissions, UserRoleAssignments, ApartmentOwnerships, ApartmentOccupancies, ApartmentAccessGrants, AccountPermissionOverrides, and LegacyIdentityMap.
- SQL constraints/indexes for invariants that SQL Server can enforce.
- Transactional domain validation for invariants that require service logic.
- No changes that drop, rename, repurpose, or rewrite legacy columns/tables.

**Required invariants**
- Unique Person-to-account relationship.
- Unique external provider/subject when supplied.
- Scope-shape and FK integrity.
- Users legacy source mapping to Account + same Person in one transaction; Residents source mapping may be many-to-one to Person and never receives AccountId.
- No overlapping invalid ownership/occupancy intervals; approved share-total policy enforced transactionally with concurrency protection.
- Role/scope compatibility and System-only SuperAdmin.
- Grant/revocation lifecycle with actor, reason, effective dates, and audit.
- No cascade deletion of identity, credentials, grants, or history.

**Exit criteria**
- Additive migration applies cleanly to a fresh disposable restore and can be rolled back/restored from verified backup.
- Existing application behavior remains unchanged before the compatibility layer is enabled.
- No backfill is bundled into schema creation.

### WP2 — Central authorization and relationship lifecycle

**Deliverables**
- One authorization evaluator and typed actor/resource context.
- Explicit role catalogue, role-to-scope policy, permission catalogue, direct Allow/Deny precedence, validity and revocation semantics.
- Authorized application services for resident, apartment, DVR/camera, wallet, settings, audit, export, backup, restore, and impersonation operations.
- Explicit commands for grant/revoke, move-in/move-out, ownership change, account disable/restore, building retirement, apartment closure, and account recovery.
- Session/security version increments on permission, role, relationship, credential, MFA, disable, and emergency-revoke changes.

**Rules**
- Service methods must not trust caller-supplied actor labels, account IDs, role strings, or building IDs as proof of authorization.
- Resolve the target resource inside the authorized scope; reject foreign-building/apartment IDs.
- Distinguish additive grant from complete replacement/revocation.
- Do not infer deletion from omission in partial DTOs or filtered aggregates.
- Keep repository/domain helpers internal where they are not intended as security boundaries.

**Exit criteria**
- Resource/action tests pass for anonymous, resident, admin, SuperAdmin, disabled, impersonated, wrong-building, wrong-apartment, export, and direct-service invocation cases.
- Emergency revocation fails closed; stale last-known-good state is not used for high-risk writes or secret access.

### WP3 — Credential, MFA, and session security

**Deliverables**
- Typed credential model; password/PIN semantics no longer overloaded in legacy `Users.Pin`.
- Explicit credential migration command with dry-run, approval, bounded batches, per-record outcomes, idempotency, verification, and recovery.
- Mandatory admin/SuperAdmin MFA enrollment policy and resident step-up policy for sensitive actions.
- Atomic TOTP replay prevention and recovery-code consumption; recovery-code rotation and lost-device recovery.
- Shared pending challenges, shared revocation/security-version store, shared throttling, and explicit global/per-device sign-out policy.
- Impersonation validation bound to the initiating actor, target, scope, expiry, and current actor permissions.

**Hard stop**
- Remove `PinMigrationService.MigrateLegacySecretsAsync()` from normal application startup before running the redesigned app against any data-bearing restore.
- Never clear a source credential until the destination credential has been verified and its outcome durably recorded.
- Never log/export plaintext PINs, passwords, TOTP seeds, recovery codes, API keys, or connection strings.

**Exit criteria**
- Startup is read-only with respect to business data.
- Multi-instance challenge, replay, throttle, and revocation tests pass.
- Database outage behavior is explicitly fail-closed for privileged and high-risk operations.

### WP4 — Finance, voting, operational data, and retention compatibility

**Deliverables**
- Apartment-scoped financial ledger continuity and reconciliation report.
- Explicit rules for confirmed/cancelled deposits, adjustments, monthly shares, carryovers, rounding, and compensating entries.
- Wallet view dependency inventory and verified view definitions before changing mappings.
- Secret ballot model with separate eligibility snapshot and ballot store; immutable poll lifecycle after opening/first vote.
- Camera/DVR building-scope integrity, QR token revocation/expiry, maintenance/notification visibility policy.
- Building/apartment/person/account lifecycle states and retention matrix.
- Deletion-impact preview, legal holds, retention expiry, controlled purge approval, and archive reconciliation.

**Rules**
- No physical deletion of a building/apartment/person/poll/posted financial record as a normal UI operation.
- Posted financial entries are corrected by compensating entries, not silent rewrite/delete.
- Apartment closure ends access and occupancy intervals while preserving history.
- Poll options/ballots cannot be silently deleted after voting begins.
- Cascade behavior is retained only for disposable dependants whose loss is explicitly approved and tested.

**Exit criteria**
- Per-apartment wallet balances and historical totals reconcile exactly within approved rounding tolerance.
- Historical votes and financial entries remain attributable to the appropriate retention domain without exposing secret ballot choices.
- Retention and legal-hold tests prove no unauthorized purge.

### WP5 — Audit, outbox, import/export, backup, and background operations

**Deliverables**
- Transactional outbox/audit pattern for security, financial, relationship, import, backup, restore, and retention mutations.
- Durable operation IDs, idempotency keys, retry/reconciliation state, and failure recovery.
- Backup completeness semantics: unknown collection or collection failure cannot produce a false complete-success status.
- Restore restricted to one-use, user-bound, digest-verified preview/confirmation; bypass overloads narrowed to internal/test use.
- Excel import prevalidation and atomic/staged import with per-row outcomes; no plaintext credential persistence in preview/logs.
- Separate view/export/download permissions and audit events; CSV formula-injection protection preserved.
- Distributed scheduler lease/occurrence key, safe rotation, and file/SQL history reconciliation.

**Exit criteria**
- Failure injection at every write boundary yields rollback or durable resumable state.
- Concurrent worker instances cannot duplicate scheduled backup/import/restore operations.
- Audit/outbox failure cannot silently report a sensitive mutation as fully successful.

### WP6 — Runtime and deployment foundation

**Deliverables**
- Shared durable session/cache/revocation/challenge/rate-limit/lease store.
- Shared Data Protection key ring, consistent application name, protected key encryption, rotation and recovery runbook.
- Managed secret store with environment isolation, key versions, rotation, dual-read transition where needed, and emergency revocation.
- Trusted reverse-proxy/forwarded-header contract, HTTPS/cookie security, upload limits, safe health endpoints.
- Request cancellation and correlation/operation IDs across long-running workflows.
- Operational runbooks for database outage, key compromise/rotation, emergency account revoke, partial import, backup corruption, restore, and migration recovery.

**Exit criteria**
- Restart/redeploy and multi-instance tests pass with no loss of auth challenges, revocation, or scheduler correctness.
- No secrets or personal/financial data leak through logs, health endpoints, or diagnostics.

### WP7 — Controlled migration rehearsal, compatibility, and cutover

**Deliverables**
- Privacy-safe identity candidate queue and human-reviewed LegacyIdentityMap dispositions.
- Dry-run and staged backfill with row-level outcome ledger, counts, rejects, and reconciliation.
- Compatibility layer allowing old and new identity paths to coexist temporarily without duplicate accounts or authorization widening.
- Rehearsed rollback/restore and cutover runbook.
- User-approved smoke-test checklist and production release package.

**Exit criteria**
- Zero unexplained source/target count differences.
- No unresolved high-risk identity, permission, credential, financial, voting, or retention conflicts.
- Full build, automated tests, security/regression suite, migration rehearsal, restore verification, and user smoke tests all pass with recorded outputs.
- Cutover and production release receive separate explicit approvals.

## 4. Business decisions required at WP0

The following must be explicitly decided and recorded before schema freeze:

| Domain | Decision |
|---|---|
| Identity | Who approves merges; acceptable evidence; treatment of shared contacts and disputed identities |
| Ownership | Whether shares must total exactly 100%; handling partial/unknown shares and overlapping effective periods |
| Occupancy | Trusted date evidence; overlap policy; primary occupant rules; move-in/out approval |
| Apartment access | Permission evaluator/profile permissions; delegation; emergency access; expiry and revocation reasons |
| Authorization | Role catalogue; scope compatibility; permission matrix; Allow/Deny precedence; resource-level rules |
| Account | Disabled/suspended/locked/archived states; recovery; legal erasure authority |
| Credentials | Resident/admin methods; legacy PIN reset/re-enrollment; password policy; compromise response |
| MFA | Admin/SuperAdmin enrollment deadline; resident step-up actions; break-glass and lost-device recovery |
| Sessions | Global/per-device sign-out; revocation latency; database-outage behavior; impersonation expiry |
| Preferences | Person settings versus apartment defaults and precedence |
| Finance | Carryover, cancelled deposits, adjustments, rounding, historical wallet/report dependencies |
| Voting | Person/apartment eligibility, delegation, legacy ballot retention, required secrecy assurance |
| Retention | Legal retention periods/holds by data class; archive and purge approval |
| Operations | Shared infrastructure, key rotation, trusted proxy, scheduler ownership and recovery |
| Time/money | Business timezone, UTC storage, decimal precision, rounding and effective-time rules |
| External systems | SQL reports/jobs, integrations, exports, clients and scripts depending on legacy schema |

## 5. Priority order and parallelism

The delivery is one coordinated package, but dependencies determine the order:

1. WP0 policy freeze and source-of-truth reconciliation is the first blocker.
2. WP1 additive identity foundation can proceed only after WP0 schema decisions are approved.
3. WP2 authorization, WP3 credentials/session, WP4 finance/voting/retention, WP5 audit/operations, and WP6 deployment are coordinated implementation streams. Their interfaces and invariants must be agreed before parallel coding.
4. WP7 migration rehearsal follows only after all target invariants and compatibility paths are implemented and tested.

Do not split off a shortcut migration of Users/Residents, do not copy all legacy permissions to a global role, and do not change financial or poll semantics merely to make the migration pass.

## 6. Mandatory safety gates

These gates are checkpoints inside the single program, not separate feature phases:

- **G0 — Design freeze:** WP0 decisions, ERD, data dictionary, retention matrix, permission matrix, and external dependency register approved.
- **G1 — Fresh read-only preflight:** exact database identity, metadata, migration history, all target objects absent, no partial prior Gate A creation, verified backup and restore target.
- **G2 — Additive schema approval:** owner authorization permits reviewed additive DDL/EF migrations against `BuildingManagement_RestoreTest` only. Production and destructive legacy retirement remain separately gated.
- **G3 — Service invariants:** transactions, concurrency, evaluator, revocation, audit/outbox, credential/MFA, and lifecycle tests.
- **G4 — Migration rehearsal:** human identity dispositions, dry-run, staged backfill, row-level reconciliation, rollback/restore.
- **G5 — Compatibility/cutover rehearsal:** dual-path behavior, no permission widening, finance/vote continuity, rollback window.
- **G6 — Operational acceptance:** multi-instance, monitoring, backup/restore, security, full regression, user smoke tests.
- **Production release:** separate explicit approval after all evidence is reviewed.

A failed gate stops the dependent work. Never use a partially created Gate A schema as an invitation to rerun an `IF NOT EXISTS` script; inspect and reconcile partial state first.

## 7. Immediate next execution task

This was the original pre-authorization next task and is now historical. Current work continues from the owner-approved defaults, the full table inventory, and the staged migration baseline in `Full-Database-Migration-Baseline-2026-11-18.md`.

Do not start WP1 DDL yet. In particular:
- Rerun exact-target preflight and capture a verified backup before each data-bearing rehearsal.
- `ApartmentAccessGrants.GrantType` and PermissionSetKey values are owner-approved; profiles have no implicit permissions until the evaluator and lifecycle policy are implemented.
- Identity candidates require human disposition; no fuzzy auto-link.
- Credential hashes are BCrypt-shaped but not cryptographically verified.
- Financial carryover/rounding and wallet-view external dependencies remain unresolved.
- Current startup PIN migration must be removed before app startup on a data-bearing restore.
- Retention/cascade semantics and cross-instance infrastructure must be implemented and tested before cutover.

## 8. Approval and change log

- **2026-11-18:** User approved consolidating the completed review into one unified implementation plan and continuing the work.
- This approval covers the plan and documentation only. It does not authorize DDL/DML, EF migration execution, backfill, authentication cutover, destructive operations, or production changes.
- No application source, database schema, database rows, credentials, backups, or runtime state were changed while preparing this plan.

## 9. Current implementation status — 2026-11-18

- The owner authorized implementation and rehearsal against `BuildingManagement_RestoreTest`, with recommended defaults approved. Production changes and destructive legacy retirement remain separately gated.
- Phase06 and Phase07 are applied to the disposable restore. The full-database baseline covers 44 application tables; only identity/authorization target tables currently exist. All 84 legacy identity candidates remain Pending.
- A synthetic target resident was added without any legacy link or apartment data. Unified login and cookie revalidation now work through `Persons`, `UserAccounts`, `UserCredentials`, and `UserRoleAssignments`; build and all 549 tests passed.
- No legacy identity, financial, poll, operations, media, settings, audit, or backup business rows have been backfilled. Start target-domain work from the full-database baseline and preserve the legacy tables until each domain's mapping and reconciliation gates pass.
