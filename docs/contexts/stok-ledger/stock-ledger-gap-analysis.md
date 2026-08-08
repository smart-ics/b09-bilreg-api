# Stock Ledger — Gap Analysis

**Artifact status:** Dependency-ordered implementation gap backlog — Phase 0 baseline **frozen** (2026-08-07)
**Basis:** Current codebase vs [`stok-ledger-domain.md`](./stok-ledger-domain.md) and [`stock-ledger-feasibility-review.md`](./stock-ledger-feasibility-review.md)
**Execution plan:** [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md)
**Phase 0 exit:** [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md)
**Phase 1 plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)
**Phase 2 report:** [`stock-ledger-phase2-implementation-report.md`](./stock-ledger-phase2-implementation-report.md)
**Phase 3 plan:** [`stock-ledger-phase3-implementation-plan.md`](./stock-ledger-phase3-implementation-plan.md)

**Authority rule:** During coexistence, `tb_stok + tb_buku` remain the authoritative persisted stock truth. No gap in this backlog establishes per-Item + Receipt Source authority, prevents later VB6 activity, or treats `Native` / `Reconstructed` as authority states.

**Phase 0 freeze note (2026-08-07):** Evidence fields for G-13/G-14/G-17/G-22/G-25 updated from snapshot profiling. G-19 dependencies aligned with roadmap Phase 4. G-28 added for hidden writer vocabulary. Do not reopen Stage B authority or origin semantics via gap edits.

---

## 1. Priority and acceptance model

| Priority | Meaning |
|---|---|
| P0 | Blocks safe coexistence or every first production stock consequence |
| P1 | Required for the first production rollout |
| P2 | Expands transaction/domain coverage after minimum rollout |
| P3 | Deferred improvement; not a coexistence gate |

Every gap records current evidence, why it matters, required capability, dependency, and acceptance criteria. Proposed type/table/port names are planning concepts, not claims that implementation already exists.

### Steering rules

* Reconstruction and reconciliation scope = Item + Receipt Source across all Stock Locations.
* Candidate write consistency boundary = Item + Receipt Source + Stock Location, subject to concurrency proof.
* Source-of-truth authority = `tb_stok + tb_buku` globally during coexistence.
* `Native`, `Reconstructed`, and `LegacySynchronized` = Stock Ledger fact/layer origin only.
* Initial Reconstruction != Incremental Legacy Synchronization.
* Legacy Freshness Gate replaces the rejected Authority Gate.
* New-system consequences write legacy-compatible authoritative records plus the richer Stock Ledger Representation.
* Availability Discovery and Provenance Discovery remain separate.
* Full/final cutover is deferred and is not a P0/P1 gap.

---

## 2. Current-to-target map

The backlog uses the same migration stages as the canonical domain:

| Stage | Meaning | Backlog treatment |
|---|---|---|
| Stage A — Legacy Authority | VB6 and `tb_stok + tb_buku` operate; Stock Ledger representation may be absent | Current baseline |
| Stage B — Coexistence | Both applications may process stock; legacy records remain authoritative; Stock Ledger reconstructs and synchronizes | Target of every P0/P1 coexistence gap |
| Stage C — Future Final Cutover | Stock Ledger could become source of truth and legacy records a compatibility projection | Deferred; not a gap in this backlog |

```text
Current
    VB6 clbGenStokX1 = evidenced stock transaction writer
    tb_stok          = mutable current stock (insert/update/delete)
    tb_buku          = legacy journal (insert; void may delete)
    C# StokModel     = unwired location-scoped FEFO/kartu-stok spike
    FARIN schema     = incomplete/unwired

Target coexistence
    Legacy Stock Authority
        = tb_stok + tb_buku

    Stock Ledger Representation
        = immutable movements
        + retained Stock Layers, including depleted layers
        + provenance and deterministic FIFO allocation

    Initial Reconstruction
        = Item + Receipt Source across all locations

    Legacy Synchronization
        = deletion-aware, idempotent incorporation of later legacy changes

    New-system consequence
        = Freshness Gate
        + Stock Ledger behavior
        + Legacy-Compatible Stock Consequence
        + one short transaction
```

---

## 3. Superseded gaps and concepts

| Previous concept | Disposition | Replacement |
|---|---|---|
| `Authority Gate` (former pre-coexistence gap ID G-22) | **Removed** | G-12 Legacy Freshness / Synchronization Gate |
| `IsAuthoritative = Native OR Reconstructed` | **Removed** | G-04 separate Reconstruction Status, Synchronization State/Position, and Fact Origin (`Native`, `Reconstructed`, `LegacySynchronized`) |
| `Legacy Projection Writer` | **Reworked** | G-11 Legacy Compatibility Writer for Stage B |
| Scope-aware feature flags that block VB6 | **Removed** | Capability/FO-type flags plus synchronization readiness |
| “No VB6-only writer after reconstruction” exit criteria | **Removed** | Mixed-writer synchronization and reconciliation acceptance criteria |
| First DO Receipt requires all later outbound flows to migrate | **Removed** | First DO Receipt requires safe later legacy synchronization and mixed-writer protection |

`Legacy Projection` is reserved for a possible future post-cutover Stage C and is not a current implementation responsibility.

---

## 4. Gap catalog

### G-01 — Boundary separation

| Field | Detail |
|---|---|
| Current evidence | `StokModel` is keyed effectively by Item + Location; older planning conflated reconstruction, write, locking, and authority scopes. |
| Why it matters | Over-wide locks reduce throughput; under-wide boundaries allow overlapping quantity updates; neither changes runtime authority. |
| Required capability | Explicitly model reconstruction/reconciliation at Item + Receipt Source across locations; evaluate Item + Receipt Source + Location for writes; document database lock order separately. |
| Priority | P0 |
| Dependencies | None |
| Acceptance criteria | Tests cover multiple Receipt Sources at one location, one Receipt Source across locations, and transfer across two write boundaries; no code/status derives authority from any boundary. |

### G-02 — Immutable Stock Movement model

| Field | Detail |
|---|---|
| Current evidence | Only nested `StokBukuType` lines exist; no complete Movement header, pairing, correction, or reversal model. |
| Why it matters | Rich traceability, idempotency, transfer pairing, synchronization correction, and retained history need immutable movement identity. |
| Required capability | Movement and Movement Lines for receipt, outbound, transfer, return, adjustment, correction, reversal, reconstruction, and synchronized legacy consequences. |
| Priority | P0 |
| Dependencies | G-01 |
| Acceptance criteria | A completed movement cannot be edited/deleted; a reversal references and counteracts the original; transfer lines conserve quantity. |

### G-03 — ED-constrained FIFO allocation

| Field | Detail |
|---|---|
| Current evidence | `StokModel.RemoveStok` orders by Expiration Date (FEFO); domain requires optional explicit ED filtering followed by FIFO. |
| Why it matters | Wrong ordering can select the wrong Receipt Source, valuation, and provenance. |
| Required capability | Filter by requested Item, Stock Location, and optional Expiration Date; then FIFO by Effective Receipt Time and deterministic Layer ID; support multi-layer allocation. |
| Priority | P0 |
| Dependencies | G-01 |
| Acceptance criteria | Tests cover no ED, explicit ED, equal receipt time tie-break, multi-layer split, insufficient stock, and absence of Batch from the initial request. |

### G-04 — Coexistence state and origin persistence

| Field | Detail |
|---|---|
| Current evidence | No implemented state store. Previous plan proposed `LegacyOnly`, `Native`, `Reconstructed`, and derived `IsAuthoritative`. |
| Why it matters | Reconstruction readiness, synchronization freshness, origin, and authority are different dimensions. Conflating them reintroduces the rejected cutover. |
| Required capability | Persist Reconstruction Status; Synchronization State and Position; inconsistency reason; reconstruction basis/version. Persist `Native`, `Reconstructed`, or `LegacySynchronized` as fact/layer origin only. Do not persist `IsAuthoritative`. |
| Priority | P0 |
| Dependencies | G-01 |
| Acceptance criteria | Native receipt, reconstructed baseline facts, and post-baseline legacy-synchronized facts have distinct origins; all remain under Legacy Stock Authority. A synchronization movement that only changes an existing layer does not rewrite that layer's establishment origin. |

### G-05 — Legacy reconstruction read adapter

| Field | Detail |
|---|---|
| Current evidence | `tb_buku_dal` reads by Item + Location + DO and has an enumerable binding defect; `tb_stok_dal` lacks Item + DO across-locations access. |
| Why it matters | Baseline reconstruction must include every location for the Receipt Source. |
| Required capability | Parameterized reads for `tb_buku` and `tb_stok` by Item + Receipt Source across all Stock Locations, with measured indexes and deterministic input ordering. |
| Priority | P0 |
| Dependencies | G-01 |
| Acceptance criteria | Multi-location fixture reconstructs from a bounded query; query plan/SLO is recorded; input includes surviving current rows and historical rows required for provenance. |

### G-06 — Stock Layer/Position persistence and concurrency

| Field | Detail |
|---|---|
| Current evidence | FARIN schemas exist but C# DAL/repository stack is absent/unwired; no `rowversion` is evidenced. |
| Why it matters | Remaining Quantity and retained zero layers require durable state and conflict detection. |
| Required capability | Additive persistence for layers/positions, zero-quantity retention, origin, Effective Receipt Time, deterministic order, and optimistic concurrency. |
| Priority | P0 |
| Dependencies | G-01, G-04 |
| Acceptance criteria | Round-trip preserves depleted layers; concurrent updates conflict instead of losing quantity; retries do not create duplicate layers. |

### G-07 — Movement persistence and source idempotency

| Field | Detail |
|---|---|
| Current evidence | `FARIN_StokBuku` is line-shaped and unwired; no complete Movement store or source uniqueness constraint exists. |
| Why it matters | New requests and synchronization retries must apply one accountable consequence at most once. |
| Required capability | Append-only movement persistence, source responsibility key, correction/reversal link, origin, and unique duplicate protection. |
| Priority | P0 |
| Dependencies | G-02 |
| Acceptance criteria | Replaying the same new-system request or legacy synchronization fact does not change quantity twice; duplicate result is deterministic. |

### G-08 — Availability Discovery

| Field | Detail |
|---|---|
| Current evidence | Ordinary FO lines often omit `KodeDO`; no dedicated discovery service exists. `tb_stok` contains authoritative available stock during coexistence. |
| Why it matters | Outbound transactions cannot allocate layers until candidate Receipt Sources are known. |
| Required capability | Discover authoritative available quantity by Item + Stock Location (+ optional ED) from legacy records, using synchronized Stock Ledger layers only as enriched allocation input. Return provisional candidates, not final FIFO. |
| Priority | P0 |
| Dependencies | G-05 |
| Acceptance criteria | Outbound without DO finds candidate Receipt Sources; depleted/missing legacy rows are treated correctly; final allocation is recalculated only after freshness/reconstruction checks. |

### G-09 — Provenance Discovery

| Field | Detail |
|---|---|
| Current evidence | Legacy general returns use original sale details; typed return may synthesize return ID as DO; no dedicated C# capability exists. |
| Why it matters | Returns, reversals, and corrections must preserve original Receipt Source when determinable. |
| Required capability | Resolve original Receipt Source/layer from source transaction lines, legacy writeback fields, `tb_buku`, and Stock Ledger movements; return explicit unknown/ambiguous result. |
| Priority | P1 |
| Dependencies | G-02, G-05, G-07 |
| Acceptance criteria | Known provenance restores the original/depleted layer; return transaction ID remains Source Transaction Reference; unknown provenance does not invent a historical DO. |

### G-10 — Initial Reconstruction use case

| Field | Detail |
|---|---|
| Current evidence | Only a fully commented, wrong-scope reconstruction sketch exists. |
| Why it matters | Stock Ledger cannot allocate or reconcile legacy-origin stock without a baseline. |
| Required capability | Phased reconstruction: short claim transaction; bounded read/calculation; short revalidation/persist transaction; initialize Synchronization Position; classify `Reconstructed` or `Inconsistent`. |
| Priority | P0 |
| Dependencies | G-04, G-05, G-06, G-07 |
| Acceptance criteria | Concurrent reconstruction has one committed baseline; legacy basis change during calculation triggers retry/not-current; repeated request is idempotent; success does not change authority or block VB6. |

### G-11 — Legacy Compatibility Writer

| Field | Detail |
|---|---|
| Current evidence | Legacy DAL primitives exist; no new-system stock consequence orchestrates `tb_stok`, `tb_buku`, and required FO writebacks. |
| Why it matters | During Stage B, new-system transactions must produce authoritative legacy-compatible persisted consequences. |
| Required capability | Explicit adapter for `tb_stok` insert/update/delete, `tb_buku` movement/void shapes, and required PO/DO/HPP source writebacks, enlisted in the new-system consequence transaction. |
| Priority | P0 |
| Dependencies | G-05, G-07 |
| Acceptance criteria | New receipt/outbound results are consumable by existing legacy readers; delete-on-zero remains compatible; compatibility failure rolls back the complete new-system consequence. |

### G-12 — Legacy Freshness / Synchronization Gate

| Field | Detail |
|---|---|
| Current evidence | No freshness check exists (post–Phase 2). Availability Discovery exposes reserved `StaleOrNotCurrent` but never returns it. The previous `Authority Gate` incorrectly blocked VB6 after reconstruction. Phase 3 **P3-S5** owns the gate. |
| Why it matters | Stock Ledger layers can become stale whenever VB6 changes legacy stock. |
| Required capability | Before trusted allocation or mutation, determine whether applicable legacy facts exceed the Synchronization Position; synchronize or fail closed/mark not current. The gate must never reject a legacy write merely because the scope is Native/Reconstructed. |
| Priority | P0 |
| Dependencies | G-04, G-10, G-13, G-14 |
| Acceptance criteria | Legacy change after reconstruction is detected; new processing waits for successful synchronization; unchanged scope proceeds; undeterminable freshness produces explicit `Inconsistent`/not-current outcome. |

### G-13 — Incremental Legacy Movement Discovery

| Field | Detail |
|---|---|
| Current evidence | Phase 0 (`HOSPITAL_HPL`): `fd_tgl_jam_mutasi` 100% populated but unsafe as sole cursor (ties, ID≠time, void deletes). ADR selects **fingerprint + bounded replay**; mismatch must set-diff Ledger-known identities and/or scoped re-derive. Phase 2 delivered `fingerprint-v1` init + G-05 reads + `ILegacyChangeDiscoveryPort` contract/fake only. Empirical detection experiment **not yet run** (→ Phase 3 **P3-S1**). |
| Why it matters | Incremental synchronization needs complete detection of inserts, quantity updates, depletion deletes, and void deletes. |
| Required capability | Implement and validate the Phase-0-selected deletion-aware mechanism (fingerprint + bounded delta/replay; optional non-authoritative time hint). Do not hard-code watermark-alone or `fs_kd_trs`-alone cursors. |
| Priority | P0 |
| Dependencies | G-05; Phase 0 ADRs; live DB/operations evidence |
| Acceptance criteria | Production-like tests detect insert, update, `tb_stok` delete, `tb_buku` void delete, backdated/tied movement, and repost; selected mechanism has retention/recovery and performance evidence. |

### G-14 — Synchronization Position

| Field | Detail |
|---|---|
| Current evidence | Phase 0 ADR: persist **mechanism-neutral** opaque Synchronization Position + algorithm version; first concrete payload is scoped fingerprint (not a watermark). Phase 1/2 persist and **initialize** position at reconstruction; Phase 3 must **advance** it only after catch-up + reconcile (**P3-S4**). |
| Why it matters | Freshness and retry need a committed boundary between reflected and pending legacy facts. |
| Required capability | Persist opaque position + algorithm version; advance only after the synchronization batch and reconciliation commit; hash drift alone does not list deletes — pair with G-13 set-diff/re-derive. |
| Priority | P0 |
| Dependencies | G-04, G-13 |
| Acceptance criteria | Crash before commit leaves prior position; retry is safe; position cannot advance past an unapplied/delete-undetected change; operational tooling can explain the current position. |

### G-15 — Catch-up idempotency and Legacy Synchronization

| Field | Detail |
|---|---|
| Current evidence | No synchronization use case exists. `SyncBatch` idempotency kind and `LegacySynchronized` origin are reserved in Domain/DB but unused. Phase 3 **P3-S2…P3-S4** own interpretation + catch-up. |
| Why it matters | Duplicate polling/retry can inflate or reduce quantity twice; voids require correction semantics. |
| Required capability | Convert discovered legacy deltas into accountable Stock Ledger movements/corrections with `LegacySynchronized` origin and the legacy Source Transaction Reference; establish any new layer created by that movement as `LegacySynchronized`; update existing layers without rewriting their establishment origin; deduplicate, retry, reconcile, and advance position atomically for the batch. |
| Priority | P0 |
| Dependencies | G-02, G-07, G-13, G-14 |
| Acceptance criteria | Same legacy movement/batch processed twice is quantity-neutral; legacy void becomes accountable correction/reversal; failed batch retries; success returns state to `Current`. |

### G-16 — Synchronization reconciliation and drift classification

| Field | Detail |
|---|---|
| Current evidence | No reconciliation behavior exists. Legacy and Stock Ledger intentionally represent zero rows and void history differently. |
| Why it matters | Raw row equality creates false positives; ignoring differences hides real stock drift. |
| Required capability | Reconcile Item + Receipt Source across locations; classify intentional representational differences, pending synchronization, provenance limitation, and material inconsistency. |
| Priority | P0 for material drift classification that gates allocation; P1 for operational reporting views |
| Dependencies | G-06, G-07, G-15 |
| Acceptance criteria | Deleted zero `tb_stok` row vs retained depleted layer is balanced; real quantity mismatch is explicit; unresolved material difference leaves scope `Inconsistent`. |

### G-17 — Mixed-writer concurrency

| Field | Detail |
|---|---|
| Current evidence | Phase 0 concurrency ADR: interim policy (short TX; Item→Receipt Source→Location→legacy row order; revalidate; conditional update; Ledger OCC; `UPDLOCK/HOLDLOCK` as .NET candidate only). FQ-06 **unresolved** — no controlled VB6 session proof yet. |
| Why it matters | VB6 and .NET can consume overlapping quantity or create stale FIFO decisions. |
| Required capability | Characterize deployed VB6 transactions; prove shared protocol under load; revalidate legacy quantity and synchronization basis before commit; retry deadlocks/conflicts. |
| Priority | P0 |
| Dependencies | G-01, G-11; operational evidence (FQ-06) |
| Acceptance criteria | Concurrent legacy/new outbound stress test yields one valid winner or non-overlapping allocations, never negative stock/lost update; reconstruction/synchronization/native overlap tests are deterministic. |

### G-18 — Consequence Unit of Work

| Field | Detail |
|---|---|
| Current evidence | Generic transaction helpers exist; no stock UoW spans legacy authority and Stock Ledger stores. |
| Why it matters | A new-system transaction cannot claim success with only one representation committed. |
| Required capability | Explicit short transaction for Movement, Layer/Position, idempotency, relevant coexistence state, Legacy Compatibility Writer, and required FO writeback. Reconstruction remains phased; legacy-originated synchronization uses its own idempotent batch transaction. |
| Priority | P0 |
| Dependencies | G-06, G-07, G-11 |
| Acceptance criteria | Failure injection at every persistence step causes full rollback of the consequence; retry commits once. |

### G-19 — First native consequence: DO Receipt

| Field | Detail |
|---|---|
| Current evidence | No C# write use case exists; VB6 `DM` receipt writes inbound `tb_stok`/`tb_buku`. Phase 0 FO matrix characterizes DM strongly. |
| Why it matters | Receipt is the smallest path proving new domain behavior and coexistence persistence. |
| Required capability | Idempotent DO Receipt producing Native-origin layer/movement and legacy-compatible authoritative records in one transaction; initialize scope synchronization basis/state. |
| Priority | P1 |
| Dependencies | G-02, G-04, G-06, G-07, G-11, G-12, G-13, G-15, G-18 (production enable additionally requires G-17) |
| Acceptance criteria | New receipt is visible to VB6; later VB6 transfer/consume is detected and synchronized before next new-system touch; no authority flag/cutover is created. |

### G-20 — Outbound, transfer, return, adjustment, and reversal behavior

| Field | Detail |
|---|---|
| Current evidence | Legacy handlers exist for MT, DU/DT, PK/MN, RB, RU/RT, AJ, DR/DS, RP; target handlers do not. |
| Why it matters | Domain coverage must grow transaction by transaction while remaining compatible with active VB6 paths. |
| Required capability | Capability handlers using Availability or Provenance Discovery, Freshness Gate, FIFO, Legacy Compatibility Writer, Movement/Layer updates, and idempotency. |
| Priority | P1 for the first enabled outbound/return capability; P2 for later transaction-family expansion |
| Dependencies | G-03, G-08, G-12, G-18 for ordinary outbound/transfer; G-09 is additionally required for provenance-sensitive return, correction, and reversal families. |
| Acceptance criteria | Each enabled type has post/void tests, documented legacy coexistence path, synchronization implication, rollback behavior, and quantity conservation. |

### G-21 — Virtual Stock Locations and reservation

| Field | Detail |
|---|---|
| Current evidence | DR/DS are represented through legacy transfer/sale mutation types; no target virtual-location representation is selected. |
| Why it matters | Reservation must remove quantity from ordinary availability without losing provenance. |
| Required capability | Choose a legacy-compatible virtual-location representation; implement reserve, release, consume, reversal, and synchronization mapping. |
| Priority | P2 |
| Dependencies | G-20 |
| Acceptance criteria | Reserved quantity is excluded from ordinary allocation; legacy DR/DS remain operational; transfer conservation and sync ordering hold. |

### G-22 — Transaction coverage matrix and capability routing

| Field | Detail |
|---|---|
| Current evidence | Phase 0 writer inventory / FO matrix characterization-approved for planning. Snapshot: DR/DS/DT/RT unused locally; DB/RJ unrouted; `AJX_*` present (see G-28). |
| Why it matters | A transaction is not migrated merely because Stock Ledger can represent it; legacy may still perform the same/related operation. |
| Required capability | Maintain per-FO post/void writer, new support, authoritative legacy records, discovery, synchronization, compatibility, feature flag, and rollback status. |
| Priority | P1 |
| Dependencies | G-19, G-20 |
| Acceptance criteria | Every enabled path has a defined new-writer or legacy-writer-plus-sync route; no route assumes per-DO ownership; unknown DB/RJ legacy prefixes are explicitly investigated/deferred. |

### G-23 — Coexistence test harness

| Field | Detail |
|---|---|
| Current evidence | Existing tests cover FEFO/in-memory behavior and limited legacy DALs; no mixed-writer or synchronization suite exists. |
| Why it matters | Coexistence correctness emerges from writer ordering, retry, void/delete, and intentional model differences. |
| Required capability | Domain, orchestration, SQL integration, failure-injection, and concurrency fixtures based on real VB6 behavior. |
| Priority | P0 for coexistence correctness scenarios; P1 for expanded operational, volume, and recovery suites |
| Dependencies | Ongoing across G-02–G-22 |
| Acceptance criteria | CI covers Legacy→New, New→Legacy, alternating writers, depleted-layer difference, real mismatch, duplicate sync, concurrent outbound, basis changes during reconstruction, and partial-failure rollback. |

### G-24 — Operational recovery and observability

| Field | Detail |
|---|---|
| Current evidence | No synchronization queue/state visibility, drift dashboard, or recovery procedure exists. |
| Why it matters | Operators need to distinguish pending catch-up, retryable failure, intentional difference, and material inconsistency. |
| Required capability | Metrics/logs by scope and source reference; retry/containment action; reconciliation explanation; feature flags by capability; rollback that leaves legacy stock usable. |
| Priority | P1 |
| Dependencies | G-12, G-15, G-16, G-22 |
| Acceptance criteria | Operators can identify stale/inconsistent scopes, retry safely, disable new paths without disabling VB6, and explain last synchronization/reconciliation outcome. |

### G-25 — Performance and indexing

| Field | Detail |
|---|---|
| Current evidence | Phase 0 profile: ~4.7M `tb_buku`, ~11k `tb_stok`; live indexes listed (no `(barang, do)` or `fd_tgl_jam_mutasi` index); proposed additive indexes recorded; DBA approval and SLOs still open. |
| Why it matters | Reconstruction, discovery, change detection, and diffing must be bounded on production histories. |
| Required capability | Apply measured query indexes after DBA approval; define p95 SLOs for Availability Discovery, reconstruction, freshness check, and synchronization. |
| Priority | P1 |
| Dependencies | G-05, G-13 |
| Acceptance criteria | Production-scale test meets approved SLO without organization-wide replay or long stock locks. |

### G-26 — Read models

| Field | Detail |
|---|---|
| Current evidence | Kartu-stok GET depends on an unwired repository; current read model does not expose freshness/origin. |
| Why it matters | Operational users and diagnostics need both legacy authority and richer Ledger detail without confusing source-of-truth status. |
| Required capability | Reads by Item/location/Receipt Source, movement history, origin, synchronization state, and reconciliation explanation. |
| Priority | P1 |
| Dependencies | G-06, G-07, G-16 |
| Acceptance criteria | Reads clearly label legacy authority and representation freshness; query path does not mutate or establish authority. |

### G-27 — Optional internal events and technical cleanup

| Field | Detail |
|---|---|
| Current evidence | No events are required; `ModelStateEnum` persistence tracking leaks into domain types. |
| Why it matters | Cleanup can improve architecture but must not block coexistence correctness. |
| Required capability | Internal domain signals only when useful; remove dirty-state leakage when types are already changed; no required broker/outbox. |
| Priority | P3 |
| Dependencies | Core behavior complete |
| Acceptance criteria | No event infrastructure or cleanup is on the production critical path; any refactor preserves behavior. |

### G-28 — Hidden / alternate legacy writer vocabulary

| Field | Detail |
|---|---|
| Current evidence | Phase 0 snapshot contains `AJX_MIN` / `AJX_PLUS` jenis not in current `clbGenStokX1` constants; synthetic `SYS` / `SYS-01` buku rows with blank jenis; deployed `xVoidDelete` callers not fully enumerated. |
| Why it matters | Synchronization and FO matrix completeness fail closed if unknown writers mutate authority without discovery vocabulary. |
| Required capability | Inventory and classify non-script jenis and synthetic rows; map or explicitly defer each before claiming sync completeness for adjustments or related families; feed G-13 discovery filters and G-22 matrix. |
| Priority | P1 (before AJ enablement / sync-completeness claims) |
| Dependencies | Phase 0 writer inventory; G-13, G-22 |
| Acceptance criteria | Every observed stock `fs_kd_jenis_mutasi` in the target environment is either mapped, intentionally ignored with rationale, or blocks the related capability flag; `AJX_*` owner confirmed or deferred with ops signoff. |

---

## 5. Dependency order

```text
Boundary + domain foundation
    G-01 G-02 G-03 G-04
        |
Persistence + legacy reads
    G-05 G-06 G-07 G-08 G-23
        |
Initial baseline
    G-10
        |
Change evidence + synchronization
    G-13 -> G-14 -> G-15 -> G-16
        |                    |
Mixed-writer safety         |
    G-11 G-17 G-18 ---------+
        |
Freshness gate
    G-12
        |
Native receipt and first outbound
    G-19 G-20 G-09
        |
Coverage + operations
    G-21 G-22 G-24 G-25 G-26 G-28

Deferred: G-27 and future global final cutover
```

G-13 through G-17 must be proven before coexistence production rollout. They may be prototyped in parallel with the additive domain/persistence foundation, but they cannot be deferred behind transaction expansion. G-28 must be addressed before claiming adjustment/sync vocabulary completeness.

---

## 6. Required coexistence tests

| Scenario | Required assertion |
|---|---|
| Legacy then New | VB6 changes legacy records; new touch detects and synchronizes; Stock Ledger outcome matches authoritative quantity/provenance. |
| New then Legacy | New consequence writes legacy + Ledger; VB6 later changes same Item + DO; Ledger catches up without authority transfer. |
| Alternating writers | `New -> Legacy -> Legacy -> New -> New` conserves quantity and advances synchronization position only after successful batches. |
| Depleted layer difference | Legacy deletes zero stock row; Ledger retains depleted layer; reconciliation remains balanced. |
| Real mismatch | Legacy quantity cannot be reconciled to movement/layer facts; scope becomes explicitly `Inconsistent`. |
| Duplicate synchronization | Same legacy movement/batch twice does not duplicate inbound/outbound quantity. |
| Concurrent outbound | VB6 and .NET attempt overlapping consumption; no negative stock or lost update; conflict/retry result is explicit. |
| Reconstruction race | Legacy movement during reconstruction invalidates Phase C basis and causes retry/not-current. |
| Synchronization/native race | Native consequence cannot allocate from pre-sync state; lock/version conflict retries safely. |
| Partial failure | Legacy-compatible write or Ledger persistence failure rolls back the new-system consequence. |

---

## 7. Definition of production-ready coexistence

Minimum production readiness is global capability evidence, not an `IsAuthoritative` scope flag:

1. VB6 continues to process stock.
2. New application processes enabled transaction types.
3. `tb_stok + tb_buku` remain authoritative and usable by existing consumers.
4. New transactions produce required legacy-compatible authoritative records.
5. Stock Ledger retains richer provenance, FIFO allocation, Movement, and depleted layers.
6. Later legacy-originated changes are detected and incorporated.
7. Freshness can be established before Stock Ledger-dependent decisions.
8. Synchronization and request retries do not duplicate quantity.
9. Reconciliation distinguishes intentional representation differences from real drift.
10. Negative stock remains impossible under mixed writers.
11. Legacy compatibility and FO writeback behavior are characterized and protected.
12. Disabling new Stock Ledger paths leaves legacy stock operational.
13. Operational recovery exists for stale and inconsistent scopes.
14. DO Receipt can be followed by VB6 outbound and later new-system processing of the same DO.
15. Alternating legacy/new writers conserve quantity through repeated synchronization.
16. Reconstruction and synchronization failures surface explicit `Inconsistent` outcomes.
17. Failure injection proves atomic new-system consequence persistence.
18. No operator procedure treats `Native` / `Reconstructed` / `LegacySynchronized` as authority.

Full FO migration, prevention of legacy writes, per-DO ownership, and final cutover are explicitly not prerequisites.

---

## 8. Deferred and out of scope

| Item | Reason |
|---|---|
| Global/final authority cutover | Separate future business and operational decision |
| Per-Item or per-Receipt-Source cutover | Rejected coexistence model |
| `IsAuthoritative` state | Incorrectly derives authority from origin/readiness |
| Legacy-as-projection architecture | Stage C possibility only |
| Mass historical migration | Lazy bounded reconstruction is sufficient |
| Porting `clbGenStokX1` | Anti-corruption adapters and direct orchestration are safer |
| Event sourcing / mandatory broker | Not required by domain or repository architecture |
| Mandatory CDC | Candidate only if evidence supports it |
| Batch-constrained selection | Business policy unresolved; store compatibility only |
| Dedicated Reconciliation Aggregate persistence | Lightweight behavior/outcome storage is enough for minimum rollout |
| ModelState cleanup and optional events | P3 technical improvement |
