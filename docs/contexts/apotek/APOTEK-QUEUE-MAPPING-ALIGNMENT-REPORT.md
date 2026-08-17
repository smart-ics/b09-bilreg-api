# Apotek Queue Mapping Alignment Report

**Artifact status:** Completed alignment record  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Date:** 2026-08-17  
**Scope:** Resolve the remaining Queue Mapping target inconsistency across Outpatient Apotek artifacts. BA-03 ownership and existing business-rule meaning are unchanged.

---

## Canonical model

Outpatient Queue Mapping associates a Pharmacy Queue Entry with the originating demand source only:

- **Resep Kerja**
- **Jual Bebas**

Mapping shall **not** target a Sales Order, Invoice, or Dispensing.

After a Sales Order exists, worklists join from the mapped Resep Kerja or Jual Bebas to downstream Sales Order, Invoice, and Dispensing records. Those joins are not mapping targets.

Tracker Mapping still *resolves existing Resep* as clinical evidence. The recorded association is the corresponding **Resep Kerja**. Tracker Mapping does not create a Resep, does not resolve Jual Bebas, and does not map to Sales Order, Invoice, or Dispensing.

Manual Mapping identifies an existing **Resep Kerja** or **Jual Bebas**. A presented Resep Fisik is recorded first, producing Resep Kerja, then mapped.

---

## BA-03 preserved

`OutpatientQueueMapping` remains a navigation/association mechanism only. It is not an aggregate root. It does not own lifecycle, workflow state, or transactional consistency. Queue identity remains in Patient Tracker. Medication demand lifecycle remains in Pharmacy aggregates (`TelaahResep`, `SalesOrder`, `Invoice`, `Dispensing`). Incorrect mapping is updated in place. Mapping-change history is not required.

This alignment only names the pharmacy-side mapping endpoint. It does not reopen BA-03.

---

## Files changed

| File | Change |
|---|---|
| `apotek-domain.md` | Glossary, §3.11, §5.7, §6.5, `BR-APT-062`–`064`, `BR-APT-084`, §8.6, event `Outpatient Queue Mapped` |
| `apotek-domain-id.md` | Same sections as the English domain |
| `outpatient-apotek-screen-and-aggregate-design.md` | Workbench mapping action; §5.1 mermaid now `MAP -.-> RK` and `MAP -.-> JB`; §5.2 mapping table; invariant 6 |
| `outpatient-apotek-workflow.md` | Overview, included scope, `WF-APT-RJ-001` facts/flows, `WF-APT-RJ-006` trigger |
| `outpatient-apotek-workflow-id.md` | Same as English workflow |
| `sop/SOP-APT-RJ-001-Antrian-dan-Mapping-EN.md` | Purpose, steps, exception 5.3, completion criteria |
| `sop/SOP-APT-RJ-001-Antrian-dan-Mapping-ID.md` | Title, purpose, steps, exception 5.3, completion criteria |
| `sop/SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-EN.md` | Precondition: mappings to at least two Resep Kerja and/or Jual Bebas |
| `sop/SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-ID.md` | Same precondition |
| `sop/DAFTAR-SOP-APT-RJ.md` | SOP-001 ID title |
| `outpatient-apotek-repository-gap-analysis-report.md` | BA-03 decision and §9.1: pharmacy-side endpoint is Resep Kerja or Jual Bebas only |
| `outpatient-apotek-persistence-design.md` | §4.2 association wording; §4.3 cardinality labels `Demand source (Resep Kerja \| Jual Bebas)`. Mermaid and `DemandKind` were already canonical |
| `APOTEK-TERMINOLOGY-ALIGNMENT-REPORT.md` | Remaining item 2 marked resolved by this report |
| `docs/ARTIFACTS.md` | Index row for this report; SOP-001 ID title |

---

## Intentionally unchanged

| Item | Reason |
|---|---|
| BA-03 ownership | Mapping is still not an aggregate; no mapping history; update in place |
| Mapping shall not create or modify Resep, Telaah Resep, or Sales Order | Existing `BR-APT-062` meaning preserved |
| Tracker Mapping does not apply to Jual Bebas | Existing `BR-APT-063` |
| One Resep Kerja may yield two Sales Orders (BPJS + Patient-Pay) sharing one mapping row | Existing `BR-APT-084`, `BR-APT-124` |
| SOP-003 / SOP-004 “mapping already displayed” plus an active Sales Order | Those SOPs consume mapping; they do not retarget it |
| SOP-006 display of Sales Order, Invoice, and Dispensing progress | Progress of mapped demands, not mapping endpoints |
| ALN-001 | Historical BA-03 ownership record; it did not assert mapping-to-Sales-Order as current vocabulary |
| Generated `output/` SOP packs | Derived artifacts; sources were updated |

---

## Confirmation

Living Outpatient Apotek diagrams, aggregate maps, narratives, examples, and descriptions now use **one canonical Queue Mapping model**:

**Pharmacy Queue Entry ↔ Resep Kerja | Jual Bebas**

No remaining current-state illustration attaches Queue Mapping directly to Sales Order, Invoice, or Dispensing.
