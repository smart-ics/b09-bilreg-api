# Stock Ledger Phase 3 / P3-S6 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-08  
**Slice:** P3-S6 — Sync retry, crash safety, duplicate protection, .NET serialization  
**Plan:** [`stock-ledger-phase3-implementation-plan.md`](./stock-ledger-phase3-implementation-plan.md)

---

## Objective

Harden **synchronization itself** for:

- Bounded, sync-specific retry on claim / OCC conflicts
- Crash-before-commit recovery (prior Synchronization Position retained until successful finalize)
- Duplicate concurrent catch-up → one winner, quantity-neutral
- .NET-side sync serialization via Scope claim (not live VB6)

Without building Phase-4 native-write infrastructure or introducing generic retry frameworks.

---

## Repository inspection summary

| Finding | Implication for P3-S6 |
|---|---|
| P3-S4 `SynchronizeStockLedgerScopeHandler` already had minimum claim + `ClaimConflict` + SyncBatch idempotency | Extend; do not rebuild |
| Unconditional pass-through when Scope was already `SynchronizationRequired` | Concurrent dual-apply risk; replaced with claim/resume semantics |
| P3-S5 Freshness Gate mapped `ClaimConflict` → `StaleOrNotCurrent` with no in-call retry | Gate still invokes catch-up once; retries are **internal** to the handler |
| `ReconstructionClaimService` + `MaxBasisChangeRetries = 3` | Pattern to mirror for sync-only claim + bounded retry |
| No P3-S6-targeted review backlog items | No Required/Recommended backlog actions for this slice |

---

## Review backlog items assigned to this slice

| Item | Target | Priority | Disposition |
|---|---|---|---|
| *(none)* | P3-S6 | — | No Required or Recommended backlog items target P3-S6 |
| R-001–R-007 | P3-S1…P3-S4 | Resolved | Unchanged; consumed via existing sync stack |

---

## What was implemented

| Area | Deliverable |
|---|---|
| Application | `SynchronizationClaimService` — claim from `Current`/`LegacyChangePending`; `AlreadyClaimed` when `SynchronizationRequired` unless `allowResume` |
| Application | `SynchronizeStockLedgerScopeHandler` — `MaxSyncConflictRetries = 3`; `ExecuteOnce` reloads Scope + rediscovers each attempt; attempt 1 strict claim, attempt 2+ crash-resume |
| Application | Freshness Gate explanation updated for exhausted bounded retries (still one logical catch-up per gate call) |
| Tests | `SynchronizationClaimServiceTest` (4) + `LegacyStockLedgerSynchronizationRetryTest` (6) |

**Explicitly not implemented:** Polly/middleware retry frameworks; FO/native consequence UoW; Compatibility Writer; production DI; P3-S7 harness activation; live VB6 FQ-06 proof.

### Retry / claim flow

```text
Handle (outer request boundary)
  for attempt = 1..MaxSyncConflictRetries:
    Reload Scope + DiscoverChanges (fingerprint-v1) + legacy snapshot
    Unchanged + coverage complete → AlreadyCurrent
    Claim(allowResume: attempt > 1)
         AlreadyClaimed → ClaimConflict (retry if attempts remain)
         Claimed → ApplyCatchUp → Reconcile → CompleteSynchronization
    CONCURRENCY_CONFLICT → ClaimConflict (retry if attempts remain)
  Exhausted → ClaimConflict fail closed
```

Crash recovery: stuck `SynchronizationRequired` fails attempt 1 (no resume), succeeds on attempt 2+ with resume + SyncBatch idempotency.

---

## Repository decisions made

| Decision | Rationale |
|---|---|
| Inline retry loop in handler (no separate Polly-style helper type) | Plan allows trivial loop; keeps sync-specific and reviewable |
| `SynchronizationClaimService` mirrors reconstruction claim | Clear winner/loser; resume flag separates concurrent vs crash-recovery |
| Attempt 1 never resumes `SynchronizationRequired` | Strengthens .NET-side serialization for concurrent callers |
| Attempt 2+ allows resume | Crash recovery within the same request boundary without new DB columns |
| Freshness Gate unchanged structurally | Still one `_synchronize` call; internal retries satisfy P3-S6 without nested catch-up |
| No new Scope columns / claim tokens | Prefer existing conditional sync-state update + SyncBatch + Position OCC |

---

## Deviations from the implementation plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Optional `LegacyStockLedgerSynchronizationRetry` helper class | Retry loop lives in `SynchronizeStockLedgerScopeHandler.Handle` with `MaxSyncConflictRetries` | None — same behavior, fewer types |
| Illustrative class names | `SynchronizationClaimService` | Matches reconstruction naming |

No architectural reopen. No Phase 4+ scope. No generic retry framework.

---

## Review backlog resolution

No P3-S6-targeted backlog items. Prior Resolved items (R-001–R-007) remain unchanged.

---

## Phase 4+ caller contract (document only — not implemented)

Native consequence UoW / FO writers (Phase 4+) **must**:

1. **Not allocate** from Stock Ledger layers until `LegacyStockFreshnessGate` returns `Current` or `SynchronizedNow` (`IsSafeToTrustLedgerLayers == true`).
2. **Serialize** native write boundaries with synchronization claim / Scope synchronization state (do not allocate while Scope is `SynchronizationRequired` / mid-sync).
3. Honor `StaleOrNotCurrent` / `Inconsistent` as **fail-closed** for Ledger-dependent decisions.
4. Never treat provisional G-08 Availability Discovery as Ledger authority.

**Explicit non-claim:** FQ-06 / production G-17 (live VB6/.NET mixed-writer proof) is **not** done. That remains Phase 9.

---

## Test results

| Suite | Result |
|---|---|
| Solution build | **Succeeded** |
| `SynchronizationClaimServiceTest` | **4 passed** |
| `LegacyStockLedgerSynchronizationRetryTest` | **6 passed** |
| `StockLedgerFeature` (`xUnit.MaxParallelThreads=1`) | **233 passed**, 7 skipped, 0 failed |

Covered:

| Test | Asserts |
|---|---|
| `Claim_FromCurrent_TransitionsToSynchronizationRequired` | Conditional claim |
| `Claim_WhenAlreadySynchronizationRequired_WithoutResume_ReturnsAlreadyClaimed` | Concurrent serialization |
| `Claim_WhenAlreadySynchronizationRequired_WithResume_ReturnsClaimed` | Crash-resume affordance |
| `Claim_ConcurrentFromCurrent_SingleSynchronizationRequiredWinner` | 8 racers → 1 winner |
| `CrashResume_StuckSynchronizationRequired_CompletesIdempotently` | Rediscover ≥ 2; finalize; fingerprint-v1 |
| `ConcurrentCatchUp_FromCurrent_OneWinner_QuantityNeutral` | Parallel sync; qty + position correct |
| `DuplicateConcurrentCatchUp_DoesNotDoubleApplyQuantity` | No double quantity |
| `FreshnessGate_ExhaustedClaimConflict_ReturnsStaleOrNotCurrent` | At-most-once gate sync call |
| `FreshnessGate_InternalRetryClearsTransientConflict_ReturnsSynchronizedNow` | Internal resume clears stuck claim |
| `ConcurrentCatchUp_DepletedIntentionalDifference_RemainsBalanced` | Depleted layer retained |

---

## Known limitations

- Concurrent losers that retry with `allowResume` while a winner is still mid-apply may enter apply alongside the winner; SyncBatch idempotency + `CompleteSynchronization` conditional update keep quantity/position correct. Stronger claim tokens would need durable columns (deferred).
- No true SQL TX-abort mid-UoW simulator beyond stuck-`SynchronizationRequired` resume fixture.
- Parallel `devTest` runs can still deadlock (pre-existing); StockLedgerFeature filter is green single-threaded.
- FQ-06 / production G-17 **not** claimed.
- P3-S7 coexistence harness placeholders remain skipped by design.

---

## Acceptance checklist

| Criterion | Status |
|---|---|
| Bounded sync-specific retry | **PASS** — `MaxSyncConflictRetries = 3` |
| Crash-before-commit retains prior position until finalize | **PASS** — resume test |
| Duplicate concurrent catch-up → one winner, quantity-neutral | **PASS** |
| OCC/version conflict → bounded retry or explicit fail | **PASS** — `CONCURRENCY_CONFLICT` → `ClaimConflict` with retry |
| .NET-side sync serialization | **PASS** — claim service + concurrent tests |
| Re-discover on each retry attempt | **PASS** — counting discovery spy |
| No generic retry framework | **PASS** |
| FQ-06 / G-17 **not** claimed | **PASS** — documented |
| Required backlog for this slice | **N/A** — none assigned |
| StockLedgerFeature regression green | **PASS** (233 passed, 7 skipped) |

---

## Files changed

| Path | Change |
|---|---|
| `src/bilreg/Bilreg.Application/.../SynchronizationClaimService.cs` | **Added** — sync claim / resume |
| `src/bilreg/Bilreg.Application/.../UseCases/SynchronizeStockLedgerScopeCommand.cs` | Bounded retry + `ExecuteOnce`; use claim service |
| `src/bilreg/Bilreg.Application/.../LegacyStockFreshnessGate.cs` | Exhausted-retry explanation; doc note |
| `src/bilreg/Bilreg.Test/.../SynchronizationClaimServiceTest.cs` | **Added** |
| `src/bilreg/Bilreg.Test/.../LegacyStockLedgerSynchronizationRetryTest.cs` | **Added** |
| `src/bilreg/Bilreg.Test/.../SynchronizeStockLedgerScopeHandlerTest.cs` | Wire claim service in harness |
| `src/bilreg/Bilreg.Test/.../LegacyStockFreshnessGateTest.cs` | Wire claim service in harness |
| `docs/contexts/stok-ledger/stock-ledger-phase3-implementation-plan.md` | Slice progress P3-S6 → COMPLETE |
| `docs/ARTIFACTS.md` | Registered this summary |
| This file | **Added** |

No Infrastructure / SqlDb / Api schema changes.

---

## Next slice readiness (P3-S7)

Sync hardening is ready for coexistence harness activation:

1. Un-skip / implement Phase-3-owned G-23 scenarios: Legacy→New, duplicate sync batch, real mismatch, .NET sync serialization race (in-process doubles).
2. Leave Phase 4/5/8 harness markers skipped with owners listed.
3. Do not claim full G-23 matrix or production coexistence.
