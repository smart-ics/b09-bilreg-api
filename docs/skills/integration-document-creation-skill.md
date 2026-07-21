# integration-document-creation-skill.md

## Purpose

Create an integration document that defines how two bounded contexts collaborate without transferring or duplicating business authority.

The resulting document must explain:

* why the integration exists,
* which context owns each business decision,
* what facts or commands cross the boundary,
* what business effect each contract produces,
* and how duplicate, delayed, failed, or corrected delivery is handled.

The document must remain transport-agnostic unless the user explicitly requests an API or technical transport contract.

---

## When to Use This Skill

Use this skill when:

* two bounded contexts must exchange business facts or commands;
* an architecture gap identifies a missing cross-context contract;
* a context needs to consume another context’s authoritative data;
* one context must notify another after committing a local business fact;
* ownership is already defined but the collaboration contract is not;
* implementation agents need a stable reference before creating API contracts or code.

Do not use this skill for:

* internal communication inside one bounded context;
* UI interaction design;
* database schema design;
* REST endpoint or JSON schema design;
* SOP creation;
* domain modeling owned entirely by one context.

---

## Required Inputs

Before creating the document, inspect the available:

1. Source Context DOMAIN document.
2. Target Context DOMAIN document.
3. Relevant SOPs.
4. Source and target ARCHITECTURE documents.
5. Existing integration code or contracts, when available.
6. Open-gap or decision-backlog documents.

If information is incomplete, do not invent business rules. Record unresolved items explicitly.

---

## Core Principles

### 1. Preserve bounded-context ownership

Each business fact and state transition must have exactly one authoritative owner.

An integration may communicate a fact, but it must not silently transfer ownership.

Example:

* RNA owns `Accommodation Assigned`.
* Admisi owns closing its Waiting List.
* RNA publishes the placement result.
* Admisi applies the Waiting List transition.

### 2. Publish facts, not foreign decisions

A context should publish what it knows authoritatively.

Avoid contracts such as:

```text
CloseWaitingList
CreateBill
CompleteOrder
```

when those decisions belong to the receiving context.

Prefer:

```text
AccommodationAssigned
ServiceExecutionRecorded
AccommodationReleased
```

The receiving context decides its own consequence.

### 3. Keep the document transport-agnostic

Define:

* business contract,
* direction,
* payload meaning,
* ownership,
* reliability expectations.

Do not define unless explicitly requested:

* HTTP route,
* HTTP verb,
* message broker,
* controller,
* JSON serialization,
* database table,
* framework class.

These belong to API or technical contract artifacts.

### 4. Separate local truth from delivery state

A locally committed fact remains valid even when delivery fails.

The document must clearly distinguish:

* business fact status,
* delivery status,
* acknowledgement status,
* downstream business outcome.

### 5. Require stable identity and idempotency

Every retriable contract must have a stable identity.

Repeated delivery of the same fact must not repeat the receiving context’s business effect.

### 6. Corrections are new facts

Do not overwrite or retract previously published facts silently.

A correction must:

* reference the original fact,
* carry a new revision or correction identity,
* preserve the original history,
* allow the receiver to reconcile its own state.

---

## Document Naming

Use:

```text
<SOURCE>-<TARGET>-INTEGRATION.md
```

Examples:

```text
RNA-ADMISI-INTEGRATION.md
RNA-TATA-REKENING-INTEGRATION.md
CPOE-RNA-INTEGRATION.md
```

For bidirectional collaboration, use the two context names in the dominant workflow order.

---

## Required Document Structure

```text
# <SOURCE> ↔ <TARGET> Integration

## 1. Purpose and Scope

## 2. Context Ownership

## 3. Collaboration Overview

## 4. Business Contracts

## 5. Contract Payload Semantics

## 6. State and Responsibility Effects

## 7. Reliability and Consistency

## 8. Failure and Reconciliation

## 9. Security and Audit

## 10. Versioning and Compatibility

## 11. Open Decisions

## 12. Traceability
```

---

## Section Guidance

## 1. Purpose and Scope

Describe:

* why the integration exists;
* which business workflows require it;
* what the integration enables;
* what is explicitly outside its scope.

Keep this section short.

Example:

```text
This integration allows RNA to consume Admisi-owned Waiting List entries
and publish placement results after Accommodation Assignment.

It does not transfer Waiting List ownership to RNA and does not allow RNA
to modify Admisi persistence directly.
```

---

## 2. Context Ownership

Provide an ownership matrix.

| Business concern         | Authoritative owner | Consumer          |
| ------------------------ | ------------------- | ----------------- |
| Waiting List             | Admisi              | RNA               |
| Accommodation Assignment | RNA                 | Admisi            |
| Bed readiness            | RNA                 | Admisi, reporting |
| Waiting List closure     | Admisi              | RNA may observe   |

Rules:

* Every row must have one owner.
* Do not use “shared ownership.”
* Presentation or cached data does not imply authority.
* The receiver must not update the owner’s persistence directly.

---

## 3. Collaboration Overview

Show the end-to-end interaction using a simple sequence.

Example:

```text
Admisi creates Waiting List
→ RNA receives or reads Waiting List entry
→ Ward reviews
→ Ward rejects
    → Admisi keeps Waiting List active
or
→ RNA assigns Accommodation
    → RNA publishes Placement Result
    → Admisi closes Waiting List
```

This section should describe business collaboration, not infrastructure topology.

---

## 4. Business Contracts

Separate contracts by direction.

### 4.1 Source → Target

For each contract define:

| Contract | Type | Trigger | Business meaning | Receiver effect |
| -------- | ---- | ------- | ---------------- | --------------- |

Contract types may include:

* Business Fact
* Command Request
* Query Contract
* Acknowledgement
* Correction Fact

Prefer business facts over cross-context commands.

### 4.2 Target → Source

Use the same structure.

Each contract must state:

* who produces it;
* when it is produced;
* whether the local business transaction has already committed;
* what the receiver may do;
* what the receiver must not infer.

---

## 5. Contract Payload Semantics

Define the minimum business fields for each contract.

Recommended common envelope:

```text
ContractId
ContractType
ContractVersion
SourceContext
SourceFactId
SourceRevision
OccurredAt
RecordedAt
CorrelationId
```

Add only fields required to understand and process the business fact.

Example:

```text
AccommodationAssigned

WaitingListId
RegistrationId
AccommodationAllocationId
WardId
RoomId
BedId
AssignedAt
AssignedBy
```

Rules:

* Use stable IDs.
* Avoid duplicating full patient demographics.
* Avoid financial or clinical fields not owned by the source.
* Distinguish business time from recording time.
* State whether field values are authoritative, references, or display snapshots.

---

## 6. State and Responsibility Effects

Provide a transition table.

| Current owner/state | Received contract | New owner/state | Context performing transition |
| ------------------- | ----------------- | --------------- | ----------------------------- |

Example:

| Waiting List state | Contract              | Result         | Owner  |
| ------------------ | --------------------- | -------------- | ------ |
| Active             | PlacementRejected     | Remains Active | Admisi |
| Active             | AccommodationAssigned | Closed         | Admisi |

Clarify:

* whether responsibility changes;
* the exact fact that causes it;
* which context performs the state transition;
* whether there are intermediate states;
* what happens when the contract is not received.

Do not let transport acknowledgement become a business state transition unless explicitly intended.

---

## 7. Reliability and Consistency

Define:

### Delivery guarantee

Choose and state one:

* synchronous single-process call;
* at-least-once delivery;
* asynchronous durable delivery;
* read-on-demand query;
* mixed model.

### Idempotency

Define:

* idempotency key;
* duplicate handling;
* semantic replay behavior;
* conflicting replay behavior.

### Ordering

State whether:

* no ordering is required;
* ordering is required per aggregate;
* ordering is required per registration, order, Waiting List, or another scope.

### Transaction boundary

Clarify:

* what commits atomically in the source context;
* what is eventual;
* that cross-context distributed atomicity is not assumed.

Example:

```text
Accommodation Assignment and its outbound obligation commit atomically
inside RNA.

Admisi Waiting List closure occurs eventually after receiving the fact.

Failure to close the Waiting List does not roll back the committed
Accommodation Assignment.
```

---

## 8. Failure and Reconciliation

Define expected behavior for:

| Failure case                | Required behavior                           |
| --------------------------- | ------------------------------------------- |
| Receiver unavailable        | Persist and retry                           |
| Duplicate delivery          | Return prior outcome or ignore safely       |
| Unknown reference           | Reject technically and preserve source fact |
| Stale revision              | Reject or quarantine                        |
| Permanent rejection         | Escalate for reconciliation                 |
| Partial downstream success  | Track per destination                       |
| Correction delivery failure | Retry correction independently              |

Specify:

* who owns recovery;
* whether manual recovery is allowed;
* what users can observe;
* what must never be repeated.

Do not use destructive rollback of valid source facts as ordinary recovery.

---

## 9. Security and Audit

Define:

### Authentication

* trusted in-process application identity;
* authenticated service principal;
* authenticated user context;
* other approved mechanism.

### Authorization

State:

* who may invoke actor-facing commands;
* which source systems may publish integration facts;
* how Ward, tenant, facility, or patient scope is enforced.

### Audit

Record at minimum:

```text
Source Context
Source Fact ID
Contract Type
Contract Version
Business Time
Delivery Time
Sender Identity
Receiver Identity
Correlation ID
Attempt Count
Acknowledgement Result
Failure Reason
```

Do not expose unnecessary clinical or personal data in logs.

---

## 10. Versioning and Compatibility

Define:

* contract version field;
* additive change policy;
* breaking change policy;
* deprecation process;
* minimum supported versions;
* replay compatibility;
* correction compatibility.

Recommended rule:

```text
Additive optional fields may remain within the current major version.

Changes to business meaning, identity, required fields, or state effects
require a new major contract version.
```

---

## 11. Open Decisions

List only unresolved matters that materially affect implementation.

For each item define:

| ID | Decision | Owner | Blocks | Safe interim behavior |
| -- | -------- | ----- | ------ | --------------------- |

Do not hide unresolved assumptions inside contract examples.

Do not let implementation agents resolve open business decisions by convention.

---

## 12. Traceability

Map contracts to their sources.

| Contract | Domain rule | SOP step | Architecture use case |
| -------- | ----------- | -------- | --------------------- |

References should include:

* source DOMAIN;
* target DOMAIN;
* relevant SOPs;
* source and target ARCHITECTURE;
* related ADRs;
* related open gaps.

---

## Contract Classification Rules

Use the following distinctions carefully.

### Business Fact

A statement that something already happened.

Examples:

```text
AccommodationAssigned
AccommodationReleased
ServiceExecutionRecorded
```

### Command Request

A request asking another context to make its own decision.

Examples:

```text
RequestWaitingListCreation
RequestOrderCancellation
```

Use commands only when the receiver may accept or reject them according to its authority.

### Query Contract

A request for current authoritative information.

Examples:

```text
GetWaitingListEntry
GetTataRekeningFinalizationStatus
ResolveTarifService
```

Queries must not silently become cached authority.

### Acknowledgement

A technical or contractual confirmation that a fact was received.

Acknowledgement does not automatically mean the receiver completed its business transition.

### Correction Fact

A new fact that corrects or invalidates an earlier fact while preserving history.

---

## Quality Checklist

Before finalizing, verify:

### Ownership

* Does every business decision have one authoritative owner?
* Does either context directly mutate the other context’s persistence?
* Is any business state incorrectly treated as shared?

### Contracts

* Is every contract named in business language?
* Is the trigger clear?
* Is the business meaning clear?
* Is the receiver effect clear?
* Are prohibited inferences stated?

### Payload

* Are stable identities present?
* Is the payload minimal?
* Are business time and recorded time separated?
* Is unnecessary patient, clinical, or financial data excluded?

### Reliability

* Is idempotency defined?
* Is duplicate handling defined?
* Is ordering defined?
* Are local and cross-context transaction boundaries explicit?
* Is recovery possible without repeating business behavior?

### Corrections

* Is the original fact preserved?
* Is correction identity and revision defined?
* Can every receiver reconcile independently?

### Scope

* Is the document transport-agnostic?
* Are REST, JSON, SQL, or broker details excluded unless explicitly requested?
* Are unresolved decisions clearly marked?

### Traceability

* Can every contract be traced to DOMAIN, SOP, or ARCHITECTURE?
* Does the integration document avoid creating new business rules?

---

## Output Requirements

The generated document must:

* use concise, implementation-oriented language;
* treat AI implementation agents as a first-class audience;
* explicitly distinguish Existing, Target, and Deferred behavior where relevant;
* avoid vague terms such as “sync data” or “notify system” without defining the fact;
* avoid transport-specific details unless requested;
* include no invented business policy;
* preserve bounded-context authority;
* remain pragmatic and as short as possible while still being unambiguous.

---

## Recommended Generation Prompt

```text
Using integration-document-creation-skill.md, create the integration
document between [SOURCE CONTEXT] and [TARGET CONTEXT].

Use the relevant DOMAIN, SOP, ARCHITECTURE, existing codebase evidence,
and open-gap documents as sources.

The document must:

- preserve bounded-context ownership;
- define business facts, commands, queries, acknowledgements, and
  corrections crossing the boundary;
- define minimum payload semantics;
- define state and responsibility effects;
- define idempotency, ordering, transaction boundaries, retries,
  failure handling, and reconciliation;
- remain transport-agnostic;
- identify unresolved decisions explicitly;
- avoid inventing missing business rules;
- be optimized for AI implementation agents.

Finally, summarize:
1. authoritative ownership,
2. contracts in each direction,
3. implementation blockers,
4. decisions still required.
```
