# 02-domain.md — TRSBILLING

# 1. Domain Purpose

TRSBILLING is:

# Hospital Operational Financial Receivable Ledger

responsible for:
- billing recognition,
- receivable allocation,
- billing finalization,
- payment settlement,
- accounting projection source.

TRSBILLING receives financial charge from:
- operational subsystem,
- pharmacy,
- inventory,
- room charge,
- external workflow subsystem.

---

# 2. Domain Boundary

TRSBILLING owns:

```text
financial truth
```

Operational subsystem owns:

```text
operational truth
```

---

## Operational Truth

Represents:
- medical activity,
- workflow execution,
- operational history,
- operational correction.

Examples:
- tindakan,
- room occupancy,
- transport usage,
- drug dispensing.

---

## Financial Truth

Represents:
- receivable,
- billing responsibility,
- settlement state,
- accounting projection source.

TRSBILLING does NOT own:
- medical workflow,
- operational audit trail,
- clinical history.

---

# 3. Aggregate Boundary

TRSBILLING aggregate exists at:

# Registration Scope

because:
- payer allocation is registration-based,
- finalize/reopen is registration-based,
- settlement is registration-based,
- financial responsibility is registration-based.

Aggregate root:

```text
TrsBillingRegister
```

---

# 4. Aggregate Composition

| Model | Responsibility |
|---|---|
| `TrsBillingRegister` | lifecycle orchestration aggregate |
| `BillingCharge` | financial charge entry |
| `BillingComponent` | financial distribution projection |
| `PayerAllocation` | payer responsibility authority |

---

# 5. Aggregate Persistence

| Model | Persistence |
|---|---|
| `TrsBillingRegister` | `BILRG_TrsBillingRegister` |
| `BillingCharge` | `ta_trs_billing` |
| `BillingComponent` | `ta_trs_billing2` |
| `PayerAllocation` | `ta_registrasi3` |

---

# 6. TrsBillingRegister

Represents:

# Registration Financial Lifecycle

Responsibilities:
- billing state,
- finalize state,
- reopen state,
- settlement state,
- orchestration boundary.

---

## Billing Lifecycle

```text
OPEN
    ↓
CLOSE BILL
    ↓
FINALIZED
    ↓
PAID
```

---

## OPEN

Billing still mutable.

Allowed:
- add/remove charge,
- allocation recalculation,
- projection regeneration.

---

## CLOSE BILL

Operational freeze point.

No more operational billing generation allowed.

---

## FINALIZED

Billing verified and ready for:
- cashier,
- settlement,
- accounting projection.

Allocation becomes locked.

---

## PAID

Payment completed.

Billing becomes financially frozen.

---

# 7. BillingCharge

Represents:

# Authoritative Financial Charge

Persistence:

```text
ta_trs_billing
```

Stores:
- registration,
- tarif,
- pricing snapshot,
- source transaction snapshot,
- financial amount,
- operational reference.

---

## Characteristics

| Characteristic | Value |
|---|---|
| authoritative | YES |
| mutable before finalize | YES |
| mutable after payment | NO |
| pricing immutable | YES |
| accounting snapshot immutable | YES |

---

# 8. BillingComponent

Represents:

# Financial Distribution Projection

Persistence:

```text
ta_trs_billing2
```

---

## BillingComponent is NOT

- event sourcing history,
- accounting journal,
- immutable mutation stream,
- payment history.

---

## BillingComponent Represents

- tarif component distribution,
- payer allocation distribution,
- receivable ownership transfer,
- accounting-ready decomposition.

---

## Projection Semantics

### `FN_TRS_P`

Represents:

```text
receivable acquisition
```

---

### `FN_TRS_N`

Represents:

```text
receivable release
ownership transfer
```

NOT:
- debit/credit,
- positive/negative accounting value.

---

# 9. PayerAllocation

Represents:

# Registration-Level Financial Responsibility

Persistence:

```text
ta_registrasi3
```

Defines:
- who financially pays,
- payer distribution,
- guarantor allocation.

Allocation authority exists at:
- registration scope,
- not billing-row scope.

---

# 10. Financial Allocation Strategy

Allocation uses:

# proportional decomposition

across:
- billing component,
- payer allocation,
- accounting projection.

Allocation stores:

```text
absolute currency value
```

NOT:
- percentage responsibility.

---

# 11. Composite Charge

TRSBILLING supports:

- bundled charge,
- package billing,
- multi-component charge,
- compressed commercial representation.

Reason:

```text
Operational Atomicity
≠
Commercial Atomicity
```

One operational workflow may produce:
- single commercial charge,
- multiple financial charge,
- composite billing decomposition.

---

# 12. Source Transaction

Every BillingCharge must reference:

# operational charge source

Examples:
- tindakan,
- room charge,
- pharmacy,
- transport,
- external subsystem charge.

TRSBILLING requires:
- operational traceability,
- source idempotency,
- retry-safe charge generation.

---

# 13. Pricing Snapshot

Pricing resolved at:

# transaction time

Snapshot becomes immutable.

Changes in:
- tarif,
- patient class,
- payer configuration,
- pricing policy

must NOT alter historical billing.

---

# 14. Accounting Snapshot

Accounting mapping resolved at:

# transaction time

Snapshot becomes immutable.

Changes in:
- COA mapping,
- accounting configuration,
- tarif-account mapping

must NOT alter historical projection.

---

# 15. Reopen Semantics

Reopen allowed only before payment settlement.

Reopen strategy:

```text
DELETE projection
→ REGENERATE projection
```

because:
- billing2 is derived projection,
- not immutable financial history.

---

# 16. Void Semantics

Before finalize/payment:
- physical delete allowed.

Operational correction responsibility remains in:
- source subsystem,
- not TRSBILLING history.

---

# 17. Accounting Position

TRSBILLING is:

# Accounting Projection Source

NOT:
- accounting journal engine,
- general ledger,
- financial reporting engine.

Accounting journal generation:
- asynchronous,
- externalized,
- accounting-owned.

---

# 18. Integration Pattern

Integration pattern:

```text
Subsystem
→ Charge Request
→ TRSBILLING
```

Subsystem:
- MUST NOT manipulate billing persistence directly,
- MUST NOT manipulate billing allocation directly,
- MUST NOT own financial receivable state.

---

# 19. Authority Matrix

| Domain | Authority |
|---|---|
| Operational Subsystem | operational activity |
| Pricing | tarif & pricing policy |
| TRSBILLING | financial receivable |
| Tata Rekening | billing verification |
| Cashier | settlement |
| Accounting | journal generation |

---

# 20. Legacy Compatibility Principle

TRSBILLING prioritizes:

# compatibility over architectural purity

Therefore:
- existing billing workflow preserved,
- existing accounting integration preserved,
- existing posting strategy preserved,
- migration remains incremental.

New orchestration aggregate exists to:
- clarify aggregate boundary,
- simplify lifecycle handling,
- improve orchestration consistency,
- reduce modeling ambiguity.

NOT to replace legacy financial tables.

---

# 21. Final Domain Positioning

TRSBILLING is:

# Registration-Scoped Operational Financial Receivable Aggregate

that:
- receives financial recognition,
- stores immutable pricing/accounting snapshot,
- orchestrates billing lifecycle,
- manages payer allocation,
- supports settlement,
- generates accounting projection,
- while preserving legacy HIS compatibility.