# SOP MF-RJ-007 — Resolve Uncollected Outpatient Medication

**Artifact status:** Canonical target operational specification

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-007`

**Bahasa Indonesia companion:** [SOP MF-RJ-007 — Menyelesaikan Obat Rawat Jalan yang Tidak Diambil](./SOP-MF-RJ-007-Resolve-Uncollected-Outpatient-Medication-ID.md)

**Application terminology status:** `Apotek` and `Apotek Rajal` are established application terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable manual procedure for giving Prepared or In-Transit medication that was not collected an authorized expiry, Inventory disposition, and payer-specific commercial outcome.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Pharmacy Supervisor | Human | Authorizes and records the manual end of the collection opportunity. |
| Pharmacy Staff | Human | Identifies the affected demand and quantities and verifies the resulting operational outcomes. |
| Medication Fulfillment Application | Application | Records No-Show, `Expired`, `Collection Window Expired`, Unfulfilled Medication Outcome, and final Sales Order progress. |
| Inventory | Subsystem | Determines return eligibility and supplies the authoritative return or other final disposition. |
| Tata Rekening | Subsystem | Supplies Credit Note, Refund, or another final commercial outcome for paid medication. |
| Patient Tracker | Subsystem | Retains the existing queue lifecycle and does not reverse `DoneAt`. |

## 3. Preconditions

1. Pharmacy Supervisor and Pharmacy Staff are signed in with the required exception-resolution permission.
2. The medication is `Prepared` or In-Transit, and Medication Handover has not completed.
3. The Patient did not collect the medication.
4. The authorized closing role, affected quantity, reason, and effective business time are known.
5. Sales Invoice presence and financial disposition are visible for each payer allocation.

## 4. Operational Steps

1. **Pharmacy Staff** opens the uncollected Queue Entry and verifies the originating Pharmacy Sales Order, Dispense Order, affected quantities, payer allocations, handover absence, and current Inventory disposition.
2. **Pharmacy Supervisor** confirms that the permitted collection opportunity has ended; no automatic or invented numerical time limit is used.
3. **Pharmacy Supervisor** records the manual uncollected-medication resolution with responsible party, effective business time, affected quantity, and reason `Collection Window Expired`.
4. **Medication Fulfillment Application** records the Patient as No-Show for the affected fulfillment and changes each affected Dispense Order to `Expired`.
5. **Medication Fulfillment Application** records an Unfulfilled Medication Outcome for every affected quantity and preserves Source Traceability.
6. **Inventory** evaluates the reserved or In-Transit medication and supplies Return to Stock only when eligible, or supplies another final Inventory disposition.
7. **Medication Fulfillment Application** displays the authoritative Inventory disposition without inferring stock movement.
8. For an uninvoiced BPJS allocation, **Medication Fulfillment Application** keeps the BPJS Sales Invoice absent and resolves only the fulfillment and Inventory consequences.
9. For a paid General Patient allocation, **Medication Fulfillment Application** sends the required financial consequence to **Tata Rekening** and keeps the Pharmacy Sales Order `Active`.
10. **Tata Rekening** supplies Credit Note, Refund, or another accountable final commercial outcome; **Medication Fulfillment Application** displays the result against the originating allocation.
11. For mixed coverage, **Medication Fulfillment Application** records the uninvoiced covered consequence and paid Patient-payable consequence separately.
12. **Patient Tracker** retains the existing Queue Entry state and `DoneAt`; the No-Show resolution does not reopen the queue.
13. **Medication Fulfillment Application** changes the Pharmacy Sales Order to `Resolved` only after every accepted quantity, Inventory disposition, and required commercial consequence is final.
14. **Pharmacy Staff** verifies the final state or the explicitly displayed outstanding financial consequence.

## 5. Operational Exceptions

### 5.1 Inventory rejects Return to Stock

- **Inventory** supplies the rejection and another accountable final disposition.
- **Medication Fulfillment Application** keeps the Sales Order unresolved until that disposition is displayed.

### 5.2 Paid financial consequence remains outstanding

- **Medication Fulfillment Application** displays the Dispense Order as `Expired` and the Pharmacy Sales Order as `Active`.
- **Pharmacy Staff** does not erase or reclassify the paid consequence as an uninvoiced BPJS outcome.
- **Tata Rekening** completes the required financial resolution.

### 5.3 Queue is already `Done`

- **Patient Tracker** retains `DoneAt` unchanged.
- **Pharmacy Supervisor** continues the Medication Fulfillment resolution without reopening or completing the queue again.

## 6. Completion Criteria

1. Every affected Dispense Order is `Expired` with reason `Collection Window Expired`, responsible party, effective business time, and affected quantity.
2. Every affected quantity has an Unfulfilled Medication Outcome and an authoritative Inventory disposition.
3. No BPJS Sales Invoice exists when BPJS handover did not occur.
4. A paid General Patient Pharmacy Sales Order remains `Active` until the final Credit Note, Refund, or other commercial outcome is visible.
5. The Pharmacy Sales Order is `Resolved` only after all fulfillment and commercial consequences are final.
6. Queue `DoneAt` remains unchanged.

## 7. References

- [Medication Fulfillment Domain](../medication-fulfillment-domain.md), especially `BR-MF-018`–`BR-MF-019`, `BR-MF-027`, `BR-MF-045`–`BR-MF-047`, `BR-MF-052`–`BR-MF-060`, `BR-MF-069`, `BR-MF-078`–`BR-MF-080`, and `BR-MF-095`.
- [Outpatient Medication Fulfillment Workflow](../outpatient-medication-fulfillment-workflow.md), `WF-MF-RJ-007`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md).
- [Tata Rekening Domain](../../../contexts/TataRekening/02-domain.md).
