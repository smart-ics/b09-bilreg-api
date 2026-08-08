# Stock Ledger Phase 3 / P3-S1 — Implementation Summary

**Status:** COMPLETE — **P3-S1 gate: PASS**  
**Date:** 2026-08-08  
**Slice:** P3-S1 — Live Legacy Change Discovery (G-13)  
**Plan:** [`stock-ledger-phase3-implementation-plan.md`](./stock-ledger-phase3-implementation-plan.md)

---

## Objective

Implement deletion-aware **change detection only**: compare stored Synchronization Position with current legacy authority via `fingerprint-v1`, perform set-diff of Ledger-known legacy identities vs surviving `tb_buku` / `tb_stok`, classify detectable delta kinds, and fail closed with `RequiresScopedReDerive` / `Undeterminable` when classification is not confident.

---

## What was implemented

| Area | Deliverable |
|---|---|
| Application | `LegacyChangeDiscoveryIdentityKeys` — durable `SYNC|BUKU|…` / `SYNC|STOK|…` idempotency key format embedding fingerprint-v1 material fields |
| Application | `LegacyChangeDiscoveryClassifier` — pure fingerprint compare + set-diff classification (`Unchanged`, `ChangesDetected`, `Undeterminable`) |
| Infrastructure | `LegacyChangeDiscoveryPort` — live `ILegacyChangeDiscoveryPort` using `ILegacyStockReadPort`, `LegacyReconstructionBasisCalculator`, and Ledger idempotency reads |
| Infrastructure | `IStockSourceIdempotencyDal.ListSyncIdentityKeysForScope` — read-only Ledger-known identity keys for one Item + Receipt Source |
| Tests | `LegacyChangeDiscoveryClassifierTest` (11 pure unit tests) |
| Tests | `LegacyChangeDiscoveryPortTest` (10 G-13 detection experiments on disposable `devTest` fixtures) |

**Explicitly not implemented (later slices):** sync catch-up, delta interpretation intents (P3-S2), reconciliation (P3-S3), Freshness Gate (P3-S5), Scope transitions, position advancement, production DI.

---

## Repository decisions made

| Decision | Rationale |
|---|---|
| Extend `IStockSourceIdempotencyDal` instead of new repo | Reuses existing idempotency persistence; discovery only needs read of `SyncBatch` / `SourceConsequence` keys scoped by `BrgId` + `ReceiptSourceId` |
| Pure classifier in Application | Keeps set-diff rules SQL-free and unit-testable; Infrastructure port stays thin |
| Ledger-known identities via idempotency keys with embedded material | Reconstruction baseline does not persist per-journal rows; durable `SYNC|…` keys (written by catch-up in P3-S4, seeded in tests) provide set-diff anchors without a second fingerprint implementation |
| Composite identity `(LayananId, LegacyJournalId)` / `(LayananId, LegacyRowId)` | Matches scoped `tb_buku` / `tb_stok` shape; avoids `fs_kd_trs`-alone cursor (Phase 0 FQ-01) |
| Fingerprint mismatch with **no** Ledger-known keys ⇒ `RequiresScopedReDerive` | ADR normative rule: hash drift alone is not a delete event stream |
| No production DI registration | Consistent with Phase 1/2/3 plan — internal/test invocation only |

---

## Deviations from the plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Illustrative class name `LegacyChangeDiscoveryPort` | Used as implemented | None — matches plan |
| Ledger-known journals from “movement identities” | Read from `BILRG_StokSourceIdempotency` keys (`SYNC|BUKU|…`, `SYNC|STOK|…`) | Equivalent durable identity source; catch-up (P3-S4) must write the same key format |
| Immediate post-reconstruction void-delete without any sync idempotency | Returns `RequiresScopedReDerive` until Ledger-known keys exist | Safe fail-closed; first catch-up batch establishes keys |

No architectural reopen. Single `fingerprint-v1` authority preserved.

---

## Test results

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~LegacyChangeDiscovery` | **21 passed**, 0 failed |
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` (single-threaded) | **161 passed**, 7 skipped, 0 failed |
| Solution build | Succeeded |

Detection experiments covered: journal insert, journal update (incl. backdated mutation time), `tb_buku` void delete, `tb_stok` depletion delete, balance update, repost (void+insert), unchanged, fingerprint continuity with reconstruction-initialized position, `RequiresScopedReDerive` without Ledger-known keys, read-only side effects, duplicate journal identity ⇒ `Undeterminable`.

**Note:** Parallel test runs against shared `devTest` can deadlock (pre-existing pattern with multiple legacy-writing fixtures); single-threaded run is green.

---

## Known limitations

1. Post-reconstruction discovery before any `SyncBatch` identity keys exist cannot set-diff void-deletes — returns `RequiresScopedReDerive` (by design).
2. New surviving `tb_stok` row identities without Ledger-known balance anchors add `RequiresScopedReDerive` (fail-closed for unclassified balance appearance).
3. `SourceConsequence` keys are parsed when they use the same `SYNC|…` format; FO keys with other shapes are ignored until Phase 4 defines mapping.
4. No production DI / HTTP endpoints.

---

## Acceptance checklist

| Criterion | Status |
|---|---|
| G-13: detect insert, update, stok delete, buku void delete, backdated mutation, repost on disposable fixtures | **PASS** |
| No watermark-alone / `fs_kd_trs`-alone cursors | **PASS** |
| Discovery read-only; does not advance position or apply sync | **PASS** |
| Single `LegacyReconstructionBasisCalculator` / `fingerprint-v1` | **PASS** |
| Unclassifiable ⇒ `RequiresScopedReDerive` / `Undeterminable`, not silent guess | **PASS** |
| Hash drift without set-diff signal ⇒ `RequiresScopedReDerive` | **PASS** |
| **P3-S1 implementation gate** | **PASS** — P3-S2…P3-S8 may proceed |

---

## Files changed

| Path | Change |
|---|---|
| `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyChangeDiscoveryIdentityKeys.cs` | Added |
| `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyChangeDiscoveryClassifier.cs` | Added |
| `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/LegacyChangeDiscoveryPort.cs` | Added |
| `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockSourceIdempotencyDal.cs` | Extended |
| `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/LegacyChangeDiscoveryClassifierTest.cs` | Added |
| `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/LegacyChangeDiscoveryPortTest.cs` | Added |
| `docs/contexts/stok-ledger/stock-ledger-phase3-implementation-plan.md` | Slice progress |
| `docs/ARTIFACTS.md` | Registry entry |
| `docs/contexts/stok-ledger/stock-ledger-P3-S1-implementation-summary.md` | Added |

---

## Next slice readiness

**P3-S2 can proceed.** Stable delta shapes (`JournalInsert`, `JournalUpdate`, `JournalVoidDelete`, `BalanceDelete`, `BalanceUpdate`, `RequiresScopedReDerive`) are produced by the live adapter and pure classifier. P3-S2 should map these deltas (+ in-memory Ledger snapshot inputs) to side-effect-free accountable intents. P3-S4 catch-up must persist `SyncBatch` idempotency using `LegacyChangeDiscoveryIdentityKeys` so subsequent discovery set-diff remains deletion-aware.
