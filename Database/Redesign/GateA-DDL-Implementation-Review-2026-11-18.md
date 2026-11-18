# Gate A DDL implementation review — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Scope:** Static review of `GateA-Additive-Identity-Foundation.sql` against the approved DEC register and the inspected EF Core baseline.  
**Execution status:** Reviewed and amended in source control. No SQL was executed; no database was modified; no EF migration was generated or applied in this review.

## 1. Baseline verified from repository

The migration source files establish the expected four-migration sequence:

1. `20261116120818_InitialCreate`
2. `20261117012657_Phase04_StandardizeKeyColumnTypes`
3. `20261117050000_Phase05_RemoveRedundantResidentIndexes`
4. `20261117130000_AddUserTwoFactor`

The Gate A SQL preflight now requires all four IDs, the legacy `Users`, `Residents`, `Buildings`, and `Apartments` tables, and the exact disposable database name `BuildingManagement_RestoreTest`. It also stops if any of the 13 target tables already exists. A partial or previously-created target schema must be investigated, not silently skipped.

## 2. Amendment made

`UserCredentials.ReplacedByCredentialId` previously referenced only `CredentialId`. That allowed a credential row belonging to one account to point to a replacement credential belonging to a different account.

The DDL now adds a unique candidate key on `(CredentialId, AccountId)` and makes the replacement self-reference composite on `(ReplacedByCredentialId, AccountId)`. This enforces same-account credential replacement at the database boundary.

## 3. Gate A safety properties

- Additive target tables only; no legacy table or column is dropped, renamed, or rewritten.
- No identity, ownership, occupancy, role, or permission backfill is performed.
- Person/account cardinality is enforced by the unique `UserAccounts.PersonId` constraint.
- External provider and subject are nullable as a pair and uniquely indexed when present.
- Account credentials and 2FA secrets are separated from legacy account rows in the target design.
- Ownership and occupancy retain effective dates and provenance fields.
- Apartment access grants and role/permission assignments retain scope, validity, grantor, and revocation information.
- Legacy identity mappings preserve unresolved rows; no email/phone-based automatic person merge is performed.
- The DDL transaction uses `XACT_ABORT ON` and an explicit transaction.

## 4. Important invariants not expressible by the current DDL alone

These remain service/domain transaction invariants and require dedicated tests:

1. Ownership shares for an apartment and effective period must not exceed 100%; overlapping periods and historical corrections need deterministic validation.
2. Primary occupancy must not overlap in time. The filtered unique index protects only one open-ended primary row; it does not prevent all date-range overlaps.
3. Role and permission grants must be evaluated against UTC effective dates, revocation, scope, deny precedence, and fresh MFA for sensitive actions.
4. A person must not be merged based solely on shared phone, email, or apartment membership.
5. A credential replacement chain must remain acyclic and preserve account ownership; the composite FK now enforces ownership, while cycle prevention belongs in the credential rotation transaction.
6. `PermissionSetKey` and `GrantType` need a versioned application registry before grants are issued.
7. The `SecretCiphertext` column requires a configured, versioned encryption provider and key rotation/recovery plan; schema storage alone does not provide encryption.

## 5. EF Core implementation gate

The application currently has a legacy `AppDbContext` model and an existing model snapshot. The Gate A SQL file is not yet an EF Core migration. Before database execution through the application deployment path, implementation must:

- Add strongly typed entities and DbSets for the 13 target tables.
- Configure exact SQL types, lengths, defaults, composite keys/FKs, filtered indexes, check constraints, and `rowversion` semantics.
- Generate the migration and updated `AppDbContextModelSnapshot` using the repository's actual .NET SDK and EF Core toolchain.
- Compare generated migration SQL with the reviewed Gate A DDL; avoid maintaining two divergent schema authorities.
- Add migration tests for wrong database name, missing baseline, partial target-table set, FK enforcement, uniqueness, checks, rollback, and idempotent deployment behavior.
- Execute only against a fresh disposable restore after a verified recoverable backup and preflight inventory.

Do not apply the standalone SQL file and then apply an equivalent EF migration: that would create schema-history drift. The EF migration should become the sole application deployment path.

## 6. Next implementation sequence

1. Implement the identity entities and model configuration in EF Core.
2. Generate and review the migration plus model snapshot.
3. Add migration-level and domain-level tests.
4. Rehearse on a fresh `BuildingManagement_RestoreTest` restore and capture pre/post metadata and integrity evidence.
5. Only after schema rehearsal passes, design evidence-backed legacy identity mapping; ambiguous rows remain unresolved.

## 7. Execution evidence

- Repository source files inspected: `Data/AppDbContext.cs`, `Data/Entities.cs`, the current model snapshot, the four migration source files, and Gate A DDL.
- Static source review only.
- No `dotnet build`, `dotnet test`, `dotnet ef`, SQL execution, backup, or restore was performed in this turn.
- No production database or deployment target was accessed.