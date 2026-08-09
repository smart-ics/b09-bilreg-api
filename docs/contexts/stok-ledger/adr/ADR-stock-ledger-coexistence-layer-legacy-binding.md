# ADR — Coexistence StockLayerId ↔ LegacyRowId Binding

**Status:** Accepted — Phase-5 / P5-S3 architectural correction (2026-08-09)  
**Context:** Stock Ledger Stage B coexistence; Native Stock Transfer consequence  
**Related:** [`stock-ledger-phase5-implementation-plan.md`](../stock-ledger-phase5-implementation-plan.md) §2.5 / §5 / §8, [`stock-ledger-phase5-s3-review.md`](../stock-ledger-phase5-s3-review.md), BR-STL-024 / BR-STL-043 / BR-STL-066–067 / BR-STL-076–077 / BR-STL-082

---

## Decision

Under Stage B coexistence, **Ledger layer identity** and **legacy balance-row identity** remain distinct:

| Identity | Owner | Meaning |
|---|---|---|
| `StockLayerId` | Stock Ledger Domain | Accountable layer consumed/established by FIFO and movements |
| `LegacyRowId` (`tb_stok.fs_kd_trs`) | Legacy Stock Authority | Physical balance row depleted/inserted by compatibility writers |

The Application coexistence boundary **must** maintain an explicit binding between them while a legacy row is the authoritative balance for a layer:

```text
Trusted Allocation (StockLayerId)
    -> resolve coexistence binding (StockLayerId -> LegacyRowId)
    -> commit-ready consequence slice
    -> Ledger movement / Positions
    -> Legacy writer executes exact LegacyRowId
```

### Normative contract

1. A commit-ready outbound/transfer consequence slice **shall** carry both the selected `StockLayerId` and its exact source `LegacyRowId`.
2. Destination layers created by Native transfer/receipt **shall** pre-assign the destination `LegacyRowId` in the same atomic consequence and persist the binding.
3. Attribute/quantity matching **must not** select a legacy row when more than one candidate exists. Quantity is state, not identity.
4. Missing, stale, or ambiguous binding evidence ⇒ fail closed as `Inconsistent` (or reconstruction/sync inconsistency). Do not invent identities.
5. Domain FIFO continues to select only `StockLayerId`. Infrastructure writers continue to execute only explicit `LegacyRowId`. Neither layer owns the other identity.
6. The binding is **compatibility metadata**, not Domain authority and not Stage C ownership.

### Persistence shape (additive)

Store an Application persistence projection keyed by `StockLayerId`:

- `LegacyRowId`
- write-scope material evidence (`BrgId`, `ReceiptSourceId`, `LayananId`, unit cost, ED, batch)
- bound-at timestamp / audit columns

Do **not** add `LegacyRowId` to the Domain `StockLayerModel` as a Domain identity. Do **not** invent FK constraints to `tb_stok`.

### Establishment points

Bindings are written only when both identities are known without guessing:

- Reconstruction baseline when one surviving balance row establishes one layer
- Native DO Receipt when ST id is pre-assigned with the new layer
- Native Stock Transfer when destination ST id is pre-assigned with the destination layer
- Lazy unique establishment for historical unbound layers only when material attributes uniquely identify one surviving balance row

### Lifecycle

- Binding remains after legacy zero-row delete for historical traceability.
- Allocation may use a binding only when the bound row still survives with sufficient quantity (writer revalidation remains authoritative inside the consequence TX).
- Sync/freshness may refresh or invalidate binding usability when material evidence drifts; it must not silently rebind to a different row by quantity guess.

---

## Why

P5-S3 review found that `TransferLegacyCompatibilityMapper` re-inferred `LegacyRowId` from DO/location/HPP/ED/batch/quantity after FIFO had already selected a `StockLayerId`. Under repeated `MT_IN` accumulation with shared attributes, exact-qty disambiguation can commit Ledger + legacy with divergent physical-row identity while totals still conserve.

That violates the Phase-5 rule that legacy OUT/IN must follow the Ledger allocation plan, and it undermines void, concurrency lock targeting, sync balance anchors, and later outbound families.

Prior P5-S3 remediations (whole-mutasi idempotency, destination OCC) fixed local grain/version defects but did not close the cross-authority identity gap.

---

## Consequences

- P5-S3 mapper becomes a deterministic formatter of already-resolved slices.
- P5-S4 void/reverse should look up bindings rather than re-guess destination/source rows.
- P5-S5 lock order targets the bound `LegacyRowId`.
- Phase 6/7 transfer/outbound reuse the same binding contract.
- Existing unbound scopes remain safe: unique lazy bind or fail closed — no speculative mass backfill.

---

## Explicit non-decisions

- No authority transfer / `IsAuthoritative` / Stage C.
- No Domain redesign of Stock Layer identity.
- No generic allocation framework.
- No claim that FQ-06 / live VB6 concurrency is solved.
