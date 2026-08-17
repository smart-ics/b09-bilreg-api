# ALN-004 Resolution Report

**Issue:** ALN-004 — Inventory Contract Alignment  
**Status:** Resolved  
**Date:** 2026-08-16  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Scope:** Artifact alignment only. No inventory architecture redesign. No business-behavior change beyond applying locked BA-09 terminology and ownership.

**Authoritative source (locked):** `docs/contexts/apotek/outpatient-apotek-repository-gap-analysis-report.md` — BA-09

BA-09 was not re-analyzed, reinterpreted, replaced, or redesigned.

**Reference pattern:** `apotek-domain.md` `BR-APT-098`–`BR-APT-104` and §5.13; `outpatient-apotek-workflow.md`; `SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-EN.md`.

---

## Locked decision applied (BA-09)

Stock Ledger remains a pure stock authority. Pharmacy owns Sales Order, Dispensing, dispensing lifecycle, `Prepared`, `Handed Over`, and No Show resolution. Stock Ledger owns stock quantity, Mutasi, Remove Stock, and movement history only.

| Pharmacy event | Inventory / Stock Ledger action |
|---|---|
| Dispensing Started / Pharmacy Reserve | Stock Mutasi: Pharmacy Unit → Dispensing Temporary Unit |
| Dispensing Completed / `Prepared` | No inventory action (`Prepared` is a Dispensing state) |
| Medication Handed Over | Remove Stock from Dispensing Temporary Unit |
| No Show resolution | Stock Mutasi: Dispensing Temporary Unit → Pharmacy Unit |

No separate `ReserveStock` contract. Inventory does not store No Show status. Custody while awaiting handover is **Dispensing Temporary Custody**.

---

## Files modified

| File | Change |
|---|---|
| `apotek-domain.md` | Event catalog: replace `Stock Reserved` with workflow Mutasi / Remove Stock / return Mutasi events |
| `outpatient-apotek-screen-and-aggregate-design.md` | §3.2 Exception Worklist; §3.3 Dispensing; §3.4 Serah Obat; §5.5 Inventory authority |
| `sop/SOP-APT-RJ-002-*-EN.md` and `-ID.md` | Inventory actor, Mutasi display, not Dispense Authorized |
| `sop/SOP-APT-RJ-003-*-EN.md` and `-ID.md` | Pharmacy Reserve Mutasi; handover Remove Stock; decline return Mutasi |
| `sop/SOP-APT-RJ-004-*-EN.md` and `-ID.md` | Same Mutasi / Remove Stock path as SOP-005 / `WF-APT-RJ-004` |
| `sop/SOP-APT-RJ-006-*-EN.md` and `-ID.md` | Per-Dispense-Order Mutasi / Remove Stock |
| `sop/SOP-APT-RJ-007-*-EN.md` and `-ID.md` | Dispensing Temporary Custody; Pharmacy-directed return Mutasi; Inventory does not own No Show |
| `apotek-domain-id.md` | Glossary, `BR-APT-014`/`033`/`039`/`068`–`071`/`078`/`079`, `BR-APT-098`–`104`, §5.14, event catalog |
| `outpatient-apotek-workflow-id.md` | Companion aligned to English workflow Mutasi language |
| `APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md` | ALN-004 marked resolved; ALN-007 In-Transit SOP/screen leftover closed |
| `docs/ARTIFACTS.md` | Indexed this report |

Unchanged (already BA-09-aligned): English `outpatient-apotek-workflow.md`; SOP-005 EN; domain `BR-APT-098`–`BR-APT-104` and §5.13 body.

---

## Sections modified (canonical EN)

| Artifact | Sections |
|---|---|
| Domain | Event catalog (`Stock Reserved` removed) |
| Screen design | §3.2 No Show row; §3.3 items 1 and 6; §3.4 step 7; §5.5 Inventory row |
| SOP-002 | §2 Inventory; §4 step 12 |
| SOP-003 | §2 Inventory; §4 steps 8 and 17; §5.1; §6 criterion 3 |
| SOP-004 | §2 Inventory; §4 steps 4 and 14; §6 criterion 3 |
| SOP-006 | §2 Inventory; §4 step 16 |
| SOP-007 | Purpose; §2 Inventory; §3 item 2; §4 step 6 |

---

## Removed legacy inventory-contract references

| Retired wording | Replacement |
|---|---|
| Stock Reservation / reserved stock / reservation outcome | Pharmacy Reserve = Stock Mutasi Pharmacy Unit → Dispensing Temporary Unit |
| Inventory Issue | Remove Stock from Dispensing Temporary Unit |
| `Stock Reserved` (domain event) | `Stock Transferred to Dispensing Temporary Unit` (plus `Stock Removed from Dispensing Temporary Unit`, `Stock Returned to Pharmacy Unit`) |
| In-Transit / in-transit medication | Dispensing Temporary Custody (`Prepared` remains a Dispensing state) |
| Inventory owns reservation, issue, Prepared, No Show | Stock Ledger owns quantity and movement history only |
| Inventory evaluates reserved/In-Transit for Return to Stock as if it owned fulfillment | Pharmacy requests return Mutasi; Inventory applies that movement and does not store No Show |

`ReserveStock` was never introduced as a new object. Mentions of “no `ReserveStock` contract” remain only as the BA-09 prohibition.

---

## Evidence of BA-09 alignment

| BA-09 rule | Evidence |
|---|---|
| Reserve = Mutasi; no `ReserveStock` | Domain `BR-APT-098`; SOP-003/004 Mutasi steps; screen Dispensing item 1 |
| `Prepared` is Dispensing only; no inventory action | Domain `BR-APT-099`/`100`; workflow already stated this; SOP-003/004 still move Dispensing to `Prepared` without an Inventory status |
| Handover = Remove Stock | Domain `BR-APT-101`; SOP-003/004/005/006; screen Serah Obat step 7 |
| No Show = return Mutasi; Pharmacy-owned | Domain `BR-APT-102`; SOP-007 step 6; workflow `WF-APT-RJ-007` |
| Stock Ledger owns quantity and history only | Screen §5.5; workflow participant table (EN already); domain §5.13 |
| Domain and workflow event names match | Domain catalog now lists the three Stock Ledger events used by `outpatient-apotek-workflow.md` §4 participants |

---

## Ownership confirmation

**Pharmacy owns:** Sales Order, Dispensing, dispensing lifecycle, `Prepared`, Medication Handover, No Show resolution, Dispense Authorized evaluation.

**Stock Ledger owns:** Stock quantity, Stock Mutasi, Remove Stock, movement history.

**Stock Ledger does not own:** reservation status, issue status, fulfillment progress, `Prepared`, No Show.

That split is now consistent across English domain, English workflow, screen design, SOP-002–007, and the Indonesian domain/workflow/SOP companions.

---

## Confirmation: alignment only

This resolution did **not**:

- change BA-09 lifecycle mapping or add a reservation aggregate;
- change when Pharmacy Reserve Mutasi may occur (before arrival is still allowed);
- change that Medication Preparation still requires Dispense Authorized;
- change handover gates or BPJS invoice-at-handover;
- change No-Show commercial outcomes (BPJS uninvoiced vs paid General Patient);
- invent a new queue status or Inventory fulfillment state.

ALN-005, ALN-006, and ALN-007 (ADR example labels only) remain open.
