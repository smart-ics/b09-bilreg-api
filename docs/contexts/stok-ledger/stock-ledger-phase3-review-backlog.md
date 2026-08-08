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
| 2026-08-08 | P3-S4 | **Implemented — awaiting review** | Incremental catch-up + position advancement; R-001–R-004 coded and tested. Status remains awaiting re-review; not marked Resolved. |
| 2026-08-08 | P3-S4 | **REQUIRES CHANGES** | Thin orchestration + G-14/G-15 happy path accepted; R-001–R-004 verified Resolved for stated actions. **Major:** R-001 bootstrap anchors every journal identity to the aggregate reconstruction movement, so `JournalVoidDelete` → `Reverse` can over-apply across multi-line / multi-location baselines and commit incorrect layer depletions in short TXs before reconcile fails closed (R-007). Do not start P3-S5 until R-007 is fixed. |
| 2026-08-08 | P3-S4 R-007 remediation | **Implemented — awaiting review** | Option A fail-closed void target guard coded and tested (`TryResolveSafeVoidTarget`); multi-location sibling Remaining Quantity unchanged; single-location void happy path hardened. Status remains awaiting re-review; not marked Resolved. |
| 2026-08-08 | P3-S4 R-007 remediation re-review | **APPROVED** | R-007 verified Resolved. `TryResolveSafeVoidTarget` fail-closes unsafe aggregate voids before PersistIntents; multi-location sibling qty unchanged; 1:1 void Synchronized + Reversal hardened. P3-S4 complete; P3-S5 may begin. No new backlog items. |
| 2026-08-08 | P3-S5 | **APPROVED** | Legacy Freshness Gate (G-12) accepted: discovery fast-path + at-most-once catch-up; fail-closed `StaleOrNotCurrent` / `Inconsistent`; no Authority Gate / no legacy mutation; no P3-S6+ scope. No backlog items targeted P3-S5. P3-S6 may begin. No new backlog items. |
| 2026-08-08 | P3-S6 | **APPROVED** | Sync-specific bounded retry (`MaxSyncConflictRetries = 3`) + `SynchronizationClaimService` claim/resume; crash-before-finalize retains prior position until resume completes; concurrent catch-up quantity-neutral via SyncBatch + CompleteSynchronization OCC; Freshness Gate still one catch-up call with internal retries; FQ-06 / G-17 production non-claim documented; Phase-4 caller contract documented only. No backlog items targeted P3-S6. No new backlog items. Known allowResume co-apply window is SyncBatch/OCC-safe and deferred (no durable claim-token columns). P3-S7 may begin. |

---

## R-001

Source:
P3-S1 Review

Status:
Resolved

Target Slice:
P3-S4

Priority:
Required

Description:

Baseline reconstruction does not yet establish the complete Legacy Change Discovery identity-key set. Until that set exists, fingerprint mismatch correctly fails closed to scoped re-derive and cannot rely on set-diff for void/update classification.

Required Action:

Ensure the first synchronization establishes the complete baseline identity set before relying on set-diff for incremental discovery.

Resolution:

Verified 2026-08-08: `LegacySyncIdentityBootstrapper` persists complete `SYNC|BUKU|…` / `SYNC|STOK|…` SyncBatch keys for surviving journals/balances on first sync (Unchanged-without-keys or sole `RequiresScopedReDerive`). Covered by `SynchronizeStockLedgerScopeHandlerTest.FirstSynchronization_BootstrapsDiscoveryIdentityKeys_R001`. Related residual void-anchor over-apply tracked separately as R-007.

---

## R-002

Source:
P3-S1 Review

Status:
Resolved

Target Slice:
P3-S4

Priority:
Required

Description:

Discovery identity keys embed fingerprint material into `BILRG_StokSourceIdempotency.IdempotencyKey`, which is currently sized for shorter keys. Catch-up persistence of the P3-S1 key format risks truncation or insert failure at production field widths.

Required Action:

Confirm durable storage capacity for the discovery identity-key format before catch-up writes those keys, and adjust persistence only if capacity is insufficient.

Resolution:

Verified 2026-08-08: schema-max journal key ≈ 180 chars; `IdempotencyKey` widened to `VARCHAR(400)` (create script + `BILRG_StokSourceIdempotency.AlterIdempotencyKey.sql` with index drop/recreate); fixture applies alter; boundary persistence covered by `MaxLengthDiscoveryIdentityKey_PersistsSuccessfully_R002`.

---

## R-003

Source:
P3-S2 Review

Status:
Resolved

Target Slice:
P3-S4

Priority:
Recommended

Description:

`LegacySyncIntentType` encodes catch-up application contracts that must not be re-invented in the orchestration handler: `AdjustLayerRemainingQuantity` targets `TargetLayerId` and must not rewrite Stock Layer establishment origin; `RepresentationalBalanceOmission` is quantity-neutral (retain depleted layer; never erase history); `ProposedMovement` lines are Domain movement consequences to apply (after P3-S2 JournalUpdate fix: compensatory Correct/Reverse lines, not absolute restatements of prior receipts).

Required Action:

When composing catch-up persistence, apply intents by kind using the fields above; keep business meaning in the interpreter + Domain factories, not duplicated in the handler.

Resolution:

Verified 2026-08-08: `LegacySyncIntentApplicator` projects intents by kind; layer origin preserved on quantity adjust; omission is quantity-neutral. Covered by applicator unit tests + `BalanceUpdate_PreservesLayerEstablishmentOrigin_R003`. Handler composition hygiene (Insert+BalanceUpdate / Reverse+Adjust) is mechanical double-count prevention, not re-interpretation of delta meaning.

---

## R-004

Source:
P3-S2 Review

Status:
Resolved

Target Slice:
P3-S4

Priority:
Recommended

Description:

Some interpreter `SyncIdempotencyKey` values are synthetic and are **not** material snapshots parseable by `LegacyChangeDiscoveryIdentityKeys.TryParseJournalKey` / `TryParseBalanceKey` — notably void keys (`…|VOID|…`) and depleted-balance omission keys (`…|OMISSION`). Treating every sync idempotency key as a P3-S1 material encoding would mis-classify or reject valid intents.

Required Action:

Persist synthetic keys as opaque SyncBatch idempotency identities; do not require TryParse success for void/omission intents. Continue to honor R-002 for key width on all formats (including synthetic).

Resolution:

Verified 2026-08-08: `CommitSyncEvidence` / SyncBatch persist synthetic keys without parsing; `LegacySyncLedgerSnapshotLoader` parses material keys only (TryParse failure ⇒ ignored for anchors). Covered by `SyntheticVoidAndOmissionKeys_PersistOpaque_WithoutTryParse_R004`.

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

Verified 2026-08-08: misleading `AllowsSynchronizationPositionAdvance` replaced by `AllowsMaterialSynchronizationAdvance` (true for Balanced / IntentionalDifference / PendingSynchronization; false for MaterialInconsistency / ProvenanceLimitation). Port still overlays `PendingSynchronization` for sync-required Scope without mutating Scope or advancing position. Covered by classifier + port tests (`SynchronizationRequired` / `LegacyChangePending` ⇒ material-safe; mismatch / ProvenanceLimitation ⇒ not material-safe). Catch-up composition verified in `PendingSynchronization_MaterialSafe_CompletesSynchronization_R006`.

---

## R-007

Source:
P3-S4 Review

Status:
Resolved

Target Slice:
P3-S4

Priority:
Required

Description:

R-001 bootstrap (and snapshot anchors) link every surviving journal identity to the single aggregate reconstruction `StockMovementId`. `LegacySyncDeltaInterpreter.InterpretJournalVoidDelete` then calls `prior.Reverse(...)`, which reverses **all** lines of that movement. For multi-line / multi-location reconstructed baselines, voiding one journal therefore proposes (and P3-S4 short-TX persistence can commit) layer depletions at unaffected locations before material reconcile fails closed and marks Scope `Inconsistent`. Position is retained, but Ledger Remaining Quantity at sibling locations is already wrong. JournalUpdate already fail-closes non-uniform prior lines via `TryResolvePriorSingleDirection`; JournalVoidDelete has no equivalent guard. The integration void test only asserts history retention and soft-accepts either Synchronized or Inconsistent.

Required Action:

Before applying a void reversal against an aggregate reconstruction movement, fail closed unless the prior movement is a safe 1:1 void target for the voided journal identity (e.g. single uniform line at the voided `LayananId`, or an equivalent location-scoped compensatory correction — not a full multi-line `Reverse`). Do not persist over-broad Reverse consequences. Cover with a multi-location / multi-line reconstructed baseline void integration test that proves sibling-location Remaining Quantity is unchanged and Scope fails closed without incorrect depletion. Strengthen the existing single-location void test to require Synchronized + reversal movement when the 1:1 path is safe.

Resolution:

Verified 2026-08-08: Option A — `TryResolveSafeVoidTarget` allows `Reverse` only when the prior Movement has exactly one line total and that line is at the voided `LayananId`; otherwise interpreter returns Ambiguous with empty intents before any PersistIntents. Handler marks Scope `Inconsistent` and retains prior Synchronization Position. Multi-location void leaves sibling Remaining Quantity unchanged and persists no Reversal; single-location 1:1 void requires Synchronized + one Reversal + depleted layer. Covered by `JournalVoidDelete_MultiLinePrior_ReturnsAmbiguous_R007`, `JournalVoid_MultiLocationBaseline_FailsClosed_SiblingQuantityUnchanged_R007`, and strengthened `JournalVoid_RetainsHistory_AndCreatesReversal`. Interpreter+handler suites green (35 passed). Intentional residual: precise journal→line Correct for multi-line aggregates remains future work when identity targeting exists — fail-closed is the approved production behavior for current anchors.
