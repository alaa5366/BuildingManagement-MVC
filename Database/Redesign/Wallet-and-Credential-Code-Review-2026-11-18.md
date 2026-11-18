# Wallet and Credential Code Review — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Scope:** repository source review only; no application execution and no database modification.

## 1. Wallet-view discrepancy: traced to schema artifact vs migration chain

Repository inspection establishes:

- `Data/AppDbContext.cs` maps `ApartmentWalletMovementsView` to `dbo.vw_ApartmentWalletMovements` and exposes a DbSet.
- `BuildingManagement-FullSchema.sql` contains a concrete `CREATE VIEW dbo.vw_ApartmentWalletMovements` definition.
- The checked-in EF migration `20261116120818_InitialCreate.cs` does **not** create this view. The later migrations do not supply a view-creation migration either.
- `Services/WalletService.cs` does not reference or query `ApartmentWalletMovementsView`; repository code search found the mapping and schema artifact, but no runtime consumer.
- No repository definition or mapping for `vw_ApartmentWalletBalance` was found. It should not be treated as a required existing object without new evidence.

The checked-in `vw_ApartmentWalletMovements` definition returns apartment number, confirmed deposits, all wallet adjustments, and their sum. It explicitly describes itself as movement totals only; it does **not** compute the complete wallet balance. It omits carry-over, monthly fee, expense distribution, revenue distribution, and month-specific filtering. The complete balance is calculated by `WalletService.ComputeWalletBalance`.

**Decision:** do not recreate either view from assumptions. The live absence of `vw_ApartmentWalletMovements` is explained by the absence of a view-creation migration and the fact that the EF mapping alone does not create a SQL view. Because no runtime consumer was found, this is currently a schema/model drift cleanup item rather than a demonstrated runtime dependency. Before deciding to retain the view, inspect the diagnostic output for SQL-module dependencies and confirm no external reporting/client consumes it. The undocumented `vw_ApartmentWalletBalance` expectation should be removed unless a concrete consumer/definition is discovered.

## 2. PIN implementation: source facts and risks

Repository source review establishes:

- `ResidentEntity.PinHash` is documented as BCrypt.
- `SecretVerifier.Hash` creates BCrypt hashes with work factor 10. `BcryptPasswordHasher.HashPin` also uses factor 10; general password hashing uses factor 12. The resident creation path also uses BCrypt.
- `SecretVerifier.Verify` verifies BCrypt values and rejects non-BCrypt values by default; plaintext comparison is controlled by the emergency `Auth:AllowLegacyPlainPins` setting.
- `SecretVerifier.IsBcrypt` only checks a `$2a$`, `$2b$`, or `$2y$` prefix. It does not prove that the complete hash is well-formed or verifies correctly.
- `PinMigrationService.MigrateLegacySecretsAsync` is invoked during application startup from `Program.cs`. It hashes non-empty non-prefixed Users PIN and Residents PinHash values, clears non-empty `Buildings.AdminPin`, and processes legacy `Apartments.Pin` values.
- For Residents, any non-empty value that does not start with a recognized BCrypt prefix is treated as the raw PIN and hashed. That is unsafe if a value is malformed or belongs to a different legacy encoding.
- Building AdminPin values are cleared without a per-building evidence record in this routine. Apartment PIN values are copied to a newly created resident or to residents with blank PinHash, then cleared; when residents already have non-empty PIN hashes, the routine clears the apartment PIN without proving that the resident credential is equivalent.

The aggregate profile previously found five Users with non-empty legacy PIN values and 79 Residents with non-empty PinHash values. It did not classify their formats. Therefore it does not establish whether all 79 resident values are valid BCrypt hashes or whether the five User PINs remain plaintext at the time of the new diagnostic.

**Important operational warning:** do not start the application against the disposable restore merely to inspect these records. Startup currently invokes a mutating credential migration. Run the new SQL-only diagnostic first. Do not enable `Auth:AllowLegacyPlainPins` as a workaround.

## 3. New read-only diagnostic

Files added:

- `Database/Diagnostics/Wallet-and-Credential-ReadOnly-Validation.sql`
- `Database/Diagnostics/Run-Wallet-and-Credential-ReadOnly-Validation.ps1`

Run from the repository root after pulling the branch:

```powershell
.\Database\Diagnostics\Run-Wallet-and-Credential-ReadOnly-Validation.ps1
```

The runner is restricted to `BuildingManagement_RestoreTest`, uses Windows Integrated Security, exports separate CSV result sets, and automatically commits/pushes only its timestamped result folder to `development/database-redesign-enterprise`. It refuses to stage results if the Git index already contains staged changes.

The SQL returns:
1. Database identity and exact requested wallet-view presence.
2. Definitions of wallet-named views that actually exist.
3. SQL-module dependency references to wallet-named objects.
4. Aggregate credential-format classes and lengths for Users PIN, Residents PinHash/PasswordHash, Apartments PIN, and Buildings AdminPin.
5. Aggregate totals/empty/non-empty counts.

No PIN, hash, name, phone, email, or other row-level identifying/secret value is exported. A `BCRYPT_SHAPE_60_UNVERIFIED` classification is only a format/length indicator; it is **not** cryptographic verification.

## 4. Next decision gate

After the diagnostic results are reviewed:

- Decide whether the unused `vw_ApartmentWalletMovements` mapping/entity should be removed in a future code cleanup or whether a view-creation migration is needed for a documented consumer.
- Do not invent `vw_ApartmentWalletBalance`.
- Validate BCrypt-shaped values using an offline/test verification harness that does not export the hashes or PINs; aggregate SQL metadata alone cannot cryptographically verify a hash.
- Define explicit handling for non-BCrypt legacy values. Do not blindly hash arbitrary stored strings.
- Redesign startup migration so it is not an unreviewed destructive data migration: use an explicit, audited, transactional, dry-run-capable process with row counts, per-record outcomes, rollback/backup, and no clearing of source columns until destination verification succeeds.

## 5. Safety boundary

This review used repository source only. The new SQL/PowerShell diagnostic has been committed but has **not** been run here. No SQL DDL/DML, EF migration, credential rewrite, startup migration, view creation, or application cutover has been performed.
