# Phase 42 — Deposit actor forensics

- Date: 2026-11-19
- Branch: `development/database-redesign-enterprise`
- Script: `scripts/Invoke-Phase42-DepositActorForensics.ps1`
- Mode: read-only.

## Purpose

Phase41 found ten confirmed deposit actors and sixteen cancelled deposit actors unresolved by exact legacy Users UID mapping. This phase profiles the literal actor values from `ConfirmedBy`, `CancelledBy`, `CreatedBy`, `UpdatedBy`, and deposit edit history against the five legacy administrative user records.

## Matching tiers

- Exact UID match: potentially resolvable only when the same UID has a reviewed `LegacyIdentityMap` row (`MappingStatus=1`, non-null PersonId and AccountId) and a real `UserAccounts` row.
- Exact or trimmed display-name/email match: diagnostic candidate only. It is not accepted as proof of historical actor identity and never updates identity mappings.
- No match or multiple candidates: remains unresolved.

## Safety

The script reads only `BuildingManagement_RestoreTest`. It does not modify users, identity maps, accounts, credentials, deposits, financial periods, ledger rows, or wallet balances. Raw actor values are written to a local timestamped report so they can be compared exactly; review that report before sharing it outside the project team.

## Run

```powershell
git pull --ff-only
.\scripts\Invoke-Phase42-DepositActorForensics.ps1
```

The output will establish whether the legacy actor strings are UIDs, display names, email addresses, or values that do not match the current administrative user inventory. It will not automatically resolve any actor.
