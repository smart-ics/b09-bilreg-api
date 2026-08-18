# WORKFLOW CREATION SKILL

# PURPOSE

Generate a paired English and Bahasa Indonesia business-workflow specification derived from an established domain specification:

- English: `WORKFLOW.md` or a context-specific equivalent such as `apotek-rajal-workflow.md`.
- Bahasa Indonesia: `WORKFLOW-ID.md` or the matching context-specific equivalent such as `apotek-rajal-workflow-id.md`.

The English workflow document is the canonical, AI-agent-facing Business Workflow Specification. The Indonesian document is its human-facing semantic companion. It must read as a clear Indonesian business-workflow document, not as English terminology placed in Indonesian sentence structure.

Both documents describe how established domain responsibilities, rules, states, and events coordinate to produce a business outcome.

The document answers:

> How does the business flow from an accepted trigger to an accountable outcome under the established domain model?

It does NOT answer:

- What the business concepts and rules are.
- How an operator uses the application.
- How the software is built.
- How contexts exchange technical payloads.

Unless the request explicitly limits the output to one language, produce and maintain both versions.

---

# MANDATORY CREATION ORDER

Use this artifact order:

```text
DOMAIN
  -> WORKFLOW
      -> SOP
      -> ARCHITECTURE and other technical artifacts
```

`DOMAIN.md` is a mandatory prerequisite for `WORKFLOW.md`.

Never create a workflow specification when its authoritative domain specification does not exist.

If the domain is missing, incomplete, ambiguous, or contradictory:

1. stop workflow creation;
2. identify the exact domain gap;
3. identify the affected workflow decision or outcome;
4. request or perform a domain update only when authorized; and
5. resume workflow creation only after the English domain source is corrected and its Indonesian companion is synchronized.

Do not use workflow creation as permission to invent domain truth.

---

# REQUIRED INPUTS

Before creating or revising a workflow document, read completely:

1. the canonical English domain document;
2. its Bahasa Indonesia companion when available;
3. every external domain document that owns a referenced business fact;
4. existing workflow documents in the same context when synchronization or overlap matters; and
5. approved business discussion or source material supplied for the requested workflow.

Build a source inventory containing:

- canonical Ubiquitous Language;
- Actors & Roles;
- Business Rule identifiers;
- Aggregate and Domain Object identities;
- state names and permitted lifecycle transitions;
- Domain Event names;
- bounded-context ownership; and
- explicitly unresolved domain decisions.

The workflow may use these facts. It must not redefine them.

---

# AUDIENCE AND LANGUAGE CONTRACT

## English version

The primary audience is an AI Agent that uses the workflow for analysis, architecture, implementation planning, test-scenario derivation, and artifact traceability.

The English version must optimize for:

- deterministic sequence interpretation;
- explicit triggers, participants, decisions, and outcomes;
- exact use of canonical domain terminology;
- direct traceability to Business Rules, states, and Domain Events;
- complete alternative and exception paths;
- low ambiguity; and
- efficient use as prompt context.

## Indonesian version

The primary audience is a domain expert, product owner, trainer, analyst, developer, or other human stakeholder.

The Indonesian version must optimize for:

- natural and understandable Bahasa Indonesia;
- language that an Indonesian business reader can understand on the first reading;
- readability during business review and training preparation;
- semantic parity with the English version; and
- preservation of established English domain terminology, identifiers, states, and events.

In explanatory prose, use ordinary Indonesian first. Preserve a canonical English term when translation would disconnect the workflow from DOMAIN.md, code, events, states, or other artifacts, but explain its business meaning naturally. Do not retain English merely because it appears in the English source or domain document.

The Indonesian version is not an independent reinterpretation. It must not add, remove, reorder, weaken, or strengthen any trigger, precondition, step, decision, exception, handoff, or outcome relative to the English source.

---

# OUTPUT MODES

## Mode A — Create bilingual workflow documents from an established domain

1. Read and validate the required domain sources.
2. Build the domain source inventory.
3. Discover and validate workflow triggers, sequencing, decisions, handoffs, exceptions, and outcomes.
4. Resolve every workflow statement to an authoritative domain or external source.
5. Create the complete English workflow document.
6. Create the Indonesian companion from the completed English document.
7. Verify structural, semantic, and domain-traceability parity.
8. Inspect the workflow section in both domain documents for duplicated specific sequencing.
9. When the new workflow pair duplicates an embedded domain workflow, generalize both domain workflow sections and replace the detailed sequence with workflow inventory and links.
10. Verify that the domain pair and workflow pair no longer claim canonical ownership of the same workflow detail.

Model the workflow once in English. Do not independently invent two workflows.

## Mode B — Translate an existing English workflow document

1. Treat the English workflow as the semantic source.
2. Read the authoritative domain pair before translating.
3. Validate that the workflow still conforms to the domain.
4. Preserve workflow identifiers, names, triggers, step order, decisions, alternatives, exceptions, outcomes, rule references, state names, and Domain Event names.
5. Create or update the Indonesian companion.
6. Inspect both domain workflow sections for duplicated specific sequencing.
7. Generalize the domain workflow sections when the translated workflow pair is the canonical detailed source.
8. Report domain drift or ambiguity; do not silently repair it only in the Indonesian version.

## Mode C — Synchronize an existing workflow pair

1. Read the current domain pair and both workflow documents.
2. Identify the domain or workflow semantic delta.
3. Update the English canonical workflow first or in the same change.
4. Synchronize the Indonesian companion.
5. Synchronize the generalized workflow overview and links in both domain documents.
6. Verify that no behavior exists in only one language version and that no detailed workflow has two canonical sources.

Do not replace carefully chosen Indonesian terminology with literal machine translation.

## Mode D — Deduplicate a workflow embedded in the domain pair

Use this mode when a specific workflow document already exists but DOMAIN.md or DOMAIN-ID.md still contains the same detailed sequence.

1. Read the complete domain pair and workflow pair.
2. Compare triggers, steps, decisions, alternatives, exceptions, compensations, and outcomes.
3. Classify each embedded domain statement as domain truth, general workflow overview, specific workflow detail, or unresolved conflict.
4. Keep domain truth in its authoritative domain section.
5. Keep only a generalized workflow overview, inventory, business outcome summary, and links in the domain workflow section.
6. Keep specific sequencing only in the workflow pair.
7. Synchronize the English and Indonesian domain sections in the same change.
8. Verify that no business rule, state, event, boundary, or accountable outcome was lost during deduplication.

---

# FILE NAMING AND PAIRING

Use the repository's established context naming convention.

Examples:

| English | Bahasa Indonesia |
|---|---|
| `WORKFLOW.md` | `WORKFLOW-ID.md` |
| `CPOE-WORKFLOW.md` | `CPOE-WORKFLOW-ID.md` |
| `apotek-rajal-workflow.md` | `apotek-rajal-workflow-id.md` |
| `apotek-ranap-workflow.md` | `apotek-ranap-workflow-id.md` |

The `-ID` suffix identifies the Bahasa Indonesia companion. It does not define a different workflow or business authority.

The English document must link to its Indonesian companion. The Indonesian companion must link to its English source. Both must link to the authoritative domain document.

Add both paths to the documentation index when the repository maintains one.

---

# DESIGN PHILOSOPHY

The paired workflow documents describe one business coordination truth for different audiences.

The workflow owns:

- workflow purpose and outcome;
- entry trigger and preconditions;
- sequence of business responsibilities;
- participant responsibility within the workflow;
- decision points and branch selection;
- alternative paths;
- exception and business-compensation paths;
- cross-context business handoffs;
- exit outcomes and postconditions;
- applicable business timing references; and
- traceability to authoritative domain facts.

The workflow does NOT own:

- Ubiquitous Language definitions;
- Business Capability definitions;
- general Actor or Role definitions;
- Domain Object or Aggregate definitions;
- Business Rules;
- state definitions or permitted lifecycle transitions;
- Domain Event definitions;
- UI, menu, button, field, or operator instructions;
- software architecture;
- APIs, messages, payloads, or transport protocols;
- database schema or persistence;
- source code; or
- deployment and operational recovery procedures.

The workflow applies domain truth. It never becomes a second source of domain truth.

When a specific workflow artifact exists, it is the canonical source for that workflow's trigger-to-outcome sequencing. The domain workflow section remains authoritative only for the generalized workflow landscape and its relationship to domain truth.

---

# DOMAIN-FIRST VALIDATION GATE

Before writing the workflow, validate every requested business statement using the following classification.

| Requested statement | Required treatment |
|---|---|
| Uses an existing domain term | Use the exact canonical term. |
| Applies an existing Business Rule | Reference its exact identifier. |
| Moves an object through an existing transition | Use the exact state names and permitted transition. |
| Responds to or produces an existing Domain Event | Use the exact event name. |
| Coordinates a fact owned by another context | Reference that context's authoritative domain. |
| Introduces a new business term | Stop and add it to the domain first when authorized. |
| Introduces a new invariant or policy | Stop and add a Business Rule to the domain first when authorized. |
| Introduces a new state or transition | Stop and update the domain lifecycle first when authorized. |
| Introduces a new Domain Event | Stop and update the domain event inventory first when authorized. |
| Contradicts an existing domain statement | Stop and report the contradiction. |
| Has no authoritative source | Record it as a blocking domain gap; do not invent it. |

A workflow may introduce workflow identifiers, workflow titles, and descriptive step labels. These are navigation aids, not new domain terminology.

---

# DOMAIN WORKFLOW DEDUPLICATION AND GENERALIZATION

Every workflow-creation or synchronization task must inspect the `Business Workflows`, `Workflow Bisnis`, or equivalent section in both domain documents.

## Canonical ownership after a specific workflow document exists

Use this ownership split:

| Content | Canonical owner |
|---|---|
| Ubiquitous Language, Business Rules, Aggregates, states, lifecycles, and Domain Events | DOMAIN pair |
| General workflow landscape, workflow names, broad purpose, and links | DOMAIN workflow section |
| Trigger, preconditions, detailed sequence, decisions, branches, exceptions, compensations, handoffs, and postconditions | WORKFLOW pair |

The same detailed workflow must not remain canonical in both DOMAIN and WORKFLOW.

## Required deduplication procedure

When overlap exists:

1. identify the exact detailed workflow material in the English domain;
2. identify its semantically matching material in the Indonesian domain;
3. ensure the English workflow contains the complete authoritative sequence;
4. synchronize the Indonesian workflow companion;
5. classify every domain statement before removing or generalizing it;
6. retain domain truth in the appropriate domain section;
7. replace detailed domain sequencing with a generalized workflow overview and relative links;
8. update both domain-language versions in the same change;
9. update the documentation index when paths or ownership descriptions change; and
10. validate links and semantic coverage across all four documents.

Do not delete an embedded domain statement merely because it appears in a workflow. If it expresses an invariant, policy, state transition, event definition, or ownership boundary, keep it in DOMAIN.md or move it to the correct domain section before generalizing the workflow section.

Do not move unresolved contradictions into the workflow. Stop and resolve the canonical English domain first when authorized.

## Allowed generalized domain workflow content

Once a specific workflow artifact exists, the domain workflow section may contain only:

- a short statement explaining how the domain is applied through workflows;
- canonical workflow names;
- a one-sentence scope or business-outcome summary for each workflow;
- applicable care-setting or channel classification;
- relative links to the English and Indonesian workflow artifacts; and
- an explicit statement that detailed sequencing is owned by the linked workflow documents.

It must not retain:

- numbered detailed steps;
- trigger and precondition specifications;
- decision tables;
- alternative-flow details;
- exception or compensation sequences;
- participant-by-step handoffs;
- detailed postconditions; or
- copied Business Rule prose.

Recommended English domain pattern:

```markdown
## 10. Business Workflows

This domain is applied through the following business workflows:

| Workflow | General business outcome | Canonical workflow artifact |
|---|---|---|
| Outpatient Apotek | Resolve outpatient medication demand through handover or an accountable non-fulfillment outcome. | [Workflow](../contexts/apotek/outpatient-apotek-workflow.md) |

Detailed triggers, sequencing, decisions, exceptions, and handoffs are
owned by the referenced workflow specification. Domain rules, states,
and events remain authoritative in this document.
```

Recommended Indonesian domain pattern:

```markdown
## 10. Workflow Bisnis

Domain ini diterapkan melalui workflow bisnis berikut:

| Workflow | Outcome bisnis umum | Artifact workflow canonical |
|---|---|---|
| Outpatient Apotek | Menyelesaikan permintaan obat Rawat Jalan melalui handover atau outcome non-fulfillment yang accountable. | [Workflow](../contexts/apotek/outpatient-apotek-workflow-id.md) |

Trigger, urutan, keputusan, exception, dan handoff terperinci dimiliki
oleh spesifikasi workflow yang direferensikan. Business Rules, state,
dan Domain Events tetap authoritative dalam dokumen ini.
```

If the domain workflow section is already generalized and contains valid links, preserve it. Do not rewrite it for stylistic preference.

If no specific workflow artifact exists, detailed workflow content may remain embedded in the domain according to the domain-creation policy.

---

# OWNERSHIP BOUNDARIES

Use the following artifact boundary consistently.

| Artifact | Authoritative question |
|---|---|
| DOMAIN | What exists, who owns it, and what must always be true? |
| WORKFLOW | How do established responsibilities coordinate from trigger to business outcome? |
| SOP | How does an operator perform the workflow using the application? |
| ARCHITECTURE | How does the software realize the domain and workflow? |
| INTEGRATION | What business facts cross a bounded-context boundary and with what ownership? |

When content belongs elsewhere, reference the authoritative artifact instead of duplicating it.

---

# REQUIRED DOCUMENT STRUCTURE

Every English and Indonesian workflow document must contain the following sections in the same order.

1. Workflow Overview

2. Domain Authority and References

3. Scope and Boundaries

4. Participants and Responsibility Handoffs

5. Entry Conditions and Triggers

6. Workflow Inventory

7. Workflow Specifications

8. Cross-Context Handoffs

9. Business Timing and Service Limits

10. Traceability

Use exactly this order.

Recommended Indonesian section headings:

| No. | English document | Indonesian document |
|---|---|---|
| 1 | Workflow Overview | Gambaran Umum Workflow |
| 2 | Domain Authority and References | Authority Domain dan Referensi |
| 3 | Scope and Boundaries | Cakupan dan Batasan |
| 4 | Participants and Responsibility Handoffs | Partisipan dan Perpindahan Tanggung Jawab |
| 5 | Entry Conditions and Triggers | Kondisi Awal dan Trigger |
| 6 | Workflow Inventory | Daftar Workflow |
| 7 | Workflow Specifications | Spesifikasi Workflow |
| 8 | Cross-Context Handoffs | Handoff Lintas Context |
| 9 | Business Timing and Service Limits | Waktu Bisnis dan Batas Layanan |
| 10 | Traceability | Keterlacakan |

Section numbering and order must remain identical across both versions.

---

# SECTION GUIDANCE

## 1. Workflow Overview

Describe concisely:

- the workflow purpose;
- the business outcome;
- the applicable care setting, service channel, or business situation; and
- why separate coordination is required.

Do not redefine the domain purpose or business concepts.

## 2. Domain Authority and References

Identify:

- the canonical English domain source;
- its Indonesian companion;
- external domain authorities used by the workflow; and
- any approved source documents that constrain the flow.

Include an explicit authority statement:

```text
This workflow applies the referenced domain specifications.
It does not redefine Ubiquitous Language, Aggregate boundaries,
Business Rules, states, lifecycles, or Domain Events.
```

## 3. Scope and Boundaries

Define:

- where the workflow begins;
- where it ends;
- business situations included;
- business situations excluded; and
- ownership that remains external.

Use business boundaries, not application-module or technical-component boundaries.

## 4. Participants and Responsibility Handoffs

List only participants that act in this workflow.

For each participant, describe its workflow-specific responsibility and the fact that ends or transfers that responsibility.

| Participant | Responsibility in this workflow | Handoff condition |
|---|---|---|
| Pharmacist | Completes Telaah Resep. | Telaah Resep Completed. |
| Cashier | Establishes payment evidence. | Payment Clearance Established. |

Do not redefine the participant's general business authority. Reference DOMAIN.md for that definition.

A bounded context or external authority may appear in this section when it owns a business handoff. Do not describe a technical service as a participant merely because it implements the interaction.

## 5. Entry Conditions and Triggers

Define separately:

- the business event or fact that triggers the workflow;
- facts that must already be true;
- conditions that block entry; and
- the participant or context responsible for recognizing the trigger.

Every entry condition must be grounded in the authoritative domain or an explicitly referenced external authority.

Do not include login, permission, menu, or application-availability conditions. Those belong to SOP or operational documentation.

## 6. Workflow Inventory

List the workflows or cohesive workflow scenarios covered by the document.

| ID | Workflow | Business outcome |
|---|---|---|
| `WF-APT-RJ-001` | General Patient Fulfillment | Paid medication is handed over or receives an accountable non-fulfillment outcome. |

Workflow identifiers must:

- use `WF-<FEATURE-CODE>-<NUMBER>` or an established repository equivalent;
- remain stable across revisions;
- be identical in both language versions; and
- never be reused for a different business outcome.

The Indonesian inventory must retain the canonical English workflow name and provide an Indonesian description or translation in an additional column.

## 7. Workflow Specifications

Define every inventory entry using the same substructure.

```markdown
### WF-<CODE>-001 — <Canonical English Workflow Name>

**Indonesia:** <Indonesian workflow name>

#### Purpose

#### Trigger

#### Preconditions

#### Participants

#### Input Business Facts

#### Main Flow

#### Decision and Alternative Flows

#### Exception and Compensation Flows

#### Outcomes and Postconditions

#### Domain References

#### Domain Events
```

### Purpose

State the specific business outcome of this workflow. Do not repeat the entire domain purpose.

### Trigger

Name one authoritative business fact or event that starts the workflow.

### Preconditions

List domain conditions required before the trigger can be acted upon. Preserve normative strength.

### Participants

List only participants involved in this workflow specification. Their identities must match section 4 and the domain source.

### Input Business Facts

List authoritative facts available at workflow entry. Identify the owning context when the fact is external.

Do not define technical payloads.

### Main Flow

Write a numbered business sequence.

Every step must identify:

- the responsible participant or context;
- the business action or decision;
- the authoritative object or fact affected; and
- the observable business result or handoff.

Use exact canonical terms. Keep each step at business level.

Good:

```text
1. Cashier establishes Payment Clearance for the applicable Sales Invoice.
2. Apotek establishes Fulfillment Clearance for the allocated Dispense Order quantity.
```

Bad:

```text
1. User clicks Pay.
2. API updates the status column.
```

### Decision and Alternative Flows

Define branches that still pursue the workflow's business outcome.

For every decision, state:

- the decision owner;
- the authoritative condition;
- each permitted branch; and
- the point where the branch rejoins or exits the main flow.

Use a decision table when two or more conditions combine.

Do not create a new policy in the table. Reference the governing Business Rule.

### Exception and Compensation Flows

Describe business failures or deviations that require an accountable alternative outcome.

For each exception, state:

- the detected business condition;
- the responsible decision owner;
- the affected quantities, obligations, or responsibilities;
- the business compensation or final outcome; and
- the events or states that record the resolution.

Business compensation includes outcomes such as cancellation, return, replacement, backorder, credit, or refund when authorized by the domain.

Do not describe transaction rollback, message retry, database recovery, or infrastructure failure handling.

### Outcomes and Postconditions

List every permitted final business outcome and the facts that must be true for each outcome.

Distinguish successful completion, accountable non-completion, cancellation, and unresolved escalation when the domain permits them.

### Domain References

Reference exact Business Rule identifiers, Aggregate or Domain Object names, and lifecycle states applied by the workflow.

Do not copy full rule definitions unless a short quotation is essential for disambiguation. Prefer links and identifiers.

### Domain Events

Separate:

- events that trigger or are consumed by the workflow; and
- events produced or observed as workflow outcomes.

Use exact Domain Event names. Do not define event payloads.

## 8. Cross-Context Handoffs

Describe business-responsibility transfers involving another bounded context.

| From | Authoritative business fact | To | Resulting responsibility |
|---|---|---|---|
| Payment | Payment Clearance Established | Apotek | Evaluate applicable Fulfillment Clearance. |

For every handoff:

- identify one authoritative owner of the source fact;
- identify what the receiver may decide;
- state what the receiver must not infer when ambiguity is possible; and
- reference a dedicated integration artifact when one exists.

Do not define HTTP, messaging, serialization, payload fields, delivery guarantees, or idempotency here. Those belong to integration or architecture artifacts.

## 9. Business Timing and Service Limits

Record only timing that has business meaning, such as:

- pickup limits;
- Dose Windows;
- dispensing cutoffs;
- validity periods;
- no-show limits; and
- expected business-response periods.

Every timing value must come from DOMAIN.md, an external authoritative policy, or an explicitly cited approved source.

If the value is unresolved, identify the gap without inventing a number.

Do not include technical timeout, retry interval, polling frequency, scheduler configuration, or performance tuning.

## 10. Traceability

Provide a compact matrix mapping each workflow to authoritative sources.

| Workflow ID | Domain rules | States | Domain Events | External authority |
|---|---|---|---|---|
| `WF-APT-RJ-001` | `BR-APT-020`, `BR-APT-040` | `Issued`, `Financially Cleared` | `Payment Clearance Established` | Payment |

Also reference:

- the canonical domain pair;
- relevant external domain documents;
- related integration documents;
- related SOPs when they already exist; and
- related architecture documents when they already exist.

Traceability does not transfer authority to the workflow.

---

# WRITING STYLE

Write from the business-coordination perspective.

Use:

- canonical domain terminology;
- explicit participants and responsibility handoffs;
- stable workflow identifiers;
- numbered business steps;
- explicit conditions and outcomes;
- compact decision tables; and
- direct references to rules, states, and events.

Avoid:

- restating domain definitions;
- vague verbs such as `process`, `handle`, or `manage` without naming the action and outcome;
- implementation terminology;
- operator instructions; and
- unnecessary narrative.

## English writing style

- Optimize for AI retrieval and deterministic sequencing.
- Use concise declarative statements.
- Use exact canonical term, state, event, and rule names.
- Prefer explicit branches and outcomes over implied context.
- Avoid synonyms for the same action or business fact.

## Indonesian writing style

- Write for an Indonesian domain expert, product owner, or trainer, not for a bilingual technical reader.
- Use natural Bahasa Indonesia rather than literal word-for-word translation.
- Prefer a familiar Indonesian word over an English loanword when the meaning remains precise.
- Explain an unavoidable technical or canonical term in plain Indonesian at its first use. If the exact English term must remain for traceability, write the Indonesian meaning first followed by the exact term in parentheses, except where a required heading or table preserves the canonical identity first.
- Split long or abstract sentences when that makes the participant, action, condition, handoff, and result easier to understand.
- Describe the real-world business result before relying on an internal object, state, event, or workflow name.
- Avoid literal or unnatural constructions such as `membentuk`, `mempertahankan keterlacakan`, `outcome`, `eligible`, `authority`, `coverage`, `disposition`, or `clarification` when a clear Indonesian sentence expresses the same meaning.
- Do not use English grammar inside an Indonesian sentence.
- Preserve canonical English workflow names, identifiers, domain terms, states, events, and rule identifiers.
- Provide `**Indonesia:**` beneath every canonical workflow heading.
- Translate the explanation, not the identity of authoritative domain facts.
- Do not mix languages unnecessarily when a common Indonesian expression is clear.

### Indonesian terminology decision order

For every noun, verb, and phrase in Indonesian explanatory prose, choose wording in this order:

1. Use an established official Indonesian business, clinical, legal, financial, or industry term when one exists.
2. Otherwise use common Indonesian that is precise in the workflow context.
3. If a technical term has no natural common equivalent, explain it in Indonesian and retain the exact English term once in parentheses.
4. Preserve the English term unchanged only for identifiers, canonical workflow or domain identities, rule codes, state names, event names, proper names, or terms whose translation would change the intended meaning.

Canonical names and identifiers must remain stable, but they must not replace an understandable explanation of what participants do and what business result follows.

### Indonesian readability test

Before accepting Indonesian workflow prose, ask:

- Would the intended Indonesian stakeholder understand the flow without consulting the English document?
- Does each step state who acts, what happens in the business, and what result or handoff follows?
- Is every retained English word necessary under the terminology decision order?
- If a technical term remains, is its practical business meaning clear from the same sentence or its first-use explanation?
- Does the sentence sound natural when read aloud in Indonesian?

Revise the prose if any answer is no.

---

# RELATIONSHIP WITH OTHER ARTIFACTS

The English domain document defines canonical business truth.

The English workflow document defines canonical business coordination derived from that truth.

The Indonesian companions communicate the same meanings to human readers.

SOP describes operator execution.

ARCHITECTURE describes software realization.

INTEGRATION describes stable collaboration contracts across bounded contexts.

Never duplicate ownership across these artifacts.

When a specific workflow pair is created, update the workflow section in both domain documents during the same task whenever it contains duplicated specific sequencing. The domain section must become a generalized workflow overview and link to the workflow pair.

When workflow discovery reveals a missing business concept, rule, state, transition, or event, update DOMAIN.md first when authorized.

When operator details are required, reference or create SOP.

When technical realization is required, reference or create ARCHITECTURE or INTEGRATION as appropriate.

---

# TRANSLATION AND SYNCHRONIZATION RULES

When creating or updating the Indonesian version:

1. Read the complete English workflow source.
2. Read the English and Indonesian domain pair.
3. Build a term map from the domain Ubiquitous Language.
4. Preserve all section numbers and their order.
5. Preserve every workflow identifier and canonical English workflow name.
6. Preserve triggers, preconditions, participant identities, input facts, numbered steps, decisions, branches, exceptions, compensations, outcomes, and postconditions.
7. Preserve step order and responsibility ownership.
8. Preserve Business Rule identifiers, state names, and Domain Event names exactly.
9. Preserve handoff direction, timing conditions, and traceability references.
10. Translate explanatory prose by meaning, not word for word. Apply the Indonesian terminology decision order and readability test to every overview, responsibility, trigger, precondition, input fact, step, decision, exception, compensation, outcome, handoff, timing statement, and traceability explanation.
11. Verify that no workflow behavior exists in only one language version.
12. When the domain workflow section is generalized, preserve the same workflow inventory, general outcome summaries, authority statement, and language-appropriate links in both domain versions.

Do not silently resolve ambiguity by making the Indonesian version more specific than the English source. Correct the English canonical source first when authorized, then synchronize its companion.

When creating both versions from scratch, complete and validate the English workflow before translating it. Do not alternate between languages while the flow is still unstable.

---

# QUALITY CHECKLIST

Before completing the workflow pair, verify domain dependency:

✓ A canonical English domain document exists.

✓ The complete English domain document was read.

✓ The Indonesian domain companion was read when available.

✓ Every workflow term exists in the domain or an identified external authority.

✓ Every applied Business Rule uses its exact identifier.

✓ Every referenced state and transition is permitted by the domain lifecycle.

✓ Every referenced Domain Event exists in the domain event inventory.

✓ No unresolved domain gap was converted into an invented workflow decision.

✓ Both domain workflow sections were inspected for duplicated specific sequencing.

✓ Detailed workflow content was removed from the domain pair when the workflow pair became its canonical source.

✓ Every removed or generalized domain statement was classified so that no domain truth was lost.

✓ The generalized domain sections contain valid links to the language-appropriate workflow documents.

✓ DOMAIN and WORKFLOW do not both claim canonical ownership of the same detailed sequence.

Then verify workflow quality:

✓ Purpose and business outcome are explicit.

✓ Scope has clear entry and exit boundaries.

✓ Every participant has a workflow-specific responsibility.

✓ Trigger and preconditions are authoritative.

✓ Every workflow has a stable identifier.

✓ Main-flow steps are sequential and explicitly owned.

✓ Every decision identifies its owner, condition, branches, and outcome.

✓ Alternative paths rejoin or exit explicitly.

✓ Exceptions produce an accountable compensation, final outcome, or explicit unresolved escalation.

✓ Cross-context handoffs preserve authoritative ownership.

✓ Business timing is sourced and not invented.

✓ Traceability covers rules, states, events, and external authorities.

✓ No Ubiquitous Language, Aggregate, Business Rule, lifecycle, state, or Domain Event is redefined.

✓ No UI, menu, button, form, or operator instruction is described.

✓ No API, payload, database, class, framework, or infrastructure is described.

Then verify bilingual parity:

✓ Both documents contain the same ten sections in the same order.

✓ Workflow identifiers, counts, and canonical English names match.

✓ Participant identities and responsibility handoffs match.

✓ Triggers, preconditions, and input facts match.

✓ Numbered main-flow steps and their order match.

✓ Decision branches, alternatives, exceptions, and compensations match.

✓ Outcomes and postconditions match.

✓ Business Rule identifiers, state names, Domain Event names, timing, and references match.

✓ Every Indonesian workflow heading contains an `**Indonesia:**` description.

✓ Indonesian prose is natural and understandable to humans.

✓ The Indonesian workflow can be understood by its intended stakeholder without consulting the English version.

✓ Common Indonesian wording is used wherever it remains precise; English is retained only for a canonical identity, identifier, proper name, or genuinely necessary technical term.

✓ Each retained technical English term is either clear from context or explained in plain Indonesian at first use.

✓ No Indonesian sentence is a literal English construction or uses English terminology where a common Indonesian expression is clearer.

✓ No condition, responsibility, sequence, or normative strength changed during translation.

✓ Links among the domain pair and workflow pair are valid.

✓ Generalized workflow inventories and authority statements in the English and Indonesian domain documents are semantically aligned.

If any item fails, revise the document before delivery.

---

# AI OPTIMIZATION RULE

Generate deterministic English workflow documents and semantically faithful Indonesian companions.

Prioritize:

- domain-grounded interpretation;
- explicit sequence and responsibility;
- complete decision and exception coverage;
- stable identifiers;
- traceability;
- low ambiguity; and
- low token usage.

Avoid:

- invented domain truth;
- duplicated definitions;
- synonym drift;
- implementation leakage;
- operational leakage; and
- unnecessary theory.

For AI-agent prompting, load artifacts in this order:

```text
1. English DOMAIN
2. English WORKFLOW
3. Indonesian companions only when human terminology or review context is needed
```

When human feedback changes business truth, update the English domain first or in the same change, synchronize the Indonesian domain companion, then update the workflow pair.

When human feedback changes only coordination sequencing without changing domain truth, update the English workflow first or in the same change, then synchronize the Indonesian workflow companion.

---

# IMPORTANT PRINCIPLE

The domain-document pair owns business truth.

The workflow-document pair owns business coordination derived from that truth.

The English workflow is optimized for AI Agents.

The Indonesian workflow is optimized for humans.

They must never define different workflows.

Both explain:

> How established domain truth coordinates from trigger to accountable business outcome.

Nothing more.
