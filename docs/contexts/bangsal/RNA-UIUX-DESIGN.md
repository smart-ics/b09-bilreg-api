# RUANG RANAP Operational Management UI/UX Design

## 1. Design Scope

### Operational problems addressed

- Give RUANG RANAP staff one place to see accommodation demand, current patient accommodation, bed readiness, and RUANG RANAP Service work within their authorized RUANG RANAP scope.
- Make the next accountable action visible without treating list or projection data as authority.
- Support accommodation assignment, retained accommodation, Mother-and-Baby Rooming-In, internal transfer, inter-ward Release-to-Waiting-List, release, and bed recovery while preserving history.
- Support ordered, Ad Hoc, and Independent RUANG RANAP work through optional assignment, truthful execution recording, correction, and execution-fact publication.
- Keep integration failure, stale work, and policy-blocked work visible to the role responsible for resolving it.

### Primary users

- Perawat Ruangan and Bidan Ruangan.
- Kepala Ruangan or RUANG RANAP Coordinator.
- Bed Coordinator, where this role exists.
- Ward Nurses and Head Nurses performing retained, Rooming-In, release, and placement actions.
- Performer.
- Housekeeping and Maintenance/Facilities actors.
- Authorized correction, audit, and integration-recovery actors.

### Included workflows

- SOP-RNA-A01 through SOP-RNA-A07.
- SOP-RNA-S01 through SOP-RNA-S04.
- Read-only accommodation and execution history required to understand and safely perform those workflows.
- Human recovery of failed or rejected integration obligations when architecture grants that responsibility.

### Explicit exclusions

- Clinical Order creation, amendment, cancellation, discontinuation, and clinical authorization; the UI may show CPOE facts and route users to the owning workflow.
- NERS documentation authoring, Medication Administration, Shift Handover, clinical discharge authorization, and specialized departmental fulfilment.
- Service Definition/configuration, performer types, quantity/unit rules, documentation requirements, completion criteria, and outcome catalogues owned by Tarif or another named owning context.
- Charge Eligibility, tariff, package, coverage, bill amount, adjustment, payment, or any other Tata Rekening decision. The user may classify an execution as billable or non-billable solely to select the RNA delivery path; this is not a financial decision.
- Bed, room, RUANG RANAP, class, Service, practitioner, or policy master-data maintenance.
- Temporary Absence. It is explicitly not needed and must not be modeled, displayed, or actioned by RNA.
- Accommodation correction as an ordinary workflow; it is restricted to Head Nurse, same-Ward facts, and pre-`FINALIZED` Tata Rekening status.
- Generic dashboards or analytics not required to operate the included workflows.
- Admission Waiting List/transfer-queue management and destination routing, which remain in Admisi.
- Inter-ward clinical content, which remains in EMR.
- BOR calculation.

### Unresolved decisions

**Authorization phase boundary (ARCH-020).** The current implementation assumes only baseline authentication and coarse-grained application access. Fine-grained contextual authorization is intentionally out of scope for the current phase and deferred to Phase-99. The UI rules below describe business semantics and future capability boundaries; they do not imply that contextual enforcement is currently implemented. Domain and SOP artifacts remain authoritative.

The following decisions materially control action availability. The UI must not substitute free text, a generic override, or a locally invented role for a missing policy.

| Decision group | Governing gaps | UI consequence until resolved |
|---|---|---|
| Admisi Waiting List processing and placement | GAP-RNA-001/002 and GAP-RNA-ARCH-015 CLOSED (DESIGN) | Ward reviews `WAITING`/`READY_FOR_WARD_REVIEW`; legacy `Accepted` is not placement proof. Rejection requires accountable free text. Assignment moves responsibility to RNA. Inter-ward release creates/reuses Admisi Waiting List; destination Ward is optional until Admisi routes it. Corrections requiring reversal show `ReconciliationRequired`. |
| Retained Accommodation, Mother-and-Baby Rooming-In, and inter-ward movement | GAP-RNA-003/004/005 CLOSED | Retained and Rooming-In actions are available only to Ward Nurse/Head Nurse with the approved invariants. Inter-ward UI provides source Release and Admisi notification state only; destination uses the ordinary Waiting List placement screen. |
| Accommodation correction and final Bed Ready verification | GAP-RNA-006 CLOSED; transactional Bed Readiness model | No destructive correction is offered. Head Nurse may append a same-Ward Accommodation Correction Fact only before Tata Rekening `FINALIZED`; each readiness action records a transaction; `Ready` requires authenticated evidence where applicable and an authorized verifier before the Bed current-state projection is refreshed. |
| Tarif Service eligibility, exceptional authority, and correction | GAP-RNA-008/019 CLOSED (DESIGN) / implementation dependency; GAP-RNA-010/011 and ARCH-016 CLOSED (DESIGN) | The UI resolves active Services through `Bangsal → Layanan → Tarif.AllowedLayanan`. Billable save requires a selected eligible Service; non-billable save requires description only. It visibly distinguishes `ServiceDependencyUnavailable` from `NoEligibleService`, never invents Service Definition rules, and does not claim Phase-99 authorization enforcement. |
| CPOE delivery and reconciliation | CPOE-RNA-OD-007 CLOSED; GAP-RNA-ARCH-016 CLOSED (DESIGN) / implementation dependency | Show durable Pending/Acknowledged/Failed/Rejected/Reconciliation Required states. Retry reuses the same obligation identity; reconciliation queries the authoritative owner. A failed acknowledgement never reverses the committed local fact. |
| Initial CPOE/RNA rollout | CPOE-RNA-OD-010 CLOSED FOR V1 | UI and recovery tooling support contract `v1` only. Rollback disables new native intake while preserving all committed v1 facts and visible delivery backlog. |
| Billable execution delivery to Tindakan/Tata Rekening | GAP-RNA-012 and GAP-RNA-ARCH-017 CLOSED (DESIGN) by `RNA-TATA-REKENING-INTEGRATION.md`; implementation dependency | User chooses billable or non-billable. Billable requires eligible Service and shows Pending/Acknowledged/Failed/Rejected/Reconciliation Required plus linked TindakanId; non-billable requires description and expects no Tindakan. |
| Contextual capabilities and business time | ARCH-020 (CLOSED — Deferred to Phase-99); RNA Business Time Standard | Current phase provides baseline authentication/coarse application access only; fine-grained contextual capability enforcement is deferred. Preserve the business rules and display `OccurredAt` as authoritative business time and `RecordedAt` as audit/technical tracing time. |

## 2. Workspace Model

The product uses one scoped **Operasional RUANG RANAP** workspace. It is organized around recurring work rather than aggregates or backend use cases.

### Main workspace structure

```text
┌ Operasional RUANG RANAP ─ [RUANG RANAP scope] ─ [Shift/Today] ─ [Refresh] ┐
│ [Perlu Ditangani] [Pasien & Akomodasi] [Bed] [Layanan] [Pemulihan*]       │
├───────────────────────────┬───────────────────────────────────────────────┤
│ Worklist                  │ Selected context                              │
│ Search and filters        │ Identity, current state, warnings             │
│ Prioritized work items    │ Decision-critical facts and history           │
│                           │                                               │
│                           ├───────────────────────────────────────────────┤
│                           │ Contextual action surface                      │
└───────────────────────────┴───────────────────────────────────────────────┘
* Pemulihan appears only for authorized recovery/support roles.
```

The workspace has three persistent regions:

1. **Scope and mode bar** — identifies the active RUANG RANAP scope and operational mode.
2. **Worklist** — supports scanning, prioritizing, filtering, and selecting work.
3. **Context and action surface** — shows the selected patient, bed, Waiting List entry, execution, or recovery obligation before an action is started.

Consequential actions open a focused action panel over the context area. The selected work item remains identifiable, and canceling the action returns to the same list position and filters.

### Navigation model

- **Perlu Ditangani** combines only actionable or blocked items: Admisi Waiting List entries for Ward review, service work due, retained releases, bed-recovery work, and failed obligations. It does not create an RNA transfer-decision queue.
- **Pasien & Akomodasi** finds a patient or Waiting List entry and opens the Patient Accommodation Workspace.
- **Bed** opens the Bed Status and Recovery Board.
- **Layanan** opens the RUANG RANAP Service Worklist and Execution Workspace.
- **Pemulihan** opens the Integration Recovery Console for explicitly authorized support actors.
- Deep links may open a patient, allocation, bed, execution, or recovery item directly, but the scope bar and owning RUANG RANAP remain visible.

### Information hierarchy

Across all modes, information appears in this order:

1. Patient/bed/work identity and responsible RUANG RANAP.
2. Current authoritative state, responsibility, and next available action.
3. Safety, Mandatory Bed Assignability, time, stale-data, and integration warnings.
4. Facts required for the current decision.
5. Related active allocations or work.
6. Chronological history, audit, and external-delivery detail under progressive disclosure.

Patient clinical content is limited to the minimum facts supplied by an owning domain and required for the current accommodation or execution decision.

### Movement between worklist, context, and actions

- Selecting an item updates the context without losing the worklist.
- Starting an action revalidates the selected item. If it changed, the panel does not open with stale assumptions; the user sees the newer state and must reassess.
- After success, the context updates first, then the item moves to its correct list state. The UI states what changed and which downstream acknowledgement remains pending, if any.
- Opening related work, such as a bed from an accommodation or an execution from a patient, preserves a back path to the originating context.

## 3. Screen Inventory

| Screen / View | User Goal | Primary User | Source SOP / Scenario |
|---|---|---|---|
| Operational Attention Worklist | Find the most urgent unresolved work within the current RUANG RANAP | Perawat Ruangan, Kepala Ruangan | All included SOPs; architecture operational projections |
| Waiting List Review & Placement | Review an Admisi Waiting List entry and assign suitable accommodation | Kepala Ruangan, Perawat Ruangan, Bed Coordinator | SOP-RNA-A01 |
| Patient Accommodation Workspace | Understand and manage all active and historical accommodation purposes for one registration | Perawat Ruangan, Kepala Ruangan | SOP-RNA-A02 through A06 |
| Inter-Ward Release Workspace | Release source accommodation and show notification delivery to Admisi | Perawat Ruangan asal, Kepala Ruangan asal | SOP-RNA-A05 |
| Bed Status & Recovery Board | Find assignable beds and restore unavailable beds to Ready | Perawat Ruangan, Kepala Ruangan, Housekeeping, Maintenance | SOP-RNA-A07; bed selection in A01/A04 |
| RUANG RANAP Service Worklist | Find and prioritize ordered and exception service work | Perawat Ruangan, Kepala Ruangan, Performer | SOP-RNA-S01 |
| Service Execution Workspace | Record ordered, Ad Hoc, or Independent execution as eligible Service + Performer + Performed At when billable, or description + Performer + Performed At when non-billable | Performer, Perawat Ruangan, Kepala Ruangan | SOP-RNA-S01, SOP-RNA-S02 |
| Execution Correction Review | Correct or mark a service execution Entered in Error without erasing the original | Authorized correction actor, Kepala Ruangan | SOP-RNA-S03 |
| Integration Recovery Console | Reconcile failed, stale, rejected, duplicate, or conflicting obligations without repeating business behavior | Authorized support actor | Exception paths in A01, A05, S01–S04; UC-RNA-032/033 |

## 4. Screen Specifications

### 4.1 Operational Attention Worklist

#### Purpose

Provide a role-scoped starting point for unresolved operational work without obscuring the type of decision required.

#### Layout

```text
┌ Perlu Ditangani ─ [Search patient/RegId/work ID] ─ [Filters] ─ [Refresh] ┐
│ Summary: Waiting List 3 | Service due 8 | Bed recovery 4 | Blocked 2       │
├───────────────────────────┬──────────────────────────────────────────────┤
│ [All] [Akomodasi] [Bed]   │ Selected item summary                        │
│ [Layanan]                 │ Current state · owner · age/due time         │
│                           │ Blocking reason / next available action      │
│ Prioritized items         │ [Open owning workspace]                      │
└───────────────────────────┴──────────────────────────────────────────────┘
```

#### Information Shown

- Work type, patient or bed identity, responsible RUANG RANAP, current state, age or due time, assigned actor, and blocking reason.
- A text label and icon for priority or overdue state; color is supplementary only.
- Source identity for legacy- or CPOE-originated work where it prevents mistaken authority.
- Counts reflect the active filters and authorized scope, not hospital-wide totals.

#### Available Actions

| Action | Available When | Required Input | User-Visible Result |
|---|---|---|---|
| Open owning workspace | The user may read the selected work type | None | The corresponding accommodation, bed, service, or recovery context opens with the item selected. |
| Claim/assign service work | The item is service work and the user may assign it | Performer/team, when used | The assignee appears; the work remains unexecuted until a Service Execution Fact is recorded. |
| Refresh | The user may read the current mode | None | List states and counts refresh; changed or removed selection is explained. |

#### UI States

- **Loading:** existing filters and selection remain visible with a progress indicator.
- **Empty:** states “No unresolved work in this scope” and retains filters.
- **Ready:** list uses a stable priority/due-time order and exposes why each item appears.
- **Stale/conflict:** changed items are updated in place and labeled; the user is not allowed to proceed from the old state.
- **Permission:** unauthorized categories do not appear. Lack of a mutation capability does not hide readable work.
- **Failure:** the last successful list remains visible as potentially stale, with refresh/retry guidance.

### 4.2 Waiting List Review & Placement

#### Purpose

Let authorized staff inspect an Admisi-owned Waiting List entry, select a candidate accommodation, and record actual patient receipt without taking ownership before Bed Assignment succeeds.

#### Layout

```text
┌ Waiting List Admisi ─ Patient / RegId ─ destination RUANG RANAP ─ status ┐
├──────────────────────┬────────────────────────────────────────────────┤
│ Waiting List review  │ Identity · destination · placement needs      │
│ Under Admisi         │ Ward review and assignment history            │
│ Rejected / Assigned  ├────────────────────────────────────────────────┤
│                      │ Candidate room/bed list                        │
│                      │ readiness · active purposes · assignability    │
│                      ├────────────────────────────────────────────────┤
│                      │ [Reject]                                        │
│                      │ [Assign] [Record actual receipt]               │
└──────────────────────┴────────────────────────────────────────────────┘
```

#### Information Shown

- Patient identity, registration identity, destination RUANG RANAP, placement need, Waiting List age, source, and revision.
- Admission ownership and Ward rejection reason when present.
- Candidate rooms/beds with active-master, intended-Ward, readiness, active-allocation, capacity/occupancy, and freshness time.
- Waiting List review result, proposed/active allocation, actual receipt, and successful-placement result to Admisi.
- Existing conflicting active Clinical Accommodation, prior Release, or Admisi Waiting List facts.

#### Available Actions

| Action | Available When | Required Input | User-Visible Result |
|---|---|---|---|
| Reject Waiting List entry | Ward cannot place the patient at this time | Sanitized accountable free-text reason | Result is visible to Admisi; Waiting List remains owned by Admission and no allocation is created. |
| Select candidate bed | Bed is currently displayed as potentially usable | Candidate bed | Candidate detail opens; no bed is reserved or assigned by selection alone. |
| Assign Clinical Accommodation | Entry is visible for the Ward; Mandatory Bed Assignability and authority are available | Bed, Accommodation Purpose, business time | Active Clinical Accommodation appears. Successful result closes the Waiting List through Admisi and responsibility moves to RNA. If the authoritative recheck fails, no allocation is created and the current reason is shown. |
| Record actual receipt | An active destination allocation exists and the user may receive the patient | Actual receipt time; required receipt evidence | Patient appears received at the Clinical Accommodation; acknowledgement delivery state is visible. |

#### UI States

- **Waiting List retained by Admission:** a Ward rejection is named with its reason; no RNA responsibility or allocation exists.
- **No suitable bed:** shows mandatory assignability failure; Ward may reject with a reason.
- **Operational consideration:** gender, isolation, equipment, and hospital-specific policy may be visible as Ward context but never make RNA reject Bed Assignment.
- **Concurrent placement conflict:** candidate and patient context refresh; the user must select again.
- **Placement result pending/failed:** placement remains visible as committed; delivery failure is not presented as failed placement.
- **Success:** current Clinical Accommodation, receipt time, Waiting List close result, and RNA responsibility are shown separately.

### 4.3 Patient Accommodation Workspace

#### Purpose

Show one registration’s current Clinical Accommodation, other simultaneous purposes, and complete allocation continuity, then expose only the accommodation actions valid for the current state.

#### Layout

```text
┌ Patient / RegId ─ Responsible RUANG RANAP ─ Current Clinical Location ┐
│ Alerts: retained release at discharge · Admisi notification             │
├─────────────────────────────┬─────────────────────────────────────────┤
│ Active allocations          │ Selected allocation                     │
│ • Clinical Accommodation    │ purpose · occupant role · treatments    │
│ • Retained Accommodation    │ started/received · room/bed · readiness │
│ • Rooming-In association    │ related registration and history        │
│                             ├─────────────────────────────────────────┤
│ Allocation timeline         │ Contextual actions                      │
│                             │ [Transfer] [Retain/Review] [Rooming-In] │
│                             │ [Release] [Restricted correction]       │
└─────────────────────────────┴─────────────────────────────────────────┘
```

#### Information Shown

- Patient and registration identity, responsible RUANG RANAP, and current Clinical Location.
- Every active allocation separately, including purpose, Primary/Associated Occupant role, start time, physical-occupancy indication when supplied, and occupancy/reporting/charging treatments.
- Rooming-In related registration without merging patient identities.
- Pending internal transfer or an Admisi notification after inter-ward Release.
- Active Retained Accommodation and its mandatory discharge-release state.
- Chronological allocation history with proposal, activation, reclassification, transfer, release, and correction relationships.
- Companion Accommodation flag and its associated patient registration; it is visibly labeled as non-patient occupancy.

#### Available Actions

| Action | Available When | Required Input | User-Visible Result |
|---|---|---|---|
| Start internal transfer | Clinical Accommodation is active; source and target remain in the same RUANG RANAP; target Mandatory Bed Assignability is established | Reason, candidate target, approval, actual move time, source disposition | Target becomes Clinical Accommodation only after success; source is shown as Released or Retained with preserved continuity. |
| Retain former accommodation | User is Ward Nurse/Head Nurse; another treatment accommodation is active or being established | Reason and start time | Former allocation remains Active, occupies capacity, produces accommodation facts, and is not shown as Clinical Location. |
| Release Retained Accommodation at discharge | User is Ward Nurse/Head Nurse and an authoritative discharge fact is present | Release reason and business time | Retained allocation becomes Released; RNA accommodation closure remains incomplete while another retained allocation is Active. |
| Start Rooming-In | User is Ward Nurse/Head Nurse; Mother and Baby have distinct registrations; Patient Social Data confirms Baby MR → Mother MR; bed has one Primary and no Associated Occupant | Mother/Baby registrations, relationship evidence/reference, start time | Separate Baby associated allocation appears; capacity does not increase. |
| End Rooming-In | Association is active and each registration’s resulting accommodation can be stated | End reason/time and release or transfer outcome for the associated allocation | Association ends; both registrations remain separate and each accommodation outcome is visible. |
| Release allocation | Allocation is Active, its purpose has ended, and replacement location or inter-ward Waiting List flow is clear when clinical care continues | Structured release reason and actual end time | Allocation becomes Released; bed post-use condition and recovery follow-up appear. Encounter discharge is not implied. |
| Open inter-ward Release | Clinical Accommodation is active and user may release it | None | Inter-Ward Release Workspace opens with source context preserved. |
| Correct Accommodation Fact | User is Head Nurse; original fact belongs to the same Ward; Tata Rekening is not `FINALIZED` | Correct facts, reason, correction time, original-fact reference | Original remains unchanged; a new correction fact is linked and its downstream publication status appears. |

#### UI States

- **No active Clinical Accommodation:** prominent exception state; other active purposes remain visible and are not promoted to Clinical Accommodation.
- **Multiple active purposes:** each purpose and treatment is shown explicitly; the UI does not summarize them as “multiple beds” without meaning.
- **Policy blocked:** affected action is unavailable with a concise policy reason.
- **Inter-ward Release pending:** source allocation remains Active until Release commits; afterward Admisi notification state is shown without RNA destination ownership.
- **Release rejected:** entered reason/time are preserved; current conflicting fact is shown for reassessment.
- **Correction blocked:** clearly distinguishes not Head Nurse, another Ward owns the original fact, and Tata Rekening `FINALIZED`; the last condition directs users to the external administrative process.
- **History:** original and corrected records are visually linked and never replaced in the timeline.

### 4.4 Inter-Ward Release Workspace

#### Purpose

Release the source Clinical Accommodation and make the Admisi notification outcome visible. This workspace does not coordinate a destination ward, own a transfer queue, or contain clinical content.

#### Layout

```text
┌ Release ID ─ Patient / RegId ─ Source RUANG RANAP ─ Release state ┐
├───────────────────────────────────────────────────────────────────┤
│ Source allocation · reason · business time · actor                │
│ Post-release Bed Readiness                                        │
│ Admisi notification: Pending / Acknowledged / Failed              │
│ [Release to Waiting List] [Open integration recovery*]            │
└───────────────────────────────────────────────────────────────────┘
* Only for authorized recovery actors.
```

#### Information Shown

- Source Clinical Accommodation, reason, accountable actor, business time, and release identity.
- Explicit statement that Admisi owns the Waiting List and operational responsibility after committed Release.
- Post-release Bed Readiness transaction and current-state projection.
- Admisi notification delivery state and correlation identity under audit detail.
- Link to the EMR clinical-content workflow when supplied by EMR; RNA does not display or mutate that content here.

#### Available Actions

| Action | Available When | Required Input | User-Visible Result |
|---|---|---|---|
| Release to Waiting List | Source Clinical Accommodation is Active and user is an authorized source Ward Nurse/Head Nurse | Structured reason and business time | Source allocation becomes Released, post-use readiness is recorded, and Admisi notification becomes Pending/Acknowledged. |
| Retry/reconcile Admisi notification | Release is committed, delivery failed/stale, and user has integration-recovery authority | Recovery reason when required | Same stable Release fact is retried; no accommodation transition is repeated. |
| Open destination placement | A later ordinary Admisi Waiting List entry is visible to an authorized destination actor | None | Waiting List Review & Placement opens; it is the same workflow used for every Waiting List patient. |

#### UI States

- **Before Release:** source allocation remains Active; no destination ownership is shown.
- **Release committed / notification pending:** source is Released and Admisi notification is visibly pending; RNA has no post-Release operational responsibility.
- **Notification failed:** Release remains committed; recovery uses the same stable fact identity.
- **Waiting List / destination:** shown only from Admisi-owned facts. Ward rejection keeps responsibility with Admission; successful Bed Assignment is the only responsibility change.
- **Cancelled transfer:** route/status is displayed from Admisi when available; RNA offers no separate cancellation-return workflow.

### 4.5 Bed Status & Recovery Board

#### Purpose

Support bed search for placement and track each unavailable bed through cleaning, inspection, maintenance, restriction resolution, and final Ready verification.

#### Layout

```text
┌ Bed ─ [Room/Class] [Readiness] [Recovery owner] [Age] [Refresh] ┐
├───────────────────────────────┬──────────────────────────────────┤
│ Room-grouped bed list         │ Selected bed                     │
│ Bed 01 Ready                  │ readiness · active purposes      │
│ Bed 02 Cleaning Required      │ restrictions · recovery tasks   │
│ Bed 03 Out of Service         │ evidence · verification history │
│                               │ [Record progress] [Mark Ready]   │
└───────────────────────────────┴──────────────────────────────────┘
```

#### Information Shown

- Bed identity, room, class/reference characteristics, master-active indication, readiness, active allocation purposes, occupancy effect, and availability summary.
- Cleaning, inspection, maintenance, safety, or operational restriction and its responsible actor.
- Recovery work age, assignee, evidence, completion time, outstanding blockers, and verifier history.
- Fact freshness. “Potentially available” is used until a placement command rechecks authoritative conditions.

#### Available Actions

| Action | Available When | Required Input | User-Visible Result |
|---|---|---|---|
| Record post-release condition | Accommodation has been released or an authorized issue is found | Condition, structured reason, business time, required evidence | Bed shows Cleaning Required, Blocked, or Out of Service and is unavailable. |
| Start recovery work | User owns the assigned recovery type | Start time and assignee when required | Task shows In Progress; bed remains unavailable. |
| Record recovery result | Assigned work is completed or a blocker is resolved | Result, completion time, evidence/reference, remaining issue if any | Completed work appears; unresolved conditions keep the bed unavailable. |
| Mark Ready | All required recovery conditions are resolved, no incompatible allocation exists, and user is an approved verifier | Business Time; Responsible Actor from identity; Reason optional; Evidence optional | An explicit verified Ready transaction is appended and Bed current-state projection becomes Ready; a conflict leaves it unavailable. |
| Open active allocation | User may read the allocation | None | Patient Accommodation Workspace opens at the relevant allocation. |

#### UI States

- **Ready:** clearly distinguish from master-active; shows no unresolved blocker.
- **Cleaning/blocked/out of service:** reason, age, and responsible actor are always visible.
- **Recovery completed, verification pending:** not shown as available.
- **Allocation contradiction:** Mark Ready is unavailable and reconciliation is escalated.
- **Stale source evidence:** names the fact requiring refresh or confirmation.
- **Permission:** Housekeeping/Maintenance can record only their assigned evidence; final verification remains a distinct capability.

### 4.6 RUANG RANAP Service Worklist

#### Purpose

Let RUANG RANAP staff scan expected work, distinguish its true source and state, and optionally assign or open work without implying execution.

#### Layout

```text
┌ Layanan RUANG RANAP ─ [Search] [Status] [Performer] [Due] [Source] ┐
├────────────────────────────┬────────────────────────────────────────┤
│ Pending / Assigned         │ Selected work summary                  │
│ Executed / Cancelled       │ patient · Service · instruction       │
│ Integration blocked       │ planned occurrence · due time · source│
│                            │ assignment · blockers                  │
│                            │ [Assign] [Open execution]              │
└────────────────────────────┴────────────────────────────────────────┘
```

#### Information Shown

- Patient and registration, RUANG RANAP Service, source classification, source identity, planned occurrence, instruction summary, priority/due time, status, assignee, and blocker.
- Clear distinction among Pending, Assigned, Executed, Cancelled/Withdrawn, and integration-blocked work.
- CPOE or approved legacy source label. Legacy work is never labeled as a native Clinical Order unless it is one.
- Historical `OrderTdk` is shown read-only with an explicit legacy label. Native billable executions show their linked `Tindakan`; non-billable executions show that no `Tindakan` is expected.
- Existing Accommodation Stay records are displayed from the legacy core plus RNA extensions. Legacy-origin facts and missing historical detail remain explicitly labeled; the UI does not imply a migration or invent earlier business history.
- Changed, cancelled, or discontinued source facts affecting work not yet performed.

#### Available Actions

| Action | Available When | Required Input | User-Visible Result |
|---|---|---|---|
| Assign Performer/team | Work is Pending and the user has assignment authority | Performer/team | Assignment is shown; no execution is implied. |
| Open execution | User may read the patient/work context | None | Service Execution Workspace opens at the selected occurrence. |
| Open source order | An owning source view is available and user may access it | None | Source context opens read-only or in its owning system; RNA does not edit the order. |
| Refresh changed source | A newer source revision is indicated | None | Current instruction and impact are shown; an existing execution fact is not overwritten. |

#### UI States

- **Empty:** distinguishes no work from filters excluding work.
- **Source changed/cancelled:** affected unperformed work is prominent; source-owned changes are not editable here.
- **Service dependency unavailable:** work stays visible; billable recording is unavailable until Tarif can return the eligible-Service query.
- **No eligible Service:** the Tarif query succeeded but returned no active Service for the Bangsal's Layanan; billable recording is unavailable, while non-billable description-only recording remains available.
- **Duplicate/ambiguous legacy source:** item is quarantined from execution and routed to recovery.
- **Late/overdue:** due-time label and elapsed time are explicit; color is supplementary.
- **Failure:** last successful worklist remains visible as potentially stale.

### 4.7 Service Execution Workspace

#### Purpose

Enable an authorized Performer to record the truthful fact that billable work used an eligible Tarif Service, or that non-billable work occurred with a description, while preserving source, occurrence, actual time, recorded time, correction history, and delivery status.

#### Layout

```text
┌ Patient / RegId ─ Tarif Service ─ Pending/Assigned/Executed       ┐
│ Source: Clinical Order / Legacy / Ad Hoc / Independent            │
├─────────────────────────────┬──────────────────────────────────────┤
│ Work context                │ Execution fact                      │
│ instruction · occurrence    │ Service ID · Performer              │
│ source · assignment         │ Performed At · Recorded At          │
│ Tarif reference status      │ Fact ID · correction revision       │
├─────────────────────────────┴──────────────────────────────────────┤
│ [Assign] [Record execution] [Create Ad Hoc/Independent]            │
│ History · authority · CPOE delivery · Tata Rekening delivery       │
└────────────────────────────────────────────────────────────────────┘
```

#### Information Shown

- Patient, care context, responsible RUANG RANAP, Bangsal Layanan, active eligible Tarif Service list/display when available, source identity, and planned occurrence where applicable.
- Optional assignment, actual Performer, Performed At, Recorded At, fact identity, and correction revision.
- For Ad Hoc/Independent work: authority basis, chronology, subsequent authorization status, and overdue state.
- Per-destination delivery state for CPOE and Tata Rekening. These states do not expose Charge Eligibility or financial results.

#### Available Actions

| Action | Available When | Required Input | User-Visible Result |
|---|---|---|---|
| Record ordered execution | Work is Pending/Assigned and user may record execution | Billable: eligible Service selected from the Bangsal's active Tarif list. Non-billable: description. Performer and Performed At. | Service Execution Fact appears; billable work shows CPOE and Tata Rekening delivery states, non-billable has no Tindakan delivery. |
| Create Ad Hoc/Independent execution | User has professional authority | Billable: eligible Service selected from the Bangsal's active Tarif list. Non-billable: description. Truthful source, authority basis, patient context, Performer, Performed At; emergency/late chronology when applicable | Execution fact appears with its actual source; no prospective Clinical Order or local Service Definition is fabricated. |
| Record delayed entry | Actual work occurred earlier and policy permits retrospective recording | Performer, Performed At, delay reason; Recorded At supplied by system | Both times and delay reason appear in history. |
| Hold and request clarification | Ordered instruction is unclear, wrong destination, changed, or unsafe | Structured issue and explanation as required | Work remains unresolved with blocker and follow-up owner; order changes stay in CPOE. |
| Open correction review | An execution fact exists and user may request or perform correction | None | Execution Correction Review opens with the original fact locked as evidence. |

#### UI States

- **Validation:** billable requires an eligible Service; non-billable requires description; both require Performer and Performed At. Errors are identified next to the relevant input and entered values remain.
- **Not executed:** source cancellation/withdrawal or an unperformed work item never creates a Service Execution Fact.
- **Authority missing:** non-emergency creation is unavailable. Emergency flow is available only under approved policy and requires explicit basis.
- **Subsequent authorization pending/overdue:** show the deadline (24 hours from `OccurredAt` or discharge, whichever is earlier), accountable authorizer, acknowledgement separately from authorization, escalation owner, terminal `Authorization Overdue`, and any late review. Execution remains visible and is never erased or relabeled as timely/prospectively authorized.
- **Concurrent change:** new source or execution state is shown before retry; the user must reassess.
- **Delivery failure:** the local execution fact remains committed and visible; failed delivery is a separate state.

### 4.8 Execution Correction Review

#### Purpose

Let a specifically authorized actor append a correction or mark an invalid execution Entered in Error while preserving the original and its downstream effects.

#### Layout

```text
┌ Correction Review ─ Execution ID ─ Patient / Service ┐
├───────────────────────┬───────────────────────────────┤
│ Original (read-only)  │ Proposed correction           │
│ source · performer    │ correction / Entered in Error │
│ Service · Performer   │ corrected facts · reason      │
│ Performed/Recorded At │ replacement link if required  │
├───────────────────────┴───────────────────────────────┤
│ Delivery impact: CPOE | Tata Rekening                  │
│ [Submit for review] [Confirm correction]               │
└────────────────────────────────────────────────────────┘
```

#### Information Shown

- Complete original execution fact, current correction chain, and dispute/review state.
- Suspected error type, proposed correct facts, and downstream delivery impact.
- Original and replacement patient/service identities side by side for wrong-patient or wrong-service cases.

#### Available Actions

| Action | Available When | Required Input | User-Visible Result |
|---|---|---|---|
| Submit correction for review | User may report an error but cannot finalize it | Error type, explanation, proposed facts/evidence | Review is pending; original remains authoritative and visible. |
| Append ordinary correction | User is the owning-Ward Head Nurse, is not approving their own report/proposal, and correct facts are established | Corrected facts, structured reason, correction time | Original and correction are linked; affected downstream correction obligations appear. |
| Finalize material or disputed correction | An independent Clinical Governance-authorized reviewer is assigned | Corrected facts, structured reason, complete audit identity, evidence, and replacement link when required | Independent-review identity and evidence appear in the immutable correction chain. |
| Mark Entered in Error | Record should not have existed and an independent Clinical Governance-authorized reviewer approves | Structured reason, evidence, replacement reference when actual work needs a correct record | Original remains visible as Entered in Error; no physical deletion occurs. |
| Create replacement execution | Action occurred but original identity/facts require a new valid execution | Service, Performer, Performed At, source/correlation, and link to original | Replacement appears in the correction chain and follows normal execution-fact delivery. |

#### UI States

- **Policy/authority unavailable:** final correction actions are unavailable; reporting and review evidence may still be retained if authorized.
- **Self-approval blocked:** the owning-Ward Head Nurse cannot finalize their own report or correction proposal.
- **Material/disputed facts:** finalization is held until an independent Clinical Governance-authorized reviewer and required evidence are present.
- **External document owned elsewhere:** only the RNA reference can be corrected; route to the owning domain is shown.
- **Downstream correction pending/failed:** correction remains committed; each destination state is shown separately.

### 4.9 Integration Recovery Console

#### Purpose

Expose cross-context obligations that need authorized reconciliation while preventing a retry from repeating accommodation or execution behavior.

#### Layout

```text
┌ Pemulihan Integrasi ─ [Direction] [Collaborator] [Status] [Age] ┐
├──────────────────────────┬───────────────────────────────────────┤
│ Failed/stale obligations │ Selected fact and attempt history     │
│ correlation · age        │ authoritative local fact/state        │
│ retry/rejection status   │ destination response · next action    │
│                          │ [Retry delivery] [Reconcile status]    │
└──────────────────────────┴───────────────────────────────────────┘
```

#### Information Shown

- Direction, collaborator, source fact, correlation identity, local authoritative fact/state, attempt history, last response category, age, and current owner.
- Whether the issue is duplicate, stale, failed, rejected, ambiguous, or conflicting.
- Minimum patient context needed to identify the obligation; broad clinical content is not included.

#### Available Actions

| Action | Available When | Required Input | User-Visible Result |
|---|---|---|---|
| Retry stable delivery | Local fact is committed, retry is permitted, and user has recovery authority | Reason when policy requires manual retry | A new delivery attempt appears under the same obligation; no aggregate action is repeated. |
| Reconcile acknowledgement | Owning collaborator can be queried or evidence is available | Reconciliation evidence/reference | Obligation updates to acknowledged, still pending, or conflicting with explanation. |
| Open owning record | User may read the corresponding accommodation or execution record | None | Owning workspace opens without granting recovery mutation rights there. |
| Escalate permanent rejection/conflict | Automatic/manual retry cannot safely resolve the issue | Structured escalation reason and owner | Obligation remains visible with escalation owner; source truth is unchanged. |

#### UI States

- **Stale:** age and retry/recovery availability are explicit.
- **Duplicate:** prior accepted fact/result is linked; no duplicate business action is offered.
- **Conflict:** source and destination facts are shown without a “force overwrite” action.
- **Dependency unavailable:** local truth remains visible; retry waits or is scheduled according to operational policy.
- **Permission:** console is hidden from ordinary clinical roles unless their capability explicitly includes recovery.

## 5. Cross-Screen Interaction Rules

### Scope, search, filtering, and sorting

- The active RUANG RANAP scope is always visible. Users with multiple scopes must deliberately select one or an authorized aggregate scope.
- Patient search supports the identifiers and names permitted by the host product; exact identity is confirmed before any mutation.
- Each worklist exposes only filters supported by its projection. Active filters are visible, removable individually, and retained when returning from detail.
- Default ordering is operational: unresolved priority first, then due time or work age, with a stable identity tie-breaker. Historical views default to newest event first.
- Refresh displays a freshness time. It never implies that a displayed candidate authorizes a command.

### Selection and navigation persistence

- Mode, filters, scroll position, and selected item survive opening and closing an action panel.
- If a selected item leaves the list after success, the UI explains its new state and selects the nearest remaining item rather than silently jumping.
- Links between patient, bed, Service execution, delivery, and recovery contexts preserve a visible back path.

### Action availability and authorization

The distinctions below are target interaction semantics. Current implementation enforcement is limited to baseline authentication and coarse-grained application access; fine-grained contextual authorization is deferred to Phase-99 under ARCH-020.

- **Unavailable by state** means the action is not currently valid; the UI states the lifecycle reason.
- **Policy blocked** means required governance or configuration is absent; the action is unavailable and names the missing policy category.
- **Unauthorized** means the current user lacks permission; sensitive policy details are not exposed.
- Read access does not imply mutation access. Assignment, execution recording, correction, readiness verification, delivery recovery, and audit capabilities remain distinct.
- Disabled controls are used only when showing the unavailable action helps explain the next step. Otherwise unauthorized actions are omitted.

### Revalidation, stale data, and concurrency

- Opening a consequential action revalidates current state, responsibility, policy, and relevant dependency facts.
- Submitting revalidates again. A conflict never silently applies the user’s earlier choice to new facts.
- On conflict, preserve safe entered values, show what changed, and require explicit reassessment.
- Projection freshness warnings remain visible, but command rejection is described as an authoritative recheck result rather than “UI error.”

### Confirmation and validation

- Confirmation is required for Release (including Release-to-Waiting-List), Mark Ready, execution recording, corrections, and Entered in Error.
- Confirmation summarizes the patient/bed/service identity, actual business time, resulting state, and consequential downstream effects.
- Structured reason, source, authority, Service reference, Performer, and time inputs use governed options where applicable. Free text supplements but never replaces required execution facts.
- Recoverable validation or delivery failures preserve entered values.

### Time and audit presentation

- `OccurredAt` (actual business time) and `RecordedAt` (system persistence time) are labeled separately whenever they can differ.
- History orders business facts by `OccurredAt`; worklists may retain their operational priority while showing the fact time. `RecordedAt` is shown only as audit/technical tracing metadata.
- When actual business time is unknown, the UI presents `OccurredAt = RecordedAt`; it never invents an earlier time.
- Late entry requires a visible reason and never rewrites `RecordedAt` to match `OccurredAt`/Performed At.
- History shows actor, `OccurredAt`, `RecordedAt` where material, reason, and correction relationship.
- Stable technical identities and source references appear under **Detail Sistem & Audit**, not in the primary operational summary unless needed to distinguish duplicate or legacy work.

### Feedback and downstream delivery

- Success feedback names the committed local result first, then lists each pending, acknowledged, rejected, or failed downstream delivery separately.
- A failed acknowledgement does not reverse or visually negate a committed accommodation, execution fact, or correction.
- Users are never told a Clinical Order, Waiting List, bill, or payment changed unless the owning context confirms it.

### Loading, empty, and failure behavior

- Worklists retain the last successful result during refresh and label it potentially stale on failure.
- Empty states distinguish no work, no results under current filters, missing scope, and insufficient permission.
- Action failures use distinct messages for invalid input, not found, forbidden, policy blocked, lifecycle conflict, concurrent change, dependency unavailable, and unexpected failure.

### Responsive behavior

- Desktop and ward-station tablet widths use the split worklist/context layout.
- Narrow widths show worklist, context, and action as sequential views with a persistent back path and current patient/bed/service identity.
- Dense bed and worklist scanning remains table/list based; actions are never hidden behind hover-only controls.

## 6. Coverage Matrix

| SOP / Scenario | User Goal | Screen / View | Action | Coverage |
|---|---|---|---|---|
| SOP-RNA-A01 — Pemrosesan Waiting List Admisi dan Penetapan Akomodasi | Review Waiting List, establish Mandatory Bed Assignability, assign accommodation, and record receipt | Waiting List Review & Placement | Reject; Assign Clinical Accommodation; Record actual receipt | Covered; GAP-RNA-001/002 CLOSED. Rejection leaves responsibility with Admission; successful assignment closes Waiting List and moves responsibility to RNA. |
| SOP-RNA-A02 — Pengelolaan Retained Accommodation | Keep allocation Active/capacity-consuming/fact-producing and release all at discharge | Patient Accommodation Workspace | Retain former accommodation; Release Retained Accommodation | Covered; GAP-RNA-003 CLOSED; Ward Nurse/Head Nurse only. |
| SOP-RNA-A03 — Pengelolaan Rooming-In | Start/end Mother-Baby Rooming-In without merging registrations or increasing capacity | Patient Accommodation Workspace | Start Rooming-In; End Rooming-In | Covered; GAP-RNA-004 CLOSED; Patient Social Data relation; Ward Nurse/Head Nurse only; no BOR logic. |
| SOP-RNA-A04 — Transfer Akomodasi Internal RUANG RANAP | Move within the same RUANG RANAP and preserve source disposition | Patient Accommodation Workspace; Bed Status & Recovery Board | Start internal transfer; select target; release or retain source | Covered; only Mandatory Bed Assignability applies. |
| SOP-RNA-A05 — Release ke Waiting List | Release source accommodation and notify Admisi; destination later uses ordinary Waiting List placement | Inter-Ward Release Workspace; Waiting List Review & Placement | Release to Waiting List; Retry/Reconcile notification; ordinary destination Assign | Covered; GAP-RNA-005 CLOSED; no RNA transfer queue or destination ownership before successful Bed Assignment. |
| SOP-RNA-A06 — Pelepasan dan Koreksi Akomodasi | End an accommodation purpose or append a same-Ward correction fact before `FINALIZED` | Patient Accommodation Workspace | Release allocation; Correct Accommodation Fact | Covered; GAP-RNA-006 CLOSED. Original never changes; Head Nurse only; RNA publishes correction and downstream contexts reconcile their own data. |
| SOP-RNA-A07 — Pemulihan Kesiapan Bed | Record readiness transactions and explicitly verify Bed Ready | Bed Status & Recovery Board | Record condition/progress/result; Record Ready | Covered; GAP-RNA-007 CLOSED. History is authoritative; each action appends `OccurredAt` (or `OccurredAt = RecordedAt` when unknown), `RecordedAt`, Responsible Actor, Status, optional Reason/Evidence and updates current-state projection. |
| SOP-RNA-S01 — Pencatatan Pelaksanaan berdasarkan Clinical Order | Find, optionally assign, and record ordered RNA execution | RUANG RANAP Service Worklist; Service Execution Workspace | Assign; Hold/clarify; Record execution | Covered; Tarif active-Service eligibility and source receipt/change are integrations. The UI selects but never defines Services or evaluates financial eligibility. |
| SOP-RNA-S02 — Pencatatan Tindakan Ad Hoc atau Independen | Record actual action under a truthful authority basis | Service Execution Workspace | Create Ad Hoc/Independent execution; record delayed/emergency chronology | Covered; GAP-RNA-010 CLOSED. UI exposes the approved 24-hour-or-discharge deadline, assignment, acknowledgement, escalation, overdue state, and late review; native integration remains under ARCH-016. |
| SOP-RNA-S03 — Koreksi Fakta Pelaksanaan | Preserve original execution fact while appending correction or Entered in Error | Execution Correction Review | Submit review; Append correction; Mark Entered in Error; Create replacement | Covered; GAP-RNA-011 CLOSED. Ordinary correction prohibits self-approval; material/disputed/identity/replacement/Entered-in-Error changes require independent second review and evidence. Downstream correction delivery is automated and visible. |
| SOP-RNA-S04 — Publikasi Billable Service Execution Fact | Publish billable facts, link one Tindakan, and monitor acknowledgement | Service Execution Workspace; Integration Recovery Console | Choose billable/non-billable; Retry/Reconcile for recovery authority | Covered; GAP-RNA-012 and ARCH-017 are closed in design. Non-billable creates no Tindakan; implementation remains. |
| Automated Admisi Waiting List entry receipt | Make a Waiting List entry visible once without taking Waiting List ownership | Operational Attention Worklist; Waiting List Review & Placement | None; system-initiated | Covered as automated inbound work with visible source/revision and recovery state. |
| Automated CPOE/legacy work receipt and change | Make one execution obligation per source occurrence and apply source changes | RUANG RANAP Service Worklist | None; system-initiated | Covered as automated inbound work; ambiguous legacy facts are quarantined. |
| Automated execution-fact delivery | Deliver committed facts without changing local truth on failure | Service Execution Workspace; Integration Recovery Console | None normally; Retry/Reconcile only for recovery authority | Covered with separate per-destination states and no financial interpretation. |
| Integration recovery | Resolve stale, failed, rejected, duplicate, or conflicting obligations safely | Integration Recovery Console | Retry stable delivery; Reconcile; Escalate | Covered for authorized support actors; retry cannot repeat aggregate behavior. |
| Companion Accommodation | Assign and release a companion bed through ordinary Bed Assignment | Patient Accommodation Workspace | Select bed; set `IsCompanionBed`; release | Covered as an extension of SOP-RNA-A01. It uses ordinary mandatory assignability and audit, is associated with the patient registration, and is visibly non-patient occupancy for downstream reporting; it does not create or consume an Admisi Waiting List entry. |
| Temporary Absence | No RNA workflow | None | None | Intentionally not modeled; GAP-RNA-014 is CLOSED (NOT NEEDED). |
