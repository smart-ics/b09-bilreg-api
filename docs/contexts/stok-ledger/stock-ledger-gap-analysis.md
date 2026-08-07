# Stock Ledger — Gap Analysis

**Artifact status:** Implementation gap backlog  
**Basis:** Current codebase vs [`stok-ledger-domain.md`](./stok-ledger-domain.md)  
**Feasibility overview:** [`stock-ledger-feasibility-review.md`](./stock-ledger-feasibility-review.md)  
**Execution plan:** [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md)

---

## 1. How to read this document

Each gap has:

* **ID** — stable reference for roadmap/tasks
* **Target** — domain requirement
* **Current evidence** — what exists in repo now
* **Dependency rank** — lower number = implement earlier
* **Priority** — P0 blocking / P1 required for first production slice / P2 full domain / P3 deferrable polish

---

## 2. Current → Target map

```text
Current (kartu-stok spike)
  StokModel(Brg, Layanan) + Layer + nested Buku
  FEFO by ExpDate
  Legacy tb_* DALs
  FARIN SQL unwired / C# DAL deleted in working tree
  VB6 Generate still authoritative writer (outside this API)

Target (Stock Ledger)
  Stock Position(Item, Receipt Source) across locations
  Stock Movement aggregate (immutable)
  ED-constrained FIFO by Effective Receipt Time
  Legacy Reconstruction + Migration status
  Dual-write Legacy Projection
  Reconciliation per Item+DO
  Virtual Stock Location reservation
  Consequence use cases from other BCs
```

---

## 3. Gap catalog (dependency-ordered)

### G-01 — Aggregate boundary realignment

| | |
|---|---|
| **Target** | Stock Position AR = Item + Receipt Source across all Stock Locations (domain §6.2) |
| **Current** | `StokModel` AR = `BrgId + LayananId` |
| **Rank** | 1 |
| **Priority** | P0 |
| **Work** | Introduce Position / Layer models matching domain; demote or replace `StokModel` as location read model only |
| **Exit** | Domain tests prove layers for one DO span multiple layanan; qty conservation across transfer |

### G-02 — Stock Movement aggregate

| | |
|---|---|
| **Target** | Immutable Stock Movement + Lines; correction/reversal via new movements (BR-STL-022–028, 055–057) |
| **Current** | `StokBukuType` nested under layer; no movement header; no reverse/correct |
| **Rank** | 1 |
| **Priority** | P0 |
| **Work** | Domain types + factories for Receipt / Transfer / Consumption / Return / Adjustment / Reversal |
| **Exit** | Movement recorded once; reverse creates counter-movement; original unchanged |

### G-03 — Allocation policy (ED + FIFO)

| | |
|---|---|
| **Target** | Explicit Expiry Selection then FIFO by Effective Receipt Time + LayerId (BR-STL-029–038); optional batch rule decided explicitly |
| **Current** | FEFO `OrderBy(ExpDate)` in `StokModel.RemoveStok`; tests assert FEFO (`UT04`) |
| **Rank** | 2 |
| **Priority** | P0 |
| **Work** | Domain allocation service; inputs: location, qty, optional ED, optional batch; outputs: FIFO Allocation lines |
| **Exit** | Unit tests cover: no ED → FIFO by receipt time; with ED → filter then FIFO; insufficient stock → reject; multi-layer split |

### G-04 — Reconstruction / migration status store

| | |
|---|---|
| **Target** | Legacy Stock Reconstruction lifecycle + authority per Item+DO (BR-STL-062–075, 102–107) |
| **Current** | No domain type; commented repo sketch scoped by Brg+Layanan; no table |
| **Rank** | 2 |
| **Priority** | P0 |
| **Work** | `LegacyStockReconstruction` aggregate + `StockLedgerMigration` persistence (`KodeBarang`, `KodeDO`, status, version, timestamps, watermark, validation flags) |
| **Exit** | Same Barang+DO cannot complete reconstruction twice; concurrent trigger serializes |

### G-05 — Legacy read adapter (cross-location by DO)

| | |
|---|---|
| **Target** | Reconstruction reads all layanan for Item+Receipt Source |
| **Current** | `tb_buku_dal.ListData` filters Brg+Layanan+DO; DO parameter binding buggy for list; no cross-location API |
| **Rank** | 3 |
| **Priority** | P0 |
| **Work** | New queries: list `tb_buku` by Barang+DO (all layanan); list surviving `tb_stok` by Barang+DO; fix `IN` binding; indexes |
| **Exit** | Integration test on sample DO reconstructs Gudang/Apotek/IGD zero and non-zero layers |

### G-06 — FARIN / Stock Ledger persistence for Position + Layer

| | |
|---|---|
| **Target** | Persist layers including QtySisa=0; load Position by Item+DO |
| **Current** | SQL exists for location-keyed `FARIN_Stok*`; C# DAL/Repo deleted/unwired; schema missing receipt-time / reconstructed flags / version |
| **Rank** | 3 |
| **Priority** | P0 |
| **Work** | Revise schema to Position+Layer needs; implement repos; **do not** delete depleted layers |
| **Exit** | Save/load round-trip keeps zero layers; optimistic concurrency on Remaining Qty |

### G-07 — Stock Movement persistence + idempotency

| | |
|---|---|
| **Target** | Append-only movements; one active consequence per source responsibility (BR-STL-004/005) |
| **Current** | None |
| **Rank** | 4 |
| **Priority** | P0 |
| **Work** | Movement tables + unique key on (SourceTransactionId, ConsequenceType) or equivalent |
| **Exit** | Replaying same source command does not double qty |

### G-08 — Reconstruction use case (lazy bootstrap)

| | |
|---|---|
| **Target** | On first touch of unreconstructed Item+DO, reconstruct all locations; validate; establish authority |
| **Current** | None (wrong-scope commented sketch) |
| **Rank** | 4 |
| **Priority** | P0 |
| **Work** | Application orchestrator: lock migration → read legacy → build layers (new ids) → validate equation → mark Reconstructed/Inconsistent |
| **Exit** | Bootstrap once; depleted locations retained at 0; inconsistent blocks native writes |

### G-09 — Dual-write Legacy Projection

| | |
|---|---|
| **Target** | BR-STL-076–080: project legacy `tb_buku`/`tb_stok` without overriding Stock Ledger authority |
| **Current** | No writer from new model; VB6 still sole writer |
| **Rank** | 5 |
| **Priority** | P1 |
| **Work** | Projection service: append `tb_buku`; upsert/delete `tb_stok` with **legacy** delete-on-zero; map IDs |
| **Exit** | After native movement, legacy tables match projection rules; Ledger still has zero layers |

### G-10 — Consequence application pipeline (first FO type)

| | |
|---|---|
| **Target** | Source fact → Consequence Request → Movement → Position update (domain §1.5) |
| **Current** | Only read query `StokGetKartuStokQuery` |
| **Rank** | 5 |
| **Priority** | P1 |
| **Work** | Pick one type (recommend **Mutasi/Transfer** or **DO receipt**); implement command handler with UoW covering Ledger + Projection |
| **Exit** | End-to-end test: bootstrap if needed → apply → reconcile balanced → legacy projection correct |

### G-11 — Transfer / return / reverse behaviors in domain

| | |
|---|---|
| **Target** | BR-STL-039–045, 089–091, 055–061 |
| **Current** | Add always new layer; no transfer; no restore-to-layer |
| **Rank** | 5 |
| **Priority** | P1 |
| **Work** | Domain methods/services for transfer pairing, return-to-layer, reversal |
| **Exit** | Tests for mutasi conservation; retur restores original layer when known |

### G-12 — Reconciliation aggregate + use case

| | |
|---|---|
| **Target** | BR-STL-046–054 equation per Item+DO |
| **Current** | None |
| **Rank** | 6 |
| **Priority** | P1 |
| **Work** | Compute Recognized In = Remaining + Final Out (+ adjustments); persist outcome |
| **Exit** | Fast recon without global `tb_buku` replay for bootstrapped DO |

### G-13 — Expand FO consequence coverage

| | |
|---|---|
| **Target** | Parity with VB6 catalog needed by hospital ops |
| **Current** | VB6 handles DO, MT, DU/DR/DS/DT, PK, MN, RB, RU/RT, AJ, RP |
| **Rank** | 7 |
| **Priority** | P1/P2 |
| **Work** | Map each prefix to consequence; implement incrementally; keep VB6 for unmapped types |
| **Exit** | Documented coverage matrix; dual path only where intentional |

### G-14 — Virtual Stock Location + reservation

| | |
|---|---|
| **Target** | BR-STL-092–096; reserved/serah flows |
| **Current** | `JenisLokasi` lacks Virtual; no reservation API |
| **Rank** | 8 |
| **Priority** | P2 |
| **Work** | Virtual location identity strategy; transfer-based reserve/release/consume |
| **Exit** | Reserved stock not selectable from ordinary location |

### G-15 — Domain events

| | |
|---|---|
| **Target** | Domain §9 catalog |
| **Current** | None |
| **Rank** | 8 |
| **Priority** | P2 |
| **Work** | Raise on record/deplete/reconstruct/reconcile; integrate with project event approach ([`docs/concepts/operational-events.md`](../../concepts/operational-events.md)) |
| **Exit** | At least movement-recorded + reconstruction-completed published for consumers |

### G-16 — Kartu stok / API read models

| | |
|---|---|
| **Target** | Useful reads without wrong AR |
| **Current** | Controller loads `StokModel` by Brg+Layanan via broken repo |
| **Rank** | 6 |
| **Priority** | P1 |
| **Work** | Read models: balance by location; layers by DO; kartu from Movement/Layer |
| **Exit** | GET endpoints work against Stock Ledger data |

### G-17 — Test harness

| | |
|---|---|
| **Target** | Production confidence |
| **Current** | Domain FEFO tests; legacy DAL tests; reconstruction tests commented; FARIN DAL tests deleted |
| **Rank** | 3 (build alongside) |
| **Priority** | P0/P1 |
| **Work** | Domain policy tests; reconstruction fixtures from VB6 narrative; concurrency tests; dual-write integration |
| **Exit** | CI covers P0 gaps |

### G-18 — Remove / quarantine persistence dirty state from domain

| | |
|---|---|
| **Target** | Clean domain model |
| **Current** | `ModelStateEnum` on layer/buku |
| **Rank** | 2 |
| **Priority** | P1 |
| **Work** | Persistence models in Infra |
| **Exit** | Domain assemblies free of save-tracking enums |

### G-19 — ChargeContext `IStokRepo` rename + restore list if needed

| | |
|---|---|
| **Target** | Clear bounded-context naming; tarif still lists stock |
| **Current** | Charge StokRepo deleted in working tree; name collision with Inventory |
| **Rank** | 9 |
| **Priority** | P2 |
| **Work** | Restore as `ITarifStokList` (or similar) reading legacy or Ledger projection |
| **Exit** | No ambiguous `IStokRepo` |

### G-20 — Performance indexes & bootstrap SLO

| | |
|---|---|
| **Target** | Bounded bootstrap cost |
| **Current** | No guaranteed index strategy for Barang+DO on `tb_buku` |
| **Rank** | 4 |
| **Priority** | P1 |
| **Work** | DBA indexes; measure p95 bootstrap; optional background bootstrap later |
| **Exit** | Documented SLO; alert on timeout/Inconsistent rate |

---

## 4. Intentionally deferred (not gaps for first production slice)

| Item | Reason to defer |
|---|---|
| Mass historical migration of all DO | Contradicts BR-STL-075 and cost model |
| Porting VB6 `Generate` structure into C# services | Architecture violation |
| Perfect financial GL posting from Stock Ledger | Owned by Finance BC |
| UI/workflow for Inventory Officer beyond API reads | Separate workflow artifacts |
| Fixing all historical legacy data quality | Handle via Inconsistent + ops process |

---

## 5. Dependency graph (simplified)

```text
G-01 Aggregate realignment
G-02 Movement aggregate
G-03 Allocation policy
G-18 Domain purity
        │
        ▼
G-04 Migration status ──► G-05 Legacy read adapter ──► G-08 Bootstrap use case
G-06 Position/Layer persistence ─────────────────────►┘
G-07 Movement persistence + idempotency ─────────────► G-10 First consequence pipeline
        │
        ▼
G-09 Dual-write projection
G-11 Transfer/return/reverse
G-12 Reconciliation
G-16 Read models
        │
        ▼
G-13 Expand FO coverage
G-14 Virtual locations
G-15 Domain events
G-19 Charge rename
G-20 Perf SLO
```

---

## 6. Definition of “production-ready Stock Ledger” (minimum)

A DO is production-ready under Stock Ledger when:

1. Migration status is `Reconstructed` (or native-new DO marked authoritative without legacy replay).
2. Layers for all touched locations exist, including Qty=0.
3. New consequences for that DO go through Movement + Position with idempotency.
4. Legacy projection remains correct for consumers still on `tb_stok`/`tb_buku`.
5. Reconciliation for that DO runs without full-history global scan.
6. Concurrent writers cannot double-bootstrap or double-apply the same source transaction.

Hospital-wide readiness is **gradual**: many DO remain legacy-only until first touch (lazy bootstrap).
)
