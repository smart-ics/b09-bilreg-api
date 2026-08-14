# Stock Ledger S1-F2 — Implementation Summary

## 1. Slice identity
- Slice ID: S1-F2
- Plan card title: UC-STL-006 Post Internal Consumption Consequence
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Implemented `PostInternalConsumptionConsequenceCommand` / handler that mirrors the S1-F1 sale-issue outbound envelope with fixed MovementKind `InternalConsumption` / legacy `"PK"`: freshness gate, S1-B allocation (Explicit ED → FEFO → FIFO), dual-write outbound buku + stok deplete in one UoW, idempotent by `TrsReffId`, and reject-on-insufficient without Commit.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/PostInternalConsumptionConsequenceCommand.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/PostInternalConsumptionConsequenceHandlerTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-F2-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — S1-F2 Done → S1-G1

### Removed
- none

## 4. Behaviour / contracts implemented
- **UC-STL-006** Post Internal Consumption Consequence (domain §3.8; BR-STL-010–012 via allocator).
- **MovementKind:** `InternalConsumption` ↔ legacy `"PK"` (fixed; no sale-kind discriminator).
- **ADR-STL-005:** pre-assign `NunaId.NewLegacyCompact("BK")` before BindingInserts.
- **ADR-STL-007 / GAP-STL-001:** coexistence flag already on UoW/gate; handler always calls gate after candidate load.
- **GAP-STL-004:** gate-on-demand only (`LegacyFreshnessGate` → UC-STL-012); no worker.
- **ADR-STL-009:** load `QtySisa>0` candidates only via `ListAllocationCandidates`.
- Flow: Guard → **idempotency by `TrsReffId` (`ListByTrsReffId` before allocate)** → candidates → EnsureFreshAsync → reload → allocate → DecreaseLokasi per line → draft → UoW.Commit.
- Legacy ops per line: outbound buku (`PK`) → deplete source stok.
- Source `LegacyStokId` resolved via `ILegacyStockReadPort.ListBalances` filtered by Layanan+TglEd+QtySisa≥allocated (exactly one match required).
- Scope watermark advanced per DO using the consumption legacy buku id.
- Idempotent when complete InternalConsumption lines already exist for `TrsReffId` with matching Brg/Layanan/Qty; partial / wrong-kind collision → hard error.
- Insufficient → `InsufficientStock` + ShortfallQty; no UoW.

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `PostInternalConsumptionConsequenceHandlerTest` | Happy path PK dual-write; FEFO multi-balance; insufficient (no Commit); idempotent retry | Pass (4) |

## 6. Verification evidence
- Commands run:
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostInternalConsumptionConsequenceHandlerTest"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~LegacyStockWriterPortTest|FullyQualifiedName~StockConsequenceUnitOfWork"`
- Outcome (pass/fail): Pass — Failed: 0; PK 4; regression 8
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice; first PK run failed on `fs_kd_mutasi` length (>10) — fixed test TrsReffId seeds to ≤10 chars

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **Command naming `*Command` not `*Cmd`** — matches MutasiFeature / StockLedger C1–F1 and plan card / G-05 Inventory live naming.
  2. **Gate mocked via `LegacyFreshnessGate` + Moq `IMediator`** — same as D2/E/F1; gate is concrete.
  3. **Legacy stok resolve via read port (exactly one match)** — same pragmatic interim as S1-E/F1; multi-stok-row same Layanan+ED remains residual risk.
  4. **No shared `OutboundIssueConsequenceOrchestrator`** — plan allows optional helper; F2 keeps a standalone mirrored handler to avoid ceremony (same pragmatic choice as F1). Duplication is intentional and explicit.

## 8. Artifact updates
- Files updated: Domain `README.md` next-card pointer; this summary
- Why: no durable domain/architecture redesign; PK kind and standalone-handler choice recorded here rather than expanding architecture.

## 9. Done-when checklist
- [x] PK path passes; no duplicate FEFO logic in DAL.
- [x] Multi-balance allocation + dual-write verified.
- [x] Idempotent + insufficient scenarios covered.
- [x] No HTTP controller; no consumption void; no G1 work started.
- [x] Implementation summary written.
- [x] Domain README next-card pointer updated.
- [x] Verification filters pass.

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-G1** (UC-STL-004 Post Sale Void Consequence — reverse-journal via Binding; requires sale bindings from F1)
- Blockers / residual risks:
  - Legacy deplete assumes exactly one `tb_stok` row with Layanan+TglEd and QtySisa ≥ allocated; second receipt same DO (multiple legacy stok, one ledger lokasi) needs a multi-row deplete strategy before production multi-receipt consumption.
  - Callers must not reuse a `StockBatchModel` after Commit failure — always reload.
  - Consumption void is out of scope; G1 is sale void only.
- Anything the next agent must not redo: Do not reimplement writer/UoW/gate/watermark helper/receipt/transfer/sale/consumption handlers; do not add HTTP/worker/FEFO-in-DAL; do not add buku delete; reuse Binding lookup for void targets — never infer by qty match.
