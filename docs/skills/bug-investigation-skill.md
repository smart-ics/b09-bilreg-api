# BUG INVESTIGATION SKILL

# PURPOSE

Generate and maintain a deterministic BUG-INVESTIGATION.md artifact for reported defects.

The Investigation Agent exists to answer:

> Why does the defect occur and what approved fix decision should be implemented?

The Investigation Agent is responsible for:

* defect analysis;
* evidence collection;
* root cause identification;
* impact analysis;
* fix option analysis;
* decision recording.

The Investigation Agent is NOT responsible for:

* implementation planning;
* coding;
* remediation;
* deployment.

---

# POSITION IN ARTIFACT CHAIN

```text
BUG REPORT
    ↓
BUG INVESTIGATION
    ↓
DECISION
    ↓
IMPLEMENTATION PLAN
    ↓
IMPLEMENTATION
    ↓
REVIEW
    ↓
REMEDIATION
```

Planning consumes BUG-INVESTIGATION.md.

Investigation does not create implementation tasks.

---

# OPERATION MODES

The skill supports two modes.

## Mode 1 — Create Investigation

Create a new BUG-INVESTIGATION.md from a reported defect.

Purpose:

* understand the defect;
* identify root cause;
* identify missing evidence;
* evaluate fix options.

Output:

```text
BUG-INVESTIGATION.md
```

---

## Mode 2 — Record Decision

Update an existing BUG-INVESTIGATION.md after:

* root cause confirmation;
* missing evidence closure;
* business decision;
* technical decision.

Purpose:

* record approved decisions;
* close investigation items;
* prepare artifact for planning.

Output:

Updated BUG-INVESTIGATION.md.

Do not create a new artifact.

Update the existing artifact.

---

# INVESTIGATION ENTRY POINT

Investigation may start from:

1. User-provided bug description
2. ISSUE.md

If only a bug description is provided:

* Extract Problem Statement.
* Infer affected workflow.
* Create initial BUG-INVESTIGATION.md.

Do not require ISSUE.md.

ISSUE.md becomes relevant only when operating within the complete SDLC framework.

---

# REQUIRED INPUTS

One of the following is required:

* Bug Description
* ISSUE.md

Optional:

* DOMAIN.md
* WORKFLOW.md
* GAP-ANALYSIS.md
* ARCHITECTURE.md
* Source Code
* Database Schema
* Existing BUG-INVESTIGATION.md

---

# INVESTIGATION PRINCIPLE

Investigation is discovery.

Investigation is not implementation.

The agent must:

* collect evidence;
* identify causes;
* identify uncertainty.

The agent must not:

* write code;
* generate implementation plans;
* assume root causes without evidence.

---

# BUG-INVESTIGATION STRUCTURE

## 1. Investigation Information

```text
Investigation ID:
...

Status:
IN PROGRESS | READY FOR PLANNING | CLOSED

Created At:
...

Updated At:
...
```

---

## 2. Problem Statement

Describe:

* reported issue;
* affected workflow;
* affected modules;
* business impact.

---

## 3. Current Behavior

Describe actual observed behavior.

---

## 4. Expected Behavior

Describe expected behavior.

---

## 5. Investigation Findings

Document all evidence collected.

Examples:

* source files;
* services;
* workflows;
* database records;
* event handlers;
* queries;
* logs;
* screenshots.

Every finding should be traceable.

---

## 6. Root Cause Candidates

Format:

```text
RC-01

Description:
...

Status:
UNCONFIRMED | CONFIRMED | REJECTED

Evidence:
...
```

Multiple candidates are allowed.

A candidate must not be marked CONFIRMED without supporting evidence.

---

## 7. Missing Evidence

Used when investigation cannot yet prove or reject a root cause.

Format:

```text
ME-01

Description:
...

Status:
OPEN | CLOSED

Resolution:
...
```

All Missing Evidence must be closed before planning.

---

## 8. Fix Options

Document alternative solutions.

Format:

```text
FO-01

Description:
...

Advantages:
...

Risks:
...
```

Multiple options may exist.

Do not make implementation plans.

Do not write code.

---

## 9. Recommended Fix

Recommend the most reasonable option based on available evidence.

Recommendation remains provisional until a decision is recorded.

Format:

```text
Recommended Option:
FO-01

Reason:
...
```

---

## 10. Decisions

Initially empty.

Decisions are added only through Record Decision mode.

Never modify previous decisions.

Never delete historical decisions.

Append only.

Format:

```text
Decision ID:
DEC-001

Timestamp:
2026-09-16 14:30

Decision:
Adopt FO-01

Reason:
...

Implementation Scope:
...

Constraints:
...

Approved By:
...
```

Multiple decisions are allowed.

---

## 11. Readiness Assessment

Evaluate whether investigation is ready for planning.

Criteria:

* All Root Cause Candidates are CONFIRMED or REJECTED.
* All Missing Evidence are CLOSED.
* At least one Decision exists.

Format:

```text
Root Cause Status:
PASS | FAIL

Missing Evidence Status:
PASS | FAIL

Decision Status:
PASS | FAIL

Ready For Planning:
YES | NO
```

---

# RECORD DECISION MODE

When operating in Record Decision mode:

1. Read existing BUG-INVESTIGATION.md.
2. Locate affected Root Cause Candidate.
3. Locate affected Missing Evidence item.
4. Update statuses as necessary.
5. Append a new Decision entry.
6. Recalculate Readiness Assessment.

Never remove historical findings.

Never rewrite previous decisions.

Append only.

---

# PLANNING COMPATIBILITY

BUG-INVESTIGATION.md must be consumable by IMPLEMENTATION PLANNING SKILL.

The Planning Agent must be able to identify:

* approved fix decisions;
* affected workflows;
* affected modules;
* implementation scope;
* constraints;
* risks.

The Planning Agent must not need to perform additional investigation.

---

# FORBIDDEN

Do not:

* generate implementation phases;
* generate implementation slices;
* generate implementation plans;
* generate source code;
* redesign DOMAIN;
* redesign WORKFLOW;
* redesign GAP decisions;
* redesign ARCHITECTURE;
* hide uncertainty;
* mark a root cause as confirmed without evidence.

If investigation cannot prove a root cause:

* create a Root Cause Candidate;
* create Missing Evidence items;
* keep the investigation open.

Do not invent conclusions.

---

# SUCCESS CRITERIA

Investigation is successful when:

✓ root cause is identified

✓ evidence is documented

✓ uncertainty is explicit

✓ fix options are documented

✓ approved decisions are recorded

✓ readiness status is explicit

✓ Planning Agent can generate IMPLEMENTATION-PLAN.md without re-investigating the defect

✓ implementation scope is deterministic

✓ historical investigation decisions remain traceable
