# Outpatient Medication Fulfillment Workflow

**Artifact status:** Canonical business workflow specification

**Bounded context:** Medication Fulfillment (`Pelayanan Obat Pasien`)

**Care setting:** Outpatient Pharmacy

**Bahasa Indonesia companion:** [outpatient-medication-fulfillment-workflow-id.md](./outpatient-medication-fulfillment-workflow-id.md)

**Authoritative domain:** [Medication Fulfillment Domain](./medication-fulfillment-domain.md)

## 1. Workflow Overview

This workflow coordinates outpatient medication service from acquisition of a Pharmacy Queue Number through accountable Medication Handover or an accountable non-fulfillment outcome.

It covers Electronic Prescriptions, recorded Physical Prescriptions, and Direct Medication Requests; Tracker Mapping and Manual Mapping; General Patient, BPJS, and mixed-coverage commercial paths; one Pharmacy Queue Entry mapped to multiple medication demands; dispensing; pickup; Final Dispense Review; Patient Education; and No-Show resolution.

Prescription Review may run before Patient arrival and independently of queue mapping. Commercial and physical fulfillment progress remain independent and are coordinated through the Pharmacy Sales Order.

```text
Pharmacy Queue Entry and medication demand
  -> Outpatient Queue Mapping
  -> Prescription Review or direct-request acceptance
  -> Pharmacy Sales Order
       -> Billing Allocation -> Sales Invoice according to payer timing
       -> Fulfillment Allocation -> Dispense Order
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
| [Medication Fulfillment Domain](./medication-fulfillment-domain.md) | Prescription Review, Pharmacy Sales Order, allocations, Sales Invoice timing, Dispense Order, clearance, dispensing, handover, and non-fulfillment policy. |
| [Medication Fulfillment Domain — Bahasa Indonesia](./medication-fulfillment-domain-id.md) | Human-readable semantic companion to the canonical Medication Fulfillment domain. |
| [Patient Tracker Domain](../../contexts/pasien-tracker/TRACKER-DOMAIN.md) | Pharmacy Queue Entry identity, Queue Number, Queue Session, `CreatedAt`, `ServedAt`, `DoneAt`, and queue lifecycle. |
| [CPOE Domain](../../contexts/cpoe/CPOE-DOMAIN.md) | Original Electronic Prescription and clinician intent; CPOE remains authoritative for its Clinical Order. |
| [Tata Rekening Domain](../../contexts/TataRekening/02-domain.md) | Financial Charge, Financial Responsibility, financial correction, and settlement responsibility. |
| Approved outpatient pharmacy business discussion | Queue acquisition and mapping, payer timing, professional responsibilities, consolidated pickup, mixed coverage, and manual uncollected-medication resolution. |

Inventory, Payment, SEP, Fornas, and Medication Catalog authorities remain external even when no dedicated domain artifact is registered.

## 3. Scope and Boundaries

### 3.1 Begins

The end-to-end outpatient coordination begins when a Patient obtains a Pharmacy Queue Number by either:

- directly requesting a Queue Number; or
- presenting valid tracker or registration evidence that causes a Queue Entry to be created.

Prescription Review for an Electronic Prescription may have begun or completed before this end-to-end trigger.

### 3.2 Ends

The workflow ends when every medication demand mapped to the Pharmacy Queue Entry has reached one of these accountable outcomes:

- Medication Handover completed for the intended Dispense Order quantities;
- an accepted quantity received an accountable Unfulfilled Medication Outcome;
- an authorized manual uncollected-medication resolution established `Collection Window Expired` and all required stock and commercial consequences were resolved; or
- an explicitly identified external financial resolution remains outstanding and the Pharmacy Sales Order correctly remains `Active`.

### 3.3 Included

- Electronic Prescription availability before or after Queue Entry creation.
- Physical Prescription recording by Pharmacy Staff.
- Direct Medication Request acceptance, Pharmacist referral, or decline.
- Tracker Mapping and Manual Mapping.
- One Queue Entry mapped to one or more medication demands.
- Prescription Review and pre-Sales-Order Medication Substitution.
- General Patient verbal Purchase Confirmation before Sales Invoice establishment.
- BPJS coverage from valid SEP and authoritative Fornas mapping.
- Mixed BPJS-covered and Patient-payable quantities.
- Payment Clearance, Coverage Clearance, and Fulfillment Clearance.
- Stock Reservation, Medication Preparation, pickup calling, Final Dispense Review, Authorized Recipient verification, Patient Education, Medication Dispense, and Medication Handover.
- Backorder or another approved stock source for the same medication product.
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

## 4. Participants and Responsibility Handoffs

| Participant | Responsibility in this workflow | Handoff condition |
|---|---|---|
| Patient or Caregiver | Obtains a Queue Number, supplies mapping evidence or a Physical Prescription, gives verbal confirmation when Patient-payable, pays when required, presents for pickup, receives education, and accepts medication when authorized. | Evidence is supplied, confirmation is given or declined, Payment Clearance is obtained, or Medication Handover completes. |
| Patient Tracker | Owns Pharmacy Queue Entry identity, Queue Number, and queue lifecycle. | `Queue Entry Created`, `Queue Service Started`, or `Queue Service Completed`. |
| Pharmacy Staff | Performs administrative queue calls, Manual Mapping, records Physical Prescriptions, assesses Direct Medication Requests within authority, coordinates allocations, communicates General Patient value, establishes a confirmed Sales Invoice, and performs the pickup call. | `Outpatient Queue Mapped`, `Pharmacy Sales Order Established`, `Sales Invoice Established`, or `Patient Called for Pickup`. |
| Pharmacist | Performs Prescription Review, authorizes eligible Medication Substitution before Pharmacy Sales Order establishment, approves referred Direct Medication Requests, verifies the Authorized Recipient, performs Final Dispense Review, and provides Patient Education. | `Prescription Review Completed`, `Final Dispense Review Completed`, or Medication Handover is authorized to complete. |
| Pharmacy Technician | Performs Medication Preparation and Compounding and decides Backorder or another approved stock source for the same medication product within authority. | `Medication Prepared`, `Dispense Order Backordered`, or an exception is escalated. |
| Cashier or Payment Authority | Receives required Patient payment and supplies Payment Clearance. | `Payment Clearance Established`. |
| SEP and Fornas Authorities | Supply encounter-level SEP validity and item-level BPJS coverage. | `Coverage Clearance Established` for the covered quantity. |
| Inventory | Owns Stock Availability, Stock Reservation, Inventory Issue, return eligibility, and Return to Stock. | `Stock Reserved`, authoritative Inventory Issue, or accepted return disposition. |
| Tata Rekening | Owns Financial Responsibility and the required financial consequence when paid medication is not fulfilled or collected. | Credit Note, Refund, or another final commercial outcome is supplied. |
| CPOE or Prescribing Clinician | Owns the original Electronic Prescription and responds to Clinical Clarification or a required replacement Prescription. | Prescription is available, clarification is supplied, or corrected intent is established. |
| Pharmacy Supervisor | Authorizes exceptional expiry, manual uncollected-medication resolution, and decisions outside ordinary authority. | Accountable exception outcome is established. |

## 5. Entry Conditions and Triggers

### 5.1 End-to-end trigger

`Queue Entry Created` for the outpatient pharmacy Service Point starts queue coordination. Patient Tracker records `CreatedAt` at Queue Number issuance.

### 5.2 Independent medication-demand triggers

- An Electronic Prescription becomes available from CPOE or another clinical-order authority.
- Pharmacy Staff records a presented Physical Prescription.
- Pharmacy Staff accepts a Direct Medication Request within authority or after required Pharmacist approval.

These triggers may occur before or after Outpatient Queue Mapping as permitted by `BR-MF-061` and `BR-MF-062`.

### 5.3 Preconditions

- Every Prescription retains an authoritative source and Patient association.
- A Physical Prescription is recorded before Prescription Review.
- A Direct Medication Request is accepted before it may establish a Pharmacy Sales Order.
- Tracker Mapping requires valid evidence that resolves the applicable medication demand.
- Manual Mapping requires Pharmacy Staff to identify the Queue Number and applicable demand.
- Medication Preparation requires an active Dispense Order and payer-appropriate Fulfillment Clearance.
- Medication Handover requires a Prepared Medication, Authorized Recipient, successful Final Dispense Review, and applicable Patient Education.

### 5.4 Blocking conditions

- An unresolved direct Queue Number cannot proceed past mapping-dependent clearance.
- A Prescription with an incomplete Prescription Review cannot establish a Pharmacy Sales Order.
- A rejected Prescription or declined Direct Medication Request cannot establish a Pharmacy Sales Order.
- A General Patient quantity cannot begin Medication Preparation without Payment Clearance.
- A BPJS-covered quantity cannot begin Medication Preparation without valid SEP, authoritative Fornas coverage, and Fulfillment Clearance.
- A medication identity cannot be substituted after Pharmacy Sales Order establishment.

## 6. Workflow Inventory

| ID | Workflow | Business outcome |
|---|---|---|
| `WF-MF-RJ-001` | Acquire and Map Outpatient Pharmacy Queue | A Pharmacy Queue Entry is accountably mapped to one or more medication demands, or its unresolved/declined outcome is handed back to the applicable queue policy. |
| `WF-MF-RJ-002` | Accept Outpatient Medication Demand | Accepted medication demand establishes a traceable Pharmacy Sales Order and primary outpatient Dispense Order, or receives an accountable rejection or clarification outcome. |
| `WF-MF-RJ-003` | Fulfill Medication for a General Patient | Verbally confirmed and paid medication is prepared and handed over, or receives an accountable alternative or exception outcome. |
| `WF-MF-RJ-004` | Fulfill Medication for a BPJS Patient | Covered medication is prepared without a prior Sales Invoice and the BPJS Sales Invoice is established only with successful Medication Handover. |
| `WF-MF-RJ-005` | Fulfill Mixed-Coverage Medication | Covered and Patient-payable quantities receive separate commercial allocation and clearance while remaining coordinated for one pickup. |
| `WF-MF-RJ-006` | Coordinate Multiple Medication Demands in One Queue | Multiple independent demand, Sales Order, invoice, and Dispense Order lifecycles are coordinated into one queue service and pickup session without being merged. |
| `WF-MF-RJ-007` | Resolve Uncollected Outpatient Medication | Prepared but uncollected medication receives an authorized expiry, Inventory return disposition, and payer-specific commercial resolution. |

## 7. Workflow Specifications

### WF-MF-RJ-001 — Acquire and Map Outpatient Pharmacy Queue

**Indonesia:** Mengambil dan Memapping Antrean Apotek Rawat Jalan

#### Purpose

Associate one Pharmacy Queue Entry with every applicable outpatient medication demand without making queue arrival a prerequisite for Prescription Review.

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
- Existing Electronic Prescriptions or Pharmacy Sales Orders when already available.
- Physical Prescription or Direct Medication Request when presented at the counter.

#### Main Flow

1. Patient Tracker establishes the Pharmacy Queue Entry, assigns the Queue Number, and records `CreatedAt`.
2. When valid tracker or registration evidence resolves the applicable medication demand, Medication Fulfillment establishes Tracker Mapping.
3. Medication Fulfillment establishes Outpatient Queue Mapping between the Queue Entry and every resolved applicable demand.
4. For each mapped demand, Medication Fulfillment exposes its authoritative progress as a queue-facing projection without transferring state ownership to Patient Tracker.
5. Queue coordination waits for payer-appropriate fulfillment while Prescription Review and Pharmacy Sales Order establishment may continue independently.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| Tracker or registration evidence resolves demand | Medication Fulfillment | Use Tracker Mapping; no initial administrative call is required. |
| Evidence fails to resolve demand | Pharmacy Staff | Fall back to Manual Mapping. |
| Queue Number was obtained directly | Pharmacy Staff | Call the Queue Number for administrative identification and Manual Mapping. |
| One Queue Entry has multiple applicable demands | Pharmacy Staff | Map every demand separately to the same Queue Entry under `BR-MF-084` and `BR-MF-085`. |

For Manual Mapping:

1. Pharmacy Staff calls the unresolved Queue Number without starting Patient Tracker service.
2. Pharmacy Staff identifies an existing Prescription or Pharmacy Sales Order, records a presented Physical Prescription, or evaluates a Direct Medication Request.
3. Medication Fulfillment establishes Manual Mapping for every identified applicable demand.

#### Exception and Compensation Flows

- If a Direct Medication Request is declined, no Direct Medication Request record or Pharmacy Sales Order is established. Final disposition of the still-Waiting Queue Entry follows Patient Tracker's applicable withdrawal policy and remains external to Medication Fulfillment.
- If the Queue Number cannot be matched to an accountable Patient Journey or medication demand, the Queue Entry remains unmapped and cannot receive mapping-dependent Fulfillment Clearance.
- Mapping correction adds an accountable correcting fact and shall not rewrite Prescription Review or Pharmacy Sales Order history.

#### Outcomes and Postconditions

- Success: `Outpatient Queue Mapped` exists for one or more demands.
- Accountable non-completion: the Queue Entry remains unmapped pending evidence, or a declined Direct Medication Request produces no medication demand.
- Mapping does not establish `ServedAt`, create an Electronic Prescription, or complete Prescription Review.

#### Domain References

`BR-MF-061`–`BR-MF-065`, `BR-MF-082`, `BR-MF-084`–`BR-MF-087`; `BR-TRK-026`–`BR-TRK-035`.

#### Domain Events

- Consumed: `Queue Entry Created`.
- Produced or observed: `Outpatient Queue Mapped`, `Queue Entry Identified` when externally applicable.

### WF-MF-RJ-002 — Accept Outpatient Medication Demand

**Indonesia:** Menerima Permintaan Obat Rawat Jalan

#### Purpose

Turn a reviewed Prescription or accepted Direct Medication Request into a traceable Pharmacy Sales Order and primary outpatient Dispense Order.

#### Trigger

A Prescription becomes available, or a Direct Medication Request is presented for acceptance.

#### Preconditions

- A Prescription is owned by CPOE or another accountable clinical-order authority, or Pharmacy Staff has authority to assess the Direct Medication Request.
- A Physical Prescription has been recorded before review.

#### Participants

Pharmacist, Pharmacy Staff, Pharmacy Technician, CPOE or Prescribing Clinician.

#### Input Business Facts

- Electronic Prescription or recorded Physical Prescription.
- Direct Medication Request details when applicable.
- Medication Catalog and professional acceptance policy.
- Stock Availability as an external fulfillment fact that does not determine clinical acceptance.

#### Main Flow

1. For a Prescription, the Pharmacist starts Prescription Review as soon as the Prescription is available, without waiting for Patient arrival or Outpatient Queue Mapping.
2. The Pharmacist assigns a disposition to every Prescription Line and requests Clinical Clarification when required.
3. When an authorized Medication Substitution is needed, the Pharmacist completes it during Prescription Review and preserves the originally requested medication, accepted substitute, reason, affected quantity, and responsible Pharmacist.
4. The Pharmacist completes Prescription Review as `Approved`, `Partially Approved`, or `Rejected`.
5. For an accepted Direct Medication Request, Pharmacy Staff accepts within authority or obtains required Pharmacist approval; no Prescription is created.
6. Medication Fulfillment establishes a Pharmacy Sales Order from exactly one completed accepted-demand source and preserves Source Traceability.
7. Medication Fulfillment establishes applicable Billing Allocations and Fulfillment Allocations independently.
8. For the normal outpatient episode, Medication Fulfillment establishes one active primary Dispense Order for the active Pharmacy Sales Order.
9. Inventory may establish Stock Reservation before Patient arrival or queue mapping, while Medication Preparation waits for applicable Fulfillment Clearance.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| All Prescription Lines accepted | Pharmacist | `Approved`; all accepted lines may establish the Pharmacy Sales Order. |
| Some lines accepted | Pharmacist | `Partially Approved`; only Accepted Medication Lines enter the Pharmacy Sales Order. |
| No line accepted | Pharmacist | `Rejected`; no Pharmacy Sales Order is established. |
| Clarification required | Pharmacist and Prescribing Clinician | Enter `Clarification Required`; resume review only after accountable clarification. |
| Direct request within staff authority | Pharmacy Staff | Accept and establish the Direct Medication Request source. |
| Direct request needs professional approval | Pharmacy Staff and Pharmacist | Refer, then accept only after approval. |
| Direct request declined | Pharmacy Staff or Pharmacist | Do not establish a request record or Pharmacy Sales Order. |

#### Exception and Compensation Flows

- Stock shortage does not change the Prescription Review Outcome. The Pharmacy Technician may choose Backorder or another approved stock source for the same medication product after Pharmacy Sales Order establishment.
- Medication identity shall not be substituted after Pharmacy Sales Order establishment. A later clinical replacement requires a corrected or replacement Prescription and a new Prescription Review decision.
- Any accepted quantity that cannot be fulfilled must retain an accountable Backorder, `Cancelled`, `Expired`, or other Unfulfilled Medication Outcome.

#### Outcomes and Postconditions

- Success: `Prescription Review Completed`, `Pharmacy Sales Order Established`, `Fulfillment Allocation Established`, and `Dispense Order Established` are observed as applicable.
- Partial success: only Accepted Medication Lines enter the Pharmacy Sales Order.
- Rejection: no Pharmacy Sales Order exists for the rejected source.
- The Pharmacy Sales Order is not a Sales Invoice, Dispense Order, reservation, or dispense evidence.

#### Domain References

`BR-MF-001`–`BR-MF-019`, `BR-MF-029`–`BR-MF-034`, `BR-MF-050`, `BR-MF-061`, `BR-MF-068`, `BR-MF-083`, `BR-MF-086`, `BR-MF-089`; Prescription Review and Pharmacy Sales Order lifecycles.

#### Domain Events

- Consumed: `Clinical Order Created` or another authoritative Prescription-availability fact.
- Produced: `Prescription Review Started`, `Clinical Clarification Requested`, `Medication Substitution Authorized`, `Prescription Review Completed`, `Direct Medication Request Accepted`, `Pharmacy Sales Order Established`, `Billing Allocation Established`, `Fulfillment Allocation Established`, `Dispense Order Established`, `Stock Reserved` when externally supplied.

### WF-MF-RJ-003 — Fulfill Medication for a General Patient

**Indonesia:** Memenuhi Obat untuk Pasien Umum

#### Purpose

Obtain verbal Purchase Confirmation before Sales Invoice establishment, obtain Patient payment, and complete accountable outpatient Medication Handover.

#### Trigger

Outpatient Queue Mapping, an active Pharmacy Sales Order, applicable Billing Allocations, and a calculated Patient-payable amount are available.

#### Preconditions

- The General Patient amount is calculated from accountable Billing Allocations and the applicable Pricing Snapshot.
- No Sales Invoice has yet been established for the proposed Patient-payable sale.
- An applicable Dispense Order exists or can be established from Fulfillment Allocations.

#### Participants

Patient or Caregiver, Pharmacy Staff, Cashier or Payment Authority, Pharmacy Technician, Pharmacist, Patient Tracker, Inventory, Tata Rekening.

#### Input Business Facts

- Outpatient Queue Mapping.
- Pharmacy Sales Order and Patient-payable Billing Allocations.
- Pricing Snapshot and calculated amount.
- Dispense Order and Stock Reservation when already available.

#### Main Flow

1. Pharmacy Staff receives the Patient in the Purchase Confirmation interaction—within the Manual Mapping counter interaction when possible, or through a separate administrative Queue Number call after Tracker Mapping—and communicates the calculated total verbally before a Sales Invoice exists. This interaction does not establish `ServedAt` or `DoneAt`.
2. The Patient gives verbal Purchase Confirmation.
3. Pharmacy Staff establishes the Sales Invoice from the confirmed Billing Allocations; Sales Invoice establishment is the accountable evidence that confirmation was obtained, and no separate confirmation object or transaction exists.
4. The Cashier receives payment and supplies Payment Clearance for the Sales Invoice.
5. Medication Fulfillment establishes Fulfillment Clearance for the applicable Dispense Order quantities.
6. Inventory secures the required Stock Reservation when not already reserved.
7. The Pharmacy Technician begins Medication Preparation under the released Dispense Order.
8. Medication Fulfillment observes `Medication Preparation Started`; Patient Tracker enters the Pharmacy Queue Entry into In Service and records `ServedAt`.
9. The Pharmacy Technician completes Medication Preparation; the Dispense Order reaches `Prepared` and the medication remains In-Transit Medication.
10. When every Dispense Order intended for the coordinated handover is `Prepared` or has an accountable exception outcome, Pharmacy Staff performs the pickup call.
11. Patient Tracker makes the Pharmacy Queue Entry `Done` and records `DoneAt` at the pickup-call time.
12. With the Patient or caregiver present, the Pharmacist verifies the Authorized Recipient, completes Final Dispense Review, and provides applicable Patient Education in the same counter interaction.
13. Medication Fulfillment records Medication Dispense and completes Medication Handover for each applicable Dispense Order quantity.
14. Medication Handover completes the Dispense Order quantity and requests Inventory's authoritative Inventory Issue outcome.
15. The Pharmacy Sales Order becomes `Resolved` only when all accepted quantities and commercial consequences have final accountable outcomes.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| Patient declines before Sales Invoice establishment | Patient | No Sales Invoice is established; unused Stock Reservation is released; Patient-payable allocation receives an accountable declined or commercially unallocated outcome. |
| Calculated amount changes before establishment | Pharmacy Staff | Communicate the revised amount and obtain verbal confirmation again before establishing the Sales Invoice. |
| Multiple demands share one Queue Entry | Pharmacy Staff | Apply `WF-MF-RJ-006`; retain separate Sales Orders, invoices, and Dispense Orders. |
| Patient does not collect after pickup call | Pharmacy Supervisor | Apply `WF-MF-RJ-007`. |

#### Exception and Compensation Flows

- If payment is not completed after Sales Invoice establishment, Medication Preparation remains blocked. The Sales Invoice may be `Cancelled` only while its lifecycle permits.
- If an issued or financially cleared Sales Invoice needs correction, use Financial Adjustment, Credit Note, or Refund under Tata Rekening authority; do not silently replace it.
- If shortage occurs after payment, the Pharmacy Technician may select Backorder or another approved stock source for the same medication product. Substitution is prohibited because the Pharmacy Sales Order already exists.
- If fulfillment cannot complete, affected quantities receive an accountable Unfulfilled Medication Outcome and Tata Rekening receives the required financial consequence.
- A failed Final Dispense Review prevents Medication Handover and returns the affected Dispense Order to accountable exception resolution without changing the original Prescription.

#### Outcomes and Postconditions

- Successful completion: Sales Invoice is financially cleared, Dispense Order is `Completed`, Medication Handover identifies the Authorized Recipient, and all quantities remain traceable.
- Declined purchase: no Sales Invoice exists for the declined proposal.
- Paid non-fulfillment or No-Show: fulfillment and commercial consequences remain separately accountable; the Pharmacy Sales Order remains `Active` until both are final.
- Queue `Done` does not prove Medication Handover.

#### Domain References

`BR-MF-020`–`BR-MF-028`, `BR-MF-033`–`BR-MF-046`, `BR-MF-056`–`BR-MF-060`, `BR-MF-067`–`BR-MF-072`, `BR-MF-076`–`BR-MF-083`, `BR-MF-088`, `BR-MF-095`; Sales Invoice and Dispense Order lifecycles; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046`.

#### Domain Events

- Consumed: `Outpatient Queue Mapped`, `Billing Allocation Established`, `Payment Clearance Established`, `Stock Reserved`.
- Produced or observed: `Sales Invoice Established`, `Sales Invoice Issued`, `Fulfillment Clearance Established`, `Medication Preparation Started`, `Pharmacy Service Started`, `Medication Prepared`, `Patient Called for Pickup`, `Queue Service Started`, `Queue Service Completed`, `Final Dispense Review Completed`, `Medication Dispensed`, `Medication Handed Over`, `Pharmacy Sales Order Resolved`.

### WF-MF-RJ-004 — Fulfill Medication for a BPJS Patient

**Indonesia:** Memenuhi Obat untuk Pasien BPJS

#### Purpose

Prepare covered outpatient medication without prior Sales Invoice or Patient payment and establish the BPJS Sales Invoice only with successful Medication Handover.

#### Trigger

Outpatient Queue Mapping, an applicable active Pharmacy Sales Order, a Dispense Order, valid SEP, and authoritative item-level Fornas coverage are available.

#### Preconditions

- The Patient has a valid SEP for the applicable encounter.
- Each covered quantity is supported by authoritative Fornas mapping.
- No Patient Purchase Confirmation or Patient payment is required for the covered quantity.
- The BPJS Sales Invoice has not yet been established.

#### Participants

Patient or Caregiver, Pharmacy Staff, Pharmacy Technician, Pharmacist, Patient Tracker, SEP and Fornas Authorities, Inventory, Tata Rekening.

#### Input Business Facts

- Outpatient Queue Mapping.
- Pharmacy Sales Order, covered Billing Allocations, and Dispense Order.
- Valid SEP and item-level Fornas coverage.
- Stock Availability and Stock Reservation outcomes.

#### Main Flow

1. The SEP and Fornas authorities establish Coverage Clearance for each covered quantity.
2. Medication Fulfillment establishes Fulfillment Clearance for the applicable Dispense Order quantities without requiring an existing Sales Invoice.
3. Inventory secures Stock Reservation when not already reserved.
4. The Pharmacy Technician starts Medication Preparation.
5. `Medication Preparation Started` causes Patient Tracker to record `ServedAt` and move the Pharmacy Queue Entry to In Service.
6. The Pharmacy Technician completes Medication Preparation; the Dispense Order reaches `Prepared` and the medication remains In-Transit Medication.
7. When every Dispense Order intended for the coordinated handover is `Prepared` or has an accountable exception outcome, Pharmacy Staff performs the pickup call.
8. Patient Tracker records `DoneAt` and makes the Queue Entry `Done` at the pickup-call time.
9. With the Patient or caregiver present, the Pharmacist verifies the Authorized Recipient, completes Final Dispense Review, and provides applicable Patient Education in the same counter interaction.
10. As one accountable business outcome, Medication Fulfillment establishes the BPJS Sales Invoice from the covered Billing Allocations, records Medication Dispense, and completes Medication Handover.
11. Medication Handover completes each applicable Dispense Order quantity and requests Inventory's authoritative Inventory Issue outcome.
12. The Pharmacy Sales Order becomes `Resolved` only when every accepted quantity and required commercial consequence has a final outcome.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| Electronic Prescription and Tracker Mapping succeed in the normal path | Medication Fulfillment | One outpatient call occurs: the pickup call after `Prepared`. |
| SEP invalid | SEP authority | Coverage Clearance is absent; Medication Preparation remains blocked. |
| Item not covered by Fornas | Pharmacy Staff | Route the non-covered quantity through `WF-MF-RJ-005`. |
| Multiple mapped demands | Pharmacy Staff | Apply `WF-MF-RJ-006`; maintain separate records and one coordinated pickup. |
| Patient does not collect | Pharmacy Supervisor | Apply `WF-MF-RJ-007`; no BPJS Sales Invoice is established. |

#### Exception and Compensation Flows

- A BPJS No-Show before Medication Handover establishes no Sales Invoice and requires no Sales Invoice cancellation.
- Shortage after Pharmacy Sales Order establishment permits Backorder or another approved stock source for the same medication product; it does not permit substitution.
- Failed Final Dispense Review prevents both BPJS Sales Invoice establishment and Medication Handover.
- Inventory determines whether reserved or In-Transit Medication is eligible for return.

#### Outcomes and Postconditions

- Successful completion: BPJS Sales Invoice establishment and Medication Handover form one accountable outcome; Patient-payable amount is zero and payment disposition is `Not Required`.
- Coverage blocked: no preparation occurs for the uncleared quantity.
- No-Show: no BPJS Sales Invoice exists; Dispense Order and stock follow `WF-MF-RJ-007`.
- Queue `Done` remains independent of Medication Handover completion.

#### Domain References

`BR-MF-020`–`BR-MF-026`, `BR-MF-029`–`BR-MF-045`, `BR-MF-066`, `BR-MF-068`–`BR-MF-069`, `BR-MF-073`–`BR-MF-079`, `BR-MF-081`–`BR-MF-083`, `BR-MF-088`, `BR-MF-090`, `BR-MF-095`; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046`.

#### Domain Events

- Consumed: `Outpatient Queue Mapped`, `Coverage Clearance Established`, `Fulfillment Allocation Established`, `Stock Reserved`.
- Produced or observed: `Fulfillment Clearance Established`, `Medication Preparation Started`, `Pharmacy Service Started`, `Medication Prepared`, `Patient Called for Pickup`, `Queue Service Started`, `Queue Service Completed`, `Final Dispense Review Completed`, `Sales Invoice Established`, `Sales Invoice Issued`, `Medication Dispensed`, `Medication Handed Over`, `Pharmacy Sales Order Resolved`.

### WF-MF-RJ-005 — Fulfill Mixed-Coverage Medication

**Indonesia:** Memenuhi Obat dengan Coverage Campuran

#### Purpose

Separate BPJS-covered and Patient-payable commercial responsibility while coordinating all quantities intended for one outpatient pickup.

#### Trigger

One Pharmacy Sales Order contains quantities classified partly as BPJS-covered and partly as Patient-payable.

#### Preconditions

- A valid SEP exists for the encounter.
- Authoritative Fornas mapping identifies covered and non-covered quantities.
- Pharmacy Staff can establish separate Billing Allocations without changing the accepted medication identity or quantity.

#### Participants

Patient or Caregiver, Pharmacy Staff, Cashier or Payment Authority, Pharmacy Technician, Pharmacist, Patient Tracker, SEP and Fornas Authorities, Inventory, Tata Rekening.

#### Input Business Facts

- One Pharmacy Sales Order and its Sales Order Lines.
- Valid SEP and authoritative item-level Fornas coverage.
- Covered and Patient-payable Billing Allocations.
- Applicable Dispense Order and Fulfillment Allocations.

#### Main Flow

1. Pharmacy Staff separates Billing Allocations into BPJS-covered and Patient-payable quantities.
2. SEP validity and Fornas mapping establish Coverage Clearance for covered quantities.
3. Before any General Patient Sales Invoice exists, Pharmacy Staff conducts the Purchase Confirmation interaction defined by `WF-MF-RJ-003` and communicates the calculated Patient-payable amount verbally.
4. The Patient gives verbal Purchase Confirmation for the non-covered quantities.
5. Pharmacy Staff establishes a separate General Patient Sales Invoice from the confirmed Patient-payable Billing Allocations.
6. The Cashier supplies Payment Clearance for the General Patient Sales Invoice.
7. Medication Fulfillment establishes Fulfillment Clearance for covered quantities from Coverage Clearance and for Patient-payable quantities from Payment Clearance.
8. After every quantity intended for the handover has its applicable clearance, the Pharmacy Technician begins and completes Medication Preparation.
9. The first `Medication Preparation Started` records Patient Tracker `ServedAt`; every intended Dispense Order reaches `Prepared` before pickup.
10. Pharmacy Staff performs one coordinated pickup call; Patient Tracker records `DoneAt`.
11. With the Patient or caregiver present, the Pharmacist verifies the Authorized Recipient, completes Final Dispense Review, and provides Patient Education.
12. Medication Fulfillment establishes the BPJS Sales Invoice from covered Billing Allocations only as Medication Handover succeeds; the General Patient Sales Invoice already exists and is financially cleared.
13. Medication Fulfillment records Medication Dispense and Medication Handover for all applicable quantities and requests Inventory Issue outcomes.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| Patient confirms the non-covered portion | Patient | Establish and collect the General Patient Sales Invoice; coordinate both payer portions. |
| Patient declines the non-covered portion before invoice establishment | Patient | Establish no General Patient Sales Invoice; give the allocation an accountable declined or commercially unallocated outcome; continue the BPJS portion independently. |
| A covered item lacks Fornas coverage | Pharmacy Staff | Reclassify it as Patient-payable only through accountable Billing Allocation; communicate the revised amount and request verbal confirmation. |
| Not all intended quantities have clearance | Medication Fulfillment | Do not start coordinated preparation for those quantities and do not perform the pickup call. |

#### Exception and Compensation Flows

- An established General Patient Sales Invoice follows General Patient cancellation and correction rules; the BPJS Sales Invoice remains absent until handover.
- No-Show after payment follows the paid General Patient commercial path while the absent BPJS Sales Invoice follows the uninvoiced BPJS path.
- Partial non-fulfillment preserves payer-specific Billing Allocations and requires separate commercial consequences.
- Substitution is prohibited after Pharmacy Sales Order establishment.

#### Outcomes and Postconditions

- Success: separate Sales Invoices represent covered and Patient-payable Medication Sales, and one coordinated Medication Handover preserves allocation-level traceability.
- Patient declines non-covered quantities: covered quantities may complete independently with no General Patient Sales Invoice for the declined portion.
- Any unresolved commercial consequence keeps the Pharmacy Sales Order `Active`.

#### Domain References

`BR-MF-015`, `BR-MF-020`–`BR-MF-028`, `BR-MF-040`–`BR-MF-046`, `BR-MF-056`–`BR-MF-060`, `BR-MF-070`–`BR-MF-078`, `BR-MF-090`–`BR-MF-095`.

#### Domain Events

- Consumed: `Billing Allocation Established`, `Coverage Clearance Established`, `Payment Clearance Established`, `Outpatient Queue Mapped`.
- Produced or observed: `Sales Invoice Established`, `Sales Invoice Issued`, `Fulfillment Clearance Established`, `Medication Preparation Started`, `Medication Prepared`, `Patient Called for Pickup`, `Final Dispense Review Completed`, `Medication Dispensed`, `Medication Handed Over`, `Pharmacy Sales Order Resolved` when fully reconciled.

### WF-MF-RJ-006 — Coordinate Multiple Medication Demands in One Queue

**Indonesia:** Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrean

#### Purpose

Coordinate two or more independently accountable medication demands in one outpatient queue and pickup session without merging their business records.

#### Trigger

One Pharmacy Queue Entry is mapped to two or more Prescriptions, Direct Medication Requests, or resulting Pharmacy Sales Orders.

#### Preconditions

- Every demand has a separate Outpatient Queue Mapping to the common Queue Entry.
- Every Prescription follows its own Prescription Review.
- Every accepted source establishes its own Pharmacy Sales Order and active primary outpatient Dispense Order.

#### Participants

Pharmacy Staff, Pharmacist, Pharmacy Technician, Patient or Caregiver, Patient Tracker, Cashier or Payment Authority, SEP and Fornas Authorities, Inventory.

#### Input Business Facts

- One Pharmacy Queue Entry.
- Two or more mapped medication demands.
- Per-demand Pharmacy Sales Order, Billing Allocation, Fulfillment Allocation, Sales Invoice, clearance, and Dispense Order progress.

#### Main Flow

1. Medication Fulfillment retains a separate Outpatient Queue Mapping for every demand associated with the common Pharmacy Queue Entry.
2. Each demand progresses independently through Prescription Review or direct acceptance, Pharmacy Sales Order establishment, commercial allocation, fulfillment allocation, and payer clearance.
3. The queue-facing view projects the authoritative progress of each mapped demand without owning those states.
4. The first applicable `Medication Preparation Started` causes Patient Tracker to record one `ServedAt` for the common Queue Entry.
5. Pharmacy Staff waits until every Dispense Order intended for the pickup is `Prepared` or has an accountable exception outcome.
6. Pharmacy Staff performs one coordinated pickup call; Patient Tracker records one `DoneAt` for the common Queue Entry.
7. With the Patient or caregiver present, the Pharmacist verifies the Authorized Recipient, completes Final Dispense Review for each Prepared Medication, and provides consolidated Patient Education while preserving medication-specific instructions.
8. Medication Fulfillment records Medication Dispense and Medication Handover against every applicable Dispense Order and Pharmacy Sales Order separately.

#### Decision and Alternative Flows

| Condition | Decision owner | Branch |
|---|---|---|
| Every included demand is ready | Pharmacy Staff | Perform one coordinated pickup call and handover session. |
| One demand has an accountable exception outcome | Pharmacy Staff | Include the resolved exception in the coordinated communication and proceed with ready demands when their payer rules permit. |
| One demand remains unresolved | Pharmacy Staff | Do not declare that demand ready; defer the coordinated call unless the Patient accepts an accountable partial path permitted by its payer workflow. |
| Demands have different payer classifications | Pharmacy Staff | Apply the applicable General, BPJS, or mixed-coverage workflow to each demand before coordination. |

#### Exception and Compensation Flows

- Correcting one mapping shall not rewrite another demand's history.
- Cancellation, expiry, Backorder, financial correction, and return remain attached to their originating Pharmacy Sales Order and Dispense Order.
- One successful Medication Handover shall not be inferred to fulfill another mapped demand without its own handover fact.

#### Outcomes and Postconditions

- One Queue Entry has one `CreatedAt`, at most one `ServedAt`, and one `DoneAt`.
- Each Prescription, Pharmacy Sales Order, Sales Invoice, and Dispense Order retains independent identity and lifecycle.
- One pickup call and counter interaction may coordinate multiple accountable Medication Handover facts.

#### Domain References

`BR-MF-011`, `BR-MF-015`, `BR-MF-022`, `BR-MF-030`, `BR-MF-056`–`BR-MF-060`, `BR-MF-084`–`BR-MF-088`, `BR-MF-095`; `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, `BR-TRK-045a`.

#### Domain Events

- Consumed: `Outpatient Queue Mapped`, `Medication Preparation Started`, `Medication Prepared`.
- Produced or observed: `Queue Service Started`, `Patient Called for Pickup`, `Queue Service Completed`, `Final Dispense Review Completed`, `Medication Dispensed`, `Medication Handed Over` for each applicable demand.

### WF-MF-RJ-007 — Resolve Uncollected Outpatient Medication

**Indonesia:** Menyelesaikan Obat Rawat Jalan yang Tidak Diambil

#### Purpose

Give Prepared Medication that is not collected an authorized expiry, stock disposition, and payer-specific commercial outcome without inventing an automatic time limit.

#### Trigger

The Pharmacy Supervisor or another authorized role manually determines that the permitted collection opportunity has ended for Prepared Medication that was not handed over.

#### Preconditions

- Medication remains Prepared or In-Transit and Medication Handover has not completed.
- The Patient did not collect the medication.
- The accountable manual closing authority and effective business time are known.

#### Participants

Pharmacy Supervisor, Pharmacy Staff, Inventory, Tata Rekening, Patient Tracker.

#### Input Business Facts

- Pharmacy Queue Entry, which may already be `Done` after the pickup call.
- Pharmacy Sales Order, Dispense Order, and unresolved quantities.
- Sales Invoice presence and financial disposition by payer.
- Prepared or In-Transit Medication and Inventory disposition eligibility.

#### Main Flow

1. The authorized role performs the manual uncollected-medication resolution and records the Patient as No-Show for the affected fulfillment.
2. Medication Fulfillment gives each affected Dispense Order the terminal state `Expired`.
3. The resolution retains reason `Collection Window Expired`, responsible party, effective business time, affected quantity, and Source Traceability.
4. Inventory determines the authoritative disposition of reserved or In-Transit Medication and accepts Return to Stock only when eligible.
5. Medication Fulfillment records the resulting Unfulfilled Medication Outcome for each affected quantity.
6. Medication Fulfillment resolves the payer-specific commercial consequence.
7. The Pharmacy Sales Order becomes `Resolved` only after every accepted quantity and required commercial consequence has a final accountable outcome.

#### Decision and Alternative Flows

| Payer condition | Commercial outcome |
|---|---|
| BPJS Sales Invoice was not established because handover failed | No invoice is established or cancelled; resolve fulfillment and Inventory only, then resolve the Pharmacy Sales Order when all outcomes are final. |
| General Patient Sales Invoice is paid | Tata Rekening or the responsible financial authority supplies Credit Note, Refund, or another final outcome; the Pharmacy Sales Order remains `Active` until then. |
| General Patient proposal was declined before invoice establishment | No Sales Invoice exists; resolve any unused reservation and commercially unallocated quantity. |
| Mixed coverage | Resolve covered uninvoiced and paid Patient-payable consequences separately using their Billing Allocations. |

#### Exception and Compensation Flows

- No numerical collection limit is invented. Until an authoritative policy supplies one, only the authorized manual activity establishes the end of the collection opportunity.
- Queue `DoneAt` is not reversed; No-Show resolution belongs to Medication Fulfillment after queue completion.
- Inventory may reject Return to Stock under its own policy; the rejected return still requires an accountable final Inventory disposition.
- A paid commercial consequence shall not be silently erased or treated as the uninvoiced BPJS path.

#### Outcomes and Postconditions

- BPJS No-Show: Dispense Order is `Expired`, no BPJS Sales Invoice exists, and stock has an accountable disposition.
- Paid General Patient No-Show: Dispense Order is `Expired`; the Pharmacy Sales Order remains `Active` until the financial outcome is final.
- Final resolution: Pharmacy Sales Order is `Resolved` with reason `Collection Window Expired` after all fulfillment and commercial consequences are final.

#### Domain References

`BR-MF-018`–`BR-MF-019`, `BR-MF-027`, `BR-MF-045`–`BR-MF-047`, `BR-MF-052`–`BR-MF-060`, `BR-MF-069`, `BR-MF-078`–`BR-MF-080`, `BR-MF-095`; Dispense Order, quantity, pickup, and Pharmacy Sales Order lifecycles.

#### Domain Events

- Consumed: `Patient Called for Pickup`, `Medication Prepared`.
- Produced or observed: `Outpatient No-Show Recorded`, `Dispense Order Expired`, `Unfulfilled Medication Recorded`, `Medication Returned`, `Sales Invoice Credited`, `Refund Required`, `Pharmacy Sales Order Resolved` when fully reconciled.

## 8. Cross-Context Handoffs

| From | Authoritative business fact | To | Resulting responsibility |
|---|---|---|---|
| Patient Tracker | `Queue Entry Created`, Queue Number, `CreatedAt` | Medication Fulfillment | Establish Tracker Mapping or Manual Mapping without taking ownership of queue identity. |
| Medication Fulfillment | `Medication Preparation Started` | Patient Tracker | Move the Pharmacy Queue Entry to In Service and record `ServedAt`; Patient Tracker shall not infer Medication Handover. |
| Medication Fulfillment | `Patient Called for Pickup` | Patient Tracker | Make the Pharmacy Queue Entry `Done` and record `DoneAt`; later professional review and handover remain Medication Fulfillment facts. |
| CPOE or clinical-order authority | Prescription availability and clinician intent | Medication Fulfillment | Perform Prescription Review without modifying the original Prescription. |
| Medication Fulfillment | Fulfillment projection by Prescription and realized Medication Handover | EMR reporting | Display Prescription-to-realization information without changing the CPOE Clinical Order in the initial scope. |
| SEP authority | Valid SEP | Medication Fulfillment | Evaluate encounter-level BPJS coverage; SEP alone does not identify covered medication quantities. |
| Fornas authority | Item-level coverage mapping | Medication Fulfillment | Establish Coverage Clearance only for applicable covered quantities together with valid SEP. |
| Cashier or Payment authority | `Payment Clearance Established` | Medication Fulfillment | Establish applicable Fulfillment Clearance; payment does not prove stock or handover. |
| Inventory | Stock Availability and `Stock Reserved` | Medication Fulfillment | Prepare only authorized Dispense Order quantities; stock facts do not rewrite Prescription Review. |
| Medication Fulfillment | Handover, expiry, shortage, or return request | Inventory | Supply authoritative Inventory Issue or return disposition; Medication Fulfillment shall not infer inventory movement. |
| Medication Fulfillment | Financial Charge, Credit Note, or Refund requirement | Tata Rekening | Resolve Financial Responsibility and settlement consequences without changing fulfillment history. |

## 9. Business Timing and Service Limits

| Timing fact | Authoritative rule |
|---|---|
| Pharmacy `CreatedAt` | Recorded when Patient Tracker issues the Queue Number. |
| Pharmacy `ServedAt` | Recorded when the first applicable Dispense Order produces `Medication Preparation Started`. |
| Pharmacy `DoneAt` | Recorded when Pharmacy Staff performs the coordinated pickup call. |
| Prescription Review | May begin as soon as the Prescription is available; it does not wait for Patient arrival or mapping. |
| General Patient preparation | Cannot begin before Payment Clearance establishes Fulfillment Clearance. |
| BPJS preparation | Cannot begin before valid SEP, covered Fornas mapping, and Fulfillment Clearance. A Sales Invoice is not required. |
| Pickup call | Occurs only after every Dispense Order intended for that handover is `Prepared` or has an accountable exception outcome. |
| Final Dispense Review and education | Occur with the Patient or caregiver present after the pickup call and before Medication Handover. |
| BPJS Sales Invoice | Established only with successful Medication Handover. |
| Collection limit | No numerical value is currently authoritative. An authorized manual uncollected-medication resolution establishes `Collection Window Expired`. |

Technical timeouts, polling, retries, and application performance are outside this workflow.

## 10. Traceability

| Workflow ID | Domain rules | States | Domain Events | External authority |
|---|---|---|---|---|
| `WF-MF-RJ-001` | `BR-MF-061`–`BR-MF-065`, `BR-MF-082`, `BR-MF-084`–`BR-MF-087` | `Unmapped`, `Mapped`, `Waiting` | `Queue Entry Created`, `Outpatient Queue Mapped`, `Queue Entry Identified` | Patient Tracker |
| `WF-MF-RJ-002` | `BR-MF-001`–`BR-MF-019`, `BR-MF-050`, `BR-MF-061`, `BR-MF-086`, `BR-MF-089` | `Available`, `Under Review`, `Clarification Required`, `Approved`, `Partially Approved`, `Rejected`, `Established`, `Active` | `Prescription Review Started`, `Clinical Clarification Requested`, `Medication Substitution Authorized`, `Prescription Review Completed`, `Direct Medication Request Accepted`, `Pharmacy Sales Order Established`, `Dispense Order Established` | CPOE, Medication Catalog, Inventory |
| `WF-MF-RJ-003` | `BR-MF-020`–`BR-MF-028`, `BR-MF-040`–`BR-MF-046`, `BR-MF-067`–`BR-MF-072`, `BR-MF-076`–`BR-MF-083`, `BR-MF-088`, `BR-MF-095` | `Established`, `Issued`, `Financially Cleared`, `Released`, `Preparing`, `Prepared`, `Reviewed`, `Completed`, `In Service`, `Done` | `Sales Invoice Established`, `Payment Clearance Established`, `Medication Preparation Started`, `Medication Prepared`, `Patient Called for Pickup`, `Final Dispense Review Completed`, `Medication Handed Over` | Patient Tracker, Payment, Inventory, Tata Rekening |
| `WF-MF-RJ-004` | `BR-MF-066`, `BR-MF-073`–`BR-MF-079`, `BR-MF-081`–`BR-MF-083`, `BR-MF-088`, `BR-MF-090`, `BR-MF-095` | `Awaiting Clearance`, `Released`, `Preparing`, `Prepared`, `Reviewed`, `Completed`, `In Service`, `Done` | `Coverage Clearance Established`, `Medication Preparation Started`, `Patient Called for Pickup`, `Sales Invoice Established`, `Medication Handed Over` | Patient Tracker, SEP, Fornas, Inventory, Tata Rekening |
| `WF-MF-RJ-005` | `BR-MF-015`, `BR-MF-022`, `BR-MF-044`, `BR-MF-090`–`BR-MF-095` | Payer-specific Sales Invoice and shared Dispense Order states | `Billing Allocation Established`, `Coverage Clearance Established`, `Payment Clearance Established`, `Sales Invoice Established`, `Medication Handed Over` | SEP, Fornas, Payment, Tata Rekening |
| `WF-MF-RJ-006` | `BR-MF-084`–`BR-MF-088`, `BR-MF-095` | Per-demand authoritative states; one queue `Waiting` → `In Service` → `Done` | `Outpatient Queue Mapped`, `Medication Preparation Started`, `Patient Called for Pickup`, `Medication Handed Over` | Patient Tracker |
| `WF-MF-RJ-007` | `BR-MF-018`–`BR-MF-019`, `BR-MF-027`, `BR-MF-045`–`BR-MF-047`, `BR-MF-052`–`BR-MF-060`, `BR-MF-079`–`BR-MF-080` | `Expired`, `Active`, `Resolved` | `Outpatient No-Show Recorded`, `Dispense Order Expired`, `Unfulfilled Medication Recorded`, `Medication Returned`, `Sales Invoice Credited`, `Refund Required`, `Pharmacy Sales Order Resolved` | Inventory, Tata Rekening |

Related canonical artifacts:

- [Medication Fulfillment Domain](./medication-fulfillment-domain.md)
- [Medication Fulfillment Domain — Bahasa Indonesia](./medication-fulfillment-domain-id.md)
- [Outpatient Medication Fulfillment Workflow — Bahasa Indonesia](./outpatient-medication-fulfillment-workflow-id.md)
- [Patient Tracker Domain](../../contexts/pasien-tracker/TRACKER-DOMAIN.md)
- [CPOE Domain](../../contexts/cpoe/CPOE-DOMAIN.md)
- [Tata Rekening Domain](../../contexts/TataRekening/02-domain.md)

No outpatient pharmacy SOP or dedicated integration/architecture artifact is referenced by this workflow at this revision.
