# Stock Ledger S1-A1 — Implementation Summary

## 1. Slice identity
- Slice ID: S1-A1
- Plan card title: Domain skeleton + v2 DDL (§8)
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Introduced Stock Ledger v2 domain skeleton (`StockBatchModel`, `LocationStockBalanceModel`, `StockMovementModel`) with non-negative qty and depleted-balance retention (BR-STL-010…012), plus five architecture §8 DDL scripts (including denormalized `TglMasuk` on lokasi) registered in `Bilreg.SqlDb.sqlproj`. No FEFO, DAL, or MediatR.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/IStockBatchKey.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/IStokLokasiKey.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/IStokMutasiKey.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockBatchKeyType.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockLedgerSentinel.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/MovementKindEnum.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/AlignmentStatusEnum.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/BindingKindEnum.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/LocationStockBalanceModel.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockBatchModel.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockMovementModel.cs`
- `src/bilreg/Bilreg.SqlDb/InventoryContext/StockLedgerFeature/BILRG_StokBatch.sql`
- `src/bilreg/Bilreg.SqlDb/InventoryContext/StockLedgerFeature/BILRG_StokLokasi.sql`
- `src/bilreg/Bilreg.SqlDb/InventoryContext/StockLedgerFeature/BILRG_StokMutasi.sql`
- `src/bilreg/Bilreg.SqlDb/InventoryContext/StockLedgerFeature/BILRG_StokLegacyScope.sql`
- `src/bilreg/Bilreg.SqlDb/InventoryContext/StockLedgerFeature/BILRG_StokLegacyBinding.sql`
- `src/bilreg/Bilreg.SqlDb/InventoryContext/StockLedgerFeature/BILRG_StockLedgerV2_Rollback.sql`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockBatchDomainTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockMovementDomainTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-A1-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.SqlDb/Bilreg.SqlDb.sqlproj` — Folder + six `<None Include>` scripts
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — next card pointer to S1-A2

### Removed
- none

## 4. Behaviour / contracts implemented
- Domain: `StockBatchModel.Create` / `IncreaseLokasi` / `DecreaseLokasi`; hospital-wide `QtySisa` synced to Σ lokasi; `LokasiQtyTotal`.
- Domain: `LocationStockBalanceModel.IncreaseQty` / `DecreaseQty` — reject negative (BR-STL-010); retain `QtySisa=0` (BR-STL-011, BR-STL-012); `NoBatch` stored, not part of uniqueness (GAP-STL-005).
- Domain: `StockMovementModel.CreateInbound` / `CreateOutbound` — exactly one of QtyIn/QtyOut > 0 (BR-STL-017); `ReversesMutasiId` defaults `""`.
- Enums: S1 `MovementKindEnum` INT catalog (GAP-STL-003); `AlignmentStatusEnum`; `BindingKindEnum`.
- Persistence DDL only (no DAL): five tables per architecture §8.1; ADR-STL-002 (no `Vod*`); lokasi UX `(StokBatchId, LayananId, TglEd)` without `NoBatch`; filtered FEFO index; mutasi UX `(TrsReffId, MovementKind, StokLokasiId)`; binding filtered unique on `LegacyBukuId` / `StokMutasiId`.
- IDs: `NunaId.New("STB"|"STL"|"STM")` — 3-char prefixes required by Nuna.Lib (ADR-STL-005 spirit; see overrides).
- Coexistence / dual-write / gate: not applicable in A1.
- Safe interim: GAP-STL-003 (S1 MovementKind values only); GAP-STL-005 (`NoBatch` excluded from UX).

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `StockBatchDomainTest` | Create zero qty; increase; multi-lokasi sum; decrease; reject negative; retain depleted; accumulate same L1 key | Pass (7) |
| `StockMovementDomainTest` | Inbound/outbound factories; reject both sides > 0; reject both zero; `ReversesMutasiId` empty default | Pass (5) |

## 6. Verification evidence
- Commands run:
  - `dotnet build "src/bilreg/b09-bilreg-api.sln"` (via prior full test build)
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockBatchDomainTest|FullyQualifiedName~StockMovementDomainTest"`
- Outcome (pass/fail): Pass — Failed: 0, Passed: 12
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice; scripts not applied to live DB in A1 (S1-A2 schema fixture will apply)

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **Model shape:** plan G-05 / MutasiFeature `*Model` class aggregates instead of feature-model-generation preferred `record`/`*Type` — pragmatic match to InventoryContext and the A1 card file list.
  2. **No `Vod*`:** ADR-STL-002 exception to DATABASE soft-delete columns — documented in each SQL header.
  3. **`NunaId.New(prefix)`:** ADR writes `NunaId.New()`; runtime requires exactly 3-character prefix → used `STB`/`STL`/`STM` for batch/lokasi/mutasi. Still explicit and compatible with `VARCHAR(12)` PKs.

## 8. Artifact updates
- Files updated: `README.md` under Domain StockLedgerFeature (next-card handoff only)
- Why: no durable domain/architecture redesign; slice summary records ID-prefix clarification for ADR-STL-005. Ask before editing canonical architecture if the 3-char prefix requirement should be noted there.

## 9. Done-when checklist
- [x] Five SQL scripts match §8 (including lokasi `TglMasuk`).
- [x] Domain skeleton compiles with behaviour for non-negative + depleted retention.
- [x] Domain unit tests pass.
- [x] sqlproj lists the new scripts.

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-A2**
- Blockers / residual risks: DDL not yet applied to test DB — A2 schema fixture must create the five tables. BindingId generation not in domain skeleton yet (no Binding aggregate in A1); A2 repos will use `NunaId.New("…")` with a 3-char prefix (e.g. `STB`-style).
- Anything the next agent must not redo: Do not start FEFO (S1-B); do not invent columns beyond §8; do not add `Vod*` or put `NoBatch` on lokasi UX; do not implement MediatR/hydrate/dual-write in A2 beyond the A2 card.
