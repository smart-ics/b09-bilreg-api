# Stock Ledger S1-A0 — Implementation Summary

## 1. Slice identity
- Slice ID: S1-A0
- Plan card title: Retire obsolete v1 source from the feature tree
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Confirmed that obsolete Stock Ledger **v1** types and BILRG six-table SQL are absent from the compile tree, so greenfield v2 can occupy `InventoryContext/StockLedgerFeature` without mixing designs. Added a Domain feature README pointing implementers at architecture ADR-STL-001 and the Slice 1 plan (next card S1-A1).

## 3. What changed
### Added
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md`
- `docs/contexts/stok-ledger/stok-ledger-S1-A0-implementation-summary.md`

### Changed
- none

### Removed
- none (v1 Domain/Application/Infrastructure/Test sources and v1 SqlDb scripts were already absent from this branch; nothing to delete or deregister from `Bilreg.SqlDb.sqlproj` or DI)

## 4. Behaviour / contracts implemented
- No use cases, ports, tables, or runtime rules added in this card.
- ADR-STL-001 / C-05 respected: v1 six-table model not present and must not be reintroduced under `StockLedgerFeature`.
- Coexistence / dual-write / gate / binding: not applicable (foundation retire only).
- Safe interim gaps: not exercised (no GAP-STL-* choices required for this card).

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| *(none new)* | Solution builds; filter `FullyQualifiedName~StockLedgerFeature` returns 0 tests until later cards add v2 tests | Pass (0 matches) |

## 6. Verification evidence
- Commands run:
  - `dotnet build "src/bilreg/b09-bilreg-api.sln"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockLedgerFeature" --no-build`
- Outcome (pass/fail): both passed (build exit 0; test run exit 0 with no matching tests)
- Notable warnings: none relevant to this slice

Discovery greps (zero hits in `src/`):
- `StockLayer`, `StockPosition`, `StockMovementLine`, `ReconstructStockLedgerBaseline`, `SynchronizeStockLedgerScope`, `ILegacyCompatibilityWriterPort`
- `StockLedgerFeature`, `BILRG_StokMovement`, `BILRG_StokPosition`, `BILRG_StokLedgerScope`, `BILRG_StokSourceIdempotency`, `StokLayerLegacyBinding`
- No `StockLedger` / `BILRG_Stok*` entries in `Bilreg.SqlDb.sqlproj`
- No v1 DI in `ApplicationService.cs` / `InfrastructureService.cs`

**Note:** `StokFeature` (`StokLayerModel`, `FARIN_StokLayer`, legacy `tb_*`) was left untouched — separate Inventory feature, not Stock Ledger v1.

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides (if any): none required. This card is retire + placeholder README only; skills for model/persistence/use-case generation do not apply yet.

## 8. Artifact updates
- Files updated (or “none”): none (canonical domain/architecture unchanged; ARTIFACTS.md not indexed for this process summary — ask if a Stock Ledger slice-summary row should be added)
- Why: no durable design truth changed; v1 was already absent

## 9. Done-when checklist
- [x] No v1 Stock Ledger types remain in Domain/Application/Infrastructure/Test compile trees.
- [x] Solution builds.
- [x] Feature folder is ready for greenfield v2 names from architecture §8.

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-A1**
- Blockers / residual risks: If another local/unpushed branch still holds v1 `StockLedgerFeature` sources, merge/rebase before S1-A1 so v1 and v2 never share the namespace. On this workspace snapshot that risk does not apply. Do **not** DROP production v1 BILRG tables in S1 (ops backlog).
- Anything the next agent must not redo: Do not reintroduce v1 Movement/Line/Layer/Position/Scope/Idempotency types “for reference” under `StockLedgerFeature`; do not start FEFO, DAL, or use cases in A1 beyond the A1 card; do not treat `StokFeature` as Stock Ledger v1 cleanup.
