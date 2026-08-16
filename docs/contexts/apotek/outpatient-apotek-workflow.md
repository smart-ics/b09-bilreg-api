# Outpatient Apotek Workflow

**Artifact status:** Canonical business workflow specification

**Bounded context:** Apotek (`Pelayanan Obat Pasien`)

**Care setting:** Outpatient Pharmacy

**Bahasa Indonesia companion:** [outpatient-apotek-workflow-id.md](./outpatient-apotek-workflow-id.md)

**Authoritative domain:** [Apotek Domain](./apotek-domain.md)

## 1. Workflow Overview

This workflow coordinates outpatient medication service from acquisition of a Pharmacy Queue Number through accountable Medication Handover or an accountable non-fulfillment outcome.

It covers Resep Elektronik, recorded Resep Fisik, and Direct Medication Requests; Tracker Mapping and Manual Mapping; General Patient, BPJS, and mixed-coverage commercial paths; one Pharmacy Queue Entry mapped to multiple medication demands; dispensing; pickup; Final Dispense Review; Patient Education; and No-Show resolution.

Telaah Resep may run before Patient arrival and independently of queue mapping. Commercial and physical fulfillment progress remain independent and are coordinated through the Sales Order.

```text
Pharmacy Queue Entry and medication demand
  -> Outpatient Queue Mapping
  -> Telaah Resep or direct-request acceptance
  -> Sales Order
       -> Sales Invoice according to payer timing
            -> Sales Invoice Items reference Sales Order Lines
       -> Dispense Order
            -> Dispense Order Lines reference Sales Order Lines
  -> required clearance
  -> Medication Preparation
  -> pickup call
  -> Pharmacist review, recipient verification, and education
  -> Medication Handover or accountable non-fulfillment resolution
```

## 2. Domain Authority and References

This workflow applies the referenced domain specifications. It does not redefine Ubiquitous Language, Aggregate boundaries, Business Rules, states, lifecycles, or Domain Events.

| Authority | Responsibility used by this workflow |
|---|---|
| [Apotek Domain](./apotek-domain.md) | Telaah Resep, Sales Order, Sales Invoice timing, Dispense Order, clearance, dispensing, handover, and non-fulfillment policy. |
| [Apotek Domain — Bahasa Indonesia](./apotek-domain-id.md) | Human-readable semantic companion to the canonical Apotek domain. |
| [Patient Tracker Domain](../../contexts/pasien-tracker/TRACKER-DOMAIN.md) | Pharmacy Queue Entry identity, Queue Number, Queue Session, `CreatedAt`, `ServedAt`, `DoneAt`, and queue lifecycle. |
| [CPOE Domain](../../contexts/cpoe/CPOE-DOMAIN.md) | Original Resep Elektronik and clinician intent; CPOE remains authoritative for its Clinical Order. |
| [Tata Rekening Domain](../../contexts/TataRekening/02-domain.md) | Financial Charge, Financial Responsibility, financial correction, and settlement responsibility. |
| Approved outpatient pharmacy business discussion | Queue acquisition and mapping, payer timing, professional responsibilities, consolidated pickup, mixed coverage, and manual uncollected-medication resolution. |

Inventory, Payment, SEP, Fornas, and Medication Catalog authorities remain external even when no dedicated domain artifact is registered.

## 3. Scope and Boundaries

### 3.1 Begins

The end-to-end outpatient coordination begins when a Patient obtains a Pharmacy Queue Number by either:

- directly requesting a Queue Number; or
- presenting valid tracker or registration evidence that causes a Queue Entry to be created.

Telaah Resep for an Resep Elektronik may have begun or completed before this end-to-end trigger.

### 3.2 Ends

The workflow ends when every medication demand mapped to the Pharmacy Queue Entry has reached one of these accountable outcomes:

- Medication Handover completed for the intended Dispense Order quantities;
- an accepted quantity received an accountable Unfulfilled Medication Outcome;
- an authorized manual uncollected-medication resolution established `Collection Window Expired` and all required stock and commercial consequences were resolved; or
- an explicitly identified external financial resolution remains outstanding and the Sales Order correctly remains `Active`.

### 3.3 Included

- Resep Elektronik availability before or after Queue Entry creation.
- Resep Fisik recording by Pharmacy Staff.
- Direct Medication Request acceptance or decline by Pharmacy Staff.
- Tracker Mapping and Manual Mapping.
- One Queue Entry mapped to one or more medication demands.
- Telaah Resep and pre-Sales-Order Medication Substitution.
- General Patient verbal Purchase Confirmation before Sales Invoice establishment.
- BPJS coverage from valid SEP and authoritative Fornas mapping.
- Mixed BPJS-covered and Patient-payable quantities, represented as a BPJS-covered Sales Order and an independent Patient-Pay Sales Order when Fornas classifies some lines as Not Covered.
- Payment Clearance, Coverage Clearance, and Dispense Authorized.
- Pharmacy Reserve (Stock Mutasi to Dispensing Temporary Unit), Medication Preparation, pickup calling, Final Dispense Review, Authorized Recipient verification, Patient Education, Medication Dispense, and Medication Handover.
- Stock Shortage Handling through a Partial Sales Order of fulfillable lines and Salinan Resep for unfulfilled prescription lines.
- No-Show and manual uncollected-medication resolution.

### 3.4 Excluded

- Application screens, fields, save actions, and operator navigation; those belong to SOP.
- Inventory balance and movement rules, including return eligibility.
- Payment receipt and settlement execution.
- SEP creation and BPJS eligibility administration.
- Fornas master-data governance.
- Clinical Order modification and CPOE lifecycle changes.
- Medication Administration.
- Numerical collection limits; the closing decision is manual until a separate policy supplies a value.
- Backorder, alternate stock source selection, fulfillment routing, and inter-pharmacy sourcing.

## 4. Participants and Responsibility Handoffs

| Participant | Responsibility in this workflow | Handoff condition |
|---|---|---|
| Patient or Caregiver | Obtains a Queue Number, supplies mapping evidence or a Resep Fisik, gives verbal confirmation when Patient-payable, pays when required, presents for pickup, receives education, and accepts medication when authorized. | Evidence is supplied, confirmation is given or declined, Payment Clearance is obtained, or Medication Handover completes. |
| Patient Tracker | Owns Pharmacy Queue Entry identity, Queue Number, and queue lifecycle. | `Queue Entry Created`, `Queue Service Started`, or `Queue Service Completed`. |
| Pharmacy Staff | Performs administrative queue calls, Manual Mapping, records Resep Fisik, accepts or declines Direct Medication Requests, coordinates Sales Order progression, communicates General Patient value, establishes a confirmed Sales Invoice, performs Medication Preparation and Compounding, applies Stock Shortage Handling through a Partial Sales Order and Salinan Resep, and performs the pickup call. | `Outpatient Queue Mapped`, `Sales Order Established`, `Sales Invoice Established`, `Medication Prepared`, or `Patient Called for Pickup`. |
| Pharmacist | Performs Telaah Resep, authorizes eligible Medication Substitution before Sales Order establishment, verifies the Authorized Recipient, performs Final Dispense Review, and provides Patient Education. | `Telaah Resep Completed`, `Final Dispense Review Completed`, or Medication Handover is authorized to complete. |
| Cashier or Payment Authority | Receives required Patient payment and supplies Payment Clearance. | `Payment Clearance Established`. |
| SEP and Fornas Authorities | Supply encounter-level SEP validity and item-level BPJS coverage. | `Coverage Clearance Established` for the covered quantity. |
| Stock Ledger | Owns Stock Availability, Stock Mutasi, Remove Stock, and movement history. | `Stock Transferred to Dispensing Temporary Unit`, `Stock Removed from Dispensing Temporary Unit`, or `Stock Returned to Pharmacy Unit`. |
| Tata Rekening | Owns Financial Responsibility and the required financial consequence when paid medication is not fulfilled or collected. | Credit Note, Refund, or another final commercial outcome is supplied. |
| CPOE | Owns the original Resep Elektronik, which Apotek does not modify. | The original Resep is available. |
| Pharmacy Supervisor | Authorizes exceptional expiry, manual uncollected-medication resolution, and decisions outside ordinary authority. | Accountable exception outcome is established. |

## 5. Entry Conditions and Triggers

### 5.1 End-to-end trigger

`Queue Entry Created` for the outpatient pharmacy Service Point starts queue coordination. Patient Tracker records `CreatedAt` at Queue Number issuance.

### 5.2 Independent medication-demand triggers

- A Resep Elektronik becomes available from CPOE or another clinical-order authority.
- Pharmacy Staff records a presented Resep Fisik.
- Pharmacy Staff accepts or declines a Direct Medication Request.

These triggers may occur before or after Outpatient Queue Mapping as permitted by `BR-APT-061` and `BR-APT-062`.

### 5.3 Preconditions

- Every Resep retains an authoritative source and Patient association.
- A Resep Fisik is recorded before Telaah Resep.
- A Direct Medication Request is accepted before it may establish a Sales Order.
- Tracker Mapping requires valid evidence that resolves one or more applicable existing Resep; it does not resolve a Direct Medication Request.
- Manual Mapping requires Pharmacy Staff to identify the Queue Number and applicable demand.
- Medication Preparation requires an active Dispense Order and payer-appropriate Dispense Authorized.
- Medication Handover requires a Prepared Medication, Authorized Recipient, successful Final Dispense Review, and applicable Patient Education.

### 5.4 Blocking conditions

- An unresolved direct Queue Number cannot proceed past mapping-dependent clearance.
- A Resep with an incomplete Telaah Resep cannot establish a Sales Order.
- A rejected Resep or declined Direct Medication Request cannot establish a Sales Order.
- A General Patient quantity cannot begin Medication Preparation without Payment Clearance.
- A BPJS-covered quantity cannot begin Medication Preparation without valid SEP, authoritative Fornas coverage, and Dispense Authorized.
- A medication identity cannot be substituted after Sales Order establishment.

## 6. Workflow Inventory

| ID | Workflow | Business outcome |
|---|---|---|
| `WF-APT-RJ-001` | Acquire and Map Outpatient Pharmacy Queue | A Pharmacy Queue Entry is accountably mapped to one or more medication demands, or its unresolved/declined outcome is handed back to the applicable queue policy. |
| `WF-APT-RJ-002` | Accept Outpatient Medication Demand | An accepted Resep establishes a traceable Sales Order and primary outpatient Dispense Order, or receives a rejection outcome. |
| `WF-APT-RJ-003` | Fulfill Medication for a General Patient | Verbally confirmed and paid medication is prepared and handed over, or receives an accountable alternative or exception outcome. |
| `WF-APT-RJ-004` | Fulfill Medication for a BPJS Patient | Covered medication is prepared without a prior Sales Invoice and the BPJS Sales Invoice is established only with successful Medication Handover. |
| `WF-APT-RJ-005` | Fulfill Mixed-Coverage Medication | Fornas Not Covered lines form an independent Patient-Pay Sales Order; Covered lines remain on the BPJS path; both may be coordinated for one pickup. |
| `WF-APT-RJ-006` | Coordinate Multiple Medication Demands in One Queue | Multiple independent demand, Sales Order, invoice, and Dispense Order lifecycles are coordinated into one queue service and pickup session without being merged. |
| `WF-APT-RJ-007` | Resolve Uncollected Outpatient Medication | Prepared but uncollected medication receives an authorized expiry, Inventory return disposition, and payer-specific commercial resolution. |

## 7. Workflow Specifications

### WF-APT-RJ-001 — Acquire and Map Outpatient Pharmacy Queue

**Indonesia:** Mengambil dan Memapping Antrean Apotek Rawat Jalan

#### Purpose

Associate one Pharmacy Queue Entry with every applicable outpatient medication demand without making queue arrival a prerequisite for Telaah Resep.

#### Trigger

`Queue Entry Created` for the outpatient pharmacy Service Point.

#### Preconditions

- A Queue Session exists for the outpatient pharmacy Service Point.
- The Patient directly requests a Queue Number or presents tracker or registration evidence.

#### Participants

Patient or Caregiver, Patient Tracker, Pharmacy Staff.

#### Input Business Facts

- Pharmacy Queue Entry, Queue Number, and `CreatedAt` from Patient Tracker.
- Tracker or registration evidence when presented.
- Existing Resep Elektronik or Sales Orders when already available.
- Resep Fisik or Direct Medication Request when presented at the counter.

#### Main Flow

1. Patient Tracker establishes the Pharmacy Queue Entry, assigns the Queue Number, and records `CreatedAt`.
2. When valid tracker or registration evidence resolves one or more applicable existing Resep, Apotek establishes Tracker Mapping.
3. Apotek establishes a separate Outpatient Queue Mapping between the Queue Entry and every resolved Resep.
4. For each mapped Resep, Apotek exposes its authoritative progress as a queue-facing projection without transferring state ownership to Patient Tracker.
5. Queue coordination waits for payer-appropriate fulfillment while Telaah Resep and Sales Order establishment may continue independently.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| Tracker or registration evidence resolves one or more existing Resep | Apotek | Use Tracker Mapping for each Resep; no initial administrative call is required. |
| Evidence fails to resolve a Resep | Pharmacy Staff | Fall back to Manual Mapping. |
| Queue Number was obtained directly | Pharmacy Staff | Call the Queue Number for administrative identification and Manual Mapping. |
| One Queue Entry has multiple applicable demands | Pharmacy Staff | Map every demand separately to the same Queue Entry under `BR-APT-084` and `BR-APT-085`. |

For Manual Mapping:

1. Pharmacy Staff calls the unresolved Queue Number without starting Patient Tracker service.
2. Pharmacy Staff identifies an existing Resep or Sales Order, records a presented Resep Fisik, or evaluates a Direct Medication Request.
3. Apotek establishes Manual Mapping for every identified applicable demand.

#### Exception and Compensation Flows

- If a Direct Medication Request is declined, no Direct Medication Request record or Sales Order is established. Final disposition of the still-Waiting Queue Entry follows Patient Tracker's applicable withdrawal policy and remains external to Apotek.
- If the Queue Number cannot be matched to an accountable Patient Journey or medication demand, the Queue Entry remains unmapped and cannot receive mapping-dependent Dispense Authorized.
- If a queue mapping is incorrect, Pharmacy Staff selects the correct Resep or medication-demand source and Apotek updates the active mapping. No mapping-change history is required. This update does not modify the Resep, Hasil Telaah Resep, or Sales Order.

#### Outcomes and Postconditions

- Success: `Outpatient Queue Mapped` exists for one or more demands.
- Accountable non-completion: the Queue Entry remains unmapped pending evidence, or a declined Direct Medication Request produces no medication demand.
- Mapping does not establish `ServedAt`, create an Resep Elektronik, or complete Telaah Resep.

#### Domain References

`BR-APT-061`–`BR-APT-065`, `BR-APT-082`, `BR-APT-084`–`BR-APT-087`; `BR-TRK-026`–`BR-TRK-035`.

#### Domain Events

- Consumed: `Queue Entry Created`.
- Produced or observed: `Outpatient Queue Mapped`, `Queue Entry Identified` when externally applicable.

### WF-APT-RJ-002 — Accept Outpatient Medication Demand

**Indonesia:** Menerima Permintaan Obat Rawat Jalan

#### Purpose

Turn a reviewed Resep or accepted Direct Medication Request into a traceable Sales Order and primary outpatient Dispense Order.

#### Trigger

A Resep becomes available, or a Direct Medication Request is presented for acceptance.

#### Preconditions

- A Resep is owned by CPOE or another accountable clinical-order authority, or Pharmacy Staff has authority to assess the Direct Medication Request.
- A Resep Fisik has been recorded before review.
- For a Resep, the originating Registration remains active.

#### Participants

Pharmacist, Pharmacy Staff, CPOE or Dokter Penulis Resep.

#### Input Business Facts

- Resep Elektronik or recorded Resep Fisik.
- Direct Medication Request details when applicable.
- Medication Catalog and professional acceptance policy.
- Stock Availability as an external fulfillment fact that does not determine clinical acceptance.

#### Main Flow

1. For a Resep, the Pharmacist starts Telaah Resep as soon as the Resep is available, without waiting for Patient arrival or Outpatient Queue Mapping.
2. The Pharmacist reviews every Baris Resep. When needed, the Pharmacist clarifies with the Dokter Penulis Resep outside the system; the Resep remains unchanged and the review remains `Under Review`.
3. The Pharmacist decides each line as accepted as prescribed, accepted with a substitute, or rejected. Accepted medication is recorded on a Sales Order Line; a substitute includes its reason, affected quantity, responsible Pharmacist, and reference to the original Baris Resep.
4. The Pharmacist completes Telaah Resep as `Approved`, `Partially Approved`, or `Rejected`.
5. For a Direct Medication Request, Pharmacy Staff accepts or declines it; no Resep is created. Optional Pharmacist consultation is operational SOP guidance only and is not modeled as approval.
6. Apotek establishes a Sales Order from exactly one completed accepted-demand source and preserves Source Traceability.
7. Apotek may form Sales Invoices with their Sales Invoice Items and Dispense Orders with their Dispense Order Lines independently and at different business times. Every medication Sales Invoice Item and every Dispense Order Line references exactly one applicable Sales Order Line.
8. For the normal outpatient path within the active Registration, Apotek establishes one active primary Dispense Order for the active Sales Order.
9. Inventory may record Pharmacy Reserve through Stock Mutasi before Patient arrival or queue mapping, while Medication Preparation waits for applicable Dispense Authorized.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| All Baris Resep accepted | Pharmacist | `Approved`; all accepted lines may establish the Sales Order. |
| Some lines accepted | Pharmacist | `Partially Approved`; only Accepted Medication Lines enter the Sales Order. |
| No line accepted | Pharmacist | `Rejected`; no Sales Order is established. |
| Direct request accepted | Pharmacy Staff | Accept and establish the Direct Medication Request source. |
| Direct request declined | Pharmacy Staff | Do not establish a request record or Sales Order. |
| Unused Iter remains but Pharmacist declines honor | Pharmacist | Decline fulfillment; record accountable outcome without consuming Iter. |
| Unused Iter honored at fulfillment | Pharmacist | Proceed with fulfillment; system records Iter consumption. |
| Patient Request partial prescription | Pharmacy Staff | Establish Sales Order with selected lines only; excluded lines remain on Prescription; issue Salinan Resep when required. |
| Stock Shortage partial prescription | Pharmacy Staff | Establish Sales Order with fulfillable lines only; unavailable lines remain on Prescription; issue Salinan Resep when required. |
| Professional review required for partial path | Pharmacist | Approve or reject the resulting fulfillment decision; system does not auto-substitute or route externally. |
| Fornas Not Covered lines | Pharmacy Staff | Establish an independent Patient-Pay Sales Order for uncovered lines; Covered lines form the BPJS-covered Sales Order. |

#### Exception and Compensation Flows

- Stock shortage after Sales Order establishment does not change Hasil Telaah Resep. Pharmacy Staff shall not create Backorder or select an alternate stock source. Unfulfillable quantity receives an Unfulfilled Medication Outcome and Salinan Resep when applicable.
- Partial Prescription Fulfillment before Sales Order establishment is permitted only for Patient Request, Stock Shortage, or Fornas Not Covered lines. No other partiality reason is recognized.
- Medication identity on an established Sales Order Line shall not be changed. If a later replacement is needed, cancel the affected line or order, review the same original Resep again, and establish a new Sales Order Line without requiring a corrected or replacement Resep.
- Any accepted quantity that cannot be fulfilled must retain an accountable `Cancelled`, `Expired`, or other Unfulfilled Medication Outcome. Outpatient Pharmacy shall not retain Backorder.

#### Outcomes and Postconditions

- Success: `Telaah Resep Completed`, `Sales Order Established`, and `Dispense Order Established` are observed as applicable.
- Partial success: only selected or fulfillable prescription lines enter the Sales Order under Patient Request or Stock Shortage; excluded lines remain on the originating Prescription and may receive Salinan Resep.
- Rejection: no Sales Order exists for the rejected source.
- The Sales Order is not a Sales Invoice, Dispense Order, reservation, or dispense evidence.

#### Domain References

`BR-APT-001`–`BR-APT-019`, `BR-APT-029`–`BR-APT-034`, `BR-APT-050`, `BR-APT-054`, `BR-APT-061`, `BR-APT-068`, `BR-APT-083`, `BR-APT-086`, `BR-APT-089`, `BR-APT-105`–`BR-APT-124`; Telaah Resep and Sales Order lifecycles.

#### Domain Events

- Consumed: `Clinical Order Created` or another authoritative Resep-availability fact.
- Produced: `Telaah Resep Started`, `Medication Substitution Authorized`, `Telaah Resep Completed`, `Direct Medication Request Accepted`, `Sales Order Established`, `Dispense Order Established`, `Stock Transferred to Dispensing Temporary Unit` when externally supplied.

### WF-APT-RJ-003 — Fulfill Medication for a General Patient

**Indonesia:** Memenuhi Obat untuk Pasien Umum

#### Purpose

Obtain verbal Purchase Confirmation before Sales Invoice establishment, obtain Patient payment, and complete accountable outpatient Medication Handover.

#### Trigger

Outpatient Queue Mapping, an active Sales Order, applicable Sales Order Lines, and a calculated Patient-payable amount are available.

#### Preconditions

- The General Patient amount is calculated from the applicable Sales Order Lines and Pricing Snapshot.
- No Sales Invoice has yet been established for the proposed Patient-payable sale.
- An applicable Dispense Order exists or can be established from Sales Order Lines through its Dispense Order Lines.

#### Participants

Patient or Caregiver, Pharmacy Staff, Cashier or Payment Authority, Pharmacy Staff, Pharmacist, Patient Tracker, Inventory, Tata Rekening.

#### Input Business Facts

- Outpatient Queue Mapping.
- Sales Order and applicable Patient-payable Sales Order Line quantities.
- Pricing Snapshot and calculated amount.
- Dispense Order and Pharmacy Reserve (Stock Mutasi to Dispensing Temporary Unit) when already available.

#### Main Flow

1. Pharmacy Staff receives the Patient in the Purchase Confirmation interaction—within the Manual Mapping counter interaction when possible, or through a separate administrative Queue Number call after Tracker Mapping—and communicates the calculated total verbally before a Sales Invoice exists. This interaction does not establish `ServedAt` or `DoneAt`.
2. The Patient gives verbal Purchase Confirmation.
3. Pharmacy Staff establishes the Sales Invoice and its Sales Invoice Items from the confirmed Sales Order Line quantities; Sales Invoice establishment is the accountable evidence that confirmation was obtained, and no separate confirmation object or transaction exists.
4. The Cashier receives payment and supplies Payment Clearance for the Sales Invoice.
5. Apotek evaluates financial and coverage evidence as Dispense Authorized for the applicable Dispense Order quantities.
6. Stock Ledger records Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit when Pharmacy Reserve is required and not already in Dispensing Temporary Unit.
7. The Pharmacy Staff begins Medication Preparation under the released Dispense Order.
8. Apotek observes `Medication Preparation Started`; Patient Tracker enters the Pharmacy Queue Entry into In Service and records `ServedAt`.
9. The Pharmacy Staff completes Medication Preparation; the Dispense Order reaches `Prepared` when required dispensing movements are complete. Stock Ledger takes no action at `Prepared`; medication remains in Dispensing Temporary Custody.
10. When every Dispense Order intended for the coordinated handover is `Prepared` or has an accountable exception outcome, Pharmacy Staff performs the pickup call.
11. Patient Tracker makes the Pharmacy Queue Entry `Done` and records `DoneAt` at the pickup-call time.
12. With the Patient or caregiver present, the Pharmacist verifies the Authorized Recipient, completes Final Dispense Review, and provides applicable Patient Education in the same counter interaction. A passed review appends its immutable review record and moves the Dispense Order to `Reviewed`.
13. Apotek records Medication Dispense and completes Medication Handover for each applicable Dispense Order quantity.
14. Medication Handover completes the Dispense Order quantity and requests Remove Stock from Dispensing Temporary Unit through Stock Ledger.
15. The Sales Order becomes `Resolved` only when all accepted quantities and commercial consequences have final accountable outcomes.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| Patient declines before Sales Invoice establishment | Patient | No Sales Invoice is established; unused quantity in Dispensing Temporary Unit returns to Pharmacy Unit through Stock Mutasi; the affected Patient-payable Sales Order Line quantity receives an accountable declined or commercially unallocated outcome. |
| Calculated amount changes before establishment | Pharmacy Staff | Communicate the revised amount and obtain verbal confirmation again before establishing the Sales Invoice. |
| Multiple demands share one Queue Entry | Pharmacy Staff | Apply `WF-APT-RJ-006`; retain separate Sales Orders, invoices, and Dispense Orders. |
| Patient does not collect after pickup call | Pharmacy Supervisor | Apply `WF-APT-RJ-007`. |

#### Exception and Compensation Flows

- If payment is not completed after Sales Invoice establishment, Medication Preparation remains blocked. The Sales Invoice may be `Cancelled` only while its lifecycle permits.
- If an issued or financially cleared Sales Invoice needs correction, use Financial Adjustment, Credit Note, or Refund under Tata Rekening authority; do not silently replace it.
- If shortage occurs after payment, Pharmacy Staff shall not create Backorder or select an alternate stock source. Unfulfillable quantity receives an Unfulfilled Medication Outcome and Salinan Resep when applicable, plus Credit Note or Refund under Tata Rekening authority. Substitution is prohibited because the Sales Order already exists.
- If fulfillment cannot complete, affected quantities receive an accountable Unfulfilled Medication Outcome and Tata Rekening receives the required financial consequence.
- A failed Final Dispense Review appends an immutable review record containing the reason, responsible Pharmacist, effective business time, and affected quantity; returns the affected Dispense Order from `Prepared` to `Preparing`; and prevents Medication Handover. After correction, the Dispense Order returns to `Prepared` and requires another Final Dispense Review.

#### Outcomes and Postconditions

- Successful completion: Sales Invoice is financially cleared, Dispense Order is `Completed`, Medication Handover identifies the Authorized Recipient, and all quantities remain traceable.
- Declined purchase: no Sales Invoice exists for the declined proposal.
- Paid non-fulfillment or No-Show: fulfillment and commercial consequences remain separately accountable; the Sales Order remains `Active` until both are final.
- Queue `Done` does not prove Medication Handover.

#### Domain References

`BR-APT-020`–`BR-APT-028`, `BR-APT-033`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-067`–`BR-APT-072`, `BR-APT-076`–`BR-APT-083`, `BR-APT-088`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`, `BR-APT-125`–`BR-APT-128`; Sales Invoice and Dispense Order lifecycles; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046`.

#### Domain Events

- Consumed: `Outpatient Queue Mapped`, `Payment Clearance Established`, `Stock Transferred to Dispensing Temporary Unit`.
- Produced or observed: `Sales Invoice Established`, `Sales Invoice Issued`, `Dispense Authorized Evaluated`, `Medication Preparation Started`, `Pharmacy Service Started`, `Medication Prepared`, `Patient Called for Pickup`, `Queue Service Started`, `Queue Service Completed`, `Final Dispense Review Completed`, `Final Dispense Review Failed`, `Medication Dispensed`, `Medication Handed Over`, `Sales Order Resolved`.

### WF-APT-RJ-004 — Fulfill Medication for a BPJS Patient

**Indonesia:** Memenuhi Obat untuk Pasien BPJS

#### Purpose

Prepare covered outpatient medication without prior Sales Invoice or Patient payment and establish the BPJS Sales Invoice only with successful Medication Handover.

#### Trigger

Outpatient Queue Mapping, an applicable active Sales Order, a Dispense Order, valid SEP, and authoritative item-level Fornas coverage are available.

#### Preconditions

- The Patient has a valid SEP for the applicable encounter.
- Each covered quantity is supported by authoritative Fornas mapping.
- No Patient Purchase Confirmation or Patient payment is required for the covered quantity.
- The BPJS Sales Invoice has not yet been established.

#### Participants

Patient or Caregiver, Pharmacy Staff, Pharmacist, Patient Tracker, SEP and Fornas Authorities, Inventory, Tata Rekening.

#### Input Business Facts

- Outpatient Queue Mapping.
- Sales Order, covered Sales Order Line quantities, and Dispense Order.
- Valid SEP and item-level Fornas coverage.
- Stock Availability and Pharmacy Reserve (Stock Mutasi to Dispensing Temporary Unit) outcomes.

#### Main Flow

1. The SEP and Fornas authorities establish Coverage Clearance for each covered quantity.
2. Apotek evaluates financial and coverage evidence as Dispense Authorized for the applicable Dispense Order quantities without requiring an existing Sales Invoice.
3. Stock Ledger records Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit when Pharmacy Reserve is required and not already in Dispensing Temporary Unit.
4. The Pharmacy Staff starts Medication Preparation.
5. `Medication Preparation Started` causes Patient Tracker to record `ServedAt` and move the Pharmacy Queue Entry to In Service.
6. The Pharmacy Staff completes Medication Preparation; the Dispense Order reaches `Prepared` when required dispensing movements are complete. Stock Ledger takes no action at `Prepared`; medication remains in Dispensing Temporary Custody.
7. When every Dispense Order intended for the coordinated handover is `Prepared` or has an accountable exception outcome, Pharmacy Staff performs the pickup call.
8. Patient Tracker records `DoneAt` and makes the Queue Entry `Done` at the pickup-call time.
9. With the Patient or caregiver present, the Pharmacist verifies the Authorized Recipient, completes Final Dispense Review, and provides applicable Patient Education in the same counter interaction. A passed review appends its immutable review record and moves the Dispense Order to `Reviewed`.
10. As one accountable business outcome, Apotek establishes the BPJS Sales Invoice and its Sales Invoice Items from the covered Sales Order Line quantities, records Medication Dispense, and completes Medication Handover.
11. Medication Handover completes each applicable Dispense Order quantity and requests Remove Stock from Dispensing Temporary Unit through Stock Ledger.
12. The Sales Order becomes `Resolved` only when every accepted quantity and required commercial consequence has a final outcome.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| Resep Elektronik and Tracker Mapping succeed in the normal path | Apotek | One outpatient call occurs: the pickup call after `Prepared`. |
| SEP invalid | SEP authority | Coverage Clearance is absent; Medication Preparation remains blocked. |
| Item not covered by Fornas | Pharmacy Staff | Route the non-covered quantity through `WF-APT-RJ-005`. |
| Multiple mapped demands | Pharmacy Staff | Apply `WF-APT-RJ-006`; maintain separate records and one coordinated pickup. |
| Patient does not collect | Pharmacy Supervisor | Apply `WF-APT-RJ-007`; no BPJS Sales Invoice is established. |

#### Exception and Compensation Flows

- A BPJS No-Show before Medication Handover establishes no Sales Invoice and requires no Sales Invoice cancellation.
- Shortage after Sales Order establishment does not permit Backorder, alternate stock source, or substitution. Unfulfillable quantity receives an Unfulfilled Medication Outcome and Salinan Resep when applicable.
- Failed Final Dispense Review appends its immutable review record, returns the Dispense Order from `Prepared` to `Preparing`, and prevents both BPJS Sales Invoice establishment and Medication Handover. Correction returns the order to `Prepared` and requires a new review.
- Inventory may reject a return Mutasi when eligible quantity is not available; Pharmacy still records the accountable No Show outcome and any required commercial consequence.

#### Outcomes and Postconditions

- Successful completion: BPJS Sales Invoice establishment and Medication Handover form one accountable outcome; Patient-payable amount is zero and payment disposition is `Not Required`.
- Coverage blocked: no preparation occurs for the uncleared quantity.
- No-Show: no BPJS Sales Invoice exists; Dispense Order and stock follow `WF-APT-RJ-007`.
- Queue `Done` remains independent of Medication Handover completion.

#### Domain References

`BR-APT-020`–`BR-APT-026`, `BR-APT-029`–`BR-APT-045`, `BR-APT-066`, `BR-APT-068`–`BR-APT-069`, `BR-APT-073`–`BR-APT-079`, `BR-APT-081`–`BR-APT-083`, `BR-APT-088`, `BR-APT-090`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046`.

#### Domain Events

- Consumed: `Outpatient Queue Mapped`, `Coverage Clearance Established`, `Stock Transferred to Dispensing Temporary Unit`.
- Produced or observed: `Dispense Authorized Evaluated`, `Medication Preparation Started`, `Pharmacy Service Started`, `Medication Prepared`, `Patient Called for Pickup`, `Queue Service Started`, `Queue Service Completed`, `Final Dispense Review Completed`, `Final Dispense Review Failed`, `Sales Invoice Established`, `Sales Invoice Issued`, `Medication Dispensed`, `Medication Handed Over`, `Sales Order Resolved`.

### WF-APT-RJ-005 — Fulfill Mixed-Coverage Medication

**Indonesia:** Memenuhi Obat dengan Coverage Campuran

#### Purpose

Separate BPJS-covered and Patient-Pay commercial responsibility into independent Sales Orders while coordinating all quantities intended for one outpatient pickup.

#### Trigger

Fornas validation classifies some prescription lines as Covered and some as Not Covered.

#### Preconditions

- A valid SEP exists for the encounter.
- Authoritative Fornas mapping classifies each prescription line as Covered or Not Covered.
- Not Covered lines have not been automatically cancelled.

#### Participants

Patient or Caregiver, Pharmacy Staff, Cashier or Payment Authority, Pharmacist, Patient Tracker, SEP and Fornas Authorities, Inventory, Tata Rekening.

#### Input Business Facts

- Originating Prescription and Fornas line classifications.
- BPJS-covered Sales Order for Covered lines, when established.
- Independent Patient-Pay Sales Order for Not Covered lines, when established.
- Valid SEP and authoritative item-level Fornas coverage.
- Applicable Dispense Orders for each Sales Order.

#### Main Flow

1. Fornas validation classifies each prescription line as Covered or Not Covered.
2. Covered lines enter the BPJS-covered Sales Order and follow `WF-APT-RJ-004`. Coverage evidence is sufficient for Dispense Authorized on those lines.
3. Not Covered lines do not remain on the BPJS path. Pharmacy Staff may establish a separate Patient-Pay Sales Order for those lines as Partial Prescription Fulfillment.
4. The Patient-Pay Sales Order follows `WF-APT-RJ-003`: verbal Purchase Confirmation, General Patient Sales Invoice, and Payment Clearance. Payment Clearance is required before Dispense Authorized on Patient-Pay lines.
5. Each line is authorized independently: Covered Line → Coverage Evidence → Dispense Authorized; Patient-Pay Line → Payment Clearance → Dispense Authorized.
6. After every quantity intended for the handover has Dispense Authorized from its own path, Pharmacy Staff begins and completes Medication Preparation on each applicable Dispense Order.
7. The first `Medication Preparation Started` records Patient Tracker `ServedAt`; every intended Dispense Order reaches `Prepared` before pickup.
8. Pharmacy Staff performs one coordinated pickup call; Patient Tracker records `DoneAt`.
9. With the Patient or caregiver present, the Pharmacist verifies the Authorized Recipient, completes Final Dispense Review for each Prepared Dispense Order, and provides Patient Education.
10. Apotek establishes the BPJS Sales Invoice only as Medication Handover of the BPJS-covered Sales Order succeeds. The Patient-Pay Sales Invoice already exists and is financially cleared.
11. Apotek records Medication Dispense and Medication Handover for all applicable quantities and requests Remove Stock from Dispensing Temporary Unit.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| Patient confirms the Patient-Pay Sales Order | Patient | Establish and collect the General Patient Sales Invoice; coordinate both Sales Orders for pickup. |
| Patient declines the Patient-Pay Sales Order before invoice establishment | Patient | Establish no General Patient Sales Invoice; give the Patient-Pay Sales Order an accountable declined outcome; continue the BPJS-covered Sales Order independently. |
| All lines are Covered | Apotek | Remain on `WF-APT-RJ-004`; do not create a Patient-Pay Sales Order. |
| Not all intended quantities have Dispense Authorized | Apotek | Do not start coordinated preparation for unauthorized quantities and do not perform the pickup call. |

#### Exception and Compensation Flows

- An established General Patient Sales Invoice follows General Patient cancellation and correction rules; the BPJS Sales Invoice remains absent until handover of the BPJS-covered Sales Order.
- No-Show after payment follows the paid General Patient commercial path while the absent BPJS Sales Invoice follows the uninvoiced BPJS path.
- Each Sales Order retains independent commercial and fulfillment consequences.
- Substitution is prohibited after Sales Order establishment.
- A failed Final Dispense Review appends its immutable review record, returns only the affected Dispense Order from `Prepared` to `Preparing`, and does not rewrite the other Sales Order.

#### Outcomes and Postconditions

- Success: a BPJS-covered Sales Order and a Patient-Pay Sales Order exist for different lines of the same Prescription; separate Sales Invoices represent each payer path; one coordinated Medication Handover may complete both.
- Patient declines Patient-Pay quantities: the BPJS-covered Sales Order may complete independently.
- Any unresolved commercial consequence keeps its own Sales Order `Active`.

#### Domain References

`BR-APT-011`, `BR-APT-015`, `BR-APT-020`–`BR-APT-028`, `BR-APT-040`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-070`–`BR-APT-078`, `BR-APT-090`–`BR-APT-096`, `BR-APT-108`, `BR-APT-119`–`BR-APT-124`.

#### Domain Events

- Consumed: `Coverage Clearance Established`, `Payment Clearance Established`, `Outpatient Queue Mapped`.
- Produced or observed: `Sales Order Established`, `Sales Invoice Established`, `Sales Invoice Issued`, `Dispense Authorized Evaluated`, `Medication Preparation Started`, `Medication Prepared`, `Patient Called for Pickup`, `Final Dispense Review Completed`, `Final Dispense Review Failed`, `Medication Dispensed`, `Medication Handed Over`, `Sales Order Resolved` when fully reconciled.

### WF-APT-RJ-006 — Coordinate Multiple Medication Demands in One Queue

**Indonesia:** Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrean

#### Purpose

Coordinate two or more independently accountable medication demands in one outpatient queue and pickup session without merging their business records.

#### Trigger

One Pharmacy Queue Entry is mapped to two or more Resep, Direct Medication Requests, or resulting Sales Orders.

#### Preconditions

- Every demand has a separate Outpatient Queue Mapping to the common Queue Entry.
- Every Resep follows its own Telaah Resep.
- Every accepted source establishes its own Sales Order and active primary outpatient Dispense Order.

#### Participants

Pharmacy Staff, Pharmacist, Patient or Caregiver, Patient Tracker, Cashier or Payment Authority, SEP and Fornas Authorities, Inventory.

#### Input Business Facts

- One Pharmacy Queue Entry.
- Two or more mapped medication demands.
- Per-demand Sales Order, Sales Invoice, clearance, and Dispense Order progress, including their line-level relationships.

#### Main Flow

1. Apotek retains a separate Outpatient Queue Mapping for every demand associated with the common Pharmacy Queue Entry.
2. Each demand progresses independently through Telaah Resep or direct acceptance, Sales Order establishment, commercial invoicing, fulfillment planning, and payer clearance.
3. The queue-facing view projects the authoritative progress of each mapped demand without owning those states.
4. The first applicable `Medication Preparation Started` causes Patient Tracker to record one `ServedAt` for the common Queue Entry.
5. Pharmacy Staff waits until every Dispense Order intended for the pickup is `Prepared` or has an accountable exception outcome.
6. Pharmacy Staff performs one coordinated pickup call; Patient Tracker records one `DoneAt` for the common Queue Entry.
7. With the Patient or caregiver present, the Pharmacist verifies the Authorized Recipient, completes Final Dispense Review for each Prepared Medication, and provides consolidated Patient Education while preserving medication-specific instructions. Each passed review appends its immutable review record and moves its Dispense Order to `Reviewed`.
8. Apotek records Medication Dispense and Medication Handover against every applicable Dispense Order and Sales Order separately.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| Every included demand is ready | Pharmacy Staff | Perform one coordinated pickup call and handover session. |
| One demand has an accountable exception outcome | Pharmacy Staff | Include the resolved exception in the coordinated communication and proceed with ready demands when their payer rules permit. |
| One demand remains unresolved | Pharmacy Staff | Do not declare that demand ready; defer the coordinated call unless the Patient accepts an accountable partial path permitted by its payer workflow. |
| Demands have different payer classifications | Pharmacy Staff | Apply the applicable General, BPJS, or mixed-coverage workflow to each demand before coordination. |

#### Exception and Compensation Flows

- Correcting one mapping shall not rewrite another demand's history.
- Cancellation, expiry, unfulfilled shortage, financial correction, and return remain attached to their originating Sales Order and Dispense Order.
- One successful Medication Handover shall not be inferred to fulfill another mapped demand without its own handover fact.
- A failed Final Dispense Review appends its immutable review record and returns only its originating Dispense Order from `Prepared` to `Preparing`. That demand is excluded from handover until correction returns it to `Prepared` and a new review passes; other demands remain independently accountable under their payer workflow.

#### Outcomes and Postconditions

- One Queue Entry has one `CreatedAt`, at most one `ServedAt`, and one `DoneAt`.
- Each Resep, Sales Order, Sales Invoice, and Dispense Order retains independent identity and lifecycle.
- One pickup call and counter interaction may coordinate multiple accountable Medication Handover facts.

#### Domain References

`BR-APT-011`, `BR-APT-015`, `BR-APT-022`, `BR-APT-030`, `BR-APT-056`–`BR-APT-060`, `BR-APT-084`–`BR-APT-088`, `BR-APT-095`–`BR-APT-096`; `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, `BR-TRK-045a`.

#### Domain Events

- Consumed: `Outpatient Queue Mapped`, `Medication Preparation Started`, `Medication Prepared`.
- Produced or observed: `Queue Service Started`, `Patient Called for Pickup`, `Queue Service Completed`, `Final Dispense Review Completed`, `Final Dispense Review Failed`, `Medication Dispensed`, `Medication Handed Over` for each applicable demand.

### WF-APT-RJ-007 — Resolve Uncollected Outpatient Medication

**Indonesia:** Menyelesaikan Obat Rawat Jalan yang Tidak Diambil

#### Purpose

Give Prepared Medication that is not collected an authorized expiry, stock disposition, and payer-specific commercial outcome without inventing an automatic time limit.

#### Trigger

The Pharmacy Supervisor or another authorized role manually determines that the permitted collection opportunity has ended for Prepared Medication that was not handed over.

#### Preconditions

- Medication remains `Prepared` in Dispensing Temporary Custody and Medication Handover has not completed.
- The Patient did not collect the medication.
- The accountable manual closing authority and effective business time are known.

#### Participants

Pharmacy Supervisor, Pharmacy Staff, Inventory, Tata Rekening, Patient Tracker.

#### Input Business Facts

- Pharmacy Queue Entry, which may already be `Done` after the pickup call.
- Sales Order, Dispense Order, and unresolved quantities.
- Sales Invoice presence and financial disposition by payer.
- `Prepared` Dispense Order state and quantity held in Dispensing Temporary Unit.

#### Main Flow

1. The authorized role performs the manual uncollected-medication resolution and records the Patient as No-Show for the affected fulfillment.
2. Apotek gives each affected Dispense Order the terminal state `Expired`.
3. The resolution retains reason `Collection Window Expired`, responsible party, effective business time, affected quantity, and Source Traceability.
4. Apotek records No Show resolution and requests Stock Mutasi from Dispensing Temporary Unit back to Pharmacy Unit for eligible quantity.
5. Apotek records the resulting Unfulfilled Medication Outcome for each affected quantity.
6. Apotek resolves the payer-specific commercial consequence.
7. The Sales Order becomes `Resolved` only after every accepted quantity and required commercial consequence has a final accountable outcome.

#### Decision and Alternative Flows

| Payer condition | Commercial outcome |
|---|---|
| BPJS Sales Invoice was not established because handover failed | No invoice is established or cancelled; resolve fulfillment and Inventory only, then resolve the Sales Order when all outcomes are final. |
| General Patient Sales Invoice is paid | Tata Rekening or the responsible financial authority supplies Credit Note, Refund, or another final outcome; the Sales Order remains `Active` until then. |
| General Patient proposal was declined before invoice establishment | No Sales Invoice exists; resolve any unused reservation and commercially unallocated quantity. |
| Mixed coverage | Resolve covered uninvoiced and paid Patient-payable consequences separately using their Sales Order Line and Sales Invoice Item relationships. |

#### Exception and Compensation Flows

- No numerical collection limit is invented. Until an authoritative policy supplies one, only the authorized manual activity establishes the end of the collection opportunity.
- Queue `DoneAt` is not reversed; No-Show resolution belongs to Apotek after queue completion.
- Inventory may reject Return to Stock under its own policy; the rejected return still requires an accountable final Inventory disposition.
- A paid commercial consequence shall not be silently erased or treated as the uninvoiced BPJS path.

#### Outcomes and Postconditions

- BPJS No-Show: Dispense Order is `Expired`, no BPJS Sales Invoice exists, and stock has an accountable disposition.
- Paid General Patient No-Show: Dispense Order is `Expired`; the Sales Order remains `Active` until the financial outcome is final.
- Final resolution: Sales Order is `Resolved` with reason `Collection Window Expired` after all fulfillment and commercial consequences are final.

#### Domain References

`BR-APT-018`–`BR-APT-019`, `BR-APT-027`, `BR-APT-045`–`BR-APT-047`, `BR-APT-052`–`BR-APT-060`, `BR-APT-069`, `BR-APT-078`–`BR-APT-080`, `BR-APT-095`; Dispense Order, quantity, pickup, and Sales Order lifecycles.

#### Domain Events

- Consumed: `Patient Called for Pickup`, `Medication Prepared`.
- Produced or observed: `Outpatient No-Show Recorded`, `Dispense Order Expired`, `Unfulfilled Medication Recorded`, `Medication Returned`, `Sales Invoice Credited`, `Refund Required`, `Sales Order Resolved` when fully reconciled.

## 8. Cross-Context Handoffs

| From | Authoritative business fact | To | Resulting responsibility |
|---|---|---|---|
| Patient Tracker | `Queue Entry Created`, Queue Number, `CreatedAt` | Apotek | Establish Tracker Mapping or Manual Mapping without taking ownership of queue identity. |
| Apotek | `Medication Preparation Started` | Patient Tracker | Move the Pharmacy Queue Entry to In Service and record `ServedAt`; Patient Tracker shall not infer Medication Handover. |
| Apotek | `Patient Called for Pickup` | Patient Tracker | Make the Pharmacy Queue Entry `Done` and record `DoneAt`; later professional review and handover remain Apotek facts. |
| CPOE or clinical-order authority | Resep availability and clinician intent | Apotek | Perform Telaah Resep without modifying the original Resep. |
| Apotek | Fulfillment projection by Resep and realized Medication Handover | EMR reporting | Display Resep-to-realization information without changing the CPOE Clinical Order in the initial scope. |
| SEP authority | Valid SEP | Apotek | Evaluate encounter-level BPJS coverage; SEP alone does not identify covered medication quantities. |
| Fornas authority | Item-level coverage mapping | Apotek | Establish Coverage Clearance only for applicable covered quantities together with valid SEP. |
| Cashier or Payment authority | `Payment Clearance Established` | Apotek | Evaluate Dispense Authorized from payment evidence; payment does not prove stock or handover. |
| Stock Ledger | Stock Availability and `Stock Transferred to Dispensing Temporary Unit` | Apotek | Record Mutasi and Remove Stock only from Pharmacy-authorized requests; stock facts do not rewrite Telaah Resep. |
| Apotek | Handover, expiry, shortage, or No Show return request | Stock Ledger | Record Remove Stock or return Mutasi; Apotek shall not infer inventory movement without acknowledged Stock Ledger outcomes. |
| Apotek | Financial Charge, Credit Note, or Refund requirement | Tata Rekening | Resolve Financial Responsibility and settlement consequences without changing fulfillment history. |

## 9. Business Timing and Service Limits

| Timing fact | Authoritative rule |
|---|---|
| Pharmacy `CreatedAt` | Recorded when Patient Tracker issues the Queue Number. |
| Pharmacy `ServedAt` | Recorded when the first applicable Dispense Order produces `Medication Preparation Started`. |
| Pharmacy `DoneAt` | Recorded when Pharmacy Staff performs the coordinated pickup call. |
| Telaah Resep | May begin as soon as the Resep is available; it does not wait for Patient arrival or mapping. |
| General Patient preparation | Cannot begin before Payment Clearance and applicable Sales Invoice evidence satisfy Dispense Authorized. |
| BPJS preparation | Cannot begin before valid SEP, covered Fornas mapping, and Dispense Authorized. A Sales Invoice is not required. |
| Pickup call | Occurs only after every Dispense Order intended for that handover is `Prepared` or has an accountable exception outcome. |
| Final Dispense Review and education | Occur with the Patient or caregiver present after the pickup call and before Medication Handover. |
| BPJS Sales Invoice | Established only with successful Medication Handover. |
| Collection limit | No numerical value is currently authoritative. An authorized manual uncollected-medication resolution establishes `Collection Window Expired`. |

Technical timeouts, polling, retries, and application performance are outside this workflow.

## 10. Traceability

The `Domain References` section of each workflow specification is the source of record. The `Domain rules` column below is a projection and must match those references exactly, including Patient Tracker rules where referenced.

| Workflow ID | Domain rules | States | Domain Events | External authority |
|---|---|---|---|---|
| `WF-APT-RJ-001` | `BR-APT-061`–`BR-APT-065`, `BR-APT-082`, `BR-APT-084`–`BR-APT-087`, `BR-APT-097`; `BR-TRK-026`–`BR-TRK-035`, `BR-TRK-051` | `Unmapped`, `Mapped`, `Waiting` | `Queue Entry Created`, `Outpatient Queue Mapped`, `Queue Entry Identified` | Patient Tracker |
| `WF-APT-RJ-002` | `BR-APT-001`–`BR-APT-019`, `BR-APT-029`–`BR-APT-034`, `BR-APT-050`, `BR-APT-054`, `BR-APT-061`, `BR-APT-068`, `BR-APT-083`, `BR-APT-086`, `BR-APT-089`, `BR-APT-105`–`BR-APT-124` | `Available`, `Under Review`, `Approved`, `Partially Approved`, `Rejected`, `Established`, `Active` | `Telaah Resep Started`, `Medication Substitution Authorized`, `Telaah Resep Completed`, `Direct Medication Request Accepted`, `Sales Order Established`, `Dispense Order Established` | CPOE, Medication Catalog, Inventory |
| `WF-APT-RJ-003` | `BR-APT-020`–`BR-APT-028`, `BR-APT-033`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-067`–`BR-APT-072`, `BR-APT-076`–`BR-APT-083`, `BR-APT-088`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`, `BR-APT-125`–`BR-APT-128`; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046` | `Established`, `Issued`, `Financially Cleared`, `Released`, `Preparing`, `Prepared`, `Reviewed`, `Completed`, `In Service`, `Done` | `Sales Invoice Established`, `Payment Clearance Established`, `Medication Preparation Started`, `Medication Prepared`, `Patient Called for Pickup`, `Final Dispense Review Completed`, `Final Dispense Review Failed`, `Medication Handed Over` | Patient Tracker, Payment, Inventory, Tata Rekening |
| `WF-APT-RJ-004` | `BR-APT-020`–`BR-APT-026`, `BR-APT-029`–`BR-APT-045`, `BR-APT-066`, `BR-APT-068`–`BR-APT-069`, `BR-APT-073`–`BR-APT-079`, `BR-APT-081`–`BR-APT-083`, `BR-APT-088`, `BR-APT-090`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046` | `Awaiting Clearance`, `Released`, `Preparing`, `Prepared`, `Reviewed`, `Completed`, `In Service`, `Done` | `Coverage Clearance Established`, `Medication Preparation Started`, `Patient Called for Pickup`, `Final Dispense Review Failed`, `Sales Invoice Established`, `Medication Handed Over` | Patient Tracker, SEP, Fornas, Inventory, Tata Rekening |
| `WF-APT-RJ-005` | `BR-APT-011`, `BR-APT-015`, `BR-APT-020`–`BR-APT-028`, `BR-APT-040`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-070`–`BR-APT-078`, `BR-APT-090`–`BR-APT-096`, `BR-APT-108`, `BR-APT-119`–`BR-APT-124` | Independent BPJS-covered and Patient-Pay Sales Order states | `Coverage Clearance Established`, `Payment Clearance Established`, `Sales Order Established`, `Final Dispense Review Failed`, `Sales Invoice Established`, `Medication Handed Over` | SEP, Fornas, Payment, Tata Rekening |
| `WF-APT-RJ-006` | `BR-APT-011`, `BR-APT-015`, `BR-APT-022`, `BR-APT-030`, `BR-APT-056`–`BR-APT-060`, `BR-APT-084`–`BR-APT-088`, `BR-APT-095`–`BR-APT-096`; `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, `BR-TRK-045a` | Per-demand authoritative states; one queue `Waiting` → `In Service` → `Done` | `Outpatient Queue Mapped`, `Medication Preparation Started`, `Patient Called for Pickup`, `Final Dispense Review Failed`, `Medication Handed Over` | Patient Tracker |
| `WF-APT-RJ-007` | `BR-APT-018`–`BR-APT-019`, `BR-APT-027`, `BR-APT-045`–`BR-APT-047`, `BR-APT-052`–`BR-APT-060`, `BR-APT-069`, `BR-APT-078`–`BR-APT-080`, `BR-APT-095` | `Expired`, `Active`, `Resolved` | `Outpatient No-Show Recorded`, `Dispense Order Expired`, `Unfulfilled Medication Recorded`, `Medication Returned`, `Sales Invoice Credited`, `Refund Required`, `Sales Order Resolved` | Inventory, Tata Rekening |

Related canonical artifacts:

- [ADR-APT-002 Pharmacy and Stock Ledger Boundary](./adr/ADR-APT-002-pharmacy-stock-ledger-boundary.md)
- [Apotek Domain](./apotek-domain.md)
- [Apotek Domain — Bahasa Indonesia](./apotek-domain-id.md)
- [Outpatient Apotek Workflow — Bahasa Indonesia](./outpatient-apotek-workflow-id.md)
- [Patient Tracker Domain](../../contexts/pasien-tracker/TRACKER-DOMAIN.md)
- [CPOE Domain](../../contexts/cpoe/CPOE-DOMAIN.md)
- [Tata Rekening Domain](../../contexts/TataRekening/02-domain.md)

The seven paired outpatient operational specifications are listed in the [Outpatient Apotek SOP Index](./sop/DAFTAR-SOP-APT-RJ.md). Pharmacy stock integration is defined in [ADR-APT-002](./adr/ADR-APT-002-pharmacy-stock-ledger-boundary.md).
