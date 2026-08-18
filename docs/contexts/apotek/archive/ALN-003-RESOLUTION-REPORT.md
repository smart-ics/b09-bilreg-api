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
    → Invoice
    → Dispensing
```

Traceability (canonical ubiquitous language):

```text
Commercial flow
  Sales Order Item → Invoice Item

Fulfillment flow
  Sales Order Item → Dispensing Item
```

The prompt’s phrase “Sales Invoice Line” is the same commercial line as the domain term **Invoice Item**. No new “Sales Invoice Line” object was introduced.

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
| `outpatient-apotek-screen-and-aggregate-design.md` | §3.2 “payer allocations” → payer classification of Sales Order Items |
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

SOP-002 step 11 (primary outpatient Dispensing) is unchanged.

---

## Removed Allocation references

| Location (before) | After |
|---|---|
| SOP-002 §1.1 “through Billing Allocation … through Fulfillment Allocation” | Independent Invoice and Dispensing paths; line references only |
| SOP-002 diagram `Billing Allocation → Invoice` / `Fulfillment Allocation → Dispensing` | `Invoice Item ← Sales Order Item` / `Dispensing Item ← Sales Order Item` |
| SOP-002 actor “traceable allocations” | traceable Sales Order and primary Dispensing |
| SOP-002 step 10 “establishes applicable Billing Allocations and Fulfillment Allocations” | May form Invoice Items and Dispensing Items independently (`WF-APT-RJ-002` step 7) |
| SOP-002 completion “Sales Order, Billing Allocation, Fulfillment Allocation, and primary Dispensing” | Sales Order, Sales Order Items, and primary Dispensing with Dispensing Items |
| SOP-003 actor “Displays allocations” | Displays Sales Order Item amounts |
| SOP-003 precondition “Patient-payable Billing Allocations” | Patient-payable Sales Order Items |
| SOP-003 precondition “established from Fulfillment Allocations” | established from Sales Order Items through Dispensing Items |
| SOP-003 step 1 “amount and Billing Allocations” | amount from applicable Sales Order Items |
| SOP-003 step 5 “from the confirmed Billing Allocations” | Invoice and Invoice Items from confirmed Sales Order Item quantities |
| SOP-003 5.1 “records the allocation as declined” | Sales Order Item quantity declined or commercially unallocated (domain outcome, not an allocation record) |
| SOP-004 precondition/step 1/step 13 “covered Billing Allocations” | covered Sales Order Items / Invoice Items from those lines |
| SOP-006 actor “separate allocations” | separate Invoices and Dispensings |
| SOP-006 step 2 Billing Allocation, Fulfillment Allocation | Sales Order Items, Invoice, Dispense Authorized evaluation, Dispensing |
| Screen §3.2 “payer allocations” | payer classification of Sales Order Items |

Indonesian SOP-002/003/004/006 companions no longer describe “bagian tagihan / kesiapan pelayanan” as separate allocation records.

SOP-007 still says “payer allocation” in uncollected-resolution wording. That is not the retired Billing Allocation / Fulfillment Allocation object and was outside the ALN-003 file list.

---

## Evidence of alignment with Sales Order / Invoice / Dispensing

| Canonical statement | SOP evidence after alignment |
|---|---|
| `BR-APT-015` — Invoice and Dispensing may form independently | SOP-002 step 10 copies `WF-APT-RJ-002` step 7 |
| `BR-APT-021` — Invoice Item originates from one Sales Order Item | SOP-002 diagram; SOP-003 step 5; SOP-004 step 13 |
| `BR-APT-029` — Dispensing Item references one Sales Order Item | SOP-002 diagram and completion 2; SOP-003 precondition 4 |
| Domain §6.5 — correlation through line references, not a third object | SOP-006 step 2 lists Sales Order Items, Invoice, Dispensing; no allocation row |
| `WF-APT-RJ-003` — invoice from confirmed Sales Order Item quantities | SOP-003 step 5 |
| `WF-APT-RJ-004` — BPJS invoice from covered Sales Order Item quantities at handover | SOP-004 step 13 |

No Billing Allocation or Fulfillment Allocation name remains in operational Apotek SOPs 002–006 (EN/ID) or in the screen-and-aggregate design.

---

## Confirmation: no business-rule or workflow-behavior change

This resolution did **not**:

- add an allocation aggregate, entity, table, or projection;
- change when an Invoice is established (General Patient after confirmation; BPJS at successful handover);
- change when a primary Dispensing is established (SOP-002 step 11 unchanged);
- change independent invoice vs dispense timing;
- change Dispense Authorized evaluation;
- change mixed-coverage independent Sales Orders or coordinated pickup;
- change decline / commercially unallocated *outcome* on a Sales Order Item (only the “allocation record” wording).

ALN-004 through ALN-007 remain open.
