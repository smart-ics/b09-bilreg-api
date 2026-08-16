# Apotek Domain

**Artifact status:** Canonical business specification

**Bounded context:** Apotek (`Pelayanan Obat Pasien`)

**Version scope:** Target business model across outpatient, inpatient, emergency, and unit-dose settings

**Bahasa Indonesia companion:** [apotek-domain-id.md](./apotek-domain-id.md)

**Related business contexts:** [CPOE](../../contexts/cpoe/CPOE-DOMAIN.md), [Patient Tracker](../../contexts/pasien-tracker/TRACKER-DOMAIN.md), [Tata Rekening](../../contexts/TataRekening/02-domain.md)

## 1. Business Overview

### 1.1 Purpose and value

Apotek turns patient-specific medication demand accepted by Pharmacy into accountable commercial invoicing and physical Apotek. It separates the legacy `Trs.DU (DO-Bill)` combination of stock delivery and billing into independent `Sales Invoice` and `Dispense Order` lifecycles, coordinated by a `Sales Order`.

The business must ensure that:

- the clinician's original Resep remains authoritative and traceable;
- only professionally accepted medication demand enters a Sales Order;
- Sales Invoices may be formed from Sales Order Lines independently of Dispense Orders formed from those same Sales Order Lines;
- billing, payment or coverage, stock availability, preparation, and handover remain distinct business facts;
- partial billing and partial fulfillment remain quantitatively accountable; and
- every accepted quantity reaches an accountable fulfilled or unfulfilled outcome.

### 1.2 Scope

This context covers:

1. Telaah Resep and Direct Medication Request acceptance;
2. Sales Order establishment, commercial invoicing, and fulfillment planning;
3. Medication Sale and Sales Invoice formation;
4. Dispense Order establishment and physical dispensing;
5. commercial or coverage clearance for fulfillment;
6. Medication Dispense and Medication Handover; and
7. shortage, substitution, cancellation, return, and other non-fulfillment outcomes; Outpatient Pharmacy does not support Backorder; and
8. the currently defined outpatient queue, payer, pickup, and no-show workflow policy.

It applies across outpatient, inpatient, emergency, and Unit Dose Dispensing settings.

### 1.3 Business boundaries

Apotek owns Hasil Telaah Resep, Sales Order, Medication Sale represented by Sales Invoice, Dispense Order, Medication Dispense, Medication Handover, and fulfillment resolution.

It relies on related contexts without taking over their authority:

- CPOE or another clinical-order authority owns the original Resep and clinician intent;
- Medication Catalog or formulary authority owns medication identity and formulary policy;
- Inventory owns authoritative stock balances and stock movements;
- Payment owns receipts and settlement evidence;
- Tata Rekening owns registration-level Financial Responsibility, payer allocation, finalization, and settlement initiation;
- Patient Tracker owns outpatient queue identity and lifecycle; and
- the clinical care context owns Medication Administration.

Purchasing, supplier management, replenishment, warehouse transfer, enterprise accounting, and Medication Administration are outside this context.

Outpatient queue identity and lifecycle remain externally owned by Patient Tracker. The Patient Tracker `QueueEntry` is the sole canonical outpatient-pharmacy queue identity. Legacy Farinv queue identity is deprecated, shall not create active queue records for new outpatient-pharmacy interactions, and historical Farinv queue data is read-only. No dual-active queue model is permitted.

Apotek owns the outpatient business decision that associates a Pharmacy Queue Entry with the applicable medication demand and owns the payer, pickup, handover, and no-show policy applied after that association.

For Outpatient Pharmacy, the fulfillment boundary is the active Registration Period. A Resep may be reviewed, re-reviewed, and fulfilled while its originating Registration remains active. No separate Fulfillment Episode concept exists.

For outpatient pharmacy queues, Patient Tracker records `CreatedAt` when the Queue Number is issued, `ServedAt` when the first applicable Dispense Order enters `Preparing`, and `DoneAt` when Pharmacy Staff performs the pickup call. Those queue milestones describe operational queue progress and do not prove Medication Handover. Patient Tracker `Apotek-Start` and `Apotek-Done` evidence remain reusable but shall reference the canonical `QueueEntryId`, not Farinv queue identity.

### 1.4 Central business separation

The target flow is:

```text
Resep or Direct Medication Request
  -> Telaah Resep or direct acceptance
  -> Sales Order
       -> zero or more Sales Invoices
       -> zero or more Dispense Orders
            -> Medication Dispense
            -> Medication Handover
```

A Resep does not become a Sales Order. A completed professional decision authorizes a new Sales Order while preserving the Resep as its clinical source.

## 2. Ubiquitous Language
| Term | Definition |
|---|---|
| Apotek | The bounded context that coordinates patient-specific medication demand from Pharmacy acceptance through commercial invoicing and physical fulfillment resolution. |
| Patient Medication Demand | A patient-specific need for medication originating from a Resep or Direct Medication Request. |
| Resep | The clinician's authoritative intent for medication to be supplied or administered to a Patient. |
| Resep Elektronik | A Resep created and transmitted through an electronic clinical-order authority. |
| Resep Fisik | A nonelectronic Resep that must be recorded before Pharmacy can review it. |
| Direct Medication Request | A retail-style medication request without a Resep, originating outside the hospital care workflow, that Pharmacy Staff may accept or decline. |
| Baris Resep | One requested medication, dosage instruction, and quantity within a Resep. |
| Source Traceability | The accountable relationship from Medication Sale and dispensing outcomes back to their Sales Order, accepted demand, and original source. |
| Telaah Resep | The Pharmacist's administrative, pharmaceutical, and clinical assessment of a Resep. |
| Hasil Telaah Resep | The professional decision on a Resep: approved, partially approved, or rejected. Accepted medication is materialized as a Sales Order Line. |
| Accepted Medication Line | A medication line professionally accepted for inclusion in a Sales Order, independently of current stock availability. |
| Sales Order | The accepted medication demand owned by Pharmacy and used as the common source of Medication Sales and Dispense Orders. |
| Sales Order Line | One accepted medication, quantity, instructions, and applicable commercial basis within a Sales Order. |
| Accepted Quantity | The maximum quantity of a Sales Order Line available for accountable invoicing, physical fulfillment, and resolution. |
| Medication Sale | The commercial transaction represented by one Sales Invoice from one Sales Order. |
| Sales Invoice | The authoritative commercial document and Aggregate Root representing one Medication Sale. |
| Legacy DU | The legacy `Trs.DU (DO-Bill)` transaction that combined medication billing and stock-delivery concerns; in the target model its facts are represented through a Sales Invoice and one or more Dispense Orders coordinated by the same Sales Order and traced at line level. |
| Sales Invoice Item | One medication, BHP, or other catalog sales line, quantity, price, discount, line-level charges, and value within a Sales Invoice. Every medication or BHP Sales Invoice Item originates from exactly one Sales Order Line and represents the portion of that line billed by the invoice. |
| BHP | A standard catalog item that may appear as a sales line. It is not a free-form invoice component. |
| Line-level Charge | An item-specific commercial charge attached to a sales line, such as packaging or compounding fees. |
| Invoice-level Charge | A transaction-wide commercial adjustment attached to a Sales Invoice, such as rounding. |
| Pricing Snapshot | The immutable commercial basis used when a Sales Invoice is established. |
| Payer | The Patient, BPJS, insurer, company, or other party expected to bear a medication charge. |
| Financial Charge | The financial consequence supplied to Tata Rekening from a Medication Sale. |
| Purchase Confirmation | A General Patient's verbal decision to proceed after Pharmacy Staff communicates the calculated amount before Sales Invoice establishment. It is a workflow activity and is not retained as a separate business object or transaction. |
| General Patient | A Patient whose applicable Medication Sale requires Purchase Confirmation and Patient payment before Medication Preparation. |
| BPJS Patient | A Patient whose applicable Medication Sale is covered through BPJS policy without Patient Purchase Confirmation or Patient payment. |
| Payment Clearance | Evidence that the required payment condition has been satisfied. |
| Coverage Clearance | Evidence that the applicable payer authorizes fulfillment without immediate Patient payment. For outpatient BPJS fulfillment, it combines a valid SEP for the encounter with item-level coverage determined from the authoritative Fornas mapping. |
| Dispense Authorized | A policy evaluation result indicating medication preparation and dispensing may start, derived from financial and coverage evidence. It is not an aggregate, entity, source of truth, or transaction boundary. |
| Financial Adjustment | An accountable correction to a Medication Sale or its financial consequences. |
| Credit Note | A commercial document reducing or reversing an issued Sales Invoice amount. |
| Refund | The accountable return of previously settled funds. |
| Dispense Order | The authoritative instruction to physically fulfill one or more Sales Order Lines from one Sales Order. |
| Dispense Order Line | One medication quantity to be physically fulfilled within a Dispense Order. It references exactly one Sales Order Line and carries the applicable care setting and Dispense Cycle. |
| Dispense Cycle | A defined fulfillment period or batch, especially for inpatient and Unit Dose Dispensing. |
| Unit Dose Dispensing | Fulfillment in patient-specific unit doses or defined administration periods. |
| Stock Availability | Inventory's representation of quantity currently available to support fulfillment. |
| Pharmacy Unit | The ordinary pharmacy Stock Location from which outpatient medication is issued into dispensing custody. |
| Dispensing Temporary Unit | The pharmacy Stock Location that holds medication under active dispensing custody after Dispensing Started and before handover or No Show return. |
| Pharmacy Reserve | Pharmacy-directed placement of stock for a Dispense Order, implemented only as Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit. |
| Stock Mutasi | Stock Ledger's accountable transfer of quantity between Stock Locations without changing Receipt Source. |
| Remove Stock | Stock Ledger's accountable outbound removal of quantity from a Stock Location, including from Dispensing Temporary Unit on Medication Handover. |
| Medication Preparation | Picking, counting, labelling, packaging, and otherwise preparing medication for fulfillment. |
| Compounding | Preparing a medication product from ingredients or components for a specific fulfillment need. |
| Final Dispense Review | The final professional Medication Review performed by the Pharmacist with the Patient or caregiver present after the pickup call and before Medication Handover. |
| Final Dispense Review Record | An immutable record of one Final Dispense Review attempt for a Dispense Order, including its passed or failed outcome, reason when failed, responsible Pharmacist, effective business time, and affected quantity. One Dispense Order may have multiple review records. |
| Prepared Medication | Medication whose physical preparation is complete and awaits final review or handover. |
| Dispensing Temporary Custody | Medication quantity held in Dispensing Temporary Unit after Dispensing Started and before Medication Handover or No Show return. |
| Medication Dispense | The accountable fact that a quantity of medication was actually supplied for a Patient. |
| Medication Handover | The accountable transfer of medication to an Authorized Recipient. |
| Authorized Recipient | The Patient, caregiver, practitioner, ward, or other party to whom medication is handed over. Recipient verification is an operational Pharmacist responsibility and is not system-enforced. |
| Patient Education | Medication counseling provided to the Patient or caregiver before Medication Handover. |
| Patient Education Acknowledgement | The lightweight record that the Pharmacist confirmed counseling was provided. It records education timestamp and responsible Pharmacist. Detailed counseling notes are optional. |
| Fulfilled Quantity | The quantity of a Sales Order Line that reached a successful Medication Dispense outcome. |
| Partial Prescription Fulfillment | Establishing Sales Order(s) from a subset of prescription lines when Patient Request, Stock Shortage, or Fornas Not Covered applies. |
| Partial Fulfillment | Fulfillment execution in which one Sales Order is fulfilled through multiple Dispense Orders, or less than the total Accepted Quantity of a Sales Order Line is fulfilled while another quantity remains unresolved or receives a different outcome. This is not Partial Prescription Fulfillment policy. |
| Fulfillment Completion | The condition in which every Accepted Quantity has an accountable final outcome. |
| Medication Administration | The clinical fact that medication was actually given to or consumed by the Patient; it is externally owned. |
| Medication Shortage | Insufficient stock to fulfill an allocated medication quantity. |
| Stock Discrepancy | A difference between recorded and physical stock that affects fulfillment. |
| Backorder | An unresolved quantity retained for later fulfillment when supply becomes available. Outpatient Pharmacy does not support Backorder. |
| Medication Substitution | The accountable replacement of a requested medication product under applicable professional authority. |
| Unfulfilled Medication Outcome | A final, accountable reason that an accepted medication quantity was not fulfilled. |
| Salinan Resep | An accountable Prescription Copy record of prescribed medication or quantity not included in the Sales Order or not fulfilled, when applicable. |
| Dispense Cancellation | The accountable ending of a Dispense Order before successful fulfillment. |
| Fulfillment Expiry | The ending of a fulfillment opportunity because its permitted service period elapsed. |
| Medication Return | The accountable return of medication previously prepared, transferred, or handed over. |
| Return to Stock | Inventory's authoritative acceptance of eligible returned medication into available stock. |
| No-Show | An outpatient outcome in which the Patient does not collect medication within the applicable service limit. |
| Pharmacy Queue Entry | A Patient's participation in an outpatient pharmacy queue represented by one Patient Tracker `QueueEntry` whose identity and lifecycle are owned by Patient Tracker. |
| Legacy Farinv Queue Entry | A deprecated historical pharmacy queue record from the Farinv subsystem. It is read-only and shall not be created for new outpatient-pharmacy interactions. |
| Outpatient Queue Mapping | The accountable association of a Pharmacy Queue Entry with the applicable Resep, Direct Medication Request, Sales Order, or another traceable medication-demand source. |
| Tracker Mapping | Outpatient Queue Mapping established automatically when valid Patient Tracker or registration evidence resolves one or more existing Resep. It does not create a Resep and does not apply to a Direct Medication Request. |
| Manual Mapping | Outpatient Queue Mapping established by Pharmacy Staff after the Queue Number and applicable medication demand are identified. |
| Pharmacy Service Start Evidence | The `Medication Preparation Started` fact that causes the outpatient Pharmacy Queue Entry to record `ServedAt`. |
| Care Setting | The operational setting whose policy applies to fulfillment, such as outpatient, inpatient, or emergency care. |
| Outpatient Fulfillment | Apotek under outpatient arrival, queue, payment, pickup, and no-show policies. |
| Inpatient Fulfillment | Apotek under inpatient ward, scheduled supply, and return policies. |
| Emergency Fulfillment | Apotek under the applicable urgent-care policy. |
| Dose Window | A defined administration period used to plan a Dispense Cycle without representing Medication Administration itself. |
| Ward Delivery | Medication Handover from Pharmacy to an Authorized Recipient in an inpatient ward. |

## 3. Business Capabilities

### 3.1 Medication Demand Acceptance

Accept reviewed Resep demand or an authorized Direct Medication Request without changing the original clinical intent.

### 3.2 Telaah Resep

Establish the professional disposition of each Resep and its lines, including partial acceptance and accepted substitutes.

### 3.3 Sales Order Management

Establish and maintain the accepted medication demand, its quantities, source traceability, and final resolution.

### 3.4 Commercial Invoicing

Form one or more Medication Sales and Sales Invoices from Sales Order Lines without depending on Dispense Order count or timing. Sales Invoice Items express the quantity and value billed from their source Sales Order Lines.

### 3.5 Fulfillment Planning

Form one or more Dispense Orders from Sales Order Lines based on care setting, quantity, location, cycle, and fulfillment policy.

### 3.6 Dispense Authorization

Evaluate financial and coverage evidence to determine when medication preparation and dispensing may start (`Dispense Authorized`).

### 3.7 Physical Dispensing

Coordinate Pharmacy Reserve through Stock Mutasi, Medication Preparation, Compounding, Final Dispense Review, Medication Dispense, Medication Handover, and No Show stock return.

### 3.8 Partial and Unit-Dose Fulfillment

Support Partial Prescription Fulfillment at the Prescription-to-Sales Order boundary, independently counted billing and fulfillment tranches through multiple Dispense Orders per Sales Order, Unit Dose Dispensing, and Dose Windows.

### 3.9 Exception and Return Resolution

Resolve shortages, discrepancies, substitutions, cancellations, expiries, returns, no-shows, and required financial consequences. Outpatient Pharmacy does not retain Backorder or route fulfillment to an alternate stock source.

### 3.10 Cross-Setting Traceability

Preserve quantitative and source traceability across outpatient, inpatient, emergency, billing, dispensing, and handover outcomes.

### 3.11 Outpatient Queue Coordination

Associate an externally owned Pharmacy Queue Entry with applicable medication demand through Tracker Mapping or Manual Mapping without making queue arrival a prerequisite for Telaah Resep.

### 3.12 Outpatient Payer, Pickup, and No-Show Resolution

Apply General Patient and BPJS clearance, invoice-timing, pickup, handover, and no-show policies while preserving independent commercial and fulfillment outcomes.

## 4. Actors & Roles

### 4.1 Dokter Penulis Resep

Owns the original Resep. Any clarification with the Pharmacist occurs outside the system and does not alter that original Resep. The Dokter Penulis Resep does not own Pharmacy acceptance, Sales Invoice formation, or dispensing execution.

### 4.2 Pharmacist

Owns Hasil Telaah Resep, Medication Substitution authorized during Telaah Resep, Final Dispense Review, operational Authorized Recipient verification, and Patient Education Acknowledgement. Authorized Recipient verification is not a system-enforced gate. The Pharmacist does not own administrative outpatient queue calling. Medication Substitution authority ends when the Sales Order is established.

### 4.3 Pharmacy Staff

Coordinates accepted demand, Sales Order progression, outpatient administrative interaction, Medication Preparation, Compounding, and accountable handover within assigned authority. For outpatient fulfillment, Pharmacy Staff calls Queue Numbers, establishes Manual Mapping, communicates the calculated General Patient amount before Sales Invoice establishment, saves the confirmed Sales Invoice, prepares medication according to the Dispense Order, and performs the pickup call. For a Direct Medication Request, Pharmacy Staff accepts or declines without creating a Resep. Optional Pharmacist consultation is operational SOP guidance only and is not modeled as an approval gate. When outpatient stock cannot support full fulfillment, Pharmacy Staff includes only fulfillable lines in the Sales Order and issues Salinan Resep for unfulfilled lines. Pharmacy Staff shall not create Backorder, select an alternate stock source, or substitute the medication.

### 4.4 Patient or Caregiver

Provides applicable confirmation and payment, receives education, and accepts medication when acting as an Authorized Recipient.

### 4.5 Cashier

Receives payment and supplies Payment Clearance evidence. The Cashier does not establish medication eligibility or fulfillment quantity.

### 4.6 Inpatient Authorized Recipient

Receives Ward Delivery for the Patient and remains identifiable in the Medication Handover outcome. Receipt does not represent Medication Administration.

### 4.7 Pharmacy Supervisor

Owns exceptional decisions beyond ordinary authority, including approved expiry, return, shortage resolution, and accountable correction.

## 5. Domain Objects

### 5.1 Telaah Resep

Represents Pharmacy's professional assessment process for one Resep. It retains per-line decisions, the responsible Pharmacist, and source traceability without rewriting the Resep. Accepted medication is materialized in the Sales Order.

### 5.2 Sales Order

Represents one accepted patient-specific medication demand. It owns Sales Order Lines, accepted quantities, and resolution progress. It coordinates commercial invoicing and physical fulfillment through Sales Invoices and Dispense Orders whose items or lines reference its Sales Order Lines.

### 5.3 Sales Invoice

Represents one Medication Sale from one Sales Order. It owns Sales Invoice Items, payer classification, commercial value, financial disposition, adjustments, and Financial Charge outcome. Each medication Sales Invoice Item identifies the one Sales Order Line from which it originates.

The Sales Order Line is the sales-order item in this context. Its relationship with Sales Invoice Items expresses the billed portion directly:

```text
Sales Order Line
        │
        └──> Sales Invoice Item
```

One Sales Order Line may be represented by Sales Invoice Items in one or more Sales Invoices for partial billing. Each Sales Invoice Item originates from exactly one Sales Order Line.

### 5.4 Dispense Order

Represents one physical fulfillment instruction from one Sales Order. It owns Dispense Order Lines, each of which references exactly one Sales Order Line from that Sales Order, together with preparation and review progress, Medication Dispense outcomes, and final fulfillment disposition.

### 5.5 Medication Dispense

Represents the actual medication and quantity supplied for a Patient, including responsible party, effective time, and source Dispense Order.

### 5.6 Medication Handover

Represents transfer of medication at handover time, including destination when applicable and Patient Education Acknowledgement. Optional recipient phone number and relationship to the Patient may be recorded for reference only and do not prove identity or legal authorization.

### 5.7 Outpatient Queue Mapping

Represents the active association between an externally owned Pharmacy Queue Entry and the applicable medication-demand source. If the selected source is incorrect, the association is updated in place and no mapping-change history is required. It records the current mapping method without owning Queue Number or queue lifecycle.

### 5.8 Unfulfilled Medication Outcome

Represents the final reason an accepted quantity was not fulfilled and identifies any Salinan Resep, return, or financial correction required. Outpatient Pharmacy does not use backorder closure.

### 5.9 Final Dispense Review Record

Represents one immutable Final Dispense Review attempt owned as a detail of one Dispense Order. Review records are appended rather than replaced so repeated failed and successful reviews remain accountable in their original order.

### 5.10 Patient Education Acknowledgement

Represents the Pharmacist's confirmation that medication counseling was provided before Medication Handover. It records education timestamp and responsible Pharmacist. It is not a structured counseling-content record. Detailed counseling notes may be attached only when the Pharmacist considers additional documentation necessary.

### 5.11 Pharmacy and Stock Ledger boundary

Pharmacy owns Sales Order, Dispense Order, dispensing lifecycle, `Prepared`, `Handed Over`, and No Show resolution. Stock Ledger owns stock quantity, Mutasi, Remove Stock, and movement history only.

Pharmacy Reserve is implemented only as Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit. Medication Handover requests Remove Stock from Dispensing Temporary Unit. No Show resolution requests Stock Mutasi from Dispensing Temporary Unit back to Pharmacy Unit. `Prepared` is a Dispense Order state only and is not an Inventory state. Partial fulfillment semantics belong to Sales Order; one Sales Order may be fulfilled through multiple Dispense Orders.

```text
Dispensing Started
  -> Stock Mutasi: Pharmacy Unit -> Dispensing Temporary Unit

Dispensing Completed / Prepared
  -> no Stock Ledger action

Medication Handed Over
  -> Remove Stock from Dispensing Temporary Unit

No Show resolution
  -> Stock Mutasi: Dispensing Temporary Unit -> Pharmacy Unit
```

## 6. Aggregates

### 6.1 Telaah Resep Aggregate

**Aggregate Root:** `Telaah Resep`

The aggregate keeps the Resep source reference, per-line professional disposition, responsible Pharmacist, and completion outcome mutually consistent. It stores no clarification communication and cannot modify the clinician's original Resep.

### 6.2 Sales Order Aggregate

**Aggregate Root:** `Sales Order`

The aggregate owns Sales Order Lines, accepted quantities, fulfilled quantities, unfulfilled outcomes, and overall resolution. It coordinates the commercial and fulfillment lifecycles and reconciles quantities recorded by Sales Invoice Items and Dispense Order Lines that reference each Sales Order Line.

It ensures that invoiced quantities and physical fulfillment remain traceable and do not exceed their applicable Sales Order Line authority. It does not own Sales Invoice payment settlement, inventory balances, or physical dispensing execution.

### 6.3 Medication Sale Aggregate

**Aggregate Root:** `Sales Invoice`

The aggregate represents one Medication Sale. It keeps Sales Invoice Items, Pricing Snapshot, Payer, line-level charges, invoice-level charges, financial disposition, Credit Notes, refunds, and Financial Charge outcome mutually consistent. General Patient verbal Purchase Confirmation is evidenced by the accountable establishment of the Sales Invoice and is not retained as a separate object. Non-medication commercial facts use the legacy sales model: BHP as a catalog sales line, item-specific charges on the line, and transaction-wide adjustments on the invoice. No additional invoice component model exists.

A Sales Invoice references exactly one Sales Order but may cover one or more of its Sales Order Lines.

### 6.4 Dispense Order Aggregate

**Aggregate Root:** `Dispense Order`

The aggregate owns Dispense Order Lines and keeps their physical preparation, its one-to-many immutable Final Dispense Review Records, Patient Education Acknowledgement, Medication Dispense, Medication Handover, cancellation, expiry, return, and non-fulfillment outcomes mutually consistent.
A Dispense Order references exactly one Sales Order and may fulfill one or more of its Sales Order Lines. Every Dispense Order Line references exactly one Sales Order Line from that Sales Order; one Sales Order Line may be fulfilled through multiple Dispense Order Lines across multiple Dispense Orders.

### 6.5 Cross-aggregate relationship
A Sales Order may have zero or more Sales Invoices and zero or more Dispense Orders. Sales Invoices and Dispense Orders are not required to have equal counts or formation times.

Their business correlation is expressed through Sales Invoice Item and Dispense Order Line references to Sales Order Lines and Dispense Authorized policy evaluation over financial and coverage evidence. Sharing a Sales Order does not by itself establish that every Sales Invoice clears every Dispense Order.

Outpatient Queue Mapping is an active relationship to an externally owned Pharmacy Queue Entry, not an Aggregate Root of Apotek or a transaction log of mapping changes. Patient Tracker remains authoritative for Queue Session, Queue Number, and queue lifecycle.

## 7. Business Rules

### 7.1 Sources and professional acceptance

- **BR-APT-001** — Patient Medication Demand shall originate from exactly one Resep or Direct Medication Request.
- **BR-APT-002** — The original Resep and clinician intent shall remain owned by its clinical-order authority.
- **BR-APT-003** — Every Resep shall complete Telaah Resep before any of its lines enter a Sales Order.
- **BR-APT-004** — Only a Pharmacist shall establish the final decision for each Baris Resep as accepted as prescribed, accepted with a substitute, or rejected. The original Resep shall not be modified; accepted medication is recorded on a Sales Order Line.
- **BR-APT-005** — A Hasil Telaah Resep shall record a final decision for every reviewed Baris Resep. Clarification with the Dokter Penulis Resep occurs outside the system, is not recorded as a state or transaction, and the review remains `Under Review` until a decision is made.
- **BR-APT-006** — A rejected Resep shall not establish a Sales Order.
- **BR-APT-007** — A partially approved Resep may establish a Sales Order containing only Accepted Medication Lines.
- **BR-APT-008** — Clinical acceptance shall be independent of current Stock Availability; stock facts shall not rewrite professional eligibility.
- **BR-APT-009** — A Direct Medication Request is a retail-style medication request originating outside the hospital care workflow. Pharmacy Staff shall accept or decline it without creating a Resep. Pharmacist consultation may occur operationally but is optional SOP guidance only and shall not be modeled as approval workflow, authority threshold, escalation, risk classification, domain state, or business-rule gate.

### 7.2 Sales Order

- **BR-APT-010** — A Sales Order shall originate from exactly one completed accepted-demand source.
- **BR-APT-011** — One Resep shall establish at most one active Sales Order per Registration while that Registration remains active, except that Fornas Not Covered lines may establish a separate Patient-Pay Sales Order independent of the BPJS-covered Sales Order under `BR-APT-119`–`BR-APT-124`.
- **BR-APT-012** — A Sales Order shall contain at least one Sales Order Line with a positive Accepted Quantity.
- **BR-APT-013** — Every Sales Order Line shall retain Source Traceability to its Baris Resep or Direct Medication Request line; for an accepted substitute, the Sales Order Line contains the substitute while its source reference remains the original Baris Resep.
- **BR-APT-014** — A Sales Order shall not be a Sales Invoice, payment record, Pharmacy Reserve movement, Dispense Order, or Medication Dispense evidence.
- **BR-APT-015** — Sales Invoice and Dispense Order formation may occur independently and at different business times.
- **BR-APT-016** — The total active quantity of Dispense Order Lines referencing a Sales Order Line shall not exceed its unresolved Accepted Quantity.
- **BR-APT-017** — Fulfilled Quantity shall not exceed its Dispense Order Line quantity.
- **BR-APT-018** — Every Accepted Quantity shall eventually be fulfilled, cancelled, expired, or assigned another accountable Unfulfilled Medication Outcome. Outpatient Pharmacy shall not assign Backorder.
- **BR-APT-019** — A Sales Order shall reach Fulfillment Completion only when every Accepted Quantity has a final accountable outcome.
- **BR-APT-105** — Outpatient Pharmacy fulfillment boundary shall be the active Registration Period. A Resep may be reviewed, re-reviewed, and fulfilled while its originating Registration remains active. No separate Fulfillment Episode concept shall exist.
- **BR-APT-106** — Prescription repeat entitlement shall be owned by the Resep through the Legacy Resep `Iter` mechanism. The system shall allocate Iter, track Iter consumption, and calculate remaining Iter.
- **BR-APT-107** — The system shall not determine whether an unused Iter remains valid for fulfillment. The Pharmacist shall decide whether an unused Iter may still be honored at fulfillment time and may decline fulfillment even when remaining Iter exists.

### 7.3 Medication Sale and Sales Invoice

- **BR-APT-020** — Every Medication Sale shall be represented by exactly one Sales Invoice.
- **BR-APT-021** — Every Sales Invoice shall derive from exactly one Sales Order. Every medication Sales Invoice Item shall originate from exactly one Sales Order Line of that Sales Order.
- **BR-APT-022** — A Sales Order may produce zero, one, or multiple Sales Invoices.
- **BR-APT-023** — A Sales Invoice may cover one or more Sales Order Lines through its Sales Invoice Items and shall preserve each item's source line, billed quantity, and value.
- **BR-APT-024** — A Sales Invoice Item shall not introduce a free-form non-medication invoice component. BHP shall appear only as a catalog sales line. Item-specific charges shall be line-level charges. Transaction-wide adjustments shall be invoice-level charges.
- **BR-APT-025** — A Sales Invoice shall retain the Pricing Snapshot and Payer applicable when it is established.
- **BR-APT-026** — Sales Invoice formation shall not prove that stock is available, transferred to Dispensing Temporary Unit, prepared, dispensed, or handed over.
- **BR-APT-027** — An issued or financially settled Sales Invoice shall be corrected through an accountable Financial Adjustment, Credit Note, or Refund outcome rather than silent replacement.
- **BR-APT-028** — Every Financial Charge sent to Tata Rekening shall retain Source Traceability to its Sales Invoice and Sales Order.

### 7.4 Dispense Order and dispensing

- **BR-APT-029** — Every Dispense Order shall derive from Sales Order Lines of exactly one Sales Order, and every Dispense Order Line shall reference exactly one Sales Order Line from that Sales Order.
- **BR-APT-030** — A Sales Order may produce zero, one, or multiple Dispense Orders.
- **BR-APT-031** — A Dispense Order may cover one or more Sales Order Lines and shall preserve each direct line reference and quantity. One Sales Order Line may be split across multiple Dispense Order Lines.
- **BR-APT-032** — Dispense Order count, quantity split, and timing may differ from Sales Invoice count, value split, and timing.
- **BR-APT-033** — Stock Mutasi and Remove Stock shall remain authoritative Stock Ledger outcomes requested by Pharmacy for a Dispense Order Line.
- **BR-APT-034** — Medication Preparation and Compounding shall use an active Dispense Order as their authority.
- **BR-APT-035** — Prepared Medication shall complete Final Dispense Review before Medication Handover.
- **BR-APT-036** — A Medication Dispense shall not exceed the unresolved quantity of its Dispense Order Line.
- **BR-APT-037** — Medication Handover shall record its effective business time. Authorized Recipient verification is an operational responsibility of the dispensing Pharmacist and shall not be system-enforced. The system may optionally record recipient phone number and relationship to the Patient for reference only.
- **BR-APT-038** — Ward Delivery shall not be treated as Medication Administration.
- **BR-APT-039** — Medication Administration shall not be inferred from Sales Invoice, Remove Stock, Medication Dispense, or Ward Delivery.
- **BR-APT-096** — Each Final Dispense Review attempt shall append an immutable Final Dispense Review Record to its Dispense Order. A failed review shall record its reason, responsible Pharmacist, effective business time, and affected quantity, shall return the Dispense Order from `Prepared` to `Preparing`, and shall prohibit Medication Handover. After correction, the Dispense Order shall return to `Prepared` and undergo a new Final Dispense Review; only the latest review record with a passed outcome may transition it to `Reviewed` and authorize Medication Handover.

### 7.5 Dispense Authorization and cross-aggregate coordination

- **BR-APT-040** — Medication Preparation and Dispensing shall proceed only when Pharmacy policy evaluates the applicable financial and coverage evidence as Dispense Authorized.
- **BR-APT-041** — Payment Clearance shall come from the responsible payment authority and shall not be inferred solely from Sales Invoice existence.
- **BR-APT-042** — Coverage Clearance shall identify the applicable Payer and covered fulfillment authority.
- **BR-APT-043** — Dispense Authorized shall be a policy evaluation result only. It shall not be persisted as an aggregate, entity, source of truth, or transaction boundary.
- **BR-APT-044** — One Sales Invoice may support Dispense Authorized evaluation for multiple Dispense Orders, and one Dispense Order may rely on multiple Sales Invoice Items or Sales Invoices when policy requires.
- **BR-APT-045** — A paid or financially cleared Sales Invoice shall not guarantee successful fulfillment when shortage, discrepancy, expiry, or another valid exception occurs.
- **BR-APT-046** — A financial clearance followed by non-fulfillment shall produce an accountable Unfulfilled Medication Outcome and the required Credit Note, Refund, or other approved commercial resolution. It shall not substitute a Sales Order Line after Sales Order establishment. Outpatient Pharmacy shall not resolve that condition through Backorder or an alternate stock source.

### 7.6 Partial fulfillment, UDD, and exceptions

- **BR-APT-047** — Partial Fulfillment at Sales Order execution level shall be represented by one Sales Order fulfilled through multiple Dispense Orders, or by fulfilled and unresolved quantities on Sales Order Lines. This is fulfillment execution and is not Partial Prescription Fulfillment policy. Dispense Order shall not own Partial Prescription Fulfillment policy semantics.
- **BR-APT-048** — Unit Dose Dispensing may divide one Sales Order Line into multiple Dispense Cycles and Dispense Orders.
- **BR-APT-049** — A Dose Window shall guide fulfillment planning and shall not assert Medication Administration.
- **BR-APT-050** — For an accepted substitute, the Sales Order Line shall record the substitute, responsible Pharmacist, reason, and affected quantity while retaining its reference to the original Baris Resep. Medication identity on an established Sales Order Line shall not be changed; a later replacement is handled by cancelling the affected line or order, reviewing the same original Resep again, and establishing a new Sales Order Line without requiring a corrected or replacement Resep.
- **BR-APT-051** — A Medication Shortage or Stock Discrepancy shall not alter the original Resep or erase an existing Sales Invoice.
- **BR-APT-052** — A Medication Return shall identify its source Dispense Order, quantity, reason, and final Inventory disposition.
- **BR-APT-053** — Return to Stock shall occur only when Inventory accepts the returned medication under its own policy.
- **BR-APT-054** — A Salinan Resep shall identify prescribed medication or quantity that remained unfulfilled or was excluded from the Sales Order, including lines eligible for external fulfillment.
- **BR-APT-055** — A No-Show shall be a Pharmacy-owned outpatient policy outcome, shall not be stored as an Inventory status, and shall not be imposed on inpatient Ward Delivery.
- **BR-APT-108** — Partial Prescription Fulfillment is permitted only for Patient Request, Stock Shortage, and Fornas Not Covered lines. No other reason is recognized by the system.
- **BR-APT-109** — For Patient Request, Pharmacy Staff may establish a Sales Order containing only selected prescription lines. Excluded prescription lines remain unfulfilled on the originating Prescription. The system shall support Salinan Resep for unfulfilled lines.
- **BR-APT-110** — For Stock Shortage before Sales Order establishment, Pharmacy Staff may establish a Sales Order containing only fulfillable prescription lines. Unavailable prescription lines remain unfulfilled on the originating Prescription. The system shall support Salinan Resep for unfulfilled lines. No outstanding fulfillment obligation, waiting demand, or backorder record shall be created.
- **BR-APT-111** — The Pharmacist remains responsible for approving the resulting fulfillment decision when professional review is required. The system shall not automatically determine alternative substitutions or external fulfillment actions.
- **BR-APT-112** — Partial Prescription Fulfillment partiality exists only between Prescription and Sales Order. Partiality does not exist between Sales Order and Dispense Order.
- **BR-APT-113** — A Sales Order fulfilled through one or more Dispense Orders is fulfillment execution and is not Partial Prescription Fulfillment policy.
- **BR-APT-114** — Outpatient Pharmacy shall not support Backorder. A stock shortage shall not create an outstanding fulfillment obligation, waiting demand, or backorder record.
- **BR-APT-115** — Outpatient stock shortage shall be resolved immediately through a Partial Sales Order of fulfillable lines and Salinan Resep for unfulfilled prescription lines. The Prescription Copy may be used by the Patient to obtain medication from another pharmacy.
- **BR-APT-116** — When outpatient inventory is insufficient, only fulfillable prescription lines may be included in the Sales Order. Unfulfillable lines remain outside the Sales Order on the originating Prescription.
- **BR-APT-117** — Outpatient Pharmacy shall not implement alternate stock source selection, fulfillment routing, inter-pharmacy sourcing, or backorder management. Inventory availability shall be evaluated against the currently available stock authority.
- **BR-APT-118** — When an outpatient shortage is identified after Sales Order establishment or financial clearance, the unfulfillable quantity shall receive an accountable Unfulfilled Medication Outcome and Salinan Resep when applicable, plus Credit Note or Refund when commercial consequences exist. It shall not be backordered or routed to an alternate stock source.
- **BR-APT-119** — Fornas validation shall classify prescription lines as Covered or Not Covered.
- **BR-APT-120** — Covered lines shall follow the normal BPJS fulfillment workflow. Coverage evidence shall be sufficient for Dispense Authorized on those lines.
- **BR-APT-121** — Not Covered lines shall not be automatically cancelled. Pharmacy may establish a separate Patient-Pay Sales Order for uncovered prescription lines. That Patient-Pay Sales Order shall be independent of the BPJS-covered Sales Order. Uncovered lines shall not remain in the BPJS fulfillment path.
- **BR-APT-122** — A Patient-Pay Sales Order shall require Payment Clearance under the normal self-pay workflow before Dispense Authorized is granted. This is financial evidence evaluation, not a Financial Clearance aggregate.
- **BR-APT-123** — Each prescription line shall follow its own authorization path: a BPJS Covered Line uses Coverage Evidence to become Dispense Authorized; a Patient-Pay Line uses Payment Clearance to become Dispense Authorized.
- **BR-APT-124** — Establishing a separate Patient-Pay Sales Order for Fornas Not Covered lines is Partial Prescription Fulfillment. One originating Prescription may result in a BPJS-covered Sales Order and a Patient-Pay Sales Order for different prescription lines.
- **BR-APT-125** — Outpatient Pharmacy shall adopt the existing legacy sales model for non-medication commercial facts. No additional invoice component model shall be introduced.
- **BR-APT-126** — BHP shall be treated as a standard catalog item and may appear as a sales line. BHP shall not be represented as a free-form invoice item.
- **BR-APT-127** — Item-specific charges, including packaging and compounding fees, shall be recorded as line-level charges on the applicable sales line.
- **BR-APT-128** — Transaction-wide adjustments, including rounding, shall be recorded as invoice-level charges on the Sales Invoice.
- **BR-APT-129** — Authorized Recipient verification shall remain an operational responsibility of the dispensing Pharmacist and shall not be system-enforced.
- **BR-APT-130** — During Medication Handover, the system may optionally record recipient phone number and relationship to the Patient for reference only. Recorded recipient information shall not constitute identity proof, legal authorization, or a workflow gate.
- **BR-APT-131** — The system shall not require identity validation, legal relationship verification, document capture, or an authorization workflow as Medication Handover recipient evidence.
- **BR-APT-132** — Patient Education shall be recorded as a lightweight Patient Education Acknowledgement before Medication Handover. The Pharmacist shall confirm that medication counseling has been provided. The acknowledgement is a handover gate.
- **BR-APT-133** — Patient Education Acknowledgement shall record education timestamp and responsible Pharmacist. It shall not require structured counseling content, medication-specific templates, patient signature, or identity of the person educated.
- **BR-APT-134** — Detailed counseling notes are optional and shall be recorded only when the Pharmacist considers additional documentation necessary. Absence of notes shall not block Medication Handover after acknowledgement is recorded.

### 7.7 Completion and history

- **BR-APT-056** — Sales Order commercial progress and fulfillment progress shall be tracked independently.
- **BR-APT-057** — Commercial resolution shall not by itself complete physical fulfillment, and physical fulfillment shall not by itself prove financial resolution.
- **BR-APT-058** — Material review, Sales Invoice formation, Dispense Order formation, clearance, dispensing, handover, exception, and correction decisions shall retain responsible party and effective business time.
- **BR-APT-059** — Source Traceability shall be preserved from Resep or Direct Medication Request through Sales Order, Sales Invoice, Dispense Order, and final outcomes.
- **BR-APT-060** — A completed or cancelled business outcome shall not be erased; a later correction shall add an accountable correcting fact. This rule does not apply to correcting an active Outpatient Queue Mapping, which is updated in place under `BR-APT-062`.

### 7.8 Outpatient workflow policy

- **BR-APT-061** — A Pharmacist may perform Telaah Resep as soon as a Resep is available; Patient arrival and Outpatient Queue Mapping shall not be prerequisites.
- **BR-APT-062** — Outpatient Queue Mapping shall associate existing business records and shall not create or modify a Resep, Hasil Telaah Resep, or Sales Order. An incorrect mapping shall be updated in place to the correct source without requiring mapping-change history.
- **BR-APT-063** — Tracker Mapping shall be used when valid tracker or registration evidence resolves one or more applicable existing Resep. Tracker Mapping shall not create a Resep or resolve a Direct Medication Request; a failed Tracker Mapping shall fall back to Manual Mapping.
- **BR-APT-064** — A directly issued or otherwise unresolved Pharmacy Queue Entry shall remain unmapped until Pharmacy Staff identifies and associates its applicable medication demand.
- **BR-APT-065** — Pharmacy Staff shall own administrative queue and pickup calling; those responsibilities shall not be transferred to the Pharmacist.
- **BR-APT-066** — In the normal BPJS Resep Elektronik flow with successful Tracker Mapping, the Patient shall require one outpatient pharmacy call: the pickup call after every applicable Dispense Order reaches `Prepared`.
- **BR-APT-067** — Outpatient Queue Mapping and General Patient Purchase Confirmation may be completed in one counter interaction when the applicable Sales Order Lines and calculated amount are available. When Tracker Mapping completes without the Patient at the counter, Pharmacy Staff shall call the Queue Number for the Purchase Confirmation interaction before Sales Invoice establishment; this administrative call shall not establish `ServedAt` or `DoneAt`.
- **BR-APT-068** — An outpatient Dispense Order and its Pharmacy Reserve through Stock Mutasi may occur before Patient arrival or Outpatient Queue Mapping, but Medication Preparation shall still require Dispense Authorized.
- **BR-APT-069** — Medication prepared for outpatient pickup shall remain in Dispensing Temporary Custody until accountable Medication Handover or No Show return movement.
- **BR-APT-070** — Before a General Patient Sales Invoice exists, Pharmacy Staff shall communicate the amount calculated from the applicable Sales Order Lines and Pricing Snapshot and obtain verbal Purchase Confirmation. Saving the confirmed transaction shall establish the Sales Invoice and its Sales Invoice Items from those lines; no separate Purchase Confirmation object or transaction shall be retained.
- **BR-APT-071** — When a General Patient declines Purchase Confirmation before the transaction is saved, no Sales Invoice shall be established and unused Pharmacy Reserve quantity shall return to Pharmacy Unit through Stock Mutasi. A Sales Invoice established after confirmation may be cancelled only while its lifecycle permits; an issued or financially cleared consequence shall follow `BR-APT-027`.
- **BR-APT-072** — General Patient Medication Preparation shall not begin before Dispense Authorized is satisfied from Payment Clearance and applicable Sales Invoice evidence.
- **BR-APT-073** — A BPJS Patient shall not be asked for Purchase Confirmation or Patient payment; the Patient-payable amount shall be zero and payment disposition shall be `Not Required`, while gross or covered value may remain non-zero.
- **BR-APT-074** — BPJS Medication Preparation may begin when Outpatient Queue Mapping, an applicable Dispense Order, Coverage Clearance, and Dispense Authorized are satisfied; an existing Sales Invoice shall not be a prerequisite.
- **BR-APT-075** — For the current outpatient BPJS policy, the Sales Invoice shall be established only as part of successfully confirmed Medication Handover; Sales Invoice establishment and handover completion shall form one accountable business outcome.
- **BR-APT-076** — Pharmacy Staff shall call the Patient for outpatient pickup after every applicable Dispense Order in the coordinated pickup reaches `Prepared`. The Pharmacist shall then perform Final Dispense Review with the Patient or caregiver present before Medication Handover.
- **BR-APT-077** — During the same counter interaction after the pickup call, the Pharmacist shall operationally verify the recipient, complete Final Dispense Review, record Patient Education Acknowledgement, and only then complete outpatient Medication Handover. Recipient verification shall not be a system-enforced gate.
- **BR-APT-078** — Successful outpatient Medication Handover shall complete the applicable Dispense Order quantity and request Remove Stock from Dispensing Temporary Unit through Stock Ledger.
- **BR-APT-079** — A BPJS No-Show before Medication Handover shall not establish or cancel a Sales Invoice. An authorized manual uncollected-medication resolution shall make the affected Dispense Order `Expired`, request Stock Mutasi from Dispensing Temporary Unit back to Pharmacy Unit when applicable, and allow the Sales Order to become `Resolved` with reason `Collection Window Expired` only after every accepted quantity and commercial consequence has a final outcome.
- **BR-APT-080** — A General Patient No-Show after payment shall use the same authorized manual uncollected-medication resolution for the fulfillment consequence, but its Sales Order shall remain `Active` until Tata Rekening or the responsible financial authority supplies the required final Credit Note, Refund, or other accountable commercial outcome.
- **BR-APT-081** — `Medication Preparation Started` shall be Pharmacy Service Start Evidence for every outpatient payer path and shall cause Patient Tracker to record `ServedAt`. Sales Invoice formation and Purchase Confirmation shall not establish outpatient pharmacy `ServedAt`.
- **BR-APT-082** — Patient Tracker shall remain authoritative for Pharmacy Queue Entry identity, Queue Number, and queue lifecycle even when Apotek owns Outpatient Queue Mapping and call purpose.
- **BR-APT-083** — No user shall enter a Legacy DU or independent medication Sales Invoice Items manually; a user action may trigger Sales Invoice formation only from accountable Sales Order Lines. Users shall not enter free-form non-medication invoice items.
- **BR-APT-084** — One Pharmacy Queue Entry may be mapped to one or more Resep or Direct Medication Requests. Each mapped demand shall retain its own Telaah Resep when applicable, Sales Order, Sales Invoices, Dispense Order, and accountable lifecycle.
- **BR-APT-085** — Mapping multiple medication demands to one Pharmacy Queue Entry shall coordinate one outpatient service and shall not merge their Sales Orders, Sales Invoices, or Dispense Orders.
- **BR-APT-086** — Within one active Registration, one active Sales Order shall coordinate through one active Dispense Order for the normal outpatient path. The common Pharmacy Queue Entry may coordinate multiple such Sales Order and Dispense Order pairs.
- **BR-APT-087** — A queue-facing per-demand progress view shall be a projection of Apotek facts for each mapped demand; Patient Tracker shall not become authoritative for Telaah Resep, Sales Invoice, or Dispense Order state.
- **BR-APT-088** — One coordinated pickup call shall occur only after every Dispense Order intended for that handover has reached `Prepared` or received an accountable exception outcome.
- **BR-APT-089** — Pharmacy Staff shall accept or decline a Direct Medication Request. Acceptance establishes the Direct Medication Request record; decline shall not establish a Direct Medication Request record or Sales Order. No Pharmacist approval, referral, escalation, or approval threshold applies.
- **BR-APT-090** — Outpatient BPJS Coverage Clearance shall require both a valid SEP for the applicable encounter and authoritative item-level Fornas coverage for the quantity being cleared.
- **BR-APT-091** — Fornas Not Covered prescription lines shall not remain on a BPJS-covered Sales Order. Covered lines shall form a BPJS-covered Sales Order. Not Covered lines may form a separate Patient-Pay Sales Order. Each Sales Order shall produce only the Sales Invoice of its own payer path.
- **BR-APT-092** — For a Patient-Pay Sales Order, the General Patient Sales Invoice shall be established only after verbal Purchase Confirmation and shall follow the self-pay workflow. The BPJS Sales Invoice of the independent BPJS-covered Sales Order shall be established only with successful Medication Handover under `BR-APT-075`.
- **BR-APT-093** — A coordinated mixed-coverage pickup call shall wait until every Dispense Order intended for the handover has Dispense Authorized from its own path and has reached `Prepared`.
- **BR-APT-094** — If the Patient declines the Patient-Pay Sales Order before its Sales Invoice is established, that Patient-Pay Sales Order shall receive an accountable declined outcome, while the independent BPJS-covered Sales Order may continue.
- **BR-APT-095** — Patient Tracker shall record outpatient pharmacy `DoneAt` when Pharmacy Staff performs the coordinated pickup call. Queue completion shall not prove Final Dispense Review, Patient Education Acknowledgement, Medication Dispense, or Medication Handover.
- **BR-APT-097** — Patient Tracker `QueueEntry` shall be the sole canonical outpatient-pharmacy queue identity. Legacy Farinv queue identity is deprecated and shall not create active queue records. Historical Farinv queue data is read-only. No dual-active queue model is permitted. Patient Tracker `Apotek-Start` and `Apotek-Done` evidence shall reference the canonical `QueueEntryId`.

### 7.9 Pharmacy and Stock Ledger boundary

- **BR-APT-098** — Pharmacy Reserve shall be implemented only as Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit. No separate `ReserveStock` contract shall exist.
- **BR-APT-099** — `Prepared` shall be a Dispense Order state only. A Dispense Order reaches `Prepared` when all required dispensing movements for that preparation have completed. `Prepared` shall not be an Inventory state.
- **BR-APT-100** — Dispensing Started shall cause Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit. Dispensing Completed or `Prepared` shall cause no Inventory action.
- **BR-APT-101** — Medication Handed Over shall cause Remove Stock from Dispensing Temporary Unit through Stock Ledger.
- **BR-APT-102** — No Show resolution shall be owned by Pharmacy. Inventory shall apply only the Stock Mutasi from Dispensing Temporary Unit back to Pharmacy Unit directed by Pharmacy and shall not store No Show status.
- **BR-APT-103** — Stock Ledger shall not own dispensing, `Prepared`, `Handed Over`, No Show, or fulfillment lifecycle states.
- **BR-APT-104** — Partial fulfillment semantics shall belong to Sales Order. A Sales Order may be fulfilled through multiple Dispense Orders.

## 8. State Machines & Lifecycles

### 8.1 Telaah Resep lifecycle

```text
Available
  -> Under Review
       -> Approved
       -> Partially Approved
       -> Rejected
```

`Approved`, `Partially Approved`, and `Rejected` are final review outcomes. Any out-of-system clarification leaves the review `Under Review`; it creates no separate state or domain event.

### 8.2 Sales Order lifecycle

```text
Established
  -> Active
       -> Resolved
       -> Cancelled
```

| State | Business meaning |
|---|---|
| Established | Accepted demand exists and commercial invoicing and fulfillment planning may begin. |
| Active | At least one accepted quantity remains commercially or physically unresolved. |
| Resolved | Every accepted quantity and required commercial consequence has an accountable final outcome. |
| Cancelled | Remaining accepted demand was ended under an authorized decision; prior outcomes remain. |

Commercial progress and fulfillment progress are separate dimensions within `Active` and are not combined into proliferating state names.

### 8.3 Sales Invoice lifecycle

```text
Established
  -> Issued
       -> Financially Cleared
            -> Resolved

Established or Issued
  -> Cancelled

Issued or Financially Cleared
  -> Adjusted or Credited
       -> Resolved
```

Payment and settlement evidence remains externally owned. `Financially Cleared` may be supported by Payment Clearance or Coverage Clearance according to payer policy.

### 8.4 Dispense Order lifecycle

```text
Established
  -> Awaiting Clearance
  -> Released
  -> Preparing
  -> Prepared
  -> Reviewed
  -> Completed

Prepared
  -> Preparing (Final Dispense Review failed; correction required)
       -> Prepared (correction completed; review required again)

Established, Awaiting Clearance, Released, Preparing, Prepared, or Reviewed
  -> Cancelled | Expired | Unfulfilled
```

`Completed` requires accountable Medication Dispense and the applicable Medication Handover. `Unfulfilled` requires a reason and resolution of allocated stock and financial consequences. Every Final Dispense Review attempt is appended as an immutable detail of the Dispense Order. A failed attempt returns `Prepared` to `Preparing`; it does not erase an earlier record or authorize handover. A later passed attempt is required before `Reviewed`.

### 8.5 Apotek quantity lifecycle

```text
Accepted Quantity
  -> Invoiced through Sales Invoice Item or Commercially Unallocated
  -> Referenced by Dispense Order Line or Not Yet Planned for Fulfillment
  -> Fulfilled | Cancelled | Expired | Other Unfulfilled Outcome
```

Billing and fulfillment branches progress independently. Final Sales Order resolution requires reconciliation of both branches, not identical document counts. Outpatient Pharmacy does not use `Backordered`.

### 8.6 Outpatient Queue Mapping relationship

```text
Unmapped
  -> Mapped
       method: Tracker Mapping | Manual Mapping
```

This relationship associates pharmacy demand with an externally owned Pharmacy Queue Entry. It does not replace the Patient Tracker queue lifecycle and does not transition Telaah Resep.

### 8.7 Outpatient pickup and handover lifecycle

```text
Prepared
  -> Ready for Pickup
       -> Patient Called
            -> Final Review Completed
                 -> Education Provided
                      -> Handed Over

Prepared, Ready for Pickup, or Patient Called
  -> No-Show
       -> manual uncollected-medication resolution
            -> Dispense Order Expired
            -> Collection Window Expired resolution reason
```

The pickup call ends the Patient Tracker queue but does not complete Medication Handover. Final Dispense Review and Patient Education Acknowledgement occur with the Patient or caregiver present after that call. The Pharmacist operationally verifies the recipient during that counter interaction; verification is not a system-enforced lifecycle step. The system may optionally record recipient phone number and relationship for reference. Patient Education Acknowledgement records timestamp and responsible Pharmacist; detailed counseling notes are optional. The applicable commercial consequence is payer-specific. A General Patient may already have a financially cleared Sales Invoice, while the current BPJS policy establishes its Sales Invoice only with successful Medication Handover.

## 9. Domain Events

| Domain Event | Business meaning |
|---|---|
| Telaah Resep Started | A Pharmacist began professional assessment of a Resep. |
| Telaah Resep Completed | Every reviewed line received a final professional disposition. |
| Outpatient Queue Mapped | A Pharmacy Queue Entry was accountably associated with applicable medication demand. |
| Direct Medication Request Accepted | A permitted demand without a Resep was accepted by Pharmacy. |
| Sales Order Established | Accepted medication demand became available for commercial invoicing and fulfillment planning. |
| Sales Invoice Established | A Medication Sale and its Sales Invoice Items were formed from Sales Order Lines. |
| Sales Invoice Issued | The Sales Invoice became an authoritative commercial document. |
| Payment Clearance Established | The responsible payment authority confirmed the applicable payment condition. |
| Coverage Clearance Established | The applicable Payer authorized covered fulfillment. |
| Dispense Authorized Evaluated | Pharmacy policy determined that preparation and dispensing may proceed for the applicable quantity. |
| Dispense Order Established | A physical fulfillment instruction and its Dispense Order Lines were formed directly from Sales Order Lines. |
| Stock Reserved | Inventory secured stock for a Dispense Order. |
| Medication Preparation Started | Physical preparation began under a released Dispense Order. |
| Medication Prepared | The medication quantity on a Dispense Order Line completed physical preparation. |
| Final Dispense Review Completed | With the Patient or caregiver present after the pickup call, Prepared Medication passed the required final professional check. |
| Final Dispense Review Failed | Prepared Medication failed its final professional review; an immutable review record was appended and the Dispense Order returned from `Prepared` to `Preparing` for correction. |
| Patient Education Acknowledged | The Pharmacist confirmed that medication counseling was provided; education timestamp and responsible Pharmacist were recorded. |
| Patient Called for Pickup | Pharmacy Staff called the Patient for outpatient Medication Handover. |
| Medication Dispensed | An accountable medication quantity was supplied for the Patient. |
| Medication Handed Over | Medication was transferred to the Patient or other recipient as determined operationally by the Pharmacist. |
| Outpatient No-Show Recorded | A Patient did not collect medication within the applicable outpatient service limit. |
| Medication Shortage Identified | Available stock could not support the intended fulfillment quantity. |
| Medication Substitution Authorized | An accountable authority approved replacement of the requested medication. |
| Dispense Order Backordered | An unresolved quantity was retained for later fulfillment. Not used in Outpatient Pharmacy. |
| Dispense Order Cancelled | An authorized decision ended the Dispense Order before completion. |
| Dispense Order Expired | The permitted fulfillment period ended without completion. |
| Unfulfilled Medication Recorded | An accepted quantity received a final non-fulfillment outcome. |
| Medication Returned | Medication previously prepared or supplied was returned. |
| Sales Invoice Credited | A Credit Note reduced or reversed a Sales Invoice consequence. |
| Refund Required | A financial resolution requires return of settled funds. |
| Pharmacy Service Started | `Medication Preparation Started` established outpatient pharmacy `ServedAt` evidence. |
| Sales Order Resolved | Every accepted quantity and required commercial consequence received an accountable final outcome. |

## 10. Business Workflows

This domain is applied through care-setting-specific workflow specifications. The domain rules, Aggregate boundaries, states, lifecycles, and Domain Events in this document remain authoritative.

| Workflow | General business outcome | Canonical workflow artifact |
|---|---|---|
| Outpatient Apotek | Coordinate outpatient queue intake, medication-demand acceptance, payer clearance, dispensing, pickup, handover, and accountable non-fulfillment resolution. | [English](./outpatient-apotek-workflow.md) · [Bahasa Indonesia](./outpatient-apotek-workflow-id.md) |

Detailed triggers, sequencing, decisions, alternatives, exceptions, compensations, handoffs, and postconditions for outpatient fulfillment are owned by the referenced workflow specification. Detailed workflows for other Care Settings require their own future workflow artifacts.
