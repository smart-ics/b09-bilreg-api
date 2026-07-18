# ARCHITECTURE CREATION SKILL

# PURPOSE

Generate `ARCHITECT.md` for a business feature.

`ARCHITECT.md` is the canonical Technical Specification.

Its purpose is to describe how the software realizes the business defined in `DOMAIN.md`.

The document answers:

> How does the software realize the business?

It does NOT answer:

- What the business is.
- How operators perform their work.

---

# DESIGN PHILOSOPHY

ARCHITECT.md describes software realization.

It translates business concepts into application structure while preserving the business meaning defined in DOMAIN.md.

It owns:

- software architecture
- application boundaries
- use cases
- module organization
- technical decisions
- integrations

It does NOT describe:

- business definitions
- business policies
- operational procedures
- menu navigation
- user interactions

The document should remain valid even if operational procedures change.

---

# OWNERSHIP

ARCHITECT.md exclusively owns:

- Architecture Overview
- Module Boundaries
- Use Cases
- Aggregate Realization
- Repository Strategy
- API Philosophy
- Integration
- Authentication & Authorization
- Infrastructure
- Technical Decisions (ADRs)

These topics must not be duplicated in DOMAIN.md or SOP.md.

---

# REQUIRED STRUCTURE

Every ARCHITECT.md must contain the following sections.

1. Architecture Overview

2. Module Boundaries

3. Use Cases

4. Aggregate Realization

5. Repository Strategy

6. API Philosophy

7. Integration

8. Authentication & Authorization

9. Infrastructure

10. Architectural Decisions (ADRs)

Use exactly this order.

---

# ARCHITECTURE OVERVIEW

Describe the overall architecture.

Examples:

- Clean Architecture
- Vertical Slice
- Layered Architecture
- Modular Monolith

Describe:

- architectural style
- dependency direction
- project organization

Avoid implementation details of individual features.

---

# MODULE BOUNDARIES

Describe how the feature is organized.

Identify:

- modules
- responsibilities
- dependencies

Modules should represent logical boundaries.

Avoid folder structures unless architecturally significant.

---

# USE CASES

Describe the application use cases.

A use case represents one application interaction that realizes a business capability.

Each use case should include:

- name
- purpose
- primary aggregate
- primary outcome

Examples:

- Register Tenant
- Activate Subscription
- Submit Signing Request

Do not describe implementation flow.

Do not describe UI interaction.

---

# AGGREGATE REALIZATION

Describe how aggregates are realized in software.

Include:

- Aggregate Root
- child entities
- aggregate responsibilities
- consistency boundaries

Do not redefine business meaning already described in DOMAIN.md.

Focus on software realization.

---

# REPOSITORY STRATEGY

Describe the repository philosophy.

Examples:

- one repository per aggregate
- aggregate reconstruction
- explicit persistence mapping

Repository responsibilities should be described conceptually.

Avoid SQL.

Avoid implementation code.

---

# API PHILOSOPHY

Describe the application interface philosophy.

Examples:

- REST
- CQRS
- Command/Query separation
- endpoint conventions
- versioning strategy

Avoid documenting every endpoint.

Avoid payload specification.

---

# INTEGRATION

Describe external system integration.

Examples:

- HIS
- Payment Gateway
- Email Service
- Digital Signature Provider

For each integration describe:

- purpose
- communication style
- ownership
- synchronization strategy

Avoid implementation details.

---

# AUTHENTICATION & AUTHORIZATION

Describe the security architecture.

Include:

- authentication mechanism
- authorization philosophy
- identity provider
- permission model

Avoid implementation code.

Avoid token format details.

---

# INFRASTRUCTURE

Describe infrastructure dependencies.

Examples:

- database
- message broker
- storage
- cache
- background workers

Describe architectural responsibilities rather than deployment procedures.

---

# ARCHITECTURAL DECISIONS (ADRs)

Record important architectural decisions.

Each ADR should contain:

- Decision
- Rationale
- Consequence

Only include decisions that significantly affect the architecture.

Avoid historical discussion.

---

# WRITING STYLE

Write from the software architect's perspective.

Use precise technical language.

Be concise.

Prefer architectural principles over implementation details.

Avoid unnecessary theory.

---

# RELATIONSHIP WITH OTHER ARTIFACTS

DOMAIN.md defines business truth.

ARCHITECT.md defines technical realization.

SOP.md defines operational procedures.

Never duplicate ownership across these documents.

Reference DOMAIN.md when explaining business intent.

Reference SOP.md only when operational procedures are relevant.

---

# QUALITY CHECKLIST

Before completing ARCHITECT.md, verify:

✓ Architecture style is clearly defined.

✓ Module boundaries are explicit.

✓ Use cases cover every business capability.

✓ Aggregate realization is consistent with DOMAIN.md.

✓ Repository strategy is defined.

✓ API philosophy is documented.

✓ Integration architecture is complete.

✓ Authentication and authorization are defined.

✓ Infrastructure dependencies are identified.

✓ Important architectural decisions are recorded.

✓ No business rules are duplicated from DOMAIN.md.

✓ No operational procedures are duplicated from SOP.md.

✓ No UI navigation is described.

If any item fails, revise the document.

---

# AI OPTIMIZATION RULE

Generate deterministic documents.

Prioritize:

- clarity
- consistency
- explicit ownership
- low ambiguity
- maintainability
- low token usage

Avoid:

- speculative architecture
- duplicated concepts
- business leakage
- operational leakage
- unnecessary abstraction

Consistency is more important than creativity.

---

# IMPORTANT PRINCIPLE

ARCHITECT.md owns technical truth.

It explains:

"How the software realizes the business."

Nothing more.
