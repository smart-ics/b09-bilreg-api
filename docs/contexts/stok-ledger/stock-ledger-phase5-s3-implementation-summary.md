# Stock Ledger Phase 5 / P5-S3 — Implementation Summary

**Status:** OCC remediation complete — gate re-review **NO-GO** (LegacyRowId exact-qty); do not start P5-S4 until review is PASS / PASS WITH MINOR FINDINGS  
**Date:** 2026-08-08 (OCC remediation); gate re-review 2026-08-09  
**Slice:** P5-S3 — Native Stock Transfer UseCase + capability flag  
**Plan:** [`stock-ledger-phase5-implementation-plan.md`](./stock-ledger-phase5-implementation-plan.md)  
**Gate review:** [`stock-ledger-phase5-s3-review.md`](./stock-ledger-phase5-s3-review.md) — **NO-GO** (LegacyRowId exact-qty wrong-row); prior OCC increase NO-GO cleared

---

## Objective

Deliver a thin MediatR/Inventory-style UseCase that posts one authorized Stock Transfer as a Native conserved Transfer movement, updates source/destination Positions/Layers, refreshes coexistence Synchronization Position(s) with `fingerprint-v1`, and commits paired MT OUT/IN legacy consequences through the existing UoW — behind a disabled-by-default capability flag independent from DO Receipt.

---

## Final SourceConsequence contract (normative)

| Concern | Contract |
|---|---|
| Identity key | `MT\|{SourceTransactionId}` (`fs_kd_mutasi` / whole mutasi) |
| Grain | **Whole mutasi** — independent of line order, BrgId set, or line count |
| Supported surface | Multi-line / multi-`BrgId` into **one** `CreateTransfer` movement (one source + one destination location) |
| Exact duplicate | `AlreadyCommitted` — quantity-neutral |
| Reordered lines (same MT id) | `AlreadyCommitted` — quantity-neutral |
| Partial retry / subset of lines (same MT id) | `AlreadyCommitted` — quantity-neutral; does **not** open a second consequence |
| Blank MT id | Fail closed (`ArgumentException`) |
| Ambiguous LegacyRowId match | `Inconsistent` (fail closed) |
| No matching LegacyRowId / insufficient qty at mapper | `InsufficientStock` (fail closed) |

Callers must post the **complete** authorized mutasi in one request. An incomplete first commit under the same MT id permanently owns that SourceConsequence responsibility.

**P5-S4 handoff (idempotency):** Transfer void / original-consequence lookup **must** use the same whole-mutasi key `MT|{SourceTransactionId}`. One void SourceConsequence responsibility per MT document (not per Item / not first-line BrgId).

**P5-S4 handoff (Position OCC):** Both source consume and destination establish/increase already use **one** `Version + 1` per write-scope per consequence commit (merged layers then `StockPositionModel.Create(..., baseVersion + 1)`). Void/reverse helpers **must** follow the same single-bump discipline — do not loop `AddLayer` / per-layer rebuilds that overshoot `stored.Version + 1`. Source and destination Positions are both mutable under transfer; reversal must update both sides with that contract.

---

## Destination OCC remediation (2026-08-08)

### Root cause

`StockPositionRepo.SaveChanges` requires exactly `stored.Version + 1` on update. `ApplySourceConsumption` already did one bump after all layer consumes. `BuildDestinationPositions` looped `AddLayer` (each bump), so existing destination + N>1 inbound layers became `stored + N` → concurrency throw. Empty destinations used Insert (any Version OK), so establish-only tests hid the defect.

### Chosen remediation

Rewrite `BuildDestinationPositions` to mirror source: merge existing layers + new inbound layers, then `StockPositionModel.Create(writeScope, merged, baseVersion + 1)`. No Domain/repo redesign; OCC fail-closed semantics unchanged.

### Related low-risk hygiene in this slice

- Test seeds auto-pre-assign BK/ST via Ulid random tail (FQ-02 compact-id residue).
- `TransferLegacyCompatibilityMapper` MT OUT/IN compact ids use Ulid random tail instead of `NunaId.NewLegacyCompact` (same collision class under rapid multi-leg generation).

---

## Implemented changes

| Area | Deliverable |
|---|---|
| Application options | `StockLedgerStockTransferOptions` (`SECTION_NAME = "StockLedgerStockTransfer"`, `Enabled` default **false**) |
| Idempotency | `TransferConsequenceIdempotency.BuildSourceConsequenceKey(sourceTransactionId)` → `MT\|{mutasiId}` |
| Legacy mapper | `TransferLegacyCompatibilityMapper` — allocation-explicit LegacyRowId resolve; typed `TransferLegacyRowResolutionException` (Ambiguous vs NoMatch); OUT/IN pairs; Ulid-tail compact BK/ST; post-transfer fingerprint projection |
| UoW | `StockConsequenceDraft.AdditionalScopeStates` + persist loop for multi-DO Scope refresh in one TX |
| UseCase | `PostStockTransferStockConsequenceCommand` / `Handler` — trusted allocation → `CreateTransfer` → UoW → bootstrap + live fingerprint reconcile; source **and** destination single OCC bump |
| Tests | `PostStockTransferStockConsequenceHandlerTest` (12 scenarios on disposable `devTest`, including increase OCC + multi-item reorder/partial-retry) |
| Docs | This summary + plan Slice Progress + ARTIFACTS |

**Explicitly not implemented:** transfer void (P5-S4); lock order / revalidate / ConcurrentOutbound (P5-S5); coexistence proof (P5-S6); production DI/HTTP; enabling flag in appsettings.

---

## Architectural decisions

| Decision | Rationale |
|---|---|
| MediatR handler mirrors `PostDoReceiptStockConsequenceHandler` | Inventory UseCase convention; orchestrator stays a plain service |
| Separate `StockLedgerStockTransferOptions` | Plan §9 independence from `StockLedgerDoReceipt` |
| Mutasi-scoped key `MT\|{SourceTransactionId}` (no BrgId) | MT responsibility is `fs_kd_mutasi`; multi-line surface already allowed; DO-style `…\|{BrgId}\|…` is unsafe when first-line BrgId can change |
| Call P5-S1 orchestrator per line; no second Freshness Gate | Freshness already proven inside trusted allocation |
| Resolve `LegacyRowId` in Application mapper from live balances | Ledger `StockLayerId` ≠ `fs_kd_trs`; fail closed on ambiguous match |
| Ambiguous mapper match → `Inconsistent` | Ambiguity is identity inconsistency, not insufficient quantity |
| Preserve destination layer Receipt Source + Effective Receipt Time + Batch from source layer | No new Receipt Source; conservation of provenance |
| One OCC version bump per **source** write-scope after all layer consumes | Multi-layer same position must not overshoot `stored.Version + 1` |
| One OCC version bump per **destination** write-scope after merging all inbound layers | Same OCC contract for establish **and** increase; mirrors source |
| `AdditionalScopeStates` on draft | Multi-DO transfer refreshes each Item+DO Scope in the same short TX |
| Pre-commit fingerprint projection + post-commit live reconcile | Same pattern as P4 void; keeps Scope refresh inside UoW |
| Discovery/freshness/reconstruction stay outside consequence TX | Plan §8 boundary |
| Ulid-tail compact BK/ST for MT mapper legs | Avoid `NewLegacyCompact` timestamp collisions under multi-leg generation |

---

## Deviations from plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Summary filename `stock-ledger-P5-S3-implementation-summary.md` (§12) | `stock-ledger-phase5-s3-implementation-summary.md` (task request; matches S1/S2) | Naming only; ARTIFACTS links the chosen path |
| Optional Domain transfer helper | None needed — Application composition of existing Domain factories sufficient | Cleaner reuse |
| Initial slice used `MT\|{firstBrgId}\|{mutasiId}` | Remediated to `MT\|{mutasiId}` after NO-GO review | Contract now matches multi-line surface |
| Destination build used `AddLayer` loop | Remediated to single-bump `Create` after merge | Establish + increase both satisfy Position OCC |

No locked domain/ADR decisions were reopened.

---

## Tests added/updated

Focused run (disposable `devTest`), twice:

```text
dotnet test Bilreg.Test/Bilreg.Test.csproj --filter FullyQualifiedName~PostStockTransferStockConsequenceHandlerTest
Passed!  - Failed: 0, Passed: 12, Skipped: 0
```

| Test | Result |
|---|---|
| `HappyPath_CapabilityEnabled_NativeOriginAndConservedLegacyOutIn` | PASS — Native origin; conserved Ledger + legacy OUT/IN; `fingerprint-v1`; destination ERT preserved; key `MT\|{mtId}` |
| `CapabilityDisabled_ReturnsWithoutWrites` | PASS |
| `DuplicateSourceResponsibility_IsQuantityNeutral` | PASS — `AlreadyCommitted` |
| `MultiItem_ReorderedLines_SameMutasi_IsQuantityNeutral` | PASS — NO-GO scenario; reorder does not open second consequence |
| `MultiItem_PartialRetry_SameMutasi_IsQuantityNeutral` | PASS — NO-GO scenario; subset retry stays quantity-neutral |
| `MultiLayerSingleDo_TransferConserved` | PASS — establish path; multi OUT/IN pairs |
| `ExistingDestination_MultiLayerIncrease_PreservesOcc` | PASS — **OCC NO-GO scenario**; pre-existing dest + ≥2 inbound layers; `Version == prior + 1` |
| `MultiDoLine_TransferConserved` | PASS — both scopes refreshed Current |
| `ExplicitExpirationDateFilter_Path` | PASS — ED-constrained destination only |
| `InsufficientStock_FailClosed` | PASS — no partial MT journals |
| `StaleScope_FailClosed` | PASS — SynchronizationRequired → fail closed |
| `NoIsAuthoritative_OnScopeOrMovement` | PASS |

---

## Limitations intentionally deferred

- Transfer void / unsafe reverse (`MT_IN_V` / `MT_OUT_V`) — P5-S4
- Lock order, quantity revalidation inside consequence TX, ConcurrentOutbound — P5-S5
- Native transfer → VB6-shaped change → Phase 3 sync coexistence proof — P5-S6
- Full multi-position failure-injection matrix (happy-path atomicity only here)
- Production enablement / public HTTP / DI registration
- Payload content comparison under a reused MT id (duplicate identity wins; no deep equality)

---

## Known risks / residuals

| Risk | Mitigation in P5-S3 | Residual |
|---|---|---|
| Ambiguous `LegacyRowId` match | Fail closed as `Inconsistent` | Concurrent writers may still race until P5-S5 |
| Creating new Receipt Source at destination | Destination layers copy source Receipt Source + valuation + ERT | Reviewer check |
| Partial OUT without IN | Single ambient UoW TX with live writer | P5-S5 failure injection |
| Multi-DO Scope refresh missed | `AdditionalScopeStates` + per-DO bootstrap/reconcile | Coexistence identity coverage → P5-S6 |
| Accidental capability enablement | Default `Enabled = false`; independent section name | Production config hygiene |
| Incomplete first commit under MT id | Whole-mutasi key permanently owns responsibility | Caller must post complete mutasi; P5-S4 void assumes whole document |
| Same-`BrgId` multi-line overlapping plans | Fail-closed at `Consume` today | Enforce unique BrgId per request or accumulate reserved qty across lines |
| `SmallestUnitId` null on MT legs | Qty/DO/HPP/ED/batch conserved | Compatibility gap (read-port / mapper); empty `fs_kd_satuan` |

---

## Reviewer notes (before authorizing P5-S4)

Confirm:

1. Capability default **false** and independent from DO Receipt options.
2. `CreateTransfer` conservation holds; destination does **not** invent a new Receipt Source.
3. Trusted allocation (Freshness + reload + FIFO) runs before any write; stale/insufficient fail closed.
4. Legacy OUT/IN follows the Ledger allocation plan (no writer re-FEFO); `fingerprint-v1` refresh per affected DO.
5. SourceConsequence key is `MT\|{SourceTransactionId}`; multi-item reorder and partial retry under the same MT id are quantity-neutral (original idempotency NO-GO covered by tests).
6. Ambiguous LegacyRowId → `Inconsistent`; no-match → `InsufficientStock`.
7. No void path, concurrency locks, ConcurrentOutbound, production HTTP, or FQ-06 claim landed in this slice.
8. Destination establish **and** increase: one OCC bump per write-scope; `ExistingDestination_MultiLayerIncrease_PreservesOcc` covers the review Major finding.

**Prior re-review (2026-08-08):** Whole-mutasi idempotency accepted; gate **NO-GO** on destination OCC increase.

**OCC remediation accepted (2026-08-09 re-review):** Destination OCC + increase test cleared. Gate remains **NO-GO** on LegacyRowId exact-qty wrong-row — see [`stock-ledger-phase5-s3-review.md`](./stock-ledger-phase5-s3-review.md). Do not authorize P5-S4 until that Major is fixed and re-reviewed.
