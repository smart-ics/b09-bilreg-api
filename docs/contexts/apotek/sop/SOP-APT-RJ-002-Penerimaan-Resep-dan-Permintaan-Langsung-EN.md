# SOP APT-RJ-002 — Accept Outpatient Medication Demand

**Artifact status:** Canonical target operational specification

**Bounded context:** Apotek

**Workflow:** `WF-APT-RJ-002`

**Bahasa Indonesia companion:** [SOP APT-RJ-002 — Menerima Permintaan Obat Rawat Jalan](./SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-ID.md)

**Subsystem terminology status:** `Pharmacy System` and `Outpatient Pharmacy` are established subsystem terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for accepting a reviewed Resep or authorized Jual Bebas and establishing its traceable Sales Order and primary outpatient Dispensing.

### 1.1 Position of Patient Medication Demand in the data flow

`Patient Medication Demand` is an umbrella business concept for a patient-specific medication need. It is not an additional transaction after a Resep. A Patient Medication Demand originates from exactly one of these sources:

- a `Resep`, either a Resep Elektronik or a recorded Resep Fisik; or
- a `Jual Bebas`, when a medication request without a Resep is permitted.

Only an accepted Patient Medication Demand establishes a `Sales Order`. The Sales Order then coordinates two independent downstream paths: the commercial path to a Medication Sale represented by an `Invoice`, and the physical-fulfillment path to a `Dispensing`. Traceability is through Sales Order Items: each Invoice Item and each Dispensing Item references exactly one Sales Order Item.

```text
Resep ───────────────────┐
                         ├─ Patient Medication Demand
Jual Bebas ──────────────┘
                                  ├─ rejected → no Sales Order
                                  └─ accepted → Sales Order
                                                   ├─ Invoice
                                                   │     └── Invoice Item ← Sales Order Item
                                                   └─ Dispensing
                                                         └── Dispensing Item ← Sales Order Item
```

The source Resep or Jual Bebas remains traceable and does not become the Sales Order. Invoice and Dispensing may be established and progress independently according to the applicable payer and fulfillment policies.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Pharmacist | Human | Reviews every Baris Resep, performs any needed clarification with the Dokter Penulis Resep outside the system, establishes the accepted medication, and completes the review. |
| Pharmacy Staff | Human | Records Resep Fisik; accepts or declines Jual Bebas; and, before Sales Order establishment, applies Stock Shortage Handling through a Partial Sales Order of fulfillable items and Salinan Resep. Shortage after Sales Order establishment follows `SOP-APT-RJ-003` exception 5.4 (or the equivalent payer SOP). |
| CPOE | Subsystem | Supplies the authoritative original Resep, which Apotek does not modify. |
| Medication Catalog | Subsystem | Supplies medication identity and formulary information used during review. |
| Pharmacy System | Subsystem | Records review outcomes and establishes a traceable Sales Order and primary Dispensing. |
| Inventory | Subsystem | Supplies Stock Availability and Pharmacy Reserve through Stock Mutasi without deciding professional acceptance. |

## 3. Preconditions

1. The responsible Pharmacy Staff or Pharmacist is signed in to `Apotek Rajal` with the required permission.
2. A Resep Elektronik is available from an authoritative source, a Resep Fisik has been recorded, or a Jual Bebas is presented to authorized Pharmacy Staff.
3. The Resep identifies the Patient and source Clinical Order.
4. Medication Catalog information and applicable professional acceptance policy are available.

## 4. Operational Steps

1. **Pharmacy System** displays the available Resep Elektronik, recorded Resep Fisik, or Jual Bebas without requiring Patient arrival or queue mapping.
2. For a Resep, **Pharmacist** starts Telaah Resep and verifies Patient, source, medication, dosage instruction, quantity, and available clinical information.
3. **Pharmacist** records one disposition for every Baris Resep.
4. When clarification is needed, **Pharmacist** contacts the Dokter Penulis Resep outside the system. The application records neither the request, the response, nor a special clarification state; the review remains `Under Review` until the Pharmacist decides.
5. **Pharmacist** establishes each Baris Resep's final disposition as accepted as prescribed, accepted with a substitute, or rejected. The original Resep and Baris Resep remain unchanged.
6. For an accepted substitute, **Pharmacist** records the substitute medication, reason, affected quantity, and responsible Pharmacist on the Sales Order Item, which retains its reference to the original Baris Resep.
7. **Pharmacist** completes the Telaah Resep as `Approved`, `Partially Approved`, or `Rejected`.
8. For a Jual Bebas, **Pharmacy Staff** records the request details and either accepts or declines it.
9. For an approved or partially approved Resep, or an accepted Jual Bebas, **Pharmacy System** establishes one Sales Order from that source and preserves Source Traceability.
10. **Pharmacy System** may form Invoices with their Invoice Items and Dispensings with their Dispensing Items independently and at different business times. Every medication Invoice Item and every Dispensing Item references exactly one applicable Sales Order Item.
11. **Pharmacy System** establishes one active primary outpatient Dispensing for the normal path within the active Registration and displays its initial state.
12. **Inventory** may record Pharmacy Reserve through Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit; **Pharmacy System** displays that Mutasi without treating it as Dispense Authorized. Dispense Authorized is a separate policy evaluation over financial and/or coverage evidence.
13. **Pharmacist** or **Pharmacy Staff**, according to the source path, verifies the final review outcome, Sales Order identifier, accepted items, and Dispensing identifier.

## 5. Operational Exceptions

### 5.1 No item is accepted or a Jual Bebas is declined

- **Pharmacy System** records `Rejected` for the reviewed Resep or records no Jual Bebas for a declined Jual Bebas.
- **Pharmacy System** establishes no Sales Order.

### 5.2 Stock is insufficient before Sales Order establishment

This exception applies only when the shortage is identified **before** the Sales Order is established (`BR-APT-110`, `WF-APT-RJ-002` alternative flow). It does not apply after a Sales Order exists.

- **Inventory** displays the shortage or discrepancy outcome without altering the Hasil Telaah Resep.
- **Pharmacy Staff** establishes the Sales Order with fulfillable prescription items only. Unfulfillable items remain on the originating Prescription. Those items are not placed on the Sales Order and are not later removed from a Sales Order, because no Sales Order yet contains them.
- **Pharmacy System** supports Salinan Resep for unfulfilled items. The Patient may use the Prescription Copy to obtain medication from another pharmacy.
- **Pharmacy Staff** does not create Backorder, select an alternate stock source, or substitute the medication.

If the Sales Order is already established, do not use this exception. Do not remove items from that Sales Order. Apply `SOP-APT-RJ-003` exception 5.4 (General Patient) or `SOP-APT-RJ-004` exception 5.2 (BPJS): keep the Sales Order, record an Unfulfilled Medication Outcome, and apply financial correction when required (`BR-APT-118`).

### 5.3 Medication replacement is required after Sales Order establishment

- **Pharmacy System** blocks medication-identity changes on the existing Sales Order Item.
- **Pharmacist** cancels the affected line or order under the applicable rules, reviews the same original Resep again, and establishes a new Sales Order Item. The original Resep remains unchanged and no corrected or replacement Resep is required.

## 6. Completion Criteria

1. Every reviewed Baris Resep has a final disposition. An incomplete review remains `Under Review`.
2. An accepted source displays a traceable Sales Order, Sales Order Items, and primary Dispensing whose Dispensing Items reference those Sales Order Items.
3. A rejected Resep or declined Jual Bebas has no Sales Order.
4. Stock evidence has not changed the professional acceptance outcome.
5. A shortage identified before Sales Order establishment produced a Sales Order of fulfillable items only, with unfulfillable items remaining on the Prescription and Salinan Resep supported. A shortage identified after Sales Order establishment was not handled by dropping items from that Sales Order; it follows `SOP-APT-RJ-003` exception 5.4 or the equivalent payer SOP.

## 7. References

- [Apotek Domain](../apotek-domain.md), especially `BR-APT-001`–`BR-APT-019`, `BR-APT-029`–`BR-APT-034`, `BR-APT-050`, `BR-APT-054`, `BR-APT-061`, `BR-APT-068`, `BR-APT-083`, `BR-APT-086`, `BR-APT-089`, and `BR-APT-105`–`BR-APT-118`.
- [Outpatient Apotek Workflow](../outpatient-apotek-workflow.md), `WF-APT-RJ-002`.
- [CPOE Domain](../../../contexts/cpoe/CPOE-DOMAIN.md).
