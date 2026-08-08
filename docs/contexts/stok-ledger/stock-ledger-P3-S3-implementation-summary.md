# Stock Ledger Phase 3 / P3-S3 — Implementation Summary

**Status:** COMPLETE (R-006 Resolved)  
**Date:** 2026-08-08  
**Slice:** P3-S3 — Material reconciliation adapter (G-16 P0)  
**Plan:** [`stock-ledger-phase3-implementation-plan.md`](./stock-ledger-phase3-implementation-plan.md)

---

## Objective

Classify whether a scope’s Ledger representation is **materially consistent** with legacy authority (`tb_stok` remaining quantity vs Ledger Remaining Quantity across all Stock Locations) so that P3-S4 can decide **safe-to-advance vs fail-closed** for Synchronization Position advancement. Classify only — no repair, no Ledger mutation, no position advancement.

---

## Repository inspection summary

| Finding | Implication for P3-S3 |
|---|---|
| `IStockReconciliationPort` + outcome/difference enums already exist (P1-S7) | Implement live adapter; do not change contract |
| `FakeStockReconciliationPort` defaults to Balanced | Keep; no change required |
| No live reconciliation adapter | New Infrastructure `StockReconciliationPort` |
| `LegacyStockReadPort` (G-05) already lists balances by Item + Receipt Source | Reuse for legacy remaining totals |
| `StockLayerDal.ListData` is write-scope only (Item+DO+Location) | Extend with `ListByLedgerScope` for cross-location read |
| P3-S1 thin-adapter + pure classifier pattern | Mirror for reconciliation |
| Depleted-layer intentional difference proven in P2-S4 / P2-S8 | Reuse fixture shape for reconcile tests |
| P3-S2 interpreter is independent | Do not compose in P3-S3 |

---

## Review backlog items assigned to this slice

| Item | Target | Priority | Disposition |
|---|---|---|---|
| R-006 | P3-S3 | Required | **Resolved** (material-safe-advance helper re-reviewed) |
| R-001 | P3-S4 | Required | **Deferred** — not P3-S3 |
| R-002 | P3-S4 | Required | **Deferred** — not P3-S3 |
| R-003 | P3-S4 | Recommended | **Deferred** — catch-up intent application |
| R-004 | P3-S4 | Recommended | **Deferred** — synthetic sync idempotency keys |

---

## What was implemented

| Area | Deliverable |
|---|---|
| Application | `StockReconciliationClassifier` — pure location-level remaining-quantity compare; outcomes `Balanced` / `IntentionalDifference` / `MaterialInconsistency`; `AllowsMaterialSynchronizationAdvance` helper for P3-S4 (R-006) |
| Infrastructure | `IStockLayerDal.ListByLedgerScope` — read-only layers for Item + Receipt Source across locations |
| Infrastructure | `StockReconciliationPort` — live `IStockReconciliationPort`; overlays `PendingSynchronization` / `ProvenanceLimitation` from Scope state |
| Tests | `StockReconciliationClassifierTest` — 7 pure unit tests |
| Tests | `StockReconciliationPortTest` — 6 disposable-DB integration tests |

**Explicitly not implemented (later slices):** catch-up MediatR / TX / position advancement (P3-S4), Freshness Gate (P3-S5), auto-repair, valuation reconciliation, G-23 harness activation (P3-S7), production DI.

### Classification rules delivered

| Condition | Outcome |
|---|---|
| Scope remaining totals match; no differences | `Balanced` (safe to advance) |
| Totals match; only depleted layer vs absent `tb_stok` | `IntentionalDifference` (safe to advance; BR-STL-080/110) |
| Any location quantity mismatch / missing ledger layers | `MaterialInconsistency` (not safe) |
| Material OK + Scope `SynchronizationRequired` / `LegacyChangePending` | `PendingSynchronization` (lifecycle overlay; **material-safe** for catch-up completion — R-006) |
| Scope missing or not `Reconstructed` | `ProvenanceLimitation` (not material-safe) |

Material inconsistency always wins over pending-sync overlay. P0 compares **remaining quantity only** (no valuation).

---

## Repository decisions made

| Decision | Rationale |
|---|---|
| Pure Application classifier + thin Infrastructure port | Matches P3-S1 pattern; keeps classification SQL-free and unit-testable |
| Extend `IStockLayerDal` instead of new repo | Minimal additive read; reuse existing table/DTO |
| `AllowsMaterialSynchronizationAdvance` on classifier | Explicit **material** P3-S4 handoff; includes `PendingSynchronization` so catch-up can complete while Scope is still sync-required |
| Quantity-only P0 | Avoids false-positive hard blockers from valuation/row-shape equality (plan risk) |
| No production DI | Consistent with Phase 3 plan — test/internal invocation only |
| Integration tests use `FakeLegacyStockReadPort` + live Ledger DAL | Proves adapter against reconstructed Ledger layers without depending on live `tb_stok` seeding for every case |

---

## Deviations from the implementation plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Illustrative class name `StockReconciliationPort` | Used as implemented | None |
| Optional thin Application façade | Pure static classifier instead of façade service | Better: no speculative DI; same observability |

No architectural reopen. Locked decisions preserved.

---

## Review backlog resolution

| Item | Status |
|---|---|
| R-006 (P3-S3) | **Resolved** |
| R-001 (P3-S4) | **Not resolved** — correctly deferred |
| R-002 (P3-S4) | **Not resolved** — correctly deferred |
| R-003 (P3-S4) | **Not resolved** — correctly deferred |
| R-004 (P3-S4) | **Not resolved** — correctly deferred |

---

## Remediation — R-006 (2026-08-08)

### Problem

Port correctly overlays `PendingSynchronization` when Scope is `SynchronizationRequired` / `LegacyChangePending` and material quantities match. The old helper `AllowsSynchronizationPositionAdvance` returned **false** for that outcome, which would deadlock P3-S4 (reconcile runs before `CompleteSynchronization`).

### Fix

| Area | Change |
|---|---|
| `StockReconciliationClassifier` | Replaced with `AllowsMaterialSynchronizationAdvance` — material safety only |
| Material-safe outcomes | `Balanced`, `IntentionalDifference`, `PendingSynchronization` → true |
| Material-unsafe outcomes | `MaterialInconsistency`, `ProvenanceLimitation` → false |
| Port overlay | Unchanged — still returns `PendingSynchronization` for sync-required Scope when material matched |
| Scope mutation / position advance | Still none in P3-S3 |

### Tests added/changed

- Scope `SynchronizationRequired` + matching qty ⇒ `PendingSynchronization` + material-safe = true
- Scope `LegacyChangePending` + matching qty ⇒ `PendingSynchronization` + material-safe = true
- Material mismatch ⇒ `MaterialInconsistency` + material-safe = false
- `ProvenanceLimitation` ⇒ material-safe = false
- `Balanced` / `IntentionalDifference` ⇒ material-safe = true
- Read-only reconcile side-effect test remains green

### Repository decision

Separate **material reconciliation safety** from **sync lifecycle state**. P3-S4 must call `AllowsMaterialSynchronizationAdvance(portResult)` after `Reconcile`, not treat `PendingSynchronization` as a material failure. Do not remove the Port overlay.

### Test results (post-remediation)

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockReconciliation` | **17 passed**, 0 failed |
| `StockLedgerFeature` (single-threaded) | **197 passed**, 7 skipped, 0 failed |
| Solution build | Succeeded |

---

## Test results (original slice delivery)

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockReconciliation` | **14 passed**, 0 failed (pre-R-006) |
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` (single-threaded) | **189 passed**, 7 skipped, 0 failed |
| Solution build | Succeeded |
| Full `StockLedgerFeature` filter under parallel load | Pre-existing `devTest` deadlocks (same note as P3-S1/S2); **not caused by P3-S3** |

Covered scenarios: multi-location balanced; depleted-layer intentional difference; per-location qty mismatch; ledger-active with no legacy row; legacy with no ledger layers; scope total mismatch; determinism; post-reconstruction intentional/balanced; SQL-tampered mismatch; read-only side effects; `PendingSynchronization` (material-safe); `ProvenanceLimitation` (unreconstructed + missing scope).

---

## Known limitations

1. Valuation / cost / ED / batch drift is not classified as `ValuationMismatch` in P0 — quantity-only; Phase 8 residual.
2. Journal-level differences (`MissingLegacyJournal`) are not emitted by this slice; remaining-quantity compare is the advance gate.
3. Integration fixtures use fake legacy balances with live Ledger persistence (same pattern as reconstruction handler tests).
4. No production DI / HTTP (Phase 3 plan).

---

## Acceptance checklist

| Criterion | Status |
|---|---|
| Live `IStockReconciliationPort` classifies remaining quantity | **PASS** |
| Depleted-layer intentional difference ⇒ Balanced or IntentionalDifference (not MaterialInconsistency) | **PASS** |
| Real qty mismatch ⇒ MaterialInconsistency | **PASS** |
| No movement / Scope mutation side effects | **PASS** |
| Optional `PendingSynchronization` when Scope requires sync | **PASS** |
| Safe-to-advance vs not-safe-to-advance decideable by P3-S4 | **PASS** (`AllowsMaterialSynchronizationAdvance`) |
| No repair / catch-up / position advance / Freshness Gate | **PASS** |

---

## Files changed

| Path | Change |
|---|---|
| `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/StockReconciliationClassifier.cs` | Added; R-006 renamed helper to `AllowsMaterialSynchronizationAdvance` |
| `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockReconciliationPort.cs` | Added; explanation clarified for PendingSynchronization overlay |
| `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockLayerDal.cs` | Extended `ListByLedgerScope` |
| `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockReconciliationClassifierTest.cs` | Added; R-006 material-safe cases |
| `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockReconciliationPortTest.cs` | Added; R-006 PendingSynchronization + LegacyChangePending |
| `docs/contexts/stok-ledger/stock-ledger-phase3-implementation-plan.md` | Slice progress |
| `docs/ARTIFACTS.md` | Registry entry |
| `docs/contexts/stok-ledger/stock-ledger-P3-S3-implementation-summary.md` | Added |

---

## Next slice readiness

**Ready for P3-S4** (R-006 Resolved; R-005 also Resolved). Catch-up orchestration should compose:

```text
discover → interpret → short TX apply → StockReconciliationPort.Reconcile(scope)
→ complete Synchronization Position ONLY if StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result)
```

P3-S4 must still resolve review backlog R-001 / R-002 before relying on set-diff post-reconstruction, and keep the sync handler thin (R-003 / R-004).
