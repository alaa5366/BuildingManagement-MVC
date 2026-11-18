# Implementation Backlog and Decision Register

**Date:** 2026-11-18  
**Branch:** `development/database-redesign-enterprise`  
**Status:** Planning artifact only. No implementation or database execution is authorized by this backlog.

## 1. Delivery rules

- Execute work in dependency order; do not parallelize changes that alter the same domain contract.
- Each epic requires a design review, implementation plan, tests, operational notes, and acceptance evidence.
- No epic may silently change the meaning of legacy financial, identity, or access data.
- Database changes start only after the relevant gate is explicitly approved and rehearsed on a disposable restore.
- Do not combine broad UI rewrites with foundational data migrations in one unreviewable change.
- Keep the legacy path available until its replacement has passed parity and rollback criteria.

## 2. Epic backlog

| ID | Epic | Priority | Dependencies | Completion evidence |
|---|---|---:|---|---|
| ARCH-01 | Architecture and domain boundary freeze | P0 | None | Approved module map, dependency rules, aggregate ownership, glossary |
| ARCH-02 | Domain glossary, aggregate and command/query contract freeze | P0 | ARCH-01 | Canonical terms, use-case catalogue, typed outcomes, actor context, module contracts |
| DATA-05 | Full target entity inventory and phased schema boundary | P1 | ARCH-02 | Every module entity classified as Gate A, later target, retained legacy, read model, or external artifact |
| AUTH-02 | Legacy permission-to-resource scope catalogue | P0 | SEC-01, ARCH-02 | All permission keys mapped to resource/action/scope; stale workflows and decomposed privileges resolved |
| DB-01 | Authoritative SQL deployment and view reconciliation | P0 | DATA-01 | DEC-28 approved; view definition included in the authoritative deployment path; fresh-restore parity |
| FIN-03 | Financial ledger and wallet domain contract | P0 | DATA-04, ARCH-02 | Wallet semantics, period, rounding, reversal authority, and reconciliation fixtures approved |
| DATA-01 | Reconcile schema and deployment truth | P0 | ARCH-01 | Live inventory vs EF model/snapshot/history vs FullSchema vs views/triggers report |
| DATA-02 | Finalize target ERD and data dictionary | P0 | DATA-01 | Key types/lengths/collations, constraints, delete rules, indexes approved |
| DATA-03 | Identity mapping and provenance contract | P0 | DATA-02 | LegacyIdentityMap rules, uniqueness, review queue, restart semantics |
| DATA-04 | Finance source mapping and parity contract | P0 | DATA-01 | Per-building/apartment/month totals, variances explained, rounding policy |
| SEC-01 | Authorization scope catalogue | P0 | ARCH-01 | Every permission key classified by scope/resource/consumer; ambiguous grants held |
| SEC-02 | Security baseline and secret inventory | P0 | ARCH-01 | Secret locations, key management, redaction, rate-limit, no-store, threat model |
| APP-01 | Application foundation | P1 | ARCH-01 | Composition root, typed outcomes, validation, clock/current actor, correlation, cancellation |
| APP-02 | Transactional audit and outbox | P1 | APP-01, SEC-02 | Atomic audit/outbox tests, idempotent worker, retry/dead-letter/compensation |
| ID-01 | Person and account domain | P1 | DATA-02, DATA-03, APP-01 | One-account-per-person invariant and legacy provenance tests |
| ID-02 | Authentication and credential policies | P1 | ID-01, SEC-02 | No secret trimming; PIN/password policy; lockout/throttling; safe credential migration |
| ID-03 | MFA and session lifecycle | P1 | ID-02, SEC-01 | Admin/SuperAdmin mandatory enrollment, recovery, replay prevention, revocation SLA |
| AUTH-01 | Scoped role assignment and permission evaluator | P1 | SEC-01, ID-01 | System/building/apartment/personal matrix, deny precedence, fail-closed tests |
| PROP-01 | Ownership, occupancy and apartment access | P1 | ID-01, AUTH-01 | Temporal overlap/share constraints, explicit access grants, retained history |
| FIN-01 | Financial ledger and command state machines | P1 | APP-01, DATA-04 | Idempotency, concurrency, reversals, strict periods, no physical delete |
| FIN-02 | Wallet projections and reconciliation | P1 | FIN-01 | Legacy/target parity, deterministic rounding, documented variance sign-off |
| POLL-01 | Eligibility, delegation and secret ballot | P1 | ID-01, PROP-01, AUTH-01 | No voter-choice join path, duplicate prevention, secrecy and tally tests |
| QR-01 | QR capability tokens and guest access sessions | P1 | APP-01, AUTH-01, ID-03 | Signed target-bound claims, validate-before-consume, atomic one-time use, no-store/referrer/rate limits, stable session revalidation |
| IMP-01 | Import and migration workbench | P2 | APP-02, DATA-03, DOC-01 | Actor/target/file-digest binding, strict parser, preview, row-level validation, idempotent commit, checkpoint and reconciliation |
| SEC-03 | Centralized authorization evaluator | P1 | SEC-01, ID-01, APP-01 | One policy engine for role + permission + scope + account/session + resource; Deny precedence; fail-closed tests |
| OPS-01 | Maintenance, notifications and presence | P2 | PROP-01, AUTH-01, APP-02 | Explicit visibility/recipient/counting semantics and bounded queries |
| DOC-01 | Private document and receipt pipeline | P1 | APP-02, SEC-02 | Private storage, decoded-content limits, authorized download, orphan compensation |
| INT-01 | OCR and import hardening | P2 | DOC-01, APP-01 | OCR advisory-only, per-user limits, no silent coercion/defaults |
| MEDIA-01 | Camera/DVR/media orchestration | P2 | AUTH-01, APP-02, SEC-02 | Narrow credential access, retryable provisioning/decommissioning |
| SET-01 | Settings and integration secret vault | P2 | APP-01, SEC-02 | Write-only secrets, encrypted storage, no GET side effects |
| OPS-02 | Backup, restore and retention | P1 | APP-02, SEC-02 | Typed outcomes, artifact/history reconciliation, restore drill, no-store downloads |
| OBS-01 | Audit and application log center | P2 | APP-02, AUTH-01 | Scoped query, correlation, retention, safe redaction, query budgets |
| WEB-01 | New controllers, view models and views | P2 | Relevant module APIs | Thin controllers, no EF entities/secrets, antiforgery and authorization coverage |
| CUT-01 | Compatibility adapters and module cutover | P1 | Module acceptance | Feature-flagged rollout, parity, rollback, no legacy write drift |
| RET-01 | Legacy retirement | P3 | All cutovers | Zero unexplained variances, retention approval, explicit decommission approval |

## 3. First execution sequence

1. **ARCH-01 / ARCH-02:** consolidate the module boundaries, domain glossary, aggregate ownership, and command/query contracts. Draft artifacts are now published; next is a cross-module conflict pass.
2. **DATA-01 / DB-01:** close live-schema, EF model/snapshot/history, FullSchema, and SQL-view deployment discrepancies. DEC-28 owner confirmation selects the single authoritative deployment path.
3. **DATA-02 / DATA-05:** reconcile exact key metadata and classify every target entity by phase; do not infer physical types from the proposed ERD.
4. **SEC-01 / AUTH-02:** classify every legacy permission key by actual endpoint, service operation, resource, and scope; resolve stale workflows and privilege decomposition.
5. **DATA-03:** finalize LegacyIdentityMap review authority, evidence rubric, and restartable migration workflow.
6. **DATA-04 / FIN-03:** freeze financial source baselines and wallet parity fixtures; obtain DEC-15/16/17 and finance authority decisions before ledger schema.
7. Close the technical items in the cross-document consistency review, then prepare the Gate A column/constraint manifest and disposable-restore rehearsal package.
8. Gate A execution remains a separate explicit approval. Preparing a rehearsal package does not authorize running it.

## 4. Decision register

Status values: **Open** = requires business/technical approval; **Proposed** = a safe default is recommended but not yet accepted; **Evidence required** = cannot be decided from current source/profile; **Blocked** = dependent work must not proceed.

| ID | Decision | Recommended default | Status / blocking impact |
|---|---|---|---|
| DEC-01 | Human identity proof and merge authority | No automatic merge; designated reviewer approves with evidence and durable audit | Open; blocks identity backfill |
| DEC-02 | Shared phone/email handling | Treat as shared-contact candidate, not identity equality | Proposed; blocks fuzzy matching |
| DEC-03 | Person without account | Allow Person/ownership/occupancy without interactive account | Proposed; confirm notice delivery |
| DEC-04 | Account cardinality | Zero or one interactive account per Person; enforce transactionally and uniquely | Approved principle; schema/implementation pending |
| DEC-05 | Ownership evidence and shares | Require explicit source evidence, 0–100 share, approved total policy, effective dates | Open; blocks ownership backfill |
| DEC-06 | Primary occupancy | At most one active primary occupant per apartment at an instant; primary may be absent | Approved principle; temporal enforcement design pending |
| DEC-07 | Temporary/emergency access | Explicit dated grant with issuer, reason, scope and revocation | Open; blocks access migration |
| DEC-08 | Legacy permission scope | Classify each permission by actual resource consumer; ambiguous grants remain unresolved | Evidence required; blocks permission backfill |
| DEC-09 | SuperAdmin semantics | System-scoped role assignment on ordinary account, not separate identity type | Approved principle |
| DEC-10 | Admin MFA rollout | Mandatory with staged enrollment, recovery, bootstrap and monitored exception process | Approved direction; operational details open |
| DEC-11 | Resident MFA | Optional, with safe recovery | Proposed |
| DEC-12 | Voting subject | Poll explicitly declares Person or Apartment subject; Apartment requires representative delegation | Open; blocks poll redesign |
| DEC-13 | Ballot secrecy | Separate eligibility/nullifier from anonymous ballot choice; no direct join key | Approved principle; cryptographic design review required |
| DEC-14 | Ballot retention/correction | Retain immutable ballot evidence and documented annul/re-poll process; never edit individual choice | Open |
| DEC-15 | Financial accounting basis | Preserve apartment wallet semantics until accounting owner signs off on ledger model | Evidence required; blocks finance cutover |
| DEC-16 | Opening balance and period close | Explicit opening entries and closed-period correction via reversal/adjustment | Proposed |
| DEC-17 | Rounding remainder | Deterministic documented allocation, independent of incidental query ordering | Open; blocks financial parity |
| DEC-18 | Financial deletion/erasure | Posted entries are reversed/voided, not physically deleted | Approved target principle |
| DEC-19 | Maintenance visibility | Default to apartment/resource scope unless an explicit building-wide category is approved | Open |
| DEC-20 | Notification ownership/read state | Define recipient as Person/account/apartment per notification type | Open |
| DEC-21 | Presence meaning | Count explicit subject type (person/account/device/apartment); expose label in UI | Open |
| DEC-22 | Receipt storage and retention | Private object storage, short-lived authorized downloads, documented retention | Open |
| DEC-23 | OCR role | Advisory extraction only; never confirms payment | Approved principle |
| DEC-24 | Media authorization | Enforce at application and media-server layers; no direct credential DTO exposure | Approved principle |
| DEC-25 | Backup deletion semantics | Reconcile artifact and history; typed partial/missing states, audited operator action | Open |
| DEC-26 | Audit retention/tamper evidence | Separate security/business audit from diagnostic logs; define retention and privileged access | Open |
| DEC-27 | Deployment topology | Treat current deployment as single instance until proven otherwise; design ports for future shared state | Evidence required |
| DEC-28 | SQL view/schema deployment authority | Recommended: EF Core migrations are authoritative; FullSchema is generated/reference-only. Alternative: FullSchema is sole authority and EF deployment is reconciled/retired. | Owner decision required; blocked by DATA-01 |
| DEC-29 | Startup legacy PIN migration | Remove/feature-gate automatic startup mutation; require dry-run/checkpoint/operator approval | P0 blocker before runtime validation on preserved legacy data |
| DEC-30 | Health endpoint exposure | Minimal liveness public to trusted infrastructure; readiness details restricted to monitoring network | Open |

## 5. Definition of Done for every epic

An epic is complete only when all applicable items are true:

- Domain invariants are explicit and tested.
- Authorization is tested at the use-case and endpoint boundaries, including negative cases.
- Persistence constraints and transaction behavior are integration-tested against SQL Server.
- Query performance is measured on representative restored data with bounded result sizes.
- Audit records are durable, correlated, privacy-safe, and tested for failure behavior.
- External side effects are idempotent and have retry/compensation paths.
- Migration/reconciliation evidence exists when legacy data is involved.
- Rollback and operational runbooks are written and rehearsed where applicable.
- No secret or personal/financial row-level data is committed to Git diagnostics.
- The owner accepts the phase exit evidence.

## 6. Current gate state

- Architecture and backlog documents are planning artifacts.
- Gate A DDL remains **NOT AUTHORIZED**.
- No EF entities, migrations, database objects, or application runtime paths were changed by this planning work.
- No backfill, identity merge, credential rewrite, finance mutation, or cutover was performed.
- The next permissible work is further read-only reconciliation and finalization of the decision register; implementation begins only after the relevant approvals.


## 7. Additional decisions recovered from the earlier blueprint and mapping review

The earlier blueprint contains seven product-policy questions that must remain visible in the consolidated register. The mapping and current target model also reveal one additional decision about apartment-level preferences.

| ID | Decision | Recommended default | Status / blocking impact |
|---|---|---|---|
| DEC-31 | Ownership completeness | Permit explicitly incomplete/unknown ownership periods; never normalize partial shares to 100%. Require exactly 100% only when the period is declared complete and legally verified. | Open; blocks ownership backfill and share constraints |
| DEC-32 | Person-mode voting eligibility | Make eligibility a versioned poll policy; explicitly decide whether owners, occupants, or both qualify. Tenants/occupants may vote only when the poll policy says so. | Open; blocks poll eligibility implementation |
| DEC-33 | Apartment-mode voting eligibility | Define whether eligibility derives from owners, occupants, or an approved combination, and snapshot the rule before poll opening. | Open; blocks apartment-mode poll setup |
| DEC-34 | Multiple-apartment representation | Permit one representative to represent multiple apartments in one poll only if each apartment has a distinct valid delegation and the poll policy allows it. | Open; affects delegation uniqueness constraints |
| DEC-35 | Apartment vs person preferences | Keep existing apartment-level language/notification/payment preferences as defaults during migration; add person-level overrides only with explicit precedence and consent semantics. | Open; blocks preference cutover |
| DEC-36 | Legacy authentication compatibility | Do not silently carry plaintext legacy PINs. Preserve old verifier paths only where the verifier and cryptographic keys are proven; otherwise require reset/re-enrollment. Decide whether password, PIN, and external-provider sign-in remain supported during transition. | Open; blocks credential cutover |
| DEC-37 | Retention and legal deletion | Define retention periods and legal holds separately for buildings, finance, audit, identity evidence, and ballots. Financial/audit evidence is not erased by ordinary account/building deletion. | Open; blocks archival/deletion policy and final retirement |
| DEC-38 | Ballot secrecy assurance level | Default to operational separation with documented metadata-risk controls; if cryptographic unlinkability or certification is required, commission a separate protocol/security review before promising that property. | Open; blocks final ballot architecture claim |

These decisions supplement DEC-01 through DEC-30; they do not replace or silently resolve them.

## 8. Recovered cross-document consistency items

1. The earlier blueprint's seven open questions are now represented by DEC-31 through DEC-38 (the preferences question is additionally represented as DEC-35).
2. The data dictionary proposes both a `RecoveryCodeHashes` field and a normalized `UserRecoveryCodes` child table. Treat the child table as authoritative and remove the redundant serialized field from the final physical schema unless a separately justified compatibility need is approved.
3. The target architecture describes the modular monolith and application boundaries; the older blueprint/ERD remain logical and physical data proposals. Neither overrides the newer dependency rules.
4. `AccountPermissionOverrides` is required to preserve account-specific `UserPermissions`; it must be present consistently in the ERD, data dictionary, Gate A SQL, migration mapping, and permission evaluator. It is not interchangeable with role-wide `RolePermissions`.
5. `LegacyIdentityMap` is the authoritative provenance name. Retire references to the earlier conceptual `IdentityMigrationMap` name except in historical change notes.
6. The Gate A table inventory is additive-only and does not contain poll, finance-ledger, outbox, audit, private-document, or integration-secret target structures. Those belong to later approved phases and must not be implied by Gate A.
7. The current design uses a Person as the subject of ownership/occupancy/access grants, while interactive authorization is performed by UserAccount. Application services must resolve account -> Person -> effective relationship and never substitute AccountId for PersonId.
8. `ApartmentAccessGrants` grants access but does not create ownership, occupancy, or an account. Its permission profile and relationship to the general authorization evaluator require explicit policy before physical implementation.
9. Secret-ballot relational separation is not a cryptographic guarantee. Timing, privileged DB access, backups, and infrastructure logs remain metadata risks; the product must approve the assurance level.
10. The proposed SQL key types/lengths for all legacy FKs remain provisional until exact live metadata, EF model, and deployment artifacts are reconciled. No proposed type should be copied into DDL by assumption.


## 9. Additional permission and finance decisions consolidated from module designs

These decisions are also detailed in the Permission Scope Catalogue and Financial Ledger Design. The IDs below consolidate them into the main owner register.

| ID | Decision | Recommended default | Status / blocking impact |
|---|---|---|---|
| DEC-39 | SuperAdmin privilege boundaries | No unconditional bypass. Explicit System role grants; separate grants and fresh MFA for secret access, restore, global secret operations, and system maintenance. | Owner decision; blocks role templates and privileged-operation policies |
| DEC-40 | Financial permission granularity | Split wallet read, deposit confirm/cancel, adjustment, expense/revenue post/reverse, export, period close/reopen. | Owner decision; blocks finance authorization model |
| DEC-41 | Meaning of audit.view.own | Personal scope means own Person/account events; apartment-wide visibility requires a separate Apartment-scope permission. | Owner decision; blocks audit visibility policy |
| DEC-42 | WhatsApp permission boundary | Separate message dispatch from provider configuration and secret management. | Owner decision; blocks integration permissions |
| DEC-43 | Missing import/tools/sync workflows | Do not migrate grants for absent workflows. Retire stale catalog entries or approve a rebuilt supported workflow with a defined source of truth. | Owner decision; blocks those permission migrations |
| DEC-44 | Building financial timezone | Explicit IANA timezone per building; use a controlled fallback only for legacy records without a setting. | Owner decision; blocks period-boundary implementation |
| DEC-45 | Financial operation authority | Separate deposit confirmation, reversal, high-value adjustment, period reopen, and export duties; use step-up/dual approval for high-risk operations. | Owner decision; blocks finance role matrix |
| DEC-46 | Confirmed deposit correction | Keep confirmed original immutable; correct with a linked reversal and replacement under explicit authority. | Proposed default; confirm before finance command implementation |

These are additive to DEC-01 through DEC-38. The separate PERM/FIN identifiers remain as cross-references in their module documents.

## 10. Source-review evidence added on 2026-11-18

The following read-only source reviews now provide implementation evidence for the existing epics and decision register. They do not close owner decisions or authorize implementation.

| Review artifact | Backlog impact / evidence |
|---|---|
| [EF Core / Live SQL Schema Reconciliation](EF-Core-Live-Schema-Reconciliation-2026-11-18.md) | DATA-01 / DB-01: 30 mapped tables, 313 mapped columns, 27 secondary indexes, and 32 FK relationships match the disposable restore; FullSchema has 119 string-length differences and four missing 2FA columns; wallet view deployment and LoginThrottle ownership remain open. |
| [Financial and Voting Domain Review](Financial-and-Voting-Domain-Review-2026-11-18.md) | FIN-01 / FIN-02 / FIN-03 / POLL-01: historical carry-forward recalculation gaps, order-dependent equal allocation, mutable/deletable financial source rows, current-fee historical recalculation, apartment-keyed votes, and ballot choice disclosure in audit. |
| [Maintenance, Notifications, and Presence Review](Maintenance-Notifications-Presence-Review-2026-11-18.md) | OPS-01 / APP-02: maintenance-to-expense linkage is not durable; notification ownership/read state is apartment-scoped; presence query collapses multiple accounts in one apartment. |
| [Backup, Restore, and Retention Review](Backup-Restore-and-Retention-Review-2026-11-18.md) | OPS-02: in-process restore sessions, no distributed scheduler lease, minute-granularity filenames, stale history after retention, and local-disk storage assumptions. |
| [Security, Audit, and Operations Review](Security-Audit-and-Operations-Review-2026-11-18.md) | SEC-02 / SET-01 / OBS-01 / APP-02: global secrets are echoed to the settings view, PIN migration runs at startup, local key/log storage assumptions, CSP hardening, and append-only audit preservation. |

### Updated execution order

1. Continue read-only source review of remaining integration/media/QR and authorization workflows; do not implement isolated fixes in the legacy aggregate path.
2. Complete cross-document consistency and target domain contracts, using the new source reviews as acceptance requirements for FIN, POLL, OPS, OBS, SET, and OPS-02.
3. Resolve only the owner decisions that block a specific domain contract (DEC-15/16/17/31/32/33/37/38/44/45/46 as applicable); independent source review continues while they remain open.
4. Rehearse any approved schema change against a disposable restore only after explicit authorization. Gate A remains **NOT AUTHORIZED**.

### QR/media source review evidence

[QR Access, DVR, and Media Integration Review — 2026-11-18](QR-DVR-and-Media-Integration-Review-2026-11-18.md) adds concrete acceptance requirements to QR-01, MEDIA-01, AUTH-01, APP-02, and SEC-02: fail-closed QR issuance, bounded/revocable capabilities, account/person-bound access sessions, secret-free DVR DTOs, and an idempotent MediaMTX reconciliation/outbox workflow.

The QR/DVR/media review also confirms that HLS URL discovery authorization is not equivalent to playlist/segment authorization. MEDIA-01 and AUTH-01 acceptance must include MediaMTX-side per-request authorization (or an authenticated HLS gateway), explicit resident camera visibility scope, and reconciliation of DVR desired/observed stream configuration.

## 11. Owner approval of recommended defaults — 2026-11-18

The owner approved the recommended defaults for **DEC-01 through DEC-46** as the binding design baseline. The detailed approved wording and implementation boundaries are recorded in [Owner Approval — Recommended Decision Defaults](Owner-Approval-Recommended-Defaults-2026-11-18.md).

This approval supersedes the prior Open/Proposed labels **for selection of the recommended design default**. It does not erase evidence requirements or implementation blockers:
- Evidence-required decisions remain blocked until evidence is collected (notably identity proof, ownership provenance, legacy permission consumers, deployment topology, and accounting sign-off).
- DEC-15 authorizes preserving current wallet semantics during transition only; it does not approve a new accounting model or finance cutover.
- DEC-32/33 authorize versioned per-poll eligibility configuration, not a universal voter class.
- DEC-38 authorizes operational ballot separation only, not cryptographic unlinkability.
- DEC-28 selects EF Core migrations as authoritative and FullSchema as generated/reference-only; SQL-view deployment reconciliation remains required.
- DEC-29 requires removal/feature-gating of startup PIN mutation before preserved legacy-data runtime validation.
- Gate A DDL remains **NOT AUTHORIZED**. This approval does not authorize EF migrations, schema changes, data backfill, identity merge, credential rewrite, finance mutation, production change, or cutover.

All future design artifacts and acceptance tests must use the approved defaults. Any exception requires a numbered decision change and renewed owner approval.
