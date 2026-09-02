# REQUEST CHANGE SKILL

# PURPOSE

Analyze a proposed change request and determine:

1. Impact Level
2. Owning Artifact
3. Required Artifact Changes
4. Downstream Propagation Scope
5. Suggested Update Sequence

The skill produces a single artifact:

```text
impact-analysis.md
```

The skill does NOT modify any artifact.

The skill does NOT perform implementation.

The skill does NOT rewrite DOMAIN, WORKFLOW, SOP, ARCHITECTURE, IMPLEMENTATION PLAN, or TEST artifacts.

The skill exists solely to determine:

> Where does this change belong, and what artifacts must be updated because of it?

---

# DESIGN PRINCIPLE

Every requested change originates from exactly one primary ownership level.

Changes propagate downstream.

Changes do not propagate upstream.

Examples:

```text
DOMAIN
  ↓
WORKFLOW
  ↓
SOP
  ↓
ARCHITECTURE
  ↓
IMPLEMENTATION
  ↓
TEST
```

If a change originates in DOMAIN, every downstream artifact may require revision.

If a change originates in ARCHITECTURE, DOMAIN, WORKFLOW, and SOP remain unchanged.

---

# IMPACT LEVELS

The skill must classify every change request into exactly one primary impact level.

## BUSINESS

Business truth changes.

Typical indicators:

- New business concept
- New ubiquitous language term
- New business capability
- New business rule
- New aggregate
- New domain object
- New domain event
- New lifecycle state
- New ownership rule
- New business invariant

Owning Artifact:

```text
DOMAIN.md
```

Propagation:

```text
DOMAIN
  ↓
WORKFLOW
  ↓
SOP
  ↓
ARCHITECTURE
  ↓
IMPLEMENTATION
  ↓
TEST
```

---

## OPERATIONAL

Business truth remains unchanged.

Business coordination changes.

Typical indicators:

- Workflow sequence changes
- Workflow trigger changes
- Responsibility handoff changes
- Decision flow changes
- Alternative flow changes
- Exception flow changes
- SOP changes
- User journey changes
- Business process changes

Owning Artifacts:

```text
WORKFLOW.md
SOP.md
```

Propagation:

```text
WORKFLOW
  ↓
SOP
  ↓
ARCHITECTURE
  ↓
IMPLEMENTATION
  ↓
TEST
```

DOMAIN remains unchanged.

---

## TECHNICAL

Business behavior remains unchanged.

Software realization changes.

Typical indicators:

- Architecture changes
- Integration changes
- Persistence strategy changes
- API design changes
- Messaging changes
- Security architecture changes
- Deployment architecture changes
- UI architecture changes

Owning Artifact:

```text
ARCHITECTURE.md
```

Propagation:

```text
ARCHITECTURE
  ↓
IMPLEMENTATION
  ↓
TEST
```

DOMAIN, WORKFLOW, and SOP remain unchanged.

---

## IMPLEMENTATION

Only source code behavior changes.

Typical indicators:

- Bug fixes
- Refactoring
- Query optimization
- Naming cleanup
- Performance improvement
- Internal code restructuring

Owning Artifact:

```text
Implementation
```

Propagation:

```text
IMPLEMENTATION
  ↓
TEST
```

All higher-level artifacts remain unchanged.

---

# REQUIRED INPUTS

The skill must read:

1. The change request.
2. The affected DOMAIN.md when available.
3. The affected WORKFLOW.md when available.
4. The affected SOP.md when available.
5. The affected ARCHITECTURE.md when available.
6. Existing impact-analysis.md when supplied.

The skill must determine ownership from authoritative artifacts.

Do not infer ownership solely from user wording.

---

# ANALYSIS PROCESS

## Step 1

Read the requested change.

Example:

```text
Product Code registration shall start from barcode capture.
```

---

## Step 2

Determine the affected business concern.

Examples:

```text
Business Truth
Business Coordination
Technical Realization
Implementation
```

---

## Step 3

Determine Impact Level.

Allowed values:

```text
BUSINESS
OPERATIONAL
TECHNICAL
IMPLEMENTATION
```

Exactly one value must be selected.

---

## Step 4

Determine Owning Artifact.

Examples:

```text
DOMAIN.md
WORKFLOW.md
SOP.md
ARCHITECTURE.md
Implementation
```

---

## Step 5

Determine downstream artifact propagation.

Use the propagation rules defined in this skill.

---

## Step 6

Identify required changes for every impacted artifact.

Describe:

- what must change
- why it must change

Do not write the actual change.

---

## Step 7

Produce impact-analysis.md.

Stop.

No artifact modifications are allowed.

---

# OUTPUT ARTIFACT

Generate:

```text
impact-analysis.md
```

---

# REQUIRED STRUCTURE

```markdown
# Impact Analysis

## Requested Change

<change request>

## Impact Level

BUSINESS | OPERATIONAL | TECHNICAL | IMPLEMENTATION

## Owning Artifact

<artifact>

## Reason

Explain why this artifact owns the change.

## Required Artifact Changes

### DOMAIN

Required / Not Required

<change summary>

### WORKFLOW

Required / Not Required

<change summary>

### SOP

Required / Not Required

<change summary>

### ARCHITECTURE

Required / Not Required

<change summary>

### IMPLEMENTATION

Required / Not Required

<change summary>

### TEST

Required / Not Required

<change summary>

## Suggested Update Sequence

1. ...
2. ...
3. ...

## Notes

Additional observations and risks.
```

---

# CLASSIFICATION RULES

Use the following decision table.

| Question | Classification |
|-----------|---------------|
| Does business truth change? | BUSINESS |
| Does workflow coordination change? | OPERATIONAL |
| Does software design change? | TECHNICAL |
| Does only source code change? | IMPLEMENTATION |

Apply the highest impacted level.

Examples:

| Change | Impact |
|----------|---------|
| New Business Rule | BUSINESS |
| New Domain Event | BUSINESS |
| New Workflow Step | OPERATIONAL |
| New Decision Branch | OPERATIONAL |
| New Screen Navigation | OPERATIONAL |
| New API Design | TECHNICAL |
| New Database Strategy | TECHNICAL |
| Refactoring | IMPLEMENTATION |
| Query Optimization | IMPLEMENTATION |

---

# NON-GOALS

The skill must never:

- update DOMAIN.md
- update WORKFLOW.md
- update SOP.md
- update ARCHITECTURE.md
- update IMPLEMENTATION PLAN
- update TEST artifacts
- generate implementation tasks
- generate implementation slices
- generate code

The skill only determines impact.

---

# SUCCESS CRITERIA

A successful execution produces:

1. One impact classification.
2. One owning artifact.
3. A complete downstream impact assessment.
4. A deterministic update sequence.
5. No artifact modifications.

The output must allow a human or another skill to perform the actual changes using the appropriate artifact-specific skill.