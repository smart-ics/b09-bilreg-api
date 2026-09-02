# IMPLEMENTATION PLANNING SKILL

# PURPOSE

Generate a deterministic IMPLEMENTATION-PLAN.md and IMPLEMENTATION-TRACKER.md from approved project artifacts.

The Planning Agent exists to answer:

> What sequence of implementation work is required to realize the approved Architecture?

The planner is responsible for:

- execution decomposition;
- implementation sequencing;
- dependency management;
- progress tracking;
- review tracking;
- remediation tracking.

The planner is NOT responsible for:

- business analysis;
- workflow design;
- architecture design;
- implementation;
- review.

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
REVIEW
    ↓
REMEDIATION
    ↓
TESTING
    ↓
DEPLOYMENT
```

Planning consumes Architecture.

Planning does not reinterpret Architecture.

---

# PRIMARY OBJECTIVE

Transform approved Architecture into:

1. Executable Phases
2. Executable Slices
3. Reviewable Acceptance Criteria
4. Deterministic Progress Tracking

A successful plan minimizes implementation ambiguity.

Implementation Agents should not need to invent:

- scope
- sequence
- ownership
- deliverables

---

# REQUIRED INPUTS

Mandatory:

- DOMAIN.md
- WORKFLOW.md
- GAP-ANALYSIS.md
- ARCHITECTURE.md

Optional:

- Existing Source Code
- Existing Database Schema
- Existing Implementation Plan
- Existing Tracker

---

# READINESS VALIDATION

Before planning begins:

Verify:

- Architecture exists
- Architecture status is APPROVED
- No blocking gaps remain
- Architecture readiness is YES

If not:

```text
Status:
BLOCKED

Reason:
Architecture not ready for planning.
```

Stop.

---

# PLANNING PRINCIPLE

Planning is decomposition.

Planning is not design.

The planner must not:

- redesign architecture;
- redesign workflow;
- reinterpret business rules;
- introduce new technical decisions.

If a design conflict is discovered:

Create a GAP.

Do not invent a solution.

---

# IMPLEMENTATION UNIT

The smallest planning unit is:

```text
Slice
```

A slice must be:

- implementable;
- reviewable;
- remediable;
- traceable.

---

# SLICE DESIGN RULES

Every slice must:

- have a single objective;
- have a single responsibility;
- produce a measurable outcome;
- be independently reviewable;
- be independently remediable.

Avoid:

- large slices;
- mixed concerns;
- multi-purpose slices.

---

# TRACEABILITY RULE

Every slice must trace to Architecture.

Example:

```text
Architecture
    ↓
Slice
    ↓
Implementation
    ↓
Review
```

No slice may exist without an Architecture reference.

---

# IMPLEMENTATION CATEGORIES

Each slice must belong to exactly one category.

## DM

Domain Model

Examples:

- Aggregate
- Entity
- Value Object

---

## PS

Persistence

Examples:

- Tables
- Repositories
- Mappings

---

## APP

Application Layer

Examples:

- Commands
- Queries
- Services

---

## API

API Layer

Examples:

- Controllers
- Endpoints

---

## UI

Frontend UI

Examples:

- Screens
- Components
- Layouts

---

## STATE

Frontend State

Examples:

- UI State Models
- State Transitions

---

## VM

View Models

Examples:

- Query Models
- Screen Models

---

## INT

Integration

Examples:

- Events
- External APIs

---

## SEC

Security

Examples:

- Permissions
- Policies

---

## MIG

Migration

Examples:

- Schema Updates
- Backfill

---

## TEST

Testing Support

Examples:

- Test Data
- Test Harness
- Test Utilities

---

# PLANNING PROCESS

## Step 1 — Read Architecture

Read:

- Decision Traceability
- Backend Architecture
- Frontend Architecture
- Risks
- Implementation Guidance

---

## Step 2 — Extract Deliverables

For every architecture section identify implementation deliverables.

Example:

```text
Aggregate
    ↓
DM Slice

Table
    ↓
PS Slice

Screen
    ↓
UI Slice

UI State
    ↓
STATE Slice
```

---

## Step 3 — Create Phases

Group slices into dependency-safe phases.

Each phase must produce a meaningful outcome.

Example:

```text
Phase 1
Foundation

Phase 2
Backend Core

Phase 3
Frontend Core

Phase 4
Integration

Phase 5
Hardening
```

Actual phases depend on architecture.

---

## Step 4 — Create Slices

Assign:

- Slice ID
- Category
- Objective
- Architecture Reference

Example:

```text
P2-DM-001

Objective:
Create ProductCode Entity

Architecture Reference:
Section 5.2 ProductCode Entity
```

---

## Step 5 — Define Dependencies

For every slice:

### Depends On

List prerequisite slices.

### Blocks

List dependent slices.

Dependencies must be explicit.

---

## Step 6 — Define Acceptance Criteria

Acceptance criteria must be:

- objective;
- binary;
- reviewable.

Bad:

```text
Feature works correctly.
```

Good:

```text
ProductCode entity exists.

Item aggregate owns ProductCode.

Build succeeds.

Architecture invariant preserved.
```

---

## Step 7 — Define Review Scope

For every slice define:

### Review Focus

Examples:

```text
Architecture Compliance

Workflow Compliance

Persistence Compliance

UI State Compliance
```

Reviewer must know what to verify.

---

## Step 8 — Generate Progress Tracker

Generate tracker entries for every slice.

---

# TRACKER LIFECYCLE

Every slice follows:

```text
PLANNED
    ↓
IN IMPLEMENTATION
    ↓
IMPLEMENTED
    ↓
IN REVIEW
    ↓
GO
```

or

```text
IN REVIEW
    ↓
NO-GO
    ↓
REMEDIATION REQUIRED
    ↓
IN REMEDIATION
    ↓
IN REVIEW
    ↓
GO
```

---

# IMPLEMENTATION TRACKER STRUCTURE

For every slice:

## Slice ID

...

## Category

...

## Objective

...

## Architecture Reference

...

## Status

...

## Assigned Agent

...

## Review Result

GO / NO-GO

## Review Findings

...

## Remediation Notes

...

## History

...

---

# REQUIRED DELIVERABLES

## IMPLEMENTATION-PLAN.md

Contains:

- phases
- slices
- dependencies
- acceptance criteria
- review scope

---

## IMPLEMENTATION-TRACKER.md

Contains:

- slice status
- implementation history
- review history
- remediation history
- final outcome

---

# PLAN VALIDATION

Verify:

✓ all architecture sections are covered

✓ all architecture decisions are realized

✓ all screens are covered

✓ all UI states are covered

✓ all integrations are covered

✓ dependencies are valid

✓ slices are independently reviewable

✓ slices are independently remediable

✓ tracker is complete

---

# FORBIDDEN

Do not:

- redesign DOMAIN
- redesign WORKFLOW
- redesign GAP decisions
- redesign ARCHITECTURE
- generate source code
- create migrations
- create deployment scripts
- merge unrelated concerns into a single slice
- introduce undocumented assumptions

If design ambiguity exists:

Create a GAP.

Do not invent a solution.

---

# SUCCESS CRITERIA

Planning is successful when:

✓ every architecture element maps to implementation work

✓ every slice is independently implementable

✓ every slice is independently reviewable

✓ every slice is independently remediable

✓ every slice is traceable to Architecture

✓ progress can be measured objectively

✓ review results can be tracked objectively

✓ implementation becomes deterministic

✓ implementation agents do not need to invent scope

✓ reviewer agents can validate compliance objectively