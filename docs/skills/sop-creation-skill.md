# SOP CREATION SKILL

# PURPOSE

Generate `SOP.md` for a business capability.

`SOP.md` is the canonical Operational Specification.

Its purpose is to describe how an operator performs a business capability using the application.

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

2. Preconditions

3. Operational Steps

4. Operational Exceptions

5. Completion Criteria

6. References

Use exactly this order.

---

# PURPOSE

Briefly describe the operational objective.

Do not explain business motivation.

Do not explain business rules.

Those belong to DOMAIN.md.

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

Example:

1. Login to PenaEl Portal.

2. Open **Tenant → Signer Management**.

3. Click **Add Signer**.

4. Complete all mandatory fields.

5. Click **Save**.

6. Verify the confirmation message.

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

✓ Preconditions are complete.

✓ Operational steps are sequential.

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
