# Full Database Backfill — Execution Gate

**Branch:** `development/database-redesign-enterprise`  
**Repository HEAD reviewed:** `1b84f5878b45ccba0df2f90f5847049ac066e70f`  
**Target rehearsal database:** `(localdb)\\MSSQLLocalDB / BuildingManagement_RestoreTest`  
**Scope:** all legacy application data domains, not finance-only.  
**Safety:** no production writes; no source-table deletion or mutation.

## 1. Actual state (verified from repository)

The restored inventory records 44 application tables. The current EF model/migrations contain:
- Initial application schema and key standardization.
- Phase06 identity/authorization foundation.
- Phase07 ownership-record provenance.
- Phase08 financial ledger foundation and integrity constraints.

The current `Data/` entity/configuration files are limited to `Entities.cs`, `IdentityEntities.cs`, `IdentityModelConfiguration.cs`, `FinanceEntities.cs`, and `FinanceModelConfiguration.cs`. There are no target EF entity/configuration modules or migrations yet for voting, maintenance, notification recipients/delivery, presence sessions, QR capabilities, media credential references, document/import/export operations, protected integration secrets, audit/outbox, or backup/restore operations.

Therefore a full data backfill cannot truthfully be executed yet: for those domains, the approved target tables do not exist. Inserting data into invented tables or treating legacy tables as migrated would be unsafe and would create a false completion claim.

## 2. Legacy inventory and disposition

| Legacy tables | Target / disposition | Backfill status |
|---|---|---|
| Buildings, Floors, Apartments, BuildingSettings, FinancialCategories | Retain as canonical shared property/reference tables; no duplicate copy | Reference data retained; parity validation required |
| Users, Residents, UserPermissions, BuildingAdmins | Persons, UserAccounts, credentials, role assignments, permission overrides, LegacyIdentityMap | Target schema exists; row mapping remains gated by identity evidence and permission model |
| Deposits, DepositEditHistory, Expenses, Revenues, MonthlyApartmentShares, WalletAdjustments | PaymentDeposits, ledger transactions/entries, source links, allocation evidence, reconciliation | Target schema exists; no data backfill executed |
| Polls, PollOptions, PollVotes | Retain legacy definitions/history; future voting policy, eligibility, delegation, ballots | Target schema absent; preserve apartment-attributed vote semantics |
| MaintenanceRecords | Requests, work orders, assignments, status events, attachments | Target schema absent |
| Notifications | Messages, recipients, delivery attempts, per-recipient read state | Target schema absent; shared apartment state cannot be guessed into person-level state |
| Presence | Account/device sessions and heartbeat projection | Target schema absent |
| QrTokens, QrUsages | Target-bound capabilities, atomic uses, revocation | Target schema absent |
| Dvrs, Cameras | Secret-free credential references, provisioning and access sessions | Target schema absent; encrypted credentials must not be copied blindly |
| GlobalSettings | Typed settings and protected integration secrets | Target schema absent; secret migration requires verified key compatibility |
| AuditLog | Append-only audit events and outbox | Target schema absent |
| BackupHistory, ScheduledBackupSettings | Backup jobs/artifacts, retention events, restore operations | Target schema absent |
| SyncPendingChanges | Durable outbox/inbox | Target schema absent |
| LoginThrottle | Reassess durable throttle requirement | No target migration planned yet |

## 3. Mandatory gates before data movement

1. **Identity:** classify every source identity candidate; do not merge by name/contact; do not promote resident relationship rows to accounts. Preserve unresolved candidates in a review queue. Resolve the direct-per-user permission semantics before permission import.
2. **Property relationships:** keep Buildings/Floors/Apartments keys stable. Do not infer legal ownership shares or occupancy start dates from `IsOwner`, `IsPrimary`, or display text. Import only supported evidence; unresolved relationships remain explicitly pending.
3. **Finance:** apply and verify the reviewed PaymentDeposit resolution constraint update before backfill. Add legacy actor-reference support before importing confirmed/cancelled deposits whose actors cannot be mapped to accounts. Preserve pending/cancelled states, source links, monthly-share amounts, carryover snapshots, and historical rounding differences. Never rewrite source amounts.
4. **Voting:** preserve historical PollVotes as apartment-attributed evidence; never call them anonymous ballots or link them to a person without source proof.
5. **Notifications/presence:** do not reinterpret apartment-shared read/presence state as person/account state.
6. **QR/media/secrets:** never export token material, API secrets, or decrypted camera passwords into migration reports. Move secrets only through verified cryptographic/key-management paths.
7. **Audit/backup/files:** preserve external artifact references and retention evidence; verify backup file digests before restore rehearsal.

## 4. Required execution architecture

The eventual runner must:
- require the exact disposable RestoreTest database identity and a verified backup checksum;
- run a read-only preflight first and stop on missing columns, orphan references, unknown enum/status values, unresolved mandatory mappings, or target schema gaps;
- use stable source keys/fingerprints and unique source links for idempotency;
- perform each domain batch transactionally and record inserted/already-present/rejected counts;
- never delete or update legacy source rows;
- produce privacy-safe aggregate reconciliation by source table, target entity, building, month, status, and financial category;
- verify source-to-target counts and domain totals after every batch;
- publish the report to this repository without committing unrelated working-tree changes.

## 5. Execution status

- Schema rehearsal: identity/authorization and finance foundations applied to RestoreTest (as reported by the local runner).
- Finance resolution constraint follow-up: SQL was generated and reviewed; its application is not confirmed by the latest runner output.
- Data backfill: **NOT STARTED**. No claim of transferred rows is made.
- Production/cutover: **NOT AUTHORIZED**.

## 6. Next implementation order

1. Complete identity/permission mapping contracts and safe identity candidate dispositions.
2. Implement and migrate operational target schemas (voting, maintenance, notifications, presence, QR, media, settings/secrets, audit/outbox, backup/restore, import/export/document metadata).
3. Add finance legacy actor-reference support and finalize finance preflight.
4. Build one coordinated, restartable full-database backfill orchestrator with per-domain adapters.
5. Run dry-run, then RestoreTest backfill, then full reconciliation and restore/rollback rehearsal.
6. Keep legacy tables authoritative until each domain passes parity and receives explicit cutover approval.

**Conclusion:** This is a whole-database migration program. The current repository is not yet in a state where all 44 legacy tables can be copied into new target tables. The missing target modules must be implemented first; pretending otherwise risks data loss or semantic corruption.
