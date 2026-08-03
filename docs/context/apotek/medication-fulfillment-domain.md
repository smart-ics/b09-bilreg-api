# Medication Fulfillment Domain

**Artifact status:** Canonical business specification

**Bounded context:** Medication Fulfillment (`Pelayanan Obat Pasien`)

**Version scope:** Target business model across outpatient, inpatient, emergency, and unit-dose settings

**Bahasa Indonesia companion:** [medication-fulfillment-domain-id.md](./medication-fulfillment-domain-id.md)

**Related business contexts:** [CPOE](../../contexts/cpoe/CPOE-DOMAIN.md), [Outpatient Pharmacy policy](./apotek-rajal-domain.md), [Tata Rekening](../../contexts/TataRekening/02-domain.md)

## 1. Business Overview

### 1.1 Purpose and value

Medication Fulfillment turns patient-specific medication demand accepted by Pharmacy into accountable commercial allocation and physical medication fulfillment. It separates the legacy `Trs.DU (DO-Bill)` combination of stock delivery and billing into independent `Sales Invoice` and `Dispense Order` lifecycles, coordinated by a `Pharmacy Sales Order`.

The business must ensure that:

- the clinician's original Prescription remains authoritative and traceable;
- only professionally accepted medication demand enters a Pharmacy Sales Order;
- Sales Invoices and Dispense Orders are allocated independently from Sales Order Lines;
- billing, payment or coverage, stock availability, preparation, and handover remain distinct business facts;
- partial billing and partial fulfillment remain quantitatively accountable; and
- every accepted quantity reaches an accountable fulfilled or unfulfilled outcome.

### 1.2 Scope

This context covers:

1. Prescription Review and Direct Medication Request acceptance;
2. Pharmacy Sales Order establishment and allocation;
3. Medication Sale and Sales Invoice formation;
4. Dispense Order establishment and physical dispensing;
5. commercial or coverage clearance for fulfillment;
6. Medication Dispense and Medication Handover; and
7. shortage, substitution, backorder, cancellation, return, and other non-fulfillment outcomes.

It applies across outpatient, inpatient, emergency, and Unit Dose Dispensing settings.

### 1.3 Business boundaries

Medication Fulfillment owns Prescription Review Outcome, Pharmacy Sales Order, Billing Allocation, Fulfillment Allocation, Medication Sale represented by Sales Invoice, Dispense Order, Medication Dispense, Medication Handover, and fulfillment resolution.

It relies on related contexts without taking over their authority:

- CPOE or another clinical-order authority owns the original Prescription and clinician intent;
- Medication Catalog or formulary authority owns medication identity and formulary policy;
- Inventory owns authoritative stock balances and stock movements;
- Payment owns receipts and settlement evidence;
- Tata Rekening owns registration-level Financial Responsibility, payer allocation, finalization, and settlement initiation;
- Patient Tracker owns outpatient queue identity and lifecycle; and
- the clinical care context owns Medication Administration.

Purchasing, supplier management, replenishment, warehouse transfer, enterprise accounting, and Medication Administration are outside this context.

**Known alignment requirement:** the existing Outpatient Pharmacy specification predates this cross-setting separation and directly derives `Sale` from `Order Dispensing`. That earlier relationship does not govern the shared target model and must be aligned before related implementation changes are considered complete. Outpatient-specific queue, pickup, and no-show policies remain owned by the Outpatient Pharmacy specification.

### 1.4 Central business separation

The target flow is:

```text
Prescription or Direct Medication Request
  -> Prescription Review or direct acceptance
  -> Pharmacy Sales Order
       -> zero or more Sales Invoices
       -> zero or more Dispense Orders
            -> Medication Dispense
            -> Medication Handover
```

A Prescription does not become a Pharmacy Sales Order. A completed professional decision authorizes a new Pharmacy Sales Order while preserving the Prescription as its clinical source.

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Medication Fulfillment | The bounded context that coordinates patient-specific medication demand from Pharmacy acceptance through commercial allocation and physical fulfillment resolution. |
| Patient Medication Demand | A patient-specific need for medication originating from a Prescription or Direct Medication Request. |
| Prescription | The clinician's authoritative intent for medication to be supplied or administered to a Patient. |
| Electronic Prescription | A Prescription created and transmitted through an electronic clinical-order authority. |
| Physical Prescription | A non-electronic Prescription that must be recorded before Pharmacy can review it. |
| Direct Medication Request | An authorized request for medication without a Prescription. |
| Prescription Line | One requested medication, dosage instruction, and quantity within a Prescription. |
| Source Traceability | The accountable relationship from Medication Sale and dispensing outcomes back to their Sales Order, accepted demand, and original source. |
| Prescription Review | The Pharmacist's administrative, pharmaceutical, and clinical assessment of a Prescription. |
| Prescription Review Outcome | The professional disposition of a Prescription or Prescription Line as approved, partially approved, rejected, or requiring clarification. |
| Clinical Clarification | An accountable request to resolve an ambiguity or concern with the responsible clinician before final acceptance. |
| Accepted Medication Line | A medication line professionally accepted for inclusion in a Pharmacy Sales Order, independently of current stock availability. |
| Pharmacy Sales Order | The accepted medication demand owned by Pharmacy and used as the common source of commercial and fulfillment allocations. |
| Sales Order Line | One accepted medication, quantity, instructions, and applicable commercial basis within a Pharmacy Sales Order. |
| Accepted Quantity | The maximum quantity of a Sales Order Line available for accountable allocation and resolution. |
| Order Allocation | The accountable assignment of all or part of a Sales Order Line to a commercial or fulfillment purpose. |
| Billing Allocation | The assignment of quantity or value from a Sales Order Line to a Medication Sale represented by a Sales Invoice. |
| Fulfillment Allocation | The assignment of quantity from a Sales Order Line to a Dispense Order. |
| Medication Sale | The commercial transaction formed from one or more Billing Allocations of one Pharmacy Sales Order. |
| Sales Invoice | The authoritative commercial document and Aggregate Root representing one Medication Sale. |
| Billing Line | One medication or applicable service, quantity, price, discount, and value within a Sales Invoice. |
| Pricing Snapshot | The immutable commercial basis used when a Billing Allocation and Sales Invoice are established. |
| Payer | The Patient, BPJS, insurer, company, or other party expected to bear a medication charge. |
| Financial Charge | The financial consequence supplied to Tata Rekening from a Medication Sale. |
| Purchase Confirmation | A General Patient's decision to proceed after the applicable commercial value is communicated. |
| Payment Clearance | Evidence that the required payment condition has been satisfied. |
| Coverage Clearance | Evidence that the applicable payer authorizes fulfillment without immediate Patient payment. |
| Fulfillment Clearance | The business authorization allowing a Dispense Order to proceed under the applicable payment or coverage policy. |
| Financial Adjustment | An accountable correction to a Medication Sale or its financial consequences. |
| Credit Note | A commercial document reducing or reversing an issued Sales Invoice amount. |
| Refund | The accountable return of previously settled funds. |
| Dispense Order | The authoritative instruction to physically fulfill one or more Fulfillment Allocations from one Pharmacy Sales Order. |
| Dispense Order Line | One medication and allocated quantity to be physically fulfilled within a Dispense Order. |
| Dispense Cycle | A defined fulfillment period or batch, especially for inpatient and Unit Dose Dispensing. |
| Unit Dose Dispensing | Fulfillment in patient-specific unit doses or defined administration periods. |
| Stock Availability | Inventory's representation of quantity currently available to support fulfillment. |
| Stock Allocation | Inventory's association of stock with a fulfillment need before final issue. |
| Stock Reservation | Stock secured for a Dispense Order so it is not promised to another demand. |
| Inventory Issue | Inventory's authoritative recognition that medication left an inventory location. |
| Medication Preparation | Picking, counting, labelling, packaging, and otherwise preparing medication for fulfillment. |
| Compounding | Preparing a medication product from ingredients or components for a specific fulfillment need. |
| Final Dispense Review | The final professional check that prepared medication corresponds to the Dispense Order and may proceed to handover. |
| Prepared Medication | Medication whose physical preparation is complete and awaits final review or handover. |
| In-Transit Medication | Prepared medication removed from general availability but not yet handed to its authorized recipient. |
| Medication Dispense | The accountable fact that a quantity of medication was actually supplied for a Patient. |
| Medication Handover | The accountable transfer of medication to an Authorized Recipient. |
| Authorized Recipient | A verified Patient, caregiver, practitioner, ward, or other party permitted to receive medication for the Patient. |
| Patient Education | The accountable explanation of medication use, storage, precautions, and other relevant information to the Patient or caregiver. |
| Fulfilled Quantity | The quantity of a Sales Order Line that reached a successful Medication Dispense outcome. |
| Partial Fulfillment | Fulfillment of less than the total Accepted Quantity while another quantity remains unresolved or receives a different outcome. |
| Fulfillment Completion | The condition in which every Accepted Quantity has an accountable final outcome. |
| Medication Administration | The clinical fact that medication was actually given to or consumed by the Patient; it is externally owned. |
| Medication Shortage | Insufficient stock to fulfill an allocated medication quantity. |
| Stock Discrepancy | A difference between recorded and physical stock that affects fulfillment. |
| Backorder | An unresolved quantity retained for later fulfillment when supply becomes available. |
| Medication Substitution | The accountable replacement of a requested medication product under applicable professional authority. |
| Unfulfilled Medication Outcome | A final, accountable reason that an accepted medication quantity was not fulfilled. |
| Copy Prescription | An accountable record of prescribed medication or quantity not fulfilled, when applicable. |
| Dispense Cancellation | The accountable ending of a Dispense Order before successful fulfillment. |
| Fulfillment Expiry | The ending of a fulfillment opportunity because its permitted service period elapsed. |
| Medication Return | The accountable return of medication previously prepared, transferred, or handed over. |
| Return to Stock | Inventory's authoritative acceptance of eligible returned medication into available stock. |
| No-Show | An outpatient outcome in which the Patient does not collect medication within the applicable service limit. |
| Care Setting | The operational setting whose policy applies to fulfillment, such as outpatient, inpatient, or emergency care. |
| Outpatient Fulfillment | Medication Fulfillment under outpatient arrival, queue, payment, pickup, and no-show policies. |
| Inpatient Fulfillment | Medication Fulfillment under inpatient ward, scheduled supply, and return policies. |
| Emergency Fulfillment | Medication Fulfillment under the applicable urgent-care policy. |
| Dose Window | A defined administration period used to plan a Dispense Cycle without representing Medication Administration itself. |
| Ward Delivery | Medication Handover from Pharmacy to an Authorized Recipient in an inpatient ward. |

## 3. Business Capabilities

### 3.1 Medication Demand Acceptance

Accept reviewed Prescription demand or an authorized Direct Medication Request without changing the original clinical intent.

### 3.2 Prescription Review

Establish the professional disposition of each Prescription and its lines, including clarification and partial acceptance.

### 3.3 Pharmacy Sales Order Management

Establish and maintain the accepted medication demand, its quantities, source traceability, and final resolution.

### 3.4 Commercial Allocation

Allocate Sales Order Lines into one or more Medication Sales and Sales Invoices without depending on Dispense Order count or timing.

### 3.5 Fulfillment Allocation

Allocate Sales Order Lines into one or more Dispense Orders based on care setting, quantity, location, cycle, and fulfillment policy.

### 3.6 Commercial and Coverage Clearance

Determine when a Dispense Order may proceed from Payment Clearance, Coverage Clearance, or another approved payer policy.

### 3.7 Physical Dispensing

Coordinate stock reservation, Medication Preparation, Compounding, Final Dispense Review, Medication Dispense, and Medication Handover.

### 3.8 Partial and Unit-Dose Fulfillment

Support independently counted billing and fulfillment tranches, including Unit Dose Dispensing and Dose Windows.

### 3.9 Exception and Return Resolution

Resolve shortages, discrepancies, substitutions, backorders, cancellations, expiries, returns, no-shows, and required financial consequences.

### 3.10 Cross-Setting Traceability

Preserve quantitative and source traceability across outpatient, inpatient, emergency, billing, dispensing, and handover outcomes.

## 4. Actors & Roles

### 4.1 Prescribing Clinician

Owns the original Prescription and responds to Clinical Clarification. The Prescribing Clinician does not own Pharmacy acceptance, Sales Invoice formation, or dispensing execution.

### 4.2 Pharmacist

Owns Prescription Review Outcome, professionally authorized Medication Substitution, Final Dispense Review, and applicable Patient Education.

### 4.3 Pharmacy Staff

Coordinates accepted demand, Sales Order allocation, outpatient administrative interaction, preparation workflow, and accountable handover within assigned authority.

### 4.4 Pharmacy Technician

Performs Medication Preparation and Compounding according to the Dispense Order and resolves preparation discrepancies under professional supervision.

### 4.5 Patient or Caregiver

Provides applicable confirmation and payment, receives education, and accepts medication when acting as an Authorized Recipient.

### 4.6 Cashier

Receives payment and supplies Payment Clearance evidence. The Cashier does not establish medication eligibility or fulfillment quantity.

### 4.7 Inpatient Authorized Recipient

Receives Ward Delivery for the Patient and remains identifiable in the Medication Handover outcome. Receipt does not represent Medication Administration.

### 4.8 Pharmacy Supervisor

Owns exceptional decisions beyond ordinary authority, including approved expiry, return, shortage resolution, and accountable correction.

## 5. Domain Objects

### 5.1 Prescription Review

Represents Pharmacy's professional assessment of one Prescription. It retains per-line decisions, clarification, responsible Pharmacist, and source traceability without rewriting the Prescription.

### 5.2 Pharmacy Sales Order

Represents one accepted patient-specific medication demand. It owns Sales Order Lines, accepted quantities, Billing Allocations, Fulfillment Allocations, and resolution progress.

### 5.3 Billing Allocation

Represents the portion of a Sales Order Line assigned to one Medication Sale and Sales Invoice. It retains the applicable quantity or value basis and Pricing Snapshot reference.

### 5.4 Fulfillment Allocation

Represents the portion of a Sales Order Line assigned to one Dispense Order. It retains quantity, care setting, and applicable Dispense Cycle.

### 5.5 Sales Invoice

Represents one Medication Sale from one Pharmacy Sales Order. It owns Billing Lines, payer classification, commercial value, financial disposition, adjustments, and Financial Charge outcome.

### 5.6 Dispense Order

Represents one physical fulfillment instruction from one Pharmacy Sales Order. It owns Dispense Order Lines, preparation and review progress, Medication Dispense outcomes, and final fulfillment disposition.

### 5.7 Fulfillment Clearance

Relates one Dispense Order's permitted quantity to Payment Clearance, Coverage Clearance, or another approved commercial evidence. It may reference multiple Sales Invoices when required by allocation policy.

### 5.8 Medication Dispense

Represents the actual medication and quantity supplied for a Patient, including responsible party, effective time, and source Dispense Order.

### 5.9 Medication Handover

Represents transfer to an Authorized Recipient, including recipient verification, handover time, destination when applicable, and Patient Education responsibility.

### 5.10 Unfulfilled Medication Outcome

Represents the final reason an accepted quantity was not fulfilled and identifies any Copy Prescription, backorder closure, return, or financial correction required.

## 6. Aggregates

### 6.1 Prescription Review Aggregate

**Aggregate Root:** `Prescription Review`

The aggregate keeps the Prescription source reference, per-line professional disposition, Clinical Clarification, responsible Pharmacist, and completion outcome mutually consistent. It cannot modify the clinician's original Prescription.

### 6.2 Pharmacy Sales Order Aggregate

**Aggregate Root:** `Pharmacy Sales Order`

The aggregate owns Sales Order Lines, Billing Allocations, Fulfillment Allocations, accepted quantities, fulfilled quantities, unfulfilled outcomes, and overall resolution.

It ensures that commercial and fulfillment allocations remain traceable and do not exceed their applicable Sales Order Line authority. It does not own Sales Invoice payment settlement, inventory balances, or physical dispensing execution.

### 6.3 Medication Sale Aggregate

**Aggregate Root:** `Sales Invoice`

The aggregate represents one Medication Sale. It keeps Billing Lines, Pricing Snapshot, Payer, Purchase Confirmation when applicable, financial disposition, Credit Notes, refunds, and Financial Charge outcome mutually consistent.

A Sales Invoice references exactly one Pharmacy Sales Order but may cover one or more of its Sales Order Lines.

### 6.4 Dispense Order Aggregate

**Aggregate Root:** `Dispense Order`

The aggregate keeps Fulfillment Allocations, physical preparation, Final Dispense Review, Medication Dispense, Medication Handover, cancellation, expiry, return, and non-fulfillment outcomes mutually consistent.

A Dispense Order references exactly one Pharmacy Sales Order but may fulfill one or more of its Sales Order Lines.

### 6.5 Cross-aggregate relationship

A Pharmacy Sales Order may have zero or more Sales Invoices and zero or more Dispense Orders. Sales Invoices and Dispense Orders are not required to have equal counts or formation times.

Their business correlation is expressed through Billing Allocations, Fulfillment Allocations, and Fulfillment Clearance at Sales Order Line and quantity level. Sharing a Pharmacy Sales Order does not by itself establish that every Sales Invoice clears every Dispense Order.

## 7. Business Rules

### 7.1 Sources and professional acceptance

- **BR-MF-001** — Patient Medication Demand shall originate from exactly one Prescription or Direct Medication Request.
- **BR-MF-002** — The original Prescription and clinician intent shall remain owned by its clinical-order authority.
- **BR-MF-003** — Every Prescription shall complete Prescription Review before any of its lines enter a Pharmacy Sales Order.
- **BR-MF-004** — Only a Pharmacist shall establish Prescription Review Outcome and professionally authorized Medication Substitution.
- **BR-MF-005** — A Prescription Review Outcome shall record a disposition for every reviewed Prescription Line.
- **BR-MF-006** — A rejected Prescription shall not establish a Pharmacy Sales Order.
- **BR-MF-007** — A partially approved Prescription may establish a Pharmacy Sales Order containing only Accepted Medication Lines.
- **BR-MF-008** — Clinical acceptance shall be independent of current Stock Availability; stock facts shall not rewrite professional eligibility.
- **BR-MF-009** — A Direct Medication Request shall follow its applicable professional and organizational acceptance policy without creating a Prescription.

### 7.2 Pharmacy Sales Order

- **BR-MF-010** — A Pharmacy Sales Order shall originate from exactly one completed accepted-demand source.
- **BR-MF-011** — One Prescription shall establish at most one active Pharmacy Sales Order within one fulfillment episode.
- **BR-MF-012** — A Pharmacy Sales Order shall contain at least one Sales Order Line with a positive Accepted Quantity.
- **BR-MF-013** — Every Sales Order Line shall retain Source Traceability to its Prescription Line or Direct Medication Request line.
- **BR-MF-014** — A Pharmacy Sales Order shall not be a Sales Invoice, payment record, Stock Reservation, Dispense Order, or Medication Dispense evidence.
- **BR-MF-015** — Billing Allocation and Fulfillment Allocation may occur independently and at different business times.
- **BR-MF-016** — The active Fulfillment Allocations of a Sales Order Line shall not exceed its unresolved Accepted Quantity.
- **BR-MF-017** — Fulfilled Quantity shall not exceed the quantity allocated for fulfillment.
- **BR-MF-018** — Every Accepted Quantity shall eventually be fulfilled, cancelled, expired, backordered, or assigned another accountable Unfulfilled Medication Outcome.
- **BR-MF-019** — A Pharmacy Sales Order shall reach Fulfillment Completion only when every Accepted Quantity has a final accountable outcome.

### 7.3 Medication Sale and Sales Invoice

- **BR-MF-020** — Every Medication Sale shall be represented by exactly one Sales Invoice.
- **BR-MF-021** — Every Sales Invoice shall derive from Billing Allocations of exactly one Pharmacy Sales Order.
- **BR-MF-022** — A Pharmacy Sales Order may produce zero, one, or multiple Sales Invoices.
- **BR-MF-023** — A Sales Invoice may cover one or more Sales Order Lines and shall preserve each source allocation.
- **BR-MF-024** — A Billing Line shall not introduce a medication line absent from its source Pharmacy Sales Order, except an explicitly authorized non-medication commercial component.
- **BR-MF-025** — A Sales Invoice shall retain the Pricing Snapshot and Payer applicable when it is established.
- **BR-MF-026** — Sales Invoice formation shall not prove that stock is available, reserved, prepared, dispensed, or handed over.
- **BR-MF-027** — An issued or financially settled Sales Invoice shall be corrected through an accountable Financial Adjustment, Credit Note, or Refund outcome rather than silent replacement.
- **BR-MF-028** — Every Financial Charge sent to Tata Rekening shall retain Source Traceability to its Sales Invoice and Pharmacy Sales Order.

### 7.4 Dispense Order and dispensing

- **BR-MF-029** — Every Dispense Order shall derive from Fulfillment Allocations of exactly one Pharmacy Sales Order.
- **BR-MF-030** — A Pharmacy Sales Order may produce zero, one, or multiple Dispense Orders.
- **BR-MF-031** — A Dispense Order may cover one or more Sales Order Lines and shall preserve each source allocation.
- **BR-MF-032** — Dispense Order count, quantity split, and timing may differ from Sales Invoice count, value split, and timing.
- **BR-MF-033** — Stock Reservation and Inventory Issue shall remain authoritative Inventory outcomes requested for a Dispense Order.
- **BR-MF-034** — Medication Preparation and Compounding shall use an active Dispense Order as their authority.
- **BR-MF-035** — Prepared Medication shall complete Final Dispense Review before Medication Handover.
- **BR-MF-036** — A Medication Dispense shall not exceed the unresolved quantity of its Dispense Order Line.
- **BR-MF-037** — Medication Handover shall identify an Authorized Recipient and its effective business time.
- **BR-MF-038** — Ward Delivery shall not be treated as Medication Administration.
- **BR-MF-039** — Medication Administration shall not be inferred from Sales Invoice, Inventory Issue, Medication Dispense, or Ward Delivery.

### 7.5 Clearance and cross-aggregate coordination

- **BR-MF-040** — A Dispense Order shall proceed only when the Fulfillment Clearance required by its care-setting and payer policy is present.
- **BR-MF-041** — Payment Clearance shall come from the responsible payment authority and shall not be inferred solely from Sales Invoice existence.
- **BR-MF-042** — Coverage Clearance shall identify the applicable Payer and covered fulfillment authority.
- **BR-MF-043** — Fulfillment Clearance shall identify the Dispense Order quantity it authorizes and its supporting commercial evidence.
- **BR-MF-044** — One Sales Invoice may support clearance for multiple Dispense Orders, and one Dispense Order may rely on multiple commercial allocations when policy requires.
- **BR-MF-045** — A paid or financially cleared Sales Invoice shall not guarantee successful fulfillment when shortage, discrepancy, expiry, or another valid exception occurs.
- **BR-MF-046** — A financial clearance followed by non-fulfillment shall produce an accountable backorder, substitution, Credit Note, Refund, or other approved resolution.

### 7.6 Partial fulfillment, UDD, and exceptions

- **BR-MF-047** — Partial Fulfillment shall preserve the fulfilled, unresolved, and unfulfilled quantities separately.
- **BR-MF-048** — Unit Dose Dispensing may divide one Sales Order Line into multiple Dispense Cycles and Dispense Orders.
- **BR-MF-049** — A Dose Window shall guide fulfillment planning and shall not assert Medication Administration.
- **BR-MF-050** — Medication Substitution shall preserve the requested medication, supplied medication, responsible authority, reason, and affected quantity.
- **BR-MF-051** — A Medication Shortage or Stock Discrepancy shall not alter the original Prescription or erase an existing Sales Invoice.
- **BR-MF-052** — A Medication Return shall identify its source Dispense Order, quantity, reason, and final Inventory disposition.
- **BR-MF-053** — Return to Stock shall occur only when Inventory accepts the returned medication under its own policy.
- **BR-MF-054** — A Copy Prescription shall identify the prescribed medication or quantity that remained unfulfilled.
- **BR-MF-055** — A No-Show shall be an outpatient policy outcome and shall not be imposed on inpatient Ward Delivery.

### 7.7 Completion and history

- **BR-MF-056** — Pharmacy Sales Order commercial progress and fulfillment progress shall be tracked independently.
- **BR-MF-057** — Commercial resolution shall not by itself complete physical fulfillment, and physical fulfillment shall not by itself prove financial resolution.
- **BR-MF-058** — Material review, allocation, invoice, clearance, dispensing, handover, exception, and correction decisions shall retain responsible party and effective business time.
- **BR-MF-059** — Source Traceability shall be preserved from Prescription or Direct Medication Request through Pharmacy Sales Order, Sales Invoice, Dispense Order, and final outcomes.
- **BR-MF-060** — A completed or cancelled business outcome shall not be erased; a later correction shall add an accountable correcting fact.

## 8. State Machines & Lifecycles

### 8.1 Prescription Review lifecycle

```text
Available
  -> Under Review
       -> Clarification Required
            -> Under Review
       -> Approved
       -> Partially Approved
       -> Rejected
```

`Approved`, `Partially Approved`, and `Rejected` are final review outcomes. A corrected or replaced Prescription requires a separately traceable review decision.

### 8.2 Pharmacy Sales Order lifecycle

```text
Established
  -> Active
       -> Resolved
       -> Cancelled
```

| State | Business meaning |
|---|---|
| Established | Accepted demand exists and allocation may begin. |
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

Established, Awaiting Clearance, Released, Preparing, Prepared, or Reviewed
  -> Cancelled | Expired | Unfulfilled
```

`Completed` requires accountable Medication Dispense and the applicable Medication Handover. `Unfulfilled` requires a reason and resolution of allocated stock and financial consequences.

### 8.5 Medication fulfillment quantity lifecycle

```text
Accepted Quantity
  -> Billing Allocated or Commercially Unallocated
  -> Fulfillment Allocated or Fulfillment Unallocated
  -> Fulfilled | Backordered | Cancelled | Expired | Other Unfulfilled Outcome
```

Billing and fulfillment branches progress independently. Final Sales Order resolution requires reconciliation of both branches, not identical document counts.

## 9. Domain Events

| Domain Event | Business meaning |
|---|---|
| Prescription Review Started | A Pharmacist began professional assessment of a Prescription. |
| Clinical Clarification Requested | A concern requires clarification before review completion. |
| Prescription Review Completed | Every reviewed line received a final professional disposition. |
| Direct Medication Request Accepted | A permitted non-prescription demand was accepted by Pharmacy. |
| Pharmacy Sales Order Established | Accepted medication demand became available for commercial and fulfillment allocation. |
| Billing Allocation Established | A Sales Order quantity or value was allocated to a Medication Sale. |
| Fulfillment Allocation Established | A Sales Order quantity was allocated to a Dispense Order. |
| Sales Invoice Established | A Medication Sale was formed from Billing Allocations. |
| Sales Invoice Issued | The Sales Invoice became an authoritative commercial document. |
| Payment Clearance Established | The responsible payment authority confirmed the applicable payment condition. |
| Coverage Clearance Established | The applicable Payer authorized covered fulfillment. |
| Fulfillment Clearance Established | A Dispense Order quantity was authorized to proceed. |
| Dispense Order Established | A physical fulfillment instruction was formed from Fulfillment Allocations. |
| Stock Reserved | Inventory secured stock for a Dispense Order. |
| Medication Preparation Started | Physical preparation began under a released Dispense Order. |
| Medication Prepared | The allocated medication completed physical preparation. |
| Final Dispense Review Completed | Prepared Medication passed the required final professional check. |
| Medication Dispensed | An accountable medication quantity was supplied for the Patient. |
| Medication Handed Over | Medication was transferred to an Authorized Recipient. |
| Medication Shortage Identified | Available stock could not support the intended fulfillment quantity. |
| Medication Substitution Authorized | An accountable authority approved replacement of the requested medication. |
| Dispense Order Backordered | An unresolved quantity was retained for later fulfillment. |
| Dispense Order Cancelled | An authorized decision ended the Dispense Order before completion. |
| Dispense Order Expired | The permitted fulfillment period ended without completion. |
| Unfulfilled Medication Recorded | An accepted quantity received a final non-fulfillment outcome. |
| Medication Returned | Medication previously prepared or supplied was returned. |
| Sales Invoice Credited | A Credit Note reduced or reversed a Sales Invoice consequence. |
| Refund Required | A financial resolution requires return of settled funds. |
| Pharmacy Sales Order Resolved | Every accepted quantity and required commercial consequence received an accountable final outcome. |

## 10. Business Workflows

### 10.1 Accept a Prescription and establish a Pharmacy Sales Order

```text
Prescription becomes available
  -> Pharmacist performs Prescription Review
  -> every Prescription Line receives a disposition
  -> Accepted Medication Lines are identified
  -> Pharmacy Sales Order is established
  -> Source Traceability to the original Prescription is preserved
```

If no line is accepted, no Pharmacy Sales Order is established.

### 10.2 Accept a Direct Medication Request

```text
Direct Medication Request is received
  -> applicable acceptance policy is completed
  -> accepted medication lines are identified
  -> Pharmacy Sales Order is established
```

No Prescription is created merely to support a direct request.

### 10.3 Allocate one Sales Order into multiple Sales Invoices

```text
Active Pharmacy Sales Order
  -> Billing Allocations divide applicable Sales Order Lines
  -> one or more Medication Sales are established
  -> each Medication Sale is represented by one Sales Invoice
  -> every Billing Line retains its Sales Order source
```

The split may follow payer, commercial period, covered quantity, or another approved business basis.

### 10.4 Allocate one Sales Order into multiple Dispense Orders

```text
Active Pharmacy Sales Order
  -> Fulfillment Allocations divide applicable Sales Order Lines
  -> one or more Dispense Orders are established
  -> each Dispense Order follows its care-setting and Dispense Cycle policy
```

Dispense Order splits are independent of Sales Invoice splits.

### 10.5 Clear and fulfill medication

```text
Sales Invoice or approved coverage basis exists
  -> Payment Clearance or Coverage Clearance is established
  -> Fulfillment Clearance authorizes an applicable Dispense Order quantity
  -> stock is reserved
  -> medication is prepared
  -> Final Dispense Review completes
  -> Medication Dispense is recorded
  -> Medication Handover completes
```

The exact ordering of invoice formation, stock reservation, and preparation follows payer and care-setting policy, but required clearance must exist before the policy's controlled fulfillment point.

### 10.6 Unit Dose Dispensing

```text
Active inpatient Pharmacy Sales Order
  -> accepted quantity is planned across Dose Windows
  -> each Dispense Cycle receives a Fulfillment Allocation
  -> one or more Dispense Orders are established
  -> medication is prepared and reviewed per cycle
  -> Ward Delivery records Medication Handover
  -> future cycles remain independently changeable while unfulfilled
```

Ward Delivery does not record Medication Administration.

### 10.7 Resolve paid medication that cannot be fulfilled

```text
Sales Invoice is financially cleared
  -> Dispense Order encounters shortage or discrepancy
  -> responsible parties assess available resolution
       -> later fulfillment through Backorder
       -> authorized Medication Substitution
       -> fulfillment from another approved stock source
       -> cancellation with Credit Note or Refund
       -> other accountable Unfulfilled Medication Outcome
  -> Sales Order quantities and commercial consequences are reconciled
```

The Prescription and prior financial facts remain traceable.

### 10.8 Resolve outpatient No-Show

```text
Prepared Medication is ready for outpatient handover
  -> Patient does not collect within the applicable service limit
  -> No-Show is established
  -> Dispense Order follows expiry or cancellation policy
  -> medication is returned when eligible
  -> paid, covered, or uninvoiced commercial consequences are resolved
```

### 10.9 Complete a Pharmacy Sales Order

```text
All Billing Allocations reach a final commercial disposition
  + all Accepted Quantities reach fulfilled or accountable unfulfilled outcomes
  -> Pharmacy Sales Order becomes Resolved
```

Resolution does not require Sales Invoice and Dispense Order counts to match.
