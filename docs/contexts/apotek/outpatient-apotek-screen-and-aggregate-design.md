# Outpatient Apotek Screen and Aggregate Design

**Artifact status:** Proposed design decision  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`)  
**Scope:** Outpatient pharmacy  
**Related artifacts:** [Apotek Domain](./apotek-domain.md), [Outpatient Apotek Workflow](./outpatient-apotek-workflow.md), [Outpatient Apotek SOP](./sop/DAFTAR-SOP-APT-RJ.md), [Outpatient Apotek Repository Gap Analysis](./outpatient-apotek-repository-gap-analysis-report.md) (BA-03), [Outpatient Apotek Persistence Design](./outpatient-apotek-persistence-design.md)

## 1. Decision Summary

Outpatient Apotek will use **four operational screens**. Each screen has a **worklist** for selecting work and a **workbench** for completing accountable actions.

The screens are organized by operational role and business responsibility, rather than by one screen per SOP. The General Patient, BPJS, and mixed-coverage workflows are payer-specific paths inside the same operational workbenches.

| Screen | Primary role | Worklist | Workbench outcome |
|---|---|---|---|
| Telaah Resep | Pharmacist | Outpatient electronic prescriptions and recorded external prescriptions | Professional review result and Sales Order establishment |
| Pelayanan Penjualan | Pharmacy Staff | Waiting outpatient pharmacy queue entries, with direct lookup for external demand; Exception Worklist for operational resolution | Queue mapping, direct-demand acceptance, payer handling, applicable sale establishment, and exception resolution |
| Dispensing | Pharmacy Staff | Released or in-progress Dispense Orders, grouped by pharmacy queue entry | Prepared medication and accountable preparation outcome |
| Serah Obat | Pharmacist, supported by Pharmacy Staff | Queue entries whose intended medication is ready for pickup, filtered by operational pickup categories | Final review, optional recipient reference, education, dispense, and handover |

Supporting operational decisions that do **not** add screens or aggregates:

- an **Exception Worklist** inside Pelayanan Penjualan for No-Show, expiry, return, correction, and pending financial or inventory consequences;
- optional **attention counters / filters** on each screen for prioritization;
- Serah Obat **operational worklist categories** for pickup prioritization;
- a read-only **Patient Medication Journey** projection for consolidated visibility;
- **Outpatient Queue Mapping** as a navigation/association mechanism only (BA-03); and
- **Pharmacy Queue Close** as an operational fact/event, not an aggregate.

This decision assumes that queue-number issuance is owned by c013-kiosk-queue-display-web and that the queue infrastructure is shared with Outpatient Admission.

## 2. Governing Principles

1. A pharmacy queue entry groups a Patient's counter interaction; it does not merge prescriptions, Sales Orders, Sales Invoices, or Dispense Orders.
2. Sales, physical preparation, and handover are separate accountable facts. A completed payment or queue does not prove medication handover.
3. A Sales Invoice must be derived from accountable Sales Order Lines. Users must not manually create independent medication invoice items or free-form non-medication invoice items. BHP appears as a catalog sales line. Packaging and compounding fees are line-level charges. Rounding and other transaction-wide adjustments are invoice-level charges.
4. Dispensing is not a status update on a Sales Order. It is executed through a separate Dispense Order aggregate.
5. The existing Patient Tracker owns pharmacy queue identity, queue number, and queue lifecycle. Outpatient Queue Mapping is a navigation/association mechanism only; it is not an aggregate root and does not own queue or medication-demand lifecycle. Apotek owns its operational and fulfillment facts on the Pharmacy aggregates (`TelaahResep`, `SalesOrder`, `SalesInvoice`, `DispenseOrder`). Pharmacy Queue Close is an operational fact/event that requests Patient Tracker `Withdrawn`; it is not an aggregate.
6. The four-screen model is an outpatient scope decision. It does not preclude inpatient, emergency, unit-dose, or future exception-focused worklists.
7. Operational worklists, attention indicators, and patient-journey views are projections and UX aids. They do not introduce workflow states, aggregate roots, or alternate lifecycles.
8. Dispense Authorized is a policy evaluation result derived from financial and/or coverage evidence. It authorizes Medication Preparation and Dispensing. It is not an aggregate, entity, persisted business object, source of truth, or transaction boundary.

## 3. Screen Design

### 3.1 Screen Telaah Resep

**Primary user:** Pharmacist.

#### Worklist

- Electronic outpatient prescriptions available for review.
- External or physical prescriptions already recorded by Pharmacy Staff.
- Optional filters for urgency, originating unit, prescribing clinician, and review state.

Patient arrival and pharmacy queue mapping are not prerequisites for review. A Pharmacist may review an available prescription before the Patient is present at the pharmacy.

#### Workbench

The workbench lets the Pharmacist:

1. verify patient, prescription source, medication, dosage instruction, quantity, and available clinical information;
2. decide each prescription line as accepted as prescribed, accepted with an authorized substitute, or rejected;
3. record the substitute, reason, quantity, and responsible Pharmacist when applicable;
4. complete the review as `Approved`, `Partially Approved`, or `Rejected`; and
5. establish one Sales Order for an approved or partially approved prescription.

The original prescription remains unchanged. Clarification with the prescriber remains outside the system and does not create a special workflow state.

### 3.2 Screen Pelayanan Penjualan

**Primary user:** Pharmacy Staff.

#### Worklist

- Outpatient pharmacy queue entries in `Waiting` state.
- A direct patient, registration, prescription, or queue-number search for work that does not yet have a usable queue association.

The screen must show the separate progress of every medication demand mapped to a common queue entry. It must not merge their Sales Orders, invoices, or fulfillment work.

#### Exception Worklist

Pelayanan Penjualan also exposes an **Exception Worklist** for operational resolution. This is a worklist/projection inside the same screen, not a fifth operational screen and not a new aggregate.

The Exception Worklist surfaces cases that need accountable resolution under existing Sales Order, Sales Invoice, Dispense Order, Inventory, and Tata Rekening rules, including:

| Exception category | Typical operational concern |
|---|---|
| No Show | `Prepared` medication in Dispensing Temporary Custody was not collected; aligns with SOP APT-RJ-007 / `WF-APT-RJ-007` |
| Pickup Expired | Collection Window elapsed; awaiting override for handover or `WF-APT-RJ-007` close |
| Expired Medication | Authorized fulfillment expiry / `Collection Window Expired` and related Dispense Order terminal outcomes |
| Return Request | Return of prepared, transferred, or handed-over medication awaiting Inventory disposition |
| Correction Request | Post-payment or post-handover commercial or fulfillment correction that must not silently replace history |
| Financial Consequence Pending | Credit Note, Refund, or other Tata Rekening outcome still outstanding after expiry or return |
| Inventory Disposition Pending | Return to Stock or other final Inventory disposition still outstanding |

Commands remain on the existing aggregates and external authorities. The worklist only selects and prioritizes work already governed by return/correction handling in this screen and by SOP APT-RJ-007 for uncollected medication.

#### Workbench

The workbench contains four activities.

1. **Map queue number to medication demand**
   - Associate one queue entry with one or more existing prescriptions, Direct Medication Requests, or resulting Sales Orders.
   - Support both Tracker Mapping and Manual Mapping.
   - Correct an incorrect mapping without rewriting unrelated demand.
   - Close a Queue Entry that is not progressed into the pharmacy workflow from `Waiting` with a mandatory close reason. Patient Tracker sets `Withdrawn`. This close is not available after `In Service`.

2. **Record external prescription**
   - Record the external or physical prescription as a source document.
   - Route it to Telaah Resep; it is not sold directly.

3. **Record direct medication request**
   - Record a retail-style request without a prescription, originating outside the hospital care workflow.
   - Accept or decline it. Optional Pharmacist consultation is SOP guidance only and is not an approval gate.
   - Establish a Sales Order only after the request is accepted.

4. **Manage sale and return/correction request**
   - Determine payer classification of Sales Order Lines and show the calculated Patient-payable amount.
   - For General Patient quantities, capture verbal purchase confirmation and establish a Sales Invoice from the applicable Sales Order Lines.
   - For BPJS quantities, show SEP and Fornas coverage outcomes; do not request Patient payment or establish the BPJS Sales Invoice early.
   - For mixed coverage, Fornas Not Covered lines form an independent Patient-Pay Sales Order. Covered lines remain on the BPJS-covered Sales Order. Do not keep uncovered lines on the BPJS fulfillment path.
   - For Partial Prescription Fulfillment, establish a Sales Order from selected or fulfillable prescription lines only when Patient Request or Stock Shortage applies. Excluded lines remain on the originating prescription. Issue Salinan Resep for unfulfilled lines when external fulfillment is required. Pharmacist approves when professional review is required.
   - Submit a return or correction request rather than freely reversing a sale. A post-payment or post-handover return requires authorization by an authorized pharmacist according to operational policy, plus Inventory and Tata Rekening outcomes. No monetary approval threshold applies.
   - Resolve Exception Worklist items through the same accountable paths: No-Show / expiry under SOP APT-RJ-007, return and correction through authorized-pharmacist outcomes, and display of pending financial or inventory consequences without inventing stock or settlement facts.

Queue mapping and General Patient purchase confirmation can occur in the same counter interaction. Neither action records pharmacy `ServedAt` or `DoneAt`.

### 3.3 Screen Dispensing

**Primary user:** Pharmacy Staff.

#### Worklist

- Dispense Orders that are `Released`, `Preparing`, or returned to `Preparing` after a failed final review.
- The list may be grouped by pharmacy queue entry for operational convenience, but selection and update remain per Dispense Order.

#### Workbench

The workbench lets Pharmacy Staff:

1. view applicable Dispense Authorized evaluation and Pharmacy Reserve (Stock Mutasi to Dispensing Temporary Unit) outcomes;
2. begin preparation only for authorized quantities;
3. perform picking, counting, labelling, packaging, and compounding when required;
4. record preparation completion and move the Dispense Order to `Prepared`;
5. record or display shortage, Salinan Resep for unfulfilled lines, cancellation, and other accountable exceptions; and
6. show prepared medication in Dispensing Temporary Custody until accountable handover or No Show return Mutasi.

The first `Medication Preparation Started` event is Pharmacy Service Start Evidence. It causes Patient Tracker to record `ServedAt` and move the pharmacy queue entry to `In Service`.

**Design decision:** Dispensing is a transaction represented by `Dispense Order`; it is not merely a `SalesOrder.Status` update. This preserves independent sales, payment/coverage, stock, preparation, final review, and handover lifecycles.

### 3.4 Screen Serah Obat

**Primary user:** Pharmacist. Pharmacy Staff performs the physical handover after Pharmacist authorization.

#### Worklist

- Pharmacy queue entries where every Dispense Order intended for the coordinated pickup is `Prepared` or has an accountable exception outcome.
- Each worklist item presents the per-demand and per-Dispense-Order progress beneath the common queue entry.

**Operational worklist categories** (projection-level filters for prioritization; not Dispense Order states):

| Category | Operational meaning |
|---|---|
| Ready for Pickup | Intended medication is prepared or accountably resolved and awaiting the coordinated pickup call, within the Collection Window |
| Pickup Expired | Collection Window elapsed without Medication Handover; ordinary handover is blocked until Collection Window Override |
| Ready for Review | Patient or caregiver is present after the pickup call; Final Dispense Review is due |
| Ready for Handover | Final Dispense Review has passed and physical handover may proceed |
| Completed | Medication Dispense and Medication Handover are recorded for the intended quantities |

These categories do not change Dispense Order lifecycle semantics (`Preparing`, `Prepared`, `Reviewed`, `Completed`, `Expired`, and related exception outcomes remain authoritative). The Collection Window (default 7 days) starts when the Dispense Order first becomes Ready for Pickup. Pickup Expired is a projection only. The workbench structure is unchanged; categories only organize the worklist.

#### Workbench

The workbench supports the following sequence:

1. Pharmacy Staff performs one coordinated pickup call after all intended orders are ready or accountably resolved.
2. Patient Tracker records `DoneAt` and moves the queue entry to `Done`.
3. With the Patient or caregiver present, the Pharmacist operationally verifies the recipient. The system may optionally record phone number and relationship for reference; it does not validate identity, legal relationship, documents, or authorization.
4. The Pharmacist completes a Final Dispense Review for every prepared Dispense Order and records Patient Education Acknowledgement (timestamp and responsible Pharmacist). Detailed counseling notes are optional.
5. A passed review moves the Dispense Order to `Reviewed`; a failed review returns only that order to `Preparing` and appends an immutable review record.
6. After Pharmacist authorization, Pharmacy Staff completes the physical handover. If the worklist category is Pickup Expired, an authorized pharmacist must first record Collection Window Override with reason; ordinary handover is blocked until then.
7. The system records Medication Dispense and Medication Handover for every applicable quantity, requests Remove Stock from Dispensing Temporary Unit, and applies payer-specific sales consequences.

For current outpatient BPJS policy, successful handover establishes the BPJS Sales Invoice. For a General Patient, the invoice may already be financially cleared before preparation. In both cases, queue completion is not evidence of handover.

If No Show Resolution (`WF-APT-RJ-007`) occurs before the pickup call, Patient Tracker may record `DoneAt` and move the still-`In Service` Queue Entry to `Done` without a pickup call. If the Queue Entry is already `Done`, `DoneAt` is retained. `DoneAt` is never reversed. No new queue status is introduced.

### 3.5 Shared operational projections

#### Attention counters / attention filters

Each operational screen may expose lightweight attention indicators for prioritization and filtering. Examples:

| Indicator | Typical screen use |
|---|---|
| Need Review | Telaah Resep or Serah Obat final review pending |
| Need Payment | Pelayanan Penjualan General Patient or mixed Patient-payable amount awaiting clearance |
| Need Preparation | Dispensing work waiting to start or continue |
| Ready Pickup | Serah Obat coordinated pickup ready |
| Need Resolution | Pelayanan Penjualan Exception Worklist items |

These indicators are optional operational productivity aids. They:

- do not represent workflow states;
- do not introduce new aggregates;
- do not alter aggregate lifecycles; and
- are UI / worklist concerns only.

Exact badge thresholds, colors, and placement remain implementation choices.

#### Patient Medication Journey projection

Apotek may expose a read-only **Patient Medication Journey** projection that consolidates one Patient's outpatient medication progress for operational visibility and troubleshooting.

It is **not**:

- a new aggregate;
- a new workflow; or
- a fifth operational screen.

It may be surfaced through a side drawer, detail panel, or contextual patient drill-down from any of the four screens. It may consolidate facts from:

- Queue Mapping;
- Telaah Resep;
- Sales Order;
- Sales Invoice;
- Dispense Order;
- Final Review; and
- Medication Handover;

together with visible payment/coverage, exception, and external disposition outcomes when already known.

The projection has no command responsibilities. All mutations continue through the screen workbenches and existing aggregates. Queue Mapping in this projection is the same navigation/association used by worklists; it is not an aggregate.

## 4. Shared Queue Integration Decision

Apotek will reuse the Patient Tracker queue infrastructure also used by Outpatient Admission. This reuse is limited to queue identity, numbering, display, calling, and lifecycle.

### 4.1 Canonical queue identity (BA-01)

**Decision:** Patient Tracker `QueueEntry` is the sole canonical outpatient-pharmacy queue identity.

| Rule | Requirement |
|---|---|
| Canonical identity | One Patient Tracker `QueueEntry` per outpatient-pharmacy interaction |
| F-09 evidence | `Apotek-Start` and `Apotek-Done` remain reusable but must reference `QueueEntryId` from Patient Tracker |
| Legacy Farinv | Deprecated; must not create active queue records |
| Historical Farinv data | Read-only |
| Dual-active model | Prohibited |

Legacy Farinv queue records may be consulted for historical reporting only. New outpatient-pharmacy intake, display, mapping, and milestone updates must use the shared Patient Tracker queue platform.

### 4.2 Queue milestones

| Queue milestone | Apotek action | Meaning |
|---|---|---|
| `CreatedAt` / `Waiting` | Queue number issued by kiosk or tracker | Patient has a pharmacy queue entry; medication demand may still be unmapped or unreviewed |
| `Withdrawn` | Pharmacy Staff records Pharmacy Queue Close with mandatory reason | Pre-service participation ended; not `In Service` or `Done`. TAKEN is not a Tracker state |
| `ServedAt` / `In Service` | First applicable Dispense Order records `Medication Preparation Started` | Physical pharmacy preparation has started |
| `DoneAt` / `Done` | Pharmacy Staff performs coordinated pickup call, or authorized No Show Resolution completes an `In Service` Queue Entry | Queue service lifecycle completed; it does not prove final review or handover. `DoneAt` is never reversed. No new queue status. |

Apotek must expose its own queue-facing progress projection. Patient Tracker must not become authoritative for Telaah Resep, Sales Invoice, payment/coverage clearance, Dispense Order, review, or handover status.

The Admission queue's generic actions must therefore not be reused as direct pharmacy business transitions. Pharmacy-specific actions emit the corresponding Patient Tracker queue updates at the defined milestones.

## 5. Aggregate Discovery

### 5.1 Aggregate map

```mermaid
flowchart TD
  RX["Resep / Resep Luar"] --> TR["Telaah Resep"]
  DR["Direct Medication Request"] --> SO["Sales Order"]
  TR --> SO
  Q["Patient Tracker: Queue Entry"] -.-> MAP["Outpatient Queue Mapping\n(navigation/association;\nnot an aggregate)"]
  MAP -.-> SO
  SO --> INV["Sales Invoice"]
  SO --> DO["Dispense Order"]
  PAY["Payment Clearance / Coverage Clearance"] -.-> AUTH["Dispense Authorized\n(policy evaluation result;\nnot an aggregate, entity,\npersisted object, or\ntransaction boundary)"]
  INV -.-> PAY
  AUTH -.->|"authorizes preparation"| DO
  DO --> HO["Medication Dispense and Handover"]
  CLOSE["Pharmacy Queue Close\n(operational fact/event;\nnot an aggregate)"] -.->|"requests Withdrawn"| Q
```

### 5.2 Aggregate roots and responsibilities

The Apotek aggregate-root list is exactly:

- `TelaahResep`
- `SalesOrder`
- `SalesInvoice`
- `DispenseOrder`

| Aggregate root | Responsibility | Does not own |
|---|---|---|
| `TelaahResep` | Keeps the prescription source, per-line professional disposition, responsible Pharmacist, and final review outcome consistent | Original clinical prescription and prescriber clarification communications |
| `SalesOrder` | Owns accepted demand, Sales Order Lines, accepted quantity, fulfillment/unfulfilled progress, and overall resolution; reconciles commercial and fulfillment quantities | Payment settlement, inventory balance, physical preparation, or handover execution |
| `SalesInvoice` | Owns one medication sale, catalog sales lines including BHP, line-level charges, invoice-level charges, pricing snapshot, payer, financial disposition, and commercial adjustments | Sales Order quantity authority, payment evidence, stock, physical dispensing, or a separate invoice-component model |
| `DispenseOrder` | Owns preparation, lines, immutable final review attempts, Patient Education Acknowledgement, Collection Window Override when applicable, Medication Dispense, Medication Handover, expiry, cancellation, return, and non-fulfillment outcomes | Sales Invoice payment settlement and authoritative stock balance |

`OutpatientQueueMapping` and Pharmacy Queue Close are **not** aggregate roots. They do not appear in the catalog above.

| Object | Classification | Responsibility | Does not own |
|---|---|---|---|
| `OutpatientQueueMapping` | Navigation/association mechanism only (BA-03) | Answers operational navigation and worklist questions: which queue entry is serving this medication demand, and which medication demands are associated with this queue entry. Identifies Tracker Mapping or Manual Mapping. | Business lifecycle, workflow state, approval state, operational progress, transactional consistency, or active/inactive relationship state. Queue identity and lifecycle remain owned by Patient Tracker. Medication demand lifecycle remains owned by the Pharmacy aggregates (`SalesOrder`, `DispenseOrder`, and related roots). No independent consistency boundary. |
| Pharmacy Queue Close | Operational fact/event | Pharmacy Staff fact that ends a Pharmacy Queue Entry not progressed into the pharmacy workflow. Records a mandatory close reason, responsible Pharmacy Staff, and effective business time. Requests Patient Tracker `Withdrawn` from `Waiting`. | Patient Tracker queue state itself (`Withdrawn` is set by Patient Tracker). Not an aggregate, not a queue state, and not a path that establishes a Sales Order, Dispense Order, or Medication Handover. |

### 5.3 Dispense Authorized (not an aggregate)

**Design decision (BA-08):** Preparation authorization is **Dispense Authorized**.

Dispense Authorized is a **policy evaluation result** derived from financial and/or coverage evidence. It is not an aggregate, not an entity, not a persisted business object, not a source of truth, and not a transaction boundary.

It is required before Medication Preparation Started and Dispensing. It is not required for Medication Handover. Handover remains governed by Prepared medication, passed Final Dispense Review, and Patient Education Acknowledgement.

Typical evidence used by the evaluation (`BR-APT-040`–`BR-APT-044`, `BR-APT-072`, `BR-APT-074`):

| Payer path | Evidence |
|---|---|
| General Patient | Sales Invoice created; Payment Clearance |
| BPJS | Valid SEP; authoritative Fornas coverage (Coverage Clearance) |
| Other insurance | Valid coverage approval |

No parallel clearance object is introduced. The evaluation does not own Dispense Order lifecycle, stock, or handover facts.

### 5.4 Relationships and invariants

1. `TelaahResep` completes before an accepted prescription establishes its `SalesOrder`.
2. A `SalesOrder` owns one or more Sales Order Lines. Each accepted line carries a fixed medication identity after establishment.
3. A `SalesInvoice` references exactly one Sales Order and may invoice one or more of its lines. Each invoice item references exactly one Sales Order Line.
4. A `DispenseOrder` references exactly one Sales Order and may fulfill one or more of its lines. Each dispense line references exactly one Sales Order Line.
5. Invoice quantity and dispense quantity may progress independently, but neither may exceed the applicable Sales Order Line authority.
6. A common queue entry may be associated with multiple medication demands through `OutpatientQueueMapping`. Mapping is a navigation/association mechanism only; it does not own lifecycle or transactional consistency. Each demand retains independent Telaah Resep, Sales Order, Sales Invoice, and Dispense Order identity and lifecycle.
7. A Sales Order becomes `Resolved` only after every accepted quantity and required commercial consequence reaches a final accountable outcome.
8. A Dispense Order becomes `Completed` only after accountable Medication Dispense and applicable Medication Handover are recorded.
9. A failed Final Dispense Review never overwrites history; it appends a review record and returns only the affected Dispense Order from `Prepared` to `Preparing`.
10. Dispense Authorized is a policy evaluation over financial and/or coverage evidence; it authorizes preparation quantity and does not own Dispense Order lifecycle, stock, or handover facts.
11. Pharmacy Queue Close is an operational fact/event, not an aggregate and not a queue state. It requests Patient Tracker `Withdrawn` from `Waiting` and does not add a pharmacy state to the queue.

### 5.5 External authorities

The following are required collaborators, not Apotek aggregates:

| External authority | Apotek dependency |
|---|---|
| CPOE / clinical order authority | Original electronic prescription and clinician intent |
| Patient Tracker | Pharmacy queue entry identity, queue number, `CreatedAt`, `ServedAt`, `DoneAt`, and `Withdrawn` |
| Inventory / Stock Ledger | Stock availability, Stock Mutasi, Remove Stock, and movement history only. Does not own reservation, issue, `Prepared`, handover, or No Show status. |
| Payment / Cashier | Payment Clearance evidence |
| SEP and Fornas authorities | BPJS eligibility and item-level Coverage Clearance evidence |
| Tata Rekening | Financial charge, credit note, refund, and final settlement consequences |

### 5.6 Aggregate review of the operational decisions in this artifact

| Decision | Aggregate impact |
|---|---|
| Exception Worklist in Pelayanan Penjualan | None. Projection / worklist over existing Sales Order, Dispense Order, Sales Invoice, Inventory, and Tata Rekening outcomes |
| Attention counters / filters | None. UI prioritization only |
| Serah Obat operational categories | None. Worklist filters; Dispense Order states remain authoritative |
| Dispense Authorized (BA-08) | None added. Policy evaluation result; **not** an aggregate, entity, persisted object, or transaction boundary |
| Patient Medication Journey | None. Read-only consolidation projection |
| Outpatient Queue Mapping (BA-03) | None added. Navigation/association mechanism only; **not** an aggregate root |
| Pharmacy Queue Close | None added. Operational fact/event; **not** an aggregate root |

**Conclusion:** The aggregate map in §5.1–§5.2 contains only `TelaahResep`, `SalesOrder`, `SalesInvoice`, and `DispenseOrder` as aggregate roots. The operational decisions above are incorporated as screen/worklist behavior, projections, read models, UX aids, associations, or operational facts.

## 6. Explicit Non-Decisions

This artifact intentionally does not define:

- physical database table design (see [Outpatient Apotek Persistence Design](./outpatient-apotek-persistence-design.md));
- API, event, or command contracts;
- detailed screen layout, component design, or navigation;
- exact attention-badge thresholds, colors, or placement;
- detailed Patient Medication Journey drawer or panel layout;
- inpatient, emergency, or unit-dose fulfillment worklists.

Exception authorization for returns, corrections, expired collection overrides, and other dispensing exceptions is authority-based and is owned by the domain (`BR-APT-135`–`BR-APT-137`). This artifact does not introduce a monetary approval threshold.

Those remaining decisions should follow the aggregate boundaries established here.
