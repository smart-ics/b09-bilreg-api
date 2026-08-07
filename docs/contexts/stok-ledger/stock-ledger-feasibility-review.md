# Stock Ledger — Implementation Feasibility Review

**Artifact status:** Implementation-oriented feasibility review
**Date basis:** Working-tree inspection of `b09-bilreg-api` (August 2026)
**Canonical business truth:** [`stok-ledger-domain.md`](./stok-ledger-domain.md)
**Legacy behavior reference:** [`clbGenStokX1.cls`](./clbGenStokX1.cls) (VB6 Transaction Script — extract behavior only; do not port)
**Companion artifacts:** [`stock-ledger-gap-analysis.md`](./stock-ledger-gap-analysis.md), [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md)

**Revision note:** This revision supersedes the rejected per-Item + Receipt Source authority-cutover model. During coexistence, `tb_stok + tb_buku` remain the authoritative persisted stock truth for every scope. `Native`, `Reconstructed`, and `LegacySynchronized` classify how Stock Ledger facts were established; they do not transfer authority or prohibit later VB6 activity.

---

## 0. Executive verdict

| Question | Feasibility conclusion |
|---|---|
| Is the target domain realizable? | **Conditionally yes.** The additive movement/layer model, lazy reconstruction, retained depleted layers, and legacy-compatible writes fit the repository architecture. Safe coexistence also requires incremental Legacy Synchronization and mixed-writer concurrency controls that are not implemented today. |
| What remains authoritative during coexistence? | **`tb_stok + tb_buku`.** Stock Ledger is a richer representation, not the persisted source of truth for any reconstructed or natively processed scope. |
| Does reconstruction establish authority? | **No.** It establishes a baseline for Item + Receipt Source across all Stock Locations. Later VB6 movements remain permitted and can make that baseline stale. |
| Is incremental synchronization already technically proven? | **No.** `tb_buku` rows can be deleted by legacy void behavior, `tb_stok` is updated/deleted in place, and no repository evidence proves a complete monotonic legacy cursor. Detection mechanics are a P0 feasibility decision. |
| What is the safest first technical slice? | **DO Receipt** remains a good first native consequence because Receipt Source is known and no allocation is required. It must write legacy-compatible authoritative records and a Stock Ledger representation in one short transaction. |
| Must all outbound flows migrate before DO Receipt can run? | **No.** Later VB6 consumption is allowed. The production gate is synchronization and mixed-writer correctness, not per-scope ownership. |
| What is the minimum production outcome? | Both applications can operate while legacy records remain usable and authoritative, Stock Ledger detects and incorporates legacy-originated changes idempotently, reconciliation distinguishes real drift from intentional representational differences, and negative stock remains impossible. |

The target is feasible as an incremental modernization, but production feasibility is not established until the legacy-change detection and mixed-writer locking questions in §3.4 and §6 are resolved with database evidence and concurrency tests.

---

## 0.1 Accepted implementation decisions

| ID | Decision |
|---|---|
| D1 | **Runtime authority is global during coexistence.** `tb_stok + tb_buku` remain the persisted source of truth. There is no per-Item + Receipt Source authority transfer. |
| D2 | **Origin is not authority.** `Native` means recorded directly under Stock Ledger rules; `Reconstructed` means established during initial Legacy Stock Reconstruction; `LegacySynchronized` means recorded in Stock Ledger from a legacy-originated transaction that occurred after the baseline. These labels are provenance classifications only. |
| D3 | **Separate four boundaries.** Reconstruction and reconciliation use Item + Receipt Source across all Stock Locations. A write consistency boundary may be Item + Receipt Source + Stock Location. Database locking may be narrower still. None is an authority boundary. |
| D4 | **Separate discovery responsibilities.** Availability Discovery finds currently available Receipt Sources; Provenance Discovery finds the original Receipt Source/layer for returns, reversals, or corrections. |
| D5 | **Freshness replaces authority gating.** Before Stock Ledger relies on layers, it must establish that applicable legacy changes through a known Synchronization Position are incorporated, or treat the scope as not current. |
| D6 | **Reconstruction and synchronization are different capabilities.** Reconstruction establishes the initial baseline. Incremental Legacy Synchronization incorporates later legacy-originated movements without repeating full reconstruction when the baseline remains valid. |
| D7 | **New-system consequences preserve legacy authority.** A new stock transaction applies Stock Ledger semantics and writes the required `tb_stok` / `tb_buku` shapes as a Legacy-Compatible Stock Consequence. |
| D8 | **One short database transaction per new-system consequence.** Stock Ledger Movement/Layer changes and the Legacy-Compatible Stock Consequence commit together when they share the current SQL Server database. Reconstruction remains phased to avoid long write locks. |
| D9 | **Preserve legacy persistence compatibility.** The legacy representation may delete zero-balance `tb_stok` rows or remove/modify legacy history as existing callers require, while Stock Ledger retains depleted layers and accountable correction/reversal history. |
| D10 | **DO Receipt remains the first technical slice.** Migrating all outbound paths is not a prerequisite. Production use requires proven freshness, synchronization, and mixed-writer safety for later legacy activity. |
| D11 | **No mandatory broker, event sourcing, or CDC.** Change detection is an explicit technical decision to be proven from live schema/data and operational behavior. |
| D12 | **Final cutover is deferred.** A future state in which Stock Ledger becomes source of truth and legacy tables become a compatibility projection is outside this plan. |

---

## 0.2 Coexistence stages

### Stage A — Legacy Authority

```text
Legacy VB6
    -> tb_stok + tb_buku
         = authoritative persisted stock truth

Stock Ledger Representation
    = absent for some scopes
```

### Stage B — Coexistence (current implementation target)

```text
Legacy VB6
    -> tb_stok + tb_buku

New Application
    -> Stock Ledger Domain
         +-> Legacy-Compatible Stock Consequence
         |     -> tb_stok + tb_buku
         +-> Stock Ledger Representation

tb_stok + tb_buku
    = authoritative persisted stock truth
```

Legacy transactions may continue after a scope is reconstructed or processed natively. Their effects are incorporated through Legacy Synchronization before Stock Ledger relies on its representation again.

### Stage C — Future final cutover

```text
Stock Ledger = source of truth
Legacy records = compatibility projection
```

Stage C is a possible future decision only. This review defines no cutover phase, criteria, or decommissioning commitment.

---

## 0.3 State and classification model

Do not create `IsAuthoritative`, `Stock Ledger Authority Established`, or an ownership status per Item + Receipt Source.

| Dimension | Values | Meaning |
|---|---|---|
| Reconstruction Status | `NotReconstructed`, `ReconstructionRequired`, `Reconstructing`, `Reconstructed`, `Inconsistent` | Whether an initial Item + Receipt Source baseline exists across all locations |
| Synchronization State | `Current`, `LegacyChangePending`, `SynchronizationRequired`, `Inconsistent` | Whether the Stock Ledger Representation reflects applicable legacy facts through its Synchronization Position |
| Fact/Layer Origin | `Native`, `Reconstructed`, `LegacySynchronized` | How an individual Stock Ledger fact was established. For an existing layer, its establishment origin remains unchanged when later synchronization movements modify its quantity. |
| Runtime Authority | `LegacyStockRecord` during Stage B | Global coexistence decision, not scope state |

Domain lifecycle labels use spaces (for example, `Not Reconstructed` and `Legacy Change Pending`). PascalCase forms in this review denote candidate persisted/code values only; final names must follow repository conventions without changing the domain meaning.

`Native` does not mean “created after cutover.” A Native fact may coexist with later legacy-originated changes to the same Receipt Source. A later legacy-originated movement is recorded with `LegacySynchronized` origin; if it only changes an existing layer, that layer retains the origin with which it was originally established.

---

## 1. Repository evidence snapshot

### 1.1 Existing assets

| Area | Repository evidence | Current implication |
|---|---|---|
| Domain spike | `src/bilreg/Bilreg.Domain/InventoryContext/StokFeature/` | `StokModel`, layers, nested buku facts, and FEFO-like allocation exist in memory; they are not a complete Stock Ledger implementation. |
| Application/API | `IStokRepo`, read-only kartu-stok query, `StokController` GET | No evidenced new-system stock consequence command. |
| Legacy persistence | `tb_stok_dal.cs`, `tb_buku_dal.cs`, DTOs | Explicit SQL/Dapper-compatible adapters exist, but do not provide complete reconstruction/synchronization queries. |
| Legacy schemas | `tb_stok.sql`, `tb_buku.sql` | Legacy current-stock and journal records remain the coexistence authority. Both use string transaction IDs; no `rowversion` is present. |
| FARIN schemas | `FARIN_Stok.sql`, `FARIN_StokLayer.sql`, `FARIN_StokBuku.sql` | Additive schema concepts exist, but the persistence stack is absent/unwired in the working tree. |
| Tests | `StokModelTest.cs`; legacy DAL tests; commented `StokRepoTest.cs` | Current tests protect FEFO and limited DAL behavior, not target FIFO, synchronization, or mixed-writer safety. |
| Legacy behavior | `clbGenStokX1.cls` | Current operational behavior reference: 13 routed FO families with post/void paths. |

### 1.2 Current implementation completeness

The current C# direction remains a location-scoped kartu-stok spike:

```text
StokModel (Item + Location)
    -> StokLayerModel[]
         -> Stock lot metadata
         -> nested StokBukuType[]
```

Missing production capabilities include immutable movement persistence, Item + Receipt Source reconstruction, synchronization state/position, Legacy Freshness Gate, Availability Discovery, Provenance Discovery, legacy-compatible consequence orchestration, idempotency, mixed-writer concurrency, and reconciliation.

### 1.3 Verified legacy mutation characteristics

| Legacy record | Verified behavior | Synchronization consequence |
|---|---|---|
| `tb_buku` | Inserted for movements; legacy void paths can physically delete rows. No update path was found in the current DAL. | A simple “read rows after cursor” strategy cannot detect all voids/deletions. |
| `tb_stok` | Inserted for inbound, updated for partial outbound, and deleted at depletion. | Snapshot comparison is required for current quantity; row absence may be valid depletion. |
| `fs_kd_trs` | Unique string PK generated through legacy counters. | Useful identity for surviving rows, but not a complete change cursor because deleted rows disappear. |
| `fd_tgl_jam_mutasi` | Present as `VARCHAR(19)` with a default value. The visible VB6 `AddStok` path sets separate date/time fields but does not prove this combined column is populated. | A timestamp + ID cursor is only a candidate after production-data validation. |
| `fd_tgl_mutasi`, `fs_jam_mutasi` | Used by VB6 FIFO ordering. | Business ordering evidence, not proven insertion order; backdating/ties remain possible. |

No repository evidence proves that the current schema exposes a monotonic, deletion-aware movement sequence.

---

## 2. Domain-to-implementation fit

### 2.1 Responsibility and authority

Stock Ledger owns target semantics for movement, layers, provenance, FIFO, virtual locations, reconstruction, synchronization, and reconciliation. This DDD responsibility does not make its persistence the runtime source of truth during coexistence.

| Concern | Scope/boundary | Runtime meaning |
|---|---|---|
| Reconstruction | Item + Receipt Source, all Stock Locations | Establish initial Stock Ledger baseline |
| Reconciliation | Item + Receipt Source, all Stock Locations | Compare conservation and legacy-vs-ledger position |
| Logical Stock Position | Item + Receipt Source across locations | Domain view of provenance and layers |
| Write consistency | Candidate: Item + Receipt Source + Stock Location | Protect local Remaining Quantity invariants |
| Database locking | Smallest rows/ranges that prevent overlapping mutation | Technical concurrency mechanism |
| Source-of-truth authority | Global Stage B decision | `tb_stok + tb_buku`; never inferred from the scopes above |

The candidate write boundary remains technically plausible, but repository concurrency tests must prove it under mixed VB6/.NET writers.

### 2.2 Availability and provenance discovery

| | Availability Discovery | Provenance Discovery |
|---|---|---|
| Purpose | Find Receipt Sources with authoritative available quantity for outbound stock | Recover the original Receipt Source/layer for return, reversal, correction, or investigation |
| Authoritative coexistence input | `tb_stok` current stock, interpreted with applicable `tb_buku` history | Source transaction detail, legacy writeback fields, `tb_buku`, and Stock Ledger movements |
| Stock Ledger contribution | Current synchronized layers, FIFO ordering, multi-layer allocation | Rich movement/layer traceability and retained depleted layers |
| Failure | Insufficient authoritative stock or stale representation | Provenance unknown/ambiguous |

Availability Discovery must consult or validate against Legacy Stock Authority. Stock Ledger layers alone cannot be trusted merely because reconstruction once completed.

### 2.3 Initial reconstruction

Reconstruction remains scoped to Item + Receipt Source across all Stock Locations:

```text
Phase A (short TX)
    acquire reconstruction work
    -> ReconstructionStatus = Reconstructing
    -> commit

Phase B (outside long write lock)
    read applicable tb_buku + tb_stok
    -> calculate baseline and retained depleted layers

Phase C (short TX)
    revalidate reconstruction claim and legacy basis
    -> persist baseline
    -> initialize Synchronization Position
    -> Reconstructed | Inconsistent
    -> commit
```

Phase C must prove that the legacy basis did not change during calculation. If it changed, retry from a new basis or mark synchronization/reconstruction pending; do not persist a knowingly stale baseline.

Successful reconstruction means:

```text
balanced baseline exists
!= authority transferred
!= later legacy writes prohibited
!= permanently synchronized
```

### 2.4 Incremental Legacy Synchronization

After reconstruction:

```text
Legacy transaction
    -> tb_stok / tb_buku change
    -> Stock Ledger Representation may be stale
    -> detect change after Synchronization Position
    -> incorporate legacy consequence idempotently
    -> compare resulting position with legacy authority
         -> Current
         -> Inconsistent
```

The required capability is incremental in business meaning: incorporate only changes after the established baseline when those changes can be identified reliably. Full reconstruction on every touch is not the target design.

Required planning responsibilities:

1. detect new, changed, and removed applicable legacy facts;
2. capture a stable source identity or deterministic fingerprint for deduplication;
3. apply each legacy consequence at most once;
4. advance Synchronization Position only after successful application and reconciliation;
5. retry without duplicating quantity;
6. classify explainable representation differences separately from material drift;
7. mark the scope `Inconsistent` when available legacy facts cannot produce a valid layer outcome.

### 2.5 Legacy Freshness Gate

```text
New Stock Request
    -> identify affected Item + Receipt Source scopes
    -> determine whether legacy authority changed after Synchronization Position
         -> no change: use current Stock Ledger Representation
         -> changed: synchronize incremental legacy facts
                     -> validate position
                     -> continue only if Current
         -> cannot establish freshness: fail closed / mark Inconsistent
```

This gate protects allocation quality. It does not block VB6 or transfer source-of-truth authority.

### 2.6 New-system consequence

For a new transaction during coexistence:

```text
Source Business Fact
    -> Freshness Gate for affected existing scopes
    -> apply Stock Ledger domain rules
    -> write Legacy-Compatible Stock Consequence
    -> write Stock Ledger Movement/Layer representation
    -> commit one short transaction
```

The exact persistence order is an application/SQL design detail. The invariant is that a successful new-system result must not leave legacy authority without its required consequence or leave Stock Ledger claiming a movement that the authoritative legacy records do not contain.

---

## 3. Synchronization feasibility

### 3.1 Candidate mechanisms

| Option | Repository fit | Strength | Limitation / proof required |
|---|---|---|---|
| Composite legacy watermark, plus deletion detection | Candidate only | Bounded incremental reads if timestamp/ID quality is proven | `fd_tgl_jam_mutasi` population is unproven; void deletes are invisible to append polling |
| Per-scope deterministic history fingerprint plus bounded delta/replay | Compatible with explicit SQL and lazy scope | Detects any scoped change, including deletion; avoids organization-wide replay | Must define efficient diff identity and indexes; may reread a bounded Item + Receipt Source history |
| Additive legacy change log written by both stock writer paths | Strong target option | Explicit ordering, operation type, tombstones, and idempotency source | Requires a compatible change to every active writer; not currently present |
| SQL Server Change Tracking/CDC | Possible infrastructure option | Captures row changes/deletes without rewriting domain behavior | Availability, operations ownership, retention, and deployment support are not evidenced; not required by this plan |
| Reconstruct fully on every request | Technically possible fallback | Avoids cursor assumptions | Reject as the planned steady state unless measured history size and safety evidence prove it is the minimum viable mechanism |

The implementation roadmap must close the evidence gap before selecting one. A hybrid can use a lightweight change indicator to trigger bounded per-scope diffing without treating the indicator as the source of business meaning.

### 3.2 Duplicate avoidance and retry

Synchronization needs a durable processed-source identity. A candidate may combine legacy transaction identity, movement type, Item, Receipt Source, location, direction, and quantity, but the final key must be validated against actual void/repost behavior.

Rules:

* never advance Synchronization Position before all selected legacy changes are persisted;
* repeat of the same synchronization batch must be quantity-neutral;
* a deleted legacy journal fact must result in an accountable Stock Ledger correction/reversal interpretation, not silent erasure;
* a changed `tb_stok` snapshot without explainable movement evidence produces `Inconsistent`, not an invented movement;
* retry may resume from the last committed position or replay an idempotent bounded batch.

### 3.3 Reconciliation semantics

Reconciliation compares material quantity/provenance outcomes while recognizing model differences:

| Difference | Classification |
|---|---|
| Legacy deletes a zero-balance `tb_stok` row; Stock Ledger retains a depleted layer | Intentional representation difference; not a quantity mismatch |
| Legacy void removes/modifies its historical row; Stock Ledger retains original plus correction/reversal | Intentional historical representation difference if net authoritative quantity and traceability reconcile |
| Legacy quantity differs from the sum of current synchronized Stock Ledger layers | Material mismatch unless an identified synchronization batch is pending |
| Legacy cannot identify original layer but quantity and Receipt Source can be reconstructed deterministically | Reconstructed provenance limitation, explicitly classified |
| Quantity by Receipt Source cannot be derived consistently | `Inconsistent`; block Stock Ledger-dependent allocation for the scope |

### 3.4 Unresolved feasibility questions

**Phase 0 update (2026-08-07):** Resolutions and interims are recorded in [`stock-ledger-phase-0-implementation-report.md`](./stock-ledger-phase-0-implementation-report.md). Summary: FQ-01/04/05 resolved; FQ-02 limited; FQ-03/07 interim; FQ-06 unresolved with concurrency ADR interim. Sync ADR selects fingerprint + bounded replay.

| ID | Question | Why it blocks production proof |
|---|---|---|
| FQ-01 | Is `fd_tgl_jam_mutasi` populated and ordered consistently in the live database? | Determines whether a composite watermark is usable. |
| FQ-02 | Are legacy IDs monotonic and transactionally allocated across all VB6 instances? | Determines whether IDs can assist ordering/deduplication. |
| FQ-03 | Which callers use `xVoidDelete`, and do deployed writers update/delete `tb_buku` differently from repository evidence? | Determines deletion/tombstone requirements. |
| FQ-04 | What indexes and triggers exist in production beyond repository SQL? | Determines query cost and hidden write behavior. |
| FQ-05 | What are the row volumes and hottest Item + Receipt Source histories? | Determines whether bounded diff/replay meets latency targets. |
| FQ-06 | Are VB6 read-modify-write stock operations enclosed in SQL transactions, and which isolation/lock behavior do they use? | Determines the minimum shared mixed-writer lock protocol. |
| FQ-07 | Can all active legacy writer entry points emit an additive change record if no safe existing cursor exists? | Determines whether a change-log option is operationally possible. |

---

## 4. Legacy compatibility assessment

### 4.1 Classification

| Classification | Meaning |
|---|---|
| Preserve in legacy | Keep existing `tb_stok` / `tb_buku` behavior required by legacy callers |
| Replace semantically in Stock Ledger | Represent the same business outcome using immutable movements, retained layers, or richer provenance |
| Enrich | Add Stock Ledger-only detail without requiring the legacy schema to carry it |
| Defer | Outside the minimum coexistence release |

### 4.2 Behavior catalog

| Legacy behavior | Legacy representation during coexistence | Stock Ledger representation |
|---|---|---|
| ED filter then FIFO by mutation/receipt order | Preserve compatible selection/output fields | Explicit Expiration Date filter, then FIFO by Effective Receipt Time and deterministic Layer ID |
| Batch stored but not used as selection key | Preserve storage compatibility | Do not add Batch to initial Stock Consequence Request; policy remains deferred |
| Delete `tb_stok` row at zero | **Preserve** | Retain Depleted Stock Layer |
| Legacy void may delete `tb_buku` | Preserve where existing callers require it | Retain original movement and add accountable reversal/correction |
| Outbound void restores by inserting a new legacy stock row | Preserve | Restore original Stock Layer when provenance is known |
| `tb_buku` movement rows | Preserve authoritative legacy history shape | Enrich with immutable Movement header/lines and source idempotency |
| FO writeback of PO/DO/HPP | Preserve through compatibility adapter | Keep outside Stock Ledger core semantics while retaining source references |
| Transfer OUT/IN pair | Preserve legacy-compatible pair | Coordinated transfer movement preserving Receipt Source and valuation |
| General sales return uses original sale provenance | Preserve | Provenance Discovery and original-layer restoration |
| Typed return may synthesize return ID as DO | Preserve only if required by legacy compatibility | Do not invent Receipt Source; use Provenance Discovery or accountable unknown-provenance fallback |
| Adjustment PLUS/MINUS | Preserve required legacy consequence | Record only after source adjustment authority is established; Stock Opname remains observation |
| Reservation/serah encoded through legacy mutation types | Preserve when those paths coexist | Model as transfer to/from a Virtual Stock Location when implemented |
| Repack consumes inputs and establishes output | Preserve legacy stock/HPP writebacks | Model traceable transformation consequences in a later transaction slice |

The previous `Remove` classification was misleading where legacy behavior still has to operate. “Preserve in legacy + replace semantically in Stock Ledger” is the normal coexistence rule.

---

## 5. DO Receipt feasibility and transaction coverage

DO Receipt is still the lowest-complexity native consequence:

* Receipt Source is the DO identity;
* no Availability Discovery or outbound FIFO is required;
* one receiving location is involved;
* the new path can prove Movement, Layer, idempotency, Legacy-Compatible Stock Consequence, and transaction rollback.

Required coexistence scenario:

```text
New system receives DO-010
    -> writes authoritative tb_stok / tb_buku consequence
    -> writes Native Stock Ledger facts

VB6 later consumes or transfers DO-010
    -> authoritative legacy records change
    -> Stock Ledger becomes LegacyChangePending

New system later touches DO-010
    -> detects change
    -> synchronizes it idempotently
    -> validates current position
    -> continues
```

A migrated outbound flow remains valuable for exercising FIFO and concurrency, but it is not required to prevent an authority conflict. The critical pilot prerequisite is a proven synchronization/concurrency path for any legacy outbound activity that can follow the new receipt.

---

## 6. Mixed-writer concurrency

### 6.1 Races to protect

| Race | Required outcome |
|---|---|
| New app reads stock, then VB6 changes it before new commit | New app revalidates authoritative rows/basis and retries or rejects; it must not commit stale allocation. |
| VB6 changes a reconstructed or Native-origin Receipt Source | Change remains valid legacy activity; Stock Ledger is marked/detected stale and synchronized. |
| VB6 and new app consume overlapping quantity | Database coordination prevents both from successfully consuming the same units; negative stock is impossible. |
| Reconstruction reads while legacy movement commits | Phase C detects basis change and retries or leaves scope not current. |
| Synchronization and a native transaction overlap | Serialize on the affected write boundary or use version/basis validation so the native transaction sees synchronized facts. |
| FIFO allocation becomes stale before persistence | Re-read/revalidate eligible authoritative stock inside the consequence transaction. |
| Legacy write succeeds but Stock Ledger write fails on the new path | One shared transaction rolls back the new-system consequence; if infrastructure cannot guarantee this, do not enable that path. |

### 6.2 Minimum safe model

Repository evidence shows SQL Server, explicit SQL, ambient/unit-of-work transaction helpers, and use of `UPDLOCK, HOLDLOCK` elsewhere. It does not prove a shared lock protocol in VB6 stock code.

The minimum target is:

1. short SQL transactions;
2. deterministic lock order by Item, Receipt Source, and Stock Location;
3. update/range locks or equivalent conditional writes on the actual authoritative legacy stock rows selected for allocation;
4. revalidation of quantity and synchronization basis before commit;
5. uniqueness for source-consequence idempotency;
6. optimistic versioning on Stock Ledger positions;
7. retry on deadlock/version conflict without duplicate movement;
8. fail closed when a scope cannot be made current.

This is a target, not proven current behavior. Row locks used only by .NET may not prevent a stale VB6 read-modify-write from overwriting a later quantity. Phase 0 must characterize deployed VB6 transaction/isolation behavior and prove either:

* existing row/range locking is sufficient under both writers; or
* a minimal shared database locking/conditional-update protocol is required at the legacy compatibility boundary.

Disabling VB6 for reconstructed scopes is not an accepted mitigation.

---

## 7. Architecture responsibilities

| Responsibility | Target port/adapter concept |
|---|---|
| Read authoritative current availability | Legacy Stock Authority read adapter |
| Read reconstruction history by Item + Receipt Source across locations | Legacy reconstruction read adapter |
| Detect legacy changes after a position | Legacy change discovery adapter |
| Persist reconstruction and synchronization state/position | Stock Ledger coexistence state store |
| Apply legacy-originated changes idempotently | Legacy Synchronization use case |
| Write new-system authoritative legacy shapes | Legacy Compatibility Writer |
| Persist layers and immutable movements | Stock Ledger repositories |
| Guard freshness before allocation | Legacy Freshness Gate in Application orchestration |
| Compare representations | Reconciliation service with intentional-difference classification |

“Legacy Projection Writer” is reserved for a possible post-cutover Stage C. It is not the correct Stage B responsibility name.

---

## 8. Risks

| ID | Risk | Severity | Required control |
|---|---|---|---|
| R1 | Treating Native/Reconstructed/LegacySynchronized origin as authority | Critical | State model in §0.3; no `IsAuthoritative` |
| R2 | Stock Ledger allocation from stale layers after VB6 activity | Critical | Freshness Gate + synchronization |
| R3 | Append-only cursor misses legacy void deletion | Critical | Deletion-aware discovery, bounded diff, change log, or validated CDC/CT |
| R4 | Two applications consume overlapping quantity | Critical | Shared database concurrency protocol and stress tests |
| R5 | Partial new-system write leaves legacy and ledger split | Critical | One consequence transaction; failure injection |
| R6 | Reconstruction completes from a moving legacy basis | Critical | Phase C basis revalidation |
| R7 | Duplicate synchronization repeats quantity | Critical | Durable source identity/fingerprint + uniqueness |
| R8 | Intentional depleted-layer difference reported as drift | High | Difference classification |
| R9 | Full reconstruction used on every request | High | Incremental mechanism decision and performance SLO |
| R10 | Provisional Availability Discovery used as final FIFO | High | Synchronize/reconstruct, then reload and allocate |
| R11 | Legacy persistence semantics changed to fit Stock Ledger | High | Characterization tests + compatibility adapter |
| R12 | Cursor assumed from unvalidated timestamp/defaults | High | Production data audit before schema decision |
| R13 | FO writeback fields omitted | High | Transaction coverage and legacy consumer tests |
| R14 | Porting VB6 control flow into the new domain | Critical | Anti-corruption adapters; direct application orchestration |
| R15 | Final cutover work pulled into coexistence delivery | Medium | Explicitly deferred Stage C |

---

## 9. Production feasibility criteria

The minimum coexistence release is feasible only when evidence demonstrates:

1. VB6 may continue processing stock for reconstructed and Native-origin scopes.
2. New application transactions produce required authoritative `tb_stok` / `tb_buku` records.
3. Stock Ledger records richer Movement, Layer, provenance, allocation, and retained-depletion facts.
4. Legacy-originated changes after reconstruction are detected and incorporated.
5. Stock Ledger can establish freshness before relying on its representation.
6. Repeated synchronization does not duplicate quantity.
7. Reconstruction and synchronization failures become explicit `Inconsistent` outcomes.
8. Reconciliation detects material drift but ignores declared representational differences.
9. Overlapping VB6/.NET outbound attempts cannot create negative stock or lost updates.
10. Legacy consumers and FO writeback expectations remain operational.
11. Disabling new Stock Ledger processing leaves authoritative legacy stock usable.
12. DO Receipt can be followed by VB6 outbound and later new-system processing of the same DO.
13. Alternating legacy/new writers conserve quantity through repeated synchronization.

Full transaction migration and final cutover are not production-readiness prerequisites for Stage B.

---

## 10. Feasibility conclusion

The corrected coexistence design is **feasible in architecture and domain shape, but technically conditional**:

* keep Item + Receipt Source as reconstruction/reconciliation scope, not authority scope;
* retain the candidate Item + Receipt Source + Location write boundary pending concurrency proof;
* preserve phased reconstruction and initialize a synchronization basis at completion;
* add Legacy Synchronization and a Legacy Freshness Gate before trusted allocation;
* use `Native` / `Reconstructed` / `LegacySynchronized` only as fact origin;
* write legacy-compatible authoritative records for every new-system consequence;
* preserve legacy delete/void/writeback behavior while enriching Stock Ledger history;
* prove deletion-aware change discovery and a mixed-writer concurrency protocol before rollout;
* defer final cutover.

The principal remaining unknown is not domain feasibility; it is whether the deployed legacy schema/data and VB6 transaction behavior provide a safe incremental change signal and shared locking behavior. The roadmap treats that as an explicit Phase 0 evidence gate rather than inventing a cursor or assuming VB6 can be disabled.
