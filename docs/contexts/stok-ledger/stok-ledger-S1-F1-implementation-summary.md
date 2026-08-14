# Stock Ledger S1-F1 — Implementation Summary

## 1. Slice identity
- Slice ID: S1-F1
- Plan card title: UC-STL-003 Post Sale Issue Consequence
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Implemented `PostSaleIssueConsequenceCommand` / handler that runs freshness gate (UC-STL-012), allocates at one Layanan via S1-B (`StockOutboundAllocator` — Explicit ED → FEFO → FIFO), applies outbound sale movements (`SaleIssueDb`/`Du`/`Dt`), and dual-writes legacy `DB`/`DU`/`DT` plus stok deplete in one UoW. Hospital-wide batch `QtySisa` decreases; depleted lokasi retained at zero.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/PostSaleIssueConsequenceCommand.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/PostSaleIssueConsequenceHandlerTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-F1-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — S1-F1 Done → S1-F2

### Removed
- none

## 4. Behaviour / contracts implemented
- **UC-STL-003** Post Sale Issue Consequence (domain 10.3; BR-STL-022…027, 010–012).
- **MovementKind:** `SaleIssueDb` / `SaleIssueDu` / `SaleIssueDt` ↔ legacy `"DB"` / `"DU"` / `"DT"` via command `SaleIssueKindEnum`.
- **ADR-STL-005:** pre-assign `NunaId.NewLegacyCompact("BK")` before BindingInserts.
- **ADR-STL-007 / GAP-STL-001:** coexistence flag already on UoW/gate; handler always calls gate after candidate load.
- **GAP-STL-004:** gate-on-demand only (`LegacyFreshnessGate` → UC-STL-012); no worker.
- **ADR-STL-009:** load `QtySisa>0` candidates only via `ListAllocationCandidates`.
- Flow: Guard → **idempotency by `TrsReffId` (`ListByTrsReffId` before allocate)** → candidates → EnsureFreshAsync → reload → allocate → DecreaseLokasi per line → draft → UoW.Commit.
- Legacy ops per line: outbound buku → deplete source stok.
- Source `LegacyStokId` resolved via `ILegacyStockReadPort.ListBalances` filtered by Layanan+TglEd+QtySisa≥allocated (exactly one match required).
- Scope watermark advanced per DO using the sale legacy buku id.
- Idempotent when complete sale-issue lines already exist for `TrsReffId` with matching Brg/Layanan/Qty; partial / wrong-kind collision → hard error.
- Insufficient → `InsufficientStock` + ShortfallQty; no UoW.

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `PostSaleIssueConsequenceHandlerTest` | FEFO multi-balance; Explicit ED; FIFO no-ED; insufficient (no Commit); dual-write DB + DU/DT; idempotent retry; gate abort | Pass (9) |

## 6. Verification evidence
- Commands run:
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostSaleIssueConsequenceHandlerTest"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~LegacyStockWriterPortTest|FullyQualifiedName~StockConsequenceUnitOfWork|FullyQualifiedName~PostGoodsReceiptConsequenceHandlerTest|FullyQualifiedName~PostStockTransferConsequenceHandlerTest"`
- Outcome (pass/fail): Pass — Failed: 0; sale 9; regression 23
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice; one intermittent `tb_buku` PK collision on first full F1 run (NunaId compact race) — re-run clean

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **Command naming `*Command` not `*Cmd`** — matches MutasiFeature / StockLedger C1–E and plan card / G-05 Inventory live naming.
  2. **Gate mocked via `LegacyFreshnessGate` + Moq `IMediator`** — same as D2/E; gate is concrete.
  3. **Legacy stok resolve via read port (exactly one match)** — same pragmatic interim as S1-E; multi-stok-row same Layanan+ED remains residual risk.
  4. **No shared `OutboundIssueConsequenceOrchestrator`** — plan defers optional helper to S1-F2; F1 keeps self-contained handler to avoid premature abstraction.

## 8. Artifact updates
- Files updated: Domain `README.md` next-card pointer; this summary
- Why: no durable domain/architecture redesign; SaleKind mapping and legacy-stok resolve choices recorded here rather than expanding architecture.

## 9. Done-when checklist
- [x] Multi-balance allocation + dual-write verified.
- [x] FEFO / Explicit ED / FIFO no-ED / insufficient / idempotent scenarios covered.
- [x] Idempotency short-circuits before gate/allocate.
- [x] No HTTP controller; no sale void; no PK/F2 work started.
- [x] Implementation summary written.
- [x] Domain README next-card pointer updated.
- [x] Verification filters pass.

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-F2** (UC-STL-006 Post Internal Consumption Consequence — MovementKind `PK`)
- Blockers / residual risks:
  - Legacy deplete assumes exactly one `tb_stok` row with Layanan+TglEd and QtySisa ≥ allocated; second receipt same DO (multiple legacy stok, one ledger lokasi) needs a multi-row deplete strategy before production multi-receipt sales.
  - Callers must not reuse a `StockBatchModel` after Commit failure — always reload.
  - **F2 should reuse this orchestration** (optionally extract shared outbound helper); do not copy allocate-then-Exists idempotency — short-circuit by `TrsReffId` before allocate.
- Anything the next agent must not redo: Do not reimplement writer/UoW/gate/watermark helper/receipt/transfer/sale handlers; do not add HTTP/worker/FEFO-in-DAL; do not add buku delete; reuse `ScopeUpdates` list for multi-DO drafts.
