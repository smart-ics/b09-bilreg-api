# Stock Ledger Phase 1 / P1-S7 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P1-S7 — Application ports (contracts only)  
**Plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Prior slices:** [`stock-ledger-phase1-s1-implementation-summary.md`](./stock-ledger-phase1-s1-implementation-summary.md) … [`stock-ledger-phase1-s6-implementation-summary.md`](./stock-ledger-phase1-s6-implementation-summary.md)  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS  

---

## WHAT

Application-layer **ports/contracts only** for later Stock Ledger phases. No live legacy adapters, reconstruction, synchronization, Freshness Gate, Availability/Provenance Discovery behavior, FO transactions, or writes to `tb_stok` / `tb_buku`. No authority semantics (`IsAuthoritative` absent).

| Port | Gap | Phase ownership (behavior) | Role |
|---|---|---|---|
| `ILegacyStockReadPort` | G-05 | Phase 2 | Cross-location legacy balance/journal reads for reconstruction |
| `ILegacyCompatibilityWriterPort` | G-11 | Phase 4+ | Legacy-compatible `tb_stok` / `tb_buku` / FO writebacks in consequence TX |
| `ILegacyChangeDiscoveryPort` | G-13 | Phase 3 | Fingerprint + set-diff / scoped re-derive discovery (ADR) |
| `IAvailabilityDiscoveryPort` | G-08 | Phase 2/5 | Provisional available Receipt Source candidates (not final FIFO) |
| `IProvenanceDiscoveryPort` | G-09 | Phase 7 | Original Receipt Source/layer resolution; explicit Unknown/Ambiguous |
| `IStockReconciliationPort` | G-16 | Phase 3/8 | Drift classification vs intentional representational differences |

Supporting contract models (sealed records + enums) live beside each port under:

`Bilreg.Application/InventoryContext/StockLedgerFeature/Ports/`

Test-only doubles live under:

`Bilreg.Test/InventoryContext/StockLedgerFeature/Fakes/StockLedgerPortFakes.cs`

including a throw-capable `FakeLegacyCompatibilityWriterPort` for P1-S8 UoW rollback wiring.

Legacy `StokFeature` / Infrastructure legacy DALs were not modified. No production DI registration for these ports.

---

## WHY these contract boundaries fit the current architecture

- Follows Clean Architecture already used by Stock Ledger: Application owns orchestration contracts; Infrastructure adapts later; Domain keeps keys/enums/`SynchronizationPositionType` without SQL.
- Matches existing Application gateway patterns (`ILabRegIntegration`, `IQueueNumberCompatibilityAdapter`, Journey contracts): explicit interfaces + sealed request/result records, not a generic port framework.
- Reuses P1-S1–S4 types in signatures (`IStockLedgerScopeKey`, `IBrgKey`, `ILayananKey`, `ISourceTransactionReferenceKey`, `StockMovementKindEnum`, `SynchronizationPositionType`) so later slices cannot invent parallel identity/authority models.
- Keeps Availability Discovery and Provenance Discovery as **separate ports** (feasibility D4 / roadmap rule).
- Encodes the sync ADR at the contract level (`ComputeCurrentFingerprint` + `DiscoverChanges` with opaque position) without hard-coding watermark cursors or implementing Freshness Gate orchestration.
- Places fakes only in `Bilreg.Test` so Scrutor/production DI cannot accidentally “discover” pretend adapters.

---

## Important decisions / deviations from the plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Illustrative port names only | Concrete interfaces + request/result/enum models | Needed for typed P1-S8 injection and later phase implementations |
| Optional `Ports/` folder | Used `…/StockLedgerFeature/Ports/` | Matches plan layout; keeps repo interfaces at feature root (P1-S6) separate from external ports |
| “Compile-time presence; optional fakes” | 10 contract tests + configurable fakes | Proves port presence, fake assignability, no-authority rule, and throw-on-Apply for S8 |
| Discovery signatures unspecified | `ComputeCurrentFingerprint` + `DiscoverChanges` | Separates freshness compare from mismatch set-diff/re-derive path per ADR |
| Compatibility writer payload unspecified | Explicit balance mutation + journal entry records | Avoids opaque `object` drafts; still no live legacy mapping |
| Async ports | Synchronous methods | Consistent with other Application gateways in this repo |

No planned P1-S7 work was deferred that blocks P1-S8.

---

## Tests / build results

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **74 passed** (64 prior + **10** P1-S7), 0 failed |
| Solution build (via test host) | Succeeded (0 errors) |
| Production adapters mutating `tb_stok` / `tb_buku` | None |
| Freshness Gate / reconstruction / FO write endpoints | None |
| `IsAuthoritative` on port contract types | Asserted absent (UT09) |

---

## Remaining limitations / technical debt

1. No Infrastructure implementations for any of the six ports — intentional until their owning phases.
2. Compatibility writer request shape may need FO-family-specific writeback fields when Phase 4 DO Receipt lands.
3. `LegacyDiscoveredDeltaKindEnum` lists the mutation kinds G-13 must eventually detect; empirical detection experiments remain Phase 3 residual debt.
4. Reconciliation port returns classification outcomes only — no persistence of reconciliation assessments yet (domain §5.7 aggregate deferred).
5. No production DI registration / capability flags — correct for Phase 1; P1-S8 should continue to wire Test fakes only for the legacy writer.

---

## P1-S8 readiness

**P1-S8 can proceed.** No blocker remains from P1-S7.

P1-S8 should introduce the consequence Unit of Work skeleton, inject `ILegacyCompatibilityWriterPort`, and use `FakeLegacyCompatibilityWriterPort` (throw-on-Apply) to prove Ledger-side transactional rollback. Do not implement live legacy writers, reconstruction, Freshness Gate, or public stock write endpoints in S8.
