# ALN-002 Resolution Report

**Issue:** ALN-002 — Fulfillment Clearance → Dispense Authorized Alignment  
**Status:** Resolved  
**Date:** 2026-08-16  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Scope:** Document alignment only. No architecture redesign. No business-behavior or workflow-sequence change beyond applying locked BA-08 terminology and ownership.

**Authoritative source (locked):** `../working/outpatient-apotek-repository-gap-analysis-report.md` — BA-08

BA-08 was not re-analyzed, reinterpreted, replaced, or redesigned.

**Reference pattern:** `SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-EN.md`; `outpatient-apotek-workflow.md`; `apotek-domain.md` (`Dispense Authorized`, `BR-APT-040`, `BR-APT-043`).

---

## Locked decision applied (BA-08)

There is no separate business concept called Financial Clearance or Fulfillment Clearance. Pharmacy policy evaluates financial and coverage evidence to determine **Dispense Authorized** — whether medication preparation and dispensing may start.

Dispense Authorized:

- is a policy evaluation result derived from financial and/or coverage evidence;
- is not an aggregate;
- is not an entity;
- is not a persisted business object;
- is not a source of truth;
- is not a transaction boundary;
- is required before Medication Preparation Started and Dispensing;
- is not required for Medication Handover.

Typical evidence (unchanged from BA-08): General Patient — Invoice created and payment completed; BPJS — prescription exists, SEP valid, Fornas coverage valid; other insurance — coverage approval valid.

---

## Files modified

| File | Role in ALN-002 |
|---|---|
| `outpatient-apotek-screen-and-aggregate-design.md` | Retired `Fulfillment Clearance` object name; use `Dispense Authorized` evaluation. |
| `sop/SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md` | Step 12 no longer names Fulfillment Clearance. |
| `sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md` | Actors table and step 7 evaluate Dispense Authorized; do not establish a clearance object. |
| `sop/SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-EN.md` | Actors table and step 3 evaluate Dispense Authorized from coverage evidence. |
| `apotek-domain-id.md` | Indonesian companion aligned to English domain BA-08 wording. |
| `outpatient-apotek-workflow-id.md` | Indonesian companion aligned to English workflow evaluation language. |
| `sop/DAFTAR-SOP-APT-RJ.md` | Glossary term replaced. |
| `APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md` | ALN-002 marked resolved. |
| `docs/ARTIFACTS.md` | Indexed this report. |

Unchanged (already aligned): `apotek-domain.md`, `outpatient-apotek-workflow.md`, `SOP-APT-RJ-005-*-EN.md`, BA-08 text in the Gap Analysis (historical mention of the retired name is the decision record).

---

## Sections modified

| Artifact | Sections |
|---|---|
| Screen design | §2 principle 8; §3.3 Dispensing workbench item 1; §5.1 aggregate map; §5.3 retitled to Dispense Authorized; §5.4 invariant 10; §5.6 matrix row |
| SOP-002 EN | §4 step 12 |
| SOP-003 EN | §2 Pharmacy System actor; §4 step 7 |
| SOP-004 EN | §2 Pharmacy System actor; §4 step 3 |
| Domain ID | Ubiquitous language; §3.6; §5.5; §6.5; §7.5 `BR-APT-040`/`043`/`044`; `BR-APT-068`/`072`/`074`; event catalog |
| Workflow ID | §3.3; §5.3–§5.4; WF-APT-RJ-001 exception; WF-APT-RJ-002 step 9; WF-APT-RJ-003 step 5 and events; WF-APT-RJ-004 step 2 and events; §8 payment integration; §9 timing table |
| SOP daftar | Pedoman Istilah row |

SOP-002 step 10 (`Billing Allocation` / `Fulfillment Allocation`) was not rewritten; that leftover is ALN-003.

---

## All removed Fulfillment Clearance references

Operational artifacts no longer use the retired name except as Gap Analysis / this report history.

| Location (before) | Replacement |
|---|---|
| Screen §2 principle 8 | Dispense Authorized as policy evaluation result; not aggregate, entity, persisted object, source of truth, or transaction boundary |
| Screen §3.3 “view applicable Fulfillment Clearance” | “view applicable Dispense Authorized evaluation” |
| Screen §5.1 node `Fulfillment Clearance` | Node `Dispense Authorized` (policy evaluation; dotted, not an aggregate) |
| Screen §5.3 titled Fulfillment Clearance | §5.3 Dispense Authorized (BA-08); evidence table; not required for handover |
| Screen §5.4 item 10 | Dispense Authorized authorizes preparation quantity |
| Screen §5.6 “Fulfillment Clearance clarification” | “Dispense Authorized (BA-08)” — none added as aggregate |
| SOP-002 step 12 “without treating it as Fulfillment Clearance” | without treating stock evidence as Dispense Authorized; Dispense Authorized is a separate policy evaluation |
| SOP-003 “records the invoice and clearances” | displays Payment Clearance and evaluates Dispense Authorized |
| SOP-003 step 7 “establishes Fulfillment Clearance” | evaluates financial and coverage evidence as Dispense Authorized; not a persisted object |
| SOP-004 “Records Coverage and Fulfillment Clearance” / “atomically records” | displays Coverage Clearance, evaluates Dispense Authorized, records BPJS invoice and handover (same accountable outcome already in step 13; no distributed-transaction wording) |
| SOP-004 step 3 “establishes the corresponding Fulfillment Clearance” | evaluates Dispense Authorized without requiring an Invoice; not a persisted object |
| Domain ID glossary, §3.6, §5.5 object, §6.5, BR-APT-040/043/044/068/072/074, event `Fulfillment Clearance Established` | Dispense Authorized / `Dispense Authorized Evaluated`, matching English domain |
| Workflow ID scope, preconditions, exceptions, WF-003/004 “membentuk Fulfillment Clearance”, events, payment integration, timing table | evaluate Dispense Authorized, matching English workflow |
| DAFTAR-SOP “Fulfillment Clearance \| Izin Penyiapan Obat” | Dispense Authorized \| Dispense Authorized |

---

## Evidence that artifacts now match BA-08

| BA-08 statement | Evidence after alignment |
|---|---|
| No business concept named Fulfillment Clearance | Retired name gone from screen design, listed EN SOPs, ID domain/workflow, SOP glossary. Remaining mentions are Gap Analysis decision text and this/ALN review history. |
| Dispense Authorized is policy evaluation from financial/coverage evidence | Screen §2.8, §5.3; SOP-003 step 7; SOP-004 step 3; SOP-005 pattern unchanged; workflow EN already used “evaluates … as Dispense Authorized”. |
| Not an aggregate, entity, source of truth, or transaction boundary | Screen §2.8, §5.1, §5.3, §5.6; SOP-003/004 “not established as a persisted business object”; domain EN `BR-APT-043`; domain ID `BR-APT-043`. |
| Required before preparation; not required for handover | Screen §5.3; handover gates unchanged (Prepared, Final Dispense Review, Patient Education Acknowledgement). |
| General / BPJS evidence paths | Screen §5.3 table copies BA-08 payer evidence. SOP-003 still uses Payment Clearance then evaluation. SOP-004 still uses Coverage Clearance then evaluation without a prior Invoice. |
| SOP-005 as pattern | SOP-003/004 now use “evaluates Dispense Authorized” like SOP-005 step 9. |

English domain and English workflow were already BA-08-aligned and were not rewritten.

---

## Confirmation: alignment only

This resolution did **not**:

- change BA-08 or payer evidence rules;
- add a Dispense Authorized aggregate, table, or transaction;
- change when General Patient preparation may start (Payment Clearance and invoice evidence);
- change when BPJS preparation may start (SEP, Fornas coverage, no prior Invoice);
- change mixed-coverage independent authorization (`WF-APT-RJ-005` / SOP-005);
- change handover gates or BPJS invoice-at-handover sequencing;
- resolve ALN-003 (Billing/Fulfillment Allocation) or ALN-004 (Stock Reservation / Inventory Issue).

SOP-004 actor text no longer says the system “atomically” records invoice and handover. That wording implied a special transaction boundary BA-08 already rejected. Step 13 still records BPJS Invoice, Medication Dispense, and Medication Handover as one accountable business outcome. Sequence and gates are unchanged.

ALN-003 through ALN-007 remain open.
