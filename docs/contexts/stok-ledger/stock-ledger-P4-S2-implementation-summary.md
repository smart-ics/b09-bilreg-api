# Stock Ledger Phase 4 / P4-S2 — Implementation Summary

**Status:** COMPLETE (NO-GO remediated)  
**Date:** 2026-08-08  
**Slice:** P4-S2 — Native DO Receipt consequence UseCase + Scope baseline + capability gate  
**Plan:** [`stock-ledger-phase4-implementation-plan.md`](./stock-ledger-phase4-implementation-plan.md)  
**Dependency:** P4-S1 gate **PASS** ([`stock-ledger-P4-S1-implementation-summary.md`](./stock-ledger-P4-S1-implementation-summary.md))

---

## Objective

Answer: can a DO Receipt be represented deterministically as a Native Stock Movement / Stock Layer, initialize coexistence Scope + Synchronization Position without authority semantics, and commit with the P4-S1 legacy consequence through the existing UoW — behind a disabled-by-default capability flag?

---

## What was implemented

| Area | Deliverable |
|---|---|
| Domain | `StockLedgerScopeStateModel.EstablishFromNativeReceipt` — `NotReconstructed` → `Reconstructed` + `Current` + position (no fake Phase A–C claim) |
| Application options | `StockLedgerDoReceiptOptions` (`SECTION_NAME = "StockLedgerDoReceipt"`, `Enabled` default **false**) |
| Idempotency | `DoReceiptConsequenceIdempotency.BuildSourceConsequenceKey` → `DO\|{BrgId}\|{ReceiptSourceId}\|{SourceTransactionId}` |
| Mapper | `DoReceiptLegacyCompatibilityMapper` — pre-assigns BK/ST ids, 1:1 journal/balance pairs, fingerprint snapshot projection |
| UseCase | `PostDoReceiptStockConsequenceCommand` / `PostDoReceiptStockConsequenceHandler` |
| Greenfield guard | Pre-mutation probe: empty `ListCurrentBalances` + `ListJournalEntries` **and** empty `IStockPositionRepo.ListByLedgerScope` before establish/Commit |
| Infrastructure | `LegacyCompatibilityWriterPort` honors pre-assigned `LegacyJournalId` / `LegacyRowId` when present |
| Tests | `PostDoReceiptStockConsequenceHandlerTest` (12 scenarios) + Domain UT15/UT16 |
| Docs | This summary + plan progress + roadmap + ARTIFACTS |

**Explicitly not implemented:** void/correction (P4-S4); failure-injection / New→Legacy harness (P4-S3); production DI/HTTP; successful post onto already-`Reconstructed` or prior-history scopes; enabling capability in production appsettings.

---

## Handler outcomes

| Outcome | Meaning |
|---|---|
| `Committed` | Native Movement/Layer/Position + Scope baseline + legacy DM post + SYNC identity bootstrap |
| `AlreadyCommitted` | Prior `SourceConsequence` key found (or UoW duplicate) — quantity-neutral, no rewrite |
| `Disabled` | Capability off — zero writes |
| `ScopeNotEligible` | Not greenfield: already baselined / mid-status / **prior legacy or Ledger history for Item+DO** |
| `Inconsistent` | Scope / gate Inconsistent — fail closed |
| `StaleOrNotCurrent` | Freshness Gate did not prove current — fail closed |

---

## Greenfield eligibility (NO-GO remediation)

Phase 4 §2.4 requires proceed only when Scope is absent/`NotReconstructed` **and there is no prior Ledger/legacy history** for Item+DO.

After capability, idempotency, and Scope-status / Freshness Gate checks, the `NotReconstructed` branch now:

1. Reads `ILegacyStockReadPort.ListCurrentBalances` + `ListJournalEntries`.
2. Reads `IStockPositionRepo.ListByLedgerScope`.
3. If any list is non-empty → `ScopeNotEligible` with **no** UoW Commit, **no** Scope establish, **no** SYNC bootstrap.

This closes the review Major: prior VB6 (or other) history must not be treated as an empty scope (double INSERT + wrong `fingerprint-v1` Synchronization Position).

Idempotency remains **before** eligibility so a genuine retry of an already committed P4-S2 receipt stays quantity-neutral `AlreadyCommitted`.

---

## Repository decisions

| Decision | Rationale |
|---|---|
| Pre-assign BK/ST ids in Application mapper | UoW persists Scope before legacy Apply; fingerprint must be known before commit without changing UoW write order (P4-S3 owns atomicity proof) |
| Writer honors provided ids | Minimal P4-S1 extension; empty id → generate as before (backward compatible) |
| Identity bootstrap post-commit | Separate short TX via `LegacySyncIdentityBootstrapper` / `CommitSyncEvidence` (quantity-neutral; matches Phase 3 first-sync bootstrap) |
| Idempotency checked before Scope eligibility | Duplicate retry after successful Native establish must return `AlreadyCommitted`, not `ScopeNotEligible` |
| Prior history → `ScopeNotEligible` (not `Inconsistent`) | Scope status itself is not inconsistent; greenfield establish is simply not allowed |
| Already-`Reconstructed` → fail closed after gate | P4-S2 posts greenfield only; position refresh / multi-path post deferred |
| No production DI / HTTP | Capability remains off; tests compose manually |
| `DO\|…` idempotency prefix | Distinct from `RECON\|…` / `SYNC\|…`; discovery continues to ignore non-`SYNC\|…` keys |

---

## Deviations from the Phase 4 plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Fingerprint from post-write read-back inside same TX | Pre-assign ids + compute from mapper snapshot before Commit; assert equality vs post-commit read-back in tests | Avoids UoW reorder; P4-S3 still proves live atomicity |
| Optional production composition root | Omitted entirely (same as P4-S1) | Safer; no accidental enablement |
| Reconstructed + Current gate path | Returns `ScopeNotEligible` (not a second post) | Matches plan “do not post on reconstructed scope in P4-S2” |
| Initial S2 shipped without prior-history probe | Remediated: empty legacy + empty Ledger positions required | Closes NO-GO before P4-S3 |

---

## Validation evidence

Focused run (disposable `devTest` only):

```text
dotnet test Bilreg.Test/Bilreg.Test.csproj --filter FullyQualifiedName~PostDoReceiptStockConsequenceHandlerTest|FullyQualifiedName~StockLedgerScopeStateTest|FullyQualifiedName~LegacyCompatibilityWriterPortTest
Passed!  - Failed: 0, Passed: 35, Skipped: 0
```

| Test | Result |
|---|---|
| Happy-path Native origin + legacy rows + SYNC bootstrap | Pass |
| Capability disabled ⇒ no Ledger/legacy writes | Pass |
| Duplicate source ⇒ AlreadyCommitted, quantity-neutral | Pass |
| Scope position `fingerprint-v1` matches post-commit calculator | Pass |
| No `IsAuthoritative` on Scope/Movement/Position types | Pass |
| Reconstructed + undeterminable discovery ⇒ StaleOrNotCurrent | Pass |
| **Prior legacy history + NotReconstructed ⇒ ScopeNotEligible, no mutation** | Pass |
| Prior Ledger position + NotReconstructed ⇒ ScopeNotEligible, no mutation | Pass |
| Multi-line happy path | Pass |
| Reconstructed + Current ⇒ ScopeNotEligible | Pass |
| Inconsistent Scope ⇒ Inconsistent, no writes | Pass |
| ReconstructionRequired ⇒ ScopeNotEligible | Pass |
| Domain EstablishFromNativeReceipt happy + illegal sources | Pass |

---

## Residual risks / handoff to P4-S3

| Item | Note for next slice |
|---|---|
| **G-18 live atomicity** | UoW still persists Ledger before legacy Apply; ambient TX rollback is assumed — P4-S3 owns failure-injection proof with live writer |
| **FQ-02 / pre-assigned ids** | BK/ST generated before TX; if Commit rolls back, those id strings are unused (not durable rows). Document only — no ParamNoDal counter advance |
| **New→Legacy harness** | Still skipped; activate in P4-S3 using this UseCase behind flag |
| **Bootstrap outside consequence TX** | SYNC keys inserted after successful Commit; if bootstrap fails mid-way, Ledger+legacy already durable — retry bootstrap is quantity-neutral via SyncBatch idempotency |
| **Reconstructed / prior-history multi-line post** | Still fail-closed; do not invent second post path in S3 unless required for harness |
| **FQ-06 / Scope claim race** | Greenfield Scope persist is not claim-conditional (`ExpectedPriorReconstructionStatus` / `TryInsertNew`) — Phase 9 residual; **not** a substitute for the prior-history guard |
| **Void** | Writer still rejects Delete / `IsVoid` — P4-S4 |
| **Capability** | Keep default-off; do not register production DI/HTTP |

**Ready for P4-S3:** yes — the original NO-GO prior-legacy scenario is explicitly tested and green.

---

## Rollback / containment

Keep `StockLedgerDoReceiptOptions.Enabled = false`. Do not register handler or live writer in production DI. Remove or stop composing `PostDoReceiptStockConsequenceHandler` in tests. Disposable-DB rows cleaned per test.
