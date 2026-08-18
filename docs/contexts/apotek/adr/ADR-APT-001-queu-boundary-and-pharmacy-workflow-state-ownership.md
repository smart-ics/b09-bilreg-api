Saya menyarankan membuat artifact sebagai **Architecture Decision Record (ADR)** supaya agent masa depan menganggap ini sebagai keputusan yang sudah dikunci, bukan sekadar diskusi.

---

# ADR-APT-001 Queue Boundary and Pharmacy Workflow State Ownership

**Status:** Accepted
**Date:** 2026-08-15
**Owner:** Architecture Team

## Context

Outpatient Pharmacy (Apotek Rawat Jalan) requires operational progress tracking beyond the generic Patient Tracker queue lifecycle.

Examples of Pharmacy workflow states include:

* Telaah Resep
* Sales Confirmation
* Payment Confirmation
* Dispensing
* Dispensed
* Medication Handover
* No Show
* Expired

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
Prescription Review
Sales Confirmation
Payment Confirmation
Dispensing
Medication Preparation
Medication Handover
Pickup Expiration
No Show Handling
Pharmacy Operational Progress
```

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

The following are Pharmacy workflow states:

```text
Reviewed
WaitingPayment
Paid
Dispensing
Dispensed
HandedOver
Expired
NoShow
```

These states do not describe queue movement.

They describe Pharmacy operations.

Therefore they belong to Pharmacy.

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

```text
Queue Status = InService

Pharmacy Status = Dispensing
```

```text
Queue Status = InService

Pharmacy Status = WaitingPayment
```

```text
Queue Status = Done

Pharmacy Status = HandedOver
```

---

### Not Allowed

```text
Queue Status = Dispensing
```

```text
Queue Status = WaitingPayment
```

```text
Queue Status = HandedOver
```

These statuses must never be added to `AntrianStatusEnum`.

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

Saya juga menyarankan menaruh file ini di:

```text
docs/architecture/adr/ADR-APT-001-queue-boundary-and-pharmacy-workflow-state-ownership.md
```

agar agent implementasi dan reviewer dapat menemukannya sebagai keputusan arsitektur yang sudah final.
