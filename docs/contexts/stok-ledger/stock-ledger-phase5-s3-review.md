# Stock Ledger Phase 5 / P5-S3 — Slice Review

**Review date:** 2026-08-09  
**Artifact status:** Slice gate re-review after coexistence binding remediation — **PASS WITH MINOR FINDINGS**  
**Slice:** P5-S3 — Native Stock Transfer UseCase + capability flag  
**Plan:** [`stock-ledger-phase5-implementation-plan.md`](./stock-ledger-phase5-implementation-plan.md)  
**Implementation summary:** [`stock-ledger-phase5-s3-implementation-summary.md`](./stock-ledger-phase5-s3-implementation-summary.md)  
**Architecture ADR:** [`adr/ADR-stock-ledger-coexistence-layer-legacy-binding.md`](./adr/ADR-stock-ledger-coexistence-layer-legacy-binding.md)

**Inputs reviewed:**

- Phase 5 plan § P5-S3 + §8 atomic boundary + Trusted Allocation rule
- Prior NO-GO sequence (idempotency → destination OCC → LegacyRowId exact-qty)
- Coexistence binding ADR and additive `BILRG_StokLayerLegacyBinding` projection
- Actual codebase:
  - `PostStockTransferStockConsequenceCommand.cs` / Handler
  - `StockLayerLegacyBindingResolver.cs` / `TransferLegacyCompatibilityMapper.cs`
  - `StockConsequenceUnitOfWork` `LayerLegacyBindings`
  - Reconstruction / Native DO Receipt binding establishment
  - `PostStockTransferStockConsequenceHandlerTest` (14 scenarios)

**Not claimed by this review:** P5-S4 void, P5-S5 concurrency, P5-S6 coexistence proof, FQ-06 / production enablement.

---

## Executive verdict

### **PASS WITH MINOR FINDINGS**

P5-S4 may start. The LegacyRowId exact-qty wrong-row Major is cleared by an Application-owned coexistence binding contract:

```text
Trusted Allocation (StockLayerId)
    -> resolve durable / unique binding -> LegacyRowId
    -> commit-ready consequence
    -> Ledger + legacy writer execute the same physical row
```

Prior cleared NO-GOs remain cleared (whole-mutasi idempotency; destination OCC increase).

Focused transfer suite: **14/14 passed**, including:

- `SameAttributeMultiBalance_OutboundUsesBoundFifoLayerRow`
- `AmbiguousUnboundSameAttributeBalances_FailClosedInconsistent`

---

## Major finding status

### [Major] Exact-qty LegacyRowId disambiguation can OUT the wrong row — **CLEARED**

| Check | Result |
|---|---|
| Mapper no longer selects candidates by attribute/qty | **Pass** — `TransferLegacyCompatibilityMapper.Map` requires `SourceLegacyRowId` / `DestinationLegacyRowId` |
| Quantity never used as identity tie-breaker | **Pass** — resolver material match only; multi-candidate ⇒ Ambiguous/`Inconsistent` |
| Destination MT_IN pre-assigns ST id + persists binding | **Pass** — handler + UoW `LayerLegacyBindings` |
| Reconstruction / Native DO Receipt establish bindings when identities are known | **Pass** |
| Wrong-row regression (FIFO layer vs exact-qty peer) | **Pass** |
| Ambiguous unbound same-attribute fail-closed | **Pass** |

---

## Remaining minor findings (carry forward; do not block P5-S4)

### [Minor] Same-`BrgId` multi-line plans are independent

Per-line `TrustedStockAllocationOrchestrator.PlanAsync` does not reserve prior lines’ allocations. Duplicate Item lines can plan overlapping layers, then throw in `Consume` instead of typed `InsufficientStock`. Typical mutasi is one line per barang; behavior is fail-closed but contract-rough.

### [Minor] `SmallestUnitId` not propagated on MT legs

Handler still passes `SmallestUnitId: null`; writer may persist empty `fs_kd_satuan`. Qty / DO / HPP / ED / batch conservation holds.

---

## Prior NO-GOs — still cleared

| Finding | Status |
|---|---|
| Whole-mutasi SourceConsequence key `MT\|{SourceTransactionId}` | Cleared |
| Destination Position OCC single bump on multi-layer increase | Cleared |
| Capability default false, independent of DO Receipt | Cleared |
| `CreateTransfer` conservation; no new Receipt Source at destination | Cleared |
| Trusted allocation before write; stale/insufficient fail closed | Cleared |
| Allocation-explicit legacy OUT | **Met** after binding remediation |

---

## Acceptance / architecture assessment

| Aspect | Assessment |
|---|---|
| Native conserved `CreateTransfer` + flag | Met |
| Trusted allocation → short TX UoW → live MT OUT/IN | Met |
| Legacy follows Ledger layer identity via coexistence binding | **Met** |
| Scope `fingerprint-v1` refresh (multi-DO) | Met |
| Destination establish / increase under OCC | Met |
| Whole-mutasi idempotency | Met |
| Domain / writer responsibility split preserved | Met — Domain owns `StockLayerId`; writer executes explicit `LegacyRowId`; Application owns the bridge |

### Contract change accepted

```text
Before:
    StockLayer allocation -> mapper infers LegacyRowId

After:
    StockLayer allocation -> resolved coexistence binding -> commit-ready consequence
    mapper/writer consume exact LegacyRowId
```

This is the smallest architectural adjustment that closes the repeated P5-S3 instability class without redesigning Stock Ledger, introducing frameworks, or transferring Stage B authority.

---

## Reviewer-note checklist

| # | Note | Result |
|---|---|---|
| 1 | Capability default false, independent of DO Receipt | Pass |
| 2 | `CreateTransfer` conservation; no new Receipt Source; ERT preserved | Pass |
| 3 | Trusted allocation before writes; stale/insufficient fail closed | Pass |
| 4 | Legacy follows Ledger plan; `fingerprint-v1` per affected DO | Pass |
| 5 | Whole-mutasi key; multi-item reorder/partial retry quantity-neutral | Pass |
| 6 | Ambiguous LegacyRowId → `Inconsistent` | **Pass** |
| 7 | No void / locks / ConcurrentOutbound / HTTP / FQ-06 | Pass |
| 8 | Destination establish/increase stable under Position OCC | Pass |
| 9 | Durable StockLayerId ↔ LegacyRowId binding on create paths | **Pass** |

---

## Why PASS (concrete)

The repeated NO-GO pattern was cross-authority identity loss at the P5-S3 Application composition boundary. The coexistence binding projection + commit-ready slice contract preserves the Ledger-selected layer’s physical row through legacy write, and fail-closes when that binding cannot be established uniquely.

**P5-S4 is authorized** to proceed. Void/reverse helpers should consume the same binding projection rather than re-inferring rows.

---

## Recommended next step

1. Begin P5-S4 Transfer void/reversal using whole-mutasi idempotency + binding-aware reverse targeting.
2. Keep capability default-off.
3. Carry remaining Minors unless they block void correctness.
