# Artifact Management Skill

## Purpose

Keep repository artifacts clean, trustworthy, and retrieval-friendly.

The skill prevents historical reports from polluting design analysis while preserving traceability.

---

# Artifact Classification

Every artifact belongs to exactly one category.

## Active

Current source of truth.

Examples:

- DOMAIN.md
- SOP.md
- ARCHITECTURE.md
- PERSISTENCE-DESIGN.md
- SCREEN-DESIGN.md
- ADR-*

Characteristics:

- Normative
- Current
- Used for all design decisions

---

## Working

Temporary analysis artifacts.

Examples:

- Gap Analysis
- Investigation Report
- Review Report
- Feasibility Report
- Assessment Report

Characteristics:

- May contain open decisions
- May contain assumptions
- Not authoritative

---

## Archived

Historical artifacts.

Characteristics:

- Decisions already absorbed into Active artifacts
- No longer participate in normal context retrieval
- Retained only for traceability and historical reference

Location:

```text
/archive
```

---

# Retrieval Rules

## Work Mode (Default)

Use:

- Active artifacts
- Working artifacts

Ignore:

- archive/*

Archived artifacts must not be retrieved unless explicitly requested.

Examples:

- search archive
- historical investigation
- previous decision rationale
- decision traceability
- find old report

---

# Authority Hierarchy

When artifacts conflict:

```text
ADR
    >
Active Design
    >
Working Document
    >
Archived Document
```

Higher authority always wins.

---

# Decision Management Rule

Reports do not own decisions.

Before a Working document can be closed:

- decision must be transferred into ADR
  OR
- decision must be transferred into an Active design artifact

Decisions must never live exclusively inside reports.

---

# Artifact Metadata

Recommended header:

```yaml
Artifact-Type: ReviewReport
Status: Working
Normative-Level: Historical
Superseded-By:
Archive-Eligible: No
```

Examples:

```yaml
Artifact-Type: ADR
Status: Active
Normative-Level: Authoritative
```

```yaml
Artifact-Type: Design
Status: Active
Normative-Level: Normative
```

---

# Cleanup Mode

Purpose:

Identify Working artifacts that can be archived.

---

## Step 1

Scan all Working artifacts.

Look for:

- Open Decision
- TBD
- TODO
- Pending
- Requires Investigation
- Unresolved Questions

---

## Step 2

Classify:

```text
Working
Archive Candidate
Requires Review
```

---

## Step 3

Validate Traceability

For each Archive Candidate:

Verify that every accepted decision has been absorbed into:

- ADR
- DOMAIN
- SOP
- ARCHITECTURE
- PERSISTENCE DESIGN
- Other Active artifacts

If traceability cannot be proven:

```text
Requires Review
```

---

## Step 4

Archive Recommendation

Produce:

```text
Move:
    xxx-review-report.md

Reason:
    All decisions absorbed into:
        ADR-APT-003
        PERSISTENCE-DESIGN.md
```

---

# Core Principle

Reports generate decisions.

ADRs and Active Design artifacts own decisions.

Once all decisions are absorbed into Active artifacts, the report becomes an Archive Candidate.