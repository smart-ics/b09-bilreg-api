# GAP ANALYSIS CREATION SKILL

# PURPOSE

Generate and maintain a canonical `GAP-ANALYSIS.md` artifact that evaluates the alignment between:

- `DOMAIN.md`
- `WORKFLOW.md`
- Existing Code Base (optional)

The purpose of Gap Analysis is to identify uncertainty, contradiction, missing business decisions, implementation constraints, and technical risks before Architecture creation.

Gap Analysis exists to answer:

> Can the current business domain and workflow be implemented safely, consistently, and deterministically?

If not:

- identify the issue;
- explain the impact;
- propose resolution options;
- capture analyst decisions;
- record resolution history; and
- determine Architecture readiness.

Gap Analysis is both:

1. an Analysis Report; and
2. a Decision Register.

It is the authoritative record of unresolved and resolved analysis findings before Architecture exists.

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

Gap Analysis is the bridge between:

- business definition; and
- technical design.

Architecture consumes decisions recorded in Gap Analysis.

Architecture must not silently re-open closed decisions.

---

# PRIMARY OBJECTIVE

Answer:

> What uncertainties exist, and how should they be resolved before Architecture is created?

The output must help an Architect create Architecture.md without discovering major unresolved questions.

---

# GAP ANALYSIS AUTHORITY

Gap Analysis is allowed to challenge:

- DOMAIN.md
- WORKFLOW.md
- Existing Code Base

Gap Analysis is allowed to recommend changes to:

- DOMAIN.md
- WORKFLOW.md

Gap Analysis must not assume existing artifacts are correct.

If a simpler, clearer, safer, or more implementable solution requires changing DOMAIN or WORKFLOW, the recommendation should explicitly state so.

---

# MODES

## MODE A — Create Gap Analysis

Create a new Gap Analysis report.

Input:

- DOMAIN.md
- WORKFLOW.md
- Code Base (optional)

Output:

- GAP-ANALYSIS.md

---

## MODE B — Update Existing Gap Analysis

Update an existing report after:

- new findings
- new artifact changes
- code-base review
- analyst feedback

The report must remain synchronized.

---

## MODE C — Close Gap

Example prompt:

```text
Decision for GAP-PCR-001 is:

Inactive Product Code may be reactivated.

Using gap-analysis-skill,
please close the gap.
```

Expected behavior:

- locate the gap;
- record the decision;
- update status;
- update readiness assessment;
- update decision register.

---

## MODE D — Reopen Gap

Example prompt:

```text
Reopen GAP-PCR-001.

Reason:
Business policy has changed.
```

Expected behavior:

- change status to OPEN;
- preserve historical decision;
- record reopen reason;
- update readiness assessment.

---

# REQUIRED INPUTS

Mandatory:

- DOMAIN.md
- WORKFLOW.md

Optional:

- Existing GAP-ANALYSIS.md
- Existing Code Base
- Existing database schema
- Existing Architecture.md
- Existing implementation plans

---

# ANALYSIS DIMENSIONS

The analysis evaluates five dimensions.

---

## 1. Domain Consistency

Review:

- terminology
- business rules
- states
- lifecycle
- ownership
- responsibilities
- domain events

Identify missing or conflicting business truth.

---

## 2. Workflow Consistency

Review:

- triggers
- preconditions
- participants
- decision points
- exception paths
- outcomes

Identify workflow ambiguity or incompleteness.

---

## 3. Domain ↔ Workflow Alignment

Validate:

- workflow terminology exists in domain;
- workflow states exist in domain;
- workflow decisions are supported by business rules;
- workflow events exist in domain;
- workflow ownership respects aggregate boundaries.

---

## 4. Technical Feasibility

(Optional)

Review:

- aggregate structure
- persistence model
- integration points
- current implementation
- infrastructure constraints

Determine whether current implementation can support the desired business behavior.

---

## 5. Architecture Decision Requirements

Identify decisions that Architecture must eventually realize.

Do not create architecture.

Do not design implementation.

Identify what must be decided.

---

# GAP CLASSIFICATION

Every finding must belong to one category.

---

## DOMAIN GAP

Identifier:

```text
GAP-DOM-xxx
```

Examples:

- missing business rule
- missing state
- undefined ownership
- undefined event
- missing terminology

---

## WORKFLOW GAP

Identifier:

```text
GAP-WF-xxx
```

Examples:

- missing trigger
- missing decision owner
- missing exception path
- ambiguous outcome

---

## ALIGNMENT GAP

Identifier:

```text
GAP-ALN-xxx
```

Examples:

- workflow uses undefined state
- workflow introduces new rule
- workflow contradicts domain

---

## TECHNICAL GAP

Identifier:

```text
GAP-TECH-xxx
```

Examples:

- database limitation
- integration limitation
- incompatible aggregate structure
- unsupported workflow

---

## ARCHITECTURE DECISION GAP

Identifier:

```text
GAP-ADR-xxx
```

Examples:

- aggregate ownership
- persistence strategy
- synchronization strategy
- migration strategy
- integration strategy

---

# GAP STATUS

Every gap must have exactly one status.

---

## OPEN

Gap exists.

Decision not finalized.

May block Architecture.

---

## CLOSED

Decision recorded.

Resolution agreed.

Architecture must treat the decision as established input.

---

# GAP SEVERITY

Every gap must have severity.

| Severity | Meaning |
|-----------|----------|
| Critical | Architecture cannot proceed |
| High | Major risk exists |
| Medium | Design quality affected |
| Low | Clarification recommended |

---

# REQUIRED DOCUMENT STRUCTURE

# GAP ANALYSIS

## 1. Executive Summary

### Overall Assessment

One of:

```text
READY FOR ARCHITECTURE
READY WITH CONDITIONS
NOT READY FOR ARCHITECTURE
```

### Gap Summary

| Type | Open | Closed |
|--------|--------|--------|
| Domain | | |
| Workflow | | |
| Alignment | | |
| Technical | | |
| Architecture Decision | | |

### Blocking Gaps

List all OPEN Critical gaps.

---

## 2. Domain Analysis

Review:

- terminology
- business rules
- lifecycle
- ownership
- responsibilities
- domain events

Document findings.

---

## 3. Workflow Analysis

Review:

- triggers
- preconditions
- participants
- decisions
- exceptions
- outcomes

Document findings.

---

## 4. Domain-Workflow Alignment Analysis

Validate consistency between:

- DOMAIN
- WORKFLOW

Document findings.

---

## 5. Technical Feasibility Analysis

(Optional)

Review:

- codebase
- database
- integrations
- architecture constraints

Document findings.

---

## 6. Architecture Decision Inventory

List unresolved architecture decisions.

| Gap ID | Topic | Status |
|---------|---------|---------|

Example:

| GAP-ADR-001 | ProductCode Ownership | OPEN |

---

## 7. Detailed Gap Findings

Every gap must follow this structure.

---

### GAP-XXX

#### Status

OPEN | CLOSED

#### Category

Domain | Workflow | Alignment | Technical | ADR

#### Severity

Critical | High | Medium | Low

#### Description

Describe the issue.

#### Evidence

Reference:

- DOMAIN section
- WORKFLOW section
- Code Base location

when available.

#### Impact

Explain:

- ambiguity
- inconsistency
- implementation risk
- technical limitation

#### Resolution Options

##### Option A

...

##### Option B

...

##### Option C

...

#### Recommendation

Preferred option.

Must explain why.

#### Decision

Required only when CLOSED.

Example:

```text
Inactive Product Code may be reactivated.
```

#### Decision Rationale

Explain why the decision was chosen.

#### Resolution

Describe:

- artifact changes required;
- architecture implication;
- implementation implication.

#### Blocking Status

BLOCKING | NON-BLOCKING

#### History

Example:

```text
2026-08-31
Created.

2026-09-01
Closed by Product Owner decision.
```

---

## 8. Artifact Change Recommendations

### DOMAIN.md Recommendations

| Gap | Recommendation |
|------|------|

---

### WORKFLOW.md Recommendations

| Gap | Recommendation |
|------|------|

This section is mandatory.

Gap Analysis may recommend changes to DOMAIN and WORKFLOW.

---

## 9. Decision Register

The Decision Register is the authoritative summary of all resolved gaps.

| Gap ID | Status | Decision |
|---------|---------|---------|

Example:

| GAP-PCR-001 | CLOSED | Product Code may be reactivated |
| GAP-PCR-002 | CLOSED | Item owns ProductCode |
| GAP-PCR-003 | OPEN | Pending |

This section is intended for:

- Architects
- Planners
- Implementers
- Reviewers

to quickly understand all approved decisions.

---

## 10. Architecture Readiness Assessment

Architecture may begin only when:

- no OPEN Critical gap exists;
- all blocking contradictions are resolved;
- all mandatory business decisions are recorded.

### Open Blocking Gaps

List all remaining blockers.

### Final Assessment

One of:

```text
READY FOR ARCHITECTURE
READY WITH CONDITIONS
NOT READY FOR ARCHITECTURE
```

### Architect Guidance

Summarize:

- important decisions already made;
- mandatory assumptions;
- remaining risks.

---

# DECISION OWNERSHIP RULE

Once a gap is CLOSED:

- the recorded decision becomes authoritative input;
- Architecture must consume it;
- Architecture must not silently replace it;
- reopening requires explicit analyst or stakeholder action.

---

# FORBIDDEN

Do not:

- write source code;
- create implementation plans;
- create database schema;
- generate migrations;
- invent architecture designs;
- silently resolve contradictions.

Instead:

- identify;
- explain;
- propose options;
- recommend;
- record decisions.

---

# SUCCESS CRITERIA

Gap Analysis is successful when:

✓ major uncertainty is removed

✓ business contradictions are visible

✓ implementation risks are visible

✓ analyst decisions are recorded

✓ Architecture can proceed confidently

✓ closed decisions are traceable

✓ implementers do not need to invent business decisions

✓ reviewers can trace implementation back to approved decisions

✓ DOMAIN → WORKFLOW → GAP ANALYSIS → ARCHITECTURE forms a deterministic design chain