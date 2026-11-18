# Owner Approval — Recommended Decision Defaults (2026-11-18)

**Branch:** `development/database-redesign-enterprise`  
**Approval:** The project owner approved the recommended defaults for DEC-01 through DEC-46 as the baseline product and architecture direction.  
**Effective scope:** Design documents, domain contracts, acceptance criteria, future implementation plans, and test expectations.  
**Not authorized:** This approval does not authorize Gate A DDL, EF migrations, schema changes, data backfill, identity merges, credential rewrites, financial mutations, production changes, or cutover. Each implementation gate remains separate.

## Approved defaults

| Decision | Approved default |
|---|---|
| DEC-01 | Never auto-merge human identities. A designated reviewer approves a merge only with evidence and durable audit. |
| DEC-02 | Shared phone/email is a shared-contact candidate, never proof that two records are the same person. |
| DEC-03 | A Person may exist and own/occupy/access resources without an interactive account. Notice delivery must use an explicitly configured recipient route. |
| DEC-04 | Zero or one interactive account per Person; enforce transactionally and with a unique invariant. |
| DEC-05 | Ownership requires source evidence, a 0–100 share, effective dates, and an explicit completeness/total policy. Never invent missing ownership evidence. |
| DEC-06 | At most one active primary occupant per apartment at an instant; an apartment may have no primary occupant. |
| DEC-07 | Temporary/emergency access requires an explicit dated grant with issuer, reason, scope, and revocation. |
| DEC-08 | Classify every legacy permission by actual resource consumer. Ambiguous grants remain unresolved and are not migrated. |
| DEC-09 | SuperAdmin is a System-scoped role assignment on an ordinary account, not a separate identity type. |
| DEC-10 | Admin and SuperAdmin MFA is mandatory, with staged enrollment, recovery, bootstrap, and monitored exceptions. |
| DEC-11 | Resident MFA is optional, with safe recovery. |
| DEC-12 | Person is the default voting subject. A poll may explicitly choose Apartment subject; that requires a valid representative/delegation per apartment and a snapshotted eligibility policy. |
| DEC-13 | Separate voter eligibility/nullifier from anonymous ballot choice; no direct join key between identity and choice. |
| DEC-14 | Preserve immutable ballot evidence. Corrections use a documented annul/re-poll process; never edit an individual vote. |
| DEC-15 | Preserve legacy apartment-wallet semantics during transition. Distinguish wallet position from cash/report totals; no new accounting interpretation or finance cutover until the accounting owner signs off on the ledger model. |
| DEC-16 | Represent opening/carryover balances as explicit, provenance-bearing opening ledger entries per apartment and period. Closed periods are corrected by reversal/adjustment, not silent rewrite. |
| DEC-17 | Use deterministic largest-remainder allocation with a stable Apartment key tie-break so allocations sum exactly and are independent of query order. |
| DEC-18 | Posted financial entries are reversed/voided, never physically deleted through ordinary workflows. |
| DEC-19 | Resident maintenance visibility is apartment/resource-scoped by default. Building-wide visibility requires an explicitly approved category. |
| DEC-20 | Notifications are addressed to a Person/account by default. Apartment/building recipients are explicit broadcast audiences; each individual recipient has independent read state. |
| DEC-21 | Canonical presence is account/session/device-based. Person, apartment, and building counts are separate derived metrics and must be labeled by subject type. Storage failure is an Unknown/Degraded state, not zero. |
| DEC-22 | Receipts use private object storage, short-lived authorized downloads, and documented retention/legal-hold policy. |
| DEC-23 | OCR is advisory extraction only and never confirms or posts a payment. |
| DEC-24 | Camera authorization is enforced both in the application and at the media server/HLS gateway. No direct credential-bearing DTOs. |
| DEC-25 | Backup artifact and history are reconciled with typed Available/Missing/Deleted/Failed states; deletion is an audited operator action and does not erase history evidence. |
| DEC-26 | Separate security/business audit from diagnostic logs. Define independent retention, tamper-evidence, legal hold, and privileged-access rules. |
| DEC-27 | Treat current deployment as single-instance until deployment evidence proves otherwise; design interfaces for future shared state and scale-out without claiming multi-instance readiness. |
| DEC-28 | EF Core migrations are the authoritative SQL deployment path. FullSchema is generated/reference-only. Reconcile and provision SQL views through the migration-owned deployment path. |
| DEC-29 | Remove or feature-gate automatic startup PIN mutation. Credential migration requires dry-run, checkpoint, evidence, and operator-approved execution. |
| DEC-30 | Minimal liveness is available only to trusted infrastructure. Readiness details are restricted to the monitoring network; do not expose diagnostics publicly. |
| DEC-31 | Allow explicitly incomplete/unknown ownership periods. Require exactly 100% only for periods declared complete and legally verified; never normalize partial shares to 100%. |
| DEC-32 | Eligibility is a versioned policy per poll. Person-mode polls explicitly select owner, occupant, or another approved class; tenant eligibility is never implicit. |
| DEC-33 | Apartment-mode eligibility explicitly defines whether owners, occupants, or an approved combination qualify; snapshot the rule before poll opening. |
| DEC-34 | One representative may represent multiple apartments only with a distinct valid delegation for each apartment and when that poll's policy permits it. |
| DEC-35 | Existing apartment-level preferences remain defaults. Person-level overrides are additive, with explicit precedence and consent semantics. |
| DEC-36 | Never silently carry plaintext legacy PINs. Reuse legacy verifiers only when verifier and keys are proven; otherwise require reset/re-enrollment. Password, PIN, and external-provider sign-in remain separately configurable during transition. |
| DEC-37 | Separate retention/legal-hold policies for identity evidence, finance, audit, ballots, buildings, and operational logs. Ordinary account/building deletion never erases required financial/audit evidence. |
| DEC-38 | Default to operational ballot separation with documented metadata-risk controls. Do not claim cryptographic unlinkability or certification without a separate protocol/security review. |
| DEC-39 | SuperAdmin has no unconditional permission bypass. Use explicit System grants and separate grants plus fresh MFA for secrets, restore, global secret operations, and system maintenance. |
| DEC-40 | Split finance permissions into read, deposit confirm/cancel, adjustment, expense/revenue post/reverse, export, and period close/reopen. |
| DEC-41 | `audit.view.own` means the actor's own Person/account events. Apartment-wide visibility requires a separate Apartment-scope permission. |
| DEC-42 | Separate WhatsApp message dispatch from provider configuration and secret management. |
| DEC-43 | Do not migrate permissions for absent import/tools/sync workflows. Retire stale catalog entries or rebuild a supported workflow with an explicit source of truth before granting access. |
| DEC-44 | Store an explicit IANA timezone per building for financial periods. Use a controlled, documented fallback only for legacy buildings without a configured timezone. |
| DEC-45 | Separate deposit confirmation, reversal, high-value adjustment, period reopen, and export duties. Apply step-up authentication and dual approval to high-risk actions. |
| DEC-46 | A confirmed deposit is immutable. Correct it with an explicitly authorized, linked reversal and replacement. |

## Implementation constraints retained after approval

1. These defaults are binding for design and future implementation planning; any deviation requires a numbered decision change and owner approval.
2. Evidence-dependent items remain blocked until the required source evidence is collected. Approval does not fabricate identity proof, ownership evidence, permission consumers, deployment topology, or accounting semantics.
3. DEC-15 approves preservation of current wallet meaning during transition, not an unreviewed new accounting model.
4. DEC-32/33 approve configurable, versioned eligibility, not one universal owner/tenant rule for every poll.
5. DEC-38 approves operational separation only; no claim of cryptographic unlinkability is made.
6. Gate A remains **NOT AUTHORIZED**. No DDL, EF migration, backfill, or cutover is authorized by this record.
