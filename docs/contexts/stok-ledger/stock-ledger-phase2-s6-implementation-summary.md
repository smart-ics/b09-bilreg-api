# Stock Ledger Phase 2 / P2-S6 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P2-S6 — Phase B/C orchestration + persist (G-10 core)  
**Plan:** [`stock-ledger-phase2-implementation-plan.md`](./stock-ledger-phase2-implementation-plan.md)  
**Prior foundation:** [`stock-ledger-phase2-s5-implementation-summary.md`](./stock-ledger-phase2-s5-implementation-summary.md); P2-S1…S4 calculators/adapters; Phase 1 repos/UoW  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS; G-10 acceptance  

---

## WHAT

End-to-end Initial Reconstruction for one Item + Receipt Source: Phase A claim (P2-S5) → Phase B read/fingerprint/calculate outside any write TX → Phase C revalidate basis and persist a balanced reconstructed baseline **or** mark `Inconsistent`. Successful reconstruction initializes opaque Synchronization Position + algorithm version. No authority transfer; no legacy stock row writes.

| Area | Delivered |
|---|---|
| Application (MediatR) | `ReconstructStockLedgerBaselineCommand` / `ReconstructStockLedgerBaselineHandler` + result outcomes |
| UoW extension | `StockConsequenceDraft` accepts `IdempotencyKind` + optional conditional Scope status update for Phase C |
| Calculator tweak | Deterministic Movement/Layer ids truncated to `VARCHAR(26)` (persistable ULID-width) |
| Tests | `ReconstructStockLedgerBaselineHandlerTest` — happy path, idempotency, basis change, inconsistent, depleted persist, rollback, Phase B TX boundary, legacy untouched, multi-location |
| DI / API | **No** production DI registration for Stock Ledger repos/ports; **no** public HTTP enablement |

P2-S7 Availability Discovery, Phase 3 sync/Freshness Gate, FO/native consequences, and production coexistence enablement were **not** implemented.

---

## Orchestration / persistence approach (repository-first)

| Decision | Why |
|---|---|
| MediatR command/handler | Matches Inventory UseCase pattern; P2-S5 deferred MediatR to this slice |
| Compose P2-S1…S5 as-is | Plan forbids recreating claim/read/fingerprint/calculator/persist primitives |
| Respect `AlreadyClaimed` + `Reconstructing` | Allows retry after Phase C failure / basis-change without a Domain “release claim” transition |
| Terminal short-circuit for `Reconstructed` / `Inconsistent` | BR-STL-070/071 idempotency; no overwrite without recovery (P2-S8) |
| Phase B outside write TX | Feasibility/roadmap TX-A/B/C; spy UoW asserts zero active scopes during Phase B reads |
| Phase C re-read + re-fingerprint before persist | Do not persist stale baseline when legacy moved during Phase B |
| Bounded internal retry (3) on basis change | Plan: retry or leave recoverable; exhausted → `BasisChangedRetryRequired`, Scope stays `Reconstructing` |
| Balanced+movement via `IStockConsequenceUnitOfWork` | Reuse G-18 short Ledger commit; `LegacyWrite = null` |
| `ReconstructionBaseline` idempotency kind | P2-S3 kind avoids FO `SourceConsequence` key collision |
| Conditional Scope update (`expectedPrior = Reconstructing`) | Phase C claim revalidation; concurrent completer wins safely |
| Inconsistent / empty-balanced via dedicated short TX | No Movement required; Scope (+ optional empty idempotency) only |

Workflow:

```text
Terminal short-circuit (Reconstructed / Inconsistent)?
  -> return Already*
Phase A: ReconstructionClaimService.Claim  (short TX, commit)
Phase B: ILegacyStockReadPort + fingerprint + baseline calc  (no write TX)
Phase C:
  re-read + re-fingerprint
  if basis changed -> retry Phase B/C (bounded) or BasisChangedRetryRequired
  if Inconsistent -> MarkReconstructionInconsistent (conditional Scope update)
  if Balanced -> CompleteReconstruction + persist Movement/Positions/idempotency
```

---

## Important transaction-boundary decisions

1. **Phase A commits before Phase B** — claim TX never spans history read/calculation.
2. **Phase B opens no `IUnitOfWork` scope** — reads and pure calculation only.
3. **Phase C is a separate short TX** — basis revalidation reads occur *before* opening the persist TX; persist uses ambient `TransHelper` via UoW/repos.
4. **Phase C failure rolls back additive Ledger rows** — Movement/Layer/Position/idempotency; prior Phase A `Reconstructing` claim remains (recoverable retry).
5. **No legacy compatibility writer call** on reconstruction path — authority stays on `tb_stok` / `tb_buku`.

---

## Deviations from the Phase 2 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Illustrative orchestration naming | `ReconstructStockLedgerBaselineCommand` | Aligns with Inventory MediatR command style |
| Production DI “only as needed” | Tests construct handler + deps manually | Same Phase 1/2 stance; no production stock endpoint |
| Synthetic `RBL|…` ids from P2-S4 | SHA-256 hex (26 chars) derived from those seeds | `StockMovementId` / `StockLayerId` columns are `VARCHAR(26)`; long pipe-ids truncate on persist |
| “leave not-current” on basis change | Leave `Reconstructing` + `BasisChangedRetryRequired` | Domain has no Reconstructing→not-current transition; recoverable without inventing sync states |

No planned P2-S6 behavior was deferred that blocks P2-S7.

---

## Tests / build results

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **125 passed**, **8 skipped** (G-23 placeholders), 0 failed |
| P2-S6 new tests | 9 (`ReconstructStockLedgerBaselineHandlerTest`) |
| Prior suite | Preserved (116 prior executable + 9 new = 125) |
| Happy path | `Reconstructed` + sync position = Phase B/C fingerprint |
| Duplicate trigger | `AlreadyReconstructed`; layer/position/movement counts unchanged |
| Basis change during Phase B | No Movement/Layer/Position/idempotency; Scope remains `Reconstructing` |
| Inconsistent | Explicit reason; no fake-balanced layers |
| Depleted retention | Persisted Remaining = 0 after commit |
| Phase C persist failure | Additive Ledger rows rolled back; claim remains |
| Phase B TX boundary | Active write scope count = 0 during Phase B reads |
| Legacy `tb_stok` / `tb_buku` | Unchanged; compatibility writer not invoked |
| P2-S1…S5 behavior | Unchanged aside from persistable id width in P2-S4 calculator |

---

## Remaining work handed to P2-S7

**P2-S7 — Availability Discovery live adapter (G-08)** can proceed. It should:

1. Implement provisional Availability Discovery from legacy authority by Item + Stock Location (+ optional Expiration Date), returning Receipt Source candidates — **not** final FIFO.
2. Reuse G-05 read patterns from P2-S1 where helpful; keep Availability as a separate port.
3. **Not** treat Stock Ledger layers as authority merely because reconstruction completed.
4. **Not** implement Freshness Gate, incremental sync, FO/native stock transactions, or production coexistence enablement.

P2-S8 (later) will harden coexistence harness/recovery for incomplete additive reconstruction and close Phase 2 exit criteria.

P2-S6 does **not** deliver: Availability Discovery, Legacy Change Discovery, Freshness Gate, FO Receipt, or production DI against hospital DB.
