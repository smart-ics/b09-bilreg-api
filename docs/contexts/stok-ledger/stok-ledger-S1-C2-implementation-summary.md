# Stock Ledger S1-C2 — Implementation Summary

## 1. Slice identity
- Slice ID: S1-C2
- Plan card title: Catch-up (UC-STL-011) + Freshness Gate (UC-STL-012)
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Implemented watermark-based catch-up of legacy `tb_buku` rows into the v2 ledger (UC-STL-011), gate-on-demand Ensure Freshness for scopes touched by later native posts (UC-STL-012), and `LegacyFreshnessGate` with `StockLedger:CoexistenceEnabled` cutover no-op (ADR-STL-007 / GAP-STL-001 / GAP-STL-004). Shared journal apply logic extracted so hydrate and catch-up keep C1 binding-skip / null-batch → Inconsistent invariants.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/StockLedgerCoexistenceOptions.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyWatermarkHelper.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyScopeJournalReplayer.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyFreshnessGate.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/CatchUpScopeFromLegacyCommand.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/EnsureFreshnessForScopesCommand.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/CatchUpScopeFromLegacyHandlerTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/EnsureFreshnessForScopesHandlerTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/LegacyFreshnessGateTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/LegacyWatermarkHelperTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-C2-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyWatermarkHelper.cs` — absent-watermark (`EmptyDate` + empty buku id) treats all journals as after-watermark
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/CatchUpScopeFromLegacyCommand.cs` — allowCreateBatch when no batch + pending
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyScopeJournalReplayer.cs` — xmldoc for catch-up first-batch path
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/HydrateScopeFromLegacyCommand.cs` — delegates journal loop to `LegacyScopeJournalReplayer`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/HydrateScopeFromLegacyHandlerTest.cs` — constructs replayer
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/CatchUpScopeFromLegacyHandlerTest.cs` — empty-aligned first batch + bindings/outbound-without-batch cases
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/EnsureFreshnessForScopesHandlerTest.cs` — empty-aligned MarkStale path
- `src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs` — `Configure<StockLedgerCoexistenceOptions>`; scoped `LegacyScopeJournalReplayer`, `LegacyFreshnessGate`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — S1-C2 Done pointer

### Removed
- none

## 4. Behaviour / contracts implemented
- **UC-STL-011 Catch Up Scope From Legacy:** filter journals with port `TglMutasi` / `LegacyBukuId` after watermark (datetime `>` or equal + `fs_kd_trs` / buku id ordinal greater); **absent watermark** (`EmptyDate` + empty `LastLegacyBukuId` from C1 empty hydrate) treats every journal as after-watermark; append mutasi + bindings; `AdvanceWatermark` (Stale → Aligned); **may create the first StokBatch** when none exists and pending creatable inbound journals are present (empty-aligned → first legacy DO); bindings-only / outbound-only with no batch → Inconsistent; NotAligned / Inconsistent early exits without legacy reads.
- **UC-STL-012 Ensure Freshness For Scopes:** NotAligned → Hydrate then CatchUp; Aligned + rows beyond watermark (including empty-watermark) → `MarkStale` then CatchUp; Stale → CatchUp; Inconsistent → `AbortedInconsistent` (fail-fast).
- **LegacyFreshnessGate:** S1-D+ helper; when `CoexistenceEnabled=false` no-ops Success (ADR-STL-007 comment in code); otherwise sends EnsureFreshness.
- **GAP-STL-001:** options section `StockLedger`, property `CoexistenceEnabled`.
- **GAP-STL-003:** unknown MovementKind → Inconsistent via shared replayer (no invent).
- **GAP-STL-004:** gate-on-demand only; no background worker.
- **C1 invariants preserved:** binding skip + missing batch → Inconsistent immediately; `batchDirty` only after successful apply; domain apply failures → Inconsistent; hydrate still returns `StaleNeedsCatchUp` for pre-marked Stale; C1 empty hydrate still uses `EmptyDate` sentinel (ADR-STL-003).
- **Coexistence / dual-write / UoW:** not expanded (S1-D1).

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `LegacyWatermarkHelperTest` | EmptyDate + empty buku → any journal after; real watermark excludes older; empty-aligned scope detects later rows | Pass |
| `CatchUpScopeFromLegacyHandlerTest` | Post-watermark only; same-datetime tie-break by buku id; idempotent empty pending; binding skip no double qty; Stale→Aligned (empty / with pending); unknown kind → Inconsistent; empty-aligned creates first batch from DO inbound; bindings without batch → Inconsistent; outbound-only without batch → Inconsistent; NotAligned / prior Inconsistent without reads | Pass |
| `EnsureFreshnessForScopesHandlerTest` | NotAligned hydrates then catch-up; Aligned+new → MarkStale + catch-up; empty-aligned + later legacy → MarkStale + catch-up; Stale catch-up only; Inconsistent abort; hydrate Inconsistent abort; multi-scope success | Pass |
| `LegacyFreshnessGateTest` | Multi-scope delegate; coexistence false no-op; inconsistent abort propagated | Pass |

## 6. Verification evidence
- Commands run:
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~CatchUpScopeFromLegacy|FullyQualifiedName~EnsureFreshnessForScopes|FullyQualifiedName~LegacyFreshnessGateTest|FullyQualifiedName~LegacyWatermarkHelper"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~HydrateScopeFromLegacy|FullyQualifiedName~LegacyStockReadPortTest|FullyQualifiedName~LegacyMovementKindMapperTest"`
- Outcome (pass/fail): Pass — C2+helper Failed: 0, Passed: 26; C1 regression Failed: 0, Passed: 36
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **No UoW for catch-up persist:** same as C1 — separate repo calls until S1-D1. Explicit; Moq tests do not need ambient TX.
  2. **Plan naming `*Command`** over skill `*Cmd` — matches MutasiFeature / plan G-05.
  3. **Manual DI** for `LegacyScopeJournalReplayer` + `LegacyFreshnessGate` (not Scrutor Nuna markers).
  4. **Shared replayer extraction** — pragmatic DRY for hydrate/catch-up without redesigning domain.

## 8. Artifact updates
- Files updated: Domain `README.md` next-card pointer; this summary (review fix: absent-watermark + first-batch catch-up)
- Why: behaviour matches architecture §5 UC-STL-011/012 and §8 watermark predicate; empty-watermark coexistence path corrected without changing ADR-STL-003 sentinel

## 9. Done-when checklist
- [x] Watermark + no double apply verified (including empty-watermark / absent watermark).
- [x] Gate aborts Inconsistent scopes.
- [x] No background worker introduced.
- [x] `StockLedger:CoexistenceEnabled` options bound; cutover no-op documented in code.
- [x] Verification filters pass (C2 + C1 regression).
- [x] Implementation summary written.

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-D1** (legacy writer + atomic `IStockConsequenceUnitOfWork`)
- Blockers / residual risks:
  - Mid-replay Inconsistent after some inserts still leaves partial mutasi/binding rows until S1-D1 UoW (same as C1).
  - Native posts must call `LegacyFreshnessGate.EnsureFreshAsync` before UoW (architecture §13); not wired until S1-D+.
- Anything the next agent must not redo: Do not reimplement watermark predicate, replayer, or freshness gate; do not add dual-write/HTTP/FEFO-in-DAL; leave UoW atomicity to S1-D1; keep binding-skip + null-batch → Inconsistent; keep EmptyDate hydrate sentinel and absent-watermark special-case in helper.
