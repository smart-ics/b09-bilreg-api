# Rawat Inap Admission Architecture

The authoritative semantic contract for collaboration with RNA is [`docs/contexts/bangsal/ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md). This document defines Admisi architecture and ownership only; it does not duplicate the cross-context contract.

---

## 1. Architecture Overview

### Architectural Style

The Rawat Inap Admission module is implemented as a Modular Monolith using Clean Architecture with Vertical Slice application services.

The architecture realizes the business model defined in `admisi-ranap-domain.md` by separating business capabilities into independent application modules while preserving aggregate boundaries.

Dependencies flow inward:

```
Presentation

↓

Application

↓

Domain

↓

Infrastructure
```

The Domain layer remains independent of infrastructure and operational concerns.

---

### Architectural Principles

- One Aggregate Root per business consistency boundary.
- One Application Use Case realizes one business capability.
- Cross-aggregate collaboration occurs through application services.
- RNA Accommodation Assignment is outside this module.
- Waiting List is realized as an independent aggregate.

---

## 2. Feature Boundaries

### Opname Request Feature

Responsibilities

- Clinical admission request
- Opname Request lifecycle

Depends on

- Admission Module

---

### Reservation Feature

Responsibilities

- Planned admission
- Reservation lifecycle

Independent from Opname Request — no cross-aggregate reference at domain or persistence level.

Depends on

- Admission Module

---

### Admission Feature

Responsibilities

- Administrative admission
- Admission lifecycle

Produces

- Waiting List creation request

Admission never performs accommodation.

---

### Waiting List Feature

Responsibilities

- Waiting List lifecycle
- Accommodation request management
- Publish or expose Admisi-owned demand to RNA only through the approved integration contract

Consumes

- Admission

Produces

- Admisi-owned Waiting List demand for RNA, under the approved integration contract

---

## 3. Use Cases

### Create Opname Request

Purpose

Create a clinical hospitalization request.

Primary Aggregate

Opname Request

Outcome

Opname Request created.

---

### Cancel Opname Request

Purpose

Cancel a clinical hospitalization request.

Primary Aggregate

Opname Request

Outcome

Opname Request cancelled.

---

### Create Reservation

Purpose

Plan a future inpatient admission.

Primary Aggregate

Reservation

Outcome

Reservation created.

---

### Maintain Reservation

Purpose

Maintain reservation information.

Primary Aggregate

Reservation

Outcome

Reservation updated.

---

### Process Opname Request Admission

Purpose

Administratively admit a patient from a clinical Opname Request.

Operator selects Care Class (`KelasDk`); system validates against `ta_kelas_dk`. System loads Bangsal eligible for the selected Care Class. Operator selects destination Bangsal from the filtered list.

Primary Aggregate

Admission

Supporting Aggregate

Opname Request (fulfilled in the same application transaction)

Outcome

Admission created; Opname Request fulfilled.

---

### Process Reservation Admission

Purpose

Administratively admit a patient from a planned Reservation.

Operator selects Care Class (`KelasDk`); system validates against `ta_kelas_dk`. System loads Bangsal eligible for the selected Care Class. Operator selects destination Bangsal from the filtered list.

Primary Aggregate

Admission

Supporting Aggregate

Reservation (realized in the same application transaction; auto-maintained when still `Reserved`)

Outcome

Admission created; Reservation realized.

---

### Update Admission

Purpose

Maintain administrative admission information.

Care Class may be changed; system reloads eligible Bangsal. If the current Bangsal is no longer eligible, selection is cleared; save is blocked until a new Bangsal is chosen.

Primary Aggregate

Admission

Outcome

Admission updated.

---

### Cancel Admission

Purpose

Cancel an administrative admission.

Primary Aggregate

Admission

Outcome

Admission cancelled.

---

### Create Waiting List

Purpose

Create an accommodation request for an admitted patient.

Primary Aggregate

Waiting List

Outcome

Waiting List created.

---

### Update Waiting List

Purpose

Maintain accommodation requirements while waiting.

Primary Aggregate

Waiting List

Outcome

Waiting List updated.

---

### Close Waiting List

Purpose

Apply the Admisi-owned Waiting List transition caused by a valid RNA `AccommodationAssigned` fact.

Primary Aggregate

Waiting List

Outcome

Waiting List closed according to [`ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md).

---

## 4. Aggregate Realization

### Opname Request

Aggregate Root

Owns

- Clinical request information
- Request status

Consistency Boundary

Clinical admission request.

---

### Reservation

Aggregate Root

Owns

- Reservation information
- Reservation status
- Kelas Rawat (`KelasReff`)
- Destination Bangsal (`BangsalReff`)

Consistency Boundary

Admission planning.

---

### Admission

Aggregate Root

Owns

- Administrative admission information
- Admission status
- Reservation realization
- Opname Request fulfillment
- Care Class (`KelasDkId`, validated via `ta_kelas_dk`)
- Destination Bangsal (`BangsalId` / `BangsalReff`)

Does not own

- Waiting List
- `ta_kelas`
- Room
- Bed
- Accommodation

Consistency Boundary

Administrative admission.

---

### Waiting List

Aggregate Root

Owns

- Waiting status
- Waiting priority
- Kelas Rawat (`KelasReff`)
- Destination Bangsal (`BangsalReff`)

Does not own

- Room
- Bed
- Bed Assignment

Consistency Boundary

Accommodation request lifecycle.

---

## 5. Repository Strategy

One repository is provided for each Aggregate Root.

- OpnameRequestRepository
- ReservationRepository
- AdmissionRepository
- WaitingListRepository

Repositories reconstruct complete aggregates and persist aggregate state atomically.

Cross-aggregate updates are coordinated by the Application layer.

Repositories never coordinate business workflows.

Admission repository validates Care Class existence via the `ta_kelas_dk` master. Bangsal validation confirms the Bangsal is eligible for the given `KelasDkId` (via Room → `ta_kelas` → `ta_kelas_dk` mapping in infrastructure). Admission persistence does not reference `ta_kelas` directly.

---

## 6. API Philosophy

The module exposes REST APIs following Command-Query Separation.

Commands

- Create
- Update
- Cancel
- Fulfill
- Close

Queries

- Aggregate retrieval
- Waiting List retrieval
- Admission lookup

Application APIs expose business use cases rather than CRUD endpoints.

The Waiting List API exposes Admisi-owned accommodation demand. RNA-facing demand, placement-result, release, acknowledgement, and reconciliation semantics are defined only by [`ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md); a concrete API remains transport-specific implementation work.

---

## 7. Integration

### Doctor Services

Purpose

Provide clinical admission requests.

Communication

Synchronous.

---

### RNA Accommodation Assignment

Purpose

Collaborate on Admisi-owned Waiting List demand and RNA-owned Accommodation Assignment without sharing persistence or business ownership.

Communication

Defined only by [`ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md). The architecture is transport-agnostic.

Ownership

RNA owns Accommodation Assignment.

Admisi owns Waiting List identity, lifecycle, priority, cancellation, rerouting, and closure. Neither context directly modifies the other's persistence.

---

### Patient Administration

Purpose

Provide patient demographic information.

Communication

Synchronous.

---

## 8. Authentication & Authorization

Authentication

Authenticated application users.

Authorization

Permission-based authorization.

Permissions are granted according to business responsibilities.

Examples

- Process Opname Request Admission
- Process Reservation Admission
- Maintain Reservation
- Manage Waiting List

RNA accommodation permissions belong to RNA. Admisi authorizes its own Waiting List commands and integration consumer behavior.

---

## 9. Infrastructure

Infrastructure dependencies include

- Relational database
- REST API
- Authentication provider
- Audit logging

No particular messaging infrastructure is required. Reliable cross-context delivery, acknowledgement, idempotency, correction, and reconciliation must preserve [`ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md) and do not imply a specific transport.

Waiting List persistence is maintained independently from Admission persistence to support operational performance and decouple accommodation workflows.

---

## 10. Architectural Decisions (ADRs)

### ADR-001

Decision

Waiting List is implemented as an independent Aggregate Root.

Rationale

Waiting List has its own lifecycle, persistence, and business responsibility.

Consequence

Admission and Waiting List evolve independently.

---

### ADR-002

Decision

Admission never performs Room or Bed allocation.

Rationale

Accommodation belongs to the Ward domain.

Consequence

Admission ends after creating or updating a Waiting List.

---

### ADR-003

Decision

Waiting List is persisted independently from Admission.

Rationale

Operational accommodation requires efficient access to active waiting patients without scanning historical Admissions.

Consequence

Waiting List becomes the operational working set while Admission remains the permanent administrative record.

---

### ADR-004

Decision

RNA consumes Admisi-owned Waiting List demand rather than Admission.

Rationale

Waiting List is the Admisi-owned accommodation demand independent of how the Admission originated.

Consequence

The collaboration semantics, including inter-ward release consequences, are governed only by [`ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md). Admisi retains its Waiting List authority and RNA retains Accommodation Assignment authority.

---

### ADR-005 — Canonical Care Class

Decision

Admission uses `ta_kelas_dk` as the canonical Care Class (`KelasDkId`).

Rationale

`ta_kelas` is hospital-customizable and unsuitable as a stable business reference.

Consequence

Admission business logic becomes independent of hospital-specific room configuration. `ta_kelas` remains an infrastructure mapping between Room and Care Class.

---

### ADR-006 — Persistent workspace contract (documentation)

Decision

Frontend Registration is a persistent case-workspace body (Work List remains mounted). Post-admission edits use only Ready owner commands from the persistent-workspace capability matrix. Process admission accepts exactly one source. Bed Management remains outside Admisi and follows the approved Admisi ↔ RNA integration boundary.

Rationale

Aligns UX with `docs/WORKFLOW.md` Queue + Contextual Workspace, prevents uncertified outpatient command reuse, and removes documentation contradictions that drove wrong timeline/action rules.

Consequence

Phase 2 implements Propose rows (at minimum Waiting List by `regId`) before FE depends on them. Deferred rows stay read-only in the UI. See `admisi-ranap-persistent-workspace-capability-matrix.md` and the FE Phase 1 contract.

---

### ADR-007 — Admisi ↔ RNA Collaboration Uses the Approved Integration Contract

Decision

Admisi collaborates with RNA only through [`ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md). Admisi owns Waiting List; RNA owns Accommodation Assignment. The contract is transport-agnostic and remains the single source of truth for cross-context semantics.

Rationale

The collaboration requires stable identities, local-commit boundaries, acknowledgement, correction, and reconciliation semantics without creating shared ownership or shared persistence.

Consequence

Admisi implementation exposes or consumes only Application-boundary contracts that preserve the approved semantics. Existing no-op gateway behavior, durable delivery, acknowledgements, reconciliation, and contract acceptance tests are implementation work, not unresolved architecture design.

---

## 11. Cross-Context Traceability

| Admisi concern | Governing artifact | Admisi boundary |
|---|---|---|
| Waiting List demand, priority, cancellation, rerouting, and closure | [`ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md) | Admisi-owned lifecycle; RNA has no direct write authority |
| RNA Accommodation Assignment and corrections | [`ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md) | RNA-owned facts; Admisi records only its local consequence/reference |
| Inter-ward Accommodation Release | [`ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md) | Admisi applies its own Waiting List consequence; RNA creates no Admisi state |
| Delivery, acknowledgement, and reconciliation | [`ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md) | Technical delivery state never changes business ownership |

## 12. Admisi Integration Implementation Follow-up (Not Design Gaps)

`GAP-RNA-ARCH-015` is **Closed (Design)**. Remaining work is implementation-only and must not add business rules beyond [`ADMISI-RNA-INTEGRATION.md`](../bangsal/ADMISI-RNA-INTEGRATION.md):

- implement Application-boundary demand/query and result-consumer ports plus transport adapters;
- replace the no-op Ward accommodation gateway behavior with the approved local Admisi effects;
- add durable delivery or equivalent recovery, acknowledgement handling, idempotency, and reconciliation views; and
- add contract acceptance tests for duplicate, delayed, stale, conflicting, unavailable-receiver, and correction cases.
