# SOP APT-RJ-006 — Coordinate Multiple Medication Demands in One Queue

**Artifact status:** Canonical target operational specification

**Bounded context:** Apotek

**Workflow:** `WF-APT-RJ-006`

**Bahasa Indonesia companion:** [SOP APT-RJ-006 — Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrean](./SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-ID.md)

**Subsystem terminology status:** `Pharmacy System` and `Outpatient Pharmacy` are established subsystem terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for coordinating two or more independently accountable medication demands in one outpatient queue and pickup session without merging their records.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Caregiver | Human | Completes payer-specific interactions, presents for one coordinated pickup, receives consolidated education, and accepts applicable medication. |
| Pharmacy Staff | Human | Verifies separate mappings and progress, prepares each cleared Dispense Order separately, coordinates payer readiness, communicates exceptions, and performs one pickup call. |
| Pharmacist | Human | Operationally verifies the recipient, reviews every Prepared Medication, and records Patient Education Acknowledgement for the coordinated session. Recipient verification is not system-enforced. Detailed counseling notes are optional. |
| Pharmacy System | Subsystem | Projects per-demand progress and records separate Sales Invoices, Dispense Orders, dispense, and handover outcomes. |
| Patient Tracker | Subsystem | Retains one Queue Entry with one `CreatedAt`, at most one `ServedAt`, and one `DoneAt`. |
| Cashier or Payment Authority | Human or Subsystem | Supplies Payment Clearance for applicable Patient-payable demands. |
| SEP and Fornas Authorities | Subsystem | Supply coverage evidence for applicable BPJS demands. |
| Inventory | Subsystem | Supplies per-Dispense-Order Mutasi, Remove Stock, and disposition outcomes. |

## 3. Preconditions

1. Participating staff are signed in with their required permissions.
2. One Pharmacy Queue Entry has separate Outpatient Queue Mappings to at least two medication demands.
3. Each Resep has its own Telaah Resep.
4. Each accepted source has its own Sales Order and active primary outpatient Dispense Order.

## 4. Operational Steps

1. **Pharmacy Staff** opens the common Queue Entry in `Apotek Rajal`.
2. **Pharmacy System** displays every mapped demand separately with its source, Sales Order, payer, Sales Order Lines, Sales Invoice, Dispense Authorized evaluation, and Dispense Order progress.
3. **Pharmacy Staff** verifies that no demand, Sales Order, Sales Invoice, or Dispense Order has been merged with another demand.
4. **Pharmacy Staff** applies the General Patient, BPJS, or mixed-coverage SOP to each demand according to its payer classification.
5. **Cashier or Payment Authority** supplies applicable Payment Clearance; **SEP and Fornas Authorities** supply applicable Coverage Clearance.
6. **Pharmacy Staff** prepares each released Dispense Order separately and records completion for each one.
7. On the first applicable preparation start, **Pharmacy System** records `Medication Preparation Started`; **Patient Tracker** records one `ServedAt` and moves the common Queue Entry to In Service.
8. **Pharmacy System** displays every demand intended for pickup as `Prepared` or with an accountable exception outcome.
9. **Pharmacy Staff** reviews all per-demand progress and does not declare an unresolved demand ready.
10. When all included demands are ready or accountably resolved for the intended pickup, **Pharmacy Staff** performs one coordinated pickup call.
11. **Patient Tracker** records one `DoneAt` and makes the common Queue Entry `Done`.
12. **Pharmacy Staff** communicates any accountable exception outcome together with the ready-demand information.
13. With the Patient or caregiver present, **Pharmacist** operationally verifies the recipient, completes Final Dispense Review for each Prepared Medication, and records Patient Education Acknowledgement. For each passed review, **Pharmacy System** appends the review record and displays its Dispense Order as `Reviewed`. **Pharmacy System** records education timestamp and responsible Pharmacist. Detailed counseling notes are optional. The Pharmacist may optionally record recipient phone number and relationship for reference.
14. **Pharmacy Staff** completes the physical handover after Pharmacist authorization. If Pickup Expired, an authorized pharmacist must first record Collection Window Override with reason.
15. **Pharmacy System** records Medication Dispense and Medication Handover against every applicable Dispense Order and Sales Order separately.
16. **Inventory** records separate Remove Stock or return Mutasi outcomes for each originating Dispense Order.

## 5. Operational Exceptions

### 5.1 One demand remains unresolved

- **Pharmacy System** displays that demand as not ready.
- **Pharmacy Staff** defers the coordinated call unless the applicable payer SOP permits and records an accountable partial path accepted by the Patient.

### 5.2 One demand has an accountable exception outcome

- **Pharmacy Staff** includes the resolved exception in the coordinated communication and proceeds with ready demands only when their payer procedures permit.
- **Pharmacy System** retains the exception under its originating Sales Order and Dispense Order.

### 5.3 One Final Dispense Review fails

- **Pharmacist** records the failure reason and affected quantity for the originating Dispense Order and does not authorize its handover.
- **Pharmacy System** appends an immutable review record with the Pharmacist and effective business time and returns only that Dispense Order from `Prepared` to `Preparing`; other demand records remain unchanged.
- **Pharmacy Staff** corrects and prepares the affected medication again. **Pharmacy System** returns it to `Prepared`, and **Pharmacist** performs a new review. The demand remains excluded from handover until that review passes.

### 5.4 One mapping or demand requires correction

- **Pharmacy Staff** corrects only the affected mapping or demand.
- **Pharmacy System** preserves every other demand's history and does not infer that one handover fulfilled another demand.

## 6. Completion Criteria

1. The common Queue Entry displays one `CreatedAt`, at most one `ServedAt`, and one `DoneAt`.
2. Every Resep, Sales Order, Sales Invoice, and Dispense Order retains independent identity and progress.
3. One pickup call is recorded, while each applicable demand has its own Medication Handover fact or accountable exception outcome.

## 7. References

- [Apotek Domain](../apotek-domain.md), especially `BR-APT-011`, `BR-APT-015`, `BR-APT-022`, `BR-APT-030`, `BR-APT-056`–`BR-APT-060`, `BR-APT-084`–`BR-APT-088`, `BR-APT-095`–`BR-APT-096`, and `BR-APT-129`–`BR-APT-134`, and `BR-APT-138`–`BR-APT-142`.
- [Outpatient Apotek Workflow](../outpatient-apotek-workflow.md), `WF-APT-RJ-006`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md), especially `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, and `BR-TRK-045a`.
