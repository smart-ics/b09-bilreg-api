# Stock Ledger Phase 3 / P3-S5 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-08  
**Slice:** P3-S5 — Legacy Freshness Gate (G-12)  
**Plan:** [`stock-ledger-phase3-implementation-plan.md`](./stock-ledger-phase3-implementation-plan.md)

---

## Objective

Before Stock Ledger layers are trusted for a subsequent stock decision (BR-STL-079 / G-12), prove freshness against Legacy Stock Authority:

- Unchanged reconstructed scope → pass (`Current`)
- Pending legacy changes → invoke P3-S4 catch-up **at most once** per gate call; pass only on success (`SynchronizedNow`)
- Undeterminable / failed sync → fail closed (`StaleOrNotCurrent` / `Inconsistent`)
- Never block VB6; never reject legacy writes because origin is Native/Reconstructed (replaces rejected Authority Gate)

---

## Repository inspection summary

| Finding | Implication for P3-S5 |
|---|---|
| P3-S4 `SynchronizeStockLedgerScopeHandler` complete (R-007 APPROVED) | Gate delegates catch-up; do not re-implement sync |
| Live `ILegacyChangeDiscoveryPort` + `fingerprint-v1` | Gate uses same discovery path for fast-path / explainability |
| `LegacySyncIdentityBootstrapper.HasCompleteCoverage` | Mirror sync fast-path (Unchanged + coverage) |
| `AvailabilityDiscoveryOutcomeEnum.StaleOrNotCurrent` reserved | Map gate not-current for future callers; **do not** change G-08 adapter |
| No Freshness Gate class existed | New Application helper (not MediatR / not HTTP) |
| P3-S4 harness + disposable DB collection | Reuse for integration tests |

---

## Review backlog items assigned to this slice

| Item | Target | Priority | Disposition |
|---|---|---|---|
| *(none)* | P3-S5 | — | No Required or Recommended backlog items target P3-S5 |
| R-001–R-007 | P3-S1…P3-S4 | — | Already **Resolved**; consumed via existing sync/discovery stack |

---

## What was implemented

| Area | Deliverable |
|---|---|
| Application | `LegacyStockFreshnessGate` — thin helper: discover → fast-path or single catch-up → explicit outcomes |
| Application | `LegacyStockFreshnessGateOutcomeEnum` — `Current`, `SynchronizedNow`, `StaleOrNotCurrent`, `Inconsistent` |
| Application | `LegacyStockFreshnessGateResult` — outcome + scope + explanation + position + `IsSafeToTrustLedgerLayers` |
| Tests | `LegacyStockFreshnessGateTest` — 7 tests (integration + unit fakes) |

**Explicitly not implemented (later slices):** P3-S6 retry/OCC/duplicate-sync productization; P3-S7 coexistence harness activation; G-08 adapter changes; production DI/HTTP; FIFO/FO/native writers; nested catch-up loops.

### Gate flow delivered

```text
Load Scope + preconditions (Reconstructed, fingerprint-v1 position, not already Inconsistent)
  → DiscoverChanges (outside TX; same fingerprint-v1 path)
  → Undeterminable ⇒ StaleOrNotCurrent (no sync)
  → Unchanged + identity coverage complete ⇒ Current (no sync)
  → Else invoke SynchronizeStockLedgerScopeCommand ONCE
       AlreadyCurrent     → Current
       Synchronized       → SynchronizedNow
       Inconsistent       → Inconsistent
       RequiresScopedReDerive / ClaimConflict → StaleOrNotCurrent
```

`IsSafeToTrustLedgerLayers` is true only for `Current` / `SynchronizedNow`.

---

## Repository decisions made

| Decision | Rationale |
|---|---|
| Plain Application service (not MediatR command) | Gate is a pre-decision helper, not a public endpoint (matches `ReconstructionClaimService` style) |
| Optional sync `Func<>` constructor overload | Allows at-most-once assertion in tests without a production interface |
| Gate-owned discovery before catch-up | Fast-path + explainability; sync may re-discover (acceptable; no nested loop) |
| `ClaimConflict` → `StaleOrNotCurrent` | Fail closed until P3-S6 bounded retry; caller retries on a later gate call |
| Do not mutate `AvailabilityDiscoveryPort` | Document caller composition only; G-08 stays provisional legacy-read |
| Optional `decisionContext` appends to Explanation only | Plan allows context without behavioral branching |

---

## Deviations from the implementation plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Illustrative class names | `LegacyStockFreshnessGate` | None — plan left naming to implementer |
| Inject concrete sync handler only | Primary ctor takes `SynchronizeStockLedgerScopeHandler`; secondary takes `Func<>` for tests | No production interface; same observable behavior |

No architectural reopen. No P3-S6+ scope. No Authority Gate semantics. No legacy `tb_stok` / `tb_buku` writes.

---

## Review backlog resolution

No P3-S5-targeted backlog items. Prior Resolved items (R-001–R-007) remain unchanged and are exercised indirectly when the gate invokes catch-up.

---

## Test results

| Suite | Result |
|---|---|
| Solution build | **Succeeded** |
| `LegacyStockFreshnessGateTest` | **7 passed**, 0 failed |
| `StockLedgerFeature` (`xUnit.MaxParallelThreads=1`) | **223 passed**, 7 skipped, 0 failed |

Covered:

| Test | Asserts |
|---|---|
| `UnchangedScope_AfterBootstrap_ReturnsCurrent_WithoutSecondSync` | Fast-path `Current`; sync not called; no legacy writes |
| `LegacyChange_TriggersSingleCatchUp_ReturnsSynchronizedNow` | Exactly one sync; position advanced; `SynchronizedNow` |
| `AfterSuccessfulSync_SecondGateCall_ReturnsCurrent` | Second call fast-path; total sync count remains 1 |
| `UndeterminableDiscovery_ReturnsStaleOrNotCurrent_WithoutSync` | Fail closed; sync not invoked |
| `SyncInconsistent_ReturnsInconsistent` | Material mismatch path; prior position retained |
| `PreSyncInconsistentScope_ReturnsInconsistent_WithoutCatchUp` | Immediate fail; sync not invoked |
| `Gate_DoesNotMutateLegacyAuthority` | Compatibility writer remains empty |

---

## Known limitations

- Concurrent sync `ClaimConflict` fails closed as `StaleOrNotCurrent` without bounded retry — intentional interim; **P3-S6** owns sync-specific retry/serialization.
- Gate does not register production DI or HTTP endpoints (Phase 3 plan).
- G-08 Availability Discovery still never returns `StaleOrNotCurrent` itself — future allocation callers must compose Freshness Gate first.
- Multi-line aggregate void fail-closed (R-007 residual) surfaces through catch-up as `Inconsistent` / not-current when the gate triggers sync.

---

## Acceptance checklist

| Criterion | Status |
|---|---|
| G-12: detect post-baseline legacy change | **PASS** — discovery + catch-up path |
| G-12: synchronize or fail closed | **PASS** — `SynchronizedNow` / `StaleOrNotCurrent` / `Inconsistent` |
| Unchanged scope proceeds | **PASS** — `Current` without sync |
| Undeterminable ⇒ explicit not-current | **PASS** — `StaleOrNotCurrent` |
| Never acts as Authority Gate / never blocks VB6 | **PASS** — no legacy write path; no origin-based rejection |
| Catch-up at most once per gate call | **PASS** — counted in tests |
| No nested discovery→catch-up loop | **PASS** |
| Freshness explainable from position + last outcome | **PASS** — result carries position, scope state, explanation |
| No P3-S6+ / Phase 4+ scope | **PASS** |
| Required backlog for this slice | **N/A** — none assigned |

---

## Files changed

| Path | Change |
|---|---|
| `src/bilreg/Bilreg.Application/.../LegacyStockFreshnessGate.cs` | **Added** — gate service, outcome enum, result |
| `src/bilreg/Bilreg.Test/.../LegacyStockFreshnessGateTest.cs` | **Added** — integration + unit tests |
| `docs/contexts/stok-ledger/stock-ledger-phase3-implementation-plan.md` | Slice progress P3-S5 → COMPLETE |
| `docs/ARTIFACTS.md` | Registered this summary |
| This file | **Added** |

No Infrastructure / SqlDb / Api changes.

---

## Next slice readiness (P3-S6)

Freshness Gate is callable via `LegacyStockFreshnessGate.EnsureFreshAsync`. P3-S6 should:

1. Add **sync-specific** bounded retry around catch-up (OCC / deadlock / `ClaimConflict`) — not a generic retry framework.
2. Prove crash-before-commit retains prior Synchronization Position; duplicate concurrent catch-up remains quantity-neutral.
3. Harden .NET-side sync serialization with Scope version / claim (not live VB6 FQ-06).
4. Treat Freshness Gate as the caller contract: native consequence UoW (Phase 4+) **must not allocate from layers until Freshness Gate passes** and must serialize with sync claim / Scope version — document only in P3-S6; do not implement native UoW here.

**Phase 4+ caller contract (document only):**

> Must not allocate from Stock Ledger layers until `LegacyStockFreshnessGate` returns `Current` or `SynchronizedNow` (`IsSafeToTrustLedgerLayers == true`). Must serialize native write boundaries with synchronization claim / Scope version. Honor `StaleOrNotCurrent` / `Inconsistent` as fail-closed for Ledger-dependent decisions. Never treat provisional G-08 Availability Discovery as Ledger authority.
