# Stock Ledger — Phased Implementation Roadmap

**Artifact status:** Executable coexistence implementation roadmap — Phase 0 baseline **frozen** (2026-08-07)
**Basis:** Actual codebase state + [`stok-ledger-domain.md`](./stok-ledger-domain.md) + [`stock-ledger-feasibility-review.md`](./stock-ledger-feasibility-review.md)
**Gap backlog:** [`stock-ledger-gap-analysis.md`](./stock-ledger-gap-analysis.md)
**Phase 0 exit:** [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md)
**Phase 1 plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)
**Legacy behavior reference:** [`clbGenStokX1.cls`](./clbGenStokX1.cls)
**Standards:** [`docs/ENGINEERING.md`](../../ENGINEERING.md), [`docs/DATABASE.md`](../../DATABASE.md), [`docs/NAMING.md`](../../NAMING.md)

**Implementation commitment:** This roadmap delivers safe Stage B coexistence. During every phase, `tb_stok + tb_buku` remain the authoritative persisted stock truth. No phase creates per-Item + Receipt Source ownership, derives authority from `Native` / `Reconstructed`, or prevents VB6 from later modifying a reconstructed or natively processed scope.

---

## 1. Non-negotiable guardrails

1. Do not port `clbGenStokX1` control flow into Domain/Application.
2. Do not introduce `IsAuthoritative`, scope authority transition, or per-DO ownership.
3. Use `Native` / `Reconstructed` / `LegacySynchronized` only as Stock Ledger fact/layer origin.
4. Keep Item + Receipt Source across all Stock Locations as reconstruction and reconciliation scope.
5. Evaluate Item + Receipt Source + Stock Location as the write consistency boundary; keep database locking boundary explicit and evidence-based.
6. Preserve legacy `tb_stok` zero-row deletion and required void/writeback behavior while Stock Ledger retains depleted layers and accountable corrections/reversals.
7. Keep Availability Discovery separate from Provenance Discovery.
8. Before Stock Ledger relies on layers, pass the Legacy Freshness Gate.
9. Do not repeat full reconstruction for every later legacy change unless Phase 0 evidence proves bounded replay is the safest minimum mechanism.
10. New-system consequences must persist a Legacy-Compatible Stock Consequence and Stock Ledger representation in one short SQL transaction.
11. Do not require event sourcing, a broker, CDC, or external domain-event publication.
12. Do not enable coexistence production processing until mixed-writer negative-stock protection is proven.
13. Final/global cutover, legacy decommissioning, and legacy-as-projection design are deferred.

---

## 2. Current baseline

| Asset | Roadmap treatment |
|---|---|
| Canonical domain and Indonesian companion | Keep; domain authority/runtime authority distinction governs all work |
| `StokModel`, `StokLayerModel`, nested buku/lot/reference types | Refactor or replace under target boundaries; preserve useful depleted-layer behavior |
| FEFO tests | Replace with explicit ED filter + FIFO tests |
| `tb_stok_dal` / `tb_buku_dal` | Refactor behind legacy anti-corruption adapters |
| `FARIN_Stok*` SQL | Revalidate and migrate additively before wiring |
| Missing/unwired FARIN repositories/DALs | Implement to target design; do not blind-restore old shape |
| VB6 `Generate` and 13 routed FO families | Continue operating during coexistence; characterize and synchronize |
| Kartu-stok GET | Replace with explicit read model after persistence foundation |
| Generic SQL transaction helpers | Reuse only after stock-specific transaction and conflict behavior is proven |

---

## 3. State model used by every phase

```text
Per Item + Receipt Source:
    ReconstructionStatus
        NotReconstructed
        ReconstructionRequired
        Reconstructing
        Reconstructed
        Inconsistent

    SynchronizationState
        Current
        LegacyChangePending
        SynchronizationRequired
        Inconsistent

    SynchronizationPosition
        opaque validated cursor/change token/scoped basis

Per Stock Ledger fact/layer:
    Origin = Native | Reconstructed | LegacySynchronized

Global during coexistence:
    Runtime source of truth = tb_stok + tb_buku
```

The final storage shape is decided in Phase 0/1. A proposed column name must not imply that an unproven `(timestamp, ID)` cursor is already accepted.

### Stage interpretation

| Stage | Roadmap meaning |
|---|---|
| Stage A — Legacy Authority | Starting condition: VB6 writes authoritative `tb_stok + tb_buku`; Stock Ledger may be absent. |
| Stage B — Coexistence | Phases 0–9: VB6 and enabled new-system transactions operate; legacy records remain authoritative; Stock Ledger reconstructs and synchronizes. |
| Stage C — Future Final Cutover | Stock Ledger could become source of truth and legacy records a compatibility projection. This roadmap contains no Stage C delivery phase or cutover criteria. |

---

## 4. Phase sequence

### Phase 0 — Revalidate legacy behavior and coexistence baseline

**Progress:** COMPLETE — Phase-0 baseline frozen (2026-08-07). Report: [`stock-ledger-phase-0-implementation-report.md`](./stock-ledger-phase-0-implementation-report.md). Exit review: [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md) (**PASS WITH RISKS**). Phase 1 execution plan: [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md).

| Area | Plan |
|---|---|
| Objective | Establish production evidence for legacy writers, change detection, transaction behavior, indexes, and mixed-writer locking before schema/design choices become commitments. |
| Scope | Deployed VB6 behavior, live SQL Server metadata/data profiling, repository SQL/DALs, FO transaction inventory, architecture decision records. No stock code implementation. |
| Implementation work | Confirm all active stock writer entry points and `xVoidDelete` callers; profile `fd_tgl_jam_mutasi`, IDs, ties, backdating, deletes, row volumes, indexes, and triggers; characterize VB6 transaction/isolation behavior; reproduce MT/DT anomalies found in the script; choose a deletion-aware synchronization option; define deterministic lock order and conflict policy; approve the FO coverage matrix. |
| Legacy compatibility impact | None. This phase records existing behavior, including zero-row deletion, journal deletion, source writebacks, synthetic DO conventions, and post/void pairings. |
| Synchronization impact | Select the evidence-backed Legacy Change Discovery and Synchronization Position mechanism. Reject `fs_kd_trs` alone. Treat `(fd_tgl_jam_mutasi, fs_kd_trs)` as a candidate only if production data proves it. |
| Database impact | Read-only audit first. Produce proposed indexes/change-log/SQL feature option only after evidence. No destructive legacy migration. |
| Testing | Characterization fixtures for all routed FO families; data-profile queries; controlled concurrent VB6 sessions; insert/update/stock-delete/buku-delete/backdate/repost detection experiment. |
| Validation | Evidence identifies every active writer, deletion behavior, candidate change signal, and current isolation/locking behavior. Selected synchronization mechanism detects all tested change kinds. |
| Exit criteria | FQ-01–FQ-07 in the feasibility review are resolved or explicitly assigned with a safe interim decision; concurrency and synchronization ADRs are approved; no authority-cutover language is introduced. |
| Rollback/containment | Read-only phase. Any experiment uses disposable/test data and transactions that are rolled back. |
| Dependencies | Operations/DBA access and deployed VB6 confirmation. |
| Risks | Production schema may differ from repository; hidden writer/trigger may invalidate assumptions; no safe cursor may exist, requiring an additive change log or supported SQL Server change feature. |

**Phase 0 exit checklist**

- [x] FQ-01–FQ-07 resolved or assigned safe interim ([phase-0 report](./stock-ledger-phase-0-implementation-report.md)); FQ-02 = partial/deferred
- [x] Synchronization ADR approved ([ADR-stock-ledger-legacy-change-discovery.md](./adr/ADR-stock-ledger-legacy-change-discovery.md)) — fingerprint + bounded replay; reject id/watermark-alone; mismatch ⇒ set-diff and/or scoped re-derive
- [x] Concurrency ADR approved interim ([ADR-stock-ledger-mixed-writer-concurrency.md](./adr/ADR-stock-ledger-mixed-writer-concurrency.md)) — FQ-06 live VB6 proof still open
- [x] FO coverage matrix characterization-approved ([phase-0-writer-inventory.md](./evidence/phase-0-writer-inventory.md))
- [x] No authority-cutover language introduced
- [x] Exit Review **PASS WITH RISKS** — Phase 1 GO under scaffolding conditions
- [ ] Change-kind detection experiment (insert/update/stok-delete/buku-void-delete/backdate/repost) — **residual**; carries to **G-13** / Phase 3 (does not block Phase 1 scaffolding)
- [ ] Controlled concurrent VB6 sessions — **residual**; carries to **G-17** / Phase 9 (does not block Phase 1 scaffolding)

**Phase 1 readiness (Exit Review):** GO for additive foundation only. NO-GO for claiming G-13/G-17 done, production catch-up, or FO capability enablement.

### Phase 1 — Additive Stock Ledger foundation

**Progress:** IN PROGRESS — P1-S1…P1-S7 complete; P1-S8 remaining ([execution plan](./stock-ledger-phase1-implementation-plan.md), latest [P1-S7 summary](./stock-ledger-phase1-s7-implementation-summary.md)).  
**Execution plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md) (slices P1-S1 … P1-S8). Do not expand this roadmap section into slice detail.

| Area | Plan |
|---|---|
| Objective | Establish target domain objects, additive persistence, idempotency, coexistence state, and explicit application/infrastructure boundaries without changing legacy authority. |
| Scope | G-01–G-07, G-18 foundation, and continuous G-23 tests. |
| Implementation work | Implement Movement/Lines, Stock Layer/Position, ED-constrained FIFO, source idempotency, reconstruction/synchronization states, origin classification, repositories, and stock consequence UoW. Define ports for legacy reads, Legacy Compatibility Writer, Legacy Change Discovery, Availability Discovery, Provenance Discovery, and reconciliation. |
| Legacy compatibility impact | Legacy applications continue unchanged. New tables are additive and do not replace `tb_stok`/`tb_buku`. |
| Synchronization impact | Persist a mechanism-neutral Synchronization Position shape and state transitions; do not implement a guessed cursor. |
| Database impact | Add/revise Stock Ledger tables and indexes; include optimistic concurrency for positions and uniqueness for source consequences. Keep depleted layers. No legacy FK constraints or hidden trigger business logic. |
| Testing | Domain invariants; Movement immutability; ED+FIFO; multi-layer allocation; zero-layer retention; idempotency; state transitions; repository round trips; UoW rollback skeleton. |
| Validation | Domain has no SQL; Application owns orchestration; Infrastructure owns adapters; `Native` / `Reconstructed` / `LegacySynchronized` are origin only; no `IsAuthoritative` field exists. |
| Exit criteria | G-01–G-07 foundation accepted; additive migration applies to disposable DB; all new persistence can be disabled without affecting legacy stock. |
| Rollback/containment | Disable Stock Ledger feature flags and leave additive tables unused; legacy operations continue. |
| Dependencies | Phase 0 boundary and synchronization decisions. |
| Risks | Blindly restoring deleted FARIN DAL/repository code; preserving FEFO accidentally; encoding runtime authority in migration state. |

### Phase 2 — Initial Reconstruction baseline

| Area | Plan |
|---|---|
| Objective | Establish an idempotent Stock Ledger baseline for one Item + Receipt Source across all Stock Locations without long write transactions or authority transfer. |
| Scope | G-05, G-08, G-10, reconstruction portion of G-16/G-23. |
| Implementation work | Add cross-location legacy reads and indexes; implement Phase A claim, Phase B read/calculate, Phase C revalidate/persist; retain depleted reconstructed layers; classify ambiguity; initialize synchronization basis/position; produce `Reconstructed` or `Inconsistent`. |
| Legacy compatibility impact | Read-only against legacy authority. No legacy row is rewritten merely to make reconstruction convenient. |
| Synchronization impact | Reconstruction completion initializes, but does not permanently satisfy, freshness. Phase C must detect legacy basis change and retry or mark synchronization required. |
| Database impact | Add reconstruction state/basis fields and required query indexes proven in Phase 0. |
| Testing | Multi-location DO, depleted rows absent from `tb_stok`, missing/ambiguous history, concurrent reconstructors, legacy write during Phase B, retry, duplicate trigger, large-history timeout. |
| Validation | Balanced baseline reconciles to legacy authority at an identified basis; reconstruction does not block VB6 and does not set authority. |
| Exit criteria | G-10 acceptance passes; operators can identify `Reconstructing`, `Reconstructed`, and `Inconsistent`; no long transaction spans history calculation. |
| Rollback/containment | Delete/void only the incomplete additive reconstruction output through a controlled recovery action; legacy records remain untouched and authoritative. |
| Dependencies | Phase 1 persistence; Phase 0 query/index evidence. |
| Risks | Moving legacy basis, ambiguous Receipt Source distribution, bootstrap storms, long scans without approved indexes. |

### Phase 3 — Incremental Legacy Synchronization and Freshness Gate

| Area | Plan |
|---|---|
| Objective | Keep reconstructed or Native-origin Stock Ledger scopes current when VB6 continues to change authoritative legacy records. |
| Scope | G-12–G-17, synchronization part of G-23, initial G-24 observability. |
| Implementation work | Implement deletion-aware Legacy Change Discovery, durable Synchronization Position, idempotent catch-up, `LegacySynchronized` Stock Movement origin for post-baseline legacy facts, `LegacySynchronized` origin for any new layer established by those movements, preservation of an existing layer's establishment origin when only its quantity changes, void-to-correction/reversal interpretation, synchronization reconciliation, stale/inconsistent states, and Freshness Gate. Serialize synchronization with native writes at the chosen boundary. |
| Legacy compatibility impact | VB6 remains writable for every scope. Synchronization consumes legacy facts but does not rewrite them except through an explicitly authorized recovery/correction path. |
| Synchronization impact | First complete implementation. Position advances only with a committed catch-up batch and successful material reconciliation. |
| Database impact | Implement the Phase 0-selected change evidence mechanism and indexes. If an additive change log is chosen, all active writer paths must populate it transactionally before rollout. CDC/Change Tracking is used only if approved and operationally supported. |
| Testing | Legacy→New, duplicate batch, insert/update/depletion delete/void delete, backdated/tied movement, replay, synchronization/native race, sync crash/retry, intentional depleted-layer difference, real mismatch. |
| Validation | A later VB6 change makes the Ledger representation detectably stale; next Ledger touch synchronizes before allocation; repeated catch-up is quantity-neutral; material mismatch becomes `Inconsistent`. |
| Exit criteria | G-12–G-17 pass against production-like SQL; no test relies on disabling VB6; freshness is explainable by synchronization position and last outcome. |
| Rollback/containment | Disable new-system Stock Ledger decisions; leave authoritative legacy processing active. Preserve sync evidence for diagnosis; do not “fix” drift by deleting Ledger history. |
| Dependencies | Phase 2 baseline; Phase 0 selected detection/locking mechanisms. |
| Risks | Missed deletions, cursor retention gap, stale VB6 overwrite, duplicate correction, deadlock, unsupported live SQL feature. |

### Phase 4 — First native stock consequence: DO Receipt

| Area | Plan |
|---|---|
| Objective | Prove one new-system transaction can apply Stock Ledger semantics while producing authoritative legacy-compatible stock records. |
| Scope | G-11, G-18, G-19 and DM post/void characterization. |
| Implementation work | Implement idempotent DO Receipt; create Native-origin receipt Movement/Layer; invoke Legacy Compatibility Writer for `tb_stok`, `tb_buku`, and required DO/HPP behavior; initialize/update synchronization basis; implement receipt correction/void semantics appropriate to the source transaction. |
| Legacy compatibility impact | VB6 and existing readers see the same required receipt consequence. `tb_stok`/`tb_buku` remain authoritative. |
| Synchronization impact | New-system transaction leaves both representations current at commit. A later VB6 transaction changes legacy authority and is caught by Phase 3. |
| Database impact | One short transaction spans idempotency, Movement/Layer, coexistence state, legacy stock rows, and required source writebacks. |
| Testing | Receipt replay; transaction failure at each write; New→Legacy scenario; VB6 later transfers/consumes DO then new touch synchronizes; receipt void; legacy reader compatibility. |
| Validation | Native origin is visible without authority semantics; authoritative legacy rows are correct; rollback leaves neither representation partially committed. |
| Exit criteria | Technical slice complete behind capability flag. Production enabling remains blocked until mixed-writer and end-to-end coexistence gates in Phase 9 pass; it is not blocked merely by unmigrated outbound types. |
| Rollback/containment | Disable new DO Receipt capability; VB6 receipt remains usable; additive Ledger rows already committed remain historical evidence. |
| Dependencies | Phases 1 and 3; source DO authority/valuation inputs confirmed. |
| Risks | Treating Native as authority, omitting source writeback, partial transaction, assuming later VB6 outbound is an error rather than a synchronization input. |

### Phase 5 — Availability, FIFO, transfer, and first outbound

| Area | Plan |
|---|---|
| Objective | Prove safe outbound allocation and cross-location conservation under mixed writers. Stock Transfer is the preferred first outbound because it exercises Availability Discovery, FIFO, and paired locations. |
| Scope | G-03, G-08, transfer portion of G-20, G-16/G-17/G-18 hardening. |
| Implementation work | Implement Availability Discovery from legacy authority; run reconstruction/freshness per candidate; reload current layers; apply optional ED + FIFO; implement MT OUT/IN as one coordinated consequence with legacy-compatible rows and source writebacks; implement reversal. Add one simple final outbound (PK or MN) if operationally safer than MT for the first live pilot. |
| Legacy compatibility impact | Legacy MT/PK/MN may still process the same or related Receipt Sources. New paths preserve legacy pairings, deletion-at-zero, and source references. |
| Synchronization impact | Any legacy outbound after a new receipt or transfer is incorporated before subsequent Ledger allocation. Multi-DO transactions synchronize each affected scope. |
| Database impact | Deterministic lock order across source/destination legacy rows and Ledger positions; short consequence UoW; measured availability queries. |
| Testing | Concurrent VB6/.NET outbound on overlapping stock; multi-layer/multi-DO FIFO; transfer conservation; new receipt→VB6 transfer→new outbound; rollback between OUT/IN; reversal; deadlock retry. |
| Validation | No negative stock or stale FIFO commit; transfer net quantity is zero by Item + Receipt Source; legacy consumers remain operational. |
| Exit criteria | One outbound path is production-capable under coexistence; mixed-writer stress tests pass; no requirement disables VB6 for touched scopes. |
| Rollback/containment | Disable new outbound capability and leave VB6 path active; reconcile any committed additive Ledger facts against legacy authority. |
| Dependencies | Phases 3 and 4; Availability Discovery/index SLO. |
| Risks | Provisional legacy ordering mistaken for final FIFO, lock-order deadlocks, stale VB6 read-modify-write, partial transfer. |

### Phase 6 — Reservation and Virtual Stock Locations

| Area | Plan |
|---|---|
| Objective | Realize reservation as accountable transfer to a Virtual Stock Location while keeping legacy DR/DS behavior compatible. |
| Scope | G-21; DR reserved, DS serah, release/void paths. |
| Implementation work | Decide virtual-location identity/metadata from verified schema; map DR transfer and DS handover; implement reserve, release, consume, reversal, availability exclusion, provenance preservation, and legacy compatibility writeback. |
| Legacy compatibility impact | Existing DR/DS mutation and FO writeback semantics remain supported. The legacy schema is not forced to represent every virtual-location fact. |
| Synchronization impact | Legacy DR/DS activity must synchronize in order so reserved quantity is not duplicated, orphaned, or made ordinarily available. |
| Database impact | Add only the approved virtual-location representation and indexes; do not assume a new `JenisLokasi` without evidence. |
| Testing | Reserve/release/consume, partial reserve, void, alternating VB6/new DR/DS, sync ordering, ordinary availability exclusion, transfer conservation. |
| Validation | Reserved stock is unavailable to ordinary location requests; Legacy Stock Authority and Ledger virtual representation reconcile materially. |
| Exit criteria | DR/DS capability matrix and ADR approved; both legacy and new paths have tested synchronization semantics. |
| Rollback/containment | Disable new reservation paths; preserve legacy DR/DS; do not delete Ledger movement history. |
| Dependencies | Phase 5 transfer/allocation; virtual-location decision. |
| Risks | Mapping physical service location as virtual incorrectly; out-of-order synchronization; legacy reports ignoring reservation semantics. |

### Phase 7 — Additional transaction migration

| Area | Plan |
|---|---|
| Objective | Add stock transaction families one by one without forcing remaining VB6 flows to stop. |
| Scope | Remaining G-09/G-20/G-22: DU/DT, RU/RT, PK/MN, RB, AJ, RP, post and void variants. |
| Implementation work | For each family: characterize legacy input/output; choose Availability or Provenance Discovery; implement Stock Ledger consequence; implement Legacy Compatibility Writer details; add sync mapping; add idempotency, concurrency, post/void, failure, and rollback tests; enable by FO-type capability flag. |
| Legacy compatibility impact | Same/related VB6 transaction may remain active. Source PO/DO/HPP writebacks and synthetic legacy identifiers remain compatible where required, without inventing Stock Ledger provenance. |
| Synchronization impact | Every still-legacy route has a tested change-discovery and catch-up mapping. Returns restore original layer when known; legacy void deletes map to correction/reversal. |
| Database impact | Add only indexes/metadata required by the enabled family; no mass history migration. |
| Testing | Per-family matrix scenarios plus alternating writers and replay. Prioritize return provenance, adjustment authorization, repack valuation conservation, and typed-return legacy anomaly. |
| Validation | A transaction is marked supported only when new write, legacy compatibility, continued legacy route, synchronization, and reversal are all documented and tested. |
| Exit criteria | Approved capability matrix for enabled families; no enabled route lacks a coexistence path. Remaining families may stay on VB6. |
| Rollback/containment | Disable the affected FO capability flag; VB6 remains the operational fallback and authoritative records remain usable. |
| Dependencies | Phases 3–5; Provenance Discovery before return families. |
| Risks | Treating representation support as migration, omitting source writeback, inventing Receipt Source, enabling every family at once. |

### Phase 8 — Reconciliation and operational hardening

| Area | Plan |
|---|---|
| Objective | Make synchronization, drift, retries, and intentional representation differences explainable and recoverable at production scale. |
| Scope | G-16, G-23–G-26, performance and operational controls. |
| Implementation work | Add material reconciliation views/outcomes, intentional-difference catalog, stale/inconsistent work list, retry/recovery actions, structured logs/metrics, performance SLOs, retention monitoring for the selected change mechanism, feature-flag controls, and rollback runbook. |
| Legacy compatibility impact | Existing consumers remain unchanged. Operational views explicitly state that legacy records are authoritative. |
| Synchronization impact | Monitor lag, retries, duplicate suppression, cursor/change-token health, unresolved drift, and catch-up duration. |
| Database impact | Read-optimized indexes and lightweight operational outcome storage only; no required full Reconciliation Aggregate or broker. |
| Testing | Volume, long history, change-token retention gap, poison batch, retry exhaustion, intentional difference, real drift resolution, disable/re-enable, disaster/recovery rehearsal. |
| Validation | Operators can explain and recover stale/inconsistent scopes without editing authoritative stock ad hoc or erasing Ledger history. |
| Exit criteria | Production SLOs and runbook accepted; alerts distinguish pending synchronization from material inconsistency; rollback rehearsal leaves VB6 stock usable. |
| Rollback/containment | Disable new capabilities independently; retain diagnostics and history; legacy operation continues. |
| Dependencies | Working transaction slices and synchronization telemetry. |
| Risks | Alert fatigue from false differences, cursor retention loss, expensive reconciliation, unsafe manual correction. |

### Phase 9 — Coexistence production rollout

| Area | Plan |
|---|---|
| Objective | Run VB6 and enabled new-system stock transactions safely with Legacy Stock Authority preserved. |
| Scope | Selected pilot locations/items/FO types, then measured expansion. No final cutover. |
| Implementation work | Preflight schema/change-discovery health; enable capability flags gradually; monitor synchronization lag, reconciliation, duplicates, negative-stock conflicts, legacy errors, and performance; conduct rollback drills; expand only after stable evidence. |
| Legacy compatibility impact | VB6 remains active and authoritative. Existing reports, reads, and transaction scripts must remain operational. |
| Synchronization impact | Continuous/on-demand catch-up meets SLO; Freshness Gate fails closed when current state cannot be proven. |
| Database impact | Operational migrations/indexes approved by DBA; no per-scope cutover marker. |
| Testing | Full Stage B scenarios in §8, production smoke tests, canary mixed-writer stress, feature-disable test, legacy-only continuity test. |
| Validation | All production-readiness criteria in §9 are evidenced for the enabled capability set. |
| Exit criteria | Coexistence release accepted; both applications process stock safely; no commitment to migrate every FO family or enter Stage C. |
| Rollback/containment | Turn off new transaction capabilities; keep VB6 processing and `tb_stok`/`tb_buku` usable; reconcile additive Ledger representation later. |
| Dependencies | Phases 0–4 and Phase 8 minimum for a DO Receipt-only pilot; Phase 5 only when new-system outbound is enabled; Phases 6–7 only for the enabled reservation or additional transaction families. |
| Risks | Treating pilot success as final cutover, expanding before synchronization SLO stabilizes, hidden writer, production-only lock contention. |

---

## 5. Normative request flows

### 5.1 Outbound with no Receipt Source on the source line

```text
Stock Consequence Request
    -> Availability Discovery against Legacy Stock Authority
    -> provisional Receipt Source candidates
    -> for each required scope:
         reconstruct if no baseline
         pass Legacy Freshness Gate
         synchronize pending legacy facts
    -> reload current Stock Ledger layers
    -> apply explicit Expiration Date filter when supplied
    -> apply FIFO by Effective Receipt Time, then Layer ID
    -> revalidate authoritative legacy quantity inside short transaction
    -> write Legacy-Compatible Stock Consequence
    -> write Stock Ledger Movement/Layer changes
    -> commit once
```

### 5.2 Later legacy change

```text
VB6 transaction
    -> authoritative tb_stok / tb_buku changes
    -> Stock Ledger representation becomes potentially stale

Next Ledger-dependent request (or bounded catch-up worker)
    -> detect change after Synchronization Position
    -> apply legacy consequence idempotently
    -> reconcile material position
         -> Current: continue
         -> Inconsistent: fail closed and surface recovery
```

### 5.3 Reconstruction transaction boundaries

```text
TX-A: claim Reconstructing, commit
No long TX: read and calculate baseline
TX-C: revalidate legacy basis, persist baseline, initialize sync position, commit
Later stock consequence: separate short UoW
```

Reconstruction success never transfers authority.

---

## 6. Transaction rollout matrix

Unless a row explicitly says otherwise, the legacy application **may continue to perform the same or a related transaction during coexistence**. “Yes” in the authority column always means `tb_stok + tb_buku` remain authoritative.

| Transaction | Legacy writer today | New Stock Ledger support | Legacy record still authoritative? | Reconstruction needed? | Synchronization implication | Recommended phase |
|---|---|---|---|---|---|---|
| DO Receipt `DM` post/void | `GenStokDO` / `GenStokDOVoid`; inbound add and reversal/removal | Native receipt Movement/Layer; legacy-compatible receipt; accountable reversal | Yes | No for a new DO; existing/void history may need baseline | Later VB6 activity on the DO must be discovered and synchronized | 4 |
| Stock Transfer `MT` post/void | `GenStokMutasi` / Void; OUT+IN pair across locations | Paired transfer movement, FIFO allocation, conservation, reversal | Yes | Yes for legacy Receipt Sources not yet reconstructed | Sync both legs/order; mixed-writer lock across affected locations | 5 |
| Stock Usage `PK` post/void | `GenStokPakai` / Void; FIFO outbound/restore | Final outbound + restore/reversal | Yes | Usually, after Availability Discovery | Legacy usage after baseline catches up before next Ledger allocation | 5 or 7 |
| General Sale/Dispense `DU` post/void | `GenStokDOBillUmum` / Void; outbound + PO/DO/HPP writeback | Outbound Movement; compatibility writeback; reversal | Yes | Usually | Sync legacy outbound and preserve writeback provenance for returns | 7 |
| Reserved `DR` post/void | `GenStokDOBillUmumReserved` / Void; transfer-like OUT+IN | Transfer to/from Virtual Stock Location | Yes | Usually | Sync reservation legs in order; avoid duplicate ordinary availability | 6 |
| Handover `DS` post/void | `GenStokDOBillUmumSerah` / Void; reserved destination outbound | Consume/release from Virtual Stock Location | Yes | Usually | Sync against prior DR chain; prevent orphan reserved quantity | 6 |
| Typed Sale `DT` post/void | `GenStokDOBillTipe` / Void; outbound + writeback | Outbound Movement and reversal | Yes | Usually | Void mutation-type anomaly must be characterized before mapping | 7 |
| Destruction `MN` post/void | `GenStokMusnah` / Void; outbound/restore | Authorized final outbound and reversal | Yes | Usually | Synchronize legacy disposition; Stock Ledger adds accountable history | 5 or 7 |
| Purchase Return `RB` post/void | `GenStokReturBeli` / Void; outbound constrained by DO | Provenance-preserving supplier return and reversal | Yes | Yes if original DO baseline absent | Known DO narrows scope; later legacy return/void must catch up | 7 |
| General Sales Return `RU` post/void | `GenStokReturJualUmum` / Void; inbound split from original sale lines | Provenance Discovery; restore original layers; reversal | Yes | Original scopes may need reconstruction | Sync each restored provenance slice idempotently | 7 |
| Typed Sales Return `RT` post/void | `GenStokReturJualTipe` / Void; may use return ID as synthetic DO | Provenance Discovery or explicit unknown-provenance fallback | Yes | Yes when original scope is found | Do not import synthetic DO as proven provenance; reconcile fallback | 7 |
| Adjustment `AJ` plus/minus/void | `GenStokAdjust` / Void; inbound or outbound | Already-authorized adjustment Movement; reversal | Yes | Minus usually; plus may form an accountable layer | Sync legacy AJ; Stock Opname remains observation, not adjustment authority | 7 |
| Repack `RP` post/void | `GenStokRepack` / Void; material OUT, result IN, HPP writeback | Traceable transformation consequences and valuation conservation | Yes | Material inputs usually | Sync input/output as one business responsibility; partial sync is inconsistent | 7 |
| `DB` / `RJ` constants | Constants exist, but no `Generate()` route is evidenced in this repository | None until active writer/use is proven | Yes if active externally | Unknown | Inventory hidden writer before enabling overlapping capability | 0 investigation / defer |

A row becomes “new-system supported” only after post/void behavior, Legacy Compatibility Writer, synchronization mapping, concurrency, idempotency, rollback, and reconciliation tests pass. Representation capability alone is insufficient.

---

## 7. Mixed-writer concurrency plan

### 7.1 Required conflict behavior

| Conflict | Minimum handling |
|---|---|
| New reads, VB6 commits before new write | Revalidate authoritative quantity and sync basis; retry or reject |
| VB6 reads, new commits before VB6 update | Shared lock/conditional-update protocol must prevent stale overwrite; Phase 0 must prove the mechanism |
| Reconstruction and legacy write overlap | Phase C basis mismatch; retry or remain not current |
| Synchronization and native consequence overlap | Serialize/version-check affected write boundaries; native cannot use pre-sync layers |
| Two new requests share layers | Position version/conditional update and deterministic lock order |
| FIFO selected layers change before commit | Reload/revalidate in consequence transaction |
| Multi-location transfer races another outbound | Lock in deterministic location/order and commit OUT+IN atomically |

### 7.2 Minimum SQL Server strategy

Target the smallest protocol compatible with deployed behavior:

* one short transaction for each new-system consequence;
* deterministic ordering by Item, Receipt Source, Stock Location, and legacy row identity;
* update/range locks or equivalent atomic conditional mutation on authoritative legacy rows;
* revalidation of selected quantity and synchronization basis before persistence;
* optimistic concurrency on Stock Ledger positions;
* unique idempotency keys;
* bounded deadlock/version retries.

`UPDLOCK, HOLDLOCK` is evidenced elsewhere in the repository and is a candidate, not an automatic answer. Because the visible VB6 flow performs read-modify-write, .NET-only locking is insufficient until cross-application behavior is tested. If a shared protocol requires a minimal legacy compatibility change, isolate it at stock persistence; do not rewrite unrelated VB6 transactions into DDD.

---

## 8. Testing strategy

### Required end-to-end ordering scenarios

1. **Legacy then New**
   ```text
   VB6 transaction
       -> legacy authority changes
       -> new application touches same Item + DO
       -> detects + synchronizes
       -> correct Stock Ledger outcome
   ```
2. **New then Legacy**
   ```text
   new consequence
       -> authoritative legacy + Stock Ledger records
       -> VB6 modifies same Item + DO
       -> Stock Ledger catches up
   ```
3. **Alternating writers**
   ```text
   New -> Legacy -> Legacy -> New -> New
   ```
   Verify quantity conservation, source idempotency, and synchronization-position advancement.
4. **Depleted layer difference** — legacy deletes zero `tb_stok`; Ledger retains depleted layer; result is balanced.
5. **Real mismatch** — authoritative quantity cannot be reconciled; result is explicit `Inconsistent`.
6. **Duplicate synchronization** — process the same legacy delta/batch twice; quantity changes once.
7. **Concurrent outbound** — VB6 and .NET consume overlapping quantity; negative stock/lost update is impossible.
8. **Moving reconstruction basis** — VB6 commits during Phase B; Phase C retries or leaves scope not current.
9. **Sync/native race** — native allocation waits/retries and uses post-sync facts.
10. **Partial failure** — failure in legacy-compatible or Ledger persistence rolls back the entire new consequence.
11. **Void/delete discovery** — physical legacy deletion is detected and mapped without deleting Stock Ledger history.
12. **Feature disable** — new processing is disabled; VB6 and legacy stock remain operational.

### Test layers

| Layer | Required focus |
|---|---|
| Domain | FIFO, provenance, conservation, no negative quantity, retained depleted layers, corrections/reversals |
| Application | Freshness Gate, reconstruction/sync orchestration, idempotency, state transitions, failure outcomes |
| Infrastructure | Legacy queries, deletion detection, position atomicity, locks, indexes, UoW rollback |
| Integration | Real SQL Server transaction/isolation behavior, VB6-shaped fixtures, FO writebacks |
| Concurrency | Alternating and simultaneous writers, deadlock retry, stale overwrite prevention |
| Operational | Lag/reconciliation metrics, recovery, feature-disable continuity |

---

## 9. Coexistence production-readiness gate

Before Phase 9 enables a transaction capability:

- [ ] Legacy application can still process applicable stock.
- [ ] `tb_stok + tb_buku` remain authoritative and readable by legacy consumers.
- [ ] New consequences produce complete legacy-compatible authoritative records.
- [ ] Stock Ledger records richer provenance, layers, movements, and allocation detail.
- [ ] Later legacy activity is detected, deduplicated, synchronized, and reconciled.
- [ ] Freshness is proven before every Ledger-dependent stock decision.
- [ ] Intentional representation differences are classified without false alarms.
- [ ] Real quantity/provenance drift becomes explicit `Inconsistent`.
- [ ] Concurrent VB6/.NET outbound cannot produce negative stock or lost update.
- [ ] Failure injection proves atomic new-system consequence persistence.
- [ ] Capability flags can disable new processing without disabling VB6 or corrupting legacy stock.
- [ ] Required FO post/void/writeback behavior has characterization and regression tests.
- [ ] Synchronization SLO, retention, retry, and recovery runbook are accepted.
- [ ] No implementation or operator procedure treats `Native` / `Reconstructed` / `LegacySynchronized` as authority.

Minimum production readiness supports a limited enabled transaction set safely. It does not require every stock transaction to migrate.

---

## 10. Phase summary

| Phase | Outcome | Primary gaps | Explicit non-goal |
|---|---|---|---|
| 0 | Proven legacy/sync/concurrency baseline (**frozen** 2026-08-07; PASS WITH RISKS; FQ-06 + detection experiment residual) | G-13 empirical proof; G-17; G-25 SLO/index approval; G-28 | Code implementation |
| 1 | Additive domain and persistence foundation | G-01–G-07, G-18 | Legacy authority change |
| 2 | Initial bounded reconstruction | G-05, G-10 | Per-scope cutover |
| 3 | Incremental synchronization + Freshness Gate | G-12–G-17 | Blocking VB6 |
| 4 | DO Receipt native consequence | G-11, G-19 | Requiring all outbound migration |
| 5 | Availability/FIFO/transfer/first outbound | G-08, part of G-20 | Exclusive DO ownership |
| 6 | Virtual reservation/handover | G-21 | Forced legacy schema parity |
| 7 | Transaction-by-transaction expansion | G-09, G-20, G-22 | Big-bang migration |
| 8 | Reconciliation/operations hardening | G-16, G-23–G-26 | Mandatory broker/CDC |
| 9 | Safe Stage B production coexistence | Production gate | Final/global cutover |

---

## 11. Engineer checklist per implementation increment

- [ ] Canonical domain rules and relevant gap IDs are referenced.
- [ ] `tb_stok + tb_buku` remain the declared Stage B source of truth.
- [ ] No `IsAuthoritative`, per-DO ownership, or VB6 prohibition was added.
- [ ] Reconstruction scope, write boundary, lock boundary, and authority are distinct.
- [ ] `Native` / `Reconstructed` / `LegacySynchronized` are origin only.
- [ ] Availability Discovery and Provenance Discovery are not conflated.
- [ ] Freshness Gate precedes trusted Ledger allocation.
- [ ] Legacy-originated changes have deletion-aware, idempotent synchronization.
- [ ] New consequence writes legacy-compatible authoritative records and Ledger representation atomically.
- [ ] Legacy zero-row deletion and required void/writeback behavior remain compatible.
- [ ] Stock Ledger retains depleted layers and correction/reversal history.
- [ ] Concurrent mixed-writer behavior and negative-stock prevention are tested.
- [ ] Capability coverage includes post, void, rollback, sync, and legacy continuation.
- [ ] Disabling the new capability leaves legacy stock usable.
- [ ] No final cutover work is implied.

---

## 12. Remaining technical unknowns

| Unknown | Required resolution |
|---|---|
| Live population/order of `fd_tgl_jam_mutasi` | **Resolved (Phase 0):** 100% populated; ties + deletes preclude watermark-alone cursor |
| Completeness/transactionality of legacy ID counters | **Partial / deferred (Phase 0):** ID patterns profiled; multi-instance atomicity still needs live concurrency test |
| Every deployed `xVoidDelete` caller and hidden stock writer | **Partial:** script + snapshot; ops signoff + `AJX_*` owner still needed |
| Production indexes/triggers and row volumes | **Resolved (Phase 0 snapshot):** see evidence pack; proposed indexes pending DBA |
| VB6 transaction/isolation/lock behavior | **Open:** controlled mixed-writer test (blocks Phase 9) |
| Best deletion-aware change mechanism | **Resolved (Phase 0 ADR):** fingerprint + bounded replay; CT/CDC deferred |
| Active meaning of unrouted `DB` / `RJ` constants | Legacy owner confirmation (absent in snapshot) |
| `GenStokMutasi` and typed-sale void anomalies in deployed binary/data | **Characterized:** MT void-leg imbalance in data; DT→`DU_V` in script; DT unused locally |
| Virtual Stock Location representation | Phase 6 ADR |
| Batch-constrained selection policy | Deferred business decision |

These unknowns constrain technical implementation; they do not alter the domain decision that Legacy Stock Authority remains the source of truth throughout coexistence.
