# ADR-APT-002 Pharmacy and Stock Ledger Boundary

**Status:** Accepted  
**Date:** 2026-08-16  
**Owner:** Architecture Team

## Context

Outpatient Pharmacy must coordinate physical fulfillment with Stock Ledger while preserving clear ownership between workflow lifecycle and inventory quantity facts.

Earlier artifacts mixed Pharmacy concepts (`Prepared`, `Handed Over`, `No Show`, reservation, fulfillment clearance) with Inventory authority. BA-09 closes this boundary.

Stock Ledger remains a pure stock authority. Pharmacy owns dispensing lifecycle and operational outcomes. Cross-context effects are delivered through the Integration Task Table defined in BA-07.

## Decision

### Ownership boundary

**Stock Ledger owns:**

- Stock Quantity
- Mutasi (Transfer)
- Remove Stock
- Stock Movement History

**Stock Ledger does not own:**

- Dispensing
- `Prepared`
- `Handed Over`
- No Show
- Fulfillment lifecycle

**Pharmacy owns:**

- Sales Order
- Dispensing
- Dispensing lifecycle
- `Prepared`
- `Handed Over`
- No Show resolution

### Dispensing Temporary Unit

Pharmacy fulfillment uses a dedicated Stock Location called **Dispensing Temporary Unit** to hold medication under active dispensing custody between Pharmacy Unit and final issue or return.

There is no separate inventory reservation operation. Pharmacy **Reserve** is implemented only as **Stock Mutasi** from Pharmacy Unit to Dispensing Temporary Unit.

### Lifecycle-to-stock-movement mapping

| Pharmacy event | Inventory action |
|---|---|
| Dispensing Started | Mutasi from Pharmacy Unit to Dispensing Temporary Unit |
| Dispensing Completed / `Prepared` | No inventory action |
| Medication Handed Over | Remove Stock from Dispensing Temporary Unit |
| No Show resolution | Mutasi from Dispensing Temporary Unit back to Pharmacy Unit |

### Additional rules

- **`Prepared` is a Dispensing state only.** A Dispensing reaches `Prepared` when all required dispensing movements for that preparation have completed. `Prepared` is not an Inventory state.
- **Partial fulfillment belongs to Sales Order.** A Sales Order may be fulfilled through multiple Dispensings. Dispensing does not own partial-fulfillment semantics.
- **No Show is Pharmacy-owned.** Inventory applies only the return Mutasi directed by Pharmacy. Inventory never stores No Show status.

## Rationale

- Keeps Stock Ledger accountable for quantity and movement only.
- Avoids duplicate lifecycle state between Pharmacy and Inventory.
- Reuses existing Stock Transfer (Mutasi) instead of inventing a separate reservation contract.
- Aligns with stock-ledger coexistence architecture and BA-07 integration delivery.

## Consequences

### Positive

- One clear stock integration model for outpatient dispensing.
- `Prepared`, handover, and No Show remain Pharmacy-authoritative facts.
- Implementation can use Stock Ledger transfer and remove-stock commands with Dispensing lineage.

### Negative / trade-offs

- Pharmacy orchestration must issue explicit stock consequences for reserve, handover, and No Show return.
- Existing artifacts that reference `Stock Reservation`, `Inventory Issue`, or inventory-owned fulfillment status must be updated.

### Implementation guidance

- Request stock effects only after Pharmacy business facts are authorized.
- Use idempotent Integration Tasks keyed to Dispensing Item and movement purpose.
- Do not persist `Prepared`, `Handed Over`, or No Show in Stock Ledger.
- Evaluate Dispense Authorized from financial/coverage evidence (BA-08) before Dispensing Started; handover gates remain separate.
- Reference `apotek-domain.md` (`BR-APT-098`–`BR-APT-104`) and `stok-ledger-domain.md` §1.7.

## Related decisions

- [ADR-APT-001 Queue Boundary and Pharmacy Workflow State Ownership](./ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md)
- BA-07 Integration Task Table
- BA-08 Dispense Authorized policy evaluation
- BA-09 Inventory fulfillment contract (resolved by this ADR)
