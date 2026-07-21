# ADMISI ↔ RNA Integration

**Status:** Approved target integration contract; implementation pending  
**Primary gap addressed:** `GAP-RNA-ARCH-015`  
**Audience:** AI implementation agents, architecture owners, application developers, testers  
**Scope:** Admisi Rawat Inap and RUANG RANAP Operational Management  
**Transport:** Hybrid durable push plus authoritative query reconciliation

---

## 1. Purpose and Scope

This integration enables:

1. RNA to consume or read Admisi-owned Waiting List demand for a destination Ward.
2. RNA to publish a Ward rejection without taking ownership of the Waiting List.
3. RNA to publish an authoritative successful Accommodation Assignment fact.
4. Admisi to close its Waiting List as a consequence of that successful assignment.
5. RNA to publish an inter-ward Accommodation Release fact so Admisi can resume operational responsibility and create or route Waiting List work.
6. Both contexts to acknowledge delivery and reconcile duplicate, delayed, failed, stale, or corrected contracts without mutating the other context's persistence.

This integration does **not**:

- create a separate hand-off or Accepted business state;
- transfer Waiting List ownership to RNA;
- let RNA close, cancel, prioritize, or directly modify Admisi Waiting List persistence;
- let Admisi assign, release, correct, or directly modify RNA Accommodation persistence;
- carry inter-ward clinical handover content, which belongs to EMR;
- define UI routes, HTTP endpoints, message brokers, JSON schemas, or database tables;
- introduce optional placement constraints;
- treat transport acknowledgement as a business ownership transition.

---

## 2. Context Ownership

| Business concern | Authoritative owner | Consumer | Boundary rule |
|---|---|---|---|
| Admission and inpatient registration identity | Admisi / Registration | RNA | RNA references `RegistrationId`; it does not recreate Admission state. |
| Waiting List identity and lifecycle | Admisi | RNA | RNA may read or receive Ward-scoped demand but never modifies Admisi storage. |
| Waiting List destination Ward | Admisi | RNA | RNA processes only entries addressed to its authorized Ward. |
| Waiting List priority, cancellation, rerouting, and closure | Admisi | RNA may observe | RNA publishes facts; Admisi performs its own transition. |
| Ward review rejection | RNA | Admisi | Rejection is an RNA decision fact; responsibility remains with Admisi. |
| Mandatory Bed Assignability | RNA | Admisi may observe result | RNA validates bed existence/active state, destination Ward, Ready state, occupancy conflict, and capacity. |
| Accommodation Assignment | RNA | Admisi | Successful assignment is authoritative only after RNA commits it locally. |
| Accommodation Allocation identity and history | RNA | Admisi may reference | Admisi must not infer or recreate RNA allocation history. |
| Responsibility before successful assignment | Admisi | RNA | Viewing or reviewing Waiting List does not transfer responsibility. |
| Responsibility after successful assignment | RNA | Admisi | Transfer occurs because Accommodation Assignment succeeds, not because delivery is acknowledged. |
| Inter-ward Accommodation Release | RNA | Admisi | RNA owns the release fact; it does not create Admisi Waiting List state. |
| Responsibility after committed inter-ward release | Admisi | RNA may observe | RNA does not retain a transfer queue or destination responsibility. |
| Inter-ward clinical handover content | EMR | Admisi/RNA may reference | No clinical handover payload is copied into this contract. |
| Integration delivery and acknowledgement state | Each sender for its outbound obligation | Counterparty | Delivery state is not the underlying business state. |

### Ownership invariants

1. No contract grants direct write access to the other context's repository or tables.
2. No state is described as jointly owned.
3. Cached or projected Waiting List data in RNA remains non-authoritative.
4. Cached or projected Accommodation data in Admisi remains non-authoritative.
5. A valid locally committed fact remains valid when delivery fails.
6. Each receiving context independently decides and commits its own state effect.

---

## 3. Collaboration Overview

### 3.1 Initial or ordinary placement

```text
Admisi creates or maintains Waiting List
→ RNA reads or receives the Ward-scoped Waiting List entry
→ Ward reviews the entry
    → Ward rejects
        → RNA commits Placement Rejected fact
        → Admisi keeps Waiting List under its authority
    or
    → Ward assigns a bed
        → RNA atomically commits Accommodation Assignment
          and an outbound Accommodation Assigned obligation
        → responsibility is now RNA's
        → Admisi receives Accommodation Assigned
        → Admisi closes its Waiting List
```

There is no separate acceptance step and no intermediate responsibility state.

### 3.2 Inter-ward movement

```text
Source RNA commits Accommodation Release
and an outbound Inter-Ward Accommodation Released obligation
→ responsibility returns to Admisi
→ Admisi receives the Release fact
→ Admisi creates, reactivates, or routes Waiting List work
→ destination RNA processes the Waiting List through the ordinary placement flow
```

RNA-to-RNA transfer is prohibited. The destination Ward obtains no responsibility before its own successful Bed Assignment.

### 3.3 Reconciliation principle

```text
Source local fact
≠ delivery state
≠ receiver acknowledgement
≠ receiver business transition
```

For example, an RNA Accommodation Assignment may be committed while delivery remains `Pending`. Admisi Waiting List closure is then delayed, but the Accommodation Assignment is not rolled back.

---

## 4. Business Contracts

## 4.1 ADMISI → RNA

| Contract | Type | Trigger | Business meaning | Receiver effect |
|---|---|---|---|---|
| `WaitingListDemandAvailable` | Business Fact / demand notification | Admisi commits a new or materially revised active Waiting List entry for a destination Ward | An Admisi-owned registration requires Ward placement review | RNA deduplicates the revision and exposes it as review work. No accommodation or responsibility transfer occurs. |
| `GetWaitingListReviewWork` | Query Contract | Authorized RNA actor or RNA projection requests current Ward-scoped work | Request the authoritative active Waiting List entries visible to the Ward | Admisi returns current authoritative demand snapshots. The query creates no business transition. |
| `GetWaitingListById` | Query Contract | RNA validates a selected work item before rejection or assignment | Request the authoritative current revision and status of one Waiting List entry | RNA must stop unsafe mutation if the item is absent, cancelled, closed, stale, or addressed to another Ward. |
| `PlacementResultAcknowledged` | Acknowledgement | Admisi safely records a rejection or successful-placement result | Admisi confirms technical receipt and processing outcome for the source contract | RNA updates delivery state only. It must not create or undo Accommodation state. |
| `InterWardReleaseAcknowledged` | Acknowledgement | Admisi safely records an RNA release fact and applies its own Waiting List consequence | Admisi confirms technical receipt and processing outcome | RNA updates delivery state only. It must not recreate or roll back the released allocation. |
| `WaitingListStateSnapshot` | Query Result / reconciliation fact | RNA or a recovery process queries Admisi during reconciliation | Current authoritative Waiting List state and revision | RNA compares the snapshot with its local placement/release facts and flags mismatches; it does not repair Admisi directly. |

### Receiver prohibitions

RNA must not infer:

- that Waiting List visibility means responsibility has transferred;
- that a Waiting List is active solely from an old cached snapshot;
- that a legacy `Accepted` state proves Bed Assignment;
- that Waiting List closure proves an Accommodation Allocation exists;
- that patient name, Ward label, or timestamps are stable correlation identities.

## 4.2 RNA → ADMISI

| Contract | Type | Trigger | Business meaning | Receiver effect |
|---|---|---|---|---|
| `PlacementRejected` | Business Fact | Authorized Ward reviewer commits rejection without creating an Accommodation Allocation | The Ward did not place this Waiting List entry; responsibility never left Admisi | Admisi keeps the Waiting List under its authority and records the rejection result/reason according to its own lifecycle. |
| `AccommodationAssigned` | Business Fact | RNA commits a new Clinical Accommodation from the referenced Waiting List | The registration now has an RNA-owned active Accommodation Allocation in the destination Ward | Admisi closes the matching Waiting List and records placement correlation. Responsibility has already moved to RNA because assignment succeeded. |
| `AccommodationAssignmentCorrected` | Correction Fact | Head Nurse commits an append-only correction to a previously published assignment fact | An RNA-owned assignment fact has been corrected without erasing the original | Admisi records the correction and reconciles its own references. It must not automatically reopen Waiting List unless a separately approved rule requires it. |
| `InterWardAccommodationReleased` | Business Fact | Source RNA commits release of its Clinical Accommodation for inter-ward movement | RNA no longer owns the source Clinical Accommodation; operational responsibility returns to Admisi | Admisi applies its own Waiting List creation/reactivation/routing behavior. Destination placement remains a separate future workflow. |
| `InterWardReleaseCorrected` | Correction Fact | Head Nurse commits an append-only correction to the published release fact | An RNA-owned release fact has been corrected or marked erroneous | Admisi records and reconciles the correction. Automatic cancellation/reversal of Waiting List work is not defined by this document. |
| `PlacementResultDeliveryStatusRequested` | Query Contract | Admisi or support tooling checks an unresolved outbound obligation | Request RNA's authoritative source-fact identity and current delivery metadata | RNA returns source truth and delivery status; it does not expose internal clinical data. |
| `RNAIntegrationAcknowledged` | Acknowledgement | RNA safely ingests an Admisi demand revision | RNA confirms technical receipt/deduplication of the Admisi contract | Admisi updates delivery state only; Waiting List state remains Admisi-owned. |

### Sender guarantees

For every RNA business fact:

- the referenced RNA fact has already committed locally;
- the fact has a stable source identity;
- retries reuse the same source identity and revision;
- delivery failure never silently changes the fact's business meaning;
- correction is a new fact referencing the original.

### Receiver prohibitions

Admisi must not:

- create or alter RNA Accommodation Allocation records;
- interpret technical acknowledgement as successful placement;
- close Waiting List from `PlacementRejected`;
- infer Bed Assignment from a closed Waiting List;
- infer current accommodation from the latest Waiting List destination;
- reopen or cancel Waiting List automatically from a correction unless an approved business rule explicitly requires it.

---

## 5. Contract Payload Semantics

## 5.1 Common envelope

Every retriable fact, acknowledgement, or correction must carry:

| Field | Meaning |
|---|---|
| `ContractId` | Stable identity of this contract occurrence. Retries reuse it. |
| `ContractType` | Business contract name defined in this document. |
| `ContractVersion` | Version of payload meaning and state effect. |
| `SourceContext` | `ADMISI` or `RNA`. |
| `SourceFactId` | Stable identity of the authoritative source fact or source aggregate occurrence. |
| `SourceRevision` | Monotonic revision within the relevant source identity. |
| `OccurredAt` | Authoritative UTC business time at which the source fact happened. Business facts are ordered by this value. |
| `RecordedAt` | UTC system persistence time. Used only for audit and technical tracing, never for business ordering. |
| `CorrelationId` | Cross-context workflow correlation, normally anchored by `WaitingListId` or `ReleaseFactId`. |
| `CausationId` | Contract or command that directly caused this occurrence, when available. |
| `FacilityId` | Facility scope of the contract. |
| `SenderIdentity` | Trusted context/service identity, not an arbitrary user-supplied value. |

`OccurredAt` and `RecordedAt` are distinct under the RNA Business Time Standard. When the source business time is unknown, the sender sets `OccurredAt = RecordedAt`; it must not invent an earlier time. A source offset or local business date may be retained as context metadata, but does not alter ordering.

This rule applies to every Integration Fact, acknowledgement, and correction in this contract and is consistent with RNA Accommodation, Bed Readiness, Service Execution, and Correction Facts.

## 5.2 `WaitingListDemandAvailable`

Minimum fields:

| Field | Authority / semantics |
|---|---|
| `WaitingListId` | Admisi-authoritative stable identity. |
| `WaitingListRevision` | Admisi-authoritative current revision. |
| `RegistrationId` | Stable inpatient registration correlation identity. |
| `DestinationWardId` | Admisi-authoritative intended Ward identity. |
| `WaitingListStatus` | Admisi-owned current status. RNA may process only a status contractually eligible for review. |
| `DemandCreatedAt` | Business time at which the demand entered Waiting List. |
| `DemandKind` | Admission placement or inter-ward placement, only when Admisi already owns this distinction. |
| `PriorityReference` | Optional Admisi-owned priority identity/value when available; RNA does not redefine it. |

Excluded:

- full patient demographics;
- clinical handover content;
- bed, room, or Care Class as patient prerequisites;
- billing data;
- optional placement-policy fields.

Display names may be supplied as non-authoritative snapshots but must never replace stable IDs.

## 5.3 `PlacementRejected`

Minimum fields:

| Field | Authority / semantics |
|---|---|
| `PlacementDecisionId` | Stable RNA identity for this rejection decision. |
| `WaitingListId` | Correlation to Admisi demand. |
| `WaitingListRevisionReviewed` | Revision reviewed by RNA. |
| `RegistrationId` | Correlation identity. |
| `DestinationWardId` | Ward making the decision. |
| `RejectedAt` | Business time of rejection. |
| `RejectedBy` | Accountable authenticated Ward actor. |
| `ReasonCode` | Stable reason when an approved taxonomy exists. |
| `ReasonText` | Required accountable explanation when no approved taxonomy exists; must not contain unnecessary clinical detail. |

A rejection communicates only that placement was not made. It does not cancel the Admission, close the Waiting List, or reroute the patient.

## 5.4 `AccommodationAssigned`

Minimum fields:

| Field | Authority / semantics |
|---|---|
| `AccommodationAssignmentFactId` | Stable RNA source fact identity. |
| `AccommodationAssignmentRevision` | RNA revision of the published assignment fact. |
| `WaitingListId` | Admisi correlation identity. |
| `WaitingListRevisionReviewed` | Demand revision used for placement. |
| `RegistrationId` | Registration receiving accommodation. |
| `AccommodationAllocationId` | RNA-owned allocation identity. |
| `WardId` | RNA-authoritative assigned Ward identity. |
| `RoomId` | Referenced master identity. |
| `BedId` | Referenced master identity. |
| `AccommodationPurpose` | RNA-owned declared purpose; for ordinary placement this identifies Clinical Accommodation. |
| `AssignedAt` | Actual business time of assignment/reception; represented as the fact's `OccurredAt` under the RNA Business Time Standard. |
| `AssignedBy` | Accountable authenticated RNA actor. |

Excluded:

- full patient demographics;
- financial treatment or room charge;
- BOR calculation;
- gender/isolation/equipment policy decisions;
- copied Admisi or Registration aggregates.

Admisi may retain `AccommodationAllocationId`, `WardId`, `RoomId`, and `BedId` as references or display snapshots. RNA remains authoritative.

## 5.5 `AccommodationAssignmentCorrected`

Minimum fields:

| Field | Authority / semantics |
|---|---|
| `CorrectionFactId` | Stable identity of the correction. |
| `CorrectionRevision` | Monotonic revision in the correction chain. |
| `OriginalAccommodationAssignmentFactId` | Immutable original fact reference. |
| `WaitingListId` | Original cross-context correlation. |
| `RegistrationId` | Original registration identity. |
| `CorrectionKind` | Corrected or Entered in Error, using an approved RNA value. |
| `CorrectedFields` | Minimal list of corrected cross-boundary fields. |
| `CorrectedValues` | Replacement reference values required by Admisi reconciliation. |
| `CorrectionReason` | Accountable reason. |
| `CorrectedAt` | Business time of correction; represented as the correction fact's `OccurredAt`. |
| `CorrectedBy` | Authorized Head Nurse identity. |

The original assignment fact remains queryable and is never overwritten.

## 5.6 `InterWardAccommodationReleased`

Minimum fields:

| Field | Authority / semantics |
|---|---|
| `ReleaseFactId` | Stable RNA source fact identity. |
| `ReleaseRevision` | RNA revision. |
| `RegistrationId` | Registration returned to Admisi responsibility. |
| `ReleasedAccommodationAllocationId` | RNA-owned source allocation identity. |
| `SourceWardId` | Ward releasing responsibility. |
| `DestinationWardId` | Intended destination only when already established by the approved source workflow; Admisi remains routing authority. |
| `ReleaseReason` | Accountable release reason. |
| `ReleasedAt` | Actual business time of release; represented as the release fact's `OccurredAt`. |
| `ReleasedBy` | Accountable authenticated RNA actor. |

The contract must not include an RNA-created destination acceptance state or transfer queue identity.

## 5.7 `InterWardReleaseCorrected`

Minimum fields follow the same correction envelope:

- `CorrectionFactId`
- `CorrectionRevision`
- `OriginalReleaseFactId`
- `RegistrationId`
- `CorrectionKind`
- corrected cross-boundary fields and values
- `CorrectionReason`
- `CorrectedAt`
- `CorrectedBy`

It does not define an automatic Waiting List reversal.

## 5.8 Acknowledgements

Minimum fields:

| Field | Meaning |
|---|---|
| `AcknowledgedContractId` | Contract occurrence being acknowledged. |
| `AcknowledgedSourceFactId` | Source fact identity. |
| `ReceiverOutcome` | `AcceptedForProcessing`, `AlreadyProcessed`, `RejectedTechnical`, `RejectedStale`, or `ReconciliationRequired`. |
| `ReceiverStateReference` | Optional receiver-owned state/fact identity created by processing. |
| `ReceiverRevision` | Optional receiver revision after processing. |
| `AcknowledgedAt` | Receiver processing time. |
| `FailureCode` | Stable technical/reconciliation code when not successful. |
| `FailureMessage` | Sanitized explanation without unnecessary patient or clinical data. |

An acknowledgement never substitutes for `AccommodationAssigned`, `PlacementRejected`, or `InterWardAccommodationReleased`.

---

## 6. State and Responsibility Effects

## 6.1 Waiting List placement

| Admisi current state / responsibility | Received or local fact | Resulting state / responsibility | Context performing transition |
|---|---|---|---|
| Active Waiting List; Admisi responsible | `WaitingListDemandAvailable` acknowledged by RNA | No business change; Admisi remains responsible | None |
| Active Waiting List; Admisi responsible | `PlacementRejected` | Waiting List remains under Admisi; rejection is recorded | Admisi |
| Active Waiting List; Admisi responsible | RNA commits `AccommodationAssigned`, but delivery is pending | RNA owns accommodation and responsibility; Admisi Waiting List may temporarily remain open due integration lag | RNA locally; Admisi transition pending |
| Active Waiting List; Admisi responsible | Admisi processes `AccommodationAssigned` | Waiting List becomes Closed; Admisi records placement correlation | Admisi |
| Closed Waiting List; RNA responsible | Duplicate `AccommodationAssigned` | No repeated closure or second placement effect | Admisi idempotency |
| Cancelled/closed/stale Waiting List | New conflicting assignment result | No automatic transition; quarantine for reconciliation | Admisi + RNA recovery owners |

### Exact responsibility rule

Responsibility transfers **when RNA successfully commits the Bed Assignment**, not when:

- the Waiting List is viewed;
- Ward review starts;
- RNA receives the demand;
- a technical acknowledgement is returned;
- Admisi eventually closes the Waiting List.

This can create a temporary cross-context projection mismatch during delivery failure. Reconciliation handles the mismatch; destructive rollback does not.

## 6.2 Inter-ward release

| RNA current state / responsibility | Received or local fact | Resulting state / responsibility | Context performing transition |
|---|---|---|---|
| Active Clinical Accommodation; RNA responsible | RNA commits `InterWardAccommodationReleased` | Source allocation is Released; responsibility returns to Admisi | RNA fact establishes boundary effect |
| Released; Admisi responsible | Delivery pending | Release remains valid; Admisi work may be temporarily absent | RNA retry/recovery |
| Released; Admisi responsible | Admisi processes Release fact | Admisi creates/reactivates/routes Waiting List according to its own rules | Admisi |
| Waiting List active; Admisi responsible | Destination `PlacementRejected` | Admisi remains responsible | Admisi |
| Waiting List active; Admisi responsible | Destination RNA commits `AccommodationAssigned` | Responsibility moves to destination RNA | Destination RNA |
| Destination assignment processed | Admisi processes assignment fact | Waiting List closes | Admisi |

No direct source-RNA to destination-RNA ownership transfer exists.

## 6.3 Correction effects

| Original fact | Correction received | Defined receiver effect | Explicitly not defined |
|---|---|---|---|
| `AccommodationAssigned` | `AccommodationAssignmentCorrected` | Preserve original, record correction, reconcile references | Automatic reopening of Waiting List |
| `AccommodationAssigned` | Entered in Error correction | Flag mismatch and escalate | Automatic transfer of responsibility back to Admisi |
| `InterWardAccommodationReleased` | `InterWardReleaseCorrected` | Preserve original, record correction, reconcile current Waiting List relation | Automatic cancellation of destination work |
| Any contract | Duplicate correction | Return prior correction outcome | Reapply business effect |

Responsibility reversal from an Entered in Error placement or release is an unresolved business decision and must not be invented by implementation.

---

## 7. Reliability and Consistency

## 7.1 Delivery guarantee

The approved topology is hybrid: sender-local outbox and receiver-local inbox provide at-least-once durable push; acknowledgements are durable; authoritative queries reconcile missing or uncertain delivery. A direct in-process adapter may optimize transport only when it preserves identical outbox/inbox, idempotency, retry, acknowledgement, and reconciliation semantics.

The target semantic model is **mixed**:

1. **Read-on-demand query** for RNA Ward worklists and pre-command revalidation of Admisi Waiting List state.
2. **At-least-once durable delivery** for:
   - `PlacementRejected`;
   - `AccommodationAssigned`;
   - `AccommodationAssignmentCorrected`;
   - `InterWardAccommodationReleased`;
   - `InterWardReleaseCorrected`;
   - their acknowledgements.
3. A co-deployed in-process implementation may optimize delivery, but it must preserve the same durable identities, idempotency, transaction boundaries, and recovery behavior.

The existing no-op Ward gateway and current Waiting List persistence/API are insufficient to claim this contract is implemented.

## 7.2 Idempotency

| Contract scope | Idempotency key | Duplicate behavior | Conflicting replay |
|---|---|---|---|
| Waiting List demand revision | `SourceContext + WaitingListId + WaitingListRevision` | Return prior ingestion outcome; do not create duplicate review work | Same revision with different semantic content is quarantined |
| Placement rejection | `PlacementDecisionId` | Return prior result; do not append a second rejection decision | Different payload under same ID is rejected as conflict |
| Accommodation assignment | `AccommodationAssignmentFactId` | Admisi returns prior close/correlation outcome | Same fact ID with different registration, Waiting List, allocation, or bed is quarantined |
| Inter-ward release | `ReleaseFactId` | Admisi returns prior Waiting List consequence/reference | Same fact ID with different registration/source allocation is quarantined |
| Correction | `CorrectionFactId` | Return prior correction outcome | Same correction ID with different original fact or values is quarantined |
| Actor-facing RNA assignment command | Stable request ID plus `WaitingListId` | Return the prior RNA command result | A second distinct active placement for the same Waiting List must fail |

Semantic replay must be safe after timeout: callers can repeat the same request/contract identity and obtain the previous outcome without repeating business behavior.

## 7.3 Ordering

Ordering is required:

- per `WaitingListId` and Admisi `WaitingListRevision`;
- per RNA `AccommodationAssignmentFactId` correction chain;
- per RNA `ReleaseFactId` correction chain;
- per `RegistrationId` when assignment and inter-ward release facts could otherwise cross.

Rules:

1. Lower or equal revisions already processed are duplicates or stale.
2. A future revision with a missing predecessor may be held for bounded reordering or reconciled through an authoritative query.
3. Corrections must not be applied before the original fact is known.
4. Cross-registration global ordering is not required.

## 7.4 Transaction boundaries

### Admisi local transactions

Admisi must atomically commit, as applicable:

- Waiting List creation or revision;
- its audit record;
- the durable outbound demand obligation, when push delivery is used.

When processing `AccommodationAssigned`, Admisi must atomically commit:

- idempotent inbox result;
- Waiting List closure and placement correlation;
- audit;
- acknowledgement obligation.

When processing `InterWardAccommodationReleased`, Admisi must atomically commit:

- idempotent inbox result;
- its Waiting List creation/reactivation/routing consequence;
- audit;
- acknowledgement obligation.

### RNA local transactions

RNA must atomically commit:

**Placement rejection**
- review/rejection fact;
- audit;
- outbound rejection obligation.

**Accommodation assignment**
- Accommodation Allocation;
- mandatory Bed Assignability recheck and required bed concurrency/version effects;
- audit;
- outbound `AccommodationAssigned` obligation.

**Inter-ward release**
- source Accommodation Release;
- post-use Bed Readiness transaction required by RNA;
- audit;
- outbound `InterWardAccommodationReleased` obligation.

**Correction**
- immutable correction fact;
- audit;
- outbound correction obligations.

### Cross-context boundary

No distributed atomic transaction is assumed.

Therefore:

```text
RNA Accommodation Assignment may commit before Admisi closes Waiting List.

RNA Inter-Ward Release may commit before Admisi creates or routes Waiting List work.

Admisi retry or reconciliation must repair delivery/application lag without
rolling back valid RNA facts.
```

---

## 8. Failure and Reconciliation

| Failure case | Required behavior | Recovery owner | Prohibited behavior |
|---|---|---|---|
| Admisi demand query unavailable before RNA assignment | Block unsafe assignment and show dependency failure | RNA operations/integration support | Assign from stale demand without authoritative revalidation |
| RNA unavailable when Admisi publishes demand | Persist demand obligation or retain query-visible Waiting List; retry | Admisi integration support | Transfer responsibility merely because delivery was attempted |
| Admisi unavailable after RNA assignment | Keep assignment committed; persist and retry same fact ID | RNA integration support | Delete/release the valid assignment as ordinary recovery |
| Admisi unavailable after RNA inter-ward release | Keep release committed; retry same release ID; expose missing Admisi work | RNA integration support, with Admisi escalation | Recreate source accommodation or create an RNA transfer queue |
| Duplicate delivery | Return prior receiver outcome | Receiver | Repeat placement, closure, release, or Waiting List creation |
| Unknown `WaitingListId` | Reject technically and retain sender fact for reconciliation | Admisi + RNA support | Create an uncorrelated Waiting List automatically |
| Unknown `RegistrationId` | Reject technically; query authoritative Registration/Admisi source | Receiver support | Match by patient name or demographics |
| Unknown Ward/bed/allocation reference | Reject or quarantine according to the receiver's need | Receiver support | Substitute display name as identity |
| Stale Waiting List revision at placement | RNA command must stop and reload authoritative state | RNA actor/support | Blindly retry stale business decision |
| Waiting List already closed but RNA has no matching assignment | Reconciliation-required; compare authoritative source facts | Admisi + RNA support | Infer an assignment from closure |
| RNA assignment exists but Waiting List remains active | Replay `AccommodationAssigned` or query acknowledgement; close idempotently | RNA sender first, Admisi receiver | Create a second assignment |
| Release exists but Admisi has no Waiting List consequence | Replay release fact; query acknowledgement; escalate if permanent | RNA sender first, Admisi receiver | Undo release automatically |
| Permanent technical rejection | Move obligation to visible reconciliation state with reason | Named integration recovery actors | Infinite silent retry |
| Correction arrives before original | Hold/quarantine, retrieve original, then apply | Receiver | Apply correction as a new unrelated fact |
| Correction delivery fails | Retry correction independently using same correction ID | Sender | Republish original as modified |
| Same ID, conflicting payload | Quarantine and require investigation | Both architecture/data owners | Last-write-wins overwrite |

## 8.1 Reconciliation queries

Both contexts must support authoritative reconciliation by stable identity.

Minimum reconciliation capabilities:

- Admisi: query Waiting List by `WaitingListId`, including status, revision, RegistrationId, destination Ward, placement correlation, and latest processed RNA contract.
- RNA: query assignment/release/correction by source fact ID and RegistrationId, including local business state and delivery state.
- Both: query processing outcome by `ContractId`.

## 8.2 Reconciliation rules

1. Source business truth wins for facts owned by that source.
2. Receiver business truth wins for receiver-owned transitions.
3. Recovery replays delivery or receiver application; it does not rerun the original human decision.
4. Manual recovery may:
   - retry a stable obligation;
   - mark a permanent technical failure for investigation;
   - re-query the authoritative counterparty;
   - link an orphaned contract after evidence-based review.
5. Manual recovery must never:
   - edit another context's persistence;
   - generate a new source fact identity to bypass conflict;
   - silently close/reopen Waiting List;
   - silently create/release Accommodation.

## 8.3 Observable recovery states

Operationally visible delivery states should be limited to:

- `Pending`
- `InProgress`
- `Acknowledged`
- `FailedRetryable`
- `RejectedTechnical`
- `ReconciliationRequired`

These are integration states, not Waiting List or Accommodation states.

---

## 9. Security and Audit

## 9.1 Authentication

Allowed mechanisms:

- trusted authenticated in-process application identity when both contexts are co-deployed;
- authenticated service principal when a system boundary exists;
- authenticated user context for actor-facing RNA rejection and assignment commands.

A caller-supplied actor ID without verified identity is insufficient.

## 9.2 Authorization

**Phase boundary (ARCH-020).** The current implementation assumes only baseline authentication and coarse-grained application access. Fine-grained contextual authorization, including Ward scope, service-principal mapping, and recovery-role enforcement, is intentionally out of scope for the current phase and deferred to Phase-99. The rules below preserve business ownership and contract semantics; their enforcement remains deferred.

### ADMISI publishers and queries

- Only the authoritative Admisi application boundary may publish Waiting List facts.
- RNA queries must be scoped to facility and destination Ward.
- RNA actors may only view and process Waiting List entries for Wards within their authorized operational scope.

### RNA publishers and commands

- Only the authoritative RNA application boundary may publish assignment, rejection, release, and correction facts.
- Actor-facing Bed Assignment and rejection require contextual Ward authority.
- Accommodation corrections require the approved Head Nurse authority and same-Ward rule.
- Integration recovery capability does not grant patient-wide clinical access or authority to make placement decisions.

### Minimum-data rule

Contracts and logs must not include:

- full patient demographics unless separately justified;
- clinical handover narrative;
- diagnosis or sensitive clinical details;
- billing, tariff, coverage, or payment data;
- credentials or tokens.

## 9.3 Audit

Record at minimum:

- Source Context
- Source Fact ID
- Source Revision
- Contract ID
- Contract Type
- Contract Version
- Waiting List ID or Release correlation
- Registration ID
- Facility and Ward scope
- Business Time
- Recorded Time
- Delivery/Attempt Time
- Sender Identity
- Receiver Identity
- Accountable Human Actor when applicable
- Correlation ID
- Causation ID
- Attempt Count
- Acknowledgement Result
- Receiver State Reference when available
- Failure Code and sanitized reason

Aggregate history remains the business truth. Integration audit does not replace Admisi Waiting List history or RNA Accommodation history.

---

## 10. Versioning and Compatibility

1. Every contract carries `ContractVersion`.
2. Additive optional fields may remain within the current major version.
3. Changes to any of the following require a new major version:
   - business meaning;
   - source identity;
   - required field semantics;
   - idempotency scope;
   - state or responsibility effect;
   - correction interpretation.
4. Producers must not remove a required field within a supported major version.
5. Consumers must ignore unknown optional fields unless they are explicitly marked critical.
6. Initial release supports only contract `v1`; no overlap is required because no earlier native production contract exists.
7. Historical replay retains its original contract version.
8. Compatibility, upcasting, correction mapping, and deprecation policy must be approved before `v2` or another breaking change.
9. Operations owns deployment, monitoring, rollback, and backlog verification; rollback never deletes committed v1 facts.

---

## 11. Open Decisions

| ID | Decision | Owner | Blocks | Safe interim behavior |
|---|---|---|---|---|
| `ADM-RNA-OD-001` — CLOSED | Use hybrid durable outbox/inbox push plus authoritative query reconciliation. An in-process optimization must preserve identical durability semantics. | Admisi and RNA architecture owners | Implementation only | Retry stable identity and expose failed/stale/conflicting obligations. |
| `ADM-RNA-OD-002` — CLOSED | RNA may review `WAITING` and `READY_FOR_WARD_REVIEW`. Legacy `Accepted` is not placement proof and migrates to `WAITING` unless an authoritative RNA Accommodation Assignment exists. | Admisi domain/architecture owner | State migration implementation | Never infer placement or responsibility transfer from legacy `Accepted`. |
| `ADM-RNA-OD-003` — CLOSED FOR V1 | Ward rejection requires sanitized accountable free text. No rejection-code catalogue is required for v1. | Admisi operations + RNA operations | Validation implementation | Preserve actor, Ward, reason, and business time. |
| `ADM-RNA-OD-004` — CLOSED | `InterWardAccommodationReleased` idempotently creates a new Admisi Waiting List entry; if an active matching entry exists, Admisi reuses it. | Admisi domain owner | Consumer implementation | Admisi remains routing/Waiting List owner; RNA creates no transfer queue. |
| `ADM-RNA-OD-005` — CLOSED | If `AccommodationAssigned` is corrected or Entered in Error, preserve all facts and mark `ReconciliationRequired`; do not automatically reopen Waiting List. | Admisi + RNA business owners | Reconciliation implementation | Human/approved reconciliation decides any later Waiting List action. |
| `ADM-RNA-OD-006` — CLOSED | If inter-ward release is corrected after Waiting List creation, preserve both records and require reconciliation; never automatically delete/cancel the Waiting List. | Admisi + RNA business owners | Reconciliation implementation | Preserve responsibility and history until explicit resolution. |
| `ADM-RNA-OD-007` — CLOSED FOR V1 | Initial release supports only v1 with no previous-version overlap. Define compatibility policy before v2. Operations owns rollout, rollback, monitoring, and backlog verification. | Admisi and RNA architecture owners + Operations | No v1 design block | Rollback stops new intake without deleting committed facts. |
| `ADM-RNA-OD-009` — CLOSED (DEFERRED TO PHASE-99) | Define authorized service principals, Ward-scope claims, and recovery-role mapping. Fine-grained contextual enforcement is deferred; v1 uses baseline authentication and coarse-grained application access. | Identity/Authorization owner | Phase-99 authorization enforcement only; no v1 design or acceptance-test block | Preserve the target authority semantics in the contract, but do not claim contextual enforcement exists in v1. |
| `ADM-RNA-OD-010` — CLOSED | `DestinationWardId` is optional on inter-ward release and is supplied only when already authoritatively known. Admisi remains responsible for destination routing. | Admisi operations + RNA operations | No design block; payload implementation remains | Never invent a destination Ward in RNA. |

Implementation agents must not close these decisions by convention.

---

## 12. Traceability

| Contract / rule | Source artifact | Trace |
|---|---|---|
| Waiting List remains Admisi-owned until successful Bed Assignment | `RNA-DOMAIN.md`; `RNA-SOP-INDEX.md` | Accommodation Demand Management and approved boundary statement |
| No separate Hand-Off or Accepted state | `RNA-SOP-GAPS.md` | GAP-RNA-001 CLOSED |
| Only Mandatory Bed Assignability is automated | `RNA-DOMAIN.md`; `SOP-RNA-A01`; `RNA-SOP-GAPS.md` | GAP-RNA-002 CLOSED |
| Ward rejection leaves responsibility with Admisi | `SOP-RNA-A01` | Exception and completion criteria |
| Successful assignment causes Admisi closure | `SOP-RNA-A01`; `RNA-ARCHITECTURE.md` | UC-RNA-004 and placement transaction |
| Inter-ward transfer is Release-to-Waiting-List | `RNA-DOMAIN.md`; `SOP-RNA-A05`; `RNA-SOP-INDEX.md` | UC-RNA-011–013 |
| RNA release is not rolled back when Admisi notification fails | `SOP-RNA-A05`; `RNA-ARCHITECTURE.md` | Failure handling and transaction boundary |
| Admisi Waiting List is never directly written by RNA | `RNA-ARCHITECTURE.md` | Context boundary and integration matrix |
| Current gateway is no-op / insufficient | `RNA-ARCHITECTURE.md` | Codebase evidence and GAP-RNA-ARCH-015 |
| Waiting List has stable `WaitingListId` and `RegId` in current code | Codebase evidence summarized in Patient Journey implementation analysis | Existing identity basis |
| Current code allows historical Waiting Lists and at most one active item per `RegId` | Codebase evidence summarized in Patient Journey implementation analysis | Existing persistence behavior |
| Legacy Waiting List includes `Accepted`, but it is not proof of assignment | Current codebase evidence | `ADM-RNA-OD-002` CLOSED: migrate to `WAITING` unless authoritative RNA assignment exists |
| Inbox/outbox, retries, Dapper repositories, unit-of-work, and audit are established patterns | `RNA-ARCHITECTURE.md` codebase evidence | Target implementation constraints |
| Contract structure and reliability rules | `integration-document-creation-skill.md` | Required document structure and quality checklist |

---

## 13. AI Implementation Guardrails

Implementation agents must:

1. Treat every contract in this document as semantic, not as a mandate for a specific transport.
2. Create application-boundary ports before infrastructure adapters.
3. Never import another context's repository or DAL as the public integration contract.
4. Preserve stable source IDs and revisions through all adapters.
5. Commit source facts and outbound obligations atomically.
6. Make every receiver idempotent before enabling retries.
7. Revalidate authoritative Waiting List and Bed Assignability facts at command time.
8. Keep integration delivery state separate from business aggregate state.
9. Preserve original facts and append corrections.
10. Implement visible reconciliation instead of silent data repair.
11. Add tests for duplicate, stale, delayed, out-of-order, unavailable-receiver, and conflicting-replay cases.
12. Stop and surface an open decision rather than inventing a business rule.

### Minimum acceptance tests

- Duplicate Waiting List demand does not create duplicate RNA review work.
- Viewing or acknowledging demand does not transfer responsibility.
- Rejection creates no Accommodation Allocation and leaves Admisi responsible.
- Two concurrent assignments for the same Waiting List cannot both succeed.
- `AccommodationAssigned` retry closes Waiting List at most once.
- Assignment remains committed when Admisi is unavailable.
- Admisi closure without a matching RNA assignment is detected by reconciliation.
- Inter-ward release remains committed when notification fails.
- Duplicate release creates at most one Admisi Waiting List consequence.
- Destination Ward receives no responsibility before its own assignment succeeds.
- Correction preserves the original source fact.
- Correction received before original is held or reconciled safely.
- Same contract ID with conflicting payload is quarantined.
- Baseline authentication and coarse-grained application access are required for actor-facing commands and integration endpoints; contextual Ward-scope denial is a Phase-99 acceptance criterion.

---

## 14. Closure Criteria for GAP-RNA-ARCH-015

`GAP-RNA-ARCH-015` is **CLOSED (DESIGN) / IMPLEMENTATION DEPENDENCY** because:

- this integration document is approved by Admisi and RNA architecture owners;
- all v1 decisions are resolved and only Phase-99 authorization mapping is explicitly deferred;
- `RNA-ARCHITECTURE.md` references this document as the authoritative cross-context semantic contract;
- the Admisi architecture references the same ownership and state effects;
- later API/persistence contracts preserve the identities, idempotency, transaction boundaries, and correction semantics defined here.

It is not **IMPLEMENTED** until production adapters, inbox/outbox or equivalent durability, acknowledgements, reconciliation views, and acceptance tests exist.

---

# Final Summary

## 1. Authoritative ownership

- **Admisi** owns Admission, Waiting List identity and lifecycle, priority, cancellation, rerouting, and closure.
- **RNA** owns Ward rejection, Mandatory Bed Assignability, Accommodation Assignment, Accommodation Release, and append-only Accommodation corrections.
- Responsibility remains with **Admisi** until RNA commits a successful Bed Assignment.
- Responsibility returns to **Admisi** when source RNA commits an inter-ward Accommodation Release.
- Delivery and acknowledgement states belong to integration infrastructure and never replace business ownership.

## 2. Contracts in each direction

**ADMISI → RNA**

- `WaitingListDemandAvailable`
- `GetWaitingListReviewWork`
- `GetWaitingListById`
- `WaitingListStateSnapshot`
- placement/release acknowledgements

**RNA → ADMISI**

- `PlacementRejected`
- `AccommodationAssigned`
- `AccommodationAssignmentCorrected`
- `InterWardAccommodationReleased`
- `InterWardReleaseCorrected`
- delivery-status/reconciliation query results
- inbound-demand acknowledgement

## 3. Implementation blockers

- Current Ward placement gateway is a no-op and does not provide durable delivery or acknowledgement.
- RNA persistence, inbox/outbox, integration ledger, handlers, and reconciliation views do not yet exist.
- Legacy Waiting List `Accepted` migration is approved; migration tooling must map it to `WAITING` unless an authoritative RNA assignment exists.
- Fine-grained contextual authorization and service-principal mapping are intentionally deferred to Phase-99 under ARCH-020; baseline authentication and coarse-grained application access remain the current assumption.
- Business Time is standardized by the common envelope: `OccurredAt` is authoritative business time, `RecordedAt` is persistence time for audit/technical tracing, unknown business time uses `OccurredAt = RecordedAt`, and facts are ordered by `OccurredAt`.
- The approved inter-ward release consumer, correction reconciliation, durable delivery, and recovery views are not implemented.

## 4. Deferred beyond v1

- Phase-99 service-principal and Ward-scope authorization enforcement (ARCH-020).
