# CPOE Domain

## 1. Business Overview

Computerized Provider Order Entry (CPOE) governs the expression, authorization, routing, coordination, and closure of clinical orders.

Its business purpose is to ensure that a clinical need becomes an explicit instruction, reaches a responsible fulfilment unit, receives an accountable outcome, and remains traceable throughout its lifecycle.

CPOE extends the legacy practice of recording `Tindakan` primarily for billing. A clinical order represents prospective clinical intent. It is not evidence that a service has been performed and does not itself justify a charge.

The domain covers:

- Structured order entry and authorization.
- Order classification and destination routing.
- Priority, requested timing, clinical indication, and instructions.
- Outstanding-order and receiver work management.
- Acceptance, rejection, and clarification.
- Coordination and recording of fulfilment outcomes.
- Amendment, cancellation, discontinuation, and correction history.
- Association with departmental fulfilment, result, execution documentation, and billing eligibility.
- Emergency, verbal, protocol-based, and retrospective actions.
- Auditability and responsibility for outstanding orders.
- Discharge reconciliation without blocking discharge.

The domain does not own:

- Routine nursing care performed within normal nursing responsibility.
- Detailed departmental fulfilment workflows where an authoritative fulfilment domain exists.
- The authoritative clinical result or execution document owned by another clinical domain.
- Tariff determination, financial coverage, bill calculation, or payment.
- Inventory consumption and stock control.
- Clinical review and follow-up of released results in Phase 1.

Where no specialized fulfilment domain exists, CPOE may temporarily govern Generic Fulfilment. This transitional responsibility does not change the boundary between a clinical order and evidence of its execution.

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Clinical Order | An authorized prospective clinical instruction requesting a service, intervention, examination, treatment, consultation, or other clinical activity for a patient. |
| Order Author | A permitted professional who prepares a Clinical Order. The author is not necessarily its authorizer. |
| Order Authorizer | A professional who assumes accountability for a Clinical Order within their professional authority and clinical privilege. |
| Ordering PPA | A Professional Care Provider who authors or authorizes a Clinical Order within their permitted scope. |
| Order Type | A clinically meaningful classification that determines the requested activity, required information, permitted authority, destination, and fulfilment completion criterion. |
| Order Set | A governed collection of related Clinical Orders intended for a defined clinical situation. Each contained order retains its own lifecycle. |
| Clinical Indication | The clinical reason or question that justifies a Clinical Order. |
| Priority | The clinically required urgency of an order, such as routine, urgent, or emergency. |
| Requested Timing | The intended time, schedule, frequency, duration, or condition for fulfilment. |
| Order Instruction | Information necessary for safe and appropriate fulfilment beyond the identity of the requested activity. |
| Destination | The organizational service responsible for receiving and coordinating fulfilment of an order. |
| Receiver | An authorized professional in the Destination who assumes operational handling of a dispatched order. |
| Outstanding Order | An order for which CPOE still has unresolved coordination responsibility. |
| Acceptance | The Destination's commitment to coordinate fulfilment of an order. |
| Rejection | The Destination's refusal to fulfil an order because it cannot or must not be performed as ordered. |
| Clarification Request | A formal request to resolve ambiguity, inconsistency, missing information, or safety concern before fulfilment continues. |
| Fulfilment | The performance of the activity requested by a Clinical Order. |
| Executing Domain | The clinical service that owns the authoritative fulfilment workflow and execution record for an Order Type. |
| Generic Fulfilment | A temporary CPOE-governed fulfilment capability for an ordered activity that has no specialized Executing Domain. |
| Fulfilment Outcome | The structured conclusion of fulfilment, including completion or a reason the activity was not performed. |
| Fulfilment Summary | The common operational facts CPOE retains about fulfilment while detailed execution remains owned by the Executing Domain. |
| Fulfilment Reference | The identity of the authoritative departmental fulfilment activity associated with a Clinical Order. |
| Result Reference | The association between a Clinical Order and its authoritative clinical result. |
| Execution Documentation Reference | The association between a Clinical Order and its authoritative execution or procedure documentation. |
| Completion Criterion | The business condition defined for an Order Type that determines when fulfilment is complete. |
| Occurrence | One required performance within a recurring or scheduled Clinical Order. |
| Amendment | An accountable revision to an authorized order that preserves the prior instruction and communicates the change to affected parties. |
| Cancellation | Termination of an order before clinical fulfilment has begun. |
| Discontinuation | Termination of future or remaining fulfilment after an order has become active, started, recurring, or partially fulfilled. |
| Entered in Error | A declaration that an order was recorded as a valid instruction when it should not have existed, without erasing its history. |
| Not Fulfilled | A final outcome stating that a requested activity was not performed, with an accountable reason. |
| Independent Tindakan | A clinical action performed under the professional's own authority without an individual prospective Clinical Order. |
| Ad Hoc Tindakan | An unplanned clinical action arising from an immediate patient need and classified by the authority under which it was performed. |
| Verbal Order | A Clinical Order communicated verbally or by telephone when prospective electronic authorization is impractical, requiring recorded read-back and subsequent authorization according to hospital policy. |
| Emergency Action | An urgent action performed before ordinary order authorization because delay would endanger the patient. |
| Protocol-Based Action | An action authorized by an approved clinical protocol when its defined triggering criteria are met. |
| Retrospective Order | An order recorded after execution that truthfully identifies the actual instruction time, execution time, later entry, and reason for delay. |
| Subsequent Authorization | Accountable confirmation after a verbal, emergency, or retrospective action. It does not imply that prospective authorization occurred. |
| Countersignature | Subsequent confirmation by the professional accountable for an exceptional order or action. |
| Charge Eligibility | A fulfilment fact indicating that an actual service or justified portion may be considered for billing. It is not a tariff or bill. |
| Discharge Reconciliation | Assessment and disposition of outstanding orders when an inpatient encounter ends. |
| Reconciliation Warning | A mandatory, non-blocking notice that unresolved orders exist at discharge and require acknowledgement, disposition, or escalation. |
| Carry Forward | Explicit continuation or replacement of an order under a different care context after discharge or transfer. |
| Clinical Result Review | A responsible clinician's acknowledgement, interpretation, and follow-up of a result. It is outside Phase 1. |

## 3. Business Capabilities

### 3.1 Clinical Order Definition

Defines structured Order Types, required clinical meaning, permitted authorizers, destinations, and Completion Criteria.

### 3.2 Order Authoring and Authorization

Captures clinical intent and establishes professional accountability before an order becomes actionable.

### 3.3 Order Routing

Directs an authorized order to the Destination responsible for its fulfilment.

### 3.4 Receiver Work Management

Maintains visibility and responsibility for orders awaiting acceptance, clarification, scheduling, execution, or resolution.

### 3.5 Acceptance, Rejection, and Clarification

Allows the Destination to commit to fulfilment, refuse fulfilment with a reason, or suspend affected work pending clarification.

### 3.6 Fulfilment Coordination

Tracks the common operational outcome of departmental fulfilment without taking ownership of specialized execution details.

### 3.7 Generic Fulfilment

Records execution for ordered activities that do not yet have a specialized Executing Domain.

### 3.8 Order Change Control

Governs amendment, cancellation, discontinuation, correction, and preservation of order history.

### 3.9 Exceptional Order Governance

Governs verbal, emergency, protocol-based, and retrospective actions, including required subsequent accountability.

### 3.10 Result and Documentation Association

Associates an order with its authoritative result and execution documentation without duplicating their clinical content.

### 3.11 Billing Eligibility Handover

Communicates that actual fulfilment may justify a charge while leaving financial determination to Tata Rekening.

### 3.12 Outstanding-Order Reconciliation

Ensures that responsibility is reassessed during ward transfer, DPJP replacement, and discharge.

### 3.13 Clinical Order Audit

Preserves authorship, authorization, responsibility, decisions, changes, exceptional authority, fulfilment outcome, and discharge acknowledgement.

## 4. Actors & Roles

| Actor or Role | Business Responsibility and Authority |
|---|---|
| Patient or Patient Representative | Provides consent where required, follows preparation instructions, and may accept or refuse a requested activity. |
| Order Author | Prepares a Clinical Order within permitted professional scope. A draft prepared by an author without authorization is not actionable. |
| Order Authorizer | Assumes clinical accountability for the order. May be a doctor or another PPA acting within professional authority and clinical privilege. |
| Responsible Clinician | Owns clinical follow-up responsibility within the current care context, including unresolved orders during changes of care responsibility. |
| DPJP | Holds principal clinical responsibility for the inpatient care context and receives transferred responsibility when the DPJP changes. |
| Receiver | Reviews dispatched orders and may accept, reject, or request clarification within Destination authority. |
| Fulfilment Coordinator | Coordinates scheduling, resources, preparation, location, and assignment within the Destination. |
| Clinical Verifier | Determines whether an order satisfies service-specific clinical or safety requirements before execution. |
| Performer | Executes the ordered activity within professional competency and records its outcome. |
| Result Author or Validator | Produces or validates the authoritative result when the Order Type requires one. |
| Discharge Actor | Reconciles or acknowledges outstanding orders when completing discharge. |
| Clinical Governance Authority | Defines Order Types, professional authority, clinical privilege, protocols, Completion Criteria, exceptional-order policies, and required countersignature periods. |
| Billing Officer | Handles the financial consequence of eligible fulfilled services under Tata Rekening policy. |

One person may perform several roles when hospital policy permits it. Role combination does not remove the distinct accountability of authoring, authorization, reception, verification, execution, or reconciliation.

## 5. Domain Objects

### 5.1 Clinical Order

Represents a clinical instruction for one patient and care context. It carries the Order Type, clinical indication, priority, requested timing, instructions, author, authorizer, Destination, current responsibility, and lifecycle.

### 5.2 Order Definition

Defines the stable business meaning of an Order Type, including:

- Required clinical information.
- Permitted author and authorizer roles.
- Permitted Destination.
- Whether acceptance, verification, scheduling, result, or execution documentation is required.
- Whether fulfilment is single, recurring, scheduled, or conditional.
- The Completion Criterion.
- Whether the ordered activity may create Charge Eligibility.

### 5.3 Order Authorization

Represents the accountable decision that a prepared Clinical Order may proceed. It identifies the authorizer, authority basis, and authorization time.

### 5.4 Order Responsibility

Identifies the clinical role and care context accountable for an Outstanding Order. Responsibility may transfer without changing original authorship or authorization.

### 5.5 Order Destination

Identifies the organizational service responsible for receiving and coordinating fulfilment.

### 5.6 Receiver Decision

Represents acceptance, rejection, or a Clarification Request made by the Destination, including the responsible actor, time, and reason.

### 5.7 Clarification

Represents a question that must be answered to resolve ambiguity or risk. It contains the question, requester, responsible responder, urgency, response, and resolution.

### 5.8 Order Occurrence

Represents one required performance within a recurring or scheduled order. An Occurrence may be fulfilled, omitted with reason, or terminated by discontinuation of the remaining order.

### 5.9 Fulfilment Summary

Represents the shared operational outcome required by CPOE:

- Fulfilment status.
- Start and completion times when applicable.
- Performer and place of execution when applicable.
- Not-performed reason when applicable.
- Optional explanatory note.
- Fulfilment, result, and execution-documentation references.

The Fulfilment Summary is not the authoritative specialized clinical execution record.

### 5.10 Generic Fulfilment Record

Represents authoritative execution evidence for an ordered activity only when no specialized Executing Domain exists. It identifies what was performed, by whom, when, where, the outcome, deviations, and any not-performed reason.

### 5.11 Order Amendment

Represents an accountable change to an authorized Clinical Order. It preserves the previous instruction, reason, amending authority, time, and effect on pending fulfilment.

### 5.12 Exceptional Authority Record

Identifies the basis for a Verbal Order, Emergency Action, Protocol-Based Action, or Retrospective Order, including required subsequent authorization.

### 5.13 Charge Eligibility

Represents a statement from fulfilment that an actual service, occurrence, or justified portion may be considered for billing. Tata Rekening independently determines the financial consequence.

### 5.14 Discharge Reconciliation

Represents the assessment of Outstanding Orders at discharge, their dispositions, acknowledged exceptions, transferred responsibility, and escalation where required.

## 6. Aggregates

### 6.1 Clinical Order Aggregate

**Aggregate Root:** Clinical Order

**Business responsibility:** Preserve the meaning, accountability, lifecycle, and current operational responsibility of one clinical instruction.

**Consistency boundary includes:**

- Order Authorization.
- Current Order Responsibility.
- Destination and Receiver Decisions.
- Clarifications.
- Occurrences.
- Amendments.
- Exceptional Authority Record.
- Fulfilment Summary.
- Result and documentation associations.
- Charge Eligibility association.

The Clinical Order Aggregate ensures that no order becomes actionable without valid authority, no material change loses its history, and no lifecycle outcome contradicts recorded fulfilment.

An Order Set does not create one indivisible lifecycle. Each contained Clinical Order remains independently acceptable, rejectable, amendable, cancellable, fulfillable, and charge-eligible.

### 6.2 Generic Fulfilment Aggregate

**Aggregate Root:** Generic Fulfilment Record

**Business responsibility:** Govern authoritative execution evidence where no specialized Executing Domain exists.

**Consistency boundary includes:**

- Assigned or actual Performer.
- Execution timing and place.
- Execution outcome.
- Not-performed reason.
- Execution deviations and supporting documentation association.

Generic Fulfilment exists only for ordered activities. It does not govern routine nursing care or independent professional documentation.

### 6.3 Discharge Reconciliation Aggregate

**Aggregate Root:** Discharge Reconciliation

**Business responsibility:** Preserve an accountable encounter-level disposition of Outstanding Orders without making unresolved orders a universal discharge prohibition.

**Consistency boundary includes:**

- Orders identified as outstanding at discharge.
- Disposition of each assessed order.
- Unresolved exceptions.
- Acknowledgement and reason for proceeding.
- Responsibility transferred after discharge.
- Required escalation.

## 7. Business Rules

### Clinical intent and authorization

**BR-CPOE-001** — A Clinical Order shall represent clinical intent and shall not be treated as evidence that the requested activity occurred.

**BR-CPOE-002** — A Clinical Order shall identify one patient, one care context, one Order Type, its clinical indication, priority, requested timing, instructions, author, and intended Destination as required by its Order Definition.

**BR-CPOE-003** — A draft order shall not be dispatched or fulfilled until it has valid authorization.

**BR-CPOE-004** — An Order Authorizer may be a doctor or another PPA acting within professional authority, clinical privilege, and the policy of the Order Type.

**BR-CPOE-005** — Authorship and authorization shall remain distinguishable even when performed by the same person.

**BR-CPOE-006** — An Order Set shall not remove the independent authorization, lifecycle, or outcome of each contained Clinical Order.

### Routing and receiver responsibility

**BR-CPOE-007** — Every authorized order shall have a Destination responsible for receiving it.

**BR-CPOE-008** — Acceptance means that the Destination commits to coordinate fulfilment; it does not mean that execution has begun or completed.

**BR-CPOE-009** — Rejection shall identify an accountable receiver and business reason.

**BR-CPOE-010** — A Clarification Request shall identify the question, requester, responsible responder, urgency, and resolution.

**BR-CPOE-011** — An unresolved clarification shall place the affected order on hold for clinical fulfilment.

**BR-CPOE-012** — Safe non-clinical preparation may continue during clarification when it neither changes the patient nor creates clinical risk.

**BR-CPOE-013** — Emergency fulfilment may proceed during unresolved clarification only when the emergency authority and reason for proceeding are recorded.

**BR-CPOE-014** — Clarification affecting one order shall not automatically suspend unrelated orders.

### Fulfilment and completion

**BR-CPOE-015** — The Executing Domain owns the authoritative execution record when a specialized fulfilment authority exists.

**BR-CPOE-016** — CPOE shall retain a structured Fulfilment Summary sufficient to determine whether its operational responsibility remains outstanding.

**BR-CPOE-017** — Free text may explain a fulfilment outcome but shall not replace structured fulfilment status or a structured not-performed reason.

**BR-CPOE-018** — Corrections to authoritative execution facts shall be made under the authority of the Executing Domain.

**BR-CPOE-019** — Generic Fulfilment may own execution only when no specialized Executing Domain exists for the ordered activity.

**BR-CPOE-020** — Each Order Type shall define its Completion Criterion.

**BR-CPOE-021** — A Clinical Order becomes Fulfilled only when its Order Type's Completion Criterion is satisfied.

**BR-CPOE-022** — Acceptance, scheduling, preparation, or execution start shall not by themselves mean that an order is Fulfilled.

**BR-CPOE-023** — A Clinical Order becomes Closed only when no CPOE coordination responsibility, unresolved clarification, required occurrence, or required fulfilment association remains.

**BR-CPOE-024** — Clinical Result Review is not required for closure during Phase 1.

**BR-CPOE-025** — An expected result or execution document is required for fulfilment only when the Order Definition makes it part of the Completion Criterion.

**BR-CPOE-026** — A Not Fulfilled outcome shall state why the requested activity was not performed.

### Change and termination

**BR-CPOE-027** — A change made before authorization may revise the draft without creating an Amendment.

**BR-CPOE-028** — A material change after authorization shall be recorded as an Amendment that preserves the previous instruction and identifies its reason and authority.

**BR-CPOE-029** — An Amendment affecting pending fulfilment shall be communicated to the responsible Destination.

**BR-CPOE-030** — Cancellation applies only before clinical fulfilment has begun.

**BR-CPOE-031** — Discontinuation stops future or remaining fulfilment after an order is active, started, recurring, or partially fulfilled.

**BR-CPOE-032** — Discontinuation shall not invalidate completed occurrences or valid execution history.

**BR-CPOE-033** — An order recorded against the wrong patient or otherwise created without legitimate clinical intent shall be marked Entered in Error rather than cancelled.

**BR-CPOE-034** — No order, authorization, amendment, termination, fulfilment, or exceptional-authority history may be erased from business history.

### Ad hoc and exceptional actions

**BR-CPOE-035** — Routine nursing care performed within normal nursing responsibility is outside CPOE.

**BR-CPOE-036** — A specifically ordered nursing tindakan may be governed by CPOE.

**BR-CPOE-037** — An Independent Tindakan shall be distinguished from an action for which prior authorization was required but absent.

**BR-CPOE-038** — A Verbal Order shall identify the issuer, receiver, instruction, read-back confirmation, and actual instruction time.

**BR-CPOE-039** — An Emergency Action may precede ordinary authorization when delay would endanger the patient, but its emergency basis, performer, action, and execution time shall be recorded.

**BR-CPOE-040** — A Protocol-Based Action shall identify the approved protocol, applicable version, triggering criteria, and performer.

**BR-CPOE-041** — A Retrospective Order shall distinguish instruction time, execution time, recording time, and the reason for delayed recording.

**BR-CPOE-042** — Required Subsequent Authorization shall confirm accountability without falsely representing that prospective authorization occurred.

**BR-CPOE-043** — Failure to obtain required Subsequent Authorization within the governed period shall remain an outstanding exception and shall not erase the actual execution.

### Responsibility and care transitions

**BR-CPOE-044** — Responsibility for an Outstanding Order belongs to a defined clinical role and care context, not permanently to its original author.

**BR-CPOE-045** — Ward transfer shall not silently cancel Outstanding Orders; their Destination, validity, and responsibility shall be reassessed.

**BR-CPOE-046** — Replacement of the DPJP shall transfer unresolved clinical responsibility to the succeeding DPJP or responsible care team without changing original authorship.

**BR-CPOE-047** — Discharge shall initiate reconciliation of all Outstanding Orders.

**BR-CPOE-048** — An Outstanding Order shall not universally block discharge.

**BR-CPOE-049** — Discharge with unresolved orders shall require explicit acknowledgement of a Reconciliation Warning.

**BR-CPOE-050** — Reconciliation shall preserve each unresolved order's status, disposition, acknowledgement, reason for proceeding, and post-discharge responsibility or escalation.

**BR-CPOE-051** — Discharge shall not silently cancel all Outstanding Orders.

**BR-CPOE-052** — An inpatient recurring order that is not intentionally continued shall have its remaining occurrences discontinued at discharge.

**BR-CPOE-053** — An order still clinically required after discharge shall be explicitly carried forward, converted, replaced, or assigned continuing responsibility.

### Results, documentation, and billing

**BR-CPOE-054** — An authoritative result or execution document shall remain owned by its responsible clinical domain.

**BR-CPOE-055** — CPOE shall associate the Clinical Order with available authoritative fulfilment, result, and execution-documentation references.

**BR-CPOE-056** — Creation, authorization, dispatch, acceptance, scheduling, or preparation of an order shall not create Charge Eligibility.

**BR-CPOE-057** — Charge Eligibility shall arise only from an actual fulfilment event or justified fulfilled portion defined by the responsible fulfilment policy.

**BR-CPOE-058** — Cancellation normally creates no Charge Eligibility.

**BR-CPOE-059** — Discontinuation shall preserve Charge Eligibility already created by valid completed occurrences or justified partial fulfilment.

**BR-CPOE-060** — Tata Rekening independently determines tariff, coverage, bundling, bill creation, adjustment, and payment from eligible fulfilment facts.

### Legacy coexistence and audit

**BR-CPOE-061** — CPOE is the canonical authority for prospective clinical intent introduced through CPOE.

**BR-CPOE-062** — A legacy departmental order may remain authoritative for its departmental fulfilment while retaining association with the originating Clinical Order.

**BR-CPOE-063** — A directly created legacy transaction shall be identified truthfully as legacy-originated or retrospective; it shall not be represented as a prospectively authorized native CPOE order.

**BR-CPOE-064** — One Clinical Order shall not create duplicate departmental fulfilment obligations through repeated handover.

**BR-CPOE-065** — Every material order decision shall identify the responsible actor, business time, reason where required, and resulting state.

## 8. State Machines & Lifecycles

### 8.1 Clinical Order Lifecycle

```text
Draft
  → Authorized
  → Dispatched
  → Accepted
  → In Fulfilment
  → Fulfilled
  → Closed
```

Business meaning:

| State | Meaning |
|---|---|
| Draft | Clinical intent is being prepared and is not actionable. |
| Authorized | A permitted professional has assumed accountability for the order. |
| Dispatched | The order has been handed over to its Destination. |
| Accepted | The Destination has committed to coordinate fulfilment. |
| In Fulfilment | Preparation or clinical execution has begun. |
| Fulfilled | The Order Type's Completion Criterion has been satisfied. |
| Closed | CPOE has no remaining coordination responsibility for the order in Phase 1. |

Permitted terminal alternatives:

| State | Meaning |
|---|---|
| Rejected | The Destination has refused the order with an accountable reason. |
| Cancelled | The order was terminated before clinical fulfilment began. |
| Discontinued | Future or remaining fulfilment was terminated after the order became active, started, recurring, or partially fulfilled. |
| Not Fulfilled | The requested activity reached a final non-performance outcome with a reason. |
| Entered in Error | The order should not have existed as a valid clinical instruction. |

An unresolved Clarification Request creates an `On Hold for Clarification` condition over an otherwise active order. Resolution returns the order to its appropriate lifecycle state, results in Amendment and renewed handling, or terminates the order through rejection, cancellation, discontinuation, or Entered in Error.

### 8.2 Occurrence Lifecycle

```text
Planned
  → Due
  → In Fulfilment
  → Fulfilled
```

Alternative outcomes are:

- Omitted with reason.
- Cancelled before execution.
- Discontinued as part of the remaining order.
- Not Fulfilled with reason.

The parent Clinical Order becomes Fulfilled only when all required Occurrences satisfy its Completion Criterion or when remaining Occurrences have a valid terminal disposition.

### 8.3 Clarification Lifecycle

```text
Requested
  → Responded
  → Resolved
```

A clarification may instead be withdrawn when the requester determines that no response is required. Responded does not mean Resolved; the receiver determines whether the response makes safe fulfilment possible.

### 8.4 Exceptional Authorization Lifecycle

```text
Exceptional Action Recorded
  → Subsequent Authorization Required
  → Authorized
```

If authorization is not completed within the governed period, the record becomes `Authorization Overdue`. The actual execution remains part of the clinical history.

Protocol-Based Action may be complete under the protocol's standing authority when individual countersignature is not required.

### 8.5 Discharge Reconciliation Lifecycle

```text
Outstanding Orders Identified
  → Orders Assessed
  → Dispositions Recorded
  → Reconciliation Completed
```

When an order remains unresolved:

```text
Unresolved Order Identified
  → Warning Acknowledged
  → Responsibility Assigned or Exception Escalated
  → Discharge May Proceed
```

Reconciliation completion means that unresolved conditions are visible and accountable. It does not mean every order has been clinically completed.

## 9. Domain Events

| Domain Event | Business Meaning |
|---|---|
| Clinical Order Drafted | A prospective clinical instruction has been prepared but is not actionable. |
| Clinical Order Authorized | A permitted professional has assumed accountability for the order. |
| Clinical Order Dispatched | The order has been handed over to its Destination. |
| Clinical Order Accepted | The Destination has committed to fulfilment coordination. |
| Clinical Order Rejected | The Destination has refused fulfilment with a reason. |
| Order Clarification Requested | The Destination or responsible party has raised a question that affects safe fulfilment. |
| Order Clarification Responded | A responsible party has supplied an answer. |
| Order Clarification Resolved | The order may proceed or has received another explicit disposition. |
| Order Fulfilment Started | Clinical preparation or execution has begun. |
| Order Occurrence Fulfilled | One required occurrence has met its Completion Criterion. |
| Clinical Order Fulfilled | The order has met its Completion Criterion. |
| Clinical Order Closed | CPOE has no remaining Phase 1 coordination responsibility. |
| Clinical Order Amended | An authorized instruction has been accountably revised. |
| Clinical Order Cancelled | An order has been terminated before fulfilment began. |
| Clinical Order Discontinued | Future or remaining fulfilment has been terminated. |
| Clinical Order Not Fulfilled | The requested activity ended without performance and with a reason. |
| Clinical Order Entered in Error | The order has been declared invalid as a clinical instruction while retaining its history. |
| Fulfilment Outcome Recorded | The Executing Domain or Generic Fulfilment has stated the structured operational outcome. |
| Clinical Result Made Available | An authoritative result has become associated with the order. |
| Execution Documentation Made Available | Authoritative execution documentation has become associated with the order. |
| Charge Eligibility Established | Actual fulfilment has created a fact that may be considered for billing. |
| Verbal Order Recorded | A verbally communicated instruction and read-back have been documented. |
| Emergency Action Recorded | An action performed under emergency authority has been documented. |
| Protocol-Based Action Recorded | An action has been performed under an approved protocol. |
| Retrospective Order Recorded | An order has been documented after execution with its actual chronology. |
| Exceptional Order Subsequently Authorized | Required accountability has been confirmed after the exceptional action. |
| Exceptional Authorization Became Overdue | Required subsequent accountability was not completed within policy. |
| Order Responsibility Transferred | Accountability for an Outstanding Order has moved to a succeeding role or care context. |
| Discharge Reconciliation Started | Outstanding orders are being assessed at encounter completion. |
| Reconciliation Warning Acknowledged | A discharge actor has explicitly acknowledged unresolved orders. |
| Outstanding Order Escalated | An unresolved order has been assigned for exceptional follow-up. |
| Discharge Reconciliation Completed | Outstanding orders have accountable dispositions or acknowledged exceptions. |

## 10. Business Workflows

### 10.1 Standard Clinical Order

```text
Clinical Need Identified
  → Clinical Order Authored
  → Clinical Order Authorized
  → Order Routed to Destination
  → Destination Accepts Order
  → Order Fulfilled
  → Required Result or Documentation Associated
  → Clinical Order Closed
```

### 10.2 Receiver Clarification

```text
Order Received
  → Ambiguity or Risk Identified
  → Clarification Requested
  → Affected Fulfilment Held
  → Clarification Responded
  → Order Resumed, Amended, or Terminated
```

### 10.3 Specialized Departmental Fulfilment

```text
Clinical Order Accepted
  → Departmental Fulfilment Begins
  → Authoritative Execution Recorded by Executing Domain
  → Fulfilment Summary Associated with Clinical Order
  → Required Result or Documentation Associated
  → Order Fulfilled and Closed
```

### 10.4 Generic Fulfilment

```text
Clinical Order Accepted
  → No Specialized Executing Domain Exists
  → Generic Fulfilment Performed
  → Execution Outcome Recorded
  → Order Fulfilled and Closed
```

### 10.5 Order Amendment

```text
Material Change Required
  → Amendment Authorized
  → Previous Instruction Preserved
  → Destination Informed
  → Pending Fulfilment Continues Under Amended Instruction or Is Reassessed
```

### 10.6 Cancellation and Discontinuation

```text
Order No Longer Required
  → Fulfilment History Assessed
  → Not Started: Cancelled
  → Active, Recurring, or Partially Fulfilled: Discontinued
  → Completed History and Eligible Fulfilment Preserved
```

### 10.7 Exceptional Action

```text
Immediate or Exceptional Need Identified
  → Verbal, Emergency, Protocol, or Retrospective Authority Identified
  → Action Performed and Truthful Chronology Recorded
  → Subsequent Authorization Completed When Required
  → Overdue Accountability Escalated When Not Completed
```

### 10.8 Ward Transfer or DPJP Replacement

```text
Care Context or Responsible Clinician Changes
  → Outstanding Orders Identified
  → Clinical Validity, Destination, and Responsibility Reassessed
  → Responsibility Transferred, Order Amended, or Order Terminated
```

### 10.9 Discharge Reconciliation

```text
Discharge Initiated
  → Outstanding Orders Identified
  → Each Order Assessed
  → Complete, Cancel, Discontinue, Carry Forward, or Escalate
  → Unresolved Exceptions Explicitly Acknowledged
  → Discharge Proceeds
```

### 10.10 Fulfilment to Billing

```text
Clinical Order Fulfilment Occurs
  → Executing Authority Determines Charge Eligibility
  → Eligible Fulfilment Handed to Tata Rekening
  → Tata Rekening Determines Financial Consequence
```

### 10.11 Legacy Departmental Coexistence

```text
CPOE Clinical Intent Authorized
  → Order Associated with Legacy Departmental Fulfilment
  → Legacy Department Performs Existing Fulfilment Responsibility
  → Fulfilment Outcome Returned to CPOE
  → Result, Documentation, and Billing Eligibility Associated
```

Direct legacy-originated activity follows its truthful legacy or retrospective classification and is not reclassified as prospectively authorized CPOE intent.
