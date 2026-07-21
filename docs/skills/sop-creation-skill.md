# SOP CREATION SKILL

# PURPOSE

Generate `SOP.md` for a business capability.

`SOP.md` is the canonical Operational Specification.

Its purpose is to describe how an operator performs a business capability using the application.

It must make the responsible actor for every part of the procedure unambiguous. An actor may be a human role, the application, or a named system/subsystem.

The document answers:

> How does an operator perform this task?

It does NOT answer:

- What the business is.
- How the software is implemented.

---

# DESIGN PHILOSOPHY

SOP.md describes operational procedures.

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

SOP.md exclusively owns:

- Purpose
- Actors and Responsibilities
- Preconditions
- Operational Steps
- Operational Exceptions
- Completion Criteria
- References

These topics must not be duplicated in DOMAIN.md or ARCHITECT.md.

---

# REQUIRED STRUCTURE

Every SOP must contain the following sections.

1. Purpose

2. Actors and Responsibilities

3. Preconditions

4. Operational Steps

5. Operational Exceptions

6. Completion Criteria

7. References

Use exactly this order.

---

# PURPOSE

Briefly describe the operational objective.

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

---

# WRITING STYLE

Write in Bahasa Indonesia.

Use concise operational language.

Use actual application terminology.

Use actual menu names.

Use actual button names.

Use actual field names.

Do not invent UI terminology.

Describe only observable system behavior.

---

# RELATIONSHIP WITH OTHER ARTIFACTS

DOMAIN.md defines business truth.

ARCHITECT.md defines technical realization.

SOP.md defines operational procedures.

Never duplicate ownership across these documents.

Whenever business meaning is required, reference DOMAIN.md.

Whenever technical realization is required, reference ARCHITECT.md.

---

# QUALITY CHECKLIST

Before completing an SOP, verify:

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

If any item fails, revise the document.

---

# AI OPTIMIZATION RULE

Generate deterministic documents.

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

---

# IMPORTANT PRINCIPLE

SOP.md owns operational truth.

It explains:

"How an operator performs the task."

Nothing more.
