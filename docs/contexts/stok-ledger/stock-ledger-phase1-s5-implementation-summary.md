# Stock Ledger Phase 1 / P1-S5 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P1-S5 — Additive SQL schema  
**Plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Prior slices:** [`stock-ledger-phase1-s1-implementation-summary.md`](./stock-ledger-phase1-s1-implementation-summary.md) … [`stock-ledger-phase1-s4-implementation-summary.md`](./stock-ledger-phase1-s4-implementation-summary.md)  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS  

---

## WHAT

Additive Stock Ledger persistence schema under `Bilreg.SqlDb/InventoryContext/StockLedgerFeature/`. Schema only — no DAL, repositories, reconstruction, synchronization, Freshness Gate, or FO transaction behavior.

| Script | Table / purpose |
|---|---|
| `BILRG_StokMovement.sql` | Immutable movement header (`StockMovementModel`) |
| `BILRG_StokMovementLine.sql` | Movement lines (`StockMovementLineType`) |
| `BILRG_StokLayer.sql` | Layers including depleted (`StockLayerModel`) |
| `BILRG_StokPosition.sql` | Write-scope position + OCC `Version` (`StockPositionModel`) |
| `BILRG_StokLedgerScope.sql` | Coexistence state + opaque Synchronization Position (`StockLedgerScopeStateModel`) |
| `BILRG_StokSourceIdempotency.sql` | Unique source consequence / sync batch keys (G-07) |
| `BILRG_StokLedger_Rollback.sql` | Disposable-DB drop script only |

Scripts are registered in `Bilreg.SqlDb.sqlproj` as `None` (same pattern as other additive `BILRG_*` features). Legacy `tb_stok` / `tb_buku` / `FARIN_*` were not altered.

---

## WHY this schema shape

- Field inventory comes from the **implemented** P1-S1–P1-S4 domain models, not illustrative plan names alone.
- Prefix `BILRG_` and PascalCase columns follow [`docs/DATABASE.md`](../../DATABASE.md); module stays isolated from legacy stock authority.
- No database FK constraints (logical relationships only) — avoids deployment rigidity and legacy coupling.
- No triggers / stored-proc business rules — application owns invariants.
- Optional domain values use empty-string / `'3000-01-01'` / `0x` sentinels rather than NULLs, matching project null philosophy.
- Enums persist as `INT` (`MovementKind`, `Direction`, `Origin`, reconstruction/sync status, idempotency kind).
- Quantities use `DECIMAL(18,4)` to match domain `decimal` (fractional units) without forcing a later break from legacy `INT`/`DECIMAL(18,0)`.
- ULID identities use `VARCHAR(26)` (domain `Ulid.NewUlid()`; consistent with existing FARIN ULID widths, not the generic `VARCHAR(12)` default).
- Item / Receipt Source / Location widths follow existing inventory conventions (`BrgId` 13, `ReceiptSourceId`/`fs_kd_do` 10, `LayananId` 5).

---

## Domain → table mapping

| Domain (P1-S1–S4) | Persistence |
|---|---|
| `StockMovementModel` | `BILRG_StokMovement` — `StockMovementId`, `SourceTransactionId`, `MovementKind`, `EffectiveBusinessTime`, `Origin`, `ReversedMovementId`, `CorrectedMovementId` |
| `StockMovementLineType` | `BILRG_StokMovementLine` — `StockMovementId` + `[LineNo]`, write-scope keys, `Direction`, `Quantity`, `AmountPerUnit`, `Origin`, `StockLayerId` |
| `StockLayerModel` | `BILRG_StokLayer` — layer identity, write-scope keys, forming movement, quantities, valuation, `ExpirationDate`, `EffectiveReceiptTime`, `Origin`, `Batch` |
| `StockPositionModel` | `BILRG_StokPosition` — PK `(BrgId, ReceiptSourceId, LayananId)`, `Version` (BIGINT OCC token) |
| `StockLedgerScopeStateModel` + `SynchronizationPositionType` | `BILRG_StokLedgerScope` — PK `(BrgId, ReceiptSourceId)`, status enums, `SynchronizationPositionOpaque` VARBINARY(512) + `AlgorithmVersion`, basis/reason text |
| Source idempotency (G-07 shape; no domain type yet) | `BILRG_StokSourceIdempotency` — surrogate `IdempotencyId`, unique `(IdempotencyKind, IdempotencyKey)`, optional movement/source/scope hints |

Empty optional mappings used by DAL later:

| Domain null / absent | Stored sentinel |
|---|---|
| `ReversedMovementId` / `CorrectedMovementId` / `StockLayerId` / `Batch` / text optionals | `''` |
| `ExpirationDate` absent | `'3000-01-01'` |
| `SynchronizationPosition` absent | `0x` + `AlgorithmVersion = ''` |

No `IsAuthoritative` (or equivalent) column exists on any table.

---

## Indexes, uniqueness, concurrency

| Choice | Detail |
|---|---|
| Position OCC | `Version BIGINT` on `BILRG_StokPosition`; updates condition on expected version in P1-S6 (schema ready) |
| Idempotency uniqueness | `UX_BILRG_StokSourceIdempotency_Kind_Key` on `(IdempotencyKind, IdempotencyKey)` WHERE key `<> ''` |
| Movement lookup | `IX_…_SourceTransactionId`; filtered indexes on reversal/correction links |
| Layer FIFO / position load | `IX_…_FifoCandidates` (filtered `RemainingQuantity > 0`) and `IX_…_WriteScope` (includes depleted) |
| Scope PK | `(BrgId, ReceiptSourceId)` — reconstruction scope identity |
| Position PK | `(BrgId, ReceiptSourceId, LayananId)` — write-scope identity from domain (no separate PositionId) |

---

## Deviations from the Phase-1 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Default PK `VARCHAR(12)` | `VARCHAR(26)` for movement/layer/idempotency IDs | Domain already generates ULID strings |
| Illustrative table list only | Field inventory from live domain models | S1–S4 finalized names/types (`AmountPerUnit`, opaque bytes, etc.) |
| Position “by Item+RS+Location” index | That key is the clustered PK | Natural write-scope identity; no surrogate invented |
| Scope OCC version | Not added | Domain scope state has no version token (S4 debt); Position carries OCC |
| `LineNo` column | Stored as delimited `[LineNo]` | `LINENO` is a T-SQL reserved keyword; domain name preserved |
| Deploy target named `DEVTEST` | Validated on disposable LocalDB `bilreg_stockledger_p1s5` | Same disposable/dev constraint; avoided `HOSPITAL_HPL`; LocalDB available without sharing hospital credentials in the agent session |
| Sqlproj `Build` vs `None` | `None Include` | Matches other additive `BILRG_*` scripts; avoids SSDT model coupling to legacy stock tables |

`IdempotencyKind` values are not yet a domain enum; recommended convention for P1-S6: `1 = SourceConsequence`, `2 = SyncBatch`.

---

## Database validation

Performed on disposable LocalDB database `bilreg_stockledger_p1s5` (created, validated, rolled back, dropped).

| Check | Result |
|---|---|
| Apply all six CREATE scripts | Succeeded |
| Re-apply scripts (idempotent guards) | Succeeded |
| Smoke `SELECT` against all six tables | Succeeded (empty counts) |
| Required indexes present (including unique idempotency UX) | Confirmed |
| Position OCC update conditioned on `Version` | `0 → 1` |
| Duplicate idempotency key | Error 2601 — blocked |
| Depleted layer (`RemainingQuantity = 0`) retained | Confirmed |
| Opaque Synchronization Position + algorithm version | Confirmed |
| Movement + `[LineNo]` insert/select | Confirmed |
| `tb_stok` / `tb_buku` present in disposable DB | Absent (untouched) |
| Rollback script drops all `BILRG_Stok*` tables | Remaining count `0` |

Never applied to `HOSPITAL_HPL`.

---

## Technical debt / limitations

1. No DAL/DTO/repository mapping yet — P1-S6.
2. No domain type for idempotency rows; kind codes are schema convention until Application defines them.
3. No OCC version on `BILRG_StokLedgerScope` (domain also lacks it).
4. No CHECK constraints encoding enum ranges — application/domain remains authoritative for legal values.
5. Filtered indexes require `QUOTED_IDENTIFIER ON` (script headers + `sqlcmd -I` when applying via sqlcmd).
6. Remote `devTest` / `DEVTEST` on `dev.smart-ics.com` was not used in this agent run; LocalDB disposable validation covers schema correctness. Apply the same scripts to team `devTest` before shared integration if desired.

---

## P1-S6 readiness

**P1-S6 can proceed.** No blocker remains from P1-S5.

P1-S6 should implement DAL/DTO/repository round-trips for Movement, Layer, Position, ScopeState, and Source Idempotency with Position OCC conflict handling and idempotent insert semantics. Do not start reconstruction, sync discovery, Freshness Gate, or FO transaction behavior in S6.
