# 02-domain.md — TRSBILLING

## 1. Domain Purpose

TRSBILLING is the hospital financial charge and allocation domain.

Its responsibility is:

* recognize financial charge,
* organize patient financial responsibility,
* allocate charge ownership,
* finalize billing responsibility,
* distribute payment settlement,
* generate financial decomposition.

TRSBILLING separates:

```text
Operational Truth
≠
Financial Truth
```

Operational subsystems own:

* medical activity,
* workflow execution,
* operational history.

TRSBILLING owns:

* financial charge,
* financial responsibility,
* financial allocation,
* financial settlement decomposition.

---

## 2. Domain Boundary

TRSBILLING owns:

```text
financial truth
```

Operational subsystems own:

```text
operational truth
```

Examples of operational truth:

* tindakan,
* room charge,
* pharmacy transaction,
* transport transaction,
* inventory usage.

Examples of financial truth:

* billable charge,
* payer responsibility,
* discharge allocation,
* payment allocation.

TRSBILLING does not own:

* clinical workflow,
* operational audit trail,
* medical history,
* inventory stock movement.

---

## 3. Aggregate Structure

TRSBILLING consists of two business concepts:

```text
TataRekening
TrsBill
```

However, they participate in different responsibilities.

```text
TrsBill
```

represents an individual financial charge.

```text
TataRekening
```

represents the patient financial authority for a registration.

Every TrsBill belongs to exactly one TataRekening.

A TataRekening maintains the Billing Set for its registration.

The Billing Set is required because financial lifecycle operations are performed at registration scope, not individual bill scope.

Examples:

```text
Discharge
Payment
CancelDischarge
CloseBilling
ReOpen
```

always operate against the complete Billing Set belonging to the TataRekening.

Bill creation remains independent and may occur without loading the entire Billing Set.

---

## 4. TataRekening Aggregate

Represents: **Patient Financial Authority**

TataRekening owns:

* billing lifecycle,
* discharge authority,
* payment authority,
* allocation orchestration,
* financial validation,
* billing set ownership.

TataRekening contains the Billing Set belonging to a registration.

The Billing Set represents all TrsBill participating in the patient financial lifecycle.

TataRekening does not own:

* pricing snapshot,
* tariff component snapshot,
* operational transaction details.

Those remain the responsibility of each TrsBill.

TataRekening is the authority that determines:

```text
which bills participate
in discharge and payment processing.
```

---

## 5. TataRekening Lifecycle

```text
OPEN
    ↓
CLOSED
    ↓
FINALIZED
    ↓
LUNAS
```

---

### OPEN

Allowed:

* create bill,
* delete bill,
* modify operational billing source.

---

### CLOSED

No new bill may be created.

Existing bill remains unchanged.

Purpose:

```text
operational freeze point
```

---

### FINALIZED

Triggered by:

```text
Discharge()
```

Discharge represents:

```text
financial responsibility allocation
```

At this state:

* all receivable ownership has been allocated,
* bill responsibility becomes fixed,
* discharge may be cancelled only if no payment exists.

---

### LUNAS

All financial responsibility has been converted into cash settlement.

No further modification allowed.

---

## 6. TataRekening Responsibilities

TataRekening owns:

```text
Create Bill Control
Delete Bill Control
Close Billing
ReOpen
Discharge
Cancel Discharge
Payment Allocation
Lifecycle Validation
```

TataRekening acts as:

```text
Financial Authority
Allocation Orchestrator
Lifecycle Controller
```

Important distinction:

Bill creation may occur independently through CreateBillService without loading the complete Billing Set.

However:

```text
CloseBilling
ReOpen
Discharge
CancelDischarge
Payment
```

always operate against the Billing Set owned by TataRekening.

These operations are aggregate-wide operations.

---

## 7. TrsBill Aggregate

Represents: **Financial Charge Entry**

TrsBill is the authoritative representation of a financial charge.

Stores:

* charge identity,
* pricing snapshot,
* accounting snapshot,
* source reference,
* charge amount,
* component snapshot.

Every TrsBill belongs to exactly one TataRekening.

A TrsBill may be created independently.

However, once associated with a TataRekening, it participates in the TataRekening financial lifecycle.

---

## 8. TrsBill Characteristics

| Characteristic              | Value     |
| --------------------------- | --------- |
| Financial Authority         | YES       |
| Pricing Snapshot            | Immutable |
| Accounting Snapshot         | Immutable |
| Independently Creatable     | YES       |
| Independently Dischargeable | NO        |
| Independently Payable       | NO        |
| Independently Finalizable   | NO        |

A TrsBill cannot:

* discharge itself,
* pay itself,
* finalize itself,
* reopen itself.

These operations belong exclusively to TataRekening because they require visibility over the complete Billing Set.

---

## 9. Bill Components

TrsBill contains financial breakdown information.

Three business component types exist:

```text
Transaction
Discharge
Payment
```

---

### Transaction Component

Represents original charge decomposition.

Example:

```text
Jasa Medis
Jasa Rumah Sakit
Obat
BHP
```

Resolved at transaction time.

Immutable.

---

### Discharge Component

Represents financial responsibility allocation result.

Generated from:

```text
Transaction Component
```

using:

```text
Discharge Allocation
```

Each discharge component is a copy of transaction component with:

* payer ownership,
* proportional value allocation.

---

### Payment Component

Represents settlement allocation result.

Generated from:

```text
Discharge Component
```

using:

```text
Payment Allocation
```

Each payment component is a copy of discharge component with:

* payment ownership,
* proportional settlement value.

---

## 10. Allocation Model

Allocation occurs in two levels.

---

### Level 1

TataRekening Allocation

Distributes responsibility from:

```text
Payment Provider
```

to:

```text
TrsBill
```

Examples:

```text
BPJS
KAS
SUBSIDI RS
ASURANSI
```

---

### Level 2

TrsBill Allocation

Distributes responsibility from:

```text
Bill Share
```

to:

```text
Bill Components
```

using proportional calculation.

---

## 11. Allocation Partition Rule

Every TrsBill belongs to a module group:

```text
JASA
OBAT
```

Allocation must occur within the same group.

Example:

```text
BPJS JASA
```

may only be distributed to:

```text
JASA bills
```

and never to:

```text
OBAT bills
```

Likewise:

```text
BPJS OBAT
```

may only be distributed to:

```text
OBAT bills
```

This is a domain invariant.

---

## 12. Discharge Invariant

Discharge must allocate:

```text
100%
```

of outstanding receivable.

Rule:

```text
Σ Allocation
=
Σ Outstanding Bill
```

always.

If a payer cannot cover the amount:

```text
SUBSIDI RS
```

or another responsibility allocation must be added.

Partial discharge is not allowed.

---

## 13. Payment Model

Payment may occur multiple times.

Example:

```text
Payment-1
Payment-2
Payment-3
```

Each payment distributes value proportionally across discharged responsibility.

TataRekening owns payment orchestration.

TrsBill owns payment decomposition.

---

## 14. Cancel Discharge

Allowed only when:

```text
No Payment Exists
```

Process:

```text
Remove Discharge Allocation
Regenerate Discharge Components
```

---

## 15. Bill Deletion

Bill deletion is allowed only while:

```text
TataRekening = OPEN
```

Deletion is physical removal.

Billing history is owned by operational source subsystem.

TRSBILLING does not preserve deleted billing history.

---

## 16. Financial Adjustment

TRSBILLING allows financial adjustment through additional bill creation.

Examples:

```text
Pendapatan BPJS
```

or other adjustment charge.

Adjustment responsibility belongs to user workflow.

TataRekening only processes financial values provided to it.

---

## 17. Authority Matrix

| Domain                | Authority               |
| --------------------- | ----------------------- |
| Operational Subsystem | operational activity    |
| Pricing               | tariff & pricing policy |
| TrsBill               | financial charge        |
| TataRekening          | allocation & lifecycle  |
| Accounting            | settlement execution    |
| Cashier               | payment processing      |

---

## 18. Final Domain Position

TRSBILLING is:

```text
Patient Financial Allocation Domain
```

consisting of:

```text
TataRekening
    → Financial Authority
    → Billing Set Owner
    → Lifecycle Controller

TrsBill
    → Financial Charge
    → Pricing Snapshot
    → Financial Decomposition
```

where:

```text
TataRekening
    owns the patient billing lifecycle

TataRekening
    owns the Billing Set

TrsBill
    represents individual financial charges

TrsBill
    decomposes financial allocation
```

Important distinction:

```text
Creation Scope
≠
Lifecycle Scope
```

Bill creation may occur independently.

Discharge, Payment, CancelDischarge, CloseBilling, and ReOpen always occur at TataRekening scope and operate against the complete Billing Set.
