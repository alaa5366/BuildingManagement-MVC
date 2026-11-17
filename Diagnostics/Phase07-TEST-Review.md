# Phase07 – TEST Diagnostics Review

**Review date:** 2026-11-17  
**Database:** `BuildingManagement_RestoreTest`  
**Server:** `ZOS-AAM\\LOCALDB#8B6B20E4`  
**Capture time:** `2026-11-17 10:54:46.6708236 +02:00`  
**Scope:** TEST only. Production was not accessed or modified.

## Result summary

The Phase07 read-only diagnostics completed successfully against the 32-table TEST database.

| Check | Result |
|---|---|
| Negative apartment monthly fees | No rows |
| Negative expenses / revenues | No rows |
| Non-positive deposits | No rows |
| Unknown deposit statuses | No rows |
| Unknown financial category kinds | No rows |
| Malformed MonthKey values | No rows |
| Multiple primary residents per apartment | No rows |
| Duplicate deposit display numbers per building/month | No rows |
| Settings outside preliminary numeric bounds | No rows |
| Disabled or untrusted FK/CHECK constraints | No rows |
| Existing CHECK constraint inventory | No rows |

## One date/month mismatch – reviewed

The diagnostic returned one revenue row:

- BuildingId: `LmkUDOJcjlvjiGmluz6c`
- Revenue Id: `c33ad79bffd34c69af653a9a8b1025d7`
- MonthKey: `2026-10`
- RevenueDate: `2026-09-30`

### Code review finding

The application intentionally stores the accounting month and the transaction date as separate values:

- `RevenuesService.AddRevenue(..., date, monthKey)` adds the revenue to the selected `monthKey` collection while retaining the independently supplied `date`.
- `RevenuesController.Add` accepts both `month` and `date` and passes them separately to the service.
- `BuildingSqlMapper.SaveAsync` persists `MonthKey = mk` and `RevenueDate = ParseDate(r.Date)` independently.
- `BuildingSqlMapper.LoadAsync` reloads the record into `Month(r.MonthKey)` and preserves `RevenueDate` as the revenue's displayed date.

Therefore, a revenue dated September 30 and posted to October is supported by the current application model. The diagnostic row is **not, by itself, a data-integrity defect**. It may represent a legitimate cross-month accounting entry. No global equality constraint between `RevenueDate` month and `MonthKey` should be introduced without an explicit accounting requirement.

## Decision

1. Treat the single mismatch as an informational review item, not an automatic violation.
2. Do not update or delete the row.
3. Do not add a CHECK constraint requiring the date month to equal MonthKey.
4. All other diagnostics returned no anomalies in this TEST snapshot.
5. Continue Phase07 with a separate code-level review of concurrency-sensitive write paths and transaction boundaries.

## Acceptance boundary

This review applies only to the captured TEST database state. It does not certify production data, and it authorizes no production changes or data cleanup.
