# SOP APT-RJ-004 — Fulfill Medication for a BPJS Patient

**Artifact status:** Canonical target operational specification

**Bounded context:** Apotek

**Workflow:** `WF-APT-RJ-004`

**Bahasa Indonesia companion:** [SOP APT-RJ-004 — Memenuhi Obat untuk Pasien BPJS](./SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-ID.md)

**Subsystem terminology status:** `Pharmacy System` and `Outpatient Pharmacy` are established subsystem terms. Other controls are described by operational action because approved target-workflow labels are not available.

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
| Pharmacy System | Subsystem | Records Coverage and Fulfillment Clearance, tracks preparation, and atomically records the BPJS Sales Invoice and successful handover outcome. |
| Patient Tracker | Subsystem | Records `ServedAt` at preparation start and `DoneAt` at the pickup call. |
| Inventory | Subsystem | Supplies reservation, issue, and return-disposition outcomes. |
| Tata Rekening | Subsystem | Receives the BPJS Financial Charge outcome. |

## 3. Preconditions

1. Participating staff are signed in with their required permissions.
2. Outpatient Queue Mapping, an active Sales Order, covered Billing Allocations, and an applicable Dispense Order are displayed.
3. A valid SEP exists for the encounter, and authoritative Fornas mapping supports every covered quantity.
4. The Patient-payable amount is zero, payment disposition is `Not Required`, and no BPJS Sales Invoice exists.

## 4. Operational Steps

1. **Pharmacy Staff** opens the mapped BPJS demand in `Apotek Rajal` and verifies the Patient, SEP reference, covered Billing Allocations, and Dispense Order quantities.
2. **SEP and Fornas Authorities** supply valid SEP and item-level coverage outcomes.
3. **Pharmacy System** displays Coverage Clearance for each covered quantity and establishes the corresponding Fulfillment Clearance without requiring a Sales Invoice.
4. **Inventory** secures Stock Reservation when not already present; **Pharmacy System** displays the reservation outcome.
5. **Pharmacy Staff** starts Medication Preparation only after the Dispense Order is released.
6. **Pharmacy System** records `Medication Preparation Started`; **Patient Tracker** moves the Queue Entry to In Service and records `ServedAt`.
7. **Pharmacy Staff** completes preparation or compounding and records completion; **Pharmacy System** displays the Dispense Order as `Prepared`.
8. **Pharmacy Staff** verifies that every Dispense Order intended for handover is `Prepared` or has an accountable exception outcome.
9. **Pharmacy Staff** performs one coordinated pickup call; **Patient Tracker** makes the Queue Entry `Done` and records `DoneAt`.
10. With the Patient or caregiver present, **Pharmacist** verifies the Authorized Recipient, completes Final Dispense Review, and records applicable Patient Education. When the review passes, **Pharmacy System** appends the review record and displays the Dispense Order as `Reviewed`.
11. **Pharmacy System** blocks completion when recipient verification or final review is incomplete.
12. **Pharmacy Staff** completes the physical handover after Pharmacist authorization.
13. As one accountable outcome, **Pharmacy System** establishes the BPJS Sales Invoice from covered Billing Allocations, records Medication Dispense, and records Medication Handover.
14. **Inventory** supplies the authoritative Inventory Issue outcome; **Pharmacy System** displays the Dispense Order as `Completed`.
15. **Pharmacy System** displays the Sales Order as `Resolved` only when every accepted quantity and commercial consequence is final.

## 5. Operational Exceptions

### 5.1 SEP is invalid or coverage is absent

- **SEP and Fornas Authorities** supply no Coverage Clearance for the affected quantity.
- **Pharmacy System** keeps preparation blocked.
- **Pharmacy Staff** routes non-covered quantities through `SOP-APT-RJ-005` when applicable.

### 5.2 Shortage occurs after Sales Order establishment

- **Pharmacy Staff** does not create Backorder or select an alternate stock source.
- **Pharmacy System** records an Unfulfilled Medication Outcome, supports Salinan Resep for unfulfilled lines, and preserves the accepted medication identity.

### 5.3 Final Dispense Review fails

- **Pharmacist** records the failure reason and affected quantity and does not authorize handover.
- **Pharmacy System** appends an immutable review record with the Pharmacist and effective business time, returns the Dispense Order from `Prepared` to `Preparing`, and establishes neither the BPJS Sales Invoice nor Medication Handover.
- **Pharmacy Staff** corrects and prepares the affected medication again; **Pharmacy System** returns the Dispense Order to `Prepared`, and **Pharmacist** performs a new Final Dispense Review. Previous review records remain visible and unchanged.

### 5.4 Patient does not collect medication

- **Pharmacy Supervisor** applies `SOP-APT-RJ-007`.
- **Pharmacy System** does not establish or cancel a BPJS Sales Invoice for the No-Show.

## 6. Completion Criteria

1. Patient-payable amount is zero and payment disposition is `Not Required`.
2. The BPJS Sales Invoice and Medication Handover are displayed as one successful accountable outcome.
3. The Dispense Order is `Completed`, the Authorized Recipient is identified, and Inventory Issue is displayed.
4. The Sales Order is `Resolved`, or remains `Active` with an explicitly displayed unresolved outcome.
5. Queue `Done` is not used as proof of Medication Handover.

## 7. References

- [Apotek Domain](../apotek-domain.md), especially `BR-APT-020`–`BR-APT-026`, `BR-APT-029`–`BR-APT-045`, `BR-APT-066`, `BR-APT-068`–`BR-APT-069`, `BR-APT-073`–`BR-APT-079`, `BR-APT-081`–`BR-APT-083`, `BR-APT-088`, `BR-APT-090`, `BR-APT-095`–`BR-APT-096`, and `BR-APT-114`–`BR-APT-118`.
- [Outpatient Apotek Workflow](../outpatient-apotek-workflow.md), `WF-APT-RJ-004`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md), `BR-TRK-045`, `BR-TRK-045a`, and `BR-TRK-046`.
- [Tata Rekening Domain](../../../contexts/TataRekening/02-domain.md).
