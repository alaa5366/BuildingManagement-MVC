# Cross-Document Consistency and Gate A Design Review

**Date:** 2026-11-18  
**Branch:** `development/database-redesign-enterprise`  
**Scope:** Target Architecture, Implementation Backlog/Decision Register, Target Data Dictionary/ERD, Enterprise Blueprint, Legacy Mapping, Gate A SQL, and the 31-section source review.  
**Status:** Read-only design review with documentation-only corrections. No SQL was executed.

## 1. Executive result

The documents are now substantially aligned on the central model:

- Person is the human identity; UserAccount is the interactive principal.
- A Person may have zero or one interactive account; account identity is not inferred from apartment membership.
- Ownership, occupancy, apartment access, and authorization are separate dated concepts.
- Role-wide grants and account-specific permission overrides remain distinct.
- Legacy source mapping is durable and reviewed; no fuzzy auto-merge.
- Existing apartment financial records remain authoritative until parity and reconciliation are proven.
- Secret-ballot identity/eligibility is separated from ballot choice, with no claim of cryptographic unlinkability.
- Gate A is additive design only and remains **NOT AUTHORIZED**.

The review found and corrected several documentation/schema-definition inconsistencies. Remaining items are explicitly listed below and must be closed before physical implementation approval.

## 2. Corrections completed

### 2.1 Decision register recovered older product questions

The earlier Blueprint had seven product questions that were not fully represented in the first consolidated decision register. DEC-31 through DEC-38 now carry:
- ownership completeness and partial/unknown shares;
- person-mode voter eligibility;
- apartment-mode voter eligibility;
- one representative acting for multiple apartments;
- apartment defaults vs person-specific preferences;
- legacy authentication compatibility and reset policy;
- legal retention/deletion policy;
- required ballot secrecy assurance level.

These supplement DEC-01 through DEC-30. They are not treated as approved merely because recommended defaults are documented.

### 2.2 Recovery-code storage made unambiguous

The data dictionary previously listed both a serialized `RecoveryCodeHashes` column and a normalized `UserRecoveryCodes` table. The redundant serialized column has been removed from the proposed `UserTwoFactorSettings` shape. `UserRecoveryCodes` is the single authoritative one-time-use store.

### 2.3 ERD relationships completed

The conceptual ERD now includes:
- UserAccount -> AccountPermissionOverrides;
- UserAccount -> LegacyIdentityMap for legacy Users account provenance.

The absence of any PollEligibility -> PollBallots relationship remains intentional.

### 2.4 Gate A review and grant lifecycle constraints strengthened

The proposed SQL artifact now includes:
- a constraint requiring reviewer identity and timestamp for resolved Linked/Separate LegacyIdentityMap rows;
- grant timestamp and rationale fields for UserRoleAssignments;
- grant and revocation rationale fields for AccountPermissionOverrides;
- grant timestamp for ApartmentAccessGrants;
- paired revocation timestamp/actor checks for UserRoleAssignments, AccountPermissionOverrides, and ApartmentAccessGrants.

These are design-artifact edits only. They have not been run against SQL Server and are not a migration approval.

## 3. Current Gate A inventory

The proposed additive Gate A script contains these 13 tables:

1. Persons
2. UserAccounts
3. UserCredentials
4. UserTwoFactorSettings
5. UserRecoveryCodes
6. RoleDefinitions
7. RolePermissions
8. UserRoleAssignments
9. ApartmentOwnerships
10. ApartmentOccupancies
11. ApartmentAccessGrants
12. AccountPermissionOverrides
13. LegacyIdentityMap

Gate A does **not** create the future poll/ballot model, financial ledger, outbox, audit redesign, document storage, integration-secret vault, or operational read models. These belong to later design/implementation phases. Do not imply that Gate A delivers those modules.

## 4. Remaining technical design issues before Gate A can be frozen

| ID | Finding | Required action | Status |
|---|---|---|---|
| CONS-01 | `ApartmentAccessGrants.GrantType` is a numeric code but the allowed values are not yet defined in a shared catalog or CHECK constraint. | Define stable grant types, semantics, expiry/revocation transitions, and service authorization behavior. | Open |
| CONS-02 | `ApartmentAccessGrants.PermissionSetKey` is versioned by description but has no approved code-owned catalog/versioning policy. | Define the permission profile registry, whether profiles are immutable/versioned, and how changed profiles affect existing grants. | Open |
| CONS-03 | Granting actor columns are nullable in some administrative grant tables. | Decide whether system-generated grants use a dedicated system actor; do not leave unexplained NULL actor attribution for human operations. | Open |
| CONS-04 | `UserRoleAssignments` and `AccountPermissionOverrides` permit a NULL grant actor. | Require actor for human-issued grants; define explicit service/system actor for automated provisioning. | Open |
| CONS-05 | Role and permission catalog ownership is split between code-owned definitions and SQL rows. | Define seed/version deployment, deactivation, and compatibility behavior when code and DB catalog versions differ. | Open |
| CONS-06 | Personal scope has NULL scope IDs and relies on resolving Account -> Person in application logic. | Define a single authorization evaluator contract and tests proving a personal grant can never authorize another Person. | Open |
| CONS-07 | Ownership-share aggregate and interval-overlap constraints cannot be expressed by the current row-level checks. | Implement transactional serializable/domain validation and concurrent-write tests; retain explicit incomplete ownership periods. | Open |
| CONS-08 | The filtered unique primary-occupancy index only protects currently open intervals, not arbitrary historical overlap. | Implement transactional interval validation and correction workflow; test concurrent changes. | Open |
| CONS-09 | `LegacyIdentityMap` independently references PersonId and AccountId; SQL cannot prove they belong together. | Migration application service must verify Account.PersonId = map.PersonId in the same transaction; add negative integration tests. | Open |
| CONS-10 | Resolved mapping rows now require reviewer/timestamp, but the authority to review and the evidence quality rubric are not yet specified. | Define migration reviewer permission, dual approval for ambiguous cases, and permitted evidence classes. | Open |
| CONS-11 | Gate A uses legacy key columns as nvarchar(64), supported by the latest supplied restore profile for Buildings.Id and Apartments key columns, but all dependent FK targets and EF/SQL deployment definitions still need a complete final comparison. | Re-run complete metadata/FK/collation and EF model/migration comparison immediately before any Gate A rehearsal. | Evidence required |
| CONS-12 | SQL views are absent from the restored DB while AppDbContext maps `vw_ApartmentWalletMovements`; FullSchema is a separate definition source. | Choose one authoritative, versioned SQL-view deployment mechanism and prove fresh-restore deployment. | Blocked by DATA-01 |
| CONS-13 | Existing startup PIN migration mutates legacy data at application startup. | Remove or feature-gate that startup mutation before runtime validation against evidence-preserving restores. | P0 blocker |
| CONS-14 | Existing legacy permission values are account-specific and some scopes are not yet known. | Do not backfill until every permission key has an approved resource scope and consuming endpoint/service. | P0 blocker |
| CONS-15 | Secret ballot schema separation does not prevent correlation by privileged access, timing, backups, or infrastructure logs. | Approve assurance level; if unlinkability is required, use a separately reviewed cryptographic protocol. | Open |

## 5. Product decisions still required from the owner

The consolidated register contains DEC-01 through DEC-38. The following are the decisions most likely to change core domain/schema behavior and should be prioritized for owner review:

1. **DEC-05 / DEC-31 — Ownership:** may the system store partial or unknown ownership, and when must shares total exactly 100%?
2. **DEC-07 — Access:** who may issue temporary/emergency apartment access, and what evidence/expiry is required?
3. **DEC-08 — Permissions:** should legacy user-specific grants become scoped Allow/Deny overrides? Recommendation: yes, never broaden them to roles.
4. **DEC-12 / DEC-32 / DEC-33 — Voting:** who is eligible in person polls and apartment polls: owners, occupants, tenants, or configurable policy?
5. **DEC-14 / DEC-38 — Ballot assurance:** is operational secrecy sufficient, or is cryptographic unlinkability/certification required?
6. **DEC-17 — Finance:** what deterministic rule allocates rounding remainder between apartments?
7. **DEC-19 — Maintenance:** are resident requests visible apartment-only or building-wide by category?
8. **DEC-20 / DEC-21 / DEC-35 — Preferences and presence:** are notifications/read state/preferences per person, account, or apartment; what exactly does presence count?
9. **DEC-26 / DEC-37 — Retention:** what legal retention periods and holds apply to identity, financial, audit, and ballot records?
10. **DEC-27 — Deployment:** confirm whether the intended production topology remains single-instance or requires shared cache/session/key infrastructure from the first release.

These decisions do not all block independent read-only work. Each will block only the dependent design/implementation slice.

## 6. Architecture-to-data ownership checks

| Domain | Authoritative subject | Must not be confused with |
|---|---|---|
| Human identity | Person | Resident row, apartment, phone, email |
| Interactive sign-in | UserAccount | Person or apartment relationship |
| Legal ownership | ApartmentOwnerships | Occupancy or apartment display name |
| Physical/contractual residence | ApartmentOccupancies | Ownership or login status |
| Temporary/limited apartment rights | ApartmentAccessGrants | Ownership, occupancy, role assignment |
| Administrative authorization | UserRoleAssignments + RolePermissions | UI visibility or legacy role string |
| Per-account exception | AccountPermissionOverrides | Role-wide permission |
| Financial balance | Apartment-scoped ledger/read model | Person account or report subtotal |
| Poll eligibility | PollEligibility | Ballot choice |
| Secret choice | PollBallots | Person, account, apartment, eligibility token |

## 7. Next safe work sequence

Continue without waiting for owner decisions on unrelated tracks:

1. Complete the read-only metadata/deployment reconciliation for key types, FKs, views, EF migrations, FullSchema, and indexes.
2. Review every Gate A table against the target data dictionary and ERD; produce a column/constraint parity matrix.
3. Review the authorization catalog and map each permission to endpoint, service, resource, and scope.
4. Produce the domain glossary and command/query contract catalogue for the new modules.
5. Prepare the disposable-restore Gate A rehearsal package with dry-run/rollback and post-check SQL. Do not execute it.
6. Present the owner decisions in a concise numbered list with recommendations, while continuing all independent design work.

## 8. Safety and verification statement

- This review edited documentation and proposed SQL text only.
- No Gate A SQL was executed.
- No database, EF migration, application source, or production setting was changed.
- No backfill, identity merge, credential rewrite, financial mutation, or cutover occurred.
- No build or test result is claimed.
- Gate A remains **NOT AUTHORIZED**.

The design is not frozen until the remaining technical issues and owner decisions have documented dispositions. The work may continue on independent read-only and design tracks without blocking on every open decision.


## 9. Additional Gate A consistency hardening

A second pass over the SQL artifact identified further cases where the proposed physical constraints needed to match the domain contract. The documentation artifacts were updated as follows:

- `UserAccounts.AccountStatus` now documents numeric values 0 Pending, 1 Active, 2 Locked, 3 Disabled.
- External login provider and subject must be either both NULL or both populated.
- `UserCredentials.CredentialType` is currently limited to 0 Password and 1 PIN; external provider identities are account-level, not password-hash rows. Additional credential types require an explicit schema design rather than an undocumented numeric value.
- MFA enabled state requires ciphertext, key version, and enrollment timestamp. Disabled state clears the active secret/key version.
- Role permission effect is explicitly 0 Deny / 1 Allow, with Deny precedence.
- The Gate A SQL now stops if only a partial set of its 13 target tables already exists. A complete existing set still requires separate schema parity validation; object existence is not enough.
- The data dictionary now includes grant timestamps and grant/revocation rationale fields consistent with the proposed SQL for role assignments and account permission overrides, and the grant timestamp for apartment access grants.

These remain proposed SQL/document changes only. The Gate A rehearsal plan requires a fresh read-only metadata check and schema-parity comparison before any approved execution.

## 10. Additional open implementation details

- The `CredentialType` catalog currently supports Password and PIN only. Passkeys, recovery factors, external identity providers, and future authenticators must be modeled explicitly rather than assigned ad hoc enum values.
- Disabled MFA clears the active encrypted secret. Recovery codes must be consumed/revoked as part of disable/re-enrollment workflows; this lifecycle must be tested.
- Role assignment and permission override grantor fields are nullable in the current proposed schema. Automated grants need a dedicated system actor; human-issued grants should not have missing actor attribution. This remains an owner/security-policy decision.
- ApartmentAccessGrants `GrantType` values and `PermissionSetKey` registry/version semantics remain open (CONS-01/02).
- A complete set of pre-existing Gate A tables must be validated column-by-column and constraint-by-constraint before the idempotent script is allowed to skip creation. The current script guard only rejects a partial set.
