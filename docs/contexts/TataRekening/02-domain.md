# 02-domain.md — Tata Rekening

## 0. Ubiquitous Language

| Term | Kind | Meaning |
|---|---|---|
| **Tata Rekening** | Bounded context | Financial responsibility domain |
| **Tata Rekening** | Aggregate root | One registration's financial authority and Billing Set |
| **Verifikator** | Actor | User executing verification, adjustment, finalization, carry-over workflow |
| **TrsBill** | Entity | Financial charge within the aggregate |

Bounded context and aggregate share the name **Tata Rekening**. No synonym aggregate (e.g. FinancialCase) is used.

When reading this document:

- **Verifikator** = human workflow actor.
- **Tata Rekening aggregate** = enforces rules, holds state, records responsibility.
- **Tata Rekening bounded context** = overall domain scope.

---

## 1. Domain Purpose

Tata Rekening is the hospital **patient financial responsibility domain**.

Its responsibility is:

* recognize financial charge,
* organize patient financial responsibility,
* verify financial correctness,
* apply financial adjustment,
* allocate charge ownership,
* finalize financial responsibility,
* decide carry-over from prior outstanding receivables,
* distribute payment settlement,
* generate financial decomposition.

Tata Rekening separates:

```text
Operational Truth
≠
Financial Truth
```

Operational subsystems (Charge Source) own:

* medical activity,
* workflow execution,
* operational history.

The **Tata Rekening bounded context** owns:

* financial charge recognition,
* financial responsibility,
* financial verification rules,
* financial adjustment capability,
* financial allocation,
* financial finalization,
* settlement decomposition orchestration.

---

## 2. Domain Boundary

The **Tata Rekening bounded context** owns:

```text
financial truth (registration-scoped)
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
* finalization allocation,
* payment allocation,
* carry-over responsibility.

Tata Rekening does **not** own:

* clinical workflow,
* operational audit trail,
* medical history,
* inventory stock movement,
* patient-level outstanding receivable collection (PasienBalance),
* cash collection (Cashier),
* journal posting (Accounting).

---

## 3. Neighboring Context: PasienBalance

PasienBalance is a **separate bounded context**. Tata Rekening consumes its information but does not own the aggregate.

```text
PasienBalance
    ↓
Current Outstanding Receivable (per RegId)
    ↓
consumed by
    ↓
Tata Rekening (carry-over recording)
```

| Aspect | PasienBalance | Tata Rekening aggregate |
|---|---|---|
| Scope | one patient, many registrations | one registration |
| Owns | current unsettled receivable entries | financial responsibility for active registration |
| Carry-over | never performs carry-over | records carry-over after Verifikator selection |
| Entry model | one `OutstandingEntry` per unsettled `RegId` | Billing Set of `TrsBill` for current registration |

Interaction rules:

* one patient may have multiple outstanding registrations in PasienBalance,
* at most one outstanding entry per `RegId` within PasienBalance,
* when a new registration opens, the workflow loads PasienBalance for Verifikator review,
* Verifikator selects which entries to carry over,
* the Tata Rekening aggregate records new financial responsibility for the current registration,
* PasienBalance entries are updated or removed via PasienBalance domain operations — initiated by Tata Rekening workflow, executed against PasienBalance aggregate.

Aggregate ownership remains separate. PasienBalance must not reference Tata Rekening, TrsBill, or other billing aggregates internally.

---

## 4. Aggregate Structure

The Tata Rekening bounded context contains one aggregate root and one charge entity:

```text
Tata Rekening (aggregate — financial authority per registration)
TrsBill (entity — individual financial charge)
```

They participate in different responsibilities.

```text
TrsBill
```

represents an individual financial charge.

```text
Tata Rekening
```

is the aggregate root — patient financial authority for one registration.

Every TrsBill belongs to exactly one Tata Rekening aggregate.

A Tata Rekening aggregate maintains the **Billing Set** for its registration.

The Billing Set is required because financial lifecycle operations are performed at registration scope, not individual bill scope.

Examples of registration-scoped operations:

```text
Finalize Financial Responsibility
Payment
Cancel Finalization
Close Billing
ReOpen
```

always operate against the complete Billing Set belonging to the Tata Rekening aggregate.

Bill creation remains independent and may occur without loading the entire Billing Set.

### Implementation note

Current implementation may map the aggregate to `TataRekeningModel` backed by `BILRG_TrsBillingRegister`. That is implementation vocabulary — the business aggregate name remains **Tata Rekening**.

---

## 5. Tata Rekening Aggregate

Represents: **Patient Financial Authority (per registration)** — the aggregate root of this bounded context.

The aggregate enforces:

* billing lifecycle,
* financial verification rules,
* financial adjustment recording,
* financial finalization rules,
* carry-over recording (after Verifikator selection),
* payment orchestration,
* allocation orchestration,
* financial validation,
* billing set ownership.

The aggregate contains the Billing Set belonging to a registration.

The Billing Set represents all TrsBill participating in the patient financial lifecycle for that registration.

Tata Rekening does not own:

* pricing snapshot (immutable on TrsBill),
* tariff component snapshot,
* operational transaction details,
* PasienBalance outstanding state.

Those remain the responsibility of TrsBill, Charge Source, or PasienBalance respectively.

The aggregate determines:

```text
which bills participate
in finalization and payment processing.
```

---

## 6. Tata Rekening Lifecycle

```text
OPEN
    ↓
CLOSED
    ↓
FINALIZED
    ↓
LUNAS
```

This lifecycle is **lifecycle-neutral** — shared by Rawat Jalan and Rawat Inap.

Operational events (inpatient operational discharge, outpatient completion) may trigger **Close Billing** but do not redefine the financial states.

---

### OPEN

Allowed:

* create bill,
* delete bill,
* modify operational billing source,
* financial adjustment,
* carry-over from PasienBalance (new financial responsibility creation).

---

### CLOSED

No new bill may be created.

Existing bills remain unchanged.

Purpose:

```text
operational freeze point
```

---

### FINALIZED

Triggered when the Verifikator completes:

```text
FinalizeFinancialResponsibility()
```

after financial verification and allocation input.

Represents:

```text
financial responsibility allocation completed
```

At this state:

* all receivable ownership has been allocated,
* bill responsibility becomes fixed,
* finalization may be cancelled only if no payment exists (**Cancel Finalization**).

Inpatient **operational discharge** may precede this step as part of the wider workflow — it is not the financial lifecycle state.

---

### LUNAS

All financial responsibility has been converted into cash settlement.

No further modification allowed.

---

## 7. Tata Rekening Aggregate Responsibilities

The aggregate enforces:

```text
Create Bill Control
Delete Bill Control
Close Billing
ReOpen
Financial Verification (rules)
Financial Adjustment (recording)
Finalize Financial Responsibility
Cancel Finalization
Carry-Over Recording
Payment Allocation
Lifecycle Validation
```

The Verifikator executes the corresponding workflow steps; the aggregate validates and transitions state.

Aggregate roles:

```text
Financial Authority
Verification Gate
Adjustment Recorder
Allocation Orchestrator
Finalization Controller
Lifecycle Controller
```

Important distinction:

Bill creation may occur independently through CreateBillService without loading the complete Billing Set.

However:

```text
CloseBilling
ReOpen
FinalizeFinancialResponsibility
CancelFinalization
Payment
```

always operate against the Billing Set owned by the Tata Rekening aggregate.

These operations are aggregate-wide operations.

---

## 8. Carry Over

Carry over connects PasienBalance to Tata Rekening.

```text
Open Registration
    ↓
Load PasienBalance
    ↓
Outstanding Entries
    ↓
Verifikator selects carry-over
    ↓
Tata Rekening aggregate records financial responsibility (TrsBill / adjustment)
    ↓
PasienBalance updated
```

Rules:

* Verifikator selects which outstanding receivables to carry over,
* PasienBalance never performs carry-over itself,
* carry-over materializes as financial charge or adjustment while aggregate status is OPEN,
* consumed PasienBalance entries are updated or removed through PasienBalance domain operations.

---

## 9. TrsBill Aggregate

Represents: **Financial Charge Entry**

TrsBill is the authoritative representation of a financial charge.

Stores:

* charge identity,
* pricing snapshot,
* accounting snapshot,
* source reference,
* charge amount,
* component snapshot.

Every TrsBill belongs to exactly one Tata Rekening.

A TrsBill may be created independently.

However, once associated with a Tata Rekening, it participates in the Tata Rekening financial lifecycle.

Persistence artifact: `ta_trs_billing`.

---

## 10. TrsBill Characteristics

| Characteristic | Value |
|---|---|
| Financial Authority | YES |
| Pricing Snapshot | Immutable |
| Accounting Snapshot | Immutable |
| Independently Creatable | YES |
| Independently Finalizable | NO |
| Independently Payable | NO |
| Independently Allocatable | NO |

A TrsBill cannot:

* finalize financial responsibility itself,
* pay itself,
* reopen itself,
* perform registration-scoped allocation itself.

These operations belong exclusively to the Tata Rekening aggregate because they require visibility over the complete Billing Set.

---

## 11. Bill Components

TrsBill contains financial breakdown information.

Three business component types exist:

```text
Transaction
Finalization
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

### Finalization Component

Represents **financial responsibility allocation** result.

Generated from:

```text
Transaction Component
```

using:

```text
Financial Responsibility Allocation
```

Each finalization component is a copy of transaction component with:

* payer ownership,
* proportional value allocation.

---

### Payment Component

Represents settlement allocation result.

Generated from:

```text
Finalization Component
```

using:

```text
Payment Allocation
```

Each payment component is a copy of finalization component with:

* payment ownership,
* proportional settlement value.

---

## 12. Allocation Model

Allocation occurs in two levels.

---

### Level 1

Tata Rekening aggregate allocation

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

Allocation authority source (persistence): `ta_registrasi3`.

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

## 13. Allocation Partition Rule

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

This is a domain invariant.

---

## 14. Finalization Invariant

Financial finalization must allocate:

```text
100%
```

of outstanding receivable.

Rule:

```text
Σ Allocation = Σ Outstanding Bill
```

always.

If a payer cannot cover the amount:

```text
SUBSIDI RS
```

or another responsibility allocation must be added.

Partial finalization is not allowed.

---

## 15. Payment Model

Payment may occur multiple times.

Example:

```text
Payment-1
Payment-2
Payment-3
```

Each payment distributes value proportionally across finalized responsibility.

The aggregate orchestrates payment allocation across the Billing Set.

TrsBill owns payment decomposition at charge level.

Cashier executes collection.

---

## 16. Cancel Finalization

Allowed only when:

```text
No Payment Exists
```

Process:

```text
Remove Finalization Allocation
Regenerate Finalization Components
```

Revert aggregate status FINALIZED → CLOSED when appropriate.

---

## 17. Bill Deletion

Bill deletion is allowed only while:

```text
Tata Rekening = OPEN
```

Deletion is physical removal.

Billing history is owned by operational source subsystem.

Tata Rekening does not preserve deleted billing history.

---

## 18. Financial Adjustment

The Tata Rekening bounded context owns **financial adjustment** as a business capability.

The Verifikator presents corrected or supplemental values through workflow. The aggregate records them. Operational correction remains at the Charge Source.

Examples:

| Adjustment type | Typical manifestation |
|---|---|
| manual charge | additional TrsBill |
| manual correction | additional or regenerated charge |
| waive | allocation or charge adjustment |
| subsidy | payer responsibility shift |
| carry-over adjustment | TrsBill from PasienBalance selection |
| billing correction | recalculation while OPEN |

Tata Rekening aggregate processes financial values provided through workflow. Charge Source retains operational truth.

---

## 19. Authority Matrix

| Domain / Actor | Authority |
|---|---|
| Charge Source | operational activity |
| Tarif | tariff and pricing policy |
| PasienBalance | patient outstanding receivable collection |
| Verifikator | verification, adjustment, finalization, carry-over selection workflow |
| TrsBill | financial charge snapshot |
| **Tata Rekening** (aggregate) | enforces verification, adjustment, allocation, finalization, carry-over recording, lifecycle |
| Cashier | payment processing |
| Accounting | settlement execution (journals) |

---

## 20. Final Domain Position

Tata Rekening is:

```text
Patient Financial Responsibility Domain
```

consisting of:

```text
Tata Rekening (aggregate)
    → Financial Authority
    → Billing Set Owner
    → Verification & Finalization Controller
    → Carry-Over Recorder

TrsBill
    → Financial Charge
    → Pricing Snapshot
    → Financial Decomposition

Verifikator
    → Workflow actor for verification, adjustment, finalization, carry-over
```

where:

```text
Tata Rekening aggregate owns the patient billing lifecycle (per registration)
Tata Rekening aggregate owns the Billing Set
TrsBill represents individual financial charges
TrsBill decomposes financial allocation at charge level
PasienBalance supplies outstanding context; Verifikator selects carry-over; aggregate records it
```

Important distinction:

```text
Creation Scope ≠ Lifecycle Scope
```

Bill creation may occur independently.

Finalize Financial Responsibility, Payment, Cancel Finalization, Close Billing, and ReOpen always occur at Tata Rekening aggregate scope and operate against the complete Billing Set.
