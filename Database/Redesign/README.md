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

The live legacy FK columns match the expected `nvarchar(128)` byte length and all use `SQL_Latin1_General_CP1_CI_AS`; Buildings and Apartments have the expected referenced primary keys; all twelve Gate A target tables are absent; and the database has the four expected EF migrations through AddUserTwoFactor.

The initial output did not report the database default collation. `GateA-Preflight-Validation.sql` has been extended with that check. Re-run it and add the new result file in this same Diagnostics folder. Gate A remains blocked pending that result and the ten aggregate profiling result sets. No DDL/DML was executed.


## Aggregate profiling update — 2026-11-18

The read-only aggregate profiling run has completed and its 15 result sets are committed under `Database/Diagnostics/Profiling-Results/20261118-145432/`. See `Database/Redesign/Aggregate-Profiling-Assessment-2026-11-18.md` for the findings and required reconciliation actions.

The profiling evidence is no longer a blocker. Gate A execution is still blocked pending the updated Gate A preflight (including database default collation), credential strategy, manual identity-link decisions, finance/carry-over reconciliation, wallet-view discrepancy resolution, and retention review of cascade relationships. No DDL, EF migration, backfill, or application cutover has been executed.
