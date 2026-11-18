# Owner Decision Summary — Items Requiring Product Approval

**Date:** 2026-11-18  
**Branch:** development/database-redesign-enterprise  
**Purpose:** A short decision view of the items that can change core product behavior or deployment policy. The complete register remains in Implementation-Backlog-and-Decision-Register.md. These questions do not block independent design and read-only work.

## 1. Decisions and recommendations

| Ref | Decision needed | Recommended default | Why it matters |
|---|---|---|---|
| DEC-28 | Authoritative SQL deployment path: EF migrations or FullSchema | EF Core migrations authoritative; FullSchema generated/reference-only | Prevents schema/view drift and split deployment truth |
| DEC-15 | What exactly does the apartment wallet represent? | Preserve current legacy semantics during transition; distinguish wallet position from cash/report totals; approve accounting model before cutover | Changes finance calculations and reports |
| DEC-16 | How is opening/carryover balance represented? | Explicit opening ledger entry per apartment/period with source provenance | Prevents hidden balance changes |
| DEC-17 | How is allocation rounding remainder distributed? | Deterministic largest-remainder algorithm with stable Apartment key tie-break | Ensures allocations sum exactly and repeatably |
| DEC-31 | Must ownership shares always total 100%? | Allow explicit incomplete/unknown ownership; require 100% only for verified complete periods | Prevents fabricated ownership percentages |
| DEC-32 / DEC-33 | Who may vote: owner, occupant, tenant, or configurable policy? | Versioned policy per poll; explicitly choose Person vs Apartment subject and snapshot eligibility before opening | Changes eligibility/delegation and ballot schema |
| DEC-38 | What level of ballot secrecy is required? | Operational separation by default; separate cryptographic protocol/security review if unlinkability/certification is required | Determines security design and cost |
| DEC-39 | Does SuperAdmin automatically bypass all permissions? | No unconditional bypass; explicit System grants, with separate grants and fresh MFA for secret access, restore and system maintenance | Prevents over-broad privileged access |
| DEC-40 / DEC-45 | How granular should finance permissions/approvals be? | Separate read, confirm, cancel, adjust, reverse, export, close/reopen; step-up/dual approval for high-risk actions | Supports separation of duties |
| DEC-19 | Who can see maintenance requests? | Apartment-only resident visibility by default; building-wide visibility only for approved categories | Protects resident privacy |
| DEC-35 | Are preferences per apartment or per person? | Keep apartment settings as defaults; add person-level overrides with explicit precedence | Avoids overwriting one occupant's preferences with another's |
| DEC-37 | What are retention/legal-hold rules? | Separate retention for identity, finance, audit, ballot, and operational logs; financial/audit evidence not erased by ordinary account deletion | Affects archival and legal compliance |
| DEC-44 | What timezone defines each building's financial month? | Explicit timezone per building, with controlled legacy fallback | Affects monthly fees, periods, reports and scheduler |

## 2. Other decisions tracked

The complete decision register includes identity proof/merge authority, shared contact handling, Person-without-account rules, ownership evidence, temporary access, legacy permission scopes, MFA rollout, delegation limits, notification/presence semantics, receipt retention, media authorization, backup deletion, audit retention, deployment topology, startup PIN migration, health endpoint exposure, WhatsApp separation, and the fate of missing import/tools/sync workflows.

## 3. How to respond

You can approve the recommended defaults as a group or change any item individually by its reference number. If a decision is not ready, leave it open; implementation of unrelated modules can continue.

No item in this summary authorizes Gate A DDL, EF migrations, backfill, or production changes. Gate A remains NOT AUTHORIZED.
