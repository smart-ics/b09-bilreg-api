# Stock Ledger Phase 1 / P1-S3 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P1-S3 — Stock Layer / Position + ED-constrained FIFO  
**Plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Prior slices:** [`stock-ledger-phase1-s1-implementation-summary.md`](./stock-ledger-phase1-s1-implementation-summary.md), [`stock-ledger-phase1-s2-implementation-summary.md`](./stock-ledger-phase1-s2-implementation-summary.md)  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS  

---

## WHAT

In-memory Stock Layer / Stock Position domain model and deterministic ED-constrained FIFO allocation under `InventoryContext/StockLedgerFeature`. No persistence, coexistence state, sync, reconstruction, ports, or legacy adapters.

| Area | Delivered |
|---|---|
| Layer | `StockLayerModel` / `IStockLayerKey` |
| Position | `StockPositionModel` (write scope: Item + Receipt Source + Stock Location) |
| Allocation | `StockFifoAllocator`, `StockAllocationResult`, `StockLayerAllocationType` |
| Movement bridge | Optional `StockLayerId` on `StockMovementLineType` + `ToOutboundMovementLines()` |
| Tests | `StockLayerPositionFifoTest` (17 unit tests) |

Legacy `StokFeature` FEFO path was not modified.

---

## WHY this Layer / Position / allocation design

- **Layer** is the accountable quantity unit with Initial/Remaining Quantity, Unit Valuation, optional Expiration Date, Effective Receipt Time, origin, and identity used as FIFO tie-break.
- **Position** is the write-consistency boundary established in P1-S1 (`StockWriteScopeKeyType`), not the reconstruction scope. It owns layers for one Item + Receipt Source + Stock Location, retains depleted layers, and carries a domain `Version` token for later OCC (G-06).
- **Allocator** is a small static domain service implementing locked ED → FIFO rules explicitly — not a generic framework and not FEFO.
- Insufficient stock returns an **explicit unfulfilled result** with no layer mutation, so Remaining Quantity can never go negative through allocation.
- Allocation splits identify each consumed layer so outbound movement lines satisfy BR-STL-024/035.

Authority semantics are absent: origin remains origin-only; runtime authority stays outside this slice.

---

## Reused from P1-S1 / P1-S2 (not duplicated)

| Domain concept | Existing type reused |
|---|---|
| Item | `IBrgKey` / `BrgObatType.Key` / `BrgReff` |
| Receipt Source | `IReceiptSourceKey` / `ReceiptSourceType` |
| Stock Location | `ILayananKey` / `LayananType.Key` |
| Write scope | `IStockWriteScopeKey` / `StockWriteScopeKeyType` |
| Ledger scope | `StockLedgerScopeKeyType` (projection from Position) |
| Fact origin | `StockFactOriginEnum` |
| Unit Valuation | `UnitValuationType` |
| Layer-forming movement | `IStockMovementKey` |
| Outbound lines | `StockMovementLineType` / `StockMovementModel.CreateOutbound` |

---

## FIFO and Expiration Date behavior

1. Candidates must match requested Item and Stock Location.
2. When Expiration Date is supplied, only layers with that exact date are eligible.
3. Eligible layers ordered by Effective Receipt Time ascending, then Stock Layer ID (`Ordinal`).
4. Quantity may consume multiple layers; exact depletion sets Remaining Quantity = 0.
5. Depleted layers remain in the position and are excluded from later eligibility (`RemainingQuantity > 0`).
6. Insufficient eligible quantity ⇒ `StockAllocationResult` with `IsFulfilled = false`, empty allocations, original layers unchanged.
7. Optional informational `Batch` may exist on a layer but is never used for selection or ordering.

---

## Depleted layers

- `StockLayerModel.Consume` retains the layer at `RemainingQuantity = 0`.
- `StockPositionModel.Allocate` keeps depleted layers in `Layers`.
- Later allocation skips depleted layers via `HasAvailableQuantity`.

---

## How allocation identifies consumed layers

- Each split is a `StockLayerAllocationType` carrying `StockLayerId`, quantity, Receipt Source, Unit Valuation, Expiration Date, and origin.
- `StockAllocationResult.ToOutboundMovementLines()` builds outbound `StockMovementLineType` rows with `StockLayerId` set.
- `StockMovementLineType` gained optional `StockLayerId` / `WithStockLayer(...)` — minimal P1-S2-compatible extension for BR-STL-024.

---

## Deviations from the Phase-1 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Allocator “service/method” | Static `StockFifoAllocator` + `StockPositionModel.Allocate` | Small, deterministic, no DI/framework needed for in-memory domain |
| Position key Item+RS+Location | Same, implemented via `IStockWriteScopeKey` | Reuses P1-S1 write-scope type instead of a parallel key |
| Batch ignored | Optional `Batch` informational field on layer | Makes “Batch not a selection key” testable without inventing a request Batch parameter |
| Movement redesign | Only optional `StockLayerId` on lines | Minimum bridge for outbound layer identification |

Domain doc §6.2 describes Position as Item+Receipt Source across locations; Phase-1 / P1-S1 locked write position as Item+RS+Location. This slice follows the locked Phase-1 boundary.

---

## Validation

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **38 passed** (11 P1-S1 + 10 P1-S2 + 17 P1-S3), 0 failed |
| `dotnet build src/bilreg/b09-bilreg-api.sln` | Succeeded (0 errors) |
| No SQL / repos / coexistence / StokFeature FEFO changes | Confirmed |

---

## Technical debt / notes for P1-S4

1. No coexistence / Reconstruction Status / Synchronization State transitions yet — that is P1-S4.
2. Synchronization Position (opaque + algorithm version) not introduced here.
3. Cross-position allocation (multiple Receipt Sources at one Item+Location) works via `StockFifoAllocator` over a layer pool; application orchestration that loads those positions is later.
4. Position `Version` is an in-memory OCC placeholder; persistence conflict handling is P1-S5/S6.
5. Transfer destination layer establishment / source-layer pairing still not orchestrated (domain allocation only).
6. No recording-time field separate from Effective Receipt Time (carried from P1-S2 note).

---

## P1-S4 readiness

**P1-S4 can proceed.** No blocker remains from P1-S3.

P1-S4 should introduce `StockLedgerScopeStateModel` and mechanism-neutral Synchronization Position under the same feature folder, reusing P1-S1 scope keys and origin enums. Do not start reconstruction behavior, sync discovery, or persistence in S4.
