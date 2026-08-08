# Stock Ledger Phase 3 / P3-S4 — Implementation Summary

**Status:** COMPLETE — Review APPROVED (R-007 Resolved)  
**Date:** 2026-08-08  
**Slice:** P3-S4 — Incremental catch-up engine + Synchronization Position advancement (G-14 / G-15)  
**Plan:** [`stock-ledger-phase3-implementation-plan.md`](./stock-ledger-phase3-implementation-plan.md)

---

## Objective

Implement the sole Phase-3 **orchestration** slice for one reconstructed Item + Receipt Source scope: discover → interpret → short TX apply → reconcile → advance opaque Synchronization Position only when material reconciliation permits — or fail closed without rewriting legacy authority or erasing Ledger history.

---

## Repository inspection summary

| Finding | Implication for P3-S4 |
|---|---|
| P3-S1–S3 complete; no sync MediatR handler | New UseCase modeled on `ReconstructStockLedgerBaselineHandler` |
| `IStockConsequenceUnitOfWork` requires non-null Movement | Added `CommitSyncEvidence` for bootstrap / omission |
| Scope had only `TryUpdateWhenReconstructionStatus` | Added `TryUpdateWhenSynchronizationState` for sync claim/finalize |
| Reconstruction does not write discovery identity keys | R-001 bootstrap on first sync |
| `IdempotencyKey VARCHAR(200)` insufficient at schema-max | R-002 widen to `VARCHAR(400)` |
| R-005 / R-006 already Resolved | Compose interpreter compensatory Correct + `AllowsMaterialSynchronizationAdvance` |

---

## Existing components reused

| Component | Role |
|---|---|
| `ILegacyChangeDiscoveryPort` / `LegacyChangeDiscoveryPort` | P3-S1 detection |
| `LegacySyncDeltaInterpreter` | P3-S2 pure intents |
| `IStockReconciliationPort` / `StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance` | P3-S3 material-safe advance |
| `LegacyReconstructionBasisCalculator` / `fingerprint-v1` | Position advancement |
| `StockLedgerScopeStateModel` sync transitions | Claim / Complete / Inconsistent |
| `IStockConsequenceUnitOfWork` + SyncBatch | Short TX persistence |
| `LegacyChangeDiscoveryIdentityKeys` | Discovery identity format (R-001/R-004) |

---

## Review backlog items handled

| Item | Priority | Status |
|---|---|---|
| **R-001** | Required | **Resolved** — `LegacySyncIdentityBootstrapper` writes complete `SYNC\|BUKU\|…` / `SYNC\|STOK\|…` set on first sync |
| **R-002** | Required | **Resolved** — `IdempotencyKey` widened to `VARCHAR(400)`; alter script + boundary persistence test |
| **R-003** | Recommended | **Resolved** — `LegacySyncIntentApplicator` applies by kind; layer origin preserved; omission quantity-neutral |
| **R-004** | Recommended | **Resolved** — synthetic VOID/OMISSION keys persisted opaque; snapshot loader parses material keys only |
| R-005 / R-006 | — | Unchanged (**Resolved**) |
| **R-007** | Required | **Resolved** — void fail-closed for non-1:1 aggregate priors (see remediation section); verified by re-review |

### R-002 capacity evidence

P3-S1 journal keys embed schema-max `tb_buku` field widths (trs 10, layanan 5, jenis/mutasi 10, decimal 18,2, ISO mutation time, batch 15, po 10). Measured schema-max key length ≈ 180 chars (near the old 200 limit with no headroom for synthetic suffixes). Widened to **VARCHAR(400)** via additive alter that drops/recreates `UX_BILRG_StokSourceIdempotency_Kind_Key`.

---

## Orchestration flow implemented

```text
Load Scope (must be Reconstructed + fingerprint-v1 position)
  → DiscoverChanges (P3-S1)
  → Read legacy snapshot + identity coverage (R-001)
  → Unchanged + coverage complete ⇒ AlreadyCurrent (no claim)
  → Else short TX: RequireSynchronization (conditional SyncState update)
  → Bootstrap identities when coverage incomplete (Unchanged or sole RequiresScopedReDerive)
  → Else Interpret (P3-S2) → PersistIntents (short TX per intent)
  → Reconcile (P3-S3)
  → AllowsMaterialSynchronizationAdvance?
       Yes → CompleteSynchronization(fingerprint-v1 on post-sync snapshot)
       No  → MarkSynchronizationInconsistent; prior position retained
```

One synchronization boundary per command execution (no recursive discover→catch-up).

### Composition hygiene (not business reinterpretation)

When set-diff emits both `JournalInsert`/`Outbound` and `BalanceUpdate` for the same location, applying both would double-count. PersistIntents prefers establishment intents and persists the balance SyncBatch key as quantity-neutral evidence. Likewise, `ReversePriorMovement` at a location suppresses a subsequent depleting `AdjustLayerRemainingQuantity` from the pre-apply snapshot.

---

## Transaction boundary

| Phase | TX? |
|---|---|
| Discovery, legacy read, interpretation, snapshot load | Outside write TX |
| Sync claim (`RequireSynchronization`) | Short TX |
| Each SyncBatch consequence / evidence | Short TX via UoW |
| Finalize CompleteSynchronization / MarkInconsistent | Short TX |
| Reconciliation | Read-only |

Never holds legacy history reads inside a long write transaction. Never writes `tb_stok` / `tb_buku`.

---

## Synchronization Position advancement behavior

- Advances **only** after committed catch-up (or quantity-neutral bootstrap) **and** `AllowsMaterialSynchronizationAdvance` is true.
- New opaque value from `LegacyReconstructionBasisCalculator.Compute` on a fresh post-sync legacy snapshot (`fingerprint-v1`).
- `PendingSynchronization` overlay remains material-safe (R-006) — catch-up can complete while Scope was still `SynchronizationRequired`.
- Material inconsistency / ProvenanceLimitation / Ambiguous / Undeterminable ⇒ fail closed; prior position unchanged.

---

## Idempotency behavior

- `StockSourceIdempotencyKindEnum.SyncBatch` + intent `SyncIdempotencyKey`.
- Duplicate key ⇒ `AlreadyCommitted` (quantity-neutral; no second movement/position rewrite).
- Bootstrap keys link to reconstruction movement id when present (snapshot anchors for later void/update).

---

## Scoped re-derive behavior

- **Bounded path:** fingerprint mismatch with **no** Ledger-known keys (sole `RequiresScopedReDerive`) ⇒ bootstrap identity set, then reconcile; advance only if material-safe.
- **Fail closed:** Undeterminable discovery; interpreter Ambiguous / RequiresScopedReDerive with classifiable gaps; does **not** invoke reconstruction or erase history.

---

## Persistence changes

| Change | Detail |
|---|---|
| `BILRG_StokSourceIdempotency.IdempotencyKey` | `VARCHAR(200)` → `VARCHAR(400)` (create script + alter) |
| `TryUpdateWhenSynchronizationState` | Scope DAL/repo conditional update |
| `CommitSyncEvidence` | Movement-less SyncBatch UoW path |
| `IStockSourceIdempotencyRepo.ListSyncIdentityRecordsForScope` | Full identity rows for snapshot loader |
| `IStockPositionRepo.ListByLedgerScope` | Cross-location positions for catch-up |

---

## Tests and results

| Suite | Result |
|---|---|
| Solution build | **Succeeded** |
| Void-related filter (`JournalVoid` / `R007`) | **7 passed**, 0 failed |
| `SynchronizeStockLedgerScopeHandlerTest` + `LegacySyncDeltaInterpreterTest` | **35 passed**, 0 failed |
| `StockLedgerFeature` (single-threaded, `xUnit.MaxParallelThreads=1`) | **216 passed**, 7 skipped, 0 failed |

Covered: happy-path catch-up; LegacySynchronized movement/layer origin; origin-preserving balance adjust; duplicate SyncBatch; journal void history retention (hardened Synchronized path); multi-location void fail-closed sibling qty (R-007); position advance; PendingSynchronization complete; material inconsistency / provenance / crash-before-finalize retain prior position; R-001 bootstrap; R-002 max-length key; R-004 opaque synthetic keys; no legacy row mutation.

---

## Deviations from plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Illustrative class names | `SynchronizeStockLedgerScopeCommand` / Handler | Matches reconstruction UseCase style |
| Optional helper extraction | Snapshot loader, bootstrapper, applicator | Keeps handler thin |
| Composition filter for Insert+BalanceUpdate | Added in PersistIntents | Prevents double-count without reinterpreting S2 |

No architectural reopen. No Freshness Gate / P3-S6 retry framework / production DI.

---

## Known limitations

1. Full concurrent sync serialization / bounded retry remains **P3-S6**.
2. Unclassified new `tb_stok` identities still fail closed to `RequiresScopedReDerive` (P3-S1 rule).
3. Parallel `devTest` runs can deadlock (pre-existing); single-threaded StockLedgerFeature filter is green.
4. No production DI / HTTP endpoints.
5. **R-007:** Multi-line / multi-location reconstruction voids fail closed (Scope `Inconsistent`) rather than applying a precise location-scoped compensation. Precise journal→line Correct remains future work if needed; Option A was chosen as the smallest safe change given aggregate-only identity anchors.

---

## Files changed

| Path | Change |
|---|---|
| `…/UseCases/SynchronizeStockLedgerScopeCommand.cs` | Added — thin orchestrator |
| `…/LegacySyncLedgerSnapshotLoader.cs` | Added |
| `…/LegacySyncIdentityBootstrapper.cs` | Added (R-001) |
| `…/LegacySyncIntentApplicator.cs` | Added (R-003) |
| `…/IStockConsequenceUnitOfWork.cs` + `StockConsequenceUnitOfWork.cs` | `CommitSyncEvidence` |
| `…/IStockLedgerScopeStateRepo.cs` + Scope DAL/Repo | Sync-state conditional update |
| `…/IStockSourceIdempotencyRepo.cs` + DAL/Repo | List sync identity records |
| `…/IStockPositionRepo.cs` + `StockPositionRepo.cs` | `ListByLedgerScope` |
| `…/BILRG_StokSourceIdempotency.sql` + `.AlterIdempotencyKey.sql` | R-002 widen |
| `…/SynchronizeStockLedgerScopeHandlerTest.cs` | Integration tests |
| `…/LegacySyncIntentApplicatorTest.cs` | Pure unit tests |
| Docs: plan progress, backlog, this summary, ARTIFACTS | Updated |

---

## Acceptance checklist

| Criterion | Status |
|---|---|
| Thin orchestration composing P3-S1…S3 | **PASS** |
| Position advances only after committed batch + material-safe reconcile | **PASS** |
| SyncBatch duplicate quantity-neutral | **PASS** |
| R-001 / R-002 / R-003 / R-004 implemented | **PASS** (Resolved) |
| R-007 void fail-closed for unsafe aggregate priors | **PASS** (Resolved) |
| No legacy authority writes; history retained on void | **PASS** |
| No P3-S5/S6 / Phase-4+ / sync framework | **PASS** |
| StockLedgerFeature regression green (single-threaded) | **PASS** (216 passed, 7 skipped) |

---

## P3-S5 readiness

Catch-up is callable via `SynchronizeStockLedgerScopeCommand`. **R-007 re-review passed — P3-S5 may begin.** Freshness Gate should:

1. Call discovery;
2. Invoke catch-up **at most once** when needed;
3. Never block VB6 / never act as Authority Gate;
4. Rely on material-safe advance semantics already proven here;
5. Treat Scope `Inconsistent` / catch-up Ambiguous (including multi-line void fail-closed) as not-current.

---

## R-007 remediation

### Root cause

R-001 bootstrap and snapshot journal anchors associate every surviving legacy journal identity with the **aggregate** reconstruction `StockMovementId`. `InterpretJournalVoidDelete` called `prior.Reverse(...)`, which reverses **all** Movement lines. For a multi-line / multi-location reconstructed baseline, voiding one journal could commit sibling-location layer depletions in a short TX before material reconcile marked the Scope inconsistent.

### Repository findings

- Journal identity → Movement aggregate only (no line / layer targeting).
- Phase-2 reconstruction builds one Movement with one inbound line per layer/location.
- Domain `Correct` can accept arbitrary lines, but Application has no reliable journal→line quantity mapping without inventing a second identity system.
- JournalUpdate already fail-closes non-uniform priors via `TryResolvePriorSingleDirection`.
- P3-S4 already marks interpreter `Ambiguous` as inconsistent **before** `PersistIntents`.

### Chosen solution

**Option A — fail closed for unsafe aggregate void targets.**

Added `TryResolveSafeVoidTarget` in `LegacySyncDeltaInterpreter`: allow `Reverse` only when the prior Movement has **exactly one line** at the voided `LayananId`. Otherwise return `Ambiguous` (empty intents) so no quantity write is persisted.

### Why Option A

Smallest safe change that fits current identity anchors. Option B (location-scoped Correct) would require journal→line targeting that does not exist today and would expand beyond R-007’s local remediation scope.

### Files changed (R-007)

| Path | Change |
|---|---|
| `…/LegacySyncDeltaInterpreter.cs` | `TryResolveSafeVoidTarget` + guard in `InterpretJournalVoidDelete` |
| `…/LegacySyncDeltaInterpreterTest.cs` | Multi-line void → Ambiguous unit test |
| `…/SynchronizeStockLedgerScopeHandlerTest.cs` | Multi-location sibling qty integration test; strengthened 1:1 void happy path |
| `…/stock-ledger-phase3-review-backlog.md` | R-007 → Resolved (re-review APPROVED) |
| `…/stock-ledger-phase3-implementation-plan.md` | P3-S4 progress → COMPLETE |
| This summary | Remediation section + status |

### Tests added / strengthened

| Test | Asserts |
|---|---|
| `JournalVoidDelete_MultiLinePrior_ReturnsAmbiguous_R007` | Ambiguous; empty intents; no multi-line Reverse proposed |
| `JournalVoid_MultiLocationBaseline_FailsClosed_SiblingQuantityUnchanged_R007` | Inconsistent; LY02 Remaining Quantity unchanged; no Reversal persisted; position retained |
| `JournalVoid_RetainsHistory_AndCreatesReversal` | Requires Synchronized + exactly one Reversal + LY01 Remaining Quantity 0 + position advanced + original retained |
| `JournalVoidDelete_ProducesReversalIntent` | Unchanged safe 1:1 unit path |

### Test results

| Suite | Result |
|---|---|
| Solution build | **Succeeded** |
| Void / R-007 targeted tests | **7 passed**, 0 failed |
| `SynchronizeStockLedgerScopeHandlerTest` + `LegacySyncDeltaInterpreterTest` | **35 passed**, 0 failed |
| `StockLedgerFeature` (`xUnit.MaxParallelThreads=1`) | **216 passed**, 7 skipped, 0 failed |

No legacy `tb_stok` / `tb_buku` mutation asserted via `harness.LegacyWriter.Applied.Should().BeEmpty()` on void paths.

### Remaining limitations

Multi-line reconstruction voids intentionally fail closed rather than applying a precise partial compensation. That keeps Ledger history immutable and sibling quantities correct; operators / a future slice may add location-scoped Correct only when journal→line accountability exists. R-007 is review-verified Resolved; P3-S5 may proceed.
