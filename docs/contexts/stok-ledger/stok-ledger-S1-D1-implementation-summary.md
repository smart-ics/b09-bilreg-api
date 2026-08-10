# Stock Ledger S1-D1 — Implementation Summary

## 1. Slice identity
- Slice ID: S1-D1
- Plan card title: Legacy writer port + atomic `IStockConsequenceUnitOfWork`
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Implemented `ILegacyStockWriterPort` for S1 legacy write shapes (inbound buku+stok, outbound buku, stok deplete, reverse-buku void insert) and a real `StockConsequenceUnitOfWork` that commits legacy + BILRG ledger + binding + scope watermark in one `TransHelper` SQL transaction with OCC and `StockLedger:CoexistenceEnabled` cutover skip (ADR-STL-007).

## 3. What changed
### Added
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/Ports/ILegacyStockWriterPort.cs` (+ write request/result records and `LegacyStockWriteOperation` union)
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/LegacyStockDateHelper.cs`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/LegacyStockWriterPort.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/LegacyStockWriterPortTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockConsequenceUnitOfWorkTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockConsequenceUnitOfWorkLiveAtomicityTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockLedgerSqlCollection.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-D1-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockConsequenceUnitOfWork.cs` — `StockConsequenceDraft` gains `LegacyOperations`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockBatchRepo.cs` — `SaveChanges(model, userId)`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockMutasiRepo.cs` — `Insert(model, userId)`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockLegacyBindingRepo.cs` — `Insert(model, userId)`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockLegacyScopeRepo.cs` — `SaveChanges(model, userId)`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockBatchRepo.cs` / `StockMutasiRepo.cs` / `StockLegacyBindingRepo.cs` / `StockLegacyScopeRepo.cs` — userId overloads (default `"STL"`)
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockConsequenceUnitOfWork.cs` — real Commit body
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/LegacyStockReadPort.cs` — delegates datetime helpers to `LegacyStockDateHelper`
- `src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs` — register writer + UoW
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — S1-D1 Done → S1-D2

### Removed
- none (UoW stub replaced in place)

## 4. Behaviour / contracts implemented
- **Ports / UoW:** `ILegacyStockWriterPort` + `IStockConsequenceUnitOfWork.Commit` (architecture §8.2, §13).
- **IDs:** `NunaId.NewLegacyCompact("BK"|"ST")` when caller does not pre-assign; optional pre-assigned ids so S1-D2 can build Binding rows before Commit (ADR-STL-005).
- **Inbound:** INSERT `tb_buku` then INSERT `tb_stok` (clbGenStokX1 AddStok shape); datetime↔legacy tgl/jam via shared helper.
- **Outbound / deplete:** INSERT outbound buku; UPDATE/DELETE `tb_stok` only when depleting (legacy RemoveStok); never DELETE buku.
- **Void / reverse:** INSERT compensating buku only — no buku delete helper (ADR-STL-002 / BR-STL-019).
- **UoW order:** BEGIN → legacy ops (if coexistence on) → batch/lokasi OCC SaveChanges → mutasi insert → binding insert → scope update → COMMIT; any failure rolls back.
- **Coexistence:** `CoexistenceEnabled=false` skips legacy writes (ledger-only, ADR-STL-007 / GAP-STL-001).
- **Safe interim:** GAP-STL-001 options already bound; GAP-STL-004 gate-on-demand left alone (no worker); no dual-write handlers yet (S1-D2).
- **UC-STL-001** not implemented (S1-D2).

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `LegacyStockWriterPortTest` | Inbound buku+stok + id length 10; reverse buku retains original; deplete partial; deplete-to-zero deletes stok only | Pass (4) |
| `StockConsequenceUnitOfWorkTest` | Happy path dual-write + binding + scope; OCC concurrency throws; coexistence off skips legacy | Pass (3) |
| `StockConsequenceUnitOfWorkLiveAtomicityTest` | Forced failure after legacy leaves neither side committed | Pass (1) |

## 6. Verification evidence
- Commands run:
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~LegacyStockWriterPortTest|FullyQualifiedName~StockConsequenceUnitOfWork"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~CatchUpScopeFromLegacy|FullyQualifiedName~EnsureFreshnessForScopes|FullyQualifiedName~HydrateScopeFromLegacy"` (optional C1/C2 regression)
- Outcome (pass/fail): Pass — D1 filter Failed: 0, Passed: 8; C1/C2 regression Failed: 0, Passed: 30
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice; dual-write SQL tests serialized via `StockLedgerSqlCollection` to avoid `tb_buku`/`tb_stok` deadlocks under parallel xUnit

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **Repo `userId` overloads** alongside `ISaveChange` / parameterless Insert — keeps A2/C default `"STL"` while UoW stamps real `draft.UserId`. Explicit; no Domain AuditTrail redesign.
  2. **Optional pre-assigned legacy ids on write requests** — so BindingInserts can be built before Commit without patching init-only models inside UoW. Explicit; writer still generates via `NewLegacyCompact` when empty.
  3. **`FailAfterLegacyWritesForTest` internal hook** on UoW — pragmatic live atomicity proof without production failure injection framework; `InternalsVisibleTo` Bilreg.Test.
  4. **`StockLedgerSqlCollection` DisableParallelization** for dual-write live tests — operational test hygiene; does not change domain/architecture.
  5. **No `DELETE tb_buku` API** — ADR-STL-002 / plan hard rule (override vs legacy clbGenStokX1 void-delete path).

## 8. Artifact updates
- Files updated: Domain `README.md` next-card pointer; this summary
- Why: no durable domain/architecture redesign; draft/`LegacyOperations` shape recorded here for S1-D2. Ask before editing canonical architecture if pre-assigned legacy ids should be noted in §13.

## 9. Done-when checklist
- [x] Single TX dual-write path works with rollback.
- [x] No buku delete helper exists for void.
- [x] Flag false skips legacy writes.
- [x] Verification filter passes.
- [x] Implementation summary written.

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-D2** (UC-STL-001 Post Goods Receipt Consequence)
- Blockers / residual risks:
  - Callers must call `LegacyFreshnessGate` before UoW (architecture §13); not wired until S1-D2.
  - S1-D2 should pre-assign BK/ST ids (or read writer results if draft assembly is refactored) when building BindingInserts.
  - Hydrate/catch-up still persist via separate repo calls (no UoW); only native consequence path uses UoW.
- Anything the next agent must not redo: Do not reimplement writer/UoW/date helper; do not add HTTP/worker/FEFO-in-DAL; do not add buku delete; leave sale/transfer handlers to later cards.
