# Stock Ledger S1-A2 — Implementation Summary

## 1. Slice identity
- Slice ID: S1-A2
- Plan card title: DAL, repositories, OCC, UoW contract shell
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Implemented Infrastructure DTO/DAL/Repo for the five Stock Ledger v2 tables, Application repo ports, OCC updates on batch/lokasi `Version` via `PersistedVersion` + assign-final-Version conditional UPDATE, insert-only mutasi, and `IStockConsequenceUnitOfWork` contract shell (no dual-write body). Schema fixture applies the five CREATE scripts on the test DB; round-trip, unique-constraint, depleted-retention, same-delta OCC conflict, and multi-mutation one-SaveChanges tests pass.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/IStockLegacyScopeKey.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/IStockLegacyBindingKey.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockLegacyScopeModel.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockLegacyBindingModel.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockBatchRepo.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockMutasiRepo.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockLegacyScopeRepo.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockLegacyBindingRepo.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockConsequenceUnitOfWork.cs` (+ `StockConsequenceDraft`)
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockBatchDto.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockBatchDal.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StokLokasiDto.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StokLokasiDal.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StokMutasiDto.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StokMutasiDal.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StokLegacyScopeDto.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StokLegacyScopeDal.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StokLegacyBindingDto.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StokLegacyBindingDal.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockBatchRepo.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockMutasiRepo.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockLegacyScopeRepo.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockLegacyBindingRepo.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockConsequenceUnitOfWork.cs` (throws `NotImplementedException`)
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockLedgerV2SchemaFixture.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockBatchDalTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StokLokasiDalTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StokMutasiDalTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockBatchRepoTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockLegacyScopeRepoTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockLegacyBindingRepoTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-A2-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/LocationStockBalanceModel.cs` — `IncreaseQty` / `DecreaseQty` → `internal`; `PersistedVersion` + `AcceptPersisted` for OCC baseline
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockBatchModel.cs` — `PersistedVersion` + `AcceptPersisted` for OCC baseline
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockBatchDal.cs` / `StokLokasiDal.cs` — OCC UPDATE assigns final `Version = @Version`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockBatchRepo.cs` — OCC on `PersistedVersion`; dirty rows always UpdateConditional
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — next-card pointer to S1-B / S1-C1
- `src/bilreg/Bilreg.Test/.../StockBatchRepoTest.cs` — same-delta OCC + multi-mutation one-SaveChanges regressions
- `src/bilreg/Bilreg.Test/.../StockBatchDalTest.cs` / `StokLokasiDalTest.cs` — assert assign-final Version

### Removed
- none

## 4. Behaviour / contracts implemented
- Persistence: five tables from architecture §8.1 via DTO/DAL/Repo (ADR-STL-002: no `Vod*` on DTOs/SQL).
- `IStockBatchRepo.SaveChanges`: upsert batch + touched lokasi only; OCC via `UpdateConditional` (`SET Version=@Version WHERE Version=@PersistedVersion`); dirty = `Version != PersistedVersion`; never treat equal Version+Qty as success after mutation; never DELETE depleted lokasi (BR-STL-011, BR-STL-012). Multi-mutation in memory then one SaveChanges persists final qty/versions (architecture §13).
- `IStockMutasiRepo`: insert-only; existence by `(TrsReffId, MovementKind, StokLokasiId)`; list by `TrsReffId`.
- `IStockLegacyScopeRepo` / `IStockLegacyBindingRepo`: scope watermark upsert; binding insert + lookups; unique `LegacyBukuId` / `StokMutasiId` enforced by filtered indexes.
- `ListAllocationCandidates`: `BrgId + LayananId AND QtySisa > 0` only — no FEFO/FIFO ordering (S1-B).
- `IStockConsequenceUnitOfWork` + `StockConsequenceDraft`: contract shell; stub throws until S1-D1.
- ID prefixes: `STB` / `STL` / `STM` (A1); BindingId `SBD` (A2).
- Denormalized lokasi `TglMasuk` preserved on insert/update; `NoBatch` not in L1 uniqueness (GAP-STL-005).
- Coexistence / dual-write / gate: not implemented (out of A2 scope).
- Safe interim: GAP-STL-005 only (no new gap choices).

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `StockBatchDalTest` | Insert/Get round-trip; OCC UpdateConditional assigns final Version | Pass (2) |
| `StokLokasiDalTest` | Insert/Get + TglMasuk; QtySisa=0 retains row | Pass (2) |
| `StokMutasiDalTest` | Insert/Exists/List; unique idempotency key rejects duplicate | Pass (2) |
| `StockBatchRepoTest` | Save+reload; update qty; divergent OCC; same-delta OCC; multi-mutation one save; depleted retained; natural key; allocation candidates exclude depleted | Pass (8) |
| `StockLegacyScopeRepoTest` | Insert/load/update watermark | Pass (1) |
| `StockLegacyBindingRepoTest` | Round-trip lookups; duplicate LegacyBukuId; duplicate StokMutasiId | Pass (3) |

## 6. Verification evidence
- Commands run:
  - `dotnet build "src/bilreg/b09-bilreg-api.sln"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockBatchDalTest|FullyQualifiedName~StokLokasiDalTest|FullyQualifiedName~StokMutasiDalTest|FullyQualifiedName~StockBatchRepoTest|FullyQualifiedName~StockLegacy"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockBatchDomainTest|FullyQualifiedName~StockMovementDomainTest"`
- Outcome (pass/fail): Pass — A2 filter Failed: 0, Passed: 18; A1 domain Failed: 0, Passed: 12
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice; schema fixture applied five CREATE scripts to `devTest` when missing

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **No `Vod*` on DTOs/tables:** ADR-STL-002 exception to DATABASE soft-delete columns — audit is Crt/Upd only.
  2. **BindingId prefix `SBD`:** `NunaId.New` requires 3-char prefix (same A1 override for STB/STL/STM).
  3. **Repo audit placeholders:** domain models have no `AuditTrail`; repos stamp `CrtUser`/`UpdUser` as `"STL"` at save time. Explicit and temporary until consequence handlers supply `UserId` (S1-D+).
  4. **`IStockMutasiRepo` / `IStockLegacyBindingRepo` not Scrutor-auto-registered:** they do not implement `ISaveChange`/`ILoadEntity`. A2 tests construct them directly; S1-C1/D1 should add manual `AddScoped` when handlers need DI. `IStockConsequenceUnitOfWork` intentionally not registered until S1-D1.
  5. **OCC assign-final Version:** domain keeps Version++-per-mutation; DAL writes `Version = @Version` (final) with `WHERE Version = @PersistedVersion` so one SaveChanges can persist N in-memory mutations; concurrent same-delta writers conflict when `rows != 1`.

## 8. Artifact updates
- Files updated: `README.md` under Domain StockLedgerFeature (next-card handoff only)
- Why: no durable domain/architecture redesign; slice summary records BindingId prefix and DI note. Ask before editing canonical architecture if `SBD` or UoW draft shape should be noted there.

## 9. Done-when checklist
- [x] Round-trip persist batch/lokasi/mutasi works.
- [x] Unique constraints and OCC conflict verified by tests.
- [x] Depleted lokasi row is retained.
- [x] UoW interface exists without dual-write body.

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-B** (allocation) and/or **S1-C1** (legacy read + hydrate) in parallel
- Blockers / residual risks:
  - Manual DI still needed for `IStockMutasiRepo`, `IStockLegacyBindingRepo`, and later `IStockConsequenceUnitOfWork` when MediatR handlers land.
  - Repo audit `"STL"` placeholder must be replaced by real `UserId` from consequence commands.
- Anything the next agent must not redo: Do not re-author five DDL scripts; do not add FEFO in DAL; do not implement MediatR/hydrate/dual-write beyond this card; do not add `Vod*` or put `NoBatch` on lokasi UX.
