# Stock Ledger S1-G1 — Implementation Summary

## 1. Slice identity
- Slice ID: S1-G1
- Plan card title: UC-STL-004 Post Sale Void Consequence
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Implemented `PostSaleVoidConsequenceCommand` / handler that reverse-journals a prior sale via **Binding lookup only**: inserts compensating inbound mutasi with `ReversesMutasiId`, restores lokasi/batch qty (including depleted rows), dual-writes legacy `DB_V`/`DU_V`/`DT_V` buku plus stok restore, never deletes `tb_buku`, and rejects unbound / already-reversed / inconsistent scopes.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/PostSaleVoidConsequenceCommand.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/PostSaleVoidConsequenceHandlerTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-G1-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/Ports/ILegacyStockWriterPort.cs` — `RestoreStok` + `LegacyStokRestoreWriteOperation` / request/result
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/LegacyStockWriterPort.cs` — restore via atomic `fn_qty += QtyIn`; if UPDATE rows affected == 0 fall through to recreate INSERT; recreate id = `LegacyStokIdIfRecreate ?? PreferredLegacyStokId` (no anonymous mint when Binding already chose an id)
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockConsequenceUnitOfWork.cs` — dispatch restore op
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockMutasiRepo.cs` — `ExistsReversalFor`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StokMutasiDal.cs` / `StockMutasiRepo.cs` — reversal existence query
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/LegacyStockWriterPortTest.cs` — restore recreate + partial restore + null-IfRecreate fallback
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — S1-G1 Done → S1-G2

### Removed
- none

### Review fix (P1 + P3)
- **P1 race (handler IfRecreate):** preferred-stok-exists path previously set `LegacyStokIdIfRecreate: null` while Binding already stored `restoreStokId` (= preferred). Concurrent deplete-delete before Commit could mint a new `ST*` and desync Binding. Fix: always pass `LegacyStokIdIfRecreate: restoreStokId`; writer recreate uses `LegacyStokIdIfRecreate ?? PreferredLegacyStokId`.
- **P1 race (writer UPDATE fallthrough):** `RestoreStok` previously SELECT then absolute UPDATE and returned success without checking rows affected — concurrent deplete could delete the row between SELECT and UPDATE, leaving Binding pointing at a missing `tb_stok` id. Fix: atomic `UPDATE tb_stok SET fn_qty = fn_qty + @QtyIn`; if rows affected == 0 fall through to INSERT recreate with `LegacyStokIdIfRecreate ?? PreferredLegacyStokId`; if rows == 1 return `WasRecreated: false`.
- **P3 replay:** idempotent short-circuit now validates prior void `ReversesMutasiId` set equals sale mutasi ids for `OriginalSaleTrsReffId` (mismatch throws; does not return `Idempotent`).

## 4. Behaviour / contracts implemented
- **UC-STL-004** Post Sale Void Consequence (domain §3.11 / §10.5; BR-STL-016…020; ADR-STL-002).
- **MovementKind:** original `SaleIssueDb|Du|Dt` → `SaleVoidDb|Du|Dt` ↔ legacy `"DB_V"` / `"DU_V"` / `"DT_V"` (GAP-STL-003).
- Void targets resolved **only** via `IStockLegacyBindingRepo.FindByStokMutasiId` — never by qty match.
- Flow: Guard → **idempotency by `VoidTrsReffId` (`ListByTrsReffId` before gate) + validate `ReversesMutasiId` vs `OriginalSaleTrsReffId`** → load original sale mutasi → require Binding per line → `ExistsReversalFor` → EnsureFreshAsync → `IncreaseLokasi` → inbound reverse mutasi + reverse buku + RestoreStok → Binding + watermark → UoW.Commit.
- Outcomes: `Success`, `Idempotent`, `Unbound`, `AlreadyReversed`, `OriginalNotFound`, `AbortedInconsistent`.
- **ADR-STL-005:** pre-assign void BK (+ recreate ST when deplete deleted the stok row).
- **GAP-STL-001 / ADR-STL-007:** coexistence already on UoW/gate.
- **GAP-STL-004:** gate-on-demand only; no worker.
- Safe interim: atomic increment of preferred `LegacyStokId` when the row still exists; if UPDATE affects 0 rows (already deleted or concurrent deplete-delete), INSERT recreate with Binding-chosen id (`LegacyStokIdIfRecreate`, always passed by handler). Pre-Commit `ListBalances` still chooses preferred vs pre-minted ST for Binding; writer fallthrough keeps Binding ↔ `tb_stok` aligned when preferred disappears between that check and Commit.

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `PostSaleVoidConsequenceHandlerTest` | Happy path reverse + originals retained; depleted lokasi restored + Binding `LegacyStokId` == live `tb_stok` + original buku retained; multi-balance FEFO void; unbound reject; already reversed; idempotent retry; mismatched OriginalSale on replay throws; gate abort; original not found | Pass (9) |
| `LegacyStockWriterPortTest` | Restore after full deplete recreates stok / retains buku; null IfRecreate falls back to Preferred; partial restore updates qty; preferred missing at UPDATE falls through to recreate with IfRecreate id | Pass (8 total class) |

## 6. Verification evidence
- Commands run:
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostSaleVoidConsequenceHandlerTest|FullyQualifiedName~LegacyStockWriterPortTest"`
- Outcome (pass/fail): Pass — Failed: 0; Passed: 17 (void handler 9 + writer 8)
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice; first void run failed on `TrsReffId` / `fs_kd_mutasi` length (>10) — fixed test ids to ≤10 chars

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **Command naming `*Command` not `*Cmd`** — matches MutasiFeature / StockLedger C1–F2 and plan card / G-05 Inventory live naming.
  2. **Gate mocked via `LegacyFreshnessGate` + Moq `IMediator`** — same as D2–F2; gate is concrete.
  3. **`RestoreStok` added to D1 writer** — architecture §8.2 requires void = reverse buku + update stok; D1 only shipped buku reverse. Explicit, low-ceremony extension (atomic increment UPDATE, or INSERT when rows affected == 0); no buku delete.
  4. **`ExistsReversalFor` on mutasi repo** — uses existing `ReversesMutasiId` index; no schema change.
  5. **No shared void/outbound orchestrator** — plan notes keep PK path as thin F1 mirror; G1 stays self-contained (copy watermark helper).

## 8. Artifact updates
- Files updated: Domain `README.md` next-card pointer; this summary
- Why: no durable domain/architecture redesign; RestoreStok and void TrsReffId contract recorded here rather than expanding architecture. Review fix documents the Binding/`tb_stok` recreate race (IfRecreate always passed + atomic UPDATE fallthrough) and replay validation.

## 9. Done-when checklist
- [x] Binding-based void; reject unbound / already reversed
- [x] Reverse journal with `ReversesMutasiId`; originals retained
- [x] No `tb_buku` delete; buku row count for originals not decreased
- [x] Balance restored (including depleted lokasi)
- [x] Idempotent on `VoidTrsReffId` short-circuit before gate
- [x] Binding `LegacyStokId` matches restored `tb_stok` id (including deplete recreate / race-safe IfRecreate + atomic UPDATE fallthrough)
- [x] Idempotent replay validates `OriginalSaleTrsReffId` via `ReversesMutasiId` set
- [x] No HTTP controller; no G2/consumption void work
- [x] Implementation summary written
- [x] Domain README next-card pointer updated
- [x] Verification filters pass

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-G2** (UC-STL-005 Post Sales Return Consequence — inbound restore RJ/RU/RT)
- Blockers / residual risks:
  - Legacy stok restore uses atomic qty increment; when the preferred row is gone (deplete-to-zero or concurrent delete) it recreates `tb_stok` with the Binding-chosen id. Binding on the void line points at that restored id. Same multi-stok-row interim as F1 deplete remains for production multi-receipt same-DO cases.
  - Callers must not reuse a `StockBatchModel` after Commit failure — always reload.
  - Consumption void remains deferred; do not extend G1 path for PK.
- Anything the next agent must not redo: Do not reimplement writer/UoW/gate/watermark helper/receipt/transfer/sale/consumption/void handlers; do not add HTTP/worker/FEFO-in-DAL; do not add buku delete; do not infer void targets by qty — Binding only.
