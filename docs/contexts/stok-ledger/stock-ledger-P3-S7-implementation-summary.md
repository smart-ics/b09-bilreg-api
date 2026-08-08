# Stock Ledger Phase 3 / P3-S7 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-08  
**Slice:** P3-S7 — Coexistence sync harness (G-23 sync portion)  
**Plan:** [`stock-ledger-phase3-implementation-plan.md`](./stock-ledger-phase3-implementation-plan.md)

---

## Objective

Activate Phase-3-owned G-23 coexistence harness scenarios against discovery + catch-up + Freshness Gate on disposable fixtures, using in-process doubles only. Document which placeholders remain skipped and their later-phase ownership.

---

## Repository inspection summary

| Finding | Implication for P3-S7 |
|---|---|
| `StockLedgerCoexistenceHarnessPlaceholderTest` had 7 skipped + 1 active (depleted layer from P2-S8) | Extend in place; do not create parallel harness |
| P3-S4…S6 already prove sync, gate, retry, and concurrent catch-up in dedicated test classes | Harness tests compose the same stack end-to-end under G-23 markers |
| `LegacyStockLedgerSynchronizationRetryTest` explicitly deferred harness activation | P3-S7 owns activation only in the coexistence harness class |
| No P3-S7-targeted review backlog items | No Required/Recommended backlog actions for this slice |
| Full sync harness (`CreateHarness`, gate, cleanup) duplicated across P3-S4/S5/S6 tests | Coexistence harness gets its own `SyncHarness` factory matching established patterns |

---

## Review backlog items assigned to this slice

| Item | Target | Priority | Disposition |
|---|---|---|---|
| *(none)* | P3-S7 | — | No Required or Recommended backlog items target P3-S7 |
| R-001–R-007 | P3-S1…P3-S4 | Resolved | Unchanged; consumed via existing sync stack |

**Pre-implementation checklist (from backlog):** No Required items to resolve before coding.

---

## What was implemented

| Area | Deliverable |
|---|---|
| Test | `LegacyToNew_SynchronizesLegacyOriginatedChange` — reconstruct → bootstrap → legacy journal insert → Freshness Gate → `SynchronizedNow`; `LegacySynchronized` origin; fingerprint-v1 position; no legacy writes |
| Test | `DuplicateSyncBatch_IsIdempotent` — second catch-up on unchanged snapshot is `AlreadyCurrent` and quantity-neutral |
| Test | `RealMismatch_IsClassifiedAndSurfaced` — ledger tamper + legacy change → gate `Inconsistent`; position retained; live reconcile `MaterialInconsistency` |
| Test | `SyncNativeSerializationRace_InProcessDoubles_OneWinnerQuantityNeutral` — concurrent gate “native doubles” + sync racers; one winner; correct qty/position; FQ-06 not claimed |
| Test | `DepletedLayer_IntentionalLegacyDifference_IsReconciledAsRepresentational` — unchanged from P2-S8 (still green) |
| Docs in class | XML summary listing 4 remaining skipped scenarios with Phase 4/5/8 ownership |

**Explicitly not implemented:** New→Legacy; alternating writers; concurrent outbound; live legacy+Ledger partial-failure; FO Compatibility Writer; production coexistence claims; full G-23 matrix.

### G-23 harness activation matrix

| Scenario | Status | Owner |
|---|---|---|
| Legacy→New (discovery + catch-up + gate) | **Active** | P3-S7 |
| Duplicate sync batch idempotency | **Active** | P3-S7 |
| Real material mismatch surfaced | **Active** | P3-S7 |
| .NET sync/native-serialization race (in-process doubles) | **Active** | P3-S7 |
| Depleted-layer intentional difference | **Active** (P2-S8) | Reconstruction portion |
| New→Legacy | Skipped | Phase 4 |
| Alternating writers | Skipped | Phase 4+ |
| Concurrent outbound | Skipped | Phase 5 |
| Partial-failure live legacy+Ledger rollback | Skipped | Phase 4/8 |

---

## Repository decisions made

| Decision | Rationale |
|---|---|
| Extend `StockLedgerCoexistenceHarnessPlaceholderTest` in place | Plan names this class; avoids parallel harness |
| Add `[Collection("StockLedgerP3S4")]` | Serialize with other disposable-DB sync integration tests |
| Local `SyncHarness` factory (not shared test base class) | Matches P3-S4/S5/S6 pattern; no new abstraction layer |
| Real mismatch via SQL layer tamper + legacy change | Exercises live `StockReconciliationPort` without fake reconcile port |
| Sync/native race uses alternating gate doubles + direct sync callers | In-process doubles only; honors Freshness Gate serialization contract from P3-S6 |
| Comprehensive cleanup SQL (sync movements + idempotency) | Same as P3-S4/S6; supports multi-movement coexistence fixtures |

---

## Deviations from the implementation plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Four un-skipped placeholders | Three former placeholders activated + one new test for sync/native race (no prior placeholder method) | None — all four required scenarios covered |
| Illustrative “or successor” class name | Kept `StockLedgerCoexistenceHarnessPlaceholderTest` | Naming reflects historical scaffolding; class doc updated |

No architectural reopen. No production code changes.

---

## Review backlog resolution

No P3-S7-targeted backlog items. Prior Resolved items (R-001–R-007) remain unchanged.

---

## Test results

| Suite | Result |
|---|---|
| Solution build | **Succeeded** |
| `StockLedgerCoexistenceHarnessPlaceholderTest` | **5 passed**, 4 skipped, 0 failed |
| `StockLedgerFeature` (`xUnit.MaxParallelThreads=1`) | **237 passed**, 4 skipped, 0 failed |

Covered scenarios:

| Test | Asserts |
|---|---|
| `LegacyToNew_SynchronizesLegacyOriginatedChange` | Gate `SynchronizedNow`; `LegacySynchronized` movement; fingerprint-v1; no legacy writer calls |
| `DuplicateSyncBatch_IsIdempotent` | Second sync `AlreadyCurrent`; layers/qty/position unchanged |
| `RealMismatch_IsClassifiedAndSurfaced` | Gate `Inconsistent`; scope `Inconsistent`; position retained; `MaterialInconsistency` from live reconcile |
| `SyncNativeSerializationRace_InProcessDoubles_OneWinnerQuantityNeutral` | Concurrent gate/sync; ≥1 success; final `Current` + correct qty/position |
| `DepletedLayer_IntentionalLegacyDifference_IsReconciledAsRepresentational` | Depleted layer retained; representational difference (P2-S8 regression) |

---

## Known limitations

- Harness uses `FakeLegacyStockReadPort` only — no live `HOSPITAL_HPL` mixed-writer sessions (FQ-06 / production G-17 **not** claimed).
- Real mismatch test uses deliberate SQL tamper to simulate drift; production drift paths may differ in discovery timing.
- Sync/native race uses in-process Task parallelism, not multi-process or live VB6 sessions.
- Four G-23 scenarios remain skipped by design (Phase 4/5/8 ownership documented in test class XML).
- Parallel `devTest` runs can still deadlock (pre-existing); single-threaded filter is green.

---

## Acceptance checklist

| Criterion | Status |
|---|---|
| Legacy→New harness scenario passes | **PASS** |
| Duplicate sync batch harness scenario passes | **PASS** |
| Real mismatch harness scenario passes | **PASS** |
| .NET sync/native-serialization race (in-process doubles) | **PASS** |
| Depleted-layer intentional difference remains green | **PASS** |
| Phase 4/5/8 markers remain skipped with owners listed | **PASS** (4 skips, documented) |
| Does not claim full G-23 matrix or production coexistence | **PASS** |
| Does not disable VB6 conceptually (no legacy writes in sync path) | **PASS** |
| Fingerprint-v1 continuity in harness fixtures | **PASS** |
| Required backlog for this slice | **N/A** — none assigned |
| StockLedgerFeature regression green | **PASS** (237 passed, 4 skipped) |

---

## Files changed

| Path | Change |
|---|---|
| `src/bilreg/Bilreg.Test/.../StockLedgerCoexistenceHarnessPlaceholderTest.cs` | Activated 4 P3-S7 scenarios; added sync harness infrastructure; updated skip messages + class doc |
| `docs/contexts/stok-ledger/stock-ledger-phase3-implementation-plan.md` | Slice progress P3-S7 → COMPLETE |
| `docs/ARTIFACTS.md` | Registered this summary |
| This file | **Added** |

No Application / Infrastructure / SqlDb / Api production code changes.

---

## Next slice readiness (P3-S8)

Phase 3 exit hardening can proceed:

1. Initial G-24 explainability (position + last discovery/reconcile/Scope state).
2. Phase 3 implementation report + exit checklist.
3. Plan slice progress table final COMPLETE row (P3-S8).
4. Handoff notes for Phase 4 residuals (Compatibility Writer, New→Legacy harness, FQ-06).
