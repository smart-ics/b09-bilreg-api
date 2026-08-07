# Stock Ledger Phase 2 / P2-S8 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P2-S8 — Reconstruction harness, recovery, Phase 2 exit hardening  
**Plan:** [`stock-ledger-phase2-implementation-plan.md`](./stock-ledger-phase2-implementation-plan.md)  
**Prior foundation:** [`stock-ledger-phase2-s7-implementation-summary.md`](./stock-ledger-phase2-s7-implementation-summary.md); P2-S1…S6 reconstruction A/B/C  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS; Phase 2 exit criteria (plan §9)  
**Phase report:** [`stock-ledger-phase2-implementation-report.md`](./stock-ledger-phase2-implementation-report.md)

---

## WHAT

Close Phase 2 with reconstruction-focused coexistence hardening, controlled recovery for incomplete additive reconstruction, G-25 residual documentation, and a Phase 2 exit report. No Phase 3 sync/Freshness Gate, FO writes, or production coexistence enablement.

| Area | Delivered |
|---|---|
| Application | `RecoverIncompleteReconstructionService` — short-TX scope-scoped delete of additive Ledger output |
| Port | `IIncompleteReconstructionRecoveryPort` |
| Infrastructure | `IncompleteReconstructionRecoveryPort` — DELETE against `BILRG_*` only |
| Harness tests | Successful basis-change retry; concurrent full reconstructors; Phase B read-failure stand-in |
| G-23 | Un-skipped `DepletedLayer_IntentionalLegacyDifference_IsReconciledAsRepresentational` only |
| Docs | This summary + Phase 2 implementation report + plan slice progress + Artifact Registry |
| DI / API | **No** production DI; **no** public endpoints; recovery is Application helper for disposable DB drills |

---

## Hardening / recovery work

### Controlled recovery

Mirrors the existing test `Cleanup` SQL shape as an Application service:

1. Short `IUnitOfWork` TX
2. Delete Movement/Line (deterministic reconstruction movement id), Layer, Position, Scope, ReconstructionBaseline idempotency for one Item + Receipt Source
3. Outcomes: `Recovered` (rows deleted) or `NoOp` (nothing present)
4. Never touches `tb_stok` / `tb_buku`

After recovery, Scope is absent; the next Phase A claim recreates from scratch. Terminal `Reconstructed` / `Inconsistent` can only be overwritten via this recovery path (proves P2-S6 “no overwrite without recovery”).

### Reconstruction harness gaps closed

| Theme | Coverage |
|---|---|
| Successful basis-change retry | Phase C mismatches once, then stabilizes → `Reconstructed` |
| Concurrent full reconstructors | Dual handler race; durable single baseline (1 movement / 2 layers); deadlock victim tolerated then follow-up |
| Read failure / timeout stand-in | Fake throws `TimeoutException` during Phase B; no additive persist; Scope stays `Reconstructing` |
| Duplicate trigger | Already covered in P2-S6 (`AlreadyReconstructed`) |
| Basis-change exhaust | Already covered in P2-S6 (`BasisChangedRetryRequired`) |
| Depleted intentional difference | G-23 un-skipped (below) |

### G-23 activation

| Scenario | Status |
|---|---|
| `DepletedLayer_IntentionalLegacyDifference_IsReconciledAsRepresentational` | **Activated** — Ledger retains Remaining=0; legacy balances omit depleted location |
| Legacy→New / New→Legacy / alternating / real mismatch / duplicate sync / concurrent outbound / partial live legacy+Ledger | **Remain skipped** (Phase 3/4/5/8) |

---

## Repository-driven recovery decisions

| Decision | Why |
|---|---|
| Application helper + Infrastructure delete port | Plan: optional Application recovery; keep SQL out of Application |
| Delete Scope row rather than Domain reset transition | Domain has no abandon/recover transition; Claim already inserts from scratch when Scope missing |
| Same SQL shape as test Cleanup | Prefer extending existing mechanism over new operational tooling |
| Tolerate SQL deadlock (1205) in concurrent harness | FQ-06 interim; production deadlock-retry policy is later; durable invariant is single baseline |
| Fake timeout for bounded-query failure | Practical without inventing production command-timeout/row-cap policy (G-25 deferred) |
| No production DI / HTTP | Same Phase 1/2 stance |

---

## Remaining deferred risks / debt

| Item | Status | Blocks Phase 3 start? |
|---|---|---|
| G-25 proposed `(barang, do, …)` indexes on `tb_stok`/`tb_buku` | Deferred — DBA-owned; not applied | **No** |
| G-25 production p95 SLOs for reconstruction / discovery | Deferred | **No** |
| Production command-timeout / hard row-cap on G-05 reads | Deferred (failure mode proven via fake only) | **No** |
| Automatic deadlock retry inside reconstruction Phase C | Deferred (FQ-06 / G-17); harness documents interim | **No** |
| Full G-23 mixed-writer / sync / outbound scenarios | Phase 3+ | **No** (intentionally out of Phase 2) |
| Freshness Gate, Legacy Change Discovery, sync catch-up | Phase 3 | N/A — Phase 3 work |
| Production DI / FO enablement | Later phases | N/A |

---

## Tests / build results

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **141 passed**, **7 skipped** (remaining G-23 placeholders), 0 failed |
| Prior suite (P2-S7) | 132 passed / 8 skipped |
| Net new executable | +9 (5 recovery + 3 harness + 1 G-23 depleted) |
| G-23 skipped remaining | 7 |
| Recovery legacy untouched | `tb_stok` / `tb_buku` counts unchanged |
| Phase 1 / prior Phase 2 | Preserved green |
| Phase 3 claimed complete? | **No** |

---

## Phase 2 exit

Exit criteria are evaluated in [`stock-ledger-phase2-implementation-report.md`](./stock-ledger-phase2-implementation-report.md). Phase 2 is **complete for its reconstruction scope** with G-25 index/SLO debt explicitly deferred and non-blocking for Phase 3 start. Do **not** start Phase 3 inside this slice.
