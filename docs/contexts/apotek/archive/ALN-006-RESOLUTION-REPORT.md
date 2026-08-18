# ALN-006 Resolution Report

**Issue:** ALN-006 — Stock Shortage Timing Alignment  
**Status:** Resolved  
**Date:** 2026-08-16  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Scope:** Artifact alignment only. No stock-shortage workflow redesign. No new shortage reason.

**Authoritative source (locked):** `../working/outpatient-apotek-repository-gap-analysis-report.md` — BC-10, applied through `BR-APT-110` and `BR-APT-118`.

BC-10 was not re-analyzed, reinterpreted, replaced, or redesigned.

---

## Locked decision applied (BC-10)

Outpatient Pharmacy does not support Backorder. A stock shortage does not create an outstanding fulfillment obligation, waiting demand, or backorder record.

Two shortage timings exist. The cut is **Sales Order establishment**, not “acceptance”:

| Timing | Action |
|---|---|
| **Before Sales Order establishment** (`BR-APT-110`) | Establish a Sales Order only for fulfillable items. Unfulfillable items remain on the Prescription. Salinan Resep may be issued. |
| **After Sales Order establishment** (`BR-APT-118`) | Do not modify the Sales Order. Do not remove items from that Sales Order. Record an Unfulfilled Medication Outcome. Apply financial correction or refund when commercial consequences exist. |

The canonical domain and English `WF-APT-RJ-002` already matched this split. SOP-002 exception 5.2 did not.

---

## Files modified

| File | Change |
|---|---|
| `sop/SOP-APT-RJ-002-*-EN.md` and `-ID.md` | Exception 5.2 limited to shortage **before** Sales Order establishment; cross-reference SOP-003 5.4 / SOP-004 5.2; actor and completion criterion 5 distinguish the two timings |
| `sop/SOP-APT-RJ-003-*-EN.md` and `-ID.md` | Exception 5.4 titled and scoped **after** Sales Order establishment; no line removal; financial correction; reverse cross-reference to SOP-002 5.2; completion criterion 6 |
| `sop/SOP-APT-RJ-004-*-EN.md` and `-ID.md` | Exception 5.2 same post-establishment rule; reverse cross-reference to SOP-002 5.2; completion criterion 6 |
| `APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md` | ALN-006 marked resolved; SOP area marked Aligned; remaining-defect counts updated |
| `docs/ARTIFACTS.md` | Indexed this report (and restored the ALN-004 index row) |

Unchanged (already BC-10-aligned): `apotek-domain.md` `BR-APT-110` / `BR-APT-118`; `outpatient-apotek-workflow.md` `WF-APT-RJ-002` alternative vs exception flows; Gap Analysis BC-10 body.

---

## Sections modified

| Artifact | Section | Modification |
|---|---|---|
| SOP-002 | Actors — Pharmacy Staff | Partial Sales Order / Salinan Resep applies **before** Sales Order establishment; after establishment follows SOP-003 5.4 or equivalent payer SOP |
| SOP-002 | Exception 5.2 | Retitled from “after acceptance” to “before Sales Order establishment”. Partial Sales Order of fulfillable items only. Explicit: do not use this path to drop lines from an existing Sales Order. Pointer to SOP-003 5.4 / SOP-004 5.2 / `BR-APT-118` |
| SOP-002 | Completion criterion 5 | Records the before/after cut |
| SOP-003 | Exception 5.4 | Retitled “Shortage after Sales Order establishment”. Keep Sales Order; Unfulfilled Medication Outcome; financial correction when required; pointer to SOP-002 5.2 |
| SOP-003 | Completion criterion 6 | Established Sales Order is not stripped of lines |
| SOP-004 | Exception 5.2 | Same post-establishment rule and pointer to SOP-002 5.2 |
| SOP-004 | Completion criterion 6 | Same as SOP-003 |

---

## Alignment changes made

| Before | After |
|---|---|
| SOP-002 5.2 titled “Stock is insufficient after acceptance” | Titled and scoped “before Sales Order establishment” |
| Operator could read 5.2 as dropping lines from an existing Sales Order | 5.2 forbids that reading and sends post-establishment shortage to SOP-003 5.4 / SOP-004 5.2 |
| SOP-003 5.4 titled only “Shortage” | Titled and scoped after Sales Order establishment; does not remove Sales Order items |
| SOP-004 5.2 already said “after Sales Order establishment” but did not forbid line removal | Explicit: do not remove items; do not rebuild as a partial order |

No new shortage reason was added. Patient Request partial prescription and Fornas Not Covered remain separate `WF-APT-RJ-002` alternative flows.

---

## Evidence that SOPs now match BC-10

| BC-10 / domain statement | SOP evidence after alignment |
|---|---|
| No Backorder; no outstanding outpatient demand | SOP-002 5.2, SOP-003 5.4, SOP-004 5.2: no Backorder, no alternate stock source |
| Before Sales Order: only fulfillable items on the Sales Order; unfulfillable items remain on the Prescription; Salinan Resep | SOP-002 5.2; `BR-APT-110`; `WF-APT-RJ-002` alternative “Stock Shortage partial prescription” (unchanged) |
| After Sales Order: do not modify / do not remove items; Unfulfilled Medication Outcome; financial correction when required | SOP-003 5.4; SOP-004 5.2; `BR-APT-118`; `WF-APT-RJ-002` exception “Stock shortage after Sales Order establishment” (unchanged) |
| Cut is Sales Order establishment, not “acceptance” | SOP-002 5.2 title and opening sentence; SOP-003 5.4 / SOP-004 5.2 opening sentences |

Worked path before Sales Order establishment:

1. Shortage identified.
2. Sales Order is established with fulfillable items only.
3. Unfulfillable items remain on the Prescription.
4. Salinan Resep may be issued.
5. SOP-002 5.2 applies. SOP-003 5.4 does not.

Worked path after Sales Order establishment:

1. Sales Order already exists.
2. Shortage identified (including after payment or financial clearance).
3. Sales Order items are not removed.
4. Unfulfilled Medication Outcome is recorded; Salinan Resep when applicable.
5. Commercial consequences follow `BR-APT-027` (Invoice revision while Tata Rekening still permits modification; otherwise Tata Rekening Credit Note / Refund / Financial Adjustment). Apotek does not persist Credit Note.
6. SOP-003 5.4 or SOP-004 5.2 applies. SOP-002 5.2 does not.

---

## Confirmation: no business rule was changed

This resolution did **not**:

- reopen or rewrite BC-10;
- change `BR-APT-110`, `BR-APT-118`, `BR-APT-046`, or `BR-APT-114`–`BR-APT-117`;
- change `WF-APT-RJ-002` alternative vs exception split;
- add a shortage reason, Backorder, or alternate-stock path;
- redesign the stock-shortage workflow.

SOP wording was synchronized to the already-ratified domain and workflow cut at Sales Order establishment.
