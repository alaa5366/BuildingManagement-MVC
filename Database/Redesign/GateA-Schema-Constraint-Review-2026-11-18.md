# Gate A Schema Constraint Review — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Scope:** Static review of the additive Gate A SQL and the latest read-only preflight results. No SQL was executed and no database objects or rows were changed.

## 1. Preflight result

The latest run at `Database/Diagnostics/GateA-Preflight-Results/20261118-155025/` reports:

- Target database: `BuildingManagement_RestoreTest` on `(localdb)\\MSSQLLocalDB`.
- The three legacy key columns are non-nullable `nvarchar(64)` (128 bytes).
- All three column collations match the database default: `SQL_Latin1_General_CP1_CI_AS`.
- Buildings and Apartments primary/unique keys required by the proposed FKs are present.
- All 13 Gate A target tables are absent.
- The four expected EF Core migrations are present through `20261117130000_AddUserTwoFactor`.

This is a metadata preflight pass only. It does not validate the SQL by executing it, prove application compatibility, or authorize deployment.

## 2. Database-enforced invariants already present

The current SQL provides useful hard guarantees:

- At most one `UserAccounts` row per Person through unique `PersonId`.
- External provider/subject uniqueness when both values are supplied.
- Explicit scope-shape checks for Building and Apartment scoped role assignments and permission overrides.
- Restrictive foreign keys to legacy Buildings/Apartments and target Persons/Accounts/Roles.
- Validity checks for ownership, occupancy, credentials, role assignments, and permission overrides.
- At most one open-ended primary occupancy per apartment.
- Durable source-row uniqueness in `LegacyIdentityMap`; Residents rows may map to a Person, while Users rows require a Person and Account in linked/separate states.
- Filtered uniqueness prevents multiple legacy Users source rows from pointing to the same target Account.

## 3. Required transactional service invariants

SQL constraints alone cannot safely enforce all domain rules. Before any backfill or application cutover, the migration and authorization services must validate these rules inside transactions, with concurrency protection:

1. **Legacy identity pair:** for every linked/separate `Users` source row, the referenced `UserAccounts.AccountId` must belong to the same `PersonId` stored in `LegacyIdentityMap`. The current independent FKs prove that both rows exist, not that they are paired. Reject a mismatched pair.
2. **Ownership intervals and shares:** prevent overlapping ownership periods for the same apartment/person where disallowed, and validate the total ownership share for each effective date. Never infer missing shares or normalize incomplete totals.
3. **Occupancy intervals:** prevent overlapping active primary-occupant intervals, not merely duplicate rows with `ValidToUtc IS NULL`. The filtered index protects only open-ended primary rows; historical/future interval conflicts need transactional overlap checks.
4. **Role/scope compatibility:** validate that a role is allowed at the selected scope (especially System-only SuperAdmin), and that Apartment scope belongs to the selected Building. The FK and scope-shape check do not encode role policy.
5. **Permission evaluation:** apply account status, role, scope, effective dates, explicit deny precedence, and resource ownership/building context in one centralized authorization evaluator. Do not rely on UI visibility or role name alone.
6. **Revocation lifecycle:** update validity/revocation fields consistently, record actor/reason, and ensure a revoked grant cannot remain effective due to an open-ended validity interval.
7. **Credential replacement:** ensure `ReplacedByCredentialId` points to a credential for the same account, reject self-reference/cycles, and define which credential types may have multiple active records.
8. **MFA lifecycle:** enforce enrollment verification before enabling MFA, protect against TOTP time-step replay, invalidate/reissue recovery codes atomically, and enforce mandatory MFA policy for admin/SuperAdmin at authentication and session revalidation.
9. **Account/person lifecycle:** archive/disable accounts without deleting identity, credentials, role history, or provenance. Re-check account status and scope before issuing authentication cookies and on session revalidation.

## 4. Deliberate non-unique attributes

The current schema does not make Person email or phone globally unique, and does not make `UserAccounts.NormalizedEmail` unique. Keep this intentional until product rules and the legacy duplicate/shared-contact evidence justify a stronger invariant. The current profile found duplicate phone groups; phone/email must not become automatic identity keys.

## 5. Remaining design risks before freezing Gate A

- `ApartmentAccessGrants.GrantType` uses owner-approved byte-backed values Family=1, Tenant=2, Caretaker=3, Emergency=4, Temporary=5, each paired to a fixed versioned `PermissionSetKey` with database CHECK constraints. Profiles remain fail-closed until an authorization evaluator and explicit permission policy exist; see `Phase06-Identity-Authorization-Migration-Recheck-2026-11-18.md`.
- `ApartmentAccessGrants` needs the same explicit revocation/audit lifecycle policy as role assignments and account permission overrides.
- The one-open-primary filtered index does not prevent overlapping dated primary intervals. The service-side interval rule above is mandatory; a future SQL Server trigger/exclusion-like strategy may be considered only after concurrency tests.
- `LegacyIdentityMap.SourceKey` must contain only the stable source row key, never phone, email, PIN, password/hash, or other personal/secret data.
- The Gate A script is table-by-table guarded with `IF OBJECT_ID(..., 'U') IS NULL`. It is not a partial-state repair tool. Before execution, the preflight must show all 13 objects absent. If any target object is present, stop and inspect it rather than rerunning the script blindly.

## 6. Gate decision

**Gate A remains NOT AUTHORIZED for execution.** The latest metadata preflight passes, but the schema still needs the access-grant type/lifecycle decision and the application-side transactional invariants above need an implementation/test plan. Credential verification/re-enrollment, manual identity dispositions, finance/carry-over policy, wallet-view external dependency confirmation, cascade retention policy, and disposable restore/rollback rehearsal remain separate blockers.

No DDL/DML, EF migration, backfill, credential rewrite, or application cutover was performed during this review.
