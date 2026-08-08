# Stock Ledger Phase 1 / P1-S6 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P1-S6 — DAL / DTO / Repository round-trips  
**Plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Prior slices:** [`stock-ledger-phase1-s1-implementation-summary.md`](./stock-ledger-phase1-s1-implementation-summary.md) … [`stock-ledger-phase1-s5-implementation-summary.md`](./stock-ledger-phase1-s5-implementation-summary.md)  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS  

---

## WHAT

Persistence round-trip for the Stock Ledger foundation already created in P1-S1–S5. No reconstruction, legacy synchronization, Freshness Gate, Availability/Provenance Discovery, FO transactions, or `tb_stok` / `tb_buku` writes.

| Area | Delivered |
|---|---|
| Domain rehydration | `Rehydrate` on `StockMovementModel`, `StockLayerModel`, `StockLedgerScopeStateModel` |
| Idempotency domain | `StockSourceIdempotencyModel` + `StockSourceIdempotencyKindEnum` (`SourceConsequence=1`, `SyncBatch=2`) |
| Application ports | `IStockMovementRepo`, `IStockPositionRepo`, `IStockLedgerScopeStateRepo`, `IStockSourceIdempotencyRepo`, `StockLedgerPersistenceException` |
| Infrastructure | DTO / DAL / Repo for Movement(+Line), Layer, Position, Scope, SourceIdempotency |
| Tests | Repo integration: round-trip, immutable movement, depleted layer retention, Position OCC conflict, idempotent duplicate key |

Legacy `StokFeature` / `tb_*` / `FARIN_Stok*` were not modified. No `IsAuthoritative` field was introduced.

---

## WHY this persistence approach fits the repository

- Follows the established **BILRG DTO → DAL → Repo** stack (Reservation / RedirectRajal) and Nuna CRUD contracts (`IInsert` / `IUpdate` / `IGetData` / `ISaveChange` / `ILoadEntity` / `MayBe`).
- Movement + lines follow the header/detail coordination style used by OrderMutasi / LabTestDefinition (repo owns reconstruction; DAL never returns Models).
- Position OCC mirrors **BedOperational**: domain bumps `Version`; DAL `UpdateConditional(... AND Version = @ExpectedVersion)`; repo throws explicit `CONCURRENCY_CONFLICT` on 0 rows / stale token — no silent overwrite.
- Layers are upserted by `StockLayerId` under Position save; **never deleted on zero** so depleted layers remain durable.
- Movements are **insert-once** (immutable completed facts); second `SaveChanges` throws `IMMUTABLE_CONFLICT`.
- Source idempotency uses unique index `UX_BILRG_StokSourceIdempotency_Kind_Key`; `InsertOrGetExisting` returns the stored row on duplicate (including SqlException 2601/2627 races).
- Scrutor already registers standard Nuna CRUD/repo interfaces — no manual DI or public write API for Phase 1.
- Integration tests use `ConnStringHelper.GetTestEnv()` → disposable/dev `devTest` + `TransHelper.NewScope()`, matching Inventory/BedOperational patterns; schema applied idempotently from P1-S5 scripts.

---

## Important mapping / concurrency / idempotency decisions

| Decision | Detail |
|---|---|
| Sentinels | Optional domain null → `''` / `'3000-01-01'` / `0x` empty opaque + empty `AlgorithmVersion` (P1-S5 contract) |
| Enums | Persisted as `INT` matching domain enum values |
| Quantities | `DECIMAL(18,4)` ↔ `decimal` |
| ULID IDs | `VARCHAR(26)` |
| `[LineNo]` | T-SQL reserved keyword; SELECT uses `aa.[LineNo] AS [LineNo]` |
| Position OCC | Conditional update on prior `Version`; conflict ⇒ `StockLedgerPersistenceException.Concurrency` |
| Layer updates | Only `RemainingQuantity` (+ audit Upd*) mutated on existing layers |
| Scope upsert | Insert/Update without OCC (domain has no version token — S4/S5 debt) |
| Idempotency | Pre-check by business key; on unique violation reload winner; never overwrite first consequence |

---

## Deviations from the Phase-1 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Illustrative repo names only | Concrete interfaces + Scrutor-friendly Nuna contracts | Matches Reservation / BedOperational conventions |
| Idempotency “domain type TBD” | Added `StockSourceIdempotencyModel` + kind enum | Needed for typed DTO/Repo round-trip; kinds follow S5 recommendation |
| `Rehydrate` not named in plan | Added on Movement / Layer / Scope | Private constructors blocked durable load; same pattern as LabOware / RegInap |
| Movement upsert | Insert-only + immutable conflict | Domain treats completed movements as immutable facts |
| Position+Layer as separate repos | Position repo owns Layer DAL | Plan listed Position repo; write-scope aggregate naturally owns layers |
| Disposable DB named DEVTEST/LocalDB | Validated on team `devTest` (`dev.smart-ics.com`) via `GetTestEnv()` | Same disposable/dev constraint as other Bilreg.Test repo IT; refuse `HOSPITAL_HPL` in schema fixture |

---

## Tests / build / database validation

| Check | Result |
|---|---|
| `dotnet build` (via test host) | Succeeded (0 errors) |
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **64 passed** (52 prior domain + **12** P1-S6 persistence), 0 failed |
| Schema apply on `devTest` (idempotent P1-S5 scripts) | Succeeded via `StockLedgerSchemaFixture` |
| Movement round-trip + immutable reject | Passed |
| Position round-trip + depleted layer retained | Passed |
| Position stale-version concurrency | Passed (`CONCURRENCY_CONFLICT`) |
| Scope opaque Synchronization Position round-trip | Passed |
| Idempotency duplicate `(Kind, Key)` | Passed (`WasInserted=false`, original row retained) |
| `tb_stok` / `tb_buku` usage | None |

Never applied to / written against `HOSPITAL_HPL`.

---

## Technical debt / limitations

1. Audit columns on Stock Ledger tables are filled with empty / `3000-01-01` sentinels — domain models do not yet carry `AuditTrailType`.
2. Scope state still has no OCC version (domain + schema); concurrent scope writers can last-write-win until a later slice adds a token.
3. Position first-insert race (two concurrent inserts of same write-scope PK) is not specially mapped to `CONCURRENCY_CONFLICT` yet (PK violation would surface as SqlException); OCC path covers the intended update conflict.
4. No Application UoW yet — multi-aggregate transactional commit is P1-S8.
5. No production DI smoke test beyond Scrutor’s existing interface scan; Phase 1 still has no public stock write endpoints.
6. Line bulk insert relies on `SqlBulkCopy`; ambient `TransHelper` transaction enlistment follows existing Mutasi patterns.

---

## P1-S7 readiness

**P1-S7 can proceed.** No blocker remains from P1-S6.

P1-S7 should add Application **ports only** (legacy read/write/discovery/availability/provenance/reconciliation contracts) without live legacy adapters or Freshness Gate behavior. Do not start FO transactions or reconstruction workflows in S7.
