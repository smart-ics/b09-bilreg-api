# SOP MF-RJ-004 — Fulfill Medication for a BPJS Patient

**Artifact status:** Canonical target operational specification

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-004`

**Bahasa Indonesia companion:** [SOP MF-RJ-004 — Memenuhi Obat untuk Pasien BPJS](./SOP-MF-RJ-004-Fulfill-Medication-for-a-BPJS-Patient-ID.md)

**Application terminology status:** `Apotek` and `Apotek Rajal` are established application terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for clearing and preparing BPJS-covered outpatient medication without prior Patient payment or Sales Invoice, then establishing the BPJS Sales Invoice only with successful Medication Handover.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Caregiver | Human | Presents for pickup, receives education, and accepts medication when authorized. |
| Pharmacy Staff | Human | Verifies the covered work projection, prepares or compounds medication under a released Dispense Order, coordinates readiness, and performs the pickup call. |
| Pharmacy Supervisor | Human | Authorizes the manual uncollected-medication resolution when the Patient does not collect prepared medication. |
| Pharmacist | Human | Verifies the recipient, completes Final Dispense Review, and provides Patient Education. |
| SEP and Fornas Authorities | Subsystem | Supply encounter-level SEP validity and item-level Fornas coverage. |
| Medication Fulfillment Application | Application | Records Coverage and Fulfillment Clearance, tracks preparation, and atomically records the BPJS Sales Invoice and successful handover outcome. |
| Patient Tracker | Subsystem | Records `ServedAt` at preparation start and `DoneAt` at the pickup call. |
| Inventory | Subsystem | Supplies reservation, issue, and return-disposition outcomes. |
| Tata Rekening | Subsystem | Receives the BPJS Financial Charge outcome. |

## 3. Preconditions

1. Participating staff are signed in with their required permissions.
2. Outpatient Queue Mapping, an active Pharmacy Sales Order, covered Billing Allocations, and an applicable Dispense Order are displayed.
3. A valid SEP exists for the encounter, and authoritative Fornas mapping supports every covered quantity.
4. The Patient-payable amount is zero, payment disposition is `Not Required`, and no BPJS Sales Invoice exists.

## 4. Operational Steps

1. **Pharmacy Staff** opens the mapped BPJS demand in `Apotek Rajal` and verifies the Patient, SEP reference, covered Billing Allocations, and Dispense Order quantities.
2. **SEP and Fornas Authorities** supply valid SEP and item-level coverage outcomes.
3. **Medication Fulfillment Application** displays Coverage Clearance for each covered quantity and establishes the corresponding Fulfillment Clearance without requiring a Sales Invoice.
4. **Inventory** secures Stock Reservation when not already present; **Medication Fulfillment Application** displays the reservation outcome.
5. **Pharmacy Staff** starts Medication Preparation only after the Dispense Order is released.
6. **Medication Fulfillment Application** records `Medication Preparation Started`; **Patient Tracker** moves the Queue Entry to In Service and records `ServedAt`.
7. **Pharmacy Staff** completes preparation or compounding and records completion; **Medication Fulfillment Application** displays the Dispense Order as `Prepared`.
8. **Pharmacy Staff** verifies that every Dispense Order intended for handover is `Prepared` or has an accountable exception outcome.
9. **Pharmacy Staff** performs one coordinated pickup call; **Patient Tracker** makes the Queue Entry `Done` and records `DoneAt`.
10. With the Patient or caregiver present, **Pharmacist** verifies the Authorized Recipient, completes Final Dispense Review, and records applicable Patient Education.
11. **Medication Fulfillment Application** blocks completion when recipient verification or final review is incomplete.
12. **Pharmacy Staff** completes the physical handover after Pharmacist authorization.
13. As one accountable outcome, **Medication Fulfillment Application** establishes the BPJS Sales Invoice from covered Billing Allocations, records Medication Dispense, and records Medication Handover.
14. **Inventory** supplies the authoritative Inventory Issue outcome; **Medication Fulfillment Application** displays the Dispense Order as `Completed`.
15. **Medication Fulfillment Application** displays the Pharmacy Sales Order as `Resolved` only when every accepted quantity and commercial consequence is final.

## 5. Operational Exceptions

### 5.1 SEP is invalid or coverage is absent

- **SEP and Fornas Authorities** supply no Coverage Clearance for the affected quantity.
- **Medication Fulfillment Application** keeps preparation blocked.
- **Pharmacy Staff** routes non-covered quantities through `SOP-MF-RJ-005` when applicable.

### 5.2 Shortage occurs after Sales Order establishment

- **Pharmacy Staff** records Backorder or another approved stock source for the same medication product.
- **Medication Fulfillment Application** preserves the accepted medication identity and displays the unresolved outcome.

### 5.3 Final Dispense Review fails

- **Pharmacist** records the failed review and does not authorize handover.
- **Medication Fulfillment Application** establishes neither the BPJS Sales Invoice nor Medication Handover.

### 5.4 Patient does not collect medication

- **Pharmacy Supervisor** applies `SOP-MF-RJ-007`.
- **Medication Fulfillment Application** does not establish or cancel a BPJS Sales Invoice for the No-Show.

## 6. Completion Criteria

1. Patient-payable amount is zero and payment disposition is `Not Required`.
2. The BPJS Sales Invoice and Medication Handover are displayed as one successful accountable outcome.
3. The Dispense Order is `Completed`, the Authorized Recipient is identified, and Inventory Issue is displayed.
4. The Pharmacy Sales Order is `Resolved`, or remains `Active` with an explicitly displayed unresolved outcome.
5. Queue `Done` is not used as proof of Medication Handover.

## 7. References

- [Medication Fulfillment Domain](../medication-fulfillment-domain.md), especially `BR-MF-020`–`BR-MF-026`, `BR-MF-029`–`BR-MF-045`, `BR-MF-066`, `BR-MF-068`–`BR-MF-069`, `BR-MF-073`–`BR-MF-079`, `BR-MF-081`–`BR-MF-083`, `BR-MF-088`, `BR-MF-090`, and `BR-MF-095`.
- [Outpatient Medication Fulfillment Workflow](../outpatient-medication-fulfillment-workflow.md), `WF-MF-RJ-004`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md), `BR-TRK-045`, `BR-TRK-045a`, and `BR-TRK-046`.
- [Tata Rekening Domain](../../../contexts/TataRekening/02-domain.md).
