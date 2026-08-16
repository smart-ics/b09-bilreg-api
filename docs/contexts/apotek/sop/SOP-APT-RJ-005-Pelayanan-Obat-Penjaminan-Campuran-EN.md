# SOP APT-RJ-005 — Fulfill Mixed-Coverage Medication

**Artifact status:** Canonical target operational specification

**Bounded context:** Apotek

**Workflow:** `WF-APT-RJ-005`

**Bahasa Indonesia companion:** [SOP APT-RJ-005 — Memenuhi Obat dengan Coverage Campuran](./SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-ID.md)

**Subsystem terminology status:** `Pharmacy System` and `Outpatient Pharmacy` are established subsystem terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for splitting Fornas Covered and Not Covered prescription lines into independent Sales Orders while coordinating cleared quantities for one outpatient pickup.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Caregiver | Human | Confirms or declines the Patient-Pay Sales Order, pays when confirmed, presents for pickup, receives education, and accepts medication when authorized. |
| Pharmacy Staff | Human | Establishes the BPJS-covered Sales Order for Covered lines and the independent Patient-Pay Sales Order for Not Covered lines, communicates the Patient-payable amount, records its confirmed invoice, prepares cleared medication, and performs the pickup call. |
| Pharmacy Supervisor | Human | Authorizes the manual uncollected-medication resolution when the Patient does not collect prepared medication. |
| Cashier or Payment Authority | Human or Subsystem | Receives payment and supplies Payment Clearance for the Patient-Pay Sales Invoice. |
| Pharmacist | Human | Verifies the recipient, completes Final Dispense Review, and provides Patient Education. |
| SEP and Fornas Authorities | Subsystem | Classify each prescription line as Covered or Not Covered and supply SEP validity. |
| Pharmacy System | Subsystem | Maintains independent Sales Orders, payer-specific invoices, and Dispense Authorized evaluation per line. |
| Patient Tracker | Subsystem | Records one `ServedAt` and one `DoneAt` for the common Queue Entry. |
| Inventory | Subsystem | Supplies Mutasi, Remove Stock, and return-disposition outcomes. |
| Tata Rekening | Subsystem | Receives and resolves payer-specific financial consequences. |

## 3. Preconditions

1. Participating staff are signed in with their required permissions.
2. Fornas has classified prescription lines as Covered or Not Covered.
3. A valid SEP exists for Covered lines.
4. Not Covered lines have not been automatically cancelled.
5. No Patient-Pay or BPJS Sales Invoice has been established for the proposed lines unless the procedure is resuming after an accountable exception.

## 4. Operational Steps

1. **SEP and Fornas Authorities** classify each prescription line as Covered or Not Covered. **Pharmacy System** displays that classification.
2. **Pharmacy Staff** establishes the BPJS-covered Sales Order from Covered lines only. Uncovered lines do not remain on the BPJS path.
3. **Pharmacy Staff** may establish a separate Patient-Pay Sales Order for Not Covered lines.
4. **Pharmacy System** calculates and displays the Patient-payable amount from the Patient-Pay Sales Order.
5. **Pharmacy Staff** verbally communicates that amount before a General Patient Sales Invoice exists.
6. **Patient or Caregiver** verbally confirms the Patient-Pay Sales Order.
7. **Pharmacy Staff** records the confirmed Patient-Pay transaction; **Pharmacy System** establishes a General Patient Sales Invoice from that Sales Order.
8. **Cashier or Payment Authority** receives payment and supplies Payment Clearance for the Patient-Pay Sales Invoice.
9. **Pharmacy System** evaluates Dispense Authorized independently: Covered lines from coverage evidence; Patient-Pay lines from Payment Clearance.
10. After every quantity intended for handover has Dispense Authorized, **Pharmacy Staff** starts and completes Medication Preparation on each applicable Dispense Order.
11. **Pharmacy System** records the first `Medication Preparation Started`; **Patient Tracker** moves the common Queue Entry to In Service and records one `ServedAt`.
12. **Pharmacy System** displays every intended Dispense Order as `Prepared` or with an accountable exception outcome.
13. **Pharmacy Staff** performs one coordinated pickup call; **Patient Tracker** makes the common Queue Entry `Done` and records one `DoneAt`.
14. With the Patient or caregiver present, **Pharmacist** verifies the Authorized Recipient, completes Final Dispense Review, and records Patient Education.
15. **Pharmacy Staff** completes the physical handover after Pharmacist authorization.
16. **Pharmacy System** establishes the BPJS Sales Invoice only with successful handover of the BPJS-covered Sales Order, records Medication Dispense and Medication Handover for all applicable quantities, and preserves both Sales Orders.
17. **Inventory** supplies Remove Stock outcomes; **Pharmacy System** displays final Dispense Order and Sales Order progress.

## 5. Operational Exceptions

### 5.1 Patient declines the Patient-Pay Sales Order before invoice establishment

- **Pharmacy Staff** records the decline and establishes no General Patient Sales Invoice.
- **Pharmacy System** records an accountable declined outcome on the Patient-Pay Sales Order and permits the BPJS-covered Sales Order to continue independently.

### 5.2 An item is classified Not Covered

- **Pharmacy System** displays the affected line as Not Covered and keeps it off the BPJS-covered Sales Order.
- **Pharmacy Staff** may establish the independent Patient-Pay Sales Order, communicates the amount, and obtains verbal confirmation.

### 5.3 Not all intended quantities have Dispense Authorized

- **Pharmacy System** blocks coordinated preparation and pickup for unauthorized quantities.
- **Pharmacy Staff** resolves the applicable coverage or payment path before continuing.

### 5.4 Final Dispense Review fails

- **Pharmacist** records the failure reason and affected quantity and does not authorize handover.
- **Pharmacy System** appends an immutable review record, returns only the affected Dispense Order from `Prepared` to `Preparing`, and does not rewrite the other Sales Order.
- **Pharmacy Staff** corrects and prepares the affected medication again; **Pharmacist** performs a new Final Dispense Review.

### 5.5 Established invoice, non-fulfillment, or No-Show requires correction

- **Pharmacy System** keeps BPJS-covered and Patient-Pay consequences on their own Sales Orders.
- **Tata Rekening** supplies the required correction for the paid Patient-Pay portion; the absent BPJS invoice remains absent until successful handover.
- **Pharmacy Supervisor** applies `SOP-APT-RJ-007` for uncollected medication.

## 6. Completion Criteria

1. Covered and Not Covered lines remain on independent Sales Orders.
2. The financially cleared General Patient Sales Invoice and the handover-time BPJS Sales Invoice are separate and traceable to their own Sales Orders.
3. One coordinated Medication Handover may record every applicable quantity and Authorized Recipient.
4. Each Sales Order is `Resolved`, or remains `Active` with an explicitly displayed unresolved consequence.

## 7. References

- [Apotek Domain](../apotek-domain.md), especially `BR-APT-011`, `BR-APT-015`, `BR-APT-020`–`BR-APT-028`, `BR-APT-040`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-070`–`BR-APT-078`, `BR-APT-090`–`BR-APT-096`, `BR-APT-108`, and `BR-APT-119`–`BR-APT-124`.
- [Outpatient Apotek Workflow](../outpatient-apotek-workflow.md), `WF-APT-RJ-005`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md).
- [Tata Rekening Domain](../../../contexts/TataRekening/02-domain.md).
