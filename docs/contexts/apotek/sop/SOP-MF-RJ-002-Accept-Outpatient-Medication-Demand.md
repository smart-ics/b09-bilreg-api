# SOP MF-RJ-002 — Accept Outpatient Medication Demand

**Artifact status:** Canonical target operational specification

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-002`

**Bahasa Indonesia companion:** [SOP MF-RJ-002 — Menerima Permintaan Obat Rawat Jalan](./SOP-MF-RJ-002-Accept-Outpatient-Medication-Demand-ID.md)

**Application terminology status:** `Apotek` and `Apotek Rajal` are established application terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for accepting a reviewed Prescription or authorized Direct Medication Request and establishing its traceable Pharmacy Sales Order and primary outpatient Dispense Order.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Pharmacist | Human | Reviews every Prescription Line, requests clarification, authorizes eligible substitution before Sales Order establishment, and completes the review. |
| Pharmacy Staff | Human | Records Physical Prescriptions and accepts, refers, or declines Direct Medication Requests within assigned authority. |
| Pharmacy Technician | Human | Reviews external stock outcomes and chooses Backorder or another approved source for the same product after acceptance when authorized. |
| CPOE or Prescribing Clinician | Subsystem or Human | Supplies the authoritative Prescription and responds to Clinical Clarification or provides corrected intent. |
| Medication Catalog | Subsystem | Supplies medication identity and formulary information used during review. |
| Medication Fulfillment Application | Application | Records review outcomes and establishes traceable allocations, Pharmacy Sales Order, and primary Dispense Order. |
| Inventory | Subsystem | Supplies Stock Availability and Stock Reservation outcomes without deciding professional acceptance. |

## 3. Preconditions

1. The responsible Pharmacy Staff or Pharmacist is signed in to `Apotek Rajal` with the required permission.
2. An Electronic Prescription is available from an authoritative source, a Physical Prescription has been recorded, or a Direct Medication Request is presented to authorized Pharmacy Staff.
3. The Prescription identifies the Patient and source Clinical Order.
4. Medication Catalog information and applicable professional acceptance policy are available.

## 4. Operational Steps

1. **Medication Fulfillment Application** displays the available Electronic Prescription, recorded Physical Prescription, or Direct Medication Request without requiring Patient arrival or queue mapping.
2. For a Prescription, **Pharmacist** starts Prescription Review and verifies Patient, source, medication, dosage instruction, quantity, and available clinical information.
3. **Pharmacist** records one disposition for every Prescription Line.
4. When clarification is required, **Pharmacist** records `Clarification Required` and sends the accountable clarification request to **CPOE or Prescribing Clinician**.
5. **CPOE or Prescribing Clinician** supplies clarification or a corrected or replacement Prescription; **Pharmacist** resumes the review against that authoritative information.
6. When substitution is professionally authorized, **Pharmacist** records the original medication, accepted substitute, reason, affected quantity, and responsible Pharmacist before Pharmacy Sales Order establishment.
7. **Pharmacist** completes the Prescription Review as `Approved`, `Partially Approved`, or `Rejected`.
8. For a Direct Medication Request, **Pharmacy Staff** records the request details and either accepts it within authority, refers it to **Pharmacist**, or declines it.
9. When referred, **Pharmacist** records approval or decline; **Medication Fulfillment Application** permits acceptance only after approval.
10. For an approved or partially approved Prescription, or an accepted Direct Medication Request, **Medication Fulfillment Application** establishes one Pharmacy Sales Order from that source and preserves Source Traceability.
11. **Medication Fulfillment Application** establishes applicable Billing Allocations and Fulfillment Allocations independently and displays their quantities.
12. **Medication Fulfillment Application** establishes one active primary outpatient Dispense Order for the normal episode and displays its initial state.
13. **Inventory** may return Stock Reservation evidence; **Medication Fulfillment Application** displays it without treating it as Fulfillment Clearance.
14. **Pharmacist** or **Pharmacy Staff**, according to the source path, verifies the final review outcome, Pharmacy Sales Order identifier, accepted lines, and Dispense Order identifier.

## 5. Operational Exceptions

### 5.1 No line is accepted or a direct request is declined

- **Medication Fulfillment Application** records `Rejected` for the reviewed Prescription or records no Direct Medication Request for a declined direct request.
- **Medication Fulfillment Application** establishes no Pharmacy Sales Order.

### 5.2 Stock is insufficient after acceptance

- **Inventory** displays the shortage or discrepancy outcome without altering the Prescription Review Outcome.
- **Pharmacy Technician** records Backorder or selects another approved stock source for the same medication product within authority.
- **Pharmacy Technician** does not substitute the medication.

### 5.3 Medication replacement is required after Sales Order establishment

- **Medication Fulfillment Application** blocks substitution on the existing Sales Order Line.
- **Pharmacist** requests a corrected or replacement Prescription and performs a new Prescription Review.

## 6. Completion Criteria

1. Every reviewed Prescription Line has a final disposition, or the review visibly remains `Clarification Required`.
2. An accepted source displays a traceable Pharmacy Sales Order, Billing Allocation, Fulfillment Allocation, and primary Dispense Order.
3. A rejected Prescription or declined Direct Medication Request has no Pharmacy Sales Order.
4. Stock evidence has not changed the professional acceptance outcome.

## 7. References

- [Medication Fulfillment Domain](../medication-fulfillment-domain.md), especially `BR-MF-001`–`BR-MF-019`, `BR-MF-029`–`BR-MF-034`, `BR-MF-050`, `BR-MF-061`, `BR-MF-068`, `BR-MF-083`, `BR-MF-086`, and `BR-MF-089`.
- [Outpatient Medication Fulfillment Workflow](../outpatient-medication-fulfillment-workflow.md), `WF-MF-RJ-002`.
- [CPOE Domain](../../../contexts/cpoe/CPOE-DOMAIN.md).
