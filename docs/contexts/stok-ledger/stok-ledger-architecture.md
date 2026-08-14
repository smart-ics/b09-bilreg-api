# Stock Ledger Architecture

**Artifact status:** Canonical technical specification (English)

**Bounded context:** Stock Ledger

**Governing business truth:** [stok-ledger-domain.md](./stok-ledger-domain.md)

**Version scope:** Coexistence-capable Stock Ledger v2 — greenfield realization of the approved domain model (`Stock Batch` / `Location Stock Balance` / `Stock Movement`) with Legacy Stock Record dual-write

**Operational SOPs:** Not applicable — Stock Ledger is integration-facing; use consequence scenarios in §5–§6 instead of operator SOPs

## 1. Architecture Overview

Stock Ledger is an **integration-facing, UI-agnostic** inventory-consequence module inside the Bilreg modular monolith. Originating contexts (Purchasing/Goods Receipt, Apotek/Sales, Mutasi, Pakai, etc.) call Stock Ledger **in-process** after they authorize a source business transaction. Stock Ledger does not own actor workflows or HTTP product APIs in this version.

| Aspect | Statement |
|---|---|
| Style | Clean Architecture modular monolith (Target; matches Bilreg) |
| Projects | `Bilreg.Domain`, `Bilreg.Application`, `Bilreg.Infrastructure`, `Bilreg.Api` (composition/DI only) |
| Dependency | `Api → Application → Domain`; `Infrastructure → Application → Domain` |
| Surface | In-process MediatR commands/queries and application ports only |
| Coexistence | During parallel operation, `tb_stok` + `tb_buku` remain persisted data authority; `BILRG_Stok*` holds the Stock Ledger Representation |
| Cutover | Feature flag disables coexistence adapters; side tables droppable |

**Major constraints**

- Legacy writers and readers continue to mutate `tb_stok` / `tb_buku`.
- Native Stock Ledger writes must dual-write legacy + ledger atomically while coexistence is on.
- Prior Stock Ledger v1 designs (six-table model, separate Movement header/line, Position-only OCC table, soft assumptions) are **superseded** and must not be reused.

**Major gaps (Target)**

- Greenfield `StockLedgerFeature` v2 module, five `BILRG_Stok*` tables, coexistence runtime (hydrate / catch-up / freshness gate / dual-write), and slice-1 use cases listed in §15.

## 2. Codebase Evidence and Constraints

| Evidence | Verified location | Architectural implication |
|---|---|---|
| Clean Architecture projects | `src/bilreg/Bilreg.{Domain,Application,Infrastructure,Api}` | Place v2 code in matching layers under `InventoryContext/StockLedgerFeature` |
| MediatR use cases | e.g. `InventoryContext/MutasiFeature/UseCases/*` | Commands/queries as `IRequest` handlers |
| Legacy stock tables | `Bilreg.SqlDb/.../StokFeature/tb_stok.sql`, `tb_buku.sql` | Dual-write and sync adapters must match `VARCHAR` widths and decimal scales |
| Legacy stock DAL patterns | `Bilreg.Infrastructure/.../StokFeature/tb_*_dal.cs` | Reuse patterns for parameterized SQL; do not treat DAL as public contract for other contexts |
| ID helpers | `Nuna.Lib.AutoNumberHelper.NunaId` usage across Domain | `NunaId.New()` for `BILRG_*` PKs; `NunaId.NewLegacyCompact("BK"|"ST")` for legacy `fs_kd_trs` |
| Audit / void philosophy | `docs/DATABASE.md` | Default `Vod*` soft-void; **ADR-STL-002** exempts Stock Ledger core tables (reverse journal instead) |
| Non-normative legacy behavior | `docs/stok-ledger/clbGenStokX1.cls` | Movement-kind mapping and consequence coverage reference only |
| Domain v2 | `docs/contexts/stok-ledger/stok-ledger-domain.md` | FEFO/FIFO, depleted retention, reverse-journal void, coexistence authority |

**Constraints**

- C-01: Do not grant Stock Ledger exclusive write ownership of stock while coexistence flag is on.
- C-02: Do not delete completed `BILRG_StokMutasi` rows; void = insert reversing movements.
- C-03: Do not hard-delete `BILRG_StokLokasi` at `QtySisa = 0`.
- C-04: Do not call another context’s tables/DALs as a public API; call application use cases/ports.
- C-05: If leftover Stock Ledger **v1** code exists on a branch, treat it as obsolete; implement v2 greenfield.

**Gaps**

- G-01: v2 domain types, repos, UoW, coexistence services, SQL scripts — missing (Target).
- G-02: Originating contexts (Purchasing, Apotek sale pipeline) may not yet invoke Stock Ledger — integration wiring is incremental after slice-1 core exists.

## 3. Module and Bounded-Context Boundaries

| Module | Responsibility | Owns | Depends on | Must not own |
|---|---|---|---|---|
| `StockLedgerFeature` | Inventory consequence, allocation, reconciliation representation, coexistence alignment | `Stock Batch` write model, movements, ledger projections, legacy adapters for stock dual-write/sync | Item/Location identity (by id), source `TrsReffId` facts from callers | Purchasing docs, sales invoices, mutasi approval workflows, UI |
| `StokFeature` (legacy helpers) | Existing legacy table access patterns | Optional shared DTO/DAL primitives if reused internally by adapters | SQL | Stock Ledger business rules |
| `MutasiFeature` / Sales / future Purchasing | Source business transactions | Order/sale/receipt authority | Stock Ledger as consequence sink | Ledger invariants, FEFO policy |

Cross-context: callers invoke Stock Ledger use cases **after** their own transaction authority is established. Stock Ledger may reject insufficient stock; it must not invent the business reason for the source document.

## 4. Clean Architecture Layer Responsibilities

| Layer | Feature responsibilities | Permitted dependencies | Prohibited dependencies |
|---|---|---|---|
| Domain (`Bilreg.Domain/.../StockLedgerFeature`) | `StockBatch` aggregate behavior, FEFO/FIFO allocation pure functions, movement factories, reverse-journal rules, enums | Domain shared helpers (`NunaId`), GuardClauses | Dapper, SQL, MediatR, legacy DTOs |
| Application (`Bilreg.Application/.../StockLedgerFeature`) | Use cases, orchestration (multi-batch allocation), freshness gate, hydrate/sync orchestration, repo + legacy ports, UoW contract, idempotency keys | Domain | Infrastructure types, controllers |
| Infrastructure (`Bilreg.Infrastructure/.../StockLedgerFeature`) | `BILRG_*` DAL/Repo, legacy reader/writer adapters, binding/scope persistence, SQL transactions | Application ports, Dapper/SQL | Domain rules beyond mapping |
| Api | DI registration only for this version | Application | Business decisions |

Read models for reconciliation live as Application queries + Infrastructure SQL; they must not decide lifecycle transitions.

## 5. Application Use Cases

Consequence scenarios replace SOPs. Slice-1 implements the rows marked **S1**; others remain defined but deferred.

| ID | Use case | Kind | Purpose | Authority / initiator | Primary model | Dependencies | Transaction outcome |
|---|---|---|---|---|---|---|---|
| UC-STL-001 | Post Goods Receipt Consequence | Integration Inbound | Recognize inbound qty; establish/increase batch & location balance | Purchasing / Goods Receipt | Stock Batch | Legacy writer, Scope, Binding | Dual-write receipt; idempotent on `(TrsReffId, Kind, StokLokasiId)` |
| UC-STL-002 | Post Stock Transfer Consequence | Integration Inbound | Move qty between locations; preserve `BrgMasukReffId`, `Hpp`, `TglEd` | Mutasi / Transfer | Stock Batch (1..n) | Allocation, Legacy writer | Dual-write OUT+IN; hospital-wide qty unchanged |
| UC-STL-003 | Post Sale Issue Consequence | Integration Inbound | Outbound sale via Outbound Allocation | Apotek / Sales | Stock Batch (1..n) | Allocation, Legacy writer | Dual-write issue lines |
| UC-STL-004 | Post Sale Void Consequence | Integration Inbound | Reverse-journal prior sale | Apotek / Sales | Stock Batch | Binding (mandatory), Legacy writer | Dual-write reversing lines; originals retained |
| UC-STL-005 | Post Sales Return Consequence | Integration Inbound | Restore previously issued sale qty | Apotek / Sales | Stock Batch | Legacy writer | Dual-write inbound return |
| UC-STL-006 | Post Internal Consumption Consequence | Integration Inbound | Pakai barang outbound | Consumption authority | Stock Batch (1..n) | Allocation, Legacy writer | Dual-write issue |
| UC-STL-010 | Hydrate Scope From Legacy | Command | Replay `tb_buku` for `(BrgId, BrgMasukReffId)` into ledger | Stock Ledger / gate | Scope + Batch | Legacy reader | Ledger baseline; no legacy mutate |
| UC-STL-011 | Catch Up Scope From Legacy | Background / Command | Apply newer legacy buku rows to ledger | Stock Ledger | Scope + Mutasi | Legacy reader, Binding | Append mutasi; advance watermark |
| UC-STL-012 | Ensure Freshness For Scopes | Command | Hydrate+catch-up until gate passes | Called by S1 posts | Scope | UC-STL-010/011 | Abort post if Inconsistent |
| UC-STL-020 | Reconcile Scope | Query | Compare ledger conservation (± legacy) | Steward / ops tooling (in-process) | Projection | Repos | Read-only outcome |
| UC-STL-021 | Get Availability At Location | Query | Candidate balances for allocation preview | Callers | Projection | Repos / legacy read if needed | Read-only |

**S1 delivery set:** UC-STL-001…006, 010…012, plus minimal 020/021 as needed for tests.

**Deferred (domain in-scope, not S1):** purchase return, destruction, adjustment, repack, receipt void, transfer void (except patterns reusable from sale void).

## 6. Use-Case Traceability

| Use case ID | Domain capabilities / rules | Scenario | Owning module | Authorization concern | Required tests |
|---|---|---|---|---|---|
| UC-STL-001 | 3.1, 3.2, 3.3; BR-STL-001…015 | Goods receipt | StockLedgerFeature | Trusted in-process caller + `TrsReffId` | Domain qty/Hpp; UoW dual-write; idempotent retry |
| UC-STL-002 | 3.4, 3.5; BR-STL-021, 028–029 | Transfer MT | StockLedgerFeature | Same | ED preserved; OUT=IN; OCC |
| UC-STL-003 | 3.5, 3.6; BR-STL-022…027 | Sale FEFO/FIFO | StockLedgerFeature | Same | Allocation matrix; multi-batch; dual-write |
| UC-STL-004 | 3.11; BR-STL-016…020 | Batal jual | StockLedgerFeature | Binding required | Reverse journal; reject if unbound |
| UC-STL-005 | 3.6 | Retur jual | StockLedgerFeature | Same | Inbound restore path |
| UC-STL-006 | 3.8 | Pakai | StockLedgerFeature | Same | Allocation + dual-write |
| UC-STL-010/011/012 | 3.13; BR-STL-033…035 | Coexistence | StockLedgerFeature | Internal | Watermark ordering; no double buku |
| UC-STL-020 | 3.12; BR-STL-030…032 | Reconcile | StockLedgerFeature | Steward | Includes depleted |

SOP column: **N/A** (integration-facing).

## 7. Aggregate and Domain Model Realization

### 7.1 Stock Batch Aggregate (Target)

| Concern | Decision |
|---|---|
| Root | `StockBatch` — identity `(BrgId, BrgMasukReffId)` / `StokBatchId` |
| Owns | `LocationStockBalance` children; `StockMovement` facts for this batch |
| Invariants | BR-STL-005…015, 028–029; `QtySisa` hospital-wide = Σ location `QtySisa`; never negative; depleted retained |
| Entry points | Application use cases only (no public entity persistence from controllers) |
| Cross-batch | Outbound Allocation in Application may touch multiple aggregates in **one DB transaction** |
| Write partitioning | Handlers must **not** load full movement history into memory for routine posts; load candidate balances for location (+ filters); append movements; update qty/version. History load only for hydrate/replay/reconcile |

### 7.2 Stock Reconciliation (Target)

Read/assessment model; does not mutate movements. May be a domain service + query result rather than a heavy aggregate root.

### 7.3 Prohibited domain responsibilities

- Approving purchases/sales/transfers
- Generating commercial document numbers for source contexts
- Soft-deleting movements via `Vod*`

## 8. Persistence and Repository Strategy

### 8.1 Tables (Target DDL skeleton)

Sentinel empty datetime: `3000-01-01`. Qty `DECIMAL(18,0)`, Hpp `DECIMAL(18,2)`. Audit: `CrtUser/CrtDate/UpdUser/UpdDate` only (no `Vod*`; ADR-STL-002). Logical FK only (no SQL `CONSTRAINT FK`).

#### `BILRG_StokBatch`

| Column | Type | Notes |
|---|---|---|
| `StokBatchId` | `VARCHAR(12)` | PK; `NunaId.New()` |
| `BrgId` | `VARCHAR(13)` | |
| `BrgMasukReffId` | `VARCHAR(10)` | Receipt source / DO |
| `QtySisa` | `DECIMAL(18,0)` | Stored |
| `Hpp` | `DECIMAL(18,2)` | |
| `TglMasuk` | `DATETIME` | Receipt datetime |
| `PoReffId` | `VARCHAR(10)` | Optional carry-over; `''` allowed |
| `Version` | `BIGINT` | OCC for batch qty updates |
| Audit Crt/Upd | | |

- `UX (BrgId, BrgMasukReffId)`
- `IX (BrgId, TglMasuk, BrgMasukReffId)`

#### `BILRG_StokLokasi`

| Column | Type | Notes |
|---|---|---|
| `StokLokasiId` | `VARCHAR(12)` | PK; `NunaId.New()` |
| `StokBatchId` | `VARCHAR(12)` | |
| `BrgId` | `VARCHAR(13)` | Denormalized |
| `BrgMasukReffId` | `VARCHAR(10)` | Denormalized |
| `LayananId` | `VARCHAR(5)` | |
| `TglEd` | `DATETIME` | Part of uniqueness; sentinel = absent ED |
| `NoBatch` | `VARCHAR(15)` | Optional manufacturer batch; `''` ok |
| `QtySisa` | `DECIMAL(18,0)` | Stored; 0 retained |
| `Version` | `BIGINT` | **Primary OCC** token |
| Audit Crt/Upd | | |

- `UX (StokBatchId, LayananId, TglEd)` — **L1**
- `IX_FifoFefo (BrgId, LayananId, TglEd, TglMasuk)` filtered `QtySisa > 0` (include keys/qty/version) — implement via denormalized `TglMasuk` on lokasi **or** join batch; prefer denormalize `TglMasuk` on lokasi for allocation scans

**Amendment:** add denormalized `TglMasuk DATETIME` on `BILRG_StokLokasi` for FEFO/FIFO candidate scans without join.

#### `BILRG_StokMutasi`

| Column | Type | Notes |
|---|---|---|
| `StokMutasiId` | `VARCHAR(12)` | PK |
| `StokLokasiId` | `VARCHAR(12)` | |
| `StokBatchId` / `BrgId` / `BrgMasukReffId` / `LayananId` / `TglEd` | denormalized | |
| `TrsReffId` | `VARCHAR(10)` | Source transaction |
| `MovementKind` | `INT` | Enum |
| `QtyIn` / `QtyOut` | `DECIMAL(18,0)` | Exactly one side > 0 |
| `Hpp` | `DECIMAL(18,2)` | |
| `PoReffId` | `VARCHAR(10)` | Optional carry-over |
| `TglMutasi` | `DATETIME` | |
| `ReversesMutasiId` | `VARCHAR(12)` | `''` if not reversal |
| Audit Crt/Upd | | Append-only qty/kind |

- `UX (TrsReffId, MovementKind, StokLokasiId)` — native idempotency
- Indexes: by lokasi+time, batch+time, reverses, `TrsReffId`

#### `BILRG_StokLegacyScope` (drop at cutover)

| Column | Type | Notes |
|---|---|---|
| `BrgId` + `BrgMasukReffId` | PK | |
| `AlignmentStatus` | `INT` | NotAligned / Aligned / Stale / Inconsistent |
| `TglMutasiLast` | `DATETIME` | Watermark |
| `LastLegacyBukuId` | `VARCHAR(10)` | Tie-break only (`tb_buku.fs_kd_trs`) |
| `LastSyncedAt` | `DATETIME` | |
| `InconsistencyReason` | `VARCHAR(500)` | |
| Audit Crt/Upd | | |

Catch-up predicate: compose legacy `(fd_tgl_mutasi, fs_jam_mutasi)` → datetime `>` `TglMutasiLast`, or equal and `fs_kd_trs > LastLegacyBukuId`, scoped by `fs_kd_barang` + `fs_kd_do`. **Do not** assume `fs_kd_trs` sort order across legacy vs `NewLegacyCompact` writers.

#### `BILRG_StokLegacyBinding` (drop at cutover)

| Column | Type | Notes |
|---|---|---|
| `BindingId` | `VARCHAR(12)` | PK |
| `BindingKind` | `INT` | Mutasi↔Buku / Lokasi↔Stok |
| `StokMutasiId` / `StokLokasiId` | | |
| `LegacyBukuId` / `LegacyStokId` | `VARCHAR(10)` | `tb_*.fs_kd_trs` |
| `TrsReffId` | `VARCHAR(10)` | |
| Audit Crt/Upd | | |

- Unique `LegacyBukuId` where non-empty (sync idempotency)
- Unique `StokMutasiId` where non-empty

### 8.2 Repositories (Target)

| Port | Owns |
|---|---|
| `IStockBatchRepo` | Load/save batch + lokasi balances for write scopes; OCC updates |
| `IStockMutasiRepo` | Insert-only movements; existence checks for idempotency/reversal |
| `IStockLegacyScopeRepo` | Scope status + watermark |
| `IStockLegacyBindingRepo` | Binding insert/lookup for void/sync |
| `ILegacyStockReadPort` | Parameterized reads of `tb_stok` / `tb_buku` |
| `ILegacyStockWriterPort` | Dual-write inserts/updates; **void = insert reverse buku + update stok; never delete buku for void** |
| `IStockConsequenceUnitOfWork` | Single SQL transaction spanning ledger + legacy + scope/binding |

IDs: `NunaId.New()` for all `BILRG_*` PKs; `NunaId.NewLegacyCompact("BK")` / `("ST")` for new legacy rows.

### 8.3 MovementKind

Persist `INT`. Map legacy strings from `clbGenStokX1.cls` in the anti-corruption layer (`DO`, `MT_OUT`, `MT_IN`, `DU`/`DB`/`DT`, `RU`/`RT`, `PK`, void kinds `*_V`, etc.). Slice-1 must register kinds used by S1 flows.

## 9. Read Models and Query Strategy

| Projection | Consumer purpose | Source authority | Freshness | Filters / scope | Must not decide |
|---|---|---|---|---|---|
| Availability at location | Allocation / caller preview | Ledger balances (`QtySisa > 0`); during coexistence may cross-check legacy | After freshness for write path | `BrgId`, `LayananId`, optional `TglEd` | Sale authorization |
| Scope reconciliation | Steward / tests | Movements + balances (+ legacy) | On demand | `BrgId` + `BrgMasukReffId` (± layanan) | Silent repair |
| Movement by `TrsReffId` | Void/support | `BILRG_StokMutasi` | Immediate | `TrsReffId` | Rewrite history |

## 10. Application Interfaces and API Philosophy

| Topic | Decision |
|---|---|
| Transport | **In-process only** for S1–product flows |
| HTTP | Out of scope for product UI; optional later ops endpoints must remain thin wrappers over Application |
| CQRS | Commands for consequences; queries for availability/reconcile |
| Results | Deterministic success / idempotent replay / validation fail / concurrency conflict / insufficient stock / scope inconsistent |
| Idempotency | Unique mutasi key + unique legacy buku binding |
| Versioning | Stable use-case IDs (`UC-STL-*`); additive `MovementKind` values |

Controllers must not become stock authority.

## 11. Integration and Cross-Context Collaboration

| Collaborator | Direction | Purpose | Owning authority | Contract style | Consistency / delivery | Failure handling |
|---|---|---|---|---|---|---|
| Purchasing / Goods Receipt | In → STL | UC-STL-001 | Purchasing | In-process command | Sync TX with dual-write when coexistence on | Abort both sides |
| MutasiFeature | In → STL | UC-STL-002 | Mutasi | In-process | Same | Same |
| Apotek / Sales | In → STL | UC-STL-003…005 | Sales/Apotek | In-process | Same | Same |
| Pakai / consumption | In → STL | UC-STL-006 | Consumption ctx | In-process | Same | Same |
| Legacy Stock Record | Both | Authority + mirror | Legacy | Anti-corruption ports | Sync in UoW for native; catch-up for legacy-originated | Mark scope Inconsistent; do not invent |
| Product Catalog / Location | In (ids only) | Validate ids as needed | External masters | Id references | N/A | Reject unknown if validated |

No generic event bus required for S1.

## 12. Authentication, Authorization, and Audit

| Concern | Decision |
|---|---|
| Actor-facing auth | Not applicable (no end-user Stock Ledger UI) |
| Caller | Trusted in-process module; carry `UserId` / service identity into audit columns and source facts |
| Authorization | Originating context authorizes business act; Stock Ledger enforces inventory invariants only |
| Audit | `Crt*`/`Upd*` on ledger tables; immutable movements; void via reverse journal with `ReversesMutasiId` |
| Sensitive data | Standard hospital inventory; no patient payload required in ledger rows |

## 13. Transactions, Consistency, Concurrency, and Idempotency

### Native post path (coexistence on)

```text
UC-STL-012 Ensure Freshness (scopes touched)
  -> Domain allocate / apply (in memory)
  -> UoW BEGIN
       write tb_buku / tb_stok (NewLegacyCompact ids; void = reverse insert only)
       write BILRG_StokBatch / StokLokasi / StokMutasi
       write Binding + update Scope watermark (TglMutasiLast)
       OCC UPDATE StokLokasi WHERE Version = expected
     COMMIT
```

| Topic | Policy |
|---|---|
| Transaction boundary | One SQL transaction for dual-write + ledger + binding/scope |
| OCC | `StokLokasi.Version` required; `StokBatch.Version` on batch qty update |
| Idempotency | Mutasi UX; Binding `LegacyBukuId`; retry returns prior success |
| Partial failure | Rollback entire UoW |
| Void | Resolve via Binding; reject if missing; no `tb_buku` delete |
| Legacy-originated changes | Catch-up before next native write (gate) |

### Feature flag

`StockLedger:CoexistenceEnabled` (name illustrative):

- `true`: freshness + dual-write + side tables required
- `false` (cutover): ledger-only writes; legacy ports no-op/disabled; Scope/Binding unused

## 14. Infrastructure and Operational Concerns

| Concern | Decision |
|---|---|
| SQL scripts | `Bilreg.SqlDb/InventoryContext/StockLedgerFeature/BILRG_Stok*.sql` |
| Migrations | Follow repo additive script practice |
| Background catch-up | Optional worker calling UC-STL-011; S1 may rely on gate-on-demand only |
| Observability | Log `TrsReffId`, scope keys, OCC conflicts, gate failures with correlation id |
| Config | Coexistence feature flag; connection uses existing Bilreg SQL config |
| Secrets | None specific |
| Health | Optional: count Inconsistent scopes |

## 15. Implementation Guidance for AI Agents

| Increment | Included use cases | Required layers | External dependencies | Verification gate |
|---|---|---|---|---|
| **S1-A** Schema + repos + OCC | — | SqlDb, Infra DAL/Repo, Domain entities skeleton | DB | Round-trip persist batch/lokasi/mutasi; unique constraints |
| **S1-B** Allocation domain | pure FEFO/FIFO/explicit ED | Domain + tests | — | Matrix tests (ED present/absent/override) |
| **S1-C** Hydrate + catch-up + gate | UC-STL-010…012 | App + Infra legacy read | `tb_buku` | Watermark + no double apply |
| **S1-D** Receipt dual-write | UC-STL-001 | Full stack | legacy write | Idempotent post; binding |
| **S1-E** Transfer | UC-STL-002 | Full stack | legacy | ED preserved; OUT=IN |
| **S1-F** Sale + Pakai | UC-STL-003, 006 | Full stack | legacy | Multi-balance allocation |
| **S1-G** Sale void + sales return | UC-STL-004, 005 | Full stack | legacy | Binding-based void; no buku delete |
| **S1-H** Reconcile query | UC-STL-020 | App + Infra | — | Includes depleted |

**Prohibitions for agents**

- Do not reuse Stock Ledger v1 aggregates/tables/design as the v2 model.
- Do not put FEFO rules in DAL or controllers.
- Do not infer void targets from qty; use Binding.
- Do not delete `tb_buku` on void; do not delete depleted `BILRG_StokLokasi`.
- Do not load entire multi-year mutasi history for a routine post.
- Do not invent Purchasing/Apotek document workflows inside Stock Ledger.
- Do not add HTTP product API unless a later ADR says so.
- Do not use `Vod*` soft-delete for movements.

## 16. Architectural Decisions

### ADR-STL-001 — Three-table core + two coexistence side tables

- **Decision:** Persist `BILRG_StokBatch`, `BILRG_StokLokasi`, `BILRG_StokMutasi` permanently; `BILRG_StokLegacyScope` + `BILRG_StokLegacyBinding` only during coexistence.
- **Status:** Accepted Target
- **Context:** Need depleted retention and per-DO reconciliation without v1’s six-table complexity.
- **Consequences:** Flat mutasi; cutover drops side tables; agents implement dual-write against this shape.
- **Rejected:** v1 Movement+Line+Position+Scope+Idempotency+Layer as mandatory permanent set.

### ADR-STL-002 — No soft-delete (`Vod*`) on Stock Ledger tables

- **Decision:** Omit `VodUser`/`VodDate`; void = reverse journal.
- **Status:** Accepted Target (exception to `DATABASE.md` §12–13 for these tables)
- **Rationale:** Soft-deleting movements contradicts append-only accountability.
- **Consequences:** Keep `Crt*`/`Upd*`; document exception in SQL headers.

### ADR-STL-003 — DateTime columns with sentinel

- **Decision:** Use `DATETIME` (`TglMasuk`, `TglMutasi`, `TglEd`, `TglMutasiLast`); empty = `3000-01-01`; map to/from legacy string tgl+jam in adapters.
- **Status:** Accepted Target

### ADR-STL-004 — Location uniqueness includes `TglEd`

- **Decision:** Unique `(StokBatchId, LayananId, TglEd)` (L1).
- **Status:** Accepted Target
- **Consequences:** FEFO can split balances per expiry within one DO+location.

### ADR-STL-005 — ID generation

- **Decision:** `NunaId.New()` for `BILRG_*`; `NunaId.NewLegacyCompact("BK"|"ST")` for new `tb_buku`/`tb_stok` ids.
- **Status:** Accepted Target
- **Consequences:** Watermark must use datetime (+ buku id tie-break), not id sortability alone.

### ADR-STL-006 — In-process only surface

- **Decision:** No product HTTP API in this architecture version.
- **Status:** Accepted Target

### ADR-STL-007 — Coexistence feature flag

- **Decision:** Runtime flag switches dual-write/gate on or off; side tables unused after cutover.
- **Status:** Accepted Target

### ADR-STL-008 — Optimistic concurrency on location balances

- **Decision:** `Version BIGINT` OCC on `BILRG_StokLokasi` (and batch qty updates).
- **Status:** Accepted Target
- **Context:** Prevent oversell under concurrent posts.

### ADR-STL-009 — Aggregate write partitioning

- **Decision:** Business aggregate remains Stock Batch; persistence loads allocation candidates / touched balances only, not full history, on routine posts.
- **Status:** Accepted Target

## 17. Open Gaps and Deferred Decisions

| ID | Gap or decision | Why it matters | Owner | Blocks | Safe interim |
|---|---|---|---|---|---|
| GAP-STL-001 | Exact DI config key name for coexistence flag | Ops consistency | Engineering | Cutover wiring | Use `StockLedger:CoexistenceEnabled` until renamed |
| GAP-STL-002 | When Purchasing/Apotek contexts call STL | E2E production path | Product + those contexts | Production go-live of native path | Implement STL + harness tests first |
| GAP-STL-003 | Full MovementKind numeric catalog | Adapter completeness | Stock Ledger | Non-S1 kinds | Define S1 kinds first; extend additively |
| GAP-STL-004 | Background catch-up worker vs gate-only | Ops lag under heavy legacy writes | Engineering | Perf under load | S1 gate-on-demand only |
| GAP-STL-005 | `NoBatch` in uniqueness | Rare split same ED different manufacturer batch | Domain | Edge FEFO | Keep out of UX; store for carry-over only |

Agents must not invent resolutions for GAP-STL-002 business ownership or change L1 uniqueness without a domain change.
