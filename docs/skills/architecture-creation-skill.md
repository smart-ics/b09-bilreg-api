# ARCHITECTURE CREATION SKILL

# PURPOSE

Generate a canonical `ARCHITECTURE.md` artifact that translates approved business artifacts into an implementation-ready technical design.

Architecture exists to answer:

> How will the approved Domain, Workflow, and Gap Analysis decisions be realized in software?

Architecture is a realization document.

Architecture is NOT:

- a business document;
- a workflow document;
- a planning document;
- an implementation document;
- a testing document.

Architecture transforms approved business decisions into a deterministic technical blueprint.

---

# POSITION IN ARTIFACT CHAIN

```text
DOMAIN
    ↓
WORKFLOW
    ↓
GAP ANALYSIS
    ↓
ARCHITECTURE
    ↓
IMPLEMENTATION PLAN
    ↓
IMPLEMENTATION
    ↓
TESTING
    ↓
DEPLOYMENT
```

Architecture sits between:

- Analysis
- Planning

Its purpose is to remove technical ambiguity before implementation planning begins.

---

# PRIMARY OBJECTIVE

Answer:

> What software structures, ownership boundaries, persistence models, integrations, user interfaces, screen behaviors, and technical decisions are required to realize the approved business design?

A successful Architecture document allows:

- Planner Agents
- Implementation Agents
- Review Agents

to execute without inventing design decisions.

---

# REQUIRED INPUTS

Mandatory:

- DOMAIN.md
- WORKFLOW.md
- GAP-ANALYSIS.md

Optional:

- Existing Source Code
- Existing Database Schema
- Existing Architecture Documents
- Existing Integration Specifications
- Existing UI Screenshots
- Existing UX Specifications

---

# ARCHITECTURE AUTHORITY

Architecture consumes:

- DOMAIN
- WORKFLOW
- CLOSED GAP decisions

Architecture must not redefine:

- business terminology;
- business rules;
- workflow rules;
- workflow ownership;
- business policies.

Those belong to DOMAIN and WORKFLOW.

---

# CRITICAL RULE

Architecture may not introduce new business decisions.

Every significant design decision must be traceable to:

- DOMAIN.md
- WORKFLOW.md
- CLOSED GAP decision

If a new business decision is discovered:

STOP.

Create a new GAP.

Return:

```text
Status:
BLOCKED

Reason:
New business decision required.
```

Architecture must never silently resolve business ambiguity.

---

# ARCHITECTURE RESPONSIBILITIES

Architecture is responsible for defining:

## Backend Realization

- ownership
- structure
- persistence
- integrations
- technical boundaries

## Frontend Realization

- screens
- workspace architecture
- navigation
- UI state models
- interaction rules
- view models
- frontend responsibilities

Architecture is not responsible for defining:

- business policies
- implementation phases
- implementation slices
- deployment procedures
- testing procedures

---

# DESIGN PRINCIPLES

Architecture should optimize for:

1. Clarity
2. Determinism
3. Simplicity
4. Traceability
5. Maintainability
6. Operational Efficiency
7. Low Cognitive Load

Prefer:

- existing patterns;
- existing conventions;
- existing infrastructure;
- existing framework capabilities.

Avoid introducing unnecessary complexity.

---

# ARCHITECTURE PROCESS

## Step 1 — Read Inputs

Read:

- DOMAIN.md
- WORKFLOW.md
- GAP-ANALYSIS.md

Identify:

- approved business concepts;
- approved workflows;
- approved decisions;
- remaining open gaps.

---

## Step 2 — Validate Readiness

If:

- Critical gaps remain open;
- Blocking gaps remain open;
- Required decisions remain unresolved;

Return:

```text
Status:
BLOCKED

Reason:
Architecture prerequisites incomplete.
```

Stop.

---

## Step 3 — Extract Approved Decisions

Collect:

- Closed Domain Decisions
- Closed Workflow Decisions
- Closed Technical Decisions
- Closed ADR Decisions

These become Architecture inputs.

Architecture must not reinterpret them.

---

## Step 4 — Design Backend Realization

Define:

- bounded contexts;
- aggregates;
- entities;
- value objects;
- repositories;
- persistence model;
- application services;
- integrations;
- security boundaries.

---

## Step 5 — Design Frontend Realization

Define:

- screen inventory;
- workspace structure;
- screen layout architecture;
- navigation model;
- UI state model;
- interaction rules;
- command mappings;
- validation ownership;
- view models.

---

## Step 6 — Validate Determinism

Ensure:

- ownership is explicit;
- persistence is explicit;
- integrations are explicit;
- UI behavior is explicit;
- screen responsibilities are explicit;
- implementation agents do not need to invent design.

---

# REQUIRED DOCUMENT STRUCTURE

# ARCHITECTURE

## 1. Executive Summary

### Purpose

Describe the business capability realized by this architecture.

### Status

```text
DRAFT
APPROVED
SUPERSEDED
```

### Inputs

List source artifacts:

- DOMAIN.md
- WORKFLOW.md
- GAP-ANALYSIS.md

---

## 2. Architecture Principles

Document important design principles.

Examples:

- Aggregate ownership rules
- Transaction rules
- Persistence rules
- Integration rules
- UI architecture rules
- Security rules

Only include principles relevant to the feature.

---

## 3. Decision Traceability

List all decisions consumed from Gap Analysis.

| Source Gap | Decision |
|------------|------------|
| GAP-ADR-001 | Item owns ProductCode |
| GAP-DOM-003 | ProductCode may be reactivated |

This section is mandatory.

Architecture decisions must be traceable.

---

# PART A — BACKEND ARCHITECTURE

---

## 4. Bounded Context Realization

For each context:

### Context Name

#### Responsibilities

...

#### Owns

...

#### Depends On

...

#### Exposes

...

---

## 5. Domain Model Realization

For each aggregate:

### Aggregate

#### Purpose

...

#### Aggregate Root

...

#### Child Entities

...

#### Value Objects

...

#### Invariants

...

#### Repository

...

---

## 6. Persistence Model

For each aggregate:

### Tables

...

### Primary Keys

...

### Foreign Keys

...

### Unique Constraints

...

### Indexes

...

### Soft Delete Strategy

...

### Audit Strategy

...

Persistence ambiguity should be minimized.

---

## 7. Application Layer Design

### Commands

List:

- command name
- responsibility

### Queries

List:

- query name
- responsibility

### Use Cases

Map workflow capabilities to application services.

---

## 8. Integration Design

For each integration:

### Purpose

...

### Trigger

...

### Payload

...

### Error Handling

...

### Retry Strategy

...

---

## 9. Security Design

Only if relevant.

Describe:

- authentication
- authorization
- permission boundaries
- security ownership

---

## 10. Operational Architecture

Describe:

### Migration Strategy

...

### Data Backfill Strategy

...

### Rollback Strategy

...

### Performance Considerations

...

### Concurrency Considerations

...

### Idempotency Requirements

...

Only include relevant concerns.

---

# PART B — FRONTEND ARCHITECTURE

---

## 11. Screen Inventory

Purpose:

Define all screens required to realize approved workflows.

For each screen:

### Screen ID

...

### Screen Name

...

### Purpose

...

### Primary Actor

...

### Workflow Reference

...

### Domain Reference

...

Every screen must be traceable to a workflow.

---

## 12. Screen Layout Architecture

Purpose:

Define architecture-level layout structure.

Not pixel-perfect UI.

Not visual design.

For each screen:

### Regions

Example:

```text
Toolbar
Filter Panel
Worklist
Detail Panel
Action Panel
```

### Layout Notes

...

---

## 13. Navigation Architecture

Purpose:

Define navigation structure.

Example:

```text
Dashboard
    ↓
Admission Workspace
    ↓
Admission Detail
```

For each navigation path:

### Source

...

### Target

...

### Conditions

...

---

## 14. UI State Architecture

Purpose:

Define allowed UI states and transitions.

For each screen:

### States

Example:

```text
Idle

↓ Select Patient

Patient Selected

↓ Select Class

Ready To Save

↓ Save

Saving

↓ Success

Completed
```

### Transition Rules

...

Implementation agents must not invent state transitions.

---

## 15. Workspace Modes

Purpose:

Define operational workspace modes.

Examples:

```text
Browse Mode
Edit Mode
Approval Mode
```

or

```text
Waiting Mode
Calling Mode
Working Mode
Completed Mode
```

For each mode:

### Purpose

...

### Available Actions

...

### Restrictions

...

---

## 16. Interaction Rules

Purpose:

Define enable/disable behavior.

For each rule:

### Condition

...

### Enabled Actions

...

### Disabled Actions

...

### Notes

...

These rules should not be hidden inside implementation.

---

## 17. UI Commands

Purpose:

Create frontend-backend traceability.

For each UI action:

| UI Action | Command / API |
|------------|------------|
| Save | CreateAdmissionCommand |
| Cancel | CancelAdmissionCommand |

This section is mandatory.

---

## 18. Validation Ownership

Purpose:

Prevent duplicated business logic.

### Client Validation

List:

- required fields
- formatting
- basic consistency checks

### Server Validation

List:

- business rules
- cross-aggregate rules
- authorization rules

Ownership must be explicit.

---

## 19. View Models

Purpose:

Define UI-facing data contracts.

For each ViewModel:

### Name

...

### Purpose

...

### Key Fields

...

### Source Query

...

---

## 20. Frontend Performance Architecture

Purpose:

Capture important UI performance decisions.

Examples:

### Worklist

```text
Default Load: 50 rows
Virtualization: Required
Auto Refresh: 10 seconds
```

### Search

```text
Minimum Characters: 3
Auto Search: No
Enter To Search: Yes
```

Only include meaningful decisions.

---

# PART C — TRACEABILITY

---

## 21. Traceability Matrix

Map:

```text
Domain Rule
    ↓
Workflow
    ↓
Gap Resolution
    ↓
Architecture
```

Example:

| Domain Rule | Workflow | Gap | Architecture |
|-------------|------------|------------|------------|
| BR-PCR-001 | WF-PCR-001 | GAP-DOM-002 | Aggregate Invariant |

This section is mandatory.

---

## 22. Architecture Risks

For each risk:

### Risk

...

### Impact

...

### Mitigation

...

### Residual Risk

...

Only include real risks.

---

## 23. Implementation Guidance

Summarize:

### Backend Constraints

...

### Frontend Constraints

...

### Critical Invariants

...

### Prohibited Shortcuts

...

This section helps Planning and Implementation Agents.

---

## 24. Architecture Readiness

### Ready For Planning

YES / NO

### Blocking Issues

...

### Planner Guidance

Summarize:

- expected implementation areas;
- major dependencies;
- recommended sequencing considerations.

Do not create implementation phases.

Do not create implementation slices.

---

# FRONTEND TRACEABILITY RULE

Every screen must be traceable to:

```text
Screen
    ↓
Workflow Capability
    ↓
Domain Capability
```

If a screen cannot be traced back to a workflow, it should not exist.

---

# IMPLEMENTATION TRACEABILITY RULE

Every architecture element should eventually be traceable to:

```text
Architecture Element
    ↓
Implementation Slice
    ↓
Code
```

This supports deterministic implementation and objective review.

---

# FORBIDDEN

Do not:

- redefine business rules;
- redefine workflow rules;
- create implementation phases;
- create implementation slices;
- generate source code;
- generate migrations;
- generate deployment instructions;
- silently resolve business ambiguity.

If ambiguity exists:

Create a GAP.

Do not invent a solution.

---

# SUCCESS CRITERIA

Architecture is successful when:

✓ all business decisions are traceable

✓ ownership is explicit

✓ aggregate boundaries are explicit

✓ persistence model is explicit

✓ integrations are explicit

✓ screens are explicit

✓ navigation is explicit

✓ UI states are explicit

✓ interaction rules are explicit

✓ frontend/backend responsibilities are explicit

✓ implementation agents do not need to invent design

✓ planner agents can create implementation slices safely

✓ reviewer agents can validate compliance objectively

✓ architecture faithfully realizes DOMAIN, WORKFLOW, and GAP decisions

✓ architecture introduces no new business decisions

✓ architecture becomes a deterministic blueprint for implementation