# Stock Ledger Phase 3 / P3-S2 — Implementation Summary

**Status:** COMPLETE (R-005 Resolved)  
**Date:** 2026-08-08  
**Slice:** P3-S2 — Sync delta interpretation (void/update → accountable intents)  
**Plan:** [`stock-ledger-phase3-implementation-plan.md`](./stock-ledger-phase3-implementation-plan.md)

---

## Objective

Produce a **deterministic, side-effect-free, SQL-free, persistence-free** Application interpreter that maps P3-S1 discovery deltas plus a caller-supplied in-memory Ledger snapshot into explicit accountable Stock Ledger intents (correction / reversal / LegacySynchronized establishment / layer remaining adjustment / representational omission / fail-closed ambiguity).

---

## Repository inspection summary

| Finding | Implication for P3-S2 |
|---|---|
| P3-S1 gate **PASS** with stable delta kinds | Safe to interpret `JournalInsert` / `JournalUpdate` / `JournalVoidDelete` / `BalanceDelete` / `BalanceUpdate` / `RequiresScopedReDerive` |
| No existing sync interpreter / intent model | New Application types required; do not invent Infrastructure |
| `StockMovementModel.Reverse` / `Correct` / `CreateReceipt` / `CreateOutbound` already exist | Reuse Domain factories; no Domain changes |
| `LegacyReconstructionBaselineCalculator` is the pure-calculator pattern | Mirror static calculator style for `LegacySyncDeltaInterpreter` |
| `LegacyChangeDiscoveryIdentityKeys` encodes SyncBatch keys | Reuse for `SyncIdempotencyKey` on intents (P3-S4 persistence contract) |
| Balance deltas from P3-S1 store `LegacyRowId` in `LegacyJournalId` field | Interpreter follows that DTO convention |

---

## Review backlog items assigned to this slice

| Item | Target | Priority | Disposition |
|---|---|---|---|
| R-005 | P3-S2 | Required | **Resolved** (compensatory JournalUpdate re-reviewed) |
| R-001 | P3-S4 | Required | Deferred |
| R-002 | P3-S4 | Required | Deferred |

---

## What was implemented

| Area | Deliverable |
|---|---|
| Application | `LegacySyncLedgerSnapshot` + `LegacySyncJournalAnchor` / `LegacySyncBalanceAnchor` — caller-supplied in-memory inputs |
| Application | `LegacySyncIntentKindEnum`, `LegacySyncIntentType`, `LegacySyncInterpretationOutcomeEnum`, `LegacySyncInterpretationResult` |
| Application | `LegacySyncDeltaInterpreter.Interpret` — pure mapping with fail-closed pre-checks, deterministic ordering, dedup |
| Tests | `LegacySyncDeltaInterpreterTest` — 15 pure unit tests |

**Explicitly not implemented (later slices):** reconciliation adapter (P3-S3), catch-up MediatR / TX / position advancement (P3-S4), Freshness Gate (P3-S5), Infrastructure classes, Scope transitions.

### Mapping rules delivered

| Delta | Intent |
|---|---|
| `JournalVoidDelete` | `ReversePriorMovement` via `movement.Reverse(..., LegacySynchronized)` |
| `JournalUpdate` | `CorrectPriorMovement` via `movement.Correct(..., LegacySynchronized)` with **compensatory delta lines** (R-005; not absolute restatement) |
| `JournalInsert` inbound | `ApplyLegacySynchronizedReceipt` |
| `JournalInsert` outbound | `ApplyLegacySynchronizedOutbound` |
| `BalanceUpdate` qty change | `AdjustLayerRemainingQuantity` (new movement; layer Origin preserved) |
| `BalanceDelete` depleted layer | `RepresentationalBalanceOmission` (BR-STL-110/080) |
| `BalanceDelete` active layer | `AdjustLayerRemainingQuantity` depleting remaining qty |
| `RequiresScopedReDerive` / `Undeterminable` | Fail-closed; **empty** intent list |

---

## Repository decisions made

| Decision | Rationale |
|---|---|
| Static Application interpreter (no interface) | Matches P3-S1 classifier / P2-S4 baseline calculator; avoids speculative DI |
| Snapshot is caller-owned | Keeps interpreter SQL-free; P3-S4 loads anchors from repos |
| Deterministic movement IDs via `LegacyReconstructionBaselineCalculator.DeterministicAccountableId` | Fits VARCHAR(26) and satisfies same-input ⇒ same-intent |
| Quantity direction from `QuantityIn` / `QuantityOut` only | Avoids inventing G-28 jenis vocabulary; ambiguous both/neither ⇒ Ambiguous |
| BalanceUpdate with zero remaining delta ⇒ Ambiguous | Non-quantity material drift is not force-balanced (P3-S3 / re-derive) |
| Void idempotency key synthetic (`SYNC|BUKU|…|VOID|…`) | Voided journals absent from current `tb_buku`; cannot call `BuildJournalKey` on missing row |

---

## Deviations from the implementation plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Illustrative class names left to agent | Used `LegacySyncDeltaInterpreter` / `LegacySyncIntentType` / `LegacySyncLedgerSnapshot` | None — clear and consistent with P3-S1 naming |
| Optional Domain factory extension | None needed | Domain remains unchanged |

No architectural reopen. Locked decisions preserved.

---

## Review backlog resolution

| Item | Status |
|---|---|
| R-005 (P3-S2) | **Resolved** |
| R-001 (P3-S4) | **Not resolved** — correctly deferred; first sync must establish identity-key set |
| R-002 (P3-S4) | **Not resolved** — correctly deferred; idempotency key width before catch-up writes |

---

## Remediation — R-005 (2026-08-08)

### Problem

`InterpretJournalUpdate` passed absolute current-journal lines into Domain `StockMovementModel.Correct`. Because `Correct` records those lines as a **new** correction consequence, absolute restatement would double-apply quantity on catch-up persist.

### Fix

| Area | Change |
|---|---|
| `LegacySyncDeltaInterpreter.InterpretJournalUpdate` | Build inventory-signed **compensatory** Correct lines (new qty − previously represented qty) |
| Fail-closed | Direction / Receipt Source / Stock Location / unit-valuation (and non-uniform prior lines) ⇒ `Ambiguous` |
| Quantity-neutral | Same material qty + preserved attributes ⇒ no quantity consequence (skip intent) |
| History | Prior movement never edited/deleted |

### Tests added/changed

- OUT 10 → OUT 7 ⇒ compensating Inbound 3
- IN 100 → IN 90 ⇒ compensating Outbound 10
- Quantity increase ⇒ same-direction compensatory delta
- No quantity difference ⇒ empty intents (no duplicate quantity consequence)
- Unit-cost / direction change ⇒ Ambiguous
- Existing void/reversal + determinism / history-retention tests remain green

### Repository decision

Compensatory delta is computed as inventory-signed net (`inbound increase` / `outbound decrease` → Inbound compensation). Absolute journal restatement is forbidden for JournalUpdate. Non-quantity material changes fail closed rather than inventing provenance rewrite.

### Test results (post-remediation)

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~LegacySyncDeltaInterpreter` | **20 passed**, 0 failed |
| `StockLedgerFeature` (single-threaded) | **197 passed**, 7 skipped, 0 failed |
| Solution build | Succeeded |

---

## Test results (original slice delivery)

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~LegacySyncDeltaInterpreter` | **15 passed**, 0 failed (pre-R-005) |
| Pure / non-integration StockLedgerFeature filter | **125 passed**, 7 skipped, 0 failed |
| Solution build (`Bilreg.Application` / `Bilreg.Test`) | Succeeded |
| Full `StockLedgerFeature` filter (incl. live DB fixtures) | Pre-existing `devTest` deadlocks under parallel/shared DB load (same note as P3-S1); **not caused by P3-S2** (pure tests only) |

Covered scenarios: unchanged empty intents; `RequiresScopedReDerive` fail-closed; Undeterminable → Ambiguous; void → reversal; void without anchor → Ambiguous; update → compensatory correction (R-005); inbound/outbound insert; repost void-then-insert order; duplicate delta dedup; balance decrease preserves layer Origin; depleted BalanceDelete omission; active BalanceDelete depletion; determinism; history retention.

---

## Known limitations

1. Interpreter requires journal anchors with rehydrated (or snapshot-listed) `StockMovementModel` for Reverse/Correct — P3-S4 must load movements when building the snapshot.
2. Transfer / dual-direction journals fail closed as Ambiguous (no jenis vocabulary mapping).
3. BalanceUpdate that changes only non-quantity fingerprint material (cost/ED/batch) fails closed as Ambiguous — not force-balanced.
4. No production DI / HTTP (Phase 3 plan).

---

## Acceptance checklist

| Criterion | Status |
|---|---|
| Interpreter reviewable and independently unit-testable | **PASS** |
| Completely independent from transaction handling | **PASS** |
| Domain remains SQL-free | **PASS** |
| No silent force-balance | **PASS** |
| void-delete → accountable reversal; no history erase | **PASS** |
| duplicate delta identity → single intent | **PASS** |
| quantity-only change preserves establishment origin | **PASS** |
| material ambiguity → explicit fail-closed outcome | **PASS** |
| No Infrastructure / catch-up / Freshness / reconcile | **PASS** |

---

## Files changed

| Path | Change |
|---|---|
| `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacySyncLedgerSnapshot.cs` | Added |
| `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacySyncIntentType.cs` | Added |
| `src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacySyncDeltaInterpreter.cs` | Added; R-005 compensatory JournalUpdate |
| `src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/LegacySyncDeltaInterpreterTest.cs` | Added; R-005 compensatory / Ambiguous cases |
| `docs/contexts/stok-ledger/stock-ledger-phase3-implementation-plan.md` | Slice progress |
| `docs/ARTIFACTS.md` | Registry entry |
| `docs/contexts/stok-ledger/stock-ledger-P3-S2-implementation-summary.md` | Added |

---

## Next slice readiness

**Ready for P3-S4** (R-005 Resolved). P3-S4 catch-up will compose discovery → `LegacySyncDeltaInterpreter.Interpret` → short TX persist using intent `SyncIdempotencyKey` / `ProposedMovement`, and must still resolve R-001 / R-002 before relying on set-diff post-reconstruction.
