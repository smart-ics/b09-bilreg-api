# SOP APT-RJ-001 — Acquire and Map Outpatient Pharmacy Queue

**Artifact status:** Canonical target operational specification

**Bounded context:** Apotek

**Workflow:** `WF-APT-RJ-001`

**Bahasa Indonesia companion:** [SOP APT-RJ-001 — Mengambil dan Melakukan Mapping Antrean Apotek Rawat Jalan](./SOP-APT-RJ-001-Antrian-dan-Mapping-ID.md)

**Subsystem terminology status:** `Pharmacy System` and `Outpatient Pharmacy` are established subsystem terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for associating one outpatient Pharmacy Queue Entry with every applicable Resep Kerja or Jual Bebas through Tracker Mapping or Manual Mapping. Mapping shall not target a Sales Order, Invoice, or Dispensing.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Caregiver | Human | Obtains the Queue Number at the kiosk and scans available tracker or registration evidence. |
| Pharmacy Staff | Human | Identifies an unresolved Queue Entry, verifies evidence, records a presented Resep Fisik when applicable, and establishes Manual Mapping. |
| Pharmacy System | Subsystem | Attempts Tracker Mapping to existing Resep, records Outpatient Queue Mapping against the corresponding Resep Kerja, and displays per-source progress. |
| Patient Tracker | Subsystem | Establishes the Pharmacy Queue Entry, assigns the Queue Number, records `CreatedAt`, and retains queue lifecycle authority. |

## 3. Preconditions

1. Pharmacy Staff is signed in to the `Apotek Rajal` workspace with queue-mapping permission.
2. An outpatient pharmacy Queue Session exists.
3. The Patient obtains a Queue Number at the kiosk or scans valid tracker or registration evidence.
4. Any Resep Kerja or Jual Bebas selected for mapping already exists or is recorded during Manual Mapping.

## 4. Operational Steps

1. **Patient or Caregiver** obtains an outpatient pharmacy Queue Number at the kiosk or scans valid tracker or registration evidence.
2. **Patient Tracker** establishes the Pharmacy Queue Entry, assigns the Queue Number, records `CreatedAt`, and displays or supplies the Queue Number.
3. **Pharmacy System** attempts Tracker Mapping when the supplied evidence resolves one or more applicable existing Resep.
4. **Pharmacy System** records a separate Outpatient Queue Mapping against the corresponding Resep Kerja for every resolved Resep and displays each mapped Resep Kerja with its authoritative progress.
5. If the Queue Entry remains unresolved, **Pharmacy Staff** performs an administrative Queue Number call without recording `ServedAt` or `DoneAt`.
6. **Patient or Caregiver** presents the Queue Number and available Resep, registration, or Jual Bebas evidence.
7. **Pharmacy Staff** selects the unresolved Queue Entry and identifies every applicable existing Resep Kerja or Jual Bebas.
8. When a Resep Fisik is presented, **Pharmacy Staff** records it before requesting Telaah Resep; when a Jual Bebas is presented, **Pharmacy Staff** evaluates it under the acceptance procedure.
9. **Pharmacy Staff** records Manual Mapping between the Queue Entry and every identified Resep Kerja or Jual Bebas.
10. **Pharmacy System** displays each successful mapping as `Mapped` and retains each demand as a separate record.
11. **Pharmacy Staff** verifies that all applicable demands are visible under the same Queue Entry and leaves their professional and payer processing to the applicable SOP.

## 5. Operational Exceptions

### 5.1 Evidence does not resolve a Resep

- **Pharmacy System** leaves the Queue Entry `Unmapped` and blocks mapping-dependent clearance.
- **Pharmacy Staff** obtains additional evidence and retries Manual Mapping; staff does not create an Resep Elektronik.
- **Pharmacy Staff** may instead record Pharmacy Queue Close with a mandatory reason. **Patient Tracker** sets the still-Waiting Queue Entry `Withdrawn`. Close does not record `ServedAt` or `DoneAt`.

### 5.2 Jual Bebas is declined

- **Pharmacy Staff** records no Jual Bebas and no Sales Order.
- **Pharmacy Staff** may record Pharmacy Queue Close with a mandatory reason. **Patient Tracker** sets the still-Waiting Queue Entry `Withdrawn`. No additional queue state is used.

### 5.3 Existing mapping is incorrect

- **Pharmacy Staff** selects the correct Resep Kerja or Jual Bebas.
- **Pharmacy System** updates the active Outpatient Queue Mapping to the correct source. No mapping-change history is required.
- Updating the queue mapping does not modify the Resep, Hasil Telaah Resep, or Sales Order because those records remain independently owned.

## 6. Completion Criteria

1. Each applicable Resep Kerja or Jual Bebas has a separate `Outpatient Queue Mapped` outcome linked to the same Queue Number; or the Queue Entry visibly remains `Unmapped` pending evidence; or Pharmacy Queue Close is recorded and the Queue Entry is `Withdrawn`.
2. Mapping has not recorded `ServedAt` or `DoneAt`.
3. Each mapped demand retains its own Resep Kerja or Jual Bebas identity. Downstream Sales Order, Invoice, and Dispensing remain independently owned and are not mapping targets.

## 7. References

- [Apotek Domain](../apotek-domain.md), especially `BR-APT-061`–`BR-APT-065`, `BR-APT-082`, `BR-APT-084`–`BR-APT-087`, and `BR-APT-143`–`BR-APT-145`.
- [Outpatient Apotek Workflow](../outpatient-apotek-workflow.md), `WF-APT-RJ-001`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md), especially `BR-TRK-026`–`BR-TRK-035`, `BR-TRK-039a`, and `BR-TRK-052`.
