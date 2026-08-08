# Stock Ledger Phase 4 / P4-S3 — Implementation Summary

**Status:** COMPLETE — **G-18 live-writer PASS**; G-23 New→Legacy + PartialFailure activated  
**Date:** 2026-08-08  
**Slice:** P4-S3 — Live consequence atomicity + New→Legacy visibility  
**Plan:** [`stock-ledger-phase4-implementation-plan.md`](./stock-ledger-phase4-implementation-plan.md)  
**Dependencies:** P4-S1 gate **PASS**; P4-S2 COMPLETE

---

## Objective

Answer: can a failure at any important persistence step prove that receipt consequences never partially commit (G-18 with the **live** writer), and can authoritative legacy rows written by the new system be seen by legacy-shaped readers (G-23 New→Legacy)?

---

## Verdict

**PASS.** Failure injection at idempotency→movement→position→scope→legacy Apply (including mid multi-row legacy write) leaves **zero** durable BILRG + `tb_stok`/`tb_buku` residue when the ambient `TransHelper` transaction does not Complete. Happy-path live commit + idempotent retry remains quantity-neutral. New→Legacy harness posts via `PostDoReceiptStockConsequenceHandler` and asserts visibility through `LegacyStockReadPort`. No Infrastructure enlistment fix was required; existing ambient TX enlistment holds.

---

## What was implemented

| Area | Deliverable |
|---|---|
| Test fakes | `StockLedgerFailureInjectionFakes.cs` — throwing movement/position/scope repos; `ThrowingLegacyCompatibilityWriterPort`; `PartialLegacyCompatibilityWriterPort` |
| Atomicity matrix | `StockConsequenceUnitOfWorkLiveAtomicityTest` (6 tests) on disposable `devTest` |
| G-23 New→Legacy | `NewToLegacy_ReceiptVisibleInLegacyAuthority` activated (live UseCase + live read port) |
| G-23 PartialFailure | `PartialFailure_LegacyAndLedger_RollBackTogether` activated (harness marker; matrix in atomicity class) |
| Docs | This summary + plan progress + ARTIFACTS + roadmap note |

**Explicitly not implemented:** void/correction (P4-S4); AlternatingWriters (P4-S5); ConcurrentOutbound (Phase 5); production DI/HTTP; capability enablement; UoW write-order change.

---

## Normative write order (proven)

```text
InsertOrGetExisting(SourceConsequence idempotency)
→ SaveChanges(Movement + lines)
→ SaveChanges(Position + Layers) [per location]
→ SaveChanges(Scope baseline)
→ LegacyCompatibilityWriterPort.Apply (per line: INSERT tb_buku, then INSERT tb_stok)
→ Complete()
```

Ledger pieces still persist **before** legacy Apply (P1-S8 order). Acceptable because failure before `Complete` rolls back **both** representations under one ambient TX — proven with the live writer.

---

## Failure-injection matrix

| Test | Injection | Residue after throw |
|---|---|---|
| `Commit_LegacyApplyThrows_RollsBackLedgerAndLegacy` | Throw on Apply (wrap live writer) | Zero BILRG + legacy |
| `Commit_MovementSaveThrows_RollsBackIncludingIdempotency` | Throw on movement SaveChanges | Zero (incl. idempotency) |
| `Commit_PositionSaveThrows_RollsBackAll` | Throw on position SaveChanges | Zero |
| `Commit_ScopeSaveThrows_RollsBackAll` | Throw on scope SaveChanges | Zero |
| `Commit_MidLegacyMultiRowWrite_RollsBackAll` | Apply line 1 via live writer, then throw (2-line draft) | Zero (partial line 1 rolled back) |
| `Commit_HappyPathLiveWriter_ThenRetry_IsAlreadyCommitted` | None / duplicate key | First commits once; retry `AlreadyCommitted` |

Harness:

| Test | Result |
|---|---|
| `NewToLegacy_ReceiptVisibleInLegacyAuthority` | Native movement + `LegacyStockReadPort` sees qty/HPP/DO journal |
| `PartialFailure_LegacyAndLedger_RollBackTogether` | Live Apply throw → zero residue |

---

## Repository decisions

| Decision | Rationale |
|---|---|
| Test-primary slice; no Domain/Application/production DI changes | Plan scope; happy path already in P4-S2 |
| Shared throwing decorators in Test Fakes | Reuse P2-S6 pattern; avoid parallel frameworks |
| `PartialLegacyCompatibilityWriterPort` applies truncated request then throws | Proves mid multi-row legacy write enlistment without changing live writer API |
| Keep UoW write order unchanged | Rollback holds; no evidence of Ledger-only durable intermediate state |
| Put atomicity tests in `StockLedgerP3S4` collection | Reduce shared-`devTest` deadlock noise when run with other live SQL tests |
| No enlistment code change | P4-S1 + P4-S3 evidence: `TransHelper` ambient TX already rolls back live writer inserts |

---

## Deviations from the Phase 4 plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Optional concurrent duplicate under injection matrix | Covered by P4-S2 handler replay + UoW happy-path retry; no new concurrent stress | Acceptance still met |
| Fix Infrastructure if enlistment broken | Not required | Cleaner handoff |

---

## Validation evidence

Focused gate filter (disposable `devTest` only):

```text
dotnet test --filter FullyQualifiedName~StockConsequenceUnitOfWorkLiveAtomicityTest|FullyQualifiedName~NewToLegacy_ReceiptVisibleInLegacyAuthority|FullyQualifiedName~PartialFailure_LegacyAndLedger
Passed!  - Failed: 0, Passed: 8, Skipped: 0
```

Related prior-slice regression (same session):

```text
…LiveAtomicityTest|…NewToLegacy…|…PartialFailure…|…PostDoReceipt…|…LegacyCompatibilityWriterPortTest|…StockConsequenceUnitOfWorkTest|…PhaseCPersistFailure
Passed!  - Failed: 0, Passed: 31, Skipped: 0
```

Full parallel `StockLedgerFeature` suite may show intermittent shared-DB deadlocks (known ambient risk on `devTest`, documented since P4-S1). Gate evidence is the focused filter above.

---

## Residual risks / handoff to P4-S4

| Item | Note for next slice |
|---|---|
| **Void / `DO_V`** | Writer still rejects Delete / `IsVoid` — P4-S4 owns accountable Reverse + legacy void |
| **SYNC bootstrap outside consequence TX** | Unchanged from P4-S2; quantity-neutral via SyncBatch idempotency |
| **FQ-02 / pre-assigned BK/ST** | Ids generated before TX; unused strings after rollback are **not** durable rows and do not advance ParamNoDal counters |
| **AlternatingWriters** | Still skipped — P4-S5 fixture-simulated ownership |
| **FQ-06 / production G-17** | Explicitly **not** claimed |
| **Capability** | Keep `StockLedgerDoReceipt:Enabled` default **false**; no production DI/HTTP |
| **G-18 checklist** | Live-writer acceptance criteria for this slice are met; Phase 4 exit still waits on void (S4) + coexistence (S5) |

**Ready for P4-S4:** yes.

---

## Rollback / containment

Keep capability false. Atomicity/harness tests only mutate disposable `devTest`. No production registration was added. Re-skip harness tests only if instability returns (not observed on focused filter).
