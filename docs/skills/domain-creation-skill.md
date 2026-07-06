# DOMAIN CREATION SKILL

# PURPOSE

Generate `DOMAIN.md` for a business feature.

`DOMAIN.md` is the canonical Business Specification.

Its purpose is to describe the business itself, independent of software implementation and operational procedures.

The document answers:

> What is this business?

It does NOT answer:

- How the software is built.
- How the operator performs the work.

---

# DESIGN PHILOSOPHY

DOMAIN.md describes business knowledge.

It owns:

- business vocabulary
- business concepts
- business policies
- business behavior
- business lifecycle

It does NOT describe:

- software architecture
- APIs
- database
- UI
- menu navigation
- buttons
- technical implementation
- operational procedures

The document should remain valid even if the application is rewritten.

---

# OWNERSHIP

DOMAIN.md exclusively owns:

- Ubiquitous Language
- Business Capabilities
- Actors & Roles
- Domain Objects
- Aggregates
- Business Rules
- State Machines
- Lifecycles
- Domain Events
- High-Level Business Workflows

These topics must not be duplicated in ARCHITECT.md or SOP.md.

---

# REQUIRED STRUCTURE

Every DOMAIN.md must contain the following sections.

1. Business Overview

2. Ubiquitous Language

3. Business Capabilities

4. Actors & Roles

5. Domain Objects

6. Aggregates

7. Business Rules

8. State Machines & Lifecycles

9. Domain Events

10. Business Workflows

Use exactly this order.

---

# BUSINESS OVERVIEW

Describe:

- business purpose
- business value
- scope
- business boundaries

Keep it concise.

Do not describe software.

---

# UBIQUITOUS LANGUAGE

Define every important business term.

Every definition should be:

- concise
- unambiguous
- business-oriented

Avoid technical terminology.

---

# BUSINESS CAPABILITIES

Describe the major business capabilities.

Each capability should represent one business responsibility.

Avoid implementation details.

Example:

- Patient Registration
- Tenant Management
- Subscription Management

Not:

- CRUD Tenant
- Save Button

---

# ACTORS & ROLES

List human participants and organizational roles.

Describe:

- responsibilities
- permissions
- business involvement

Do not list software systems as actors.

---

# DOMAIN OBJECTS

Describe the important business entities.

Each object should include:

- purpose
- responsibility
- important relationships

Avoid class design.

Avoid database schema.

---

# AGGREGATES

Identify Aggregate Roots.

Describe:

- consistency boundary
- owned entities
- business responsibility

Avoid implementation patterns.

Avoid repository discussion.

---

# BUSINESS RULES

Business Rules define business policy.

Rules must:

- be implementation independent
- be uniquely identified

Recommended format:

BR-<FeatureCode>-001

Rules describe:

- obligations
- constraints
- policies

Rules do NOT describe software validation.

---

# STATE MACHINES & LIFECYCLES

Describe the lifecycle of important business objects.

State transitions should represent business meaning.

Example:

Draft

↓

Submitted

↓

Approved

↓

Completed

Do not describe UI actions.

---

# DOMAIN EVENTS

Describe meaningful business events.

Events should represent something important that happened in the business.

Good examples:

Patient Registered

Subscription Activated

Signing Completed

Avoid technical events.

---

# BUSINESS WORKFLOWS

Describe workflows at a conceptual level.

Focus on business flow.

Example:

Tenant Registration

↓

Subscription Activation

↓

Signer Registration

↓

Tenant Ready

Do not describe:

- menus
- screens
- buttons
- clicks

Those belong to SOP.md.

---

# WRITING STYLE

Write from the business perspective.

Use business terminology consistently.

Be concise.

Prefer definitions over explanations.

Avoid repetition.

Avoid implementation details.

---

# RELATIONSHIP WITH OTHER ARTIFACTS

DOMAIN.md defines business truth.

ARCHITECT.md realizes the business.

SOP.md describes operational procedures.

Never duplicate ownership across these documents.

When operational details are required, reference SOP.md.

When technical realization is required, reference ARCHITECT.md.

---

# QUALITY CHECKLIST

Before completing DOMAIN.md, verify:

✓ Business terminology is consistent.

✓ Every capability is represented.

✓ Every important business object is defined.

✓ Aggregate boundaries are clear.

✓ Business rules are explicit.

✓ Lifecycles are complete.

✓ Domain events are identified.

✓ Workflows remain high level.

✓ No software architecture is described.

✓ No API is described.

✓ No database is described.

✓ No UI is described.

✓ No operational steps are described.

If any item fails, revise the document.

---

# AI OPTIMIZATION RULE

Generate deterministic documents.

Prioritize:

- clarity
- consistency
- explicit ownership
- low ambiguity
- low token usage

Avoid:

- speculative modeling
- duplicated concepts
- implementation leakage
- operational leakage
- unnecessary theory

Consistency is more important than creativity.

---

# IMPORTANT PRINCIPLE

DOMAIN.md owns business truth.

It explains:

"What the business is."

Nothing more.