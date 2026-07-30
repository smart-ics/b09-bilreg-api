# SOP CREATION SKILL

# PURPOSE

Generate a paired English and Bahasa Indonesia operational specification for a business capability:

- English: `SOP.md` or a context-specific equivalent.
- Bahasa Indonesia: `SOP-ID.md` or the matching context-specific equivalent.

The English SOP is the canonical, AI-agent-facing Operational Specification. The Indonesian SOP is its human-facing semantic companion.

Its purpose is to describe how an operator performs a business capability using the application.

It must make the responsible actor for every part of the procedure unambiguous. An actor may be a human role, the application, or a named system/subsystem.

The document answers:

> How does an operator perform this task?

It does NOT answer:

- What the business is.
- How the software is implemented.

This skill supports creating a bilingual pair from scratch, translating an existing English SOP, and synchronizing an existing pair.

Unless the request explicitly limits the output to one language, produce and maintain both versions.

---

# AUDIENCE AND LANGUAGE CONTRACT

## English version

The primary audience is an AI Agent that uses the SOP for analysis, implementation planning, testing, and operational verification.

The English version must optimize for:

- deterministic interpretation
- explicit actor ownership
- concise and sequential instructions
- exact application terminology
- low ambiguity
- efficient use as prompt context

## Indonesian version

The primary audience is an operator, supervisor, product owner, analyst, domain expert, or developer.

The Indonesian version must optimize for:

- natural and understandable Bahasa Indonesia
- practical use during operation, training, and review
- semantic parity with the English version
- preservation of exact application and domain terminology

Do not translate menu names, button labels, field labels, status values, application names, module names, role names, or other visible UI text unless the application itself provides an official Indonesian label. Preserve established English domain terms when translating them would make the procedure less recognizable or disconnect it from the application.

The Indonesian version is not an independent reinterpretation. It must not add, remove, reorder, weaken, or strengthen any operational instruction relative to the English version.

---

# OUTPUT MODES

## Mode A — Create bilingual documents from scratch

1. Discover and validate the procedure, actors, UI terminology, exceptions, and observable outcomes.
2. Create the complete English SOP.
3. Create the Indonesian companion from the completed English SOP.
4. Verify structural and semantic parity between both documents.

Define the operational procedure once. Do not independently invent two procedures.

## Mode B — Translate an existing English SOP

1. Treat the existing English SOP as the semantic source.
2. Preserve its section order, actors, preconditions, step order, UI labels, exceptions, completion criteria, and references.
3. Create or update the Indonesian companion.
4. Report ambiguity, missing actor ownership, or other defects found in the English source; do not silently repair or reinterpret them in only one language.

When an obvious formatting defect can be corrected without changing meaning, keep the fix scoped and ensure both versions remain aligned.

## Mode C — Synchronize an existing pair

When either SOP changes, identify the operational delta and update its companion. Preserve carefully chosen Indonesian terminology and exact UI labels.

---

# FILE NAMING AND PAIRING

Use the repository's established bounded-context naming convention.

Examples:

| English | Bahasa Indonesia |
|---|---|
| `SOP.md` | `SOP-ID.md` |
| `CPOE-SOP.md` | `CPOE-SOP-ID.md` |
| `SOP-TR-01-Open-Tata-Rekening.md` | `SOP-TR-01-Open-Tata-Rekening-ID.md` |

For filenames containing spaces or punctuation, insert `-ID` immediately before `.md` unless the bounded context already has a different companion convention.

The `-ID` suffix identifies the Bahasa Indonesia companion; it does not define a separate procedure or operational authority.

Add both paths to the documentation index when the repository maintains one. Use relative links between paired documents when appropriate.

---

# DESIGN PHILOSOPHY

The paired SOP documents describe the same operational procedure.

It translates business capabilities into repeatable operational steps for end users.

It owns:

- actors and their operational responsibilities
- operational procedures
- operator actions
- application usage
- operational exceptions
- completion criteria

It does NOT describe:

- business concepts
- business rules
- business workflows
- domain objects
- software architecture
- APIs
- databases
- source code

The document should remain valid even if the software architecture changes.

---

# OWNERSHIP

The SOP-document pair exclusively owns:

- Purpose
- Actors and Responsibilities
- Preconditions
- Operational Steps
- Operational Exceptions
- Completion Criteria
- References

These topics must not be duplicated in DOMAIN.md or ARCHITECT.md. Their appearance in both language versions is translation and synchronization, not duplicated artifact ownership.

---

# REQUIRED STRUCTURE

Every English and Indonesian SOP must contain the following sections in the same order.

1. Purpose

2. Actors and Responsibilities

3. Preconditions

4. Operational Steps

5. Operational Exceptions

6. Completion Criteria

7. References

Use exactly this order.

Recommended Indonesian section headings:

| No. | English document | Indonesian document |
|---|---|---|
| 1 | Purpose | Tujuan |
| 2 | Actors and Responsibilities | Aktor dan Tanggung Jawab |
| 3 | Preconditions | Prasyarat |
| 4 | Operational Steps | Langkah Operasional |
| 5 | Operational Exceptions | Pengecualian Operasional |
| 6 | Completion Criteria | Kriteria Penyelesaian |
| 7 | References | Referensi |

These heading translations are defaults. Section numbering and order must remain identical across both versions.

---

# PURPOSE

Briefly describe the operational objective.

Both versions must express the same objective and scope.

Do not explain business motivation.

Do not explain business rules.

Those belong to DOMAIN.md.

---

# ACTORS AND RESPONSIBILITIES

Name every actor that participates in the procedure and state its operational responsibility.

An actor can be:

- a human role, such as Registration Officer, Cashier, Nurse, or Supervisor
- the application as a whole
- an identified system or subsystem, such as Billing Service, Payment Gateway, or EMR

Use the role or system name, not an unnamed generic term such as "user" or "system", unless that is the actual product terminology.

For each actor, describe only what it is responsible for in this SOP. Do not describe its implementation or business authority beyond the procedure.

Example:

| Actor | Type | Operational responsibility |
|---|---|---|
| Registration Officer | Human | Enters and submits the registration data. |
| Registration Application | System | Validates the submitted data and displays the registration result. |
| Patient Master Service | Subsystem | Provides the matched patient identity when requested by the application. |

Every actor named in Operational Steps or Operational Exceptions must appear in this section. Do not list actors that have no role in the procedure.

Use the same actor identities and types in both language versions. In the Indonesian version, translate the responsibility description naturally, but preserve an actor name when it is an established role, application, system, subsystem, or Ubiquitous Language term.

---

# PRECONDITIONS

Describe everything that must already be true before the operator begins.

Examples:

- Operator has logged in.
- User has appropriate permission.
- Tenant already exists.
- Subscription is active.

Do not explain why these conditions exist.

Do not redefine business rules.

Both versions must contain the same preconditions in the same order. Translation must not turn a required condition into optional guidance.

---

# OPERATIONAL STEPS

This is the primary section.

Describe exactly how the operator performs the task.

Each step should identify:

- actor
- application or module
- menu
- action
- data entered
- system response

State the actor explicitly in every step, including automated steps. A human actor is responsible for an action; an application, system, or subsystem is responsible for its observable response or automated action. Do not imply responsibility through context alone.

Example:

1. **Tenant Administrator** logs in to PenaEl Portal.

2. **Tenant Administrator** opens **Tenant → Signer Management**.

3. **Tenant Administrator** clicks **Add Signer**.

4. **Tenant Administrator** completes all mandatory fields.

5. **Tenant Administrator** clicks **Save**.

6. **PenaEl Portal** validates the data and displays the confirmation message; **Tenant Administrator** verifies the message.

Focus on application usage.

Both versions must contain the same numbered steps, actors, actions, data, order, and observable system responses. Preserve exact menu names, button labels, field labels, status values, module names, and other visible UI text in the language presented by the application. Translate the surrounding instruction, not the UI contract.

Do not describe:

- business rules
- REST APIs
- SQL
- database tables
- source code
- software architecture

---

# OPERATIONAL EXCEPTIONS

Describe operational situations that require operator action.

Examples:

- Required field is empty.
- User lacks permission.
- Network connection is unavailable.
- Duplicate identifier detected.
- External service is temporarily unavailable.

Describe:

- actor responsible for responding to the exception
- system response
- operator action

Do not explain the underlying implementation.

Both versions must describe the same exceptions, responsible actors, system responses, and recovery actions in the same order. Do not add a workaround only to one language version.

---

# COMPLETION CRITERIA

Describe how the operator confirms that the procedure has completed successfully.

Examples:

- Success notification appears.
- New record is visible.
- Status changes to Active.
- Confirmation email has been sent.

Describe observable results only.

Do not explain internal processing.

Both versions must contain the same observable completion criteria. Preserve exact displayed status values, messages, and record names when they are application terminology.

---

# REFERENCES

Reference the authoritative documents.

Examples:

Business Definitions:
DOMAIN.md

Business Rules:
DOMAIN.md

Business Workflow:
DOMAIN.md

Architecture:
ARCHITECT.md

Do not duplicate their content.

Both versions must reference the same authoritative artifacts. The Indonesian companion must link to its English SOP source, and the English SOP should link back when the repository convention permits it.

---

# WRITING STYLE

## English writing style

- Use concise, deterministic operational English.
- Prefer direct actor-action-response statements.
- Use stable terms and avoid synonyms for the same action or application element.
- Preserve exact application terminology.

## Indonesian writing style

- Use natural, concise Bahasa Indonesia rather than literal word-for-word translation.
- Preserve exact application terminology and established role or domain names.
- Prefer direct actor-action-response statements.
- Do not mix languages unnecessarily when a common Indonesian expression is clear.
- Do not translate identifiers, UI labels, status values, proper names, or official product terms merely for stylistic consistency.

## Shared terminology rules

Use actual application terminology.

Use actual menu names.

Use actual button names.

Use actual field names.

Do not invent UI terminology.

Describe only observable system behavior.

---

# RELATIONSHIP WITH OTHER ARTIFACTS

The English and Indonesian SOPs define one operational truth for different audiences.

DOMAIN.md defines business truth.

ARCHITECT.md defines technical realization.

SOP.md defines operational procedures.

Never duplicate ownership across these documents.

Whenever business meaning is required, reference DOMAIN.md.

Whenever technical realization is required, reference ARCHITECT.md.

---

# TRANSLATION AND SYNCHRONIZATION RULES

When creating or updating the Indonesian version:

1. Read the complete English SOP before translating.
2. Build a term map for actors, application elements, domain terms, and observable status values.
3. Preserve all section numbers and their order.
4. Preserve the identity and type of every actor.
5. Preserve every precondition, numbered step, exception, completion criterion, and reference.
6. Preserve step order, modality, conditions, alternatives, and responsibility.
7. Preserve exact UI labels and other application-visible terminology.
8. Translate explanatory prose by meaning, not word for word.
9. Verify that no operational instruction exists in only one version.

Do not silently resolve ambiguity by making the Indonesian version more specific than the English source. Record or correct the ambiguity in the English source first when authorized, then synchronize both versions.

When creating both versions from scratch, complete and validate the English procedure before translating it. Do not alternate between languages while the procedure is still unstable.

When established Indonesian clinical, legal, financial, or industry terminology must be researched, prefer authoritative national terminology. Do not translate exact application labels merely because an Indonesian domain equivalent exists.

---

# QUALITY CHECKLIST

Before completing an SOP pair, verify the operational content:

✓ Exactly one operational procedure.

✓ Operational objective is clear.

✓ Actors and Responsibilities names every participating human, application, system, and subsystem.

✓ Each actor has a clear operational responsibility.

✓ Preconditions are complete.

✓ Operational steps are sequential.

✓ Every operational step explicitly names the actor responsible for its action or observable response.

✓ Every actor named in Operational Steps or Operational Exceptions appears in Actors and Responsibilities.

✓ Menu navigation is correct.

✓ Button names are correct.

✓ Field names are correct.

✓ System responses are observable.

✓ Completion criteria are measurable.

✓ References point to DOMAIN.md where business understanding is required.

✓ References point to ARCHITECT.md where technical understanding is required.

✓ No business rules are described.

✓ No business workflows are described.

✓ No domain model is described.

✓ No software architecture is described.

✓ No API is described.

✓ No database is described.

Then verify bilingual quality:

✓ Both documents contain the same seven sections in the same order.

✓ Actor identities, types, and counts match.

✓ Preconditions and their order match.

✓ Numbered operational steps and their order match.

✓ Each corresponding step preserves the same actor, action, data, condition, and observable response.

✓ Operational exceptions, responsible actors, system responses, and recovery actions match.

✓ Completion criteria and references match.

✓ Menu names, button labels, field labels, status values, and other exact application terminology match.

✓ The Indonesian prose is natural and understandable to humans.

✓ No instruction, condition, responsibility, or normative strength changed during translation.

✓ Links between the language companions are valid.

If any item fails, revise the document.

---

# AI OPTIMIZATION RULE

Generate deterministic English documents and semantically faithful Indonesian companions.

Prioritize:

- clarity
- consistency
- explicit ownership
- operational completeness
- low ambiguity
- low token usage

Avoid:

- business leakage
- technical leakage
- duplicated concepts
- speculative procedures

Consistency is more important than creativity.

For AI-agent prompting, prefer the English version. Use the Indonesian version for operator use, training, human review, validation, and discussion. When human feedback changes operational truth, update the English canonical SOP first or in the same change, then synchronize the Indonesian companion.

---

# IMPORTANT PRINCIPLE

The SOP-document pair owns one operational truth.

The English version is optimized for AI Agents.

The Indonesian version is optimized for humans.

They must never define different procedures.

Both explain:

"How an operator performs the task."

Nothing more.
