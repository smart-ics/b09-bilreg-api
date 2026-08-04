# SOP MF-RJ-003 — Fulfill Medication for a General Patient

**Artifact status:** Canonical target operational specification

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-003`

**Bahasa Indonesia companion:** [SOP MF-RJ-003 — Memenuhi Obat untuk Pasien Umum](./SOP-MF-RJ-003-Fulfill-Medication-for-a-General-Patient-ID.md)

**Application terminology status:** `Apotek` and `Apotek Rajal` are established application terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for obtaining verbal Purchase Confirmation, establishing and clearing the General Patient Sales Invoice, preparing medication, and completing accountable outpatient Medication Handover.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Caregiver | Human | Confirms or declines the calculated purchase, pays when confirmed, presents for pickup, receives education, and accepts medication when authorized. |
| Pharmacy Staff | Human | Communicates the calculated amount, records the confirmed Sales Invoice, prepares or compounds medication under a released Dispense Order, coordinates readiness, and performs the pickup call. |
| Pharmacy Supervisor | Human | Authorizes the manual uncollected-medication resolution when the Patient does not collect prepared medication. |
| Cashier or Payment Authority | Human or Subsystem | Receives payment and supplies Payment Clearance. |
| Pharmacist | Human | Verifies the recipient, completes Final Dispense Review, and provides Patient Education. |
| Medication Fulfillment Application | Application | Displays allocations and amounts, records the invoice and clearances, tracks preparation, and records dispense and handover. |
| Patient Tracker | Subsystem | Records `ServedAt` at preparation start and `DoneAt` at the pickup call. |
| Inventory | Subsystem | Supplies reservation, issue, and return-disposition outcomes. |
| Tata Rekening | Subsystem | Receives Financial Charge and supplies required correction outcomes. |

## 3. Preconditions

1. Participating staff are signed in with their required permissions.
2. Outpatient Queue Mapping, an active Pharmacy Sales Order, Patient-payable Billing Allocations, and a calculated Pricing Snapshot are displayed.
3. No Sales Invoice exists for the proposed Patient-payable sale.
4. An applicable Dispense Order exists or can be established from Fulfillment Allocations.

## 4. Operational Steps

1. **Pharmacy Staff** opens the mapped demand in `Apotek Rajal` and verifies the calculated Patient-payable amount and Billing Allocations.
2. **Pharmacy Staff** receives the Patient during Manual Mapping or performs an administrative Queue Number call after Tracker Mapping; **Patient Tracker** does not record `ServedAt` or `DoneAt` for this interaction.
3. **Pharmacy Staff** verbally communicates the calculated amount before a Sales Invoice exists.
4. **Patient or Caregiver** verbally confirms the purchase.
5. **Pharmacy Staff** records the confirmed transaction; **Medication Fulfillment Application** establishes the Sales Invoice only from the confirmed Billing Allocations and displays its identifier and amount.
6. **Cashier or Payment Authority** receives payment and supplies Payment Clearance for that Sales Invoice.
7. **Medication Fulfillment Application** displays Payment Clearance and establishes Fulfillment Clearance for the applicable Dispense Order quantities.
8. **Inventory** secures the required Stock Reservation when it is not already present; **Medication Fulfillment Application** displays the reservation outcome.
9. **Pharmacy Staff** starts Medication Preparation only after the Dispense Order is released.
10. **Medication Fulfillment Application** records `Medication Preparation Started`; **Patient Tracker** moves the Queue Entry to In Service and records `ServedAt`.
11. **Pharmacy Staff** completes preparation or compounding and records completion; **Medication Fulfillment Application** displays the Dispense Order as `Prepared`.
12. **Pharmacy Staff** verifies that every Dispense Order intended for the handover is `Prepared` or has an accountable exception outcome.
13. **Pharmacy Staff** performs one coordinated pickup call; **Patient Tracker** makes the Queue Entry `Done` and records `DoneAt`.
14. With the Patient or caregiver present, **Pharmacist** verifies the Authorized Recipient, completes Final Dispense Review, and records applicable Patient Education.
15. **Medication Fulfillment Application** blocks handover until the review, recipient, and education requirements are recorded.
16. **Pharmacy Staff** completes the physical handover after Pharmacist authorization; **Medication Fulfillment Application** records Medication Dispense and Medication Handover for each applicable quantity.
17. **Inventory** supplies the authoritative Inventory Issue outcome; **Medication Fulfillment Application** displays the Dispense Order as `Completed` when all required outcomes are present.
18. **Medication Fulfillment Application** displays the Pharmacy Sales Order as `Resolved` only when every accepted quantity and commercial consequence is final.

## 5. Operational Exceptions

### 5.1 Patient declines before invoice establishment

- **Pharmacy Staff** records the decline and does not establish a Sales Invoice.
- **Medication Fulfillment Application** records the allocation as declined or commercially unallocated and requests release of unused reservation from **Inventory**.

### 5.2 Amount changes before invoice establishment

- **Medication Fulfillment Application** displays the revised calculated amount.
- **Pharmacy Staff** communicates it again and obtains a new verbal confirmation before recording the transaction.

### 5.3 Payment is incomplete or an established invoice requires correction

- **Medication Fulfillment Application** keeps Medication Preparation blocked while Payment Clearance is absent.
- **Pharmacy Staff** cancels only when the displayed Sales Invoice lifecycle permits; otherwise **Tata Rekening** supplies Financial Adjustment, Credit Note, Refund, or another accountable outcome.

### 5.4 Shortage or failed Final Dispense Review

- **Pharmacy Staff** records Backorder or another approved source for the same medication product; no substitution is made.
- **Pharmacist** records the failed review outcome and does not authorize handover.
- **Medication Fulfillment Application** records the applicable Unfulfilled Medication Outcome and keeps required financial consequences visible.

### 5.5 Patient does not collect medication

- **Pharmacy Supervisor** applies `SOP-MF-RJ-007`; Queue `DoneAt` is not reversed.

## 6. Completion Criteria

1. The Sales Invoice is visibly financially cleared.
2. The Dispense Order is `Completed`, and Medication Handover identifies the Authorized Recipient and effective time.
3. Inventory Issue is displayed as an authoritative Inventory outcome.
4. The Pharmacy Sales Order is `Resolved`, or remains `Active` with an explicitly displayed unresolved fulfillment or commercial consequence.
5. Queue `Done` is not used as proof of Medication Handover.

## 7. References

- [Medication Fulfillment Domain](../medication-fulfillment-domain.md), especially `BR-MF-020`–`BR-MF-028`, `BR-MF-033`–`BR-MF-046`, `BR-MF-056`–`BR-MF-060`, `BR-MF-067`–`BR-MF-072`, `BR-MF-076`–`BR-MF-083`, `BR-MF-088`, and `BR-MF-095`.
- [Outpatient Medication Fulfillment Workflow](../outpatient-medication-fulfillment-workflow.md), `WF-MF-RJ-003`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md), `BR-TRK-045`, `BR-TRK-045a`, and `BR-TRK-046`.
- [Tata Rekening Domain](../../../contexts/TataRekening/02-domain.md).
