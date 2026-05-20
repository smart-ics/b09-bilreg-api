# FRONTEND_IMPLEMENTATION_PLAN.md — Laboratory Workflow Feature (LWF)

> Status: Planning only — no implementation code in this document.
> Purpose: Operational frontend architecture blueprint for Laboratory Workflow Feature (LWF).
> Scope: Vue 3 + Tailwind + shadcn-vue frontend repository.
> Primary focus: Queue-centric operational workspace architecture.  
> **Architectural locks:** `FRONTEND_AGENT_RULES.md`, `DOMAIN.md` §1A.

---

# 1. Frontend Operational Philosophy

Laboratory Workflow Feature (LWF) frontend is:

* operational workspace software,
* workflow-centric UI,
* queue-oriented operational system,
* not consumer-facing CRUD application.

The frontend must prioritize:

1. Operational clarity
2. Fast workflow execution
3. Low cognitive load
4. Keyboard efficiency
5. Workflow safety
6. Multi-workspace usability
7. Predictable workflow visibility

The frontend is designed for:

* laboratory staff,
* pathology doctors,
* administrative operators,
* operational supervisors.

The system must remain usable for:

* long operational shifts,
* high-volume queue processing,
* rapid status transitions,
* and workflow monitoring.

---

# 2. Frontend Architecture Direction

LWF frontend follows repository standards:

```text
View
 -> Composable
 -> Query
 -> Service
 -> ApiService
 -> Backend
```

Architecture priorities:

* explicit workflow orchestration,
* maintainable UI composition,
* predictable query ownership,
* AI-friendly frontend structure,
* low hidden complexity.

The frontend intentionally avoids:

* giant reactive state machines,
* speculative abstraction,
* mega generic frontend engines,
* uncontrolled global state,
* excessive watcher chains.

---

# 3. Module Structure

```text
src/modules/Laboratory/
  views/
  components/
  composables/
  queries/
  services/
  stores/
  types/
  schemas/
  configs/
```

---

# 4. Folder Responsibility

## views/

Contains:

* route-level workspace screens,
* operational workflow screens,
* queue-level orchestration.

Views coordinate:

* layout,
* tabs,
* queue ownership,
* modal visibility,
* workspace navigation.

Views must remain orchestration-focused.

---

## components/

Contains:

* workflow UI components,
* queue tables,
* result-entry UI,
* operational status visualization,
* workflow detail panels.

Component philosophy:

* reusable where stable,
* workflow-focused,
* explicit responsibility,
* operational readability first.

---

## composables/

Contains:

* workflow orchestration,
* workspace coordination,
* mutation orchestration,
* keyboard interaction orchestration,
* operational interaction flow.

Composables become primary operational workflow containers.

---

## queries/

Contains:

* TanStack Query integration,
* query ownership,
* mutation ownership,
* cache invalidation ownership.

Query contracts are architectural contracts.

---

## services/

Contains:

* API communication,
* DTO transport,
* integration handling.

No UI orchestration allowed inside services.

---

## stores/

Pinia usage intentionally minimized.

Stores are allowed ONLY for:

* workspace session management,
* opened tabs,
* active workspace,
* unsaved draft protection,
* workspace persistence.

TanStack Query remains canonical server-state.

---

## types/

Contains:

* DTO types,
* workflow enums,
* workspace typing,
* operational interaction typing.

---

## schemas/

Contains:

* Zod validation,
* API schema validation,
* mutation schema validation,
* form schema validation.

---

# 5. Workspace Philosophy

LWF uses:

```text
Hybrid Queue-Centric Operational Workspace
```

The system combines:

* operational queue,
* lightweight inline detail,
* dedicated deep workflow workspace.

The frontend intentionally avoids:

* excessive navigation,
* giant fullscreen workflow pages,
* modal-heavy workflow architecture.

---

# 6. Route Strategy

## Primary Routes

```text
/laboratory/workspace
/laboratory/verification
/laboratory/release
/laboratory/order/:orderId
```

---

## Workspace Philosophy

Main operational flow occurs inside:

```text
/laboratory/workspace
```

This workspace becomes:

* operational command center,
* queue hub,
* rapid workflow interface.

Dedicated routes are reserved for:

* complex workflows,
* detailed verification,
* amendment handling,
* advanced result review.

---

# 7. Multi-Workspace Session Architecture

Frontend supports:

```text
Multiple Simultaneous Order Workspaces
```

Users may:

* open multiple orders,
* switch between workspaces,
* preserve operational context,
* continue queue processing.

---

## Workspace Session Store

Recommended Pinia ownership:

```text
useLabWorkspaceStore
```

Responsibilities:

* opened order tabs,
* active tab,
* workspace persistence,
* unsaved-change protection,
* workspace restoration.

Pinia MUST NOT become:

* canonical server-state,
* workflow truth,
* API cache layer.

---

# 8. Main Workspace Layout

Recommended layout:

```text
+------------------------------------------------------+
| Queue Panel                                          |
|------------------------------------------------------|
| Workspace Detail Area                                |
|  - Patient Snapshot                                  |
|  - Workflow Timeline                                 |
|  - Order Detail                                      |
|  - Collection Info                                   |
|  - Result Summary                                    |
|  - Verification Status                               |
|  - Last Billing Release Check (audit trace)          |
|  - OWR Status                                        |
|------------------------------------------------------|
| Sticky Operational Action Bar                        |
+------------------------------------------------------+
```

---

# 9. Queue Architecture

Queues are primary operational navigation.

Queues must support:

* fast filtering,
* keyboard navigation,
* rapid status visibility,
* inline operational action,
* workflow prioritization.

---

# 10. Queue Categories

## Ordered Queue

Purpose:

* newly created orders,
* deferred handling,
* charge retry,
* cancellation handling.

---

## Collection Queue

Purpose:

* specimen collection,
* vacutainer preparation,
* collection monitoring.

---

## Result Entry Queue

Purpose:

* pending result entry,
* partial recording,
* abnormal result visibility.

---

## Verification Queue

Purpose:

* pathology verification,
* amendment review,
* pending medical validation.

---

## Release Queue

Purpose:

* verified orders awaiting release,
* release attempt (realtime BIL validation),
* unreleased verified result monitoring.

---

# 11. Queue UX Principles

Queues must prioritize:

* moderate density,
* readability,
* operational speed,
* low navigation friction.

The frontend intentionally avoids:

* oversized whitespace,
* consumer-style cards,
* decorative layouts.

---

# 12. Queue Columns

## Recommended Shared Columns

```text
OrderNo
PatientName
PatientId
CurrentStatus
Priority
CollectionStatus
VerificationStatus
LastBillingReleaseCheck
OWRStatus
WaitingDuration
LastUpdated
```

Additional queue-specific columns may exist.

---

# 13. Queue Refresh Strategy

Queue refresh uses:

```text
Hybrid Auto Refresh
```

Rules:

* auto refresh enabled,
* paused during active editing,
* paused during inline mutation,
* resumed after save/cancel.

Recommended interval:

* 15–30 seconds.

Manual refresh remains available.

---

# 14. Queue Interaction Strategy

Queues support:

* row selection,
* keyboard navigation,
* inline quick actions,
* workspace opening,
* multi-tab workflow.

Avoid:

* excessive row expansion,
* deep nested interaction,
* modal-driven queue workflow.

---

# 15. Keyboard Workflow Philosophy

Keyboard-centric workflow is considered:

```text
Critical Operational Requirement
```

The frontend must support:

* Tab navigation,
* Enter submit,
* Arrow navigation,
* Escape close,
* keyboard-first result entry.

---

# 16. Keyboard Navigation Areas

## Queue Navigation

Supports:

* arrow row navigation,
* enter to open workspace,
* quick action shortcuts.

---

## Result Entry

Supports:

* spreadsheet-like tab flow,
* enter-to-next-field,
* rapid numeric input,
* focus persistence.

---

## Dialogs

Supports:

* enter confirm,
* escape cancel,
* predictable focus trapping.

---

# 17. Operational Table Strategy

Repository currently has no canonical operational table component.

LWF becomes foundational operational table architecture candidate.

Recommended shared primitive:

```text
shared/components/operational-table/
```

---

# 18. Operational Table Responsibilities

Operational table should support:

* keyboard navigation,
* sticky header,
* loading state,
* row status visualization,
* server-side pagination,
* auto refresh compatibility,
* inline action slot,
* moderate-density layout.

Avoid:

* giant generic datatable frameworks,
* speculative table abstraction systems.

---

# 19. Workflow Timeline Strategy

Timeline remains:

```text
Collapsible
```

Default:

* compact summary state.

Expandable:

* full workflow history,
* timestamps,
* actor information,
* operational notes.

---

# 20. Workflow State Visualization

Workflow states must remain highly visible.

Recommended visualization:

* color-coded badges,
* timeline indicator,
* queue badges,
* sticky workspace status header.

States:

```text
Ordered
Deferred
Charged
Collected
Recorded
Verified
Released
Cancelled
Terminated
```

---

# 21. Priority & SLA Visibility

Frontend architecture must prepare for future:

```text
STAT
CITO
Routine
```

Frontend must support:

* urgency badge,
* queue sorting,
* SLA timer,
* overdue highlighting.

Even if not fully implemented in V1.

---

# 22. Result Entry Architecture

Result entry uses:

```text
Hybrid Spreadsheet + Detail Panel
```

---

## Default Mode

Primary interaction:

* grid/table style,
* keyboard optimized,
* rapid component entry.

Best for:

* high-volume operational entry.

---

## Detail Expansion

Expanded detail panel used for:

* narrative result,
* reference range detail,
* pathology notes,
* amendment review,
* abnormal explanation.

---

# 23. Result Component Visualization

Example:

```text
CBC
 ├── Hb
 ├── Leukosit
 ├── Hematokrit
```

Each component supports:

* value input,
* unit,
* reference range,
* auto-flagging,
* abnormal visibility,
* validation feedback.

---

# 24. Auto Flagging UX

Supported flags:

```text
High
Low
Normal
Critical
```

Visualization:

* compact badge,
* row highlight,
* quick abnormal visibility.

Flagging must remain visually clear without becoming visually noisy.

---

# 25. Verification Workflow UX

Verification workflow uses:

```text
Hybrid Verification Strategy
```

---

## Queue-Level Review

Allows:

* rapid inspection,
* queue triage,
* quick preview.

---

## Dedicated Verification Workspace

Used for:

* detailed review,
* pathology analysis,
* amendment comparison,
* complex abnormal result.

---

# 26. Amendment UX Strategy

Amendment is operationally sensitive.

Frontend must clearly distinguish:

* current version,
* previous versions,
* pending re-verification,
* released version.

---

## Default Visibility

Latest version visible by default.

Previous versions:

* expandable,
* immutable,
* read-only.

---

## Amendment Comparison

Recommended support:

* side-by-side comparison,
* changed component highlight,
* amendment reason visibility.

---

# 27. Billing Release Validation UX

Release eligibility is decided by **BIL at release time** — not by a separate approval screen in LWF.

The frontend must clearly separate:

```text
Workflow Status (LWF)
≠
Financial Authority (BIL)
```

---

## Release action UX

* **Release** button on **Verified** orders only.
* `PATCH release` performs realtime BIL validation.
* On **BLOCKED**: HTTP **200**, `released: false` — operational toast with `message` (not exception flow); order stays **Verified**.
* On **CLEAR**: transition to **Released**; show success confirmation.

No approve/reject financial clearance UI.

---

## Validation trace visualization

Recommended on order detail (read-only audit):

* last check timestamp,
* `billingStatus` (`CLEAR` / `BLOCKED`),
* `message` from last check.

Internal viewing of verified results remains allowed before successful release.

---

# 28. OWR Integration UX

OWR integration is asynchronous.

Frontend must support:

* async observability,
* retry visibility,
* operational monitoring.

---

## OWR Status

```text
Pending
Sent
Failed
```

Must appear:

* in queue,
* in workspace detail,
* in operational alert area.

---

## OWR Failure Visibility

OWR failure is considered operationally important.

Failures should appear:

* queue badge,
* workspace warning,
* operational notification area.

---

# 29. Dialog Philosophy

Dialogs used ONLY for:

* destructive confirmation,
* defer reason,
* cancellation reason,
* termination reason,
* amendment reason,
* release confirmation.

Avoid:

* giant workflow dialogs,
* nested modal workflow,
* modal-heavy architecture.

Prefer:

* side panel,
* inline interaction,
* dedicated workspace.

---

# 30. Loading & Async UX

Operational users must always understand:

* current loading state,
* active processing state,
* pending mutation,
* retry state.

Avoid:

* silent loading,
* hidden async behavior,
* ambiguous processing state.

---

# 31. Mutation UX Strategy

Mutations should provide:

* predictable loading state,
* toast feedback,
* inline error feedback,
* explicit retry action.

Avoid:

* browser alert,
* silent failure,
* disappearing errors.

---

# 32. Query Ownership

Recommended query ownership:

```text
queries/
  useLabOrderWorkspaceQuery
  useLabOrderDetailQuery
  useCollectionQueueQuery
  useResultEntryQueueQuery
  useVerificationQueueQuery
  useReleaseQueueQuery
```

Mutations:

```text
useChargeLabOrderMutation
useCollectSpecimenMutation
useRecordResultMutation
useVerifyResultMutation
useReleaseResultMutation
useAmendResultMutation
```

---

# 33. Composable Ownership

Recommended composables:

```text
useLabWorkspace
useLabQueue
useCollectionWorkflow
useResultEntryWorkflow
useVerificationWorkflow
useReleaseWorkflow
useWorkspaceKeyboardNavigation
```

Composable responsibilities:

* workflow orchestration,
* keyboard orchestration,
* query coordination,
* operational interaction flow.

---

# 34. Component Decomposition

## Workspace Components

```text
LabWorkspaceShell.vue
LabWorkspaceTabs.vue
LabOrderTimeline.vue
LabActionToolbar.vue
```

---

## Queue Components

```text
LabQueueTable.vue
LabQueueFilters.vue
LabQueueStatusBadge.vue
LabQueuePriorityBadge.vue
```

---

## Result Components

```text
ResultEntryGrid.vue
ResultComponentRow.vue
ReferenceRangeInfo.vue
ResultFlagBadge.vue
```

---

## Verification Components

```text
VerificationPanel.vue
ResultVersionHistory.vue
AmendmentComparisonPanel.vue
```

---

## Release Components

```text
ReleaseEligibilityCard.vue
BillingReleaseValidationTrace.vue
ReleaseBlockedAlert.vue
```

---

# 35. Error Handling Philosophy

Operational workflow errors must remain:

* visible,
* actionable,
* understandable.

Avoid:

* technical stack traces,
* hidden integration failures,
* silent rejection.

Errors should guide operational correction.

---

# 36. Frontend Testing Strategy

Priority testing areas:

* workflow transition tests,
* queue interaction tests,
* keyboard navigation tests,
* amendment workflow tests,
* immutable-state tests,
* workspace-session tests,
* result-entry interaction tests.

---

# 37. Incremental Frontend Development Strategy

## Phase 1

```text
types/
schemas/
services/
queries/
```

---

## Phase 2

```text
workspace shell
queue table
workspace tabs
status visualization
```

---

## Phase 3

```text
collection workflow
```

---

## Phase 4

```text
result entry workflow
keyboard navigation
auto flagging
```

---

## Phase 5

```text
verification workflow
amendment workflow
version comparison
```

---

## Phase 6

```text
release workflow (realtime BIL validation)
OWR monitoring
```

---

# 38. Operational Risk Areas

High-risk frontend areas:

* accidental workspace refresh during editing,
* amendment/version confusion,
* incorrect release visibility,
* keyboard focus loss,
* stale queue state,
* async mutation inconsistency,
* hidden OWR failure,
* multi-tab workspace collision.

These areas require:

* explicit UX handling,
* predictable interaction,
* workflow-safe architecture.

---

# 39. Final Frontend Engineering Principles

LWF frontend prioritizes:

* workflow-first architecture,
* operational ergonomics,
* maintainability,
* explicit orchestration,
* predictable UI behavior,
* keyboard-centric interaction,
* queue-centric navigation,
* AI-safe frontend architecture.

The long-term goal is:

> a scalable operational workspace architecture for hospital workflow systems that remains understandable, maintainable, and operationally efficient for long-term daily usage.
