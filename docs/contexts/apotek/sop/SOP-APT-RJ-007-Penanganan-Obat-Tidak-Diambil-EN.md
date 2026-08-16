# SOP APT-RJ-007 — Resolve Uncollected Outpatient Medication

**Artifact status:** Canonical target operational specification

**Bounded context:** Apotek

**Workflow:** `WF-APT-RJ-007`

**Bahasa Indonesia companion:** [SOP APT-RJ-007 — Menyelesaikan Obat Rawat Jalan yang Tidak Diambil](./SOP-APT-RJ-007-Penanganan-Obat-Tidak-Diambil-ID.md)

**Subsystem terminology status:** `Pharmacy System` and `Outpatient Pharmacy` are established subsystem terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable manual procedure for giving Prepared or In-Transit medication that was not collected an authorized expiry, Inventory disposition, and payer-specific commercial outcome.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Pharmacy Supervisor | Human | Authorizes and records the manual end of the collection opportunity. This is an authorized pharmacist according to operational policy. No monetary approval threshold applies. |
| Pharmacy Staff | Human | Identifies the affected demand and quantities and verifies the resulting operational outcomes. |
| Pharmacy System | Subsystem | Records No-Show, `Expired`, `Collection Window Expired`, Unfulfilled Medication Outcome, and final Sales Order progress. |
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

1. **Pharmacy Staff** opens the uncollected Queue Entry and verifies the originating Sales Order, Dispense Order, affected quantities, payer allocations, handover absence, and current Inventory disposition.
2. **Pharmacy Supervisor** confirms that the permitted collection opportunity has ended. The Collection Window (default 7 days) classifies Ready for Pickup as Pickup Expired when elapsed; that classification does not expire the Dispense Order. No monetary approval threshold applies.
3. **Pharmacy Supervisor** records the manual uncollected-medication resolution with responsible party, effective business time, affected quantity, and reason `Collection Window Expired`.
4. **Pharmacy System** records the Patient as No-Show for the affected fulfillment and changes each affected Dispense Order to `Expired`.
5. **Pharmacy System** records an Unfulfilled Medication Outcome for every affected quantity and preserves Source Traceability.
6. **Inventory** evaluates the reserved or In-Transit medication and supplies Return to Stock only when eligible, or supplies another final Inventory disposition.
7. **Pharmacy System** displays the authoritative Inventory disposition without inferring stock movement.
8. For an uninvoiced BPJS allocation, **Pharmacy System** keeps the BPJS Sales Invoice absent and resolves only the fulfillment and Inventory consequences.
9. For a paid General Patient allocation, **Pharmacy System** sends the required financial consequence to **Tata Rekening** and keeps the Sales Order `Active`.
10. **Tata Rekening** supplies Credit Note, Refund, or another accountable final commercial outcome; **Pharmacy System** displays the result against the originating allocation.
11. For mixed coverage, **Pharmacy System** records the uninvoiced covered consequence and paid Patient-payable consequence separately.
12. **Patient Tracker** retains the existing Queue Entry state and `DoneAt`; the No-Show resolution does not reopen the queue.
13. **Pharmacy System** changes the Sales Order to `Resolved` only after every accepted quantity, Inventory disposition, and required commercial consequence is final.
14. **Pharmacy Staff** verifies the final state or the explicitly displayed outstanding financial consequence.

## 5. Operational Exceptions

### 5.1 Inventory rejects Return to Stock

- **Inventory** supplies the rejection and another accountable final disposition.
- **Pharmacy System** keeps the Sales Order unresolved until that disposition is displayed.

### 5.2 Paid financial consequence remains outstanding

- **Pharmacy System** displays the Dispense Order as `Expired` and the Sales Order as `Active`.
- **Pharmacy Staff** does not erase or reclassify the paid consequence as an uninvoiced BPJS outcome.
- **Tata Rekening** completes the required financial resolution.

### 5.3 Queue is already `Done`

- **Patient Tracker** retains `DoneAt` unchanged.
- **Pharmacy Supervisor** continues the Apotek resolution without reopening or completing the queue again.

## 6. Completion Criteria

1. Every affected Dispense Order is `Expired` with reason `Collection Window Expired`, responsible party, effective business time, and affected quantity.
2. Every affected quantity has an Unfulfilled Medication Outcome and an authoritative Inventory disposition.
3. No BPJS Sales Invoice exists when BPJS handover did not occur.
4. A paid General Patient Sales Order remains `Active` until the final Credit Note, Refund, or other commercial outcome is visible.
5. The Sales Order is `Resolved` only after all fulfillment and commercial consequences are final.
6. Queue `DoneAt` remains unchanged.

## 7. References

- [Apotek Domain](../apotek-domain.md), especially `BR-APT-018`–`BR-APT-019`, `BR-APT-027`, `BR-APT-045`–`BR-APT-047`, `BR-APT-052`–`BR-APT-060`, `BR-APT-069`, `BR-APT-078`–`BR-APT-080`, `BR-APT-095`, and `BR-APT-135`–`BR-APT-142`.
- [Outpatient Apotek Workflow](../outpatient-apotek-workflow.md), `WF-APT-RJ-007`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md).
- [Tata Rekening Domain](../../../contexts/TataRekening/02-domain.md).
