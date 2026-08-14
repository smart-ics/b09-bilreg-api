# Stock Ledger S1-E — Implementation Summary

## 1. Slice identity
- Slice ID: S1-E
- Plan card title: UC-STL-002 Post Stock Transfer Consequence
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Implemented `PostStockTransferConsequenceCommand` / handler that runs freshness gate (UC-STL-012), allocates source stock via S1-B (`StockOutboundAllocator`), applies paired `TransferOut`/`TransferIn` movements with preserved `BrgMasukReffId`/`Hpp`/`TglEd`/`NoBatch`, and dual-writes legacy `MT_OUT` + deplete + `MT_IN` in one UoW. Hospital-wide batch `QtySisa` unchanged; depleted source lokasi retained.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/PostStockTransferConsequenceCommand.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/PostStockTransferConsequenceHandlerTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-E-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockConsequenceUnitOfWork.cs` — `ScopeUpdate` → `ScopeUpdates` list (multi-DO)
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockConsequenceUnitOfWork.cs` — loop `ScopeUpdates`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/PostGoodsReceiptConsequenceCommand.cs` — draft uses `ScopeUpdates: [scope]`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockConsequenceUnitOfWorkTest.cs` — draft shape
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockConsequenceUnitOfWorkLiveAtomicityTest.cs` — draft shape
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — S1-E Done → S1-F1

### Removed
- none

## 4. Behaviour / contracts implemented
- **UC-STL-002** Post Stock Transfer Consequence (domain 3.4–3.5; BR-STL-007, 009, 010–012, 021, 022–029).
- **MovementKind:** `TransferOut` / `TransferIn` ↔ legacy `"MT_OUT"` / `"MT_IN"`.
- **ADR-STL-005:** pre-assign `NunaId.NewLegacyCompact("BK"|"ST")` for OUT buku, IN buku, IN stok before BindingInserts.
- **ADR-STL-007 / GAP-STL-001:** coexistence flag already on UoW/gate; handler always calls gate first.
- **GAP-STL-004:** gate-on-demand only (`LegacyFreshnessGate` → UC-STL-012); no worker.
- Flow: Guard → **idempotency by `TrsReffId` (`ListByTrsReffId` before allocate)** → candidates (+ optional `BrgMasukReffIds`) → EnsureFreshAsync → allocate → DecreaseLokasi/IncreaseLokasi per line → draft → UoW.Commit.
- Legacy ops per line: outbound buku → deplete source stok → inbound buku+stok at dest.
- Source `LegacyStokId` resolved via `ILegacyStockReadPort.ListBalances` filtered by Layanan+TglEd+QtySisa≥allocated (exactly one match required).
- Scope watermark monotonic per DO using **max(OUT, IN) legacy buku id** (same `TglMutasi`; `IsAfterWatermark` ordinal tie-break).
- Idempotent when complete TransferOut/TransferIn legs already exist for `TrsReffId` (no re-allocation); partial legs → hard error. Retry after full deplete or after new source stock still returns prior success.
- Insufficient → `InsufficientStock` + ShortfallQty; no UoW.
- OCC conflicts propagate as `InvalidOperationException`.

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `PostStockTransferConsequenceHandlerTest` | ED preserved + OUT=IN + hospital qty unchanged + legacy MT_OUT/MT_IN + watermark covers OUT/IN; multi-DO split; insufficient (no Commit); idempotent retry (partial + full deplete + emptied+new stock); gate abort; OCC concurrency; depleted source retained at QtySisa=0 | Pass (9) |

## 6. Verification evidence
- Commands run:
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostStockTransferConsequenceHandlerTest"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~LegacyStockWriterPortTest|FullyQualifiedName~StockConsequenceUnitOfWork|FullyQualifiedName~PostGoodsReceiptConsequenceHandlerTest"`
- Outcome (pass/fail): Pass — Failed: 0; transfer 9 (incl. review P0/P2 retry + watermark asserts)
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **Command naming `*Command` not `*Cmd`** — matches MutasiFeature / StockLedger C1–D2 and plan card / G-05 Inventory live naming.
  2. **`StockConsequenceDraft.ScopeUpdates` list** — singular `ScopeUpdate` could not watermark multi-DO transfers; list is explicit, low-ceremony, required for S1-E (and future F1). D2/UoW tests updated.
  3. **Gate mocked via `LegacyFreshnessGate` + Moq `IMediator`** — same as D2; gate is concrete.
  4. **Legacy stok resolve via read port (exactly one match)** — no `FindByStokLokasiId` on binding repo yet; pragmatic for S1-E seed (one receipt per DO). Multi-stok-row same Layanan+ED (second receipt same DO) remains a residual risk for later slices.

## 8. Artifact updates
- Files updated: Domain `README.md` next-card pointer; this summary
- Why: no durable domain/architecture redesign; `ScopeUpdates` and legacy-stok resolve choices recorded here rather than expanding architecture.

## 9. Done-when checklist
- [x] ED preserved; OUT=IN; OCC covered.
- [x] Hospital QtySisa unchanged; multi-balance; insufficient; idempotent (incl. full-deplete + emptied+new-stock retry); watermark max(OUT, IN); gate abort; depleted source retained.
- [x] No HTTP controller; no transfer void; no F1+ work started.
- [x] Implementation summary written.
- [x] Domain README next-card pointer updated.
- [x] Verification filters pass.

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-F1** (UC-STL-003 Post Sale Issue Consequence)
- Blockers / residual risks:
  - Legacy deplete assumes exactly one `tb_stok` row with Layanan+TglEd and QtySisa ≥ allocated; second receipt same DO (multiple legacy stok, one ledger lokasi) needs a multi-row deplete strategy before production multi-receipt sales/transfers.
  - Callers must not reuse a `StockBatchModel` after Commit failure — always reload.
  - **F1 must short-circuit idempotency by `TrsReffId` before allocate** (same P0 pattern as this review fix); do not copy allocate-then-Exists.
- Anything the next agent must not redo: Do not reimplement writer/UoW/gate/watermark helper/receipt/transfer handlers; do not add HTTP/worker/FEFO-in-DAL; do not add buku delete; reuse `ScopeUpdates` list for multi-DO sale drafts.
