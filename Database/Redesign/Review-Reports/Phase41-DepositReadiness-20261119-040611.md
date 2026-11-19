TotalDeposits: 26
PendingCount: 0
ConfirmedCount: 10
CancelledCount: 16
UnknownStatusCount: 0
NonPositiveAmountCount: 0
SubCentAmountCount: 0
InvalidMonthKeyCount: 0
MissingApartmentCount: 0
ApartmentBuildingMismatchCount: 0
ConfirmedMissingStampOrActorCount: 0
ConfirmedActorUnresolvedCount: 10
CancelledMissingStampActorOrReasonCount: 14
CancelledActorUnresolvedCount: 16
PendingHasResolutionMetadataCount: 0
AlreadyInTargetCount: 0
MissingTargetPeriodCount: 26
Scope groups: 4
Scope: Building=zxhRd7cwZ07rZotXeQ17; Month=2026-09; Status=cancelled; Count=16; Amount=6356.00; ConfirmedActorUnresolved=0; CancelledActorUnresolved=16
Scope: Building=zxhRd7cwZ07rZotXeQ17; Month=2026-09; Status=confirmed; Count=4; Amount=5119.00; ConfirmedActorUnresolved=4; CancelledActorUnresolved=0
Scope: Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-09; Status=confirmed; Count=1; Amount=16780.00; ConfirmedActorUnresolved=1; CancelledActorUnresolved=0
Scope: Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-10; Status=confirmed; Count=5; Amount=6712.00; ConfirmedActorUnresolved=5; CancelledActorUnresolved=0
PeriodProfile: Building=zxhRd7cwZ07rZotXeQ17; Number=BLD-001; ExistingPeriods=0; PeriodsMissingTimeZone=0
PeriodProfile: Building=ZzgYia4qMZ23N8qhiCOV; Number=BLD-002; ExistingPeriods=0; PeriodsMissingTimeZone=0
PeriodProfile: Building=LmkUDOJcjlvjiGmluz6c; Number=BLD-003; ExistingPeriods=0; PeriodsMissingTimeZone=0
READ ONLY: no INSERT, UPDATE, DELETE, ledger posting, period creation, or wallet balance mutation was performed.
Actor resolution rule: exact legacy Users UID only, with reviewed MappingStatus=1 and a real UserAccounts row. Display-name/email/fuzzy matching is deliberately excluded.
Timestamp note: legacy DepositEntity timestamps are SQL DateTime/DateTime? values, not free-text fields. Their original timezone semantics are not established by this scan.
Period note: a present FinancialPeriod is not sufficient by itself; its timezone and UTC boundaries still require business-owner validation.
# Phase 41 â€” Deposit readiness audit

- Run: 2026-11-19T04:06:12.1542005+02:00
- Database: BuildingManagement_RestoreTest
- Mode: READ ONLY

## Aggregate findings
TotalDeposits                           : 26
PendingCount                            : 0
ConfirmedCount                          : 10
CancelledCount                          : 16
UnknownStatusCount                      : 0
NonPositiveAmountCount                  : 0
SubCentAmountCount                      : 0
InvalidMonthKeyCount                    : 0
MissingApartmentCount                   : 0
ApartmentBuildingMismatchCount          : 0
ConfirmedMissingStampOrActorCount       : 0
ConfirmedActorUnresolvedCount           : 10
CancelledMissingStampActorOrReasonCount : 14
CancelledActorUnresolvedCount           : 16
PendingHasResolutionMetadataCount       : 0
AlreadyInTargetCount                    : 0
MissingTargetPeriodCount                : 26

## Scope groups
BuildingId           MonthKey Status    DepositCount AmountTotal ConfirmedActorUnresolved CancelledActorUnresolved
----------           -------- ------    ------------ ----------- ------------------------ ------------------------
zxhRd7cwZ07rZotXeQ17 2026-09  cancelled           16     6356.00                        0                       16
zxhRd7cwZ07rZotXeQ17 2026-09  confirmed            4     5119.00                        4                        0
ZzgYia4qMZ23N8qhiCOV 2026-09  confirmed            1    16780.00                        1                        0
ZzgYia4qMZ23N8qhiCOV 2026-10  confirmed            5     6712.00                        5                        0

## Building period profile
BuildingId           BuildingNumber ExistingPeriodCount PeriodsMissingTimeZone
----------           -------------- ------------------- ----------------------
zxhRd7cwZ07rZotXeQ17 BLD-001                          0                      0
ZzgYia4qMZ23N8qhiCOV BLD-002                          0                      0
LmkUDOJcjlvjiGmluz6c BLD-003                          0                      0

## Interpretation
- Exact UID identity mappings only; no display-name, email, or fuzzy actor resolution.
- Source DateTime fields are inspected for null/presence, but timezone meaning is not inferred.
- Missing periods, unknown statuses, unresolved actors, invalid apartment scope, and malformed amounts remain blocked.
- No source row, target row, status, period, ledger entry, or wallet balance is changed.
