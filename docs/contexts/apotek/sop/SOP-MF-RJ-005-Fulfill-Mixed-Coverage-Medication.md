# SOP MF-RJ-005 — Fulfill Mixed-Coverage Medication

**Artifact status:** Canonical target operational specification

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-005`

**Bahasa Indonesia companion:** [SOP MF-RJ-005 — Memenuhi Obat dengan Coverage Campuran](./SOP-MF-RJ-005-Fulfill-Mixed-Coverage-Medication-ID.md)

**Application terminology status:** `Apotek` and `Apotek Rajal` are established application terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for separating BPJS-covered and Patient-payable commercial responsibility while coordinating the cleared quantities for one outpatient pickup.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Caregiver | Human | Confirms or declines the Patient-payable portion, pays when confirmed, presents for pickup, receives education, and accepts medication when authorized. |
| Pharmacy Staff | Human | Separates Billing Allocations, communicates the Patient-payable amount, records its confirmed invoice, coordinates readiness, and performs the pickup call. |
| Pharmacy Supervisor | Human | Authorizes the manual uncollected-medication resolution when the Patient does not collect prepared medication. |
| Cashier or Payment Authority | Human or Subsystem | Receives payment and supplies Payment Clearance for the General Patient Sales Invoice. |
| Pharmacy Technician | Human | Prepares or compounds cleared medication under the Dispense Order. |
| Pharmacist | Human | Verifies the recipient, completes Final Dispense Review, and provides Patient Education. |
| SEP and Fornas Authorities | Subsystem | Supply SEP validity and item-level coverage. |
| Medication Fulfillment Application | Application | Maintains payer-specific allocations and clearances and records separate invoices with coordinated handover. |
| Patient Tracker | Subsystem | Records one `ServedAt` and one `DoneAt` for the common Queue Entry. |
| Inventory | Subsystem | Supplies reservation, issue, and return-disposition outcomes. |
| Tata Rekening | Subsystem | Receives and resolves payer-specific financial consequences. |

## 3. Preconditions

1. Participating staff are signed in with their required permissions.
2. One active Pharmacy Sales Order contains both BPJS-covered and Patient-payable quantities.
3. A valid SEP and authoritative Fornas mapping identify the covered and non-covered quantities.
4. Applicable Fulfillment Allocations and a Dispense Order are displayed.
5. No General Patient or BPJS Sales Invoice has been established for the proposed allocations unless the procedure is resuming after an accountable exception.

## 4. Operational Steps

1. **Pharmacy Staff** opens the mixed-coverage demand in `Apotek Rajal` and verifies the Pharmacy Sales Order quantities and payer classifications.
2. **Pharmacy Staff** records separate BPJS-covered and Patient-payable Billing Allocations without changing accepted medication identity or quantity.
3. **SEP and Fornas Authorities** supply valid SEP and item-level coverage; **Medication Fulfillment Application** displays Coverage Clearance for the covered quantities.
4. **Medication Fulfillment Application** calculates and displays the Patient-payable amount from the non-covered Billing Allocations.
5. **Pharmacy Staff** verbally communicates that amount before a General Patient Sales Invoice exists.
6. **Patient or Caregiver** verbally confirms the Patient-payable portion.
7. **Pharmacy Staff** records the confirmed Patient-payable transaction; **Medication Fulfillment Application** establishes a separate General Patient Sales Invoice from those allocations.
8. **Cashier or Payment Authority** receives payment and supplies Payment Clearance for the General Patient Sales Invoice.
9. **Medication Fulfillment Application** establishes Fulfillment Clearance for covered quantities from Coverage Clearance and for Patient-payable quantities from Payment Clearance.
10. **Inventory** secures the required Stock Reservation and supplies its outcome.
11. After every quantity intended for handover has applicable clearance, **Pharmacy Technician** starts and completes Medication Preparation.
12. **Medication Fulfillment Application** records the first `Medication Preparation Started`; **Patient Tracker** moves the common Queue Entry to In Service and records one `ServedAt`.
13. **Medication Fulfillment Application** displays every intended Dispense Order as `Prepared` or with an accountable exception outcome.
14. **Pharmacy Staff** performs one coordinated pickup call; **Patient Tracker** makes the common Queue Entry `Done` and records one `DoneAt`.
15. With the Patient or caregiver present, **Pharmacist** verifies the Authorized Recipient, completes Final Dispense Review, and records Patient Education.
16. **Pharmacy Staff** completes the physical handover after Pharmacist authorization.
17. **Medication Fulfillment Application** establishes the BPJS Sales Invoice only with successful handover, records Medication Dispense and Medication Handover for all applicable quantities, and preserves both payer-specific allocations.
18. **Inventory** supplies the Inventory Issue outcomes; **Medication Fulfillment Application** displays final Dispense Order and Pharmacy Sales Order progress.

## 5. Operational Exceptions

### 5.1 Patient declines the non-covered portion before invoice establishment

- **Pharmacy Staff** records the decline and establishes no General Patient Sales Invoice.
- **Medication Fulfillment Application** records the Patient-payable allocation as declined or commercially unallocated and permits the covered portion to continue independently.

### 5.2 An item has no authoritative Fornas coverage

- **Medication Fulfillment Application** displays the affected quantity as not covered.
- **Pharmacy Staff** reclassifies it only through accountable Patient-payable Billing Allocation, communicates the revised amount, and obtains a new verbal confirmation.

### 5.3 Not all intended quantities have clearance

- **Medication Fulfillment Application** blocks coordinated preparation and pickup for the uncleared quantities.
- **Pharmacy Staff** resolves the applicable coverage or payment path before continuing.

### 5.4 Established invoice, non-fulfillment, or No-Show requires correction

- **Medication Fulfillment Application** keeps covered and Patient-payable consequences separate.
- **Tata Rekening** supplies the required correction for the paid portion; the absent BPJS invoice remains absent until successful handover.
- **Pharmacy Supervisor** applies `SOP-MF-RJ-007` for uncollected medication.

## 6. Completion Criteria

1. Covered and Patient-payable Billing Allocations remain separately visible.
2. The financially cleared General Patient Sales Invoice and the handover-time BPJS Sales Invoice are separate and traceable to the same Pharmacy Sales Order.
3. One coordinated Medication Handover records every applicable quantity and Authorized Recipient.
4. The Pharmacy Sales Order is `Resolved`, or remains `Active` with an explicitly displayed payer-specific unresolved consequence.

## 7. References

- [Medication Fulfillment Domain](../medication-fulfillment-domain.md), especially `BR-MF-015`, `BR-MF-020`–`BR-MF-028`, `BR-MF-040`–`BR-MF-046`, `BR-MF-056`–`BR-MF-060`, `BR-MF-070`–`BR-MF-078`, and `BR-MF-090`–`BR-MF-095`.
- [Outpatient Medication Fulfillment Workflow](../outpatient-medication-fulfillment-workflow.md), `WF-MF-RJ-005`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md).
- [Tata Rekening Domain](../../../contexts/TataRekening/02-domain.md).
