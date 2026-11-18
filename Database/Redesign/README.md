# Enterprise Database Redesign — Discovery Baseline

## Status

- Architecture approved by the project owner.
- Discovery branch: `development/database-redesign-enterprise`.
- This branch is based on `development/phase14-enterprise-logs-azure-ui`.
- Initial work is read-only discovery only.
- No live SQL Server database has been connected to or modified.
- No EF migration has been created or applied.

## Approved business rules

1. One person has at most one interactive account.
2. Ownership, occupancy, and authorization are independent relationships.
3. Multiple owners with shares and multiple occupants are supported.
4. A primary occupant is optional; if assigned, only one active primary occupant may exist for an apartment at a given time.
5. Multiple roles per user and multiple managed buildings are supported.
6. SuperAdmin is a system-scoped role, not a separate identity/account type.
7. Polls can vote by person or apartment. Apartment voting requires an explicitly delegated representative.
8. Secret voting is required. Identity/eligibility evidence must be kept separate from the ballot choice.
9. Wallet balances and transactions remain apartment-scoped.
10. Financial, ownership, occupancy, security, and audit history must not be silently cascade-deleted.

## Repository baseline inspected

The following files were inspected from the approved source branch:

- `Data/Entities.cs`
- `Data/AppDbContext.cs`
- `BuildingManagement-FullSchema.sql`
- `Services/ResidentsService.cs`
- `Services/UsersService.cs`
- `Services/SqlAuthService.cs`
- `Services/PollsService.cs`
- `Services/WalletService.cs`
- `Data/ResidentSqlStore.cs`
- `Data/UserSqlStore.cs`
- `Migrations/AppDbContextModelSnapshot.cs`
- `Migrations/20261117012657_Phase04_StandardizeKeyColumnTypes.cs`
- `Migrations/20261117050000_Phase05_RemoveRedundantResidentIndexes.cs`
- `Migrations/20261117130000_AddUserTwoFactor.cs`

The checked-in `BuildingManagement-FullSchema.sql` is a generated schema artifact, not proof of the currently deployed database schema. The live database must be inventoried separately.

## Confirmed repository findings

### Identity and access

- `Users` and `Residents` are separate authentication/data paths.
- `Residents.Uid` has a unique index, but the reviewed model does not define a foreign key from Residents to Users.
- `ResidentsService` creates resident records independently; it does not establish a unified Person/UserAccount identity.
- `SqlAuthService` authenticates residents using building/floor/apartment/phone/PIN, while administrators use the Users/BuildingAdmins path.
- `UserEntity.Role` is a single role string (`resident`, `admin`, `superadmin`); building administration is separately represented by `BuildingAdmins`.
- The current design therefore cannot directly represent the approved single-person/multiple-scope identity and role model without a compatibility transition.

### Apartment profile duplication

- `Apartments` contains `Owner`, `Phone`, `Email`, and `Pin` alongside property fields.
- `Residents` and `Users` also contain personal/contact/authentication data.
- `Apartments` also contains language, notification, and preferred-payment settings. These settings must be classified as apartment-shared or person-specific before migration.
- Do not drop or repurpose these fields until all reads/writes and legacy data semantics are mapped.

### Occupancy and deletion

- `Residents` is linked to `Apartments` through a composite `(BuildingId, ApartmentId)` foreign key with `ON DELETE CASCADE`.
- `Residents` contains `IsOwner` and `IsPrimary`; these are not a substitute for dated many-to-many ownership and occupancy histories.
- The cascade behavior is a data-retention risk and must be addressed only through a tested migration and service changes.

### Voting

- Current `PollVotes` primary key is `(BuildingId, PollId, ApartmentId)`, enforcing one vote per apartment per poll.
- Current `PollsService.Vote` accepts an apartment ID and stores the selected option in an apartment-keyed dictionary.
- Current service includes poll deletion. The target secret-ballot model must introduce an immutable/retained ballot lifecycle and prevent ordinary deletion of a poll after voting begins.
- Delegated apartment representatives, person voting, eligibility snapshots, and secret-ballot separation do not exist in the reviewed service/model.

### Financial and property integrity

- Existing apartment wallet logic is apartment-scoped and should be preserved during the transition.
- The checked-in schema contains `vw_ApartmentWalletMovements`; the earlier concern that the view might be absent is not true for this checked-in schema version. Live DB presence still needs verification.
- Multiple financial and operational tables have building-level cascade paths. Each must be classified by retention requirements before any delete behavior is changed.
- `DepositEditHistory` currently cascades from Deposits. Whether this is acceptable depends on whether a deposit may ever be physically deleted; target policy is to preserve posted financial history.

### Schema / migration alignment

- The checked-in FullSchema Users definition does not include the four 2FA columns, while the dedicated `AddUserTwoFactor` migration adds them. The live database and migration history must be inspected to determine whether this is expected snapshot drift or a deployed-schema difference.
- `AppDbContextModelSnapshot.cs` exists under `Migrations/` (not `Data/Migrations/`).
- Phase04 changes key-column types and rebuilds affected keys/foreign keys. Its actual applied state must be confirmed from `__EFMigrationsHistory` and live metadata before generating another migration.
- Phase05 removes redundant Residents indexes and explicitly anticipates that some indexes may already have been manually removed on a test database. Live index inventory is required before further index work.

## Read-only live database inventory

Run `Database/Diagnostics/DatabaseRedesign-Discovery.sql` against the intended SQL Server database using a read-only login where possible. It returns metadata only:

1. Server/database/version identity
2. User tables and approximate row counts
3. Column definitions and defaults
4. Primary/unique keys
5. Foreign-key column mappings and delete/update actions
6. Indexes and filters
7. Check constraints and trust status
8. Views/modules and triggers (names/metadata)
9. EF migration history
10. Synonyms

Save/export all result sets and provide them for reconciliation. Do not send connection strings, passwords, PINs, API keys, or row-level personal/financial data.

## Execution gates

The following gates are mandatory:

1. Reconcile the live database inventory with EF Core model, migrations, checked-in SQL, and runtime code.
2. Produce a source-to-target table/column mapping, including duplicate-person resolution rules.
3. Produce a data profiling report for orphaned references, duplicate candidate identities, ownership-share anomalies, date overlaps, and financial totals. Profiling queries must be reviewed before execution and must not expose secrets.
4. Finalize target ERD, data dictionary, constraints, indexes, and voting secrecy design.
5. Prepare additive, restartable, auditable migrations and a tested rollback/restore plan.
6. Apply only to a restored disposable/test database first.
7. Run reconciliation tests and application regression tests before production approval.

No destructive schema change, data migration, production deployment, or live database write is authorized by this discovery artifact alone.

## Current execution status — 2026-11-18

The live inventory result files have now been reviewed:
- `Database/Diagnostics/DatabaseRedesign-ReadOnly-Profiling_01.txt` through `_07.txt`.

They contain the seven discovery result groups (database identity, row counts, columns, keys, FKs, indexes, and migration history). They are **not** the ten data-profiling result sets emitted by `DatabaseRedesign-ReadOnly-Profiling.sql`. The distinction and the exact missing evidence are documented in `Live-Inventory-Assessment.md`.

A first additive Gate A SQL foundation has been authored:
- `Database/Redesign/GateA-Additive-Identity-Foundation.sql`.

It creates the new Person/Account/Credential/MFA/Role and dated Apartment Ownership/Occupancy/Access tables only if absent, with restrictive FKs and no legacy-table changes. It performs no data backfill, no legacy-column retirement, and no application cutover. It is a review/rehearsal script, **not an EF Core migration and not yet executed**. Do not run it against production.

Current safe boundary:
- No database has been modified by this work.
- No EF Core migration has been generated or applied.
- Legacy authentication and finance paths remain unchanged.
- Backfill/cutover remain blocked pending the missing aggregate profiling outputs and a disposable-database rehearsal.

## Additional design artifacts

- Read-only local collector: `Database/Diagnostics/Run-Redesign-Schema-Parity.ps1` — exports database identity, tables/row counts, columns, PK/UK/FK/CHECK/default constraints, indexes, views/definitions/dependencies, legacy key metadata, and EF migration history. It requires the exact restore DB/branch/remote and a clean worktree; it does not execute DDL/DML.

- `Database/Redesign/Owner-Decision-Summary.md` — concise owner-facing summary of the highest-impact open decisions with recommended defaults; unrelated design work continues while decisions remain open.

- `Database/Redesign/Audit-Outbox-and-Observability-Target-Design.md` — durable business/security audit, transactional outbox/inbox, diagnostic log separation, scope-based access, retention, redaction, and operational tests.

- `Database/Redesign/Target-Entity-Inventory-and-Phased-Schema-Roadmap.md` — whole-system target entity inventory, Gate A vs later entities, legacy coexistence, and phased schema delivery boundaries.

- `Database/Redesign/Financial-Ledger-and-Wallet-Target-Design.md` — legacy wallet formula, financial command state machines, immutable ledger concepts, allocation/rounding, period semantics, reconciliation, and owner decisions DEC-15/16/17.

- `Database/Redesign/Sql-Deployment-and-View-Reconciliation-Decision.md` — confirms the EF mapping/FullSchema/restore mismatch for vw_ApartmentWalletMovements and recommends one authoritative deployment path (EF migrations by default); owner decision DEC-28 remains open.

- `Database/Redesign/Authorization-Evaluation-Contract-and-Precedence.md` — centralized server-side authorization algorithm, scope inheritance, Deny precedence, SuperAdmin semantics, privileged step-up, revocation/cache rules, and security test matrix.

- `Database/Redesign/Legacy-Class-to-Target-Module-Refactoring-Map.md` — observed legacy Controllers/Services mapped to target modules with Replace/Split/Adapt/Retire dispositions, missing workflows, and migration order.

- `Database/Redesign/GateA-Column-and-Constraint-Parity-Matrix.md` — static comparison of all 13 Gate A tables/137 columns against the data dictionary, plus constraint inventory and remaining blockers. No SQL execution; Gate A remains unauthorized.
- `Database/Redesign/Live-Schema-Parity-Assessment-2026-11-18.md` — assessment of the 15-result-set live restore metadata collection, Gate A absence, legacy key compatibility, FK delete actions, view/migration drift, and remaining blockers. Gate A remains NOT AUTHORIZED.

- `Database/Redesign/GateA-Disposable-Restore-Rehearsal-Plan.md` — hard stops, evidence bundle, local script behavior, postflight acceptance, and rollback for a future disposable-restore rehearsal. It is preparation only; Gate A remains unauthorized.

- `Database/Redesign/Domain-Glossary-and-Application-UseCase-Catalogue.md` — canonical domain language, aggregate boundaries, command/query catalogue, typed outcomes, actor context, and cross-cutting implementation constraints.

- `Database/Redesign/Legacy-Permission-Scope-Catalogue-2026-11-18.md` — all 28 legacy permission keys mapped to proposed scopes/resources, stale catalog references, risky combined capabilities, target decomposition, and five owner decisions. Proposed only; no permission grants or code changed.

- `Database/Redesign/Cross-Document-Consistency-and-GateA-Design-Review-2026-11-18.md` — cross-check of architecture, ERD, data dictionary, mapping, Gate A SQL, and decision register; records corrections and remaining technical blockers. Documentation/design only; Gate A remains unauthorized.

- `Database/Redesign/Implementation-Backlog-and-Decision-Register.md` — sequenced epics, dependencies, phase acceptance evidence, and 30 explicit business/technical decisions with open/proposed/evidence-required/blocked states. Planning only; no implementation authorization.

- `Database/Redesign/Target-Architecture-and-Class-Refactoring-Roadmap.md` — target modular-monolith architecture, module/class responsibilities, dependency rules, cross-cutting patterns, migration/coexistence strategy, delivery phases, acceptance tests, and blocking decisions. This is a design baseline only; it does not authorize implementation or Gate A.

- `Database/Redesign/Enterprise-Database-Redesign-Blueprint.md` — architecture principles, target domains, migration/rollback approach, and unresolved product decisions.
- `Database/Redesign/Target-Data-Dictionary-and-ERD.md` — proposed target data dictionary, SQL Server type conventions, constraints/index strategy, and conceptual Mermaid ERD. Proposed types are not final until key metadata reconciliation is complete.
- `Database/Diagnostics/DatabaseRedesign-ReadOnly-Profiling.sql` — read-only, privacy-conscious profiling for identity candidates, ownership/occupancy, finance reconciliation, poll integrity, camera/DVR scope, secret presence counts, wallet-view metadata, FK delete actions, and EF migration history.

These artifacts do not create or apply database changes. The profiling script is intended for the disposable `BuildingManagement_RestoreTest` database and returns aggregate counts/totals rather than personal or secret values.

## Legacy mapping and reconciliation rules — 2026-11-18

Added:
- `Database/Redesign/Legacy-to-Target-Data-Mapping.md` — field-level mapping rules for Users, Residents, Apartments, BuildingAdmins, UserPermissions, credentials, MFA, voting, and financial history.
- Explicit UID candidate-link rules; no matching by name/phone/email alone.
- Manual-review treatment for legacy owner text and IsOwner/IsPrimary flags.
- Credential/MFA/secret handling constraints and reconciliation acceptance criteria.

**New blocking finding:** legacy `UserPermissions` is account-specific, while the proposed `RolePermissions` is role-specific. These structures are not equivalent. The target must add a scoped account-permission override model (recommended) or approve a controlled custom-role conversion before permission backfill. No permissions are to be migrated until this gap is closed.

Repository inspection also confirms that `Residents` contains one row per apartment relationship with a unique `Uid`, and that its current `IsOwner`/`IsPrimary` flags do not provide legal ownership shares or dated occupancy history. The mapping artifact records these as candidates requiring profiling/review, not automatic migrations.

Current implementation boundary remains unchanged: no EF entity changes, no EF migration, no SQL execution, no backfill, and no application cutover have been performed.

### Permission-model gap addressed in Gate A design

The additive foundation now also defines `AccountPermissionOverrides`:
- Preserves account-specific legacy permission semantics separately from role-wide `RolePermissions`.
- Supports System/Building/Apartment/Personal scope, Allow/Deny effect, effective dates, revocation audit, rowversion, and restrictive scope FKs.
- Uses deny precedence; interval overlap and duplicate active grants remain transactional service validations.
- The target data dictionary and legacy mapping document now describe this table.

This closes the schema-design gap for preserving `UserPermissions` without broadening a user's permissions to every holder of a role. It does **not** mean the table has been executed or that the EF model/backfill is implemented.

### Gate A metadata preflight

Added `Database/Diagnostics/GateA-Preflight-Validation.sql`. It is read-only and reports:
- Actual SQL type, byte length, nullability, and collation for legacy FK columns referenced by Gate A.
- Candidate PK/unique-key metadata for Buildings and Apartments.
- Existing Gate A object names (to detect partial/manual prior creation).
- EF migration history for the selected restored database.

Run this only against the disposable restored test database. A result of `TYPE_LENGTH_OK_REVIEW_COLLATION` is not a final pass by itself: compare collations across all FK components and inspect the key/index result set. Any mismatch or pre-existing partial target object blocks execution pending review. The preflight itself has not been run by this repository change.

### Latest results reviewed — Gate A preflight (2026-11-18)

Found and reviewed:
- `GateA-Preflight-Validation_01.txt` through `_06.txt`.

The live legacy FK columns match the expected `nvarchar(128)` byte length and all use `SQL_Latin1_General_CP1_CI_AS`; Buildings and Apartments have the expected referenced primary keys; all thirteen Gate A target tables are absent; and the database has the four expected EF migrations through AddUserTwoFactor.

The initial output did not report the database default collation. `GateA-Preflight-Validation.sql` has been extended with that check. Re-run it and add the new result file in this same Diagnostics folder. Gate A remains blocked pending that result and the ten aggregate profiling result sets. No DDL/DML was executed.


## Aggregate profiling update — 2026-11-18

The read-only aggregate profiling run has completed and its 15 result sets are committed under `Database/Diagnostics/Profiling-Results/20261118-145432/`. See `Database/Redesign/Aggregate-Profiling-Assessment-2026-11-18.md` for the findings and required reconciliation actions.

The profiling evidence is no longer a blocker. Gate A execution is still blocked pending the updated Gate A preflight (including database default collation), credential strategy, manual identity-link decisions, finance/carry-over reconciliation, wallet-view discrepancy resolution, and retention review of cascade relationships. No DDL, EF migration, backfill, or application cutover has been executed.
\n## Updated Gate A preflight result — 2026-11-18\n\nThe latest read-only run is committed under `Database/Diagnostics/GateA-Preflight-Results/20261118-150906/` (7 result sets). It confirms: the three referenced legacy FK key columns are non-nullable `nvarchar(64)` / 128 bytes; all use `SQL_Latin1_General_CP1_CI_AS`; the database default collation matches; expected Buildings/Apartments primary keys and indexes are present; all thirteen Gate A target objects are absent; and the four expected EF migrations remain applied.\n\nThis clears the previously pending database-default-collation check for the inspected key relationships. It does **not** clear Gate A overall. Credential strategy, manual identity-link decisions, finance/carry-over reconciliation, wallet-view discrepancy, cascade retention policy, and a disposable-restore rehearsal with rollback verification remain mandatory blockers. The run was read-only; no schema/data changes, EF migration, backfill, or cutover occurred.\n

## Wallet and PIN source review — 2026-11-18

Added `Wallet-and-Credential-Code-Review-2026-11-18.md` and a read-only local runner for wallet-view metadata/dependencies and aggregate credential-format classification. Key findings: the repository defines/maps `vw_ApartmentWalletMovements`, but no EF migration creates it and no runtime consumer was found; it is not a full balance view. No repository definition for `vw_ApartmentWalletBalance` was found. Also, `Program.cs` invokes a startup PIN migration that hashes non-prefixed credential strings and clears legacy Building/Apartments PIN fields. Do not start the application on the restore before the new SQL-only diagnostic is reviewed. Results are pending; no DDL/DML or credential migration was executed.

Run: `Database/Diagnostics/Run-Wallet-and-Credential-ReadOnly-Validation.ps1` after pulling the branch. The script exports aggregate-only credential classifications (never secret values) and commits/pushes only its result folder.



## Wallet and credential diagnostic results — 2026-11-18

Reviewed results: `Database/Diagnostics/Wallet-Credential-Validation-Results/20261118-151511/` (6 result sets, 15 rows).

- Both requested wallet views are absent from the restored test database; no SQL-module dependency references were returned. External reporting/client dependencies have not been ruled out.
- All 5 Users PINs and all 79 Residents.PinHash values are BCrypt-shaped, length 60, but remain cryptographically unverified.
- Apartments.Pin (79), Buildings.AdminPin (3), and Residents.PasswordHash (79) are empty.
- The diagnostic exports no secret values and makes no database changes.

Do not run the current startup credential migration as a diagnostic step. The next credential gate is non-mutating hash verification with aggregate-only output and an explicit invalid-credential recovery/re-enrollment plan. Gate A remains blocked by credential policy, reviewed identity linking, finance/carry-over reconciliation, external wallet-view dependency confirmation, cascade retention, and disposable rehearsal/rollback. No DDL/DML, EF migration, backfill, or cutover has been authorized or executed.



## Authentication and credential flow review — 2026-11-18

Added `Authentication-and-Credential-Flow-Review-2026-11-18.md` after reviewing the startup migration, BCrypt verifier, direct/unified login paths, account-context selection, session revalidation, MFA enforcement, and existing verifier tests.

Key decisions/findings:
- `PinMigrationService.MigrateLegacySecretsAsync()` still runs during application startup and mutates/clears legacy credential fields. Do not start the application against the restore for diagnostics; isolate this migration in an explicit audited workflow.
- BCrypt detection is prefix-only; existing unit tests currently treat prefix recognition as sufficient. Strict syntax/parseability and actual user-secret verification must remain separate concepts.
- Direct resident login omits `IsActive` from its initial selection predicate, while unified login filters it. SessionRevalidator later rejects inactive residents, but entry-point behavior should be consistent.
- Missing-PIN responses differ from generic invalid-credential responses and need an explicit disclosure/timing policy.
- Unified account choices are stored server-side in Session, and SessionRevalidator provides subsequent state/scope checks; the source review did not establish a direct authentication bypass. Add a fresh state/scope check before cookie issuance as hardening.
- Current MFA is enforced only for accounts that have enabled it; mandatory admin/SuperAdmin MFA remains a target-policy gap requiring enrollment and recovery rollout.
- `Users.Pin` currently serves admin PIN and SuperAdmin password semantics; target credentials must be typed explicitly.

No application code was changed, no tests were run, no application was started, and no database or credential values were modified/exported. Gate A remains blocked.



## Financial and wallet source review — 2026-11-18

Added `Financial-and-Wallet-Code-Review-2026-11-18.md` after reviewing `WalletService`, `BuildingSqlMapper`, monthly share entities, and expense/revenue call sites.

Key findings:
- Historical wallet calculations use the apartment's current `MonthlyFee` and current `Closed` state for every month; historical fee and eligibility are not snapshotted.
- Recalculating an old month uses today's open-apartment set. Historical allocation eligibility must be dated and preserved.
- The distribution method conserves the aggregate total by assigning the rounding remainder to the last apartment, but apartment loading has no explicit stable ordering.
- Stored monthly shares are loaded as authoritative inputs; they can be stale relative to source ledgers until recalculated.
- The +0.22 expense and +0.04 revenue differences require apartment-level attribution; aggregate totals do not identify the cause.
- Carry-over recomputation depends on prior-month balance and can inherit mutable current fee/closed-state semantics.
- Pending/cancelled deposits are excluded from wallet balance; preserve their state and audit history.

Next step: prepare a read-only per-apartment financial reconciliation diagnostic for the affected building/month and all other ledger/share mismatches. No code, tests, SQL, or financial data were changed by this review. Gate A remains blocked.



## Financial reconciliation diagnostic prepared — 2026-11-18

Added read-only diagnostic files:
- `Database/Diagnostics/Financial-Reconciliation-ReadOnly-Validation.sql`
- `Database/Diagnostics/Run-Financial-Reconciliation-ReadOnly-Validation.ps1`

The runner is restricted to `BuildingManagement_RestoreTest`, uses Windows Integrated Security, exports five separate CSV result sets, and commits/pushes only its own timestamped result folder under `Database/Diagnostics/Financial-Reconciliation-Results/` on `development/database-redesign-enterprise`. It refuses a different database, a different branch, or an already-staged Git index. It does not auto-pull.

The SQL compares source expense/revenue ledgers with stored monthly shares by building/month, exports per-apartment stored shares and current fee/closed-state indicators, computes a clearly labelled deterministic candidate allocation ordered by ApartmentId (not proof of historical order), and flags missing/orphan share rows. It is SELECT-only and does not recalculate or update any financial data. No run results exist yet; the script has not been executed or locally tested by the assistant.

Run after pulling the branch:

```powershell
.\Database\Diagnostics\Run-Financial-Reconciliation-ReadOnly-Validation.ps1
```

Do not modify any stored shares based on candidate results alone.


## Financial reconciliation results reviewed — 2026-11-18

Reviewed the five result sets (215 rows) under `Database/Diagnostics/Financial-Reconciliation-Results/20261118-152535/`. See `Financial-Reconciliation-Results-Assessment-2026-11-18.md`.

- `LmkUDOJcjlvjiGmluz6c` / 2026-10 has a confirmed stored-share rounding variance: expenses +0.22 and revenues +0.04. All 60 apartment rows exist; the 57 currently open apartments have equal rounded stored shares, so the stored totals exceed the source ledgers. The deterministic candidate assigns the final remainder to one apartment and exactly conserves both totals, but its ApartmentId order is not proof of historical order.
- `zxhRd7cwZ07rZotXeQ17` / 2026-09..11 has zero expense/revenue ledgers and shares; 15 share rows per month. September's 6,356.00 cancelled deposits remain excluded; 5,119.00 confirmed deposits are attributed to apartment 12. Carry-over totals are 0.00 / 3,319.00 / 1,519.00 across Sep/Oct/Nov and remain apartment-scoped. Negative reconstructed balances elsewhere are not automatically errors.
- `ZzgYia4qMZ23N8qhiCOV` / 2026-09 and 2026-10 reconciles exactly. November is in progress (run date Nov 18), with no monthly shares or ledger rows and +200.00 adjustments; absence of monthly shares is not treated as a defect.
- The missing/orphan result set returned no rows for the scoped months.

No financial data was changed. Do not silently recalculate historical shares. Gate A remains blocked pending explicit variance/restatement policy, negative-balance policy, historical fee/eligibility snapshots, month closing rules, credential strategy, identity-link decisions, retention policy, and disposable restore rehearsal/rollback.


## Identity resolution and authorization review — 2026-11-18

Added `Identity-Resolution-and-Authorization-Review-2026-11-18.md`.

- Existing profile evidence: 79 Residents, 5 Users, zero exact UID matches between the two sets; duplicate normalized-phone groups exist (4 in Residents, 1 in Users), while duplicate normalized-email groups were not found.
- Phone/email/name/apartment equality is not sufficient to merge people. Keep unresolved source rows separate; one Person may retain multiple apartment relationships, while one Person may have at most one interactive account.
- This review originally identified a missing durable source-to-target identity map. The gap was subsequently addressed in the Gate A design by adding `LegacyIdentityMap`; see the correction section below.
- `UserSqlStore.UpsertAsync` adds BuildingAdmins links but does not remove omitted links from an update payload. Review explicit revocation paths and historical evidence before treating every stored link as currently active.
- Preserve legacy `UserPermissions` as scoped account overrides; do not convert them to role-wide permissions.

Identity backfill remains blocked pending manual candidate dispositions, authorization revocation review, and approved merge controls. No database or application changes were made.


## Gate A identity provenance correction — 2026-11-18

Added `dbo.LegacyIdentityMap` to the proposed additive Gate A SQL and added it to `GateA-Preflight-Validation.sql`. The expected Gate A target-object inventory is now 13 tables.

- Initial source types: Users and Residents.
- Residents rows may map many-to-one to a Person; they do not directly store AccountId in the map.
- A linked Users source row requires PersonId + AccountId. The migration service must verify the account belongs to that person transactionally.
- A filtered unique index prevents two legacy Users rows mapping to the same Account. Source row identity is unique by (SourceSystem, SourceTable, SourceKey).
- Pending, rejected, and manual-review rows can remain unresolved. No matching by phone/name/email and no automatic merge.

Updated: `Target-Data-Dictionary-and-ERD.md`, `Legacy-to-Target-Data-Mapping.md`, and `Identity-Resolution-and-Authorization-Review-2026-11-18.md`.

**Next action:** pull the branch and rerun the revised read-only Gate A preflight on the disposable restore. This only checks metadata/object presence; it does not execute the Gate A DDL. The preflight runner has not been run after this change. No database changes or backfill occurred.


## Latest Gate A preflight and schema review — 2026-11-18

Reviewed the updated read-only run at `Database/Diagnostics/GateA-Preflight-Results/20261118-155025/` (7 result sets, 30 rows). It confirms the expected restored database, the three legacy key columns as non-nullable `nvarchar(64)` / 128 bytes, matching column and database-default collations, required Buildings/Apartments keys, all **13** Gate A target objects absent, and the four expected EF migrations.

Added `GateA-Schema-Constraint-Review-2026-11-18.md`. The static review documents which invariants are enforced by SQL and which require transactional service validation, including the LegacyIdentityMap Person/Account pair, dated ownership and occupancy overlaps, role/scope compatibility, permission evaluation, credential replacement, and MFA lifecycle. It also flags the still-undefined ApartmentAccessGrants type catalogue and revocation lifecycle.

The metadata preflight is a pass, not a DDL approval. Gate A remains **NOT AUTHORIZED for execution**. Credential verification/re-enrollment, manual identity dispositions, financial/carry-over policy, external wallet-view dependency confirmation, cascade retention policy, service-side invariant implementation, and disposable restore/rollback rehearsal remain outstanding. No DDL/DML, EF migration, backfill, or cutover was performed.


## Admin access lifecycle finding — 2026-11-18

A source review of `UserSqlStore` and `AdminManagementService` found that admin upsert only adds BuildingAdmins links (it does not revoke omitted links), while `AdminManagementService.DeleteAsync` physically deletes BuildingAdmins, UserPermissions, and Users in three commands before writing the audit event. The method has no explicit transaction around these deletes. The target migration must replace ordinary hard deletion with disable/revoke plus retained provenance/history, make grant/revoke semantics explicit, and use atomic audit/outbox handling. See section 8 of `Identity-Resolution-and-Authorization-Review-2026-11-18.md`. No application source or database data was changed.


## Legacy permission-scope finding — 2026-11-18

The permission matrix stores a flat account-level permission list with no BuildingId/ApartmentId on each permission. The permission attribute checks for a matching claim but does not itself enforce resource scope; session revalidation separately checks that claimed building memberships remain assigned. Permission keys imply different scopes, but the stored rows do not prove their effective resource scope. Therefore legacy permission backfill must classify each permission key against its consuming controller/service and keep ambiguous scope mappings unresolved; never fan out every permission to all assigned buildings or promote it to a role-wide grant. See section 9 of `Identity-Resolution-and-Authorization-Review-2026-11-18.md`.


## Permission mutation and session-revocation review — 2026-11-18

Extended the identity/authorization review with a code-level inspection of cookie validation, permission mutation, account lifecycle, and audit ordering (section 10 of `Identity-Resolution-and-Authorization-Review-2026-11-18.md`).

Key findings:
- The current admin session invalidation version is process-local; a multi-instance deployment can retain a stale authorization cache on another instance for up to the two-minute cache lifetime.
- PIN changes do not currently invalidate admin sessions.
- SuperAdmin validation checks SQL account state but has no shared session-revocation version/emergency revoke mechanism.
- Account changes and audit writes are separate operations; audit failure can leave a committed change without its corresponding event.
- Building membership updates have no explicit grant/revoke/replace-all contract, and an empty BuildingIds list cannot revoke all links through the current upsert path.
- Permission claims are not a substitute for resource-aware authorization; the current attribute does not evaluate building/apartment scope.

Target requirements now include a persisted account security/session version, centralized mutation service, explicit grant/revoke/replace semantics, transactional audit outbox, and cross-instance revocation tests. No source code or database was changed; no build/runtime tests were executed.


Additional audit/authentication review confirmed that `AuditLogger.LogAsync` writes through a separate DbContext after the business mutation and catches/swallow exceptions after logging a warning. Callers therefore cannot know whether the audit event was durably persisted. The authentication cookie currently uses a seven-day sliding lifetime without a persisted account security-stamp/session-version claim in the inspected setup; impersonation backup state is held in a protected two-hour cookie and its audit uses the same best-effort logger. The transactional outbox and cross-instance session-revocation requirements are therefore mandatory target controls, including active impersonation revocation.


## Authentication and MFA lifecycle review — 2026-11-18

Added section 11 to `Identity-Resolution-and-Authorization-Review-2026-11-18.md` after reviewing the login, cookie, TOTP, recovery-code, and startup credential-migration paths.

Key findings:
- Admin/SuperAdmin MFA is opt-in today; privileged login is challenged only when the account already has MFA enabled. The approved target requires mandatory MFA before privileged access.
- MFA enable/disable does not currently revoke other sessions or create a durable security audit/outbox event.
- TOTP replay protection and recovery-code consumption use a process-local keyed lock; cross-instance atomicity is not established.
- Pending login/MFA setup uses Session backed by `AddDistributedMemoryCache`, which is not a shared multi-instance session store. Login rate limiting is also currently process-local.
- `Program.cs` automatically invokes legacy PIN migration at startup. Application startup can therefore mutate credential data; do not run the app against the restore just for inspection. Replace this with a separately approved, audited, transactional migration.

Approved target decisions: mandatory MFA for admin/SuperAdmin; atomic cross-instance factor/recovery-code consumption; shared pending-auth/session and login-throttle stores; persisted account security version; durable transactional security audit; and removal of credential migration from startup. This is documentation/design only. No source or database changes, migrations, or tests were performed.


## Consolidated identity/security implementation package — 2026-11-18

The unified design is now recorded as one coordinated implementation package in section 12 of `Identity-Resolution-and-Authorization-Review-2026-11-18.md`, with eight workstreams and seven release gates. This preserves the user's preference for one consolidated delivery rather than many separate feature phases while retaining internal review and safety gates.

The package covers identity/provenance, scoped authorization, ownership/occupancy/access lifecycle, credentials/MFA, cross-instance session and impersonation revocation, transactional audit/outbox, legacy and financial continuity, and UI/API integration. Gate A remains NOT AUTHORIZED; a separate explicit approval is still required after design freeze and fresh preflight evidence. No source code or database was changed and no tests/build/migration were run.


## Controller/action resource-scope review — 2026-11-18

Added section 13 to `Identity-Resolution-and-Authorization-Review-2026-11-18.md` with the controller/action/resource-scope matrix across building, resident, apartment, wallet, financial, poll, maintenance, settings, audit, invoice, notifications, camera, impersonation, and QR endpoints.

Important findings:
- `AdminPermissionAttribute` currently returns for every authenticated non-admin principal rather than denying it. `CamerasController` combines this filter with general `[Authorize]`; resident camera viewing appears intentional in controller comments, but it needs an explicit resident access policy and must not be mistaken for enforcement of the admin permission.
- `apartment.disable` exists in the permission catalogue, but apartment disable/enable actions inherit `residents.manage` and do not enforce the dedicated key.
- AuditLog explicitly pins resident queries to their own building/apartment and admin queries to their claimed building; preserve this resource boundary.
- Financial/apartment endpoints commonly resolve child resources inside the building loaded from the session claim; this must become a service-level invariant in the target evaluator.
- Current feature permissions are coarse for sensitive read/write and financial transitions. The target matrix must separate scope from UI categories and distinguish personal account data from apartment relationships.

Gate 0 now requires explicit decisions for resident camera access, apartment disable/enable, read-vs-write/financial action separation, resident maintenance visibility, and any legacy permission with no verified consumer. No source code, schema, database rows, or credentials were changed. No build or runtime tests were executed.


Authorization test review also found that the current reflection tests verify attribute presence and only a small set of role declarations; they do not behavior-test the custom permission filters' non-admin path, the apartment disable permission, or foreign-resource access on exports/mutations. Required additions are recorded in section 13. No tests were run.


## Service, import/export and background-job review — 2026-11-18

Section 14 of the identity/authorization review now records service-boundary findings for DVR/secret access, resident PIN and lifecycle commands, wallet aggregate scope, backup/restore, Excel import/export, scheduled backup, and Presence.

Highest-priority items:
- A public `BackupService.RestoreAsync` overload bypasses the one-use user-bound preview-session path; repository search found tests but no production HTTP caller. Restrict it to an internal/test seam.
- Backup creation can silently skip unknown collections and still set Success=true when individual collection reads failed; partial output must not be represented as a complete successful backup.
- Excel ConfirmImport creates the building, saves apartments, then assigns PINs in separate operations; failure can leave partial imported state.
- Scheduled backup uses a non-atomic ShouldRun-then-Run sequence with no distributed lease/idempotency key, so multiple instances can duplicate work.
- DVR and resident services rely on controllers to enforce resource ownership; sensitive service APIs should accept verified scope/actor context or be narrowed to internal-only operations.
- Backup deletion and scheduled execution span filesystem, SQL, schedule state, rotation and audit without one atomic boundary; require durable operation state and reconciliation.

No source code or database was changed. No build or tests were run. Gate A remains NOT AUTHORIZED.


Test-source review for section 14: backup tests currently codify unknown-collection skip as Success=true; scheduled-backup tests validate settings but not distributed execution; resident service tests do not cover actor/scope authorization; no ExcelImportServiceTests or DvrServiceTests were found at the inspected paths. Existing restore-session one-use/digest tests are valuable and must be preserved. Detailed additions are in section 14. No tests were run.


### Deletion and retention review — 2026-11-18

Section 15 of `Identity-Resolution-and-Authorization-Review-2026-11-18.md` now records the source findings for building/apartment/resident/admin/poll deletion, aggregate-sync deletion semantics, cascade dependencies, backup rotation, retention requirements, and required failure-injection tests. The current physical-delete/cascade paths are not acceptable as the target lifecycle for financially or legally meaningful history. This remains documentation-only; no code or database changes were made. Gate A remains **NOT AUTHORIZED**.


### Runtime and deployment readiness review — 2026-11-18

Section 16 of `Identity-Resolution-and-Authorization-Review-2026-11-18.md` records startup mutation, local Data Protection key-ring defaults, in-memory distributed cache/rate limiting, multi-instance scheduler behavior, fail-closed session policy, and deployment controls. Startup credential migration and instance-local state are material blockers to safe multi-instance rollout. The review changed documentation only; no source/configuration/database values were changed. Gate A remains **NOT AUTHORIZED**.


### Final cross-domain review disposition — 2026-11-18

Sections 15–17 complete the static review of deletion/retention, runtime/deployment readiness, cross-artifact consistency, unresolved product policies, test gaps, and implementation acceptance criteria. The discovery review is complete for the currently available repository and supplied restore evidence, but identity cutover is not implementation-ready. All unresolved decisions and required tests are listed in the main review document.

The latest read-only Gate A preflight reviewed for this work is `Database/Diagnostics/GateA-Preflight-Results/20261118-155025/`; its metadata pass does not authorize DDL. **Gate A remains NOT AUTHORIZED.** No source code, schema, rows, credentials, backups, or runtime state were changed, and no build/tests were executed.


## Unified implementation plan approved — 2026-11-18

The completed discovery and static reviews have been consolidated into one coordinated delivery program:

- `Database/Redesign/Unified-Implementation-Plan-2026-11-18.md`

The plan groups implementation into WP0–WP7: policy/source-of-truth freeze; additive identity foundation; centralized authorization and relationship lifecycle; credentials/MFA/session security; finance/voting/retention compatibility; audit/import/export/backup/background operations; runtime/deployment foundation; and controlled migration rehearsal/cutover.

The user approved this unified plan and continuation of the work. This is **plan approval only**. It does not authorize Gate A DDL, EF migration execution, DML/backfill, authentication cutover, destructive operations, or production changes. The next task is WP0 Decision Register and source-of-truth reconciliation. Gate A remains **NOT AUTHORIZED** until a separate explicit approval after G0/G1 evidence is complete.

No application source, schema, database rows, credentials, backups, or runtime state were changed while preparing this plan. No build or tests were executed.


### SQL views and log-center review — 2026-11-18

Section 18 of the main review records the repository-wide SQL view/EF mapping inventory, the limitations of the wallet movement projection, the required future reporting/read-model architecture, and the requested enterprise structured log center. The repository search found one declared SQL view (`vw_ApartmentWalletMovements`) and one EF keyless mapping. The live database's complete view inventory is still pending a dedicated read-only `sys.views` diagnostics export; prior restore evidence only confirmed the two named wallet views were absent.

The future log center is explicitly separate from the financial ledger and the append-only business/security audit trail. It should correlate structured application logs, traces, performance telemetry, background jobs, and system health without logging credentials, PINs, MFA secrets, tokens, or ballot choices.

No view SQL or application code was changed. No database inventory query, build, or tests were executed in this documentation pass. **Gate A remains NOT AUTHORIZED.**


### Live SQL view inventory completed — 2026-11-18

Reviewed `Database/Diagnostics/View-Inventory-Results/20261118-163035/` from the disposable `BuildingManagement_RestoreTest` LocalDB restore. The live `sys.views` inventory returned **zero views**; definitions, SQL dependency edges, view consumers, indexed-view indexes, synonyms, and matching Query Store view-usage results were empty. Query Store is READ_WRITE with AUTO capture. The table metadata export includes 3,150 AuditLog rows, 237 Apartments, and 237 Residents.

This confirms a deployment/source mismatch: the repository SQL artifact declares `vw_ApartmentWalletMovements` and EF maps a keyless projection, but neither wallet view exists in the restored database. The source definition must not be treated as proof of deployment. Query Store's empty matching result is not proof that there are no dynamic-SQL, SQL Agent, SSRS, or external application consumers; these are outside the runner's scope and need separate checks.

Next: reconcile the SQL artifact, EF mapping, migration/deployment process, and all report/export consumers before approving any view creation/removal. No schema/data changes, build, or tests were performed. **Gate A remains NOT AUTHORIZED.**


### EF migration-path mismatch confirmed

A source cross-check found the keyless `ApartmentWalletMovementsView` mapping in `AppDbContext` and model snapshot/designer metadata, but none of the four actual EF migration operation files creates the SQL view. The only checked-in SQL definition located is in `BuildingManagement-FullSchema.sql`. Thus EF metadata does not deploy the object, matching the live restore inventory where no views exist. Reconcile the standalone FullSchema deployment path and EF migrations; do not create or remove the view until runtime/report consumers are verified. No source or migration was changed.


### Reporting, wallet, and audit read-path review — 2026-11-18

Section 19 of the main review documents a source-level trace of financial reporting, wallet balance calculations, aggregate hydration, audit paging/export, and CSV safety. Key finding: report period net activity (`collected - expenses + revenues`) differs from the actual apartment wallet balance formula (carry-over, confirmed deposits, monthly fee, stored expense/revenue distributions, and adjustments). The `TotalsOf` tuple also exposes both `Balance` and `TotalWallet`, which are not equivalent. These metrics need distinct contracts and reconciliation tests before any SQL read model is approved.

Reports currently operate on a fully hydrated in-memory building aggregate. Audit UI paging is SQL-filtered and bounded to 200 rows/page with stable timestamp/id ordering; the non-paged audit query is capped at 50,000 but lacks a tie-breaker and can materialize a large export. Audit inserts use a separate DbContext and swallow failures after warning logs, so critical business mutations are not atomically coupled to audit persistence. CSV safety is used in the inspected report/audit exports, but all export endpoints still need inventory.

No code/schema/data changes, build, tests, or runtime profiling were performed. **Gate A remains NOT AUTHORIZED.**


### Backup and export endpoint review — 2026-11-18

Section 20 records the static review of SuperAdmin-protected backup, restore, import, JSON, XLSX, and ZIP-download actions. JSON building export serializes the loaded aggregate (the current mapper blanks Admin PIN and apartment PIN); the full JSON action exports summaries only; XLSX exports contact/apartment/category fields and deliberately leaves PIN columns blank. Backup ZIP download decrypts the archive before returning it and is the most sensitive export path.

The export controller actions inspected do not directly record dedicated export audit events; verify whether another layer captures them. Add durable audit, cache controls, secret-canary tests, XLSX formula tests, strict backup collection validation, and atomic staged import. Existing partial-import and unknown-collection concerns remain open. No source/schema/data changes or tests were performed. **Gate A remains NOT AUTHORIZED.**


### Test-coverage review for reporting and exports — 2026-11-18

Section 21 maps existing tests and gaps. Current tests already provide broad unit coverage for wallet formulas and report arithmetic, plus audit paging/filter and CSV cell-safety tests. Missing evidence includes report-vs-wallet metric contract assertions, SQL projection parity, audit controller scope/export tests, end-to-end export security for JSON/XLSX/ZIP, backup-controller workflows, and import failure injection. No test/build was executed in this documentation update. **Gate A remains NOT AUTHORIZED.**


### Invoice and QR document-delivery review — 2026-11-18

Section 22 extends the file-response inventory to invoice PDFs and QR PNG/PDF endpoints. Invoice download enforces resident apartment claims and admin building scope and records an audit event, though persistence is best-effort. Generic QR PNG correctly sets `private, no-store`; invoice PDFs and SuperAdmin building QR PDFs need consistent no-store headers and dedicated audit evidence. QR PDF generation is SuperAdmin-only but may encode access links, so treat it as sensitive. No source changes or tests were performed. **Gate A remains NOT AUTHORIZED.**


### Resident, voting, maintenance, presence, and camera review — 2026-11-18

Section 23 of `Identity-Resolution-and-Authorization-Review-2026-11-18.md` reviews resident account lifecycle, voting privacy, maintenance-to-expense linkage, notification recipient semantics, online presence, and camera/HLS access.

Critical findings:
- Resident deletion is physical, resident CRUD/PIN changes have no durable service-level audit calls, and the legacy apartment-to-resident migration method currently returns an empty result without migrating data.
- Current voting is identifiable by apartment, and the vote audit event records the selected option alongside the apartment number; it is not a secret ballot.
- Maintenance records can be edited/deleted after their costs have been posted as expenses, without a linked correction/reversal path.
- Notifications are apartment-scoped and globally capped at 200 per building; multiple residents in one apartment share the same inbox/read state.
- Presence is keyed by building+apartment when those claims exist, so it measures apartment activity rather than unique users.
- Resident camera access is building-wide under the current controller policy; HLS delivery must enforce authorization at the media server and be tested for revocation.

The section includes concrete acceptance tests. This was static review only: no source/configuration/schema/data changes, build, or tests. **Gate A remains NOT AUTHORIZED.**


### Quick QR and DVR/media-server lifecycle review — 2026-11-18

Section 24 extends the static review to Quick QR generation/consumption, one-time PIN activation, DVR credential handling, and MediaMTX coordination.

Positive controls include AES-GCM authenticated QR tokens, a GET confirmation page that does not consume tokens, atomic use-count consumption, encrypted DVR passwords, and private-IP/relative-RTSP-path validation. Gaps include inconsistent QR parameter bounds between generation endpoints, one-time set-PIN tokens being consumed before the PIN database update (so a DB failure burns the link), apartment-wide shared PIN reset semantics, secret-bearing DVR entities being returned from service read methods, DVR updates not visibly reconciling existing MediaMTX paths, and partial external cleanup risk during DVR deletion. The section defines retry/outbox, audit, DTO, and failure-injection acceptance criteria.

Static review only; no code, schema, data, tokens, or media-server state were changed. No build/tests were run. **Gate A remains NOT AUTHORIZED.**


### Financial command and ledger correction review — 2026-11-18

Section 25 reviews deposit confirmation/cancellation/editing, manual wallet adjustments, and expense/revenue posting/deletion. Existing positive controls include pending-only deposit transitions, edit history for pending payment changes, and recalculation after financial changes. Remaining issues include trusting posted month keys for deposit lookup, lack of explicit concurrency/idempotency guarantees in the aggregate command path, insufficient service-level validation for adjustments/categories/dates, deletion audit events that can be emitted even when no row was deleted, and physical deletion of financial entries instead of immutable reversals. Required acceptance tests are listed in the main review.

Static review only; no financial operations, code, schema, or data were changed. No build/tests were run. **Gate A remains NOT AUTHORIZED.**


### Settings secrets and impersonation review — 2026-11-18

Section 26 reviews global/building/personal settings and privileged impersonation.

Priority findings:
- Global settings GET returns the stored WhatsApp token and Cloudinary API key to the view, and the view renders them into HTML input values. Replace with write-only secret handling and encrypted-at-rest storage.
- Global/building security and policy settings rely on UI bounds without equivalent service-level validation.
- Personal preferences and resident profile/PIN changes are apartment-wide in the legacy model; multiple residents share settings and PIN updates.
- Impersonation entry actions enforce roles but do not visibly enforce the catalogued `secret.access` permission. Stack overflow silently refuses the backup push while the controller still proceeds with impersonation; this must fail closed.
- Impersonation uses a protected two-hour client-carried recovery cookie; revocation/permission changes during the window are not revalidated from an authoritative server-side session. Audit role attribution is also hardcoded as admin for resident entry/exit.
- Follow-up verification confirmed `UsersService.SetAsync` hashes admin PIN values through `SecretVerifier.HashIfPlain` before persistence. This is a positive control to preserve with tests.
- The self-settings route is available to any authenticated user without explicit enforcement of the catalogued `settings.manage.own` permission; define whether this is intentionally role-intrinsic.

The section includes acceptance tests for secret exposure, key rotation, settings validation, independent resident preferences, impersonation permission/stack/revocation, and durable audit. Static review only; no code, settings, credentials, sessions, schema, or data were changed. No build/tests were run. **Gate A remains NOT AUTHORIZED.**


### Startup PIN migration and SQL mirror review — 2026-11-18

Section 27 examines the startup PIN migration and SQL mirror persistence paths.

Critical finding: `MigrateLegacySecretsAsync` performs live credential/data changes at application startup, including hashing user/resident PINs, creating resident rows for apartments with legacy PINs, and clearing legacy Building/Apartment PIN columns. It has no dry-run, explicit approval, durable source mapping, or encompassing transaction, and can partially complete or race across application instances. It must be removed from automatic startup and replaced by a separately approved, checkpointed, auditable migration workflow.

Additional findings: SQL mirror health is process-local and stores raw provider error messages; all-building aggregate loading has a documented 16-query-per-building pattern; user permission rows are replaced while building-admin links are additive only, so omitted building links are not revoked; process-local keyed locks do not coordinate multiple instances.

Static review only. The migration was not invoked, and no database/application changes, build, or tests were performed. **Gate A remains NOT AUTHORIZED.**


**Startup wiring confirmed:** `Program.cs` calls `MigrateLegacySecretsAsync()` during application startup. Do not start the application against the restore database while preserving its current legacy PIN state is required. The method must be removed/feature-gated in a separately approved code change before any further restore-database application testing.


### Runtime orchestration, backups, application logs, health, and sync inventory — 2026-11-18

Section 28 reviews the hosted backup scheduler, backup retention/history, application-log explorer, health endpoints, rate limiting, shared runtime state, Data Protection, CSP, and the synchronization feature inventory.

Key findings:
- The scheduled backup hosted service runs independently in every app process and has no visible distributed lease. Concurrent instances can run the same due backup; minute-resolution filenames can overwrite while history rows duplicate.
- Monthly backup day 29/30/31 settings are skipped in months without that date. The configured hour is UTC but the UI contract does not visibly clarify timezone.
- Retention deletes scheduled files without reconciling BackupHistory; legacy unencrypted backups remain accepted by the compatibility decryptor.
- Application log search scans files for every query. The explorer independently takes only 14 files, which can omit retained rollover files; malformed date filters silently become no filter.
- Health endpoints are minimal but have no visible authorization metadata. Rate limiting is process-local and forwarded-IP behavior depends on explicitly trusted proxy configuration. Memory cache/session and Data Protection key-ring configuration need production multi-instance validation.
- CSP still allows inline scripts and HTTP sources for media/connect/frame. The permission catalog advertises sync controller/services that are absent from the current Controllers/Services inventories; confirm retirement or external ownership.

Required acceptance tests cover multi-instance scheduler locking, schedule edge dates/timezones, crash recovery, retention/history reconciliation, backup restore integrity, rolled log coverage and performance, redaction canaries, health/proxy/rate-limit behavior, shared Data Protection, CSP, and sync ownership.

Static source review only; no application, database, backup, synchronization, filesystem, credential, or configuration changes were made. No build/tests were run. **Gate A remains NOT AUTHORIZED.**


Additional Section 28 verification: the RunNow controller currently reports success after the service returns, but the service swallows internal failures and can return early without creating a backup. DeleteBackup can also remove history rows and report success when no file exists. Backup ZIP downloads return decrypted plaintext without an explicit no-store response policy. Require typed run outcomes, strict artifact existence semantics, and no-store headers with durable audit coverage.


### Consolidated remediation roadmap — review baseline 2026-11-18

This roadmap orders the findings from Sections 1–29 by risk and dependency. It is a proposed sequence, not an authorization to change the live database.

#### P0 — Stop unsafe startup and establish migration safety

1. Remove or feature-gate startup PIN/legacy-secret mutation; remove startup DDL for LoginThrottle. No application instance should mutate a restore/production database as a side effect of startup.
2. Freeze and document the authority-of-record for every legacy store and integration. Confirm the retired/external sync feature and reconcile the missing SQL wallet views between FullSchema and EF migrations.
3. Create a reproducible, disposable restore baseline with verified backup, schema/migration inventory, row counts, checksums, and a rollback rehearsal.
4. Do not execute Gate A or any backfill/cutover until source-to-target mapping, orphan/duplicate resolution, financial reconciliation, and explicit approval are complete.

#### P1 — Close identity, authorization, and credential risks

1. Approve the Person/UserAccount/credential model and account-linking rules; preserve one interactive account per Person and explicitly model multiple building/apartment relationships.
2. Fix direct/unified authentication parity, synthetic QR identity validation, account lockout semantics, distributed MFA consumption, mandatory MFA policy, and cross-instance permission/session revocation.
3. Enforce permission allowlists and service-layer validation; audit every sensitive resident/admin/account operation and replace hard deletes with controlled deactivation/retention where required.
4. Remove plaintext secret exposure from settings, use encrypted secret storage and write-only secret updates, rotate exposed keys, and verify logs/export/download redaction and no-store behavior.
5. Resolve resident PIN sharing, QR lifecycle failure handling, DVR/MediaMTX reconciliation, camera authorization, and secret-safe DTO boundaries.

#### P1 — Protect financial integrity and privacy

1. Introduce idempotent, concurrency-safe financial state transitions with immutable ledger/reversal records and explicit actor/reason metadata.
2. Reconcile historical wallet, deposits, expenses, revenues, carryovers, adjustments, and monthly shares before any migration; resolve identified cent variances and month-specific inconsistencies.
3. Define private-ballot storage and remove apartment/choice linkage from audit trails if secret voting is required.
4. Decide apartment-vs-building visibility for maintenance, notifications, presence, and camera access; encode scope in services and database queries rather than relying on UI filtering.

#### P2 — Reliability, performance, and operational controls

1. Add distributed leases/outbox/retry/reconciliation for scheduled backups and MediaMTX side effects; make backup results truthful and verify restore integrity.
2. Centralize or instance-label logs, bound log-query cost, correct rollover coverage/date validation, and add secret-canary tests.
3. Use a real shared cache/session/Data Protection key ring and trusted-proxy configuration before multi-instance deployment.
4. Remove unnecessary CSP unsafe-inline/HTTP sources after dependency inventory; protect health endpoints at the network boundary.
5. Replace N+1 and aggregate-heavy report/log/auth paths with bounded projections, keyset/paged queries, and measured query plans.

#### P2/P3 — Test, documentation, and rollout

1. Build a layered suite: service invariants, controller authorization/object-scope tests, SQL Server integration tests, concurrency tests, privacy/export tests, and restore/deployment smoke tests.
2. Require CI to run restore/build/test plus migration-diff and source-integrity checks; capture exact commit and environment for every result.
3. Use expand → dual-read/dual-write only where safe → backfill → reconcile → cutover → contract. Every stage needs a rollback point and measurable acceptance criteria.
4. Keep old financial and identity columns until reconciliation is signed off; no destructive drop in the same release as initial identity foundation.

#### Gate status

- Gate A: **NOT AUTHORIZED**.
- Database DDL/DML, EF migration execution, backfill, cutover, production deployment, and destructive cleanup: **NOT APPROVED**.
- Evidence reviewed to date includes static source review and the user-run restore-database diagnostic CSVs described in the earlier sections. The assistant has not run the application build, test suite, restore operation, or database migration in this review.


### Authentication and session validation — Section 29

Section 29 reviews direct/unified login parity, account selection, lockout, MFA, session revalidation, QR identity, and permission validation.

Key findings:
- Direct resident login and unified login use inconsistent active-account checks; inactive residents/admins can pass primary authentication and receive a cookie that session revalidation then rejects.
- Unified login trims credentials, which can alter legitimate passwords containing surrounding whitespace.
- Unified login performs N+1 apartment/building lookups for resident candidates; direct resident/admin selection is ambiguous when duplicate phone rows exist.
- LoginThrottle resets the account counter but not the IP counter; shared-IP users can remain locked out. Live counters are process-local; startup creates dbo.LoginThrottle with raw DDL.
- MFA is opt-in in the current login flow, and TOTP/recovery consumption is protected only by an in-process lock, not cross-instance atomicity.
- Stable QR login creates a synthetic resident UID while the validator requires a matching Residents.Uid; the resulting cookie appears to be rejected on the next validated request.
- Admin permission cache invalidation is process-local; arbitrary permission strings and unknown templates are not rejected at the controller boundary.

See Section 29 for source paths, detailed reasoning, and acceptance tests. Section 30 provides the ordered remediation roadmap and release gates.


### Sections 29–31 — Authentication, QR, MFA, receipt/OCR, import, and final roadmap

New verified findings:
- Stable QR confirmation issues a synthetic resident UID (`qr-{ApartmentId}`) that the resident session revalidator does not recognize; the next request rejects the session. The separate quick-login flow uses the real resident UID.
- Stable QR confirmation responses are outside the current no-store middleware path, and the anonymous confirmation POST lacks the login rate-limit attribute.
- MFA is opt-in today: privileged login challenges only accounts where 2FA is already enabled. The target requirement for mandatory admin/superadmin MFA is not met.
- Unified login trims the password/credential string, unlike the dedicated superadmin login path. Resident PIN creation/reset services do not consistently enforce the four-digit policy; legacy and unified login paths also use different active-account filters.
- Receipt upload returns a normal Cloudinary secure URL without an application-level private/signed delivery policy. Upload can leave orphaned remote objects if database persistence fails.
- OCR is synchronous, has no explicit endpoint rate policy or image-dimension ceiling, and is heuristic extraction only—not bank verification.
- Excel import parsing silently defaults blank PINs to 0000 and coerces malformed PINs; unsupported floor labels map to ground floor. Import/tools/sync backend mappings refer to controllers/services absent from the current branch inventory.

Important correction to Section 28: the checked-in Phase08 security assessment confirms a **single-instance deployment**. Process-local throttle/cache/locks are therefore current topology choices, not confirmed multi-instance defects. Scale-out requires a separate distributed design. Proxy IP behavior still depends on production KnownProxies configuration.

Section 31 consolidates the recommended dependency order:
- P0: stop unsafe startup PIN mutation, keep Gate A blocked, resolve SQL view deployment, preserve/reconcile financial history.
- P1: fix QR/MFA/PIN/receipt/OCR security, financial idempotency and durable audit, and backup outcomes/recovery.
- P2: implement person/account and ownership/occupancy lifecycle, secret ballots, privacy scopes, and authorization/catalog consistency.
- P3: performance, log operations, backup drills, CSP hardening, and removal or implementation of dormant features.

Static source review only. No code or database changes, migrations, external service actions, builds, or tests were performed. **Gate A remains NOT AUTHORIZED.**

## Live schema reconciliation

- [Live Schema Parity Assessment — 2026-11-18](Live-Schema-Parity-Assessment-2026-11-18.md)
- [EF Core / Live SQL Schema Reconciliation — 2026-11-18](EF-Core-Live-Schema-Reconciliation-2026-11-18.md)
- [Gate A Column and Constraint Parity Matrix](GateA-Column-and-Constraint-Parity-Matrix.md)

The EF reconciliation found that the 30 EF-mapped physical tables and 313 mapped columns align with the disposable restore catalog; the additional `LoginThrottle` table is provisioned by raw startup SQL, and the mapped wallet view is absent from the restore. See the detailed report for scope and remaining checks. Gate A remains NOT AUTHORIZED.
- [Financial and Voting Domain Review — 2026-11-18](Financial-and-Voting-Domain-Review-2026-11-18.md)

The domain review identifies incomplete historical carry-forward recalculation, non-deterministic equal-share rounding, mutable/deletable financial source records, and apartment-keyed voting with ballot choice exposed in audit details. It defines the target Finance and Voting boundaries; no application or database changes were made.
