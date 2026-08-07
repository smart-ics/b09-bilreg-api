# Stock Ledger Phase 1 / P1-S8 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P1-S8 — Consequence Unit of Work skeleton + failure rollback  
**Plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Prior slices:** [`stock-ledger-phase1-s1-implementation-summary.md`](./stock-ledger-phase1-s1-implementation-summary.md) … [`stock-ledger-phase1-s7-implementation-summary.md`](./stock-ledger-phase1-s7-implementation-summary.md)  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS  

---

## WHAT

G-18 **foundation** for Stock Ledger consequence persistence: one short Unit of Work that commits Movement, Position(+Layers), Scope coexistence state, and source idempotency atomically, optionally invoking `ILegacyCompatibilityWriterPort` inside the same ambient transaction — **without** a live legacy writer, reconstruction, Freshness Gate, discovery behavior, FO transactions, or public stock write endpoints.

| Area | Delivered |
|---|---|
| Application contract | `IStockConsequenceUnitOfWork`, `StockConsequenceDraft`, `StockConsequenceCommitResult`, `StockConsequenceCommitOutcomeEnum` |
| Application orchestration | `StockConsequenceUnitOfWork` — coordinates existing P1-S6 repos + P1-S7 writer port via `IUnitOfWork` |
| Transaction boundary | Reuses `TransHelperUnitOfWork` / `TransHelper.NewScope()` (same pattern as TataRekening) |
| Tests | Happy-path commit; rollback when fake writer throws; duplicate idempotency → `AlreadyCommitted`; G-23 skipped scenario markers |
| DI / API | **No** production DI registration for UoW or live legacy writer; **no** public write endpoints |

Legacy `StokFeature` / `tb_stok` / `tb_buku` were not written. No `IsAuthoritative` field.

---

## WHY this Unit of Work approach fits the repository

- The repo already has an Application-level `IUnitOfWork` / `IUnitOfWorkScope` abstraction implemented by Infrastructure `TransHelperUnitOfWork` (ambient `TransactionScope` via Nuna `TransHelper`). TataRekening handlers use exactly this pattern: `Begin` → repo saves → `Complete` (dispose without Complete rolls back).
- Stock Ledger P1-S6 repositories already enlist in ambient transactions through Dapper/`TransHelper` (same as other Inventory/BedOperational integration tests). A new raw `SqlConnection.BeginTransaction` path would duplicate and diverge from that convention.
- Orchestration stays in **Application** (no SQL in Application): UoW injects repo ports + `ILegacyCompatibilityWriterPort` and does not open connections. Infrastructure remains the transaction adapter and DAL owner.
- Idempotency uses the existing `IStockSourceIdempotencyRepo.InsertOrGetExisting` contract rather than inventing a second uniqueness mechanism.
- Live legacy writeback remains a port call inside the TX so Phase 4 can swap the fake for a real adapter without changing the commit shape.

---

## How existing repositories / ports are coordinated

Commit order inside one `IUnitOfWork` scope:

1. **Idempotency insert-or-get** (`SourceConsequence` kind)  
   - Duplicate → return `AlreadyCommitted` with the stored movement id; dispose without `Complete` (no rewrite, no second legacy call).  
2. **Movement** insert-once (`IStockMovementRepo.SaveChanges`)  
3. **Position(+Layers)** for each draft position (`IStockPositionRepo.SaveChanges`, OCC unchanged)  
4. **Scope state** when provided (`IStockLedgerScopeStateRepo.SaveChanges`)  
5. **Legacy compatibility writer** when `LegacyWrite` is present (`ILegacyCompatibilityWriterPort.Apply`)  
6. **`Complete()`** — durable commit

On any exception before `Complete`, ambient scope disposal rolls back all Ledger writes performed in that attempt (including the idempotency row), so a retry can start cleanly.

---

## Important transaction / rollback decisions

| Decision | Detail |
|---|---|
| Synchronous `Commit` | Matches repo/port sync style and TataRekening handlers; plan’s `CommitAsync` was illustrative |
| No nested outer test TX on happy path | UoW owns the only `Complete()` so durable commit is actually proven on `devTest` |
| Fake writer failure | `FakeLegacyCompatibilityWriterPort.ThrowOnApply` after Ledger writes ⇒ no durable Movement/Position/Scope/Idempotency rows |
| Duplicate key | Short-circuit before movement/position/scope/legacy; original consequence retained |
| No production DI | Phase 1 must not enable stock consequences against live hospital DB |
| Legacy side of G-18 | Documented as Phase 4+; Ledger-side atomicity only is proven here |

---

## Deviations from the Phase-1 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| `CommitAsync` | Synchronous `Commit` | Consistent with Application repos/ports and existing UoW consumers |
| Illustrative class names only | Concrete draft/result/outcome types | Needed for typed tests and later FO handlers |
| Infrastructure “transaction boundary” class | Reused existing `TransHelperUnitOfWork` | Avoid a second transaction abstraction |
| Optional DI registration | None for UoW / writer | Plan rule: register only if required for tests; tests construct manually |

No planned P1-S8 work was deferred that blocks Phase-2 planning.

---

## Tests / build / database validation

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **77 passed**, **8 skipped** (G-23 placeholders), 0 failed |
| P1-S8 new executable tests | 3 (happy / rollback / idempotent duplicate) |
| G-23 harness markers | 8 skipped facts documenting future Legacy↔New scenarios |
| Prior P1-S1–P1-S7 behavior | Preserved (74 prior + 3 new = 77) |
| Disposable DB | `ConnStringHelper.GetTestEnv()` → `devTest` (`dev.smart-ics.com`); schema via `StockLedgerSchemaFixture` |
| `tb_stok` / `tb_buku` writes | None |
| `HOSPITAL_HPL` | Not targeted |

---

## Remaining technical debt / limitations

1. Live `ILegacyCompatibilityWriterPort` adapter and FO writeback enlistment remain Phase 4+ (G-11/G-19).  
2. UoW does not yet enforce lock order (Item → Receipt Source → Location → legacy row id) — Mixed-writer ADR hardening is later.  
3. No MediatR/FO command or capability flag wires this UoW into production traffic — intentional for Phase 1.  
4. Scope state still lacks OCC (P1-S6 debt).  
5. G-23 coexistence scenarios are markers only; full harness waits on Phases 2–4 behavior.  
6. Audit columns on Ledger tables remain sentinel-filled until domain carries `AuditTrailType`.

---

## Phase 1 completeness

**Phase 1 is COMPLETE** for its planned exit criteria (G-01–G-07 foundation, G-18 Ledger-side UoW skeleton, ports-only for later gaps, additive schema on disposable DB, no authority semantics, no production enablement).

**No blocker remains** before Phase-2 **planning/implementation** may start. Phase 2 should implement reconstruction (G-10) using the ports/persistence/UoW foundation above — do not treat Phase 1 as having delivered reconstruction, sync, Freshness Gate, or FO receipts.

Do **not** start Phase 2 inside this slice; handoff only.
