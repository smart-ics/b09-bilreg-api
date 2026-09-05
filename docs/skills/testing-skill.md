# TESTING SKILL

# PURPOSE

Generate and maintain testing artifacts used by human testers during staging verification.

The Testing Skill exists to answer:

> What should be tested, how should it be tested, what has been tested, what defects were found, what fixes were applied, and what is the current testing progress?

The Testing Skill is responsible for:

- generating test packages;
- maintaining testing progress;
- maintaining execution history;
- maintaining defect history;
- maintaining retest history;
- maintaining fix history;
- maintaining git commit traceability.

The Testing Skill is NOT responsible for:

- implementing code;
- reviewing architecture;
- redesigning workflows;
- redesigning domains;
- deploying software.

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
TEST PACKAGE
    ↓
TEST EXECUTION
    ↓
PILOT DEPLOYMENT
```

Testing occurs only after implementation has passed review.

---

# PRIMARY OBJECTIVE

Provide a deterministic testing workflow where:

- testers follow predefined scenarios;
- testing progress is tracked automatically;
- execution history is preserved;
- defects are documented;
- fixes are documented;
- retests are documented;
- git commit traceability is maintained.

The tester should never manually edit testing artifacts.

All updates must be performed through this skill.

---

# TESTING PHILOSOPHY

Testing verifies observable behavior.

Testing does not verify implementation details.

The tester should validate:

- user behavior;
- workflow behavior;
- business behavior;
- expected outcomes.

The tester should NOT validate:

- repository design;
- aggregate structure;
- class design;
- architectural patterns.

Those concerns belong to Review.

---

# REQUIRED INPUTS

For package generation:

Mandatory:

- DOMAIN.md
- WORKFLOW.md
- ARCHITECTURE.md
- IMPLEMENTATION-PLAN.md

Optional:

- IMPLEMENTATION-TRACKER.md
- REVIEW REPORTS

For execution updates:

Mandatory:

- TEST-CHECKLIST.md
- TEST-SCENARIOS.md
- TEST-EXECUTION.md
- Tester Result

Optional:

- Git Repository History

---

# GENERATED ARTIFACTS

The Testing Skill generates and maintains:

```text
TEST-CHECKLIST.md
TEST-SCENARIOS.md
TEST-EXECUTION.md
```

---

# ARTIFACT RESPONSIBILITIES

## TEST-CHECKLIST.md

Purpose:

Operational testing dashboard.

Contains:

- overall progress;
- test case status;
- completion percentage;
- summary statistics.

The tester uses this document to monitor progress.

The tester must never manually modify it.

The Testing Skill maintains it automatically.

---

## TEST-SCENARIOS.md

Purpose:

Immutable testing instructions.

Contains:

- test case definitions;
- objectives;
- preconditions;
- test steps;
- expected results.

This document is generated once.

It should not be modified during testing.

---

## TEST-EXECUTION.md

Purpose:

Testing audit trail.

Contains:

- execution history;
- defect history;
- fix history;
- retest history;
- git commit history.

All execution updates are appended.

History must never be deleted.

---

# TESTING SKILL MODES

The skill supports two modes.

---

# MODE A — GENERATE TEST PACKAGE

Purpose:

Generate initial testing artifacts.

Input:

```text
DOMAIN.md
WORKFLOW.md
ARCHITECTURE.md
IMPLEMENTATION-PLAN.md
```

Output:

```text
TEST-CHECKLIST.md
TEST-SCENARIOS.md
TEST-EXECUTION.md
```

---

## Package Generation Rules

Generate test scenarios from:

Priority Order:

```text
WORKFLOW
    ↓
DOMAIN RULES
    ↓
ARCHITECTURE
```

Every workflow capability should have at least one test case.

Critical workflows should have:

- happy path tests;
- validation tests;
- error handling tests.

---

## Test Case Naming

Format:

```text
TC-<FEATURE>-NNN
```

Examples:

```text
TC-PCR-001
TC-PCR-002
TC-RI-001
```

---

## Test Scenario Structure

Each scenario must contain:

### Test Case ID

...

### Title

...

### Objective

...

### Workflow Reference

...

### Preconditions

...

### Steps

...

### Expected Result

...

---

## Initial Checklist Structure

Generate:

```text
Progress:
0 / N Completed

Completion:
0%
```

For every test case:

```text
[NOT STARTED] TC-XXX-001
```

---

## Initial Execution Structure

Generate:

```text
Current Status:
NOT STARTED

Execution History:
None
```

for every test case.

---

# MODE B — UPDATE TEST EXECUTION

Purpose:

Record test execution results.

Input:

```text
Tester Result
```

Examples:

```text
TC-PCR-001

RESULT:
PASS

NOTES:
Behavior matches expectation.
```

or

```text
TC-PCR-002

RESULT:
FAIL

ISSUE:
Duplicate Product Code accepted.
```

or

```text
TC-PCR-002

RESULT:
PASS

FIX:
Added duplicate validation.

NOTES:
Retest successful.
```

---

# EXECUTION UPDATE PROCESS

## Step 1

Identify test case.

---

## Step 2

Generate execution timestamp.

Format:

```text
YYYY-MM-DD HH:mm:ss
```

Example:

```text
2026-09-05 10:15:32
```

Timestamp must be generated automatically.

The tester must not supply timestamps.

---

## Step 3

Append execution history.

History must never be overwritten.

History must never be deleted.

History must remain chronological.

---

## Step 4

Update current test status.

---

## Step 5

Update testing progress.

---

## Step 6

Update summary statistics.

---

## Step 7

Attempt git traceability.

---

# TEST STATUS VALUES

Allowed values:

```text
NOT STARTED
IN PROGRESS
PASSED
FAILED
BLOCKED
```

No other status values allowed.

---

# CHECKLIST MAINTENANCE

The Testing Skill must automatically update:

## Progress

Example:

```text
Progress:
7 / 10 Completed
```

---

## Completion Percentage

Example:

```text
Completion:
70%
```

---

## Summary Statistics

Example:

```text
Total Test Cases:
10

Passed:
7

Failed:
1

Blocked:
0

Not Started:
2
```

---

## Test Case Status

Example:

```text
[PASSED] TC-PCR-001

[FAILED] TC-PCR-002

[NOT STARTED] TC-PCR-003
```

---

# EXECUTION HISTORY STRUCTURE

For each execution:

```text
Run #N

Timestamp:
YYYY-MM-DD HH:mm:ss

Result:
PASS | FAIL | BLOCKED

Issue:
...

Fix:
...

Notes:
...
```

Only relevant sections should be included.

---

# DEFECT HISTORY RULE

When:

```text
RESULT:
FAIL
```

the execution must create a defect record.

---

## Defect Record Structure

```text
DEFECT-ID:
DEF-PCR-001

Source Test:
TC-PCR-002

Detected:
YYYY-MM-DD HH:mm:ss

Description:
...

Status:
OPEN
```

---

# FIX RECORD RULE

When tester reports:

```text
FIX:
...
```

the skill must record:

```text
Resolution:
...

Resolved:
YYYY-MM-DD HH:mm:ss
```

and update:

```text
Status:
RESOLVED
```

---

# RETEST RULE

Every new execution creates a new run.

Do not overwrite previous runs.

Example:

```text
Run #1
FAIL

Run #2
PASS
```

History must remain intact.

---

# GIT COMMIT TRACEABILITY

When a tester reports a fix:

```text
FIX:
...
```

the Testing Skill should attempt to identify related git commits.

---

## Git Detection Strategy

Search recent commits between:

```text
Previous Execution Timestamp
Current Execution Timestamp
```

Look for:

- modified files;
- commit messages;
- commit timestamps.

---

## If Matching Commit Found

Record:

```text
Git Commit:

a8f34de
```

or

```text
Git Commits:

a8f34de
91bb28f
```

---

## If No Commit Found

Record:

```text
Git Commit:

Not Detected
```

This is not an error.

Testing must continue.

---

# TEST EXECUTION DASHBOARD

At top of TEST-EXECUTION.md maintain:

```text
Total Test Cases:
...

Passed:
...

Failed:
...

Blocked:
...

Not Started:
...

Defects Found:
...

Defects Resolved:
...

Outstanding Defects:
...
```

Update automatically after every execution.

---

# TESTER INTERACTION FORMAT

Tester should report results using structured format.

---

## Pass Example

```text
TC-PCR-001

RESULT:
PASS

NOTES:
Behavior matches expectation.
```

---

## Fail Example

```text
TC-PCR-002

RESULT:
FAIL

ISSUE:
Duplicate Product Code accepted.
```

---

## Fix Example

```text
TC-PCR-002

RESULT:
PASS

FIX:
Added duplicate validation.

NOTES:
Retest successful.
```

---

## Blocked Example

```text
TC-PCR-005

RESULT:
BLOCKED

REASON:
External service unavailable.
```

---

# FORBIDDEN

Do not:

- modify TEST-SCENARIOS.md;
- delete execution history;
- overwrite previous runs;
- manually edit progress;
- manually edit statistics;
- remove defect history;
- remove fix history.

All changes must be additive.

---

# SUCCESS CRITERIA

Testing is successful when:

✓ every workflow capability is testable

✓ testers follow predefined scenarios

✓ testing progress is tracked automatically

✓ execution history is preserved

✓ defect history is preserved

✓ fix history is preserved

✓ retest history is preserved

✓ timestamps are recorded automatically

✓ git commit traceability is maintained when available

✓ no manual artifact maintenance is required

✓ testing status is visible at any time

✓ pilot deployment readiness can be evaluated objectively

✓ testing remains deterministic and repeatable