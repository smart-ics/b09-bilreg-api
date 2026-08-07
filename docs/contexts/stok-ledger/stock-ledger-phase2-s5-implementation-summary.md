# Stock Ledger Phase 2 / P2-S5 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P2-S5 — Phase A reconstruction claim  
**Plan:** [`stock-ledger-phase2-implementation-plan.md`](./stock-ledger-phase2-implementation-plan.md)  
**Prior foundation:** [`stock-ledger-phase2-s4-implementation-summary.md`](./stock-ledger-phase2-s4-implementation-summary.md); Phase 1 Scope transitions/repo (P1-S4/S6) + `IUnitOfWork`  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS  

---

## WHAT

Short-transaction Phase A claim that acquires reconstruction work for one Item + Receipt Source: coexistence state becomes `Reconstructing`, then commits — before any legacy history read or baseline calculation. Concurrent claimants yield exactly one durable winner; losers fail closed without corrupting Scope state.

| Area | Delivered |
|---|---|
| Application | `ReconstructionClaimService.Claim(scope)` + `ReconstructionClaimResult` / `ReconstructionClaimOutcomeEnum` |
| Infrastructure | Scope DAL `UpdateWhenReconstructionStatus`; repo `TryInsertNew` / `TryUpdateWhenReconstructionStatus` |
| Domain reuse | Existing `RequireReconstruction` → `BeginReconstruction` transitions (no Domain redesign) |
| Tests | `ReconstructionClaimServiceTest` — happy path, concurrent winner, illegal states, no history deps, legacy untouched |
| DI / API | **No** production DI; **no** public endpoints; **no** Phase B/C orchestration |

P2-S1–S4 calculators/adapters and Movement/Layer/Position persistence were **not** modified for claim behavior. `SaveChanges` remains last-writer-wins for non-claim Scope updates.

---

## Repository-driven claim / concurrency approach

| Decision | Why |
|---|---|
| Application service + `IUnitOfWork` short TX | Matches `StockConsequenceUnitOfWork` ambient `TransHelper` pattern; claim must commit before Phase B |
| Conditional status update (`WHERE ReconstructionStatus = @Expected`) | Phase 1 Scope lacks OCC; minimum claim-safety without full mixed-writer lock order (plan concurrency note) |
| Insert-as-`Reconstructing` when Scope missing | Creates row if absent in one write; PK race → reload + fail closed / continue |
| Domain transition chain before persist | Preserves illegal-transition rejection (`Reconstructed` / `Inconsistent`) without inventing claim-only Domain APIs |
| `AlreadyClaimed` outcome (not throw) for Reconstructing / lost race | Plan allows fail closed or no-op; durable state stays single `Reconstructing` winner |
| No `ILegacyStockReadPort` / calculator deps | Enforces “history not read/calculated inside claim TX” by construction |
| Keep general `SaveChanges` unchanged | Claim safety is additive; do not expand into general Scope OCC |

Claim algorithm (TX-A only):

```text
Begin TX
  Load Scope
  If missing → insert Claimed(Reconstructing); PK race → reload
  If Reconstructing → AlreadyClaimed (no write)
  Else apply domain Require/Begin → conditional update on prior status
  If 0 rows → AlreadyClaimed after reload
  Complete TX
```

---

## Deviations from the Phase 2 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Application, Infrastructure (if needed), Test | All three | Conditional update requires DAL/repo primitives |
| Illustrative claim helper naming | `ReconstructionClaimService` | Aligns with Application service style (`StockConsequenceUnitOfWork`) |
| MediatR command | Deferred to P2-S6 orchestration | Plan leaves naming to agent; P2-S6 prefers MediatR for end-to-end reconstruction |

No planned P2-S5 behavior was deferred that blocks P2-S6.

---

## Tests / build results

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **116 passed**, **8 skipped** (G-23 placeholders), 0 failed |
| P2-S5 new tests | 9 (`ReconstructionClaimServiceTest`) |
| Prior suite | Preserved (107 prior executable + 9 new = 116) |
| Concurrent claim | 8 racers → 1 `Claimed` + 7 `AlreadyClaimed`; durable `Reconstructing` |
| History read / baseline calc inside claim TX | None (deps asserted; no ports wired) |
| `tb_stok` / `tb_buku` writes | None |
| Movement/Layer/Position persist in TX-A | None |
| P2-S1…S4 behavior | Unchanged |

---

## Remaining work handed to P2-S6

**P2-S6 — Phase B/C orchestration + persist (G-10 core)** can proceed. It should:

1. Call `ReconstructionClaimService.Claim` (Phase A), then **outside** that TX: read via G-05 → fingerprint (P2-S2) → calculate (P2-S4).
2. Phase C short TX: revalidate claim still `Reconstructing` and legacy basis unchanged; persist Balanced payload (`Origin = Reconstructed`) or `MarkReconstructionInconsistent(reason)`; initialize Synchronization Position on success.
3. Prefer MediatR Application command/handler for end-to-end orchestration; DI only as needed for tests/internal invocation.
4. **Not** implement Freshness Gate, Availability Discovery, FO/native stock transactions, or production authority transfer.

P2-S5 does **not** deliver: Phase B/C orchestration, Movement/Layer persistence, Synchronization catch-up, or fingerprint computation inside the claim path.
