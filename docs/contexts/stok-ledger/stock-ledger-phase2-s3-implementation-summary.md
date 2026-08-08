# Stock Ledger Phase 2 / P2-S3 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P2-S3 — Domain readiness for reconstructed baseline facts  
**Plan:** [`stock-ledger-phase2-implementation-plan.md`](./stock-ledger-phase2-implementation-plan.md)  
**Prior foundation:** [`stock-ledger-phase2-s2-implementation-summary.md`](./stock-ledger-phase2-s2-implementation-summary.md); Phase 1 Movement / Layer / Position / Scope (P1-S2–S4)  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS; BR-STL-065–069  

---

## WHAT

Domain-only readiness so later slices can express a balanced reconstructed baseline (Movements, Layers, Positions, Source Transaction Reference, idempotency) with `Origin = Reconstructed`, depleted retention, and new accountable identities — without inventing unavailable historical layer ids, without authority transfer, and without reconstruction calculation/claim/persist.

| Area | Delivered |
|---|---|
| Domain | Optional `remainingQuantity` on `StockLayerModel.Create` (including zero for depleted reconstructed layers) |
| Domain | `StockSourceIdempotencyKindEnum.ReconstructionBaseline` for non-colliding reconstruction idempotency keys |
| Tests | `ReconstructedBaselineDomainReadinessTest` — reconstructed/depleted shapes, origin orthogonality, identity, idempotency link, no authority / no new movement kinds |
| Calculation / claim / persist | **Not** implemented (P2-S4…S6) |

No Application, Infrastructure, SqlDb, or API changes. Phase 1 models were extended, not redesigned.

---

## Phase 1 capabilities reused (no redesign)

| Capability | Existing type | Status for P2-S3 |
|---|---|---|
| Origin label | `StockFactOriginEnum.Reconstructed` | Already sufficient |
| Receipt movement | `StockMovementModel.CreateReceipt(..., origin)` | Already accepts `Reconstructed` |
| Movement lines | `StockMovementLineType` with origin | Already sufficient |
| Layer identity | `StockLayerModel.Create` → ULID when id omitted | Already sufficient |
| Depleted retention in position | `StockPositionModel.AddLayer` / `Allocate` | Already retains Remaining = 0 |
| Layer rehydrate | `StockLayerModel.Rehydrate` | Still available for persistence load |
| Scope completion | `CompleteReconstruction` + opaque position | Already sufficient (P2-S2 handoff) |
| Source transaction | `SourceTransactionReferenceType` | Already sufficient |
| Idempotency model | `StockSourceIdempotencyModel` | Extended kind only |
| Movement kinds | Receipt / Outbound / Transfer / Correction / Reversal | **No** reconstruction-specific kind added |

---

## Important implementation decisions (repository-first)

| Decision | Why |
|---|---|
| Optional `remainingQuantity` on existing `Create` | Reconstruction must establish depleted layers (`Initial > 0`, `Remaining = 0`) without inventing a fictional `Consume` step or misusing `Rehydrate` for brand-new facts |
| Default remaining = initial when omitted | Preserves all existing Native / sync call sites |
| Reuse `CreateReceipt` with `Origin = Reconstructed` | Balance-anchored baseline can form layers from receipt-shaped facts; avoids unused movement kinds (plan risk) |
| Add `ReconstructionBaseline` idempotency kind | P2-S6 needs a linkable key that cannot collide with FO `SourceConsequence` keys; Domain enum is INT-backed, no DDL change |
| No parallel reconstructed models | Phase 1 Movement/Layer/Position already carry origin and depleted retention |
| No authority fields / FEFO / sync origin conflation | Locked Stage B semantics unchanged |

---

## Deviations from the Phase 2 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| “Minimal domain extensions (if any)” | Two small extensions only | Existing Domain already covered origin, identities, positions, and scope; gaps were depleted establishment at create-time and reconstruction-safe idempotency kind |
| Update Phase 2 plan slice-progress table | **Not done** | Explicit deliverable instruction: do not modify the Phase 2 plan |
| Possibly unused factories/kinds | No new movement kinds | Plan risk called this out; Receipt + `Reconstructed` origin is enough |

No planned P2-S3 behavior was deferred that blocks P2-S4.

---

## Tests / build results

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **98 passed**, **8 skipped** (G-23 placeholders), 0 failed |
| P2-S3 new tests | 9 (`ReconstructedBaselineDomainReadinessTest`) |
| Prior suite | Preserved (89 prior executable + 9 new = 98) |
| Domain SQL / Infrastructure leakage | None |
| Baseline calculation / claim / Phase B–C orchestration | None |
| P2-S1 / P2-S2 behavior | Unchanged |

---

## Remaining work handed to P2-S4

**P2-S4 — Baseline calculation + ambiguity classification** can proceed. It should:

1. Consume scoped G-05 snapshots (`ILegacyStockReadPort`) and produce proposed Layers/Positions (and any establishing Movement intent) using:
   - `StockFactOriginEnum.Reconstructed`
   - `StockLayerModel.Create(..., remainingQuantity: …)` for active and depleted layers
   - **new** accountable layer/movement identities (do not recreate vanished legacy layer ids)
2. Classify `Balanced` vs `Inconsistent` with explicit reason (BR-STL-103/105).
3. Prefer balance-anchored Remaining Quantity from `tb_stok` with provenance enrichment from `tb_buku` (plan guidance).
4. **Not** claim TX, persist, fingerprint beyond P2-S2, Freshness Gate, or Availability Discovery.

P2-S6 (later) can persist the expressed payload and record `StockSourceIdempotencyKindEnum.ReconstructionBaseline` with a Source Transaction Reference for idempotent reconstruction outcomes.
