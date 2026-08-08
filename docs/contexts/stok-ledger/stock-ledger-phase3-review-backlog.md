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
| 2026-08-08 | P3-S3 | **REQUIRES CHANGES** | Live G-16 P0 adapter + pure classifier accepted for classify-only scope; **Major:** `AllowsSynchronizationPositionAdvance` returns false for `PendingSynchronization`, which conflicts with catch-up sequencing (Scope remains `SynchronizationRequired` when reconcile runs before `CompleteSynchronization`). Fix R-006 before treating P3-S3 complete. R-005 (P3-S2 JournalUpdate) remains a P3-S4 precondition. |
| 2026-08-08 | P3-S2 / P3-S3 remediation | **Implemented — awaiting review** | R-005 compensatory JournalUpdate + R-006 `AllowsMaterialSynchronizationAdvance` coded and unit/integration tested. Status remains awaiting re-review; not marked Resolved. |
| 2026-08-08 | P3-S2 / P3-S3 remediation re-review | **REMEDIATION APPROVED** | R-005 and R-006 verified Resolved. Compensatory JournalUpdate semantics + material-safe advance handoff accepted. P3-S4 may begin; R-001–R-004 remain open per backlog. |

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

---

## R-005

Source:
P3-S2 Review (chronicle; recorded as backlog ID during P3-S3 review)

Status:
Resolved

Target Slice:
P3-S2 (fix before P3-S4)

Priority:
Required

Description:

`LegacySyncDeltaInterpreter.InterpretJournalUpdate` builds absolute current-journal lines and passes them to Domain `StockMovementModel.Correct`. Domain `Correct` records those lines as a new Correction movement consequence — they must be **compensatory deltas** relative to the prior movement, not a restatement of the full current journal quantity. Absolute restatement would double-apply quantity when catch-up persists the ProposedMovement. Non-quantity-only journal material updates must fail closed (Ambiguous), not silently coerce.

Required Action:

Fix JournalUpdate interpretation so Correct/Reverse proposed lines are compensatory; fail closed for non-quantity-only material journal updates. Cover with pure unit tests. Do not start P3-S4 catch-up persistence until this is resolved.

Resolution:

Verified 2026-08-08: `TryBuildCompensatoryCorrectionLine` emits inventory-signed deltas only (e.g. OUT 10→7 ⇒ Inbound 3; IN 100→90 ⇒ Outbound 10); prior movement retained; quantity-neutral updates emit no Correct; direction/valuation (and Receipt Source / location / non-uniform prior) fail closed as Ambiguous. Covered by `LegacySyncDeltaInterpreterTest` compensatory/Ambiguous cases; StockLedgerFeature suite green.

---

## R-006

Source:
P3-S3 Review

Status:
Resolved

Target Slice:
P3-S3

Priority:
Required

Description:

`StockReconciliationPort` correctly overlays `PendingSynchronization` when Scope is `SynchronizationRequired` / `LegacyChangePending` and material quantities match (material inconsistency still wins). However `StockReconciliationClassifier.AllowsSynchronizationPositionAdvance` returns **false** for `PendingSynchronization`. P3-S4 catch-up must reconcile **before** `CompleteSynchronization`, while Scope is still `SynchronizationRequired` — so Port.Reconcile will return `PendingSynchronization` on the success path, and the documented helper would block position advancement even when material quantities are consistent. The Port already guarantees `PendingSynchronization` is only returned when material classification was Balanced/IntentionalDifference.

Required Action:

Separate **material** safe-to-advance from sync-state overlay. Prefer one of: (1) add/adjust a material-only helper that returns true for `Balanced`, `IntentionalDifference`, and `PendingSynchronization` (false for `MaterialInconsistency` / `ProvenanceLimitation`); or (2) document and test that catch-up must call `StockReconciliationClassifier.Classify` (or equivalent material path) for the advance decision, while Port overlay remains for Freshness/operational callers. Update P3-S3 tests and implementation summary so the P3-S4 composition recipe cannot deadlock on `PendingSynchronization`.

Resolution:

Verified 2026-08-08: misleading `AllowsSynchronizationPositionAdvance` replaced by `AllowsMaterialSynchronizationAdvance` (true for Balanced / IntentionalDifference / PendingSynchronization; false for MaterialInconsistency / ProvenanceLimitation). Port still overlays `PendingSynchronization` for sync-required Scope without mutating Scope or advancing position. Covered by classifier + port tests (`SynchronizationRequired` / `LegacyChangePending` ⇒ material-safe; mismatch / ProvenanceLimitation ⇒ not material-safe).
