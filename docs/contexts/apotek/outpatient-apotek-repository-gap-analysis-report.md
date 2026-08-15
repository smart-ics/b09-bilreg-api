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

The architecture is not ready for implementation review until the blocking decisions in section 4 are resolved. The most material are canonical queue identity, legacy DU coexistence, aggregate ownership for queue mapping, cross-context transaction semantics, and the contracts for Tracker, Inventory, Payment, SEP/Fornas, and Tata Rekening.

## 2. Classification summary

| Classification | Count | Review meaning |
|---|---:|---|
| Blocking Architecture Gap | 7 | A structural or ownership decision is unresolved; implementing around it would create incompatible sources of truth or unsafe cross-context behavior. |
| Business Clarification Gap | 13 | A policy, authority, threshold, or accountable outcome is not sufficiently defined. |
| Resolved (Business Clarification) | 1 | BC-02 ratified; artifacts updated. |
| Existing Capability Extension | 9 | A relevant capability exists but its present contract or semantics do not satisfy Apotek. |
| Missing Implementation | 15 | The design is sufficiently clear, but no conforming implementation exists. |
| Technical Debt | 11 | Existing code or documentation embodies legacy, misleading, coupled, or unverified behavior. |
| Resolved (Blocking Architecture) | 2 | BA-01 and BA-02 ratified; artifacts updated. |
| **Total open** | **55** | Each open finding has one primary classification. |

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

**Gap.** The canonical domain says mapping is an active relationship and not an aggregate root, while the proposed screen design lists `OutpatientQueueMapping` as an aggregate root. ADR-APT-001 permits several implementation structures.

**Evidence.** `apotek-domain.md:277-279`, `289-325`; screen design `:261-277`; ADR `:115-145`.

**Recommended decision.** Ratify the canonical domain: model mapping as a uniquely constrained Apotek-owned relationship/entity and projection input, not an aggregate root. Commands must update the active association in place, enforce queue-entry/source uniqueness and cardinality, and remain transactionally independent from Tracker.

**Rationale.** Mapping has no independent lifecycle or history requirement under `BR-APT-062`; promoting it to an aggregate introduces an unnecessary consistency boundary and contradicts the source of authority.

### BA-04 — Pickup/handover lifecycle versus projection categories

**Gap.** The domain shows `Ready for Pickup`, `Patient Called`, `Recipient Verified`, `Final Review Completed`, `Education Provided`, and `Handed Over` as a lifecycle, while the screen design says similar names are projection-only categories and Dispense Order states remain authoritative.

**Evidence.** `apotek-domain.md:541-559`; screen design `:151-179`.

**Recommended decision.** Persist only aggregate facts and canonical Dispense Order state. Derive Serah Obat categories from preparation, pickup-call, recipient-verification, review, education, dispense, and handover facts. Amend the domain diagram to label it a process/fact sequence rather than an additional state machine.

**Rationale.** Persisting both lifecycles creates contradictory state transitions and makes review failure, multi-order pickup, and post-`Done` handover ambiguous.

### BA-05 — Legacy DU authority and coexistence with target sales/dispensing

**Gap.** Current `PenjualanModel` remains a mutable monolithic DU-Bill. The target requires independent `SalesOrder`, `SalesInvoice`, and `DispenseOrder` lifecycles with line-level traceability. No source-of-truth or coexistence rule is defined.

**Evidence.** `apotek-domain.md:17-26`, `93-113`; `PenjualanModel.cs:56-130`, `185-190`; `PenjualanCreateCmd.cs:41-77`.

**Recommended decision.** Make target Apotek aggregates authoritative for all new outpatient flows. Define legacy DU as a compatibility representation generated from target facts or as read-only historical data; do not allow target and DU commands to mutate the same episode independently. Define stable lineage from legacy DU IDs to Sales Invoice and Dispense Order facts.

**Rationale.** Dual write authority would violate quantity reconciliation, invoice timing, no-manual-item rules, and correction history.

### BA-06 — Authoritative prescription identity and CPOE/legacy `Resep` bridge

**Gap.** The domain assigns original prescription authority to CPOE or another clinical source, but current sales code loads a legacy `ResepModel`; no canonical prescription identity, version, source-line reference, or external-prescription ownership contract exists.

**Evidence.** `apotek-domain.md:43-55`, `331-339`; workflow `:120-145`, `233-304`; `PenjualanCreateCmd.cs:50-73`.

**Recommended decision.** Define an immutable `PrescriptionSourceRef` contract containing authority, source ID/version, patient/encounter, source-line IDs, and source type. Apotek stores references and snapshots required for review but never mutates the source. Legacy `Resep` must be adapted to the same contract; physical prescriptions require an accountable Apotek intake record before review.

**Rationale.** Without a stable source contract, line-level traceability, one-active-order enforcement, re-review, and substitute provenance cannot be guaranteed.

### BA-07 — Cross-context delivery, idempotency, and reconciliation

**Gap.** API, command, and event contracts are explicit non-decisions. Existing F-09 evidence is synchronous HTTP without an outbox. The target creates causal chains across Apotek, Tracker, Inventory, Payment, SEP/Fornas, and Tata Rekening.

**Evidence.** Screen design `:317-329`; workflow `:681-712`, `:737`; F-09 report `:217-225`.

**Recommended decision.** Adopt durable at-least-once integration with stable event IDs, causation/correlation IDs, aggregate versions, idempotent consumers, retry policy, and reconciliation queries. Local aggregate commit and outbound event publication must be atomic through an outbox or equivalent durable mechanism. External facts are referenced, not copied as authority.

**Rationale.** Partial failures otherwise produce queue, financial, stock, and handover histories that disagree while each local transaction appears successful.

### BA-08 — Financial clearance and BPJS handover transaction boundary

**Gap.** `Fulfillment Clearance` is a domain object but not an aggregate; the screen design calls it a derived policy outcome. The target also requires BPJS Sales Invoice establishment and handover to form one accountable outcome across Apotek and external financial authority, but no consistency or compensation boundary is defined.

**Evidence.** `apotek-domain.md:265-267`, `320-325`, `381-389`, `425-447`; workflow `:386-539`; screen design `:271-277`.

**Recommended decision.** Keep `FulfillmentClearance` as an immutable, quantity-scoped decision record owned within Dispense Order orchestration, derived from versioned Payment/Coverage evidence. Keep Sales Invoice and Dispense Order as separate aggregates. Model BPJS “one accountable outcome” as an Apotek process transaction with idempotent steps and explicit compensation/reconciliation, not a distributed ACID transaction.

**Rationale.** Clearance needs auditable evidence without becoming a competing source of payment or coverage truth; cross-context atomicity cannot be assumed.

### BA-09 — Inventory fulfillment contract

**Gap.** Inventory owns reservation, issue, return eligibility, and final disposition, but the current repository has stock-ledger capabilities rather than a defined Dispense Order contract. Reservation timing, partial quantities, in-transit custody, issue-on-handover, and rejected returns are not architecturally connected.

**Evidence.** `apotek-domain.md:116-125`, `366-400`; workflow `:681-695`; screen design `:292-303`.

**Recommended decision.** Define quantity- and lot-aware inventory commands/results keyed by Dispense Order Line: reserve, release, issue, request return disposition, and record final disposition. Inventory remains authoritative; Apotek stores references and blocks/advances its lifecycle only from acknowledged outcomes. Every command must be idempotent and support partial results.

**Rationale.** A generic stock movement without fulfillment lineage cannot prove that the correct accepted quantity was reserved, handed over, returned, or left unresolved.

## 5. Business Clarification Gaps

### BC-01 — Collection window and manual expiry authority

**Gap.** No numeric collection limit is authoritative; manual closure is allowed, but eligibility criteria and escalation are undefined.

**Recommended decision.** Preserve manual closure for the current scope. Define who may close, required reason/evidence, minimum warnings or contact attempts if any, effective-time rules, and whether the policy varies by payer or medication class.

**Rationale.** The system can remain policy-neutral on duration while still requiring a reproducible, auditable supervisor decision.

**Evidence.** Workflow `:611-679`, `:697-710`; screen design `:82-93`.

### BC-02 — Direct Medication Request authority thresholds

**Status:** Resolved (2026-08-15)

**Gap.** Staff may accept, refer, or decline, but the threshold for Pharmacist approval is undefined.

**Evidence.** `apotek-domain.md:217-219`, `441`; screen design `:108-111`, `:317-327`.

**Decision.** Direct Medication Request does not require Pharmacist approval. Pharmacy Staff accepts or declines it directly. It is treated as a retail-style medication request originating outside the hospital care workflow. Pharmacist consultation may occur operationally but is optional SOP guidance only. The model shall not include pharmacist consultation as approval workflow, authority threshold, escalation process, risk classification, domain state, or business-rule gate.

**Rationale.** Direct Medication Request is intentionally outside prescription review and hospital care workflow governance. Modeling optional consultation as a system gate would conflate retail demand intake with Telaah Resep authority and create unnecessary workflow states.

**Ratified in.** `apotek-domain.md` (`BR-APT-009`, `BR-APT-089`); `outpatient-apotek-workflow.md`; `outpatient-apotek-screen-and-aggregate-design.md` §3.2; `sop/SOP-APT-RJ-002-*`.

### BC-03 — Return, correction, expiry, and exception approval thresholds

**Gap.** Supervisor authority is referenced, but ordinary versus exceptional decisions and required approvals are unspecified.

**Recommended decision.** Define a decision matrix by lifecycle stage, payment state, handover state, medication/lot eligibility, quantity/value, and requested outcome.

**Rationale.** The same “return” label can imply inventory disposition, financial adjustment, dispense cancellation, or post-handover correction with different authorities.

**Evidence.** `apotek-domain.md:233-235`, `391-409`; screen design `:78-119`, `:317-327`.

### BC-04 — Partial pickup when one demand remains unresolved

**Gap.** The workflow permits a partial path only when accepted by the Patient and permitted by payer workflow, but it does not state which payer paths permit it or how consent is evidenced.

**Recommended decision.** Define partial-pickup eligibility separately for General, BPJS, and mixed coverage, including consent evidence, communication obligations, and effect on later pickup calls.

**Rationale.** Without this policy, coordinated pickup readiness and `DoneAt` are indeterminate for multi-demand queues.

**Evidence.** Workflow `:541-609`, especially `:580-587`.

### BC-05 — Unmapped or declined queue disposition

**Gap.** A queue may remain unmapped or a direct request may be declined, but the external withdrawal/closure policy and responsible actor are not defined.

**Recommended decision.** Define explicit outcomes for unresolved identity, no eligible demand, duplicate entry, patient abandonment, and declined direct request. Keep the medication decision in Apotek and queue withdrawal in Tracker.

**Rationale.** Leaving entries indefinitely `Waiting` damages queue operations; silently completing them would assert false service.

**Evidence.** Workflow `:197-231`; `apotek-domain.md:413-417`.

### BC-06 — Authorized Recipient verification evidence

**Gap.** Recipient verification is mandatory, but acceptable recipient types, identifiers, proxy authority, and failure handling are not defined.

**Recommended decision.** Define required fields and validation by recipient type, including relationship/authority evidence and refusal/failure outcomes.

**Rationale.** A handover timestamp alone does not prove accountable transfer.

**Evidence.** `apotek-domain.md:122-129`, `374-379`; workflow `:128-136`.

### BC-07 — Minimum Patient Education record

**Gap.** Education is mandatory where applicable, but content, acknowledgement, exceptions, and responsible role are unspecified.

**Recommended decision.** Define a minimum structured acknowledgement plus optional notes, with explicit exceptions and responsible Pharmacist identity.

**Rationale.** A generic boolean cannot support medication-specific accountability or explain why education was omitted.

**Evidence.** `apotek-domain.md:122-129`, `428-430`; screen design `:167-179`.

### BC-08 — Authorized non-medication invoice components

**Gap.** Sales Invoice Items may include an explicitly authorized non-medication component, but the allowed component types and pricing authority are undefined.

**Recommended decision.** Enumerate allowed service/packaging components and their pricing source; reject all others by default.

**Rationale.** An open exception recreates manual invoice-item entry prohibited by `BR-APT-083`.

**Evidence.** `apotek-domain.md:354-364`, `435`.

### BC-09 — Fulfillment episode identity

**Gap.** The rule “at most one active Sales Order per Resep per episode” does not define episode boundaries, reopening, or same-day repeat handling.

**Recommended decision.** Define episode identity from patient, encounter/registration, prescription source/version, care setting, and an explicit reopen/re-review relation.

**Rationale.** This invariant cannot be enforced without a deterministic business key.

**Evidence.** `apotek-domain.md:341-352`.

### BC-10 — Shortage, Backorder, and alternate-stock authority

**Gap.** Staff may choose Backorder or another approved stock source within authority, but approval limits, patient communication, and terminal timing are undefined.

**Recommended decision.** Define eligible stock sources, authority by medication/quantity, when Backorder becomes terminal, and required patient/payer communication.

**Rationale.** These choices affect fulfillment completion, stock custody, and financial consequences.

**Evidence.** `apotek-domain.md:217-219`, `388-400`; workflow `:284-288`, `:362-368`.

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

**Gap.** The workflow allows uncovered BPJS quantities to become Patient-payable and delegates return eligibility to Inventory, but the authoritative decision, consent, and rejection outcomes are not fully specified.

**Recommended decision.** Require versioned Fornas evidence, explicit Patient confirmation before Patient-payable reclassification, and a finite set of Inventory dispositions when Return to Stock is rejected.

**Rationale.** Coverage and return decisions change financial and fulfillment outcomes and cannot be represented as free text.

**Evidence.** Workflow `:464-539`, `:640-664`; `apotek-domain.md:398-400`, `442-446`.

## 6. Existing Capability Extensions

| ID | Existing capability | Gap requiring extension | Evidence |
|---|---|---|---|
| EC-01 | Generic queue lifecycle and timestamps | Add pharmacy service-point use without changing `AntrianStatusEnum`; support pharmacy-origin start/complete commands. | `AntrianStatusEnum.cs:3-8`; `AntrianEntryModel.cs:39-111`; ADR `:55-79` |
| EC-02 | Queue call/display, session, workstation, and Loket infrastructure | Add pharmacy configuration, call-purpose semantics, and pickup announcement/completion behavior without Admission outcomes. | Queue gap analysis `:26-41`, `:47-60` |
| EC-03 | Row-version concurrency, polling, invalidation, and conflict recovery in Admission queue clients | Extract platform contracts and apply them to pharmacy projections and orchestration. | Queue gap analysis `:28-40` |
| EC-04 | Idempotent F-09 `Apotek-*` evidence append | Retarget evidence to preparation-start and pickup-call causation; add durable delivery and canonical queue-entry references. | `PharmacyQueueEvidence.cs:9-69`; F-09 report `:217-225` |
| EC-05 | CPOE, legacy `Resep`, medication catalog, and goods references | Supply immutable prescription-source and catalog contracts suitable for Telaah and Sales Order line traceability. | Domain `:43-55`; backend legacy sales files |
| EC-06 | Fornas master-data access and existing patient/registration data | Add valid-SEP plus item/quantity coverage evidence; master-data presence alone is not Coverage Clearance. | Domain `:106-108`, `442-446`; workflow `:386-539` |
| EC-07 | Stock Ledger movement and return consequence capabilities | Add reservation, release, dispense issue, partial fulfillment, and return-disposition contracts keyed to Dispense Order Lines. | Domain `:366-400`; workflow `:681-695` |
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
| MI-06 | Quantity-scoped Fulfillment Clearance decision record | No Payment/Coverage evidence correlation or preparation authorization exists. | Domain `:265-267`, `381-389`; screen design `:271-290` |
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

### 9.2 Decisions still required before architecture approval

Architecture approval requires explicit disposition of BA-03 through BA-09 and business ratification of BC-01 and BC-03 through BC-14. BA-01, BA-02, and BC-02 are resolved. These are decision gates, not delivery steps. The remaining Existing Capability Extension, Missing Implementation, and Technical Debt findings can then be evaluated against those ratified boundaries without inventing new sources of truth.

### 9.3 Overall classification

The repository is **architecture-partially-aligned but implementation-absent**:

- aligned: queue enum boundary and several reusable platform capabilities;
- conflicting: legacy DU, F-09 milestone semantics, and current frontend queue/payment model;
- unresolved: identity, aggregate relationship, integration reliability, financial and inventory boundaries, and operational policy thresholds;
- missing: all target Apotek aggregates, workflows, screens, projections, integrations, and verification.
