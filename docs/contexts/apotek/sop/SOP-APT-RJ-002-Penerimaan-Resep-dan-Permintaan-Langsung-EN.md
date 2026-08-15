# SOP APT-RJ-002 — Accept Outpatient Medication Demand

**Artifact status:** Canonical target operational specification

**Bounded context:** Apotek

**Workflow:** `WF-APT-RJ-002`

**Bahasa Indonesia companion:** [SOP APT-RJ-002 — Menerima Permintaan Obat Rawat Jalan](./SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-ID.md)

**Subsystem terminology status:** `Pharmacy System` and `Outpatient Pharmacy` are established subsystem terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for accepting a reviewed Resep or authorized Direct Medication Request and establishing its traceable Sales Order and primary outpatient Dispense Order.

### 1.1 Position of Patient Medication Demand in the data flow

`Patient Medication Demand` is an umbrella business concept for a patient-specific medication need. It is not an additional transaction after a Resep. A Patient Medication Demand originates from exactly one of these sources:

- a `Resep`, either a Resep Elektronik or a recorded Resep Fisik; or
- a `Direct Medication Request`, when a medication request without a Resep is permitted.

Only an accepted Patient Medication Demand establishes a `Sales Order`. The Sales Order then coordinates two independent downstream paths: the commercial path through Billing Allocation to a Medication Sale represented by a `Sales Invoice`, and the physical-fulfillment path through Fulfillment Allocation to a `Dispense Order`.

```text
Resep ───────────────────┐
                         ├─ Patient Medication Demand
Direct Medication Request┘        │
                                  ├─ rejected → no Sales Order
                                  └─ accepted → Sales Order
                                                   ├─ Billing Allocation → Sales Invoice
                                                   └─ Fulfillment Allocation → Dispense Order
```

The source Resep or Direct Medication Request remains traceable and does not become the Sales Order. Sales Invoice and Dispense Order may be established and progress independently according to the applicable payer and fulfillment policies.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Pharmacist | Human | Reviews every Baris Resep, performs any needed clarification with the Dokter Penulis Resep outside the system, establishes the accepted medication, and completes the review. |
| Pharmacy Staff | Human | Records Resep Fisik; accepts or declines Direct Medication Requests; and reviews stock outcomes to choose Backorder or another approved source for the same product when authorized. |
| CPOE | Subsystem | Supplies the authoritative original Resep, which Apotek does not modify. |
| Medication Catalog | Subsystem | Supplies medication identity and formulary information used during review. |
| Pharmacy System | Subsystem | Records review outcomes and establishes traceable allocations, Sales Order, and primary Dispense Order. |
| Inventory | Subsystem | Supplies Stock Availability and Stock Reservation outcomes without deciding professional acceptance. |

## 3. Preconditions

1. The responsible Pharmacy Staff or Pharmacist is signed in to `Apotek Rajal` with the required permission.
2. A Resep Elektronik is available from an authoritative source, a Resep Fisik has been recorded, or a Direct Medication Request is presented to authorized Pharmacy Staff.
3. The Resep identifies the Patient and source Clinical Order.
4. Medication Catalog information and applicable professional acceptance policy are available.

## 4. Operational Steps

1. **Pharmacy System** displays the available Resep Elektronik, recorded Resep Fisik, or Direct Medication Request without requiring Patient arrival or queue mapping.
2. For a Resep, **Pharmacist** starts Telaah Resep and verifies Patient, source, medication, dosage instruction, quantity, and available clinical information.
3. **Pharmacist** records one disposition for every Baris Resep.
4. When clarification is needed, **Pharmacist** contacts the Dokter Penulis Resep outside the system. The application records neither the request, the response, nor a special clarification state; the review remains `Under Review` until the Pharmacist decides.
5. **Pharmacist** establishes each line's final disposition as accepted as prescribed, accepted with a substitute, or rejected. The original Resep and Baris Resep remain unchanged.
6. For an accepted substitute, **Pharmacist** records the substitute medication, reason, affected quantity, and responsible Pharmacist on the Sales Order Line, which retains its reference to the original Baris Resep.
7. **Pharmacist** completes the Telaah Resep as `Approved`, `Partially Approved`, or `Rejected`.
8. For a Direct Medication Request, **Pharmacy Staff** records the request details and either accepts or declines it.
9. For an approved or partially approved Resep, or an accepted Direct Medication Request, **Pharmacy System** establishes one Sales Order from that source and preserves Source Traceability.
10. **Pharmacy System** establishes applicable Billing Allocations and Fulfillment Allocations independently and displays their quantities.
11. **Pharmacy System** establishes one active primary outpatient Dispense Order for the normal episode and displays its initial state.
12. **Inventory** may return Stock Reservation evidence; **Pharmacy System** displays it without treating it as Fulfillment Clearance.
13. **Pharmacist** or **Pharmacy Staff**, according to the source path, verifies the final review outcome, Sales Order identifier, accepted lines, and Dispense Order identifier.

## 5. Operational Exceptions

### 5.1 No line is accepted or a direct request is declined

- **Pharmacy System** records `Rejected` for the reviewed Resep or records no Direct Medication Request for a declined direct request.
- **Pharmacy System** establishes no Sales Order.

### 5.2 Stock is insufficient after acceptance

- **Inventory** displays the shortage or discrepancy outcome without altering the Hasil Telaah Resep.
- **Pharmacy Staff** records Backorder or selects another approved stock source for the same medication product within authority.
- **Pharmacy Staff** does not substitute the medication.

### 5.3 Medication replacement is required after Sales Order establishment

- **Pharmacy System** blocks medication-identity changes on the existing Sales Order Line.
- **Pharmacist** cancels the affected line or order under the applicable rules, reviews the same original Resep again, and establishes a new Sales Order Line. The original Resep remains unchanged and no corrected or replacement Resep is required.

## 6. Completion Criteria

1. Every reviewed Baris Resep has a final disposition. An incomplete review remains `Under Review`.
2. An accepted source displays a traceable Sales Order, Billing Allocation, Fulfillment Allocation, and primary Dispense Order.
3. A rejected Resep or declined Direct Medication Request has no Sales Order.
4. Stock evidence has not changed the professional acceptance outcome.

## 7. References

- [Apotek Domain](../apotek-domain.md), especially `BR-APT-001`–`BR-APT-019`, `BR-APT-029`–`BR-APT-034`, `BR-APT-050`, `BR-APT-061`, `BR-APT-068`, `BR-APT-083`, `BR-APT-086`, and `BR-APT-089`.
- [Outpatient Apotek Workflow](../outpatient-apotek-workflow.md), `WF-APT-RJ-002`.
- [CPOE Domain](../../../contexts/cpoe/CPOE-DOMAIN.md).
