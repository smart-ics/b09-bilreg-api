# Stock Ledger Phase 5 / P5-S2 — Implementation Summary

**Status:** COMPLETE  
**Gate:** **PASS**  
**Date:** 2026-08-08  
**Slice:** P5-S2 — Live MT Legacy Compatibility Writer (transfer post shape)  
**Plan:** [`stock-ledger-phase5-implementation-plan.md`](./stock-ledger-phase5-implementation-plan.md)

---

## Objective

Extend the live Infrastructure `ILegacyCompatibilityWriterPort` adapter so an **allocation-explicit** Transfer write request can reproduce the authoritative MT transfer consequence — `MT_OUT` deplete at source, then `MT_IN` insert at destination, preserving DO/PO/HPP/ED/batch — enlisted in the caller's ambient SQL transaction on disposable/test SQL. No independent FIFO/FEFO inside the writer. No Native Transfer UseCase.

---

## Implemented changes

| Area | Deliverable |
|---|---|
| Infrastructure | `LegacyCompatibilityWriterPort` Transfer post branch: validate → all `MT_OUT` → all `MT_IN` |
| Shared SQL | `DepleteStokRow` extracted for DM void and MT_OUT (UPDLOCK + DELETE-on-zero / UPDATE) |
| Application | Port XML docs only — existing DTOs reused; no new request types |
| Tests | 5 MT transfer characterization tests + helpers in `LegacyCompatibilityWriterPortTest` |
| Docs | This summary + plan Slice Progress + ARTIFACTS |

**Explicitly not implemented:** Native Transfer UseCase / capability flag (P5-S3); transfer void `MT_IN_V`/`MT_OUT_V` (P5-S4); concurrency / ConcurrentOutbound (P5-S5); coexistence proof (P5-S6); production DI/HTTP; Application mapper from `StockAllocationResult`.

---

## MT compatibility contract (A/B)

### A — Consumer-dependent behavior preserved (must)

| Concern | Implementation |
|---|---|
| Mutation jenis | Post: `MT_OUT` then `MT_IN` in one `Apply` |
| Mutation id | `fs_kd_mutasi` = MT transaction id (`MutationTransactionId`) |
| OUT leg | Deplete targeted source `tb_stok` by `LegacyRowId`; insert outbound `tb_buku` (`fn_stok_out`, jenis `MT_OUT`) carrying DO/PO/HPP/ED/batch |
| IN leg | Insert destination `tb_buku` then `tb_stok` from **explicit IN-line facts** (caller mirrors OUT; writer does not re-discover) |
| Pairing order | All OUT legs complete before any IN leg (VB6 `GenStokMutasi` order) |
| Zero-row deletion | Full OUT depletion deletes `tb_stok` row |
| Ambient TX | Opens connection into caller's `TransHelper` scope; rollback without `Complete` |

### B — Incidental mechanics not ported

| Mechanic | Treatment |
|---|---|
| VB6 `Generate` / `clbGenStokX1` control flow | Forbidden |
| Independent `RemoveStok` FEFO | Forbidden — allocation-explicit `LegacyRowId` only |
| String-concat SQL | Parameterized Dapper |
| Reading OUT journals to drive IN | Caller supplies conserved IN lines; writer validates pair conservation |
| Transfer void | Deferred to P5-S4 |

---

## Architectural decisions

| Decision | Rationale |
|---|---|
| Reuse existing write DTOs (2 lines per slice: OUT + IN) | Destination encoded on IN `LayananId`; no parallel infrastructure |
| Process all OUT then all IN | Matches VB6; prevents OUT-without-IN visibility outside TX |
| No Application Transfer mapper in S2 | Tests build explicit requests; P5-S3 owns plan→request mapping |
| Shared `DepleteStokRow` | Avoid duplicate UPDLOCK/DELETE/UPDATE between DM void and MT_OUT |
| Paired conservation validation | Fail closed if OUT/IN qty/DO/HPP/ED/batch/PO/MT-id diverge |
| Keep DM Receipt/Reversal paths intact | Unsupported kinds still throw; Outbound/Correction remain unsupported |

---

## Deviations from plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Summary filename `stock-ledger-P5-S2-implementation-summary.md` (§12) | `stock-ledger-phase5-s2-implementation-summary.md` (task request) | Naming only; ARTIFACTS links the chosen path |
| Optional Application DTO additive fields | None needed — existing line DTOs sufficient | Cleaner reuse |

No locked domain/ADR decisions were reopened.

---

## Tests added/updated

Focused run (disposable `devTest`):

```text
dotnet test Bilreg.Test/Bilreg.Test.csproj --filter FullyQualifiedName~LegacyCompatibilityWriterPortTest
Passed!  - Failed: 0, Passed: 15, Skipped: 0
```

| Test | Result |
|---|---|
| `Apply_TransferPost_ReadBack_ConservesQtyDoHppEdAtBothLocations` | PASS — source depleted; dest increased; DO/HPP/ED/batch/PO/MT id conserved |
| `Apply_TransferPost_FullDepletion_DeletesSourceStokRow` | PASS — zero-row delete |
| `Apply_TransferPost_PartialDepletion_ReducesSourceQty` | PASS — source UPDATE leaves remainder |
| `Apply_TransferPost_WithoutComplete_RollsBackBothLegs` | PASS — neither OUT nor IN durable without Complete |
| `Apply_TransferPost_MultiSlice_TwoOutInPairs` | PASS — two OUT then two IN in one Apply |
| `Apply_UnsupportedMovementKind_Throws` | PASS — updated message; Outbound still rejected |
| Existing DM receipt/void suite | PASS — regression green |

---

## Limitations intentionally deferred

- Native Transfer UseCase, Scope refresh, capability flag (P5-S3)
- `TransferLegacyCompatibilityMapper` from trusted allocation plan (P5-S3)
- Transfer void / unsafe reverse (P5-S4)
- Lock order, quantity revalidation inside consequence TX, ConcurrentOutbound (P5-S5)
- Native transfer → VB6 → Phase 3 sync coexistence proof (P5-S6)
- Production enablement / public HTTP / DI registration

---

## Known risks

| Risk | Mitigation in P5-S2 | Residual |
|---|---|---|
| Writer re-FEFO diverging from Ledger plan | No stock discovery; `LegacyRowId` required on OUT | P5-S3 must map allocation plan explicitly |
| OUT without IN partial visibility | Single ambient TX; OUT-then-IN order; rollback test | P5-S5 failure injection for multi-position |
| Destination merge instead of INSERT | Always INSERT (receipt path) | None for this slice |
| Counter concurrency (FQ-02) | Same `NunaId.NewLegacyCompact` as P4 | Production hardening residual |

---

## Gate verdict

**PASS** — consumer-dependent MT post contract satisfied on disposable DB; writer stays in Infrastructure; Domain remains SQL-free; no production enablement; DM receipt/void regression green.

---

## Reviewer notes (before authorizing P5-S3)

Confirm:

1. MT OUT/IN conservation holds on disposable DB (qty, DO, HPP, ED, batch, PO; MT id on journals).
2. Writer performs **no** independent FEFO/FIFO or Availability reads.
3. All OUT legs complete before any IN leg within one `Apply`.
4. Full depletion deletes source `tb_stok`; partial reduces; ambient TX rollback clears both legs.
5. DM receipt/void tests remain green; `Outbound`/`Correction` still unsupported.
6. No UseCase, void path, capability flag, or production enablement landed in this slice.

**Approve P5-S3 only after this gate PASS is accepted.**
