# Stock Ledger Phase 1 / P1-S4 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P1-S4 — Scope coexistence state + mechanism-neutral Synchronization Position  
**Plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)  
**Prior slices:** [`stock-ledger-phase1-s1-implementation-summary.md`](./stock-ledger-phase1-s1-implementation-summary.md), [`stock-ledger-phase1-s2-implementation-summary.md`](./stock-ledger-phase1-s2-implementation-summary.md), [`stock-ledger-phase1-s3-implementation-summary.md`](./stock-ledger-phase1-s3-implementation-summary.md)  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS  

---

## WHAT

In-memory coexistence state domain for Item + Receipt Source under `InventoryContext/StockLedgerFeature`. No SQL, fingerprint computation, Freshness Gate behavior, reconstruction calculation, ports, or legacy adapters.

| Area | Delivered |
|---|---|
| Opaque position | `SynchronizationPositionType` (opaque `byte[]` + `AlgorithmVersion`; defensive copy; content equality) |
| Scope state | `StockLedgerScopeStateModel` / `IStockLedgerScopeKey` |
| Reconstruction transitions | `RequireReconstruction`, `BeginReconstruction`, `CompleteReconstruction`, `MarkReconstructionInconsistent` |
| Synchronization transitions | `MarkLegacyChangePending`, `RequireSynchronization`, `CompleteSynchronization`, `MarkSynchronizationInconsistent` |
| Supporting fields | Optional `ReconstructionBasisVersion`, `InconsistencyReason` (G-04 shapes) |
| Tests | `StockLedgerScopeStateTest` (14 unit tests) |

Enums from P1-S1 (`ReconstructionStatusEnum`, `SynchronizationStateEnum`, `StockFactOriginEnum`) were reused — not duplicated.

Legacy `StokFeature` was not modified.

---

## WHY this approach fits the current codebase

- P1-S1 already defined scope keys and coexistence enums; S4 only adds the **state machine shape** those types were waiting for.
- Matches existing Stock Ledger domain style: immutable `record` aggregates that return new instances, `Ardalis.GuardClauses`, explicit `InvalidOperationException` on illegal transitions.
- Synchronization Position is mechanism-neutral opaque bytes + algorithm version per the locked ADR — no `(fd_tgl_jam_mutasi, fs_kd_trs)` watermark fields.
- Origin remains on facts/layers only; scope state has no `Origin` / `IsAuthoritative` members, so G-04 origin-vs-authority separation stays enforceable.
- Transition methods accept caller-supplied outcomes (position, reason) without reading `tb_buku` / `tb_stok`, matching “safe without live discovery.”

---

## Important design decisions

1. **Initial sync state for `NotReconstructed`** is `Current` with null Synchronization Position — there is not yet a Ledger baseline that can be stale. Sync lifecycle methods that need a position require `Reconstructed`.
2. **`RequireSynchronization`** accepts both `Current` and `LegacyChangePending` so fingerprint-mismatch callers (ADR) can mark required without forcing an intermediate pending step.
3. **`CompleteReconstruction` / `CompleteSynchronization`** set or advance the opaque position; they never invent fingerprint bytes.
4. **Reconstruction inconsistency** clears Synchronization Position and sets both Reconstruction and Synchronization status to `Inconsistent`.
5. **Synchronization inconsistency** keeps `ReconstructionStatus = Reconstructed` and retains the last known position (still useful for diagnosis); only Synchronization State becomes `Inconsistent`.
6. **`SynchronizationPositionType`** is a sealed class (not a record) so byte-content equality and defensive copies are explicit.

---

## Deviations from the Phase-1 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Opaque `byte[]` or `string` | `byte[]` primary + `CreateFromUtf8Token` convenience | Bytes match fingerprint storage; UTF-8 helper keeps tests readable |
| Transition examples only | Full domain lifecycle guards for reconstruction + sync | Domain §8.5/§8.6 already define the graph; encoding it now prevents illegal states in later slices |
| Inconsistency / basis not named in slice table | Added `InconsistencyReason` + `ReconstructionBasisVersion` | G-04 required capability; shapes needed before P1-S5 DDL field inventory |
| Recovery from `Inconsistent` | Not implemented | Needs accountable resolution workflow (later phases); not “safe without discovery” alone |

No planned P1-S4 work was already present beyond the P1-S1 enums/keys. Nothing was deferred that blocks P1-S5 schema.

---

## Validation

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **52 passed** (11 P1-S1 + 10 P1-S2 + 17 P1-S3 + 14 P1-S4), 0 failed |
| Solution build (via test host) | Succeeded (0 errors) |
| No SQL / repos / discovery / StokFeature changes | Confirmed |
| No watermark cursor fields / no `IsAuthoritative` | Asserted by UT11 / UT13 |

---

## Technical debt / limitations

1. No persistence — `BILRG_StokLedgerScope` DDL is P1-S5.
2. No fingerprint / set-diff / Freshness Gate behavior — Phase 3 / ports in P1-S7+.
3. No recovery transitions out of `Inconsistent` (reconstruction or synchronization).
4. No OCC version token on scope state (unlike Position); add if P1-S5/S6 evidence requires it.
5. `WithState` always replaces `InconsistencyReason` (callers pass `null` to clear); fine for current transitions, not a general merge API.

---

## P1-S5 readiness

**P1-S5 can proceed.** No blocker remains from P1-S4.

P1-S5 should inventorize fields from Movement (S2), Layer/Position (S3), and Scope State / Synchronization Position (S4) into additive `BILRG_*` scripts. Do not implement reconstruction/sync algorithms or DAL round-trips in S5.
