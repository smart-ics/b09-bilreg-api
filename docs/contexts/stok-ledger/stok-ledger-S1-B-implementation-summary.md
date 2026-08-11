# Stock Ledger S1-B — Implementation Summary

## 1. Slice identity
- Slice ID: S1-B
- Plan card title: Outbound Allocation domain (FEFO / FIFO / Explicit ED)
- Date: 2026-08-10
- Status: Done

## 2. Goal delivered

Implemented pure-domain Outbound Allocation as `StockOutboundAllocator` with immutable candidate/line/result types. Selection follows Explicit ED → FEFO → FIFO (BR-STL-022…027); matrix unit tests cover ED present/absent/override, multi-balance split, insufficient shortfall, and eligibility filters. No SQL, MediatR, or legacy.

## 3. What changed
### Added
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockAllocationCandidateType.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockAllocationLineType.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockAllocationResult.cs`
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/StockOutboundAllocator.cs`
- `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/StockOutboundAllocatorTest.cs`
- `docs/contexts/stok-ledger/stok-ledger-S1-B-implementation-summary.md`

### Changed
- `src/bilreg/Bilreg.Domain/InventoryContext/StockLedgerFeature/README.md` — S1-B Done pointer

### Removed
- none

## 4. Behaviour / contracts implemented
- Domain service: `StockOutboundAllocator.Allocate(brgId, layananId, requestedQty, candidates, explicitTglEd?)`.
- BR-STL-022 — filter to matching `BrgId` + `LayananId` and `QtySisa > 0`.
- BR-STL-023 — when `explicitTglEd` is non-null, only equal `TglEd`; order by `TglMasuk`, then `BrgMasukReffId`.
- BR-STL-024 — else if any eligible has non-sentinel ED → FEFO: `TglEd` ↑, `TglMasuk` ↑, `BrgMasukReffId` ↑.
- BR-STL-025 — else FIFO: `TglMasuk` ↑, `BrgMasukReffId` ↑.
- BR-STL-026 — split requested qty across multiple balances/lines.
- BR-STL-027 — insufficient returns `IsSuccess=false` with partial lines + `ShortfallQty`; never negative qty.
- GAP-STL-005 — `NoBatch` omitted from candidate type; not used for eligibility/ordering.
- Coexistence / dual-write / gate: not applicable in S1-B.
- Safe interim: GAP-STL-005 only.

## 5. Tests added
| Test class | Scenarios covered | Result |
|---|---|---|
| `StockOutboundAllocatorTest` | FEFO earliest ED; FIFO all-sentinel; FIFO DO tie-break; Explicit ED override; same-ED TglMasuk tie-break; multi-balance split; insufficient partial+shortfall; depleted excluded; wrong Brg/Layanan excluded; Explicit ED no match; sentinel after real ED in FEFO; reject qty≤0; `FromLokasi` mapping | Passed (13) |

## 6. Verification evidence
- Commands run:
  - `dotnet build "src/bilreg/b09-bilreg-api.sln"`
  - `dotnet test "src/bilreg/Bilreg.Test/Bilreg.Test.csproj" --filter "FullyQualifiedName~StockOutboundAllocatorTest" --no-build`
- Outcome (pass/fail): pass — build 0 errors; 13/13 tests passed
- Notable warnings: pre-existing unrelated CS86xx / obsolete warnings elsewhere in solution; none in S1-B files

## 7. Coding-standard compliance
- Followed ENGINEERING / DATABASE / NAMING / INSTRUCTION / skills: Yes / with overrides
- Overrides:
  - Plan card names allocation value objects `*Type` / `StockAllocationResult` (not `*Model`) — followed card over aggregate NAMING rule; pragmatic immutable records, no persistence.
  - Insufficient result returns **partial lines + shortfall** (plan allowed either) for operational clarity; still never negative.

## 8. Artifact updates
- Files updated: `README.md` (feature folder next-card pointer only)
- Why: durable domain/architecture truth unchanged; no ARTIFACTS.md / architecture / domain edits needed

## 9. Done-when checklist
- [x] Matrix tests pass for ED present / absent / override
- [x] No DAL/controller contains allocation ordering
- [x] `StockOutboundAllocatorTest` filter passes
- [x] Summary doc created at `docs/contexts/stok-ledger/stok-ledger-S1-B-implementation-summary.md`

## 10. Handoff to next slice
- Next recommended SLICE_ID: **S1-C1** (legacy read + hydrate); allocator consumers are later **S1-E / S1-F1 / S1-F2**
- Blockers / residual risks: none for S1-B; production caller wiring remains GAP-STL-002 (out of S1)
- Anything the next agent must not redo: Do not add FEFO/FIFO `ORDER BY` in DAL; keep `ListAllocationCandidates` unordered; call `StockOutboundAllocator.Allocate` after mapping lokasi → `StockAllocationCandidateType` (or `FromLokasi`)
