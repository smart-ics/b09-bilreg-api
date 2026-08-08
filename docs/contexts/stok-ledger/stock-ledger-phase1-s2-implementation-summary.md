# Stock Ledger Phase 1 / P1-S2 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P1-S2 — Immutable Stock Movement domain  
**Plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Prior slice:** [`stock-ledger-phase1-s1-implementation-summary.md`](./stock-ledger-phase1-s1-implementation-summary.md)  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS  

---

## WHAT

In-memory Stock Movement domain foundation under `InventoryContext/StockLedgerFeature`. No persistence, Layer/Position/FIFO, sync, reconstruction, ports, or legacy adapters.

| Area | Delivered |
|---|---|
| Aggregate | `StockMovementModel` / `IStockMovementKey` |
| Lines | `StockMovementLineType` (immutable completed line facts) |
| Supporting VOs | `SourceTransactionReferenceType`, `UnitValuationType` |
| Enums | `StockMovementKindEnum`, `StockMovementDirectionEnum` |
| Factories | `CreateReceipt`, `CreateOutbound`, `CreateTransfer` |
| Consequence ops | `Reverse()`, `Correct()` — both return **new** movements |
| Tests | `StockMovementDomainTest` (10 unit tests) |

Movement kinds supported: Receipt, Outbound, Transfer, Correction, Reversal.

Legacy `StokFeature` was not modified.

---

## WHY this aggregate shape

- One **completed** Stock Movement owns its lines as a single consistency boundary (source ref, kind, effective time, directions/quantities, transfer conservation, correction/reversal links).
- Factories produce already-completed immutable movements — matching BR-STL-022 without a mutable draft lifecycle in this slice.
- `Reverse` / `Correct` are aggregate behaviors that append history by creating new movements, never by editing the original.
- Lines keep Item, Receipt Source, Location, direction, positive quantity, Unit Valuation, and origin for later provenance / P1-S3 layer work.
- Direction is explicit (`Inbound` / `Outbound`); quantity is always positive (BR-STL-023).

---

## Reused from P1-S1 (not duplicated)

| Domain concept | Existing type reused |
|---|---|
| Item | `IBrgKey` / `BrgReff` |
| Receipt Source | `IReceiptSourceKey` / `ReceiptSourceType` |
| Stock Location | `ILayananKey` / `LayananType.Key(...)` |
| Fact origin | `StockFactOriginEnum` (origin only; not authority) |

No new Item / Receipt Source / Location wrappers were introduced.

---

## Invariants enforced

1. Completed movement lines are frozen (`ReadOnlyCollection`); mutation throws; Reverse/Correct leave the original unchanged.
2. Line quantity must be positive.
3. Direction is explicit; never encoded as negative quantity.
4. Reversal creates a new movement with flipped directions and `ReversedMovementId` → original.
5. Correction creates a new movement with caller-supplied lines and `CorrectedMovementId` → original.
6. Transfer requires equal total outbound/inbound quantity; also conserves quantity per Item + Receipt Source + Unit Valuation; exactly one source and one destination location, and they must differ.
7. Line provenance fields (Item, Receipt Source, Location, valuation, origin) are retained on the movement.
8. No `IsAuthoritative` / authority semantics on movement types; origin remains origin-only.

---

## Deviations from the Phase-1 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Illustrative `Reverse()` / `Correct()` only | Also `CreateReceipt` / `CreateOutbound` / `CreateTransfer` | Needed to create completed facts and enforce kind-specific line-direction / transfer rules |
| Movement line as child entity style | `StockMovementLineType` value object (as plan named) | Completed lines have no independent lifecycle in S2 |
| Optional draft/edit surface | None | Completed-fact-first model is smaller and matches immutability acceptance |

---

## Validation

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **21 passed** (11 P1-S1 + 10 P1-S2), 0 failed |
| `dotnet build src/bilreg/b09-bilreg-api.sln` | Succeeded |
| No SQL / repos / Layer / Position / StokFeature changes | Confirmed |

---

## Technical debt / notes for P1-S3

1. Movement lines do **not** yet carry Stock Layer identity. Outbound layer identification (BR-STL-024) belongs with Layer/Position + FIFO in P1-S3.
2. No recording-time field separate from Effective Business Time yet (BR-STL-026) — add when persistence / audit needs it.
3. Correction lines are supplied by the caller; S2 does not compute compensating quantities from position state.
4. Quantity is `decimal` (not legacy `int`) so later fractional units do not force a model break.
5. Transfer provenance conservation is by Item + Receipt Source + Unit Valuation totals, not yet by explicit source-layer → destination-layer pairing (layer pairing is P1-S3).

---

## P1-S3 readiness

**P1-S3 can proceed.** No blocker remains from P1-S2.

P1-S3 should introduce Stock Layer / Position and ED-constrained FIFO under the same `StockLedgerFeature` folder, reusing these movement types where allocation produces movement lines. Do not start reconstruction, sync, or persistence in S3.
