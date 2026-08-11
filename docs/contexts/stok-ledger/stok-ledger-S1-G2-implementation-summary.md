# Stock Ledger S1-G2 — Implementation Summary

## 1. Slice identity
- Slice ID: S1-G2
- Plan card title: UC-STL-005 Post Sales Return Consequence
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Implemented `PostSalesReturnConsequenceCommand` / handler that inbound-restores previously issued sale quantity under authorized return (RJ/RU/RT): gates the Receipt Source scope, increases existing lokasi (including depleted), dual-writes legacy inbound buku + stok via `InsertInbound`, preserves `BrgMasukReffId`, and short-circuits idempotently on matching `TrsReffId` + kind + lokasi.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/PostSalesReturnConsequenceCommand.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/PostSalesReturnConsequenceHandlerTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-G2-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — S1-G2 Done → S1-H

### Removed
- none

## 4. Behaviour / contracts implemented
- **UC-STL-005** Post Sales Return Consequence (domain §3.6 / §10.4; BR-STL-005/006/011/012/014).
- **MovementKind:** `SalesReturnRj` / `SalesReturnRu` / `SalesReturnRt` ↔ legacy `"RJ"` / `"RU"` / `"RT"` via command `SalesReturnKindEnum` (GAP-STL-003).
- Flow: Guard → EnsureFreshAsync → load batch by natural key (`BatchNotFound` if missing) → idempotency `Exists(TrsReffId, kind, lokasiId)` → `IncreaseLokasi` → inbound mutasi (`batch.Hpp`) + Binding + watermark → `LegacyInboundWriteOperation` (caller `Hpp`, `batch.TglMasuk` / `PoReffId`) → UoW.Commit.
- Outcomes: `Success`, `Idempotent`, `BatchNotFound`, `AbortedInconsistent`.
- **ADR-STL-005:** pre-assign BK/ST before BindingInserts.
- **GAP-STL-001 / ADR-STL-007:** coexistence already on UoW/gate.
- **GAP-STL-004:** gate-on-demand only; no worker.
- Legacy path mirrors receipt `AddStok` (INSERT buku + INSERT stok) — does **not** use G1 `RestoreStok`.
- Batch must already exist (Receipt Source preserved); no `StockBatchModel.Create` on return.

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `PostSalesReturnConsequenceHandlerTest` | Restore after sale (depleted lokasi); Receipt Source preserved; idempotent retry; gate abort; Ru/Rt legacy kind map; BatchNotFound | Pass (7) |

## 6. Verification evidence
- Commands run:
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostSalesReturnConsequenceHandlerTest"`
- Outcome (pass/fail): Pass — Failed: 0; Passed: 7
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **Command naming `*Command` not `*Cmd`** — matches MutasiFeature / StockLedger C1–G1 and plan card / G-05 Inventory live naming.
  2. **Gate mocked via `LegacyFreshnessGate` + Moq `IMediator`** — same as D2–G1; gate is concrete.
  3. **`BatchNotFound` outcome** — pragmatic reject when Receipt Source batch is missing (plan: do not invent DO); analogous to G1 `OriginalNotFound`.
  4. **BILRG mutasi uses `batch.Hpp`; legacy inbound uses caller `Hpp`** — BR-STL-014 for ledger valuation; caller supplies legacy valuation per command contract.

## 8. Artifact updates
- Files updated: Domain `README.md` next-card pointer; this summary
- Why: no durable domain/architecture redesign; return-kind mapping and BatchNotFound choice recorded here rather than expanding architecture.

## 9. Done-when checklist
- [x] Return path dual-writes (`InsertInbound` RJ/RU/RT + BILRG mutasi/binding)
- [x] Receipt Source (`BrgMasukReffId`) preserved on batch/mutasi/legacy
- [x] Idempotent on `TrsReffId` + kind + lokasi
- [x] Tests pass per filter above
- [x] Implementation summary written
- [x] Domain README next-card → S1-H
- [x] No HTTP, no purchase return, no void-path reuse, no writer/UoW redesign

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-H** (UC-STL-020 Reconcile Scope + minimal UC-STL-021 Availability)
- Blockers / residual risks:
  - Return always INSERTs a new legacy `tb_stok` row (legacy `AddStok` behaviour); after deplete-to-zero sale + return, ledger lokasi is restored on the same id while legacy may have multiple stok rows for the same DO — same multi-stok-row interim as F1/G1.
  - Callers must not reuse a `StockBatchModel` after Commit failure — always reload.
  - Return void (`RJ_V`/`RU_V`/`RT_V`) remains deferred.
- Anything the next agent must not redo: Do not reimplement writer/UoW/gate/watermark helper/receipt/transfer/sale/consumption/void/return handlers; do not add HTTP/worker/FEFO-in-DAL; do not add buku delete.
