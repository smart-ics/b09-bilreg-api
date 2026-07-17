# RUANG RANAP Operational Domain

## 1. Business Overview

RUANG RANAP Operational Management governs the accommodation of inpatient registrations and the execution of clinical services performed under ruang ranap responsibility.

Its business purpose is to ensure that patients are placed in appropriate accommodation, that accommodation remains operationally traceable throughout transfer and release, and that ruang ranap-performed services are recorded as truthful execution facts.

The domain covers two primary responsibilities:

- Accommodation Management.
- RUANG RANAP Service Execution.

Accommodation Management governs patient placement, occupancy purpose, current clinical location, rooming-in arrangements, retained accommodation, transfer, release, and bed readiness.

RUANG RANAP Service Execution governs truthful recording of work performed by the ruang ranap, whether work originates from a Clinical Order or is recorded as an authorized Ad Hoc Tindakan. RNA receives the work, may assign a Performer, and records an eligible Service for billable work or a description for non-billable work. It does not own the clinical intent or authorization of a Clinical Order, and it does not define the service. The Domain and SOP artifacts define these business authority semantics; the current implementation assumes only baseline authentication and coarse-grained application access, with fine-grained contextual authorization enforcement intentionally deferred to Phase-99 under ARCH-020.

RUANG RANAP Operational Management does not own:

- Clinical Order authoring, authorization, routing, amendment, cancellation, discontinuation, or reconciliation, which belong to CPOE.
- Nursing assessment, nursing diagnosis, nursing care planning, nursing intervention documentation, or nursing evaluation, which belong to NERS.
- Medication Administration.
- Shift Handover.
- Detailed Laboratory, Radiology, Operating Theatre, Pharmacy, Rehabilitation, or other specialized departmental fulfilment workflows.
- Service Definition and service configuration, including performer types, quantity/unit rules, documentation requirements, completion criteria, and outcome catalogues, which belong to Tarif Context or another explicitly named owning context.
- Charge Eligibility, tariff selection, package, coverage, bill calculation, adjustment, payment, and every other financial consequence, which belong to Tata Rekening.
- Clinical discharge authorization.
- Admission Waiting List creation, ownership, prioritization, cancellation routing, and assignment, which belong to Admisi.
- Inter-ward clinical content and responsibility, which belong to EMR.
- BOR calculation or other utilization-indicator calculation.
- Bed, room, ruang ranap, class, or service master-data governance.

RUANG RANAP Operational Management may present relevant information from neighboring domains, but presentation does not transfer business authority.

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| RUANG RANAP | An inpatient organizational unit responsible for accommodating patients and performing ruang ranap-authorized clinical services. |
| Accommodation | The use of a ruang ranap, room, bed, or related inpatient resource for a defined patient-care, retention, companion, or rooming-in purpose. |
| Accommodation Allocation | An active or historical assignment linking an inpatient registration, or its Companion Accommodation flag, to a bed/resource for a declared purpose. |
| Clinical Accommodation | The accommodation representing the patient’s current inpatient care location. |
| Retained Accommodation | An existing Accommodation Allocation intentionally kept Active while the patient receives treatment in another accommodation. It continues occupying bed capacity and producing accommodation facts until discharge. |
| Companion Accommodation | A bed assignment made through the ordinary Bed Assignment approval and validation procedure, flagged `IsCompanionBed = true` for a companion associated with an inpatient registration. It occupies the assigned bed but is not a patient, Clinical Accommodation, or patient registration. |
| Rooming-In | A Mother-and-Baby-only arrangement in which the Baby shares the Mother's bed while both retain distinct registrations and accommodation histories. |
| Primary Occupant | The patient whose occupancy establishes the principal use of the accommodation. |
| Associated Occupant | A patient accommodated together with a Primary Occupant under an allowed policy, such as a rooming-in baby. |
| Clinical Location | The ruang ranap or clinical unit where the patient is currently receiving inpatient responsibility and care coordination. |
| Physical Occupancy | The actual presence or recognized use of accommodation by a patient or companion. |
| Accommodation Purpose | The declared reason an allocation remains active, such as clinical care, room retention, rooming-in, or companion use. |
| Occupancy Treatment | The policy that determines how an allocation affects bed capacity and availability. |
| Reporting Treatment | Descriptive accommodation-fact treatment supplied to authorized downstream consumers. RNA does not calculate BOR or make financial decisions from it. |
| Bed | The lowest governed inpatient accommodation resource ordinarily used for patient placement. Bed master keeps the current operational readiness status as a fast-lookup projection of the latest valid RNA transaction; RNA does not own other master-data governance. |
| Bed Readiness | An RNA-owned operational transaction that records one transition of a bed to a new readiness state. The latest transaction projects the current readiness state exposed by Bed and determines whether a bed may receive a new permitted allocation. |
| Bed Readiness History | The append-only RNA operational history of Bed Readiness transactions. Each transition identifies the bed, new readiness state, business time, responsible actor or verifier, applicable reason, and optional evidence or reference. |
| Bed Assignability | The mandatory determination that a bed may receive an allocation: the bed exists and is active, belongs to the intended Ward, is Ready, has no conflicting active allocation, and satisfies applicable capacity or occupancy constraints. |
| Mandatory Bed Assignability | The complete and only automated placement determination in RNA: the bed exists and is active, belongs to the intended Ward, is Ready, has no conflicting active allocation, and has capacity available under the occupancy policy. |
| Accommodation Correction Fact | A new append-only fact that corrects a prior Accommodation Fact without modifying or deleting the original, identifies the correction reason, actor, time, and original fact, and remains within the original fact's Ward. |
| Assign Accommodation | Establish a new Accommodation Allocation for a registration. |
| Transfer Accommodation | Move or reclassify a patient’s accommodation while preserving prior allocation history. |
| Release Accommodation | End an active Accommodation Allocation because its declared purpose no longer applies. |
| Internal Transfer | A transfer between beds or rooms while responsibility remains within the same ruang ranap. |
| Inter-RUANG RANAP Transfer | A release-to-Admission workflow: the source RUANG RANAP releases accommodation and notifies Admisi, Admisi owns the Waiting List, and the destination RUANG RANAP later receives the patient through the ordinary Waiting List placement flow. |
| Bed Ready | A bed condition indicating that the bed may receive an allocation permitted by policy. |
| Cleaning Required | A bed condition indicating that occupancy has ended but the bed is not yet ready for reassignment. |
| Out of Service | A bed condition indicating that the bed cannot be used because of maintenance, safety, or operational restriction. |
| RUANG RANAP Service | A service identity defined by Tarif Context and referenced by RNA when the service is executed under ruang ranap responsibility. |
| RUANG RANAP Service Execution | RNA's authoritative fact that billable work used an eligible Service, or non-billable work was described, by a Performer at a stated time, together with source and correction history. |
| Ordered RUANG RANAP Service | A RUANG RANAP Service Execution originating from a Clinical Order. |
| Ad Hoc Tindakan | An unplanned clinical action arising from an immediate patient need and performed under a declared professional, emergency, protocol, or other permitted authority. |
| Independent Tindakan | A RUANG RANAP Service performed under the professional’s own authority without an individual prospective Clinical Order. |
| Execution Source | The declared origin and authority basis of a RUANG RANAP Service Execution. |
| Planned Occurrence | One expected performance derived from a scheduled or recurring Clinical Order. |
| Performer | The professional who carries out or directly leads a RUANG RANAP Service Execution. |
| Responsible RUANG RANAP | The ruang ranap accountable for received work and truthful execution recording. |
| Entered in Error | A declaration that an execution record should not have existed as a valid record, without erasing its history. |
| Performed At | The actual business time at which the service was performed; it is the Service Execution fact's `OccurredAt`. |
| Recorded At | The system persistence time for the execution fact; it is `RecordedAt` and is used only for audit and technical tracing. |
| Service Execution Fact | The immutable business statement that a Performer executed billable Service X or described non-billable work at time Z, identified for correction and, when billable, idempotent publication. |

### Business Time Standard

RNA uses one time model for every business fact:

- **`OccurredAt`** is the authoritative business time at which the fact happened. Accommodation assignment, release, and correction facts use the accountable action time; Bed Readiness uses the readiness transition time; Service Execution uses **Performed At**; and an Integration Fact uses the source fact's business time.
- **`RecordedAt`** is the system persistence time at which RNA or the sending context stored the fact. It is used only for audit and technical tracing, never to establish business order or replace the fact's business time.
- Business facts are ordered by `OccurredAt`. Late entry therefore preserves the original business chronology while retaining its later `RecordedAt`.
- When the actual business time is unknown, the producer sets `OccurredAt = RecordedAt` and does not invent an earlier time.
- `OccurredAt` and `RecordedAt` are stored as UTC instants. A supplied source offset or local business date may be retained as display/context metadata, but it does not alter ordering.

This standard applies consistently to Accommodation Facts, Bed Readiness transactions, Service Execution Facts, Correction Facts, and Admisi/RNA Integration Facts.

## 3. Business Capabilities

### 3.1 Accommodation Demand Management

Reviews Admisi-owned Waiting List entries for the intended Ward and maintains only the visibility needed to perform Bed Assignment. RNA does not own the Waiting List or take responsibility for an entry before successful Bed Assignment.

### 3.2 Accommodation Assignment

Assigns suitable accommodation for a declared purpose while preserving patient, ruang ranap, room, bed, capacity, and reporting meaning.

### 3.3 Accommodation Occupancy Management

Maintains current clinical accommodation, retained accommodation, rooming-in, companion use, and other permitted simultaneous allocations. Temporary Absence is not an RNA capability.

### 3.4 Accommodation Transfer

Coordinates internal accommodation changes and releases inter-ruang ranap cases back to the Admisi Waiting List without losing allocation history. RNA never coordinates a direct ward-to-ward transfer queue.

### 3.5 Accommodation Release and Bed Readiness

Ends accommodation purposes and records the Bed Readiness transactions that move a bed through cleaning, blocked, out-of-service, or ready states. Bed's displayed current operational state is the projection of the latest readiness transaction, never the primary history.

### 3.6 RUANG RANAP Service Work Management

Maintains visibility of received work and optional assignment until execution is recorded or the source-owned work obligation is withdrawn.

### 3.7 Ordered RUANG RANAP Service Execution

Records execution of a Tarif-defined service originating from a Clinical Order and publishes the authoritative Service Execution Fact to CPOE and other authorized consumers.

### 3.8 Ad Hoc Tindakan Execution

Records an unplanned or independently authorized execution against an existing Tarif service identity under a truthful authority classification.

### 3.9 Execution Recording and Correction

Records Performer and Performed At, preserves late-entry chronology, and appends corrections or Entered in Error history without erasing prior facts.

Authorized care-team actors may report an error. The owning Ward's Head Nurse may finalize an ordinary correction but may not approve their own report or correction proposal. Patient, Registration, Service, or source-order identity changes, replacement, Entered in Error, disputed cases, and other material corrections require an independent second reviewer authorized by Clinical Governance. Every correction carries a structured reason and complete audit identity; material corrections also require evidence.

### 3.10 Billable Service Execution Fact Publication

Publishes billable Service Execution Facts and corrections to the Tindakan/Tata Rekening boundary. One billable fact creates at most one linked `Tindakan`; non-billable execution creates none. Tata Rekening owns every subsequent tariff, billing, adjustment, and settlement consequence.

### 3.11 Accommodation Correction

Lets only the Head Nurse append an Accommodation Correction Fact before Tata Rekening is `FINALIZED`, while the corrected fact remains owned by the same Ward. RNA publishes the correction; each downstream bounded context reconciles its own data.

### 3.12 RUANG RANAP Operational Audit

Preserves accountability for accommodation decisions, ruang ranap execution decisions, performer identity, business time, reason, source, and resulting state.

## 4. Actors & Roles

| Actor or Role | Business Responsibility and Authority |
|---|---|
| RUANG RANAP Nurse | Coordinates patient accommodation, progresses ruang ranap service work, and performs or records services within professional responsibility and assignment. |
| RUANG RANAP Midwife | Performs the same ruang ranap operational responsibilities within permitted maternity, neonatal, and professional scope. |
| RUANG RANAP Coordinator or Head Nurse | Oversees ruang ranap capacity, allocation exceptions, transfer coordination, workload, and accountable escalation. |
| Bed Coordinator | Coordinates accommodation demand and cross-ruang ranap placement where the hospital assigns this responsibility separately from the ruang ranap. |
| Admisi Actor | Owns the Waiting List and routes a released inter-ward transfer through the ordinary Admission placement process. |
| Performer | Carries out or directly leads a RUANG RANAP Service Execution within professional competency and assignment. |
| Supporting Performer | Participates in an execution without replacing the accountable Performer. |
| Ordering PPA | Establishes prospective clinical intent through CPOE. The Ordering PPA does not own RUANG RANAP Service Execution. |
| Responsible Clinician | Holds clinical responsibility for the patient and may be involved when execution cannot proceed, must be changed, or requires escalation. |
| Patient or Patient Representative | Participates in accommodation and service execution, may provide required consent, and may refuse a service. |
| Housekeeping Actor | Restores bed readiness where cleaning responsibility is assigned to housekeeping. |
| Maintenance or Facilities Actor | Resolves bed or room conditions that make accommodation unavailable or unsafe. |
| Clinical Governance Authority | Defines permitted rooming-in, allocation purposes, and ad hoc authority without redefining Tarif-owned services or Tata Rekening-owned financial policy. It does not add automated RNA placement restrictions. |
| Tarif Context | Owns Service Definition, service identity, performer types, quantity/unit rules, documentation requirements, completion criteria, outcome catalogues, and service configuration. |
| Tindakan / Tata Rekening | Tindakan owner creates one linked record per billable execution; Tata Rekening owns tariff, package, coverage, amount, adjustment, payment, settlement, and every subsequent financial consequence. |

One person may perform several roles when hospital policy permits it. Role combination does not remove the distinct accountability of allocation, execution, correction, and financial handling.

## 5. Domain Objects

### 5.1 Accommodation Allocation

Represents one declared use of accommodation by an inpatient registration.

It identifies:

- The inpatient registration.
- The ruang ranap, room, bed, or other accommodation resource.
- The Accommodation Purpose.
- Whether it represents the current Clinical Accommodation.
- The Primary or Associated Occupant role.
- The association to another registration when rooming-in applies.
- The start and end of the allocation.
- Occupancy and reporting treatment.
- The assignment, transfer, retention, and release reasons.

An active allocation does not necessarily mean that the patient is physically present in the bed or that the allocation contributes to BOR.

### 5.2 Bed

Represents a governed accommodation resource with permitted occupancy policy. Bed master keeps the current operational readiness state projected from the latest RNA-owned Bed Readiness transaction.

Bed does not own the complete patient accommodation history or the primary readiness history. Current use is determined from active Accommodation Allocations and applicable occupancy policy.

### 5.3 Bed Readiness

Represents one RNA-owned operational transaction that changes a bed's readiness state. It is append-only business history, not a mutable Bed-master flag.

It distinguishes operational conditions such as:

- Ready.
- Cleaning Required.
- Cleaning in Progress.
- Blocked.
- Out of Service.

Each transaction preserves the Bed, new readiness state, business time, responsible actor or verifier, reason when applicable, and optional evidence or reference. The current state exposed by Bed is the projection of the latest valid transaction. Readiness is separate from occupancy and reporting treatment.

### 5.4 Accommodation Occupancy Policy

Defines which combinations of active allocations are permitted for a bed or room.

It may govern:

- Primary occupancy capacity.
- One associated Rooming-In Baby in addition to one Primary Occupant without increasing bed capacity.
- Companion use.
- Retained accommodation.
- Temporary co-occupancy.
- Whether a new allocation affects availability.

### 5.5 Accommodation Reporting Policy

Defines the descriptive treatment attached to accommodation facts for authorized downstream consumers. RNA does not calculate BOR, LOS, bed-days, census, billing, or other downstream outcomes.

### 5.7 Rooming-In Association

Represents the Mother-and-Baby-only association between a Mother as Primary Occupant and her Baby as Associated Occupant. The relationship is referenced from Patient Social Data: the Baby Medical Record references the Mother's Medical Record.

Each patient retains a distinct registration and accommodation history. Every bed supports at most one such Associated Occupant in addition to one Primary Occupant; this does not increase bed capacity.

### 5.8 Tarif Service Reference

For billable execution, RNA resolves an active Service identity through the authoritative `Bangsal → Layanan → Tarif.AllowedLayanan` path. RNA may retain the stable Tarif/Service ID and the minimum immutable display snapshot needed to understand historical work, but it does not copy or govern Service Definition rules. A non-billable execution has no Service reference and instead carries its required description.

Tarif Context, or another explicitly named owner coordinated by Tarif, remains authoritative for performer types, quantity/unit rules, documentation requirements, completion criteria, outcome catalogues, and service configuration.

### 5.9 RUANG RANAP Service Execution

Represents one authoritative ruang ranap execution instance.

It identifies:

- The patient and care context.
- For billable execution, the Tarif Service identity; for non-billable execution, the required description.
- The Execution Source.
- The originating Clinical Order and Planned Occurrence when applicable.
- The Responsible RUANG RANAP.
- The optional assignment and actual Performer.
- Performed At and Recorded At.
- The stable Service Execution Fact identity and publication state.
- Correction or Entered in Error history.

### 5.10 Execution Authority Record

Represents the truthful authority basis for an Ad Hoc Tindakan or Independent Tindakan.

It may identify:

- Independent professional authority.
- Approved protocol and version.
- Emergency basis.
- Verbal instruction.
- Retrospective documentation.
- Required subsequent authorization.

The existence of an execution is not erased when subsequent accountability remains incomplete.

When subsequent authorization is required, it is due within 24 hours of `OccurredAt` or before discharge, whichever is earlier. The accountable authorizer is the attending or clinically responsible physician; when unavailable, accountability moves to the designated on-call physician. Escalation proceeds from the accountable authorizer to the on-call physician/service lead and then Clinical Governance. Acknowledgement records acceptance of the follow-up task and is not an authorization decision. CPOE owns enforcement and records terminal `Authorization Overdue`; any review after the deadline is a late review, not prospective or timely authorization.

## 6. Aggregates

### 6.1 Accommodation Allocation Aggregate

**Aggregate Root:** Accommodation Allocation

**Business responsibility:** Preserve the meaning, purpose, history, and current validity of one accommodation use by one registration.

**Consistency boundary includes:**

- Accommodation Purpose.
- Occupant role.
- Current Clinical Accommodation designation.
- Start and end.
- Rooming-In Association where applicable.
- Occupancy and reporting treatment.
- Assignment, transfer, retention, and release decisions.
- Append-only Accommodation Correction Facts that reference the original fact.

The aggregate ensures that its own lifecycle remains coherent and auditable. Cross-allocation policies, including bed capacity and primary clinical location, are evaluated through domain policies across relevant active allocations.

### 6.2 Bed Operational Aggregate

**Aggregate Root:** Bed

**Business responsibility:** Preserve the transactional readiness history and governed usability of one bed.

**Consistency boundary includes:**

- Append-only Bed Readiness transactions, each with new state, business time, responsible actor or verifier, reason when applicable, and optional evidence/reference.
- The latest readiness transaction used to project the Bed-facing current state.
- Readiness restrictions and reasons.
- Bed-specific occupancy policy reference.

The Bed Aggregate does not contain all Accommodation Allocations and does not treat a single current patient field as the authority for occupancy.

### 6.3 RUANG RANAP Service Execution Aggregate

**Aggregate Root:** RUANG RANAP Service Execution

**Business responsibility:** Preserve received work, optional assignment, the truthful fact that billable work used an eligible Service or non-billable work was described, and its non-destructive correction history.

**Consistency boundary includes:**

- Execution Source.
- Clinical Order and Planned Occurrence references where applicable.
- Execution Authority Record for Ad Hoc or Independent Tindakan.
- Responsible RUANG RANAP.
- Optional assignment and actual Performer accountability.
- Billable Tarif Service identity or non-billable description, Performed At, Recorded At, and execution-fact identity.
- Correction and Entered in Error history.
- Per-destination publication and acknowledgement state, outside the business decision itself.

One Clinical Order may be associated with multiple RUANG RANAP Service Execution Aggregates when the order is recurring, scheduled, conditional, or otherwise requires several performances.

## 7. Business Rules

### Accommodation meaning and allocation

**BR-RNA-001** — An Accommodation Allocation shall identify one inpatient registration, one accommodation resource, one Accommodation Purpose, and its start time. A Companion Accommodation references the associated inpatient registration and is additionally marked `IsCompanionBed = true`; it does not create a companion patient registration.

**BR-RNA-002** — One inpatient registration may have multiple simultaneous active Accommodation Allocations when their purposes are explicit and permitted.

**BR-RNA-003** — Active Retained Accommodation or Rooming-In shall not automatically be interpreted as the patient’s current Clinical Accommodation. Companion Accommodation is never a Clinical Accommodation.

**BR-RNA-004** — The current Clinical Location shall be derived from the active allocation designated for current clinical care responsibility, not from every active allocation.

**BR-RNA-005** — A transfer to another clinical unit may end, retain, or reclassify the previous accommodation according to an explicit decision; transfer shall not automatically terminate every prior allocation.

**BR-RNA-006** — Historical Accommodation Allocations shall not be overwritten when a patient changes bed, room, ruang ranap, purpose, or reporting treatment.

**BR-RNA-007** — Release of an Accommodation Allocation ends that declared accommodation purpose and shall identify an accountable release reason.

**BR-RNA-007a** — Retained Accommodation shall remain an Active Accommodation Allocation, continue occupying bed capacity, and continue producing accommodation facts until patient discharge.

**BR-RNA-007b** — Only a Ward Nurse or Head Nurse may establish Retained Accommodation. Discharge requires every active Retained Accommodation for the registration to be released.

### Bed occupancy, rooming-in, and availability

**BR-RNA-008** — A bed may have one Primary Occupant and, only for Mother-and-Baby Rooming-In, one Associated Occupant (Baby). The associated occupant does not increase bed capacity.

**BR-RNA-009** — Rooming-In shall preserve separate registration identities for the Primary Occupant and Associated Occupant.

**BR-RNA-010** — A Rooming-In Association shall identify the Mother as Primary Occupant and the Baby as Associated Occupant by referencing the persisted Patient Social Data relationship in which the Baby Medical Record references the Mother's Medical Record.

**BR-RNA-011** — Rooming-In shall not be represented by merging the mother and baby registrations or by assigning one registration identity to both patients.

**BR-RNA-011a** — Only a Ward Nurse or Head Nurse may activate Rooming-In. Every bed supports Rooming-In under the one-Primary-plus-one-Baby rule.

**BR-RNA-011b** — RNA shall not calculate BOR for Rooming-In. Until legacy behavior is confirmed, the documented downstream billing assumption is one room charge only; RNA records and publishes accommodation facts without applying that billing decision.

**BR-RNA-012** — Bed availability shall be determined from Bed Readiness, active allocations, occupancy purpose, and Accommodation Occupancy Policy, not merely from whether any active allocation exists.

**BR-RNA-013** — RNA shall preserve explicit accommodation facts and shall not calculate BOR or infer a financial result from the number of registrations associated with a bed.

**BR-RNA-014** — RNA shall publish truthful accommodation facts when an authorized downstream consumer requires them; RNA shall not interpret those facts as Charge Eligibility or another financial decision.

**BR-RNA-015** — A bed shall receive a new allocation only when Mandatory Bed Assignability is established: the bed exists and is active, belongs to the intended Ward, is Ready, has no conflicting active allocation, and has capacity available under the applicable occupancy policy. RNA shall not reject Bed Assignment for gender, isolation, equipment, or any other hospital-specific operational policy.

Care Class and Care Context are derived facts of the active Clinical Accommodation: Care Class is derived from the assigned room/bed, and an intensive-care context is derived from the assigned ICU, PICU, NICU, or equivalent accommodation. They are outcomes of Accommodation Assignment, not patient attributes or prerequisites used to select a bed.

### Assignment and transfer

**BR-RNA-016** — Accommodation assignment shall identify the responsible actor, business time, purpose, and Mandatory Bed Assignability evidence.

**BR-RNA-016b** — Companion Accommodation shall use the same authorized Bed Assignment procedure, Mandatory Bed Assignability checks, assignment fact, release process, and audit as ordinary bed assignment, with `IsCompanionBed = true`. It occupies the assigned bed, is associated with the inpatient registration, and is published to downstream consumers with that flag so patient-counting reports, including RL, can exclude it. It does not create an Admisi Waiting List outcome or change patient clinical location.

**BR-RNA-016a** — Admission owns each Waiting List entry while Ward reviews it. Ward rejection leaves responsibility with Admission. Successful Bed Assignment creates the Accommodation Fact, closes the Waiting List as a consequence, and automatically moves responsibility to RNA. There is no Accepted state or intermediate responsibility transfer.

**BR-RNA-017** — An internal transfer shall preserve the continuity and history of both the released and newly assigned accommodation.

**BR-RNA-018** — Inter-RUANG RANAP Transfer shall not be a direct ward-to-ward transfer. The source RNA releases the active Clinical Accommodation and notifies Admisi so the patient returns to the Admission Waiting List.

**BR-RNA-019** — After Release, RNA owns no transfer queue, destination decision, or operational responsibility for the transfer. Admisi owns Waiting List routing and destination assignment; the destination Ward reviews the entry through the same Waiting List and Bed Assignment flow as any other patient.

**BR-RNA-019a** — Destination ownership before successful Bed Assignment and any explicit responsibility-transfer contract shall not exist. Inter-ward clinical content belongs to EMR.

**BR-RNA-019b** — A cancelled inter-ward transfer returns through the same Admission Waiting List process; RNA does not create a separate cancellation-return workflow.

**BR-RNA-020** — Temporary Absence is not modeled, stored, displayed, or actioned by RNA. It has no RNA lifecycle and shall not affect accommodation, occupancy, bed readiness, patient reporting, or billing facts.

**BR-RNA-021** — Discharge authorization shall not by itself prove that accommodation has been physically released.

**BR-RNA-022** — Accommodation release shall not by itself establish that the inpatient encounter has been discharged.

### Bed readiness

**BR-RNA-023** — Release of accommodation shall not automatically make the bed Ready.

**BR-RNA-024** — A bed requiring cleaning, inspection, or maintenance shall remain unavailable until the responsible readiness condition is resolved.

**BR-RNA-025** — Every Bed Readiness transition shall be recorded as an RNA-owned operational transaction; it shall identify the Bed, new readiness state, business time, responsible actor or verifier, reason where applicable, and optional evidence or reference.

**BR-RNA-025a** — The current readiness state exposed by Bed shall be a projection of the latest valid Bed Readiness transaction and shall not be treated as the primary business history or changed without recording that transaction.

**BR-RNA-025b** — Ready shall be explicitly verified by an authorized actor. A current-state Ready flag without the corresponding verified Bed Readiness transaction is not authoritative history.

### RUANG RANAP execution authority and source

**BR-RNA-026** — RNA shall record authoritative execution only for work routed to ruang ranap responsibility. A billable record requires an eligible Tarif Service identity; a non-billable record requires description. RNA shall not define which services exist or configure their execution rules.

**BR-RNA-027** — Laboratory, Radiology, Operating Theatre, Pharmacy, Rehabilitation, and other specialized services shall remain authoritative for their own execution workflows.

**BR-RNA-028** — The ruang ranap may coordinate preparation, transport, or supporting work for another Executing Domain, but shall not record that domain’s service as completed unless authority has explicitly been delegated.

**BR-RNA-029** — Every RUANG RANAP Service Execution shall declare its Execution Source.

**BR-RNA-030** — An Ordered RUANG RANAP Service shall reference the originating Clinical Order and Planned Occurrence when applicable.

**BR-RNA-031** — An Ad Hoc Tindakan shall identify the authority basis under which it was performed.

**BR-RNA-032** — An Independent Tindakan shall be distinguished from an action for which a prior Clinical Order was required but absent.

**BR-RNA-033** — RUANG RANAP Operational Management shall not retrospectively represent an Ad Hoc, Emergency, Verbal, Protocol-Based, Independent, or late-recorded action as a prospectively authorized Clinical Order.

**BR-RNA-034** — CPOE remains authoritative for Clinical Order intent, authorization, lifecycle, and exceptional-order accountability; RUANG RANAP Service Execution remains authoritative for actual ruang ranap execution evidence.

**BR-RNA-035** — A RUANG RANAP Service routed to RNA shall not produce a duplicate execution fact through CPOE Generic Fulfilment.

### RUANG RANAP execution recording

**BR-RNA-036** — Pending work represents a request or operational obligation received by RNA and is not evidence that the service occurred.

**BR-RNA-037** — Performer assignment is optional RNA work coordination and does not prove execution.

**BR-RNA-038** — Recording execution shall identify actual Performer and Performed At time, plus either an eligible Tarif Service for billable work or a description for non-billable work. Together these form RNA's authoritative Service Execution Fact.

**BR-RNA-039** — Receipt, viewing, assignment, or preparation shall not be represented as a Service Execution Fact.

**BR-RNA-040** — Work withdrawn, cancelled, deferred, or otherwise not executed shall remain a work/source coordination fact and shall not create a Service Execution Fact.

**BR-RNA-041** — RNA shall not invent or own partial, aborted, not-performed, completion, or outcome catalogues. Any such classification required by another context remains owned and supplied by that context.

**BR-RNA-042** — One Planned Occurrence shall not produce more than one active Service Execution Fact for the same actual execution.

**BR-RNA-043** — One Clinical Order may produce multiple RUANG RANAP Service Executions when multiple Occurrences or repeated performances are required.

**BR-RNA-044** — A legacy order status such as `Implemented`, `Done`, or `Executed` shall not be treated as RNA's Service Execution Fact without billable eligible Service or non-billable description, actual Performer, and Performed At evidence.

### Timing, late entry, correction, and audit

**BR-RNA-045** — Performed At and Recorded At shall remain distinguishable.

**BR-RNA-046** — A retrospective or late entry shall preserve the actual execution time, recording time, recorder, and reason for delayed recording.

**BR-RNA-047** — A correction shall preserve the original execution history and identify the correcting actor, correction time, and reason.

**BR-RNA-048** — An execution recorded against the wrong patient, wrong service, duplicate occurrence, or without legitimate basis shall be marked Entered in Error rather than physically deleted.

**BR-RNA-049** — No accommodation decision, Service Execution Fact, correction, or authority record may be erased from business history.

**BR-RNA-049a** — Only a Head Nurse may create an Accommodation Correction Fact. It may correct an Assignment, Internal Transfer, Release, Retained Accommodation, Rooming-In, or applicable Bed Readiness Fact only while the original fact belongs to the same Ward.

**BR-RNA-049b** — Accommodation Correction is allowed only before Tata Rekening reaches `FINALIZED`. After `FINALIZED`, RNA shall reject ordinary correction; any further change follows an administrative process outside RNA.

**BR-RNA-049c** — An Accommodation Correction Fact shall reference the original fact and preserve it unchanged. RNA publishes the correction fact; each downstream bounded context reconciles its own data.

### Service reference and execution fact

**BR-RNA-050** — Each Bangsal shall have one authoritative `LayananId`. For billable execution, RNA shall query Tarif for active Services whose `AllowedLayanan` includes that Layanan and shall save only a selected eligible stable `ServiceId`. Non-billable execution shall save no `ServiceId` and shall require description. RNA shall not create a local Service Definition.

**BR-RNA-051** — Performer types, quantity/unit rules, documentation requirements, completion criteria, outcome catalogues, and service configuration remain owned by Tarif Context or another explicitly named owning context and shall not become RNA aggregate invariants.

**BR-RNA-052** — The published Service Execution Fact shall contain only the stable fact identity, selected Service identity when billable, required non-billable description when not, Performer identity, Performed At, source/occurrence correlation where applicable, and correction metadata required by the integration contract.

**BR-RNA-053** — Authoritative clinical documentation remains in NERS or its owning documentation domain. RNA does not require or copy it merely to establish the execution fact.

### Financial boundary and publication

**BR-RNA-054** — The RNA execution user shall explicitly classify execution as billable or non-billable. Billable execution requires an eligible `ServiceId`; non-billable execution requires description and creates no `Tindakan`. RNA shall not calculate tariff, coverage, amount, journal, payment, or settlement.

**BR-RNA-055** — RNA shall publish only billable Service Execution Facts to the Tindakan/Tata Rekening boundary, carrying stable execution identity and selected Service but no tariff, package, coverage, amount, journal, payment, or settlement fields.

**BR-RNA-056** — Tata Rekening is the sole authority that evaluates the Service Execution Fact and determines whether any billing consequence exists.

**BR-RNA-057** — Tata Rekening may combine the fact with Tarif and other financial policies; RNA shall not duplicate those rules.

**BR-RNA-058** — RNA shall not determine financial eligibility, tariff, package inclusion, coverage, bill amount, adjustment, journal, or payment. Its user-selected billable/non-billable classification only controls whether the recorded fact must carry an eligible Service and be delivered to Tindakan; it is not a financial calculation or approval.

**BR-RNA-059** — Correction or Entered in Error after publication shall produce a new versioned correction fact referencing the original execution fact; downstream financial correction remains Tata Rekening's responsibility.

### Legacy coexistence

**BR-RNA-060** — Existing legacy order sources may continue to create ruang ranap work until CPOE replaces their prospective intent authority.

**BR-RNA-061** — Legacy-originated work shall retain its source identity and shall not be represented as native CPOE intent unless it was actually created and authorized through CPOE.

**BR-RNA-062** — Repeated legacy source delivery or retry shall not create duplicate RUANG RANAP Service Execution obligations for the same source occurrence.

**BR-RNA-063** — Transition from legacy ordering to CPOE shall not change the business meaning or history of already completed RUANG RANAP Service Executions.

**BR-RNA-063a** — After the approved facility/unit cutover, native CPOE replaces `OrderTdk` for new orders; historical `OrderTdk` remains read-only and explicitly source-labeled.

**BR-RNA-063b** — `Tindakan` remains the billable-execution record. One billable `ServiceExecutionFactId` may create at most one linked `Tindakan`; a non-billable execution creates none. `Tindakan` does not replace RNA's authoritative execution history.

**BR-RNA-063c** — Cutover rollback shall stop new native intake without deleting or recreating committed native orders, RNA execution facts, or linked `Tindakan` records.

**BR-RNA-063d** — Legacy Accommodation Stay and RNA shall operate in parallel. The legacy Stay identity and legacy-owned fields remain authoritative; RNA-owned extensions and histories are keyed by the stable legacy identity.

**BR-RNA-063e** — RNA shall not require bulk migration or backfill to read an existing Stay. Missing historical detail remains unknown; RNA shall not invent business time, purpose, or transition facts.

**BR-RNA-063f** — A field has one write authority. RNA-originated changes to legacy-owned fields use the legacy application boundary; legacy-originated bypass changes are detected through version/reconciliation checks and do not silently overwrite RNA extension history.

## 8. State Machines & Lifecycles

### 8.1 Accommodation Allocation Lifecycle

```text
Proposed
  → Active
  → Released
```

Alternative outcomes are:

- Cancelled before activation.
- Entered in Error.

Business meaning:

| State | Meaning |
|---|---|
| Proposed | Accommodation has been identified or prepared but is not yet an active allocation. |
| Active | The accommodation purpose is currently in effect. |
| Released | The declared accommodation purpose has ended. |
| Cancelled | The proposed allocation will not become active. |
| Entered in Error | The allocation should not have existed as a valid business record. |

Transfer does not overwrite an Active allocation. It releases or reclassifies the prior allocation and establishes the required new allocation while preserving continuity.

Retained Accommodation remains `Active` until patient discharge and is released as part of discharge accommodation closure. An inter-RUANG RANAP transfer does not create an RNA-to-RNA transition: the source Clinical Accommodation is released, then any later destination allocation is established independently through the Admisi Waiting List and Bed Assignment flow.

### 8.2 Bed Readiness Lifecycle

```text
Ready
  → Occupancy or Retention Use
  → Cleaning Required
  → Cleaning in Progress
  → Ready
```

Additional conditions are:

- Blocked.
- Out of Service.

A bed may return to Ready only when its blocking, cleaning, inspection, or maintenance condition has been resolved and an authorized verifier explicitly records the Ready transaction with business time, actor, optional reason, and optional evidence. The Bed-facing current state is then projected from that latest transaction.

### 8.3 RUANG RANAP Service Execution Lifecycle

```text
Pending
  → Assigned (optional)
  → Executed
```

Source coordination may instead end pending work as Cancelled or Withdrawn before execution. Correction may later mark an execution Entered in Error. These are not an RNA-owned clinical outcome catalogue.

Business meaning:

| State | Meaning |
|---|---|
| Pending | The ruang ranap has an unresolved service responsibility that has not started. |
| Assigned | A Performer has optionally been assigned; execution is not implied. |
| Executed | RNA recorded the referenced Service, actual Performer, and Performed At as an authoritative Service Execution Fact. |
| Cancelled or Withdrawn | The source-owned work obligation ended without an RNA execution fact. |
| Entered in Error | The execution record should not have existed as a valid record. |

RNA does not use this lifecycle to define service completion criteria or clinical outcomes. Those definitions remain with Tarif Context or another explicitly named owning context.

### 8.4 Execution Authority Lifecycle

For exceptional or ad hoc actions requiring later accountability:

```text
Action Recorded
  → Subsequent Authorization Required
  → Authorized
```

If the governed period expires:

```text
Subsequent Authorization Required
  → Authorization Overdue
```

The actual execution remains part of the clinical history even when accountability is overdue.
The governed period is 24 hours from execution `OccurredAt` or until discharge, whichever occurs first. A review completed afterward is retained as late review and does not reverse or relabel the terminal overdue history.

### 8.5 Service Execution Fact Delivery Lifecycle

```text
Execution Fact Recorded
  → Delivery Pending
  → Acknowledged by Tata Rekening or Delivery Failed
```

When execution is corrected:

```text
Execution Fact Previously Published
  → Correction Fact Recorded
  → Correction Delivery Pending/Acknowledged
```

Delivery state is integration metadata. Acknowledgement may expose linked `TindakanId` but not tariff or settlement outcomes; Tata Rekening owns every subsequent financial lifecycle.

## 9. Domain Events

| Domain Event | Business Meaning |
|---|---|
| Accommodation Proposed | Accommodation has been identified for possible assignment. |
| Accommodation Assigned | An Accommodation Allocation has become active. |
| Clinical Accommodation Designated | An active allocation has been identified as the patient’s current clinical accommodation. |
| Accommodation Retained | An allocation remains active for retention or another non-primary clinical purpose after clinical location changes. |
| Rooming-In Started | A Primary Occupant and Associated Occupant have begun an authorized rooming-in arrangement. |
| Rooming-In Ended | An authorized rooming-in arrangement has ended. |
| Inter-Ward Accommodation Released to Admission | The source RNA released the Clinical Accommodation for an inter-ward move and must notify Admisi. |
| Admission Waiting List Notification Prepared | RNA prepared the release fact for Admisi; Admisi remains the Waiting List owner. |
| Accommodation Released | An active accommodation purpose has ended. |
| Accommodation Correction Fact Recorded | A Head Nurse appended a correction that references an Accommodation Fact owned by the same Ward. |
| Accommodation Correction Fact Published | RNA delivered the correction fact to an authorized downstream bounded context for its own reconciliation. |
| Bed Cleaning Required | A bed cannot yet be reassigned because cleaning is required. |
| Bed Cleaning Started | Cleaning responsibility has begun. |
| Bed Marked Ready | The bed has become available for permitted assignment. |
| Bed Blocked | The bed has become unavailable for an operational reason. |
| Bed Taken Out of Service | The bed has become unusable for safety, maintenance, or operational reasons. |
| Bed Returned to Service | An out-of-service condition has been resolved. |
| RUANG RANAP Service Work Received | The ruang ranap has received a new unresolved service responsibility. |
| RUANG RANAP Service Execution Created | A RUANG RANAP Service Execution has been established from an order, ad hoc action, or other permitted source. |
| RUANG RANAP Service Assigned | Responsibility for execution has been assigned to a ruang ranap actor or team. |
| RUANG RANAP Service Executed | RNA recorded that the referenced Service was executed by the stated Performer at Performed At. |
| RUANG RANAP Service Execution Cancelled | The execution obligation ended before execution began. |
| RUANG RANAP Service Execution Entered in Error | The execution record has been declared invalid while retaining history. |
| Ad Hoc Tindakan Recorded | An unplanned or independently authorized ruang ranap action has been documented with its authority basis. |
| Subsequent Authorization Required | An exceptional execution requires later accountable confirmation. |
| Subsequent Authorization Completed | Required accountability has been confirmed. |
| Subsequent Authorization Became Overdue | Required later accountability was not completed within policy. |
| RUANG RANAP Service Execution Fact Published | The authoritative execution fact was handed to an authorized consumer without a billing interpretation. |
| RUANG RANAP Service Execution Fact Corrected | A versioned correction referencing a previously published execution fact was recorded. |
| RUANG RANAP Execution Fact Reported to CPOE | The authoritative execution fact has been handed to CPOE for order coordination. |

## 10. Business Workflows

### 10.1 Standard Accommodation Assignment

```text
Admisi Waiting List Entry Visible to Ward
  → Ward Reviews Entry
  → Reject: Waiting List Remains under Admission
  → or Mandatory Bed Assignability Established
  → Accommodation Assigned
  → Waiting List Closed by Admisi as Consequence
  → Responsibility Moves to RNA
  → Clinical Accommodation Designated
  → Occupancy and Reporting Treatment Applied
```

### 10.2 Internal Accommodation Transfer

```text
Need for Bed or Room Change Identified
  → Destination Accommodation Assessed
  → New Allocation Established
  → Patient Moved
  → Prior Allocation Released or Reclassified
  → Bed Readiness Reassessed
```

### 10.3 Inter-RUANG RANAP Release to Waiting List

```text
Transfer Need Identified
  → Source Clinical Accommodation Released
  → RNA Notifies Admisi of the Release
  → Admisi Returns Patient to Its Waiting List
  → Destination Ward Reviews Ordinary Waiting List Entry
  → Destination Accommodation Assigned Through Standard Placement
```

RNA does not own the Waiting List, a transfer queue, or operational responsibility after Release. Destination Ward rejection leaves responsibility with Admission until successful Bed Assignment. Inter-ward clinical content belongs to EMR. Cancellation returns through the same Admisi Waiting List process.

### 10.4 ICU Transfer with Retained VIP Accommodation

```text
Patient Requires ICU Transfer
  → Ward Nurse or Head Nurse Establishes VIP Allocation as Retained
  → VIP Allocation Remains Active and Occupies Bed Capacity
  → ICU Accommodation Becomes Current Clinical Accommodation Through Its Owning Placement Flow
  → VIP Allocation Continues Producing Accommodation Facts
  → VIP Allocation Released at Discharge
```

### 10.5 Mother and Baby Rooming-In

```text
Mother Has Active Primary Accommodation
  → Patient Social Data Confirms Baby Medical Record References Mother Medical Record
  → Ward Nurse or Head Nurse Activates Rooming-In
  → Rooming-In Association Established
  → Baby Receives Associated Accommodation Allocation
  → Bed Holds One Primary Occupant and One Associated Occupant Without Capacity Increase
  → Mother and Baby Retain Separate Registrations and Histories
  → Rooming-In Ends When Shared Accommodation Ends
```

RNA performs no BOR calculation. The current legacy billing assumption is one room charge only and remains documented for downstream confirmation; it is not an RNA billing rule.

### 10.6 Accommodation Release and Bed Readiness

```text
Accommodation Purpose Ends
  → Accommodation Released
  → Bed Condition Assessed
  → Cleaning, Inspection, or Maintenance Completed When Required
  → Bed Marked Ready
```

### 10.6a Accommodation Correction

```text
Accommodation Fact Error Identified
  → Head Nurse Verifies Original Fact Belongs to the Same Ward
  → Tata Rekening Status Checked
  → Not FINALIZED
  → Original Fact Preserved Unchanged
  → Accommodation Correction Fact Appended
  → RNA Publishes Correction Fact
  → Each Downstream Bounded Context Reconciles Its Own Data
```

If Tata Rekening is `FINALIZED`, RNA rejects ordinary correction and the requested change follows the administrative process outside RNA.

### 10.7 Ordered RUANG RANAP Service Execution

```text
Clinical Order Routed to RUANG RANAP
  → RUANG RANAP Service Work Received
  → Planned Occurrence Identified When Applicable
  → Execution Assigned or Taken
  → Service Execution Fact Recorded: Service, Performer, Performed At
  → Authoritative Execution Fact Reported to CPOE
  → Authoritative Execution Fact Published to Tata Rekening
```

### 10.8 Ad Hoc Tindakan

```text
Immediate Patient Need Identified
  → Permitted Authority Basis Determined
  → Existing Tarif Service Identity Referenced
  → RUANG RANAP Service Performed
  → Service Execution Fact Recorded with Truthful Chronology
  → Subsequent Authorization Requested When Required
  → Execution Fact Reported to CPOE When CPOE Coordination Applies
  → Execution Fact Published to Tata Rekening
```

### 10.9 RUANG RANAP Work Not Executed

```text
RUANG RANAP Service Work Is Pending
  → Source Cancels/Withdraws Work or Execution Does Not Occur
  → Work Coordination State Updated
  → CPOE Informed When an Order Exists
  → No Service Execution Fact Published
```

### 10.10 Execution Correction

```text
Execution Error Identified
  → Original Execution Preserved
  → Correction or Entered in Error Recorded
  → CPOE Coordination Corrected When Applicable
  → Versioned Execution Correction Fact Published to Tata Rekening
```

### 10.11 Legacy Order Coexistence

```text
Pre-Cutover OrderTdk Remains Read-Only History
  → Native CPOE Creates New Orders After CutoverAt
  → RNA Records Authoritative Execution Fact
  → Billable Execution Creates/Links One Tindakan by ServiceExecutionFactId
  → Non-Billable Execution Creates No Tindakan
  → Source Labels and History Preserved; Duplicate Tindakan Prevented
```

### 10.12 RUANG RANAP Execution Fact to Tata Rekening

```text
RUANG RANAP Records “Service X Was Executed by Performer Y at Time Z”
  → If Billable, RNA Publishes the Authoritative Execution Fact
  → Tindakan Owner Creates or Resolves One Tindakan by ServiceExecutionFactId
  → Tata Rekening Acknowledges with TindakanId or Reconciliation Outcome
  → If Non-Billable, No Tindakan Is Created
```
