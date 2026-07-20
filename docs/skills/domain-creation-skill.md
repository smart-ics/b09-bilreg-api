# DOMAIN CREATION SKILL

# PURPOSE

Generate a paired English and Bahasa Indonesia domain specification for a business feature:

- English: `DOMAIN.md` or a context-specific equivalent such as `CPOE-DOMAIN.md`.
- Bahasa Indonesia: `DOMAIN-ID.md` or the matching context-specific equivalent such as `CPOE-DOMAIN-ID.md`.

The English domain document is the canonical, AI-agent-facing Business Specification. The Indonesian document is its human-facing semantic companion.

Both documents describe the business itself, independent of software implementation and operational procedures.

The document answers:

> What is this business?

It does NOT answer:

- How the software is built.
- How the operator performs the work.

This skill supports two workflows:

1. Create both English and Indonesian domain documents from scratch.
2. Translate an existing English domain document into its Indonesian companion without changing its business meaning.

Unless the request explicitly limits the output to one language, produce and maintain both versions.

---

# AUDIENCE AND LANGUAGE CONTRACT

## English version

The primary audience is an AI Agent that uses the document for analysis, planning, and implementation.

The English version must optimize for:

- deterministic interpretation
- explicit business ownership
- stable Ubiquitous Language
- low ambiguity
- concise, structured content
- efficient use as prompt context

## Indonesian version

The primary audience is a human stakeholder, domain expert, product owner, analyst, or developer.

The Indonesian version must optimize for:

- natural and understandable Bahasa Indonesia
- readability during domain review and discussion
- semantic parity with the English version
- preservation of established English business and technical terminology

Do not force every English term into Bahasa Indonesia. Keep the English term when:

- it is standard terminology in the business, clinical, DDD, or software domain
- the Indonesian translation is uncommon or harder to understand
- translating it would obscure its relationship with code, events, states, policies, or integrations
- the project already uses the English term as part of its Ubiquitous Language

Examples of terms that may remain in English include `Clinical Order`, `Order Type`, `Aggregate Root`, `Fulfilment`, `workflow`, `lifecycle`, `domain event`, and state or event names. The applicable terms depend on the bounded context; do not preserve English mechanically when a clear and commonly used Indonesian term exists.

The Indonesian version is not an independent reinterpretation. It must not add, remove, weaken, or strengthen business meaning relative to the English version.

---

# OUTPUT MODES

## Mode A — Create bilingual documents from scratch

1. Discover and validate the business facts.
2. Establish the English Ubiquitous Language and business model.
3. Create the complete English domain document.
4. Create the Indonesian companion from the completed English document.
5. Verify structural and semantic parity between both documents.

Model business truth once. Do not independently invent two domain models.

## Mode B — Translate an existing English domain document

1. Treat the existing English document as the semantic source.
2. Preserve its section order, identifiers, rules, states, events, workflows, and boundaries.
3. Create or update the Indonesian companion.
4. Apply the Indonesian terminology rules defined by this skill.
5. Report any ambiguity or defect found in the English source; do not silently repair or reinterpret it in only one language.

When an obvious formatting defect can be corrected without changing meaning, keep the fix scoped and ensure both versions remain aligned.

## Mode C — Synchronize an existing pair

When either document changes, identify the semantic delta and update its companion. Do not replace carefully chosen Indonesian terminology with literal machine translation.

---

# FILE NAMING AND PAIRING

Use the repository's established bounded-context naming convention.

Examples:

| English | Bahasa Indonesia |
|---|---|
| `DOMAIN.md` | `DOMAIN-ID.md` |
| `CPOE-DOMAIN.md` | `CPOE-DOMAIN-ID.md` |
| `lab-domain.md` | `lab-domain-id.md` |

The `-ID` suffix identifies the Bahasa Indonesia companion; it does not indicate a separate domain or a different business authority.

Add both paths to the documentation index when the repository maintains one.

Use relative links between paired documents when appropriate.

---

# DESIGN PHILOSOPHY

The paired domain documents describe the same business knowledge.

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

Both documents should remain valid even if the application is rewritten.

---

# OWNERSHIP

The domain-document pair exclusively owns:

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

These topics must not be duplicated in ARCHITECT.md or SOP.md. Their appearance in both language versions is translation and synchronization, not duplicated artifact ownership.

---

# REQUIRED STRUCTURE

Every English and Indonesian domain document must contain the following sections in the same order.

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

Recommended Indonesian section headings:

| No. | English document | Indonesian document |
|---|---|---|
| 1 | Business Overview | Gambaran Umum Bisnis |
| 2 | Ubiquitous Language | Ubiquitous Language |
| 3 | Business Capabilities | Kapabilitas Bisnis |
| 4 | Actors & Roles | Aktor & Peran |
| 5 | Domain Objects | Domain Objects |
| 6 | Aggregates | Aggregates |
| 7 | Business Rules | Aturan Bisnis |
| 8 | State Machines & Lifecycles | State Machines & Lifecycles |
| 9 | Domain Events | Domain Events |
| 10 | Business Workflows | Workflow Bisnis |

These heading translations are defaults, not a requirement to translate standard domain terminology throughout the document body.

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

## English UL format

Use two columns:

| Term | Definition |
|---|---|
| Clinical Order | ... |

The English term is the authoritative term used by AI Agents and other technical artifacts.

## Indonesian UL format — deliberate forced-translation exception

The Ubiquitous Language table is a deliberate exception to the general rule against forced translation. It must expose both the original English term and the closest available Indonesian equivalent:

| Inggris | Indonesia | Definisi |
|---|---|---|
| Clinical Order | Instruksi Klinis | ... |

For every UL entry:

- retain the exact English term in the `Inggris` column
- provide the best available Indonesian translation in the `Indonesia` column, even when that translation is descriptive or not commonly used
- write the definition in understandable Bahasa Indonesia
- continue using the original English term in the rest of the Indonesian document when it is the standard or more recognizable terminology

The Indonesian column is a glossary aid for humans. It does not rename the canonical term, code symbol, state, event, API field, or integration contract.

Research established Indonesian clinical, legal, financial, or industry terminology where appropriate. Prefer authoritative national terminology when available. If no established equivalent exists, use a concise descriptive translation rather than inventing an unexplained abbreviation.

---

# BUSINESS CAPABILITIES

Describe the major business capabilities.

A Business Capability is an ability that the organization or domain must possess to perform a business responsibility and produce a business outcome. It describes **what the business must be able to do**, not the sequence of activities, application feature, or technical implementation. A capability may be supported by people, policy, process, and information systems.

Each capability should represent one business responsibility.

Avoid implementation details.

Example:

- Patient Registration
- Tenant Management
- Subscription Management

Not:

- CRUD Tenant
- Save Button

## Indonesian capability format — deliberate translation description

Keep the original English capability name as the subsection heading and provide an explicit Indonesian translation immediately below it:

```markdown
### 3.6 Fulfilment Coordination

**Indonesia:** Koordinasi Pelaksanaan

Melacak outcome operasional umum dari departmental fulfilment tanpa mengambil kepemilikan atas detail execution khusus.
```

This `Indonesia:` line is mandatory for every capability in the Indonesian document, even when the translated name is less common. It is a human-readable description, not a replacement for the canonical English capability name.

In the explanatory paragraph, write natural Bahasa Indonesia while retaining standard English domain terminology where translating it would be forced or ambiguous.

---

# ACTORS & ROLES

List human participants and organizational roles.

Describe:

- responsibilities
- permissions
- business involvement

Do not list software systems as actors.

In the Indonesian document, translate ordinary role descriptions naturally. Preserve role names such as `Order Authorizer`, `Receiver`, or `DPJP` when they are established Ubiquitous Language; their Indonesian equivalents remain available in the UL table.

---

# DOMAIN OBJECTS

Describe the important business entities.

Each object should include:

- purpose
- responsibility
- important relationships

Avoid class design.

Avoid database schema.

Use the same Domain Object names in both language versions. Translate the explanatory prose, not the identity of the object.

---

# AGGREGATES

Identify Aggregate Roots.

Describe:

- consistency boundary
- owned entities
- business responsibility

Avoid implementation patterns.

Avoid repository discussion.

Use the same Aggregate and Aggregate Root names in both language versions. Preserve identical consistency boundaries and ownership.

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

Business Rule identifiers must be identical across both language versions. Translate the rule statement for human readability, but preserve canonical Ubiquitous Language and normative strength.

For example, `shall`, `must`, and `must not` must not become optional guidance in translation.

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

State names and transition order must remain identical across both language versions. In the Indonesian document, translate the business meaning of each state, not the state identifier itself, unless the bounded context explicitly defines Indonesian state names as canonical.

---

# DOMAIN EVENTS

Describe meaningful business events.

Events should represent something important that happened in the business.

Good examples:

Patient Registered

Subscription Activated

Signing Completed

Avoid technical events.

Domain Event names must remain identical across both language versions because they represent stable business facts and may be referenced by AI Agents, architecture documents, tests, or integrations. Translate only their business-meaning descriptions.

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

The Indonesian document must preserve the same workflow names, steps, order, alternatives, and outcomes. Keep canonical step labels in English when they correspond to named states, events, capabilities, or domain objects; explain their meaning in Bahasa Indonesia where needed.

---

# WRITING STYLE

Write from the business perspective.

Use business terminology consistently.

Be concise.

Prefer definitions over explanations.

Avoid repetition.

Avoid implementation details.

## English writing style

- Optimize for AI retrieval and deterministic interpretation.
- Use stable terms and compact declarative statements.
- Prefer explicit constraints over implied context.
- Avoid synonyms for the same business concept.

## Indonesian writing style

- Optimize for human comprehension and domain review.
- Use natural Bahasa Indonesia rather than literal word-for-word translation.
- Preserve English terminology when it is standard, project-established, or more recognizable.
- Do not mix languages unnecessarily when a common Indonesian expression is clear.
- Do not translate identifiers, rule codes, state names, event names, or proper names merely for stylistic consistency.
- On first use, an optional form such as `Instruksi Klinis (Clinical Order)` may be used when it improves comprehension; subsequent use should prefer the canonical English term.

Forced translation is limited to designated aids in the Indonesian document:

1. The `Indonesia` column in the Ubiquitous Language table.
2. The `**Indonesia:**` description beneath every Business Capability heading.

Outside those locations, clarity and common usage take precedence over translation completeness.

---

# RELATIONSHIP WITH OTHER ARTIFACTS

The English domain document defines the canonical business truth for AI consumption. Its Indonesian companion communicates the same truth to human readers.

ARCHITECT.md realizes the business.

SOP.md describes operational procedures.

Never duplicate ownership across these documents.

When operational details are required, reference SOP.md.

When technical realization is required, reference ARCHITECT.md.

The Indonesian companion must link to its English source, and the English document should link back when the repository convention permits it.

---

# TRANSLATION AND SYNCHRONIZATION RULES

When creating or updating the Indonesian version:

1. Read the complete English source before translating.
2. Build a term map from the Ubiquitous Language table.
3. Preserve all section numbers and their order.
4. Preserve every Business Rule identifier exactly.
5. Preserve Aggregate, Domain Object, state, Domain Event, capability, actor, and workflow identity.
6. Preserve modality, conditions, exceptions, scope, and ownership.
7. Preserve tables, diagrams, transition order, and cross-references.
8. Translate explanatory prose by meaning, not word for word.
9. Apply forced Indonesian translations only in the designated UL and capability fields.
10. Verify that no business fact exists in only one version.

Do not silently resolve ambiguity by making the Indonesian version more specific than the English source. Record or correct the ambiguity in the English source first when authorized, then synchronize both versions.

When creating both versions from scratch, complete the business model in English before translating it. Do not alternate between languages while the model is still unstable.

When established Indonesian terminology must be researched, prefer authoritative domain sources such as national regulations, ministry standards, or recognized professional terminology. Record citations only when the repository's documentation style calls for them; do not turn DOMAIN.md into a research report.

---

# QUALITY CHECKLIST

Before completing the domain-document pair, verify the business content:

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

Then verify bilingual quality:

✓ Both documents contain the same 10 sections in the same order.

✓ Business Rule identifiers and counts match.

✓ Aggregate, Domain Object, state, Domain Event, capability, actor, and workflow inventories match.

✓ State transitions and workflow sequences match.

✓ The Indonesian UL table contains `Inggris`, `Indonesia`, and `Definisi` columns.

✓ Every Indonesian UL row retains the exact canonical English term and provides an Indonesian equivalent.

✓ Every Business Capability in the Indonesian version contains an `**Indonesia:**` description.

✓ Standard or commonly used English terms remain intact outside the forced-translation fields.

✓ The Indonesian prose is natural and understandable to humans.

✓ No rule, exception, boundary, or normative strength changed during translation.

✓ Links between the language companions are valid.

If any item fails, revise the document.

---

# AI OPTIMIZATION RULE

Generate deterministic English documents and semantically faithful Indonesian companions.

Prioritize:

- clarity
- consistency
- explicit ownership
- low ambiguity
- low token usage
- cross-language structural parity

Avoid:

- speculative modeling
- duplicated concepts
- implementation leakage
- operational leakage
- unnecessary theory

Consistency is more important than creativity.

For AI-agent prompting, prefer the English version. Use the Indonesian version for human review, validation, and discussion. When human feedback changes business truth, update the English canonical document first or in the same change, then synchronize the Indonesian companion.

---

# IMPORTANT PRINCIPLE

The domain-document pair owns one business truth.

The English version is optimized for AI Agents.

The Indonesian version is optimized for humans.

They must never define different businesses.

Both explain:

"What the business is."

Nothing more.
