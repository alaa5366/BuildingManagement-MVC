# Enterprise Database Redesign Blueprint

**Repository:** `alaa5366/BuildingManagement-MVC`  
**Branch:** `development/database-redesign-enterprise`  
**Status:** Design proposal based on the inspected source and the supplied `BuildingManagement_RestoreTest` inventory.  
**This document does not create or apply a migration and does not authorize production changes.**

## 1. Executive objective

Move from split resident/admin identity paths to one person-centric identity model while preserving apartment-scoped finance, property history, auditability, and existing operational behavior.

The target must support:

- One Person linked to zero or one interactive account; one account belongs to exactly one Person for human accounts.
- One person may own multiple apartments, occupy multiple apartments, and hold different roles in multiple buildings.
- An apartment may have multiple owners with percentage shares and multiple occupants.
- Ownership, occupancy, access grants, and authorization are separate concepts with dated history.
- System SuperAdmin is a system-scoped role, not a separate account type.
- Polls may be person-based or apartment-based. Apartment voting requires explicit, valid delegation.
- Secret ballots must not store a voter-to-choice foreign key or a directly joinable identifier.
- Wallets and financial balances remain apartment-scoped.
- Financial, ownership, occupancy, security, and audit history must not be silently cascade-deleted.
- Migration is additive, restartable, observable, reconcilable, and reversible before cutover.

## 2. Evidence and baseline

### 2.1 Live test database inspected

The supplied inventory identifies:

- Database: `BuildingManagement_RestoreTest`
- SQL Server: `ZOS-AAM\\LOCALDB#8B6B20E4`
- SQL Server version: `17.0.4025.3`
- Edition: Express Edition (64-bit)
- Inventory timestamp: 2026-11-18 10:30:15 UTC

Reported row counts include:

| Table | Rows |
|---|---:|
| Buildings | 3 |
| Apartments | 79 |
| Residents | 79 |
| Users | 5 |
| BuildingAdmins | 2 |
| Polls | 1 |
| PollOptions | 2 |
| PollVotes | 1 |
| Deposits | 26 |
| MonthlyApartmentShares | 113 |
| WalletAdjustments | 2 |
| AuditLog | 1,050 |

The database reports four applied EF Core migrations, all ProductVersion 8.0.10:

1. `20261116120818_InitialCreate`
2. `20261117012657_Phase04_StandardizeKeyColumnTypes`
3. `20261117050000_Phase05_RemoveRedundantResidentIndexes`
4. `20261117130000_AddUserTwoFactor`

### 2.2 Confirmed schema observations

- `Users` and `Residents` are separate tables and separate authentication paths.
- `Users.Role` is a single role string; `BuildingAdmins` separately links accounts to buildings.
- `Residents` has `Uid`, `IsOwner`, and `IsPrimary`, but no dated ownership/occupancy relationship model.
- `Apartments` includes legacy `Owner`, `Phone`, `Email`, and `Pin` fields in addition to property data.
- `Users` includes a legacy `Pin`; `Residents` includes `PinHash` and nullable `PasswordHash`.
- `GlobalSettings` contains integration credential fields, including WhatsApp and Cloudinary values.
- `Users` contains the four 2FA columns added by the recorded migration.
- Current `PollVotes` primary key is `(BuildingId, PollId, ApartmentId)`; the current service accepts an apartment ID and records the selected option against it.
- `Residents` references `Apartments` with `ON DELETE CASCADE`.
- `DepositEditHistory` references `Deposits` with `ON DELETE CASCADE`.
- `PollVotes` references `PollOptions` with `ON DELETE CASCADE`; options cascade from polls.
- `Cameras` has `BuildingId`, but its current DVR foreign key is only `DvrId`, so the FK does not itself guarantee same-building ownership.
- The inventory result supplied so far did not show a view row for `vw_ApartmentWalletMovements`, while the checked-in EF model and schema artifact refer to that view. Verify the live view separately before any migration that depends on it.
- The current live inventory reported no CHECK constraints. This must be rechecked on the final restored baseline before relying on database-enforced domain rules.

### 2.3 Important column-size reconciliation

The live database metadata reports `nvarchar(128)` for many key columns (128 bytes = 64 Unicode characters), while the checked-in EF model configures many of these as maximum length 64. Other legacy fields have wide lengths (for example, `nvarchar(max)`, `nvarchar(450)`, and `BuildingNumber nvarchar(450)`). Do not normalize or shrink columns blindly. The migration design must reconcile actual values, EF configuration, indexes, and key compatibility first.

## 3. Target logical model

The following is the proposed logical model. Exact SQL types, lengths, naming, and physical key strategy are to be finalized in the detailed data dictionary and ERD before migrations are authored.

### 3.1 Person and account identity

#### `Persons`
One human identity, independent of login and property relationships.

Proposed attributes:
- `PersonId` — stable primary key.
- `LegalName`, `DisplayName`.
- `PrimaryEmail`, `PrimaryPhone`, `Whatsapp`.
- `PhotoUrl`.
- `CreatedAtUtc`, `UpdatedAtUtc`.
- `IsArchived` / `ArchivedAtUtc` (soft lifecycle; no physical deletion for referenced people).

Contact values should not be assumed globally unique unless the business explicitly chooses that policy. Shared family phone numbers and email addresses must not cause automatic identity merges.

#### `UserAccounts`
Authentication account for a human person.

Proposed attributes:
- `AccountId` — primary key.
- `PersonId` — required unique FK to `Persons` for human accounts.
- `AccountStatus`, `CreatedAtUtc`, `LastLoginAtUtc`.
- `NormalizedEmail` and/or external login subject identifiers, subject to chosen authentication strategy.
- `SecurityStamp` / concurrency token.

Rules:
- A person has at most one interactive human account.
- A human account belongs to one person.
- A person may exist without an account.
- If future machine/service identities are required, model them explicitly as a separate account subtype rather than inventing a human Person.

#### `UserCredentials` and `UserTwoFactorSettings`
Keep password/PIN credential material and MFA lifecycle out of profile data.

- Store only salted password/PIN hashes using a modern password-hashing scheme; never store recoverable plaintext PINs.
- Store encrypted TOTP secret, hashed one-time recovery codes, replay-prevention step, enrollment/rotation timestamps.
- Secret encryption keys must be held outside the database (deployment secret store / protected key management).
- API/view models return MFA enabled/status metadata only, never secret or recovery-code material.

### 3.2 Apartment ownership, occupancy, and access

#### `ApartmentOwnerships`
Dated legal/economic ownership relationship.

Proposed attributes:
- `OwnershipId`, `BuildingId`, `ApartmentId`, `PersonId`.
- `OwnershipSharePercent` (decimal with explicit precision).
- `ValidFromUtc`, `ValidToUtc`.
- `RecordedAtUtc`, `RecordedByAccountId`, `Source`, `Notes`.

Rules:
- Multiple concurrent owners are allowed.
- Shares for the same apartment and effective date must total 100% where ownership is declared complete; partial/unknown ownership must be represented explicitly rather than silently normalized.
- No overlapping duplicate ownership row for the same person/apartment.
- Corrections are recorded as audited amendments; do not overwrite history without a trace.

#### `ApartmentOccupancies`
Dated resident/occupant relationship.

Proposed attributes:
- `OccupancyId`, `BuildingId`, `ApartmentId`, `PersonId`.
- `OccupancyType` (owner-occupant, tenant, family member, other).
- `IsPrimary`, `ValidFromUtc`, `ValidToUtc`.
- `RecordedAtUtc`, `RecordedByAccountId`, `Notes`.

Rules:
- Multiple occupants are allowed.
- A primary occupant is optional.
- At most one active primary occupant per apartment at a point in time.
- Ownership does not automatically imply occupancy; occupancy does not imply ownership.

#### `ApartmentAccessGrants`
Explicit access where a person/account needs apartment-scoped capabilities without being an owner or occupant (for example, a delegated family representative or authorized caretaker).

Proposed attributes:
- `GrantId`, `BuildingId`, `ApartmentId`, `PersonId` (and optional `GrantedToAccountId` only if justified).
- `GrantType` / permission set.
- `ValidFromUtc`, `ValidToUtc`, `GrantedByAccountId`, `RevokedAtUtc`, `RevokedByAccountId`.

Do not use this table as a substitute for legal ownership or occupancy.

### 3.3 Scoped authorization

#### `RoleDefinitions`
Catalog of stable role keys and descriptions.

#### `UserRoleAssignments`
A many-to-many assignment between account, role, and scope.

Proposed attributes:
- `RoleAssignmentId`, `AccountId`, `RoleKey`.
- `ScopeType`: System, Building, Apartment, Personal.
- `ScopeBuildingId`, `ScopeApartmentId` (nullable according to scope).
- `ValidFromUtc`, `ValidToUtc`, `GrantedByAccountId`, `RevokedAtUtc`.

Rules:
- System SuperAdmin is a System-scoped role.
- A user can hold multiple roles in one building and roles in multiple buildings.
- Scope columns must be validated as a legal combination; use separate nullable FKs and CHECK constraints or a normalized scope table if SQL Server FK rules make a polymorphic scope unsafe.
- Authorization must always evaluate both role and scope; a role name alone is never sufficient.
- Existing `UserPermissions` and `BuildingAdmins` are mapped to explicit assignments and permission grants, then retired only after application cutover.

### 3.4 Polls and secret ballots

Keep existing poll definition and option concepts, but separate voter eligibility/identity from the anonymous ballot.

#### `PollVotingPolicies`
- `PollId`, `VotingMode` (Person or Apartment).
- `IsSecret`, `AllowDelegation`, `QuorumPolicy`.
- `OpensAtUtc`, `ClosesAtUtc`, `PolicySnapshotJson` (versioned and validated).

#### `PollEligibilitySnapshots`
Immutable snapshot of who/what is eligible at poll opening.

- `EligibilityId` (random opaque ID), `PollId`.
- `SubjectType`, `SubjectId` (Person or Apartment).
- `DelegatedByPersonId` / delegation reference when applicable.
- `EligibilityState`, `ConsumedAtUtc`.
- Unique constraint on the eligible subject per poll and voting mode.

This table proves eligibility and prevents repeat voting; it must not store the selected option.

#### `PollDelegations`
- `DelegationId`, `PollId`, `ApartmentId`.
- `DelegatingPersonId`, `RepresentativePersonId`.
- `ValidFromUtc`, `ValidToUtc`, `CreatedAtUtc`, `RevokedAtUtc`.
- Evidence/authorization reference and audit metadata.

A delegation must be explicit, scoped to the relevant poll or a clearly defined period, revocable before use, and checked against current ownership/occupancy/access policy. The policy must define whether one representative can represent more than one apartment and how conflicts are handled.

#### `PollBallots`
Anonymous ballot storage:
- `BallotId` — random primary key.
- `PollId`.
- `OptionId`.
- `CastAtUtc` (precision policy to be chosen; avoid unnecessary identifying metadata).

Critical separation rules:
- No `PersonId`, `AccountId`, `ApartmentId`, `EligibilityId`, delegation ID, or voter token is stored in `PollBallots`.
- No FK or query path may join a ballot choice back to an individual eligibility row.
- The vote endpoint validates a one-time opaque credential, atomically consumes eligibility, and inserts a ballot without persisting the credential-to-ballot mapping.
- Do not log voter tokens, ballot IDs alongside identity, request bodies containing the choice, or precise correlated timestamps.
- Separate database permissions and service boundaries for eligibility and ballot access.
- Results are aggregated from ballots only; voter participation counts are aggregated from consumed eligibility only.

**Security qualification:** this design reduces direct database linkability but does not, by itself, guarantee cryptographic unlinkability against a privileged operator correlating application timing, network metadata, backups, or transaction logs. If legally certified or high-assurance anonymity is required, adopt a separately reviewed blind-signature / cryptographic voting protocol and independent security assessment.

#### `PollBallotOptions` / option reference
Ballots need a valid option reference, but a conventional FK to `PollOptions` may be retained only if it does not create a join to voter identity. Option/poll deletion is prohibited after voting opens; use restrictive delete behavior and immutable poll lifecycle states.

### 3.5 Finance, wallet, and operational records

Preserve the current apartment-scoped wallet semantics and existing financial tables during identity migration:

- `Deposits`
- `DepositEditHistory`
- `Expenses`
- `Revenues`
- `FinancialCategories`
- `MonthlyApartmentShares`
- `WalletAdjustments`

Required principles:
- Do not recalculate or rewrite historic balances as part of identity redesign.
- Do not change currency, amount precision, month-key semantics, or financial posting rules in the same migration unless separately justified and reconciled.
- Replace destructive cascades on posted financial history with restrictive/soft-archive lifecycle where feasible.
- Keep apartment references stable during the transition.
- Preserve edit history even if a deposit is voided or archived.
- Any future ownership-based apportionment is a separate business rule; do not assume ownership share automatically changes apartment wallet liability.

### 3.6 Audit, notifications, QR, cameras, and settings

- Audit events should capture stable actor AccountId/PersonId snapshots where available, role/scope at event time, target identifiers, timestamp, correlation ID, and redacted structured details. Audit retention must not depend on a live FK to an account that can be deleted.
- Notifications should support delivery to a person/account and/or apartment as distinct recipient types; preserve existing apartment notifications during transition.
- QR token issuance should identify the issuing account and scope, use one-way token hashes where practical, have expiry/revocation/usage records, and validate building/apartment consistency.
- Cameras should reference DVR through a composite same-building key (for example, unique `Dvrs(BuildingId, Id)` plus FK `Cameras(BuildingId, DvrId)`).
- Integration secrets should move from `GlobalSettings` into a dedicated encrypted secret store/table with key material external to SQL Server. UI returns configured/not-configured and masked metadata only.
- Presence should use AccountId and explicit current scope; stale presence must not grant authorization.
- Backup and restore metadata must remain operationally separate from business data migrations.

## 4. Legacy-to-target mapping

| Existing source | Target | Mapping / caution |
|---|---|---|
| `Users` | `Persons` + `UserAccounts` + credential/MFA tables | Create one candidate Person per account initially; merge only after verified identity review. Map old Role to scoped role assignments. |
| `Residents` | `Persons` + `ApartmentOccupancies` + optional `ApartmentOwnerships` + account link | Use Uid as a candidate link to Users only after exact verified match. Do not merge on name or phone alone. |
| `Apartments.Owner` | ownership candidate staging | Treat as unverified legacy text; never auto-create legal owner from free text without review. |
| `Apartments.Phone/Email` | contact candidate staging | Determine whether apartment-level contact is shared or person-specific before moving. |
| `Apartments.Pin` | legacy credential transition | Do not copy plaintext PIN into target. Force reset or perform controlled re-enrollment. |
| `Residents.IsOwner` | ownership candidate | Preserve as a candidate flag only; resolve share and validity dates explicitly. |
| `Residents.IsPrimary` | occupancy primary candidate | Validate no multiple active primary occupants; retain original values in migration evidence. |
| `BuildingAdmins` | `UserRoleAssignments` | Map to Building-scoped admin role; preserve all current links. |
| `UserPermissions` | role/permission assignments | Map permission grants without widening scope. |
| `Users.TwoFactor*` | `UserTwoFactorSettings` | Preserve encrypted secret and recovery-code hashes securely; validate key availability before cutover. |
| `PollVotes` | staged vote migration | Existing vote is apartment-attributed. Do not claim historical secret ballot. Preserve historical result/participation with explicit legacy provenance. |
| `Deposits/Expenses/Revenues/MonthlyApartmentShares/WalletAdjustments` | retain existing tables | No identity-driven rewrite of financial facts. |
| `AuditLog` | audit store | Add nullable actor identity references/snapshots; retain legacy Actor/ActorRole fields during transition. |
| `GlobalSettings` secret fields | encrypted secret store | Encrypt/rotate credentials; remove old values only after verified consumers switch and backup/restore is tested. |
| `QrTokens/QrUsages/Presence` | account-linked scope model | Backfill identity only when Uid mapping is verified; preserve expiry and usage evidence. |

### Duplicate resolution policy

1. Exact stable external UID match may be used as a candidate identity link, subject to uniqueness and ownership verification.
2. Matching email/phone is a review signal, not proof of identity.
3. Names are never sufficient to merge.
4. Shared household contacts are allowed.
5. Ambiguous records remain separate and are reported for manual resolution.
6. Every source row receives a durable migration mapping and outcome (mapped, merged with evidence, unresolved, rejected).
7. Preserve source identifiers for support and audit; do not reuse legacy IDs for unrelated target entities.

## 5. Data quality and profiling gates

Before writing data, run reviewed read-only profiling queries on a disposable restored copy. Required reports:

- Duplicate `Residents.Uid`, `Users.Uid`, normalized emails, and normalized phones; report candidate groups, do not merge.
- Resident rows without a valid apartment; apartment rows without valid building/floor references.
- `Residents` rows where `Uid` cannot be matched to a human account.
- Conflicts between `Apartments.Owner` and `Residents.IsOwner`.
- Multiple `IsPrimary=1` residents for one apartment.
- Missing/invalid ownership share candidates and date overlaps.
- Financial FK orphans, duplicate transaction identifiers, and per-apartment/month reconciliation totals.
- Poll vote option/poll/apartment consistency and duplicate participation.
- Camera/DVR building mismatches.
- QR token references to missing or mismatched buildings/apartments.
- Secret/configuration field inventory without selecting or displaying secret values.
- Actual presence/definition/dependencies of wallet views and triggers.
- FK delete actions and any untrusted/disabled constraints.
- Column-length overflow risk before any narrowing or key change.

All profiling output must avoid exposing PINs, passwords, API keys, recovery codes, personal row-level values, and payment secrets.

## 6. Migration and rollout strategy

### Stage 0 — Baseline freeze and reproducibility
- Keep the current branch and source branch untouched.
- Take a verified SQL Server backup of the disposable test database.
- Record server/database identity, migration history, schema hash, row counts, constraints, indexes, and wallet-view definitions.
- Re-run inventory immediately before each schema migration.
- Never use production as the first migration target.

### Stage 1 — Additive identity foundation
- Add new identity, credential, role, ownership, occupancy, and access tables.
- Add required indexes and restrictive FKs.
- Do not remove or rename legacy columns/tables.
- Deploy compatibility code behind feature flags; old and new paths must not create divergent identities.

### Stage 2 — Deterministic backfill
- Populate target Persons/Accounts using a migration mapping ledger.
- Backfill only high-confidence links automatically.
- Quarantine ambiguous identity and ownership candidates for review.
- Run the backfill in restartable batches with checkpoints and idempotency keys.
- Record row counts and checksums before/after.

### Stage 3 — Dual-read / controlled dual-write
- Introduce a single application service boundary for identity and scoped authorization.
- Read target first with controlled legacy fallback.
- If dual-write is required, use a transactional outbox or a single database transaction; do not rely on best-effort independent writes.
- Monitor mismatches and prohibit cutover while any unresolved critical mismatch exists.

### Stage 4 — Domain transitions
- Migrate residents and apartment access.
- Migrate scoped roles and permissions.
- Migrate poll eligibility/delegation and future secret-ballot flow. Historical votes retain legacy provenance.
- Move secrets to protected storage and verify encryption-key recovery.
- Keep finance tables and wallet behavior stable.

### Stage 5 — Cutover and legacy retirement
- Freeze writes briefly, reconcile final deltas, and switch feature flags.
- Verify sign-in, MFA, role/scope checks, ownership/occupancy screens, wallet, deposits, notifications, QR, polls, audit, and backup/restore.
- Retain legacy tables/columns read-only through an agreed observation period.
- Drop legacy structures only in a later, separately reviewed migration after proving no code, reports, integrations, or restore procedures depend on them.

## 7. Rollback and recovery

- Before cutover, rollback means disabling feature flags and retaining the untouched legacy path.
- After new writes begin, rollback must account for writes made only in the new model; use a tested reverse-sync or restore-and-replay plan.
- Never promise rollback by simply dropping new tables.
- Keep pre-migration backup immutable and test an actual restore.
- Each migration must be transactional where SQL Server permits, restartable, and have explicit preconditions/postconditions.
- Do not delete or overwrite original source rows during backfill.
- Financial reconciliation must compare counts and sums by building, apartment, month, and transaction type.

## 8. Authorization, retention, and deletion policy

- Human identity is not deleted while referenced; archive/disable accounts and retain person history.
- Ownership and occupancy are ended by closing validity intervals, not deleting rows.
- Role grants and access grants are revoked/expired, not erased.
- Polls with opened ballots cannot be physically deleted; close/archive with retained results and evidence.
- Posted financial transactions and their edit history are immutable/retained; use void/reversal entries rather than destructive delete.
- Audit records are append-only under application policy; actor references are nullable snapshots rather than deletion blockers.
- Building deletion must be a controlled archival/legal process, not a cascade that erases finance/audit evidence.
- FKs should default to RESTRICT/NO ACTION for historical and financial records; cascade only for truly disposable dependent data after explicit review.

## 9. Required technical controls

- Explicit UTC storage and clear local-time presentation rules.
- Decimal precision for ownership percentages and money defined explicitly.
- CHECK constraints for legal scope combinations, percentage bounds, date ordering, and valid status transitions.
- Filtered unique indexes for one active primary occupant and active uniqueness rules where SQL Server supports the intended semantics.
- Unique constraints for one account per person, one eligibility per subject/poll, and one consumed eligibility.
- Concurrency controls for vote consumption, ownership share updates, and role revocation.
- Composite FKs for same-building relationships.
- Query indexes aligned with tenant/building and effective-date predicates.
- Row-level authorization must be enforced in services/controllers, not only hidden UI elements.
- Security-sensitive values must be redacted from logs, diagnostics, exports, and exception messages.

## 10. Acceptance and reconciliation checklist

The redesign is not complete until all applicable checks pass:

### Identity and access
- Every human account links to exactly one Person.
- No Person has more than one interactive account.
- No ambiguous identity has been silently merged.
- Existing valid users can authenticate or complete the defined credential reset flow.
- SuperAdmin can also hold building/apartment roles without a second account.
- Cross-building and cross-apartment authorization tests prove isolation.

### Ownership and occupancy
- Multiple owners and occupants per apartment are represented.
- Ownership shares reconcile to declared totals or are explicitly marked incomplete.
- Historical ownership/occupancy intervals are preserved.
- Primary occupant uniqueness is enforced.
- Person with two apartments and person with apartments in different buildings both work correctly.

### Polling
- Person-based and apartment-based modes are both enforced.
- Apartment voting requires a valid explicit delegation.
- A voter cannot consume eligibility twice under concurrent requests.
- Secret ballot table has no identity/eligibility link.
- Results and participation totals reconcile separately.
- Historical apartment-attributed votes are clearly labelled as legacy, not retroactively secret.

### Finance and operations
- All financial row counts and monetary totals reconcile.
- Wallet balances before and after migration match for every apartment.
- Deposit edit history is retained.
- Camera/DVR building integrity is enforced.
- QR, notifications, presence, backups, and audit flows pass regression tests.

### Security and reliability
- No plaintext PIN/API credential is copied into target storage.
- No secret is returned to browser models.
- MFA enrollment, verification, replay prevention, and recovery-code rotation pass tests.
- Restore test is successful.
- Migration is restartable and failure injection proves no partial destructive state.
- Application build, unit/integration tests, and security tests pass on the target branch.

## 11. Decisions to lock before physical schema design

These decisions are not inferable from the current metadata and must be resolved in the product rules before final DDL:

1. Are ownership percentages required to total exactly 100% at every effective date, or may the system represent partial/unknown ownership?
2. Can a tenant/occupant vote personally in a person-mode poll, and who is eligible in apartment-mode polls: owners, occupants, or a configurable policy?
3. Can one representative hold active delegation for several apartments in the same poll?
4. Does apartment notification/payment preference remain shared at apartment level, or become person-specific with an optional apartment default?
5. Which existing `Users` / `Residents` authentication methods remain supported during transition (password, PIN, external provider), and what is the reset policy for plaintext legacy PINs?
6. What is the retention/legal policy for deleted buildings, financial history, audit events, and poll records?
7. Is ordinary operational ballot secrecy sufficient, or is cryptographic unlinkability/certification required?

These are product-policy decisions, not schema facts. The physical schema must not silently assume answers.

## 12. Explicit non-goals of this blueprint

- No EF Core migration has been generated.
- No database schema or data has been modified.
- No production database has been accessed.
- No existing financial table has been redesigned or recalculated.
- No legacy user/person records have been merged.
- No claim is made that the current wallet view exists in the live test database until its definition is verified.

## 13. Next engineering artifact

After the seven decisions above are confirmed, prepare:
1. A table-by-table data dictionary with exact SQL types and key lengths.
2. A Mermaid ERD for the final physical model.
3. A source-to-target column mapping workbook/document.
4. Read-only data profiling SQL with safe output.
5. Additive EF Core entity/configuration changes and migrations, tested only on a fresh disposable restore first.
