# DEPLOYMENT SKILL

# PURPOSE

Generate a deployment document that enables programmers, maintainers, testers, and operators to prepare, deploy, verify, and troubleshoot a feature or module.

The Deployment Skill exists to answer:

> What must be prepared before this feature can run, how should it be deployed, how can deployment success be verified, and how can deployment be rolled back if necessary?

The Deployment Skill transforms implementation knowledge into operational knowledge.

The Deployment Skill is NOT responsible for:

- implementing code;
- reviewing code;
- testing functionality;
- tracking deployment execution;
- maintaining deployment progress;
- generating deployment reports.

The output of this skill is a static deployment document.

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
DEPLOYMENT DOCUMENT
    ↓
STAGING PREPARATION
    ↓
TESTING
    ↓
PILOT DEPLOYMENT
    ↓
PRODUCTION DEPLOYMENT
```

Deployment documentation must be generated after implementation and review are completed.

---

# PRIMARY OBJECTIVE

Enable a maintainer who did not implement the feature to:

- prepare infrastructure;
- prepare database;
- prepare configuration;
- prepare security;
- prepare integrations;
- deploy safely;
- verify deployment success;
- recover from deployment failures.

The maintainer should not need to read source code.

The maintainer should not need to consult the implementation agent.

The deployment document should contain all operational knowledge required to run the feature.

---

# DEPLOYMENT PHILOSOPHY

Deployment documentation must be:

- complete;
- deterministic;
- operational;
- reusable;
- implementation-aware;
- environment-aware.

The deployment document should remain useful across:

- staging deployments;
- pilot deployments;
- customer deployments;
- future reinstallations.

---

# REQUIRED INPUTS

Mandatory:

- ARCHITECTURE.md
- IMPLEMENTATION-PLAN.md
- IMPLEMENTATION SUMMARIES
- REVIEW REPORTS

Optional:

- DOMAIN.md
- WORKFLOW.md
- Source Code
- Database Scripts
- Configuration Files

---

# GENERATED ARTIFACT

Generate:

```text
DEPLOYMENT.md
```

Only one deployment document should be generated.

The document is static.

The document is not intended to be updated during deployment execution.

---

# GENERATION PROCESS

## Step 1 — Analyze Infrastructure Impact

Identify affected components:

```text
Backend Applications

Frontend Applications

Databases

Background Workers

Schedulers

Message Queues

External Integrations
```

---

## Step 2 — Analyze Implementation Impact

Identify:

```text
New Features

Modified Features

Breaking Changes

Backward Compatibility Requirements
```

---

## Step 3 — Analyze Operational Impact

Identify:

```text
Database Changes

Configuration Changes

Permission Changes

Integration Changes

Manual Setup Requirements
```

---

## Step 4 — Generate Deployment Instructions

Generate a complete deployment document.

Do not assume operators have access to source code.

---

# DEPLOYMENT DOCUMENT STRUCTURE

The deployment document must contain the following sections.

---

# 1. DEPLOYMENT SUMMARY

Purpose:

Provide an executive overview.

Example:

```text
Feature:
Product Code Registry

Version:
1.0

Deployment Type:
Feature Release

Risk Level:
Low
```

Include:

```text
Feature Name

Feature Purpose

Deployment Type

Risk Assessment
```

---

# 2. APPLICABILITY

Purpose:

Define where the document applies.

Example:

```text
Applicable To:

✓ New Installation

✓ Existing Installation

✓ Staging Environment

✓ Pilot Environment

✓ Production Environment
```

---

# 3. FEATURE OVERVIEW

Purpose:

Provide operational understanding.

Describe:

```text
What the feature does

Who uses it

Business purpose

Major capabilities
```

Do not repeat the entire domain document.

Provide concise operational context.

---

# 4. DEPLOYMENT PREREQUISITES

List requirements before deployment.

Examples:

```text
Database Backup Required

Application Downtime Required

Migration Script Available

Feature Flag Available

External Service Available
```

Clearly indicate:

```text
Mandatory

Optional
```

requirements.

---

# 5. INFRASTRUCTURE IMPACT

Identify affected components.

Example:

```text
Backend:
Bilreg.Api

Frontend:
Vue Application

Database:
SQL Server

Workers:
None

External Integrations:
None
```

---

# 6. DATABASE CHANGES

This is a mandatory section.

Document all database changes.

---

## New Tables

List all new tables.

---

## Modified Tables

List all modified tables.

---

## New Columns

List all new columns.

---

## New Indexes

List all new indexes.

---

## New Constraints

List all new constraints.

---

## New Views

List all new views.

---

## New Stored Procedures

List all new stored procedures.

---

## Data Migration Requirements

Describe:

```text
Backfill Activities

Data Conversion

Data Cleanup

Data Seeding
```

If none:

```text
None
```

---

# 7. CONFIGURATION CHANGES

Document all configuration requirements.

Examples:

```text
AppSettings

Environment Variables

Feature Settings

Connection Settings
```

Example:

```text
PCR.Enabled=true

PCR.MaxLength=50
```

If none:

```text
None
```

---

# 8. FEATURE FLAGS

Document:

```text
Required Feature Flags

Optional Feature Flags
```

Example:

```text
PCR.Enabled
```

If none:

```text
None
```

---

# 9. SECURITY CHANGES

Document:

```text
Permissions

Claims

Policies

Roles
```

Examples:

```text
PCR.View

PCR.Maintain
```

If none:

```text
None
```

---

# 10. INTEGRATION CHANGES

Document:

```text
New APIs

Modified APIs

External Services

Queues

File Shares
```

Example:

```text
POST /api/product-code
```

If none:

```text
None
```

---

# 11. BACKGROUND PROCESS CHANGES

Document:

```text
Workers

Schedulers

Jobs

Timers
```

Example:

```text
ProductCodeCleanupJob
```

If none:

```text
None
```

---

# 12. MANUAL SETUP REQUIREMENTS

Document activities that require manual intervention.

Examples:

```text
Create Printer Mapping

Register Warehouse

Create Directory Structure

Upload Template Files

Register Endpoint
```

If none:

```text
None
```

---

# 13. DEPLOYMENT PROCEDURE

Provide exact deployment sequence.

Example:

```text
1. Backup Database

2. Deploy Application Files

3. Execute Database Migration

4. Apply Configuration Changes

5. Enable Feature Flags

6. Restart Services

7. Execute Smoke Test
```

Deployment steps must be executable.

Avoid vague instructions.

---

# 14. SMOKE TEST PROCEDURE

Purpose:

Verify deployment success.

The smoke test should validate observable behavior.

Examples:

```text
Login

Open Feature Menu

Create Record

Update Record

Deactivate Record
```

Do not verify implementation details.

Verify user-visible behavior.

---

# 15. ROLLBACK PROCEDURE

Purpose:

Provide recovery guidance.

Example:

```text
1. Disable Feature Flag

2. Restore Previous Application Files

3. Restore Database Backup

4. Restart Services

5. Execute Smoke Test
```

Rollback steps must be executable.

---

# 16. KNOWN LIMITATIONS

Document:

```text
Deferred Capabilities

Temporary Restrictions

Operational Constraints
```

If none:

```text
None
```

---

# 17. OPERATIONAL NOTES

Document information useful to support teams.

Examples:

```text
Product Codes may be reactivated.

Deletion is not supported.

Uniqueness is enforced globally.
```

This section captures operational knowledge not easily visible elsewhere.

---

# TRACEABILITY RULE

Deployment requirements must be traceable.

```text
Architecture
    ↓
Implementation
    ↓
Deployment Requirement
```

Every deployment instruction should be supported by implementation impact.

---

# INFORMATION QUALITY RULE

If information cannot be determined from artifacts:

Write:

```text
Not Identified
```

Do not guess.

Do not invent:

- tables;
- configurations;
- permissions;
- integrations;
- infrastructure.

---

# SUCCESS CRITERIA

Deployment documentation is successful when:

✓ infrastructure requirements are identified

✓ database requirements are identified

✓ configuration requirements are identified

✓ security requirements are identified

✓ integration requirements are identified

✓ manual setup requirements are identified

✓ deployment steps are executable

✓ smoke tests are defined

✓ rollback procedures are defined

✓ operational knowledge is preserved

✓ deployment can be performed by someone who did not implement the feature

✓ deployment knowledge is no longer trapped inside implementation artifacts

✓ the document is reusable across multiple customer deployments