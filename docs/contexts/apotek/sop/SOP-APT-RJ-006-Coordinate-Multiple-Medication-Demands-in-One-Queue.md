# SOP APT-RJ-006 — Coordinate Multiple Medication Demands in One Queue

**Artifact status:** Canonical target operational specification

**Bounded context:** Apotek

**Workflow:** `WF-APT-RJ-006`

**Bahasa Indonesia companion:** [SOP APT-RJ-006 — Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrean](./SOP-APT-RJ-006-Coordinate-Multiple-Medication-Demands-in-One-Queue-ID.md)

**Subsystem terminology status:** `Pharmacy System` and `Outpatient Pharmacy` are established subsystem terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for coordinating two or more independently accountable medication demands in one outpatient queue and pickup session without merging their records.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Caregiver | Human | Completes payer-specific interactions, presents for one coordinated pickup, receives consolidated education, and accepts applicable medication. |
| Pharmacy Staff | Human | Verifies separate mappings and progress, prepares each cleared Dispense Order separately, coordinates payer readiness, communicates exceptions, and performs one pickup call. |
| Pharmacist | Human | Verifies the recipient, reviews every Prepared Medication, and provides consolidated education with medication-specific instructions. |
| Pharmacy System | Subsystem | Projects per-demand progress and records separate allocations, invoices, Dispense Orders, dispense, and handover outcomes. |
| Patient Tracker | Subsystem | Retains one Queue Entry with one `CreatedAt`, at most one `ServedAt`, and one `DoneAt`. |
| Cashier or Payment Authority | Human or Subsystem | Supplies Payment Clearance for applicable Patient-payable demands. |
| SEP and Fornas Authorities | Subsystem | Supply coverage evidence for applicable BPJS demands. |
| Inventory | Subsystem | Supplies per-Dispense-Order reservation, issue, and disposition outcomes. |

## 3. Preconditions

1. Participating staff are signed in with their required permissions.
2. One Pharmacy Queue Entry has separate Outpatient Queue Mappings to at least two medication demands.
3. Each Prescription has its own Prescription Review.
4. Each accepted source has its own Pharmacy Sales Order and active primary outpatient Dispense Order.

## 4. Operational Steps

1. **Pharmacy Staff** opens the common Queue Entry in `Apotek Rajal`.
2. **Pharmacy System** displays every mapped demand separately with its source, Pharmacy Sales Order, payer, Billing Allocation, Fulfillment Allocation, Sales Invoice, clearance, and Dispense Order progress.
3. **Pharmacy Staff** verifies that no demand, Pharmacy Sales Order, Sales Invoice, or Dispense Order has been merged with another demand.
4. **Pharmacy Staff** applies the General Patient, BPJS, or mixed-coverage SOP to each demand according to its payer classification.
5. **Cashier or Payment Authority** supplies applicable Payment Clearance; **SEP and Fornas Authorities** supply applicable Coverage Clearance.
6. **Pharmacy Staff** prepares each released Dispense Order separately and records completion for each one.
7. On the first applicable preparation start, **Pharmacy System** records `Medication Preparation Started`; **Patient Tracker** records one `ServedAt` and moves the common Queue Entry to In Service.
8. **Pharmacy System** displays every demand intended for pickup as `Prepared` or with an accountable exception outcome.
9. **Pharmacy Staff** reviews all per-demand progress and does not declare an unresolved demand ready.
10. When all included demands are ready or accountably resolved for the intended pickup, **Pharmacy Staff** performs one coordinated pickup call.
11. **Patient Tracker** records one `DoneAt` and makes the common Queue Entry `Done`.
12. **Pharmacy Staff** communicates any accountable exception outcome together with the ready-demand information.
13. With the Patient or caregiver present, **Pharmacist** verifies the Authorized Recipient, completes Final Dispense Review for each Prepared Medication, and records consolidated Patient Education with medication-specific instructions.
14. **Pharmacy Staff** completes the physical handover after Pharmacist authorization.
15. **Pharmacy System** records Medication Dispense and Medication Handover against every applicable Dispense Order and Pharmacy Sales Order separately.
16. **Inventory** supplies separate Inventory Issue or disposition outcomes for each originating Dispense Order.

## 5. Operational Exceptions

### 5.1 One demand remains unresolved

- **Pharmacy System** displays that demand as not ready.
- **Pharmacy Staff** defers the coordinated call unless the applicable payer SOP permits and records an accountable partial path accepted by the Patient.

### 5.2 One demand has an accountable exception outcome

- **Pharmacy Staff** includes the resolved exception in the coordinated communication and proceeds with ready demands only when their payer procedures permit.
- **Pharmacy System** retains the exception under its originating Sales Order and Dispense Order.

### 5.3 One mapping or demand requires correction

- **Pharmacy Staff** corrects only the affected mapping or demand.
- **Pharmacy System** preserves every other demand's history and does not infer that one handover fulfilled another demand.

## 6. Completion Criteria

1. The common Queue Entry displays one `CreatedAt`, at most one `ServedAt`, and one `DoneAt`.
2. Every Prescription, Pharmacy Sales Order, Sales Invoice, and Dispense Order retains independent identity and progress.
3. One pickup call is recorded, while each applicable demand has its own Medication Handover fact or accountable exception outcome.

## 7. References

- [Apotek Domain](../apotek-domain.md), especially `BR-APT-011`, `BR-APT-015`, `BR-APT-022`, `BR-APT-030`, `BR-APT-056`–`BR-APT-060`, `BR-APT-084`–`BR-APT-088`, and `BR-APT-095`.
- [Outpatient Apotek Workflow](../outpatient-apotek-workflow.md), `WF-APT-RJ-006`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md), especially `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, and `BR-TRK-045a`.
