# Stock Ledger — Phase 3 Implementation Plan

**Artifact status:** Executable Phase-3 plan (implementation-ready; final polish)  
**Date:** 2026-08-08  
**Phase:** 3 — Incremental Legacy Synchronization and Freshness Gate  
**Governing baseline (LOCKED):** Phase 0 Exit Review **PASS WITH RISKS** — [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md)  
**Phase 1 foundation (COMPLETE):** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Phase 2 baseline (COMPLETE):** [`stock-ledger-phase2-implementation-report.md`](./stock-ledger-phase2-implementation-report.md) — latest slice [`stock-ledger-phase2-s8-implementation-summary.md`](./stock-ledger-phase2-s8-implementation-summary.md)  
**Roadmap:** [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md) § Phase 3  
**Gaps in scope:** G-12–G-16 (material/sync portion), synchronization portion of G-23, initial G-24 observability, sync/native serialization portion of G-17 (interim only — **not** production mixed-writer proof)  
**Standards:** [`docs/ENGINEERING.md`](../../ENGINEERING.md), [`docs/DATABASE.md`](../../DATABASE.md), [`docs/NAMING.md`](../../NAMING.md)

### Slice progress

| Slice | Status | Summary |
|---|---|---|
| P3-S1 | **COMPLETE** | Live G-13 discovery adapter + set-diff classifier + detection tests; **gate PASS** |
| P3-S2 | **COMPLETE** | Pure sync delta interpreter + intent model + unit tests; **no I/O** |
| P3-S3 | **PENDING** | — |
| P3-S4 | **PENDING** | — |
| P3-S5 | **PENDING** | — |
| P3-S6 | **PENDING** | — |
| P3-S7 | **PENDING** | — |
| P3-S8 | **PENDING** | — |

---

## 1. Purpose

Convert the Phase 3 roadmap into **small, independently executable slices** that a mid-level coding agent can implement with minimal ambiguity and low risk of premature Phase-4+ work.

Phase 3 keeps reconstructed (or later Native-origin) Stock Ledger scopes **current** when VB6 continues to change authoritative `tb_stok` / `tb_buku`, and proves a fail-closed **Legacy Freshness Gate** before Ledger-dependent decisions. It does **not** enable FO/native stock writes, transfer authority away from legacy records, or claim mixed-writer production safety (FQ-06 / G-17 production gate remains Phase 9).

---

## 2. Code-base evaluation after Phase 2 (adjust the roadmap)

Inspect the repository before each slice. The Phase 3 roadmap text remains directionally correct, but **much scaffolding already exists**. Do not rebuild parallel stacks.

### 2.1 Already delivered — reuse, do not redo

| Asset | Repository state | Phase 3 implication |
|---|---|---|
| Feature folder | `InventoryContext/StockLedgerFeature/` across Domain / Application / Infrastructure / SqlDb / Test | Extend only |
| Sync Domain state machine | `StockLedgerScopeStateModel` already has `MarkLegacyChangePending` → `RequireSynchronization` → `CompleteSynchronization` / `MarkSynchronizationInconsistent` | Orchestration must call these; do not invent authority states |
| Opaque Synchronization Position | Columns + `SynchronizationPositionType`; initialized at reconstruction with `fingerprint-v1` | G-14 **storage** is done; Phase 3 owns **advancement after committed catch-up** |
| Fingerprint calculator | `LegacyReconstructionBasisCalculator` (`AlgorithmVersion = "fingerprint-v1"`) — already used by `ReconstructStockLedgerBaselineHandler` | **Single authoritative fingerprint algorithm.** Every sync-related slice must call this calculator (or a trivial pass-through). A second hash implementation is forbidden unless an explicit algorithm-version migration is introduced |
| Discovery / reconciliation contracts | `ILegacyChangeDiscoveryPort`, `IStockReconciliationPort` + DTOs/enums | Implement live adapters; extend DTOs only if detection needs richer fields |
| Anticipatory shapes | `StockFactOriginEnum.LegacySynchronized`, `StockSourceIdempotencyKindEnum.SyncBatch`, Correction/Reversal movement factories | Ready for catch-up; unused by a sync use case today |
| Live reads / discovery | `LegacyStockReadPort` (G-05), `AvailabilityDiscoveryPort` (G-08 provisional) | Re-read / replay inputs; Availability must **not** become FIFO authority until Freshness Gate exists |
| Reconstruction A/B/C + recovery | `ReconstructStockLedgerBaselineCommand`, claim/calc/persist, `RecoverIncompleteReconstructionService` | Baseline prerequisite for sync; recovery deletes additive rows only (sync voids must **not** erase Ledger history) |
| UoW / repos | `IStockConsequenceUnitOfWork`, Movement/Position/Scope/Idempotency repos | Prefer short sync-batch TX via existing UoW patterns |
| Coexistence harness | One activated depleted-layer test; **7 skipped** Phase 3/4/5 placeholders | Un-skip only Phase-3-owned scenarios |
| Production DI / HTTP | None | Keep none through Phase 3 exit |

### 2.2 Still missing — Phase 3 owns

1. Live `ILegacyChangeDiscoveryPort` (fingerprint compare + set-diff / bounded re-derive signal; G-13 detection experiments).  
2. Pure interpretation of discovered deltas → accountable correction/reversal/update intents.  
3. Live material reconciliation adapter (G-16 P0 portion) — classify only; no repair.  
4. Incremental catch-up orchestration that applies `LegacySynchronized` facts, uses `SyncBatch` idempotency, reconciles, and advances Synchronization Position only on success (G-14/G-15).  
5. Freshness Gate orchestration (G-12) — fail closed / synchronize-first; never blocks VB6.  
6. Sync crash/retry + duplicate-sync protection + .NET-side sync serialization (partial G-17 interim; **not** live VB6 FQ-06 proof).  
7. Activation of sync-relevant G-23 harness scenarios.  
8. Initial observability notes / explainable last sync outcome (initial G-24 — not ops dashboard).

### 2.3 Roadmap misalignment corrections

| Roadmap / planning assumption | Actual post–Phase-2 state | Plan adjustment |
|---|---|---|
| Phase 3 must invent Synchronization Position persistence | Already persisted and initialized | No storage-only slice |
| Phase 3 must invent sync Domain transitions | Already unit-tested | Wire orchestration only |
| Fingerprint work starts in Phase 3 | `fingerprint-v1` already used for reconstruction | Reuse calculator behind discovery port |
| Availability Discovery is Phase 3 | Shipped in P2-S7 as provisional | Do not re-implement; gate callers in P3-S5 |
| Full G-17 mixed-writer VB6 proof is Phase 3 | FQ-06 still open; roadmap Phase 9 gate | Phase 3 proves .NET-side sync serialization / retry only |
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
| Fingerprint algorithm continuity | **Exactly one** authoritative algorithm: `LegacyReconstructionBasisCalculator` / `fingerprint-v1`. Bump algorithm version only with explicit migration notes that update reconstruction and discovery together |
| Depleted layers | Ledger retains zero Remaining Quantity; legacy may delete zero `tb_stok` — intentional difference, not material drift |
| Void / delete handling | Physical `tb_buku` / zero-row absence → accountable Ledger **correction/reversal**; never erase Stock Ledger history |
| Freshness Gate | Replaces rejected Authority Gate; never rejects a legacy write because scope is Native/Reconstructed |
| Lock order (interim) | Item → Receipt Source → Location → legacy row id ([concurrency ADR](./adr/ADR-stock-ledger-mixed-writer-concurrency.md)) |
| CT / CDC / universal change log | Deferred; do not introduce unless ADR is amended |
| Mixed-writer production correctness | **Not** a Phase 3 claim. Live VB6/.NET proof remains Phase 9 (FQ-06 / G-17) |

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
- Full re-reconstruction as the steady-state sync path (allowed only when discovery returns `RequiresScopedReDerive` and P3-S4 applies bounded re-derive — not as default catch-up)
- Generic synchronization frameworks, workflow engines, pipeline frameworks, or batch-processing platforms

---

## 5. Target module layout (extend existing)

Continue the Phase 1/2 feature folder. Do **not** create a second Stock Ledger context.

| Layer | Path | Phase 3 expectation |
|---|---|---|
| Domain | `Bilreg.Domain/InventoryContext/StockLedgerFeature/` | Only small extensions if sync facts cannot already be expressed (prefer existing Correction/Reversal/`LegacySynchronized`) |
| Application | `Bilreg.Application/InventoryContext/StockLedgerFeature/` | Discovery helpers, pure delta interpreter, Freshness Gate, sync UseCases; reuse ports/UoW |
| Application ports | `…/Ports/` | Live adapters for discovery + reconciliation; extend DTOs sparingly |
| Infrastructure | `Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/` | Live discovery/reconcile adapters using G-05 reads + Ledger repos |
| SqlDb | `Bilreg.SqlDb/InventoryContext/StockLedgerFeature/` | Prefer **no** new tables; optional additive columns only if explainability cannot reuse Scope fields |
| Test | `Bilreg.Test/InventoryContext/StockLedgerFeature/` | Slice tests; activate sync G-23 placeholders |
| Api | **No public sync/write endpoints required for Phase 3 exit.** |

### Established contracts to reuse

| Established | Role in Phase 3 |
|---|---|
| `ILegacyChangeDiscoveryPort` + delta kinds | G-13 live adapter target (detection only) |
| `IStockReconciliationPort` + outcomes | G-16 material classification target (no repair) |
| `ILegacyStockReadPort` | Snapshot inputs for fingerprint / set-diff |
| `LegacyReconstructionBasisCalculator` | **Only** fingerprint implementation (`fingerprint-v1`) |
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

### Why this slice order

| Inspiration item | Disposition |
|---|---|
| Legacy Change Discovery foundation | **P3-S1** — detection only; **implementation gate** for later sync slices |
| Synchronization Position | **Folded into P3-S4** — storage already exists; advancement belongs with catch-up |
| Legacy update/void interpretation | **P3-S2** — pure, side-effect-free intent model |
| Reconciliation (material) | **P3-S3** — advance-safety classification only |
| Incremental Synchronization Engine | **P3-S4** — sole orchestration slice; no frameworks |
| Freshness Gate | **P3-S5** — after catch-up exists |
| Recovery & Retry + sync serialization | **P3-S6** — sync-only hardening; not Phase-4 native infra |
| Coexistence Test Harness | **P3-S7** |
| Phase hardening & exit review | **P3-S8** |

### Slice map (execute in order)

```text
P3-S1 Live Legacy Change Discovery (G-13)          << implementation gate
   -> P3-S2 Sync delta interpretation (pure intents)
   -> P3-S3 Material reconciliation (classify only)
   -> P3-S4 Incremental catch-up + position advancement (G-14/G-15)
   -> P3-S5 Freshness Gate (G-12)
   -> P3-S6 Sync retry / crash / duplicate / .NET serialization
   -> P3-S7 Coexistence sync harness (G-23 sync portion)
   -> P3-S8 Phase 3 exit hardening + initial observability + report
```

### P3-S1 implementation gate (checkpoint, not a new phase)

**If P3-S1 cannot reliably implement deletion-aware discovery** against the current repository and database characteristics (fingerprint continuity with reconstruction, set-diff for void-deletes, explicit `RequiresScopedReDerive` / `Undeterminable` when classification is not confident), then:

1. **Stop.** Do not start P3-S2…P3-S8 catch-up/Freshness work.  
2. Record the failure evidence in the P3-S1 summary (fixture, change kind missed, why inference would be unsafe).  
3. Revise the discovery strategy (ADR amendment only if the Phase-0 mechanism itself is inadequate) before any synchronization orchestration proceeds.

This is an **implementation checkpoint** inside Phase 3 — not a new project phase and not a license to invent CT/CDC/change-log without ADR amendment.

Each slice must leave the solution **compiling**. Prefer disposable/test DB (`devTest` / `DEVTEST`) for persistence and live-read fixtures. Synchronization **consumes** legacy facts; it must not rewrite `tb_stok` / `tb_buku` except through an explicitly authorized recovery path (none required for Phase 3 exit).

Normative sync boundary (orchestration slices must respect; discovery alone does not apply it):

```text
Discover (fingerprint compare; on mismatch set-diff and/or RequiresScopedReDerive)
  -> Mark SynchronizationRequired (Domain transition)          [P3-S4+]
  -> Interpret deltas into accountable Ledger intents          [P3-S2 / applied in P3-S4]
  -> Short TX: apply SyncBatch idempotent movements/layers
  -> Reconcile (classify only)                                 [P3-S3]
  -> Advance Synchronization Position only if material reconcile allows
  -> Else MarkSynchronizationInconsistent / leave prior position unchanged
```

---

## 7. Slice specifications

### P3-S1 — Live Legacy Change Discovery (G-13)

| Field | Detail |
|---|---|
| **Objective** | Implement deletion-aware **change detection only**: detect that legacy authority differs from the stored Synchronization Position, classify detectable delta kinds, and decide when scoped re-derive is required or when freshness is undeterminable. |
| **This slice owns** | Live `ILegacyChangeDiscoveryPort`; `ComputeCurrentFingerprint` via **only** `LegacyReconstructionBasisCalculator`; `DiscoverChanges` fingerprint compare; set-diff of Ledger-known journal identities vs surviving `tb_buku`; surfacing `JournalInsert` / `JournalUpdate` / `JournalVoidDelete` / `BalanceDelete` / `BalanceUpdate` when confidently classifiable; returning `RequiresScopedReDerive` and/or overall `Undeterminable` when not confident; G-13 detection experiments |
| **Explicitly out of scope** | Synchronization / catch-up; any Ledger write or Scope state transition; business interpretation of voids into correction/reversal intents (P3-S2); reconciliation (P3-S3); advancing Synchronization Position; Freshness Gate; FO/native writes; inventing a second fingerprint algorithm |
| **Must not implement here** | P3-S2 intent model; P3-S3 reconcile; P3-S4 catch-up UseCase; P3-S5 gate; Phase 4+ writers |
| **Dependencies** | Phase 2 G-05 reads; `LegacyReconstructionBasisCalculator`; Phase 0 sync ADR; reconstructed Scope with stored position for mismatch tests |
| **Affected projects** | Infrastructure, Application (thin wrapper ok), Test |
| **Expected code areas** | New `LegacyChangeDiscoveryPort` (or equivalent) under Infrastructure; reuse `ILegacyStockReadPort`; call `LegacyReconstructionBasisCalculator.Compute` for fingerprints; compare stored opaque position vs current fingerprint; on mismatch perform set-diff; when set-diff cannot confidently classify the scope, return delta kind `RequiresScopedReDerive` and/or outcome `Undeterminable` with explanation — **do not** invent increasingly complex inference |
| **Fingerprint rule** | Must produce the same opaque value as reconstruction Phase C for the same scoped snapshot (`fingerprint-v1`). Prove continuity in tests against a reconstruction-initialized position |
| **Database impact** | Read-only legacy + read Ledger movement identities. No new tables. Optional proposed-index scripts remain DBA-owned (G-25). |
| **Tests** | Detection experiments on disposable fixtures: journal insert; journal/quantity update; `tb_stok` depletion delete; `tb_buku` void delete; backdated/tied mutation time; repost; unchanged ⇒ `Unchanged`; ambiguous/unclassifiable ⇒ `RequiresScopedReDerive` and/or `Undeterminable` (not silent “best guess”); fingerprint continuity with reconstruction-initialized position; hash drift alone without set-diff/re-derive signal is insufficient |
| **Acceptance criteria** | G-13 acceptance for listed change kinds without watermark-alone/`fs_kd_trs`-alone cursors; discovery remains read-only; port contract remains true (does not advance position, does not apply sync); **gate:** if acceptance cannot be met, stop Phase 3 catch-up slices (§6) |
| **Explicit exclusions** | Applying movements; interpreting business meaning beyond delta kind classification; Freshness Gate; FO writes; CT/CDC; second fingerprint implementation |
| **Rollback strategy** | Remove live adapter + tests; leave port contract and fakes intact |
| **Risks** | Dual fingerprint implementations; treating fingerprint mismatch as a delete stream without set-diff; over-inferring ambiguous scopes instead of `RequiresScopedReDerive` / `Undeterminable`; scanning unbounded histories without Item+DO predicates |
| **Deliverables** | Live G-13 adapter + detection tests + `stock-ledger-phase3-s1-implementation-summary.md` (include gate pass/fail verdict) |

---

### P3-S2 — Sync delta interpretation (void/update → accountable intents)

| Field | Detail |
|---|---|
| **Objective** | Produce a **deterministic, side-effect-free, SQL-free, persistence-free** intent model that maps discovery deltas (+ current in-memory Ledger snapshot inputs) to proposed Stock Ledger intents. |
| **This slice owns** | Pure Application interpreter/calculator; explicit intent types suitable for unit review (correction/reversal/quantity-update/new-layer-establishment/fail-closed ambiguity); mapping rules for void-delete and quantity change; preservation of existing layer establishment origin when only quantity changes |
| **Explicitly out of scope** | Any I/O, SQL, repository calls, UoW, transactions, Scope transitions, position advancement, reconciliation, Freshness Gate, catch-up handler |
| **Must not implement here** | P3-S3 adapter; P3-S4 MediatR orchestration / persist; P3-S5; any Infrastructure class |
| **Dependencies** | P3-S1 **passed gate** (delta shapes stable); Phase 1 Movement Correction/Reversal factories; Phase 2 reconstructed baseline shapes |
| **Affected projects** | Application (preferred), Domain only if factories cannot express intents, Test |
| **Expected code areas** | New interpreter under Application `StockLedgerFeature/`; inputs are plain DTOs/models already loaded by the caller; outputs are an explicit intent list + failure/ambiguity reasons; map `JournalVoidDelete` → reversal/correction intents; `BalanceDelete` / quantity changes → layer remaining updates via **new** movement intents; new inbound after baseline → `LegacySynchronized` establishment for **new** layers only |
| **Fingerprint rule** | This slice does not compute fingerprints. It must not introduce an alternate hash helper |
| **Database impact** | None |
| **Tests** | Pure unit tests only: same inputs ⇒ same intents; void-delete ⇒ accountable correction/reversal intent (no history erase); duplicate delta identity ⇒ single intent; quantity-only change does not rewrite establishment origin; material ambiguity ⇒ explicit fail-closed intent outcome; no fakes that touch SQL |
| **Acceptance criteria** | Interpreter is reviewable and independently testable; completely independent from transaction handling; Domain remains SQL-free; no silent “force balance” |
| **Explicit exclusions** | Persistence; position advance; Freshness Gate; inventing Receipt Source / provenance; TX / retry logic |
| **Rollback strategy** | Remove interpreter + tests |
| **Risks** | Porting VB6 void control flow; deleting Ledger movements to “match” legacy absence; sneaking repository calls into the “pure” calculator |
| **Deliverables** | Intent model + interpreter + unit tests + slice summary |

---

### P3-S3 — Material reconciliation adapter (G-16 P0)

| Field | Detail |
|---|---|
| **Objective** | Classify whether a scope’s Ledger representation is materially consistent with legacy authority so that **synchronization may safely advance** the Synchronization Position — or must fail closed. |
| **This slice owns** | Live `IStockReconciliationPort`; read-only compare of authoritative remaining quantity vs Ledger Remaining Quantity across locations; classification into existing outcomes (`Balanced`, `IntentionalDifference`, `PendingSynchronization`, `ProvenanceLimitation`, `MaterialInconsistency`); explanations for intentional depleted-layer difference vs real drift |
| **Explicitly out of scope** | Automatic repair; mutating Movements/Layers/Positions/Scope; advancing Synchronization Position; applying sync intents; “fixing” drift by rewriting history; ops dashboards |
| **Must not implement here** | P3-S4 catch-up persist; P3-S2 interpretation; P3-S5 gate; Phase 8 reporting views |
| **Dependencies** | Phase 2 depleted-layer harness evidence; G-05 reads; Ledger Position/Layer repos; P3-S1 gate passed (so later catch-up can trust discovery) |
| **Affected projects** | Infrastructure, Test; optional thin Application façade |
| **Expected code areas** | `StockReconciliationPort` (or equivalent); compare `tb_stok` remaining vs Ledger remaining; classify `DepletedLayerVsAbsentLegacyRow` as intentional/balanced; material quantity mismatch ⇒ `MaterialInconsistency` |
| **Fingerprint rule** | Reconciliation does not own fingerprint computation. If it needs a current authority snapshot, use G-05 reads (and existing calculator only if a fingerprint compare is already required by a caller — do not fork hashing) |
| **Database impact** | Read-only |
| **Tests** | Depleted-layer intentional difference ⇒ Balanced or IntentionalDifference (not MaterialInconsistency); real qty mismatch ⇒ MaterialInconsistency; no movement/Scope mutation side effects; optional `PendingSynchronization` when Scope already requires sync |
| **Acceptance criteria** | G-16 P0: explicit outcomes only; **safe-to-advance vs not-safe-to-advance** is decideable by P3-S4 from the result; no repair behavior |
| **Explicit exclusions** | Auto-repair; rewriting movements; authority transfer; catch-up orchestration |
| **Rollback strategy** | Remove adapter + tests; leave port contract |
| **Risks** | False positives from row-shape equality; treating valuation-only gaps as hard blockers without explanation; “helpful” mutation to force balance |
| **Deliverables** | Live G-16 adapter + tests + slice summary |

---

### P3-S4 — Incremental catch-up engine + Synchronization Position advancement (G-14 / G-15)

| Field | Detail |
|---|---|
| **Objective** | Sole **orchestration** slice for one reconstructed scope: discover → interpret → short TX apply → reconcile → advance opaque Synchronization Position, or fail closed to `Inconsistent` / retryable not-current. |
| **This slice owns** | MediatR (or Inventory-style) sync UseCase/handler that **coordinates** P3-S1…S3; Domain sync transitions (`RequireSynchronization` / `CompleteSynchronization` / `MarkSynchronizationInconsistent`); persisting `LegacySynchronized` movements via existing repos/UoW; `SyncBatch` idempotency; advancing Synchronization Position **only** after committed apply + reconcile outcome that permits advance; handling `RequiresScopedReDerive` by composing existing discovery/interpreter outcomes (not inventing new business rules in the handler) |
| **Orchestration must stay thin** | P3-S4 **coordinates** discovery, interpretation, persistence, reconciliation, and state transitions. Complex business decisions (void/update meaning, safe-to-advance classification, delta classification) **must remain** inside the reusable components from P3-S1…S3. The sync handler should primarily **compose** those services/repos/UoW. Avoid large orchestration methods that embed business rules. Do **not** let the handler become a “god handler.” |
| **Explicitly out of scope** | Freshness Gate API (P3-S5); sync retry/OCC hardening productization beyond the **minimum** claim needed for one-winner catch-up (P3-S6); FO/native writers; production DI; generic sync frameworks; re-implementing interpreter or reconcile logic inside the handler |
| **Must not implement here** | Synchronization frameworks; generic workflow engines; reusable pipeline frameworks; speculative infrastructure; generalized batch-processing engines; Phase-4 Compatibility Writer; Phase-5 allocation; recursive catch-up loops (§8) |
| **Dependencies** | P3-S1 gate passed; P3-S2; P3-S3; Phase 1 repos/UoW; Domain sync transitions; `SyncBatch` idempotency kind |
| **Affected projects** | Application (handler), Infrastructure only if existing repos need a tiny conditional-update helper, Test |
| **Expected code areas** | Thin sync command/handler modeled after `ReconstructStockLedgerBaselineCommand`; extend `IStockConsequenceUnitOfWork` / existing repos; compose discovery → interpreter → short TX persist → reconciliation → `CompleteSynchronization(newPosition)` where `newPosition` comes from **`LegacyReconstructionBasisCalculator`** on the post-sync authority snapshot |
| **Required behavior** | (1) Scope must already be `Reconstructed` with a stored position; (2) discovery/interpretation outside long write locks; (3) short TX for Ledger persist + Scope/idempotency only; (4) duplicate SyncBatch quantity-neutral; (5) void-delete becomes correction/reversal history — never erase Ledger history; (6) success ⇒ `Current` + advanced opaque position; (7) material mismatch / not-safe-to-advance ⇒ `MarkSynchronizationInconsistent` or leave prior position unchanged; (8) never rewrite legacy authority rows; (9) **one synchronization boundary per execution** — no recursive discovery→catch-up cycle inside the same request |
| **Abstraction rule** | Prefer extending existing repositories, UoW, and handler patterns. Solve **only** the Stock Ledger synchronization use case. |
| **Concurrency note** | Minimum claim so two catch-up workers do not double-apply (conditional Scope update and/or SyncBatch uniqueness). Full retry matrix and race hardening belong in P3-S6. Full mixed-writer VB6 proof is out of scope (Phase 9). |
| **Fingerprint rule** | Position advanced must use the same `fingerprint-v1` calculator as reconstruction/discovery |
| **Database impact** | Additive Ledger writes only on disposable DB. Prefer existing tables. Add columns only if last-outcome explanation cannot reuse `InconsistencyReason` / existing Scope fields — justify in slice summary. Small sync batches are acceptable; do not add batching frameworks for throughput. |
| **Tests** | Happy path Legacy→Ledger catch-up; duplicate SyncBatch idempotent; void-delete → correction/reversal retained; crash before commit leaves prior position; Inconsistent / not-safe-to-advance path; depleted intentional difference still balanced after sync; `RequiresScopedReDerive` path does not erase history; handler remains a composition of P3-S1…S3 (no duplicated business rules in handler tests as the sole coverage) |
| **Acceptance criteria** | G-14/G-15 acceptance; position advances only with committed batch + successful material reconciliation; repeated catch-up quantity-neutral; no new framework layer; handler stays thin/orchestration-only |
| **Explicit exclusions** | Freshness Gate; FO/native writes; production DI; G-17 VB6 races; workflow/pipeline/batch frameworks; god-handler business logic |
| **Rollback strategy** | Disable/remove sync handler; additive sync movements remain historical evidence (do not “fix” by deleting history); reconstruction recovery is **not** routine sync void handling |
| **Risks** | Advancing position before reconcile; using wrong origin; long TX spanning discovery; building a “sync engine” framework; embedding P3-S2/S3 rules in the handler; treating reconstruction recovery delete as sync void semantics |
| **Deliverables** | Thin catch-up UseCase + integration tests + slice summary |

---

### P3-S5 — Legacy Freshness Gate (G-12)

| Field | Detail |
|---|---|
| **Objective** | Before trusting Stock Ledger layers for a subsequent stock decision, prove freshness: unchanged proceed; pending changes synchronize or fail closed / mark not current. |
| **This slice owns** | Application Freshness Gate helper/service; calling discovery; invoking P3-S4 catch-up **at most once** when needed for this gate call; explicit gate outcomes; ensuring the gate never blocks VB6 and never rejects legacy writes because origin is Native/Reconstructed |
| **Explicitly out of scope** | FIFO allocation; FO enablement; rewriting G-08 Availability Discovery into Ledger authority; Authority Gate semantics; production HTTP; nested/recursive catch-up loops |
| **Must not implement here** | P3-S6 retry productization; Phase 4 writers; Phase 5 outbound; a second discovery→catch-up cycle inside the same gate call |
| **Dependencies** | P3-S1 (detect), P3-S4 (catch-up) |
| **Affected projects** | Application, Test |
| **Expected code areas** | Freshness Gate under Application; inputs = scope key (+ optional Ledger-dependent decision context); outcomes explicit (`Current`, `SynchronizedNow`, `StaleOrNotCurrent`, `Inconsistent`, etc. — names left to implementer); may document how future Availability callers should honor `StaleOrNotCurrent` without changing provisional G-08 into an authority source |
| **Fingerprint rule** | Gate uses discovery’s fingerprint path — no alternate calculator |
| **Database impact** | None beyond what catch-up already does |
| **Tests** | Unchanged reconstructed scope passes; legacy change after reconstruction fails closed until sync succeeds; after successful sync, gate passes; undeterminable freshness ⇒ explicit not-current/Inconsistent; gate does not mutate legacy tables; gate does not claim authority |
| **Acceptance criteria** | G-12 acceptance; freshness explainable from Synchronization Position + last sync/reconcile outcome |
| **Explicit exclusions** | FIFO; FO enablement; Authority Gate; production HTTP; native write orchestration |
| **Rollback strategy** | Remove gate + tests; catch-up remains usable directly |
| **Risks** | Reintroducing Authority Gate; silently skipping sync when undeterminable; making Availability Discovery read Ledger as authority |
| **Deliverables** | Freshness Gate + tests + slice summary |

---

### P3-S6 — Sync retry, crash safety, duplicate protection, .NET serialization

| Field | Detail |
|---|---|
| **Objective** | Harden **synchronization itself** for duplicate protection, bounded retry, transaction/crash recovery, OCC/conditional-update conflicts, and .NET-side serialization of concurrent sync attempts — without building Phase-4 native-write infrastructure. |
| **This slice owns** | Bounded, **synchronization-specific** retry for sync TX version/deadlock conflicts; crash-before-commit ⇒ prior Synchronization Position retained; duplicate concurrent catch-up ⇒ one winner, quantity-neutral; .NET-side serialization so two sync paths cannot both trust pre-sync layers; tests proving these properties |
| **Retry must stay sync-specific** | Retry/recovery belongs **only** to synchronization. Keep it a simple, explicit loop or helper around the sync UoW/handler. Do **not** introduce generic retry frameworks, reusable execution pipelines, middleware-based retry engines, Polly-style cross-cutting platforms, or infrastructure intended for future phases. |
| **Explicitly out of scope** | Live VB6 concurrent sessions; production mixed-writer enablement; FO/native consequence UoW; Compatibility Writer; transfer multi-location lock productization; generalized concurrency/retry frameworks |
| **Must not implement here** | Infrastructure intended for future Native write orchestration; production DI for FO traffic; Phase 5 outbound races; full G-17 matrix; generic middleware retry |
| **Future integration (document only)** | In the slice summary, document the **contract** Phase 4+ native consequence UoW should honor (e.g. “must not allocate from layers until Freshness Gate passes; must serialize with sync claim / Scope version”). **Do not implement** that native UoW or FO writer here |
| **Dependencies** | P3-S4, P3-S5; Position OCC; Scope conditional updates |
| **Affected projects** | Application, Infrastructure (minimal — only if existing Scope/Position conditional update needs a tiny fix), Test |
| **Expected code areas** | Small, explicit retry around existing sync handler / UoW; strengthen conditional Scope / SyncBatch uniqueness if P3-S4 left gaps; **no** new native-write service; **no** generic pipeline |
| **Fingerprint rule** | Each retry attempt re-reads persisted Scope position + current legacy snapshot and re-discovers via the same `fingerprint-v1` path; never advance on a stale fingerprint; never rely on in-memory sync caches (§8) |
| **Database impact** | None required |
| **Tests** | Sync crash mid-TX → retry safe / prior position retained; duplicate concurrent catch-up → one winner, quantity-neutral; OCC/version conflict → bounded retry or explicit fail; .NET-side sync serialization (two sync callers / Scope version) — not VB6; intentional depleted difference remains balanced; retry does not recurse into a second catch-up cycle inside one attempt beyond the bounded retry of the **same** request boundary |
| **Acceptance criteria** | Retry is safe and sync-specific; position never advances past unapplied/delete-undetected change; **explicit non-claim:** FQ-06 / production G-17 is **not** done; no generic retry framework introduced |
| **Explicit exclusions** | Controlled VB6 sessions; native write orchestration; Compatibility Writer; transfer lock productization; concurrency/retry frameworks; reusable execution pipelines |
| **Rollback strategy** | Revert sync-only retry helpers; happy-path catch-up remains |
| **Risks** | Scope creep into Phase 4/5/9; infinite retry loops; implementing “native write guard services” that belong later; genericizing retry for hypothetical FO use; using reconstruction recovery delete for sync conflicts |
| **Deliverables** | Sync hardening tests + minimal sync-only helpers + slice summary (with Phase-4 contract notes, not code) |

---

### P3-S7 — Coexistence sync harness (G-23 sync portion)

| Field | Detail |
|---|---|
| **Objective** | Activate Phase-3-owned coexistence harness scenarios against discovery + catch-up + Freshness Gate. |
| **This slice owns** | Un-skip / implement harness tests for **Legacy→New**, **duplicate sync batch**, **real mismatch**, and **.NET sync/native-serialization race** (in-process doubles only); document which placeholders remain skipped and why |
| **Explicitly out of scope** | New→Legacy (Phase 4); concurrent outbound (Phase 5); alternating writers requiring native FO writer (Phase 4+); partial live legacy+Ledger FO failure (Phase 4/8); production coexistence claims |
| **Must not implement here** | FO Compatibility Writer; Availability FIFO; Phase 8 ops suite |
| **Dependencies** | P3-S4–S6 |
| **Affected projects** | Test (primary), docs notes |
| **Expected code areas** | `StockLedgerCoexistenceHarnessPlaceholderTest` (or successor) |
| **Fingerprint rule** | Harness fixtures must use reconstruction + discovery fingerprint continuity |
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
| **This slice owns** | Explainability of Synchronization Position algorithm version, last discovery outcome, last reconcile outcome, Scope sync state / inconsistency reason; optional structured logs for sync completed/failed; Phase 3 implementation report; plan progress + Artifact Registry updates |
| **Explicitly out of scope** | Metrics product, alert routing, dashboards, Phase 4 DO Receipt, claiming FQ-06 done |
| **Must not implement here** | FO writers; production DI; ops runbook productization (Phase 8) |
| **Dependencies** | P3-S7 |
| **Affected projects** | Application/Infrastructure only if a tiny last-outcome field/log is required; Test; docs |
| **Expected code areas** | Prefer existing Scope `InconsistencyReason` / state fields; create [`stock-ledger-phase3-implementation-report.md`](./stock-ledger-phase3-implementation-report.md) when coding completes |
| **Fingerprint rule** | Report must confirm single `fingerprint-v1` authority across reconstruction, discovery, and position advancement |
| **Database impact** | Prefer zero; if a last-sync explanation column is truly necessary, keep additive/nullable and document rollback |
| **Tests** | Explainability assertions on happy-path and Inconsistent path; full `StockLedgerFeature` filter green for implemented scenarios |
| **Acceptance criteria** | Phase 3 exit criteria (§9) can be marked complete; handoff lists Phase 4 residuals; no FO enablement; G-17 production non-claim restated |
| **Explicit exclusions** | Metrics product, alert routing, Phase 4 DO Receipt |
| **Rollback strategy** | Documentation-only parts N/A; optional columns unused/nullable |
| **Risks** | Writing a report that claims G-17/FQ-06 or Phase 4 done |
| **Deliverables** | Exit report + plan slice progress COMPLETE + Artifact Registry updates + slice summary |

---

## 8. Cross-cutting Phase 3 rules

1. **Extend before inventing:** Before introducing a new abstraction, repository, helper, service, workflow, table, or framework, first determine whether an existing Phase-1 or Phase-2 implementation can be extended with minimal changes. Prefer extending `IStockConsequenceUnitOfWork`, existing repos, `LegacyReconstructionBasisCalculator`, Domain sync transitions, and Inventory MediatR handler style over parallel stacks.  
2. **Repository first:** Inspect existing StockLedgerFeature code and Phase 2 report before adding types.  
3. **Clean Architecture:** Domain has no SQL/Dapper; Application orchestrates; Infrastructure adapts legacy + Ledger SQL.  
4. **Legacy authority remains writable:** Synchronization consumes facts; it does not block VB6.  
5. **No authority transfer:** `LegacySynchronized` is origin only.  
6. **Fingerprint continuity (mandatory):** There is exactly one authoritative fingerprint algorithm — `LegacyReconstructionBasisCalculator` / `fingerprint-v1` — shared by reconstruction, discovery, and position advancement. Do not add a second hash implementation. Algorithm-version migration, if ever needed, must update all three call sites together and be documented.  
7. **Position advancement is sacred:** never advance on hash drift alone; never advance without committed catch-up + material reconciliation success.  
8. **History retention:** voids/deletes ⇒ correction/reversal movements; never delete Stock Ledger history to mirror legacy absence.  
9. **Short transactions:** discovery/interpretation outside long write locks; persist sync batch in a short TX.  
10. **No hidden runtime synchronization state:** Synchronization correctness must **never** depend on in-memory process state. Forbidden patterns include singleton synchronization caches, static dictionaries of sync progress, hidden runtime bookkeeping, process-local “already synced” sets, or temporary memory becoming the source of truth. After any retry, crash, or across multiple application instances, the correct outcome must always be derivable from **persisted Stock Ledger state**, **persisted Synchronization Position**, and the **current Legacy snapshot** (`tb_stok` / `tb_buku` via G-05).  
11. **Correctness over throughput:** Phase 3 prioritizes correctness, determinism, and reviewability over synchronization throughput. Small synchronization batches are acceptable. Implementation simplicity is preferred over optimization. Defer performance optimization until correctness has been demonstrated. Do not introduce batching frameworks or optimization strategies unless they are already required by existing repository patterns.  
12. **No recursive synchronization:** A single synchronization execution must have **one** synchronization boundary. Forbidden: Discovery → Catch-up → Persist → trigger another Discovery → trigger another Catch-up inside the same execution. If another synchronization is required after the current execution completes, it must be initiated as a **completely new** synchronization request (e.g. a later Freshness Gate call or a new command). Bounded retry of the **same** failed TX/attempt (P3-S6) is allowed; nested catch-up cycles are not.  
13. **Sync-specific retry only:** Retry/recovery (P3-S6) is a simple, explicit synchronization concern. Do not introduce generic retry frameworks, reusable execution pipelines, or middleware-based retry engines for hypothetical future phases.  
14. **Thin orchestration (P3-S4):** The sync handler coordinates; P3-S2/P3-S3 (and discovery) own business decisions. Do not grow a god handler.  
15. **Pragmatism:** no event sourcing, broker, CDC requirement, sync framework, workflow engine, or generalized batch platform.  
16. **G-17 boundary:** Phase 3 addresses .NET-side sync serialization/retry only. It does **not** claim production mixed-writer correctness. Live VB6/.NET proof remains Phase 9.  
17. **P3-S1 gate:** Do not start catch-up/Freshness slices if discovery acceptance fails.  
18. **DI:** register adapters only as needed for tests/internal invocation; no production FO traffic.  
19. **Tests per slice:** only that slice’s tests; do not implement Phase 4 New→Legacy harness.  
20. **Summaries:** each completed slice produces `stock-ledger-phase3-sN-implementation-summary.md` (WHAT / WHY / deviations / handoff) and updates the slice progress table above.

---

## 9. Phase 3 exit criteria (definition of done)

Phase 3 is complete only when all of the following hold:

- [ ] **P3-S1 gate passed:** live deletion-aware discovery accepted before catch-up was built.  
- [ ] G-13 accepted: live discovery detects insert, update, `tb_stok` delete, `tb_buku` void delete, backdated/tied movement, and repost on production-like disposable fixtures without watermark-alone/`fs_kd_trs`-alone cursors; unclassifiable scopes return `RequiresScopedReDerive` and/or `Undeterminable` rather than unsafe inference.  
- [ ] G-14 accepted: opaque Synchronization Position (+ algorithm version) advances only after committed catch-up + successful material reconciliation; crash before commit retains prior position; advanced position uses the same `fingerprint-v1` calculator as reconstruction.  
- [ ] G-15 accepted: catch-up applies `LegacySynchronized` facts with `SyncBatch` idempotency; duplicate batch is quantity-neutral; voids become correction/reversal; success returns Scope sync state to `Current`.  
- [ ] G-16 P0 accepted: reconciliation classifies only (no repair); intentional depleted-layer difference is not material inconsistency; real quantity mismatch surfaces `Inconsistent` / not-safe-to-advance.  
- [ ] G-12 accepted: Freshness Gate detects post-baseline legacy change, synchronizes or fails closed, and never acts as an Authority Gate against VB6.  
- [ ] Sync duplicate protection / retry / crash recovery / .NET-side sync serialization covered; **explicit non-claim** that FQ-06 / production G-17 is done.  
- [ ] Synchronization portion of G-23 activated for Legacy→New, duplicate sync, real mismatch, and .NET sync serialization race; Phase 4/5/8 harness markers remain skipped with owners listed.  
- [ ] Initial G-24: freshness/sync outcome is explainable from position + last discovery/reconcile/Scope state (no ops dashboard required).  
- [ ] No `IsAuthoritative`; no legacy row rewrites for sync convenience; depleted layers retained; no sync framework / workflow engine introduced.  
- [ ] Solution builds; StockLedgerFeature tests for Phase 3 slices pass; Phase 1/2 tests remain green.  
- [ ] Phase 3 implementation report published; this plan’s slice progress table updated to COMPLETE.  
- [ ] Explicit handoff notes list Phase 4 residuals (live Compatibility Writer, DO Receipt, New→Legacy harness) and remaining G-17/G-25 debt.

---

## 10. Handoff to Phase 4+

When Phase 3 exits:

| Next | Uses from Phase 3 |
|---|---|
| Phase 4 DO Receipt | Freshness Gate before Ledger-dependent writes; sync catch-up for later VB6 activity; UoW; still needs live Legacy Compatibility Writer; honor P3-S6 documented sync-serialization contract |
| Phase 5 Outbound / transfer | Freshness Gate + G-08 provisional discovery + Phase 1 FIFO; same documented serialization contract |
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
| [`adr/ADR-stock-ledger-mixed-writer-concurrency.md`](./adr/ADR-stock-ledger-mixed-writer-concurrency.md) | Interim .NET sync serialization; FQ-06 still open |
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
Before adding any new abstraction/helper/table/framework, determine whether a Phase-1/Phase-2 type can be extended instead.
Reuse LegacyReconstructionBasisCalculator as the only fingerprint algorithm (fingerprint-v1).
Do not implement later Phase 3 slices or any Phase 4+ scope (no FO writer, no production DI, no sync framework).
If implementing P3-S1: detection only — no sync, no Ledger writes, no position advancement.
If P3-S1 acceptance fails: stop and do not start later sync slices.
If implementing P3-S4: keep the handler thin — compose P3-S1…S3; no god-handler business rules; one sync boundary per execution (no recursive catch-up).
If implementing P3-S6: sync-specific retry only — no generic retry frameworks or pipelines.
Never depend on in-memory/singleton/static sync state; derive outcomes from persisted Ledger + Synchronization Position + current legacy snapshot.
Prefer correctness and small batches over throughput optimization.
Keep the solution compiling. Prefer disposable/test DB. Do not rewrite legacy authority rows.
Never erase Stock Ledger history to mirror legacy deletes.
When done: run relevant StockLedgerFeature tests, write stock-ledger-phase3-sN-implementation-summary.md,
and update the slice progress table in the Phase 3 plan.
```

---

## 13. Revision summary

### 2026-08-08 — Implementation-safety revision

| Topic | Change | Why |
|---|---|---|
| P3-S1 | Narrowed to **change detection/classification only**; explicit `RequiresScopedReDerive` / `Undeterminable` instead of complex inference; **implementation gate** before later sync slices | Highest architectural risk; prevents discovery from becoming a sync engine |
| P3-S2 | Reinforced pure functional boundaries (no SQL/I/O/TX/persistence); explicit intent model ownership | Keeps interpretation independently reviewable and free of transaction coupling |
| P3-S3 | Clarified **safe-to-advance classification only**; no automatic repair; no Ledger mutation | Prevents “helpful” rewrite of history; gates position advancement correctly |
| P3-S4 | Remains sole orchestration slice; explicitly bans sync/workflow/pipeline/batch frameworks; prefer extend existing UoW/repos/handlers | Stops over-engineering while keeping one place for catch-up |
| P3-S6 | Narrowed to duplicate sync protection, retry, crash recovery, OCC, .NET sync serialization; Phase-4 native contracts **documented only** | Prevents premature Phase-4 native-write infrastructure |
| G-17 | Restated as Phase-3 non-claim; Phase 9 owns live VB6/.NET proof | Avoids false production-readiness |
| Cross-cutting | Added “extend before inventing” rule; mandatory single fingerprint algorithm; gate + G-17 rules | Reduces parallel implementations and drift |
| Fingerprint | Every sync-related slice now states continuity with `LegacyReconstructionBasisCalculator` / `fingerprint-v1` | Reconstruction and sync must share one authority token |
| Slice ownership | Each slice now has Owns / Out of scope / Must not implement here | Clearer agent boundaries |

| Question | Answer |
|---|---|
| Did implementation sequence change? | **No** — still P3-S1 → … → P3-S8 |
| Did slice count change? | **No** — still 8 |
| Did acceptance criteria change? | **Yes, refined:** P3-S1 gate + unclassifiable ⇒ `RequiresScopedReDerive`/`Undeterminable`; G-16 “no repair”; P3-S6 native infra non-claim; single fingerprint continuity in exit criteria |
| Did roadmap assumptions change? | **No architectural reopen.** Clarified Phase 3 owns G-17 **interim/.NET-side only**; production mixed-writer remains Phase 9 (already implied; made explicit in plan/exit criteria) |

### 2026-08-08 — Final implementation-polish revision

Documentation polish only. No architecture, slice order, fingerprint, or roadmap changes.

| Topic | Change | Why |
|---|---|---|
| P3-S4 | Explicit **thin orchestration**: compose P3-S1…S3; business rules stay in those components; forbid god-handler methods | Prevents catch-up handler from absorbing business logic |
| §8 No hidden runtime sync state | Outcomes must derive from persisted Ledger + Synchronization Position + current legacy snapshot; forbid singleton/static/process-local sync caches | Determinism across retries, crashes, and multiple app instances |
| §8 Correctness over throughput | Small batches OK; simplicity over optimization; defer perf work; no new batching frameworks | Stabilizes production correctness before tuning |
| §8 No recursive synchronization | One sync boundary per execution; further sync = new request; bounded same-request retry still allowed | Prevents discovery↔catch-up feedback loops |
| P3-S6 / §8 | Retry remains simple and **synchronization-specific**; forbid generic retry frameworks/pipelines/middleware engines | Stops Phase-4+ infrastructure creep |
| Agent prompt | Reflects thin P3-S4, sync-only retry, no in-memory sync state, correctness-first | Align coding agents with polish rules |

| Question | Answer |
|---|---|
| Did implementation sequence change? | **No** |
| Did slice count / responsibilities change? | **No** — clarified only |
| Did acceptance criteria change? | **No material change** — orchestration thinness, non-recursion, and no hidden state made explicit as implementation rules |
| Did roadmap assumptions change? | **No** |

| Topic | Change | Why |
|---|---|---|
| P3-S1 | Narrowed to **change detection/classification only**; explicit `RequiresScopedReDerive` / `Undeterminable` instead of complex inference; **implementation gate** before later sync slices | Highest architectural risk; prevents discovery from becoming a sync engine |
| P3-S2 | Reinforced pure functional boundaries (no SQL/I/O/TX/persistence); explicit intent model ownership | Keeps interpretation independently reviewable and free of transaction coupling |
| P3-S3 | Clarified **safe-to-advance classification only**; no automatic repair; no Ledger mutation | Prevents “helpful” rewrite of history; gates position advancement correctly |
| P3-S4 | Remains sole orchestration slice; explicitly bans sync/workflow/pipeline/batch frameworks; prefer extend existing UoW/repos/handlers | Stops over-engineering while keeping one place for catch-up |
| P3-S6 | Narrowed to duplicate sync protection, retry, crash recovery, OCC, .NET sync serialization; Phase-4 native contracts **documented only** | Prevents premature Phase-4 native-write infrastructure |
| G-17 | Restated as Phase-3 non-claim; Phase 9 owns live VB6/.NET proof | Avoids false production-readiness |
| Cross-cutting | Added “extend before inventing” rule; mandatory single fingerprint algorithm; gate + G-17 rules | Reduces parallel implementations and drift |
| Fingerprint | Every sync-related slice now states continuity with `LegacyReconstructionBasisCalculator` / `fingerprint-v1` | Reconstruction and sync must share one authority token |
| Slice ownership | Each slice now has Owns / Out of scope / Must not implement here | Clearer agent boundaries |

| Question | Answer |
|---|---|
| Did implementation sequence change? | **No** — still P3-S1 → … → P3-S8 |
| Did slice count change? | **No** — still 8 |
| Did acceptance criteria change? | **Yes, refined:** P3-S1 gate + unclassifiable ⇒ `RequiresScopedReDerive`/`Undeterminable`; G-16 “no repair”; P3-S6 native infra non-claim; single fingerprint continuity in exit criteria |
| Did roadmap assumptions change? | **No architectural reopen.** Clarified Phase 3 owns G-17 **interim/.NET-side only**; production mixed-writer remains Phase 9 (already implied; made explicit in plan/exit criteria) |
