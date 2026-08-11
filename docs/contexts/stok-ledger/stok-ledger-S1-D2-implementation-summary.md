# Stock Ledger S1-D2 — Implementation Summary

## 1. Slice identity
- Slice ID: S1-D2
- Plan card title: UC-STL-001 Post Goods Receipt Consequence
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Implemented `PostGoodsReceiptConsequenceCommand` / handler that runs freshness gate (UC-STL-012) before atomic dual-write UoW: establish/increase batch & lokasi, inbound mutasi `GoodsReceipt` (legacy `"DO"`), binding with pre-assigned BK/ST ids, and **monotonic** scope watermark (advance only when candidate `(TglMutasi, LegacyBukuId)` is strictly after current cursor via `LegacyWatermarkHelper`). Idempotent on `(TrsReffId, MovementKind, StokLokasiId)`.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/PostGoodsReceiptConsequenceCommand.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/PostGoodsReceiptConsequenceHandlerTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-D2-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — S1-D2 Done → S1-E
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyWatermarkHelper.cs` — raw-pair `IsAfterWatermark` overload (review fix)
- `PostGoodsReceiptConsequenceCommand.cs` — monotonic watermark advance (review fix)
- `PostGoodsReceiptConsequenceHandlerTest.cs` — second-receipt legacy dual-write asserts; backdated watermark regression (review fix)
- `LegacyWatermarkHelperTest.cs` — raw-pair ordering coverage

### Removed
- none

## 4. Behaviour / contracts implemented
- **UC-STL-001** Post Goods Receipt Consequence (capabilities 3.1–3.3; BR-STL-001…015 as enforced by domain models).
- **ADR-STL-005:** pre-assign `NunaId.NewLegacyCompact("BK"|"ST")` before building BindingInserts (ids do not flow back from Commit).
- **ADR-STL-007 / GAP-STL-001:** coexistence flag already on UoW/gate; handler always calls gate first.
- **GAP-STL-004:** gate-on-demand only (reuses `LegacyFreshnessGate` → UC-STL-012); no worker.
- Flow: Guard → EnsureFreshAsync → LoadByNaturalKey → idempotency Exists probe → Create/IncreaseLokasi → draft → UoW.Commit.
- New batch `TglMasuk = TglMutasi` (hydrate/replayer parity; command has no separate receipt date).
- OCC conflicts propagate as `InvalidOperationException` (not swallowed).
- MovementKind = `GoodsReceipt` / legacy `"DO"`.
- Scope watermark is a forward catch-up cursor: backdated / concurrent earlier receipts refresh `LastSyncedAt` only and do not regress `TglMutasiLast` / `LastLegacyBukuId`.

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `PostGoodsReceiptConsequenceHandlerTest` | Success dual-write + binding + scope + legacy; idempotent retry; gate abort (no Commit); OCC concurrency; second receipt same DO increases balance **+ dual legacy buku/stok**; backdated receipt does not regress watermark | Pass |
| `LegacyWatermarkHelperTest` | Empty watermark; filter; real watermark ordering; **raw-pair overload** | Pass |

## 6. Verification evidence
- Commands run:
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~PostGoodsReceiptConsequenceHandlerTest|FullyQualifiedName~LegacyWatermarkHelperTest"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~LegacyStockWriterPortTest|FullyQualifiedName~StockConsequenceUnitOfWork"` (D1 regression)
- Outcome (pass/fail): Pass — review-fix filter Failed: 0, Passed: 11 (PostGoodsReceiptConsequenceHandlerTest 6 + LegacyWatermarkHelperTest 5)
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **Command naming `*Command` not `*Cmd`** — matches MutasiFeature / StockLedger C1–C2 and plan card / G-05 Inventory live naming.
  2. **`TglMasuk = TglMutasi` on first Create** — command has no separate TglMasuk; matches hydrate replayer; explicit and pragmatic.
  3. **Gate mocked via `LegacyFreshnessGate` + Moq `IMediator`** — gate is concrete (not interface); tests still prove abort-before-UoW and success path with real UoW.
  4. **Monotonic watermark via Application helper** — reuse `LegacyWatermarkHelper` ordering in handler; non-advancing path calls `AdvanceWatermark` with existing cursor to refresh `LastSyncedAt` / clear Stale (no new domain method).

## 8. Artifact updates
- Files updated: Domain `README.md` next-card pointer; this summary (incl. review-fix notes)
- Why: no durable domain/architecture redesign; `TglMasuk` choice and monotonic watermark behaviour recorded here rather than expanding architecture.

## 9. Done-when checklist
- [x] Idempotent post + binding verified.
- [x] No HTTP controller added.
- [x] Implementation summary written.
- [x] Domain README next-card pointer updated.
- [x] Verification filter passes.

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-E** (UC-STL-002 Post Stock Transfer Consequence)
- Blockers / residual risks:
  - Callers must not reuse a `StockBatchModel` after Commit failure — always reload (handler already reloads each request).
  - Transfer needs allocation (S1-B) + dual OUT/IN legacy ops; same gate → domain → UoW envelope.
- Anything the next agent must not redo: Do not reimplement writer/UoW/gate/date helper or goods-receipt handler; do not add HTTP/worker/FEFO-in-DAL; do not add buku delete.
