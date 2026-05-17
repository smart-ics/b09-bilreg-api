# WORKFLOW.md — Operational Workflow Engineering Standard

# 1. Core Philosophy

This system is designed as:

```text id="1jjlwm"
Workflow Orchestration System
```

and:

```text id="w8jlwm"
Human Operational Coordination System
```

The purpose of software is NOT to rigidly control human workflow.

Software should:

* guide workflow
* assist coordination
* reduce operational friction
* improve visibility
* accelerate operational execution

---

# 2. Workflow Philosophy

This system is:

```text id="pwjlwm"
Operational Workflow Driven
```

NOT:

* CRUD driven
* form driven
* entity driven

Workflow is modeled around:

* human operational movement
* hospital coordination
* queue processing
* contextual action
* operational state transition

---

# 3. Screen Philosophy

Screen is:

```text id="8jjlwm"
Queue + Contextual Workspace
```

NOT standalone CRUD form.

---

## Left Panel

Left panel represents:

```text id="h4jlwm"
Operational Work Queue
```

Purpose:

* display actionable work
* optimize operational scanning
* reduce navigation friction
* support rapid workflow execution

Examples:

* lab work queue
* pharmacy queue
* nurse task queue
* radiology queue
* billing verification queue

---

## Right Panel

Right panel represents:

```text id="zwjlwm"
Contextual Operational Workspace
```

Workspace changes dynamically based on:

* current selected work item
* current workflow state
* operational action

Examples:

* View Mode
* Collection Mode
* Result Entry Mode
* Verification Mode

---

# 4. Workflow State Philosophy

Workflow state represents:

```text id="arjlwm"
Operational Workflow State
```

NOT merely persistence status.

State determines:

* next operational action
* available UI action
* queue visibility
* operational responsibility

---

# 5. Action Philosophy

Available action MUST be generated from:

```text id="6wjlwm"
Current Workflow State
```

User SHOULD NOT manually determine workflow transition.

System guides next action automatically.

---

# 6. State Transition Matrix

Workflow MUST explicitly define:

```text id="owjlwm"
State → Allowed Action
```

Example:

| Current State | Allowed Action    |
| ------------- | ----------------- |
| Ordered       | Charge            |
| Charged       | Collect Specimen  |
| Collected     | Record Result     |
| Recorded      | Verify Result     |
| Verified      | No Further Action |

---

# 7. Workflow Transition Rule

Workflow transition SHOULD be:

* deterministic
* intention revealing
* operationally meaningful

Avoid ambiguous transition.

---

# 8. Operational Movement Philosophy

Workflow is optimized for:

```text id="vjjlwm"
Real-World Hospital Operational Movement
```

NOT merely database manipulation.

Examples:

* specimen collection round
* nurse ward movement
* doctor visit workflow
* operational batching
* queue coordination

---

## Important Principle

Human movement is part of workflow design.

Example:

Lab specimen collection is designed around:

```text id="1sjlwm"
Which patients must be visited?
Which ward should be visited?
How many tubes are required?
```

NOT:

```text id="y5jlwm"
Edit specimen form.
```

---

# 9. Queue Philosophy

Queue exists to:

```text id="zbjlwm"
Assist Operational Coordination
```

Queue is NOT rigid sequencing enforcement.

---

## Queue Supports

* workflow visibility
* operational prioritization
* human coordination
* batching
* scanning
* workload distribution

---

## Queue Override Rule

Operational staff MAY:

* reorder queue
* skip queue
* prioritize manually

System SHOULD support override WITH audit trail.

Operational flexibility is prioritized over rigid enforcement.

---

# 10. Visibility Philosophy

Primary UI focus:

```text id="nljlwm"
Current Actionable Work
```

NOT complete historical visibility.

Historical detail remains accessible contextually when required.

Avoid excessive informational noise.

---

# 11. UX Philosophy

Priority order:

```text id="wfjlwm"
1. Fast Interaction
2. Low Click Count
3. Dense Operational Information
4. Workflow Clarity
5. Visual Beauty
```

---

# 12. Operational Speed Philosophy

Operational execution speed is critical.

UI SHOULD minimize:

* unnecessary navigation
* repetitive interaction
* excessive modal flow
* deep menu hierarchy

Workflow SHOULD optimize:

* scanning speed
* action speed
* queue movement
* operational continuity

---

# 13. Contextual Workspace Philosophy

Workspace MUST adapt to workflow context.

UI mode SHOULD change dynamically based on:

* selected queue item
* workflow state
* current operational responsibility

---

## Example

Lab Order:

| State     | Workspace Mode    |
| --------- | ----------------- |
| Ordered   | View Mode         |
| Charged   | Collection Mode   |
| Collected | Result Entry Mode |
| Recorded  | Verification Mode |

---

# 14. Operational Collaboration Philosophy

Operational collaboration is more important than rigid locking.

System SHOULD support:

* multiple operational staff
* concurrent operational work
* distributed workflow execution

Avoid excessive locking strategy.

---

## Important Principle

System protects:

```text id="o1jlwm"
Workflow Consistency
```

NOT rigid UI ownership.

---

# 15. Notification Philosophy

Workflow uses:

```text id="k8jlwm"
Active Operational Notification
```

with:

```text id="vbjlwm"
Minimal Visual Disturbance
```

Notification SHOULD:

* guide attention
* highlight actionable work
* reduce missed task

Avoid:

* notification spam
* excessive popup
* visual overload

---

# 16. Dashboard Philosophy

Dashboard SHOULD prioritize:

```text id="dyjlwm"
Actionable Operational Task
```

NOT informational reporting.

Operational dashboard exists to:

* accelerate action
* identify pending work
* coordinate operational flow

---

# 17. Verification Philosophy

Verification represents:

```text id="8djlwm"
Medical / Legal Finalization
```

NOT merely administrative completion.

Verified workflow state is considered:

* operationally final
* medically finalized
* legally important

---

# 18. Amendment Philosophy

Correction after verification SHOULD create:

```text id="2mjlwm"
Amendment / Correction Flow
```

NOT destructive overwrite.

---

## Visibility Rule

UI SHOULD primarily display:

```text id="t1jlwm"
Latest Valid Result
```

Historical version MAY remain persisted based on:

* medico-legal requirement
* audit requirement
* business criticality

---

# 19. Cancellation Philosophy

Canceled work SHOULD:

```text id="3zjlwm"
Disappear From Operational Queue
```

because queue represents actionable operational work.

Historical cancellation remains accessible contextually when required.

---

# 20. Error Philosophy

Operational continuity is prioritized.

Preferred strategy:

```text id="sljlwm"
Continue Operationally When Possible
```

Examples:

* billing offline
* printer unavailable
* delayed synchronization

Workflow SHOULD degrade gracefully whenever safe.

---

# 21. Failure Handling Philosophy

When operational override occurs:

* action MUST be audited
* workflow SHOULD remain observable
* operational history SHOULD remain traceable

Avoid rigid workflow dead-end whenever possible.

---

# 22. AI Workflow Generation Rule

AI SHOULD prioritize:

```text id="afjlwm"
State Transition Clarity
+
Operational Narrative Clarity
```

AI MUST understand:

* why workflow exists
* how operational staff behave
* how hospital movement occurs
* how queue coordinates work

AI SHOULD NOT generate:

* CRUD-first workflow
* form-centric architecture
* rigid entity navigation
* over-normalized operational interaction

---

# 23. Important Principle

Hospital software exists to:

* guide operational workflow
* coordinate human activity
* reduce operational friction
* accelerate patient service

Software assists humans.

Humans remain primary operational actors.
