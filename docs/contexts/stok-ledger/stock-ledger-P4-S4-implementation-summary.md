# Stock Ledger Phase 4 / P4-S4 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-08  
**Slice:** P4-S4 — Receipt void/correction  
**Plan:** [`stock-ledger-phase4-implementation-plan.md`](./stock-ledger-phase4-implementation-plan.md)  
**Dependencies:** P4-S1–S3 COMPLETE

---

## Objective

Answer: how should receipt void/correction be represented so Stock Ledger history stays immutable, legacy-compatible authoritative records remain correct, Remaining Quantity and valuation stay coherent, and retries are idempotent — without treating void as deleting the original Native movement?

---

## Verdict

**PASS.** Native DO Receipt void creates an accountable `Reversal` movement (original Receipt retained), depletes layers to zero while retaining depleted layers, applies compensating `DO_V` + `tb_stok` DELETE through the live writer inside the existing consequence UoW, refreshes `fingerprint-v1`, and fail-closes on partial consumption / capability-off / missing original. `xVoidDelete=True` physical journal delete is explicitly deferred.

---

## What was implemented

| Area | Deliverable |
|---|---|
| Domain | `StockLedgerScopeStateModel.RefreshSynchronizationPosition` — Reconstructed + Current → Current with new opaque position |
| Idempotency | `DoReceiptVoidConsequenceIdempotency` → `DO_VOID\|{BrgId}\|{ReceiptSourceId}\|{VoidSourceTransactionId}` |
| Mapper | `DoReceiptVoidLegacyCompatibilityMapper` — `Reversal` + `DO_V` + `Delete` balance targets; post-void fingerprint projection |
| UseCase | `VoidDoReceiptStockConsequenceCommand` / `VoidDoReceiptStockConsequenceHandler` |
| Writer | `LegacyCompatibilityWriterPort` Reversal branch — UPDLOCK target ST row, DELETE-on-zero / UPDATE partial, INSERT `DO_V` |
| Capability | Same `StockLedgerDoReceiptOptions.Enabled` (default **false**) gates void |
| Tests | `VoidDoReceiptStockConsequenceHandlerTest` (6) + writer void characterization + Domain UT17/UT18 |
| Docs | This summary + plan progress + ARTIFACTS + roadmap note |

**Explicitly not implemented:** `xVoidDelete=True` journal DELETE mode; `Correct()` UseCase for DM; generic multi-FO void engine; production DI/HTTP; capability enablement; P4-S5 coexistence scenarios.

---

## Handler outcomes

| Outcome | Meaning |
|---|---|
| `Committed` | Reversal + depleted positions + legacy `DO_V`/balance delete + fingerprint reconcile |
| `AlreadyCommitted` | Void SourceConsequence key hit — quantity-neutral |
| `Disabled` | Capability off — zero writes |
| `NotEligible` | Scope not Reconstructed; original Native Receipt missing; already fully voided (`DO_V` covers DO); journal/line mismatch |
| `InsufficientStock` | Layer remaining < initial (partial consumption) or legacy balance insufficient |
| `Inconsistent` / `StaleOrNotCurrent` | Freshness Gate fail-closed |

---

## Void algorithm (normative)

```text
Capability + void idempotency
→ Resolve original post SourceConsequence + Native Receipt movement
→ Freshness Gate (Reconstructed scopes)
→ Fail closed if any layer RemainingQuantity != InitialQuantity
→ Pair legacy ST rows 1:1 to receipt lines (layanan + qty + ED/batch; unique fallback)
→ Reverse(original) + deplete layers (history retained)
→ Map DO_V Delete write → UoW Commit (idempotency → movement → positions → scope → legacy Apply)
→ Post-commit fingerprint reconcile from live LegacyStockReadPort (fingerprint-v1)
```

Default legacy mode: **compensating `DO_V`** (`xVoidDelete=False`). Original `DO` journals remain; balances deleted when qty reaches zero.

---

## Repository decisions

| Decision | Rationale |
|---|---|
| Prefer `Reverse` over `Correct` for DM void | Whole-DO void is a full quantity reverse; Correct deferred |
| Same capability flag as post | Plan acceptance: capability disabled blocks void; no second enablement surface |
| Writer targets Application-supplied `ST*` id | No silent FIFO re-selection; fail closed if row missing/insufficient |
| Whole-DO void (not per-journal R-007) | Native void reverses the original Receipt movement; R-007 remains sync-path guidance |
| Post-commit FP reconcile via `CommitSyncEvidence` (`…\|FP`) | UoW still persists Scope before legacy Apply; live read-back after commit guarantees discovery-compatible `fingerprint-v1` |
| Defer `xVoidDelete=True` | Not required for DM characterization in Phase 4; Phase 3 sync already deletion-aware if needed later |

---

## Deviations from the Phase 4 plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Fingerprint refresh only from pre-commit projection | Pre-commit projection kept for UoW Scope write; **reconcile to live read-back after Commit** when opaque differs | Safer for Phase 3 discovery; documents short second TX for Scope-only FP correction |
| Optional `xVoidDelete` with tests | Deferred fail-closed | Explicit in summary; no unsafe delete mode |

---

## Validation evidence

Focused filter (disposable `devTest` only):

```text
dotnet test --filter FullyQualifiedName~VoidDoReceipt|FullyQualifiedName~LegacyCompatibilityWriterPortTest|FullyQualifiedName~StockLedgerScopeStateTest|FullyQualifiedName~PostDoReceiptStockConsequenceHandlerTest
Passed!  - Failed: 0, Passed: 46, Skipped: 0
```

| Coverage | Result |
|---|---|
| Void after receipt — zero remaining; original retained; `DO_V` visible | Pass |
| Idempotent void retry | Pass |
| Capability disabled blocks void | Pass |
| Partial layer consume → InsufficientStock | Pass |
| Multi-line whole-DO void | Pass |
| Fingerprint advances with `fingerprint-v1` | Pass |
| Writer void + rollback characterization | Pass |
| Prior post suite regression | Pass |

---

## Residual risks / handoff to P4-S5

| Item | Note for next slice |
|---|---|
| **Post-void coexistence** | Scope is `Reconstructed` + `Current` with live fingerprint; later VB6-shaped activity should be Phase 3 discoverable — prove in P4-S5 |
| **AlternatingWriters** | Still skipped — P4-S5 fixture ownership |
| **`…\|FP` SyncBatch key** | Quantity-neutral fingerprint reconcile evidence; discovery ignores non-`SYNC\|…` keys (prefix is `DO_VOID|…|FP`) |
| **Partial consumption** | Fail-closed; not a production mixed-writer proof (FQ-06 → Phase 9) |
| **FQ-06 / production G-17** | Explicitly **not** claimed |
| **Capability** | Keep `StockLedgerDoReceipt:Enabled` default **false**; no production DI/HTTP |
| **G-11 void** | Consumer-dependent `DO_V` + delete-on-zero proven on disposable DB for Native-posted receipts |

**Ready for P4-S5:** yes.

---

## Rollback / containment

Keep capability false. Disable/stop composing `VoidDoReceiptStockConsequenceHandler`. Post path and P4-S1–S3 remain intact. Disposable-DB only for mutation tests.
