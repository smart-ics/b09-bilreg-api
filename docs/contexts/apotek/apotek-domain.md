# Apotek Domain

**Artifact status:** Canonical business specification

**Bounded context:** Apotek (`Pelayanan Obat Pasien`)

**Version scope:** Target business model across outpatient, inpatient, emergency, and unit-dose settings

**Bahasa Indonesia companion:** [apotek-domain-id.md](./apotek-domain-id.md)

**Related business contexts:** [CPOE](../../contexts/cpoe/CPOE-DOMAIN.md), [Patient Tracker](../../contexts/pasien-tracker/TRACKER-DOMAIN.md), [Tata Rekening](../../contexts/TataRekening/02-domain.md)

## 1. Business Overview

### 1.1 Purpose and value

Apotek turns patient-specific medication demand accepted by Pharmacy into accountable commercial invoicing and physical Apotek. It separates the legacy `Trs.DU (DO-Bill)` combination of stock delivery and billing into independent `Invoice` and `Dispensing` lifecycles, coordinated by a `Sales Order`.

The business must ensure that:

- the clinician's original Resep remains authoritative and traceable;
- only professionally accepted medication demand enters a Sales Order;
- Invoices may be formed from Sales Order Items independently of Dispensings formed from those same Sales Order Items;
- billing, payment or coverage, Current Stock, Available Stock, preparation, and handover remain distinct business facts;
- partial billing and partial fulfillment remain quantitatively accountable; and
- every accepted quantity reaches an accountable fulfilled or unfulfilled outcome.

### 1.2 Scope

This context covers:

1. Telaah Resep and Jual Bebas acceptance;
2. Sales Order establishment, commercial invoicing, and fulfillment planning;
3. Medication Sale and Invoice formation;
4. Dispensing establishment and physical dispensing;
5. commercial or coverage clearance for fulfillment;
6. Medication Dispense and Medication Handover; and
7. shortage, substitution, cancellation, return, and other non-fulfillment outcomes; Outpatient Pharmacy does not support Backorder; and
8. the currently defined outpatient queue, payer, pickup, and no-show workflow policy.

It applies across outpatient, inpatient, emergency, and Unit Dose Dispensing settings.

### 1.3 Business boundaries

Apotek owns Hasil Telaah Resep, Sales Order, Medication Sale represented by Invoice, Dispensing, Medication Dispense, Medication Handover, and fulfillment resolution.

It relies on related contexts without taking over their authority:

- CPOE or another clinical-order authority owns the original Resep and clinician intent;
- Medication Catalog or formulary authority owns medication identity and formulary policy;
- Inventory owns Current Stock (authoritative physical inventory quantity) and stock movements. Available Stock is a Pharmacy fulfillment-planning concept and is not an Inventory stored balance;
- Payment owns receipts and settlement evidence;
- Tata Rekening owns registration-level Financial Responsibility, payer allocation, finalization, settlement initiation, and the financial permission that determines whether an Apotek Invoice may still be revised. Apotek consumes that permission as an external business fact and does not own Financial Clearance rules;
- Patient Tracker owns outpatient queue identity and lifecycle; and
- the clinical care context owns Medication Administration.

Purchasing, supplier management, replenishment, warehouse transfer, enterprise accounting, and Medication Administration are outside this context.

Outpatient queue identity and lifecycle remain externally owned by Patient Tracker. The Patient Tracker `QueueEntry` is the sole canonical outpatient-pharmacy queue identity. Legacy Farinv queue identity is deprecated, shall not create active queue records for new outpatient-pharmacy interactions, and historical Farinv queue data is read-only. No dual-active queue model is permitted.

Apotek owns the outpatient business decision that associates a Pharmacy Queue Entry with the applicable medication demand and owns the payer, pickup, handover, and no-show policy applied after that association.

For Outpatient Pharmacy, the fulfillment boundary is the active Registration Period. A Resep may be reviewed, re-reviewed, and fulfilled while its originating Registration remains active. No separate Fulfillment Episode concept exists.

For outpatient pharmacy queues, Patient Tracker records `CreatedAt` when the Queue Number is issued, `ServedAt` when the first applicable Dispensing enters `Preparing`, and `DoneAt` when queue completion occurs. Queue completion may be triggered by the coordinated pickup call or by No Show Resolution (`WF-APT-RJ-007`) when the Queue Entry is still `In Service` because the pickup call has not occurred. The queue lifecycle remains `Waiting` → `In Service` → `Done`. No new queue status is introduced, and no pharmacy workflow state is added to the Queue Entry. Those queue milestones describe operational queue progress and do not prove Medication Handover. Queue `Done` only means the queue service lifecycle has been completed. Patient Tracker `Apotek-Start` and `Apotek-Done` evidence remain reusable but shall reference the canonical `QueueEntryId`, not Farinv queue identity.

### 1.4 Central business separation

The target flow is:

```text
Resep or Jual Bebas
  -> Telaah Resep or direct acceptance
  -> Sales Order
       -> zero or more Invoices
       -> zero or more Dispensings
            -> Medication Dispense
            -> Medication Handover
```

A Resep does not become a Sales Order. A completed professional decision authorizes a new Sales Order while preserving the Resep as its clinical source.

## 2. Ubiquitous Language
| Term | Definition |
|---|---|
| Apotek | The bounded context that coordinates patient-specific medication demand from Pharmacy acceptance through commercial invoicing and physical fulfillment resolution. |
| Patient Medication Demand | A patient-specific need for medication originating from a Resep or Jual Bebas. |
| Resep | The clinician's authoritative intent for medication to be supplied or administered to a Patient. |
| Resep Elektronik | A Resep created and transmitted through an electronic clinical-order authority. |
| Resep Fisik | A nonelectronic Resep that must be recorded before Pharmacy can review it. |
| Resep Kerja | Pharmacy's operational copy of one Resep, created from the Prescription Contract at intake. Pharmacy Telaah Resep, Sales Order establishment, and subsequent outpatient fulfillment operate on this copy. The original Resep remains owned by CPOE or another clinical-order authority. |
| Jual Bebas | A retail-style medication request without a Resep, originating outside the hospital care workflow, that Pharmacy Staff may accept or decline. Identifier form: `JualBebas`. |
| Baris Resep | One requested medication, dosage instruction, and quantity within a Resep. |
| Source Traceability | The accountable relationship from Medication Sale and dispensing outcomes back to their Sales Order, accepted demand, and original source. |
| Telaah Resep | The Pharmacist's administrative, pharmaceutical, and clinical assessment of a Resep Kerja. |
| Hasil Telaah Resep | The professional decision on a Resep: approved, partially approved, or rejected. Accepted medication is materialized as a Sales Order Item. |
| Accepted Medication Item | A medication item professionally accepted for inclusion in a Sales Order, independently of Current Stock and Available Stock. |
| Sales Order | The accepted medication demand owned by Pharmacy and used as the common source of Medication Sales and Dispensings. |
| Sales Order Item | One accepted medication, quantity, instructions, and applicable commercial basis within a Sales Order. |
| Accepted Quantity | The maximum quantity of a Sales Order Item available for accountable invoicing, physical fulfillment, and resolution. |
| Medication Sale | The commercial transaction represented by one Invoice from one Sales Order. |
| Invoice | A commercial/charge document representing medication sale information that serves as source information for Tata Rekening. It is the Aggregate Root representing one Medication Sale. |
| Legacy DU | The legacy `Trs.DU (DO-Bill)` transaction that combined medication billing and stock-delivery concerns; in the target model its facts are represented through an Invoice and one or more Dispensings coordinated by the same Sales Order and traced at item level. |
| Invoice Item | One medication, BHP, or other catalog sales item, quantity, price, discount, item-level charges, and value within an Invoice. Every medication or BHP Invoice Item originates from exactly one Sales Order Item and represents the portion of that item billed by the invoice. |
| BHP | A standard catalog item that may appear as a sales item. It is not a free-form invoice component. |
| Item-level Charge | An item-specific commercial charge attached to a sales item, such as packaging or compounding fees. |
| Invoice-level Charge | A transaction-wide commercial adjustment stored as an Invoice header attribute (for example `Pembulatan`), not as a child entity. |
| Pricing Snapshot | The immutable commercial basis used when an Invoice is established. |
| Payer | The Patient, BPJS, insurer, company, or other party expected to bear a medication charge. |
| Financial Charge | The financial consequence supplied to Tata Rekening from a Medication Sale. |
| Purchase Confirmation | A General Patient's verbal decision to proceed after Pharmacy Staff communicates the calculated amount before Invoice establishment. It is a workflow activity and is not retained as a separate business object or transaction. |
| General Patient | A Patient whose applicable Medication Sale requires Purchase Confirmation and Patient payment before Medication Preparation. |
| BPJS Patient | A Patient whose applicable Medication Sale is covered through BPJS policy without Patient Purchase Confirmation or Patient payment. |
| Payment Clearance | Evidence that the required payment condition has been satisfied. |
| Coverage Clearance | Evidence that the applicable payer authorizes fulfillment without immediate Patient payment. For outpatient BPJS fulfillment, it combines a valid SEP for the encounter with item-level coverage determined from the authoritative Fornas mapping. |
| Dispense Authorized | A policy evaluation result indicating medication preparation and dispensing may start, derived from financial and coverage evidence. It is not an aggregate, entity, source of truth, or transaction boundary. |
| Financial Adjustment | An accountable correction to a Medication Sale or its financial consequences. Owned and persisted by Tata Rekening. Not an Apotek aggregate. |
| Credit Note | An exception commercial document that reduces or reverses an Invoice amount when Tata Rekening no longer permits direct Invoice revision. Owned and persisted by Tata Rekening. Not an Apotek aggregate, entity, or table. Apotek may retain a correlation identity when Tata Rekening returns one. |
| Refund | The accountable return of previously settled funds. Owned by Tata Rekening, with Cashier execution where settlement requires it. Not an Apotek aggregate. |
| Dispensing | The authoritative instruction to physically fulfill one or more Sales Order Items from one Sales Order. |
| Dispensing Item | One medication quantity to be physically fulfilled within a Dispensing. It references exactly one Sales Order Item and carries the applicable care setting and Dispense Cycle. |
| Dispense Cycle | A defined fulfillment period or batch, especially for inpatient and Unit Dose Dispensing. |
| Unit Dose Dispensing | Fulfillment in patient-specific unit doses or defined administration periods. |
| Current Stock | The current physical inventory recorded by the inventory subsystem. Current Stock reflects physical inventory state and inventory movements. It answers: "How much inventory physically exists?" |
| Available Stock | The quantity that can still be committed to a new Sales Order. Available Stock is a fulfillment-planning concept used during Sales Order establishment and shortage evaluation. It answers: "How much inventory can still be promised to a new order?" Available Stock SHALL NOT be considered equivalent to Current Stock. The calculation formula is intentionally undefined in this domain and is reserved for a future inventory-planning design activity. |
| Stock Availability | Retired as a standalone quantity concept. Prefer Current Stock for physical inventory owned by the inventory subsystem, and Available Stock for fulfillment-planning quantity that can still be committed to a new Sales Order. Do not treat this phrase as equivalent to either term. |
| Pharmacy Unit | The ordinary pharmacy Stock Location from which outpatient medication is issued into dispensing custody. |
| Dispensing Temporary Unit | The pharmacy Stock Location that holds medication under active dispensing custody after Dispensing Started and before handover or No Show return. |
| Pharmacy Reserve | Pharmacy-directed placement of stock for a Dispensing, implemented only as Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit. |
| Stock Mutasi | Stock Ledger's accountable transfer of quantity between Stock Locations without changing Receipt Source. |
| Remove Stock | Stock Ledger's accountable outbound removal of quantity from a Stock Location, including from Dispensing Temporary Unit on Medication Handover. |
| Medication Preparation | Picking, counting, labelling, packaging, and otherwise preparing medication for fulfillment. |
| Compounding | Preparing a medication product from ingredients or components for a specific fulfillment need. |
| Final Dispense Review | The final professional Medication Review performed by the Pharmacist with the Patient or caregiver present after the pickup call and before Medication Handover. |
| Final Dispense Review Record | An immutable record of one Final Dispense Review attempt for a Dispensing, including its passed or failed outcome, reason when failed, responsible Pharmacist, effective business time, and affected quantity. One Dispensing may have multiple review records. |
| Prepared Medication | Medication whose physical preparation is complete and awaits final review or handover. |
| Dispensing Temporary Custody | Medication quantity held in Dispensing Temporary Unit after Dispensing Started and before Medication Handover or No Show return. |
| Medication Dispense | The accountable fact that a quantity of medication was actually supplied for a Patient. |
| Medication Handover | The accountable transfer of medication to an Authorized Recipient. |
| Authorized Recipient | The Patient, caregiver, practitioner, ward, or other party to whom medication is handed over. Recipient verification is an operational Pharmacist responsibility and is not system-enforced. |
| Patient Education | Medication counseling provided to the Patient or caregiver before Medication Handover. |
| Patient Education Acknowledgement | The lightweight record that the Pharmacist confirmed counseling was provided. It records education timestamp and responsible Pharmacist. Detailed counseling notes are optional. |
| Fulfilled Quantity | The quantity of a Sales Order Item that reached a successful Medication Dispense outcome. |
| Partial Prescription Fulfillment | Establishing Sales Order(s) from a subset of prescription items when Patient Request, Stock Shortage, or Fornas Not Covered applies. |
| Partial Fulfillment | Fulfillment execution in which one Sales Order is fulfilled through multiple Dispensings, or less than the total Accepted Quantity of a Sales Order Item is fulfilled while another quantity remains unresolved or receives a different outcome. This is not Partial Prescription Fulfillment policy. |
| Fulfillment Completion | The condition in which every Accepted Quantity has an accountable final outcome. |
| Medication Administration | The clinical fact that medication was actually given to or consumed by the Patient; it is externally owned. |
| Medication Shortage | Insufficient Available Stock to commit the intended quantity to a new Sales Order, or insufficient physical inventory to fulfill an already accepted quantity. Pre-establishment shortage evaluation uses Available Stock, not Current Stock. |
| Stock Discrepancy | A difference between recorded and physical stock that affects fulfillment. |
| Backorder | An unresolved quantity retained for later fulfillment when supply becomes available. Outpatient Pharmacy does not support Backorder. |
| Medication Substitution | The accountable replacement of a requested medication product under applicable professional authority. |
| Unfulfilled Medication Outcome | A final, accountable reason that an accepted medication quantity was not fulfilled. |
| Copy Resep | An accountable Copy Resep record of prescribed medication or quantity not included in the Sales Order or not fulfilled, when applicable. |
| Dispense Cancellation | The accountable ending of a Dispensing before successful fulfillment. |
| Fulfillment Expiry | The ending of a fulfillment opportunity because its permitted service period elapsed. |
| Medication Return | The accountable return of medication previously prepared, transferred, or handed over. |
| Return to Stock | Inventory's authoritative acceptance of eligible returned medication into Current Stock. |
| No-Show | An outpatient outcome in which the Patient does not collect medication and an authorized uncollected-medication resolution is recorded. |
| Collection Window | The configurable maximum days that prepared outpatient medication may remain awaiting pickup. Default is 7 days. The window starts when the Dispensing first becomes Ready for Pickup. |
| Pickup Expired | The Serah Obat worklist category after the Collection Window elapses without Medication Handover. It is a projection category, not a Dispensing state. |
| Collection Window Override | The authorized-pharmacist fact that permits Medication Handover after Pickup Expired. It records override reason, authorizing pharmacist, and effective business time. |
| Pharmacy Queue Entry | A Patient's participation in an outpatient pharmacy queue represented by one Patient Tracker `QueueEntry` whose identity and lifecycle are owned by Patient Tracker. |
| Pharmacy Queue Close | The Pharmacy Staff fact that ends a Pharmacy Queue Entry that was not progressed into the pharmacy workflow. It records a mandatory close reason, responsible Pharmacy Staff, and effective business time. Patient Tracker then sets the entry `Withdrawn` from `Waiting`. |
| Legacy Farinv Queue Entry | A deprecated historical pharmacy queue record from the Farinv subsystem. It is read-only and shall not be created for new outpatient-pharmacy interactions. |
| Outpatient Queue Mapping | The accountable association of a Pharmacy Queue Entry with the originating demand source only: a Resep Kerja or a Jual Bebas. It shall not target a Sales Order, Invoice, or Dispensing. |
| Tracker Mapping | Outpatient Queue Mapping established automatically when valid Patient Tracker or registration evidence resolves one or more existing Resep. The association is recorded against the corresponding Resep Kerja. It does not create a Resep, does not apply to a Jual Bebas, and does not map to a Sales Order, Invoice, or Dispensing. |
| Manual Mapping | Outpatient Queue Mapping established by Pharmacy Staff after the Queue Number and the applicable Resep Kerja or Jual Bebas are identified. |
| Pharmacy Service Start Evidence | The `Medication Preparation Started` fact that causes the outpatient Pharmacy Queue Entry to record `ServedAt`. |
| Care Setting | The operational setting whose policy applies to fulfillment, such as outpatient, inpatient, or emergency care. |
| Outpatient Fulfillment | Apotek under outpatient arrival, queue, payment, pickup, and no-show policies. |
| Inpatient Fulfillment | Apotek under inpatient ward, scheduled supply, and return policies. |
| Emergency Fulfillment | Apotek under the applicable urgent-care policy. |
| Dose Window | A defined administration period used to plan a Dispense Cycle without representing Medication Administration itself. |
| Ward Delivery | Medication Handover from Pharmacy to an Authorized Recipient in an inpatient ward. |

## 3. Business Capabilities

### 3.1 Medication Demand Acceptance

Accept reviewed Resep demand or an authorized Jual Bebas without changing the original clinical intent.

### 3.2 Telaah Resep

Establish the professional disposition of each Resep and its items, including partial acceptance and accepted substitutes.

### 3.3 Sales Order Management

Establish and maintain the accepted medication demand, its quantities, source traceability, and final resolution.

### 3.4 Commercial Invoicing

Form one or more Medication Sales and Invoices from Sales Order Items without depending on Dispensing count or timing. Invoice Items express the quantity and value billed from their source Sales Order Items.

### 3.5 Fulfillment Planning

Form one or more Dispensings from Sales Order Items based on care setting, quantity, location, cycle, and fulfillment policy.

### 3.6 Dispense Authorization

Evaluate financial and coverage evidence to determine when medication preparation and dispensing may start (`Dispense Authorized`).

### 3.7 Physical Dispensing

Coordinate Pharmacy Reserve through Stock Mutasi, Medication Preparation, Compounding, Final Dispense Review, Medication Dispense, Medication Handover, and No Show stock return.

### 3.8 Partial and Unit-Dose Fulfillment

Support Partial Prescription Fulfillment at the Prescription-to-Sales Order boundary, independently counted billing and fulfillment tranches through multiple Dispensings per Sales Order, Unit Dose Dispensing, and Dose Windows.

### 3.9 Exception and Return Resolution

Resolve shortages, discrepancies, substitutions, cancellations, expiries, returns, no-shows, and required financial consequences. Outpatient Pharmacy does not retain Backorder or route fulfillment to an alternate stock source.

### 3.10 Cross-Setting Traceability

Preserve quantitative and source traceability across outpatient, inpatient, emergency, billing, dispensing, and handover outcomes.

### 3.11 Outpatient Queue Coordination

Associate an externally owned Pharmacy Queue Entry with the originating Resep Kerja or Jual Bebas through Tracker Mapping or Manual Mapping without making queue arrival a prerequisite for Telaah Resep. Mapping shall not target a Sales Order, Invoice, or Dispensing.

### 3.12 Outpatient Payer, Pickup, and No-Show Resolution

Apply General Patient and BPJS clearance, invoice-timing, pickup, handover, and no-show policies while preserving independent commercial and fulfillment outcomes.

## 4. Actors & Roles

### 4.1 Dokter Penulis Resep

Owns the original Resep. Any clarification with the Pharmacist occurs outside the system and does not alter that original Resep. The Dokter Penulis Resep does not own Pharmacy acceptance, Invoice formation, or dispensing execution.

### 4.2 Pharmacist

Owns Hasil Telaah Resep, Medication Substitution authorized during Telaah Resep, Final Dispense Review, operational Authorized Recipient verification, and Patient Education Acknowledgement. Authorized Recipient verification is not a system-enforced gate. The Pharmacist does not own administrative outpatient queue calling. Medication Substitution authority ends when the Sales Order is established.

### 4.3 Pharmacy Staff

Coordinates accepted demand, Sales Order progression, outpatient administrative interaction, Medication Preparation, Compounding, and accountable handover within assigned authority. For outpatient fulfillment, Pharmacy Staff calls Queue Numbers, establishes Manual Mapping, communicates the calculated General Patient amount before Invoice establishment, saves the confirmed Invoice, prepares medication according to the Dispensing, and performs the pickup call. For a Jual Bebas, Pharmacy Staff accepts or declines without creating a Resep. Optional Pharmacist consultation is operational SOP guidance only and is not modeled as an approval gate. When outpatient stock cannot support full fulfillment, Pharmacy Staff includes only fulfillable items in the Sales Order and issues Copy Resep for unfulfilled items. Pharmacy Staff shall not create Backorder, select an alternate stock source, or substitute the medication.

### 4.4 Patient or Caregiver

Provides applicable confirmation and payment, receives education, and accepts medication when acting as an Authorized Recipient.

### 4.5 Cashier

Receives payment and supplies Payment Clearance evidence. The Cashier does not establish medication eligibility or fulfillment quantity.

### 4.6 Inpatient Authorized Recipient

Receives Ward Delivery for the Patient and remains identifiable in the Medication Handover outcome. Receipt does not represent Medication Administration.

### 4.7 Pharmacy Supervisor

An authorized pharmacist under operational policy. Authorizes returns, corrections, Collection Window Override, expired collection close, and other dispensing exceptions. Exception handling is authority-based; no monetary approval threshold model exists. Which pharmacist is authorized is operational policy. Pharmacy Supervisor is the named operational designation used where a distinct authorizing pharmacist is required.

## 5. Domain Objects

### 5.1 Telaah Resep

Represents Pharmacy's professional assessment process for one Resep Kerja. It retains per-item decisions, the responsible Pharmacist, and source traceability without rewriting the original Resep. Accepted medication is materialized in the Sales Order.

### 5.2 Sales Order

Represents one accepted patient-specific medication demand. It owns Sales Order Items, accepted quantities, and resolution progress. It coordinates commercial invoicing and physical fulfillment through Invoices and Dispensings whose items reference its Sales Order Items.

### 5.3 Invoice

Represents one Medication Sale from one Sales Order. It owns Invoice Items, payer classification, commercial value, financial disposition, adjustments, and Financial Charge outcome. Each medication Invoice Item identifies the one Sales Order Item from which it originates.

The Sales Order Item is the sales-order item in this context. Its relationship with Invoice Items expresses the billed portion directly:

```text
Sales Order Item
        │
        └──> Invoice Item
```

One Sales Order Item may be represented by Invoice Items in one or more Invoices for partial billing. Each Invoice Item originates from exactly one Sales Order Item.

### 5.4 Dispensing

Represents one physical fulfillment instruction from one Sales Order. It owns Dispensing Items, each of which references exactly one Sales Order Item from that Sales Order, together with preparation and review progress, Medication Dispense outcomes, and final fulfillment disposition.

### 5.5 Medication Dispense

Represents the actual medication and quantity supplied for a Patient, including responsible party, effective time, and source Dispensing.

### 5.6 Medication Handover

Represents transfer of medication at handover time, including destination when applicable and Patient Education Acknowledgement. Optional recipient phone number and relationship to the Patient may be recorded for reference only and do not prove identity or legal authorization.

### 5.7 Outpatient Queue Mapping

Represents the active association between an externally owned Pharmacy Queue Entry and the originating demand source. The pharmacy-side endpoint is a Resep Kerja or a Jual Bebas only. Mapping shall not target a Sales Order, Invoice, or Dispensing. After a Sales Order exists, worklists join from the mapped Resep Kerja or Jual Bebas to downstream Sales Order, Invoice, and Dispensing records; those joins are not mapping targets. If the selected source is incorrect, the association is updated in place and no mapping-change history is required. It records the current mapping method without owning Queue Number or queue lifecycle.

### 5.8 Unfulfilled Medication Outcome

Represents the final reason an accepted quantity was not fulfilled and identifies any Copy Resep, return, or financial correction required. Outpatient Pharmacy does not use backorder closure.

### 5.9 Final Dispense Review Record

Represents one immutable Final Dispense Review attempt owned as a detail of one Dispensing. Review records are appended rather than replaced so repeated failed and successful reviews remain accountable in their original order.

### 5.10 Patient Education Acknowledgement

Represents the Pharmacist's confirmation that medication counseling was provided before Medication Handover. It records education timestamp and responsible Pharmacist. It is not a structured counseling-content record. Detailed counseling notes may be attached only when the Pharmacist considers additional documentation necessary.

### 5.11 Collection Window Override

Represents an authorized pharmacist's permission to complete Medication Handover after Pickup Expired. It records override reason, authorizing pharmacist, and effective business time. It does not expire the Dispensing or return stock.

### 5.12 Pharmacy Queue Close

Represents Pharmacy Staff ending a Pharmacy Queue Entry that was not progressed into the pharmacy workflow. It records a mandatory close reason, responsible Pharmacy Staff, and effective business time. It requests Patient Tracker `Withdrawn` from `Waiting` and does not add a queue state.

### 5.13 Pharmacy and Stock Ledger boundary

Pharmacy owns Sales Order, Dispensing, dispensing lifecycle, `Prepared`, `Handed Over`, and No Show resolution. Stock Ledger owns Current Stock, Mutasi, Remove Stock, and movement history only.

Available Stock is not Current Stock. Current Stock is the inventory subsystem's physical inventory quantity. Available Stock is Pharmacy's fulfillment-planning concept for the quantity that can still be committed to a new Sales Order. It is used during Sales Order establishment and shortage evaluation. Available Stock is not a Stock Ledger stored balance and SHALL NOT be treated as equivalent to Current Stock. The Available Stock calculation formula is not specified here and is reserved for a future inventory-planning design activity.

Pharmacy Reserve is implemented only as Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit. Medication Handover requests Remove Stock from Dispensing Temporary Unit. No Show resolution requests Stock Mutasi from Dispensing Temporary Unit back to Pharmacy Unit. `Prepared` is a Dispensing state only and is not an Inventory state. Partial fulfillment semantics belong to Sales Order; one Sales Order may be fulfilled through multiple Dispensings.

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

### 5.14 Resep Kerja

Supporting document. Pharmacy's operational copy of one Resep, created at intake from the Prescription Contract (BA-06). Telaah Resep and Sales Order establishment operate on this copy. The intake copy is not silently rewritten from the clinical source. Identifier form: `ResepKerja`. Not an aggregate root.

### 5.15 Jual Bebas

Supporting document. One accepted retail-style medication request without a Resep. Pharmacy Staff accepts or declines it. Decline creates no record (`BR-APT-089`). Identifier form: `JualBebas`. Not an aggregate root.

### 5.16 Copy Resep

Supporting document. Accountable record of prescribed medication or quantity excluded from a Sales Order or not fulfilled, when applicable (`BR-APT-054`, `BR-APT-109`–`115`, `BR-APT-118`). May be issued before or after Sales Order establishment. Identifier form: `CopyResep`. Not an aggregate root.

## 6. Aggregates

### 6.1 Telaah Resep Aggregate

**Aggregate Root:** `Telaah Resep`

The aggregate keeps the Resep Kerja source reference, per-item professional disposition, responsible Pharmacist, and completion outcome mutually consistent. It stores no clarification communication and cannot modify the clinician's original Resep.

### 6.2 Sales Order Aggregate

**Aggregate Root:** `Sales Order`

The aggregate owns Sales Order Items, accepted quantities, fulfilled quantities, unfulfilled outcomes, and overall resolution. It coordinates the commercial and fulfillment lifecycles and reconciles quantities recorded by Invoice Items and Dispensing Items that reference each Sales Order Item.

It ensures that invoiced quantities and physical fulfillment remain traceable and do not exceed their applicable Sales Order Item authority. It does not own Invoice payment settlement, Current Stock, Available Stock, or physical dispensing execution. During Sales Order establishment, fulfillable quantity is evaluated against Available Stock; that evaluation does not store Available Stock on the Sales Order and does not treat Current Stock as the promised quantity.

### 6.3 Medication Sale Aggregate

**Aggregate Root:** `Invoice`

The aggregate represents one Medication Sale. It keeps Invoice Items, Pricing Snapshot, Payer, item-level charges on Invoice Items, transaction-wide commercial totals on the Invoice header, financial disposition, and Financial Charge outcome mutually consistent. It does not own Credit Notes, Refunds, or Financial Adjustment documents; those remain Tata Rekening-owned. The Invoice may retain a correlation identity to a Tata Rekening financial correction. General Patient verbal Purchase Confirmation is evidenced by the accountable establishment of the Invoice and is not retained as a separate object. Non-medication commercial facts use the legacy sales model: BHP as a catalog sales item, item-specific charges on the item, and transaction-wide adjustments as Invoice header totals. No additional invoice component model exists.

An Invoice references exactly one Sales Order but may cover one or more of its Sales Order Items.

The Invoice remains mutable while Tata Rekening still permits modification. Mutability is not an InvoiceStatus. After Issue, normal correction revises the same Invoice while that permission remains. When direct revision is no longer permitted, financial correction is delegated to Tata Rekening. Apotek does not record an Apotek Credit Note. Apotek consumes Tata Rekening financial permission as an external business fact and does not own, calculate, or persist Financial Clearance rules.

### 6.4 Dispensing Aggregate

**Aggregate Root:** `Dispensing`

The aggregate owns Dispensing Items and keeps their physical preparation, its one-to-many immutable Final Dispense Review Records, Patient Education Acknowledgement, Collection Window Override when applicable, Medication Dispense, Medication Handover, cancellation, expiry, return, and non-fulfillment outcomes mutually consistent.
A Dispensing references exactly one Sales Order and may fulfill one or more of its Sales Order Items. Every Dispensing Item references exactly one Sales Order Item from that Sales Order; one Sales Order Item may be fulfilled through multiple Dispensing Items across multiple Dispensings.

### 6.5 Cross-aggregate relationship
A Sales Order may have zero or more Invoices and zero or more Dispensings. Invoices and Dispensings are not required to have equal counts or formation times.

Their business correlation is expressed through Invoice Item and Dispensing Item references to Sales Order Items and Dispense Authorized policy evaluation over financial and coverage evidence. Sharing a Sales Order does not by itself establish that every Invoice clears every Dispensing.

Outpatient Queue Mapping is an active relationship between an externally owned Pharmacy Queue Entry and a Resep Kerja or Jual Bebas. It is not an Aggregate Root of Apotek, not a transaction log of mapping changes, and not an association to a Sales Order, Invoice, or Dispensing. Patient Tracker remains authoritative for Queue Session, Queue Number, and queue lifecycle. Pharmacy Queue Close is an Apotek operational fact that requests Patient Tracker `Withdrawn`; it is not an Aggregate Root and not a queue state.

## 7. Business Rules

### 7.1 Sources and professional acceptance

- **BR-APT-001** — Patient Medication Demand shall originate from exactly one Resep or Jual Bebas.
- **BR-APT-002** — The original Resep and clinician intent shall remain owned by its clinical-order authority.
- **BR-APT-003** — Every Resep shall complete Telaah Resep before any of its items enter a Sales Order.
- **BR-APT-004** — Only a Pharmacist shall establish the final decision for each Baris Resep as accepted as prescribed, accepted with a substitute, or rejected. The original Resep shall not be modified; accepted medication is recorded on a Sales Order Item.
- **BR-APT-005** — A Hasil Telaah Resep shall record a final decision for every reviewed Baris Resep. Clarification with the Dokter Penulis Resep occurs outside the system, is not recorded as a state or transaction, and the review remains `Under Review` until a decision is made.
- **BR-APT-006** — A rejected Resep shall not establish a Sales Order.
- **BR-APT-007** — A partially approved Resep may establish a Sales Order containing only Accepted Medication Items.
- **BR-APT-008** — Clinical acceptance shall be independent of Current Stock and Available Stock; stock facts shall not rewrite professional eligibility.
- **BR-APT-009** — A Jual Bebas is a retail-style medication request originating outside the hospital care workflow. Pharmacy Staff shall accept or decline it without creating a Resep. Pharmacist consultation may occur operationally but is optional SOP guidance only and shall not be modeled as approval workflow, authority threshold, escalation, risk classification, domain state, or business-rule gate.

### 7.2 Sales Order

- **BR-APT-010** — A Sales Order shall originate from exactly one completed accepted-demand source.
- **BR-APT-011** — One Resep shall establish at most one active Sales Order per Registration while that Registration remains active, except that Fornas Not Covered items may establish a separate Patient-Pay Sales Order independent of the BPJS-covered Sales Order under `BR-APT-119`–`BR-APT-124`.
- **BR-APT-012** — A Sales Order shall contain at least one Sales Order Item with a positive Accepted Quantity.
- **BR-APT-013** — Every Sales Order Item shall retain Source Traceability to its Baris Resep or Jual Bebas item; for an accepted substitute, the Sales Order Item contains the substitute while its source reference remains the original Baris Resep.
- **BR-APT-014** — A Sales Order shall not be an Invoice, payment record, Pharmacy Reserve movement, Dispensing, or Medication Dispense evidence.
- **BR-APT-015** — Invoice and Dispensing formation may occur independently and at different business times.
- **BR-APT-016** — The total active quantity of Dispensing Items referencing a Sales Order Item shall not exceed its unresolved Accepted Quantity.
- **BR-APT-017** — Fulfilled Quantity shall not exceed its Dispensing Item quantity.
- **BR-APT-018** — Every Accepted Quantity shall eventually be fulfilled, cancelled, expired, or assigned another accountable Unfulfilled Medication Outcome. Outpatient Pharmacy shall not assign Backorder.
- **BR-APT-019** — A Sales Order shall reach Fulfillment Completion only when every Accepted Quantity has a final accountable outcome.
- **BR-APT-105** — Outpatient Pharmacy fulfillment boundary shall be the active Registration Period. A Resep may be reviewed, re-reviewed, and fulfilled while its originating Registration remains active. No separate Fulfillment Episode concept shall exist.
- **BR-APT-106** — Prescription repeat entitlement shall be owned by the Resep through the Legacy Resep `Iter` mechanism. The system shall allocate Iter, track Iter consumption, and calculate remaining Iter.
- **BR-APT-107** — The system shall not determine whether an unused Iter remains valid for fulfillment. The Pharmacist shall decide whether an unused Iter may still be honored at fulfillment time and may decline fulfillment even when remaining Iter exists.

### 7.3 Medication Sale and Invoice

- **BR-APT-020** — Every Medication Sale shall be represented by exactly one Invoice.
- **BR-APT-021** — Every Invoice shall derive from exactly one Sales Order. Every medication Invoice Item shall originate from exactly one Sales Order Item of that Sales Order.
- **BR-APT-022** — A Sales Order may produce zero, one, or multiple Invoices.
- **BR-APT-023** — An Invoice may cover one or more Sales Order Items through its Invoice Items and shall preserve each Invoice Item's source Sales Order Item, billed quantity, and value.
- **BR-APT-024** — An Invoice Item shall not introduce a free-form non-medication invoice component. BHP shall appear only as a catalog sales item. Item-specific charges shall be item-level charges. Transaction-wide adjustments shall be Invoice header attributes.
- **BR-APT-025** — An Invoice shall retain the Pricing Snapshot and Payer applicable when it is established.
- **BR-APT-026** — Invoice formation shall not prove that stock is available, transferred to Dispensing Temporary Unit, prepared, dispensed, or handed over.
- **BR-APT-027** — Invoice mutability is governed by financial permission consumed from Tata Rekening, not by Invoice Issue or Invoice `Financially Cleared`. While Tata Rekening still permits modification, Invoice correction shall use normal Invoice revision of that same Invoice, with accountable actor and effective business time. When Tata Rekening no longer permits direct Invoice revision, Credit Note, Refund, and Financial Adjustment remain exception mechanisms **owned and persisted by Tata Rekening**. Apotek shall not persist a Credit Note, Refund, or Financial Adjustment entity and shall not introduce a replacement financial-correction aggregate. Silent replacement without that permission and accountability is forbidden. Apotek shall not define, calculate, or own the internal business rules Tata Rekening uses to determine such permission, and shall not treat Financial Clearance as an Apotek-owned object or rule set.
- **BR-APT-028** — Every Financial Charge sent to Tata Rekening shall retain Source Traceability to its Invoice and Sales Order.

### 7.4 Dispensing and dispensing

- **BR-APT-029** — Every Dispensing shall derive from Sales Order Items of exactly one Sales Order, and every Dispensing Item shall reference exactly one Sales Order Item from that Sales Order.
- **BR-APT-030** — A Sales Order may produce zero, one, or multiple Dispensings.
- **BR-APT-031** — A Dispensing may cover one or more Sales Order Items and shall preserve each direct line reference and quantity. One Sales Order Item may be split across multiple Dispensing Items.
- **BR-APT-032** — Dispensing count, quantity split, and timing may differ from Invoice count, value split, and timing.
- **BR-APT-033** — Stock Mutasi and Remove Stock shall remain authoritative Stock Ledger outcomes requested by Pharmacy for a Dispensing Item.
- **BR-APT-034** — Medication Preparation and Compounding shall use an active Dispensing as their authority.
- **BR-APT-035** — Prepared Medication shall complete Final Dispense Review before Medication Handover.
- **BR-APT-036** — A Medication Dispense shall not exceed the unresolved quantity of its Dispensing Item.
- **BR-APT-037** — Medication Handover shall record its effective business time. Authorized Recipient verification is an operational responsibility of the dispensing Pharmacist and shall not be system-enforced. The system may optionally record recipient phone number and relationship to the Patient for reference only.
- **BR-APT-038** — Ward Delivery shall not be treated as Medication Administration.
- **BR-APT-039** — Medication Administration shall not be inferred from Invoice, Remove Stock, Medication Dispense, or Ward Delivery.
- **BR-APT-096** — Each Final Dispense Review attempt shall append an immutable Final Dispense Review Record to its Dispensing. A failed review shall record its reason, responsible Pharmacist, effective business time, and affected quantity, shall return the Dispensing from `Prepared` to `Preparing`, and shall prohibit Medication Handover. After correction, the Dispensing shall return to `Prepared` and undergo a new Final Dispense Review; only the latest review record with a passed outcome may transition it to `Reviewed` and authorize Medication Handover.

### 7.5 Dispense Authorization and cross-aggregate coordination

- **BR-APT-040** — Medication Preparation and Dispensing shall proceed only when Pharmacy policy evaluates the applicable financial and coverage evidence as Dispense Authorized.
- **BR-APT-041** — Payment Clearance shall come from the responsible payment authority and shall not be inferred solely from Invoice existence.
- **BR-APT-042** — Coverage Clearance shall identify the applicable Payer and covered fulfillment authority.
- **BR-APT-043** — Dispense Authorized shall be a policy evaluation result only. It shall not be persisted as an aggregate, entity, source of truth, or transaction boundary.
- **BR-APT-044** — One Invoice may support Dispense Authorized evaluation for multiple Dispensings, and one Dispensing may rely on multiple Invoice Items or Invoices when policy requires.
- **BR-APT-045** — A paid or financially cleared Invoice shall not guarantee successful fulfillment when shortage, discrepancy, expiry, or another valid exception occurs.
- **BR-APT-046** — Non-fulfillment after Payment Clearance or Coverage Clearance sufficient for Dispense Authorized shall produce an accountable Unfulfilled Medication Outcome. Commercial consequences of an existing Invoice shall be corrected under `BR-APT-027`. It shall not substitute a Sales Order Item after Sales Order establishment. Outpatient Pharmacy shall not resolve that condition through Backorder or an alternate stock source.

### 7.6 Partial fulfillment, UDD, and exceptions

- **BR-APT-047** — Partial Fulfillment at Sales Order execution level shall be represented by one Sales Order fulfilled through multiple Dispensings, or by fulfilled and unresolved quantities on Sales Order Items. This is fulfillment execution and is not Partial Prescription Fulfillment policy. Dispensing shall not own Partial Prescription Fulfillment policy semantics.
- **BR-APT-048** — Unit Dose Dispensing may divide one Sales Order Item into multiple Dispense Cycles and Dispensings.
- **BR-APT-049** — A Dose Window shall guide fulfillment planning and shall not assert Medication Administration.
- **BR-APT-050** — For an accepted substitute, the Sales Order Item shall record the substitute, responsible Pharmacist, reason, and affected quantity while retaining its reference to the original Baris Resep. Medication identity on an established Sales Order Item shall not be changed; a later replacement is handled by cancelling the affected line or order, reviewing the same original Resep again, and establishing a new Sales Order Item without requiring a corrected or replacement Resep.
- **BR-APT-051** — A Medication Shortage or Stock Discrepancy shall not alter the original Resep or erase an existing Invoice. Invoice identity remains. Revising Invoice content under `BR-APT-027` is not erasure.
- **BR-APT-052** — A Medication Return shall identify its source Dispensing, quantity, reason, and final Inventory disposition.
- **BR-APT-053** — Return to Stock shall occur only when Inventory accepts the returned medication under its own policy.
- **BR-APT-054** — A Copy Resep shall identify prescribed medication or quantity that remained unfulfilled or was excluded from the Sales Order, including items eligible for external fulfillment.
- **BR-APT-055** — A No-Show shall be a Pharmacy-owned outpatient policy outcome, shall not be stored as an Inventory status, and shall not be imposed on inpatient Ward Delivery.
- **BR-APT-108** — Partial Prescription Fulfillment is permitted only for Patient Request, Stock Shortage, and Fornas Not Covered items. No other reason is recognized by the system.
- **BR-APT-109** — For Patient Request, Pharmacy Staff may establish a Sales Order containing only selected prescription items. Excluded prescription items remain unfulfilled on the originating Prescription. The system shall support Copy Resep for unfulfilled items.
- **BR-APT-110** — For Stock Shortage before Sales Order establishment, Pharmacy Staff may establish a Sales Order containing only fulfillable prescription items. Fulfillable quantity is determined from Available Stock, not from Current Stock. Unavailable prescription items remain unfulfilled on the originating Prescription. The system shall support Copy Resep for unfulfilled items. No outstanding fulfillment obligation, waiting demand, or backorder record shall be created.
- **BR-APT-111** — The Pharmacist remains responsible for approving the resulting fulfillment decision when professional review is required. The system shall not automatically determine alternative substitutions or external fulfillment actions.
- **BR-APT-112** — Partial Prescription Fulfillment partiality exists only between Prescription and Sales Order. Partiality does not exist between Sales Order and Dispensing.
- **BR-APT-113** — A Sales Order fulfilled through one or more Dispensings is fulfillment execution and is not Partial Prescription Fulfillment policy.
- **BR-APT-114** — Outpatient Pharmacy shall not support Backorder. A stock shortage shall not create an outstanding fulfillment obligation, waiting demand, or backorder record.
- **BR-APT-115** — Outpatient stock shortage shall be resolved immediately through a Partial Sales Order of fulfillable items and Copy Resep for unfulfilled prescription items. Copy Resep may be used by the Patient to obtain medication from another pharmacy.
- **BR-APT-116** — When outpatient inventory is insufficient, only fulfillable prescription items may be included in the Sales Order. Unfulfillable items remain outside the Sales Order on the originating Prescription.
- **BR-APT-117** — Outpatient Pharmacy shall not implement alternate stock source selection, fulfillment routing, inter-pharmacy sourcing, or backorder management. Quantity that may still be promised to a new Sales Order shall be evaluated as Available Stock. Available Stock SHALL NOT be treated as equivalent to Current Stock.
- **BR-APT-118** — When an outpatient shortage is identified after Sales Order establishment or after Payment Clearance or Coverage Clearance sufficient for Dispense Authorized, the unfulfillable quantity shall receive an accountable Unfulfilled Medication Outcome and Copy Resep when applicable. When commercial consequences exist, they shall be corrected under `BR-APT-027`. It shall not be backordered or routed to an alternate stock source.
- **BR-APT-119** — Fornas validation shall classify prescription items as Covered or Not Covered.
- **BR-APT-120** — Covered items shall follow the normal BPJS fulfillment workflow. Coverage evidence shall be sufficient for Dispense Authorized on those items.
- **BR-APT-121** — Not Covered items shall not be automatically cancelled. Pharmacy may establish a separate Patient-Pay Sales Order for uncovered prescription items. That Patient-Pay Sales Order shall be independent of the BPJS-covered Sales Order. Uncovered items shall not remain in the BPJS fulfillment path.
- **BR-APT-122** — A Patient-Pay Sales Order shall require Payment Clearance under the normal self-pay workflow before Dispense Authorized is granted. This is financial evidence evaluation, not a Financial Clearance aggregate.
- **BR-APT-123** — Each prescription item shall follow its own authorization path: a BPJS Covered item uses Coverage Evidence to become Dispense Authorized; a Patient-Pay item uses Payment Clearance to become Dispense Authorized.
- **BR-APT-124** — Establishing a separate Patient-Pay Sales Order for Fornas Not Covered items is Partial Prescription Fulfillment. One originating Prescription may result in a BPJS-covered Sales Order and a Patient-Pay Sales Order for different prescription items.
- **BR-APT-125** — Outpatient Pharmacy shall adopt the existing legacy sales model for non-medication commercial facts. No additional invoice component model shall be introduced.
- **BR-APT-126** — BHP shall be treated as a standard catalog item and may appear as a sales item. BHP shall not be represented as a free-form invoice item.
- **BR-APT-127** — Item-specific charges, including packaging and compounding fees, shall be recorded as item-level charges on the applicable sales item.
- **BR-APT-128** — Transaction-wide adjustments, including rounding, shall be recorded as Invoice header commercial totals (for example `Pembulatan`, `BiayaLain`, `DiskonLain`). They shall not be persisted as child charge records.
- **BR-APT-129** — Authorized Recipient verification shall remain an operational responsibility of the dispensing Pharmacist and shall not be system-enforced.
- **BR-APT-130** — During Medication Handover, the system may optionally record recipient phone number and relationship to the Patient for reference only. Recorded recipient information shall not constitute identity proof, legal authorization, or a workflow gate.
- **BR-APT-131** — The system shall not require identity validation, legal relationship verification, document capture, or an authorization workflow as Medication Handover recipient evidence.
- **BR-APT-132** — Patient Education shall be recorded as a lightweight Patient Education Acknowledgement before Medication Handover. The Pharmacist shall confirm that medication counseling has been provided. The acknowledgement is a handover gate.
- **BR-APT-133** — Patient Education Acknowledgement shall record education timestamp and responsible Pharmacist. It shall not require structured counseling content, medication-specific templates, patient signature, or identity of the person educated.
- **BR-APT-134** — Detailed counseling notes are optional and shall be recorded only when the Pharmacist considers additional documentation necessary. Absence of notes shall not block Medication Handover after acknowledgement is recorded.
- **BR-APT-135** — Exception handling shall be authority-based. Returns, corrections, expired collection overrides, and other dispensing exceptions shall require authorization by an authorized pharmacist according to operational policy.
- **BR-APT-136** — The system shall not introduce a monetary approval threshold model. Exception authorization shall not be determined by amount, quantity value, or similar numeric approval bands.
- **BR-APT-137** — Exception authorization shall record the authorizing pharmacist, effective business time, and reason. Operational policy determines which pharmacist is authorized. Pharmacy Supervisor is an operational designation of that authority where a named authorizing pharmacist is required.
- **BR-APT-138** — Outpatient medication awaiting pickup may remain Ready for Pickup for a configurable Collection Window. The default Collection Window is 7 days. The window starts when the Dispensing first becomes Ready for Pickup: `Prepared`, intended for outpatient pickup, and Medication Handover not completed.
- **BR-APT-139** — When the Collection Window elapses without Medication Handover, the Serah Obat worklist category shall become Pickup Expired. Pickup Expired is a projection category and shall not be a Dispensing state.
- **BR-APT-140** — Ordinary Medication Handover shall not proceed while Pickup Expired. Only an authorized pharmacist may record a Collection Window Override and then proceed with handover.
- **BR-APT-141** — Collection Window Override shall record the authorizing pharmacist, effective business time, and override reason. Medication Handover after Pickup Expired is prohibited without that override.
- **BR-APT-142** — Pickup Expired shall not by itself expire the Dispensing, reverse queue `DoneAt`, establish No-Show, or return stock. Terminal uncollected resolution remains the authorized manual activity under `BR-APT-079`, `BR-APT-080`, and `WF-APT-RJ-007`.
- **BR-APT-143** — A Pharmacy Queue Entry that has not progressed into the pharmacy workflow may be closed from the pre-service queue status. That status is Patient Tracker `Waiting`. TAKEN in this decision names that same pre-service participation and shall not be added as a Patient Tracker state.
- **BR-APT-144** — Pharmacy Queue Close shall record a mandatory close reason, responsible Pharmacy Staff, and effective business time. Patient Tracker shall set the Queue Entry `Withdrawn`. No additional queue state shall be introduced.
- **BR-APT-145** — Pharmacy Queue Close shall not record `ServedAt` or `DoneAt`, shall not assert `In Service` or `Done`, and shall not establish a Sales Order, Dispensing, or Medication Handover. After `Medication Preparation Started`, this close path shall not apply.
- **BR-APT-146** — Available Stock represents the quantity that can still be committed to a new Sales Order. It is a fulfillment-planning concept used during Sales Order establishment and shortage evaluation. Current Stock represents the current physical inventory recorded by the inventory subsystem and reflects physical inventory state and inventory movements. Available Stock ≠ Current Stock. The Available Stock calculation formula is intentionally undefined in this domain and is reserved for a future inventory-planning design activity.

### 7.7 Completion and history

- **BR-APT-056** — Sales Order commercial progress and fulfillment progress shall be tracked independently.
- **BR-APT-057** — Commercial resolution shall not by itself complete physical fulfillment, and physical fulfillment shall not by itself prove financial resolution.
- **BR-APT-058** — Material review, Invoice formation, Dispensing formation, clearance, dispensing, handover, exception, and correction decisions shall retain responsible party and effective business time.
- **BR-APT-059** — Source Traceability shall be preserved from Resep or Jual Bebas through Sales Order, Invoice, Dispensing, and final outcomes.
- **BR-APT-060** — A completed or cancelled business outcome shall not be erased; a later correction shall add an accountable correcting fact. Invoice revision of the same Invoice under `BR-APT-027` is an accountable correction and is not erasure of Invoice identity. When Tata Rekening no longer permits Invoice revision, the correcting fact is a Tata Rekening-owned Credit Note, Refund, or Financial Adjustment. Apotek may correlate to that fact and shall not persist it as an Apotek document. This rule does not apply to correcting an active Outpatient Queue Mapping, which is updated in place under `BR-APT-062`.

### 7.8 Outpatient workflow policy

- **BR-APT-061** — A Pharmacist may perform Telaah Resep as soon as a Resep is available; Patient arrival and Outpatient Queue Mapping shall not be prerequisites.
- **BR-APT-062** — Outpatient Queue Mapping shall associate a Pharmacy Queue Entry with an existing Resep Kerja or Jual Bebas only. It shall not target a Sales Order, Invoice, or Dispensing. Mapping shall not create or modify a Resep, Hasil Telaah Resep, or Sales Order. An incorrect mapping shall be updated in place to the correct Resep Kerja or Jual Bebas without requiring mapping-change history.
- **BR-APT-063** — Tracker Mapping shall be used when valid tracker or registration evidence resolves one or more applicable existing Resep. The mapping associates the Queue Entry with the corresponding Resep Kerja. Tracker Mapping shall not create a Resep, shall not resolve a Jual Bebas, and shall not map to a Sales Order, Invoice, or Dispensing; a failed Tracker Mapping shall fall back to Manual Mapping.
- **BR-APT-064** — A directly issued or otherwise unresolved Pharmacy Queue Entry shall remain unmapped until Pharmacy Staff identifies and associates its applicable Resep Kerja or Jual Bebas, or until Pharmacy Staff records a Pharmacy Queue Close under `BR-APT-143`–`BR-APT-145`.
- **BR-APT-065** — Pharmacy Staff shall own administrative queue and pickup calling; those responsibilities shall not be transferred to the Pharmacist.
- **BR-APT-066** — In the normal BPJS Resep Elektronik flow with successful Tracker Mapping, the Patient shall require one outpatient pharmacy call: the pickup call after every applicable Dispensing reaches `Prepared`.
- **BR-APT-067** — Outpatient Queue Mapping and General Patient Purchase Confirmation may be completed in one counter interaction when the applicable Sales Order Items and calculated amount are available. When Tracker Mapping completes without the Patient at the counter, Pharmacy Staff shall call the Queue Number for the Purchase Confirmation interaction before Invoice establishment; this administrative call shall not establish `ServedAt` or `DoneAt`.
- **BR-APT-068** — An outpatient Dispensing and its Pharmacy Reserve through Stock Mutasi may occur before Patient arrival or Outpatient Queue Mapping, but Medication Preparation shall still require Dispense Authorized.
- **BR-APT-069** — Medication prepared for outpatient pickup shall remain in Dispensing Temporary Custody until accountable Medication Handover or No Show return movement.
- **BR-APT-070** — Before a General Patient Invoice exists, Pharmacy Staff shall communicate the amount calculated from the applicable Sales Order Items and Pricing Snapshot and obtain verbal Purchase Confirmation. Saving the confirmed transaction shall establish the Invoice and its Invoice Items from those items; no separate Purchase Confirmation object or transaction shall be retained.
- **BR-APT-071** — When a General Patient declines Purchase Confirmation before the transaction is saved, no Invoice shall be established and unused Pharmacy Reserve quantity shall return to Pharmacy Unit through Stock Mutasi. An Invoice established after confirmation may be cancelled only while its lifecycle and Tata Rekening permission both permit; otherwise the commercial consequence shall follow `BR-APT-027`.
- **BR-APT-072** — General Patient Medication Preparation shall not begin before Dispense Authorized is satisfied from Payment Clearance and applicable Invoice evidence.
- **BR-APT-073** — A BPJS Patient shall not be asked for Purchase Confirmation or Patient payment; the Patient-payable amount shall be zero and payment disposition shall be `Not Required`, while gross or covered value may remain non-zero.
- **BR-APT-074** — BPJS Medication Preparation may begin when Outpatient Queue Mapping, an applicable Dispensing, Coverage Clearance, and Dispense Authorized are satisfied; an existing Invoice shall not be a prerequisite.
- **BR-APT-075** — For the current outpatient BPJS policy, the Invoice shall be established only as part of successfully confirmed Medication Handover; Invoice establishment and handover completion shall form one accountable business outcome.
- **BR-APT-076** — Pharmacy Staff shall call the Patient for outpatient pickup after every applicable Dispensing in the coordinated pickup reaches `Prepared`. The Pharmacist shall then perform Final Dispense Review with the Patient or caregiver present before Medication Handover.
- **BR-APT-077** — During the same counter interaction after the pickup call, the Pharmacist shall operationally verify the recipient, complete Final Dispense Review, record Patient Education Acknowledgement, and only then complete outpatient Medication Handover. Recipient verification shall not be a system-enforced gate.
- **BR-APT-078** — Successful outpatient Medication Handover shall complete the applicable Dispensing quantity and request Remove Stock from Dispensing Temporary Unit through Stock Ledger.
- **BR-APT-079** — A BPJS No-Show before Medication Handover shall not establish or cancel an Invoice. An authorized manual uncollected-medication resolution shall make the affected Dispensing `Expired`, request Stock Mutasi from Dispensing Temporary Unit back to Pharmacy Unit when applicable, and allow the Sales Order to become `Resolved` with reason `Collection Window Expired` only after every accepted quantity and commercial consequence has a final outcome.
- **BR-APT-080** — A General Patient No-Show after payment shall use the same authorized manual uncollected-medication resolution for the fulfillment consequence, but its Sales Order shall remain `Active` until the required commercial consequence is resolved under `BR-APT-027`.
- **BR-APT-081** — `Medication Preparation Started` shall be Pharmacy Service Start Evidence for every outpatient payer path and shall cause Patient Tracker to record `ServedAt`. Invoice formation and Purchase Confirmation shall not establish outpatient pharmacy `ServedAt`.
- **BR-APT-082** — Patient Tracker shall remain authoritative for Pharmacy Queue Entry identity, Queue Number, and queue lifecycle even when Apotek owns Outpatient Queue Mapping and call purpose.
- **BR-APT-083** — No user shall enter a Legacy DU or independent medication Invoice Items manually; a user action may trigger Invoice formation only from accountable Sales Order Items. Users shall not enter free-form non-medication invoice items.
- **BR-APT-084** — One Pharmacy Queue Entry may be mapped to one or more Resep Kerja or Jual Bebas. It shall not be mapped to a Sales Order, Invoice, or Dispensing. Each mapped demand shall retain its own Telaah Resep when applicable, Sales Order, Invoices, Dispensing, and accountable lifecycle.
- **BR-APT-085** — Mapping multiple medication demands to one Pharmacy Queue Entry shall coordinate one outpatient service and shall not merge their Sales Orders, Invoices, or Dispensings.
- **BR-APT-086** — Within one active Registration, one active Sales Order shall coordinate through one active Dispensing for the normal outpatient path. The common Pharmacy Queue Entry may coordinate multiple such Sales Order and Dispensing pairs.
- **BR-APT-087** — A queue-facing per-demand progress view shall be a projection of Apotek facts for each mapped demand; Patient Tracker shall not become authoritative for Telaah Resep, Invoice, or Dispensing state.
- **BR-APT-088** — One coordinated pickup call shall occur only after every Dispensing intended for that handover has reached `Prepared` or received an accountable exception outcome.
- **BR-APT-089** — Pharmacy Staff shall accept or decline a Jual Bebas. Acceptance establishes the Jual Bebas record; decline shall not establish a Jual Bebas record or Sales Order. No Pharmacist approval, referral, escalation, or approval threshold applies.
- **BR-APT-090** — Outpatient BPJS Coverage Clearance shall require both a valid SEP for the applicable encounter and authoritative item-level Fornas coverage for the quantity being cleared.
- **BR-APT-091** — Fornas Not Covered prescription items shall not remain on a BPJS-covered Sales Order. Covered items shall form a BPJS-covered Sales Order. Not Covered items may form a separate Patient-Pay Sales Order. Each Sales Order shall produce only the Invoice of its own payer path.
- **BR-APT-092** — For a Patient-Pay Sales Order, the General Patient Invoice shall be established only after verbal Purchase Confirmation and shall follow the self-pay workflow. The BPJS Invoice of the independent BPJS-covered Sales Order shall be established only with successful Medication Handover under `BR-APT-075`.
- **BR-APT-093** — A coordinated mixed-coverage pickup call shall wait until every Dispensing intended for the handover has Dispense Authorized from its own path and has reached `Prepared`.
- **BR-APT-094** — If the Patient declines the Patient-Pay Sales Order before its Invoice is established, that Patient-Pay Sales Order shall receive an accountable declined outcome, while the independent BPJS-covered Sales Order may continue.
- **BR-APT-095** — Patient Tracker shall record outpatient pharmacy `DoneAt` when queue completion occurs. Queue completion may be triggered by (1) Pharmacy Staff performing the coordinated pickup call, or (2) authorized No Show Resolution under `WF-APT-RJ-007` when the Queue Entry is still `In Service` because the pickup call has not occurred. If the Queue Entry is already `Done` when No Show Resolution is executed, `DoneAt` shall not be recorded again. `DoneAt` shall never be reversed. Queue `Done` shall not prove Final Dispense Review, Patient Education Acknowledgement, Medication Dispense, or Medication Handover; it only means the queue service lifecycle has been completed. No new queue status shall be introduced, and no pharmacy workflow state shall be added to the Queue Entry. This completion path is not Pharmacy Queue Close (`BR-APT-143`–`BR-APT-145`).
- **BR-APT-097** — Patient Tracker `QueueEntry` shall be the sole canonical outpatient-pharmacy queue identity. Legacy Farinv queue identity is deprecated and shall not create active queue records. Historical Farinv queue data is read-only. No dual-active queue model is permitted. Patient Tracker `Apotek-Start` and `Apotek-Done` evidence shall reference the canonical `QueueEntryId`.

### 7.9 Pharmacy and Stock Ledger boundary

- **BR-APT-098** — Pharmacy Reserve shall be implemented only as Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit. No separate `ReserveStock` contract shall exist.
- **BR-APT-099** — `Prepared` shall be a Dispensing state only. A Dispensing reaches `Prepared` when all required dispensing movements for that preparation have completed. `Prepared` shall not be an Inventory state.
- **BR-APT-100** — Dispensing Started shall cause Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit. Dispensing Completed or `Prepared` shall cause no Inventory action.
- **BR-APT-101** — Medication Handed Over shall cause Remove Stock from Dispensing Temporary Unit through Stock Ledger.
- **BR-APT-102** — No Show resolution shall be owned by Pharmacy. Inventory shall apply only the Stock Mutasi from Dispensing Temporary Unit back to Pharmacy Unit directed by Pharmacy and shall not store No Show status.
- **BR-APT-103** — Stock Ledger shall not own dispensing, `Prepared`, `Handed Over`, No Show, or fulfillment lifecycle states.
- **BR-APT-104** — Partial fulfillment semantics shall belong to Sales Order. A Sales Order may be fulfilled through multiple Dispensings.

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

### 8.3 Invoice lifecycle

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

Invoice lifecycle states record commercial and payment/coverage facts. They do not encode mutability. `Issued` means the Invoice is the posted commercial/charge document for collection and Financial Charge; it does not freeze Invoice content. `Financially Cleared` is not Tata Rekening Close, Finalize, or Lunas, and is not a mutation lock. While Tata Rekening still permits modification, Invoice revision remains on the same Invoice and does not require a transition to `Adjusted or Credited`. `Adjusted or Credited` is an Invoice disposition observed after Tata Rekening applies an exception financial correction when direct Invoice revision is no longer permitted (`BR-APT-027`). It is not an Apotek-owned Credit Note lifecycle.

### 8.4 Dispensing lifecycle

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

`Completed` requires accountable Medication Dispense and the applicable Medication Handover. `Unfulfilled` requires a reason and resolution of allocated stock and financial consequences. Every Final Dispense Review attempt is appended as an immutable detail of the Dispensing. A failed attempt returns `Prepared` to `Preparing`; it does not erase an earlier record or authorize handover. A later passed attempt is required before `Reviewed`.

### 8.5 Apotek quantity lifecycle

```text
Accepted Quantity
  -> Invoiced through Invoice Item or Commercially Unallocated
  -> Referenced by Dispensing Item or Not Yet Planned for Fulfillment
  -> Fulfilled | Cancelled | Expired | Other Unfulfilled Outcome
```

Billing and fulfillment branches progress independently. Final Sales Order resolution requires reconciliation of both branches, not identical document counts. Outpatient Pharmacy does not use `Backordered`.

### 8.6 Outpatient Queue Mapping relationship

```text
Unmapped
  -> Mapped
       method: Tracker Mapping | Manual Mapping
  -> Pharmacy Queue Close
       -> Patient Tracker Withdrawn
```

This relationship associates an externally owned Pharmacy Queue Entry with a Resep Kerja or a Jual Bebas only. It does not map to a Sales Order, Invoice, or Dispensing. It does not replace the Patient Tracker queue lifecycle and does not transition Telaah Resep. Pharmacy Queue Close ends unmapped or declined participation from `Waiting` without adding a queue state.

### 8.7 Outpatient pickup and handover lifecycle

```text
Prepared
  -> Ready for Pickup
       -> Patient Called
            -> Final Review Completed
                 -> Education Provided
                      -> Handed Over

Ready for Pickup or Patient Called
  -> Pickup Expired
       -> Collection Window Override
            -> Patient Called / Final Review / Education / Handed Over
       -> No-Show
            -> manual uncollected-medication resolution
                 -> Dispensing Expired
                 -> Collection Window Expired resolution reason

Prepared, Ready for Pickup, or Patient Called
  -> No-Show
       -> manual uncollected-medication resolution
            -> Dispensing Expired
            -> Collection Window Expired resolution reason
```

Ready for Pickup and Pickup Expired are projection categories. The Collection Window (default 7 days) starts when the Dispensing first becomes Ready for Pickup. Pickup Expired does not change Dispensing state. Ordinary handover is blocked until a Collection Window Override is recorded. Terminal uncollected close remains a separate authorized act.

The pickup call is one trigger that completes the Patient Tracker queue (`In Service` → `Done`) and records `DoneAt`. When No Show Resolution runs before the pickup call, Apotek may complete the associated Queue Entry on the same lifecycle; Patient Tracker records `DoneAt` at that completion. When No Show Resolution runs after the pickup call, the Queue Entry may already be `Done`; `DoneAt` is retained and never reversed. Queue `Done` does not complete Medication Handover and does not add a pharmacy workflow state to the Queue Entry. Final Dispense Review and Patient Education Acknowledgement occur with the Patient or caregiver present after a pickup call. The Pharmacist operationally verifies the recipient during that counter interaction; verification is not a system-enforced lifecycle step. The system may optionally record recipient phone number and relationship for reference. Patient Education Acknowledgement records timestamp and responsible Pharmacist; detailed counseling notes are optional. The applicable commercial consequence is payer-specific. A General Patient may already have a financially cleared Invoice, while the current BPJS policy establishes its Invoice only with successful Medication Handover.

## 9. Domain Events

| Domain Event | Business meaning |
|---|---|
| Telaah Resep Started | A Pharmacist began professional assessment of a Resep Kerja. |
| Telaah Resep Completed | Every reviewed line received a final professional disposition. |
| Outpatient Queue Mapped | A Pharmacy Queue Entry was accountably associated with a Resep Kerja or a Jual Bebas. |
| Pharmacy Queue Close Recorded | Pharmacy Staff closed a Queue Entry that had not progressed into the pharmacy workflow, with a mandatory reason. |
| Jual Bebas Accepted | A permitted demand without a Resep was accepted by Pharmacy. |
| Sales Order Established | Accepted medication demand became available for commercial invoicing and fulfillment planning. |
| Invoice Established | A Medication Sale and its Invoice Items were formed from Sales Order Items. |
| Invoice Issued | The Invoice became the posted commercial/charge document for collection and Financial Charge. Issue does not make Invoice content immutable. |
| Invoice Revised | Invoice commercial content was revised under Tata Rekening permission. The Invoice identity remains. |
| Payment Clearance Established | The responsible payment authority confirmed the applicable payment condition. |
| Coverage Clearance Established | The applicable Payer authorized covered fulfillment. |
| Dispense Authorized Evaluated | Pharmacy policy determined that preparation and dispensing may proceed for the applicable quantity. |
| Dispensing Established | A physical fulfillment instruction and its Dispensing Items were formed directly from Sales Order Items. |
| Stock Transferred to Dispensing Temporary Unit | Stock Ledger recorded Pharmacy Reserve as Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit. |
| Stock Removed from Dispensing Temporary Unit | Stock Ledger recorded Remove Stock from Dispensing Temporary Unit after Medication Handover. |
| Stock Returned to Pharmacy Unit | Stock Ledger recorded Stock Mutasi from Dispensing Temporary Unit back to Pharmacy Unit after No Show resolution or unused Pharmacy Reserve return. |
| Medication Preparation Started | Physical preparation began under a released Dispensing. |
| Medication Prepared | The medication quantity on a Dispensing Item completed physical preparation. |
| Final Dispense Review Completed | With the Patient or caregiver present after the pickup call, Prepared Medication passed the required final professional check. |
| Final Dispense Review Failed | Prepared Medication failed its final professional review; an immutable review record was appended and the Dispensing returned from `Prepared` to `Preparing` for correction. |
| Patient Education Acknowledged | The Pharmacist confirmed that medication counseling was provided; education timestamp and responsible Pharmacist were recorded. |
| Pickup Expired Classified | The Collection Window elapsed without Medication Handover; the worklist category became Pickup Expired. |
| Collection Window Override Recorded | An authorized pharmacist recorded a reason permitting handover after Pickup Expired. |
| Patient Called for Pickup | Pharmacy Staff called the Patient for outpatient Medication Handover. |
| Medication Dispensed | An accountable medication quantity was supplied for the Patient. |
| Medication Handed Over | Medication was transferred to the Patient or other recipient as determined operationally by the Pharmacist. |
| Outpatient No-Show Recorded | A Patient did not collect medication within the applicable outpatient service limit. When the associated Queue Entry is still `In Service`, this resolution may complete the queue to `Done` and record `DoneAt`; when the Queue Entry is already `Done`, `DoneAt` is not reversed. |
| Medication Shortage Identified | Available Stock could not support committing the intended quantity to a new Sales Order, or physical inventory could not support an already accepted fulfillment quantity. This event does not assert that Available Stock equals Current Stock. |
| Medication Substitution Authorized | An accountable authority approved replacement of the requested medication. |
| Dispensing Backordered | An unresolved quantity was retained for later fulfillment. Not used in Outpatient Pharmacy. |
| Dispensing Cancelled | An authorized decision ended the Dispensing before completion. |
| Dispensing Expired | The permitted fulfillment period ended without completion. |
| Unfulfilled Medication Recorded | An accepted quantity received a final non-fulfillment outcome. |
| Medication Returned | Medication previously prepared or supplied was returned. |
| Invoice Credited | Tata Rekening applied a Credit Note that reduced or reversed an Invoice consequence because direct Invoice revision was no longer permitted. Observed by Apotek; not recorded as an Apotek Credit Note document. |
| Refund Required | A financial resolution requires return of settled funds. |
| Pharmacy Service Started | `Medication Preparation Started` established outpatient pharmacy `ServedAt` evidence. |
| Sales Order Resolved | Every accepted quantity and required commercial consequence received an accountable final outcome. |

## 10. Business Workflows

This domain is applied through care-setting-specific workflow specifications. The domain rules, Aggregate boundaries, states, lifecycles, and Domain Events in this document remain authoritative.

| Workflow | General business outcome | Canonical workflow artifact |
|---|---|---|
| Outpatient Apotek | Coordinate outpatient queue intake, medication-demand acceptance, payer clearance, dispensing, pickup, handover, and accountable non-fulfillment resolution. | [English](./outpatient-apotek-workflow.md) · [Bahasa Indonesia](./outpatient-apotek-workflow-id.md) |

Detailed triggers, sequencing, decisions, alternatives, exceptions, compensations, handoffs, and postconditions for outpatient fulfillment are owned by the referenced workflow specification. Detailed workflows for other Care Settings require their own future workflow artifacts.
