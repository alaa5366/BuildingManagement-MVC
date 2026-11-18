# P0 Startup Safety Remediation — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Status:** Implemented in source; build/test verification pending.  
**Scope:** Remove credential/data mutation and raw schema creation from normal application startup.

## Changes committed

1. Removed the `PinMigrationService.MigrateLegacySecretsAsync()` invocation from `Program.cs`.
2. Removed the `PinMigrationService` dependency-injection registration and deleted the service implementation. The deleted implementation could hash user/resident PINs, create resident records from apartment PINs, and clear legacy Building/Apartment PIN columns.
3. Removed the `LoginThrottle.InitializeAsync()` startup invocation.
4. Replaced `LoginThrottle` SQL-backed startup behavior with bounded in-process fixed-window counters. The service no longer executes DDL/DML or depends on EF Core.
5. Preserved the internal hashed-key helper `LoginThrottle.K` used by the existing test suite.
6. Corrected `docs/articles/security.md` so it no longer claims that credentials are rewritten at startup or throttle counters survive restarts.

## Runtime behavior and trade-offs

- Application startup no longer performs legacy credential hashing, creates resident rows from apartment PINs, clears legacy PIN columns, creates `dbo.LoginThrottle`, or deletes expired throttle rows.
- Existing legacy plaintext credentials are not silently transformed. They require a separately designed, checkpointed, auditable migration and/or controlled user re-enrollment.
- Login throttle limits remain 5 failures per account and 20 per source IP over a 15-minute fixed window.
- **Known temporary limitation:** throttle counters are process-local and reset when the application restarts. Durable persistence must return only through an EF-managed schema/migration and tested repository implementation. The current deployment baseline is single-instance; do not claim cross-instance protection.

## Verification

- GitHub source re-read confirms `Program.cs` contains no call to `MigrateLegacySecretsAsync`, no `PinMigrationService` registration, and no `LoginThrottle.InitializeAsync` call.
- GitHub source re-read confirms `LoginThrottle.cs` contains no EF Core dependency, raw SQL, DDL, or DML.
- Existing `SecurityHardeningTests.cs` exercises account threshold, concurrent increments, account reset, IP threshold, and the internal `K` helper. The helper was preserved.
- No local build or test command was run in this session. No database was connected to, changed, migrated, or inspected by these commits.
- No production deployment or credential/data migration was performed.

## Commits

- `73adc53` — remove credential and raw SQL mutations from startup
- `253f73f` — make login throttling read/write-free at startup
- `a029d8d` — remove legacy startup migration service registration
- `f6ad1b2` — preserve tested throttle key contract
- `93738cb` — correct security documentation
- `d1204ac` — remove unsafe startup PIN migration implementation

## Next implementation work

1. Add EF-managed persistence for login throttling or explicitly adopt a centralized rate-limit store before any scale-out.
2. Design a separate legacy credential reconciliation tool with dry-run, source snapshots, stable mapping, checkpointing, transaction boundaries, audit, and rollback.
3. Continue account-centric Person/UserAccount, role/grant, session revocation, and mandatory administrator MFA implementation.
