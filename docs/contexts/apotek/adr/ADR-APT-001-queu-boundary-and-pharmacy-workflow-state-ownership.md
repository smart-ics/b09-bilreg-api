# ADR-APT-001 Queue Boundary and Pharmacy Workflow State Ownership

**Status:** Accepted
**Date:** 2026-08-15
**Owner:** Architecture Team

## Context

Outpatient Pharmacy (Apotek Rawat Jalan) requires operational progress tracking beyond the generic Patient Tracker queue lifecycle.

Pharmacy operational progress is owned by Pharmacy aggregates, not by the queue. Canonical Dispense Order states (`apotek-domain.md` §8.4) are:

```text
Established
Awaiting Clearance
Released
Preparing
Prepared
Reviewed
Completed
Cancelled
Expired
Unfulfilled
```

Related Pharmacy-owned facts include Telaah Resep, Sales Order, Sales Invoice, Dispense Authorized, Dispensing Temporary Custody, Medication Handover, Pickup Expired, and No Show. These labels are not queue statuses and must not be added to `AntrianStatusEnum`.

The existing Patient Tracker queue implementation only supports the following queue lifecycle:

```csharp
Waiting
InService
Done
Withdrawn
```

This lifecycle is intentionally generic and currently shared by Admission Queue operations. 

The queue aggregate only models queue movement:

```text
Waiting -> InService -> Done
Waiting -> Withdrawn
```

and records queue timestamps such as:

* CreatedAt
* ServedAt
* DoneAt
* WithdrawnAt

without any knowledge of Pharmacy-specific activities. 

---

## Decision

Pharmacy workflow states SHALL NOT be added to `AntrianStatusEnum`.

The queue subsystem remains responsible only for:

* Queue Number
* Queue Lifecycle
* Calling
* Serving
* Queue Completion
* Queue Withdrawal

The queue subsystem MUST remain generic and reusable across multiple business domains.

The canonical queue statuses remain:

```csharp
Waiting
InService
Done
Withdrawn
```

No Pharmacy-specific state may be introduced into the queue aggregate.

Queue completion (`InService` → `Done`) may be triggered by Pickup Call or by No Show Resolution when that resolution runs before Pickup Call. Queue `Done` does not imply medication handover. Queue `Done` only means the queue service lifecycle has been completed. `DoneAt` is recorded when that completion occurs and is never reversed. This does not change ownership: Patient Tracker still owns queue identity and lifecycle; Pharmacy still owns No Show handling and fulfillment outcomes. Pharmacy Queue Close remains the separate `Waiting` → `Withdrawn` path and shall not be used after the queue is `InService`.

### Canonical outpatient-pharmacy queue identity (BA-01)

Patient Tracker `QueueEntry` is the sole canonical outpatient-pharmacy queue identity.

- F-09 `Apotek-Start` and `Apotek-Done` evidence remain reusable but must reference `QueueEntryId` from Patient Tracker.
- Legacy Farinv queue identity is deprecated and must not create active queue records.
- Historical Farinv queue data is read-only.
- No dual-active queue model is allowed.

---

## Ownership Boundary

### Patient Tracker / Queue Owns

```text
Queue Number
Queue Status
Call Count
CreatedAt
ServedAt
DoneAt
WithdrawnAt
Calling Workflow
Display Workflow
```

### Pharmacy Owns

```text
Telaah Resep
Sales Order
Sales Invoice
Dispense Authorized (policy evaluation; not an aggregate)
Dispense Order lifecycle
Medication Preparation
Dispensing Temporary Custody
Medication Handover
Pickup Expired (projection category)
No Show Handling
Pharmacy Operational Progress
```

Pharmacy ownership of these facts is unchanged. The labels above replace informal example names (`Sales Confirmation`, `Payment Confirmation`, `WaitingPayment`, `Paid`, `Dispensing`, `Dispensed`, `HandedOver`) with the canonical domain vocabulary. They remain Pharmacy-owned and are still not queue statuses.

---

## Required Modeling Approach

Pharmacy operational progress shall be modeled separately from the queue aggregate.

Examples:

```text
QueueEntry
    |
    | 1
    |
    +---- PharmacyWork
```

or

```text
QueueEntry
    |
    | 1
    |
    +---- OutpatientQueueMapping
                |
                +---- Pharmacy Progress
```

or another equivalent design that preserves the ownership boundary.

The exact implementation structure may vary.

The ownership rule does not.

---

## Rationale

### Preserve Generic Queue Infrastructure

Patient Tracker is intended to be reusable by:

* Admission
* Pharmacy
* Laboratory
* Radiology
* Cashier
* Future service units

Embedding Pharmacy states into queue statuses would couple the queue subsystem to Pharmacy business rules.

---

### Prevent State Explosion

The following are Pharmacy-owned Dispense Order states and related Pharmacy facts. They are not queue statuses:

```text
Established
Awaiting Clearance
Released
Preparing
Prepared
Reviewed
Completed
Cancelled
Expired
Unfulfilled
Medication Handover
No Show
Dispensing Temporary Custody
```

These labels do not describe queue movement.

They describe Pharmacy operations.

Therefore they belong to Pharmacy. They must not be added to `AntrianStatusEnum`.

---

### Maintain Clear Bounded Context Ownership

Queue Context answers:

> "Where is the patient in the queue?"

Pharmacy Context answers:

> "Where is the medication fulfillment process?"

These are different concerns and must remain separated.

---

## Consequences

### Allowed

The pairings below illustrate separated ownership. Pharmacy labels are Dispense Order states or Pharmacy-owned facts from `apotek-domain.md`. They are not queue statuses.

```text
Queue Status = InService

Dispense Order = Preparing
```

```text
Queue Status = InService

Dispense Order = Awaiting Clearance
```

```text
Queue Status = Done

Dispense Order = Completed
```

```text
Queue Status = Done

Dispense Order = Expired
Pharmacy fact = No Show
```

Queue `Done` does not imply Medication Handover. Medication may remain in Dispensing Temporary Custody after the queue is `Done`.

---

### Not Allowed

```text
Queue Status = Preparing
```

```text
Queue Status = Awaiting Clearance
```

```text
Queue Status = Completed
```

```text
Queue Status = Expired
```

These Dispense Order states must never be added to `AntrianStatusEnum`.

---

## Guidance for Future Implementations

Any future Pharmacy implementation:

* MUST reuse existing queue lifecycle.
* MUST NOT modify `AntrianStatusEnum`.
* MUST NOT add Pharmacy workflow states to Patient Tracker.
* MUST implement Pharmacy operational progress using Pharmacy-owned models.
* MUST preserve queue subsystem genericity.

This decision is considered architectural and should not be revisited unless the queue subsystem itself is redesigned.

---

**References**

* Patient Tracker Queue Excavation Report 
* `AntrianEntryModel.cs` 
* `AntrianStatusEnum.cs` 
* [Apotek Domain](../apotek-domain.md) — `BR-APT-097`; Dispense Order lifecycle §8.4; Dispensing Temporary Custody
* [Outpatient Apotek Screen and Aggregate Design](../outpatient-apotek-screen-and-aggregate-design.md) — §4.1
