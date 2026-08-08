# Stock Ledger — Phase 4 Implementation Plan

**Artifact status:** Executable Phase-4 plan (implementation-ready)  
**Date:** 2026-08-08  
**Phase:** 4 — First Native Stock Consequence: DO Receipt  
**Governing baseline (LOCKED):** Phase 0 Exit Review **PASS WITH RISKS** — [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md)  
**Phase 1 foundation (COMPLETE):** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Phase 2 baseline (COMPLETE):** [`stock-ledger-phase2-implementation-report.md`](./stock-ledger-phase2-implementation-report.md)  
**Phase 3 sync/freshness (COMPLETE):** [`stock-ledger-phase3-implementation-report.md`](./stock-ledger-phase3-implementation-report.md) — latest slice [`stock-ledger-P3-S8-implementation-summary.md`](./stock-ledger-P3-S8-implementation-summary.md)  
**Roadmap:** [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md) § Phase 4  
**Gaps in scope:** G-11, G-18 (live legacy side), G-19; New→Legacy / partial-failure portion of G-23; DM post/void characterization  
**Standards:** [`docs/ENGINEERING.md`](../../ENGINEERING.md), [`docs/DATABASE.md`](../../DATABASE.md), [`docs/NAMING.md`](../../NAMING.md)

### Slice progress

| Slice | Status | Summary |
|---|---|---|
| P4-S1 | COMPLETE | [`stock-ledger-P4-S1-implementation-summary.md`](./stock-ledger-P4-S1-implementation-summary.md) — gate **PASS**; live DM post writer |
| P4-S2 | COMPLETE | [`stock-ledger-P4-S2-implementation-summary.md`](./stock-ledger-P4-S2-implementation-summary.md) — Native DO Receipt UseCase + Scope baseline + capability flag + greenfield prior-history fail-closed guard (NO-GO remediated) |
| P4-S3 | COMPLETE | [`stock-ledger-P4-S3-implementation-summary.md`](./stock-ledger-P4-S3-implementation-summary.md) — G-18 live-writer atomicity + G-23 New→Legacy / PartialFailure harness |
| P4-S4 | COMPLETE | [`stock-ledger-P4-S4-implementation-summary.md`](./stock-ledger-P4-S4-implementation-summary.md) — Receipt void/correction (accountable Reversal + DO_V; fail closed when unsafe) |
| P4-S5 | COMPLETE | [`stock-ledger-P4-S5-implementation-summary.md`](./stock-ledger-P4-S5-implementation-summary.md) — Native→VB6→sync coexistence + AlternatingWriters + exit report [`stock-ledger-phase4-implementation-report.md`](./stock-ledger-phase4-implementation-report.md) |

---

## 1. Purpose

Convert the Phase 4 roadmap into **small, independently executable slices** that a mid-level coding agent can implement with minimal ambiguity.

Phase 4 proves that **one new-system transaction — DO Receipt / legacy DM equivalent — can execute Stock Ledger semantics while also producing the required authoritative legacy-compatible stock consequence**, behind a **disabled-by-default** capability flag.

Phase 4 does **not**:

- migrate outbound / transfer / reservation / returns;
- introduce Stage C cutover or `IsAuthoritative`;
- claim production mixed-writer safety (FQ-06 / G-17 → Phase 9);
- require all outbound paths to migrate before receipt can exist as a technical capability;
- add production HTTP endpoints merely to expose the capability.

**Success meaning (locked):**

```text
Technical capability complete ≠ safe for unrestricted production use
```

---

## 2. Code-base evaluation after Phase 3 (do not redesign)

Inspect the repository before each slice. The Phase 4 roadmap remains directionally correct, but **most Stock Ledger foundation already exists**. Do not rebuild parallel stacks.

### 2.1 Already delivered — reuse, do not redo

| Asset | Repository state | Phase 4 implication |
|---|---|---|
| Feature folder | `InventoryContext/StockLedgerFeature/` across Domain / Application / Infrastructure / SqlDb / Test | Extend only |
| Receipt Domain | `StockMovementModel.CreateReceipt`, `Reverse`, `Correct`; `StockLayerModel.Create`; `StockFactOriginEnum.Native` | Express Native receipt / void without new receipt-specific Domain aggregates |
| Idempotency | `BILRG_StokSourceIdempotency` + `SourceConsequence` / `SyncBatch` kinds; UoW short-circuits duplicates | Native receipt uses `SourceConsequence`; do not invent a second key store |
| Consequence UoW | `IStockConsequenceUnitOfWork` / `StockConsequenceUnitOfWork` already commits idempotency → Movement → Position → Scope → **optional** `ILegacyCompatibilityWriterPort.Apply` inside one ambient `IUnitOfWork` | Phase 4 makes the legacy side **real**; do not invent a second TX boundary |
| Compatibility port | `ILegacyCompatibilityWriterPort` + `LegacyCompatibilityWriteRequest` (balance + journal shapes) | Implement live adapter; extend request DTOs only if DM evidence requires fields the port cannot express |
| Fake writer | `FakeLegacyCompatibilityWriterPort` (tests) | Keep for non-legacy unit tests; live path uses real adapter |
| Legacy reads | `LegacyStockReadPort` (G-05), fingerprint `LegacyReconstructionBasisCalculator` / `fingerprint-v1` | Post-receipt Scope position init + coexistence proofs |
| Phase 3 sync | Live discovery, delta interpreter, reconcile, `SynchronizeStockLedgerScopeHandler`, `LegacyStockFreshnessGate`, sync explainability | **Consume** for later-VB6 catch-up; do not reimplement |
| Coexistence harness | Active Legacy→New / duplicate / mismatch / .NET sync race; skipped `NewToLegacy`, `AlternatingWriters`, `PartialFailure_LegacyAndLedger`, `ConcurrentOutbound` | Un-skip only Phase-4-owned scenarios |
| Legacy DAL primitives | `tb_stok_dal` / `tb_buku_dal` under `StokFeature` | Reuse field knowledge; **do not** blindly reuse `SqlBulkCopy` buku insert for short ambient TX enlistment |
| Production DI / HTTP | None for Stock Ledger FO writes | Keep none for production FO traffic; capability remains off |

### 2.2 Still missing — Phase 4 owns

1. Live `ILegacyCompatibilityWriterPort` that writes authoritative `tb_stok` / `tb_buku` (and any DM-evidenced source writebacks) enlisted in the ambient consequence TX.  
2. Characterized **minimum DM compatibility contract** (post + void) grounded in `GenStokDO` / `GenStokDOVoid` / `AddStok` / `RemoveStok`, not a port of `clbGenStokX1`.  
3. Native DO Receipt Application UseCase that builds Native Movement/Layer/Position, initializes coexistence Scope + Synchronization Position, and commits through the existing UoW with a real legacy write.  
4. Small Domain Scope transition for **native-established baseline** (today only `CompleteReconstruction` reaches `Reconstructed` from `Reconstructing`).  
5. Capability / FO-type flag (disabled by default) gating the UseCase.  
6. Failure-injection proof that live legacy + Ledger never partially commit.  
7. Minimal safe receipt void/correction (or explicit fail-closed deferral).  
8. Activation of Phase-4 G-23 scenarios: New→Legacy visibility; New→VB6-change→Phase-3-sync→new touch; capability disabled continuity.  
9. Phase 4 exit report + residual handoff to Phase 5 / Phase 9.

### 2.3 Roadmap / planning misalignment corrections

| Earlier assumption | Actual post–Phase-3 state | Plan adjustment |
|---|---|---|
| Phase 4 must invent consequence UoW | `StockConsequenceUnitOfWork` already coordinates Ledger + optional legacy Apply | Wire live writer; extend only if enlistment/order gaps appear |
| Phase 4 must invent Freshness/sync | Phase 3 complete | Consume gate/sync; do not duplicate |
| Native receipt needs reconstruction first | Roadmap: reconstruction **not** required for a **new** DO | Establish Scope baseline from Native receipt + post-write fingerprint; do not force Phase A–C reconstruction for empty history |
| Port request already covers all FO writebacks | `LegacyCompatibilityWriteRequest` has balance + journal only | Characterize DM: `GenStokDO` evidenced path is **AddStok only** (no separate `tb_trs_do`/`tb_po` mutation in the stock script). Prefer filling HPP/PO/DO on stock rows; extend port only if further writebacks are evidenced as required for readers |
| Eight slices like Phase 1–3 | Receipt is one FO family; sync stack already exists | **Five** risk-driven slices (see §6) |
| Production DI for FO | Still absent by design through Phase 3 | Optional test/composition root only; **no** public write endpoints; capability default **false** |

### 2.4 Phase 3 caller contract Phase 4 must honor (from P3-S6)

Native consequence UoW / FO writers **must**:

1. Not allocate from Stock Ledger layers until Freshness Gate returns `Current` or `SynchronizedNow` (`IsSafeToTrustLedgerLayers == true`).  
2. Serialize native write boundaries with synchronization claim / Scope synchronization state.  
3. Honor `StaleOrNotCurrent` / `Inconsistent` as fail-closed for Ledger-dependent decisions.  
4. Never treat provisional G-08 Availability Discovery as Ledger authority.

**Receipt-specific nuance:** a brand-new DO Receipt does **not** allocate from existing layers. Freshness Gate is therefore:

| Situation | Required behavior |
|---|---|
| Scope absent / `NotReconstructed` and no prior Ledger/legacy history for Item+DO | Proceed to create Native receipt + legacy rows; initialize Scope baseline + `fingerprint-v1` position **after** authority rows are visible to the calculator inputs (same short TX commit; compute position from the draft/post-write snapshot that the TX is about to make durable — see P4-S2) |
| Scope already `Reconstructed` / has position (prior activity on same Item+DO) | Run Freshness Gate **before** trusting layers; fail closed if not current. Do not treat later VB6 activity as an authority violation |
| Scope `Inconsistent` | Fail closed; no Native receipt mutation |

---

## 3. Locked decisions (do not reopen)

| Topic | Locked baseline |
|---|---|
| Runtime authority (Stage B) | `tb_stok` + `tb_buku` — Native receipt does **not** transfer authority |
| Origin labels | `Native` / `Reconstructed` / `LegacySynchronized` — origin only |
| Reconstruction / reconciliation scope | Item + Receipt Source across **all** Stock Locations |
| Write consistency candidate | Item + Receipt Source + Stock Location |
| Receipt Source for DM | DO identity (`fs_kd_do` / `fs_kd_trs` of DO) — known from source fact |
| Sync mechanism | Fingerprint + bounded replay; **exactly one** fingerprint authority: `LegacyReconstructionBasisCalculator` / `fingerprint-v1` |
| Void / delete handling | Never erase Stock Ledger history; use Reverse/Correct; legacy may insert `DO_V` or physically delete journals (`xVoidDelete`) |
| Lock order (interim) | Item → Receipt Source → Location → legacy row id ([concurrency ADR](./adr/ADR-stock-ledger-mixed-writer-concurrency.md)) |
| Mixed-writer production correctness | **Not** a Phase 4 claim. Live VB6/.NET proof remains Phase 9 (FQ-06 / G-17) |
| CT / CDC / brokers / event sourcing | Forbidden unless ADR amended |
| Stage C / ownership | Forbidden |

---

## 4. Explicit exclusions (later phases)

Do **not** implement in any Phase 4 slice:

- Outbound / MT transfer / FIFO allocation orchestration (Phase 5)
- Reservation / Virtual Stock Location (Phase 6)
- DU/DT/PK/MN/RB/RU/RT/AJ/RP families (Phase 7)
- Full G-24 ops dashboard / G-25 production SLOs (Phase 8)
- Live concurrent VB6 session proof / production mixed-writer enablement (Phase 9 / FQ-06)
- Public production HTTP stock-write endpoints
- Porting `clbGenStokX1` control flow into Domain/Application
- Generic stock-processing frameworks, workflow/pipeline engines, CDC
- `IsAuthoritative`, per-Receipt-Source ownership, cutover state
- Claiming FQ-06 / production G-17 complete
- Enabling the capability by default in production configuration

---

## 5. Minimum DM / DO Receipt compatibility contract

Grounded in repository evidence (`clbGenStokX1.cls` `GenStokDO` / `GenStokDOVoid` / `AddStok` / `RemoveStok`, Phase 0 writer inventory, existing `tb_stok_dal` / `tb_buku_dal`). Distinguish **consumer-dependent behavior** from **incidental mechanics**.

### 5.1 Behavior legacy consumers depend on (must preserve)

| Concern | Evidenced contract |
|---|---|
| Entry | FO prefix `DM` → `GenStokDO`; void → `GenStokDOVoid` |
| Source read | Lines from `tb_trs_do` / `tb_trs_do2` (barang, layanan, qty, satuan, ED, batch, tgl/jam trs, PO, harga/diskon/tax) |
| Receipt Source | `fs_kd_do` = DO transaction id (`xKodeTrs`); also passed as mutasi id |
| Mutation jenis | Post journal `fs_kd_jenis_mutasi = "DO"` (`MUTASI_DO`); void compensating `"DO_V"` |
| Mutation id | `fs_kd_mutasi` = DO `fs_kd_trs` |
| Quantity unit | Converted to smallest unit before stock write (`ConvToTerkecil`) |
| Valuation / HPP | Computed from harga/diskon/tax using `MetodePersediaanHPP` (`HPP` / `HPP_DIS` / `HPP_DIS_TAX` / else); stored on **both** `tb_stok.fn_hpp` and `tb_buku.fn_hpp` |
| `tb_buku` post | Insert inbound journal: `fn_stok_in = qty`, `fn_stok_out = 0`, ED, batch, PO, DO, mutasi, tgl/jam, satuan; new `fs_kd_trs` (`BK` + counter) |
| `tb_stok` post | Insert balance row: `fn_qty = fn_qty_in = qty`, HPP, PO, DO, mutasi, tgl/jam, ED, batch, satuan; new `fs_kd_trs` (`ST` + counter) |
| Dates/times | `fd_tgl_mutasi` / `fs_jam_mutasi` from DO transaction; ED as `fd_tgl_ed` |
| Combined mutasi time | Production snapshot keeps `fd_tgl_jam_mutasi` populated; writer must leave readers/fingerprint inputs coherent (populate combined column when the schema expects it — do not rely on undocumented defaults alone) |
| `tb_stok` post shape | Always **INSERT** a new `ST*` row per FO line — never merge/update an existing balance on receipt |
| Void (`xVoidDelete = False`) | `RemoveStok` with `MUTASI_DO_VOID`: FIFO-select `tb_stok` by barang+layanan+ED (+ `fs_kd_do` for DO void), reduce/delete balance, insert compensating `DO_V` journal outbound |
| Void (`xVoidDelete = True`) | Physical `DELETE` of matching `tb_buku` rows for mutasi/barang/(layanan)/ED; still mutates `tb_stok` |
| Zero-row deletion | Outbound/void depleting `fn_qty` to 0 **deletes** `tb_stok` row (Ledger retains depleted layers) |
| Post visibility | After commit, VB6 readers / later FO can consume the DO via normal `RemoveStok` FIFO |

### 5.2 Incidental mechanics (do not port as Domain)

| Mechanic | Treatment |
|---|---|
| VB6 `Generate` router / `Case TRS_DO` | Compatibility reference only |
| String-concat SQL / `My.SQLGenerator2` | Replace with parameterized Dapper SQL enlisted in ambient TX |
| Entire `clbGenStokX1` control flow | Forbidden in Domain/Application |
| `SqlBulkCopy` buku path in existing DAL | Prefer single-row parameterized insert for consequence TX enlistment |
| Closing-stok comment / unimplemented reject | Out of Phase 4 unless evidenced as active hard dependency |
| Separate PO/DO header table updates inside `GenStokDO` | **Not evidenced** in the stock script — do not invent writebacks; HPP/PO/DO on `tb_stok`/`tb_buku` are the DM stock consequence |
| Batch as FIFO selection key | Stored on rows but **not** used in `RemoveStok` WHERE — do not invent batch-keyed selection for DM void |
| `fd_tgl_do` / `fs_jam_do` | VB6 `AddStok` does **not** set them (schema defaults `3000-01-01` / `00:00:00`). Current `LegacyStockReadPort` maps `ReceiptTime` from these columns and `LastMutationTime` from mutasi parts. **P4-S1 decision required:** either (preferred for VB6 parity) leave defaults like VB6 and, if Phase 2/3 fingerprint/receipt-time semantics break, adjust the **read adapter** to prefer mutasi time for receipt effective time — or consciously populate `fd_tgl_do`/`fs_jam_do` from FO receipt time and document the intentional deviation from VB6. Do not silently diverge without a test asserting `LegacyStockReadPort` / fingerprint behavior. |

### 5.3 Source Business Fact boundary

Stock Ledger does **not** own Purchasing/Goods Receipt. The Phase 4 UseCase accepts an **already-authorized** DO Receipt source fact (DO id, lines with item, location, qty in stock unit, ED, batch, PO, unit valuation or price components needed for HPP). Reading `tb_trs_do*` may live in an Infrastructure adapter for tests/integration, but Domain must not embed FO UI workflow.

---

## 6. Slicing decision

### Decision

Phase 4 is divided into **five incremental slices (P4-S1…P4-S5)**.

### Why not eight slices

Phases 1–3 already delivered Domain, persistence, ports, UoW, reconstruction, discovery, sync, Freshness Gate, and harness scaffolding. Repeating an eight-slice “layer tour” would create artificial slices without independent acceptance criteria.

### Why not three slices

Void semantics and New→Legacy→sync coexistence are high-risk questions that fail for different reasons than “can we insert a `tb_stok` row?” Merging them with the first live writer produces unreviewable partial enablement.

### Why this order

| Risk / question | Slice |
|---|---|
| Can the compatibility boundary express real DM post consequences without Domain leakage? | **P4-S1** (implementation gate) |
| Can Native Movement/Layer + Scope baseline + legacy write commit as one consequence? | **P4-S2** |
| Can failure at any persistence step leave neither representation partially committed? Is the receipt visible to legacy readers? | **P4-S3** |
| Can void/correction preserve Ledger history and legacy compatibility without inventing unsafe deletes? | **P4-S4** |
| Does later VB6 activity catch up via Phase 3, and can the capability stay disabled safely? | **P4-S5** |

### Slice map (execute in order)

```text
P4-S1 Live DM Legacy Compatibility Writer (post)     << implementation gate
   -> P4-S2 Native DO Receipt UseCase + Scope baseline + capability flag
   -> P4-S3 Atomicity / failure injection + New→Legacy harness
   -> P4-S4 Receipt void/correction (fail closed when unsafe)
   -> P4-S5 Phase 3 coexistence proof + exit hardening + report
```

### P4-S1 implementation gate (checkpoint, not a new phase)

**If P4-S1 cannot produce disposable-DB `tb_stok` / `tb_buku` rows that match the §5 consumer-dependent contract** (readable by `LegacyStockReadPort`, coherent HPP/PO/DO/mutasi/ED, enlisted rollback), then:

1. **Stop.** Do not start P4-S2…P4-S5 Native UseCase work.  
2. Record failure evidence in the P4-S1 summary.  
3. Fix the compatibility adapter / contract before orchestration proceeds.

---

## 7. Slice specifications

### P4-S1 — Live DM Legacy Compatibility Writer (receipt post)

| Field | Detail |
|---|---|
| **Slice ID / name** | P4-S1 — Live DM Legacy Compatibility Writer (receipt post shape) |
| **Problem / question being solved** | Can the new application reproduce the authoritative DM receipt consequence (`tb_stok` insert + `tb_buku` inbound journal + HPP/PO/DO/mutasi/ED) through the existing `ILegacyCompatibilityWriterPort` boundary without leaking VB6 control flow into Domain? |
| **Objective** | Deliver a live Infrastructure adapter that applies DM **post** shapes from `LegacyCompatibilityWriteRequest`, enlisted in the caller’s ambient SQL transaction, proven on disposable/test SQL. |
| **Why this slice exists independently** | If legacy write shapes are wrong, every later Native UseCase test is meaningless. This is the Phase 4 implementation gate. |
| **Dependencies** | Phase 1 port + UoW; Phase 2 G-05 reads (for assertion); Phase 0 DM characterization; `clbGenStokX1` reference |
| **Current code to reuse** | `ILegacyCompatibilityWriterPort`, `LegacyCompatibilityWriteRequest` / balance+journal DTOs, `StockConsequenceUnitOfWork` enlistment pattern, `tb_stok_dto` field list, `LegacyStockReadPort` for read-back asserts, `INunaCounterDal` / `ParamNoDal` patterns for ST/BK ids if appropriate, `FakeLegacyCompatibilityWriterPort` for non-live tests |
| **Expected affected projects/areas** | Infrastructure (`StockLedgerFeature` writer adapter; optional thin helpers for ST/BK id + HPP), Application only if request DTOs need additive fields, Test, docs (contract notes in summary) |
| **Implementation scope** | (1) Document §5 contract in slice summary with A/B distinction; (2) live `Apply` for DM post: insert `tb_buku` then `tb_stok` (VB6 order), **always INSERT** (never merge), generate `BK*`/`ST*` ids (`BK`/`ST` + 8 zero-padded digits via shared counters), set jenis `DO` (not `DM`), mutasi=DO id, qty in, HPP, ED, batch, PO, DO, satuan terkecil, mutasi times + coherent `fd_tgl_jam_mutasi`; (3) resolve `fd_tgl_do`/`fs_jam_do` per §5.2 decision and prove `LegacyStockReadPort` + fingerprint behavior; (4) ambient TX enlistment; (5) reject unsupported FO/movement kinds explicitly (no silent no-op); (6) characterization tests on `devTest` only |
| **Explicit exclusions** | Native Movement/Layer UseCase; void path; Freshness Gate wiring; production DI/HTTP; porting `Generate`; outbound FO families; inventing PO header / `tb_trs_do*` writebacks not evidenced in `GenStokDO` |
| **Database impact** | Writes disposable-test `tb_stok` / `tb_buku` only. No new BILRG tables required. Prefer no production `HOSPITAL_HPL` writes. |
| **Tests** | Apply DM post → read-back via `LegacyStockReadPort` matches qty/HPP/DO/location/ED; `ReceiptTime`/`LastMutationTime` coherent under the chosen `fd_tgl_do` policy; rollback when outer UoW does not `Complete`; unsupported movement kind fails explicitly; id uniqueness for inserted rows; fingerprint calculator can hash the resulting snapshot |
| **Acceptance criteria** | Gate PASS: consumer-dependent DM post contract satisfied on disposable DB; `fd_tgl_do` policy decided and tested; adapter stays in Infrastructure; Domain remains SQL-free; no production enablement |
| **Rollback / containment** | Remove/disable live adapter registration; keep port + fake; leave additive Ledger unused |
| **Risks** | Counter concurrency (FQ-02 residual); `fd_tgl_jam_mutasi` population gap; `fd_tgl_do` default vs read-adapter mismatch; bulk-copy non-enlistment; accidental Domain leakage of VB6 helpers |
| **Deliverables** | Live writer (post) + tests + `stock-ledger-P4-S1-implementation-summary.md` with **gate PASS/FAIL** |

---

### P4-S2 — Native DO Receipt consequence UseCase + Scope baseline + capability gate

| Field | Detail |
|---|---|
| **Slice ID / name** | P4-S2 — Native DO Receipt consequence |
| **Problem / question being solved** | Can a DO Receipt be represented deterministically as a Native Stock Movement / Stock Layer, initialize coexistence Scope + Synchronization Position without authority semantics, and commit with the P4-S1 legacy consequence through the existing UoW — behind a disabled-by-default capability flag? |
| **Objective** | Thin MediatR/Inventory-style UseCase that posts one DO Receipt consequence when the capability is enabled. |
| **Why this slice exists independently** | Separates “legacy shapes work” (S1) from “Native semantics + Scope baseline + flag” so Domain/Application orchestration can be reviewed without reopening DAL SQL. |
| **Dependencies** | P4-S1 **gate PASS**; Phase 1 Domain factories + UoW; Phase 2/3 fingerprint calculator; Phase 3 Freshness Gate (for already-baselined scopes) |
| **Current code to reuse** | `StockMovementModel.CreateReceipt`, `StockLayerModel.Create`, Position repos, `StockConsequenceDraft` / UoW, `LegacyReconstructionBasisCalculator`, `LegacyStockFreshnessGate`, Scope state model (extend), MediatR handler style of `ReconstructStockLedgerBaselineCommand` / `SynchronizeStockLedgerScopeCommand`, options pattern like `AdmisiRanap:Enabled` |
| **Expected affected projects/areas** | Domain (small Scope transition only), Application (UseCase + capability options), Infrastructure (DI for tests/composition if needed — not production FO HTTP), Test |
| **Implementation scope** | (1) Command accepting authorized DO Receipt source fact (multi-line allowed); (2) build Native-origin receipt movement + layers/positions; (3) map to `LegacyCompatibilityWriteRequest`; (4) short TX via existing UoW; (5) initialize Scope to baselined/`Reconstructed` + `Current` + `fingerprint-v1` position for new DO (add `EstablishFromNativeReceipt` or equivalent — **not** a fake Phase A–C reconstruction); (6) ensure discovery identity coverage after Native establish so later Phase 3 set-diff works — reuse/adapt `LegacySyncIdentityBootstrapper` / `SYNC\|BUKU\|…` / `SYNC\|STOK\|…` patterns rather than inventing a second key scheme; (7) if Scope already baselined, Freshness Gate first; (8) `SourceConsequence` idempotency key derived from source responsibility (DO + stable line identity); (9) capability flag default **false** — handler fails closed / no-ops as “disabled” without writing; (10) honor P3-S6 serialization notes (no allocate-from-stale-layers) |
| **Explicit exclusions** | Void; failure-injection matrix (P4-S3); production HTTP; outbound; claiming FQ-06; enabling flag in production appsettings |
| **Database impact** | Additive Ledger rows + legacy stock rows on disposable DB when tests enable the flag |
| **Tests** | Happy-path Native origin + legacy rows; capability disabled ⇒ no Ledger/legacy writes; duplicate source responsibility ⇒ `AlreadyCommitted` / quantity-neutral; Scope position algorithm version `fingerprint-v1`; no `IsAuthoritative`; already-baselined stale scope fails closed via Freshness Gate |
| **Acceptance criteria** | G-19 core post path works behind flag; Native origin visible; authority remains legacy; UoW remains the single short TX; no generic framework introduced |
| **Rollback / containment** | Keep flag false; remove handler registration; committed test rows only on disposable DB |
| **Risks** | Misusing `CompleteReconstruction` without claim; treating Native as authority; computing fingerprint with a second hasher; long TX around DO discovery |
| **Deliverables** | UseCase + Scope transition + capability options + tests + `stock-ledger-P4-S2-implementation-summary.md` |

---

### P4-S3 — Atomicity / failure injection + New→Legacy harness

| Field | Detail |
|---|---|
| **Slice ID / name** | P4-S3 — Live consequence atomicity + New→Legacy visibility |
| **Problem / question being solved** | Can a failure at any important persistence step prove that receipt consequences never partially commit, and can authoritative legacy rows written by the new system be seen by legacy-shaped readers (G-23 New→Legacy)? |
| **Objective** | Prove G-18 with the **live** writer (not only the P1-S8 fake) and activate the New→Legacy coexistence harness scenario. |
| **Why this slice exists independently** | Happy-path Native receipt can pass while enlistment/order bugs leave orphan Ledger or orphan `tb_stok`. This risk needs dedicated failure injection. |
| **Dependencies** | P4-S1, P4-S2 |
| **Current code to reuse** | `StockConsequenceUnitOfWork`, live writer, `StockLedgerCoexistenceHarnessPlaceholderTest.NewToLegacy_ReceiptVisibleInLegacyAuthority`, `StockConsequenceUnitOfWorkTest` failure style, disposable schema fixture |
| **Expected affected projects/areas** | Test (primary); Infrastructure/Application only if a real enlistment bug is found |
| **Implementation scope** | (1) Failure injection at: idempotency after-insert, movement save, position save, scope save, legacy Apply, and (if feasible) mid-legacy multi-row write; (2) assert **zero** durable Ledger + legacy residue on rollback; (3) un-skip/implement New→Legacy harness: new receipt ⇒ `LegacyStockReadPort` / legacy DAL sees qty; (4) concurrent duplicate request quantity-neutral if practical with existing idempotency; (5) document write order actually used |
| **Explicit exclusions** | Void; full alternating VB6 binary; FQ-06; Phase 5 concurrent outbound; production enablement |
| **Database impact** | Disposable DB only |
| **Tests** | Each injected failure rolls back both representations; New→Legacy visibility; replay remains quantity-neutral after successful commit; prior StockLedgerFeature suite stays green |
| **Acceptance criteria** | G-18 live-writer acceptance; G-23 New→Legacy activated; no partial commit observed |
| **Rollback / containment** | Re-skip harness if unstable; keep UseCase behind flag |
| **Risks** | Connection/enlistment mismatch; counters incrementing outside rolled-back TX (document if counter behavior cannot be rolled back — fail closed or compensate explicitly) |
| **Deliverables** | Atomicity tests + activated New→Legacy harness + `stock-ledger-P4-S3-implementation-summary.md` |

---

### P4-S4 — Receipt void / correction

| Field | Detail |
|---|---|
| **Slice ID / name** | P4-S4 — Receipt void/correction |
| **Problem / question being solved** | How should receipt void/correction be represented so Stock Ledger history stays immutable, legacy-compatible authoritative records remain correct, Remaining Quantity and valuation stay coherent, and retries are idempotent — without treating void as deleting the original Native movement? |
| **Objective** | Minimal safe DM void path for Native-posted receipts; fail closed when evidence is ambiguous or stock was already consumed. |
| **Why this slice exists independently** | Void semantics (`DO_V` vs `xVoidDelete`, depleted layers vs deleted `tb_stok`) are a distinct risk from posting. Inventing void inside S2 invites unsafe deletes. |
| **Dependencies** | P4-S1–S3; Domain `Reverse`/`Correct`; Phase 3 void→correction interpretation as behavioral reference (do not call sync to “implement” void) |
| **Current code to reuse** | `StockMovementModel.Reverse` / `Correct`, layer remaining updates via new movements, live writer extended for void journal/balance mutations, `RemoveStok` characterization for `MUTASI_DO_VOID`, SyncBatch/SourceConsequence idempotency patterns |
| **Expected affected projects/areas** | Infrastructure (writer void branch), Application (VoidDoReceipt UseCase), Domain only if needed, Test |
| **Implementation scope** | (1) Prefer accountable **Reversal** of the original Native receipt movement (`Native` or appropriate origin on the new movement — do not erase original); (2) legacy side: compensating `DO_V` path as default Phase 4 void mode unless tests prove `xVoidDelete=True` is required for DM callers — if delete-mode is required, implement only with deletion-aware sync compatibility already proven in Phase 3; (3) reduce/delete `tb_stok` per legacy rules; (4) idempotent void retry; (5) **fail closed** if remaining authoritative qty is insufficient / already consumed by later activity / multi-line ambiguity cannot be targeted safely (mirror R-007 spirit); (6) refresh Scope Synchronization Position with `fingerprint-v1` after successful void commit |
| **Explicit exclusions** | Generic void framework for all FO families; deleting Ledger history; silent force-balance; production enablement of delete-void if unsafe; inventing return/repack voids |
| **Database impact** | Disposable DB legacy + Ledger writes |
| **Tests** | Void after receipt restores zero remaining; original movement retained; repeated void quantity-neutral; insufficient stock fail closed; optional `xVoidDelete` only if explicitly supported with tests; capability disabled blocks void |
| **Acceptance criteria** | History retained; legacy readers see void consequence; unsafe cases fail closed rather than invent behavior |
| **Rollback / containment** | Disable void UseCase/flag branch; leave post path intact |
| **Risks** | Partial consumption after Native receipt; multi-location DO lines; delete-void sync surprises; over-general void engine |
| **Deliverables** | Void UseCase + writer void support + tests + `stock-ledger-P4-S4-implementation-summary.md` (explicit deferrals listed) |

---

### P4-S5 — Phase 3 coexistence proof + exit hardening

| Field | Detail |
|---|---|
| **Slice ID / name** | P4-S5 — Coexistence proof + Phase 4 exit |
| **Problem / question being solved** | After a Native DO Receipt, can later VB6-shaped consumption/transfer be detected and synchronized by Phase 3 mechanisms before the next Ledger-dependent operation — and can disabling the capability leave legacy operation unaffected? |
| **Objective** | Prove the normative coexistence sequence; activate remaining Phase-4 harness ownership; publish Phase 4 exit report. |
| **Why this slice exists independently** | This is the Phase 4 *reason for existing*: Native is not authority, and Phase 3 catch-up must work on Native-origin scopes. Keeping it last avoids claiming coexistence before void/atomicity exist. |
| **Dependencies** | P4-S1–S4; Phase 3 Freshness Gate + sync handler + discovery + reconcile |
| **Current code to reuse** | `LegacyStockFreshnessGate`, `SynchronizeStockLedgerScopeHandler`, coexistence harness placeholders (`AlternatingWriters`), `StockLedgerSyncExplainability`, fingerprint continuity |
| **Expected affected projects/areas** | Test + docs (primary); tiny Application glue only if Native Scope bootstrap omitted identity keys needed by discovery (prefer fix in S2 if found earlier) |
| **Implementation scope** | (1) Scenario: Native receipt → legacy authority rows → simulate VB6 consume/transfer on disposable fixtures → next touch runs Freshness Gate → sync catch-up → reconcile succeeds → processing can continue; (2) do **not** treat VB6 activity on Native-origin layer as error/authority violation; (3) activate or document `AlternatingWriters` to the extent Native writer + fixture simulation allow (**not** live VB6 binary); (4) capability disabled ⇒ legacy-only path unaffected (no new writes); (5) Phase 4 implementation/exit report; (6) update plan progress + ARTIFACTS; (7) restate FQ-06 non-claim |
| **Explicit exclusions** | Live VB6 concurrent sessions; Phase 5 outbound; production flag enablement; Stage C |
| **Database impact** | Disposable fixtures only |
| **Tests** | Coexistence sequence above; sync position advances with `fingerprint-v1`; Freshness Gate `SynchronizedNow`/`Current`; capability-off continuity; StockLedgerFeature suite green |
| **Acceptance criteria** | Roadmap Phase 4 validation scenarios for sync-after-native proven; exit checklist complete; capability remains default-off |
| **Rollback / containment** | Keep capability false; Phase 3 sync remains usable independently |
| **Risks** | Missing discovery identity bootstrap after Native establish; false Authority Gate regression; overclaiming mixed-writer safety |
| **Deliverables** | Coexistence tests + `stock-ledger-P4-S5-implementation-summary.md` + `stock-ledger-phase4-implementation-report.md` |

---

## 8. Atomic consequence boundary (normative)

One successful DO Receipt (capability enabled) must commit **all applicable** consequences in **one short SQL transaction** via `IStockConsequenceUnitOfWork` (ambient `IUnitOfWork` / `TransHelper`):

```text
Begin ambient TX
  -> Insert SourceConsequence idempotency (duplicate => AlreadyCommitted, no rewrite)
  -> Persist Native Stock Movement (+ lines)
  -> Persist Stock Position / Layers
  -> Persist Scope coexistence state (native baseline + Synchronization Position when ready)
  -> ILegacyCompatibilityWriterPort.Apply (tb_stok + tb_buku [+ evidenced writebacks only])
  -> Complete
```

**Invariant:** A successful new-system receipt must never leave the Stock Ledger representation committed without its required legacy-authoritative consequence, or vice versa.

**Write-order note:** P1-S8 currently persists Ledger pieces before legacy Apply. That remains acceptable **if and only if** failure before `Complete` rolls back **both**. P4-S3 must prove this with the live writer. If evidence shows legacy readers can observe Ledger-only intermediate state outside the TX (they should not under a single ambient TX), adjust order inside the UoW with an explicit deviation note — do not split into two commits.

**Keep outside the short TX:** unnecessary DO discovery, HPP policy loading, Freshness Gate discovery/catch-up (catch-up has its own TX), Availability Discovery.

---

## 9. Capability flag and rollout boundary

| Topic | Phase 4 rule |
|---|---|
| Mechanism | Explicit options object (e.g. `StockLedgerDoReceiptOptions.Enabled` / FO-type capability) following existing repo patterns (`AdmisiRanap:Enabled`) |
| Default | **`false`** |
| Production appsettings | Must not silently enable |
| HTTP | **No** public production write endpoint required for Phase 4 exit |
| Composition | Tests may construct handler + live writer manually (Phase 3 style). Optional non-production DI is allowed only if needed for tests — not an enablement signal |
| Phase 4 complete means | Technical slice proven behind flag |
| Still gated | Phase 5 outbound; Phase 9 mixed-writer / coexistence production rollout (FQ-06) |

---

## 10. Testing philosophy

Prefer integration tests against disposable/test SQL (`devTest` / `StockLedgerSchemaFixture`). **Never** write Phase 4 test data to production legacy stock (`HOSPITAL_HPL`).

| Area | Required coverage (Phase 4 whole) |
|---|---|
| Normal DO Receipt | Native origin + legacy rows |
| Duplicate / replay | Quantity-neutral |
| Concurrent duplicate | If practical with existing idempotency |
| Failure injection | Each important persistence boundary |
| Rollback | Neither representation partially committed |
| Legacy visibility | New→Legacy harness |
| Void / retry | P4-S4; fail closed when unsafe |
| Coexistence | Native → VB6-shaped change → Phase 3 sync → new touch |
| Capability off | No new writes; legacy path unaffected |
| Non-claims | No FQ-06 / production G-17 claim |

---

## 11. Documentation obligations (every slice)

Each implementation slice must:

1. Produce a short `stock-ledger-P4-S{n}-implementation-summary.md`.  
2. Update this plan’s **Slice progress** table.  
3. Update [`docs/ARTIFACTS.md`](../../ARTIFACTS.md) when new summaries/reports are added.  
4. Record deviations from this plan based on actual codebase findings.  
5. Avoid silently changing locked domain or ADR decisions.

At Phase 4 completion, publish `stock-ledger-phase4-implementation-report.md` covering:

- what was implemented;
- coexistence scenarios proven;
- what remains intentionally disabled;
- residual risks;
- dependencies carried into Phase 5 / Phase 9.

---

## 12. Phase 4 exit checklist

- [x] **P4-S1 gate passed:** live DM post compatibility writer accepted before Native UseCase enablement work proceeds.  
- [x] **G-11 accepted for DM post (and void if P4-S4 delivers):** legacy readers can consume new-system receipt/void consequences; delete-on-zero remains compatible; compatibility failure rolls back the complete consequence.  
- [x] **G-18 accepted with live writer:** failure injection at persistence boundaries rolls back Ledger + legacy together; retry commits once.  
- [x] **G-19 accepted as technical capability:** idempotent Native DO Receipt; Scope baseline + `fingerprint-v1` position; no authority/cutover fields.  
- [x] **Void:** accountable Ledger reversal/correction retained **or** explicitly deferred with fail-closed rationale in the exit report.  
- [x] **Phase 3 integration:** Native → later legacy change → Freshness Gate / sync catch-up → continue; Native-origin is not treated as VB6 prohibition.  
- [x] **G-23 Phase-4 markers:** New→Legacy activated; PartialFailure covered; AlternatingWriters activated to fixture-simulated extent or explicitly deferred with owner.  
- [x] **Capability flag default false;** no public production write endpoint required.  
- [x] **Explicit non-claim:** FQ-06 / production G-17 **not** done.  
- [x] **No** `IsAuthoritative`, Stage C, generic stock framework, or `clbGenStokX1` port.  
- [x] Solution builds; StockLedgerFeature tests green for Phase 4 slices; Phase 1–3 tests remain green.  
- [x] Phase 4 implementation report published; ARTIFACTS updated.

---

## 13. Residual risks / handoff

| Residual | Owner | Blocks Phase 4 coding start? |
|---|---|---|
| FQ-06 live VB6 concurrency proof / production G-17 | Phase 9 | **No** |
| G-25 proposed legacy indexes / p95 SLOs | Phase 8 / DBA | **No** |
| Legacy stock id counter multi-instance atomicity (FQ-02) | Characterize in P4-S1/S3; production hardening may continue | **No** for coding start; may constrain enablement |
| Full FO family matrix | Phases 5–7 | **No** |
| Production capability enablement | Phase 9 | **No** (must remain off at Phase 4 exit) |

---

## 14. Engineer checklist per slice

- [ ] Inspect current Stock Ledger code before coding; extend, do not fork.  
- [ ] `tb_stok + tb_buku` remain Stage B authority.  
- [ ] `Native` is origin only.  
- [ ] No `IsAuthoritative` / ownership / Stage C.  
- [ ] Short TX; no unnecessary work inside TX.  
- [ ] Fingerprint continuity: only `fingerprint-v1`.  
- [ ] Freshness Gate / sync reused, not reimplemented.  
- [ ] Capability remains default-off.  
- [ ] Disposable DB only for legacy writes in tests.  
- [ ] Slice summary + plan progress + ARTIFACTS updated.  
- [ ] No FQ-06 production claim.

---

## 15. Suggested first implementation action

Start **P4-S1** by writing the DM post compatibility characterization into the slice summary draft, then implement the live `ILegacyCompatibilityWriterPort` adapter against disposable SQL and run the gate tests. Do not begin the Native UseCase until the P4-S1 gate is **PASS**.
