# Stock Ledger S1-H — Implementation Summary

## 1. Slice identity
- Slice ID: S1-H
- Plan card title: UC-STL-020 Reconcile Scope + minimal UC-STL-021 Availability
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Delivered read-only `ReconcileScopeQuery` (UC-STL-020) that assesses hospital and per-location conservation including depleted balances and optionally compares legacy when coexistence is on, and `GetAvailabilityAtLocationQuery` (UC-STL-021) that returns positive QtySisa candidates ordered for allocation preview without authorizing sale.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/ReconcileScopeQuery.cs`
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/GetAvailabilityAtLocationQuery.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/ReconcileScopeQueryTest.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/GetAvailabilityAtLocationQueryTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-H-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/IStockMutasiRepo.cs` — `ListByScope`
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StokMutasiDal.cs` — `ListByScope` SQL
- `src/bilreg/Bilreg.Infrastructure/InventoryContext/StockLedgerFeature/StockMutasiRepo.cs` — pass-through
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockOutboundAllocator.cs` — `OrderForPreview`; `Allocate` reuses it
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — S1-H Done; S1 core complete

### Removed
- none

## 4. Behaviour / contracts implemented
- **UC-STL-020** Reconcile Scope: `(BrgId, BrgMasukReffId)` ± optional `LayananId`; includes depleted lokasi (BR-STL-030…032); differences explicit (BR-STL-035); no writes.
- **UC-STL-021** Get Availability At Location: `QtySisa > 0` via existing `ListAllocationCandidates`; optional `TglEd`; FEFO/FIFO/Explicit-ED preview via `StockOutboundAllocator.OrderForPreview` (BR-STL-023…025); does not authorize sale.
- Conservation checks: hospital `Batch vs Σ lokasi` / `Batch vs Σ movement` when unscoped; scoped `Σ lokasi vs Σ movement` when `LayananId` set; per-lokasi `QtySisa vs movement net`; optional `LegacyVsLedger` when `StockLedger:CoexistenceEnabled` (GAP-STL-001 / ADR-STL-007).
- Empty unknown scope → zeros, `IsConsistent = true` (deterministic).
- **GAP-STL-002:** in-process harness only; no production callers / HTTP.
- **GAP-STL-005:** NoBatch still excluded from availability eligibility/order.

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `ReconcileScopeQueryTest` | Conserved + depleted included; batch desync flags mismatch; empty scope consistent; read-only (row counts unchanged) | Pass (4) |
| `GetAvailabilityAtLocationQueryTest` | Positive only / excludes depleted; optional TglEd filter; FEFO preview order | Pass (3) |

## 6. Verification evidence
- Commands run:
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~ReconcileScopeQueryTest|FullyQualifiedName~GetAvailabilityAtLocationQueryTest"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockLedgerFeature"`
- Outcome (pass/fail): Pass — S1-H Failed: 0, Passed: 7; full feature Failed: 0, Passed: 169
- Notable warnings: existing solution nullable/obsolete warnings unrelated to this slice

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  1. **Query naming `*Query` not skill `*Qry`** — matches MutasiFeature / plan G-05 Inventory live naming.
  2. **Hospital conservation skipped when `LayananId` filter set** — batch qty is hospital-wide; scoped path uses `ScopedLokasiVsMovement` instead so optional location filter does not false-positive BR-STL-030 checks. Explicit in response difference kind.
  3. **`OrderForPreview` extracted in Domain** — keeps FEFO out of DAL while sharing order with `Allocate`.

## 8. Artifact updates
- Files updated: Domain `README.md` next-card pointer; this summary
- Why: no durable domain/architecture redesign; scoped-vs-hospital reconcile nuance recorded here rather than expanding architecture.

## 9. Done-when checklist
- [x] Reconcile includes depleted.
- [x] No mutation from queries.
- [x] Whole S1 program DoD checklist in §0.8 can be checked (reconcile + read-only; targeted StockLedgerFeature filter passes).
- [x] Implementation summary written.
- [x] Domain README updated (S1 core complete).

## 10. Handoff to next slice
- Next recommended SLICE_ID: **none for S1 core** — S1 complete. Deferred backlog remains in plan §3 / GAP-STL-002 (production caller wiring).
- Blockers / residual risks:
  - Legacy multi-`tb_stok` row interim (F1/G1/G2) may cause `LegacyVsLedger` differences even when BILRG is internally consistent — reported explicitly, not auto-repaired (BR-STL-035).
  - Production Purchasing/Apotek wiring remains out of S1 (GAP-STL-002).
- Anything the next agent must not redo: Do not reimplement reconcile/availability queries, `ListByScope`, or `OrderForPreview`; do not add silent repair, HTTP steward UI, scheduled reconcile worker, or rewrite return/gate/UoW/writer.
