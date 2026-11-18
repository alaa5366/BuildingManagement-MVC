# Target Entity Inventory and Phased Schema Roadmap

**Date:** 2026-11-18  
**Branch:** development/database-redesign-enterprise  
**Status:** Logical entity inventory for completeness and sequencing. Candidate names only; not physical DDL and not authorization to create any table.

## 1. Purpose and classification

This inventory ensures the redesign covers the whole application rather than only the initial identity foundation.

- **Gate A candidate:** the 13 additive identity/property/authorization tables already defined in the proposed Gate A SQL. Still NOT AUTHORIZED.
- **Later target candidate:** logical entity needed for a later approved module; columns and keys are not frozen.
- **Retain legacy initially:** existing table remains authoritative during coexistence until a separate module cutover.
- **Read model/projection:** derived/query object, not necessarily a persisted table.
- **External artifact:** file/provider resource with database metadata and lifecycle.

## 2. Identity, authentication and authorization

| Candidate entity | Classification | Purpose |
|---|---|---|
| Persons | Gate A candidate | Human identity |
| UserAccounts | Gate A candidate | Interactive account, one per Person maximum |
| UserCredentials | Gate A candidate | Versioned password/PIN verifier history |
| UserTwoFactorSettings | Gate A candidate | Encrypted MFA configuration and replay state |
| UserRecoveryCodes | Gate A candidate | One-time hashed recovery codes |
| RoleDefinitions | Gate A candidate | Controlled roles |
| RolePermissions | Gate A candidate | Role-to-permission effects |
| UserRoleAssignments | Gate A candidate | Effective-dated scoped role grants |
| AccountPermissionOverrides | Gate A candidate | Account-specific Allow/Deny with scope and validity |
| LegacyIdentityMap | Gate A candidate | Reviewed source-to-target provenance |
| LoginSessions / SessionRevocations | Later target candidate | Server-side session state/revocation if cookie/security-stamp design requires durable session registry |
| AuthenticationAttempts | Later target candidate or operational store | Security counters and risk signals; avoid raw credentials |
| AccountRecoveryRequests | Later target candidate | Audited recovery workflows, expiry, one-time completion |

## 3. Property, ownership, occupancy and access

| Candidate entity | Classification | Purpose |
|---|---|---|
| ApartmentOwnerships | Gate A candidate | Effective-dated Person ownership/share |
| ApartmentOccupancies | Gate A candidate | Effective-dated Person occupancy/primary status |
| ApartmentAccessGrants | Gate A candidate | Explicit dated access rights |
| Buildings / Floors / Apartments | Retain legacy initially | Existing property identifiers remain stable during migration |
| BuildingAdminAssignments | Retain legacy initially, map to UserRoleAssignments | Source evidence for building-scoped administrator roles |
| PropertyEvidenceDocuments | Later target candidate | Private ownership/occupancy evidence with retention/access metadata |
| ApartmentRelationshipReviewCases | Later target candidate if manual review queue is needed | Unresolved ownership/occupancy and conflicting legacy relationship evidence |

## 4. Finance and apartment wallet

| Candidate entity | Classification | Purpose |
|---|---|---|
| Deposits | Retain legacy initially | Existing payment/deposit evidence and state |
| DepositEditHistory | Retain legacy initially | Existing deposit change history |
| Expenses | Retain legacy initially | Existing expense source records |
| Revenues | Retain legacy initially | Existing revenue source records |
| MonthlyApartmentShares | Retain legacy initially | Existing period allocations |
| WalletAdjustments | Retain legacy initially | Existing manual adjustments |
| FinancialPeriods | Later target candidate | Explicit open/closed accounting periods and timezone |
| LedgerTransactions | Later target candidate | Atomic posting group/idempotency |
| LedgerEntries | Later target candidate | Immutable apartment financial movements |
| FinancialSourceLinks | Later target candidate | Link new postings to legacy deposits/expenses/revenues/adjustments |
| AllocationBatches | Later target candidate | Frozen period allocation run and algorithm version |
| AllocationLines | Later target candidate | Apartment-level deterministic allocation output |
| FinancialReversals | Later target candidate or typed LedgerTransaction | Compensating correction referencing original posting |
| ReconciliationRuns | Later target candidate | Versioned legacy/target comparison |
| ReconciliationItems | Later target candidate | Per-source/per-apartment/per-period variance and disposition |
| ApartmentWalletProjection | Read model/projection | Rebuildable current/historical wallet statement |
| BuildingFinancialSummary | Read model/projection | Bounded report summary, not source of truth |

Do not create these finance tables until DEC-15/16/17 and period/currency policies are approved and the legacy formulas are executable parity fixtures.

## 5. Polls and secret ballots

| Candidate entity | Classification | Purpose |
|---|---|---|
| Polls / PollOptions | Retain legacy initially | Existing poll definition/options |
| PollVotes | Retain legacy initially | Historical apartment-attributed votes; never relabel as anonymous |
| PollVotingPolicies | Later target candidate | Versioned Person/Apartment voting mode and eligibility policy |
| PollDelegations | Later target candidate | Dated representative authority per poll/apartment |
| PollEligibility | Later target candidate | Eligibility subject and one-time participation credential state |
| PollBallots | Later target candidate | Anonymous choice only; no identity/eligibility link |
| PollLifecycleEvents | Later target candidate | Open/close/freeze/finalize/annul events |
| PollResults | Read model/projection | Tally and result publication |
| PollAuditEvents | Prefer shared AuditEvents with poll-specific safe metadata | No selected option or identity-choice link |

## 6. QR, guest access and capability tokens

| Candidate entity | Classification | Purpose |
|---|---|---|
| QrTokens / QrUsages | Retain legacy initially | Existing token/use records while compatibility is maintained |
| QrCapabilities | Later target candidate | Target-bound short-lived capability with signed payload version, expiry and use policy |
| QrCapabilityUses | Later target candidate | Atomic use/consumption history without secret token storage |
| QrCapabilityRevocations | Later target candidate or status on QrCapabilities | Explicit revocation and actor/reason |
| GuestAccessSessions | Later target candidate | Dedicated short-lived guest principal/session; never synthetic resident identity |

Token payloads must not contain unsigned authorization fields. Query-string, referrer, proxy, application log, and analytics exposure must be controlled.

## 7. Notifications and presence

| Candidate entity | Classification | Purpose |
|---|---|---|
| Notifications | Retain legacy initially | Existing notification source records |
| NotificationMessages | Later target candidate | Canonical message content/template and origin |
| NotificationRecipients | Later target candidate | Explicit Person/account/apartment recipient identity |
| NotificationDeliveries | Later target candidate | Provider delivery attempts and outcomes |
| NotificationReadStates | Later target candidate | Per-recipient read state |
| NotificationPreferences | Later target candidate | Person/account preferences with apartment defaults if approved |
| Presence | Retain legacy initially | Existing apartment-level presence source |
| PresenceSessions | Later target candidate | Per-account/device session presence |
| PresenceHeartbeats | Later target candidate or telemetry store | Bounded heartbeat evidence/expiry |

Do not migrate shared apartment inbox/read state into person-level state without explicit policy and user-visible behavior.

## 8. Maintenance and service requests

| Candidate entity | Classification | Purpose |
|---|---|---|
| MaintenanceRecords | Retain legacy initially | Existing maintenance source |
| MaintenanceRequests | Later target candidate | Resident/admin request lifecycle |
| MaintenanceAssignments | Later target candidate | Staff/vendor assignment history |
| MaintenanceComments | Later target candidate | Conversation history with visibility |
| MaintenanceAttachments | External artifact + metadata | Private documents |
| MaintenanceLedgerLinks | Later target candidate | Durable source reference to posted expense/reversal |
| MaintenanceStatusEvents | Later target candidate or shared AuditEvents | Workflow history |

## 9. Cameras, DVRs and media

| Candidate entity | Classification | Purpose |
|---|---|---|
| Dvrs / Cameras | Retain legacy initially | Existing camera/DVR metadata |
| MediaCredentialReferences | Later target candidate | Vault reference/key version; never expose encrypted password in broad DTO |
| MediaProvisioningOperations | Later target candidate | Retryable external registration/update/delete state |
| MediaAccessSessions | Later target candidate | Authorized short-lived playback session |
| MediaAuditEvents | Prefer shared AuditEvents | View/configuration/credential operations |

## 10. Documents, receipts, OCR and exports

| Candidate entity | Classification | Purpose |
|---|---|---|
| DocumentAssets | Later target candidate | Private storage metadata, owner, classification, retention |
| DocumentAccessGrants | Later target candidate | Resource-specific download rights |
| FileProcessingJobs | Later target candidate | Malware/content scan, thumbnail, OCR, conversion state |
| ReceiptOcrExtractions | Later target candidate | Advisory extracted fields and confidence; no payment authority |
| ExportJobs | Later target candidate | Actor/scope/format/filter-bound asynchronous export |
| ExportArtifacts | External artifact + metadata | Private, expiring, audited downloads |
| ImportBatches | Later target candidate | Staged, versioned import bound to actor and target |
| ImportRows | Later target candidate | Per-row validation/commit state |
| ImportIssues | Later target candidate | Safe row-level error codes and resolution |
| ImportCommitItems | Later target candidate | Idempotent source-to-created-record mapping |

## 11. Settings, integration secrets and platform operations

| Candidate entity | Classification | Purpose |
|---|---|---|
| GlobalSettings / BuildingSettings | Retain legacy initially | Existing non-secret settings; secrets must be extracted |
| ProtectedIntegrationSecrets | Later target candidate | Encrypted provider secrets, key version and scope |
| UserPreferences | Later target candidate | Person/account-specific settings |
| OutboxMessages | Later target candidate / platform foundation | Durable external side-effect dispatch |
| InboxMessages | Later target candidate / platform foundation | Idempotent message consumption/deduplication |
| AuditEvents | Later target candidate / platform foundation | Durable business/security audit |
| BackupHistory / ScheduledBackupSettings | Retain legacy initially | Existing backup metadata/settings |
| BackupJobs | Later target candidate | Typed backup/restore operation state |
| BackupArtifacts | External artifact + metadata | Integrity-verified protected files |
| BackupRetentionEvents | Later target candidate | Retention/deletion evidence |
| RestoreOperations | Later target candidate | Approval, prechecks, execution and outcome |
| HealthSnapshots | Read model/telemetry | Safe operational health summary |

## 12. Platform-wide invariants

- Person is the human subject; UserAccount is the interactive actor.
- Every scoped relationship resolves to a valid Building/Apartment chain.
- Financial and audit history is restrictive and append-only after posting.
- Ballot choices have no voter/eligibility foreign key.
- Capability tokens and recovery codes are one-time and atomically consumed.
- Private documents and backup artifacts require explicit authorization and expiring access.
- Integration secrets are encrypted with external key management and write-only UI/API semantics.
- Outbox handlers are idempotent; external side effects are retryable and reconcilable.
- Read models are rebuildable and never become an unreviewed source of truth.
- All target entities preserve legacy source provenance until cutover acceptance.

## 13. Phased schema delivery

| Stage | Schema scope | Rule |
|---|---|---|
| Gate A | 13 identity/property/authorization foundation tables | Additive only; explicit approval and disposable restore rehearsal |
| Gate B | No fixed table list yet; final Person/account/role/relationship constraints and seed catalog | Only after Gate A parity and owner decisions |
| Finance phase | FinancialPeriods, LedgerTransactions/Entries, allocation and reconciliation entities | After financial policy and exact legacy parity |
| Poll phase | Voting policy, delegation, eligibility, ballots, lifecycle | After voter subject and secrecy assurance decisions |
| Operations phase | Maintenance, notification recipient, presence, media operation entities | After visibility/recipient semantics |
| Documents/import phase | Document assets, OCR jobs, exports and import batches | After privacy/retention/provider decisions |
| Platform phase | Audit/outbox/inbox, backup/restore orchestration, integration secrets | Foundation may be staged earlier only with explicit module contracts |
| Legacy retirement | Drop/rename old structures only after accepted module cutover | Separate approval; never bundled with additive rollout |

## 14. Explicit non-authorization

This inventory does not authorize creation of any table. Only the proposed Gate A SQL contains physical DDL, and Gate A remains NOT AUTHORIZED. All other entities are logical candidates pending detailed data dictionary, ERD, constraints, migration mapping, and owner decisions. No database or application code was changed by this document.
