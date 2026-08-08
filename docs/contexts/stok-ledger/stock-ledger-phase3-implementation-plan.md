# Stock Ledger — Phase 3 Implementation Plan

**Artifact status:** Executable Phase-3 plan  
**Date:** 2026-08-07  
**Phase:** 3 — Incremental Legacy Synchronization and Freshness Gate  
**Governing baseline (LOCKED):** Phase 0 Exit Review **PASS WITH RISKS** — [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md)  
**Phase 1 foundation (COMPLETE):** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Phase 2 baseline (COMPLETE):** [`stock-ledger-phase2-implementation-report.md`](./stock-ledger-phase2-implementation-report.md) — latest slice [`stock-ledger-phase2-s8-implementation-summary.md`](./stock-ledger-phase2-s8-implementation-summary.md)  
**Roadmap:** [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md) § Phase 3  
**Gaps in scope:** G-12–G-16 (material/sync portion), synchronization portion of G-23, initial G-24 observability, sync/native serialization portion of G-17 (interim only)  
**Standards:** [`docs/ENGINEERING.md`](../../ENGINEERING.md), [`docs/DATABASE.md`](../../DATABASE.md), [`docs/NAMING.md`](../../NAMING.md)

### Slice progress

| Slice | Status | Summary |
|---|---|---|
| P3-S1 | **PENDING** | — |
| P3-S2 | **PENDING** | — |
| P3-S3 | **PENDING** | — |
| P3-S4 | **PENDING** | — |
| P3-S5 | **PENDING** | — |
| P3-S6 | **PENDING** | — |
| P3-S7 | **PENDING** | — |
| P3-S8 | **PENDING** | — |

---

## 1. Purpose

Convert the Phase 3 roadmap into **small, independently executable slices** that a mid-level coding agent can implement with minimal ambiguity.

Phase 3 keeps reconstructed (or later Native-origin) Stock Ledger scopes **current** when VB6 continues to change authoritative `tb_stok` / `tb_buku`, and proves a fail-closed **Legacy Freshness Gate** before Ledger-dependent decisions. It does **not** enable FO/native stock writes, transfer authority away from legacy records, or claim mixed-writer production safety.

---

## 2. Code-base evaluation after Phase 2 (adjust the roadmap)

Inspect the repository before each slice. The Phase 3 roadmap text remains directionally correct, but **much scaffolding already exists**. Do not rebuild parallel stacks.

### 2.1 Already delivered — reuse, do not redo

| Asset | Repository state | Phase 3 implication |
|---|---|---|
| Feature folder | `InventoryContext/StockLedgerFeature/` across Domain / Application / Infrastructure / SqlDb / Test | Extend only |
| Sync Domain state machine | `StockLedgerScopeStateModel` already has `MarkLegacyChangePending` → `RequireSynchronization` → `CompleteSynchronization` / `MarkSynchronizationInconsistent` | Orchestration must call these; do not invent authority states |
| Opaque Synchronization Position | Columns + `SynchronizationPositionType`; initialized at reconstruction with `fingerprint-v1` | G-14 **storage** is done; Phase 3 owns **advancement after committed catch-up** |
| Fingerprint calculator | `LegacyReconstructionBasisCalculator` (`fingerprint-v1`) | Discovery **must** reuse this algorithm (or thin wrapper) — forbid a second fingerprint implementation |
| Discovery / reconciliation contracts | `ILegacyChangeDiscoveryPort`, `IStockReconciliationPort` + DTOs/enums | Implement live adapters; extend DTOs only if detection needs richer fields |
| Anticipatory shapes | `StockFactOriginEnum.LegacySynchronized`, `StockSourceIdempotencyKindEnum.SyncBatch`, Correction/Reversal movement factories | Ready for catch-up; unused by a sync use case today |
| Live reads / discovery | `LegacyStockReadPort` (G-05), `AvailabilityDiscoveryPort` (G-08 provisional) | Re-read / replay inputs; Availability must **not** become FIFO authority until Freshness Gate exists |
| Reconstruction A/B/C + recovery | `ReconstructStockLedgerBaselineCommand`, claim/calc/persist, `RecoverIncompleteReconstructionService` | Baseline prerequisite for sync; recovery deletes additive rows only (sync voids must **not** erase Ledger history) |
| UoW / repos | `IStockConsequenceUnitOfWork`, Movement/Position/Scope/Idempotency repos | Prefer short sync-batch TX via existing UoW patterns |
| Coexistence harness | One activated depleted-layer test; **7 skipped** Phase 3/4/5 placeholders | Un-skip only Phase-3-owned scenarios |
| Production DI / HTTP | None | Keep none through Phase 3 exit |

### 2.2 Still missing — Phase 3 owns

1. Live `ILegacyChangeDiscoveryPort` (fingerprint compare + set-diff / bounded re-derive; G-13 detection experiments).  
2. Pure interpretation of discovered deltas → accountable correction/reversal/update intents.  
3. Live material reconciliation adapter (G-16 P0 portion).  
4. Incremental catch-up orchestration that applies `LegacySynchronized` facts, uses `SyncBatch` idempotency, reconciles, and advances Synchronization Position only on success (G-14/G-15).  
5. Freshness Gate orchestration (G-12) — fail closed / synchronize-first; never blocks VB6.  
6. Sync crash/retry + sync-vs-native serialization **interim** within .NET (partial G-17; not live VB6 FQ-06 proof).  
7. Activation of sync-relevant G-23 harness scenarios.  
8. Initial observability notes / explainable last sync outcome (initial G-24 — not ops dashboard).

### 2.3 Roadmap misalignment corrections

| Roadmap / planning assumption | Actual post–Phase-2 state | Plan adjustment |
|---|---|---|
| Phase 3 must invent Synchronization Position persistence | Already persisted and initialized | No storage-only slice |
| Phase 3 must invent sync Domain transitions | Already unit-tested | Wire orchestration only |
| Fingerprint work starts in Phase 3 | `fingerprint-v1` already used for reconstruction | Reuse calculator behind discovery port |
| Availability Discovery is Phase 3 | Shipped in P2-S7 as provisional | Do not re-implement; gate callers in P3-S5 |
| Full G-17 mixed-writer VB6 proof is Phase 3 | FQ-06 still open; roadmap Phase 9 gate | Phase 3 proves sync/native .NET serialization only |
| Full G-24 ops suite is Phase 3 | Roadmap says “initial G-24” | Structured outcomes + docs only; dashboard = Phase 8 |
| One large Phase-3 delivery | Phase 1/2 used 8 incremental slices | Keep **8 slices** (see §6) |

---

## 3. Locked decisions (do not reopen)

| Topic | Locked baseline |
|---|---|
| Runtime authority (Stage B) | `tb_stok` + `tb_buku` — sync success does **not** transfer authority |
| Origin labels | `Native` / `Reconstructed` / `LegacySynchronized` — origin only |
| Reconstruction / reconciliation scope | Item + Receipt Source across **all** Stock Locations |
| Write consistency candidate | Item + Receipt Source + Stock Location |
| Sync mechanism | Fingerprint + bounded replay; mismatch ⇒ set-diff and/or scoped re-derive ([sync ADR](./adr/ADR-stock-ledger-legacy-change-discovery.md)) |
| Synchronization Position | Mechanism-neutral opaque value + algorithm version; advance **only** after committed catch-up + successful material reconciliation |
| Fingerprint algorithm continuity | Prefer `fingerprint-v1` via `LegacyReconstructionBasisCalculator`; bump algorithm version only with explicit migration notes |
| Depleted layers | Ledger retains zero Remaining Quantity; legacy may delete zero `tb_stok` — intentional difference, not material drift |
| Void / delete handling | Physical `tb_buku` / zero-row absence → accountable Ledger **correction/reversal**; never erase Stock Ledger history |
| Freshness Gate | Replaces rejected Authority Gate; never rejects a legacy write because scope is Native/Reconstructed |
| Lock order (interim) | Item → Receipt Source → Location → legacy row id ([concurrency ADR](./adr/ADR-stock-ledger-mixed-writer-concurrency.md)) |
| CT / CDC / universal change log | Deferred; do not introduce unless ADR is amended |

---

## 4. Explicit exclusions (later phases)

Do **not** implement in any Phase 3 slice:

- Live `ILegacyCompatibilityWriterPort` / DO Receipt / FO post-void (Phase 4 / G-11, G-19)
- Native outbound, FIFO allocation orchestration, stock transfer (Phase 5)
- Reservation / Virtual Stock Location (Phase 6)
- Provenance Discovery behavior / returns (Phase 7 / G-09)
- Full ops dashboard, lag metrics productization, poison-batch ops suite (Phase 8 / G-24 rest, G-25 SLOs)
- Live concurrent VB6 session proof / production mixed-writer enablement (Phase 9 / FQ-06 / G-17 production gate)
- Production DI registration for FO traffic; public stock write endpoints
- Production index apply on `HOSPITAL_HPL` without DBA approval (G-25 residual — non-blocking to start)
- Porting `clbGenStokX1`; `IsAuthoritative`; Stage C cutover; watermark-alone / `fs_kd_trs`-alone cursors
- Full re-reconstruction as the steady-state sync path (allowed only as last-resort fallback when ADR mismatch rule requires scoped re-derive)

---

## 5. Target module layout (extend existing)

Continue the Phase 1/2 feature folder. Do **not** create a second Stock Ledger context.

| Layer | Path | Phase 3 expectation |
|---|---|---|
| Domain | `Bilreg.Domain/InventoryContext/StockLedgerFeature/` | Only small extensions if sync facts cannot already be expressed (prefer existing Correction/Reversal/`LegacySynchronized`) |
| Application | `Bilreg.Application/InventoryContext/StockLedgerFeature/` | Discovery helpers, delta interpreter, Freshness Gate, sync UseCases; reuse ports/UoW |
| Application ports | `…/Ports/` | Live adapters for discovery + reconciliation; extend DTOs sparingly |
| Infrastructure | `Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/` | Live discovery/reconcile adapters using G-05 reads + Ledger repos |
| SqlDb | `Bilreg.SqlDb/InventoryContext/StockLedgerFeature/` | Prefer **no** new tables; optional additive columns only if explainability cannot reuse Scope fields |
| Test | `Bilreg.Test/InventoryContext/StockLedgerFeature/` | Slice tests; activate sync G-23 placeholders |
| Api | **No public sync/write endpoints required for Phase 3 exit.** |

### Established contracts to reuse

| Established | Role in Phase 3 |
|---|---|
| `ILegacyChangeDiscoveryPort` + delta kinds | G-13 live adapter target |
| `IStockReconciliationPort` + outcomes | G-16 material classification target |
| `ILegacyStockReadPort` | Snapshot inputs for fingerprint / set-diff / re-derive |
| `LegacyReconstructionBasisCalculator` | Fingerprint continuity (`fingerprint-v1`) |
| `StockLedgerScopeStateModel` sync transitions | State machine for catch-up / Freshness |
| `SynchronizationPositionType` | Durable opaque position |
| `StockFactOriginEnum.LegacySynchronized` | Origin on post-baseline sync facts |
| `StockSourceIdempotencyKindEnum.SyncBatch` | Catch-up idempotency |
| `StockMovementModel.Correct` / `Reverse` | Void/update accountable history |
| `IStockConsequenceUnitOfWork` | Short sync-batch Ledger TX |
| `AvailabilityDiscoveryOutcomeEnum.StaleOrNotCurrent` | Reserved Freshness outcome for later callers |
| Coexistence harness placeholders | Activate Phase-3-owned scenarios |

Exact new class/method names for interpreters, gates, and handlers are left to the implementing agent, provided they follow repository MediatR / Application / Infrastructure patterns (prefer Inventory UseCase style like `ReconstructStockLedgerBaselineCommand`).

---

## 6. Slicing decision

### Decision

Phase 3 is divided into **eight incremental slices (P3-S1…P3-S8)**, same pattern as Phase 1 and Phase 2.

### Why not one large Phase-3 delivery

Discovery, void interpretation, material reconciliation, catch-up persistence, Freshness Gate, and harness activation fail for different reasons. A single delivery is hard to review, hard to roll back, and easy for a mid-level model to over-engineer.

### Why this slice order (vs inspiration list)

| Inspiration item | Disposition |
|---|---|
| Legacy Change Discovery foundation | **P3-S1** — still the critical missing adapter |
| Synchronization Position | **Folded into P3-S4** — storage already exists; advancement belongs with catch-up |
| Legacy update/void interpretation | **P3-S2** — pure Application calc before any persist (safer than burying inside orchestration) |
| Reconciliation (material) | **P3-S3** — required before position may advance |
| Incremental Synchronization Engine | **P3-S4** — G-14/G-15 core |
| Freshness Gate | **P3-S5** — after catch-up exists so the gate can synchronize-or-fail-closed |
| Recovery & Retry + sync/native serialize | **P3-S6** — harden after happy path |
| Coexistence Test Harness | **P3-S7** |
| Phase hardening & exit review | **P3-S8** |

### Slice map (execute in order)

```text
P3-S1 Live Legacy Change Discovery (G-13)
   -> P3-S2 Sync delta interpretation (void/update → accountable intents)
   -> P3-S3 Material reconciliation adapter (G-16 P0)
   -> P3-S4 Incremental catch-up + position advancement (G-14/G-15)
   -> P3-S5 Freshness Gate (G-12)
   -> P3-S6 Sync retry, crash safety, sync/native serialization interim
   -> P3-S7 Coexistence sync harness (G-23 sync portion)
   -> P3-S8 Phase 3 exit hardening + initial observability + report
```

Each slice must leave the solution **compiling**. Prefer disposable/test DB (`devTest` / `DEVTEST`) for persistence and live-read fixtures. Synchronization **consumes** legacy facts; it must not rewrite `tb_stok` / `tb_buku` except through an explicitly authorized recovery path (none required for Phase 3 exit).

Normative sync boundary (every orchestration slice must respect):

```text
Discover (fingerprint compare; on mismatch set-diff and/or scoped re-derive)
  -> Mark SynchronizationRequired (Domain transition)
  -> Interpret deltas into accountable Ledger intents (no history erase)
  -> Short TX: apply SyncBatch idempotent movements/layers + reconcile
  -> Advance Synchronization Position only if material reconciliation succeeds
  -> Else MarkSynchronizationInconsistent / leave prior position unchanged
```

---

## 7. Slice specifications

### P3-S1 — Live Legacy Change Discovery (G-13)

| Field | Detail |
|---|---|
| **Objective** | Implement deletion-aware Legacy Change Discovery against live/scoped authority snapshots, validating Phase 0 ADR detection kinds. |
| **Scope** | G-13 only. Live `ILegacyChangeDiscoveryPort`. Fingerprint compare + set-diff and/or `RequiresScopedReDerive`. **No** catch-up persist, Freshness Gate, or position advancement. |
| **Dependencies** | Phase 2 G-05 reads; `LegacyReconstructionBasisCalculator`; Phase 0 sync ADR; reconstructed Scope with stored position for mismatch tests |
| **Affected projects** | Infrastructure, Application (thin wrapper ok), Test |
| **Expected code areas** | New `LegacyChangeDiscoveryPort` (or equivalent) under Infrastructure; reuse `ILegacyStockReadPort`; call `LegacyReconstructionBasisCalculator.Compute` for `ComputeCurrentFingerprint`; implement `DiscoverChanges` using stored opaque position vs current fingerprint; set-diff Ledger-known journal identities (from Movement repos / known legacy ids) against surviving `tb_buku` rows |
| **Database impact** | Read-only legacy + read Ledger movement identities. No new tables. Optional proposed-index scripts remain DBA-owned (G-25). |
| **Tests** | Detection experiments on disposable fixtures: journal insert; journal/quantity update; `tb_stok` depletion delete; `tb_buku` void delete; backdated/tied mutation time; repost; unchanged scope ⇒ `Unchanged`; undeterminable ⇒ `Undeterminable`; fingerprint continuity with reconstruction-initialized position |
| **Acceptance criteria** | G-13 acceptance: all listed change kinds are detected without watermark-alone/`fs_kd_trs`-alone cursors; hash drift alone without set-diff/re-derive is insufficient; port docs remain true (does not advance position) |
| **Explicit exclusions** | Applying movements; Freshness Gate; FO writes; CT/CDC; second fingerprint algorithm without version bump |
| **Rollback strategy** | Remove live adapter + tests; leave port contract and fakes intact |
| **Risks** | Dual fingerprint implementations drifting from reconstruction; treating fingerprint mismatch as a delete stream without set-diff; scanning unbounded histories without Item+DO predicates |
| **Deliverables** | Live G-13 adapter + detection tests + `stock-ledger-phase3-s1-implementation-summary.md` |

---

### P3-S2 — Sync delta interpretation (void/update → accountable intents)

| Field | Detail |
|---|---|
| **Objective** | Pure Application interpretation of discovery deltas into proposed Stock Ledger intents (correction/reversal/quantity update / new layer establishment) without persisting. |
| **Scope** | Interpretation portion of G-15. Input = discovery deltas + current Ledger layers/positions. Output = explicit intent list + failure/ambiguity reasons. No SQL. No Scope transition. |
| **Dependencies** | P3-S1 (delta shapes); Phase 1 Movement Correction/Reversal factories; Phase 2 reconstructed baseline shapes |
| **Affected projects** | Application (preferred), Domain only if factories cannot express intents, Test |
| **Expected code areas** | New interpreter/calculator under Application `StockLedgerFeature/`; map `JournalVoidDelete` → reversal/correction intents; `BalanceDelete` / quantity changes → layer remaining updates via new movements; new inbound journal after baseline → `LegacySynchronized` establishment for **new** layers only; preserve existing layer establishment origin when only quantity changes (G-04 / G-15) |
| **Database impact** | None |
| **Tests** | Void-delete ⇒ accountable correction/reversal intent (no history erase); duplicate delta identity ⇒ single intent; quantity-only change does not rewrite establishment origin; material ambiguity ⇒ explicit inconsistent/undeterminable intent outcome; deterministic ordering |
| **Acceptance criteria** | Interpreter produces a reviewable intent payload suitable for P3-S4 persist; Domain remains SQL-free; no silent “force balance” |
| **Explicit exclusions** | Persistence; position advance; Freshness Gate; inventing Receipt Source / provenance |
| **Rollback strategy** | Remove interpreter + tests |
| **Risks** | Porting VB6 void control flow; deleting Ledger movements to “match” legacy absence; conflating Availability Discovery with sync interpretation |
| **Deliverables** | Interpreter + unit tests + slice summary |

---

### P3-S3 — Material reconciliation adapter (G-16 P0)

| Field | Detail |
|---|---|
| **Objective** | Live material reconciliation for one Item + Receipt Source that distinguishes intentional representational differences from real quantity drift. |
| **Scope** | G-16 P0 (allocation-gating classification). Implement `IStockReconciliationPort`. Operational reporting views remain Phase 8. |
| **Dependencies** | Phase 2 depleted-layer harness evidence; G-05 reads; Ledger Position/Layer repos |
| **Affected projects** | Infrastructure, Test; optional Application façade |
| **Expected code areas** | `StockReconciliationPort` (or equivalent); compare authoritative remaining qty from `tb_stok` vs Ledger Remaining Quantity across locations; classify `DepletedLayerVsAbsentLegacyRow` as intentional/balanced; material quantity mismatch ⇒ `MaterialInconsistency` |
| **Database impact** | Read-only |
| **Tests** | Depleted-layer intentional difference ⇒ Balanced or IntentionalDifference (not MaterialInconsistency); real qty mismatch ⇒ MaterialInconsistency; pending-sync marker may return `PendingSynchronization` when Scope state already requires sync (if cheap to read); no movement mutation |
| **Acceptance criteria** | G-16 P0 acceptance for material vs intentional; may be called before position advance in P3-S4 |
| **Explicit exclusions** | Ops dashboards; rewriting movements to force balance; authority transfer |
| **Rollback strategy** | Remove adapter + tests; leave port contract |
| **Risks** | False positives from row-shape equality; treating valuation-only gaps as hard blockers without explanation |
| **Deliverables** | Live G-16 adapter + tests + slice summary |

---

### P3-S4 — Incremental catch-up engine + Synchronization Position advancement (G-14 / G-15)

| Field | Detail |
|---|---|
| **Objective** | Idempotent catch-up for one reconstructed scope: discover → interpret → short TX apply → reconcile → advance opaque Synchronization Position, or fail closed to `Inconsistent` / retryable not-current. |
| **Scope** | G-14 + G-15 core. First complete sync orchestration. Wire P3-S1…S3. |
| **Dependencies** | P3-S1–S3; Phase 1 repos/UoW; Domain sync transitions; `SyncBatch` idempotency kind |
| **Affected projects** | Application (MediatR UseCase/handler), Infrastructure as needed, Test |
| **Expected code areas** | Sync command/handler (Inventory MediatR style); mark `RequireSynchronization` when changes detected; persist movements with `Origin = LegacySynchronized`; use `StockSourceIdempotencyKindEnum.SyncBatch`; update layer Remaining Quantity without rewriting establishment origin; call reconciliation before `CompleteSynchronization(newPosition)`; on failure leave prior position unchanged |
| **Required behavior** | (1) Scope must already be `Reconstructed` with a stored position; (2) short TX only for Ledger persist + Scope/idempotency — do not hold long locks across discovery calculation; (3) duplicate batch is quantity-neutral; (4) void-delete becomes correction/reversal history; (5) success ⇒ `SynchronizationState = Current` + advanced opaque position (usually new fingerprint); (6) material mismatch ⇒ `MarkSynchronizationInconsistent`; (7) never rewrite legacy authority rows; (8) never erase Ledger history to match deletes |
| **Concurrency note** | Add the **minimum** sync claim/serialization needed so two catch-up workers do not double-apply (conditional Scope state update and/or SyncBatch uniqueness). Full mixed-writer VB6 proof is out of scope. |
| **Database impact** | Additive Ledger writes only on disposable DB. Prefer existing tables. Add columns only if last-outcome explanation cannot reuse `InconsistencyReason` / existing Scope fields — justify in slice summary. |
| **Tests** | Happy path Legacy→Ledger catch-up; duplicate SyncBatch idempotent; void-delete → correction/reversal retained; crash before commit leaves prior position (simulate TX rollback); basis/fingerprint change mid-flight does not advance stale position; Inconsistent path; depleted intentional difference still balanced after sync |
| **Acceptance criteria** | G-14/G-15 acceptance; position advances only with committed batch + successful material reconciliation; repeated catch-up quantity-neutral |
| **Explicit exclusions** | Freshness Gate API (next slice); FO/native writes; production DI; full G-17 VB6 races |
| **Rollback strategy** | Disable/remove sync handler; additive sync movements remain historical evidence on disposable DB (do not “fix” by deleting history); Scope may be recovered via existing reconstruction recovery only when wiping incomplete baseline — not as routine void handling |
| **Risks** | Advancing position before reconcile; using Native/Reconstructed origin for sync facts; long TX spanning discovery; treating reconstruction recovery delete as sync void semantics |
| **Deliverables** | Catch-up UseCase + integration tests + slice summary |

---

### P3-S5 — Legacy Freshness Gate (G-12)

| Field | Detail |
|---|---|
| **Objective** | Before trusting Stock Ledger layers for a subsequent stock decision, prove freshness: unchanged proceed; pending changes synchronize or fail closed / mark not current. |
| **Scope** | G-12 orchestration. Gate **never** blocks VB6 and **never** rejects legacy writes because origin is Native/Reconstructed. |
| **Dependencies** | P3-S1 (detect), P3-S4 (catch-up) |
| **Affected projects** | Application, Test |
| **Expected code areas** | Freshness Gate service/helper under Application; inputs = scope key (+ optional “Ledger-dependent decision” context); uses discovery; if changes detected invokes catch-up; outcomes explicit (`Current`, `SynchronizedNow`, `StaleOrNotCurrent`, `Inconsistent`, etc. — names left to implementer); may inform future Availability callers via `StaleOrNotCurrent` without rewriting provisional G-08 adapter into an authority source |
| **Database impact** | None beyond what catch-up already does |
| **Tests** | Unchanged reconstructed scope passes; legacy change after reconstruction fails closed until sync succeeds; after successful sync, gate passes; undeterminable freshness ⇒ explicit not-current/Inconsistent; gate does not mutate legacy tables; gate does not claim authority |
| **Acceptance criteria** | G-12 acceptance; freshness explainable from Synchronization Position + last sync/reconcile outcome |
| **Explicit exclusions** | FIFO allocation; FO enablement; Authority Gate semantics; production HTTP |
| **Rollback strategy** | Remove gate + tests; catch-up remains usable directly |
| **Risks** | Reintroducing Authority Gate; silently skipping sync when fingerprint undeterminable; making Availability Discovery read Ledger as authority |
| **Deliverables** | Freshness Gate + tests + slice summary |

---

### P3-S6 — Sync retry, crash safety, sync/native serialization interim

| Field | Detail |
|---|---|
| **Objective** | Harden catch-up and Freshness Gate for crash/retry and serialize sync with **in-process** native write boundaries using the interim concurrency ADR — without claiming FQ-06. |
| **Scope** | Sync portion of G-17 interim + retry/containment needed for Phase 3 exit. Not live VB6 concurrent sessions. |
| **Dependencies** | P3-S4, P3-S5; Position OCC; Scope conditional updates |
| **Affected projects** | Application, Infrastructure (minimal), Test |
| **Expected code areas** | Bounded retry for version/deadlock conflicts on sync TX; ensure Freshness Gate / catch-up cannot commit using pre-sync layer snapshots when another sync wins; document serialization rule for future native consequence UoW (hook/comment or small guard API — **no** FO writer) |
| **Database impact** | None required |
| **Tests** | Sync crash mid-TX → retry safe / prior position retained; duplicate concurrent catch-up → one winner, quantity-neutral; simulated native-vs-sync race inside test doubles or Scope/Position OCC ⇒ native path cannot observe pre-sync layers as trusted; intentional depleted difference remains balanced |
| **Acceptance criteria** | Retry is safe; position never advances past unapplied/delete-undetected change; no claim of production VB6 mixed-writer proof |
| **Explicit exclusions** | Controlled VB6 sessions; production enablement; transfer multi-location lock productization |
| **Rollback strategy** | Revert retry helpers; happy-path catch-up remains |
| **Risks** | Scope creep into Phase 5/9 concurrency matrix; infinite retry loops; using reconstruction recovery delete for sync conflicts |
| **Deliverables** | Hardening tests + any minimal serialization guard + slice summary |

---

### P3-S7 — Coexistence sync harness (G-23 sync portion)

| Field | Detail |
|---|---|
| **Objective** | Activate Phase-3-owned coexistence harness scenarios against discovery + catch-up + Freshness Gate. |
| **Scope** | Synchronization portion of G-23. Leave Phase 4/5/8 placeholders skipped. |
| **Dependencies** | P3-S4–S6 |
| **Affected projects** | Test (primary), docs notes |
| **Expected code areas** | `StockLedgerCoexistenceHarnessPlaceholderTest` (or successor): un-skip / implement **Legacy→New**, **duplicate sync batch**, **real mismatch**, and **sync/native race** (in-process). Keep skipped: New→Legacy (Phase 4), concurrent outbound (Phase 5), alternating writers that require native FO writer (Phase 4+), partial live legacy+Ledger FO failure (Phase 4/8) |
| **Database impact** | Disposable DB fixtures only |
| **Tests** | Harness scenarios above; depleted-layer intentional difference remains green; Phase 1/2 suites remain green |
| **Acceptance criteria** | Sync-relevant G-23 markers pass without disabling VB6 conceptually; skipped list explicitly documents later-phase ownership |
| **Explicit exclusions** | Claiming full G-23 matrix done; production coexistence |
| **Rollback strategy** | Re-skip scenarios if unstable; do not delete discovery/catch-up code |
| **Risks** | Activating Phase 4 scenarios early; flaky fixture coupling to `HOSPITAL_HPL` |
| **Deliverables** | Activated harness tests + slice summary |

---

### P3-S8 — Phase 3 exit hardening + initial observability + report

| Field | Detail |
|---|---|
| **Objective** | Close Phase 3 with residual documentation, initial G-24 explainability, exit checklist, and handoff to Phase 4. |
| **Scope** | Initial G-24 (structured last-outcome explanation / logs — not dashboard); Phase 3 exit report; roadmap/plan progress updates. |
| **Dependencies** | P3-S7 |
| **Affected projects** | Application/Infrastructure only if a tiny last-outcome field/log is required; Test; docs |
| **Expected code areas** | Ensure operators/tests can explain: stored Synchronization Position algorithm version, last discovery outcome, last reconcile outcome, Scope sync state / inconsistency reason; optional structured log events for sync completed/failed; create [`stock-ledger-phase3-implementation-report.md`](./stock-ledger-phase3-implementation-report.md) when coding completes |
| **Database impact** | Prefer zero; if a last-sync explanation column is truly necessary, keep additive/nullable and document rollback |
| **Tests** | Explainability assertions on happy-path and Inconsistent path; full `StockLedgerFeature` filter green for implemented scenarios |
| **Acceptance criteria** | Phase 3 exit criteria (§9) can be marked complete; handoff lists Phase 4 residuals; no FO enablement |
| **Explicit exclusions** | Metrics product, alert routing, Phase 4 DO Receipt |
| **Rollback strategy** | Documentation-only parts N/A; optional columns unused/nullable |
| **Risks** | Writing a report that claims G-17/FQ-06 or Phase 4 done |
| **Deliverables** | Exit report + plan slice progress COMPLETE + Artifact Registry updates + slice summary |

---

## 8. Cross-cutting Phase 3 rules

1. **Repository first:** Inspect existing StockLedgerFeature code and Phase 2 report before adding types; extend ports/repos/UoW rather than parallel stacks.  
2. **Clean Architecture:** Domain has no SQL/Dapper; Application orchestrates; Infrastructure adapts legacy + Ledger SQL.  
3. **Legacy authority remains writable:** Synchronization consumes facts; it does not block VB6.  
4. **No authority transfer:** `LegacySynchronized` is origin only.  
5. **Fingerprint continuity:** Reuse `fingerprint-v1` unless an explicit algorithm bump is justified.  
6. **Position advancement is sacred:** never advance on hash drift alone; never advance without committed catch-up + material reconciliation success.  
7. **History retention:** voids/deletes ⇒ correction/reversal movements; never delete Stock Ledger history to mirror legacy absence.  
8. **Short transactions:** discovery/interpretation outside long write locks; persist sync batch in a short TX.  
9. **Pragmatism:** no event sourcing, broker, CDC requirement, or new framework.  
10. **DI:** register adapters only as needed for tests/internal invocation; no production FO traffic.  
11. **Tests per slice:** only that slice’s tests; do not implement Phase 4 New→Legacy harness.  
12. **Summaries:** each completed slice produces `stock-ledger-phase3-sN-implementation-summary.md` (WHAT / WHY / deviations / handoff) and updates the slice progress table above.

---

## 9. Phase 3 exit criteria (definition of done)

Phase 3 is complete only when all of the following hold:

- [ ] G-13 accepted: live discovery detects insert, update, `tb_stok` delete, `tb_buku` void delete, backdated/tied movement, and repost on production-like disposable fixtures without watermark-alone/`fs_kd_trs`-alone cursors.  
- [ ] G-14 accepted: opaque Synchronization Position (+ algorithm version) advances only after committed catch-up + successful material reconciliation; crash before commit retains prior position.  
- [ ] G-15 accepted: catch-up applies `LegacySynchronized` facts with `SyncBatch` idempotency; duplicate batch is quantity-neutral; voids become correction/reversal; success returns Scope sync state to `Current`.  
- [ ] G-16 P0 accepted: intentional depleted-layer difference is not material inconsistency; real quantity mismatch surfaces `Inconsistent`.  
- [ ] G-12 accepted: Freshness Gate detects post-baseline legacy change, synchronizes or fails closed, and never acts as an Authority Gate against VB6.  
- [ ] Sync/native serialization interim covered in-process (OCC/conditional Scope); **no** false claim that FQ-06 / production G-17 is done.  
- [ ] Synchronization portion of G-23 activated for Legacy→New, duplicate sync, real mismatch, and sync/native race; Phase 4/5/8 harness markers remain skipped with owners listed.  
- [ ] Initial G-24: freshness/sync outcome is explainable from position + last discovery/reconcile/Scope state (no ops dashboard required).  
- [ ] No `IsAuthoritative`; no legacy row rewrites for sync convenience; depleted layers retained.  
- [ ] Solution builds; StockLedgerFeature tests for Phase 3 slices pass; Phase 1/2 tests remain green.  
- [ ] Phase 3 implementation report published; this plan’s slice progress table updated to COMPLETE.  
- [ ] Explicit handoff notes list Phase 4 residuals (live Compatibility Writer, DO Receipt, New→Legacy harness) and remaining G-17/G-25 debt.

---

## 10. Handoff to Phase 4+

When Phase 3 exits:

| Next | Uses from Phase 3 |
|---|---|
| Phase 4 DO Receipt | Freshness Gate before Ledger-dependent writes; sync catch-up for later VB6 activity; UoW; still needs live Legacy Compatibility Writer |
| Phase 5 Outbound / transfer | Freshness Gate + G-08 provisional discovery + Phase 1 FIFO; sync/native serialization hooks |
| Phase 8 Ops hardening | Initial explainability fields/logs as seeds for G-24 productization |
| Phase 9 Production coexistence | Still blocked on FQ-06 / full G-17 mixed-writer proof |

Do **not** start Phase 4 FO writes inside a Phase 3 slice.

### Residual debt expected after Phase 3 (non-blocking for Phase 4 coding start)

| Debt | Owner |
|---|---|
| G-25 proposed legacy indexes / p95 SLOs | DBA / Phase 8 |
| FQ-06 live VB6 concurrency proof | Phase 9 / G-17 production gate |
| New→Legacy / FO partial-failure harness | Phase 4+ |
| Full G-24 metrics/alerts/runbook product | Phase 8 |
| G-28 unknown jenis vocabulary completeness | Before AJ sync-completeness claims |

---

## 11. References for implementers

| Read first | Why |
|---|---|
| [`stok-ledger-domain.md`](./stok-ledger-domain.md) §5.11, §8.6; BR-STL-079, 110–115 | Sync + freshness business rules |
| [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md) § Phase 3, §5.2, §8 | Objectives, later-legacy-change flow, test themes |
| [`stock-ledger-gap-analysis.md`](./stock-ledger-gap-analysis.md) G-12–G-16, G-23 | Acceptance language |
| [`adr/ADR-stock-ledger-legacy-change-discovery.md`](./adr/ADR-stock-ledger-legacy-change-discovery.md) | Normative mismatch / set-diff / re-derive rule |
| [`adr/ADR-stock-ledger-mixed-writer-concurrency.md`](./adr/ADR-stock-ledger-mixed-writer-concurrency.md) | Interim sync/native serialization |
| [`stock-ledger-phase2-implementation-report.md`](./stock-ledger-phase2-implementation-report.md) | Exact handoff assets and non-claims |
| [`stock-ledger-feasibility-review.md`](./stock-ledger-feasibility-review.md) | Why watermark cursors fail |
| [`docs/skills/use-case-generation.md`](../../skills/use-case-generation.md) | MediatR UseCase patterns |
| [`docs/skills/feature-persistence-generation.md`](../../skills/feature-persistence-generation.md) | If Scope/idempotency persistence needs small extensions |

---

## 12. Suggested agent prompt (per slice)

When executing a single slice, use a prompt of this form:

```text
Implement only <P3-SN> from docs/contexts/stok-ledger/stock-ledger-phase3-implementation-plan.md.

Repository-first: inspect existing StockLedgerFeature code, Phase 2 report, and prior Phase 3 summaries before coding.
Reuse LegacyReconstructionBasisCalculator / existing ports / Domain sync transitions — do not invent parallel stacks.
Do not implement later Phase 3 slices or any Phase 4+ scope (no FO writer, no production DI).
Keep the solution compiling. Prefer disposable/test DB. Do not rewrite legacy authority rows.
Never erase Stock Ledger history to mirror legacy deletes.
When done: run relevant StockLedgerFeature tests, write stock-ledger-phase3-sN-implementation-summary.md,
and update the slice progress table in the Phase 3 plan.
```
