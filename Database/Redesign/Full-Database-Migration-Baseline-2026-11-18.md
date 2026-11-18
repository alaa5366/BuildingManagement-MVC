# Full Database Migration Baseline — 2026-11-18

**Target:** `BuildingManagement_RestoreTest` at `(localdb)\MSSQLLocalDB`  
**Migration scope:** all legacy application domains, not only identity  
**Execution boundary:** disposable restore only; legacy rows remain authoritative until each target domain reconciles and a separate cutover is approved.

## Current schema state

The database has 44 application tables plus `__EFMigrationsHistory`. Six EF migrations are present, through Phase07. The target identity foundation is the only target-domain schema currently installed. Finance, voting, maintenance, notifications, presence, QR, media, settings, audit, and backup data still use their legacy tables.

The following counts were captured from metadata/count-only SQL after the resident login smoke test. No row contents, names, identifiers, contact details, or secret values were returned. The target identity counts include one synthetic login account used for testing; it is not linked to legacy data.

| Domain / table | Rows | Domain / table | Rows |
|---|---:|---|---:|
| Buildings | 3 | Floors | 16 |
| Apartments | 79 | BuildingSettings | 0 |
| FinancialCategories | 38 | Users | 5 |
| Residents | 79 | UserPermissions | 81 |
| BuildingAdmins | 2 | LegacyIdentityMap | 84 |
| Persons | 1 | UserAccounts | 1 |
| UserCredentials | 1 | RoleDefinitions | 1 |
| RolePermissions | 0 | UserRoleAssignments | 1 |
| AccountPermissionOverrides | 0 | UserTwoFactorSettings | 0 |
| UserRecoveryCodes | 0 | ApartmentOwnerships | 0 |
| ApartmentOccupancies | 0 | ApartmentAccessGrants | 0 |
| Deposits | 26 | DepositEditHistory | 1 |
| Expenses | 12 | Revenues | 4 |
| MonthlyApartmentShares | 113 | WalletAdjustments | 2 |
| Polls | 1 | PollOptions | 2 |
| PollVotes | 1 | MaintenanceRecords | 1 |
| Notifications | 68 | Presence | 10 |
| Dvrs | 1 | Cameras | 3 |
| QrTokens | 9 | QrUsages | 5 |
| AuditLog | 1,053 | BackupHistory | 2 |
| ScheduledBackupSettings | 1 | GlobalSettings | 1 |
| SyncPendingChanges | 0 | LoginThrottle | 0 |

The SQL catalog reports 63 foreign keys: 22 cascade, 40 no-action, and one SET_NULL action. Every relationship must be reviewed by retention domain before retirement or delete-policy changes.

## Legacy source-to-target disposition

“Retain” means preserve the current table and application behavior while the target is built and reconciled. It does not mean the legacy source has been copied or accepted as the eventual target.

| Legacy source tables | Planned target / transition | Current disposition |
|---|---|---|
| `Buildings`, `Floors`, `Apartments`, `BuildingSettings`, `FinancialCategories` | Keep stable property keys as shared reference data; later replace aggregate writes only after domain cutover. | Retain; no duplicate target property schema. |
| `Users`, `Residents`, `UserPermissions`, `BuildingAdmins` | `Persons`, `UserAccounts`, `UserCredentials`, `UserRoleAssignments`, `AccountPermissionOverrides`, `LegacyIdentityMap`; preserve exact source keys. | Identity foundation exists; 84 source candidates remain Pending. Synthetic login proof only. |
| `Deposits`, `DepositEditHistory`, `Expenses`, `Revenues`, `MonthlyApartmentShares`, `WalletAdjustments` | Financial periods, source links, immutable ledger transactions/entries, allocation batches/lines, wallet projection, and reconciliation evidence. | Legacy authoritative; target finance schema/backfill not implemented. |
| `Polls`, `PollOptions`, `PollVotes` | Keep existing poll definitions and historical votes; add versioned voting policy, eligibility/delegation, secret ballots, and lifecycle events for new polls. | Legacy authoritative; never relabel the old apartment-attributed vote as anonymous. |
| `MaintenanceRecords` | Requests/work orders, assignments, status events, attachments, and explicit financial-source links. | Legacy authoritative; target operations schema absent. |
| `Notifications` | Message definitions, individual recipients, delivery attempts, and per-recipient read state. | Legacy apartment-shared state retained; target recipient semantics/schema absent. |
| `Presence` | Account/session/device presence with separate person/apartment projections. | Legacy apartment projection retained; target session schema absent. |
| `QrTokens`, `QrUsages` | Short-lived target-bound capabilities, atomic uses, revocation, and guest/access sessions. | Legacy tokens retained during compatibility; target capability schema absent. |
| `Dvrs`, `Cameras` | Secret-free media credential references, provisioning operations, and per-request media authorization. | Legacy configuration retained; target media operations schema absent. |
| `GlobalSettings` | Non-secret settings remain typed and scoped; provider secrets move to protected, versioned storage. | Legacy settings retained; target settings/secrets schema absent. |
| `AuditLog` | Durable append-only security/business audit events with redaction and retention controls. | Legacy audit retained; target audit/outbox schema absent. |
| `BackupHistory`, `ScheduledBackupSettings` | Backup jobs, artifacts, retention events, and restore operations with typed outcomes. | Legacy backup history/settings retained; target operations schema absent. |
| `SyncPendingChanges` | Durable outbox/inbox with idempotency, bounded retries, and safe payload policy. | Empty legacy table retained; target outbox/inbox schema absent. |
| `LoginThrottle` | Persistent authentication-attempt state only if an approved durable throttle implementation is selected. | Empty legacy table; startup DDL was removed; no target use currently. |
| `__EFMigrationsHistory` | Continue as authoritative EF deployment record. | Retain; six migrations recorded. |

## Identity profile relevant to backfill

- All 5 `Users` and 79 `Residents` keys are nonblank; there are no exact UID matches between the two tables and no repeated Resident UIDs in this restore.
- There are shared-phone groups in both source tables (1 Users group and 4 Residents groups). Shared contact details are not identity proof. No email duplicate groups were observed.
- User roles are 3 admin and 2 superadmin. One admin has no `BuildingAdmins` relationship. All five legacy users have 2FA disabled and no legacy MFA/recovery material present.
- Resident state is 77 active and 2 disabled. All 79 rows have both `IsOwner` and `IsPrimary` set. These booleans do not supply legal ownership shares or dated occupancy evidence.
- All legacy User PIN and Resident PIN-hash values have 60-character `$2a$` BCrypt-shaped form. Shape is not proof of a valid hash or credential-purpose classification. No credential values were exported.
- The 84 identity-map candidates remain Pending: 5 Users and 79 Residents. No legacy identity rows were mapped to the synthetic account.

## Whole-database delivery sequence

1. **Source freeze and parity:** reconcile every physical table, FK, index, check/default, view, trigger, migration, report, export, import, and external consumer against EF and runtime code. Capture a verified restore point before every data-bearing rehearsal.
2. **Target contracts:** freeze one coordinated target ERD/data dictionary and domain rules for identity/authorization, finance, voting, maintenance/notifications/presence, QR/media, settings/secrets, audit, backup, and retention.
3. **Additive target schema:** add target entities and migrations without dropping, renaming, or repurposing source tables. Existing source identifiers remain available for provenance.
4. **Restartable backfill:** dry-run first, then copy in bounded idempotent batches with source-to-target keys, row outcomes, safe audit events, and domain-specific reconciliation. No guessed identity merges, ownership shares, occupancy dates, secret-ballot attribution, financial corrections, or permission widening.
5. **Compatibility and cutover:** run old and new paths in compare mode, prove reconciliation for each domain, rehearse restore/rollback, and obtain separate target-specific approval before production or destructive retirement.

## Current blockers and constraints

- Finance target tables and wallet projection are not implemented. Wallet parity, cancelled-deposit behavior, historical shares/carryovers, adjustments, and rounding differences need deterministic fixtures before any ledger backfill.
- Voting target policy/eligibility/ballot tables are not implemented. The legacy vote is apartment-attributed and its selected option must remain labeled as legacy evidence; it cannot be called anonymous.
- Maintenance, notification recipient/read-state, presence-session, QR capability, media provisioning, private document/import/export, audit/outbox, and backup-operation targets are not implemented.
- Identity merges and source dispositions require reviewed evidence. User role/permission mapping, credential purpose, admin MFA, ownership evidence, and occupancy effective dates are unresolved at the source-row level.
- The prior resident login smoke test proves the new resident account path can authenticate. It does not prove that any legacy identity or domain data has been transferred.

No legacy business rows were changed by the inventory. The only new identity data is the isolated synthetic login account documented in the Phase06/Phase07 recheck.
