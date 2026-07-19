# CPOE Domain

**Artifact status:** Canonical business specification  
**Bounded context:** Computerized Physician Order Entry (CPOE)  
**Version scope:** Pragmatic V1

## 1. Business Overview

### 1.1 Purpose

CPOE captures, routes, and tracks a clinician's intent as a `Clinical Order`. It preserves that intent and its complete business history from creation to a final outcome.

CPOE does not perform clinical work. The `Destination` performs the requested work and supplies `Fulfilment Evidence`; CPOE records the reported outcome.

### 1.2 Business value

CPOE provides one authoritative record of:

- what clinical work was requested;
- who requested it and for which `Patient`;
- where each planned execution was routed;
- whether each execution remains `Active`, was `Completed`, was `Not Performed`, or was `Cancelled`;
- how a scheduled order is progressing; and
- what changed, why it changed, and who made the change.

### 1.3 Scope

V1 owns exactly four primary business capabilities:

1. Clinical Order Management.
2. Order Routing.
3. Order Fulfilment Tracking.
4. Scheduled Order Management.

V1 also owns one supporting capability: Active Order Reconciliation for `Inter Ward Transfer`.

### 1.4 Business boundaries

CPOE owns clinician intent, destination assignment, order and occurrence status, completion progress, reconciliation decisions, and complete order history.

CPOE does not own:

- execution of the requested clinical work;
- clinical results or result review;
- destination-specific execution details;
- draft, acceptance, or clarification workflows;
- generic fulfilment workflows or workflow escalation;
- complex routing rules;
- discharge reconciliation;
- advanced or infinite recurrence;
- protocols or order templates;
- AI-assisted order generation; or
- an authorization model.

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Clinical Order | The authoritative expression of a clinician's intent for specified clinical work to be performed for one Patient. |
| Clinical Order Identifier | The stable business identity of a Clinical Order throughout its lifecycle. |
| Ordering Clinician | The clinician accountable for creating the Clinical Order and for any permitted modification or cancellation. |
| Patient | The person for whom the clinical work is ordered. |
| RegId | The stable registration identity for the Patient's care episode. An Inter Ward Transfer retains the same RegId. |
| Order Type | The business classification of requested clinical work used to determine an appropriate Destination. |
| Order Specification | The requested clinical work and the instructions necessary for the Destination to understand the clinician's intent. |
| Destination | The operational unit accountable for performing a routed Order Occurrence, such as a ward, laboratory, radiology unit, or pharmacy. |
| Order Occurrence | One explicitly planned execution of a Clinical Order, with its own identity, planned execution time, Destination, status, and optional Fulfilment Evidence. |
| Single-Occurrence Order | A Clinical Order containing exactly one Order Occurrence. |
| Scheduled Order | A Clinical Order containing more than one finite, explicitly planned Order Occurrence. |
| Planned Execution Time | The business time at which an Order Occurrence is intended to be performed. |
| Active | The non-final status indicating that a Clinical Order or Order Occurrence remains outstanding. |
| Completed | A final status indicating a performed occurrence, or an order closed through fulfilment with at least one completed occurrence. |
| Not Performed | A final status indicating that an occurrence was not performed, or an order closed through fulfilment with no completed occurrence. |
| Cancelled | A final status indicating that the Ordering Clinician ended the remaining intent before all outstanding occurrences were fulfilled. |
| Fulfilment Evidence | The Destination's business attestation that an Order Occurrence was Completed or Not Performed, including the outcome, effective time, responsible Destination, evidence reference, and a reason when not performed. |
| Completion Progress | The counts of total, Active, Completed, Not Performed, and Cancelled occurrences for a Clinical Order. |
| Destination Worklist | The collection of Active Order Occurrences currently assigned to one Destination, each shown with enough order context to understand the requested work. |
| Order History | The complete, chronological, append-only business record of creation, modifications, routing decisions, fulfilment outcomes, reconciliation decisions, and cancellation. |
| Inter Ward Transfer | A change from one ward to another within the Patient's continuing registration identified by RegId. |
| Active Order Reconciliation | The assisted review of Active Order Occurrences after an Inter Ward Transfer to determine whether their Destinations remain appropriate. |
| Transfer Reconciliation | The business record coordinating one Active Order Reconciliation for one Patient transfer. |
| Affected Occurrence Review | The assessment and human decision for one Active Order Occurrence whose Destination may be affected by an Inter Ward Transfer. |
| Reconciliation Reviewer | The clinician or operational representative accountable for deciding whether an affected Destination is retained or changed. |

## 3. Business Capabilities

### 3.1 Primary: Clinical Order Management

CPOE can:

- create a Clinical Order that becomes Active immediately;
- modify clinician intent only while the affected clinical work has not been executed;
- cancel an Active Clinical Order and its remaining Active occurrences; and
- preserve a complete Order History without replacing prior business facts.

There is no Draft status in V1.

### 3.2 Primary: Order Routing

CPOE can:

- determine a Destination from the Order Type, Order Specification, RegId, and current ward when relevant;
- assign every Active Order Occurrence to exactly one Destination;
- update an eligible Destination through an explicit business decision; and
- provide Destination Worklists grouped by Destination.

Routing assigns responsibility for execution. It does not represent acceptance of the order or performance of the work.

### 3.3 Primary: Order Fulfilment Tracking

CPOE can:

- record Fulfilment Evidence supplied by the Destination;
- finalize an Order Occurrence as Completed or Not Performed;
- derive the Clinical Order status from occurrence outcomes and explicit cancellation; and
- report Completion Progress without owning execution details or clinical results.

### 3.4 Primary: Scheduled Order Management

CPOE can represent one Clinical Order as a finite set of independently tracked Order Occurrences.

V1 scheduling is deliberately explicit:

- every occurrence is planned as part of a finite list;
- each occurrence has its own Planned Execution Time, Destination, and status;
- occurrences have no dependencies on one another; and
- a final order cannot renew itself or generate additional occurrences.

V1 does not use recurrence rules, cron expressions, infinite recurrence, or an advanced scheduling engine.

### 3.5 Supporting: Active Order Reconciliation

After an Inter Ward Transfer, CPOE can:

- evaluate the Patient's Active Order Occurrences;
- identify occurrences whose Destinations may depend on the prior ward;
- present each affected occurrence for review;
- record a decision to retain or change its Destination; and
- preserve the decision in the relevant Order History.

Reconciliation is assisted. A ward transfer never changes a Destination automatically.

## 4. Actors & Roles

### 4.1 Ordering Clinician

The Ordering Clinician:

- expresses clinical intent by creating a Clinical Order;
- supplies the Patient, RegId, Order Type, Order Specification, and finite occurrence plan;
- modifies eligible intent before execution;
- cancels an eligible Clinical Order with a business reason; and
- remains identifiable in the Order History.

### 4.2 Destination Fulfilment Representative

The Destination Fulfilment Representative is the clinician or operational representative acting for the Destination. This role:

- uses the Destination Worklist to understand outstanding occurrences;
- supplies Fulfilment Evidence for Completed or Not Performed occurrences; and
- does not redefine the original clinical intent.

### 4.3 Reconciliation Reviewer

The Reconciliation Reviewer:

- assesses Affected Occurrence Reviews after an Inter Ward Transfer;
- decides whether each affected Destination is retained or changed;
- supplies the reason for that decision; and
- does not change a Destination merely because a transfer occurred.

## 5. Domain Objects

### 5.1 Entities

#### Clinical Order

Purpose: represents one clinician's intent for one Patient.

Responsibilities:

- retain its stable Clinical Order Identifier;
- identify the Patient, RegId, Ordering Clinician, Order Type, and Order Specification;
- own one or more Order Occurrences;
- govern modification, cancellation, status, and Completion Progress; and
- own the complete Order History.

#### Order Occurrence

Purpose: represents one planned execution of a Clinical Order.

Responsibilities:

- retain an identity unique within the Clinical Order;
- retain its Planned Execution Time and Destination;
- transition independently from Active to one final status; and
- retain at most one Fulfilment Evidence record.

An Order Occurrence has no business meaning outside its Clinical Order.

#### Order History Entry

Purpose: preserves one material business fact in the lifecycle of a Clinical Order.

Each entry identifies what happened, when it happened, who was responsible, the reason when required, and the relevant before-and-after business values. An entry is never revised or removed.

#### Transfer Reconciliation

Purpose: coordinates assisted review following one Inter Ward Transfer for one Patient.

Responsibilities:

- identify the prior ward, new ward, and effective transfer time;
- retain the set of Affected Occurrence Reviews; and
- conclude only when every affected occurrence has a recorded decision.

#### Affected Occurrence Review

Purpose: represents the reconciliation assessment of one Active Order Occurrence.

Responsibilities:

- identify the Clinical Order and Order Occurrence under review;
- explain why the occurrence may be affected;
- record a decision to retain or change the Destination; and
- identify the Reconciliation Reviewer, decision time, and reason.

### 5.2 Value Objects

#### RegId

The stable registration identity for the Patient's care episode. The RegId remains unchanged during an Inter Ward Transfer; the current ward and Destination are separate routing facts.

#### Order Specification

The requested clinical work and the clinical instructions required to express the Ordering Clinician's intent. It excludes execution records and clinical results.

#### Destination

An identified operational unit accountable for execution of an Order Occurrence.

#### Fulfilment Evidence

An immutable attestation from the responsible Destination. It contains only the information required to substantiate a Completed or Not Performed outcome. It is not a clinical result.

#### Completion Progress

A derived summary of occurrence counts. Its total is fixed after any occurrence leaves Active status.

## 6. Aggregates

### 6.1 Clinical Order Aggregate

**Aggregate Root:** `Clinical Order`

**Owned entities:**

- one or more `Order Occurrence` entities;
- zero or more `Order History Entry` entities.

**Consistency boundary:**

The aggregate keeps clinician intent, occurrence outcomes, overall status, Completion Progress, routing assignments, and Order History mutually consistent.

Only the Clinical Order may:

- modify its Order Specification or occurrence plan;
- change an Active occurrence's Destination or Planned Execution Time;
- finalize an occurrence from Fulfilment Evidence;
- cancel remaining Active occurrences; or
- derive its overall status and Completion Progress.

### 6.2 Transfer Reconciliation Aggregate

**Aggregate Root:** `Transfer Reconciliation`

**Owned entities:**

- zero or more `Affected Occurrence Review` entities.

**Consistency boundary:**

The aggregate ensures that one Patient transfer is assessed once as a coherent reconciliation and that every affected occurrence receives one explicit decision.

A Transfer Reconciliation may recommend and record a Destination decision. It cannot directly reassign an Order Occurrence. A confirmed change must be accepted by the relevant Clinical Order under that order's own rules and must become part of its Order History.

## 7. Business Rules

### 7.1 Clinical Order identity and creation

- **BR-CPOE-001** — A Clinical Order shall represent exactly one Ordering Clinician's intent for exactly one Patient.
- **BR-CPOE-002** — A Clinical Order shall have a stable Clinical Order Identifier, a RegId, an Order Type, an Order Specification, and at least one Order Occurrence.
- **BR-CPOE-003** — A valid Clinical Order shall become Active immediately upon creation; V1 shall not retain a Draft Clinical Order.
- **BR-CPOE-004** — Every Order Occurrence shall have an identity unique within its Clinical Order and one explicit Planned Execution Time.
- **BR-CPOE-005** — A Clinical Order containing one occurrence is a Single-Occurrence Order; a Clinical Order containing more than one occurrence is a Scheduled Order.

### 7.2 Routing and worklists

- **BR-CPOE-006** — Every Active Order Occurrence shall have exactly one Destination.
- **BR-CPOE-007** — A Destination shall be appropriate to the Order Type, Order Specification, RegId, and current ward known when the routing decision is made.
- **BR-CPOE-008** — A Destination Worklist shall contain only Active Order Occurrences assigned to that Destination.
- **BR-CPOE-009** — Routing shall not imply that the Destination accepted, started, or performed the requested work.
- **BR-CPOE-010** — A Destination change shall identify the responsible person, decision time, reason, prior Destination, and new Destination in the Order History.

### 7.3 Modification and cancellation

- **BR-CPOE-011** — The Patient and Clinical Order Identifier shall never change after creation.
- **BR-CPOE-012** — The Order Type or Order Specification may be modified only while every Order Occurrence remains Active.
- **BR-CPOE-013** — The finite occurrence plan may be modified only while every Order Occurrence remains Active.
- **BR-CPOE-014** — The Planned Execution Time or Destination of a specific occurrence may be modified only while that occurrence remains Active.
- **BR-CPOE-015** — Every modification shall record the responsible person, modification time, reason, and relevant before-and-after values in the Order History.
- **BR-CPOE-016** — Only an Active Clinical Order may be cancelled.
- **BR-CPOE-017** — Cancelling a Clinical Order shall change every remaining Active occurrence to Cancelled and shall not change occurrences already Completed or Not Performed.
- **BR-CPOE-018** — Cancellation shall record the responsible person, cancellation time, and reason in the Order History.
- **BR-CPOE-019** — A final Clinical Order or Order Occurrence shall not return to Active.

### 7.4 Fulfilment and progress

- **BR-CPOE-020** — An Active Order Occurrence may become Completed only from Fulfilment Evidence supplied by its responsible Destination.
- **BR-CPOE-021** — An Active Order Occurrence may become Not Performed only from Fulfilment Evidence supplied by its responsible Destination, and that evidence shall include a reason.
- **BR-CPOE-022** — Fulfilment Evidence shall apply to exactly one Active Order Occurrence and shall be immutable after it is recorded.
- **BR-CPOE-023** — CPOE shall record only the reported fulfilment outcome and its evidence; it shall not infer execution details or clinical results.
- **BR-CPOE-024** — Completion Progress shall equal the counts of occurrences in each status, and those counts shall always sum to the total occurrence count.
- **BR-CPOE-025** — A Clinical Order shall remain Active while at least one occurrence is Active, unless the Clinical Order is explicitly cancelled.
- **BR-CPOE-026** — When all occurrences become final through fulfilment and at least one occurrence is Completed, the Clinical Order shall become Completed.
- **BR-CPOE-027** — When all occurrences become final through fulfilment and no occurrence is Completed, the Clinical Order shall become Not Performed.
- **BR-CPOE-028** — Explicit cancellation shall make the Clinical Order Cancelled even when earlier occurrences were Completed or Not Performed; those earlier outcomes shall remain visible in Completion Progress and Order History.

### 7.5 Scheduled orders

- **BR-CPOE-029** — A Scheduled Order shall contain a finite, explicit list of Order Occurrences.
- **BR-CPOE-030** — Each occurrence shall be tracked independently; one occurrence outcome shall not directly change another occurrence's status.
- **BR-CPOE-031** — Order Occurrences shall not depend on completion, failure, or timing of other occurrences.
- **BR-CPOE-032** — A Clinical Order shall not generate occurrences from an infinite recurrence, recurrence rule, cron expression, or auto-renewal policy.
- **BR-CPOE-033** — After any occurrence becomes final, the Clinical Order's total occurrence count shall not increase or decrease.

### 7.6 Active Order Reconciliation

- **BR-CPOE-034** — Active Order Reconciliation shall be initiated only for an Inter Ward Transfer.
- **BR-CPOE-035** — Reconciliation shall evaluate only Active Order Occurrences for the transferred Patient.
- **BR-CPOE-036** — An occurrence shall be identified as affected only when its Destination may depend on the prior ward or current ward, while its RegId remains unchanged.
- **BR-CPOE-037** — An Inter Ward Transfer shall never reassign a Destination automatically.
- **BR-CPOE-038** — Every Affected Occurrence Review shall conclude with exactly one decision: retain the current Destination or change to a specified Destination.
- **BR-CPOE-039** — A reconciliation decision shall identify the Reconciliation Reviewer, decision time, and reason.
- **BR-CPOE-040** — A decision to change Destination shall apply only if the occurrence is still Active and the proposed Destination is appropriate at the time of change.
- **BR-CPOE-041** — If an occurrence becomes final before its reconciliation decision is applied, its Destination shall remain unchanged and the unapplied decision shall be recorded as such.
- **BR-CPOE-042** — A Transfer Reconciliation shall become Completed only after every Affected Occurrence Review has a recorded decision; it may complete immediately when no affected occurrences are found.

### 7.7 Audit history

- **BR-CPOE-043** — Order History shall be chronological, append-only, and complete for all material order lifecycle facts.
- **BR-CPOE-044** — A later correction or decision shall add a new Order History Entry and shall not erase or replace an earlier entry.

## 8. State Machines & Lifecycles

### 8.1 Clinical Order lifecycle

```text
Create valid Clinical Order
          |
          v
        Active
       /   |   \
      /    |    \
     v     v     v
Completed  Not Performed  Cancelled
```

| State | Business meaning | Allowed next states |
|---|---|---|
| Active | At least one occurrence remains outstanding and the order has not been explicitly cancelled. | Completed, Not Performed, Cancelled |
| Completed | All occurrences are final through fulfilment and at least one occurrence is Completed. | None |
| Not Performed | All occurrences are final through fulfilment and no occurrence is Completed. | None |
| Cancelled | The remaining clinician intent was explicitly ended; prior occurrence outcomes, if any, are retained. | None |

Creation has no Draft stage. Completed, Not Performed, and Cancelled are final.

### 8.2 Order Occurrence lifecycle

```text
          Active
         /   |   \
        v    v    v
Completed  Not Performed  Cancelled
```

| Transition | Cause |
|---|---|
| Active -> Completed | The responsible Destination supplies Completed Fulfilment Evidence. |
| Active -> Not Performed | The responsible Destination supplies Not Performed Fulfilment Evidence with a reason. |
| Active -> Cancelled | The parent Clinical Order is explicitly cancelled. |

Every final occurrence retains its outcome permanently.

### 8.3 Completion Progress lifecycle

Completion Progress begins with all occurrences Active. Each occurrence transition moves exactly one occurrence from Active to a final count. The progress is complete when the Active count reaches zero.

For a cancelled Clinical Order, progress still distinguishes prior Completed and Not Performed occurrences from the occurrences cancelled before execution.

### 8.4 Transfer Reconciliation lifecycle

```text
Inter Ward Transfer reported
            |
            v
          Active
            |
            v
        Completed
```

| State | Business meaning | Allowed next states |
|---|---|---|
| Active | Active occurrences are being evaluated or affected occurrences await decisions. | Completed |
| Completed | Every affected occurrence has a recorded decision, or no affected occurrences were found. | None |

Reconciliation completion does not imply that every proposed Destination change was applied; unapplied decisions remain recorded.

## 9. Domain Events

| Domain Event | Business meaning |
|---|---|
| Clinical Order Created | A valid Clinical Order became Active with its finite occurrence plan and initial routing. |
| Clinical Order Modified | Eligible clinician intent or occurrence planning changed before the affected work was executed. |
| Order Occurrence Routed | An Active Order Occurrence was assigned to a Destination. |
| Order Occurrence Destination Changed | An explicit decision changed the Destination of an Active Order Occurrence. |
| Order Occurrence Completed | Fulfilment Evidence established that one Order Occurrence was performed. |
| Order Occurrence Not Performed | Fulfilment Evidence established that one Order Occurrence was not performed. |
| Clinical Order Completed | Fulfilment closed all occurrences and at least one occurrence was Completed. |
| Clinical Order Not Performed | Fulfilment closed all occurrences and none was Completed. |
| Clinical Order Cancelled | The Ordering Clinician ended an Active Clinical Order and its remaining Active occurrences. |
| Transfer Reconciliation Started | An Inter Ward Transfer initiated evaluation of the Patient's Active Order Occurrences. |
| Affected Order Occurrence Identified | An Active Order Occurrence was found to have a potentially ward-dependent Destination. |
| Reconciliation Decision Recorded | A Reconciliation Reviewer decided to retain or change an affected occurrence's Destination. |
| Transfer Reconciliation Completed | Every affected occurrence received a decision, or no affected occurrences were found. |

Each event records a business fact after its governing rules have been satisfied. Events do not assert that clinical work occurred unless supported by Fulfilment Evidence.

## 10. Business Workflows

### 10.1 Create and route a Clinical Order

1. The Ordering Clinician expresses an Order Specification for one Patient and RegId.
2. The Ordering Clinician defines one occurrence or a finite set of scheduled occurrences.
3. An appropriate Destination is determined for every occurrence.
4. The Clinical Order becomes Active.
5. Each Active occurrence appears in its Destination Worklist.

**Outcome:** one Active Clinical Order exists with complete initial routing and history.

### 10.2 Modify unexecuted clinical intent

1. The Ordering Clinician identifies an eligible Active Clinical Order or Active Order Occurrence.
2. The proposed change is assessed against the modification rules.
3. The eligible business values are changed.
4. Routing is reassessed when the change can affect Destination.
5. The change and its reason are appended to Order History.

**Outcome:** unexecuted intent is updated without altering prior business facts.

### 10.3 Record occurrence fulfilment

1. The responsible Destination supplies Fulfilment Evidence for one Active occurrence.
2. The occurrence becomes Completed or Not Performed.
3. Completion Progress is recalculated.
4. The Clinical Order remains Active when another occurrence is Active.
5. When no occurrence remains Active through fulfilment, the Clinical Order becomes Completed if at least one occurrence was Completed; otherwise it becomes Not Performed.

**Outcome:** the reported execution outcome is recorded without CPOE performing or interpreting the clinical work.

### 10.4 Cancel a Clinical Order

1. The Ordering Clinician identifies an Active Clinical Order and supplies a cancellation reason.
2. Every remaining Active occurrence becomes Cancelled.
3. Earlier Completed and Not Performed occurrence outcomes remain unchanged.
4. The Clinical Order becomes Cancelled.
5. Completion Progress and Order History preserve the full outcome.

**Outcome:** outstanding clinician intent ends without obscuring work already performed or not performed.

### 10.5 Track a Scheduled Order

1. A finite occurrence plan is established when the Clinical Order is created.
2. Each occurrence is routed and tracked independently.
3. Each Fulfilment Evidence record finalizes only its referenced occurrence.
4. Completion Progress reports all occurrence counts.
5. The Clinical Order reaches a final status when no occurrence remains Active or when the order is explicitly cancelled.

**Outcome:** one Clinical Order provides finite, independently traceable executions and deterministic overall progress.

### 10.6 Reconcile Active Orders after an Inter Ward Transfer

1. An Inter Ward Transfer starts one Transfer Reconciliation for the Patient.
2. The Patient's Active Order Occurrences are evaluated against the prior and new ward context.
3. Potentially ward-dependent occurrences become Affected Occurrence Reviews.
4. The Reconciliation Reviewer decides to retain or change each affected Destination and records a reason.
5. A confirmed change is applied only when the occurrence is still Active and the proposed Destination remains appropriate.
6. Each decision and any applied change are appended to the relevant Order History.
7. The Transfer Reconciliation becomes Completed after every affected occurrence has a decision, or immediately when none are affected.

**Outcome:** ward-dependent routing is explicitly reviewed without automatic reassignment.
