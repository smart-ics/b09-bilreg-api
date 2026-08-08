# Stock Ledger — Phase 5 Implementation Plan

**Artifact status:** Executable Phase-5 plan (implementation-ready; slice planning only)  
**Date:** 2026-08-08  
**Phase:** 5 — Availability, FIFO, transfer, and first outbound  
**Governing baseline (LOCKED):** Phase 0 Exit Review **PASS WITH RISKS** — [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md)  
**Phase 1 foundation (COMPLETE):** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Phase 2 baseline (COMPLETE):** [`stock-ledger-phase2-implementation-report.md`](./stock-ledger-phase2-implementation-report.md)  
**Phase 3 sync/freshness (COMPLETE):** [`stock-ledger-phase3-implementation-report.md`](./stock-ledger-phase3-implementation-report.md)  
**Phase 4 native receipt (COMPLETE):** [`stock-ledger-phase4-implementation-report.md`](./stock-ledger-phase4-implementation-report.md)  
**Roadmap:** [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md) § Phase 5  
**Gaps in scope:** G-03 (application orchestration), G-08 (trusted caller path), transfer portion of G-20, G-16/G-17/G-18 hardening for outbound, ConcurrentOutbound portion of G-23  
**Standards:** [`docs/ENGINEERING.md`](../../ENGINEERING.md), [`docs/DATABASE.md`](../../DATABASE.md), [`docs/NAMING.md`](../../NAMING.md)

### Slice progress

| Slice | Status | Summary |
|---|---|---|
| P5-S1 | COMPLETE | Trusted Availability → Freshness → FIFO orchestration (**gate PASS**). See [`stock-ledger-phase5-s1-implementation-summary.md`](./stock-ledger-phase5-s1-implementation-summary.md). |
| P5-S2 | PENDING | — |
| P5-S3 | PENDING | — |
| P5-S4 | PENDING | — |
| P5-S5 | PENDING | — |
| P5-S6 | PENDING | — |

---

## 1. Purpose

Convert the Phase 5 roadmap into **small, independently executable slices** that a mid-level coding agent can implement with minimal ambiguity.

Phase 5 proves that the new application can perform **safe outbound allocation and cross-location conservation under mixed writers**, using **Stock Transfer (`MT`)** as the first Native outbound path. It must:

* discover provisional Receipt Source candidates from Legacy Stock Authority;
* make each required scope current via reconstruction (when needed) and the Legacy Freshness Gate;
* reload synchronized Stock Ledger layers and allocate with **optional ED filter + FIFO** (not FEFO);
* commit a conserved transfer as **paired OUT/IN** Stock Ledger + legacy-compatible consequences in one short transaction;
* reverse/correct transfers accountably;
* harden .NET-side concurrency (lock order, revalidation, OCC, bounded deadlock retry);
* activate the Phase-5 coexistence harness (`ConcurrentOutbound`) without claiming live VB6 FQ-06 proof.

Phase 5 does **not**:

* migrate reservation / Virtual Stock Location (Phase 6);
* migrate DU/DT/PK/MN/RB/RU/RT/AJ/RP families (Phase 7) — PK/MN remain optional roadmap alternatives, explicitly deferred here because MT already exercises Availability, FIFO, multi-layer, paired locations, and reversal;
* claim production mixed-writer safety (FQ-06 / production G-17 → Phase 9);
* introduce Stage C cutover, `IsAuthoritative`, or per-DO ownership;
* add public production stock-write HTTP endpoints;
* reimplement Availability Discovery, Domain FIFO, Freshness Gate, consequence UoW, or the DM Legacy Compatibility Writer.

**Success meaning (locked):**

```text
Technical outbound/transfer capability complete ≠ safe for unrestricted production use
```

---

## 2. Current repository evaluation

Inspect the repository before each slice. The Phase 5 roadmap remains directionally correct, but **most Stock Ledger foundations already exist**. Do not rebuild parallel stacks.

### 2.1 Already delivered — reuse, do not redo

| Asset | Repository state | Phase 5 implication |
|---|---|---|
| Feature folder | `InventoryContext/StockLedgerFeature/` across Domain / Application / Infrastructure / SqlDb / Test | Extend only |
| Transfer Domain | `StockMovementModel.CreateTransfer` + `EnsureTransferInvariants` (OUT=IN; one source + one dest location; conserve Item+Receipt Source+Unit Valuation) | Express Native MT without new transfer aggregates |
| Outbound Domain | `StockMovementModel.CreateOutbound`; `Reverse` / `Correct` | Use for final-outbound-shaped lines / reversals; do not invent a second movement model |
| ED-constrained FIFO | `StockFifoAllocator`, `StockPositionModel.Allocate`, `StockAllocationResult` (P1-S3; tests in `StockLayerPositionFifoTest`) | Orchestrate after freshness/reload; **do not** reimplement allocator or restore FEFO |
| Provisional Availability Discovery | `IAvailabilityDiscoveryPort` + live `AvailabilityDiscoveryPort` (P2-S7) | Consume as provisional candidates only; never treat as final FIFO or authority |
| Freshness Gate | `LegacyStockFreshnessGate` (P3-S5) with `IsSafeToTrustLedgerLayers` | Call per candidate Item+Receipt Source before trusting layers |
| Reconstruction | `ReconstructStockLedgerBaselineCommand` (Phase 2) | Invoke when Availability finds a DO with no baseline; do not invent a second baseline path |
| Sync stack | Discovery, catch-up, reconcile, `SynchronizeStockLedgerScopeHandler`, `fingerprint-v1` | Consume for later VB6 activity; do not reimplement |
| Consequence UoW | `IStockConsequenceUnitOfWork` / `StockConsequenceDraft` (multi-position ready) | One short TX for transfer OUT/IN Ledger + legacy Apply |
| DM Legacy Compatibility Writer | Live writer supports Receipt + Reversal only; Outbound/Transfer throw `NotSupportedException` | **Extend** for MT shapes; keep DM path intact |
| Native DO Receipt pattern | `PostDoReceipt` / `VoidDoReceipt` + `StockLedgerDoReceiptOptions` (default false) | Clone capability/options/idempotency style for MT; do not couple flags |
| Scope transitions | `EstablishFromNativeReceipt`, `RefreshSynchronizationPosition`, sync transitions | Transfer refreshes Scope position after commit; do not fake reconstruction |
| Coexistence harness | Active Legacy→New / New→Legacy / Native→VB6→sync / AlternatingWriters / PartialFailure; **skipped** `ConcurrentOutbound` | Un-skip only Phase-5-owned scenario |
| Concurrency ADR | Interim lock order Item → Receipt Source → Location → legacy row id | Implement .NET-side protocol; do not claim FQ-06 |
| Production DI / HTTP | None for Stock Ledger FO writes | Keep none; capability remains off |

### 2.2 Still missing — Phase 5 owns

1. **Trusted outbound allocation orchestration** — Application path that chains Availability Discovery → (reconstruct if needed) → Freshness Gate per candidate → reload current layers → ED+FIFO multi-layer / multi-DO allocation plan.  
2. Mapping Freshness Gate fail-closed outcomes to caller-visible `StaleOrNotCurrent` / insufficient / inconsistent results (live Availability adapter still never returns `StaleOrNotCurrent` by design).  
3. Characterized **minimum MT compatibility contract** grounded in `GenStokMutasi` / `GenStokMutasiVoid` / `RemoveStok` / `AddStok`, not a port of `clbGenStokX1`.  
4. Live `ILegacyCompatibilityWriterPort` support for Transfer post (paired `MT_OUT` then `MT_IN`) and Transfer void (`MT_IN_V` then `MT_OUT_V`), enlisted in ambient consequence TX.  
5. Native Stock Transfer Application UseCase that builds a conserved `CreateTransfer` movement, updates source/destination Positions/Layers, refreshes Scope Synchronization Position(s), and commits through existing UoW with live MT legacy writes.  
6. Capability / FO-type flag for Stock Transfer (disabled by default), separate from `StockLedgerDoReceipt`.  
7. Transfer void/correction with accountable Ledger reversal and fail-closed unsafe cases (including partial prior consumption / incomplete void-leg awareness).  
8. .NET-side concurrency hardening for multi-location outbound: deterministic lock order, authoritative quantity revalidation inside TX, Position OCC, bounded deadlock/version retry, negative-stock prevention.  
9. Activation of G-23 `ConcurrentOutbound` harness (fixture/.NET extent — **not** live VB6 binary).  
10. Coexistence proof: Native transfer → later VB6-shaped change → Phase 3 sync → next Ledger-dependent touch.  
11. Phase 5 exit report + residual handoff to Phase 6 / Phase 7 / Phase 9.

### 2.3 Roadmap / planning misalignment corrections

| Earlier assumption | Actual post–Phase-4 state | Plan adjustment |
|---|---|---|
| Phase 5 must “implement Availability Discovery” | Live provisional adapter shipped in **P2-S7** | Wire trusted callers; do not rebuild discovery SQL |
| Phase 5 must implement ED+FIFO | Domain allocator shipped in **P1-S3** | Orchestrate only; keep FEFO out of Domain |
| Phase 5 must invent Freshness Gate | **P3-S5** complete | Call gate; map outcomes; do not duplicate sync |
| Phase 5 must invent consequence UoW | **P1-S8** + live G-18 from Phase 4 | Reuse; prove transfer multi-position + paired legacy writes |
| Phase 5 must invent transfer conservation | Domain `CreateTransfer` already enforces conservation | Build UseCase/legacy rows that satisfy existing invariants |
| DM writer covers outbound | Writer hard-fails Outbound/Transfer | Extend writer for MT only; leave other FO kinds unsupported |
| PK/MN may be first outbound if safer than MT | MT is strongly characterized; Domain transfer already exists; PK adds a second writer family without new orchestration lessons | **Defer PK/MN to Phase 7**; MT is the sole Phase-5 FO family |
| Eight foundation-style slices | Allocation, writer, UseCase, void, concurrency, coexistence fail for different reasons; foundations already exist | **Six** risk-driven slices (see §6) |
| Production mixed-writer proof is Phase 5 exit | Roadmap Phase 5 asks for mixed-writer stress; FQ-06 still Phase 9 | Prove **.NET-side / fixture** ConcurrentOutbound + negative-stock prevention; **explicit non-claim** for live VB6 sessions |
| Production DI for FO | Still absent by design | Optional test/composition root only; **no** public write endpoints; capability default **false** |

### 2.4 Phase 3 / Phase 4 caller contract Phase 5 must honor

Native outbound / transfer UoW writers **must**:

1. Not allocate from Stock Ledger layers until Freshness Gate returns `Current` or `SynchronizedNow` (`IsSafeToTrustLedgerLayers == true`) for **each** affected Item + Receipt Source.  
2. Serialize native write boundaries with synchronization claim / Scope synchronization state (reuse Phase 3 interim serialization).  
3. Honor `StaleOrNotCurrent` / `Inconsistent` as fail-closed for Ledger-dependent decisions.  
4. Never treat provisional G-08 Availability Discovery as Ledger authority or as final FIFO.  
5. Keep `fingerprint-v1` / `LegacyReconstructionBasisCalculator` as the **only** fingerprint authority.  
6. Keep `Native` / `Reconstructed` / `LegacySynchronized` as origin only — never as runtime authority.  
7. Prefer disposable/test SQL (`devTest`); never write Phase 5 test data to production `HOSPITAL_HPL`.

**Transfer-specific nuance vs DO Receipt:**

| Situation | Required behavior |
|---|---|
| Availability finds candidate DO(s) with no Ledger baseline | Reconstruct (Phase 2) then Freshness Gate before allocate |
| Candidate scope already baselined | Freshness Gate first; sync if needed; then reload layers |
| Scope `Inconsistent` / undeterminable freshness | Fail closed; no Native transfer mutation |
| Allocation plan selects layers across multiple DOs | Treat each DO as its own Scope; freshness and position refresh per affected Scope |
| Destination location receives transferred qty | Same Receipt Source + Unit Valuation; do **not** create a new Receipt Source; establish/increase destination layers from transfer provenance |
| Later VB6 activity on transferred stock | Not an authority violation; Phase 3 sync must catch up before next Ledger-dependent decision |

### 2.5 Normative outbound flow (Phase 5)

Reuse roadmap §5.1 with repository-specific adapters:

```text
Stock Transfer Consequence Request (authorized source fact)
    -> Availability Discovery against Legacy Stock Authority (provisional candidates)
    -> for each required Item + Receipt Source:
         reconstruct if no baseline
         pass Legacy Freshness Gate (fail closed if not safe)
    -> reload current Stock Ledger layers at source location
    -> apply optional Expiration Date filter when supplied by source fact
    -> apply FIFO by Effective Receipt Time, then Layer ID (StockFifoAllocator)
    -> build conserved CreateTransfer movement (OUT source + IN destination)
    -> short TX (IStockConsequenceUnitOfWork):
         revalidate authoritative legacy qty for selected rows (lock order)
         write Legacy-Compatible MT_OUT then MT_IN
         write Stock Ledger Movement / Positions / Scope position refresh
         commit once
```

**Critical compatibility rule:** Legacy OUT/IN rows must follow the **Ledger allocation plan** (selected Receipt Sources / quantities / ED / HPP). Do **not** re-run independent VB6 FEFO inside the Native path after Ledger FIFO has chosen layers — that would diverge provenance between representations.

---

## 3. Locked decisions (do not reopen)

| Topic | Locked baseline |
|---|---|
| Runtime authority (Stage B) | `tb_stok` + `tb_buku` — Native transfer does **not** transfer authority |
| Origin labels | `Native` / `Reconstructed` / `LegacySynchronized` — origin only |
| Reconstruction / reconciliation scope | Item + Receipt Source across **all** Stock Locations |
| Write consistency candidate | Item + Receipt Source + Stock Location |
| Allocation | Optional exact ED filter, then **FIFO** by Effective Receipt Time, then Layer ID — **not** FEFO; Batch is never a selection key |
| Availability vs Provenance | Availability Discovery ≠ Provenance Discovery; Phase 5 uses Availability only |
| Sync mechanism | Fingerprint + bounded replay; **exactly one** fingerprint authority: `LegacyReconstructionBasisCalculator` / `fingerprint-v1` |
| Void / delete handling | Never erase Stock Ledger history; use Reverse/Correct; legacy may insert `MT_*_V` or physically delete journals (`xVoidDelete`) |
| Lock order (interim) | Item → Receipt Source → Location → legacy row id ([concurrency ADR](./adr/ADR-stock-ledger-mixed-writer-concurrency.md)) |
| Mixed-writer production correctness | **Not** a Phase 5 claim. Live VB6/.NET proof remains Phase 9 (FQ-06 / G-17) |
| First Phase-5 FO family | **Stock Transfer (`MT`)** only |
| CT / CDC / brokers / event sourcing | Forbidden unless ADR amended |
| Stage C / ownership | Forbidden |

---

## 4. Explicit exclusions (later phases)

Do **not** implement in any Phase 5 slice:

- Reservation / Virtual Stock Location / DR/DS (Phase 6)
- DU/DT/PK/MN/RB/RU/RT/AJ/RP families (Phase 7) — including “optional PK/MN pilot” from the roadmap summary
- Provenance Discovery behavior / returns (G-09 / Phase 7)
- Full G-24 ops dashboard / G-25 production SLOs (Phase 8)
- Live concurrent VB6 session proof / production mixed-writer enablement (Phase 9 / FQ-06)
- Public production HTTP stock-write endpoints
- Porting `clbGenStokX1` control flow into Domain/Application
- Generic stock-processing frameworks, workflow/pipeline engines, allocation “engines”, callback-driven orchestrators
- `IsAuthoritative`, per-Receipt-Source ownership, cutover state
- Claiming FQ-06 / production G-17 complete
- Enabling transfer capability by default in production configuration
- Rebuilding Availability Discovery, Domain FIFO, Freshness Gate, reconstruction, sync, or DM receipt writer
- Treating provisional Availability candidates as final FIFO without Freshness + layer reload

---

## 5. Minimum MT / Stock Transfer compatibility contract

Grounded in repository evidence (`clbGenStokX1.cls` `GenStokMutasi` / `GenStokMutasiVoid` / `RemoveStok` / `AddStok`, Phase 0 writer inventory, existing DM writer patterns). Distinguish **consumer-dependent behavior** from **incidental mechanics**.

### 5.1 Behavior legacy consumers depend on (must preserve)

| Concern | Evidenced contract |
|---|---|
| Entry | FO prefix `MT` → `GenStokMutasi`; void → `GenStokMutasiVoid` |
| Source read (post) | Lines from `tb_trs_mutasi` / `tb_trs_mutasi2` (barang, layanan asal/tujuan, qty, satuan, ED, batch, tgl/jam) |
| Mutation jenis | Post: `MT_OUT` then `MT_IN`; Void: `MT_IN_V` then `MT_OUT_V` |
| Mutation id | `fs_kd_mutasi` = MT transaction id |
| Quantity unit | Converted to smallest unit before stock write |
| OUT leg | Deplete authoritative `tb_stok` at **source** location; insert outbound `tb_buku` (`fn_stok_out`, jenis `MT_OUT`) carrying discovered/selected PO/DO/HPP/ED/batch |
| IN leg | Insert inbound `tb_stok` / `tb_buku` at **destination** from the **OUT journal facts** (not a second independent discovery); jenis `MT_IN`; preserve DO/PO/HPP/ED/batch |
| Pairing | OUT completes before IN reads OUT journals (VB6). Native path must commit both legs in **one** ambient TX using the Ledger allocation plan as the shared truth |
| Zero-row deletion | OUT depleting `fn_qty` to 0 **deletes** `tb_stok` row (Ledger retains depleted layers) |
| Void order | Void destination first (`MT_IN_V` via RemoveStok), then restore source (`MT_OUT_V` via AddStok) |
| Void (`xVoidDelete = False`) | Compensating void journals + stock mutation |
| Void (`xVoidDelete = True`) | Physical `tb_buku` delete possible — fail closed / defer unless Phase 3 sync compatibility is proven for the chosen mode (mirror P4-S4) |
| Post visibility | After commit, VB6 readers / later FO can consume stock at the destination via normal `RemoveStok` |

### 5.2 Incidental mechanics (do not port as Domain)

| Mechanic | Treatment |
|---|---|
| VB6 `Generate` router / `Case TRS_MUTASI` | Compatibility reference only |
| String-concat SQL / `My.SQLGenerator2` | Replace with parameterized Dapper SQL enlisted in ambient TX |
| Entire `clbGenStokX1` control flow | Forbidden in Domain/Application |
| VB6 `RemoveStok` FEFO ordering as Domain FIFO | Forbidden — Domain remains ED filter + FIFO by Effective Receipt Time |
| Commented HPP writeback to `tb_trs_mutasi2` | Not active required behavior unless later evidence says otherwise |
| `SqlBulkCopy` buku path | Prefer single-row parameterized insert for consequence TX enlistment |
| DR rewrite-to-MT reserved path | Phase 6 — do not conflate with ordinary MT |

### 5.3 Known anomaly Phase 5 must tolerate (not “fix” by erasing history)

Phase 0 evidence: `MT_IN_V` / `MT_OUT_V` counts can be imbalanced in retained data. Native void must keep Ledger history accountable and fail closed when a safe paired reverse cannot be determined. Synchronization of incomplete legacy void pairs remains Phase 3’s reconcile/fail-closed responsibility — do not invent silent force-balance in Phase 5.

### 5.4 Source Business Fact boundary

Stock Ledger does **not** own Stock Transfer authorization/UI. The Phase 5 UseCase accepts an **already-authorized** transfer source fact (MT id, item lines, source location, destination location, qty in stock unit, optional explicit ED, timestamps). Reading `tb_trs_mutasi*` may live in an Infrastructure adapter for tests/integration, but Domain must not embed FO UI workflow.

---

## 6. Slicing decision

### Decision

Phase 5 is divided into **six incremental slices (P5-S1…P5-S6)**.

### Why not eight foundation-style slices

Phases 1–4 already delivered Domain, persistence, ports, UoW, reconstruction, provisional Availability, sync, Freshness Gate, DM writer, and Native receipt. Repeating a “layer tour” would create artificial slices without independent acceptance criteria.

### Why not three slices

Trusted allocation, MT legacy shapes, Native UseCase wiring, void semantics, concurrency hardening, and coexistence proof fail for different reasons. Merging them produces unreviewable partial enablement and makes rollback unclear.

### Why six (not five)

Phase 4 needed five slices because receipt does not allocate. Phase 5’s **trusted allocation orchestration** is a distinct implementation gate that must be proven before any FO writer or UseCase consumes it. Concurrency hardening is also a distinct risk from void/coexistence and must not be bundled with the first happy-path UseCase.

### Why this order

| Risk / question | Slice |
|---|---|
| Can we produce a trusted, freshness-gated FIFO allocation plan from provisional Availability candidates without writing FO stock? | **P5-S1** (allocation gate) |
| Can the compatibility boundary express real MT OUT/IN consequences without Domain leakage? | **P5-S2** (writer gate) |
| Can Native Transfer Movement/Layers + legacy OUT/IN commit as one consequence behind a capability flag? | **P5-S3** |
| Can void/correction preserve Ledger history and paired legacy compatibility without unsafe deletes? | **P5-S4** |
| Can .NET-side lock order, revalidation, OCC, and ConcurrentOutbound prevent negative stock / lost updates among new writers? | **P5-S5** |
| Does later VB6-shaped activity catch up via Phase 3, and can the capability stay disabled safely? | **P5-S6** |

### Slice map (execute in order)

```text
P5-S1 Trusted Availability → Freshness → FIFO orchestration   << allocation gate
   -> P5-S2 Live MT Legacy Compatibility Writer (OUT/IN post)  << writer gate
   -> P5-S3 Native Stock Transfer UseCase + capability flag
   -> P5-S4 Transfer void/reversal (fail closed when unsafe)
   -> P5-S5 .NET concurrency hardening + ConcurrentOutbound
   -> P5-S6 Coexistence proof + Phase 5 exit hardening + report
```

### Implementation gates (checkpoints, not new phases)

**P5-S1 gate:** If trusted allocation cannot fail closed on stale/inconsistent scopes, or selects layers without Freshness + reload, **stop**. Do not start P5-S2…P5-S6 FO write work.

**P5-S2 gate:** If MT OUT/IN cannot produce disposable-DB `tb_stok` / `tb_buku` rows that match the §5 consumer-dependent contract (paired conservation, DO/HPP/ED continuity, enlisted rollback), **stop**. Do not start Native Transfer UseCase work.

---

## 7. Slice specifications

### P5-S1 — Trusted Availability → Freshness → FIFO orchestration

| Field | Detail |
|---|---|
| **Slice ID / name** | P5-S1 — Trusted outbound allocation orchestration |
| **Problem / question being solved** | Can provisional Availability candidates be turned into a deterministic, freshness-gated, ED-constrained FIFO allocation plan — including multi-DO / multi-layer splits — without treating Availability as authority and without writing FO stock? |
| **Objective** | Deliver an explicit Application orchestration service (not a framework) that returns either a trusted allocation plan or a fail-closed outcome. |
| **Why this slice exists independently** | If allocation inputs are wrong, every later Native Transfer test is meaningless. This is the Phase 5 allocation gate. |
| **Dependencies** | P2-S7 Availability; P1-S3 FIFO; P2 reconstruction UseCase; P3-S5 Freshness Gate; Phase 4 caller contract |
| **Current code to reuse** | `IAvailabilityDiscoveryPort`, `LegacyStockFreshnessGate`, `ReconstructStockLedgerBaselineCommand` / handler, `IStockPositionRepo`, `StockFifoAllocator` / `StockPositionModel.Allocate`, MediatR/Inventory UseCase style |
| **Expected affected projects/areas** | Application (new orchestrator + result types), Test; Domain only if a tiny immutable plan DTO is clearer than reusing `StockAllocationResult` collections — prefer reuse |
| **Implementation scope** | (1) Input: item, source location, requested qty, optional ED, decision context; (2) call Availability Discovery; (3) for each provisional candidate DO needed to satisfy qty: reconstruct if no baseline, then Freshness Gate; (4) if any required scope is not safe → fail closed (`StaleOrNotCurrent` / `Inconsistent` / insufficient); (5) reload current layers/positions for source location; (6) allocate with optional ED + FIFO across layers (multi-layer / multi-DO); (7) return explicit plan: selected layers, Receipt Sources, quantities, valuation/ED — **no persistence**, no legacy write, no Transfer UseCase; (8) never mutate based on provisional candidates alone; (9) keep control flow obvious and sequential |
| **Explicit exclusions** | MT writer; Native Transfer UseCase; void; concurrency locks; PK/MN; production DI/HTTP; rebuilding Availability SQL; FEFO |
| **Database impact** | May run reconstruction/sync side effects already owned by Phase 2/3 when making scopes current — no new FO stock writes. Disposable DB only for tests. |
| **Tests** | Multi-DO candidates → plan selects FIFO order after freshness; explicit ED filter; insufficient stock; stale scope fail closed; inconsistent fail closed; depleted legacy rows excluded by Availability; batch ignored; no `tb_stok`/`tb_buku` FO mutation from this slice alone; Freshness `SynchronizedNow` then allocate succeeds |
| **Acceptance criteria** | Gate PASS: trusted plan or fail-closed outcome only; Availability remains provisional; Domain FIFO reused; no FO writer changes |
| **Rollback / containment** | Remove orchestrator; Phase 2/3/4 paths unaffected |
| **Risks** | Calling Allocate before Freshness; reconstructing every candidate eagerly without bound; inventing a generic pipeline framework; conflating Availability totals with Ledger Remaining Quantity without reload |
| **Deliverables** | Orchestrator + tests + `stock-ledger-P5-S1-implementation-summary.md` with **gate PASS/FAIL** |

---

### P5-S2 — Live MT Legacy Compatibility Writer (OUT/IN post)

| Field | Detail |
|---|---|
| **Slice ID / name** | P5-S2 — Live MT Legacy Compatibility Writer (transfer post shape) |
| **Problem / question being solved** | Can the new application reproduce the authoritative MT transfer consequence (`MT_OUT` deplete at source + `MT_IN` insert at destination, preserving DO/PO/HPP/ED/batch) through `ILegacyCompatibilityWriterPort` without leaking VB6 control flow into Domain? |
| **Objective** | Extend the live Infrastructure writer for Transfer **post** shapes from an explicit request that already names the allocated OUT facts, enlisted in the caller’s ambient SQL transaction, proven on disposable/test SQL. |
| **Why this slice exists independently** | If legacy OUT/IN shapes or pairing are wrong, Native Transfer orchestration tests cannot prove coexistence. This is the Phase 5 writer gate. |
| **Dependencies** | P5-S1 gate PASS (for plan shape guidance); Phase 4 live writer patterns; Phase 0 MT characterization; `clbGenStokX1` reference |
| **Current code to reuse** | `ILegacyCompatibilityWriterPort`, `LegacyCompatibilityWriteRequest` / balance+journal DTOs, DM post/void branches as enlistment templates, `LegacyStockReadPort` for asserts, ST/BK id counters, `FakeLegacyCompatibilityWriterPort` |
| **Expected affected projects/areas** | Infrastructure (writer Transfer branch), Application only if request DTOs need additive fields for source/destination legs, Test, docs (contract notes in summary) |
| **Implementation scope** | (1) Document §5 contract in slice summary with A/B distinction; (2) accept an **allocation-explicit** transfer write request (selected DO/qty/ED/HPP/PO per OUT leg + destination location) — do not re-FIFO inside the writer; (3) apply `MT_OUT` then `MT_IN` in one `Apply` (or ordered apply inside ambient TX); (4) OUT: reduce/delete `tb_stok` at source for the specified DO/ED rows; insert outbound `tb_buku`; (5) IN: insert destination `tb_stok`/`tb_buku` copying provenance from OUT facts; (6) ambient TX enlistment + rollback proof; (7) reject unsupported FO/movement kinds explicitly (keep DM paths green); (8) characterization tests on `devTest` only |
| **Explicit exclusions** | Native Transfer UseCase; void path; Freshness Gate wiring; production DI/HTTP; porting `Generate`; PK/MN; inventing DR reserved semantics |
| **Database impact** | Writes disposable-test `tb_stok` / `tb_buku` only. No new BILRG tables required. |
| **Tests** | Apply MT post → read-back source depleted and destination increased by same qty/DO/HPP/ED; rollback when outer UoW does not `Complete`; unsupported kinds still fail explicitly; DM receipt/void regression remains green; zero-row delete on full OUT depletion |
| **Acceptance criteria** | Gate PASS: consumer-dependent MT post contract satisfied on disposable DB; writer stays in Infrastructure; Domain remains SQL-free; no production enablement |
| **Rollback / containment** | Disable Transfer branch / keep throwing for Transfer if needed; DM writer remains |
| **Risks** | Independent FEFO inside writer diverging from Ledger plan; OUT without IN partial visibility outside TX; counter concurrency (FQ-02 residual); destination insert merging incorrectly instead of VB6-compatible insert behavior |
| **Deliverables** | Live writer Transfer post + tests + `stock-ledger-P5-S2-implementation-summary.md` with **gate PASS/FAIL** |

---

### P5-S3 — Native Stock Transfer UseCase + capability flag

| Field | Detail |
|---|---|
| **Slice ID / name** | P5-S3 — Native Stock Transfer consequence |
| **Problem / question being solved** | Can an authorized Stock Transfer be represented as a Native conserved Transfer movement, update source/destination layers/positions, refresh coexistence Synchronization Position, and commit with the P5-S2 legacy consequence through the existing UoW — behind a disabled-by-default capability flag? |
| **Objective** | Thin MediatR/Inventory-style UseCase that posts one Stock Transfer consequence when the capability is enabled. |
| **Why this slice exists independently** | Separates “allocation works” (S1) and “legacy shapes work” (S2) from “Native semantics + flag + Scope refresh” so orchestration can be reviewed without reopening DAL SQL. |
| **Dependencies** | P5-S1 **gate PASS**; P5-S2 **gate PASS**; Phase 1 Domain factories + UoW; Phase 3 Freshness / sync serialization notes; Phase 4 capability/options pattern |
| **Current code to reuse** | P5-S1 orchestrator, `StockMovementModel.CreateTransfer`, Position allocate/consume results, `StockConsequenceDraft` / UoW, `RefreshSynchronizationPosition` / fingerprint calculator, `PostDoReceiptStockConsequenceCommand` as structural pattern, new `StockLedgerStockTransferOptions` (name may vary; default `Enabled = false`) |
| **Expected affected projects/areas** | Application (UseCase + options + mapper from allocation plan → legacy request), Domain only if Scope refresh needs a tiny transfer-specific helper, Infrastructure DI for tests if needed, Test |
| **Implementation scope** | (1) Command accepting authorized transfer source fact (multi-line allowed; one source + one destination location per movement invariants); (2) run P5-S1 trusted allocation for each line/qty; (3) build Native-origin Transfer movement with paired OUT/IN lines identifying layers; (4) update Positions (source consume; destination establish/increase layers preserving Receipt Source + Unit Valuation + Effective Receipt Time semantics already used by Domain); (5) map to MT legacy write request; (6) short TX via existing UoW; (7) refresh Scope Synchronization Position with `fingerprint-v1` for each affected Item+DO; (8) ensure discovery identity coverage continuity (reuse SYNC bootstrap patterns); (9) `SourceConsequence` idempotency key from MT responsibility; (10) capability flag default **false**; (11) happy-path atomicity smoke (full failure matrix in P5-S5) |
| **Explicit exclusions** | Void; ConcurrentOutbound; production HTTP; PK/MN; claiming FQ-06; enabling flag in production appsettings; generic allocation engine |
| **Database impact** | Additive Ledger rows + legacy stock rows on disposable DB when tests enable the flag |
| **Tests** | Happy-path Native Transfer origin + conserved legacy OUT/IN; capability disabled ⇒ no writes; duplicate source responsibility ⇒ quantity-neutral; multi-layer / multi-DO line; explicit ED path; insufficient / stale fail closed; no `IsAuthoritative`; fingerprint algorithm version `fingerprint-v1` |
| **Acceptance criteria** | G-20 transfer post path works behind flag; conservation holds in Ledger and legacy; authority remains legacy; UoW remains the single short TX |
| **Rollback / containment** | Keep flag false; remove handler registration; committed test rows only on disposable DB |
| **Risks** | Creating new Receipt Source at destination; long TX around discovery/freshness; skipping reload after sync; coupling to DoReceipt options |
| **Deliverables** | UseCase + options + tests + `stock-ledger-P5-S3-implementation-summary.md` |

---

### P5-S4 — Transfer void / reversal

| Field | Detail |
|---|---|
| **Slice ID / name** | P5-S4 — Transfer void/reversal |
| **Problem / question being solved** | How should transfer void/correction be represented so Stock Ledger history stays immutable, paired legacy void journals remain compatible, Remaining Quantity stays coherent at both locations, and retries are idempotent — without deleting the original Native Transfer movement? |
| **Objective** | Minimal safe MT void path for Native-posted transfers; fail closed when evidence is ambiguous or stock was already consumed after transfer. |
| **Why this slice exists independently** | Void order (`MT_IN_V` then `MT_OUT_V`), incomplete void-leg anomaly tolerance, and destination consumption races are distinct risks from posting. |
| **Dependencies** | P5-S1–S3; Domain `Reverse`/`Correct`; Phase 4 void style as reference; Phase 3 void→correction interpretation as behavioral reference |
| **Current code to reuse** | `StockMovementModel.Reverse`, P4-S4 void UseCase patterns, writer void branch extension, `RefreshSynchronizationPosition`, SourceConsequence idempotency |
| **Expected affected projects/areas** | Infrastructure (writer void branch), Application (VoidStockTransfer UseCase), Test |
| **Implementation scope** | (1) Prefer accountable **Reversal** of the original Native Transfer movement; (2) legacy side: `MT_IN_V` then `MT_OUT_V` with `xVoidDelete=False` as default Phase 5 void mode; (3) fail closed if destination qty insufficient / already consumed / paired reverse unsafe; (4) idempotent void retry; (5) refresh Scope Synchronization Position(s) after success; (6) `xVoidDelete=True` only if explicitly supported with tests — otherwise defer with fail-closed rationale (mirror P4-S4); (7) document MT void-leg imbalance anomaly as non-goal to “repair” by erasing Ledger history |
| **Explicit exclusions** | Generic void framework for all FO families; deleting Ledger history; silent force-balance; PK/MN voids; production enablement |
| **Database impact** | Disposable DB legacy + Ledger writes |
| **Tests** | Void after transfer restores source and clears destination remaining for transferred qty; original movement retained; repeated void quantity-neutral; destination already consumed ⇒ fail closed; capability disabled blocks void |
| **Acceptance criteria** | History retained; legacy readers see void consequence; unsafe cases fail closed |
| **Rollback / containment** | Disable void UseCase/flag branch; leave post path intact |
| **Risks** | Partial destination consumption; multi-DO transfer lines; delete-void sync surprises; over-general void engine |
| **Deliverables** | Void UseCase + writer void support + tests + `stock-ledger-P5-S4-implementation-summary.md` (explicit deferrals listed) |

---

### P5-S5 — .NET concurrency hardening + ConcurrentOutbound

| Field | Detail |
|---|---|
| **Slice ID / name** | P5-S5 — Concurrency hardening + ConcurrentOutbound harness |
| **Problem / question being solved** | Can two new-system outbound/transfer attempts on overlapping stock, plus fixture-simulated races against revalidation, avoid negative stock and lost updates using the interim concurrency ADR — without claiming live VB6 FQ-06 proof? |
| **Objective** | Implement deterministic lock/touch order, authoritative quantity revalidation inside the consequence TX, Position OCC conflict handling, bounded deadlock/version retries, and activate G-23 `ConcurrentOutbound`. |
| **Why this slice exists independently** | Happy-path Transfer can pass while races lose quantity. Concurrency must be proven after the UseCase exists, but before Phase 5 exit claims outbound readiness. |
| **Dependencies** | P5-S3 (and preferably P5-S4 for void conflict fail-closed behavior); concurrency ADR; Phase 3 sync/native serialization |
| **Current code to reuse** | `StockConsequenceUnitOfWork`, Position version/OCC, DM void `UPDLOCK` pattern as a starting point (not sufficient alone), coexistence harness placeholder `ConcurrentOutbound_RespectsWriteConsistencyScope`, Phase 3 claim serialization patterns |
| **Expected affected projects/areas** | Infrastructure (lock/revalidate SQL for selected `tb_stok` rows), Application (retry policy around UoW conflicts — explicit, bounded), Test (primary) |
| **Implementation scope** | (1) Inside consequence TX, lock/touch selected legacy rows in ADR order (Item → Receipt Source → Location → `fs_kd_trs`); (2) revalidate expected qty before mutate; conflict → retry/reject, never silent overwrite; (3) Position OCC conflicts surface and retry bounded times; (4) deadlock retry bounded; (5) activate ConcurrentOutbound harness with disposable fixtures (two .NET writers / overlapping qty); (6) failure injection for transfer persistence boundaries (extend P4-S3 style to multi-position + OUT/IN); (7) **explicit non-claim** text for FQ-06 / live VB6 |
| **Explicit exclusions** | Live VB6 concurrent sessions; production enablement; changing Stage B authority; inventing distributed lock services |
| **Database impact** | Disposable DB only; possible query hint usage on `tb_stok` selects inside ambient TX |
| **Tests** | Concurrent overlapping transfers: one winner or non-overlapping allocations; no negative stock; stale selection revalidates; partial failure rolls back both legs/representations; ConcurrentOutbound un-skipped and green; prior StockLedgerFeature suite remains green (single-threaded filter guidance retained) |
| **Acceptance criteria** | Interim G-17 .NET-side outbound hardening accepted for fixtures; G-23 ConcurrentOutbound activated; FQ-06 still not claimed |
| **Rollback / containment** | Re-skip ConcurrentOutbound if unstable; keep capability false |
| **Risks** | Over-claiming mixed-writer safety; lock order inversion with sync claims; flaky parallel tests on shared `devTest` |
| **Deliverables** | Hardening + harness activation + `stock-ledger-P5-S5-implementation-summary.md` |

---

### P5-S6 — Coexistence proof + Phase 5 exit

| Field | Detail |
|---|---|
| **Slice ID / name** | P5-S6 — Coexistence proof + Phase 5 exit |
| **Problem / question being solved** | After a Native Stock Transfer, can later VB6-shaped consumption/transfer be detected and synchronized by Phase 3 before the next Ledger-dependent operation — and can disabling the transfer capability leave legacy operation unaffected? |
| **Objective** | Prove the normative coexistence sequence for outbound/transfer; publish Phase 5 exit report; hand off residuals. |
| **Why this slice exists independently** | This is the Phase 5 *reason for existing beyond happy-path writes*: Native outbound is not authority, and Phase 3 catch-up must work on transferred scopes. |
| **Dependencies** | P5-S1–S5; Phase 3 Freshness Gate + sync handler + discovery + reconcile |
| **Current code to reuse** | `LegacyStockFreshnessGate`, `SynchronizeStockLedgerScopeHandler`, P4-S5 coexistence tests as template, `StockLedgerSyncExplainability`, fingerprint continuity |
| **Expected affected projects/areas** | Test + docs (primary); tiny Application glue only if transfer Scope bootstrap omitted identity keys needed by discovery |
| **Implementation scope** | (1) Scenario: Native transfer → legacy authority rows at dest → simulate VB6 consume/transfer on disposable fixtures → next touch runs Freshness Gate → sync catch-up → reconcile succeeds → processing can continue; (2) do **not** treat VB6 activity on Native-origin/transferred layers as error/authority violation; (3) capability disabled ⇒ legacy-only path unaffected; (4) Phase 5 implementation/exit report; (5) update plan progress + ARTIFACTS; (6) restate FQ-06 non-claim and PK/MN deferral |
| **Explicit exclusions** | Live VB6 concurrent sessions; Phase 6 reservation; production flag enablement; Stage C |
| **Database impact** | Disposable fixtures only |
| **Tests** | Coexistence sequence above; sync position advances with `fingerprint-v1`; Freshness Gate `SynchronizedNow`/`Current`; capability-off continuity; StockLedgerFeature suite green (single-threaded) |
| **Acceptance criteria** | Roadmap Phase 5 validation scenarios for transfer/outbound proven at technical-capability level; exit checklist complete; capability remains default-off |
| **Rollback / containment** | Keep capability false; Phase 3 sync remains usable independently |
| **Risks** | Missing discovery identity bootstrap after transfer; false Authority Gate regression; overclaiming mixed-writer safety |
| **Deliverables** | Coexistence tests + `stock-ledger-P5-S6-implementation-summary.md` + `stock-ledger-phase5-implementation-report.md` |

---

## 8. Atomic consequence boundary (normative)

One successful Stock Transfer (capability enabled) must commit **all applicable** consequences in **one short SQL transaction** via `IStockConsequenceUnitOfWork`:

```text
Begin ambient TX
  -> Insert SourceConsequence idempotency (duplicate => AlreadyCommitted, no rewrite)
  -> Lock/revalidate selected authoritative tb_stok rows (ADR order)
  -> Persist Native Stock Movement (+ paired lines)
  -> Persist Stock Positions / Layers (source + destination)
  -> Persist Scope coexistence state refresh (Synchronization Position when ready)
  -> ILegacyCompatibilityWriterPort.Apply (MT_OUT then MT_IN)
  -> Complete
```

**Invariant:** A successful new-system transfer must never leave the Stock Ledger representation committed without its required paired legacy-authoritative consequence, or vice versa. OUT without IN (or Ledger without legacy) is a defect.

**Keep outside the short TX:** Availability Discovery, reconstruction, Freshness Gate catch-up (catch-up has its own TX), HPP/policy loading, unnecessary FO header reads.

**Write-order note:** Ledger-before-legacy remains acceptable **if and only if** failure before `Complete` rolls back **both**. P5-S5 must prove this for multi-position transfer. Do not split OUT and IN into two commits.

---

## 9. Capability flag and rollout boundary

| Topic | Phase 5 rule |
|---|---|
| Mechanism | Explicit options object (e.g. `StockLedgerStockTransferOptions.Enabled`) following `StockLedgerDoReceiptOptions` |
| Default | **`false`** |
| Independence | Do **not** reuse or implicitly enable via `StockLedgerDoReceipt` |
| Production appsettings | Must not silently enable |
| HTTP | **No** public production write endpoint required for Phase 5 exit |
| Composition | Tests may construct handler + live writer manually (Phase 3/4 style) |
| Phase 5 complete means | Technical transfer/outbound slice proven behind flag |
| Still gated | Phase 6 reservation; Phase 7 other FO families; Phase 9 mixed-writer / coexistence production rollout (FQ-06) |

---

## 10. Testing philosophy

Prefer integration tests against disposable/test SQL (`devTest` / `StockLedgerSchemaFixture`). **Never** write Phase 5 test data to production legacy stock (`HOSPITAL_HPL`).

| Area | Required coverage (Phase 5 whole) |
|---|---|
| Trusted allocation | Freshness-gated FIFO plan; multi-layer; multi-DO; optional ED |
| Normal Stock Transfer | Native origin + conserved legacy OUT/IN |
| Duplicate / replay | Quantity-neutral |
| Failure injection | Persistence boundaries for multi-position + OUT/IN |
| Rollback | Neither representation / neither leg partially committed |
| Void / retry | P5-S4; fail closed when unsafe |
| ConcurrentOutbound | Activated; no negative stock / lost update among .NET writers |
| Coexistence | Native transfer → VB6-shaped change → Phase 3 sync → new touch |
| Capability off | No new writes; legacy path unaffected |
| Non-claims | No FQ-06 / production G-17 claim; no PK/MN enablement |

Parallel full-suite runs on shared `devTest` may still deadlock intermittently (documented since Phase 4). Gate evidence uses focused and single-threaded filters.

---

## 11. Risk analysis

### 11.1 Highest-risk Phase 5 areas

| Risk | Why it matters | Slice boundary mitigation |
|---|---|---|
| Stale FIFO commit | VB6 or another writer changes qty after provisional discovery | P5-S1 forces Freshness + reload; P5-S5 revalidates inside TX |
| Writer re-FEFO diverging from Ledger plan | Provenance split between `tb_buku` and Ledger layers | P5-S2 forbids independent FEFO; allocation-explicit request |
| Partial transfer (OUT without IN) | Breaks conservation and legacy readers | Single ambient TX; P5-S3/S5 atomicity proofs |
| Multi-location deadlocks | Source/dest + multi-DO locks | ADR order in P5-S5; bounded retry |
| Treating Availability as authority | Wrong DO selection / skipped freshness | P5-S1 gate; explicit provisional semantics |
| Void after destination consumption | Negative stock or invented reverse | P5-S4 fail closed |
| Over-claiming FQ-06 | Unsafe production enablement | Explicit non-claim through P5-S5/S6 and exit report |
| Over-building allocation framework | Unmaintainable “smart” pipeline | P5-S1 requires explicit sequential orchestration only |

### 11.2 Reviewer checkpoints before next slice

| After | Reviewer must confirm before continuing |
|---|---|
| P5-S1 | Gate PASS; no FO writes; Freshness precedes Allocate; FEFO absent |
| P5-S2 | Gate PASS; OUT/IN conservation on disposable DB; DM regression green; no Domain SQL |
| P5-S3 | Capability default false; CreateTransfer conservation; fingerprint-v1 refresh; no new Receipt Source at destination |
| P5-S4 | History retained; unsafe void fail closed; `xVoidDelete=True` deferred or proven |
| P5-S5 | ConcurrentOutbound green; negative stock impossible in fixtures; FQ-06 non-claim present |
| P5-S6 | Coexistence sequence green; exit report published; production flag still false |

### 11.3 Why these boundaries reduce blast radius

* Allocation bugs cannot corrupt legacy stock until S1 passes.  
* Writer shape bugs cannot corrupt Ledger semantics until S2 passes.  
* UseCase wiring is reviewable without concurrency flakiness (S3 before S5).  
* Void cannot invent unsafe deletes inside the first post slice.  
* Concurrency hardening is isolated so retries/lock hints do not obscure happy-path defects.  
* Coexistence proof is last, after post/void/concurrency exist — same rationale as P4-S5.

---

## 12. Documentation obligations (every slice)

Each implementation slice must:

1. Produce a short `stock-ledger-P5-S{n}-implementation-summary.md`.  
2. Update this plan’s **Slice progress** table.  
3. Update [`docs/ARTIFACTS.md`](../../ARTIFACTS.md) when new summaries/reports are added.  
4. Record deviations from this plan based on actual codebase findings.  
5. Avoid silently changing locked domain or ADR decisions.

At Phase 5 completion, publish `stock-ledger-phase5-implementation-report.md` covering:

- what was implemented;
- coexistence and ConcurrentOutbound scenarios proven;
- what remains intentionally disabled;
- residual risks;
- dependencies carried into Phase 6 / Phase 7 / Phase 9.

---

## 13. Phase 5 exit checklist

- [x] **P5-S1 gate passed:** trusted Availability → Freshness → FIFO orchestration accepted before FO write work proceeds.  
- [ ] **P5-S2 gate passed:** live MT OUT/IN compatibility writer accepted before Native Transfer UseCase proceeds.  
- [ ] **G-08 trusted caller path accepted:** provisional Availability is never final FIFO; Freshness/reconstruction precede layer trust.  
- [ ] **G-03 application orchestration accepted:** optional ED + FIFO multi-layer / multi-DO allocation used by Native Transfer.  
- [ ] **G-20 transfer portion accepted as technical capability:** Native Stock Transfer post + void behind disabled-by-default flag; conservation holds; legacy readers can consume results.  
- [ ] **G-18 hardened for transfer:** failure injection proves no partial Ledger/legacy and no OUT-without-IN commit.  
- [ ] **G-17 interim (.NET-side) outbound hardening accepted:** lock order, revalidate, OCC, bounded retry; ConcurrentOutbound activated.  
- [ ] **Explicit non-claim:** FQ-06 / production G-17 / live VB6 concurrent sessions **not** done.  
- [ ] **Phase 3 integration:** Native transfer → later legacy change → Freshness Gate / sync catch-up → continue; Native/transferred origin is not treated as VB6 prohibition.  
- [ ] **Capability flag default false;** independent from DO Receipt; no public production write endpoint required.  
- [ ] **PK/MN explicitly deferred** to Phase 7 with rationale.  
- [ ] **No** `IsAuthoritative`, Stage C, generic stock/allocation framework, or `clbGenStokX1` port.  
- [ ] Solution builds; StockLedgerFeature tests green for Phase 5 slices (single-threaded); Phase 1–4 tests remain green.  
- [ ] Phase 5 implementation report published; ARTIFACTS updated.

---

## 14. Residual risks / handoff

| Residual | Owner | Blocks Phase 5 coding start? |
|---|---|---|
| FQ-06 live VB6 concurrency proof / production G-17 | Phase 9 | **No** |
| G-25 proposed legacy indexes / p95 SLOs for Availability | Phase 8 / DBA | **No** (measure in tests; do not block) |
| Legacy stock id counter multi-instance atomicity (FQ-02) | Production hardening may continue | **No** |
| PK/MN / other FO families | Phase 7 | **No** |
| Reservation / Virtual Stock Location | Phase 6 | **No** |
| Production capability enablement | Phase 9 | **No** (must remain off at Phase 5 exit) |
| MT void-leg imbalance in retained legacy data | Sync/reconcile tolerance (Phase 3/8); Native void fail closed | **No** |
| Shared disposable-DB deadlock under parallel tests | Test hygiene | **No** |

### Later phases may consume

| From Phase 5 | Use |
|---|---|
| Trusted allocation orchestration | Ordinary outbound families (PK/MN/DU/…) in Phase 7 |
| MT Legacy Compatibility Writer (post + void) | Pattern for other OUT/IN-shaped FO families |
| Transfer UseCase + capability options | Pattern for FO capability gating |
| ConcurrentOutbound harness | Extend toward Phase 9 live VB6 proof — do not treat as FQ-06 |
| Freshness-before-allocate discipline | Required for every later Ledger-dependent outbound |

**Do not** claim FQ-06 / production G-17 complete when enabling FO paths. Live VB6/.NET mixed-writer proof remains **Phase 9**.

---

## 15. Engineer checklist per slice

- [ ] Inspect current Stock Ledger code before coding; extend, do not fork.  
- [ ] `tb_stok + tb_buku` remain Stage B authority.  
- [ ] `Native` is origin only.  
- [ ] No `IsAuthoritative` / ownership / Stage C.  
- [ ] Availability remains provisional until Freshness + layer reload.  
- [ ] FIFO is ED filter + Effective Receipt Time + Layer ID — not FEFO.  
- [ ] Short TX; discovery/freshness/catch-up outside consequence TX.  
- [ ] Fingerprint continuity: only `fingerprint-v1`.  
- [ ] Freshness Gate / sync / reconstruction / Domain FIFO reused, not reimplemented.  
- [ ] Transfer capability remains default-off and independent from DO Receipt.  
- [ ] Disposable DB only for legacy writes in tests.  
- [ ] Slice summary + plan progress + ARTIFACTS updated.  
- [ ] No FQ-06 production claim.  
- [ ] No generic allocation/workflow framework.

---

## 16. Suggested first implementation action

Start **P5-S1** by implementing the trusted allocation orchestrator against existing Availability + Freshness + Position reload + `StockFifoAllocator`, with fail-closed stale/inconsistent tests and **no** FO writer changes. Do not begin P5-S2 MT writer work until the P5-S1 gate is **PASS**.
