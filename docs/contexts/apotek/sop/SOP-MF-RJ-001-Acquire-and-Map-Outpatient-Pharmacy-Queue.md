# SOP MF-RJ-001 — Acquire and Map Outpatient Pharmacy Queue

**Artifact status:** Canonical target operational specification

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-001`

**Bahasa Indonesia companion:** [SOP MF-RJ-001 — Mengambil dan Memetakan Antrean Apotek Rawat Jalan](./SOP-MF-RJ-001-Acquire-and-Map-Outpatient-Pharmacy-Queue-ID.md)

**Application terminology status:** `Apotek` and `Apotek Rajal` are established application terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for associating one outpatient Pharmacy Queue Entry with every applicable medication demand through Tracker Mapping or Manual Mapping.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Caregiver | Human | Obtains the Queue Number and presents available tracker, registration, Prescription, or direct-request evidence. |
| Pharmacy Staff | Human | Identifies an unresolved Queue Entry, verifies evidence, records a presented Physical Prescription when applicable, and establishes Manual Mapping. |
| Medication Fulfillment Application | Application | Attempts Tracker Mapping, records Outpatient Queue Mapping, and displays per-demand progress. |
| Patient Tracker | Subsystem | Establishes the Pharmacy Queue Entry, assigns the Queue Number, records `CreatedAt`, and retains queue lifecycle authority. |

## 3. Preconditions

1. Pharmacy Staff is signed in to the `Apotek Rajal` workspace with queue-mapping permission.
2. An outpatient pharmacy Queue Session exists.
3. The Patient directly requests a Queue Number or presents valid tracker or registration evidence.
4. Any medication demand selected for mapping already has an accountable source or is recorded during Manual Mapping.

## 4. Operational Steps

1. **Patient or Caregiver** requests an outpatient pharmacy Queue Number or presents valid tracker or registration evidence.
2. **Patient Tracker** establishes the Pharmacy Queue Entry, assigns the Queue Number, records `CreatedAt`, and displays or supplies the Queue Number.
3. **Medication Fulfillment Application** attempts Tracker Mapping when the supplied evidence resolves one or more applicable medication demands.
4. **Medication Fulfillment Application** records a separate Outpatient Queue Mapping for every resolved demand and displays each mapped demand with its authoritative progress.
5. If the Queue Entry remains unresolved, **Pharmacy Staff** performs an administrative Queue Number call without recording `ServedAt` or `DoneAt`.
6. **Patient or Caregiver** presents the Queue Number and available Prescription, registration, or direct-request evidence.
7. **Pharmacy Staff** selects the unresolved Queue Entry and identifies every applicable existing Prescription or Pharmacy Sales Order.
8. When a Physical Prescription is presented, **Pharmacy Staff** records it before requesting Prescription Review; when a Direct Medication Request is presented, **Pharmacy Staff** evaluates it under the acceptance procedure.
9. **Pharmacy Staff** records Manual Mapping between the Queue Entry and every identified applicable demand.
10. **Medication Fulfillment Application** displays each successful mapping as `Mapped` and retains each demand as a separate record.
11. **Pharmacy Staff** verifies that all applicable demands are visible under the same Queue Entry and leaves their professional and payer processing to the applicable SOP.

## 5. Operational Exceptions

### 5.1 Evidence does not resolve a medication demand

- **Medication Fulfillment Application** leaves the Queue Entry `Unmapped` and blocks mapping-dependent clearance.
- **Pharmacy Staff** obtains additional evidence and retries Manual Mapping; staff does not create an Electronic Prescription.

### 5.2 Direct Medication Request is declined

- **Pharmacy Staff** records no Direct Medication Request and no Pharmacy Sales Order.
- **Patient Tracker** retains the still-Waiting Queue Entry until its external withdrawal policy is applied.

### 5.3 Existing mapping is incorrect

- **Pharmacy Staff** records an accountable mapping correction.
- **Medication Fulfillment Application** preserves the prior mapping, Prescription Review, and Pharmacy Sales Order history.

## 6. Completion Criteria

1. Each applicable demand has a separate `Outpatient Queue Mapped` outcome linked to the same Queue Number; or the Queue Entry visibly remains `Unmapped` pending evidence.
2. Mapping has not recorded `ServedAt` or `DoneAt`.
3. Each mapped demand retains its own Prescription, Pharmacy Sales Order, Sales Invoice, and Dispense Order identity when applicable.

## 7. References

- [Medication Fulfillment Domain](../medication-fulfillment-domain.md), especially `BR-MF-061`–`BR-MF-065`, `BR-MF-082`, and `BR-MF-084`–`BR-MF-087`.
- [Outpatient Medication Fulfillment Workflow](../outpatient-medication-fulfillment-workflow.md), `WF-MF-RJ-001`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md), especially `BR-TRK-026`–`BR-TRK-035`.
