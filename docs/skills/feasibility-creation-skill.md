# FEASIBILITY ASSESSMENT SKILL

# PURPOSE

Generate a canonical `FEASIBILITY-ASSESSMENT.md` artifact that evaluates whether a requested feature, enhancement, refactoring, integration, or operational change can be safely implemented in the current system.

The Feasibility Assessment exists to answer:

> Can this request be implemented safely in the current system, what areas are affected, what decisions are required, and is the request ready for implementation planning?

The Feasibility Assessment is an analysis artifact.

It is NOT:

* a business-domain document;
* a workflow document;
* an architecture document;
* an implementation plan;
* an implementation report;
* a review report.

Its purpose is to establish planning readiness.

---

# POSITION IN ARTIFACT CHAIN

## Legacy Enhancement Flow

```text
BUSINESS REQUEST
        ↓
FEASIBILITY ASSESSMENT
        ↓
IMPLEMENTATION PLAN
        ↓
IMPLEMENTATION
        ↓
REVIEW
```

---

## Structured Design Flow

```text
DOMAIN
    ↓
WORKFLOW
    ↓
GAP ANALYSIS
    ↓
ARCHITECTURE
```

Architecture-driven initiatives normally do not require a feasibility assessment.

Feasibility Assessment is primarily intended for:

* legacy systems;
* undocumented systems;
* enhancement requests;
* operational improvements;
* integration requests;
* maintenance projects.

---

# PRIMARY OBJECTIVE

Determine:

* implementation viability;
* affected system areas;
* required changes;
* technical risks;
* business risks;
* open questions;
* planning readiness.

The assessment must reduce uncertainty before planning begins.

---

# REQUIRED INPUTS

Mandatory:

* Business Request

At least one of:

* Existing Source Code
* Existing Database Schema
* Existing Screens
* Existing APIs
* Existing Documentation

Optional:

* Architecture Documents
* Design Documents
* Previous Feasibility Assessments
* User Interface Mockups
* Business Discussions

---

# CRITICAL RULE

The Feasibility Assessment may not create new business decisions.

The Feasibility Assessment may not silently resolve ambiguity.

If critical information is missing:

Return:

```text
Status:
BLOCKED

Reason:
Insufficient information.
```

Document missing information.

Do not invent assumptions.

---

# ASSESSMENT PHILOSOPHY

The assessment should focus on:

* current-state understanding;
* impact analysis;
* implementation viability;
* risk identification;
* planning preparation.

The assessment should avoid:

* implementation details;
* architecture redesign;
* implementation phases;
* implementation slices;
* source code generation.

---

# FEASIBILITY PROCESS

## Step 1 — Understand Request

Identify:

* requested capability;
* requested behavior;
* expected outcome;
* affected users;
* business motivation.

Summarize the request objectively.

Do not reinterpret the request.

---

## Step 2 — Analyze Current State

Identify:

### Existing Business Flow

...

### Existing Screens

...

### Existing APIs

...

### Existing Database Objects

...

### Existing Integrations

...

### Existing Security Model

...

Document only relevant findings.

---

## Step 3 — Impact Analysis

Determine affected areas.

### Backend Impact

Examples:

* Aggregate
* Entity
* Repository
* Service
* Command
* Query

### Database Impact

Examples:

* Table
* View
* Stored Procedure
* Index
* Constraint

### Frontend Impact

Examples:

* Screen
* Workspace
* ViewModel
* Navigation
* Validation

### Integration Impact

Examples:

* API
* Event
* External System

Every impacted area must be explicitly listed.

---

## Step 4 — Gap Analysis

Identify gaps between:

```text
Current State
        ↓
Requested State
```

Classify gaps:

### Functional Gap

Missing functionality.

### Technical Gap

Technical limitation.

### Data Gap

Missing data structure.

### UX Gap

User interaction limitation.

### Integration Gap

External dependency limitation.

---

## Step 5 — Solution Options

For each significant gap identify:

### Option A

...

### Advantages

...

### Disadvantages

...

### Risk

...

If multiple options are unnecessary, document only the recommended option.

---

## Step 6 — Recommended Approach

Describe:

* preferred solution;
* rationale;
* major implementation strategy;
* implementation constraints.

Keep discussion at solution level.

Do not create architecture.

---

## Step 7 — Risk Assessment

For each risk:

### Risk

...

### Impact

...

### Probability

LOW | MEDIUM | HIGH

### Mitigation

...

---

## Step 8 — Open Questions

Identify unresolved questions.

Classify:

### Business Question

...

### Technical Question

...

### Operational Question

...

Questions that block planning must be identified explicitly.

---

## Step 9 — Planning Readiness Assessment

Determine:

```text
READY
```

or

```text
NOT READY
```

Planning is READY when:

* request understood;
* impacts identified;
* solution selected;
* risks identified;
* blocking questions resolved.

---

# REQUIRED DOCUMENT STRUCTURE

# FEASIBILITY ASSESSMENT

## 1. Executive Summary

### Request

...

### Recommendation

...

### Feasibility Result

```text
FEASIBLE
```

or

```text
PARTIALLY FEASIBLE
```

or

```text
NOT FEASIBLE
```

---

## 2. Request Understanding

Describe:

* requested capability;
* business objective;
* expected outcome.

---

## 3. Current State Analysis

### Existing Business Flow

...

### Existing Components

...

### Existing Database

...

### Existing Integrations

...

---

## 4. Impact Analysis

### Backend Impact

...

### Database Impact

...

### Frontend Impact

...

### Integration Impact

...

### Security Impact

...

---

## 5. Gap Analysis

List identified gaps.

| Gap ID  | Type       | Description |
| ------- | ---------- | ----------- |
| GAP-001 | Functional | ...         |

---

## 6. Solution Options

For each significant gap.

### Option

...

### Advantages

...

### Disadvantages

...

### Recommendation

...

---

## 7. Recommended Approach

Describe preferred solution.

Must remain implementation-neutral.

---

## 8. Risks

| Risk | Impact | Probability | Mitigation |
| ---- | ------ | ----------- | ---------- |

---

## 9. Open Questions

List unresolved items.

If none:

```text
None.
```

---

## 10. Implementation Impact Inventory

Purpose:

Provide deterministic planning input.

### Backend

List all affected backend components.

### Database

List all affected database objects.

### Frontend

List all affected UI components.

### Integration

List all affected integrations.

### Security

List all affected security elements.

This section is mandatory.

---

## 11. Planning Readiness

### Status

```text
READY
```

or

```text
NOT READY
```

### Blocking Issues

...

### Planner Guidance

Summarize:

* implementation scope;
* major dependencies;
* sequencing concerns;
* review concerns.

Do not create implementation phases.

Do not create implementation slices.

---

# PLANNING HANDOFF RULE

The output of this assessment must allow a Planning Agent to determine:

```text
What must change
```

without determining:

```text
How it will be implemented
```

The assessment identifies impact.

The Planning Agent creates slices.

The Implementation Agent writes code.

The Review Agent verifies compliance.

---

# FORBIDDEN

Do not:

* generate architecture;
* generate implementation plans;
* create implementation slices;
* generate source code;
* redesign the entire system;
* introduce undocumented assumptions;
* create new business decisions.

If a decision is required:

Document it as an Open Question.

---

# SUCCESS CRITERIA

A Feasibility Assessment is successful when:

✓ request is clearly understood

✓ current state is analyzed

✓ impacted areas are identified

✓ gaps are documented

✓ viable solution identified

✓ risks are identified

✓ open questions are documented

✓ planning readiness is determined

✓ implementation scope is understandable

✓ planning can begin without additional discovery

✓ no architecture or implementation work is performed

✓ assessment remains objective and evidence-based
