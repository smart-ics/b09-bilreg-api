# Stock Ledger Phase 5 / P5-S3 — Implementation Summary

**Status:** COMPLETE — gate **PASS WITH MINOR FINDINGS**  
**Date:** 2026-08-08 (initial); OCC remediation 2026-08-08; coexistence binding remediation 2026-08-09  
**Slice:** P5-S3 — Native Stock Transfer UseCase + capability flag  
**Plan:** [`stock-ledger-phase5-implementation-plan.md`](./stock-ledger-phase5-implementation-plan.md)  
**Gate review:** [`stock-ledger-phase5-s3-review.md`](./stock-ledger-phase5-s3-review.md) — **PASS WITH MINOR FINDINGS**  
**Architecture ADR:** [`adr/ADR-stock-ledger-coexistence-layer-legacy-binding.md`](./adr/ADR-stock-ledger-coexistence-layer-legacy-binding.md)

---

## Objective

Deliver a thin MediatR/Inventory-style UseCase that posts one authorized Stock Transfer as a Native conserved Transfer movement, updates source/destination Positions/Layers, refreshes coexistence Synchronization Position(s) with `fingerprint-v1`, and commits paired MT OUT/IN legacy consequences through the existing UoW — behind a disabled-by-default capability flag independent from DO Receipt — with **allocation-explicit LegacyRowId** preserved via an Application coexistence binding.

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
| Ambiguous LegacyRowId / missing unique binding | `Inconsistent` (fail closed) |
| No matching LegacyRowId / insufficient qty at bound row | `InsufficientStock` (fail closed) |

Callers must post the **complete** authorized mutasi in one request. An incomplete first commit under the same MT id permanently owns that SourceConsequence responsibility.

**P5-S4 handoff (idempotency):** Transfer void / original-consequence lookup **must** use the same whole-mutasi key `MT|{SourceTransactionId}`.

**P5-S4 handoff (Position OCC):** Both source consume and destination establish/increase use **one** `Version + 1` per write-scope per consequence commit.

**P5-S4 handoff (coexistence binding):** Void/reverse **must** use `BILRG_StokLayerLegacyBinding` / `IStockLayerLegacyBindingRepo` to target physical rows. Do not re-infer LegacyRowId by attribute/qty.

---

## Coexistence binding remediation (2026-08-09)

### Root cause

Ledger FIFO selects `StockLayerId`. The transfer mapper previously rebuilt `LegacyRowId` from live balances using DO/location/HPP/ED/batch and an exact-qty escape hatch. Under repeated `MT_IN` accumulation with shared attributes, that could OUT a different `tb_stok` row than the FIFO-selected layer while still committing both authorities.

### Chosen remediation (architectural, bounded)

1. ADR: Application owns StockLayerId ↔ LegacyRowId coexistence binding.
2. Additive table `BILRG_StokLayerLegacyBinding` (compatibility projection; no Domain identity change).
3. Commit-ready `TransferAllocationSlice` carries `StockLayerId`, `SourceLegacyRowId`, `DestinationLegacyRowId`.
4. `StockLayerLegacyBindingResolver` resolves durable bindings; unique lazy establish only when material attributes identify exactly one surviving row; never quantity tie-break.
5. Destination ST ids pre-assigned with destination layers in the same consequence; bindings persisted via `StockConsequenceDraft.LayerLegacyBindings`.
6. Reconstruction and Native DO Receipt establish bindings when both identities are known.
7. Mapper becomes a deterministic formatter — no candidate selection.

### Tests added for the Major

| Test | Result |
|---|---|
| `SameAttributeMultiBalance_OutboundUsesBoundFifoLayerRow` | PASS — FIFO-first bound row depleted; exact-qty peer untouched |
| `AmbiguousUnboundSameAttributeBalances_FailClosedInconsistent` | PASS — no wrong-row commit |

---

## Destination OCC remediation (2026-08-08) — retained

`BuildDestinationPositions` mirrors source: merge existing + inbound layers, then `StockPositionModel.Create(..., baseVersion + 1)`. Covered by `ExistingDestination_MultiLayerIncrease_PreservesOcc`.

---

## Implemented changes

| Area | Deliverable |
|---|---|
| ADR | `ADR-stock-ledger-coexistence-layer-legacy-binding.md` |
| SQL | `BILRG_StokLayerLegacyBinding.sql` |
| Application | `StockLayerLegacyBindingType`, repo port, resolver; UoW `LayerLegacyBindings` |
| Infrastructure | Binding DTO/DAL/Repo |
| Legacy mapper | Explicit row ids only; no attribute/qty selection |
| UseCase | Binding-aware transfer artifact build + destination ST pre-assign |
| Establishment | Reconstruction + Native DO Receipt write bindings |
| Sync loader | Prefer binding anchors over material/qty match |
| Options / idempotency | Unchanged contracts (`StockLedgerStockTransferOptions`, whole-mutasi key) |
| Tests | 14 transfer scenarios on disposable `devTest` |
| Docs | This summary + review + plan progress + ARTIFACTS |

**Explicitly not implemented:** transfer void (P5-S4); lock order / ConcurrentOutbound (P5-S5); coexistence proof (P5-S6); production DI/HTTP; enabling flag in appsettings.

---

## Architectural decisions

| Decision | Rationale |
|---|---|
| Separate coexistence binding projection | Keep Domain free of `fs_kd_trs`; keep writer free of discovery |
| Application consequence boundary owns resolution | Matches Stage B Trusted Allocation → Ledger → Legacy-follows-plan |
| Fail closed on multi-candidate unbound scopes | Prevents silent wrong-row commits |
| Pre-assign destination LegacyRowId with dest layer | Later hops (void/outbound) retain identity without re-guessing |
| Capability default false | Plan §9 |

---

## Tests

```text
dotnet test Bilreg.Test/Bilreg.Test.csproj --filter FullyQualifiedName~PostStockTransferStockConsequenceHandlerTest
Passed!  - Failed: 0, Passed: 14, Skipped: 0
```

Prior happy-path / idempotency / OCC / multi-DO / ED / stale / insufficient scenarios remain green.

---

## Limitations intentionally deferred

- Transfer void / unsafe reverse (`MT_IN_V` / `MT_OUT_V`) — P5-S4
- Lock order, quantity revalidation inside consequence TX, ConcurrentOutbound — P5-S5
- Native transfer → VB6-shaped change → Phase 3 sync coexistence proof — P5-S6
- Full multi-position failure-injection matrix
- Production enablement / public HTTP / DI registration
- Same-`BrgId` multi-line reservation / typed insufficient
- `SmallestUnitId` continuity on MT legs

---

## Known risks / residuals

| Risk | Mitigation in P5-S3 | Residual |
|---|---|---|
| Ambiguous unbound historical scopes | Fail closed as `Inconsistent` | Operator/sync may need unique evidence before transfer |
| Binding stale after legacy-side mutation | Freshness Gate + writer UPDLOCK revalidation | P5-S5 concurrency hardening |
| Partial OUT without IN | Single ambient UoW TX | P5-S5 failure injection |
| Accidental capability enablement | Default `Enabled = false` | Production config hygiene |

---

## Reviewer notes (P5-S4 authorization)

Confirm:

1. Capability default **false** and independent from DO Receipt options.
2. `CreateTransfer` conservation; destination does **not** invent a new Receipt Source.
3. Trusted allocation before writes; stale/insufficient fail closed.
4. Legacy OUT/IN follows Ledger plan via durable/unique binding; no mapper re-FEFO / exact-qty guess.
5. Whole-mutasi key quantity-neutral under reorder/partial retry.
6. Ambiguous unbound multi-candidate → `Inconsistent`.
7. Destination OCC increase single bump.
8. Wrong-row and Ambiguous regression tests green.

**Gate accepted:** P5-S4 is authorized.
