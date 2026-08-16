# Outpatient Apotek Repository Gap Analysis Report

**Artifact status:** Architecture review input  
**Review date:** 2026-08-15  
**Scope:** Current `b09-bilreg-api` backend and `c012_myhospital_web` frontend compared with:

- `apotek-domain.md`
- `outpatient-apotek-workflow.md`
- `outpatient-apotek-screen-and-aggregate-design.md`
- `adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md`

This report identifies and classifies gaps and unresolved decisions only. It intentionally contains no implementation roadmap, sequencing, milestone, or delivery estimate.

## 1. Executive assessment

The target outpatient Apotek model is not an incremental completion of the current implementation. It is a new bounded-context model that separates the legacy `Trs.DU (DO-Bill)` into `SalesOrder`, `SalesInvoice`, and `DispenseOrder`, while reusing selected queue, inventory, financial, catalog, and frontend infrastructure.

Current repository state:

- `b09-bilreg-api` has no implemented Apotek bounded context, target aggregates, persistence, API surface, or workflow tests.
- Backend pharmacy behavior is split between legacy `SalesContext` (`Resep`, checklist-style `Telaah`, and monolithic `Penjualan`) and F-09 Patient Tracker evidence (`Apotek-Start` / `Apotek-Done`).
- `c012_myhospital_web` has one mock-only `Apotek Rajal` prototype, not the required four-screen model.
- The generic queue enum already complies with ADR-APT-001, but current F-09 event triggers and the frontend's queue states conflict with the canonical workflow.
- Existing Patient Tracker, Tata Rekening, Stock Ledger, CPOE, Fornas, medication catalog, query, and UI-shell capabilities are reusable only through explicit extensions and adapters.

The architecture is not ready for implementation review until the blocking decisions in section 4 are resolved. All blocking architecture gaps (BA-01 through BA-09) are now resolved; remaining gates are business clarification (BC-11 through BC-13).

## 2. Classification summary

| Classification | Count | Review meaning |
|---|---:|---|
| Blocking Architecture Gap | 0 | A structural or ownership decision is unresolved; implementing around it would create incompatible sources of truth or unsafe cross-context behavior. |
| Business Clarification Gap | 3 | A policy, authority, threshold, or accountable outcome is not sufficiently defined. |
| Resolved (Business Clarification) | 11 | BC-01, BC-02, BC-03, BC-04, BC-05, BC-06, BC-07, BC-08, BC-09, BC-10, and BC-14 ratified; artifacts updated. |
| Existing Capability Extension | 9 | A relevant capability exists but its present contract or semantics do not satisfy Apotek. |
| Missing Implementation | 15 | The design is sufficiently clear, but no conforming implementation exists. |
| Technical Debt | 11 | Existing code or documentation embodies legacy, misleading, coupled, or unverified behavior. |
| Resolved (Blocking Architecture) | 9 | BA-01 through BA-09 ratified; artifacts updated. |
| **Total open** | **38** | Each open finding has one primary classification. |

## 3. Baseline and evidence

### 3.1 Canonical target

- The domain separates accepted demand, commercial sale, and physical fulfillment (`apotek-domain.md:17-26`, `65-77`).
- Patient Tracker owns queue identity and lifecycle; Apotek owns mapping and pharmacy progress (`apotek-domain.md:43-61`; ADR-APT-001:55-79, 83-145).
- `ServedAt` is caused by the first `Medication Preparation Started`; `DoneAt` is caused by the coordinated pickup call and does not prove handover (`apotek-domain.md:61`, `433-447`; `outpatient-apotek-workflow.md:681-710`).
- The target has four operational screens and payer-specific paths within those workbenches (`outpatient-apotek-screen-and-aggregate-design.md:8-26`).
- The domain defines four aggregate roots: `TelaahResep`, `SalesOrder`, `SalesInvoice`, and `DispenseOrder`; it explicitly says `Outpatient Queue Mapping` is not an Apotek aggregate root (`apotek-domain.md:289-325`).

### 3.2 Backend baseline

- `AntrianStatusEnum` contains only `Waiting`, `InService`, `Done`, and `Withdrawn`, satisfying the ADR enum boundary (`src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianStatusEnum.cs:3-8`).
- `AntrianEntryModel` owns queue timestamps and transitions, but its service-start and cancellation methods carry Admission semantics (`.../AntrianEntryModel.cs:91-138`).
- F-09 appends idempotent `Apotek-Start` and `Apotek-Done` evidence (`src/bilreg/Bilreg.Application/AdmisiContext/AntrianFeature/PharmacyQueueEvidence.cs:9-69`), while the implementation report ties them to sale confirmation and delivery (`docs/contexts/pasien-tracker/tracker-f09-implementation-report.md:22-41`, `85-100`).
- `PenjualanModel` creates a legacy `DU` directly from `Resep`, permits item addition/removal, and has no independent Sales Order or Dispense Order lifecycle (`src/bilreg/Bilreg.Domain/SalesContext/PenjualanFeature/PenjualanModel.cs:56-130`, `185-190`).
- `PenjualanCreateHandler` creates that transaction without a completed target Telaah gate (`src/bilreg/Bilreg.Application/SalesContext/PenjualanFeature/UseCases/PenjualanCreateCmd.cs:41-77`).
- The existing `TelaahModel` is a checklist model without target per-line disposition or lifecycle (`src/bilreg/Bilreg.Domain/SalesContext/TelaahFeature/TelaahModel.cs:9-116`).

### 3.3 Frontend baseline

- The module exposes one outpatient tab, `Apotek Rajal`, rather than four operational screens (`c012_myhospital_web/src/modules/Pharmacy/configs/tabs.ts:5-22`).
- The outpatient view is backed by random and hard-coded mock data (`.../views/ApotekRajal.vue:40-110`, `110-331`).
- Its `QueueItem.status` combines pharmacy progress with queue state using `Open`, `Taken`, `Assigned`, `Prepared`, and `Delivered`; payment is represented as `depositStatus` (`.../types/apotek.ts:1-30`).
- Selecting an open item mutates it to `Taken`, and the screen permits local item removal (`.../views/ApotekRajal.vue:360-393`).
- Primary Call, Deliver, preparation, compounding, and handover controls are inert; handover is gated by deposit status rather than the target clearance and final-review model (`.../components/RajalQueueSidebar.vue:80-97`; `.../components/RajalTransactionDetail.vue:320-360`).
- The only query hook targets an unused `/pharmacy/obat` contract that is not present in the audited backend (`.../queries/obatQueries.ts:1-38`).

## 4. Blocking Architecture Gaps

### BA-01 — Canonical pharmacy queue identity and F-09 coexistence

**Status:** Resolved (2026-08-15)

**Gap.** The accepted design assumes the shared Patient Tracker queue platform, while the closed F-09 implementation uses a separate Farinv queue with its own `Taken → Assigned → Prepared → Delivered` lifecycle and Tracker evidence. Running both creates duplicate queue identities, numbers, displays, and milestone facts.

**Evidence.** Screen design `:28`, `:228-240`; F-09 report `:22-41`, `:85-100`; queue gap analysis `:62-74`.

**Decision.** Patient Tracker `QueueEntry` is the sole canonical outpatient-pharmacy queue identity. F-09 evidence remains reusable but must reference `QueueEntryId` from Patient Tracker. Legacy Farinv queue identity is deprecated and must not create active queue records. Historical Farinv queue data is read-only. No dual-active queue model is allowed.

**Ratified in.** `apotek-domain.md` (`BR-APT-097`); `outpatient-apotek-screen-and-aggregate-design.md` §4.1; `ADR-APT-001`; `TRACKER-DOMAIN.md` (`BR-TRK-051`); `outpatient-apotek-queue-gap-analysis.md`.

### BA-02 — Pharmacy-to-Tracker milestone command contract

**Status:** Resolved (2026-08-15)

**Gap.** The platform has Admission-shaped `start-service` and completion paths. The target requires first preparation to start service without depending on a current Loket claim, and a coordinated pickup action to announce and complete the queue atomically or idempotently.

**Evidence.** `outpatient-apotek-screen-and-aggregate-design.md:228-240`; `outpatient-apotek-workflow.md:681-710`; `AntrianEntryModel.cs:91-138`; queue gap analysis `:50-60`.

**Decision.** BA-02 is considered resolved by ADR-APT-001 and the current Outpatient Apotek domain and workflow decisions. The ownership boundary is already defined: Patient Tracker owns queue lifecycle; Pharmacy owns pharmacy workflow. Queue milestone causation is already defined: first Medication Preparation Started causes Queue `ServedAt` / `InService`; coordinated Pickup Call causes Queue `DoneAt` / `Done`. No further architecture decision is required. Remaining work, if any, is implementation-level integration contract definition (commands, events, API) and must not be treated as a blocking architecture gap.

**Rationale.** ADR-APT-001 and the ratified domain/workflow artifacts already separate queue lifecycle from pharmacy progress and bind Tracker milestones to Apotek-orchestrated facts. Naming concrete commands remains useful implementation guidance but does not block architecture approval.

**Ratified in.** `adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md`; `apotek-domain.md`; `outpatient-apotek-workflow.md`; `outpatient-apotek-screen-and-aggregate-design.md` §4.1.

### BA-03 — `OutpatientQueueMapping` consistency boundary

**Status:** Resolved (2026-08-15)

**Gap.** The canonical domain says mapping is an active relationship and not an aggregate root, while the proposed screen design lists `OutpatientQueueMapping` as an aggregate root. ADR-APT-001 permits several implementation structures.

**Evidence.** `apotek-domain.md:277-279`, `289-325`; screen design `:261-277`; ADR `:115-145`.

**Decision.** `OutpatientQueueMapping` is not an aggregate root. It is a navigation/association mechanism only. It does not own business lifecycle, workflow state, approval state, operational progress, or transactional consistency. It does not maintain active/inactive relationship state. It does not establish an independent consistency boundary. Queue identity and lifecycle remain owned by Patient Tracker. Medication demand lifecycle remains owned by the corresponding Pharmacy aggregates (`Sales Order`, `Dispense Order`, and related roots). The association exists only to answer operational navigation and worklist questions such as: which queue entry is serving this medication demand, and which medication demands are associated with this queue entry.

**Rationale.** Mapping is a read/navigation aid across externally owned queue identity and Pharmacy-owned demand lifecycles. Promoting it to an aggregate or lifecycle-bearing relationship would create a competing consistency boundary without independent business outcomes.

**Ratified in.** `apotek-domain.md`; `outpatient-apotek-screen-and-aggregate-design.md` §5; `adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md`.

### BA-04 — Pickup/handover lifecycle versus projection categories

**Status:** Resolved (2026-08-16)

**Gap.** The domain shows `Ready for Pickup`, `Patient Called`, `Recipient Verified`, `Final Review Completed`, `Education Provided`, and `Handed Over` as a lifecycle, while the screen design says similar names are projection-only categories and Dispense Order states remain authoritative.

**Evidence.** `apotek-domain.md:541-559`; screen design `:151-179`.

**Decision.** Pickup/handover categories are not aggregate lifecycle states. They are projection/worklist categories only. `DispenseOrder` remains the authoritative aggregate lifecycle. Operational milestones such as Patient Called, Final Review Completed, Education Provided, and Medication Handed Over are process facts/events, not aggregate states. Recipient verification is a Pharmacist operational check and is not a process fact, aggregate state, or worklist category (BC-06). Worklist categories (`Ready for Pickup`, `Pickup Expired`, `Ready for Review`, `Ready for Handover`, `Completed`) are derived projections used for operational organization and prioritization.

**Rationale.** A single authoritative lifecycle in `DispenseOrder` avoids contradictory transitions and keeps review failure, multi-order pickup, and post-`Done` handover unambiguous. Serah Obat organization is served by projections derived from aggregate facts and events, not a parallel state machine.

**Ratified in.** `apotek-domain.md`; `outpatient-apotek-screen-and-aggregate-design.md` §3.4; `outpatient-apotek-workflow.md`.

### BA-05 — Legacy DU authority and coexistence with target sales/dispensing

**Status:** Resolved (2026-08-16)

**Gap.** Current `PenjualanModel` remains a mutable monolithic DU-Bill. The target requires independent `SalesOrder`, `SalesInvoice`, and `DispenseOrder` lifecycles with line-level traceability. No source-of-truth or coexistence rule is defined.

**Evidence.** `apotek-domain.md:17-26`, `93-113`; `PenjualanModel.cs:56-130`, `185-190`; `PenjualanCreateCmd.cs:41-77`.

**Decision.** Legacy DU and new Sales Invoice are independent features. Both may operate concurrently during the transition period. No dual-write strategy is permitted. Sales Invoice must not generate, synchronize, transform into, or mirror a Legacy DU. Legacy DU remains owned by the legacy workflow. Sales Invoice remains owned by the new outpatient pharmacy workflow.

**Coexistence strategy.** Legacy DU generates Billing and Stock movements through existing legacy mechanisms. Sales Invoice generates Billing and Stock movements through the new architecture. Stock coexistence is handled by the existing stock-ledger coexistence architecture. Both transaction sources may generate bills into the Tata Rekening bounded context.

**Reporting strategy.** Unified sales reporting shall be provided by a read-only reporting adapter/projection in the new system. The adapter aggregates Legacy DU and Sales Invoice transactions into a unified reporting view without modifying the legacy system. Reporting must remain valid when only Legacy DU transactions exist.

**Rationale.** Independent feature ownership with parallel billing/stock paths avoids dual-write corruption while allowing phased rollout. Unified reporting is a read-side concern and must not couple the transactional models.

**Ratified in.** `apotek-domain.md`; `outpatient-apotek-workflow.md`; coexistence and reporting adapter design (to be specified at implementation).

### BA-06 — Authoritative prescription identity and CPOE/legacy `Resep` bridge

**Status:** Resolved (2026-08-16)

**Gap.** The domain assigns original prescription authority to CPOE or another clinical source, but current sales code loads a legacy `ResepModel`; no canonical prescription identity, version, source-line reference, or external-prescription ownership contract exists.

**Evidence.** `apotek-domain.md:43-55`, `331-339`; workflow `:120-145`, `233-304`; `PenjualanCreateCmd.cs:50-73`.

**Decision.** A canonical Prescription Contract shall be defined and respected by both Legacy Resep and CPOE. The new Outpatient Pharmacy system shall consume the Prescription Contract instead of directly depending on Legacy Resep or CPOE-specific models. Pharmacy operations shall be based on a Prescription Snapshot created from the Prescription Contract at intake time. The snapshot is authoritative for pharmacy operational workflows and lifecycle processing.

**Prescription revision handling.** Source prescription revisions must be detected. Source prescription revisions must not automatically modify existing pharmacy snapshots. No silent synchronization or automatic rewriting of pharmacy operational data is allowed. When a revision is detected, the system shall create an operational review task for pharmacy staff to evaluate and handle the change.

**Rationale.** A shared contract decouples pharmacy from source-specific models while preserving traceability. Snapshot-at-intake makes pharmacy workflows deterministic; explicit revision detection with staff review prevents silent drift from clinical source changes.

**Ratified in.** `apotek-domain.md`; `outpatient-apotek-workflow.md`; Prescription Contract and snapshot design (to be specified at implementation).

### BA-07 — Cross-context delivery, idempotency, and reconciliation

**Status:** Resolved (2026-08-16)

**Gap.** API, command, and event contracts are explicit non-decisions. Existing F-09 evidence is synchronous HTTP without an outbox. The target creates causal chains across Apotek, Tracker, Inventory, Payment, SEP/Fornas, and Tata Rekening.

**Evidence.** Screen design `:317-329`; workflow `:681-712`, `:737`; F-09 report `:217-225`.

**Decision.** The system shall use an Integration Task Table mechanism for cross-context integration. The mechanism must be transactional, retryable, idempotent, and reconcile-able. The goal is reliable cross-context delivery between Pharmacy and dependent bounded contexts such as Billing, Stock, Reporting, and Queue-related integrations. A distributed transaction is not required. A message broker is not required. A transactional outbox is not required.

**Integration approach.** Business transaction and Integration Task creation must be committed atomically. Integration workers process pending tasks asynchronously. Failed tasks must be visible, retryable, and auditable. Processing must be idempotent to prevent duplicate side effects. Reconciliation capability must exist to identify and recover missed or failed integrations.

**Rationale.** An Integration Task Table provides durable at-least-once delivery without mandating distributed transactions, message brokers, or a separate outbox pattern. Atomic task creation with the business transaction preserves consistency; async workers, idempotency, and reconciliation address partial failures.

**Ratified in.** `apotek-domain.md`; `outpatient-apotek-workflow.md`; Integration Task Table design (to be specified at implementation).

### BA-08 — Dispense Authorization and financial/coverage evidence boundary

**Status:** Resolved (2026-08-16)

**Gap.** Prior artifacts mixed `Financial Clearance` and `Fulfillment Clearance` terminology and implied a domain object or transaction boundary for preparation authorization. The target also required BPJS Sales Invoice establishment and Medication Handover to be coordinated without a defined consistency model.

**Evidence.** `apotek-domain.md:265-267`, `320-325`, `381-389`, `425-447`; workflow `:386-539`; screen design `:271-277`.

**Decision.** There is no separate business concept called Financial Clearance or Fulfillment Clearance. What exists is financial and coverage evidence evaluated by Pharmacy policy to determine **Dispense Authorized** — whether medication preparation and dispensing may start.

**Financial and coverage evidence.**

| Payer path | Evidence |
|---|---|
| General Patient | Sales Invoice created; payment completed |
| BPJS | Prescription exists; SEP valid; Fornas coverage valid |
| Other insurance | Coverage approval valid |

**Dispense Authorized.** Dispense Authorized is not an aggregate, entity, source of truth, or transaction boundary. It is a policy evaluation result derived from financial and coverage evidence. It is required before Medication Preparation Started and Dispensing. It is not required for Medication Handover.

**Medication Handover gates.** Medication Handover is governed separately by: Medication Prepared; Final Dispense Review passed; Patient Education Acknowledgement recorded. Authorized Recipient verification is a Pharmacist operational responsibility and is not a system-enforced gate (BC-06). Patient Education is a lightweight acknowledgement of counseling, not a structured counseling-content record (BC-07).

**BPJS invoice and handover.** BPJS Sales Invoice creation and Medication Handover do not require a distributed transaction or special transaction boundary. The BA-07 Integration Task Table architecture remains sufficient for cross-context coordination.

**Rationale.** Preparation authorization is a derived policy outcome over external financial and coverage facts, not a persisted clearance domain object. Separating Dispense Authorized from handover gates avoids conflating payer readiness with physical handover accountability. Cross-context steps remain asynchronously reliable without distributed ACID.

**Ratified in.** `apotek-domain.md`; `outpatient-apotek-workflow.md`; `adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md`.

### BA-09 — Inventory fulfillment contract

**Status:** Resolved (2026-08-16)

**Gap.** Inventory owns reservation, issue, return eligibility, and final disposition, but the current repository has stock-ledger capabilities rather than a defined Dispense Order contract. Reservation timing, partial quantities, Dispensing Temporary Custody, issue-on-handover, and rejected returns are not architecturally connected.

**Evidence.** `apotek-domain.md:116-125`, `366-400`; workflow `:681-695`; screen design `:292-303`.

**Decision.** Stock Ledger remains a pure stock authority and does not own Pharmacy workflow concepts. Pharmacy owns Sales Order, Dispense Order, dispensing lifecycle, `Prepared`, `Handed Over`, and No Show resolution. Stock Ledger owns stock quantity, Mutasi, Remove Stock, and stock movement history only.

**Lifecycle mapping.**

| Pharmacy event | Inventory action |
|---|---|
| Dispensing Started | Mutasi from Pharmacy Unit to Dispensing Temporary Unit |
| Dispensing Completed / `Prepared` | No inventory action |
| Medication Handed Over | Remove Stock from Dispensing Temporary Unit |
| No Show resolution | Mutasi from Dispensing Temporary Unit back to Pharmacy Unit |

**Additional rules.** Reserve equals Mutasi; no separate `ReserveStock` contract. `Prepared` is a Dispense Order state only, reached when required dispensing movements complete; it is not an Inventory state. Partial fulfillment is represented at Sales Order level; a Sales Order may be fulfilled by multiple Dispense Orders. No Show is Pharmacy-owned; Inventory applies only the return movement directed by Pharmacy and never stores No Show status.

**Rationale.** Separating workflow ownership from stock movement prevents duplicate lifecycle truth. Reusing Mutasi avoids a parallel reservation model and aligns with existing stock-ledger coexistence.

**Ratified in.** `apotek-domain.md` (`BR-APT-098`–`BR-APT-104`); `stok-ledger-domain.md` §1.7; `outpatient-apotek-workflow.md`; `adr/ADR-APT-002-pharmacy-stock-ledger-boundary.md`.

## 5. Business Clarification Gaps

### BC-01 — Collection window and manual expiry authority

**Status:** Resolved (2026-08-16)

**Gap.** No numeric collection limit is authoritative; manual closure is allowed, but eligibility criteria and escalation are undefined.

**Evidence.** Workflow `:611-679`, `:697-710`; screen design `:82-93`.

**Decision.** Medication awaiting pickup may remain in Ready for Pickup for a configurable Collection Window (default 7 days). After that period the worklist category becomes Pickup Expired. Ordinary handover shall not proceed while Pickup Expired. Only an authorized pharmacist may record a Collection Window Override and proceed with handover. The override reason must be recorded. Pickup Expired does not by itself expire the Dispense Order or return stock; terminal uncollected resolution remains `WF-APT-RJ-007`.

**Rationale.** A configurable window gives operators a deterministic Ready for Pickup vs Pickup Expired projection without auto-closing fulfillment. Override preserves late handover under pharmacist authority. Terminal No-Show close stays a separate authorized act so collection-window elapsed is not confused with Dispense Order `Expired`.

**Ratified in.** `apotek-domain.md` (`BR-APT-138`–`BR-APT-142`); `outpatient-apotek-workflow.md`; `outpatient-apotek-screen-and-aggregate-design.md` §3.4; `sop/SOP-APT-RJ-003-*` through `sop/SOP-APT-RJ-007-*`.

### BC-02 — Direct Medication Request authority thresholds

**Status:** Resolved (2026-08-15)

**Gap.** Staff may accept, refer, or decline, but the threshold for Pharmacist approval is undefined.

**Evidence.** `apotek-domain.md:217-219`, `441`; screen design `:108-111`, `:317-327`.

**Decision.** Direct Medication Request does not require Pharmacist approval. Pharmacy Staff accepts or declines it directly. It is treated as a retail-style medication request originating outside the hospital care workflow. Pharmacist consultation may occur operationally but is optional SOP guidance only. The model shall not include pharmacist consultation as approval workflow, authority threshold, escalation process, risk classification, domain state, or business-rule gate.

**Rationale.** Direct Medication Request is intentionally outside prescription review and hospital care workflow governance. Modeling optional consultation as a system gate would conflate retail demand intake with Telaah Resep authority and create unnecessary workflow states.

**Ratified in.** `apotek-domain.md` (`BR-APT-009`, `BR-APT-089`); `outpatient-apotek-workflow.md`; `outpatient-apotek-screen-and-aggregate-design.md` §3.2; `sop/SOP-APT-RJ-002-*`.

### BC-03 — Return, correction, expiry, and exception approval thresholds

**Status:** Resolved (2026-08-16)

**Gap.** Supervisor authority is referenced, but ordinary versus exceptional decisions and required approvals are unspecified.

**Evidence.** `apotek-domain.md:233-235`, `391-409`; screen design `:78-119`, `:317-327`.

**Decision.** Exception handling is authority-based rather than amount-threshold-based. Returns, corrections, expired collection overrides, and other dispensing exceptions require authorization by an authorized pharmacist according to operational policy. No monetary approval threshold model is introduced.

**Rationale.** Exception accountability is professional authorization, not a value band. Amount-based escalation would invent an approval model the organization does not operate. Operational policy names which pharmacist is authorized; the system records that authorizing pharmacist, reason, and effective business time.

**Ratified in.** `apotek-domain.md` (`BR-APT-135`–`BR-APT-137`); `outpatient-apotek-workflow.md`; `outpatient-apotek-screen-and-aggregate-design.md` §3.2; `sop/SOP-APT-RJ-003-*`, `sop/SOP-APT-RJ-007-*`.

### BC-04 — Partial Prescription Fulfillment

**Status:** Resolved (2026-08-16)

**Gap.** Partial fulfillment policy between Prescription and Sales Order was undefined: permitted reasons, responsible actors, unfulfilled-line handling, and the boundary versus Sales Order-to-Dispense Order execution splits were not ratified.

**Evidence.** `apotek-domain.md:137`, `417-427`; workflow `:291-296`, `:541-609`; `apotek-domain.md:341-352`.

**Decision.** Partial Prescription Fulfillment is permitted only for Patient Request and Stock Shortage. No other reason is recognized by the system.

**Patient Request.** When a Patient cannot or does not wish to purchase the entire prescription, Pharmacy Staff may establish a Sales Order containing only the selected prescription lines. Excluded prescription lines remain unfulfilled. The system shall support Salinan Resep (Prescription Copy) generation for unfulfilled lines.

**Stock Shortage.** When inventory availability prevents full fulfillment, Pharmacy Staff may establish a Sales Order containing only fulfillable prescription lines. Unavailable prescription lines remain unfulfilled. The system shall support Salinan Resep for unfulfilled lines.

**Ownership.** The Pharmacist remains responsible for approving the resulting fulfillment decision when professional review is required. The system does not automatically determine alternative substitutions or external fulfillment actions.

**Partiality boundary.** Partiality exists only between Prescription and Sales Order. Partiality does not exist between Sales Order and Dispense Order. A Sales Order may be fulfilled by one or more Dispense Orders; that is fulfillment execution, not Partial Prescription Fulfillment policy.

**Unfulfilled lines.** Prescription lines not included in the Sales Order remain part of the originating Prescription. The system shall support issuing Salinan Resep containing unfulfilled lines for external fulfillment when required.

**Rationale.** Two explicit, auditable reasons at the Prescription-to-Sales Order boundary prevent ad hoc line exclusion while separating professional review ownership from staff-operational intake. Salinan Resep preserves traceability for lines fulfilled elsewhere.

**Ratified in.** `apotek-domain.md` (`BR-APT-047`, `BR-APT-054`, `BR-APT-108`–`BR-APT-113`); `outpatient-apotek-workflow.md`; `outpatient-apotek-screen-and-aggregate-design.md` §3.2.

### BC-05 — Unmapped or declined queue disposition

**Status:** Resolved (2026-08-16)

**Gap.** A queue may remain unmapped or a direct request may be declined, but the external withdrawal/closure policy and responsible actor are not defined.

**Evidence.** Workflow `:197-231`; `apotek-domain.md:413-417`.

**Decision.** Queue entries that are not progressed into the pharmacy workflow may be closed directly from the pre-service queue status (decision name TAKEN; canonical Patient Tracker state `Waiting`). A mandatory close reason shall be recorded. Patient Tracker shall set the entry `Withdrawn`. No additional queue state is introduced.

**Rationale.** Closing from pre-service participation ends idle unmapped or declined entries without asserting `In Service` or `Done`. `Withdrawn` already exists for participation that ends before service starts. TAKEN is not added to the Tracker lifecycle. Medication decline remains an Apotek fact; queue identity and terminal queue state remain Tracker-owned.

**Ratified in.** `apotek-domain.md` (`BR-APT-064`, `BR-APT-143`–`BR-APT-145`); `TRACKER-DOMAIN.md` (`BR-TRK-039a`, `BR-TRK-052`); `outpatient-apotek-workflow.md` `WF-APT-RJ-001`; `outpatient-apotek-screen-and-aggregate-design.md` §3.2; `sop/SOP-APT-RJ-001-*`.

### BC-06 — Authorized Recipient verification evidence

**Status:** Resolved (2026-08-16)

**Gap.** Recipient verification is mandatory, but acceptable recipient types, identifiers, proxy authority, and failure handling are not defined.

**Evidence.** `apotek-domain.md:122-129`, `374-379`; workflow `:128-136`.

**Decision.** Authorized recipient verification remains an operational responsibility of the dispensing Pharmacist and is not system-enforced. During Medication Handover, the system may optionally record recipient information (phone number and relationship to the Patient) for reference purposes only. No identity validation, legal relationship verification, document capture, or authorization workflow is required.

**Rationale.** Professional handover accountability stays with the Pharmacist at the counter. Modeling identity proof, proxy authority, or document capture as system gates would invent an authorization workflow the organization does not operate. Optional phone number and relationship are reference notes only and do not prove legal entitlement.

**Ratified in.** `apotek-domain.md` (`BR-APT-037`, `BR-APT-077`, `BR-APT-129`–`BR-APT-131`); `outpatient-apotek-workflow.md`; `outpatient-apotek-screen-and-aggregate-design.md` §3.4; `sop/SOP-APT-RJ-003-*`, `sop/SOP-APT-RJ-004-*`, `sop/SOP-APT-RJ-005-*`, `sop/SOP-APT-RJ-006-*`.

### BC-07 — Minimum Patient Education record

**Status:** Resolved (2026-08-16)

**Gap.** Education is mandatory where applicable, but content, acknowledgement, exceptions, and responsible role are unspecified.

**Evidence.** `apotek-domain.md:122-129`, `428-430`; screen design `:167-179`.

**Decision.** Patient Education is recorded using a lightweight education acknowledgement model. The Pharmacist confirms that medication counseling has been provided before handover. The system records education timestamp and responsible Pharmacist. Detailed counseling notes are optional and only required when the Pharmacist considers additional documentation necessary.

**Rationale.** Acknowledgement with timestamp and responsible Pharmacist is sufficient handover accountability. A structured counseling-content model, medication-specific templates, or mandatory notes would over-specify documentation the organization does not operate. Optional notes remain available when professional judgment requires extra record.

**Ratified in.** `apotek-domain.md` (`BR-APT-077`, `BR-APT-132`–`BR-APT-134`); `outpatient-apotek-workflow.md`; `outpatient-apotek-screen-and-aggregate-design.md` §3.4; `sop/SOP-APT-RJ-003-*`, `sop/SOP-APT-RJ-004-*`, `sop/SOP-APT-RJ-005-*`, `sop/SOP-APT-RJ-006-*`.

### BC-08 — Authorized non-medication invoice components

**Status:** Resolved (2026-08-16)

**Gap.** Sales Invoice Items could include an explicitly authorized non-medication component, but allowed types and pricing authority were undefined. An open exception would recreate manual invoice-item entry prohibited by `BR-APT-083`.

**Evidence.** `apotek-domain.md:387`, `479`; screen design `:34`, `:284-300`.

**Decision.** Adopt the existing legacy sales model. Non-medication components are not represented as free-form invoice items.

**BHP.** BHP is treated as a standard catalog item and may appear as a sales line.

**Charges.** Item-specific charges (for example packaging or compounding fees) are recorded as line-level charges. Transaction-wide adjustments (for example rounding) are recorded as invoice-level charges.

**Model boundary.** No additional invoice component model is introduced.

**Rationale.** Reusing the legacy sales charge structure keeps BHP and fees inside catalog lines and existing charge attachments, so `BR-APT-083` remains intact without a new commercial-component aggregate.

**Ratified in.** `apotek-domain.md` (`BR-APT-024`, `BR-APT-083`, `BR-APT-125`–`BR-APT-128`); `outpatient-apotek-screen-and-aggregate-design.md` §5.2.

### BC-09 — Fulfillment boundary and prescription repeat (Iter) policy

**Status:** Resolved (2026-08-16)

**Gap.** The rule “at most one active Sales Order per Resep per episode” did not define episode boundaries, reopening, or same-day repeat handling. Iter entitlement and Iter validity at fulfillment time were not distinguished.

**Evidence.** `apotek-domain.md:365`, `459`; workflow `:270`; `apotek-domain.md:341-352`.

**Decision.** The system does not introduce a separate Fulfillment Episode concept. For Outpatient Pharmacy, the fulfillment boundary is the Registration Period. A prescription may be reviewed, re-reviewed, and fulfilled while the originating Registration remains active. The Registration acts as the fulfillment boundary.

**Repeat policy (Iter).** Prescription repeat entitlement is owned by the prescription through the existing Legacy Resep `Iter` mechanism. The system remains responsible for Iter allocation, Iter consumption tracking, and remaining Iter calculation.

**Repeat validity authority.** The system does not determine whether an unused Iter remains valid for fulfillment. The Pharmacist is responsible for deciding whether an unused Iter may still be honored at fulfillment time — for example, old prescription, delayed patient return, clinical appropriateness concerns, or operational policy considerations. The Pharmacist may decline fulfillment even when remaining Iter exists.

**Invariant update.** Replace “one active Sales Order per Prescription per Episode” with “one active Sales Order per Prescription per Registration” or equivalent Registration-based terminology.

**Rationale.** Registration is an existing, externally owned boundary with deterministic identity. Separating system-managed Iter entitlement from pharmacist-judged Iter validity preserves clinical accountability without inventing a parallel episode aggregate.

**Ratified in.** `apotek-domain.md` (`BR-APT-011`, `BR-APT-086`, `BR-APT-105`–`BR-APT-107`); `outpatient-apotek-workflow.md`; `sop/SOP-APT-RJ-002-*`.

### BC-10 — Shortage, Backorder, and alternate-stock authority

**Status:** Resolved (2026-08-16)

**Gap.** Staff could choose Backorder or another approved stock source within authority, but approval limits, patient communication, terminal timing, and whether outpatient Pharmacy retained outstanding demand were undefined.

**Evidence.** `apotek-domain.md:227`, `375`, `416`; workflow `:85`, `:105`, `:291-294`, `:372`, `:450`.

**Decision.** Outpatient Pharmacy does not support Backorder. When a stock shortage occurs, the system does not create an outstanding fulfillment obligation, waiting demand, or backorder record.

**Stock Shortage Handling.** Stock shortage is resolved immediately through a Partial Sales Order and Salinan Resep (Prescription Copy) for unfulfilled prescription lines:

```text
Prescription
  -> Available Lines
       -> Sales Order
```

Only fulfillable prescription lines may be included in the Sales Order. Unfulfillable lines remain outside the Sales Order on the originating Prescription.

**Prescription Copy.** The system shall support Salinan Resep generation containing the unfulfilled prescription lines. The Prescription Copy may be used by the Patient to obtain medication from another pharmacy.

**Alternate stock source.** Outpatient Pharmacy does not implement alternate stock source selection, fulfillment routing, inter-pharmacy sourcing, or backorder management. Inventory availability is evaluated against the currently available stock authority.

**Rationale.** The organization does not operationally retain outstanding outpatient medication demand when stock is unavailable. Shortage is resolved immediately through partial fulfillment and Prescription Copy issuance rather than deferred fulfillment.

**Ratified in.** `apotek-domain.md` (`BR-APT-018`, `BR-APT-046`, `BR-APT-110`, `BR-APT-114`–`BR-APT-118`); `outpatient-apotek-workflow.md`; `outpatient-apotek-screen-and-aggregate-design.md` §3.3; `sop/SOP-APT-RJ-002-*`, `sop/SOP-APT-RJ-003-*`, `sop/SOP-APT-RJ-004-*`.

### BC-11 — Pharmacy call purpose and display wording

**Gap.** Mapping calls and pickup calls use the same queue call facility but have different meaning and milestone effects; presentation requirements are unspecified.

**Recommended decision.** Define call-purpose codes and patient-facing announcements for identification/purchase confirmation versus pickup. Only pickup can cause `DoneAt`.

**Rationale.** Operators and patients must distinguish “come for identification/payment discussion” from “medication ready for pickup.”

**Evidence.** `apotek-domain.md:417-419`; workflow `:197-210`, `:335-351`.

### BC-12 — Role and permission matrix

**Gap.** Actor responsibilities are described, but command-level permissions, dual-control cases, delegation, and override policy are absent. The frontend currently grants generic roles to one combined screen.

**Recommended decision.** Ratify a command-level matrix for Pharmacy Staff, Pharmacist, Supervisor, Cashier, and system integration identities, including override reason and audit requirements.

**Rationale.** Screen access is not sufficient authorization for professional decisions, financial corrections, or expiry.

**Evidence.** `apotek-domain.md:207-235`; workflow `:99-112`; frontend tabs `:5-22`.

### BC-13 — Physical prescription capture standard

**Gap.** A physical prescription must be recorded before review, but required source fields, image retention, authenticity evidence, and duplicate detection are undefined.

**Recommended decision.** Define minimum source metadata, line capture, prescriber/facility identity, document image policy, and deduplication key.

**Rationale.** The captured record becomes the immutable clinical source reference for all downstream traceability.

**Evidence.** Workflow `:72-86`, `:120-135`; screen design `:104-106`.

### BC-14 — Fornas non-coverage reclassification and return eligibility

**Status:** Resolved (2026-08-16)

**Gap.** Fornas non-coverage outcomes were ambiguous: whether uncovered BPJS lines stayed on the BPJS path, were cancelled, or became Patient-payable, and how that related to Dispense Authorization, was not ratified.

**Evidence.** Workflow `:464-539`; `apotek-domain.md:480-494`; screen design `:114-117`.

**Decision.** Fornas validation classifies prescription lines as Covered or Not Covered.

**Covered lines.** Covered lines follow the normal BPJS fulfillment workflow. Coverage evidence is sufficient for Dispense Authorized.

**Not Covered lines.** Not Covered lines are not automatically cancelled. The system may establish a separate Patient-Pay Sales Order for the uncovered prescription lines. That Patient-Pay Sales Order is independent from the BPJS-covered Sales Order. Uncovered BPJS lines do not remain in the BPJS fulfillment path.

**Financial clearance.** Patient-Pay Sales Orders require financial clearance before Dispense Authorized is granted. Financial clearance follows the normal self-pay workflow (Payment Clearance and Sales Invoice evidence). It is not a separate Financial Clearance aggregate (BA-08).

**Dispense Authorization.** Each prescription line follows its own authorization path:

- BPJS Covered Line → Coverage Evidence → Dispense Authorized
- Patient-Pay Line → Financial Clearance (self-pay Payment Clearance) → Dispense Authorized

**Partial Prescription Fulfillment.** Creating a separate Patient-Pay Sales Order for uncovered lines is a valid form of Partial Prescription Fulfillment. One originating Prescription may therefore result in a BPJS-covered Sales Order and a Patient-Pay Sales Order for different prescription lines.

**Rationale.** Line-level Fornas classification with independent Sales Orders keeps BPJS and self-pay commercial paths from sharing one mixed Sales Order, while still allowing coordinated pickup of separately authorized quantities.

**Ratified in.** `apotek-domain.md` (`BR-APT-011`, `BR-APT-091`–`BR-APT-094`, `BR-APT-108`, `BR-APT-119`–`BR-APT-124`); `outpatient-apotek-workflow.md` (`WF-APT-RJ-005`); `outpatient-apotek-screen-and-aggregate-design.md` §3.2; `sop/SOP-APT-RJ-005-*`.

## 6. Existing Capability Extensions

| ID | Existing capability | Gap requiring extension | Evidence |
|---|---|---|---|
| EC-01 | Generic queue lifecycle and timestamps | Add pharmacy service-point use without changing `AntrianStatusEnum`; support pharmacy-origin start/complete commands. | `AntrianStatusEnum.cs:3-8`; `AntrianEntryModel.cs:39-111`; ADR `:55-79` |
| EC-02 | Queue call/display, session, workstation, and Loket infrastructure | Add pharmacy configuration, call-purpose semantics, and pickup announcement/completion behavior without Admission outcomes. | Queue gap analysis `:26-41`, `:47-60` |
| EC-03 | Row-version concurrency, polling, invalidation, and conflict recovery in Admission queue clients | Extract platform contracts and apply them to pharmacy projections and orchestration. | Queue gap analysis `:28-40` |
| EC-04 | Idempotent F-09 `Apotek-*` evidence append | Retarget evidence to preparation-start and pickup-call causation; add durable delivery and canonical queue-entry references. | `PharmacyQueueEvidence.cs:9-69`; F-09 report `:217-225` |
| EC-05 | CPOE, legacy `Resep`, medication catalog, and goods references | Supply immutable prescription-source and catalog contracts suitable for Telaah and Sales Order line traceability. | Domain `:43-55`; backend legacy sales files |
| EC-06 | Fornas master-data access and existing patient/registration data | Add valid-SEP plus item/quantity coverage evidence; master-data presence alone is not Coverage Clearance. | Domain `:106-108`, `442-446`; workflow `:386-539` |
| EC-07 | Stock Ledger movement and return consequence capabilities | Add Mutasi to/from Dispensing Temporary Unit and Remove Stock on handover keyed to Dispense Order Lines; no separate reservation contract. | Domain `:366-400`; workflow `:681-695`; ADR-APT-002 |
| EC-08 | Tata Rekening, payment, tariff, and financial-adjustment capabilities | Add medication Financial Charge lineage, Payment Clearance consumption, BPJS/general invoice timing, credit/refund correlation, and pricing snapshots. | Domain `:247-267`, `354-389`; workflow `:306-539` |
| EC-09 | Vue module shell, TanStack Query/Zod patterns, Admission queue client patterns, shared UI blocks | Reuse visual/query primitives behind Apotek-owned DTOs and role-specific workbenches; do not reuse Admission commands or current mock state model. | Frontend tabs `:5-22`; `obatQueries.ts:1-38`; queue gap analysis `:26-41` |

## 7. Missing Implementation

| ID | Missing capability | Repository finding | Target evidence |
|---|---|---|---|
| MI-01 | Target `TelaahResep` aggregate, persistence, API, and tests | Existing checklist model has no target line dispositions, lifecycle, completion outcome, or application/persistence surface. | Domain `:239-241`, `291-295`, `329-339`, `451-461` |
| MI-02 | `SalesOrder` aggregate and quantitative reconciliation | No target type, table, handler, API, or tests. | Domain `:243-245`, `297-303`, `341-352`, `463-479` |
| MI-03 | `SalesInvoice` aggregate and payer-specific invoicing | Only legacy DU exists; no target invoice timing, pricing snapshot, mixed payer, credit-note, or financial-charge lineage. | Domain `:247-259`, `305-311`, `354-364`, `481-497` |
| MI-04 | `DispenseOrder` aggregate and immutable final-review records | No target type, state machine, preparation, review, dispense, handover, expiry, or return implementation. | Domain `:261-287`, `313-318`, `366-379`, `499-518` |
| MI-05 | Outpatient queue mapping and per-demand progress projection | No mapping relation/table/API; current queue entry has one `ReffId`/`ReffDesc`, and frontend has one transaction per queue card. | Domain `:145-149`, `277-279`, `531-539`; workflow `:161-231` |
| MI-06 | Dispense Authorized policy evaluation over financial/coverage evidence | No payer-path evidence correlation or preparation authorization policy exists in implementation. | Domain `:265-267`, `381-389`; screen design `:271-290`; BA-08 |
| MI-07 | Outpatient pharmacy service-point intake and configuration | Shared infrastructure exists, but no demonstrated pharmacy intake contract, seed/configuration, or kiosk path. | Workflow `:114-118`; screen design `:28`, `228-240` |
| MI-08 | Four operational outpatient screens | Only one mock `Apotek Rajal` tab exists; Telaah Resep, Pelayanan Penjualan, Dispensing, and Serah Obat workbenches are absent. | Screen design `:8-19`, `41-179`; frontend tabs `:5-22` |
| MI-09 | Exception Worklist, attention projections, and Patient Medication Journey | No read models or UI exist. | Screen design `:21-26`, `78-93`, `181-226` |
| MI-10 | General, BPJS, mixed-coverage, multi-demand, and no-show workflows | `WF-APT-RJ-001`–`007` have no conforming backend orchestration or frontend workflow. | Workflow `:147-679` |
| MI-11 | Cross-context integrations | No target Apotek integration with CPOE source facts, Tracker milestones, Payment Clearance, SEP/Fornas Coverage Clearance, Inventory fulfillment, Tata Rekening consequences, or EMR realization projection. | Workflow `:681-695` |
| MI-12 | Pharmacy API contracts and validated frontend DTOs/mutations | Contracts are explicitly undefined; frontend has no workflow queries/mutations and one unused catalog stub. | Screen design `:317-329`; frontend query `:1-38` |
| MI-13 | Command-level authorization, audit, and operational observability | Actor roles are not enforced on target commands; no Apotek metrics, reconciliation status, or failure worklist exists. | Domain `:207-235`, `403-409`; workflow `:99-112` |
| MI-14 | Verification suite | No aggregate, workflow, API-contract, integration, or frontend tests cover `BR-APT-*` or `WF-APT-RJ-*`. | Domain `:327-447`; workflow `:714-726` |
| MI-15 | Current end-user and technical documentation alignment | Frontend module documentation remains placeholder/legacy and does not describe the canonical outpatient model. | Canonical screen/workflow artifacts versus `c012_myhospital_web/docs/modules/pharmacy/` |

## 8. Technical Debt

| ID | Debt | Impact | Evidence |
|---|---|---|---|
| TD-01 | Monolithic mock `Apotek Rajal` frontend | Suggests one queue transaction owns sale, preparation, and handover; obscures the required four responsibility boundaries. | `ApotekRajal.vue:110-421` |
| TD-02 | Legacy pharmacy states stored as queue status | Directly violates ADR-APT-001 and can make Tracker authoritative for pharmacy progress. | `types/apotek.ts:1-30`; ADR `:55-79` |
| TD-03 | Order Deposit model used as payment/fulfillment gate | Cannot represent Sales Invoice, Payment Clearance, BPJS no-payment policy, or mixed coverage. | `types/apotek.ts:3-30`; `RajalTransactionDetail.vue:348-360` |
| TD-04 | Inert action controls, random mock values, and local item deletion | Creates false UI completeness and permits behavior prohibited by source-line-only invoicing. | `ApotekRajal.vue:40-110`, `377-393`; queue/detail components |
| TD-05 | Mutable monolithic legacy `PenjualanModel` / DU | Combines commercial and fulfillment concerns and permits independent item mutation. | `PenjualanModel.cs:56-130`, `185-190` |
| TD-06 | Dead-end checklist `TelaahModel` | Name overlaps the canonical aggregate but semantics and persistence are incomplete, increasing accidental reuse risk. | `TelaahModel.cs:9-116` |
| TD-07 | F-09 milestone semantics and synchronous evidence delivery | Sale confirmation currently means start; delivery means done; failures can diverge Farinv and Bilreg. | F-09 report `:22-41`, `:85-100`, `:217-225` |
| TD-08 | Admission-specific queue vocabulary and behavior | `start-service`, registration completion/cancellation, DTO enrichment, and client names encourage unsafe direct reuse. | `AntrianEntryModel.cs:125-138`; queue gap analysis `:26-60` |
| TD-09 | Unused `/pharmacy/obat` query stub and nonconforming comments | Assumes a backend endpoint not found in the audited API and is not used by the module. | `obatQueries.ts:1-38` |
| TD-10 | Stale/legacy Pharmacy documentation and ADR artifact hygiene | Legacy SOP/state names conflict with canonical design; ADR filename has `queu`, contains conversational preamble/postscript, and uses informal pharmacy state names. | ADR `:1-5`, `:15-24`, `:265-271`; `c012_myhospital_web/docs/modules/pharmacy/` |
| TD-11 | Missing or skipped legacy integration verification | Existing legacy persistence cannot serve as a trusted migration baseline, and no target regression suite exists. | Current backend test inventory and F-09 report `:186-195` |

## 9. Architecture review conclusions

### 9.1 Decisions already settled

- Patient Tracker `QueueEntry` is the sole canonical outpatient-pharmacy queue identity; legacy Farinv queue is read-only and must not create active records (BA-01).
- Patient Tracker owns queue lifecycle; Pharmacy owns pharmacy workflow. First Medication Preparation Started causes Queue `ServedAt` / `InService`; coordinated Pickup Call causes Queue `DoneAt` / `Done`. No further architecture decision is required for the Pharmacy-to-Tracker milestone contract (BA-02).
- `OutpatientQueueMapping` is a navigation/association mechanism only, not an aggregate root. It does not own lifecycle, workflow state, or transactional consistency. Queue identity remains in Patient Tracker; medication demand lifecycle remains in Pharmacy aggregates (BA-03).
- Pickup/handover categories are projection/worklist categories only, not aggregate lifecycle states. `DispenseOrder` remains authoritative; operational milestones are process facts/events; worklist categories (`Ready for Pickup`, `Pickup Expired`, `Ready for Review`, `Ready for Handover`, `Completed`) are derived projections. Recipient verification is a Pharmacist operational check, not a process fact or worklist category (BA-04, BC-06).
- Legacy DU and Sales Invoice are independent features that may coexist during transition with no dual-write. Each owns its own workflow and billing/stock paths; unified sales reporting is a read-only adapter aggregating both sources (BA-05).
- A canonical Prescription Contract is shared by Legacy Resep and CPOE; pharmacy consumes the contract via a Prescription Snapshot at intake. Source revisions are detected but do not auto-modify snapshots; staff review tasks handle changes (BA-06).
- Cross-context integration uses an Integration Task Table: transactional, retryable, idempotent, and reconcile-able. Business transaction and task creation commit atomically; workers process asynchronously. No distributed transaction, message broker, or transactional outbox is required (BA-07).
- There is no Financial Clearance or Fulfillment Clearance domain object. Pharmacy policy evaluates financial/coverage evidence to determine Dispense Authorized before preparation and dispensing; handover uses separate gates. BPJS invoice and handover coordination uses the Integration Task Table without a distributed transaction (BA-08).
- Stock Ledger owns stock quantity and movements only; Pharmacy owns dispensing lifecycle. Reserve is Mutasi to Dispensing Temporary Unit; handover removes stock; No Show return is Mutasi back to Pharmacy Unit. `Prepared` and partial fulfillment semantics belong to Pharmacy aggregates (BA-09).
- Keep queue lifecycle generic: `Waiting`, `InService`, `Done`, `Withdrawn`.
- Keep pharmacy operational state out of Patient Tracker.
- Treat `ServedAt` as first preparation-start evidence.
- Treat `DoneAt` as coordinated pickup-call evidence, not handover.
- Keep Sales Invoice and Dispense Order independent and coordinated through Sales Order Lines.
- Do not create manual medication invoice items.
- Establish the current BPJS Sales Invoice only with successful handover.
- Keep Final Dispense Review attempts immutable; failure returns only the affected Dispense Order to `Preparing`.
- Preserve separate records for multiple medication demands sharing one queue.
- Direct Medication Request is accepted or declined by Pharmacy Staff without Pharmacist approval; optional consultation is SOP-only and not a domain gate (BC-02).
- Outpatient Pharmacy fulfillment boundary is the active Registration Period; no separate Fulfillment Episode concept. At most one active Sales Order per Prescription per Registration, except that Fornas Not Covered lines may establish a separate Patient-Pay Sales Order independent of the BPJS-covered Sales Order. Iter entitlement is system-managed through Legacy Resep `Iter`; Pharmacist decides whether unused Iter may be honored at fulfillment time and may decline even when remaining Iter exists (BC-09, BC-14).
- Partial Prescription Fulfillment is permitted for Patient Request, Stock Shortage, and Fornas Not Covered lines at the Prescription-to-Sales Order boundary. Excluded or uncovered lines remain on the originating Prescription or move to a separate Patient-Pay Sales Order; Salinan Resep supports external fulfillment when lines stay unfulfilled. Pharmacist approves when professional review is required. Multiple Dispense Orders per Sales Order is execution only, not this policy (BC-04, BC-14).
- Outpatient Pharmacy does not support Backorder, alternate stock source selection, fulfillment routing, or inter-pharmacy sourcing. Shortage is resolved immediately by placing only fulfillable lines on the Sales Order and issuing Salinan Resep for unfulfilled lines; no outstanding fulfillment obligation is retained (BC-10).
- Fornas classifies lines as Covered or Not Covered. Covered lines follow BPJS fulfillment with coverage evidence sufficient for Dispense Authorized. Not Covered lines are not auto-cancelled; they may form an independent Patient-Pay Sales Order requiring self-pay financial clearance before Dispense Authorized. One Prescription may yield both a BPJS-covered Sales Order and a Patient-Pay Sales Order (BC-14).
- Invoice commercial structure follows the legacy sales model. Non-medication components are not free-form invoice items. BHP is a catalog sales line. Item-specific charges (packaging, compounding) are line-level charges; transaction-wide adjustments (rounding) are invoice-level charges. No additional invoice component model is introduced (BC-08).
- Authorized Recipient verification is an operational Pharmacist responsibility and is not system-enforced. Medication Handover may optionally record recipient phone number and relationship for reference only. The system shall not require identity validation, legal relationship verification, document capture, or an authorization workflow (BC-06).
- Patient Education is a lightweight acknowledgement that medication counseling was provided before handover. The system records education timestamp and responsible Pharmacist. Detailed counseling notes are optional and recorded only when the Pharmacist considers additional documentation necessary (BC-07).
- Exception handling is authority-based. Returns, corrections, expired collection overrides, and other dispensing exceptions require authorization by an authorized pharmacist according to operational policy. No monetary approval threshold model is introduced (BC-03).
- Medication awaiting pickup may remain Ready for Pickup for a configurable Collection Window (default 7 days), then becomes Pickup Expired. Ordinary handover is blocked until an authorized pharmacist records a Collection Window Override with reason. Pickup Expired does not expire the Dispense Order; terminal uncollected close remains `WF-APT-RJ-007` (BC-01).
- Queue entries not progressed into the pharmacy workflow may be closed from Waiting (decision TAKEN) with a mandatory close reason. Tracker sets Withdrawn. No additional queue state is introduced. Close is not available after In Service (BC-05).

### 9.2 Decisions still required before architecture approval

Architecture approval requires business ratification of BC-11 through BC-13. BA-01 through BA-09, BC-01 through BC-10, and BC-14 are resolved. These are decision gates, not delivery steps. The remaining Existing Capability Extension, Missing Implementation, and Technical Debt findings can then be evaluated against those ratified boundaries without inventing new sources of truth.

### 9.3 Overall classification

The repository is **architecture-partially-aligned but implementation-absent**:

- aligned: queue enum boundary and several reusable platform capabilities;
- conflicting: legacy DU, F-09 milestone semantics, and current frontend queue/payment model;
- unresolved: operational policy thresholds and remaining implementation against ratified boundaries;
- missing: all target Apotek aggregates, workflows, screens, projections, integrations, and verification.
