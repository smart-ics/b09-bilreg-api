# Stock Ledger Phase 1 / P1-S1 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P1-S1 — Context scaffolding and boundary types  
**Plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS  

---

## WHAT

Additive `StockLedgerFeature` scaffolding and boundary types only. No movement, layer, persistence, sync, or reconstruction behavior.

| Area | Delivered |
|---|---|
| Folders | `StockLedgerFeature/` under Domain, Application, Infrastructure, SqlDb, Test (empty layers use `.gitkeep`) |
| Receipt Source identity | `ReceiptSourceType` / `IReceiptSourceKey` (`ReceiptSourceId` ↔ legacy KodeDO / `fs_kd_do`) |
| Reconstruction / reconciliation scope | `StockLedgerScopeKeyType` / `IStockLedgerScopeKey` = Item + Receipt Source |
| Candidate write scope | `StockWriteScopeKeyType` / `IStockWriteScopeKey` = Item + Receipt Source + Stock Location |
| Enums | `StockFactOriginEnum`, `ReconstructionStatusEnum`, `SynchronizationStateEnum` |
| Tests | `StockLedgerBoundaryTypesTest` (11 unit tests) |

Legacy `StokFeature` was not modified.

---

## WHY

- Encode locked G-01 / G-04 type boundaries in code before later slices add behavior.
- Keep reconstruction scope and write scope as distinct types so agents cannot conflate them with authority.
- Origin / reconstruction / synchronization enums are separate dimensions; none imply `IsAuthoritative`.

---

## Reused (not duplicated)

| Domain concept | Existing type reused |
|---|---|
| Item | `IBrgKey` / `BrgId` (`BrgContext`) |
| Stock Location | `ILayananKey` / `LayananId` (`InventoryContext.StokFeature`) |

No new `ItemId` or `StockLocationId` wrappers were added.

---

## Deviations from the Phase-1 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Suggested `ItemId` / `StockLocationId` types | Reuse `BrgId` / `LayananId` | Same semantics already exist; avoid duplicate identity abstractions |
| Name `IStockLedgerScopeKey` only | Also added `IStockWriteScopeKey` / `StockWriteScopeKeyType` | Needed to test and preserve Item+RS vs Item+RS+Location distinction |
| Optional csproj `<Folder>` entries | Not added | Real files / `.gitkeep` are enough for SDK-style projects |

---

## Validation

| Check | Result |
|---|---|
| `dotnet build src/bilreg/b09-bilreg-api.sln` | Succeeded |
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | 11 passed, 0 failed |
| No `IsAuthoritative` members on new boundary types | Asserted by UT11 |
| No SQL / repos / StokFeature changes | Confirmed |

---

## Notes for P1-S2

1. Put Movement under `InventoryContext/StockLedgerFeature`, not `StokFeature`.
2. Movement lines should key Item / Receipt Source / Location via `IBrgKey`, `IReceiptSourceKey`, `ILayananKey` (or the scope types).
3. Use `StockFactOriginEnum` on facts later; do not derive authority from it.
4. Do not implement Layer/Position/FIFO, persistence, sync position, or scope-state transitions in S2.
5. Runtime authority remains `tb_stok` + `tb_buku` — outside these types.
