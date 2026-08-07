# Stock Ledger — Phase 2 Implementation Plan

**Artifact status:** Executable Phase-2 plan  
**Date:** 2026-08-07  
**Phase:** 2 — Initial Reconstruction baseline  
**Governing baseline (LOCKED):** Phase 0 Exit Review **PASS WITH RISKS** — [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md)  
**Phase 1 foundation (COMPLETE):** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md) (P1-S1…P1-S8) — latest handoff [`stock-ledger-phase1-s8-implementation-summary.md`](./stock-ledger-phase1-s8-implementation-summary.md)  
**Roadmap:** [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md) § Phase 2  
**Gaps in scope:** G-05, G-08, G-10, reconstruction portion of G-16 / G-23  
**Standards:** [`docs/ENGINEERING.md`](../../ENGINEERING.md), [`docs/DATABASE.md`](../../DATABASE.md), [`docs/NAMING.md`](../../NAMING.md)

### Slice progress

| Slice | Status | Summary |
|---|---|---|
| P2-S1 | **COMPLETE** | [`stock-ledger-phase2-s1-implementation-summary.md`](./stock-ledger-phase2-s1-implementation-summary.md) |
| P2-S2 | **COMPLETE** | [`stock-ledger-phase2-s2-implementation-summary.md`](./stock-ledger-phase2-s2-implementation-summary.md) |
| P2-S3 | **COMPLETE** | [`stock-ledger-phase2-s3-implementation-summary.md`](./stock-ledger-phase2-s3-implementation-summary.md) |
| P2-S4 | **COMPLETE** | [`stock-ledger-phase2-s4-implementation-summary.md`](./stock-ledger-phase2-s4-implementation-summary.md) |
| P2-S5 | Pending | — |
| P2-S6 | Pending | — |
| P2-S7 | Pending | — |
| P2-S8 | Pending | — |

---

## 1. Purpose

Convert the Phase 2 roadmap into **small, independently executable slices** that a mid-level coding agent can implement with minimal ambiguity.

Phase 2 establishes an **idempotent Stock Ledger baseline** for one Item + Receipt Source across all Stock Locations — using phased claim / calculate / revalidate-persist — **without** long write transactions spanning history calculation, and **without** transferring runtime authority away from `tb_stok` + `tb_buku`.

Phase 2 builds on the Phase 1 foundation already in the repository. It does **not** redesign Stock Ledger architecture, and it does **not** implement incremental Legacy Synchronization, Freshness Gate, FO receipts, or production coexistence enablement.

---

## 2. What Phase 1 already delivered (do not redo)

| Area | Repository state after P1-S8 |
|---|---|
| Feature folder | `InventoryContext/StockLedgerFeature/` across Domain, Application, Infrastructure, SqlDb, Test |
| Domain | Movement/Lines (immutable); Layer/Position + ED+FIFO; Scope coexistence state transitions; opaque `SynchronizationPositionType`; origin/status enums; source idempotency model |
| Persistence | Additive `BILRG_*` tables + DAL/DTO/Repo round-trips; Position OCC; depleted-layer retention; insert-once movements |
| Ports (contracts only) | `ILegacyStockReadPort`, `ILegacyCompatibilityWriterPort`, `ILegacyChangeDiscoveryPort`, `IAvailabilityDiscoveryPort`, `IProvenanceDiscoveryPort`, `IStockReconciliationPort` |
| UoW | `IStockConsequenceUnitOfWork` / `StockConsequenceUnitOfWork` — Ledger-side atomicity + fake legacy writer rollback |
| DI / API | No production DI for UoW/ports; no public stock write endpoints |
| Tests | StockLedgerFeature suite green; G-23 coexistence scenarios are **skipped placeholders** |

**Phase 1 explicitly did not deliver:** live `tb_stok` / `tb_buku` read adapters; reconstruction A/B/C; fingerprint computation against live legacy data; Freshness Gate; Availability Discovery behavior; FO/native consequences.

Implementers must **inspect the current code** before each slice and extend existing types/ports rather than inventing parallel Stock Ledger stacks.

---

## 3. Locked decisions (do not reopen)

| Topic | Locked baseline |
|---|---|
| Runtime authority (Stage B) | `tb_stok` + `tb_buku` — reconstruction success does **not** transfer authority |
| Origin labels | `Native` / `Reconstructed` / `LegacySynchronized` — origin only |
| Reconstruction / reconciliation scope | Item + Receipt Source across **all** Stock Locations |
| Write consistency candidate | Item + Receipt Source + Stock Location |
| Reconstruction TX shape | Short claim TX → calculate **outside** long write TX → short revalidate/persist TX ([roadmap §5.3](./stock-ledger-implementation-roadmap.md)) |
| Sync mechanism (selected; **not** fully implemented here) | Fingerprint + bounded replay ([sync ADR](./adr/ADR-stock-ledger-legacy-change-discovery.md)) |
| Synchronization Position | Mechanism-neutral opaque value + algorithm version; Phase 2 **initializes** it at successful reconstruction; Phase 3 advances it |
| Depleted layers | Ledger **retains** zero Remaining Quantity layers; legacy may delete zero `tb_stok` rows (intentional representational difference) |
| Ambiguity | Unknown Receipt Source distribution or material ordering ambiguity ⇒ `Inconsistent` (BR-STL-103 / BR-STL-105) — never silently invent balance |
| Allocation | FIFO remains Phase 1 domain helper; Phase 2 does **not** implement outbound allocation |

---

## 4. Explicit exclusions (later phases)

Do **not** implement in any Phase 2 slice:

- Incremental Legacy Synchronization catch-up / position advancement (Phase 3 / G-14–G-15)
- Full `ILegacyChangeDiscoveryPort` mismatch set-diff / void-delete interpretation / G-13 detection experiments (Phase 3)
- Legacy Freshness Gate orchestration (Phase 3 / G-12)
- Live `ILegacyCompatibilityWriterPort` / DO Receipt / FO post-void (Phase 4+)
- FIFO outbound, stock transfer, reservation, returns (Phases 5–7)
- Provenance Discovery behavior (Phase 7 / G-09)
- Full reconciliation ops aggregate / production observability (Phases 3/8 / G-16 beyond reconstruction classification, G-24)
- Mixed-writer production enablement and live VB6 concurrency proof (Phase 9 / G-17)
- Production index rollout on `HOSPITAL_HPL` without DBA approval (G-25 residual)
- Public HTTP APIs that enable production stock consequences
- Porting `clbGenStokX1` control flow; `IsAuthoritative`; Stage C cutover

**Fingerprint note:** Phase 2 may compute a scoped legacy **basis fingerprint** solely to (a) revalidate Phase C and (b) initialize Synchronization Position. That is **not** Phase 3 discovery/catch-up. Do not claim G-12–G-15 complete.

---

## 5. Target module layout (extend existing)

Continue the Phase 1 feature folder. Do **not** create a second Stock Ledger context.

| Layer | Path | Phase 2 expectation |
|---|---|---|
| Domain | `Bilreg.Domain/InventoryContext/StockLedgerFeature/` | Only if reconstruction facts require small domain extensions (factories/kinds already partially present) |
| Application | `Bilreg.Application/InventoryContext/StockLedgerFeature/` | Orchestration / calculation / UseCases as needed; reuse existing ports and UoW |
| Application ports | `…/Ports/` | Implement live adapters for G-05 and G-08; leave other ports contracts-only |
| Infrastructure | `Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/` | Live legacy **read** adapters; fingerprint helper if infrastructure-owned |
| SqlDb | `Bilreg.SqlDb/InventoryContext/StockLedgerFeature/` | Optional additive/index scripts only; Scope coexistence columns already exist from P1-S5 |
| Test | `Bilreg.Test/InventoryContext/StockLedgerFeature/` | Slice tests; reconstruction portion of G-23 |
| Api | **No public reconstruction/write endpoints required for Phase 2 exit.** Operator-visible status is the persisted Scope state (queryable via repo/tests). Optional diagnostic read remains G-26. |

**Legacy spike:** Leave `StokFeature` FEFO / kartu-stok spike untouched. Prefer Stock Ledger ports over “fixing” defective `tb_buku_dal` enumerable binding as the reconstruction path.

### Established contracts to reuse (already in repo)

Prefer these over inventing parallel names:

| Established | Role in Phase 2 |
|---|---|
| `ILegacyStockReadPort` + balance/journal DTOs | G-05 live adapter target |
| `IAvailabilityDiscoveryPort` + candidate DTOs | G-08 live adapter target |
| `StockLedgerScopeStateModel` transitions | Phase A/C status machine |
| `SynchronizationPositionType` | Initialize opaque position at success |
| `IStockConsequenceUnitOfWork` | Short Phase C Ledger persist |
| Movement / Layer / Position / Scope / Idempotency repos | Persist reconstructed baseline |
| `StockFactOriginEnum.Reconstructed` | Origin on reconstructed facts |

Exact new class/method names for calculators, claim helpers, and handlers are **left to the implementing agent**, provided they follow repository MediatR / Application / Infrastructure patterns.

---

## 6. Slice map (execute in order)

```text
P2-S1 Live legacy reconstruction reads (G-05)
   -> P2-S2 Reconstruction basis / fingerprint capture (init-only)
   -> P2-S3 Domain readiness for reconstructed baseline facts
   -> P2-S4 Baseline calculation + ambiguity classification
   -> P2-S5 Phase A claim (short TX + concurrent claim safety)
   -> P2-S6 Phase B/C orchestration + persist (G-10 core)
   -> P2-S7 Availability Discovery live adapter (G-08)
   -> P2-S8 Reconstruction harness, recovery, Phase 2 exit hardening
```

Each slice must leave the solution **compiling**. Prefer disposable/test DB (`devTest` / `DEVTEST`) for persistence and live-read fixtures — never write reconstruction output as a reason to mutate `HOSPITAL_HPL` legacy authority rows.

Normative reconstruction boundary (every orchestration slice must respect):

```text
TX-A: claim Reconstructing, commit
No long TX: read and calculate baseline
TX-C: revalidate legacy basis, persist baseline, initialize sync position, commit
```

---

## 7. Slice specifications

### P2-S1 — Live legacy reconstruction read adapter (G-05)

| Field | Detail |
|---|---|
| **Objective** | Provide parameterized, read-only legacy stock/journal access by Item + Receipt Source **across all Stock Locations**, with deterministic ordering. |
| **Scope** | G-05 only. Implement the existing `ILegacyStockReadPort` contract against authoritative `tb_stok` / `tb_buku`. No reconstruction orchestration. No legacy writes. |
| **Dependencies** | Phase 1 ports + schema; Phase 0 query evidence ([`evidence/phase-0-profile-results.md`](./evidence/phase-0-profile-results.md)) |
| **Affected projects** | Infrastructure, Test; optionally SqlDb for **optional** proposed-index scripts |
| **Affected folders/classes** | New Infrastructure adapter(s) under `StockLedgerFeature/`; reuse Application port DTOs; do not treat legacy `StokFeature` DAL as the sole reconstruction API if it cannot express cross-location Item+DO reads cleanly |
| **Required behavior** | (1) List surviving current balances for the Reconstruction Scope; (2) list journal rows for the same scope including historical provenance rows; (3) deterministic input order; (4) bounded parameterized queries |
| **Database** | Read-only against legacy tables on disposable/test environments. Optional additive index **scripts** may be checked in (Phase 0 proposed `(barang, do, …)` shapes) but production apply remains DBA-owned (G-25). Scope coexistence columns already exist — do not invent parallel scope tables. |
| **Tests** | Multi-location Item+DO fixture returns balances/journals from all locations; empty scope returns empty collections; ordering is stable; no write side effects |
| **Acceptance** | G-05 acceptance: multi-location fixture reconstructs from a bounded query; input includes surviving current rows and historical rows required for provenance; query/SLO notes recorded in the slice summary |
| **Rollback** | Remove adapter + tests; leave port contract intact |
| **Risks** | Accidental long unindexed scans; rewriting `tb_buku_dal` in place and breaking callers; treating synthetic/duplicate journal ids as unique chronology |
| **Deliverables** | Live G-05 adapter + tests + short slice summary |

---

### P2-S2 — Reconstruction basis / fingerprint capture (init-only)

| Field | Detail |
|---|---|
| **Objective** | Compute a mechanism-neutral scoped legacy **basis fingerprint** suitable for Phase C revalidation and for initializing Synchronization Position on successful reconstruction. |
| **Scope** | Reconstruction portion of sync ADR only. Capture opaque bytes + algorithm version from a scoped legacy snapshot (balances + journals). **Do not** implement change discovery, set-diff, catch-up, or Freshness Gate. |
| **Dependencies** | P2-S1 |
| **Affected projects** | Application and/or Infrastructure, Test |
| **Affected folders/classes** | Small helper/service owned where SQL-free vs SQL-bound fits repo patterns; may feed later Phase 3 discovery but must not claim G-13 done |
| **Required behavior** | Same scoped inputs ⇒ same opaque value for a fixed algorithm version; algorithm version is explicit and stored with the position; fingerprint is a freshness/basis token, not an authority claim |
| **Forbidden** | Hard-coding `(fd_tgl_jam_mutasi, fs_kd_trs)` as Synchronization Position schema; advancing stored position for post-baseline catch-up; implementing `DiscoverChanges` acceptance |
| **Database** | None beyond reads already available via P2-S1 |
| **Tests** | Deterministic fingerprint for fixed fixture; different fixture ⇒ different value; algorithm version present; no Domain SQL |
| **Acceptance** | Position can be produced for CompleteReconstruction without watermark-alone semantics; Phase C can compare before/after basis |
| **Rollback** | Remove helper + tests |
| **Risks** | Over-building Phase 3 discovery inside this slice; unstable hashing that flips on non-material column noise |
| **Deliverables** | Basis/fingerprint helper + unit tests + slice summary |

---

### P2-S3 — Domain readiness for reconstructed baseline facts

| Field | Detail |
|---|---|
| **Objective** | Close any **small** Domain gaps required so reconstructed Movements/Layers can be expressed with `Origin = Reconstructed`, depleted retention, and accountable identities — without inventing unavailable historical layer ids (BR-STL-065–069). |
| **Scope** | Domain-only readiness for G-10 persist. Inspect existing Movement/Layer factories first; extend only what reconstruction cannot already express. |
| **Dependencies** | Phase 1 domain (P1-S2/S3); no dependency on P2-S4 calculation results beyond knowing requirements |
| **Affected projects** | Domain, Test |
| **Required behavior** | Reconstructed layers receive **new** accountable identities; depleted reconstructed layers with Remaining Quantity = 0 are representable; reconstructed facts remain distinguishable by origin; reconstruction outcome can be linked to an accountable Source Transaction Reference / idempotency key later in P2-S6 |
| **Forbidden** | `IsAuthoritative`; FEFO; mutating completed movements in place; encoding runtime authority into enums |
| **Database** | None |
| **Tests** | Factory/invariant tests for reconstructed/depleted shapes; origin remains orthogonal to Reconstruction Status |
| **Acceptance** | Domain can express a balanced reconstructed baseline payload that later slices persist; solution builds |
| **Rollback** | Revert domain additions |
| **Risks** | Large domain redesign; adding unused movement kinds “for completeness”; conflating reconstruction with sync origin |
| **Deliverables** | Minimal domain extensions (if any) + tests + slice summary |

---

### P2-S4 — Baseline calculation + ambiguity classification

| Field | Detail |
|---|---|
| **Objective** | Pure calculation of a proposed Stock Ledger baseline (layers/positions intent + outcome classification) from G-05 read results — **outside** any long write transaction. |
| **Scope** | Calculation + classification portion of G-10; reconstruction portion of G-16 (intentional depleted-layer difference vs real mismatch). No persistence. No claim TX. |
| **Dependencies** | P2-S1 (inputs), P2-S3 (output shapes) |
| **Affected projects** | Application (preferred) and/or Domain helpers, Test |
| **Required behavior** | (1) Scope = Item + Receipt Source across all locations; (2) build active layers from surviving legacy balances; (3) retain depleted reconstructed layers when history supports them and legacy zero-rows are absent; (4) reconcile Remaining Quantity to legacy authority at the identified basis; (5) classify `Balanced` vs `Inconsistent` with explicit reason; (6) do not invent unavailable historical identities; (7) material ambiguity ⇒ Inconsistent (BR-STL-103/105); (8) deterministic fallback ordering only when it does **not** change material outcomes (BR-STL-104) |
| **Guidance (not prescription)** | Prefer a **balance-anchored** baseline (authority Remaining Quantity from `tb_stok`, provenance enrichment from `tb_buku`) over replaying every journal row as a Ledger movement. Full journal-as-movement materialization is unnecessary for Phase 2 and risks Phase 3 sync duplication. Implementing agents may refine details after inspecting fixtures. |
| **Database** | None |
| **Tests** | Multi-location DO; depleted rows absent from `tb_stok` but retained in proposed Ledger layers; quantity mismatch ⇒ Inconsistent; ambiguous material outcomes ⇒ Inconsistent; repeated calculation on same input is deterministic |
| **Acceptance** | Calculator produces an explicit outcome suitable for Phase C persist or Inconsistent marking; no SQL in Domain |
| **Rollback** | Remove calculator + tests |
| **Risks** | Silent “force balance”; treating Batch as allocation identity; building full event-sourced replay |
| **Deliverables** | Baseline calculator + unit tests + slice summary |

---

### P2-S5 — Phase A reconstruction claim

| Field | Detail |
|---|---|
| **Objective** | Acquire reconstruction work for one Item + Receipt Source in a **short** transaction: coexistence state becomes `Reconstructing`, then commit — before any long history calculation. |
| **Scope** | Phase A of G-10 only. Use existing Scope state transitions (`NotReconstructed` → `ReconstructionRequired` → `Reconstructing` as already modeled). |
| **Dependencies** | Phase 1 Scope repo/UoW patterns; P2-S2 optional for storing claimed basis label later (not required to compute fingerprint inside TX-A) |
| **Affected projects** | Application, Infrastructure (if claim needs conditional update), Test |
| **Required behavior** | (1) Create Scope row if missing; (2) claim exactly one active reconstruction per scope; (3) concurrent claimants ⇒ one winner / others fail closed or no-op without corrupting state; (4) do not read/calculate full history inside the claim TX; (5) do not persist Movements/Layers in TX-A |
| **Concurrency note** | Phase 1 documented Scope **lacks OCC**. This slice should add the **minimum** claim-safety mechanism required for “one committed claim” (conditional status update and/or equivalent). Do not expand into full mixed-writer lock-order enforcement (Phase 3/9). |
| **Database** | Disposable DB writes to additive Scope table only |
| **Tests** | Happy-path claim; duplicate concurrent claim ⇒ single Reconstructing winner; illegal transitions rejected; legacy tables untouched |
| **Acceptance** | Operators/tests can observe `Reconstructing`; claim TX is short; VB6 not blocked by claim semantics |
| **Rollback** | Remove claim orchestration; reset disposable Scope rows |
| **Risks** | Holding locks across Phase B; last-writer-wins Scope updates without claim predicates |
| **Deliverables** | Phase A claim path + tests + slice summary |

---

### P2-S6 — Phase B/C orchestration + persist (G-10 core)

| Field | Detail |
|---|---|
| **Objective** | End-to-end Initial Reconstruction for one scope: after claim, read/calculate outside long write TX; revalidate basis; persist baseline **or** mark `Inconsistent`; initialize Synchronization Position on success. |
| **Scope** | G-10 core acceptance. Wire P2-S1…S5. Reuse `IStockConsequenceUnitOfWork` for short Phase C Ledger persistence where appropriate. |
| **Dependencies** | P2-S1–S5; Phase 1 repos/UoW |
| **Affected projects** | Application (UseCase/handler/service), Test; DI registration **only as needed** for reconstruction tests / internal invocation |
| **Required behavior** | (1) Phase B: read via G-05, capture basis fingerprint, calculate baseline; (2) Phase C: revalidate claim still `Reconstructing` and legacy basis unchanged; (3) if basis changed ⇒ do **not** persist stale baseline — retry or leave not-current / recoverable claim state per domain rules; (4) if balanced ⇒ persist Movement/Position(+Layers)/Scope=`Reconstructed` + opaque Synchronization Position + reconstruction basis version; origin=`Reconstructed`; (5) if inconsistent ⇒ Scope=`Inconsistent` with reason, no fake-balanced layers; (6) repeated request for already reconstructed same basis is idempotent (no duplicate quantities/layers); (7) success does not rewrite legacy rows and does not set authority |
| **Idempotency** | Use existing source idempotency persistence; extend kind/key shape only if reconstruction cannot safely reuse `SourceConsequence` without colliding with later FO keys |
| **API surface** | Prefer Application command/handler (MediatR) consistent with Inventory UseCases. **No** public production HTTP enablement required for exit. |
| **Database** | Disposable DB additive Ledger writes only; legacy read-only |
| **Tests** | Happy path → `Reconstructed` + initialized sync position; duplicate trigger idempotent; basis change during Phase B ⇒ no stale persist; Inconsistent path; multi-location DO; depleted-layer retention after persist; UoW failure rolls back Ledger writes |
| **Acceptance** | G-10 acceptance criteria pass; no long TX spans history calculation; operators can identify `Reconstructing` / `Reconstructed` / `Inconsistent` via Scope state |
| **Rollback** | Controlled removal of incomplete additive reconstruction rows on disposable DB; legacy untouched |
| **Risks** | Persisting before revalidation; using Native origin; enabling production DI against hospital DB; implementing Freshness Gate “while we’re here” |
| **Deliverables** | Reconstruction orchestration + integration tests + slice summary |

---

### P2-S7 — Availability Discovery live adapter (G-08)

| Field | Detail |
|---|---|
| **Objective** | Implement provisional Availability Discovery from legacy authority by Item + Stock Location (+ optional Expiration Date), returning Receipt Source candidates — **not** final FIFO allocation. |
| **Scope** | G-08 live adapter against existing `IAvailabilityDiscoveryPort`. Depends on G-05 read capability / patterns but remains a separate port (Availability ≠ Provenance). |
| **Dependencies** | P2-S1 (shared legacy read patterns); reconstruction may be used by later callers but this slice must not implement outbound allocation |
| **Affected projects** | Infrastructure, Test; optional Application wiring |
| **Required behavior** | Discover authoritative available quantity from legacy records; return provisional candidates; treat depleted/missing legacy rows correctly; do **not** treat Stock Ledger layers as authority merely because reconstruction completed; do **not** perform final FIFO here |
| **Freshness note** | Callers in later phases must reconstruct/synchronize before trusting Ledger-enriched allocation. This slice may return candidates from legacy authority without implementing G-12 Freshness Gate. Document that `StaleOrNotCurrent` orchestration belongs to Phase 3/5 callers if not fully meaningful yet. |
| **Database** | Read-only legacy |
| **Tests** | Item+Location with multiple DO candidates; optional ED filter; empty/insufficient stock outcome; depleted rows not offered as available qty |
| **Acceptance** | G-08 acceptance for provisional discovery; no FIFO outbound; no FO writes |
| **Rollback** | Remove adapter + tests; leave port contract |
| **Risks** | Using discovery result as final allocation; reading Ledger instead of legacy authority during coexistence |
| **Deliverables** | Live G-08 adapter + tests + slice summary |

---

### P2-S8 — Reconstruction harness, recovery, Phase 2 exit hardening

| Field | Detail |
|---|---|
| **Objective** | Close Phase 2 with reconstruction-focused coexistence tests, controlled recovery for incomplete additive reconstruction, documentation of deferred items, and an exit report. |
| **Scope** | Reconstruction portion of G-23; reconstruction portion of G-16 intentional-difference assertions; rollback/containment story from the roadmap. |
| **Dependencies** | P2-S6 (and P2-S7 for any discovery-related markers) |
| **Affected projects** | Test, docs; optionally Application recovery helper |
| **Required behavior** | (1) Cover roadmap Phase 2 test themes still missing after P2-S6: concurrent reconstructors, legacy write during Phase B, retry, duplicate trigger, large-history timeout/bounded-query failure mode as practical on disposable DB; (2) un-skip or replace **only** reconstruction-relevant G-23 placeholders; leave Phase 3/4/5 skips in place; (3) provide a **controlled recovery** path that deletes/voids incomplete additive reconstruction output without touching legacy authority; (4) record index/SLO residual debt for G-25 |
| **Forbidden** | Claiming G-12–G-17 done; enabling production coexistence; mutating `HOSPITAL_HPL` for convenience |
| **Database** | Disposable DB only for recovery drills |
| **Tests** | Harness scenarios above; recovery leaves legacy rows unchanged; Phase 1 suite remains green |
| **Acceptance** | Phase 2 exit criteria checklist (section 9) can be marked complete; solution builds; StockLedgerFeature tests pass for implemented scenarios |
| **Rollback** | N/A (documentation + tests); recovery tool must be safe/no-op when unused |
| **Risks** | Expanding into sync harness; writing a Phase 3 implementation “report” that pretends Freshness Gate exists |
| **Deliverables** | Hardening tests + recovery notes + [`stock-ledger-phase2-implementation-report.md`](./stock-ledger-phase2-implementation-report.md) (create when coding completes) + this plan’s slice progress updated to COMPLETE |

---

## 8. Cross-cutting Phase 2 rules

1. **Repository first:** Inspect existing StockLedgerFeature code before adding types; extend ports/repos/UoW rather than parallel stacks.  
2. **Clean Architecture:** Domain has no SQL/Dapper; Application orchestrates; Infrastructure adapts legacy reads.  
3. **Read-only legacy authority:** Reconstruction never rewrites `tb_stok` / `tb_buku` merely to make reconstruction convenient.  
4. **No authority transfer:** Success ⇒ baseline exists ≠ authority moved ≠ VB6 blocked ≠ permanently synchronized.  
5. **Short transactions only for claim/persist:** History calculation stays outside long write locks.  
6. **Pragmatism:** No event sourcing, broker, CDC requirement, or new architectural framework.  
7. **FEFO spike:** Do not extend `StokFeature` FEFO.  
8. **DI:** Register reconstruction/read adapters when slices need them for tests or internal invocation; do **not** enable production FO/stock write traffic.  
9. **Tests per slice:** Only the tests listed for that slice; do not implement full mixed-writer coexistence harness.  
10. **Summaries:** Each completed slice should produce a short `stock-ledger-phase2-sN-implementation-summary.md` in the same style as Phase 1 slice summaries (WHAT / WHY / deviations / handoff).

---

## 9. Phase 2 exit criteria (definition of done)

Phase 2 is complete only when all of the following hold:

- [ ] G-05 live adapter accepted: Item + Receipt Source reads across all locations with deterministic ordering and bounded queries.  
- [ ] G-10 accepted: phased A/B/C reconstruction; concurrent claim safe; basis change during calculation does not persist stale baseline; repeated request idempotent.  
- [ ] Successful reconstruction initializes opaque Synchronization Position + algorithm version and sets Scope to `Reconstructed` **or** surfaces `Inconsistent` with reason.  
- [ ] Depleted reconstructed layers are retained in Ledger even when corresponding zero `tb_stok` rows are absent.  
- [ ] G-08 live provisional Availability Discovery exists (candidates from legacy authority; not final FIFO).  
- [ ] Reconstruction portion of G-16/G-23 covered for intentional depleted-layer difference and reconstruction races; later-phase G-23 markers remain skipped.  
- [ ] No long write transaction spans history calculation.  
- [ ] No `IsAuthoritative`; no legacy row rewrites for reconstruction convenience; VB6 remains conceptually unblocked.  
- [ ] Solution builds; StockLedgerFeature tests for Phase 2 slices pass; Phase 1 tests remain green.  
- [ ] Controlled recovery path for incomplete additive reconstruction is documented/available on disposable DB.  
- [ ] Phase 2 implementation report published; this plan’s slice progress table updated to COMPLETE.  
- [ ] Explicit handoff notes list Phase 3 residuals (Freshness Gate, discovery catch-up, G-13 experiments, production indexes).

---

## 10. Handoff to Phase 3+

When Phase 2 exits:

| Next | Uses from Phase 2 |
|---|---|
| Phase 3 Sync / Freshness | Initialized Synchronization Position; basis fingerprint helper; reconstructed baseline; G-05 reads; Scope `SynchronizationRequired` transitions |
| Phase 4 DO Receipt | Reconstructed-or-native scope readiness patterns; UoW; still needs live Legacy Compatibility Writer |
| Phase 5 Outbound / transfer | G-08 provisional discovery + FIFO domain already in Phase 1; still needs freshness before allocation |

Do **not** start Phase 3 inside a Phase 2 slice.

---

## 11. References for implementers

| Read first | Why |
|---|---|
| [`stok-ledger-domain.md`](./stok-ledger-domain.md) §7.9, §8.5, §10.8; BR-STL-062–075, 102–107 | Reconstruction business rules |
| [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md) § Phase 2, §5.3 | Objectives, TX boundaries, test themes |
| [`stock-ledger-gap-analysis.md`](./stock-ledger-gap-analysis.md) G-05, G-08, G-10 | Acceptance language |
| [`stock-ledger-feasibility-review.md`](./stock-ledger-feasibility-review.md) §2.3 | Phase A/B/C protocol |
| [`adr/ADR-stock-ledger-legacy-change-discovery.md`](./adr/ADR-stock-ledger-legacy-change-discovery.md) | Position/fingerprint shape (init now; catch-up later) |
| [`adr/ADR-stock-ledger-mixed-writer-concurrency.md`](./adr/ADR-stock-ledger-mixed-writer-concurrency.md) | Reconstruction vs legacy write conflict expectation |
| [`stock-ledger-phase1-s8-implementation-summary.md`](./stock-ledger-phase1-s8-implementation-summary.md) | Exact foundation and known debt |
| [`evidence/phase-0-profile-results.md`](./evidence/phase-0-profile-results.md) | Proposed legacy indexes / volume reality |
| [`docs/skills/use-case-generation.md`](../../skills/use-case-generation.md) | UseCase/MediatR patterns when adding orchestration |
| [`docs/skills/feature-persistence-generation.md`](../../skills/feature-persistence-generation.md) | If claim concurrency needs repo/DAL adjustments |

---

## 12. Suggested agent prompt (per slice)

When executing a single slice, use a prompt of this form:

```text
Implement only <P2-SN> from docs/contexts/stok-ledger/stock-ledger-phase2-implementation-plan.md.

Repository-first: inspect existing StockLedgerFeature code and Phase 1 summaries before coding.
Do not implement later Phase 2 slices or any Phase 3+ scope.
Keep the solution compiling. Prefer disposable/test DB. Do not rewrite legacy authority rows.
When done: run relevant StockLedgerFeature tests, write stock-ledger-phase2-sN-implementation-summary.md,
and update the slice progress table in the Phase 2 plan.
```
