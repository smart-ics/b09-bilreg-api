# Stock Ledger — Implementation Feasibility Review

**Artifact status:** Implementation-oriented feasibility review  
**Date basis:** Working tree inspection of `b09-bilreg-api` (August 2026)  
**Canonical business truth:** [`stok-ledger-domain.md`](./stok-ledger-domain.md)  
**Legacy behavior reference:** [`clbGenStokX1.cls`](./clbGenStokX1.cls) (VB6 Transaction Script — extract behavior only; do not port)  
**Companion artifacts:** [`stock-ledger-gap-analysis.md`](./stock-ledger-gap-analysis.md), [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md)

---

## 0. Executive verdict

| Question | Answer |
|---|---|
| Is the **domain document** realizable? | **Yes** — lazy reconstruction per Item + Receipt Source (`Barang + DO`) is feasible and is the correct migration strategy. |
| Is the **current C# implementation direction** sufficient to realize that domain? | **Not yet.** Current code is a partial kartu-stok / FEFO slice. Aggregate boundaries, FIFO policy, movement model, reconstruction, reconciliation, dual-write, and write-side use cases are incomplete or misaligned. |
| Can work continue from what exists? | **Yes**, but only after a deliberate **domain-model realignment** (Phase 0–1 in the roadmap). Do not extend `StokModel(BrgId, LayananId)` as the Stock Position aggregate root. |
| Safest path? | Keep legacy `tb_stok` / `tb_buku` + VB6 `Generate` as the live writer until a dual-write consequence pipeline exists; build Stock Ledger as a parallel authoritative model with lazy bootstrap; never port `clbGenStokX1` Transaction Script into Domain/Application. |

---

## 1. Repository evidence snapshot

### 1.1 What exists and is readable on disk

| Area | Path evidence | Status |
|---|---|---|
| Domain models | `src/bilreg/Bilreg.Domain/InventoryContext/StokFeature/` | Present: `StokModel`, `StokLayerModel`, `StokBukuType`, `StokLotType`, `TrsReffType`, `LayananType`, `JenisLokasiType` |
| Application contracts | `.../Bilreg.Application/InventoryContext/StokFeature/` | Present: `IStokRepo`, `ILayananRepo`, `IJenisLokasiRepo`, read-only `StokGetKartuStokQuery` |
| API | `.../Bilreg.Api/Controllers/InventoryContext/StokFeature/StokController.cs` | GET kartu stok only; depends on `IStokRepo.LoadEntity` |
| Legacy DALs | `tb_stok_dal.cs`, `tb_buku_dal.cs` (+ DTOs) | Present; CRUD-ish for legacy tables |
| Location masters | `JenisLokasi*`, `Layanan*` | Present and wired |
| FARIN SQL | `FARIN_Stok.sql`, `FARIN_StokLayer.sql`, `FARIN_StokBuku.sql` | Schema present |
| Domain unit tests | `StokModelTest.cs` | Active; asserts **FEFO by ExpDate** |
| Legacy DAL tests | `tb_stok_dal_test.cs`, `tb_buku_dal_test.cs` | Present |
| Legacy VB6 | `docs/contexts/stok-ledger/clbGenStokX1.cls` | Present (~2090 lines); authoritative for current ops behavior |
| Domain docs | `stok-ledger-domain.md` / `-id.md` | Present and detailed |

### 1.2 Working-tree gap: FARIN persistence stack

Git status at review time marks these as **deleted** in the working tree (and `Read` confirms they are absent from disk):

* `StokRepo.cs`, `StokDal.cs`, `StokLayerDal.cs`, `StokBukuDal.cs` and related DTOs
* `StokDalTest.cs`, `StokLayerDalTest.cs`, `StokBukuDalTest.cs`
* ChargeContext `StokDal` / `StokRepo` (tarif list over `tb_stok`)

`StokRepoTest.cs` remains on disk but is **fully commented**. It sketches `RekonstruksiStok` scoped by `Brg + Layanan` and references a never-shipped `IStokBukuMapDal`.

**Implication:** Even before deletion, HEAD-era `StokRepo` was incomplete (`NotImplementedException` on public methods; reconstruction logic commented). There is **no live persistence path** for the new FARIN stock model today. `StokGetKartuStokQuery` cannot succeed against a real repository implementation until one is restored and completed.

### 1.3 Architectural shape of current domain code

```text
StokModel (treated as AR: BrgId + LayananId)
  └── StokLayerModel[]
        ├── StokLotType (PurchaseId, ReceiveId, ExpDate, BatchNo)
        ├── TrsReffType TrsReffIn
        └── StokBukuType[] (Masuk / Keluar lines)
```

This is a **location-scoped kartu stok**, not the domain’s Stock Position aggregate (`Item + Receipt Source` across locations).

---

## 2. Domain completeness

Compared to [`stok-ledger-domain.md`](./stok-ledger-domain.md) §5–§9 and BR-STL-* rules.

### 2.1 Aggregates

| Spec aggregate | Current code | Verdict |
|---|---|---|
| **Stock Position** (AR: Item + Receipt Source across locations) | `StokModel` keyed by Item + Location | **Misaligned** — must be redesigned |
| **Stock Movement** (immutable movement + lines) | Nested `StokBukuType` under layer only | **Missing** as aggregate |
| **Stock Reconciliation** | Absent | **Missing** |
| **Legacy Stock Reconstruction** | Absent in domain; commented infra sketch wrong scope | **Missing** |

### 2.2 Entities / value objects present vs needed

| Spec concept | Current mapping | Gap |
|---|---|---|
| Receipt Source | `StokLotType.ReceiveId` (≈ `fs_kd_do`) | No first-class VO; no completed-receipt rules |
| Stock Layer | `StokLayerModel` | Missing: Item/Location on entity (parent-only), Effective Receipt Time, Native vs Reconstructed flag, layer lifecycle, restore-to-same-layer |
| Stock Movement Line | `StokBukuType` | Partial line VO; no movement header, pairing, correction links |
| FIFO Allocation | Inlined in `StokModel.RemoveStok` | No allocation result object; policy is FEFO, not BR-STL-029/032 |
| Virtual Stock Location | Absent (`JenisLokasi` = GDN/APT/CAR/GIZ only) | Missing |
| Stock Consequence Request | Absent | Missing |
| Reconstruction Status / Migration watermark | Absent | Missing (user intent: `StockLedgerMigration` per Barang+DO) |
| Domain events (§9) | None | Missing |

### 2.3 Behaviors implemented today

| Behavior | Location | Notes |
|---|---|---|
| Add stock → new layer + masuk buku | `StokModel.AddStok` / `StokLayerModel.Create` | Always new layer; never restore depleted layer |
| Remove stock with overdraw guard | `StokModel.RemoveStok` / `StokLayerModel.RemoveStok` | Depleted layers **kept** (good vs BR-STL-017) |
| Allocation order | `OrderBy(ExpDate)` | **FEFO**; conflicts with BR-STL-029/032 and with legacy ED-filter + FIFO by mutation time |
| Qty consistency inside aggregate | Sum of `QtySisa` | Local only; no cross-location conservation |

### 2.4 Behaviors that exist only (or mainly) outside domain

| Behavior | Where today | Should live |
|---|---|---|
| Transaction-type catalog (DO, MT, DU, DR, DS, AJ, RP, …) | VB6 `Generate` prefix dispatch | Source BCs + consequence request mapping; Stock Ledger records consequence types, does not own FO |
| ED-scoped FIFO consume | VB6 `RemoveStok` | Domain allocation policy |
| Delete `tb_stok` at qty 0 | VB6 `RemoveStok` | **Must not** enter Stock Ledger domain |
| FO writeback PO/DO/HPP | VB6 sale/serah/repack | Application anti-corruption / legacy projection adapter |
| Reconstruction from `tb_buku` | Commented `StokRepo` / tests | Domain Reconstruction aggregate + Application orchestrator; Infra reads legacy tables |
| HPP method selection on DO | VB6 `MetodePersediaanHPP` | Valuation policy / Purchasing — not silent inside ledger |

### 2.5 Invariants

| Invariant (spec) | Enforced in current domain? |
|---|---|
| Remaining Qty ≥ 0 | Partially (outbound guard) |
| Depleted layers retained | Yes (in memory) |
| Receipt Source immutable across transfer | No transfer API |
| Transfer conservation | No |
| Idempotent source consequence (BR-STL-004/005) | No |
| Reconstruction once per Item+DO (BR-STL-070) | No |
| Reconciliation equation (BR-STL-049) | No |

**Domain completeness score:** foundation for **layer + nested book lines** only (~15–20% of target domain surface). Insufficient to claim Stock Ledger readiness.

---

## 3. Infrastructure review

### 3.1 Legacy persistence (usable)

| Component | Capability | Gaps for Stock Ledger |
|---|---|---|
| `tb_stok_dal` | Insert/Update/Delete/Get/List by Brg+Layanan | No list-by-DO across locations; delete API enables legacy delete-on-zero |
| `tb_buku_dal` | Bulk Insert; Delete by `fs_kd_trs`; List by Brg+Layanan+DO | **Bug:** SQL uses `fs_kd_do = @fs_kd_do` while binding `IEnumerable` — not a proper `IN` list. No list-by-Barang+DO across all layanan. No Update/Get. |

These DALs are adequate as **legacy adapters** after query fixes; they are not Stock Ledger storage.

### 3.2 FARIN schema (present) vs wiring (absent)

| Table | Intended role | Schema fit vs domain | Runtime |
|---|---|---|---|
| `FARIN_Stok` | Header qty by Brg+Layanan | Matches **current** `StokModel` key, **not** Position by Receipt Source | Unwired |
| `FARIN_StokLayer` | Layer rows | Has ReceiveId/PurchaseId/Exp/Batch/Hpp/Qty; index is `(BrgId, LayananId, ExpDate)` — favors FEFO, weak for FIFO-by-receipt-time and DO-scoped recon | Unwired |
| `FARIN_StokBuku` | Movement lines under layer | Nested book model; **not** Stock Movement aggregate | Unwired |

Missing SQL entirely:

* Stock Movement header (+ correction/reversal links)
* Reconstruction / migration status (`Item + Receipt Source`)
* Reconciliation outcomes
* Idempotency / processed-source ledger
* Legacy projection map (commented `FARIN_StokBukuMap` never shipped)
* Concurrency token (`RowVersion` / optimistic version)
* Virtual location master rows

### 3.3 Capability matrix

| Capability | Persistence support today |
|---|---|
| Stock Layer persistence | Schema only; C# stack deleted/unwired |
| Stock Movement persistence | No header model; FARIN_StokBuku is line-shaped only |
| Legacy Projection | Read adapters partial; no write projection service |
| Reconstruction | No status table; legacy list queries incomplete for cross-location DO scope |
| Reconciliation | None |
| FIFO allocation persistence | None (allocation not a persisted fact) |
| Virtual Stock Locations | None |
| Idempotency | None |
| Concurrency | None on FARIN or legacy stock tables |

### 3.4 Repository responsibilities still missing

Implementers will need (Application ports; Infrastructure adapters):

1. `IStockPositionRepo` — load/save Position by Item + Receipt Source (all locations’ layers)
2. `IStockMovementRepo` — append-only movements; load by source transaction for idempotency
3. `ILegacyStockReconstructionRepo` / `IStockLedgerMigrationStore` — status, watermark, lock
4. `ILegacyStockReadAdapter` — list `tb_buku` / `tb_stok` by Barang+DO **across locations**
5. `ILegacyStockProjectionWriter` — dual-write to `tb_stok`/`tb_buku` with legacy delete-on-zero semantics **only on legacy tables**
6. `IStockReconciliationRepo` — persist reconciliation outcomes
7. Optional: source-consequence inbox / uniqueness store

`IStokRepo` as currently defined should be treated as a **legacy/kartu-stok contract** to be replaced or narrowed to a read model, not extended as the Stock Ledger write port.

---

## 4. Legacy compatibility (VB6 → classification)

Source: `clbGenStokX1.cls`. Pattern: `Generate(prefix)` → `GenStok*` → `AddStok` / `RemoveStok`.

### 4.1 Classification legend

| Class | Meaning |
|---|---|
| **Preserve** | Keep business meaning in Stock Ledger (possibly re-expressed) |
| **Replace** | Same intent, different mechanism that matches domain rules |
| **Migrate** | Keep for coexistence / dual-write / FO compatibility during transition |
| **Remove** | Must not exist in Stock Ledger authority (may remain temporarily in legacy projection only) |

### 4.2 Behavior catalog

| Legacy behavior | Class | Why |
|---|---|---|
| ED hard-filter on consume, then FIFO by `fd_tgl_mutasi`/`fs_jam_mutasi` | **Preserve** (policy) / **Replace** (implementation) | Aligns with BR-STL-031 then BR-STL-032; reimplement in domain allocator, not VB script |
| Batch stored but **ignored** in `RemoveStok` WHERE | **Replace** | Decide explicitly: filter by batch when FO supplies it, or stop pretending FO batch selects stock |
| Delete `tb_stok` when qty reaches 0 | **Remove** from Stock Ledger; **Migrate** on legacy projection only | Conflicts with BR-STL-017/018/079 |
| Always INSERT new `tb_stok` on Add/void restore | **Replace** | Prefer restore same layer when provenance known (BR-STL-089); new layer only when identity unknown (BR-STL-091) |
| `tb_buku` append-only journal (normal path) | **Migrate** → Stock Movement Lines | Preserve audit idea; new model owns authority |
| `xVoidDelete` hard-deletes `tb_buku` | **Remove** | Conflicts with BR-STL-022/028; use compensating reversal movements |
| Prefix catalog DO/MT/DU/DR/DS/DT/PK/MN/RB/RU/RT/AJ/RP | **Migrate** | Map FO types to Consequence Requests; Stock Ledger does not own FO |
| Reserved (`DR`) as mutasi to destination layanan | **Preserve/Migrate** | Maps to Virtual Stock Location transfer (BR-STL-092) |
| Serah (`DS`) consumes from reserved destination | **Migrate** | Explicit two-step consequence; location must be explicit |
| FO writeback of PO/DO/HPP on sale/serah/repack | **Migrate** then phase out | Required for legacy FO until ledger is source of allocation facts |
| HPP method selection on DO | **Migrate** | Valuation policy owned outside pure ledger core |
| Adjust ± (`AJ`) as opname proxy | **Replace** | Opname observes; authorized adjustment is the source fact (BR-STL-108/109) |
| Retur jual umum provenance from sale lines | **Preserve** | Strong traceability pattern → restore layer |
| Retur jual tipe creating synthetic DO = return id | **Migrate** carefully | May create new Receipt Source incorrectly; domain prefers restore or accountable new layer without inventing provenance |
| Purchase return pinned to `fs_kd_do` | **Preserve** | Receipt-source targeting |
| Repack material out + hasil in with derived HPP | **Migrate** | Consequence composition owned by Repack BC; ledger records quantity/valuation consequences |
| Silent under-restore on retur jual umum | **Replace** | Must become explicit failure / unfulfilled |
| Silent `False` on unknown prefix | **Replace** | Explicit rejection |
| No locking / no transaction in class | **Replace** | Application unit-of-work + optimistic concurrency on Remaining Qty |
| SQL string concatenation | **Remove** | Security/maintainability; parameterized adapters only |
| ID generators `BK`/`ST` | **Migrate** for legacy projection; **Replace** for native (ULID/new ids) |

### 4.3 Compatibility requirements that must stay during coexistence

1. Legacy apps continue calling VB6 (or equivalent) paths that mutate `tb_stok`/`tb_buku` with delete-on-zero.
2. Dual-write from new consequence pipeline must project legacy shapes **including** delete-on-zero on `tb_stok`, while Stock Ledger keeps depleted layers.
3. FO tables that expect PO/DO/HPP writeback need an explicit adapter until FO reads Stock Ledger allocations.
4. UOM conversion to smallest unit remains a pre-ledger concern (legacy `ConvToTerkecil`).

---

## 5. Architecture alignment (SOLID / Clean Architecture / DDD)

### 5.1 What is already aligned

* Layer folders Domain / Application / Infrastructure / Api exist.
* Domain types do not reference SQL directly.
* `JenisLokasi` / `Layanan` repos are thin.
* Domain docs correctly assign transaction authority to other BCs.

### 5.2 Leakage and misplacements

| Issue | Evidence | Fix |
|---|---|---|
| Persistence dirty flags in domain | `ModelStateEnum` on `StokLayerModel` / `StokBukuType` | Move tracking to Infra mappers or separate persistence models |
| Allocation policy encoded as FEFO in aggregate | `StokModel.RemoveStok` OrderBy ExpDate | Domain service / policy matching BR-STL-029–032; Explicit Expiry Selection as input |
| Aggregate root wrong for Stock Position | `IStokKey : IBrgKey, ILayananKey` | Redefine Position as Item + Receipt Source |
| Reconstruction sketched in repository | Commented `RekonstruksiStok` in repo tests | Domain aggregate + Application use case; repo only loads/saves |
| Business rules in VB6 Transaction Script | `clbGenStokX1` | Extract to domain; do not port script structure |
| Name collision | `ChargeContext...IStokRepo` vs Inventory `IStokRepo` | Rename Charge contract to avoid confusion |
| API depends on unwired repo | `StokController` → `StokGetKartuStokQuery` | Stabilize read model after Position redesign |

### 5.3 SOLID notes

* **SRP:** `StokModel` currently mixes position totals, layer collection, and allocation policy.
* **OCP:** Adding Explicit Expiry / batch filters requires editing `RemoveStok` rather than composing policies.
* **DIP:** Application depends on `IStokRepo`, but no working Infra implementation exists — dependency is inverted correctly in shape, broken in runtime.

---

## 6. Risks

| ID | Risk | Severity | Notes |
|---|---|---|---|
| R1 | Extending current `StokModel(Brg,Layanan)` locks wrong aggregate boundary | **Critical** | Blocks DO-scoped reconciliation and reconstruction |
| R2 | Dual-write drift if any write path skips Stock Ledger or legacy projection | **Critical** | Requires single consequence pipeline gate |
| R3 | Concurrent bootstrap for same Barang+DO | **Critical** | Needs serialized Reconstruction status / lock |
| R4 | Bootstrap discovers legacy `tb_stok` vs `tb_buku` already inconsistent | **High** | Must surface `Inconsistent`, not silent balance |
| R5 | FEFO-in-code vs ED+FIFO-in-legacy vs FIFO-in-domain confusion | **High** | Causes wrong HPP/provenance on sales |
| R6 | `tb_buku_dal.ListData` DO binding bug + missing cross-location query | **High** | Breaks reconstruction data load |
| R7 | Hot DO with 20 years of `tb_buku` makes first bootstrap slow | **High** | Index `(fs_kd_barang, fs_kd_do)`; timeouts; async/job option later |
| R8 | Legacy `xVoidDelete` / FO writeback assumptions break if new path omits them too early | **High** | Compatibility adapters required in coexistence |
| R9 | FARIN schema favors ExpDate index; weak for receipt-time FIFO and DO recon | **Medium** | Schema revision before production write |
| R10 | Deleted FARIN DAL/tests mid-refactor leave repo unbuildable for StokFeature | **Medium** | Restore or replace deliberately in Phase 1 |
| R11 | Insufficient automated tests for reconstruction / dual-write / concurrency | **High** | Testing risk until harness exists |
| R12 | Partial retur / void bugs in VB6 copied “as-is” | **Medium** | Classify Replace; do not preserve silent failures |
| R13 | Operational cutover unclear (who is authority per DO) | **High** | Migration status table is mandatory |
| R14 | Performance of dual-write three stores per trx | **Medium** | Same DB transaction; measure |
| R15 | Team ports VB6 script into C# services | **Critical** (process) | Forbidden by this review; roadmap enforces DDD shape |

---

## 7. Feasibility conclusions

1. **Business approach (lazy bootstrap per Barang+DO, keep zero layers, coexist with legacy)** remains **feasible** and matches BR-STL-062–075.
2. **Current implementation** is an **early spike**, not a sufficient trajectory if continued unchanged.
3. The largest corrective action is **aggregate realignment** before more persistence work: Stock Position by Receipt Source; Stock Movement as first-class immutable record; Reconstruction status store.
4. Legacy VB6 is invaluable for **behavior extraction** and **compatibility projection**, not as a template for Domain/Application structure.
5. Safest roadmap: stabilize domain model → FARIN persistence for Position/Layer/Movement → Reconstruction aggregate + legacy read adapters → dual-write consequence use cases for one FO type → expand FO coverage → reconciliation → virtual reservation.

See [`stock-ledger-gap-analysis.md`](./stock-ledger-gap-analysis.md) for the capability backlog and [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md) for phased execution.
)
