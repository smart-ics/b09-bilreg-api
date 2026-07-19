# CPOE ↔ RNA Integration

**Status:** Approved v1 semantic contract; design closed; implementation pending.

This document is optimized for AI implementation agents. Normative terms such as **must**, **must not**, and **may** describe target contract semantics, not evidence that code already exists.

## 1. Purpose and Scope

This integration lets CPOE hand authorized Clinical Order work to RUANG RANAP Operational Management (RNA), lets RNA coordinate that work without taking order authority, and lets RNA return authoritative ward execution facts so CPOE can evaluate its own occurrence, fulfilment, and closure state.

It also carries CPOE-owned instruction changes and terminations to RNA, RNA-owned execution corrections to CPOE, and exceptional RNA execution evidence to CPOE when subsequent accountability is required.

It does not:

- transfer Clinical Order intent, authorization, amendment, cancellation, discontinuation, completion, or reconciliation authority to RNA;
- transfer ward execution evidence or correction authority to CPOE;
- make dispatch, receipt, assignment, preparation, or transport acknowledgement proof of execution;
- make an RNA execution fact sufficient for CPOE fulfilment when the Order Definition requires additional occurrences, results, or documentation;
- define Service, performer, outcome, quantity/unit, documentation, or Completion Criterion policy;
- establish Charge Eligibility or any financial consequence;
- replace specialized Laboratory, Radiology, Operating Theatre, Pharmacy, Rehabilitation, or other executing-domain contracts;
- prescribe HTTP, messaging, broker, serialization, controller, or database design.

### 1.1 Behavior status

| Status | Meaning for this boundary |
|---|---|
| **Existing** | Bilreg has transaction, audit, explicit persistence, and feature-local durable retry patterns. Legacy `OrderTdk` and `Tindakan` exist; the target contract replaces new `OrderTdk` while retaining `Tindakan` as the billable-execution record. |
| **Target** | Every business contract, inbox/outbox obligation, acknowledgement, query, correction, and reconciliation rule in this document. |
| **Deferred** | Decisions in Section 11 and native CPOE/RNA implementation and cutover. Safe interim behavior applies until each owner decides. |

## 2. Context Ownership

| Business concern | Authoritative owner | Consumer / collaborator |
|---|---|---|
| Clinical Order identity, instruction, definition version, authorization, priority, requested timing, Destination, and instruction revision | CPOE | RNA |
| Order and occurrence lifecycle, clarification coordination, Completion Criterion, fulfilment/closure decision, outstanding responsibility, cancellation, discontinuation, and Entered in Error | CPOE | RNA may observe or request clarification |
| Destination actor's structured clarification request | RNA actor as the submitting authority; CPOE owns the resulting clarification/hold state | CPOE |
| RNA pending-work projection, optional Performer assignment, preparation, work start, and ward work visibility | RNA | Internal to RNA; CPOE does not consume these progress facts |
| Actual RUANG RANAP Service Execution: eligible referenced Service when billable or free-text description when non-billable, actual Performer, and Performed At | RNA | CPOE and other authorized consumers |
| Correction or Entered in Error decision for an RNA Service Execution Fact | RNA under an approved correction-authority policy | CPOE reconciles its own state |
| Exceptional-order accountability, subsequent authorization, overdue status, and escalation | CPOE / Clinical Governance | RNA supplies truthful execution and authority-basis evidence |
| Service identity and Service Definition/configuration | Tarif or another explicitly named definition owner | CPOE and RNA reference it |
| Clinical documentation content | NERS or its explicitly named documentation owner | CPOE stores a reference only when required; RNA does not copy it |
| Charge Eligibility and every financial consequence | The explicitly approved fulfilment/financial authority; RNA domain currently forbids RNA from deciding or storing eligibility | Outside this contract |
| Contract delivery, attempt, and acknowledgement state | Each sender/receiver integration boundary | Operational support |

Rules:

1. No context may write the other context's aggregate or persistence directly.
2. A local projection, reference, or display snapshot never becomes foreign authority.
3. RNA must not manufacture, amend, cancel, discontinue, fulfil, or close a Clinical Order.
4. CPOE must not manufacture, overwrite, or delete RNA execution evidence.
5. CPOE Generic Fulfilment must not execute an obligation routed to RNA; one routed occurrence must not produce duplicate authoritative execution evidence.

## 3. Collaboration Overview

### 3.1 Ordered ward service

```text
CPOE authorizes and dispatches an order/occurrence to RNA
→ RNA receives the obligation idempotently as pending work
→ no acceptance is required and RNA cannot reject the Clinical Order
→ authorized RNA actor may request structured clarification; CPOE owns the response/hold state
→ RNA may assign or coordinate internal progress; none of these facts is sent to CPOE or treated as execution
→ Performer performs the service
→ RNA commits ServiceExecutionRecorded and outbound obligations
→ CPOE ingests the fact idempotently
→ CPOE evaluates occurrence outcome, Completion Criterion, and remaining responsibility
→ CPOE may become Fulfilled/Closed only under its own rules
```

### 3.2 Instruction change or termination

```text
CPOE commits amendment, cancellation, discontinuation, or Entered in Error
→ CPOE publishes the new authoritative order revision/fact
→ RNA applies only a newer `InstructionRevision` to pending work and retains prior revisions
→ cancellation/discontinuation stops only work not executed before its `EffectiveAt`
→ execution with `PerformedAt` before `EffectiveAt` remains valid input to CPOE
→ execution with `PerformedAt` after `EffectiveAt` is preserved and marked `ReconciliationRequired`
→ a later RNA correction causes CPOE to reevaluate occurrence, fulfilment, and closure
→ if automatic reevaluation is unsafe, CPOE opens `ReconciliationRequired`; neither context deletes or backdates a fact
```

### 3.3 Exceptional ward execution

```text
RNA records an Ad Hoc, Independent, Emergency, Verbal, Protocol-Based,
or late/retrospective execution with truthful chronology and authority basis
→ when CPOE accountability is required, RNA publishes ExceptionalExecutionRecorded
→ CPOE records the exceptional action/accountability without inventing prospective authorization
→ CPOE owns subsequent authorization, overdue handling, and escalation
```

### 3.4 Correction

```text
RNA preserves the original Service Execution Fact
→ authorized RNA actor commits a new correction or Entered in Error fact
→ RNA publishes the correction with its own identity and original-fact reference
→ CPOE preserves prior associations and reconciles its order/occurrence state
→ neither context rewrites history
```

## 4. Business Contracts

All listed contracts are **Target**. A business fact is produced only after its owner's local business transaction commits. A command request asks the receiver to apply a decision under its own authority and may be rejected. An acknowledgement confirms processing status; it is not a clinical or business-state transition.

### 4.1 CPOE → RNA

| Contract | Type | Trigger / commit point | Business meaning | Permitted RNA effect | RNA must not infer |
|---|---|---|---|---|---|
| `ClinicalOrderDispatchedToRna` | Business Fact | CPOE commits dispatch of an authorized order/occurrence and its outbound obligation | One current instruction revision is actionable at RNA Destination | Deduplicate; create/update one pending obligation; expose scoped work | Service occurred; RNA accepted it; assigned Performer is authorized; order is billable |
| `ClinicalOrderInstructionAmended` | Business Fact | CPOE commits a material authorized amendment and outbound obligation | A new instruction revision supersedes the prior revision for future/pending fulfilment | Apply in order; flag/reassess affected pending work; retain prior revision history | Prior completed execution is invalid; CPOE has decided RNA correction |
| `ClinicalOrderCancelled` | Business Fact | CPOE commits cancellation before fulfilment began | The order/occurrence is no longer to start | End matching pending work as source-cancelled when no RNA execution has committed | Delete history; invalidate committed execution; cancellation equals acknowledgement |
| `ClinicalOrderDiscontinued` | Business Fact | CPOE commits termination of future/remaining activity after activation/start/partial fulfilment | No new remaining occurrence may start beyond the stated effective boundary | Stop remaining pending work in scope; preserve completed executions | Completed executions are reversed or Entered in Error |
| `ClinicalOrderEnteredInError` | Business Fact | CPOE commits that the instruction should not have existed | The order is invalid as clinical intent, with history preserved | Remove unexecuted work from actionable work; raise reconciliation for any committed RNA execution | RNA execution automatically did not occur or may be deleted |
| `GetClinicalOrderState` | Query Contract | RNA needs authoritative current order/occurrence/revision state, including reconciliation after missing or conflicting facts | Read current CPOE truth | Use response for display, command-time validation, and reconciliation | A cached response remains authority after later revisions |
| `CpoeProcessingAcknowledgement` | Acknowledgement | CPOE processes an RNA fact or command request | States duplicate/new processing outcome and current receiver reference where available | Update delivery state only | Order is Fulfilled/Closed unless the acknowledgement explicitly carries a separately owned state snapshot |

### 4.2 RNA → CPOE

| Contract | Type | Trigger / commit point | Business meaning | Permitted CPOE effect | CPOE must not infer |
|---|---|---|---|---|---|
| `RequestOrderClarification` | Command Request | Authorized RNA actor identifies ambiguity/risk before execution | RNA asks CPOE to open structured clarification | Validate and create CPOE-owned clarification/hold state | Every unrelated order is held; RNA may change the instruction |
| `ServiceExecutionRecorded` | Business Fact | RNA atomically commits authoritative execution, audit, and outbound obligation | Referenced Service was actually performed by the stated Performer at `OccurredAt` | Associate RNA fulfilment reference; record occurrence/fulfilment summary input; evaluate CPOE Completion Criterion | Order is necessarily Fulfilled/Closed; result/document exists; Charge Eligibility exists |
| `ServiceExecutionCorrected` | Correction Fact | RNA commits an append-only correction, replacement, or Entered in Error decision and outbound obligation | A prior RNA execution fact is corrected without erasing it | Preserve history; update/supersede association; reevaluate CPOE-owned occurrence, fulfilment, closure, and reconciliation state | CPOE may edit RNA history or automatically recreate missing work |
| `ExceptionalExecutionRecorded` | Business Fact | RNA commits truthful exceptional execution evidence and determines that CPOE accountability applies | An action occurred without ordinary prospective native-CPOE flow under the declared basis and chronology | Record exceptional action/accountability; create required subsequent-authorization work only under approved CPOE policy | Prospective authorization occurred; every Ad Hoc/Independent action requires CPOE follow-up |
| `GetRnaExecutionState` | Query Contract | CPOE needs authoritative execution/correction/delivery state | Read current RNA truth by stable identity | Reconcile CPOE association and operational support state | Querying transfers correction authority to CPOE |
| `RnaProcessingAcknowledgement` | Acknowledgement | RNA processes a CPOE fact or command outcome | States duplicate/new processing outcome and RNA work reference where available | Update delivery state only | RNA accepted, started, or executed the service unless separately stated by an authoritative contract |

### 4.3 Acknowledgement outcomes

Every acknowledgement must distinguish at least:

- `AcceptedNew` — contract processed for the first time;
- `AcceptedDuplicate` — same identity and same semantic payload; prior outcome returned;
- `RejectedTechnical` — invalid version, missing required field, unknown/unauthorized source, or unknown reference;
- `RejectedBusiness` — command request conflicts with receiver-owned rules/current state;
- `DeferredReconciliation` — safe application requires a missing revision, source fact, or human decision.

Acknowledgement of receipt or processing never substitutes for a separately owned business fact.

## 5. Contract Payload Semantics

### 5.1 Common envelope

Every retriable contract carries:

| Field | Semantics |
|---|---|
| `ContractId` | Stable identity of this delivery obligation. Retries reuse it. |
| `ContractType` | Stable business-contract name. |
| `ContractVersion` | Major-compatible semantic version. |
| `SourceContext` | Authoritative producer: `CPOE` or `RNA`. |
| `SourceFactId` | Stable identity of the committed source fact or command request. |
| `SourceRevision` | Monotonic revision within the source fact/order/execution chain. |
| `OccurredAt` | Authoritative business time of the source fact. |
| `RecordedAt` | Source persistence time; audit/technical tracing only. |
| `CorrelationId` | End-to-end workflow correlation. |
| `CausationId` | Contract/fact/request that caused this fact, when applicable. |
| `FacilityId` | Facility/tenant scope for authorization and reconciliation. |

`OccurredAt` and `RecordedAt` are UTC instants. Business ordering uses `OccurredAt` plus the explicit revision; `RecordedAt` never replaces business time. When actual business time is unknown, the producer sets `OccurredAt = RecordedAt` and must not invent an earlier time.

### 5.2 `ClinicalOrderDispatchedToRna`

Minimum fields:

- `ClinicalOrderId` — authoritative CPOE identity;
- `OrderOccurrenceId` — required when the obligation is occurrence-specific; otherwise absent, not fabricated;
- `FulfilmentObligationId` — stable deduplication identity for one routed obligation;
- `InstructionRevision` and `OrderDefinitionVersionId`;
- `PatientId` and `RegId` as stable references required to identify the correct patient and registration; current Ward and Destination are carried separately;
- `DestinationId` and `DestinationWardId` identifying RNA scope;
- `Priority`, requested start/due timing, and the minimum structured instruction values required by the bound Order Definition;
- `OrderingActorId` and `AuthorizerId` as accountable references;
- `AuthorizationOccurredAt` and `DispatchOccurredAt`.

CPOE does not supply or map a Tarif `ServiceId`. RNA receives sufficient order detail for the user to understand the requested work, resolves active eligible Services through its authoritative `Bangsal → Layanan → Tarif.AllowedLayanan` query, and revalidates the chosen billable Service when saving execution. If Tarif is unavailable RNA exposes `ServiceDependencyUnavailable`; if the successful query has no result it exposes `NoEligibleService`. Either outcome blocks a billable save only; non-billable execution uses required description and no `ServiceId`. Exact transport field shapes may be refined in the API contract, but an adapter must not drop required instruction values or copy unnecessary clinical narrative/demographics.

### 5.3 CPOE amendment and termination facts

Common minimum fields:

- `ClinicalOrderId`;
- `OrderOccurrenceId` or affected-occurrence scope when applicable;
- `InstructionRevision`;
- `EffectiveAt`;
- `DecisionActorId`;
- `ReasonCode` and/or sanitized accountable reason where the CPOE domain requires one.

`ClinicalOrderInstructionAmended` additionally carries the new minimum structured instruction or an authoritative retrievable revision reference. `ClinicalOrderDiscontinued` identifies future/remaining occurrence scope. `ClinicalOrderEnteredInError` never asserts that a physical RNA execution did not occur.

### 5.4 RNA coordination command requests

Common minimum fields:

- `RequestId` as stable semantic retry identity;
- `ClinicalOrderId`, `FulfilmentObligationId`, and `OrderOccurrenceId` when applicable;
- `InstructionRevisionObserved`;
- `DestinationId` / `DestinationWardId`;
- `ReceiverActorId` and `DecisionOccurredAt`;
- accountable reason for rejection or structured question, urgency, and expected responder for clarification.

Actor identity is an attested business reference; authenticated user/service identity is baseline current-phase evidence, while contextual Ward authorization is a Phase-99 enforcement concern under ARCH-020.

### 5.5 `ServiceExecutionRecorded`

Minimum fields:

- `ServiceExecutionFactId` — stable RNA fact identity and primary fact idempotency key;
- `ServiceExecutionId` — RNA aggregate/reference identity;
- `ClinicalOrderId`;
- `OrderOccurrenceId` when applicable;
- `FulfilmentObligationId`;
- `InstructionRevisionExecuted`;
- `PatientId` and `RegId` stable references; current Ward and Destination remain separate routing facts;
- `ServiceId` — required for a billable execution and selected by RNA from Tarif services allowed for the Bangsal's mapped `Layanan`; absent for a non-billable execution;
- `NonBillableDescription` — required free text when `ServiceId` is absent;
- `PerformerId` — captured from the authenticated user when execution is saved;
- `PerformedAt` — captured as the save time and equal to envelope `OccurredAt`;
- `RecordedAt` and `RecorderActorId`;
- `DestinationWardId` and execution-place reference only when required for coordination;
- `ExecutionDocumentationReference` only when one exists and its owner permits association.

It must not contain billable/non-billable, eligibility, tariff amount, package, coverage, bill, journal, payment, invented outcome catalogue, or copied clinical documentation content.

### 5.6 `ServiceExecutionCorrected`

Minimum fields:

- `CorrectionFactId` — stable identity of this new correction fact;
- `OriginalServiceExecutionFactId`;
- `CorrectionRevision` — monotonic within the original fact's chain;
- `CorrectionKind` — `Corrected`, `Replaced`, or `EnteredInError` only when authorized RNA policy permits it;
- corrected values limited to RNA-owned semantics;
- `ReplacementServiceExecutionFactId` when replacement is used;
- `CorrectionReason`, `CorrectingActorId`, `CorrectedAt` as `OccurredAt`, and `RecordedAt`.

The original fact remains immutable. A correction must not silently modify a Clinical Order, Service Definition, clinical document, or financial record.

### 5.7 `ExceptionalExecutionRecorded`

Minimum fields:

- the RNA execution identity and execution fields from Section 5.5, without a fabricated `ClinicalOrderId`;
- `ExecutionSource` and `AuthorityBasisType`;
- source-specific accountable references available from RNA, such as emergency reason, verbal issuer/receiver and read-back, protocol identity/version, or actual instruction time;
- `SubsequentAuthorizationRequired` only when supplied by approved CPOE/Clinical Governance policy;
- late-entry reason when `PerformedAt` and `RecordedAt` differ and policy requires it.

RNA must not classify an action as prospectively authorized merely because CPOE later records accountability.

### 5.8 Query responses

`GetClinicalOrderState` returns the current authoritative order/occurrence state, current instruction revision, Destination, relevant termination/clarification condition, responsibility reference, and last processed RNA fact/request identities. It excludes unnecessary clinical content.

`GetRnaExecutionState` returns execution identity, source/occurrence correlation, current correction chain, authoritative validity, RNA business revision, and per-contract delivery state. Delivery state is not execution validity.

## 6. State and Responsibility Effects

| Current owner/state | Received contract | Receiver-owned result | Context performing transition |
|---|---|---|---|
| CPOE `Authorized` | `ClinicalOrderDispatchedToRna` at RNA | RNA pending work created; CPOE remains order owner | RNA creates work; CPOE already committed dispatch |
| CPOE `Dispatched` | `SubmitDestinationAcceptance` | CPOE may move to `Accepted` after validation | CPOE |
| CPOE `Dispatched` | `SubmitDestinationRejection` | CPOE may move to `Rejected` after validation | CPOE |
| Active CPOE order | `RequestOrderClarification` | CPOE may add `On Hold for Clarification` condition | CPOE |
| RNA pending work | `ClinicalOrderInstructionAmended` | Work references/reassesses new revision; old revision retained | RNA |
| RNA pending, not executed | `ClinicalOrderCancelled` | Work ends as source-cancelled; no execution fact | RNA |
| RNA pending/future work | `ClinicalOrderDiscontinued` | Remaining affected work ends; completed execution remains | RNA |
| RNA execution has `PerformedAt` before termination `EffectiveAt` | later-delivered cancellation/discontinuation | Preserve execution; CPOE reevaluates from business time, regardless of delivery order | Each context preserves its truth; CPOE decides order state |
| RNA execution has `PerformedAt` after termination `EffectiveAt` | execution/termination race | Preserve both facts and mark `ReconciliationRequired` | CPOE clinical/integration support resolves; no delete or backdating |
| CPOE occurrence outstanding | `ServiceExecutionRecorded` | Fulfilment association recorded and Completion Criterion reevaluated | CPOE |
| CPOE order with one execution | `ServiceExecutionRecorded` for another valid occurrence | Additional occurrence association; parent decision reevaluated | CPOE |
| CPOE association exists | `ServiceExecutionCorrected` | Association superseded/corrected; occurrence/order reevaluated | CPOE |
| CPOE occurrence/order is already closed | later `ServiceExecutionCorrected` | Reevaluate completion/closure; reopen or supersede state when existing rules are sufficient, otherwise mark `ReconciliationRequired` | CPOE; RNA correction remains immutable |
| No native prospective CPOE order | `ExceptionalExecutionRecorded` | Exceptional accountability may be created; no false prospective order | CPOE |

Responsibility rules:

1. CPOE retains Clinical Order coordination responsibility until its own state machine transfers or closes it.
2. RNA becomes responsible for ward work coordination only after it has a valid routed obligation; this does not transfer Clinical Order ownership.
3. Acknowledgement, delivery failure, or query availability never transfers responsibility.
4. When a contract is missing, the source fact remains valid and the receiver remains in its last valid local state; recovery is eventual.
5. CPOE completion is never a side effect performed inside the RNA transaction.

## 7. Reliability and Consistency

### 7.0 Approved delivery topology

CPOE and RNA use a **hybrid durable-push plus authoritative-query topology**:

- The sender atomically commits its business fact and a durable outbox obligation.
- A background delivery worker pushes each obligation at least once.
- The receiver applies it through a durable inbox, deduplicates by stable contract identity, commits the processing outcome locally, and returns a durable acknowledgement.
- Retry reuses the same identity and never repeats source or receiver business behavior.
- Failed, stale, permanently rejected, or conflicting obligations remain visible as recovery work.
- `GetClinicalOrderState` and `GetRnaExecutionState` are authoritative pull queries for reconciliation when delivery or acknowledgement is missing or uncertain.
- A direct in-process adapter may optimize transport when CPOE and RNA are co-deployed, but it must preserve the same outbox, inbox, identity, acknowledgement, retry, and reconciliation semantics.

### 7.1 Delivery model

The semantic contract uses a **mixed model**:

- direct synchronous application orchestration may be used when both modules are in one process and immediate actor feedback is required;
- durable at-least-once delivery is required across a process/system boundary and for committed facts whose loss would strand work or state;
- authoritative read-on-demand queries support command-time validation and reconciliation;
- no exactly-once transport or distributed transaction is assumed.

Transport choice does not change identities, ownership, or state effects.

### 7.2 Idempotency

| Contract family | Idempotency scope |
|---|---|
| CPOE dispatch | `SourceContext + FulfilmentObligationId + InstructionRevision` |
| CPOE amendment/termination | `SourceContext + ClinicalOrderId + SourceFactId + SourceRevision` |
| RNA coordination request | `SourceContext + RequestId` |
| RNA execution | `SourceContext + ServiceExecutionFactId` plus uniqueness of active execution for the same actual `OrderOccurrenceId`/obligation |
| RNA correction | `SourceContext + CorrectionFactId`; chain ordered under `OriginalServiceExecutionFactId` |
| Exceptional execution | `SourceContext + ServiceExecutionFactId` |

Rules:

1. Same identity and semantically identical payload returns the recorded prior outcome without repeating business behavior.
2. Same identity with a conflicting payload is quarantined as `ReconciliationRequired`; last-write-wins is forbidden.
3. A retry reuses the original identity and revision.
4. A new instruction revision, correction, or replacement receives a new source fact identity; it is not a retry.
5. One occurrence/obligation creates one RNA pending-work item but may produce zero or more distinct execution facts. Each saved service or non-billable description receives its own stable `ServiceExecutionFactId`; replay of that identity must not duplicate it.

### 7.3 Ordering

Ordering is required:

- per `ClinicalOrderId` by `InstructionRevision` for instruction, cancellation, discontinuation, and Entered in Error facts;
- per `OrderOccurrenceId` / `FulfilmentObligationId` where occurrence-specific work is used;
- per `OriginalServiceExecutionFactId` by `CorrectionRevision`;
- per `ServiceExecutionFactId` before its correction.

No global cross-order ordering is required. A missing predecessor is held for bounded reordering or resolved by authoritative query. Lower revisions are duplicate/stale; an unrecognized higher revision must not be applied blindly.

`OccurredAt` establishes business chronology, but an explicit owner revision resolves delivery order. `RecordedAt` is never a sequencing substitute.

### 7.4 Transaction boundaries

CPOE atomically commits, as applicable:

- its Clinical Order decision/history and aggregate version;
- explicit audit;
- stable outbound dispatch, amendment, termination, or acknowledgement obligation.

RNA atomically commits, as applicable:

- inbox/request outcome and pending-work projection reference;
- its Service Execution or append-only correction history;
- explicit audit;
- stable outbound execution/correction/exceptional obligation.

Receiver application and acknowledgement commit in a receiver-local transaction. Remote work never participates in the source transaction.

Therefore:

```text
CPOE dispatch may commit before RNA shows pending work.
RNA execution may commit before CPOE updates its Fulfilment Summary.
CPOE termination may commit while an RNA execution fact is in flight.
Delivery failure never rolls back a valid committed source fact.
```

### 7.5 Retries

- Retry retryable failures with the same stable identity and recorded attempt count.
- Use bounded backoff and stale-processing recovery; exact timing is operational configuration, not business policy.
- Stop automatic retry for permanent authorization, semantic, version, or conflicting-payload rejection and expose reconciliation.
- Retry each destination and each correction independently.
- Manual retry replays delivery/receiver application; it never reruns the original human action or creates a new fact identity.

## 8. Failure and Reconciliation

| Failure case | Required behavior | Recovery owner | Prohibited behavior |
|---|---|---|---|
| RNA unavailable after CPOE dispatch | Keep order/dispatch committed; retain and retry obligation | CPOE integration support | Re-authorize or create a second fulfilment obligation |
| CPOE unavailable after RNA execution | Keep execution committed; retry same fact ID; expose pending delivery | RNA integration support | Delete/re-perform the execution |
| Duplicate delivery | Return prior outcome | Receiver | Repeat order/work/execution transition |
| Unknown order/occurrence/obligation | Reject technically; preserve source fact; query authority | Both integration supports | Match by patient name or silently create an order |
| Ineligible/unknown selected Service, or unknown patient, registration, Destination, or Ward reference | Prevent save or quarantine/reject and reconcile with authoritative owner | RNA/receiver support | Bypass `Bangsal → Layanan → Tarif.AllowedLayanan`, substitute display text as identity, or create local master data |
| Missing instruction revision | Hold and retrieve authoritative state | RNA | Apply future revision blindly |
| Stale coordination command | Return receiver-owned business conflict/current reference | CPOE | Overwrite newer Clinical Order state |
| Termination received after RNA execution committed | Preserve both facts; CPOE ingests execution and reevaluates; flag conflict if lifecycle cannot be resolved automatically | CPOE clinical/integration support | Backdate termination or erase execution |
| Execution arrives after CPOE termination because delivery was delayed | Validate `PerformedAt`, termination effective time, and revisions; preserve both; apply only CPOE-approved state rules | CPOE | Reject evidence solely because it arrived late |
| Correction before original | Hold/quarantine, retrieve original, then apply in chain order | CPOE | Treat correction as unrelated execution |
| Correction delivery failure | Retry correction independently with same ID | RNA | Mutate and republish the original fact |
| Execution marked Entered in Error after CPOE used it | Preserve history; reevaluate occurrence/order; expose unresolved work if no approved automatic consequence | CPOE | Silently recreate RNA work or delete prior CPOE audit |
| Permanent rejection | Move to visible `ReconciliationRequired` with sanitized reason | Sender plus receiver support | Infinite silent retry |
| Same ID, conflicting payload | Quarantine and investigate | Both architecture/data owners | Last-write-wins |
| Partial downstream success | Track outcome per destination/contract | Source integration support | Treat one consumer's acknowledgement as every consumer's success |

### 8.1 Authoritative reconciliation queries

Minimum capabilities:

- CPOE can query current order, occurrence, obligation, instruction revision, Destination, lifecycle, and last processed RNA identity by stable ID.
- RNA can query execution, original/correction chain, source correlation, authoritative validity, and delivery state by stable ID.
- Both can query processing outcome by `ContractId` / request identity.

### 8.2 Reconciliation rules

1. CPOE source truth wins for Clinical Order facts; RNA source truth wins for Service Execution facts.
2. Each receiver independently applies its owned transition; reconciliation never directly edits foreign persistence.
3. Recovery replays a committed obligation or re-queries authority; it does not repeat clinical execution or actor decisions.
4. Manual recovery may retry, retrieve missing revisions, link an orphan after evidence-based review, or record a permanent technical disposition.
5. Manual recovery must not fabricate an order/occurrence, change `PerformedAt`, bypass correction authority, or generate a new ID to evade a conflict.

### 8.3 Observable integration states

Use delivery states separate from business state:

- `Pending`
- `InProgress`
- `Acknowledged`
- `FailedRetryable`
- `RejectedTechnical`
- `RejectedBusiness`
- `ReconciliationRequired`

## 9. Security and Audit

### 9.1 Authentication

Allowed mechanisms are a trusted authenticated in-process application identity or an authenticated service principal across a system boundary. Actor-facing commands also require authenticated human context. A caller-supplied actor ID is never sufficient evidence.

### 9.2 Authorization

**Phase boundary (ARCH-020).** The current implementation assumes only baseline authentication and coarse-grained application access. Fine-grained contextual authorization, Ward scope, professional competency, actor attestation, and service-principal enforcement are intentionally out of scope for the current phase and deferred to Phase-99. The rules below remain target contract and business-ownership semantics; enforcement is deferred.

- Only CPOE's authoritative application boundary may publish Clinical Order facts or answer order-state queries.
- Only RNA's authoritative application boundary may publish RNA execution/correction facts or answer execution-state queries.
- Service principals are authorized by source context, contract type, facility, Destination, and Ward scope.
- Clarification, execution, and correction require contextual human authority in the owning application; the service principal is not the clinical actor. RNA has no accept/reject decision for a dispatched Clinical Order.
- RNA correction follows the approved authority matrix in `GAP-RNA-011`; contextual enforcement details remain subject to the ARCH-020 Phase-99 boundary.
- Exceptional follow-up follows the policy approved by `GAP-RNA-010`: authorization is due within 24 hours of `OccurredAt` or before discharge, whichever is earlier; acknowledgement is not authorization; overdue and late review preserve the original execution chronology.
- Query access is patient/care-relationship and operational-scope constrained.

Contracts and logs must exclude unnecessary demographics, diagnosis, free clinical narrative, documentation content, financial data, credentials, and tokens.

### 9.3 Audit

Record at minimum:

- source context, source fact ID, source revision;
- contract ID, type, and version;
- Clinical Order, occurrence/obligation, and RNA execution/correction identities as applicable;
- patient/registration reference and facility/Destination/Ward scope;
- business time, recorded time, delivery time;
- sender and receiver identity;
- accountable human actor where applicable;
- correlation and causation identity;
- attempt count, acknowledgement outcome, receiver state reference;
- sanitized failure code/reason.

Aggregate history remains business truth. Integration audit does not replace CPOE order history or RNA execution/correction history.

## 10. Versioning and Compatibility

1. Every contract carries `ContractVersion`.
2. Additive optional fields may remain within the current major version when they do not change interpretation.
3. Changes to meaning, source identity, required fields, idempotency scope, revision semantics, state/responsibility effect, or correction behavior require a new major version.
4. Producers must not remove required fields within a supported major version; consumers ignore unknown optional non-critical fields.
5. The initial release supports only contract major version `v1`; no compatibility overlap is required because no earlier native CPOE/RNA contract or production obligation exists.
6. Historical replay uses the version under which the source fact was committed.
7. Before introducing `v2` or another breaking change, CPOE and RNA architecture owners must approve support lifetime, deprecation overlap, replay/upcasting, correction compatibility, and treatment of pending inbox/outbox obligations.
8. Operations owns initial deployment, monitoring, rollback, and verification that no delivery backlog is lost. Rollback disables new native intake and never deletes committed `v1` facts.

### 10.1 Approved legacy cutover

Cutover is asymmetric and forward-only per approved facility/unit `CutoverAt`:

- Native CPOE replaces `OrderTdk` for every new order created after `CutoverAt`; historical `OrderTdk` remains read-only with its source identity and label.
- `Tindakan` is not replaced. It remains the billable-execution record created from a billable native CPOE/RNA `ServiceExecutionFact`.
- One billable `ServiceExecutionFactId` creates at most one `Tindakan`, linked by `ClinicalOrderId`, `OrderOccurrenceId`, `ServiceExecutionFactId`, and selected `ServiceId`; retry returns the existing record.
- A non-billable execution creates no `Tindakan`.
- Execution correction remains append-only and invokes the approved downstream correction process; it never overwrites RNA execution history.
- Historical views expose legacy and native records with explicit source labels.
- Ambiguous legacy identity is quarantined for reconciliation and never relabeled as native CPOE intent.
- Rollback stops new native intake and may restore legacy `OrderTdk` intake, but never deletes or recreates committed native orders, execution facts, or linked `Tindakan` records.

## 11. Open Decisions

| ID | Decision | Owner | Blocks | Safe interim behavior |
|---|---|---|---|---|
| `CPOE-RNA-OD-001` — CLOSED | CPOE dispatches stable `ClinicalOrderId`, `OrderOccurrenceId`, `FulfilmentObligationId`, `InstructionRevision`, patient/care references, destination RNA, order detail, and requested timing; it does not select or map a Tarif `ServiceId`. RNA resolves eligible services through `Bangsal → Layanan → Tarif.AllowedLayanan`. One occurrence may produce zero or more execution facts. Billable execution requires an eligible `ServiceId`; non-billable execution requires free-text description instead. Saving immediately records execution with authenticated user as `PerformerId` and save time as `PerformedAt`. `ServiceId` may change only through append-only correction, and CPOE does not validate it. | CPOE + RNA + Tarif owners | No policy block; payload schema and eligibility-query implementation remain | Do not invent a static CPOE-to-Tarif mapping, allow an ineligible service, or infer one execution per occurrence. |
| `CPOE-RNA-OD-002` — CLOSED | No formal RNA acceptance is required: a valid dispatched occurrence automatically becomes RNA work. RNA cannot reject a Clinical Order; only CPOE may cancel or discontinue it. An authorized RNA actor may request clarification with a structured reason, and the occurrence remains pending/held until CPOE responds. | CPOE Clinical Governance + RNA Operations | No policy block; clarification command implementation remains | Never infer acceptance from receipt. Do not implement RNA rejection or let clarification amend the order. |
| `CPOE-RNA-OD-003` — CLOSED | CPOE consumes clarification plus final `ServiceExecutionRecorded`, `ServiceExecutionCorrected`, Entered-in-Error correction, and `ExceptionalExecutionRecorded` facts only. Viewing, assignment, preparation, and work-start progress remain internal RNA facts. | CPOE + RNA domain owners | No policy block; final-fact delivery remains | Do not publish or infer CPOE fulfilment progress from internal RNA coordination. |
| `CPOE-RNA-OD-004` — CLOSED | Apply amendments and terminations by increasing `InstructionRevision` and business-effective time. Cancellation/discontinuation stops only work not executed before `EffectiveAt`. Execution before that boundary remains valid input; execution after it is preserved but marked `ReconciliationRequired`. A correction remains allowed after CPOE closure and triggers reevaluation; CPOE applies its existing rules or opens reconciliation when automatic resolution is unsafe. | CPOE domain owner + RNA Operations/Clinical Governance | No policy block; automated reevaluation and reconciliation implementation remain | Preserve every fact; never delete, backdate, or resolve by message arrival order. |
| `CPOE-RNA-OD-005` — CLOSED | Approved subsequent-authorization period, accountable authorizer, escalation chain, acknowledgement, and terminal handling for exceptional RNA execution. Authorization is due within 24 hours of `OccurredAt` or before discharge, whichever is earlier. The attending/clinically responsible physician is accountable, falling back to the designated on-call physician; escalation proceeds to on-call/service lead and then Clinical Governance. Acknowledgement accepts the task but is not authorization. CPOE sets terminal `Authorization Overdue` after the deadline and records later review as late review. | CPOE + Clinical Governance | `ExceptionalExecutionRecorded`; GAP-RNA-010 CLOSED | Preserve execution and truthful chronology; never self-authorize or represent overdue/late review as prospective or timely authorization. |
| `CPOE-RNA-OD-006` — CLOSED | Approved RNA execution correction authority, second review, evidence, and disputed-correction path. | Clinical Governance + RNA Operations | `ServiceExecutionCorrected`; GAP-RNA-011 CLOSED | Authorized care-team actors may report; owning-Ward Head Nurse may finalize ordinary corrections without self-approval; material, identity, replacement, Entered in Error, and disputed corrections require an independent Clinical Governance-authorized second reviewer. Preserve the original and hold correction when authority/evidence is unclear. |
| `CPOE-RNA-OD-007` — CLOSED | Use hybrid durable push plus authoritative pull reconciliation. The sender commits its fact and outbox atomically; a worker pushes at least once; the receiver deduplicates through a durable inbox, commits its outcome, and returns a durable acknowledgement. Retries reuse the same identity. Failed/stale/conflicting work remains visible. `GetClinicalOrderState` and `GetRnaExecutionState` reconcile uncertain delivery. An in-process optimization must preserve identical durability semantics. | CPOE and RNA architecture owners | No design block; inbox/outbox, workers, acknowledgements, retry policy, queries, and recovery tooling remain implementation work | Never use a best-effort direct call, treat acknowledgement as business truth, or repeat business behavior during retry. |
| `CPOE-RNA-OD-008` — CLOSED | Use an asymmetric forward-only cutover per facility/unit `CutoverAt`. Native CPOE replaces new `OrderTdk`; historical `OrderTdk` remains read-only. `Tindakan` is retained as the billable-execution record: one billable `ServiceExecutionFactId` creates at most one `Tindakan`, linked to order, occurrence, execution fact, and selected Service; non-billable execution creates none. Retry returns the existing `Tindakan`. Corrections are append-only. Rollback stops native intake without deleting or recreating committed native facts or `Tindakan`. | Data owner + CPOE/RNA/Tindakan architecture + Operations | No policy block; cutover tooling, idempotent Tindakan adapter, historical view, rollback procedure, and reconciliation remain implementation work | Preserve explicit source identity; quarantine ambiguity; never dual-create `OrderTdk`, duplicate `Tindakan`, or relabel legacy history as native. |
| `CPOE-RNA-OD-009` — CLOSED (DEFERRED TO PHASE-99) | Fine-grained service-principal identity, facility/Destination/Ward claims, actor attestation, and recovery-role mapping are explicitly deferred under ARCH-020. V1 assumes baseline authentication and coarse-grained application access while preserving all target authority rules. | Identity/Authorization + Clinical Governance | Phase-99 implementation only; no v1 design block | Do not claim contextual enforcement exists or weaken the documented business-authority rules. |
| `CPOE-RNA-OD-010` — CLOSED FOR V1 | The initial release supports only contract `v1`. No backward-compatibility or overlap window is required because no prior native contract or production facts exist. Version-support and deprecation policy must be approved before `v2` or any breaking change. Operations owns deployment, monitoring, rollback, and backlog verification; rollback disables new intake without deleting committed `v1` facts. Fine-grained authorization remains deferred to Phase-99. | CPOE and RNA architecture owners + Operations | No v1 design block; future breaking change is gated by a new version decision | Do not build speculative multi-version machinery for v1 or delete committed facts during rollback. |

Implementation agents must not close these decisions by convention.

## 12. Traceability

| Contract / rule | Domain rule | SOP / gap | Architecture / code evidence |
|---|---|---|---|
| Clinical Order is intent, not execution | `BR-CPOE-001`; `BR-RNA-036`, `BR-RNA-039` | SOP-RNA-S01 steps 1–4 | CPOE use cases: Dispatch; RNA architecture ordered-work target |
| CPOE owns authorization, change, termination, and completion | `BR-CPOE-003`, `020`–`033`; `BR-RNA-034` | SOP-RNA-S01 exception handling | CPOE architecture Clinical Order lifecycle use cases |
| RNA owns ward execution | `BR-CPOE-015`–`018`; `BR-RNA-026`–`038` | SOP-RNA-S01 | CPOE ADR-006; RNA Service Work and Execution module |
| One occurrence may produce multiple distinct executions without replay duplication | `BR-CPOE-064`; `BR-RNA-035`, `042`, `043` as amended by the approved dispatch contract | SOP-RNA-S01 completion criteria; CPOE-RNA-OD-001 CLOSED | Stable execution-fact identity and RNA occurrence correlation |
| Execution minimum: eligible Service for billable or description for non-billable, plus system-captured Performer and Performed At | `BR-RNA-038`, `045`, `052` as amended by the approved dispatch contract | SOP-RNA-S01 steps 4–6; CPOE-RNA-OD-001 CLOSED | RNA architecture UC-RNA-024/026 target |
| Execution does not automatically fulfil/close order | `BR-CPOE-020`–`025`; `BR-RNA-041` | SOP-RNA-S01 completion criteria | CPOE Evaluate Completion / Record Fulfilment Summary |
| CPOE termination preserves completed history | `BR-CPOE-030`–`034`; `BR-RNA-040` | SOP-RNA-S01 exceptions | CPOE amendment/cancellation/discontinuation use cases |
| Correction is RNA-owned and append-only | `BR-CPOE-018`; `BR-RNA-047`–`049`, `059` | SOP-RNA-S03; GAP-RNA-011 CLOSED | CPOE ADR-006/015; RNA UC-RNA-028 target |
| Exceptional execution preserves truthful chronology | `BR-CPOE-037`–`043`; `BR-RNA-031`–`034`, `045`, `046` | SOP-RNA-S02; GAP-RNA-010 CLOSED | CPOE Record Exceptional Action / Subsequent Authorization |
| Durable delivery and idempotency | CPOE audit/history rules; RNA fact identity rules | SOP-RNA-S01/S02/S03 failure paths | CPOE ADR-011/012; RNA architecture queue evidence |
| No native CPOE/RNA implementation exists | Not a domain rule | GAP-RNA-ARCH-016 | `CPOE-ARCHITECTURE.md` and `RNA-ARCHITECTURE.md` codebase evidence |
| Native CPOE replaces `OrderTdk`; `Tindakan` remains the idempotent billable-execution record | `BR-CPOE-061`–`064`; `BR-RNA-044`, `060`–`063` as amended by CPOE-RNA-OD-008 | SOP-RNA-S01 prerequisite; CPOE-RNA-OD-008 CLOSED | Historical `OrderTdk` is read-only; stable `ServiceExecutionFactId` prevents duplicate `Tindakan` |

Primary sources:

- `docs/contexts/cpoe/CPOE-DOMAIN.md`
- `docs/contexts/cpoe/CPOE-ARCHITECTURE.md`
- `docs/contexts/bangsal/RNA-DOMAIN.md`
- `docs/contexts/bangsal/RNA-ARCHITECTURE.md`
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S01-Pelaksanaan-RNA-Service-Berdasarkan-Clinical-Order.md`
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S02-Pelaksanaan-Tindakan-Ad-Hoc-atau-Independen.md`
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S03-Koreksi-RNA-Service-Execution.md`
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md`
- `docs/concepts/operational-events.md`
- `docs/ENGINEERING.md`
- `src/bilreg/Bilreg.Domain/ChargeContext/TindakanFeature/OrderTdkModel.cs`
- `src/bilreg/Bilreg.Application/ChargeContext/TindakanFeature/UseCases/TdkCreateTindakanByOrderCmd.cs`
- `src/bilreg/Bilreg.Application/ChargeContext/TindakanFeature/UseCases/TdkCreateTindakanCmd.cs`
- `src/bilreg/Bilreg.Domain/LabContext/LabOwareFeature/LabOwareOutboundQueueModel.cs`

## 13. AI Implementation Guardrails

Implementation agents must:

1. Treat this as a semantic contract, not a transport or persistence specification.
2. Implement application-boundary ports before adapters; never expose another context's repository/DAL as the contract.
3. Mark all work as Target until native CPOE and RNA modules exist and tests prove it.
4. Preserve source IDs, revisions, business time, and correction chains through every adapter.
5. Make receivers idempotent before enabling automatic retry.
6. Commit local business facts, audit, and mandatory outbound obligations atomically.
7. Keep order, RNA execution, delivery, and acknowledgement state separate.
8. Re-query the authoritative owner for missing/stale/conflicting state; never infer from legacy status text.
9. Never create CPOE Generic Fulfilment for work routed to RNA.
10. Never infer execution from dispatch, receipt, assignment, preparation, internal work start, legacy `Executed`, or an acknowledgement.
11. Never infer CPOE completion from one RNA fact without applying the bound Completion Criterion.
12. Preserve original facts and append corrections; do not physically delete business history.
13. Stop at Section 11 blockers instead of inventing clinical policy.

### Minimum acceptance tests

- Duplicate dispatch creates one RNA obligation and returns the prior outcome.
- Same contract ID with conflicting payload is quarantined.
- Out-of-order instruction revision is held and reconciled.
- Receipt, viewing, Performer assignment, preparation, and internal work start create no execution fact.
- One occurrence can create multiple distinct execution facts, while retry of any `ServiceExecutionFactId` applies it at most once.
- RNA execution remains committed while CPOE is unavailable and is later applied once.
- One execution fact does not close an order whose Completion Criterion remains unsatisfied.
- Cancellation before execution ends pending work without an execution fact.
- Delayed cancellation after committed execution preserves both facts and triggers CPOE evaluation/reconciliation.
- Discontinuation preserves completed occurrences and stops only future/remaining scope.
- Correction received before original is held safely.
- Correction preserves the original and causes CPOE reevaluation at most once.
- Exceptional execution never appears prospectively authorized.
- Unknown order/occurrence/service/reference does not match by display data.
- Baseline authentication and coarse-grained application access are required for actor-facing commands and integration endpoints; contextual source/Ward/actor-attestation enforcement is a Phase-99 acceptance criterion.

## 14. Final Summary

### 14.1 Authoritative ownership

- **CPOE** owns Clinical Order intent, authorization, instruction revision, Destination, coordination state, occurrence lifecycle, Completion Criterion, cancellation/discontinuation, exceptional accountability, fulfilment/closure, and reconciliation.
- **RNA** owns ward pending-work coordination, optional Performer assignment, actual RUANG RANAP Service Execution evidence, and append-only correction/Entered in Error of that evidence under approved policy.
- **Tarif or the named definition owner** owns Service identity/configuration; **NERS or another documentation owner** owns clinical documentation.
- Delivery and acknowledgement are integration state, never clinical truth.

### 14.2 Contracts in each direction

**CPOE → RNA:** `ClinicalOrderDispatchedToRna`, `ClinicalOrderInstructionAmended`, `ClinicalOrderCancelled`, `ClinicalOrderDiscontinued`, `ClinicalOrderEnteredInError`, `GetClinicalOrderState`, and `CpoeProcessingAcknowledgement`.

**RNA → CPOE:** `RequestOrderClarification`, `ServiceExecutionRecorded`, `ServiceExecutionCorrected` (including Entered in Error), `ExceptionalExecutionRecorded`, `GetRnaExecutionState`, and `RnaProcessingAcknowledgement`.

### 14.3 Implementation blockers

- Native `CpoeContext` and `RnaContext` code, persistence, handlers, inbox/outbox, acknowledgements, projections, and reconciliation tooling do not exist.
- Dispatch semantics are approved by `CPOE-RNA-OD-001`; the concrete payload schema, Bangsal-to-Layanan eligibility query, and execution persistence are not implemented.
- Fine-grained contextual clinical/Ward authorization and service-principal mapping are intentionally deferred to Phase-99 under ARCH-020; baseline authentication and coarse-grained application access remain the current assumption.
- Exceptional-action follow-up policy (`GAP-RNA-010`) and RNA correction authority (`GAP-RNA-011`) are approved; CPOE/RNA implementation, delivery, and reconciliation remain required.
- Legacy cutover policy is approved; facility/unit cutover configuration, idempotent `Tindakan` creation/linkage, historical access, reconciliation, and rollback tooling are not implemented.

### 14.4 Implementation follow-ups and deferred work

- Exceptional subsequent-authorization workflow implementation (deadline scheduling, assignment, acknowledgement, escalation, overdue transition, and late-review recording).
- RNA correction delivery, post-closure reevaluation, and reconciliation implementation.
- Phase-99 security mappings remain deferred; no v1 semantic-contract decision remains.
