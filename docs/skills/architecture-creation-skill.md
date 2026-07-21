---
name: architecture-creation
description: Create or revise a feature ARCHITECTURE.md after DOMAIN.md and business SOPs are established. Use for codebase-grounded technical specifications in a Clean Architecture modular monolith, especially when AI implementation agents need explicit ownership, dependencies, use cases, consistency boundaries, integrations, constraints, and traceability without UI design or endpoint payload specifications.
---

# Architecture Creation Skill

## Purpose

Create `<FEATURE>-ARCHITECTURE.md`, the canonical technical specification for one business feature or bounded context.

The document must answer:

> How does the existing software, or an explicitly identified target design, realize the business defined in `DOMAIN.md`?

Write the specification so that an AI implementation agent can:

- locate the relevant code;
- preserve business and bounded-context ownership;
- place responsibilities in the correct Clean Architecture layer;
- identify the commands, queries, repositories, projections, and integrations to implement;
- recognize existing behavior, target behavior, gaps, and prohibited shortcuts;
- implement complete use cases incrementally without inventing missing decisions.

`ARCHITECTURE.md` is not an API contract, UI specification, SOP, implementation plan, or database design document.

## Position in the Delivery Workflow

Use this workflow:

1. Define `DOMAIN.md`.
2. Define business SOPs or operational scenarios.
3. Create `ARCHITECTURE.md` using this skill.
4. Design UI/UX from the approved business use cases and operational scenarios.
5. Define the detailed API contract from approved UI needs and application interfaces.
6. Implement and validate complete end-to-end use-case increments.

Do not interpret step 6 as adoption of Vertical Slice Architecture. It describes an incremental delivery method. The governing software structure remains Clean Architecture unless repository evidence and an explicit decision state otherwise.

## Audience

Treat AI coding agents as a first-class audience, while keeping the document readable by human architects and senior engineers.

Prefer explicit statements over implied conventions. For every material responsibility, identify:

- the owning bounded context or module;
- the responsible Clean Architecture layer;
- the authoritative aggregate, read model, or external authority;
- permitted dependencies;
- consistency and transaction expectations;
- integration direction;
- known evidence, target decision, or unresolved gap.

Avoid prose that requires the reader to infer where behavior belongs.

## Required Inputs

Inspect all available evidence before writing:

1. The feature's canonical `DOMAIN.md`.
2. The feature's business SOPs or operational scenarios.
3. Relevant neighboring-domain documents.
4. Existing architecture documents and ADRs.
5. The actual codebase, tests, project files, dependency registration, persistence mappings, migrations, and integration adapters.
6. Existing repository conventions for commands, queries, handlers, repositories, DALs, result types, authorization, transactions, audit, and testing.
7. Legacy behavior only when it remains an active compatibility constraint or migration source.

Do not generate a codebase-specific architecture from documentation alone when the codebase is available.

If a required business decision is missing or contradictory, stop and ask for clarification. Do not turn a business ambiguity into a technical assumption.

If a technical detail is not yet decided, record it as an explicit target decision, constraint, or gap. Do not present it as existing fact.

## Evidence and Truth Classification

Classify material technical statements using these meanings:

- **Existing:** verified in the current codebase or authoritative technical artifact.
- **Target:** required future design supported by business needs and architectural decisions.
- **Constraint:** an existing condition the target must preserve or deliberately migrate.
- **Gap:** required capability or mechanism that does not yet exist.
- **Out of Scope:** intentionally excluded from this feature.

The document need not prefix every sentence with a label, but it must make these categories unmistakable.

Never describe a proposed class, table, endpoint, event, or worker as already implemented without evidence.

When code and technical documentation disagree:

1. Treat `DOMAIN.md` as authoritative for business meaning and ownership.
2. Treat verified code as authoritative for current implementation state.
3. Record the mismatch as a gap, migration concern, or ADR candidate.
4. Do not silently rewrite business meaning to match legacy code.

## Governing Architecture

Assume the application is a modular monolith using Clean Architecture when that matches repository evidence.

Express the dependency direction explicitly:

```text
API / Presentation -> Application -> Domain
Infrastructure     -> Application -> Domain
```

Apply these rules:

- `Domain` owns aggregate behavior, entities, value objects, domain policies, and domain events.
- `Application` owns use cases, commands, queries, handlers, orchestration, authorization ports, repository contracts, integration ports, and transaction boundaries.
- `Infrastructure` implements persistence, DALs, external adapters, durable delivery, caches, clocks, identifiers, and other technical ports.
- `API` adapts HTTP or other transports, authentication context, request/response mapping, dependency registration, and runtime composition.
- Inner layers must not depend on API, Infrastructure, transport DTOs, database rows, or external SDK models.
- Cross-context work must use application or integration contracts, never another context's tables, DALs, or repositories as its public contract.
- Presentation may compose several contexts, but composition does not transfer business authority.

Clean Architecture does not require global technical folders such as `Commands`, `Handlers`, or `Repositories`. Code may be grouped by business capability within each layer while preserving inward dependency rules.

Do not call this organization Vertical Slice Architecture unless the repository and an explicit ADR establish it as the governing pattern.

## Artifact Ownership

### `DOMAIN.md` owns

- ubiquitous language;
- business capabilities;
- actors and business authority;
- domain objects and business aggregates;
- business rules and invariants;
- state machines and lifecycles;
- domain events;
- high-level business workflows.

### SOP owns

- operational purpose and trigger;
- participant responsibilities;
- operational sequence;
- operational exceptions;
- completion criteria and required business evidence.

### `ARCHITECTURE.md` owns

- software structure and dependency direction;
- module and bounded-context realization;
- application use cases;
- Clean Architecture layer placement;
- aggregate realization and consistency boundaries;
- repositories, projections, and persistence strategy;
- application interfaces and API philosophy;
- cross-context and external integrations;
- authorization, audit, transactions, concurrency, and idempotency;
- infrastructure responsibilities;
- target-state gaps and architectural decisions.

### Later artifacts own

- UI layout, components, interaction state, and navigation;
- endpoint inventory and exact routes;
- request and response schemas;
- field-level validation messages;
- implementation task breakdown and scheduling;
- user operation manuals.

Reference other artifacts rather than duplicating their owned content.

## Creation Procedure

### 1. Establish business coverage

Read `DOMAIN.md` and build an internal mapping of:

- business capability;
- relevant aggregate or domain object;
- business rules;
- lifecycle transitions;
- domain events;
- neighboring authority.

Use SOPs to discover required application interactions, exceptions, evidence, and hand-offs. Do not copy SOP steps into the architecture document.

### 2. Discover the current architecture

Inspect the codebase before proposing structure. Search for:

- solution and project boundaries;
- feature or bounded-context organization;
- domain model conventions;
- MediatR or equivalent application patterns;
- repository interfaces and implementations;
- Dapper, ORM, SQL, migration, and transaction conventions;
- query projections and pagination patterns;
- API controllers and transport mapping;
- authentication, contextual authorization, and audit mechanisms;
- integration adapters, outbox/inbox, retries, and background workers;
- tests demonstrating expected architectural conventions.

Name concrete files, namespaces, or types only when verified. Use them as evidence and implementation anchors, not as an exhaustive file inventory.

### 3. Identify current-to-target gaps

For every business capability, determine whether the codebase provides:

- an authoritative write model;
- the required application use cases;
- operational read models;
- cross-context contracts;
- authorization and audit;
- transactional and idempotent behavior;
- tests at the appropriate boundaries.

Record missing mechanisms as explicit gaps. Do not hide gaps behind generic statements such as "use repository pattern" or "integrate with CPOE."

### 4. Design the technical realization

Choose the smallest consistency boundary that preserves the domain rules. Separate:

- domain decisions from application orchestration;
- write models from operational projections;
- local authority from external facts;
- synchronous consistency from eventual propagation;
- business time from recording time;
- actor-initiated commands from service-originated integration facts.

Reuse proven repository conventions unless they violate the domain or a recorded ADR.

### 5. Validate implementability

Before finalizing, confirm that an implementation agent can answer:

- Which module owns this behavior?
- Which use case initiates it?
- Which aggregate or projection is involved?
- Which layer contains the decision and which layer performs I/O?
- Which dependencies may be called?
- Where is the transaction boundary?
- How are retries, duplicates, concurrency, and partial failure handled?
- Which authority supplies each external fact?
- What must be tested?
- What is existing, target, missing, or forbidden?

If the document cannot answer these questions, revise it.

## Required `ARCHITECTURE.md` Structure

Use the following sections in this exact order. Keep a section concise, but do not omit it. State `Not applicable` with a reason when necessary.

### 1. Architecture Overview

Describe:

- feature purpose as a technical capability, referencing `DOMAIN.md`;
- current and target architectural style;
- modular-monolith or deployment boundary;
- Clean Architecture dependency direction;
- major current constraints and target gaps;
- whether the feature is actor-facing, UI-agnostic, integration-facing, or a composition host.

Do not redefine the business domain.

### 2. Codebase Evidence and Constraints

Provide a compact table:

| Evidence | Verified location | Architectural implication |
|---|---|---|

Include only evidence that materially constrains the design, such as established layer projects, transaction mechanisms, repository patterns, legacy tables, integration mechanisms, or absent target modules.

Then list explicit constraints and gaps. Distinguish current facts from target design.

### 3. Module and Bounded-Context Boundaries

For each module, state:

| Module | Responsibility | Owns | Depends on | Must not own |
|---|---|---|---|---|

Use business capabilities as logical modules. Do not derive module boundaries from menus or database tables.

State cross-context authority explicitly. A host UI may invoke a neighboring context's use case without owning it.

### 4. Clean Architecture Layer Responsibilities

Map feature responsibilities to the existing projects or layers:

| Layer | Feature responsibilities | Permitted dependencies | Prohibited dependencies |
|---|---|---|---|

Identify verified project names or namespaces when available.

Explain where these concerns belong:

- domain behavior;
- application orchestration;
- repository and integration ports;
- persistence and external adapters;
- transport mapping and composition root;
- read-model queries.

### 5. Application Use Cases

Define application interactions, not screens or endpoint inventory.

Use a stable ID for each use case, such as `UC-<FEATURE>-001`.

For every use case, provide:

| ID | Use case | Kind | Purpose | Authority / initiator | Primary model | Dependencies | Transaction outcome |
|---|---|---|---|---|---|---|---|

`Kind` must distinguish at least:

- Command;
- Query;
- Integration Inbound;
- Integration Outbound;
- Background Process, when applicable.

Cover every business capability and every SOP-triggered application interaction. Several SOPs may use one use case, and one SOP may coordinate several use cases.

Do not describe clicks, forms, HTTP routes, payload fields, or handler pseudocode.

### 6. Use-Case Traceability

Provide a traceability table:

| Use case ID | Domain capabilities / rules | SOP references | Owning module | Authorization concern | Required tests |
|---|---|---|---|---|---|

Reference rule and SOP identifiers rather than reproducing their content.

Every domain capability must map to at least one use case, policy mechanism, projection, or explicit out-of-scope decision.

### 7. Aggregate and Domain Model Realization

For each aggregate root, describe:

- verified existing type or target type;
- owned child entities and value objects;
- protected invariants by domain-rule reference;
- consistency boundary;
- allowed creation and mutation entry points;
- emitted domain facts or events;
- references to other aggregates or contexts by identity;
- prohibited responsibilities.

Do not restate business definitions. Do not turn DTOs or database rows into domain models without justification.

When a use case spans multiple aggregates or contexts, keep each aggregate transactionally independent unless a verified local transaction and invariant require atomicity. Place coordination in Application.

### 8. Persistence and Repository Strategy

Describe:

- one repository per aggregate root unless evidence justifies otherwise;
- repository interface ownership in the inward layer;
- explicit reconstruction and persistence mapping;
- insert/update/history semantics;
- transaction ownership;
- optimistic concurrency or other conflict handling;
- identifier and business-time handling;
- migration and legacy compatibility boundaries;
- which operations must not access another module's tables directly.

Name relevant tables or DAL types only when verified and architecturally significant. Do not provide full schemas or SQL.

### 9. Read Models and Query Strategy

Define purpose-built projections required for operational work, history, detail, reconciliation, search, and integration recovery.

For each projection, state:

| Projection | Consumer purpose | Source authority | Freshness | Filters / scope | Must not decide |
|---|---|---|---|---|---|

Queries may use optimized DALs without reconstructing aggregates when they remain read-only, authorized, and do not decide lifecycle transitions.

Do not let a projection become a second write authority.

### 10. Application Interfaces and API Philosophy

Describe the boundary exposed to UI hosts, other modules, and external services:

- command/query separation;
- transport independence of Application;
- REST or repository-standard transport philosophy;
- deterministic result and error categories;
- idempotency requirements;
- versioning and backward compatibility;
- pagination, filtering, and history-query principles;
- composition rules for actor-facing hosts.

Do not specify exact routes, request/response payloads, or UI-specific view models. Those belong to the later API contract.

Make clear that an HTTP controller exposes an application capability; it does not become the business authority.

### 11. Integration and Cross-Context Collaboration

For each integration, provide:

| Collaborator | Direction | Purpose | Owning authority | Contract style | Consistency / delivery | Failure handling |
|---|---|---|---|---|---|---|

Distinguish:

- direct in-process application calls;
- authenticated inbound facts;
- outbound notifications or hand-offs;
- asynchronous durable delivery;
- legacy anti-corruption adapters.

Specify stable source identities and idempotency for retried facts. Do not use a generic event bus merely because `DOMAIN.md` names domain events.

### 12. Authentication, Authorization, and Audit

Describe separately:

- caller authentication;
- actor identity versus service identity;
- contextual authorization by patient, care context, organization, role, assignment, or destination;
- command authorization versus query scoping;
- accountable actor carried by service-originated facts;
- audit evidence for material decisions, corrections, and exceptional actions;
- protection of sensitive data.

Do not rely solely on hiding UI actions. Domain and Application boundaries must enforce authoritative decisions.

### 13. Transactions, Consistency, Concurrency, and Idempotency

For each material multi-step use case, state:

- transaction boundary;
- invariants protected synchronously;
- facts propagated eventually;
- duplicate-detection key;
- retry behavior;
- concurrency conflict policy;
- compensation or recovery responsibility after partial failure.

Do not claim distributed atomicity across bounded contexts unless the architecture truly provides it.

### 14. Infrastructure and Operational Concerns

Describe only relevant infrastructure responsibilities:

- database and migration ownership;
- background workers;
- durable delivery or recovery storage;
- cache use and invalidation authority;
- observability, structured logging, metrics, and correlation;
- configuration and feature flags;
- health and integration-recovery concerns.

Do not include deployment procedures or environment-specific secrets.

### 15. Implementation Guidance for AI Agents

Provide an implementation map, not a sprint plan:

| Increment | Included use cases | Required layers | External dependencies | Verification gate |
|---|---|---|---|---|

Order increments by dependency and risk. Each increment should be demonstrable end-to-end while remaining implemented through Clean Architecture layers.

For every increment, identify:

- domain tests for invariant behavior;
- application tests for orchestration and authorization;
- infrastructure tests for persistence and adapters;
- contract or integration tests at module boundaries;
- API tests only after transport contracts exist;
- regression behavior that must remain unchanged.

List explicit implementation prohibitions, such as:

- do not mutate another context's tables;
- do not put business decisions in controllers, handlers, DALs, or projections;
- do not duplicate neighboring-domain lifecycle logic;
- do not infer completion from billing or UI state;
- do not erase correction history;
- do not fabricate missing dependencies.

### 16. Architectural Decisions

Record only significant decisions using stable IDs such as `ADR-<FEATURE>-001`.

For each ADR include:

- **Decision**
- **Status:** Existing, Accepted Target, Proposed, or Superseded
- **Context**
- **Rationale**
- **Consequences**
- **Rejected alternatives**, when materially relevant
- **Evidence or governing references**

Do not use ADRs to repeat ordinary framework conventions.

### 17. Open Gaps and Deferred Decisions

List only unresolved items that block or constrain later UI, API-contract, or implementation work.

For each gap include:

| ID | Gap or decision | Why it matters | Owner / authority needed | Blocks | Safe interim position |
|---|---|---|---|---|---|

Do not silently resolve business questions. Do not use open gaps as permission for implementation agents to invent behavior.

## Writing Rules

Write in precise, imperative-friendly technical language.

Use:

- stable identifiers;
- compact tables for repeated mappings;
- explicit `must`, `must not`, `may`, and `should` statements;
- verified code references when they help an agent locate implementation anchors;
- business-rule and SOP references instead of copied business prose;
- one canonical term for each concept.

Avoid:

- generic architecture theory;
- aspirational claims without evidence;
- speculative classes, endpoints, events, tables, or folders;
- full payloads, SQL, and implementation code;
- UI components, navigation, and interaction details;
- SOP step reproduction;
- duplicate business rules;
- ambiguous words such as `handle`, `manage`, `process`, or `support` without naming the authority, action, and outcome;
- declaring every application command an independent architectural slice;
- treating technical folder structure as a business module model.

Prefer concise completeness over low token count. A shorter document is not better if it leaves an implementation agent to guess.

## Completion Checklist

Before delivering `ARCHITECTURE.md`, verify:

- [ ] `DOMAIN.md`, relevant SOPs, neighboring contexts, and the codebase were inspected.
- [ ] Business ambiguities were clarified or left explicitly unresolved.
- [ ] Existing, target, constraint, gap, and out-of-scope statements are distinguishable.
- [ ] Clean Architecture and dependency direction are explicit.
- [ ] Existing project and repository conventions are preserved or consciously superseded by ADR.
- [ ] Every business capability and relevant SOP interaction maps to application use cases.
- [ ] Every use case has an owner, kind, model, dependencies, transaction outcome, authorization concern, and required tests.
- [ ] Aggregate boundaries preserve domain rules without absorbing application orchestration.
- [ ] Repository, projection, transaction, concurrency, and idempotency strategies are explicit.
- [ ] Cross-context authority and integration direction are explicit.
- [ ] Read models cannot mutate or decide authoritative state.
- [ ] API philosophy is defined without prematurely specifying the API contract.
- [ ] UI design and SOP details are absent.
- [ ] Current code is not confused with target design.
- [ ] AI implementation increments are end-to-end use-case increments, not a claim of Vertical Slice Architecture.
- [ ] Open gaps identify what agents must not invent.
- [ ] ADRs capture significant decisions and their consequences.
- [ ] No section relies on vague conventions that an implementation agent must infer.

If any check fails, revise the document before delivery.

## Final Principle

`DOMAIN.md` defines business truth.

SOPs define operational truth.

`ARCHITECTURE.md` defines codebase-grounded technical truth.

UI/UX and API contracts are derived later from that approved foundation.

The architecture document must be precise enough for an AI agent to implement without granting it authority to invent business behavior or bypass bounded-context ownership.
