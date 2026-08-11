# Stock Ledger S1-C1 — Implementation Summary

## 1. Slice identity
- Slice ID: S1-C1
- Plan card title: Legacy read port + UC-STL-010 Hydrate
- Date: 2026-08-10
- Status: Done (review fixes applied)

## 2. Goal delivered

Implemented `ILegacyStockReadPort` with scoped Item+DO SQL against `tb_buku`/`tb_stok` (no legacy mutation), S1 MovementKind ACL (`LegacyMovementKindMapper`), and MediatR `HydrateScopeFromLegacyCommand` that replays journals into v2 batch/lokasi/mutasi + scope watermark + binding, with Aligned short-circuit idempotency and Inconsistent on unknown kinds / domain apply failures / missing batch baseline after binding-only recovery.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/Ports/ILegacyStockReadPort.cs` (+ journal/balance read models)
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyMovementKindMapper.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/HydrateScopeFromLegacyCommand.cs` (Command, OutcomeEnum, Result, Handler)
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/LegacyStockReadPort.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/LegacyMovementKindMapperTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/HydrateScopeFromLegacyHandlerTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/LegacyStockReadPortTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-C1-implementation-summary.md`

### Changed (review fix)
- Hydrate: on any binding skip with null batch (after reload), `MarkInconsistent` immediately — never `Create` a replacement baseline from later unbound journals (covers all-skipped and partial binding + remaining journals)
- Hydrate: `batchDirty` only after successful apply — failed first apply does not persist empty StockBatch
- Hydrate: domain apply failures (`IncreaseLokasi`/`DecreaseLokasi`/mutasi create) → `MarkInconsistent` (same persist path as unknown kind)
- `ListJournals`: order in C# by composed `TglMutasi` then `LegacyBukuId` (parity with `ComposeMutasiDateTime` for null/empty jam)
- `src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs` — manual `AddScoped` for `ILegacyStockReadPort`, `IStockMutasiRepo`, `IStockLegacyBindingRepo`

### Removed
- none

## 4. Behaviour / contracts implemented
- **UC-STL-010** Hydrate Scope From Legacy: NotAligned → list journals → map kind → apply inbound/outbound on `StockBatchModel` → insert mutasi + MutasiBuku binding → `MarkAligned` watermark (`TglMutasiLast`, `LastLegacyBukuId`) only when batch baseline exists (or journals empty).
- **Idempotency:** scope already Aligned → `Idempotent` (no reads/writes); binding unique on `LegacyBukuId` skips already-applied journals if re-entered while NotAligned **without double qty**; skip + existing batch → Aligned; **any** binding skip with missing batch → Inconsistent immediately (never invent a second baseline from remaining unbound journals; never false Aligned).
- **GAP-STL-003:** S1 legacy strings only (`DO`, `MT_OUT`/`MT_IN`, `DB`/`DU`/`DT`, `PK`, `RJ`/`RU`/`RT`, `DB_V`/`DU_V`/`DT_V`); unknown → `MarkInconsistent` + outcome Inconsistent (no invent).
- **Domain apply failures:** missing lokasi, negative remaining, blank LayananId, etc. → `MarkInconsistent` with buku id + reason (no throw mid-hydrate); empty in-memory batch from failed first apply is not persisted.
- **Stale:** returns `StaleNeedsCatchUp` without mutation (S1-C2).
- **Legacy reads:** `ListJournals` / `ListBalances` filter `fs_kd_barang` + `fs_kd_do` only; journals ordered after map by composed datetime then `fs_kd_trs` / `LegacyBukuId`. Does not reuse Layanan-scoped `tb_buku_dal.ListData`.
- **ListBalances:** implemented on port; hydrate handler does not call it (replay is journal-driven).
- **Coexistence / dual-write / gate:** not implemented (S1-C2 / S1-D1).
- **BR-STL-011/012:** depleted lokasi retained via domain `DecreaseLokasi`.
- Safe interim: GAP-STL-003 only; GAP-STL-005 unchanged (NoBatch carried but unused for uniqueness).

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `LegacyMovementKindMapperTest` | 13 S1 strings (+ case-insensitive DO); null/empty/unknown/`DO_V` fail | Pass |
| `HydrateScopeFromLegacyHandlerTest` | Empty → Aligned; DO inbound; deplete retained; Aligned idempotent; binding+batch skip (qty retained); binding without batch → Inconsistent; partial binding + remaining unbound → Inconsistent (no Create/Aligned); outbound-before-inbound → Inconsistent (no empty batch save); unknown → Inconsistent; prior Inconsistent; Stale deferral | Pass |
| `LegacyStockReadPortTest` | Scoped journals + order; empty/whitespace jam compose order; sentinel ED + datetime compose; scoped balances; parse helpers (null/empty jam → 00:00:00) | Pass |

## 6. Verification evidence
- Commands run:
  - `dotnet build "src/bilreg/b09-bilreg-api.sln"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~HydrateScopeFromLegacy|FullyQualifiedName~LegacyStockReadPortTest|FullyQualifiedName~LegacyMovementKindMapperTest"`
- Outcome (pass/fail): Pass — Failed: 0, Passed: 36
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **No UoW for hydrate persist:** batch, mutasi, binding, and scope saved via separate repo calls (plan allows until S1-D1). Explicit; Moq/handler tests do not need ambient TX; live read tests use `TransHelper.NewScope()`.
  2. **Audit `"STL"` placeholder** retained from A2 on repo stamps; command carries `UserId` for future use.
  3. **Manual DI** for read port + mutasi/binding repos (not Scrutor-auto via `ISaveChange`/`ILoadEntity`).
  4. **Plan naming `*Command`** over skill `*Cmd` — matches MutasiFeature / plan G-05 Inventory convention.
  5. **`TrsReffId` fallback:** if legacy `fs_kd_mutasi` blank, use `fs_kd_trs` so mutasi factory Guard passes.

## 8. Artifact updates
- Files updated: `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` (next-card pointer only)
- Why: behaviour matches architecture §5 UC-STL-010 and §8 scope/binding; no canonical domain/architecture redesign

## 9. Done-when checklist
- [x] Hydrate establishes ledger from legacy without writing legacy.
- [x] Idempotent re-hydrate does not double qty (Aligned short-circuit; binding skip with batch baseline; binding skip without batch → Inconsistent immediately, including when later unbound journals remain).
- [x] Unknown kinds mark Inconsistent.
- [x] Verification filter passes.
- [x] Implementation summary written.

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-C2** (catch-up UC-STL-011 + freshness gate UC-STL-012)
- Blockers / residual risks:
  - Mid-replay Inconsistent after some inserts still leaves partial mutasi/binding rows until steward resolution (no UoW until S1-D1). Prefer Inconsistent over false Aligned; any binding skip with missing batch → Inconsistent immediately (no replacement baseline from later journals).
  - Hydrate does not compare `ListBalances` to Σ lokasi (reconcile is S1-H).
  - `StockLedger:CoexistenceEnabled` options still deferred to C2.
- Anything the next agent must not redo: Do not reimplement scoped legacy SQL or MovementKind ACL; extend watermark catch-up predicate on the same port; do not add dual-write or HTTP; do not put FEFO in DAL; do not invent MovementKinds beyond S1 map.
