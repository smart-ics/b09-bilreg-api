# Stock Ledger Phase 3 Review Backlog

## Purpose

This document tracks cross-slice engineering findings that must be remembered during the remaining Phase 3 implementation.

It is **not** a bug list.

It is **not** a TODO list.

It contains only implementation-review findings that affect future slices.

---

## Review chronicle

| Date | Slice reviewed | Decision | Notes |
|---|---|---|---|
| 2026-08-08 | P3-S1 | Gate PASS (prior review) | R-001 / R-002 opened for P3-S4 |
| 2026-08-08 | P3-S2 | **REQUIRES CHANGES** | Pure interpreter structure accepted; **JournalUpdate → Correct must emit compensatory delta lines** (Domain `Correct` convention), and non-quantity-only journal material updates must fail closed. Fix before treating P3-S2 complete / before P3-S4 consumes intents. P3-S3 may proceed (does not compose the interpreter). |

---

## R-001

Source:
P3-S1 Review

Status:
Open

Target Slice:
P3-S4

Priority:
Required

Description:

Baseline reconstruction does not yet establish the complete Legacy Change Discovery identity-key set. Until that set exists, fingerprint mismatch correctly fails closed to scoped re-derive and cannot rely on set-diff for void/update classification.

Required Action:

Ensure the first synchronization establishes the complete baseline identity set before relying on set-diff for incremental discovery.

Resolution:

—

---

## R-002

Source:
P3-S1 Review

Status:
Open

Target Slice:
P3-S4

Priority:
Required

Description:

Discovery identity keys embed fingerprint material into `BILRG_StokSourceIdempotency.IdempotencyKey`, which is currently sized for shorter keys. Catch-up persistence of the P3-S1 key format risks truncation or insert failure at production field widths.

Required Action:

Confirm durable storage capacity for the discovery identity-key format before catch-up writes those keys, and adjust persistence only if capacity is insufficient.

Resolution:

—

---

## R-003

Source:
P3-S2 Review

Status:
Open

Target Slice:
P3-S4

Priority:
Recommended

Description:

`LegacySyncIntentType` encodes catch-up application contracts that must not be re-invented in the orchestration handler: `AdjustLayerRemainingQuantity` targets `TargetLayerId` and must not rewrite Stock Layer establishment origin; `RepresentationalBalanceOmission` is quantity-neutral (retain depleted layer; never erase history); `ProposedMovement` lines are Domain movement consequences to apply (after P3-S2 JournalUpdate fix: compensatory Correct/Reverse lines, not absolute restatements of prior receipts).

Required Action:

When composing catch-up persistence, apply intents by kind using the fields above; keep business meaning in the interpreter + Domain factories, not duplicated in the handler.

Resolution:

—

---

## R-004

Source:
P3-S2 Review

Status:
Open

Target Slice:
P3-S4

Priority:
Recommended

Description:

Some interpreter `SyncIdempotencyKey` values are synthetic and are **not** material snapshots parseable by `LegacyChangeDiscoveryIdentityKeys.TryParseJournalKey` / `TryParseBalanceKey` — notably void keys (`…|VOID|…`) and depleted-balance omission keys (`…|OMISSION`). Treating every sync idempotency key as a P3-S1 material encoding would mis-classify or reject valid intents.

Required Action:

Persist synthetic keys as opaque SyncBatch idempotency identities; do not require TryParse success for void/omission intents. Continue to honor R-002 for key width on all formats (including synthetic).

Resolution:

—
