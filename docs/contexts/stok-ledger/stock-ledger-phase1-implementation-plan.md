# Stock Ledger — Phase 1 Implementation Plan

**Artifact status:** Executable Phase-1 plan  
**Date:** 2026-08-07  
**Phase:** 1 — Additive Stock Ledger foundation  
**Governing baseline (LOCKED):** Phase 0 Exit Review **PASS WITH RISKS** — [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md)  
**Roadmap:** [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md) § Phase 1  
**Gaps in scope:** G-01–G-07, G-18 foundation, continuous G-23 test **scaffolding**  
**Standards:** [`docs/ENGINEERING.md`](../../ENGINEERING.md), [`docs/DATABASE.md`](../../DATABASE.md), [`docs/NAMING.md`](../../NAMING.md)

### Slice progress

| Slice | Status | Summary |
|---|---|---|
| P1-S1 | **COMPLETE** | [`stock-ledger-phase1-s1-implementation-summary.md`](./stock-ledger-phase1-s1-implementation-summary.md) |
| P1-S2 | **COMPLETE** | [`stock-ledger-phase1-s2-implementation-summary.md`](./stock-ledger-phase1-s2-implementation-summary.md) |
| P1-S3 | **COMPLETE** | [`stock-ledger-phase1-s3-implementation-summary.md`](./stock-ledger-phase1-s3-implementation-summary.md) |
| P1-S4 | **COMPLETE** | [`stock-ledger-phase1-s4-implementation-summary.md`](./stock-ledger-phase1-s4-implementation-summary.md) |
| P1-S5 | Not started | |
| P1-S6 | Not started | |
| P1-S7 | Not started | |
| P1-S8 | Not started | |

---

## 1. Purpose

Convert the Phase 1 roadmap into **small, independently executable slices** that a mid-level coding agent can implement with minimal ambiguity.

Phase 1 establishes additive domain objects, persistence, idempotency keys, coexistence **state shapes**, application **ports**, and a consequence UoW **skeleton** — without changing Legacy Stock Authority and without implementing reconstruction, synchronization, Freshness Gate behavior, or FO transactions.

---

## 2. Locked decisions (do not reopen)

| Topic | Locked baseline |
|---|---|
| Runtime authority (Stage B) | `tb_stok` + `tb_buku` |
| Origin labels | `Native` / `Reconstructed` / `LegacySynchronized` — origin only; **not** authority |
| Reconstruction / reconciliation scope | Item + Receipt Source across **all** Stock Locations |
| Write consistency candidate | Item + Receipt Source + Stock Location |
| Lock order (interim) | Item → Receipt Source → Location → legacy row id |
| Sync mechanism (selected; not implemented here) | Fingerprint + bounded replay; mismatch ⇒ set-diff and/or scoped re-derive ([sync ADR](./adr/ADR-stock-ledger-legacy-change-discovery.md)) |
| Synchronization Position (Phase 1 shape) | **Mechanism-neutral** opaque value + algorithm version — **not** `(fd_tgl_jam_mutasi, fs_kd_trs)` |
| Allocation | Explicit ED filter (optional) then **FIFO** by Effective Receipt Time, then Layer ID — **not** FEFO |

---

## 3. Explicit exclusions (later phases)

Do **not** implement in any Phase 1 slice:

- Reconstruction use case / Phase A–C persistence workflow (Phase 2 / G-10)
- Legacy Change Discovery algorithm, fingerprint computation against live `tb_buku`, Freshness Gate behavior (Phase 3 / G-12–G-15)
- DO Receipt or any FO post/void (Phase 4+)
- Availability Discovery or Provenance Discovery **behavior** (ports only)
- Outbound / transfer / reservation / returns
- Mixed-writer production enablement (G-17)
- Restoring or wiring empty `FARIN_Stok*` as authority or sync feed
- Capability flags that enable production stock consequences
- Rewriting VB6 or mutating `HOSPITAL_HPL`

---

## 4. Target module layout

Introduce a **new** feature folder so the legacy FEFO `StokFeature` spike remains untouched until G-27 cleanup:

| Layer | Path |
|---|---|
| Domain | `Bilreg.Domain/InventoryContext/StockLedgerFeature/` |
| Application | `Bilreg.Application/InventoryContext/StockLedgerFeature/` (+ `Ports/`, `UseCases/` as needed) |
| Infrastructure | `Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/` |
| SqlDb | `Bilreg.SqlDb/InventoryContext/StockLedgerFeature/` |
| Test | `Bilreg.Test/InventoryContext/StockLedgerFeature/` |
| Api | **No new public write endpoints in Phase 1.** Optional internal/diagnostic read deferred to G-26. |

**Do not** implement `IStokRepo` for the old kartu-stok spike as part of Phase 1 Stock Ledger. Leave `StokFeature` as-is.

### Naming (follow `docs/NAMING.md`)

| Concept | Suggested type name |
|---|---|
| Movement aggregate | `StockMovementModel` |
| Movement line | `StockMovementLineType` |
| Layer | `StockLayerModel` |
| Position (write boundary) | `StockPositionModel` |
| Scope coexistence state | `StockLedgerScopeStateModel` (Item + Receipt Source) |
| Origin enum | `StockFactOriginEnum` |
| Reconstruction status | `ReconstructionStatusEnum` |
| Synchronization state | `SynchronizationStateEnum` |
| Repo | `IStockMovementRepo`, `IStockPositionRepo`, `IStockLedgerScopeStateRepo`, … |

PascalCase enum **members** map to domain lifecycle labels without changing meaning (feasibility note on spaces vs code values).

### Additive tables (suggested; finalize in Slice 5)

Prefix **`BILRG_`** per `docs/DATABASE.md`. Illustrative names:

| Table | Purpose |
|---|---|
| `BILRG_StokMovement` | Immutable movement header |
| `BILRG_StokMovementLine` | Movement lines |
| `BILRG_StokLayer` | Layers including depleted (`RemainingQty = 0` retained) |
| `BILRG_StokPosition` | Current position at Item + Receipt Source + Location + optimistic version |
| `BILRG_StokLedgerScope` | Reconstruction status, sync state, opaque Synchronization Position, algorithm version |
| `BILRG_StokSourceIdempotency` | Unique source consequence / sync batch keys |

No FK to legacy `tb_stok` / `tb_buku`. No triggers encoding business rules.

---

## 5. Slice map (execute in order)

```text
P1-S1 Scaffolding + enums/value objects (G-01, G-04 types)
   -> P1-S2 Movement domain (G-02)
   -> P1-S3 Layer/Position + ED+FIFO (G-03)
   -> P1-S4 Scope coexistence state + opaque sync position (G-04)
   -> P1-S5 Additive SQL scripts (G-06/G-07 schema)
   -> P1-S6 DAL/DTO/Repo round-trips (G-06, G-07)
   -> P1-S7 Application ports only (G-05/08/09/11/13/16 contracts)
   -> P1-S8 Consequence UoW skeleton + failure rollback (G-18 foundation)
```

Each slice must leave the solution **compiling**. Prefer disposable/test DB (`DEVTEST`) for persistence slices — never `HOSPITAL_HPL` writes.

---

## 6. Slice specifications

### P1-S1 — Context scaffolding and boundary types

**Progress:** COMPLETE (2026-08-07) — see [`stock-ledger-phase1-s1-implementation-summary.md`](./stock-ledger-phase1-s1-implementation-summary.md).

| Field | Detail |
|---|---|
| **Objective** | Create `StockLedgerFeature` folders and core value types/enums that encode locked boundaries without behavior yet. |
| **Scope** | G-01 documentation-in-code: keys for Item, Receipt Source, Stock Location; enums for origin, reconstruction status, synchronization state. `.gitKeep` / csproj folder entries if required by repo convention. |
| **Dependencies** | Phase 0 freeze; none on other P1 slices |
| **Affected projects** | Domain, Application, Infrastructure, SqlDb, Test (folders); optionally csproj `<Folder>` entries |
| **Affected folders/classes** | `StockLedgerFeature/`; e.g. `ItemId`/`ReceiptSourceId`/`StockLocationId` types or reuse existing inventory ids if already present; `StockFactOriginEnum`, `ReconstructionStatusEnum`, `SynchronizationStateEnum`; `IStockLedgerScopeKey` |
| **Database** | None |
| **Tests** | Unit: enum values cover domain set; key equality for Item+Receipt Source vs Item+Receipt Source+Location |
| **Acceptance** | Solution builds; no `IsAuthoritative` field; no SQL; old `StokFeature` unchanged |
| **Rollback** | Delete new folders |
| **Risks** | Accidentally placing types under `StokFeature` and inheriting FEFO assumptions |
| **Deliverables** | Folders + enums/keys + tests |

---

### P1-S2 — Immutable Stock Movement domain

**Progress:** COMPLETE (2026-08-07) — see [`stock-ledger-phase1-s2-implementation-summary.md`](./stock-ledger-phase1-s2-implementation-summary.md).

| Field | Detail |
|---|---|
| **Objective** | Implement Movement + Lines as immutable completed facts with reversal/correction references and transfer conservation rules in-memory. |
| **Scope** | G-02 only (domain). Movement kinds needed for foundation: Receipt, Outbound, Transfer, Correction, Reversal (enough for later sync/native). No persistence. |
| **Dependencies** | P1-S1 |
| **Affected projects** | Domain, Test |
| **Affected classes** | `StockMovementModel`, `StockMovementLineType`, factory/create methods, `Reverse()` / `Correct()` returning **new** movements (no mutate-in-place) |
| **Database** | None |
| **Tests** | Domain invariant: completed movement immutable; reversal references original; transfer OUT+IN quantity conservation; reject negative line qty |
| **Acceptance** | G-02 acceptance criteria at domain level; no repository |
| **Rollback** | Remove movement types |
| **Risks** | Modeling mutable nested buku like the FEFO spike |
| **Deliverables** | Movement domain + unit tests |

---

### P1-S3 — Stock Layer / Position + ED-constrained FIFO

**Progress:** COMPLETE (2026-08-07) — see [`stock-ledger-phase1-s3-implementation-summary.md`](./stock-ledger-phase1-s3-implementation-summary.md).

| Field | Detail |
|---|---|
| **Objective** | In-memory Layer and Position with Remaining Quantity, depleted retention, optional ED filter then FIFO allocation. |
| **Scope** | G-03; domain portion of G-06 (invariants, version field on Position). **Replace FEFO** — do not copy `StokModel.RemoveStok` ordering. |
| **Dependencies** | P1-S1; optionally P1-S2 for allocation producing movement lines |
| **Affected projects** | Domain, Test |
| **Affected classes** | `StockLayerModel`, `StockPositionModel` (key: Item + Receipt Source + Location); allocator service/method e.g. `StockFifoAllocator` |
| **FIFO rules** | 1) Filter by Item + Location; 2) optional Expiration Date equality filter when requested; 3) order by Effective Receipt Time ascending; 4) tie-break Layer Id; 5) multi-layer split; 6) insufficient stock ⇒ explicit failure; 7) Batch **not** a selection key |
| **Database** | None |
| **Tests** | No ED; explicit ED; equal receipt-time tie-break; multi-layer split; insufficient stock; depleted layer retained at qty 0; Batch ignored |
| **Acceptance** | G-03 acceptance; negative Remaining Quantity impossible |
| **Rollback** | Remove layer/position/allocator types |
| **Risks** | Accidental FEFO; treating Batch as sort key |
| **Deliverables** | Layer/Position/allocator + unit tests |

---

### P1-S4 — Scope coexistence state + mechanism-neutral Synchronization Position

**Progress:** COMPLETE (2026-08-07) — see [`stock-ledger-phase1-s4-implementation-summary.md`](./stock-ledger-phase1-s4-implementation-summary.md).

| Field | Detail |
|---|---|
| **Objective** | Persistable **domain model** for Item + Receipt Source coexistence state without implementing sync/reconstruction behavior. |
| **Scope** | G-04 state machine **shapes** and transitions that are safe without live discovery: e.g. `NotReconstructed` → `ReconstructionRequired`; set opaque Synchronization Position + algorithm version; mark `SynchronizationRequired` / `Inconsistent` via explicit methods. **Do not** call legacy SQL or compute fingerprints. |
| **Dependencies** | P1-S1 |
| **Affected projects** | Domain, Test |
| **Affected classes** | `StockLedgerScopeStateModel`; `SynchronizationPositionType` (opaque `byte[]` or `string` + `AlgorithmVersion`); transition methods with guard clauses |
| **Forbidden** | Hard-coding timestamp+id cursor fields as the position schema; `IsAuthoritative` |
| **Database** | None (schema in P1-S5) |
| **Tests** | Valid transitions; reject illegal transitions; opaque position round-trip in memory; origin enum orthogonal to sync state |
| **Acceptance** | G-04 domain acceptance for state/origin separation |
| **Rollback** | Remove scope state types |
| **Risks** | Encoding watermark columns into the model “for convenience” |
| **Deliverables** | Scope state domain + unit tests |

---

### P1-S5 — Additive SQL schema

| Field | Detail |
|---|---|
| **Objective** | Create additive `BILRG_*` DDL for Movement, Line, Layer, Position, Scope, Idempotency with indexes and optimistic concurrency column on Position. |
| **Scope** | Schema only for G-06/G-07/G-04 persistence. Include uniqueness for source idempotency keys. Keep depleted layers (no delete-on-zero for Ledger layers). |
| **Dependencies** | P1-S2–S4 field inventory finalized |
| **Affected projects** | `Bilreg.SqlDb/InventoryContext/StockLedgerFeature/` |
| **Scripts** | One script per table (or one cohesive feature script) + indexes; follow `DATABASE.md` audit columns |
| **Proposed indexes** | Position by Item+ReceiptSource+Location; Movement by SourceTransactionRef; Scope by Item+ReceiptSource; unique idempotency key |
| **Explicitly out** | Legacy table alters; `FARIN_*` revival; production deploy to `HOSPITAL_HPL` |
| **Tests** | Apply scripts to disposable DB (`DEVTEST`); smoke `SELECT` against new tables |
| **Acceptance** | Scripts apply cleanly; disabling/unused tables do not affect `tb_stok`/`tb_buku` |
| **Rollback** | `DROP` new `BILRG_*` tables on disposable DB only |
| **Risks** | Naming collision; adding FKs to legacy stock tables |
| **Deliverables** | SQL scripts checked into SqlDb project |

---

### P1-S6 — DAL / DTO / Repository round-trips

| Field | Detail |
|---|---|
| **Objective** | Infrastructure persistence for Movement, Layer, Position, ScopeState, Idempotency with optimistic concurrency on Position. |
| **Scope** | G-06, G-07 persistence. Map domain ↔ DTO ↔ SQL. Idempotent insert for source keys (second insert = deterministic duplicate result). |
| **Dependencies** | P1-S5 |
| **Affected projects** | Application (repo interfaces), Infrastructure (DAL/DTO/Repo), Test |
| **Affected classes** | `IStockMovementRepo`, `IStockPositionRepo`, `IStockLedgerScopeStateRepo`, `IStockSourceIdempotencyRepo` (+ DAL/DTO pairs) |
| **Concurrency** | Position update conditioned on version token; conflict ⇒ explicit exception/result (no silent overwrite) |
| **Database** | Disposable DB only |
| **Tests** | Repository integration: insert/load movement; retain depleted layer; concurrent position update conflict; idempotency unique constraint / duplicate handling |
| **Acceptance** | Round-trip preserves domain fields; no Domain SQL; legacy tables unused |
| **Rollback** | Remove infra types; drop tables on disposable DB |
| **Risks** | Reusing broken `tb_buku_dal` patterns; writing to legacy tables |
| **Deliverables** | Repos + integration tests |

---

### P1-S7 — Application ports (contracts only)

| Field | Detail |
|---|---|
| **Objective** | Define Application-layer ports for later phases **without** implementations that hit legacy stock or perform discovery. |
| **Scope** | Interface stubs + XML/doc comments referencing ADRs/gaps. Optional no-op or `NotImplemented` test doubles **only in Test**, not production DI wiring for live behavior. |
| **Dependencies** | P1-S1–S4 types for signatures |
| **Affected projects** | Application, Test |
| **Ports to add** | `ILegacyStockReadPort` (G-05); `ILegacyCompatibilityWriterPort` (G-11); `ILegacyChangeDiscoveryPort` (G-13 — note fingerprint/set-diff contract from sync ADR); `IAvailabilityDiscoveryPort` (G-08); `IProvenanceDiscoveryPort` (G-09); `IStockReconciliationPort` (G-16) |
| **Forbidden** | Implementing Freshness Gate orchestration; querying `tb_buku` for real sync; enabling FO writes |
| **Tests** | Compile-time presence; optional fake doubles for UoW wiring in P1-S8 |
| **Acceptance** | Ports exist; zero production adapters that mutate legacy stock; comments state Phase ownership |
| **Rollback** | Remove port files |
| **Risks** | “Helpful” partial implementations that pretend discovery works |
| **Deliverables** | Port interfaces + test fakes |

---

### P1-S8 — Consequence Unit of Work skeleton

| Field | Detail |
|---|---|
| **Objective** | Explicit short UoW that can persist Ledger Movement/Layer/Position/Scope/Idempotency in one SQL transaction and roll back on failure — **without** calling real Legacy Compatibility Writer. |
| **Scope** | G-18 **foundation** only. Inject `ILegacyCompatibilityWriterPort` but use a Test fake that can throw to prove rollback. No real `tb_stok` writes. |
| **Dependencies** | P1-S6, P1-S7 |
| **Affected projects** | Application (UoW/service), Infrastructure (transaction boundary using existing SQL connection patterns), Test |
| **Affected classes** | e.g. `IStockConsequenceUnitOfWork` / `StockConsequenceUnitOfWork`; method like `CommitAsync(StockConsequenceDraft)` |
| **Behavior** | Begin TX → write idempotency → movement/layers/position/scope → optional fake legacy writer → commit; on any failure roll back all Ledger writes |
| **Database** | Disposable DB |
| **Tests** | Happy-path commit; failure after Ledger write before “legacy” fake ⇒ no durable Ledger rows; duplicate idempotency key safe; G-23 harness **markers**/empty tests documenting future Legacy→New scenarios (skip/fail-not-implemented is OK) |
| **Acceptance** | Failure injection proves Ledger-side atomicity; documentation states legacy side proven in Phase 4+ |
| **Rollback** | Remove UoW; feature remains unused |
| **Risks** | Wiring production DI to run UoW against live hospital DB |
| **Deliverables** | UoW + rollback tests + G-23 placeholder test class |

---

## 7. Cross-cutting Phase 1 rules

1. **Clean Architecture:** Domain has no SQL/Dapper; Application orchestrates; Infrastructure adapts.  
2. **Pragmatism:** Prefer explicit models over generic frameworks; no event sourcing, no broker, no CQRS split beyond existing MediatR habits if a diagnostic query appears later.  
3. **FEFO spike:** Do not extend `StokModel` FEFO; new FIFO lives in `StockLedgerFeature`.  
4. **FARIN:** Do not treat empty `FARIN_Stok*` as Stock Ledger persistence. Prefer new `BILRG_*` tables.  
5. **Exit Review GO conditions:** No hard-coded watermark position; no Freshness Gate claim; discovery is port-only; no production FO enablement.  
6. **Tests per slice:** Only the tests listed for that slice; do not build full coexistence harness yet.  
7. **DI:** Register repos/UoW only if required for tests; no public API enabling stock writes.

---

## 8. Phase 1 exit criteria (definition of done)

- [ ] G-01–G-07 foundation accepted at domain + persistence level (ports for G-05/08/09 present as contracts).  
- [ ] G-18 skeleton proves Ledger-side transactional rollback with fake legacy writer.  
- [ ] Additive migration applies on disposable DB; unused tables do not affect legacy stock.  
- [ ] No `IsAuthoritative`; origin enums are origin-only.  
- [ ] Synchronization Position is opaque + algorithm version.  
- [ ] FIFO (not FEFO) covered by unit tests.  
- [ ] Solution builds; Phase 1 feature not enabled for production traffic.  
- [ ] Implementation notes updated (short Phase 1 report) when coding completes — **out of scope for this planning document**.

---

## 9. Handoff to Phase 2+

When Phase 1 exits:

| Next | Uses from Phase 1 |
|---|---|
| Phase 2 Reconstruction | Scope state transitions, legacy read **port implementation**, Layer/Movement persistence |
| Phase 3 Sync / Freshness | `ILegacyChangeDiscoveryPort` implementation (fingerprint + set-diff/re-derive), opaque position advancement, G-13 residual detection experiments |
| Phase 4 DO Receipt | UoW + real `ILegacyCompatibilityWriterPort`, Native origin movements |

Do not start Phase 2 inside a Phase 1 slice.

---

## 10. References for implementers

| Read first | Why |
|---|---|
| [`stok-ledger-domain.md`](./stok-ledger-domain.md) | Business rules |
| [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md) | GO/NO-GO constraints |
| [`adr/ADR-stock-ledger-legacy-change-discovery.md`](./adr/ADR-stock-ledger-legacy-change-discovery.md) | Position shape + mismatch rule (implement later) |
| [`adr/ADR-stock-ledger-mixed-writer-concurrency.md`](./adr/ADR-stock-ledger-mixed-writer-concurrency.md) | Lock order / OCC expectations |
| [`stock-ledger-gap-analysis.md`](./stock-ledger-gap-analysis.md) G-01–G-07, G-18 | Acceptance language |
| [`docs/skills/feature-model-generation.md`](../../skills/feature-model-generation.md) | Model patterns |
| [`docs/skills/feature-persistence-generation.md`](../../skills/feature-persistence-generation.md) | DTO/DAL/Repo patterns |
