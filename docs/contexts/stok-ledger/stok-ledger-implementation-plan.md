# Stock Ledger v2 — Slice 1 Implementation Plan

**Artifact status:** Mid-tier-executable implementation plan (English)

**Bounded context:** Stock Ledger

**Audience:** Mid-tier implementation agent (Composer-class). Execute one card per focused session. Do **not** redesign domain aggregates, FEFO/FIFO policy, table shape, coexistence model, or ADRs.

**Governing business truth:** [stok-ledger-domain.md](./stok-ledger-domain.md)

**Governing technical truth:** [stok-ledger-architecture.md](./stok-ledger-architecture.md)

**Bahasa Indonesia companion (domain only):** [stok-ledger-domain-id.md](./stok-ledger-domain-id.md)

**Non-normative legacy reference:** `docs/stok-ledger/clbGenStokX1.cls` (and duplicate under this folder) — movement-kind string evidence only.

**Version scope:** Coexistence-capable Stock Ledger **v2** greenfield under `InventoryContext/StockLedgerFeature`. Existing Stock Ledger **v1** code/SQL in tree is **obsolete** and must not be extended as the v2 model (architecture C-05, ADR-STL-001).

---

## 0. Preamble

### 0.1 Purpose

This plan lets a mid-tier coding agent implement Slice 1 (**S1-A … S1-H**) incrementally **without making architectural or business decisions**. Every table column, aggregate boundary, allocation rule, void strategy, dual-write requirement, and HTTP-surface prohibition is already decided in domain + architecture. Use each Open Gap’s **Safe interim** from architecture §17; do not reopen gaps as design work.

### 0.2 Authority order (do not contradict)

1. `docs/contexts/stok-ledger/stok-ledger-domain.md`
2. `docs/contexts/stok-ledger/stok-ledger-architecture.md` — especially §5 use cases, §8 persistence, §13 coexistence/dual-write, §15 increments, §16 ADRs, §17 gaps
3. Global standards: `docs/ENGINEERING.md`, `docs/NAMING.md`, `docs/DATABASE.md`, `docs/INSTRUCTION.md`
4. Skills: `docs/skills/feature-model-generation.md`, `docs/skills/feature-persistence-generation.md`, `docs/skills/use-case-generation.md`
5. Non-normative: `docs/stok-ledger/clbGenStokX1.cls`

If something remains ambiguous after those sources, stop and list a **Planner blocker**. Do not invent columns, uniqueness keys, MovementKind catalogs beyond S1, or caller ownership.

### 0.3 Global coding constraints checklist

Every increment’s implementer MUST:

| # | Constraint | Source |
|---|---|---|
| G-01 | Place code under `{Layer}/InventoryContext/StockLedgerFeature/` with matching namespaces | ENGINEERING + architecture §3–4 |
| G-02 | Dependency: `Api → Application → Domain`; `Infrastructure → Application → Domain`. No Domain→SQL/MediatR; no Application→DAL/DTO | ENGINEERING §5 |
| G-03 | Aggregates expose behaviour (`Create`, inbound/outbound apply, reverse), not public setters mutating qty | ENGINEERING §6; feature-model-generation |
| G-04 | Persistence: DTO `FromModel`/`ToModel`; DAL explicit SQL + `AddParam`; Repo reconstructs aggregate; no AutoMapper/EF | feature-persistence-generation; DATABASE |
| G-05 | Live Inventory naming: Command/Query use `*Command` / `*Query` (match MutasiFeature); Model `{Name}Model` / `{Name}Type`; Enum `{Name}Enum`; DTO `{Name}Dto`; DAL `{Name}Dal`; Repo `{Name}Repo` | NAMING + MutasiFeature precedent |
| G-06 | Stock Ledger core tables: **omit `Vod*`**; audit `CrtUser/CrtDate/UpdUser/UpdDate` only (ADR-STL-002 exception to DATABASE §12–13). Document exception in SQL headers | DATABASE + ADR-STL-002 |
| G-07 | Sentinel empty datetime `3000-01-01`; Qty `DECIMAL(18,0)`; Hpp `DECIMAL(18,2)`; no SQL FK constraints | DATABASE + architecture §8 |
| G-08 | IDs: `NunaId.New()` for all `BILRG_*` PKs; `NunaId.NewLegacyCompact("BK"|"ST")` for new legacy `tb_buku`/`tb_stok` ids | ADR-STL-005 |
| G-09 | MediatR handlers in same file as command/query; GuardClauses for primitives; business rules in Domain | use-case-generation |
| G-10 | Tests in `Bilreg.Test/InventoryContext/StockLedgerFeature/`; xunit + FluentAssertions; DAL tests use `TransHelper.NewScope()` | repo test convention |
| G-11 | DI: MediatR auto-registers handlers; Scrutor auto-registers DAL/Repo with Nuna markers; **manually** register UoW, legacy ports, coexistence options in `InfrastructureService.cs` / `ApplicationService.cs` as needed | Api configurations |
| G-12 | SqlDb scripts under `Bilreg.SqlDb/InventoryContext/StockLedgerFeature/`; register in `Bilreg.SqlDb.sqlproj` as `<None Include="...">` (additive BILRG pattern). Prefer denormalized `TglMasuk` on `BILRG_StokLokasi` per architecture §8 amendment | architecture §8, §14 |

### 0.4 Global prohibitions (from architecture §15)

- Do **not** reuse Stock Ledger **v1** aggregates, six-table design, or v1 use-case names as the v2 model.
- Do **not** put FEFO/FIFO/Explicit-ED rules in DAL or controllers.
- Do **not** infer void targets from quantity; resolve via `BILRG_StokLegacyBinding`.
- Do **not** delete `tb_buku` on void; do **not** hard-delete depleted `BILRG_StokLokasi` (`QtySisa = 0`).
- Do **not** load entire multi-year mutasi history for a routine post (ADR-STL-009).
- Do **not** invent Purchasing/Apotek document workflows inside Stock Ledger.
- Do **not** add HTTP product API (ADR-STL-006).
- Do **not** use `Vod*` soft-delete for ledger movements.
- Do **not** grant Stock Ledger exclusive write ownership of stock while coexistence flag is on (C-01).
- Do **not** call another context’s tables/DALs as a public API; call application use cases/ports (C-04).
- Do **not** implement Reserved Order, Serah Obat / Medication Handover, purchase return, destruction, adjustment, repack, receipt void, or transfer void in S1 (unless noted as deferred backlog).
- Do **not** invent resolutions for GAP-STL-002 business ownership or change L1 uniqueness (`StokBatchId, LayananId, TglEd`) without a domain change.

### 0.5 Safe interim decisions (architecture §17) — use these; do not reopen

| Gap | Safe interim for all S1 cards |
|---|---|
| GAP-STL-001 | Config key / options: `StockLedger:CoexistenceEnabled` (bind via options type under StockLedger section as documented in each card) |
| GAP-STL-002 | Implement STL + **in-process harness tests**; do **not** wire Purchasing/Apotek production callers in S1 |
| GAP-STL-003 | Define **S1 MovementKind INT values only** (additive); map legacy strings used by S1 flows; unsupported kinds → mark scope **Inconsistent** (do not invent mappings) |
| GAP-STL-004 | **Gate-on-demand only** (UC-STL-012); no background catch-up worker in S1 |
| GAP-STL-005 | Store `NoBatch` for carry-over; **exclude** from L1 uniqueness and allocation eligibility |

### 0.6 S1 delivery set (use-case coverage)

| Use case | Increment card(s) |
|---|---|
| Foundation (schema/repos/OCC) | S1-A0, S1-A1, S1-A2 |
| Allocation domain | S1-B |
| UC-STL-010 Hydrate | S1-C1 |
| UC-STL-011 Catch-up | S1-C2 |
| UC-STL-012 Freshness gate | S1-C2 |
| UC-STL-001 Goods receipt | S1-D1, S1-D2 |
| UC-STL-002 Transfer | S1-E |
| UC-STL-003 Sale issue | S1-F1 |
| UC-STL-006 Internal consumption | S1-F2 |
| UC-STL-004 Sale void | S1-G1 |
| UC-STL-005 Sales return | S1-G2 |
| UC-STL-020 Reconcile | S1-H |
| UC-STL-021 Availability (minimal) | S1-H |

### 0.7 Recommended execution order

```text
S1-A0 → S1-A1 → S1-A2
     → S1-B  ∥  S1-C1 → S1-C2
     → S1-D1 → S1-D2 → S1-E → S1-F1 → S1-F2 → S1-G1 → S1-G2 → S1-H
```

`S1-B` may proceed in parallel with `S1-C1` after `S1-A2` is Done. `S1-H` may start a read skeleton after `S1-A2`; finalize after movements exist from C–G.

### 0.8 Whole-program Definition of Done (S1)

- [ ] Five v2 tables exist per architecture §8 (`BILRG_StokBatch`, `BILRG_StokLokasi` with denormalized `TglMasuk`, `BILRG_StokMutasi`, `BILRG_StokLegacyScope`, `BILRG_StokLegacyBinding`); no `Vod*` on ledger tables.
- [ ] Domain allocation matrix (FEFO / FIFO / Explicit ED) passes unit tests.
- [ ] Hydrate + catch-up + freshness gate: watermark ordering; no double apply; Inconsistent aborts native posts.
- [ ] Native posts UC-STL-001…006 dual-write atomically when `StockLedger:CoexistenceEnabled = true`; idempotent retries; OCC on lokasi/batch.
- [ ] Sale void uses Binding; reverse journal; no `tb_buku` delete; originals retained; depleted lokasi retained.
- [ ] Reconcile includes depleted balances; read-only.
- [ ] No product HTTP API; no production Purchasing/Apotek wiring (GAP-STL-002 interim).
- [ ] Obsolete **v1 source** under `StockLedgerFeature` removed or quarantined so new code cannot depend on it; historical docs under `docs/contexts/stok-ledger/stock-ledger-*.md` may remain as archive.
- [ ] Targeted filters for `StockLedgerFeature` v2 tests pass.

### 0.9 Planner blockers

**None.** GAP-STL-002 blocks production E2E caller wiring only, not S1 core + harness.

---

## 1. Increment cards

### S1-A0 — Retire obsolete v1 source from the feature tree

#### 1. Goal

Clear the `InventoryContext/StockLedgerFeature` folders of obsolete **v1** types (Movement/Line/Layer/Position/Scope/Idempotency model) so greenfield v2 can occupy the same feature path without mixing designs. Do **not** DROP deployed v1 SQL tables in production databases in this card; remove/stop shipping v1 scripts from the active feature path and stop compiling v1 C#.

#### 2. In scope / Out of scope

**In scope:** Delete or move-out of compile of all v1 Domain/Application/Infrastructure/Test sources under `StockLedgerFeature`; remove v1 SQL scripts from `Bilreg.SqlDb/InventoryContext/StockLedgerFeature/` and from `Bilreg.SqlDb.sqlproj` if present; leave a short `README.md` in the feature folders stating “v2 greenfield — see architecture ADR-STL-001”.

**Out of scope:** Creating v2 tables; implementing use cases; DROP TABLE against live hospital DBs; rewriting historical phase summaries under `docs/contexts/stok-ledger/stock-ledger-*.md`.

#### 3. Prerequisites

None. First card.

#### 4. Files to add or change

| Action | Path |
|---|---|
| Delete (compile tree) | `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/**` (all v1 types) |
| Delete | `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/**` (all v1) |
| Delete | `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/**` (all v1) |
| Delete | `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/**` (all v1 tests) |
| Delete / deregister | `src/bilreg/Bilreg.SqlDb/InventoryContext/StockLedgerFeature/BILRG_StokMovement*.sql`, `BILRG_StokLayer*.sql`, `BILRG_StokPosition.sql`, `BILRG_StokLedgerScope.sql`, `BILRG_StokSourceIdempotency*.sql`, `BILRG_StokLayerLegacyBinding.sql`, rollback scripts; any `sqlproj` entries for those files |
| Add | `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` (one paragraph: obsolete v1 removed; v2 per architecture) |
| Optionally remove | Manual DI registrations that only exist for v1 ports (`ILegacyCompatibilityWriterPort`, reconstruction/sync services) from `ApplicationService.cs` / `InfrastructureService.cs` if present |

**Namespaces vacated:** `Bilreg.{Domain|Application|Infrastructure|Test}.InventoryContext.StockLedgerFeature`

#### 5. Ordered steps

1. Inventory compile references: search solution for `StockLayer`, `StockPosition`, `StockMovementLine`, `ReconstructStockLedgerBaseline`, `SynchronizeStockLedgerScope`, `ILegacyCompatibilityWriterPort`.
2. Delete all v1 C# under the four `StockLedgerFeature` folders listed above.
3. Delete v1 SQL scripts from SqlDb feature folder; remove matching `sqlproj` includes.
4. Remove orphaned DI registrations that no longer compile.
5. Add placeholder README under Domain feature folder.
6. Build solution; fix only compile breaks caused by deleted v1 references (do not reintroduce v1).
7. Confirm no test project still references deleted types.

#### 6. Persistence / SQL

No new tables. Do **not** invent DROP scripts for production cutover of v1 tables in this card. v1 table cleanup is ops/backlog outside S1 implementation.

#### 7. Tests to add

| Project | Class | Scenarios / asserts |
|---|---|---|
| `Bilreg.Test` | *(none new)* | Solution builds; filter `FullyQualifiedName~StockLedgerFeature` returns **0** tests until later cards add v2 tests |

#### 8. Verification commands

```powershell
dotnet build "src/bilreg/b09-bilreg-api.sln"
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockLedgerFeature"
```

Expect build success and zero matching tests (or only empty placeholder if you add one).

#### 9. Done when

- [ ] No v1 Stock Ledger types remain in Domain/Application/Infrastructure/Test compile trees.
- [ ] Solution builds.
- [ ] Feature folder is ready for greenfield v2 names from architecture §8.

#### 10. Handoff notes

Next: **S1-A1**. Do not leave partial v1 types “for reference” in the same namespace.

---

### S1-A1 — Domain skeleton + v2 DDL (§8)

#### 1. Goal

Introduce Stock Batch aggregate skeleton and five v2 SQL scripts exactly as architecture §8 (including denormalized `TglMasuk` on lokasi). Register scripts in `Bilreg.SqlDb.sqlproj`. No FEFO algorithm yet; no legacy adapters.

#### 2. In scope / Out of scope

**In scope:** Domain models/keys/enums for batch, lokasi balance, mutasi, alignment status, binding kind, S1 MovementKind stub; DDL for five tables; sqlproj registration; SQL header noting ADR-STL-002 (no `Vod*`).

**Out of scope:** DAL/Repo implementation (S1-A2); allocation (S1-B); hydrate; dual-write; MediatR commands.

#### 3. Prerequisites

S1-A0 Done.

#### 4. Files to add or change

| Layer | Path / type |
|---|---|
| Domain | `Bilreg.Domain/InventoryContext/StockLedgerFeature/StockBatchModel.cs` |
| Domain | `LocationStockBalanceModel.cs` |
| Domain | `StockMovementModel.cs` |
| Domain | `IStockBatchKey.cs`, `IStokLokasiKey.cs`, `IStokMutasiKey.cs` |
| Domain | `StockBatchKeyType.cs` (BrgId + BrgMasukReffId) |
| Domain | `MovementKindEnum.cs` — S1 values only (see appendix) |
| Domain | `AlignmentStatusEnum.cs` — NotAligned / Aligned / Stale / Inconsistent |
| Domain | `BindingKindEnum.cs` — MutasiBuku / LokasiStok |
| Domain | `StockLedgerSentinel.cs` — `EmptyDate = 3000-01-01` |
| SqlDb | `Bilreg.SqlDb/InventoryContext/StockLedgerFeature/BILRG_StokBatch.sql` |
| SqlDb | `BILRG_StokLokasi.sql` (include denormalized `TglMasuk`) |
| SqlDb | `BILRG_StokMutasi.sql` |
| SqlDb | `BILRG_StokLegacyScope.sql` |
| SqlDb | `BILRG_StokLegacyBinding.sql` |
| SqlDb | `BILRG_StockLedgerV2_Rollback.sql` (dev/test drop of the five tables only) |
| SqlDb | Update `Bilreg.SqlDb.sqlproj` — Folder + `<None Include="InventoryContext\StockLedgerFeature\...">` |

Namespaces: `Bilreg.Domain.InventoryContext.StockLedgerFeature`

#### 5. Ordered steps

1. Create domain key interfaces and `StockBatchKeyType`.
2. Create `StockBatchModel` with factory `Create`, hospital-wide `QtySisa`, `Hpp`, `TglMasuk`, `Version`, child collection of `LocationStockBalanceModel` (private list, public `IEnumerable`).
3. Create `LocationStockBalanceModel` with identity `(StokBatchId, LayananId, TglEd)`, denormalized Brg/DO/`TglMasuk`, `QtySisa`, `Version`, `NoBatch` stored but not part of uniqueness logic.
4. Create append-only `StockMovementModel` factory ensuring exactly one of QtyIn/QtyOut > 0; `ReversesMutasiId` default `""`.
5. Enforce non-negative qty and depleted retention in domain behaviours (BR-STL-010…012) — throw on negative; do not delete zero balances.
6. Author five CREATE TABLE scripts per architecture §8 column lists only — **do not invent columns**.
7. Add indexes/UX exactly as §8: batch UX `(BrgId, BrgMasukReffId)`; lokasi UX `(StokBatchId, LayananId, TglEd)`; filtered FEFO index; mutasi UX `(TrsReffId, MovementKind, StokLokasiId)`; binding uniqueness rules.
8. Register scripts in sqlproj; document no-`Vod*` exception in each script header.
9. Apply scripts to test database (or ensure schema fixture in A2 will apply them).

#### 6. Persistence / SQL

Reference architecture **§8.1 only**. Required tables:

- `BILRG_StokBatch`
- `BILRG_StokLokasi` (+ denormalized `TglMasuk DATETIME`)
- `BILRG_StokMutasi`
- `BILRG_StokLegacyScope`
- `BILRG_StokLegacyBinding`

Do not add `VodUser`/`VodDate`. Do not add `NoBatch` to unique indexes (GAP-STL-005).

#### 7. Tests to add

| Project | Class | Scenarios / asserts |
|---|---|---|
| `Bilreg.Test` | `StockBatchDomainTest.cs` | Create batch; increase/decrease lokasi; reject negative; retain `QtySisa=0`; hospital-wide qty = Σ lokasi |
| `Bilreg.Test` | `StockMovementDomainTest.cs` | Inbound/outbound factories; reject both sides > 0; reverse reference field defaults empty |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockBatchDomainTest|FullyQualifiedName~StockMovementDomainTest"
```

#### 9. Done when

- [ ] Five SQL scripts match §8 (including lokasi `TglMasuk`).
- [ ] Domain skeleton compiles with behaviour for non-negative + depleted retention.
- [ ] Domain unit tests pass.
- [ ] sqlproj lists the new scripts.

#### 10. Handoff notes

Next: **S1-A2** (DAL/Repo/OCC). Do not start FEFO in A1.

---

### S1-A2 — DAL, repositories, OCC, UoW contract shell

#### 1. Goal

Implement Infrastructure DTO/DAL/Repo for the five tables; Application repo ports; OCC updates on lokasi/batch `Version`; insert-only mutasi; `IStockConsequenceUnitOfWork` contract shell (no dual-write body yet).

#### 2. In scope / Out of scope

**In scope:** Persistence round-trip; unique-constraint tests; OCC conflict test; schema fixture for test DB.

**Out of scope:** Legacy read/write ports; MediatR consequence handlers; allocation; hydrate.

#### 3. Prerequisites

S1-A1 Done.

#### 4. Files to add or change

| Layer | Path |
|---|---|
| Application | `IStockBatchRepo.cs` — `ISaveChange<StockBatchModel>`, load by `IStockBatchKey` / by `(BrgId,BrgMasukReffId)`, load allocation candidates by BrgId+LayananId (candidates only) |
| Application | `IStockMutasiRepo.cs` — insert-only; existence by `(TrsReffId, MovementKind, StokLokasiId)`; list by TrsReffId |
| Application | `IStockLegacyScopeRepo.cs` |
| Application | `IStockLegacyBindingRepo.cs` |
| Application | `IStockConsequenceUnitOfWork.cs` — interface + draft types only |
| Infrastructure | `StockBatchDto.cs`, `StockBatchDal.cs`, `StokLokasiDto.cs`, `StokLokasiDal.cs`, `StockBatchRepo.cs` |
| Infrastructure | `StokMutasiDto.cs`, `StokMutasiDal.cs`, `StockMutasiRepo.cs` |
| Infrastructure | `StokLegacyScopeDto.cs`, `StokLegacyScopeDal.cs`, `StockLegacyScopeRepo.cs` |
| Infrastructure | `StokLegacyBindingDto.cs`, `StokLegacyBindingDal.cs`, `StockLegacyBindingRepo.cs` |
| Test | `StockLedgerV2SchemaFixture.cs`, `StockBatchDalTest.cs`, `StokLokasiDalTest.cs`, `StokMutasiDalTest.cs`, `StockBatchRepoTest.cs`, `StockLegacyScopeRepoTest.cs`, `StockLegacyBindingRepoTest.cs` |

Namespaces: `Bilreg.Application.InventoryContext.StockLedgerFeature`, `Bilreg.Infrastructure.InventoryContext.StockLedgerFeature`, `Bilreg.Test.InventoryContext.StockLedgerFeature`

#### 5. Ordered steps

1. Implement DTOs with `FromModel`/`ToModel` using PascalCase column names matching BILRG tables (not legacy `fs_*`).
2. Implement DALs with Nuna CRUD markers; OCC update SQL: `UPDATE ... SET QtySisa=@q, Version=Version+1, Upd* WHERE Id=@id AND Version=@expected`; return/assert rows affected = 1.
3. Implement `StockBatchRepo.SaveChanges` coordinating batch + touched lokasi only; mutasi via separate insert-only repo.
4. Implement scope/binding repos.
5. Declare `IStockConsequenceUnitOfWork` with a method signature for later commit of ledger+legacy+binding draft; leave implementation throwing `NotImplementedException` or empty until S1-D1.
6. Write schema fixture that ensures five tables exist on test DB (apply scripts if missing).
7. Round-trip and constraint tests.

#### 6. Persistence / SQL

Same five tables as S1-A1. Mutasi: no Update/Delete for quantity/kind. Lokasi: never DELETE when `QtySisa=0`.

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `StockBatchDalTest` / `StokLokasiDalTest` / `StokMutasiDalTest` | Insert/Get round-trip inside `TransHelper.NewScope()` |
| `StockBatchRepoTest` | Save batch+lokasi; reload; OCC conflict when Version stale |
| `StokMutasiDalTest` | Unique `(TrsReffId, MovementKind, StokLokasiId)` second insert fails |
| `StokLokasiDalTest` | Update to `QtySisa=0` retains row |
| `StockLegacyBindingRepoTest` | Unique LegacyBukuId / StokMutasiId behaviour |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockBatchDalTest|FullyQualifiedName~StokLokasiDalTest|FullyQualifiedName~StokMutasiDalTest|FullyQualifiedName~StockBatchRepoTest|FullyQualifiedName~StockLegacy"
```

#### 9. Done when

- [ ] Round-trip persist batch/lokasi/mutasi works.
- [ ] Unique constraints and OCC conflict verified by tests.
- [ ] Depleted lokasi row is retained.
- [ ] UoW interface exists without dual-write body.

#### 10. Handoff notes

Next parallel tracks: **S1-B** (allocation) and **S1-C1** (legacy read + hydrate).

---

### S1-B — Outbound Allocation domain (FEFO / FIFO / Explicit ED)

#### 1. Goal

Implement pure-domain Outbound Allocation (BR-STL-022…027) as a domain service/function with matrix unit tests. No SQL, MediatR, or legacy.

#### 2. In scope / Out of scope

**In scope:** `StockOutboundAllocator` (or equivalent name) taking candidate balances + requested qty + optional Explicit `TglEd`; returning ordered allocation lines or insufficient-stock failure.

**Out of scope:** Loading candidates from DB; dual-write; sale handlers.

#### 3. Prerequisites

S1-A1 Done (balance candidate shape). Prefer S1-A2 Done if sharing types, but pure domain can proceed after A1.

#### 4. Files to add or change

| Path |
|---|
| `Bilreg.Domain/InventoryContext/StockLedgerFeature/StockOutboundAllocator.cs` |
| `StockAllocationCandidateType.cs` — BrgId, LayananId, StokLokasiId, StokBatchId, BrgMasukReffId, TglEd, TglMasuk, QtySisa |
| `StockAllocationLineType.cs` / `StockAllocationResult.cs` |
| `Bilreg.Test/.../StockOutboundAllocatorTest.cs` |

#### 5. Ordered steps

1. Define candidate and result types (immutable).
2. Implement selection order:
   - If Explicit ED present → filter `TglEd` equal only (BR-STL-023).
   - Else if any eligible candidate has non-sentinel ED → FEFO: ascending `TglEd`, then ascending `TglMasuk`, then `BrgMasukReffId` (BR-STL-024).
   - Else → FIFO by `TglMasuk` then `BrgMasukReffId` (BR-STL-025).
3. Exclude `QtySisa <= 0` and wrong BrgId/LayananId (BR-STL-022).
4. Ignore `NoBatch` for ordering/eligibility (GAP-STL-005).
5. Split qty across multiple balances; on shortfall return insufficient result — never negative (BR-STL-027).
6. Write matrix unit tests.

#### 6. Persistence / SQL

None.

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `StockOutboundAllocatorTest` | FEFO with EDs; FIFO when all ED sentinel; Explicit ED override ignores other EDs; same-ED tie-break by TglMasuk; multi-balance split; insufficient qty; depleted excluded |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockOutboundAllocatorTest"
```

#### 9. Done when

- [ ] Matrix tests pass for ED present / absent / override.
- [ ] No DAL/controller contains allocation ordering.

#### 10. Handoff notes

Allocation is consumed by S1-E / S1-F*. Keep pure.

---

### S1-C1 — Legacy read port + UC-STL-010 Hydrate

#### 1. Goal

Implement `ILegacyStockReadPort` against `tb_stok`/`tb_buku` (parameterized, Item+DO scope) and UC-STL-010 hydrate that establishes ledger baseline for one `(BrgId, BrgMasukReffId)` without mutating legacy.

#### 2. In scope / Out of scope

**In scope:** Read port; hydrate command/handler; scope row Aligned + watermark init; binding for applied legacy buku rows; S1 MovementKind mapping for kinds encountered in hydrate; unsupported kind → Inconsistent.

**Out of scope:** Catch-up incremental watermark (S1-C2); native dual-write; background worker.

#### 3. Prerequisites

S1-A2 Done.

#### 4. Files to add or change

| Path |
|---|
| `Application/.../Ports/ILegacyStockReadPort.cs` |
| `Application/.../UseCases/HydrateScopeFromLegacyCommand.cs` (+ Handler, Result, OutcomeEnum) |
| `Application/.../LegacyMovementKindMapper.cs` — string → `MovementKindEnum` for S1 kinds only |
| `Infrastructure/.../LegacyStockReadPort.cs` — do **not** reuse defective cross-DO list patterns from `tb_buku_dal` blindly; write scoped SQL Item+DO |
| `Test/.../LegacyStockReadPortTest.cs`, `HydrateScopeFromLegacyHandlerTest.cs`, `LegacyMovementKindMapperTest.cs` |
| Optional DI | Manual `services.AddScoped<ILegacyStockReadPort, LegacyStockReadPort>()` in `InfrastructureService.cs` |

#### 5. Ordered steps

1. Define read DTOs for balance and journal projections needed by hydrate (Application-facing types, not Infrastructure DTOs leaked upward).
2. Implement `ListBalances(BrgId, BrgMasukReffId)` and `ListJournals(...)` ordered for replay: compose datetime from legacy tgl+jam (and `fd_tgl_jam_mutasi` when present), then `fs_kd_trs`.
3. Map legacy columns: `fs_kd_barang`→BrgId, `fs_kd_do`→BrgMasukReffId, `fs_kd_layanan`→LayananId, `fn_hpp`→Hpp, ED sentinel, qty in/out.
4. Hydrate handler: if scope already Aligned with data, define deterministic idempotent behaviour (no duplicate mutasi — use Binding unique on LegacyBukuId).
5. Replay journals into batch/lokasi/mutasi establishing conservation; retain depleted balances when journal net is zero at a location.
6. Set `AlignmentStatus=Aligned`, `TglMutasiLast`, `LastLegacyBukuId`.
7. On unmapped MovementKind string: set Inconsistent + reason; do not invent kind.
8. Tests with fakes and/or live test DB seeded buku/stok rows.

#### 6. Persistence / SQL

Writes: `BILRG_StokBatch`, `BILRG_StokLokasi`, `BILRG_StokMutasi`, `BILRG_StokLegacyScope`, `BILRG_StokLegacyBinding`. Reads: `tb_buku`, `tb_stok` only via port.

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `LegacyMovementKindMapperTest` | S1 strings map; unknown → fail/Inconsistent signal |
| `HydrateScopeFromLegacyHandlerTest` | Empty scope → Aligned empty/with rows; replay DO inbound establishes batch; depleted retained; double hydrate idempotent via binding; unknown kind → Inconsistent |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~HydrateScopeFromLegacy|FullyQualifiedName~LegacyStockReadPortTest|FullyQualifiedName~LegacyMovementKindMapperTest"
```

#### 9. Done when

- [ ] Hydrate establishes ledger from legacy without writing legacy.
- [ ] Idempotent re-hydrate does not double qty.
- [ ] Unknown kinds mark Inconsistent.

#### 10. Handoff notes

Next: **S1-C2** catch-up + UC-STL-012 gate.

---

### S1-C2 — Catch-up (UC-STL-011) + Freshness Gate (UC-STL-012)

#### 1. Goal

Apply newer legacy buku rows after watermark; expose Ensure Freshness for scopes touched by native posts. Gate-on-demand only (GAP-STL-004).

#### 2. In scope / Out of scope

**In scope:** CatchUp command; EnsureFreshness command (calls hydrate if NotAligned, catch-up if Stale); Stale detection when legacy has rows beyond watermark; abort outcome when Inconsistent.

**Out of scope:** Background worker; native dual-write posts.

#### 3. Prerequisites

S1-C1 Done.

#### 4. Files to add or change

| Path |
|---|
| `UseCases/CatchUpScopeFromLegacyCommand.cs` |
| `UseCases/EnsureFreshnessForScopesCommand.cs` |
| `LegacyFreshnessGate.cs` (Application helper used by later posts) |
| `StockLedgerCoexistenceOptions.cs` — property `CoexistenceEnabled`; section name supporting key `StockLedger:CoexistenceEnabled` (GAP-STL-001) |
| Tests: `CatchUpScopeFromLegacyHandlerTest.cs`, `EnsureFreshnessForScopesHandlerTest.cs`, `LegacyFreshnessGateTest.cs` |
| DI: `Configure<StockLedgerCoexistenceOptions>` |

#### 5. Ordered steps

1. Implement catch-up predicate exactly per architecture §8: datetime `>` watermark OR equal datetime AND `fs_kd_trs > LastLegacyBukuId`, scoped by barang+do. **Do not** sort by id alone across writers.
2. Append mutasi + update balances; insert bindings; advance watermark.
3. Binding unique on `LegacyBukuId` prevents double apply.
4. EnsureFreshness: for each scope key — NotAligned→Hydrate; else CatchUp; if Inconsistent return abort outcome.
5. When coexistence flag false (cutover mode): gate may no-op per ADR-STL-007 (document in code comments); S1 tests primarily cover `true`.
6. Tests: watermark order; no double apply; Stale→Aligned; Inconsistent fails gate.

#### 6. Persistence / SQL

Same as C1; watermark columns on `BILRG_StokLegacyScope` only as §8.

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `CatchUpScopeFromLegacyHandlerTest` | Applies only post-watermark rows; tie-break by buku id; idempotent |
| `EnsureFreshnessForScopesHandlerTest` | Hydrates NotAligned; catch-up Stale; aborts Inconsistent |
| `LegacyFreshnessGateTest` | Multi-scope call aggregates outcomes |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~CatchUpScopeFromLegacy|FullyQualifiedName~EnsureFreshnessForScopes|FullyQualifiedName~LegacyFreshnessGateTest"
```

#### 9. Done when

- [ ] Watermark + no double apply verified.
- [ ] Gate aborts Inconsistent scopes.
- [ ] No background worker introduced.

#### 10. Handoff notes

Every native post S1-D+ must call UC-STL-012 before UoW (§13).

---

### S1-D1 — Legacy writer port + atomic `IStockConsequenceUnitOfWork`

#### 1. Goal

Implement `ILegacyStockWriterPort` for S1 write shapes and a real `StockConsequenceUnitOfWork` that commits ledger + legacy + binding + scope watermark in **one SQL transaction**, with OCC.

#### 2. In scope / Out of scope

**In scope:** Writer methods needed for receipt (and extensible for later OUT/IN/void reverse-insert); UoW commit; rollback on any failure; ID generation ADR-STL-005; void path rule: **never delete buku**.

**Out of scope:** Full UC-STL-001 handler (S1-D2); sale/transfer commands.

#### 3. Prerequisites

S1-A2 + S1-C2 Done (gate exists; scope/binding repos exist).

#### 4. Files to add or change

| Path |
|---|
| `Application/.../Ports/ILegacyStockWriterPort.cs` |
| `Application/.../StockConsequenceUnitOfWork.cs` (or Infra impl registered to Application interface — prefer Application orchestration calling ports inside one ambient/explicit TX opened by UoW) |
| `Infrastructure/.../LegacyStockWriterPort.cs` |
| Tests: `LegacyStockWriterPortTest.cs`, `StockConsequenceUnitOfWorkTest.cs`, `StockConsequenceUnitOfWorkLiveAtomicityTest.cs` |
| DI: register writer + UoW scoped |

#### 5. Ordered steps

1. Define draft DTO for UoW: mutasi inserts, lokasi/batch upserts with expected Version, binding rows, scope watermark update, legacy buku/stok operations.
2. Implement legacy inbound insert: INSERT `tb_buku` then INSERT `tb_stok` with `NewLegacyCompact` ids; map datetime↔legacy tgl/jam strings.
3. Implement outbound deplete for later cards: UPDATE/DELETE **stok** rows as legacy does; INSERT reverse/out buku; **never DELETE buku for void**.
4. UoW: BEGIN → write legacy → write BILRG_* → binding → scope → OCC updates → COMMIT; any failure ROLLBACK.
5. When `CoexistenceEnabled=false`: skip legacy writes (ledger-only) per ADR-STL-007.
6. Live atomicity test: inject failure after legacy write → assert no partial commit.

#### 6. Persistence / SQL

Ledger tables §8 + legacy `tb_buku`/`tb_stok` column widths from SqlDb StokFeature scripts. Do not add ledger columns.

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `LegacyStockWriterPortTest` | Inbound creates buku+stok; ids length 10; void/reverse insert does not delete buku |
| `StockConsequenceUnitOfWorkTest` | Happy path commits binding; OCC fail rolls back |
| `StockConsequenceUnitOfWorkLiveAtomicityTest` | Forced mid-flight failure leaves neither side committed |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~LegacyStockWriterPortTest|FullyQualifiedName~StockConsequenceUnitOfWork"
```

#### 9. Done when

- [ ] Single TX dual-write path works with rollback.
- [ ] No buku delete helper exists for void.
- [ ] Flag false skips legacy writes.

#### 10. Handoff notes

Next: **S1-D2** UC-STL-001 command on top of UoW + gate.

---

### S1-D2 — UC-STL-001 Post Goods Receipt Consequence

#### 1. Goal

MediatR command posts inbound receipt: freshness gate → apply domain → dual-write UoW → binding; idempotent on `(TrsReffId, MovementKind, StokLokasiId)`.

#### 2. In scope / Out of scope

**In scope:** `PostGoodsReceiptConsequenceCommand` + handler + harness tests.

**Out of scope:** Purchasing document UI/API; FO HPP derivation (caller supplies unit Hpp); HTTP.

#### 3. Prerequisites

S1-D1 Done; S1-C2 Done.

#### 4. Files to add or change

| Path |
|---|
| `UseCases/PostGoodsReceiptConsequenceCommand.cs` |
| Domain behaviours used: establish/increase batch & lokasi, inbound mutasi `MovementKind` receipt (DO) |
| Tests: `PostGoodsReceiptConsequenceHandlerTest.cs` |
| Fakes under `Test/.../Fakes/` as needed |

#### 5. Ordered steps

1. Command fields: BrgId, BrgMasukReffId, LayananId, Qty, Hpp, optional TglEd, TrsReffId, TglMutasi, PoReffId optional, UserId, optional NoBatch.
2. Guard primitives; call EnsureFreshness for scope; abort if Inconsistent.
3. Load or create batch by `(BrgId, BrgMasukReffId)`; apply inbound to lokasi UX key including TglEd sentinel if absent.
4. Build mutasi inbound; build legacy writer ops; build bindings.
5. Commit UoW.
6. Idempotent retry: if mutasi UX exists, return prior success without double qty.
7. Tests covering qty/Hpp, dual-write, idempotent retry, gate abort, second receipt same DO increases balance.

#### 6. Persistence / SQL

§8 tables + legacy via UoW. MovementKind = S1 receipt (DO).

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `PostGoodsReceiptConsequenceHandlerTest` | Success creates batch+lokasi+mutasi+binding+legacy; idempotent retry; Inconsistent scope aborts; OCC conflict surfaced |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostGoodsReceiptConsequenceHandlerTest"
```

#### 9. Done when

- [ ] Idempotent post + binding verified.
- [ ] No HTTP controller added.

#### 10. Handoff notes

Next: **S1-E** transfer using allocation/provenance + same envelope.

---

### S1-E — UC-STL-002 Post Stock Transfer Consequence

#### 1. Goal

Move quantity between Layanan locations preserving BrgMasukReffId, Hpp, TglEd; OUT=IN; hospital-wide batch qty unchanged; dual-write MT_OUT + MT_IN.

#### 2. In scope / Out of scope

**In scope:** Transfer command/handler; source allocation or explicit provenance as supplied by command; OCC; gate.

**Out of scope:** MutasiFeature approval workflow ownership; transfer void.

#### 3. Prerequisites

S1-A2, S1-B, S1-C2, S1-D2 (or hydrated stock at source).

#### 4. Files to add or change

| Path |
|---|
| `UseCases/PostStockTransferConsequenceCommand.cs` |
| Tests: `PostStockTransferConsequenceHandlerTest.cs` |

#### 5. Ordered steps

1. Command: BrgId, Qty, SourceLayananId, DestLayananId, TrsReffId, TglMutasi, UserId, optional Explicit ED / optional explicit BrgMasukReffId list.
2. Freshness for all touched scopes (each receipt source consumed).
3. Load candidates at source; allocate via S1-B (or explicit provenance).
4. For each allocation line: outbound mutasi + inbound mutasi same qty/ED/Hpp/DO; update source/dest lokasi; batch hospital qty unchanged.
5. Dual-write legacy OUT then IN legs; bindings; OCC.
6. Idempotency per `(TrsReffId, Kind, StokLokasiId)` for each leg.
7. Tests: ED preserved; OUT=IN; batch qty unchanged; multi-balance; OCC; depleted source retained.

#### 6. Persistence / SQL

§8 + legacy. Kinds: MT_OUT, MT_IN.

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `PostStockTransferConsequenceHandlerTest` | ED preserved; OUT=IN; hospital QtySisa unchanged; insufficient rejects; idempotent |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostStockTransferConsequenceHandlerTest"
```

#### 9. Done when

- [ ] ED preserved; OUT=IN; OCC covered.

#### 10. Handoff notes

Next: **S1-F1** sale issue.

---

### S1-F1 — UC-STL-003 Post Sale Issue Consequence

#### 1. Goal

Outbound sale at one Layanan via Outbound Allocation (Explicit ED → FEFO → FIFO); multi-batch dual-write; legacy kinds DB/DU/DT as selected by command discriminator.

#### 2. In scope / Out of scope

**In scope:** Sale issue command; allocation; multi-balance mutasi; bindings.

**Out of scope:** Apotek invoice documents; HTTP; sale void (S1-G1); pakai (S1-F2).

#### 3. Prerequisites

S1-B, S1-C2, stock via S1-D2 and/or S1-E / hydrate.

#### 4. Files to add or change

| Path |
|---|
| `UseCases/PostSaleIssueConsequenceCommand.cs` — include sale subtype for DB/DU/DT mapping |
| Tests: `PostSaleIssueConsequenceHandlerTest.cs` |

#### 5. Ordered steps

1. Command: BrgId, LayananId, Qty, TrsReffId, TglMutasi, UserId, optional Explicit TglEd, SaleKind discriminator → MovementKind DB|DU|DT.
2. Gate all scopes that may be touched (may require candidate load first, then gate those DOs).
3. Load `QtySisa>0` candidates only (ADR-STL-009); allocate; apply outbound per batch aggregate in one UoW.
4. Dual-write each issue line; bindings; OCC.
5. Insufficient → reject; no negative.
6. Idempotent retry.

#### 6. Persistence / SQL

§8 + legacy. S1 kinds DB/DU/DT only for this card.

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `PostSaleIssueConsequenceHandlerTest` | FEFO multi-balance; Explicit ED; FIFO no-ED; insufficient; dual-write lines; idempotent |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostSaleIssueConsequenceHandlerTest"
```

#### 9. Done when

- [ ] Multi-balance allocation + dual-write verified.

#### 10. Handoff notes

Next: **S1-F2** reuse allocation path with kind PK.

---

### S1-F2 — UC-STL-006 Post Internal Consumption Consequence

#### 1. Goal

Pakai barang outbound using same allocation + dual-write envelope as sale; MovementKind `PK`.

#### 2. In scope / Out of scope

**In scope:** Consumption command/handler/tests.

**Out of scope:** Destruction, adjustment; consumption void.

#### 3. Prerequisites

S1-F1 Done (shared orchestration) or S1-B+C2+D2 with shared helper extracted in F1.

#### 4. Files to add or change

| Path |
|---|
| `UseCases/PostInternalConsumptionConsequenceCommand.cs` |
| Shared helper if needed: `OutboundIssueConsequenceOrchestrator.cs` (Application) — only if it reduces duplication without ceremony |
| Tests: `PostInternalConsumptionConsequenceHandlerTest.cs` |

#### 5. Ordered steps

1. Mirror F1 flow with kind PK and command name Internal Consumption.
2. Reuse allocator + UoW + gate.
3. Tests: allocation + dual-write + insufficient + idempotent.

#### 6. Persistence / SQL

§8 + legacy; kind PK.

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `PostInternalConsumptionConsequenceHandlerTest` | Happy path PK; multi-balance; reject insufficient |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostInternalConsumptionConsequenceHandlerTest"
```

#### 9. Done when

- [ ] PK path passes; no duplicate FEFO logic in DAL.

#### 10. Handoff notes

Next: **S1-G1** sale void (requires sale bindings from F1).

---

### S1-G1 — UC-STL-004 Post Sale Void Consequence

#### 1. Goal

Reverse-journal prior sale using **Binding** lookup; retain originals; dual-write reverse buku inserts; never delete `tb_buku`; reject if unbound or already reversed.

#### 2. In scope / Out of scope

**In scope:** Sale void command; `ReversesMutasiId`; balance restore when valid.

**Out of scope:** Receipt void; transfer void; inferring targets by qty match.

#### 3. Prerequisites

S1-F1 Done (sale mutasi + bindings exist).

#### 4. Files to add or change

| Path |
|---|
| `UseCases/PostSaleVoidConsequenceCommand.cs` |
| Tests: `PostSaleVoidConsequenceHandlerTest.cs` |

#### 5. Ordered steps

1. Command: original TrsReffId (sale), UserId, TglMutasi, void TrsReffId if distinct per caller contract — follow architecture: resolve movements via Binding for that sale TrsReffId / mutasi ids.
2. Gate scopes.
3. Load original mutasi by TrsReffId; for each, require Binding; reject if missing.
4. Insert reverse mutasi with opposite direction, `ReversesMutasiId` set; restore lokasi qty; OCC.
5. Legacy: insert compensating `*_V` buku; update/recreate stok as writer rules; **no DELETE buku**.
6. Idempotent if reverse already present.
7. Tests: originals untouched; unbound reject; no buku delete; qty restored.

#### 6. Persistence / SQL

§8 `ReversesMutasiId`; Binding mandatory; legacy reverse insert only.

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `PostSaleVoidConsequenceHandlerTest` | Reverse journal; originals retained; unbound rejected; buku row count not decreased for originals; balance restored |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostSaleVoidConsequenceHandlerTest"
```

#### 9. Done when

- [ ] Binding-based void; no buku delete; originals visible.

#### 10. Handoff notes

Next: **S1-G2** sales return (inbound restore path).

---

### S1-G2 — UC-STL-005 Post Sales Return Consequence

#### 1. Goal

Inbound restore of previously issued sale quantity under authorized return; dual-write; preserve Receipt Source; kinds RJ/RU/RT per command discriminator.

#### 2. In scope / Out of scope

**In scope:** Sales return command/handler/tests.

**Out of scope:** Purchase return; inventing DO if unknown — command must supply provenance keys required by domain.

#### 3. Prerequisites

S1-D1 UoW; typically after F1 so return can target known ED/DO; may restore into lokasi UX key.

#### 4. Files to add or change

| Path |
|---|
| `UseCases/PostSalesReturnConsequenceCommand.cs` |
| Tests: `PostSalesReturnConsequenceHandlerTest.cs` |

#### 5. Ordered steps

1. Command includes BrgId, LayananId, Qty, BrgMasukReffId, TglEd, Hpp, TrsReffId, return kind RJ|RU|RT, UserId, TglMutasi.
2. Gate scope; inbound apply like receipt but MovementKind return; dual-write; binding; idempotent.
3. Tests: increases correct lokasi/batch; dual-write; idempotent.

#### 6. Persistence / SQL

§8 + legacy; kinds RJ/RU/RT.

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `PostSalesReturnConsequenceHandlerTest` | Inbound restore; provenance preserved; idempotent |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostSalesReturnConsequenceHandlerTest"
```

#### 9. Done when

- [ ] Return path dual-writes and preserves Receipt Source.

#### 10. Handoff notes

Next: **S1-H** reconcile + minimal availability.

---

### S1-H — UC-STL-020 Reconcile Scope + minimal UC-STL-021 Availability

#### 1. Goal

Read-only reconcile for `(BrgId, BrgMasukReffId)` ± optional LayananId including **depleted** balances; optional legacy compare when coexistence on. Minimal availability query for allocation preview/tests.

#### 2. In scope / Out of scope

**In scope:** `ReconcileScopeQuery`, `GetAvailabilityAtLocationQuery`; projection SQL; tests.

**Out of scope:** Silent repair; steward HTTP UI; scheduled reconcile worker.

#### 3. Prerequisites

S1-A2 Done; meaningful data from C–G preferred for integration tests.

#### 4. Files to add or change

| Path |
|---|
| `UseCases/ReconcileScopeQuery.cs` |
| `UseCases/GetAvailabilityAtLocationQuery.cs` |
| Infrastructure projection helpers as needed (no business transitions) |
| Tests: `ReconcileScopeQueryTest.cs`, `GetAvailabilityAtLocationQueryTest.cs` |

#### 5. Ordered steps

1. Reconcile: sum movements vs Σ lokasi including QtySisa=0; compare hospital conservation; optionally compare legacy for aligned scopes; emit differences explicitly (BR-STL-035).
2. Availability: list QtySisa>0 candidates at BrgId+LayananId ordered for preview (must not authorize sale).
3. Tests: conserved pass; depleted included; mismatch reported; empty scope deterministic.

#### 6. Persistence / SQL

Read `BILRG_StokBatch`, `BILRG_StokLokasi`, `BILRG_StokMutasi`; optional legacy read port. No writes.

#### 7. Tests to add

| Class | Scenarios / asserts |
|---|---|
| `ReconcileScopeQueryTest` | Includes depleted; flags mismatch; read-only |
| `GetAvailabilityAtLocationQueryTest` | Returns positive balances only; filter by optional TglEd |

#### 8. Verification commands

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~ReconcileScopeQueryTest|FullyQualifiedName~GetAvailabilityAtLocationQueryTest"
```

Also run full feature filter:

```powershell
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockLedgerFeature"
```

#### 9. Done when

- [ ] Reconcile includes depleted.
- [ ] No mutation from queries.
- [ ] Whole S1 program DoD checklist in §0.8 can be checked.

#### 10. Handoff notes

S1 complete for core. Production caller wiring is **out of S1** (GAP-STL-002). Deferred backlog in §3.

---

## 2. Cross-cutting appendix

### 2.1 ID generation

| Target | API |
|---|---|
| All `BILRG_*` PKs (`StokBatchId`, `StokLokasiId`, `StokMutasiId`, `BindingId`) | `NunaId.New()` |
| New `tb_buku.fs_kd_trs` | `NunaId.NewLegacyCompact("BK")` |
| New `tb_stok.fs_kd_trs` | `NunaId.NewLegacyCompact("ST")` |

Watermark must use datetime (+ `LastLegacyBukuId` tie-break), **not** id sortability alone (ADR-STL-005).

### 2.2 Coexistence flag (safe interim name)

- Config: **`StockLedger:CoexistenceEnabled`** (GAP-STL-001).
- Options type example: `StockLedgerCoexistenceOptions` with bool `CoexistenceEnabled`, bound from configuration section `StockLedger` (property name `CoexistenceEnabled` → key `StockLedger:CoexistenceEnabled`).
- `true`: freshness + dual-write + side tables required.
- `false` (cutover): ledger-only writes; legacy ports no-op; Scope/Binding unused.

### 2.3 Dual-write / Freshness Gate / Binding void (reference only — do not redesign)

Native post path (architecture §13):

```text
UC-STL-012 Ensure Freshness (scopes touched)
  -> Domain allocate / apply (in memory)
  -> UoW BEGIN
       write tb_buku / tb_stok (NewLegacyCompact; void = reverse insert only)
       write BILRG_StokBatch / StokLokasi / StokMutasi
       write Binding + update Scope watermark
       OCC UPDATE StokLokasi WHERE Version = expected
     COMMIT
```

- Void: resolve via Binding; reject if missing; no `tb_buku` delete; set `ReversesMutasiId`.
- Depleted `BILRG_StokLokasi` retained at zero.
- Routine posts load allocation candidates only, not full history.

### 2.4 S1 MovementKind INT catalog (GAP-STL-003 interim)

Assign additive INT values in `MovementKindEnum` in S1-A1; map legacy strings in ACL. Minimum S1 set:

| Enum member (suggested name) | Legacy string | Used by |
|---|---|---|
| GoodsReceipt | `DO` | UC-STL-001, hydrate |
| TransferOut | `MT_OUT` | UC-STL-002 |
| TransferIn | `MT_IN` | UC-STL-002 |
| SaleIssueDb | `DB` | UC-STL-003 |
| SaleIssueDu | `DU` | UC-STL-003 |
| SaleIssueDt | `DT` | UC-STL-003 |
| InternalConsumption | `PK` | UC-STL-006 |
| SalesReturnRj | `RJ` | UC-STL-005 |
| SalesReturnRu | `RU` | UC-STL-005 |
| SalesReturnRt | `RT` | UC-STL-005 |
| SaleVoidDb | `DB_V` | UC-STL-004 |
| SaleVoidDu | `DU_V` | UC-STL-004 |
| SaleVoidDt | `DT_V` | UC-STL-004 |

Hydrate/catch-up may encounter additional legacy strings from `clbGenStokX1.cls`. For any string **not** in the S1 enum map: mark scope **Inconsistent** with reason; do not invent a kind. Extend additively in later slices.

### 2.5 Suggested PR / commit boundaries

| Card | Suggested PR title |
|---|---|
| S1-A0 | `stl: retire obsolete Stock Ledger v1 sources` |
| S1-A1 | `stl: v2 domain skeleton + BILRG_Stok* DDL` |
| S1-A2 | `stl: v2 DAL/repos/OCC` |
| S1-B | `stl: outbound allocation FEFO/FIFO domain` |
| S1-C1 | `stl: legacy read + hydrate scope` |
| S1-C2 | `stl: catch-up + freshness gate` |
| S1-D1 | `stl: legacy writer + consequence UoW` |
| S1-D2 | `stl: post goods receipt consequence` |
| S1-E | `stl: post stock transfer consequence` |
| S1-F1 | `stl: post sale issue consequence` |
| S1-F2 | `stl: post internal consumption consequence` |
| S1-G1 | `stl: post sale void consequence` |
| S1-G2 | `stl: post sales return consequence` |
| S1-H | `stl: reconcile + availability queries` |

Prefer one PR per card. Do not combine A0 with D2.

### 2.6 Reference patterns (approved)

| Concern | Mirror |
|---|---|
| MediatR command/handler file layout | `MutasiFeature/UseCases/OrderMutasiAddItemCommand.cs` |
| DTO/DAL | `MutasiFeature/OrderMutasiDto.cs`, `OrderMutasiDal.cs` |
| DAL tests | `Bilreg.Test/.../MutasiFeature/OrderMutasiDalTest.cs` |
| Legacy table shapes | `Bilreg.SqlDb/InventoryContext/StokFeature/tb_stok.sql`, `tb_buku.sql` |
| DI Scrutor | `Bilreg.Api/Configurations/InfrastructureService.cs` |

### 2.7 Default verification (any card)

```powershell
dotnet build "src/bilreg/b09-bilreg-api.sln"
dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockLedgerFeature"
```

---

## 3. Explicit non-goals & deferred backlog

Mid-tier agents **must not** implement in S1:

| Item | Reason |
|---|---|
| Product HTTP APIs / controllers for Stock Ledger | ADR-STL-006 |
| Purchasing / Apotek / Mutasi production caller wiring | GAP-STL-002 interim |
| Reserved Order stock consequence | Domain deferred BR-STL-036 |
| Serah Obat / Medication Handover (`DS`) | Domain deferred BR-STL-036 |
| Purchase return, destruction, adjustment, repack | Architecture §5 deferred |
| Receipt void, transfer void | Deferred; only sale void pattern in S1 |
| Background catch-up worker | GAP-STL-004 interim |
| Full MovementKind catalog beyond S1 | GAP-STL-003 interim |
| Putting `NoBatch` into L1 uniqueness | GAP-STL-005 |
| Reusing v1 six-table model / reconstruction/synchronize use cases as v2 | C-05 / ADR-STL-001 |
| Soft-delete (`Vod*`) on ledger movements | ADR-STL-002 |
| Silent repair of reconciliation differences | BR-STL-035 |
| Exclusive write ownership of `tb_stok`/`tb_buku` while coexistence on | C-01 |
| DROP of coexistence side tables (cutover ops) | Post-S1 cutover |
| Production DROP of obsolete v1 BILRG tables | Ops backlog; not S1 coding |

---

## 4. Traceability checklist (for reviewers)

| Architecture §15 | Plan cards |
|---|---|
| S1-A | S1-A0 + S1-A1 + S1-A2 |
| S1-B | S1-B |
| S1-C | S1-C1 + S1-C2 |
| S1-D | S1-D1 + S1-D2 |
| S1-E | S1-E |
| S1-F | S1-F1 + S1-F2 |
| S1-G | S1-G1 + S1-G2 |
| S1-H | S1-H |

End of plan.
