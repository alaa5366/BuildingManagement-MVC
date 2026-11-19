BuildingManagement Financial and Wallet Read-only Reconciliation
Generated: 2026-11-18T15:25:35.5764207+02:00
Server instance: (localdb)\MSSQLLocalDB
Database: BuildingManagement_RestoreTest
Result sets exported: 5
Total rows: 215

Purpose:
- Compare expense/revenue source-ledger totals with persisted apartment-share totals by building/month.
- Export per-apartment stored shares, carry-over, confirmed deposits, adjustments, and CURRENT apartment fee/closed state.
- Calculate a deterministic candidate equal allocation ordered by ApartmentId for comparison only.
- Identify missing monthly share rows and share rows without a matching apartment.

Privacy and safety:
- Windows Integrated Security; no password is requested or saved.
- Restricted to BuildingManagement_RestoreTest and guarded in SQL.
- SELECT/metadata-only; no stored shares are recalculated or changed.
- No resident names, phone numbers, emails, PINs, hashes, or credentials are exported.
- CurrentMonthlyFee and CurrentlyClosed are current values, not historical snapshots.
- Candidate allocations use ApartmentId ordering and are NOT proof of the historical allocation order or historical eligibility.

Review:
- Prioritize building/month variances already found in the profiling assessment.
- Compare per-apartment stored shares with candidate values, missing rows, and current eligibility.
- Do not update or overwrite any stored monthly share based on this diagnostic alone.
