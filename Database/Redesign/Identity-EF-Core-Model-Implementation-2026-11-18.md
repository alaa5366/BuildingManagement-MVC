# Identity EF Core model implementation — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Scope:** Gate A identity and authorization target model, mapped into the existing `AppDbContext`.  
**Database execution:** None. No migration was generated or applied in this environment.

## Implemented in source control

- `Data/IdentityEntities.cs`: 13 typed entities for Persons, UserAccounts, UserCredentials, UserTwoFactorSettings, UserRecoveryCodes, RoleDefinitions, RolePermissions, UserRoleAssignments, ApartmentOwnerships, ApartmentOccupancies, ApartmentAccessGrants, AccountPermissionOverrides, and LegacyIdentityMap.
- `Data/IdentityModelConfiguration.cs`: explicit SQL Server table/column mappings, lengths, datetime2(3), rowversion, keys/alternate keys, filtered indexes, check constraints, and restrictive foreign keys.
- `Data/AppDbContext.cs`: DbSets registered and the identity model configuration invoked from OnModelCreating.
- `Database/Redesign/Generate-GateA-EF-Migration.ps1`: guarded local workflow to generate the EF migration from the actual runtime model, build, run tests, then optionally stage and push only the model/migration files.

## Important design details

- One account per person is enforced by the unique alternate key on `UserAccounts.PersonId`.
- Credential replacement uses the composite principal key `(CredentialId, AccountId)`, preventing cross-account replacement references.
- Person identity is separate from account credentials, MFA, apartment ownership, occupancy, and access grants.
- Role assignments and account overrides retain explicit scope, effective dates, grantor/revocation attribution, and rowversion concurrency.
- Legacy source mappings retain unresolved states; there is no email/phone-driven identity merge.
- Ownership share totals and arbitrary date-range overlap checks remain domain transaction invariants; the DDL's row-level checks do not claim to enforce aggregate temporal rules.

## Migration generation procedure

After pulling the branch on the developer workstation:

```powershell
cd D:\Projects\BuildingManagement-MVC
git switch development/database-redesign-enterprise
git pull origin development/database-redesign-enterprise
.\Database\Redesign\Generate-GateA-EF-Migration.ps1
```

The script:
1. Requires the expected branch and a clean working tree.
2. Generates `Phase06_IdentityAuthorizationFoundation` with EF Core 8.0.10 against the actual `AppDbContext`.
3. Runs application build and the existing test project.
4. Requires the explicit `COMMIT-REVIEWED-MIGRATION` phrase before staging/committing/pushing the identity model and Migrations folder.
5. Does not connect to, create, or modify any database.

Review the generated migration and snapshot against `GateA-Additive-Identity-Foundation.sql` before committing. If the migration differs, correct the model or mapping; do not apply both SQL and EF migration paths.

## Current status and limitations

- Entity and model-configuration source files have been committed to the branch.
- EF migration and updated model snapshot are **not yet generated**; the local script is the next execution step because it must use the repository's actual SDK/toolchain.
- No build or tests were run by the remote repository edits.
- No database connection, backup, DDL, data backfill, or production operation occurred.
