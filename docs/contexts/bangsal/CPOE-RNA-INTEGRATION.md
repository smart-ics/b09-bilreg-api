# CPOE ↔ RNA Integration

**Status:** Canonical V1 semantic contract  
**Participants:** CPOE and RUANG RANAP Operational Management (RNA)

## 1. Purpose and Scope

This contract coordinates one CPOE `Order Occurrence` routed to an RNA `Destination` with RNA's authoritative evidence of whether the requested work was performed.

CPOE represents clinician intent. RNA performs and records ward work. Neither context writes or reinterprets the other context's business truth.

The V1 boundary contains exactly seven business contracts:

**CPOE → RNA**

1. `ActiveOrderOccurrenceRouted`
2. `ActiveOrderOccurrenceModified`
3. `ClinicalOrderCancelled`
4. `ReconciliationDestinationDecision`

**RNA → CPOE**

1. `OccurrenceCompletedEvidence`
2. `OccurrenceNotPerformedEvidence`
3. `ExecutionEvidenceCorrectionNotice`

### 1.1 Explicit exclusions

This contract does not introduce:

- `Authorized` or `Dispatched` CPOE states;
- Destination acceptance or rejection;
- clarification, hold, or acceptance workflows;
- discontinuation;
- `Entered in Error` as a Clinical Order state;
- Completion Criteria beyond the canonical CPOE occurrence rules;
- exceptional subsequent authorization or escalation;
- retrospective CPOE accountability for Ad Hoc or Independent Tindakan;
- reopening, superseding, or reactivating a final Order Occurrence;
- multiple current valid execution-evidence chains for one `OrderOccurrenceId`;
- clinical results or result review;
- financial eligibility, tariff, billing, or settlement semantics; or
- a transport, API, messaging, or persistence design.

## 2. Context Ownership

| Business truth | Owner | Collaborator responsibility |
|---|---|---|
| Clinical Order intent, Order Specification, finite occurrence plan, Destination, modification, cancellation, occurrence status, order status, and Order History | CPOE | RNA observes only the facts required to coordinate its work. |
| Pending ward work and internal work coordination | RNA | CPOE does not infer execution from receipt, visibility, or assignment. |
| Actual ward execution, Performer, Performed At, execution description/reference, and append-only evidence correction | RNA | CPOE records the reported fulfilment status and evidence reference. |
| `Not Performed` determination and reason for an RNA-owned Destination | RNA | CPOE finalizes the referenced Active occurrence according to its rules. |
| Active Order Reconciliation and confirmed Destination decision | CPOE | RNA exposes transfer facts and applies only a confirmed decision to matching pending work. |
| Ad Hoc and Independent Tindakan authority, review, and escalation | RNA and Clinical Governance | CPOE is not involved unless both a real `ClinicalOrderId` and `OrderOccurrenceId` already exist. |
| Financial enrichment and consequences | Tarif, Tindakan, and Tata Rekening according to their respective ownership | CPOE fulfilment and RNA execution truth do not wait for financial completion. |

Ownership rules:

1. CPOE shall not create, modify, or delete RNA execution evidence.
2. RNA shall not create, modify, cancel, or finalize a Clinical Order or Order Occurrence.
3. RNA shall not change an order Destination because accommodation changed.
4. CPOE shall not infer execution from RNA work receipt, assignment, preparation, or financial publication.
5. A correction notice does not transfer RNA correction authority to CPOE.

## 3. Shared Correlation Model

### 3.1 Registration and routing identity

- `PatientId` identifies the Patient.
- `RegId` identifies the continuing registration and remains unchanged during an inter-ward transfer.
- `DestinationId` identifies the operational unit responsible for the occurrence.
- Current Ward and Destination are routing facts separate from RegId.

### 3.2 Order and occurrence identity

```text
ClinicalOrderId
  → one or more finite OrderOccurrenceId values
  → zero or one current valid RNA fulfilment evidence chain per OrderOccurrenceId
```

For native CPOE work:

- both `ClinicalOrderId` and `OrderOccurrenceId` are mandatory;
- each RNA evidence item applies to exactly one Order Occurrence;
- multiple RNA executions for one Clinical Order are allowed only across different Order Occurrences; and
- a correction revises the occurrence's evidence chain and does not create another performance.

## 4. CPOE → RNA Contracts

### 4.1 `ActiveOrderOccurrenceRouted`

**Type:** Business fact.

**Meaning:** One Active Order Occurrence has exactly one RNA Destination and is ready to appear as pending RNA work.

**Required business content:**

- `ClinicalOrderId`;
- `OrderOccurrenceId`;
- `PatientId`;
- `RegId`;
- `DestinationId` and responsible Ward when applicable;
- `OrderType`;
- the minimum Order Specification required to understand the requested work;
- `PlannedExecutionTime`;
- `OrderingClinicianId`; and
- routing decision time.

**RNA effect:** Create or expose one pending-work responsibility for the occurrence.

**RNA must not infer:** acceptance, execution, Performer assignment, financial classification, or a clinical result.

### 4.2 `ActiveOrderOccurrenceModified`

**Type:** Business fact.

**Meaning:** CPOE accepted a permitted change affecting an Order Occurrence that is still Active.

**Required business content:**

- `ClinicalOrderId`;
- `OrderOccurrenceId`;
- `PatientId` and `RegId`;
- modification time;
- responsible decision actor;
- reason;
- changed Order Specification, Planned Execution Time, or Destination values; and
- relevant prior values needed to understand the change.

**RNA effect:** Update matching pending work while preserving its earlier received facts.

**RNA must not:** alter already recorded execution evidence or apply the modification to a final occurrence.

### 4.3 `ClinicalOrderCancelled`

**Type:** Business fact.

**Meaning:** CPOE explicitly cancelled an Active Clinical Order and changed its remaining Active occurrences to Cancelled.

**Required business content:**

- `ClinicalOrderId`;
- affected `OrderOccurrenceId` values that were Active at cancellation;
- cancellation time;
- responsible actor; and
- cancellation reason.

**RNA effect:** End matching pending work as source-cancelled.

**RNA must not:**

- create RNA Fulfilment Evidence for source cancellation;
- erase execution evidence recorded before cancellation; or
- treat accommodation release or transfer as this contract.

### 4.4 `ReconciliationDestinationDecision`

**Type:** Confirmed CPOE business decision.

**Meaning:** CPOE completed an Affected Occurrence Review and the relevant Clinical Order accepted the decision while the occurrence was eligible.

**Required business content:**

- `TransferReconciliationId`;
- `AffectedOccurrenceReviewId`;
- `ClinicalOrderId`;
- `OrderOccurrenceId`;
- `PatientId` and unchanged `RegId`;
- prior Ward and new Ward;
- decision: `Retain` or `Change`;
- prior Destination;
- confirmed Destination when the decision is `Change`;
- decision time;
- Reconciliation Reviewer; and
- reason.

**RNA effect:** Retain or update matching pending-work responsibility according to the confirmed decision.

**RNA must not:** derive this decision from transfer, release, or assignment facts; directly modify the Clinical Order Aggregate; or apply the decision to already final work.

## 5. RNA → CPOE Contracts

### 5.1 `OccurrenceCompletedEvidence`

**Type:** Business fact.

**Meaning:** RNA performed the work for one routed Order Occurrence and recorded authoritative execution truth.

**Required business content:**

- `EvidenceReference`;
- `ClinicalOrderId`;
- `OrderOccurrenceId`;
- `PatientId`;
- `RegId`;
- `Outcome = Completed`;
- `ResponsibleDestination`;
- `Performer`;
- `PerformedAt` as the effective business time; and
- execution description or reference.

`ServiceId` may be included when already resolved but is not required. CPOE fulfilment evidence shall not wait for Tarif resolution or financial publication.

**CPOE effect:** If the occurrence is Active and the evidence is valid for its Destination, finalize it as Completed, recalculate Completion Progress, and derive the parent order status.

### 5.2 `OccurrenceNotPerformedEvidence`

**Type:** Business fact.

**Meaning:** The responsible RNA Destination determined that one routed Order Occurrence was not performed.

**Required business content:**

- `EvidenceReference`;
- `ClinicalOrderId`;
- `OrderOccurrenceId`;
- `PatientId`;
- `RegId`;
- `Outcome = Not Performed`;
- `EffectiveTime`;
- `ResponsibleDestination`; and
- `Reason`.

**CPOE effect:** If the occurrence is Active and the evidence is valid for its Destination, finalize it as Not Performed, recalculate Completion Progress, and derive the parent order status.

This contract is not an execution fact and does not introduce a complex outcome catalogue.

### 5.3 `ExecutionEvidenceCorrectionNotice`

**Type:** Append-only correction notice.

**Meaning:** RNA corrected or marked Entered in Error an RNA-owned execution-evidence item while preserving the original evidence.

**Required business content:**

- `CorrectionReference`;
- original `EvidenceReference`;
- `ClinicalOrderId`;
- `OrderOccurrenceId`;
- `PatientId` and `RegId`;
- correction kind;
- correction time;
- correcting actor;
- correction reason; and
- corrected evidence summary or replacement evidence reference when applicable.

**CPOE effect:** Append the correction notice to accountable history and record a conflict or reconciliation need.

**CPOE must not automatically:**

- reopen a final occurrence;
- supersede its terminal status;
- reactivate it;
- change Completed to Not Performed or Cancelled;
- change Not Performed or Cancelled to Completed; or
- create another Order Occurrence or RNA performance.

## 6. State and Responsibility Effects

| Current condition | Contract | Result |
|---|---|---|
| CPOE occurrence is Active and routed to RNA | `ActiveOrderOccurrenceRouted` | RNA exposes one pending responsibility; CPOE remains order owner. |
| RNA work is pending and CPOE accepts an eligible change | `ActiveOrderOccurrenceModified` | RNA updates pending work and preserves earlier facts. |
| RNA work is pending and CPOE cancels the parent order | `ClinicalOrderCancelled` | RNA marks matching pending work source-cancelled; no RNA evidence is created. |
| Accommodation changes | No CPOE decision yet | RegId remains unchanged; RNA does not change Destination or cancel work automatically. |
| CPOE confirms reconciliation decision | `ReconciliationDestinationDecision` | RNA retains or changes pending-work Destination responsibility exactly as decided. |
| RNA performed work and CPOE occurrence is Active | `OccurrenceCompletedEvidence` | CPOE occurrence becomes Completed. |
| RNA determines work was not performed and CPOE occurrence is Active | `OccurrenceNotPerformedEvidence` | CPOE occurrence becomes Not Performed. |
| RNA corrects evidence associated with a final occurrence | `ExecutionEvidenceCorrectionNotice` | CPOE preserves the terminal status and records a conflict or reconciliation need. |
| Evidence conflicts with a Cancelled or otherwise final occurrence | Completed or Not Performed evidence | Both authoritative facts are preserved; CPOE records a conflict or reconciliation need without rewriting the final status. |

## 7. Correction Semantics

```text
RNA correction recorded
  → ExecutionEvidenceCorrectionNotice reported to CPOE
  → CPOE records the conflict or reconciliation need
  → Existing CPOE terminal status is not automatically rewritten
```

Rules:

1. RNA preserves the original evidence and its correction chain.
2. CPOE preserves the original fulfilment association and the correction notice in accountable history.
3. Correction does not prove another execution occurred.
4. Correction does not generate a new occurrence.
5. Correction does not authorize CPOE to edit RNA evidence.
6. The V1 contract promises no final-status reopening, supersession, or reactivation.

## 8. Inter-Ward Reconciliation

```text
Inter-Ward Transfer retains RegId
  → RNA exposes prior Ward, new Ward, transfer time, and relevant Active work references
  → CPOE performs assisted Active Order Reconciliation
  → CPOE confirms Retain or Change for each affected occurrence
  → ReconciliationDestinationDecision reported to RNA
  → RNA updates matching pending work only after the confirmed decision
```

Transfer participation rules:

1. RNA does not own the Transfer Reconciliation Aggregate.
2. Transfer, release, and assignment do not automatically change Destination.
3. Transfer, release, and assignment do not cancel an order or occurrence.
4. A decision arriving after work became final is retained as unapplied; final work is not changed.

## 9. Idempotency and Conflict Handling

Each business contract has one stable contract identity for semantic retries.

1. Repeating the same identity and same business content shall return the previously recorded processing result without repeating business behavior.
2. Reusing an identity with different business content shall create a visible conflict; last-write-wins is prohibited.
3. `OrderOccurrenceId` is mandatory for every native CPOE/RNA obligation and evidence chain.
4. `EvidenceReference` identifies the original RNA evidence; `CorrectionReference` identifies each append-only correction.
5. Delivery or processing acknowledgement is integration metadata and never means acceptance, execution, fulfilment, or cancellation.
6. Missing or conflicting facts are reconciled from each context's authoritative history; neither context fabricates foreign state.

## 10. V1 Business Workflows

### 10.1 Routed occurrence completed

```text
ActiveOrderOccurrenceRouted
  → RNA pending work
  → Work performed
  → RNA records execution truth
  → OccurrenceCompletedEvidence
  → CPOE occurrence Completed
  → CPOE recalculates Completion Progress and parent status
```

### 10.2 Routed occurrence not performed

```text
ActiveOrderOccurrenceRouted
  → RNA pending work
  → Responsible Destination determines work was not performed
  → RNA records reason and evidence reference
  → OccurrenceNotPerformedEvidence
  → CPOE occurrence Not Performed
  → CPOE recalculates Completion Progress and parent status
```

### 10.3 Clinical Order cancelled

```text
ClinicalOrderCancelled
  → RNA identifies matching pending occurrences
  → Pending work marked source-cancelled
  → No RNA fulfilment evidence created
  → Existing RNA execution history remains unchanged
```

### 10.4 Execution evidence corrected

```text
RNA preserves original evidence
  → RNA records append-only correction
  → ExecutionEvidenceCorrectionNotice
  → CPOE appends correction history
  → CPOE records conflict or reconciliation need
  → Existing terminal status remains unchanged
```

## 11. Implementation Guardrails

- Implement only the seven V1 business contracts defined in this document.
- Do not derive additional CPOE lifecycle states from RNA work coordination.
- Do not create Destination acceptance, rejection, clarification, hold, discontinuation, or exceptional-authorization contracts.
- Do not treat Ad Hoc or Independent Tindakan as retrospective CPOE work.
- Do not require ServiceId, tariff, billing, or financial acknowledgement before reporting Completed evidence.
- Do not create more than one current valid evidence chain for one OrderOccurrenceId.
- Do not rewrite a final CPOE occurrence because RNA evidence was corrected.
