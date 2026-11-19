# Runtime Legacy Data-Access Audit — 2026-11-19

**Repository:** `alaa5366/BuildingManagement-MVC`  
**Branch:** `fix/build-errors-2026-11-19`  
**Inspected HEAD:** `6997f063e913d8761522bfb4b8f67058403aca6b`  
**Mode:** Static source inspection only. No database connection, DDL/DML, backfill, build, or test execution was performed.

## Executive finding

The application has a mixed data-access architecture. Target-schema tables and services exist, but several services named `Target*` still query or write legacy EF entities. The Buildings details path still uses the old whole-building aggregate mapper, which directly queries multiple legacy tables. Therefore, removing one missing legacy table query would only expose the next dependency and would not complete the cutover.

Do not drop or rename legacy tables as a workaround. Several remain source evidence or canonical reference data, and some target domains are only partially modeled in EF.

## Confirmed runtime dependencies

| Runtime path | Current implementation | Legacy dependency / gap |
|---|---|---|
| `BuildingsController.Details` | `BuildingsService.GetByIdAsync` → `BuildingSqlStore.GetByIdAsync` → `BuildingSqlMapper.LoadAsync` | Reads `Expenses`, `Revenues`, `MonthlyApartmentShares`, `Deposits`, `DepositEditHistory`, `WalletAdjustments`, `Notifications`, `Polls`, `PollOptions`, `PollVotes`, and `MaintenanceRecords`, in addition to the still-canonical property/reference tables. |
| Building aggregate writes | `BuildingSqlStore.SaveFullAsync` → `BuildingSqlMapper.SaveAsync` | Synchronizes legacy child tables and removes rows absent from the in-memory aggregate. This whole-aggregate delete-by-absence behavior must not be carried into immutable ledger, audit, ballot, or target operational records. |
| Expense/revenue screens | `ExpensesController`, `RevenuesController` → `TargetFinanceReadService` | The read service still reads `db.Expenses` and `db.Revenues`. |
| Expense/revenue writes | Controllers → `TargetExpenseRevenueWorkflowService` | The service posts ledger transactions/entries and allocation evidence, but also reads/writes `db.Expenses` and `db.Revenues` as source-detail rows. A missing legacy table breaks idempotency and detail retrieval. |
| Invoice category breakdown | `InvoiceService.BuildInvoiceDataAsync` | Reads `db.Expenses` to recover category IDs for ledger source keys. Category detail is not yet fully represented in the target ledger model. |
| Maintenance | `TargetMaintenanceService` | Uses `db.MaintenanceRecords`; it does not use the Phase09 target tables `MaintenanceRequests`, `MaintenanceAssignments`, `MaintenanceComments`, `MaintenanceAttachments`, `MaintenanceStatusEvents`, and `MaintenanceLedgerLinks`. |
| Notifications | `TargetNotificationsService` | Uses `db.Notifications`; it does not use `NotificationMessages`, `NotificationRecipients`, `NotificationDeliveries`, `NotificationReadStates`, or `NotificationPreferences`. |
| Polls | `TargetPollsService` | Uses `db.Polls` and `db.PollVotes`; it does not implement the target policy/delegation/eligibility/secret-ballot lifecycle represented by `PollVotingPolicies`, `PollDelegations`, `PollEligibility`, `PollBallots`, and `PollLifecycleEvents`. |
| Presence and business audit | `PresenceService`, aggregate audit mapper | Already use `PresenceSessions` / `PresenceHeartbeats` and `AuditEvents` respectively. These are examples of target-backed paths. |
| Admin identity mirror | `UserSqlStore` | Still uses legacy `Users`, `UserPermissions`, and `BuildingAdmins`; target identity services and mappings coexist and need a controlled per-use-case cutover. |
| Resident management | `ResidentsService`, `ResidentSqlStore` | Still uses legacy `Residents`; target Person/Account/Occupancy/Access entities exist but do not yet replace every resident workflow. |

## EF model gap

`Data/AppDbContext.cs` exposes legacy DbSets such as `Expenses`, `Revenues`, `MonthlyApartmentShares`, `Deposits`, `Notifications`, `Polls`, `PollVotes`, and `MaintenanceRecords`.

`Data/OperationalEntities.cs` and `OperationalModelConfiguration.cs` currently map only a limited subset of operational target entities (notably presence sessions/heartbeats and audit events, plus saved WhatsApp templates). The Phase09 migration creates additional target operational tables, but those tables are not all represented by target EF entities/configurations/services. Consequently, the presence of target tables in SQL does not mean the runtime has cut over to them.

## Why the Buildings Details error is not isolated

The first failing query in `BuildingSqlMapper.LoadAsync` is `db.Expenses` (line 133 at the inspected HEAD). Even if that query were skipped, the same method immediately queries other legacy finance and operational tables. The mapper is also used by the full-building read/write compatibility layer, so fixing only the controller or hiding the SQL exception would leave other endpoints and writes exposed.

The current code and the reported runtime error may also be running against different database connection targets. Verify the application's actual `DB_NAME()` before concluding that the table is absent from the specifically approved RestoreTest database.

## Required remediation order

1. **Freeze unsafe aggregate writes:** stop using full-aggregate synchronization for target-owned finance, audit, ballot, notification, maintenance, and identity data. Preserve existing source records.
2. **Complete target EF mappings:** add reviewed entities/configurations/DbSets for the already-defined target tables. Do not invent a new table where a target table already exists.
3. **Finance source detail:** represent category, note/description, effective date, and source identity in a target-owned detail record linked to the immutable ledger transaction. Then update expense/revenue reads, idempotency checks, invoice breakdowns, and reports to use target records only.
4. **Operational workflows:** implement target-backed stores/services for maintenance, notifications, and voting. Preserve legacy poll definitions/history where the target design intentionally retains them; use target ballots/policies for new voting behavior.
5. **Building read model:** replace `BuildingSqlMapper.LoadAsync` for Details/QR and other read-only screens with focused query services/view models assembled from canonical property tables and target domain stores. Avoid loading unrelated domains to render a building profile.
6. **Identity cutover:** migrate each admin/resident use case from `Users`/`Residents` mirrors to Person/Account/RoleAssignment/PermissionOverride/Occupancy/Access data only after mapping and reconciliation gates pass.
7. **Views and exports:** trace every view, report, PDF, CSV, and resident/admin page to its controller/service and verify it no longer requires legacy domain tables after its cutover.
8. **Regression gate:** add integration tests against a target-only schema fixture (legacy domain tables absent) for Buildings Details, finance, wallet, invoice, reports, maintenance, notifications, polls, QR, identity, and resident flows. Add tests proving no target-owned ledger/audit/ballot rows are removed by aggregate saves.
9. **Database gate:** run additive migrations/backfill only on the verified disposable RestoreTest after backup/preflight; reconcile row counts, source links, financial totals, and domain-specific invariants before any production cutover.

## Safety boundary

- Do not drop, rename, truncate, or rewrite legacy source tables as a quick fix.
- Do not silently return empty data when a target table or mapping is missing.
- Do not treat the existence of a target SQL table as proof that the application uses it.
- Do not infer account/person identity from name, phone, or email alone.
- Do not change financial amounts, allocation outcomes, posted ledger entries, or historical voting semantics during refactoring.
- No local database was accessed or modified by this audit.
