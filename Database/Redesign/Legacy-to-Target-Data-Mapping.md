# Legacy-to-Target Data Mapping and Reconciliation Rules

**Branch:** `development/database-redesign-enterprise`  
**Status:** implementation mapping draft based on the checked-in EF entities/context and the restored-test schema inventory.  
**Safety:** this document authorizes no DDL/DML, backfill, credential conversion, merge, or production change.

## 1. Mapping principles

1. Preserve every legacy row and source key until its target mapping is reconciled.
2. Never identify a human by name, email, phone, WhatsApp, apartment number, or a combination of these alone.
3. Exact legacy UID equality is a candidate linkage, not automatic proof that two records describe one human. Require uniqueness on both sides and review collisions, shared logins, and inconsistent personal details.
4. A person may exist without an interactive account. A resident relationship is not itself proof of ownership.
5. Do not copy PIN plaintext, password hashes, MFA secrets, API tokens, or encrypted device credentials into a new format without a tested, documented cryptographic compatibility path.
6. Preserve apartment-scoped finance and legacy apartment-attributed poll votes without fabricating person attribution.
7. Every backfill operation must be restartable, idempotent, auditable, and retain a source-to-target mapping record.

## 2. Identity and account mapping

| Legacy source | Legacy field(s) | Proposed target | Mapping rule | Confidence / action |
|---|---|---|---|---|
| `Users` | `Uid` | `UserAccounts` legacy reference + `LegacyIdentityMap` | Preserve exact UID in migration provenance; new AccountId is a UUID. | High for source-account continuity; do not reuse UID as new PK. |
| `Users` | `Name` | `Persons.DisplayName` | Candidate person profile value. | Medium; reconcile against exact-UID Residents records. |
| `Users` | `Email`, `Phone`, `Whatsapp`, `PhotoUrl` | `Persons.PrimaryEmail`, `PrimaryPhone`, `Whatsapp`, `PhotoUrl` | Normalize only for comparison; retain original value in source until reviewed. | Medium; conflict review required. |
| `Users` | `IsActive`, `IsDisabled`, `DisabledReason` | `UserAccounts.AccountStatus` | Define an explicit status mapping and precedence; disabled must not become active. | Requires status mapping test. |
| `Users` | `Role` | `UserRoleAssignments` | `superadmin` => System scope; `admin` => Building scope only where matching `BuildingAdmins` evidence exists. | `resident` and role conflicts require review; no inferred building scope. |
| `Users` | `CreatedAt`, `UpdatedAt`, `LastLoginAt` | account/person audit timestamps | Preserve source timestamps and timezone interpretation. | Verify stored timezone convention. |
| `Residents` | `Uid` | candidate link to `Users.Uid`; then account/person | Exact UID, unique on both sources, no conflicting identity evidence. | Candidate only; profile duplicates and multiple apartment relationships are possible. |
| `Residents` | `Id` | `LegacyIdentityMap.SourceKey` | Keep as source row identity; do not treat it as PersonId. | High. |
| `Residents` | `Name`, `Phone`, `Whatsapp`, `Email`, `PhotoUrl` | `Persons` | Use as profile evidence after UID linkage; conflicting values are queued for review, not overwritten by last-write-wins. | Medium / manual conflict resolution. |
| `Residents` | `IsActive`, `IsDisabled`, `DisabledReason` | account status and/or relationship status | Separate ability to log in from whether the person currently occupies/has access to an apartment. | Domain decision; do not collapse into one flag. |
| `Residents` | `PinHash`, `PasswordHash` | `UserCredentials` | Only if hash algorithm, parameters, verification implementation, and credential policy are confirmed. | Security gate; never copy plaintext. |
| `Residents` | `IsOwner` | `ApartmentOwnerships` candidate | Candidate ownership relationship only; no share percentage or legal effective dates can be inferred. | Manual/legal review; no automatic ownership creation. |
| `Residents` | resident row + `BuildingId`, `ApartmentId` | `ApartmentOccupancies` candidate | Relationship candidate; use explicit effective dates only if trustworthy source dates exist. | Requires business rule and date provenance. |
| `Residents` | `IsPrimary` | `ApartmentOccupancies.IsPrimary` candidate | Preserve only after detecting multiple active primary rows and resolving them. | Blocked by profiling. |
| `BuildingAdmins` | `BuildingId`, `AdminUid` | `UserRoleAssignments` | Create one Building-scoped admin assignment for a verified account. | High if both FKs resolve; preserve source row provenance. |
| `UserPermissions` | `Uid`, `Permission` | `AccountPermissionOverrides` | Preserve each account-specific permission as `AccountPermissionOverrides`; retain explicit effect and scope. | Blocking design gap; see section 7. |

### Account uniqueness and UID collisions

- The target invariant is one interactive account per Person, not one Person per legacy UID.
- A single `Residents.Uid` may occur across several apartments as relationship rows; this can represent one person with multiple apartment relationships and must not create multiple accounts.
- A UID appearing in both `Users` and `Residents` is a linkage candidate only. Validate counts, status, and profile consistency.
- A UID reused by multiple humans, shared household logins, or duplicate normalized email/phone groups must be quarantined for review.
- Do not generate Person rows from each Residents row before grouping by validated identity evidence.

## 3. Apartment and relationship mapping

| Legacy source | Legacy field(s) | Proposed target | Mapping rule | Confidence / action |
|---|---|---|---|---|
| `Apartments` | `BuildingId`, `Id` | target relationship FKs | Preserve exact composite key values and SQL type/collation. | High; no rekey in this rollout. |
| `Apartments` | `Owner` | ownership candidate evidence | Text is not a verified Person reference and contains no share/date. | Manual review only; never create owner automatically. |
| `Apartments` | `Phone`, `Email` | apartment contact/default contact | Do not automatically copy to Person; may be shared/family/building contact. | Preserve as legacy apartment fields until semantics confirmed. |
| `Apartments` | `Pin` | none in target identity tables | Treat as sensitive legacy authentication material. | Do not migrate; reset/re-enroll according to approved credential policy. |
| `Apartments` | `IsClosed`, `IsDisabled`, `OpenDate`, `CloseDate` | apartment lifecycle | Keep on Apartments; not an occupancy record. | Preserve. |
| `Apartments` | `Language` | apartment default preference | Keep as default until person-level preference is implemented and precedence defined. | Decision required. |
| `Apartments` | `NotifyWhatsApp`, `NotifyEmail`, `NotifyInApp` | apartment default notification preferences | Do not assume one apartment setting represents every occupant. | Decision required; preserve existing behavior. |
| `Apartments` | `PreferredPaymentMethod` | apartment default payment preference | Preserve as apartment-level default; do not reinterpret as account payment data. | Decision required. |
| `Apartments` | `MonthlyFee` and wallet-related fields | existing financial model | No identity migration; keep apartment-scoped accounting unchanged. | Preserve and reconcile totals. |

### Ownership and occupancy rules

- `IsOwner` does not establish legal ownership percentage, title evidence, or an effective date.
- `IsPrimary` is a current flag, not a dated occupancy history.
- Multiple active occupants are valid; multiple active primary occupants for one apartment are not.
- Do not infer a current occupancy start date from `CreatedAt` unless the business explicitly confirms that this field means occupancy start.
- Never infer ownership from the `Apartments.Owner` display string, apartment phone, or email.
- Any incomplete ownership-share total must remain explicitly incomplete; do not normalize percentages to 100%.

## 4. Authorization mapping

| Legacy source | Proposed target | Mapping rule |
|---|---|---|
| `Users.Role = superadmin` | `RoleDefinitions` + System-scoped `UserRoleAssignments` | Only for active, verified account; preserve disabled state and source audit. |
| `Users.Role = admin` + `BuildingAdmins` | Building-scoped role assignments | Use each explicit BuildingAdmins row; do not grant access to every building. |
| `Users.Role = resident` | No direct role assignment until resident account semantics are reconciled | A resident's apartment membership/access is distinct from an admin role. |
| `UserPermissions` | Account-level permission override (not yet modeled) | Must preserve allow/deny and scope semantics; see section 7. |
| Existing controller policies / claims | authorization compatibility layer | Inventory all role strings, claims, permission keys, and resource checks before cutover. |

Every target authorization check must evaluate role, permission, scope, validity interval, account status, and target resource tenant/building. A role name alone is never sufficient.

## 5. Credentials, MFA, and secrets

| Legacy source | Data | Target handling |
|---|---|---|
| `Users.Pin` | legacy PIN field | Treat as plaintext/unknown format until proven otherwise. Do not copy. Require secure reset/re-enrollment. |
| `Residents.PinHash` | resident PIN hash | Verify BCrypt format and supported work factor with test vectors before any credential migration. |
| `Residents.PasswordHash` | optional password hash | Identify algorithm/version/parameters; migrate only with a verified verifier or force reset. |
| `Users.TwoFactorSecretEncrypted` | encrypted TOTP secret | Keep legacy authentication path until encryption key, ciphertext format, and successful decrypt/verify round-trip are proven. |
| `Users.TwoFactorRecoveryCodesJson` | stored recovery-code hashes | Validate exact hash format, one-time semantics, and replay protection before normalization into child rows. |
| `GlobalSettings` | integration API secrets | Move only through authenticated encryption with external key management; browser DTOs expose configured state only. |
| `Dvrs.PasswordEncrypted` | device password ciphertext | Separate domain from account credentials; preserve and verify existing encryption compatibility. |

Never include secret values in profiling output, migration logs, exception messages, audit details, or browser responses.

## 6. Voting and financial history

- Existing `PollVotes` is apartment-attributed and keyed by `(BuildingId, PollId, ApartmentId)`. Preserve it as legacy voting evidence; do not relabel it as anonymous or person-attributed.
- New secret ballots must not contain `PersonId`, `AccountId`, `ApartmentId`, eligibility ID, credential hash, or a join key back to the voter.
- Existing deposits, expenses, revenues, monthly shares, wallet adjustments, and audit records remain on their current keys and services during identity rollout.
- Before and after any future migration, compare counts and aggregate totals by building/month/status/category. Investigate all differences; never “fix” a mismatch by changing source financial rows.

## 7. Blocking model gap: direct user permissions

The current application has `UserPermissions(Uid, Permission)`, which stores permissions per user. The proposed target currently has `RolePermissions(RoleKey, PermissionKey, Effect)`, which stores permissions per role. These are not semantically equivalent.

Before migrating `UserPermissions`, choose one of these explicit models:
1. Add `AccountPermissionOverrides` with AccountId, PermissionKey, Effect (Allow/Deny), scope, validity, grant/revocation audit, and a restrictive FK to UserAccounts; or
2. Convert each legacy permission set to a controlled custom role only if role lifecycle, duplication, review, and scope semantics are formally defined.

**Recommended:** option 1, with deny precedence and explicit scope. Do not treat legacy per-user grants as role definitions or silently broaden their access. The additive Gate A SQL and EF model must be amended to include the chosen structure before permission backfill.

## 8. Profiling gates and required outputs

Run `Database/Diagnostics/DatabaseRedesign-ReadOnly-Profiling.sql` against the disposable restored database and retain all ten result sets. Required review items:

- Duplicate UID/email/phone groups and cross-table UID matches.
- Residents without valid apartment references; apartments without building/floor references.
- Legacy owner text versus IsOwner flags.
- Multiple active primary residents and per-apartment resident/owner/primary counts.
- Finance aggregates by building/month/status/category, monthly shares, and wallet adjustments.
- Poll vote-to-option and vote-to-poll integrity.
- Cameras without DVR and cross-building DVR assignments.
- Secret-presence counts only; never export secret values.
- Wallet view presence, FK delete actions, and exact EF migration history.

Also capture exact SQL type, max_length, collation, nullability, and key order for every FK component used by Gate A. The seven existing inventory files do not contain the profiling outputs above.

## 9. Cutover acceptance criteria

No backfill or application cutover until all are true:

- [ ] UID candidate groups have a reviewed disposition: linked, separate, or unresolved.
- [ ] One account per Person is enforced without merging shared accounts.
- [ ] Every account has a reviewed credential path; no plaintext PIN is copied.
- [ ] User role and direct permission semantics are preserved at the correct scope.
- [ ] Ownership/occupancy dates and legal evidence are approved; unresolved rows remain unresolved.
- [ ] Current primary occupant uniqueness and interval-overlap rules pass.
- [ ] Finance totals and legacy poll vote counts reconcile exactly.
- [ ] All cross-building references are checked.
- [ ] MFA secret recoverability and key rotation are tested.
- [ ] Disposable restore, rollback, and application regression tests pass.

**Current status:** mapping rules documented; no entity code, EF migration, SQL execution, backfill, or application cutover has been performed by this artifact.


## 10. Durable legacy identity provenance — Gate A schema decision

The Gate A additive script now includes `dbo.LegacyIdentityMap` (not the earlier generic `IdentityMigrationMap` name).

- One row is uniquely identified by `(SourceSystem, SourceTable, SourceKey)`; initial allow-listed sources are `Users` and `Residents`.
- Each Residents source row may map to a Person; multiple Residents rows may intentionally map to the same Person because they can represent multiple apartment relationships.
- Each Users source row may map to a Person and exactly one Account. A filtered unique index prevents multiple Users source rows from mapping to the same Account.
- Residents source rows cannot directly map an Account in this provenance table. Account linkage is established through the reviewed Person/Account relationship, not duplicated across resident relationship rows.
- Status values: 0 Pending, 1 Linked, 2 Separate, 3 Rejected, 4 ManualReview. Pending/rejected/manual-review rows may retain NULL targets; linked/separate rows require a Person. A Users row in Linked or Separate disposition also requires an Account; Pending, Rejected, and ManualReview rows must not have target IDs.
- The table stores evidence references and safe review reasons, not raw contact values, PINs, hashes, or other secrets.
- FKs are restrictive. The mapping is migration evidence and must survive ordinary Person/Account lifecycle operations.

The database constraints do not prove that AccountId belongs to the same PersonId in this mapping row; the migration service must verify that pair transactionally against UserAccounts before accepting a linked Users row. This avoids relying on an unenforced polymorphic/source-specific relationship.

This schema is additive design only. The revised Gate A script has not been executed, and no backfill has started.

## 11. Operational backfill execution status — 2026-11-19

The following additive backfills have reported exact source/target/provenance reconciliation on the restored test database. Source rows remain intact:

| Phase | Legacy source | Reported rows | Result |
|---|---|---:|---|
| 10 | Notifications | 68 | RECONCILIATION PASS |
| 11 | MaintenanceRecords | 1 | RECONCILIATION PASS |
| 12 | AuditLog | 1,053 | RECONCILIATION PASS |
| 13 | BackupHistory | 2 | RECONCILIATION PASS |

Phase 14 adds `BackupScheduleConfigurations` as a distinct configuration target for `ScheduledBackupSettings`; schedule configuration is not a backup execution/job. Its backfill script is dry-run by default, requires the Phase14 migration to be recorded, rejects unsupported or out-of-range settings rather than normalizing them, preserves the exact CollectionsJson value, and writes LegacyDataMigrationMap provenance. Phase14 backfill is pending execution and reconciliation.

These phases are additive migration steps, not application cutover. Legacy tables remain authoritative until the later application-read/write transition is separately implemented and verified.


## 8. Operational backfill execution status

| Phase | Legacy source | Target | Status |
|---|---|---|---|
| 10 | Notifications | NotificationMessages / NotificationRecipients / NotificationReadStates | Reconciliation passed |
| 11 | MaintenanceRecords | MaintenanceRequests | Reconciliation passed |
| 12 | AuditLog | AuditEvents | Reconciliation passed |
| 13 | BackupHistory | BackupJobs | Reconciliation passed |
| 14 | ScheduledBackupSettings | BackupScheduleConfigurations | Reconciliation passed |
| 15 | Presence | PresenceSessions | Script and schema migration published; apply pending |

### Presence observation rules (Phase 15)
- The legacy Presence row stores a last-seen observation, not a login/session start or a heartbeat history.
- `StartedAtUtc`, `AccountId`, and `DeviceReference` remain NULL; the legacy UID is retained only as a source reference.
- `ExpiresAtUtc` is derived as LastSeen + the existing two-minute online threshold. No historical heartbeat rows are fabricated.
- One source row maps to one deterministic PresenceSessions row with LegacyDataMigrationMap provenance. The legacy table is never modified or deleted.


| 16 | QrTokens / QrUsages | QrCapabilities / LegacyDataMigrationMap | Schema and idempotent backfill published; apply pending |

### QR legacy migration rules (Phase 16)
- A capability lookup digest is SHA-256 of the legacy opaque UID. The existing legacy HMAC signature must still be validated before this digest is used for authorization; the digest alone is not proof of a valid QR token.
- Account/person identity is not inferred. Target type remains unclassified and target reference explicitly retains legacy apartment scope.
- Single-use tokens retain MaxUses=1; legacy multi-use semantics are represented by the maximum integer bound.
- QrUsages are aggregate counters, not event-level records. They are retained as provenance evidence and are not expanded into invented QrCapabilityUses events.
- Device fingerprints are intentionally excluded from migration output. Source QR rows remain unchanged.


| 17 | Polls / PollOptions / PollVotes | PollLifecycleEvents + LegacyDataMigrationMap evidence | Schema and idempotent backfill published; apply pending |

### Poll migration rules (Phase 17)
- Poll creation is recorded as a lifecycle event with EventType 0 explicitly marked unclassified; the original question, description, deadline, and closed state are preserved in event metadata.
- PollOptions and PollVotes are fingerprinted and retained as legacy evidence in LegacyDataMigrationMap; no option or vote rows are discarded.
- Legacy votes remain apartment-attributed. They are not converted into person-attributed votes or anonymous PollBallots because the source semantics do not establish either identity model.
- No eligibility credentials, delegation, ballot tokens, or synthetic individual vote events are created. Source tables remain unchanged.
