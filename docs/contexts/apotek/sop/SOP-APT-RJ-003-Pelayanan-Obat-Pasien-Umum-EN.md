# SOP APT-RJ-003 — Fulfill Medication for a General Patient

**Artifact status:** Canonical target operational specification

**Bounded context:** Apotek

**Workflow:** `WF-APT-RJ-003`

**Bahasa Indonesia companion:** [SOP APT-RJ-003 — Memenuhi Obat untuk Pasien Umum](./SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-ID.md)

**Subsystem terminology status:** `Pharmacy System` and `Outpatient Pharmacy` are established subsystem terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for obtaining verbal Purchase Confirmation, establishing and clearing the General Patient Sales Invoice, preparing medication, and completing accountable outpatient Medication Handover.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Caregiver | Human | Confirms or declines the calculated purchase, pays when confirmed, presents for pickup, receives education, and accepts medication when authorized. |
| Pharmacy Staff | Human | Communicates the calculated amount, records the confirmed Sales Invoice, prepares or compounds medication under a released Dispense Order, coordinates readiness, and performs the pickup call. |
| Pharmacy Supervisor | Human | Authorizes the manual uncollected-medication resolution when the Patient does not collect prepared medication. Authorization is by an authorized pharmacist according to operational policy; no monetary approval threshold applies. |
| Cashier or Payment Authority | Human or Subsystem | Receives payment and supplies Payment Clearance. |
| Pharmacist | Human | Operationally verifies the recipient, completes Final Dispense Review, and records Patient Education Acknowledgement. Recipient verification is not system-enforced. Detailed counseling notes are optional. |
| Pharmacy System | Subsystem | Displays allocations and amounts, records the invoice and clearances, tracks preparation, and records dispense and handover. |
| Patient Tracker | Subsystem | Records `ServedAt` at preparation start and `DoneAt` at the pickup call. |
| Inventory | Subsystem | Supplies reservation, issue, and return-disposition outcomes. |
| Tata Rekening | Subsystem | Receives Financial Charge and supplies required correction outcomes. |

## 3. Preconditions

1. Participating staff are signed in with their required permissions.
2. Outpatient Queue Mapping, an active Sales Order, Patient-payable Billing Allocations, and a calculated Pricing Snapshot are displayed.
3. No Sales Invoice exists for the proposed Patient-payable sale.
4. An applicable Dispense Order exists or can be established from Fulfillment Allocations.

## 4. Operational Steps

1. **Pharmacy Staff** opens the mapped demand in `Apotek Rajal` and verifies the calculated Patient-payable amount and Billing Allocations.
2. **Pharmacy Staff** receives the Patient during Manual Mapping or performs an administrative Queue Number call after Tracker Mapping; **Patient Tracker** does not record `ServedAt` or `DoneAt` for this interaction.
3. **Pharmacy Staff** verbally communicates the calculated amount before a Sales Invoice exists.
4. **Patient or Caregiver** verbally confirms the purchase.
5. **Pharmacy Staff** records the confirmed transaction; **Pharmacy System** establishes the Sales Invoice only from the confirmed Billing Allocations and displays its identifier and amount.
6. **Cashier or Payment Authority** receives payment and supplies Payment Clearance for that Sales Invoice.
7. **Pharmacy System** displays Payment Clearance and establishes Fulfillment Clearance for the applicable Dispense Order quantities.
8. **Inventory** secures the required Stock Reservation when it is not already present; **Pharmacy System** displays the reservation outcome.
9. **Pharmacy Staff** starts Medication Preparation only after the Dispense Order is released.
10. **Pharmacy System** records `Medication Preparation Started`; **Patient Tracker** moves the Queue Entry to In Service and records `ServedAt`.
11. **Pharmacy Staff** completes preparation or compounding and records completion; **Pharmacy System** displays the Dispense Order as `Prepared`.
12. **Pharmacy Staff** verifies that every Dispense Order intended for the handover is `Prepared` or has an accountable exception outcome.
13. **Pharmacy Staff** performs one coordinated pickup call; **Patient Tracker** makes the Queue Entry `Done` and records `DoneAt`.
14. With the Patient or caregiver present, **Pharmacist** operationally verifies the recipient, completes Final Dispense Review, and records Patient Education Acknowledgement. When the review passes, **Pharmacy System** appends the review record and displays the Dispense Order as `Reviewed`. **Pharmacy System** records education timestamp and responsible Pharmacist. Detailed counseling notes are optional. The Pharmacist may optionally record recipient phone number and relationship for reference.
15. **Pharmacy System** blocks handover until Final Dispense Review has passed and Patient Education Acknowledgement is recorded. Recipient identity is not a system gate. Detailed counseling notes are not required.
16. **Pharmacy Staff** completes the physical handover after Pharmacist authorization; **Pharmacy System** records Medication Dispense and Medication Handover for each applicable quantity.
17. **Inventory** supplies the authoritative Inventory Issue outcome; **Pharmacy System** displays the Dispense Order as `Completed` when all required outcomes are present.
18. **Pharmacy System** displays the Sales Order as `Resolved` only when every accepted quantity and commercial consequence is final.

## 5. Operational Exceptions

### 5.1 Patient declines before invoice establishment

- **Pharmacy Staff** records the decline and does not establish a Sales Invoice.
- **Pharmacy System** records the allocation as declined or commercially unallocated and requests release of unused reservation from **Inventory**.

### 5.2 Amount changes before invoice establishment

- **Pharmacy System** displays the revised calculated amount.
- **Pharmacy Staff** communicates it again and obtains a new verbal confirmation before recording the transaction.

### 5.3 Payment is incomplete or an established invoice requires correction

- **Pharmacy System** keeps Medication Preparation blocked while Payment Clearance is absent.
- **Pharmacy Staff** cancels only when the displayed Sales Invoice lifecycle permits; otherwise **Tata Rekening** supplies Financial Adjustment, Credit Note, Refund, or another accountable outcome.

### 5.4 Shortage

- **Pharmacy Staff** does not create Backorder or select an alternate stock source; no substitution is made.
- **Pharmacy System** records the applicable Unfulfilled Medication Outcome, supports Salinan Resep for unfulfilled lines, and keeps required financial consequences visible.

### 5.5 Final Dispense Review fails

- When Final Dispense Review fails, **Pharmacist** records the reason and affected quantity and does not authorize handover.
- **Pharmacy System** appends an immutable review record with the Pharmacist and effective business time, returns the Dispense Order from `Prepared` to `Preparing`, and keeps handover blocked.
- **Pharmacy Staff** corrects and prepares the affected medication again; **Pharmacy System** returns the Dispense Order to `Prepared`, and **Pharmacist** performs a new Final Dispense Review. Previous review records remain visible and unchanged.

### 5.6 Patient does not collect medication

- **Pharmacy Supervisor** applies `SOP-APT-RJ-007`; Queue `DoneAt` is not reversed.

## 6. Completion Criteria

1. The Sales Invoice is visibly financially cleared.
2. The Dispense Order is `Completed`, and Medication Handover records effective time. Recipient phone number and relationship may be recorded optionally for reference.
3. Inventory Issue is displayed as an authoritative Inventory outcome.
4. The Sales Order is `Resolved`, or remains `Active` with an explicitly displayed unresolved fulfillment or commercial consequence.
5. Queue `Done` is not used as proof of Medication Handover.

## 7. References

- [Apotek Domain](../apotek-domain.md), especially `BR-APT-020`–`BR-APT-028`, `BR-APT-033`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-067`–`BR-APT-072`, `BR-APT-076`–`BR-APT-083`, `BR-APT-088`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`, and `BR-APT-125`–`BR-APT-134`.
- [Outpatient Apotek Workflow](../outpatient-apotek-workflow.md), `WF-APT-RJ-003`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md), `BR-TRK-045`, `BR-TRK-045a`, and `BR-TRK-046`.
- [Tata Rekening Domain](../../../contexts/TataRekening/02-domain.md).
