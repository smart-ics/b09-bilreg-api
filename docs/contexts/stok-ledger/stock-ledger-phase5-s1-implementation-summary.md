# Stock Ledger Phase 5 / P5-S1 — Implementation Summary

**Status:** COMPLETE  
**Gate:** **PASS**  
**Date:** 2026-08-08  
**Slice:** P5-S1 — Trusted Availability → Freshness → FIFO orchestration  
**Plan:** [`stock-ledger-phase5-implementation-plan.md`](./stock-ledger-phase5-implementation-plan.md)

---

## Objective

Deliver an explicit Application orchestration service that turns provisional Availability Discovery candidates into a deterministic, freshness-gated, ED-constrained FIFO allocation plan — or a fail-closed outcome — **without** writing FO stock, persisting allocation mutations, or treating Availability as Ledger authority.

---

## Implemented changes

| Area | Deliverable |
|---|---|
| Application | `TrustedStockAllocationOrchestrator` + `TrustedStockAllocationRequest` / `TrustedStockAllocationResult` / `TrustedStockAllocationOutcomeEnum` |
| Flow | Availability → reconstruct (if needed) per candidate DO → Freshness Gate → reload layers at source location → `StockFifoAllocator.Allocate` |
| Tests | `TrustedStockAllocationOrchestratorTest` (9 scenarios) |
| Docs | This summary + plan Slice Progress + ARTIFACTS |

**Explicitly not implemented:** MT Legacy Compatibility Writer (P5-S2); Native Transfer UseCase / capability flag (P5-S3); void (P5-S4); concurrency locks / ConcurrentOutbound (P5-S5); coexistence proof (P5-S6); production DI/HTTP.

---

## Orchestrator outcomes

| Outcome | Meaning |
|---|---|
| `PlanReady` | Freshness-proven layers allocated; payload is Domain `StockAllocationResult` |
| `InsufficientAuthoritativeStock` | Availability found no usable provisional stock |
| `InsufficientLedgerStock` | After Freshness + reload, Ledger layers cannot fulfill qty |
| `StaleOrNotCurrent` | Scope Reconstructing, basis-change retry exhausted, or Freshness Gate unsafe |
| `Inconsistent` | Scope or Freshness Gate Inconsistent |

---

## Architectural decisions

| Decision | Rationale |
|---|---|
| Plain service (not MediatR) | Plan-only; matches `LegacyStockFreshnessGate` style; no UoW commit |
| Reuse `StockAllocationResult` as plan payload | Avoid parallel Domain DTO; P5-S2 can map allocations directly |
| Gate **all** unique Availability candidate DOs (ordered by `ReceiptSourceId`) | FIFO order is Ledger Effective Receipt Time, not Availability SQL order |
| Separate `InsufficientLedgerStock` vs `InsufficientAuthoritativeStock` | Distinguishes provisional legacy totals from post-reload Ledger truth |
| Reconstruct via existing handler; Freshness via existing gate | Extend, do not rebuild Phase 2/3 |
| No production DI | Consistent with Stock Ledger feature; tests compose manually |
| No position `SaveChanges` / legacy writer in orchestrator | Allocation plan is non-mutating (reconstruction/sync side effects remain Phase 2/3 owned) |

---

## Deviations from plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Summary filename `stock-ledger-P5-S1-implementation-summary.md` (§12) | `stock-ledger-phase5-s1-implementation-summary.md` (task request) | Naming only; ARTIFACTS links the chosen path |
| Optional MediatR UseCase | Plain orchestrator service | Cleaner for read-only plan; P5-S3 can wrap as needed |

No locked domain/ADR decisions were reopened.

---

## Tests added/updated

Focused run (disposable `devTest` + unit fakes):

```text
dotnet test Bilreg.Test/Bilreg.Test.csproj --filter FullyQualifiedName~TrustedStockAllocationOrchestratorTest
Passed!  - Failed: 0, Passed: 9, Skipped: 0
```

| Test | Result |
|---|---|
| `MultiDoCandidates_SelectsFifoOrderAfterFreshness` | PASS — earliest Effective Receipt Time first; may span DOs |
| `ExplicitExpirationDateFilter_AllocatesMatchingEdOnly` | PASS |
| `InsufficientAuthoritativeStock_ReturnsBeforeGate` | PASS — no reconstruct / no gate |
| `InsufficientLedgerStock_AfterFreshness_FailClosed` | PASS |
| `StaleScope_FailClosed` | PASS |
| `InconsistentScope_FailClosed` | PASS |
| `SynchronizedNow_ThenAllocateSucceeds` | PASS — gate catch-up then allocate |
| `ReconstructWhenNoBaseline_ThenAllocate` | PASS |
| `NoLegacyStockMutation_FromOrchestratorAlone` | PASS — writer Applied empty; Ledger qty unchanged |

---

## Limitations intentionally deferred

- MT OUT/IN legacy writer shapes (P5-S2)
- Native Transfer UseCase, Scope refresh, capability flag (P5-S3)
- Transfer void / unsafe reverse (P5-S4)
- Lock order, quantity revalidation inside consequence TX, ConcurrentOutbound (P5-S5)
- Native transfer → VB6 → Phase 3 sync coexistence proof (P5-S6)
- Production enablement / public HTTP / DI registration

---

## Known risks

| Risk | Mitigation in P5-S1 | Residual |
|---|---|---|
| Allocate before Freshness | Gate runs before reload + FIFO; stale/inconsistent tests | P5-S5 still must revalidate inside TX |
| Treat Availability qty as authority | Plan uses reloaded Ledger `RemainingQuantity` only | Callers must not skip orchestrator |
| Eager reconstruct of non-candidates | Only Availability candidate DOs are prepared | None for this slice |
| Accidental FO write | No writer/UoW in orchestrator; mutation test asserts empty Apply | P5-S2+ must keep plan→writer mapping allocation-explicit |

---

## Gate verdict

**PASS** — trusted plan or fail-closed outcome only; Availability remains provisional; Domain FIFO reused; no FO writer changes.

---

## Reviewer notes (before authorizing P5-S2)

Confirm:

1. Freshness Gate always precedes `StockFifoAllocator.Allocate`.
2. Fail-closed paths cover stale, inconsistent, insufficient authoritative, and insufficient Ledger.
3. Multi-DO plan selects by Effective Receipt Time (+ Layer ID), not Availability order / FEFO / batch.
4. `StockAllocationResult.Allocations` carries layer id, Receipt Source, qty, valuation, ED for P5-S2 writer mapping.
5. No MT writer, Transfer UseCase, capability flag, or concurrency lock work landed in this slice.
6. Focused tests green on disposable DB.

**Approve P5-S2 only after this gate PASS is accepted.**
