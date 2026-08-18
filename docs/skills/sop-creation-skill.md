# SOP CREATION SKILL

# PURPOSE

Generate a paired English and Bahasa Indonesia operational specification for a business capability:

- English: `SOP.md` or a context-specific equivalent.
- Bahasa Indonesia: `SOP-ID.md` or the matching context-specific equivalent.

The English SOP is the canonical, AI-agent-facing Operational Specification. The Indonesian SOP is its human-facing semantic companion. It must read as a clear Indonesian operational document, not as English terminology placed in Indonesian sentence structure.

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
- language that an Indonesian operator can understand on the first reading
- practical use during operation, training, and review
- semantic parity with the English version
- preservation of exact application and domain terminology

Do not translate menu names, button labels, field labels, status values, application names, module names, role names, or other visible UI text unless the application itself provides an official Indonesian label.

In explanatory prose, use ordinary Indonesian first. Preserve an English domain term only when it is an official visible label, an identifier, a proper name, an exact term required for traceability, or it has no clear and commonly understood Indonesian equivalent. Do not retain English merely because it appears in the English source or domain document.

The Indonesian version is not an independent reinterpretation. It must not add, remove, reorder, weaken, or strengthen any operational instruction relative to the English version.

---

# OUTPUT MODES

## Mode A — Create bilingual documents from scratch

1. Discover the authoritative domain pair and any applicable workflow pair.
2. Validate the procedure, actors, workflow alignment, UI terminology, exceptions, and observable outcomes.
3. Create the complete English SOP.
4. Create the Indonesian companion from the completed English SOP.
5. Verify source traceability and structural and semantic parity between both documents.

Define the operational procedure once. Do not independently invent two procedures.

## Mode B — Translate an existing English SOP

1. Treat the existing English SOP as the semantic source.
2. Preserve its section order, actors, preconditions, step order, UI labels, exceptions, completion criteria, and references.
3. Create or update the Indonesian companion.
4. Report ambiguity, missing actor ownership, or other defects found in the English source; do not silently repair or reinterpret them in only one language.

When an obvious formatting defect can be corrected without changing meaning, keep the fix scoped and ensure both versions remain aligned.

## Mode C — Synchronize an existing pair

When either SOP changes, identify the operational delta and update its companion. Re-read the applicable domain and workflow sources so that synchronization does not preserve an obsolete business sequence. Preserve carefully chosen Indonesian terminology and exact UI labels.

---

# SOURCE DISCOVERY AND REQUIRED INPUTS

`DOMAIN.md` or its context-specific equivalent is always required. A dedicated workflow document is conditional and must not be assumed to exist.

Before creating or revising an SOP, read completely:

1. the canonical English domain document;
2. its Bahasa Indonesia companion when available;
3. the domain's `Business Workflows`, `Workflow Bisnis`, or equivalent section;
4. every applicable canonical English workflow document that exists;
5. the applicable Indonesian workflow companion when available;
6. every external domain or workflow document that owns a referenced business fact or handoff;
7. the existing English and Indonesian SOPs when translating or synchronizing; and
8. approved application evidence for menus, screens, buttons, fields, permissions, system responses, and recovery actions.

Use repository indexes, domain links, naming conventions, and nearby context files to discover workflow artifacts. Do not decide that no workflow exists merely because the file is not named exactly `WORKFLOW.md`.

A dedicated workflow pair is optional input because it is created only when a domain contains two or more distinct business workflows. When a domain has only one workflow and no dedicated workflow artifact exists, derive workflow context from the domain's workflow section.

Apply these cases:

| Discovered state | Required SOP treatment |
|---|---|
| Applicable workflow document exists | Read it and use it as the canonical source for trigger-to-outcome coordination. |
| No workflow document exists and the domain contains one workflow | Use the domain workflow section as the coordination source. Do not require or create a workflow document merely to create the SOP. |
| Domain identifies two or more workflows but no dedicated workflow document exists | Inspect whether the domain still contains enough authoritative coordination for the requested SOP. Report the missing expected workflow artifact as a documentation gap; do not invent missing sequencing. |
| Domain delegates detailed sequencing to a workflow link, but the target is missing or unreadable | Stop SOP creation for the affected procedure and report the broken authority chain. |
| Workflow and domain contradict each other | Stop and report the exact contradiction. Domain remains authoritative for business truth; the workflow must be corrected before the SOP derives operational steps from it. |
| Workflow exists but does not cover the requested procedure | Do not force-fit it. Use another applicable workflow or the domain when authoritative coverage exists; otherwise report a source gap. |

The absence of a workflow document is not by itself a reason to fail SOP creation. Missing authoritative information is.

Build a compact source inventory containing:

- canonical domain terms, roles, Business Rule identifiers, states, and Domain Events;
- applicable workflow identifiers, names, scope, triggers, participants, main sequence, decisions, exceptions, handoffs, and outcomes;
- the mapping from the requested SOP to exactly one operational procedure within an applicable workflow or domain flow;
- approved UI terminology and observable system behavior; and
- unresolved business, workflow, or application gaps.

Do not use SOP creation as permission to create a missing workflow artifact unless the request explicitly includes workflow creation.

---

# SOURCE AUTHORITY AND DERIVATION ORDER

Use this derivation order:

```text
DOMAIN (mandatory business truth)
  -> WORKFLOW (conditional coordination truth, when present)
      -> SOP (operational execution)
```

The sources have different authority:

| Source | Authority used by the SOP |
|---|---|
| DOMAIN | Ubiquitous Language, roles, Business Rules, objects, states, lifecycles, Domain Events, and generalized workflow landscape. |
| WORKFLOW, when present | Workflow scope, trigger, preconditions, participant handoffs, detailed business sequence, decisions, alternatives, exceptions, compensations, and outcomes. |
| Approved application evidence | Menus, screens, buttons, fields, permissions, operator actions, displayed responses, and operational recovery actions. |
| SOP | The deterministic procedure that maps the authoritative business flow to observable application use. |

An SOP must preserve the applicable workflow's business order, decision conditions, responsibility handoffs, and permitted outcomes while adding operational detail. It must not copy workflow prose merely to restate business coordination, and it must not alter the workflow to match current UI behavior.

If application behavior conflicts with DOMAIN or an applicable WORKFLOW, report the conflict. Do not silently make the SOP a new source of business truth.

Before writing the procedure, classify every required statement:

| Required statement | Authoritative treatment |
|---|---|
| Defines a term, role, rule, object, state, transition, or Domain Event | Validate against DOMAIN. If missing or contradictory, report a domain gap. |
| Defines workflow scope, order, decision, branch, handoff, compensation, or outcome | Validate against the applicable WORKFLOW when present; otherwise validate against the domain workflow section. If missing, report a coordination gap. |
| Defines a menu, screen, button, field, permission, displayed response, or operator recovery action | Validate against approved application evidence. If missing, report an operational-evidence gap. |
| Merely restates authoritative business or workflow content | Replace the duplication with the minimum operational instruction and a reference. |
| Conflicts with a more authoritative source | Stop the affected procedure and report both statements and their sources. |

Do not resolve a source gap by increasing detail in the SOP.

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

It translates business capabilities and, when available, their authoritative workflows into repeatable operational steps for end users.

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

These topics must not be duplicated in DOMAIN.md, WORKFLOW.md, or ARCHITECT.md. Their appearance in both language versions is translation and synchronization, not duplicated artifact ownership.

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

When an applicable workflow exists, keep the operational objective within that workflow's scope and outcome. Reference the workflow instead of repeating its business rationale or detailed coordination.

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

Separate operational preconditions from business entry conditions:

- derive business preconditions from the applicable workflow when one exists, otherwise from the domain;
- add only operational prerequisites needed to use the application, such as authentication, permission, and application availability; and
- do not weaken, strengthen, or omit an authoritative workflow precondition.

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

When an applicable workflow exists:

- map the SOP to the relevant workflow identifier and specification;
- preserve the workflow's business sequence and responsibility handoffs;
- preserve every applicable decision, alternative path, exception outcome, and completion outcome;
- translate each applicable business step into one or more observable operator and application steps;
- do not expose business-only or cross-context steps as UI actions when the operator does not perform them; describe only the observable system response or required handoff; and
- do not add a business branch, state transition, compensation, or outcome that the workflow or domain does not authorize.

An SOP does not need a one-to-one step count with its source workflow. It does need complete traceability: every operational step must support an authoritative workflow step or an operational prerequisite, and every workflow step relevant to the procedure must be represented by an operator action, observable application behavior, or explicit handoff.

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

When a workflow exists, distinguish its business exception and compensation paths from application-level operational exceptions. Preserve the authorized business outcome, but describe only what the operator does and what the application visibly reports. Do not invent a retry, override, cancellation, refund, or other recovery path that changes the workflow outcome.

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

When a workflow exists, every completion criterion must correspond to one of its permitted outcomes or postconditions. An application success message is not sufficient when the workflow requires an additional observable handoff, state, or accountable outcome.

---

# REFERENCES

Reference the authoritative documents.

Examples:

Business Definitions:
DOMAIN.md

Business Rules:
DOMAIN.md

Business Workflow:
WORKFLOW.md (when an applicable workflow artifact exists); otherwise DOMAIN.md

Architecture:
ARCHITECT.md

Do not duplicate their content.

When a workflow document exists, reference the exact workflow artifact and applicable workflow identifier or section. Also retain the domain reference because the workflow applies rather than replaces domain truth.

When no workflow document exists, reference the exact domain workflow section used as the coordination source. Do not add a dead or placeholder `WORKFLOW.md` link.

Both versions must reference the same authoritative artifacts. The Indonesian companion must link to its English SOP source, and the English SOP should link back when the repository convention permits it.

---

# WRITING STYLE

## English writing style

- Use concise, deterministic operational English.
- Prefer direct actor-action-response statements.
- Use stable terms and avoid synonyms for the same action or application element.
- Preserve exact application terminology.

## Indonesian writing style

- Write for a busy Indonesian operator, not for a bilingual technical reader.
- Use natural, concise Bahasa Indonesia rather than literal word-for-word translation.
- Prefer a familiar Indonesian word over an English loanword when the meaning remains precise. For example: `resep`, `item obat`, `pesanan apotek`, `tagihan`, `penyiapan obat`, `stok`, `antrian`, `pasien`, and `staf apotek`.
- Explain an unavoidable technical term in plain Indonesian at its first use. If the exact English term must remain for traceability, write the Indonesian meaning first followed by the exact term in parentheses; do not repeat the English term unless needed.
- Split a long or abstract sentence into short sentences when that makes the actor, action, and result easier to understand.
- Name the real-world result before its internal document or status. For example, write “sistem membuat tugas untuk menyiapkan obat (`Dispensing`)”, not “sistem membentuk `Dispensing`”.
- Avoid literal or unnatural constructions such as “membentuk”, “mempertahankan ketertelusuran”, “disposition”, “outcome”, “eligible”, “authority”, “coverage”, or “clarification” when a clear Indonesian sentence can express the same operational meaning.
- Do not use English grammar inside an Indonesian sentence. Do not mix English and Indonesian unnecessarily.
- Preserve exact application terminology and official role names only where required by the rules above.
- Prefer direct actor-action-response statements.
- Do not translate identifiers, UI labels, status values, proper names, or official product terms merely for stylistic consistency.

### Indonesian terminology decision order

For every noun, verb, and phrase in the Indonesian SOP, choose wording in this order:

1. Use an established official Indonesian label when the application displays one.
2. Otherwise use common Indonesian that is precise in the operational context.
3. If a technical term has no natural common equivalent, explain it in Indonesian and retain the exact term once in parentheses.
4. Preserve the English term unchanged only for identifiers, visible UI text, proper names, or terms whose translation would change the intended meaning.

Do not use a glossary-style English term as a substitute for an explanation. A reader must be able to tell what to do, what the system will show, and why the next step is allowed without translating the sentence back into English.

### Indonesian readability test

Before accepting an Indonesian step, ask:

- Would a Staf Apotek understand the action without knowing the English source?
- Does the sentence say what happens in the real workflow, instead of only naming an internal object?
- Is every English word necessary under the terminology decision order?
- If a technical term remains, is its practical meaning clear from the same sentence?

Revise the step if any answer is no.

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

WORKFLOW.md, when present, defines detailed business coordination derived from the domain.

ARCHITECT.md defines technical realization.

SOP.md defines operational procedures.

Never duplicate ownership across these documents.

Whenever business meaning is required, reference DOMAIN.md.

Whenever detailed business sequencing, decisions, handoffs, exceptions, or outcomes are required and an applicable workflow exists, reference WORKFLOW.md.

When no applicable workflow artifact exists, use the domain workflow section without treating the absence as an error unless authoritative sequencing is missing.

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
8. Translate explanatory prose by meaning, not word for word. Apply the Indonesian terminology decision order and readability test to every heading, actor responsibility, step, exception, and completion criterion.
9. Verify that no operational instruction exists in only one version.

Do not silently resolve ambiguity by making the Indonesian version more specific than the English source. Record or correct the ambiguity in the English source first when authorized, then synchronize both versions.

When creating both versions from scratch, complete and validate the English procedure before translating it. Do not alternate between languages while the procedure is still unstable.

When established Indonesian clinical, legal, financial, or industry terminology must be researched, prefer authoritative national terminology. Do not translate exact application labels merely because an Indonesian domain equivalent exists.

---

# QUALITY CHECKLIST

Before completing an SOP pair, verify the operational content:

✓ The complete canonical English domain was read and its companion was read when available.

✓ The domain workflow section was inspected and applicable workflow artifacts were discovered by links, index, naming convention, and context.

✓ Every applicable English workflow was read completely and its Indonesian companion was read when available.

✓ When no workflow artifact exists, the domain contains sufficient authoritative coordination for the requested procedure.

✓ The SOP is mapped to exactly one operational procedure and, when applicable, an exact workflow identifier or section.

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

✓ References point to the applicable WORKFLOW document and workflow identifier when one exists.

✓ No dead or placeholder WORKFLOW reference is added when the workflow artifact does not exist.

✓ Operational steps preserve applicable workflow order, decisions, responsibility handoffs, exceptions, and permitted outcomes.

✓ Every applicable workflow step is represented by an operator action, observable application behavior, or explicit handoff.

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

✓ The Indonesian document can be understood by the intended operator without consulting the English version.

✓ Common Indonesian wording is used wherever it remains precise; English is retained only for an exact UI term, identifier, proper name, or genuinely necessary technical term.

✓ Each retained technical English term is either self-explanatory in context or explained in plain Indonesian at first use.

✓ No Indonesian sentence is a literal English construction or uses English terminology where a common Indonesian expression is clearer.

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

For AI-agent prompting, load authoritative artifacts in this order:

```text
1. English DOMAIN
2. Applicable English WORKFLOW, when it exists
3. English SOP
4. Indonesian companions only when human terminology or review context is needed
```

Use the Indonesian SOP for operator use, training, human review, validation, and discussion. When human feedback changes operational truth, update the English canonical SOP first or in the same change, then synchronize the Indonesian companion. When feedback changes business truth or business coordination, update DOMAIN or WORKFLOW respectively before or in the same change, then revalidate the SOP pair.

---

# IMPORTANT PRINCIPLE

The SOP-document pair owns one operational truth.

The English version is optimized for AI Agents.

The Indonesian version is optimized for humans.

They must never define different procedures.

Both explain:

"How an operator performs the task."

Nothing more.
