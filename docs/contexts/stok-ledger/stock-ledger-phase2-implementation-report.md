# Stock Ledger — Phase 2 Implementation Report

**Status:** COMPLETE (with deferred G-25 debt; does not claim Phase 3)  
**Date:** 2026-08-07  
**Phase:** 2 — Initial Reconstruction baseline  
**Plan:** [`stock-ledger-phase2-implementation-plan.md`](./stock-ledger-phase2-implementation-plan.md)  
**Governing baseline:** Phase 0 Exit Review **PASS WITH RISKS** — [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md)  
**Phase 1 foundation:** COMPLETE (P1-S1…P1-S8)  
**Latest slice:** [`stock-ledger-phase2-s8-implementation-summary.md`](./stock-ledger-phase2-s8-implementation-summary.md)

---

## Implementation outcome

Phase 2 delivered an **idempotent Initial Reconstruction baseline** for one Item + Receipt Source across all Stock Locations, plus provisional Availability Discovery from legacy authority — **without** transferring runtime authority away from `tb_stok` + `tb_buku`, and **without** implementing incremental sync or Freshness Gate.

| Slice | Outcome |
|---|---|
| P2-S1 | Live G-05 read adapter (`LegacyStockReadPort`) |
| P2-S2 | SQL-free basis fingerprint (`LegacyReconstructionBasisCalculator`) |
| P2-S3 | Domain depleted create + `ReconstructionBaseline` idempotency kind |
| P2-S4 | Balance-anchored baseline calculator + Balanced/Inconsistent |
| P2-S5 | Short-TX Phase A claim (`ReconstructionClaimService`) |
| P2-S6 | MediatR Phase B/C orchestration + persist (G-10 core) |
| P2-S7 | Live G-08 provisional Availability Discovery |
| P2-S8 | Reconstruction harness hardening, controlled recovery, exit report |

**Success meaning (locked):** a balanced reconstructed baseline exists ≠ authority transferred ≠ VB6 blocked ≠ permanently synchronized.

---

## Major decisions

1. **Runtime authority stays on legacy** for Stage B; reconstruction success initializes Ledger representation only.
2. **TX shape A/B/C:** short claim → calculate outside write TX → short revalidate/persist (feasibility §2.3 / roadmap §5.3).
3. **Opaque Synchronization Position** initialized at successful reconstruction (`fingerprint-v1`); Phase 3 advances it.
4. **Depleted layers retained** in Ledger when zero `tb_stok` rows are absent (intentional G-16 representational difference).
5. **Ambiguity → Inconsistent** with reason; never invent a balanced baseline.
6. **Terminal short-circuit** for `Reconstructed`/`Inconsistent`; overwrite only via P2-S8 recovery.
7. **Recovery deletes additive `BILRG_*` rows** for the scope; never rewrites `tb_stok`/`tb_buku`; Claim recreates Scope afterward.
8. **No production DI / public stock endpoints** through Phase 2.
9. **Availability Discovery is provisional** — does not consult Ledger layers; `StaleOrNotCurrent` reserved for Phase 3/5 Freshness Gate callers.

---

## Deviations from the Phase 2 plan

| Plan suggestion | Actual | Why |
|---|---|---|
| Illustrative orchestration names | `ReconstructStockLedgerBaselineCommand` / Handler | Inventory MediatR style |
| Production DI “as needed” | Tests construct deps manually | Same Phase 1/2 stance; no FO consumer yet |
| Synthetic long `RBL|…` ids | SHA-256 hex truncated to VARCHAR(26) | Persistable column width |
| Basis-change “leave not-current” | Leave `Reconstructing` + `BasisChangedRetryRequired` | No Domain Reconstructing→not-current transition |
| Domain recover/reset transition | Delete Scope via recovery helper | Claim inserts from scratch; avoid inventing Domain recovery graph |
| Hard production timeout/row cap | Fake `TimeoutException` stand-in only | Avoid unrelated G-25 production optimization in Phase 2 |
| Concurrent Phase C always deadlock-free | Harness tolerates SQL 1205; durable single-baseline asserted | FQ-06 interim; auto-retry deferred |

No deviation reopens Stage B authority, origin labels, or sync ADR selection.

---

## Test / build status

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **141 passed**, **7 skipped**, 0 failed |
| Skipped | Remaining G-23 Phase 3/4/5/8 placeholders |
| Phase 1 suite | Remains green within the StockLedgerFeature filter |
| Legacy authority writes by reconstruction/recovery | None |
| Solution build | Succeeds (existing unrelated warnings only) |

---

## Unresolved risks / debt

| Debt | Owner phase | Blocks Phase 3 start? |
|---|---|---|
| G-25 proposed legacy indexes `(barang, do, …)` not applied | DBA / later | **No** |
| G-25 production p95 SLOs | Ops / Phase 8 | **No** |
| Production read command-timeout / row-cap policy | Later | **No** |
| Automatic deadlock retry in reconstruction Phase C | FQ-06 / G-17 | **No** |
| G-13 change-detection experiments | Phase 3 | Expected Phase 3 work |
| Freshness Gate (G-12), sync catch-up (G-14/15), reconciliation ops (G-16 rest) | Phase 3+ | Expected Phase 3 work |
| Full G-23 mixed-writer harness | Phase 3–5 | Expected later |
| Production DI / FO enablement | Phase 4+ | Expected later |

---

## Phase 2 exit checklist (plan §9)

Evaluated against repository evidence after P2-S8:

- [x] **G-05 live adapter accepted** — Item + Receipt Source reads across locations; deterministic ordering; queries bounded by Item+DO parameters (`LegacyStockReadPort` + P2-S1 tests).
- [x] **G-10 accepted** — phased A/B/C; concurrent claim safe; basis change does not persist stale baseline; repeated request idempotent; successful retry covered in P2-S8.
- [x] **Successful reconstruction initializes sync position + algorithm version** and sets Scope to `Reconstructed` **or** surfaces `Inconsistent` with reason.
- [x] **Depleted reconstructed layers retained** when corresponding zero `tb_stok` rows are absent.
- [x] **G-08 live provisional Availability Discovery** exists (candidates from legacy; not final FIFO).
- [x] **Reconstruction portion of G-16/G-23 covered** — depleted intentional difference activated; reconstruction races covered; later-phase G-23 markers remain skipped (7).
- [x] **No long write transaction spans history calculation** — Phase B TX-boundary spy tests.
- [x] **No `IsAuthoritative`; no legacy row rewrites** for reconstruction convenience; VB6 conceptually unblocked.
- [x] **Solution builds; StockLedgerFeature tests pass; Phase 1 remains green.**
- [x] **Controlled recovery path** for incomplete additive reconstruction documented/available on disposable DB (`RecoverIncompleteReconstructionService`).
- [x] **Phase 2 implementation report published**; plan slice progress updated to COMPLETE.
- [x] **Explicit handoff notes** list Phase 3 residuals (below).

**Exit verdict:** Phase 2 exit criteria are **satisfied**. Residual G-25 index/SLO work is **deferred** and does **not** block starting Phase 3. Phase 3 behavior is **not** claimed complete.

---

## Readiness / handoff for Phase 3

Phase 3 may consume:

| From Phase 2 | Use |
|---|---|
| Initialized Synchronization Position + algorithm version | Catch-up / Freshness starting point |
| `LegacyReconstructionBasisCalculator` | Fingerprint continuity |
| Reconstructed baseline (Movement/Layers/Positions/Scope) | Sync target representation |
| G-05 `ILegacyStockReadPort` live adapter | Re-read / replay inputs |
| Scope transitions toward `SynchronizationRequired` (Domain already has sync transitions) | Orchestration once discovery exists |
| G-08 provisional discovery | Later allocation callers (still need Freshness Gate first) |
| Recovery helper | Containment if reconstruction/sync leaves incomplete additive rows |

Phase 3 must still implement (not started here):

1. Legacy Change Discovery / G-13 detection experiments  
2. Incremental Legacy Synchronization catch-up (G-14/G-15)  
3. Freshness Gate orchestration (G-12)  
4. Sync portion of G-23 harness  
5. Production index apply + SLO definition (G-25) as ops allow  

**Do not** treat Availability Discovery results as authoritative Ledger-enriched FIFO input until Freshness Gate exists.
