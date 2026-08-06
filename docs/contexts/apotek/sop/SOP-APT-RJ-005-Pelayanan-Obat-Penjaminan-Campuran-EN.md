# SOP APT-RJ-005 — Fulfill Mixed-Coverage Medication

**Artifact status:** Canonical target operational specification

**Bounded context:** Apotek

**Workflow:** `WF-APT-RJ-005`

**Bahasa Indonesia companion:** [SOP APT-RJ-005 — Memenuhi Obat dengan Coverage Campuran](./SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-ID.md)

**Subsystem terminology status:** `Pharmacy System` and `Outpatient Pharmacy` are established subsystem terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for separating BPJS-covered and Patient-payable commercial responsibility while coordinating the cleared quantities for one outpatient pickup.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Caregiver | Human | Confirms or declines the Patient-payable portion, pays when confirmed, presents for pickup, receives education, and accepts medication when authorized. |
| Pharmacy Staff | Human | Separates Billing Allocations, communicates the Patient-payable amount, records its confirmed invoice, prepares or compounds cleared medication, coordinates readiness, and performs the pickup call. |
| Pharmacy Supervisor | Human | Authorizes the manual uncollected-medication resolution when the Patient does not collect prepared medication. |
| Cashier or Payment Authority | Human or Subsystem | Receives payment and supplies Payment Clearance for the General Patient Sales Invoice. |
| Pharmacist | Human | Verifies the recipient, completes Final Dispense Review, and provides Patient Education. |
| SEP and Fornas Authorities | Subsystem | Supply SEP validity and item-level coverage. |
| Pharmacy System | Subsystem | Maintains payer-specific allocations and clearances and records separate invoices with coordinated handover. |
| Patient Tracker | Subsystem | Records one `ServedAt` and one `DoneAt` for the common Queue Entry. |
| Inventory | Subsystem | Supplies reservation, issue, and return-disposition outcomes. |
| Tata Rekening | Subsystem | Receives and resolves payer-specific financial consequences. |

## 3. Preconditions

1. Participating staff are signed in with their required permissions.
2. One active Sales Order contains both BPJS-covered and Patient-payable quantities.
3. A valid SEP and authoritative Fornas mapping identify the covered and non-covered quantities.
4. Applicable Fulfillment Allocations and a Dispense Order are displayed.
5. No General Patient or BPJS Sales Invoice has been established for the proposed allocations unless the procedure is resuming after an accountable exception.

## 4. Operational Steps

1. **Pharmacy Staff** opens the mixed-coverage demand in `Apotek Rajal` and verifies the Sales Order quantities and payer classifications.
2. **Pharmacy Staff** records separate BPJS-covered and Patient-payable Billing Allocations without changing accepted medication identity or quantity.
3. **SEP and Fornas Authorities** supply valid SEP and item-level coverage; **Pharmacy System** displays Coverage Clearance for the covered quantities.
4. **Pharmacy System** calculates and displays the Patient-payable amount from the non-covered Billing Allocations.
5. **Pharmacy Staff** verbally communicates that amount before a General Patient Sales Invoice exists.
6. **Patient or Caregiver** verbally confirms the Patient-payable portion.
7. **Pharmacy Staff** records the confirmed Patient-payable transaction; **Pharmacy System** establishes a separate General Patient Sales Invoice from those allocations.
8. **Cashier or Payment Authority** receives payment and supplies Payment Clearance for the General Patient Sales Invoice.
9. **Pharmacy System** establishes Fulfillment Clearance for covered quantities from Coverage Clearance and for Patient-payable quantities from Payment Clearance.
10. **Inventory** secures the required Stock Reservation and supplies its outcome.
11. After every quantity intended for handover has applicable clearance, **Pharmacy Staff** starts and completes Medication Preparation.
12. **Pharmacy System** records the first `Medication Preparation Started`; **Patient Tracker** moves the common Queue Entry to In Service and records one `ServedAt`.
13. **Pharmacy System** displays every intended Dispense Order as `Prepared` or with an accountable exception outcome.
14. **Pharmacy Staff** performs one coordinated pickup call; **Patient Tracker** makes the common Queue Entry `Done` and records one `DoneAt`.
15. With the Patient or caregiver present, **Pharmacist** verifies the Authorized Recipient, completes Final Dispense Review, and records Patient Education. When the review passes, **Pharmacy System** appends the review record and displays the Dispense Order as `Reviewed`.
16. **Pharmacy Staff** completes the physical handover after Pharmacist authorization.
17. **Pharmacy System** establishes the BPJS Sales Invoice only with successful handover, records Medication Dispense and Medication Handover for all applicable quantities, and preserves both payer-specific allocations.
18. **Inventory** supplies the Inventory Issue outcomes; **Pharmacy System** displays final Dispense Order and Sales Order progress.

## 5. Operational Exceptions

### 5.1 Patient declines the non-covered portion before invoice establishment

- **Pharmacy Staff** records the decline and establishes no General Patient Sales Invoice.
- **Pharmacy System** records the Patient-payable allocation as declined or commercially unallocated and permits the covered portion to continue independently.

### 5.2 An item has no authoritative Fornas coverage

- **Pharmacy System** displays the affected quantity as not covered.
- **Pharmacy Staff** reclassifies it only through accountable Patient-payable Billing Allocation, communicates the revised amount, and obtains a new verbal confirmation.

### 5.3 Not all intended quantities have clearance

- **Pharmacy System** blocks coordinated preparation and pickup for the uncleared quantities.
- **Pharmacy Staff** resolves the applicable coverage or payment path before continuing.

### 5.4 Final Dispense Review fails

- **Pharmacist** records the failure reason and affected quantity and does not authorize handover.
- **Pharmacy System** appends an immutable review record with the Pharmacist and effective business time, returns the affected Dispense Order from `Prepared` to `Preparing`, blocks coordinated handover, and keeps the BPJS Sales Invoice absent.
- **Pharmacy Staff** corrects and prepares the affected medication again; **Pharmacy System** returns the Dispense Order to `Prepared`, and **Pharmacist** performs a new Final Dispense Review. Previous review records remain visible and unchanged.

### 5.5 Established invoice, non-fulfillment, or No-Show requires correction

- **Pharmacy System** keeps covered and Patient-payable consequences separate.
- **Tata Rekening** supplies the required correction for the paid portion; the absent BPJS invoice remains absent until successful handover.
- **Pharmacy Supervisor** applies `SOP-APT-RJ-007` for uncollected medication.

## 6. Completion Criteria

1. Covered and Patient-payable Billing Allocations remain separately visible.
2. The financially cleared General Patient Sales Invoice and the handover-time BPJS Sales Invoice are separate and traceable to the same Sales Order.
3. One coordinated Medication Handover records every applicable quantity and Authorized Recipient.
4. The Sales Order is `Resolved`, or remains `Active` with an explicitly displayed payer-specific unresolved consequence.

## 7. References

- [Apotek Domain](../apotek-domain.md), especially `BR-APT-015`, `BR-APT-020`–`BR-APT-028`, `BR-APT-040`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-070`–`BR-APT-078`, and `BR-APT-090`–`BR-APT-096`.
- [Outpatient Apotek Workflow](../outpatient-apotek-workflow.md), `WF-APT-RJ-005`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md).
- [Tata Rekening Domain](../../../contexts/TataRekening/02-domain.md).
