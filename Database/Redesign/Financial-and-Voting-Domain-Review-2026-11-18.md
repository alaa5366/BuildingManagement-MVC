# Financial and Voting Domain Review — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Review type:** source-level static review of the current implementation and persistence mapper.  
**Database impact:** none. No DDL, DML, migration, build, or runtime test was performed.

## 1. Executive summary

The current implementation is still an aggregate-first compatibility architecture:

- Controllers load a complete `Building` object.
- Financial and voting services mutate in-memory `Building` / `MonthData` collections.
- `BuildingSqlMapper.SaveAsync` translates the aggregate into normalized SQL tables using collection synchronization.
- SQL rows are rehydrated into the aggregate on the next request.

This is a deliberate transition bridge, but it is not the target domain architecture. Financial posting, payment state transitions, allocation, carry-forward, audit, and voting are not yet represented as independently governed domain operations.

The most important findings are:

1. Changes to a historical month's expenses, revenues, or manual adjustments do not consistently recalculate carry-forward balances in later months.
2. Expense/revenue allocation is equal-per-open-apartment and assigns all rounding remainder to whichever apartment happens to be last in collection order. It does not implement the approved Largest Remainder rule and does not consume ownership-share data.
3. Expenses and revenues are physically deleted; there is no reversal/void history in the reviewed service/controller path.
4. Monthly fees are calculated from the apartment's current `MonthlyFee`, not an effective-dated fee record or immutable monthly charge.
5. Voting is one vote per apartment, stores the selected option against the apartment, and writes the selected option text plus apartment number into AuditLog. This is not secret-ballot behavior.
6. Poll eligibility is not modeled per poll; active status is based on Closed and a parsed deadline.
7. The aggregate synchronization mapper deletes SQL rows absent from the incoming aggregate collection. Its rowversion guard protects aggregate saves against stale Building snapshots, but the delete-on-absence behavior makes it essential that every aggregate load be complete and that no domain write bypass the aggregate concurrency protocol.

## 2. Financial domain — current behavior and risks

### 2.1 Aggregate and persistence path

Evidence:
- `Services/WalletService.cs`
- `Services/ExpensesService.cs`
- `Services/RevenuesService.cs`
- `Controllers/WalletController.cs`
- `Controllers/ExpensesController.cs`
- `Controllers/RevenuesController.cs`
- `Data/BuildingSqlMapper.cs`
- `Data/BuildingSqlStore.cs`

`BuildingSqlMapper.LoadAsync` loads Expenses, Revenues, MonthlyApartmentShares, Deposits, DepositEditHistory, and WalletAdjustments, then reconstructs `Building.Months`. Save performs a full aggregate synchronization inside a SQL transaction. The generic `Sync` helper upserts incoming rows and physically removes existing rows whose keys are absent from the incoming collection.

The Building rowversion is checked before the aggregate write and a no-op parent update is forced so child-only aggregate saves advance the token. This is a useful concurrency safeguard for writes that use this path. It does not remove the architectural risk of full-aggregate replacement, and direct writes to child tables must be reviewed to ensure they cannot bypass the aggregate token.

**Target direction:** replace full aggregate read/modify/replace with bounded domain commands and repositories. Each command should own its transaction, expected concurrency token, validation, audit/outbox effects, and explicit write set.

### 2.2 Carry-forward recalculation is incomplete

`WalletService.RecomputeCarryOverFrom` recalculates every existing month from a supplied month onward. The controller calls it after confirming or cancelling a deposit.

However, the reviewed expense and revenue controllers recalculate the current month's distribution after adding or deleting an expense/revenue but do not call `RecomputeCarryOverFrom`. `WalletController.AddAdjustment` adds an adjustment but also does not recalculate subsequent carry-over balances.

Therefore, when an earlier month is changed after later months already exist, the later months can retain stale `CarryOver` values. The resulting apartment balances can disagree with the recomputed historical month.

Required target behavior:
- All balance-affecting commands must identify the earliest affected accounting period.
- Recalculate or post deterministic adjustment entries for all later periods in one controlled operation.
- Preserve closed/posted periods; corrections to closed periods should use explicit reversal/adjustment entries, not silently rewrite historical balances.

### 2.3 Allocation rule and rounding

`RecalculateDistribution` and `RecalculateRevenueDistribution` split totals equally among apartments where `Closed == false`.

`DistributeExactCents` rounds a common share to two decimals using AwayFromZero, then assigns the entire remaining difference to the final apartment in the collection. That makes the result dependent on apartment ordering and can place a disproportionate rounding adjustment on one apartment.

This does not implement the approved DEC-17 Largest Remainder approach. It also does not use effective-dated ownership/occupancy percentages or the configured `BuildingSettings.ExpenseDistribution` value in the reviewed calculation path.

**Target direction:** define the allocation basis explicitly (equal, ownership share, area, or another approved basis), snapshot the eligible parties and weights for each accounting period, and use deterministic Largest Remainder allocation with stable tie-breaking. Store the allocation inputs and resulting shares so historical results are reproducible.

### 2.4 Monthly fee is not a historical transaction

`ComputeWalletBalance` subtracts `Apartment.MonthlyFee` at calculation time. The unified statement also creates a synthetic monthly-fee row using the apartment's current fee. The reviewed persistence model does not show an immutable monthly charge row or effective-dated fee history.

Changing an apartment's monthly fee can therefore change how an old month is interpreted when it is recalculated or displayed. Historical charges should be posted as period-specific financial entries with amount, currency, rule/source, effective period, and posting status.

### 2.5 Expense and revenue deletion

`ExpensesService.DeleteExpense` and `RevenuesService.DeleteRevenue` remove entries from the in-memory month collection. The mapper's `Sync` then physically deletes the corresponding SQL row. The reviewed paths do not create a reversal record or preserve the deleted amount, category, date, actor, and reason.

For an accounting-grade design, posted financial entries should not be silently deleted. Use Draft -> Posted -> Reversed/Voided state transitions, immutable original entries, linked reversal entries, and mandatory actor/reason/time metadata. Any legacy deletion must be reconciled and retained in the migration evidence before cutover.

### 2.6 Deposit lifecycle — partial controls, incomplete command boundary

The current deposit workflow has useful protections:
- New deposits require a positive amount and an existing apartment.
- New resident deposits are Pending.
- Only Pending deposits can be confirmed or cancelled.
- Pending deposits can be edited only with a positive new amount, and an edit-history row is added.
- Confirmed and cancelled deposits are not editable through `UpdatePendingDeposit`.

Remaining target gaps:
- Status is represented as free-form strings (`pending`, `confirmed`, `cancelled`) across the model and mapper rather than a governed state machine.
- The transition command, audit append, notification creation, and carry-forward recomputation are coordinated in the controller rather than one application-service transaction.
- There is no explicit idempotency key for payment submission or confirmation; duplicate browser submissions/retries need a first-class protection strategy.
- Deposit numbering is calculated by scanning the month's in-memory collection for the maximum D-number. Concurrent requests can calculate the same next number unless the database enforces a unique sequence/number constraint and retries conflicts.
- The controller's update path catches broad exceptions and returns a generic message; operational failure classification and retry behavior should be centralized.

**Target direction:** use a Deposit aggregate/state machine, optimistic concurrency, idempotent command IDs, a database uniqueness constraint for human-readable numbers, and an atomic outbox for notifications.

### 2.7 Manual wallet adjustments

`AddWalletAdjustment` accepts positive or negative values and stores reason, month, actor, and timestamp. The controller checks only that the amount is non-zero; apartment existence and month validity are deferred to the mapper, which may skip invalid records with a warning. The controller does not surface those mapper warnings as a rejected command.

The target command must validate apartment/building scope, accounting period, amount precision/range, reason, and actor before persistence. Adjustments should be immutable and corrections should be made through linked reversal/replacement entries.

### 2.8 Reporting semantics

`GetAllTransactions` intentionally summarizes recurring charges, expense distribution, revenue distribution, and carry-forward for the latest selected month only, while listing deposits and adjustments across the selected month range. This may be a valid statement presentation rule, but it is not a complete historical ledger. Reports must distinguish:
- a current balance snapshot;
- a period activity statement;
- a full historical ledger;
- a building-level cash movement report.

Do not reuse one query/result model for all four meanings.

## 3. Voting domain — current behavior and risks

Evidence:
- `Services/PollsService.cs`
- `Controllers/PollsController.cs`
- `Controllers/ResidentPollsController.cs`
- `Views/ResidentHome/Index.cshtml`
- `Models/Poll.cs`
- `Data/Entities.cs`
- `Data/BuildingSqlMapper.cs`

### 3.1 Ballot identity is the apartment

The service accepts `aptId` and rejects a second vote when `poll.Votes.ContainsKey(aptId)`. The SQL table `PollVotes` has primary key (BuildingId, PollId, ApartmentId), and stores OptionId directly.

This implements one vote per apartment, not one vote per eligible person/account. It cannot represent multiple independently eligible owners/occupants in one apartment, delegation, changing eligibility, or an audit-safe record of who was eligible without linking the ballot to the voter.

DEC-32/33 (owner/occupant/tenant eligibility) must be resolved before target voting schema and migration mapping are finalized.

### 3.2 Selected option leaks into AuditLog

After a successful vote, `ResidentPollsController.Vote` reads the selected option text and writes it to AuditLog together with the poll question and apartment number. This directly defeats secret-ballot expectations even if the vote table is later hidden from ordinary users.

The target audit event must record only that an eligible subject cast a ballot, with poll/session correlation and outcome metadata that cannot reveal the selected option. Ballot storage must be separated from identity/eligibility evidence.

### 3.3 Poll lifecycle and eligibility are incomplete

`IsPollActive` checks only `!Closed` and `!IsPollExpired`. Deadline parsing uses `DateTime.TryParse` and compares the result to UTC; an invalid non-empty deadline is treated as not expired, and timezone interpretation is implicit.

There is no per-poll eligibility policy, eligibility snapshot, quorum rule evaluation, ballot opening/closing timestamp, or explicit immutable published definition in the reviewed service. The building settings contain a voting quorum percentage, but the reviewed poll command path does not evaluate it.

The target model needs an explicit PollDefinition, PollEligibilityPolicy, frozen eligible-subject snapshot, PollSession/lifecycle, ballot issuance/acceptance, and close/tally workflow. Deadline values must be stored as timezone-aware UTC instants with the building timezone captured where local scheduling is required.

### 3.4 Poll deletion and cascade retention

The admin controller supports physical poll deletion. The live schema cascades Poll -> PollOptions -> PollVotes. A poll with votes therefore loses its ballot rows when deleted. For any poll that has opened or received ballots, the target should close/archive it and preserve the definition, eligibility evidence, ballot evidence, and tally according to the approved retention policy. Physical deletion should be limited to unpublished drafts with no votes and an explicit retention-safe rule.

### 3.5 Tally and voter feedback

The current view uses apartment-keyed vote presence to decide whether a resident has voted. This cannot be preserved unchanged when ballots are anonymized. The target needs a separate per-eligible-subject participation receipt/token that can answer “has voted” without exposing the selected option or making the ballot identity linkable.

## 4. Target domain boundaries

### Finance
- `AccountingPeriod` / `BuildingAccountingPolicy`
- `FinancialCategory`
- `Expense` and `Revenue` with Draft/Posted/Reversed lifecycle
- `ApartmentCharge` for period-specific recurring fees
- `AllocationRun` + `AllocationInputSnapshot` + `ApartmentAllocation`
- `Deposit` state machine and immutable deposit edit/reversal events
- `WalletLedgerEntry` (immutable signed entries) and `WalletBalanceSnapshot`
- `WalletAdjustment` as a controlled journal command
- `FinancialAuditEvent` and transactional outbox

### Voting
- `PollDefinition` and immutable published version
- `PollEligibilityPolicy` and `PollEligibleSubjectSnapshot`
- `PollSession` with explicit lifecycle and UTC boundaries
- `BallotCredential` / one-time participation receipt
- `SecretBallot` stored without apartment/user identifiers
- `VoteTally` and auditable close/certification record
- separate eligibility/participation evidence from ballot selection

## 5. Owner decisions to surface when implementation design reaches them

No immediate decision is required to continue source review. The following existing decisions remain blockers for the relevant design:
- DEC-15: preserve existing wallet semantics until the accounting model is approved.
- DEC-17: deterministic Largest Remainder allocation.
- DEC-31: allow incomplete ownership shares with explicit state.
- DEC-32/33: per-poll eligibility policy (owner/occupant/tenant).
- DEC-38: operational secret ballot separation vs stronger cryptographic isolation.
- DEC-37: distinct retention policies for financial, identity, audit, and voting data.

Recommended defaults:
- Posted finance records are immutable; corrections use reversals.
- Allocation inputs and results are period snapshots, not recalculated from current apartment state.
- Ballot option and voter identity are never stored together in ordinary application/audit records.
- Published polls with votes are never physically deleted through normal administration.

## 6. Review boundaries

- Findings are based on checked-in source inspection only.
- No database was queried in this phase.
- No migration, DDL, DML, application build, or tests were executed.
- No code changes were made.
- Gate A remains **NOT AUTHORIZED**.
