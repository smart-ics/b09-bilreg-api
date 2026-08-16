# ALN-003 Resolution Report

**Issue:** ALN-003 — Remove Legacy Allocation Model  
**Status:** Resolved  
**Date:** 2026-08-16  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Scope:** Documentation alignment only. No new business concept. No replacement allocation object. No business-rule or workflow-sequence change.

**Authoritative model:** `apotek-domain.md` (`BR-APT-015`, `BR-APT-021`, `BR-APT-029`; §5.2–§5.4, §6.2–§6.5) and `outpatient-apotek-workflow.md` `WF-APT-RJ-002` steps 7–8.

---

## Locked model applied

Billing Allocation and Fulfillment Allocation are retired. They are not aggregates, entities, records, projections, workflow steps, or business objects.

Approved structure:

```text
Sales Order
    → Sales Invoice
    → Dispense Order
```

Traceability (canonical ubiquitous language):

```text
Commercial flow
  Sales Order Line → Sales Invoice Item

Fulfillment flow
  Sales Order Line → Dispense Order Line
```

The prompt’s phrase “Sales Invoice Line” is the same commercial line as the domain term **Sales Invoice Item**. No new “Sales Invoice Line” object was introduced.

---

## Files modified

| File | Change |
|---|---|
| `sop/SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md` | Diagram, actor, step 10, completion criterion 2 |
| `sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md` | Actor, preconditions 2 and 4, steps 1 and 5, exception 5.1 |
| `sop/SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-EN.md` | Precondition 2, steps 1 and 13 |
| `sop/SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-EN.md` | Actor, step 2 |
| `sop/SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-ID.md` | Companion diagram, step 10, completion 2 |
| `sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-ID.md` | Companion preconditions, steps 1 and 5, exception 5.1 |
| `sop/SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-ID.md` | Companion precondition 2, steps 1 and 13 |
| `sop/SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-ID.md` | Companion actor, step 2 |
| `outpatient-apotek-screen-and-aggregate-design.md` | §3.2 “payer allocations” → payer classification of Sales Order Lines |
| `APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md` | ALN-003 marked resolved |
| `docs/ARTIFACTS.md` | Indexed this report |

Unchanged (already aligned): `apotek-domain.md`, `outpatient-apotek-workflow.md`.

---

## Sections modified

| Artifact | Sections |
|---|---|
| SOP-002 EN | §1.1 data-flow text and diagram; §2 Pharmacy System; §4 step 10; §6 criterion 2 |
| SOP-003 EN | §2 Pharmacy System; §3 items 2 and 4; §4 steps 1 and 5; §5.1 |
| SOP-004 EN | §3 item 2; §4 steps 1 and 13 |
| SOP-006 EN | §2 Pharmacy System; §4 step 2 |
| Screen design | §3.2 workbench “Manage sale” payer wording |
| SOP-002/003/004/006 ID | Matching companion sections |

SOP-002 step 11 (primary outpatient Dispense Order) is unchanged.

---

## Removed Allocation references

| Location (before) | After |
|---|---|
| SOP-002 §1.1 “through Billing Allocation … through Fulfillment Allocation” | Independent Sales Invoice and Dispense Order paths; line references only |
| SOP-002 diagram `Billing Allocation → Sales Invoice` / `Fulfillment Allocation → Dispense Order` | `Sales Invoice Item ← Sales Order Line` / `Dispense Order Line ← Sales Order Line` |
| SOP-002 actor “traceable allocations” | traceable Sales Order and primary Dispense Order |
| SOP-002 step 10 “establishes applicable Billing Allocations and Fulfillment Allocations” | May form Sales Invoice Items and Dispense Order Lines independently (`WF-APT-RJ-002` step 7) |
| SOP-002 completion “Sales Order, Billing Allocation, Fulfillment Allocation, and primary Dispense Order” | Sales Order, Sales Order Lines, and primary Dispense Order with Dispense Order Lines |
| SOP-003 actor “Displays allocations” | Displays Sales Order Line amounts |
| SOP-003 precondition “Patient-payable Billing Allocations” | Patient-payable Sales Order Lines |
| SOP-003 precondition “established from Fulfillment Allocations” | established from Sales Order Lines through Dispense Order Lines |
| SOP-003 step 1 “amount and Billing Allocations” | amount from applicable Sales Order Lines |
| SOP-003 step 5 “from the confirmed Billing Allocations” | Sales Invoice and Sales Invoice Items from confirmed Sales Order Line quantities |
| SOP-003 5.1 “records the allocation as declined” | Sales Order Line quantity declined or commercially unallocated (domain outcome, not an allocation record) |
| SOP-004 precondition/step 1/step 13 “covered Billing Allocations” | covered Sales Order Lines / Sales Invoice Items from those lines |
| SOP-006 actor “separate allocations” | separate Sales Invoices and Dispense Orders |
| SOP-006 step 2 Billing Allocation, Fulfillment Allocation | Sales Order Lines, Sales Invoice, Dispense Authorized evaluation, Dispense Order |
| Screen §3.2 “payer allocations” | payer classification of Sales Order Lines |

Indonesian SOP-002/003/004/006 companions no longer describe “bagian tagihan / kesiapan pelayanan” as separate allocation records.

SOP-007 still says “payer allocation” in uncollected-resolution wording. That is not the retired Billing Allocation / Fulfillment Allocation object and was outside the ALN-003 file list.

---

## Evidence of alignment with Sales Order / Sales Invoice / Dispense Order

| Canonical statement | SOP evidence after alignment |
|---|---|
| `BR-APT-015` — Sales Invoice and Dispense Order may form independently | SOP-002 step 10 copies `WF-APT-RJ-002` step 7 |
| `BR-APT-021` — Sales Invoice Item originates from one Sales Order Line | SOP-002 diagram; SOP-003 step 5; SOP-004 step 13 |
| `BR-APT-029` — Dispense Order Line references one Sales Order Line | SOP-002 diagram and completion 2; SOP-003 precondition 4 |
| Domain §6.5 — correlation through line references, not a third object | SOP-006 step 2 lists Sales Order Lines, Sales Invoice, Dispense Order; no allocation row |
| `WF-APT-RJ-003` — invoice from confirmed Sales Order Line quantities | SOP-003 step 5 |
| `WF-APT-RJ-004` — BPJS invoice from covered Sales Order Line quantities at handover | SOP-004 step 13 |

No Billing Allocation or Fulfillment Allocation name remains in operational Apotek SOPs 002–006 (EN/ID) or in the screen-and-aggregate design.

---

## Confirmation: no business-rule or workflow-behavior change

This resolution did **not**:

- add an allocation aggregate, entity, table, or projection;
- change when a Sales Invoice is established (General Patient after confirmation; BPJS at successful handover);
- change when a primary Dispense Order is established (SOP-002 step 11 unchanged);
- change independent invoice vs dispense timing;
- change Dispense Authorized evaluation;
- change mixed-coverage independent Sales Orders or coordinated pickup;
- change decline / commercially unallocated *outcome* on a Sales Order Line (only the “allocation record” wording).

ALN-004 through ALN-007 remain open.
