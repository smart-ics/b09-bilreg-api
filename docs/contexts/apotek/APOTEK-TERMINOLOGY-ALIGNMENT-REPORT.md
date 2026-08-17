# Apotek Terminology Alignment Report

**Artifact status:** Completed alignment record; remaining review items closed  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Date:** 2026-08-17  
**Scope:** Eliminate terminology drift introduced by persistence-design vocabulary decisions. No business-rule, lifecycle, ownership, cardinality, or architectural-decision change. Architect decisions of 2026-08-17 close the remaining review items.

**Source of approved vocabulary:** `outpatient-apotek-persistence-design.md`

| Retired (not current vocabulary) | Canonical |
|---|---|
| Direct Medication Request | Jual Bebas |
| DirectRequest | JualBebas |
| Sales Invoice / `SalesInvoice` | Invoice / `Invoice` |
| Dispense Order / `DispenseOrder` | Dispensing / `Dispensing` |
| Line (entity, table, class) | Item |
| Resep Snapshot / Prescription Snapshot | Resep Kerja |

`ItemNo` is unchanged.

Approved `Dispensing` vocabulary split (architect decision, Issue #1):

| Form | Use |
|---|---|
| `Dispensing` | Aggregate name |
| dispensing | Process/activity prose |
| Dispensing Temporary Unit / Location | Inventory custody and location terminology |

---

## Files reviewed

### Living canonical artifacts (updated)

| File | Role |
|---|---|
| `apotek-domain.md` | Canonical domain |
| `apotek-domain-id.md` | Indonesian domain companion |
| `outpatient-apotek-workflow.md` | Canonical workflow |
| `outpatient-apotek-workflow-id.md` | Indonesian workflow companion |
| `outpatient-apotek-screen-and-aggregate-design.md` | Screen and aggregate design |
| `outpatient-apotek-persistence-design.md` | Persistence design (already canonical; no edit) |
| `outpatient-apotek-repository-gap-analysis-report.md` | Gap analysis |
| `adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md` | Queue ownership ADR |
| `adr/ADR-APT-002-pharmacy-stock-ledger-boundary.md` | Stock Ledger boundary ADR |
| `sop/DAFTAR-SOP-APT-RJ.md` | SOP index and term guide |
| `sop/SOP-APT-RJ-001-*` through `sop/SOP-APT-RJ-007-*` (EN and ID) | Outpatient SOPs |
| `ALN-001-RESOLUTION-REPORT.md` through `ALN-007-RESOLUTION-REPORT.md` | Prior alignment records (current-state names updated) |
| `APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md` | Prior cross-artifact review |

### Neighbor and skill artifacts that referenced these names (updated)

| File | Why included |
|---|---|
| `docs/ARTIFACTS.md` | Apotek index titles; non-existent queue-gap rows removed |
| `docs/contexts/stok-ledger/stok-ledger-domain.md` | Pharmacy ownership list used Dispense Order |
| `docs/skills/sop-creation-skill.md` | SOP example used Dispense Order |
| `docs/skills/workflow-creation-skill.md` | Workflow example used Sales Invoice / Dispense Order |

### Generated output (regenerated 2026-08-17)

| File | Finding |
|---|---|
| `output/markdown/SOP-Apotek-Rawat-Jalan-ID.md` | Recompiled from the aligned Indonesian SOP sources |
| `output/pdf/SOP-Apotek-Rawat-Jalan-ID.pdf` | Regenerated from the same aligned Indonesian SOP sources |

SOP filenames that contain `Permintaan-Langsung` were kept unchanged by architect decision. Internal titles and body text use Jual Bebas.

Queue-gap paths `outpatient-apotek-queue-gap-analysis.md` and `outpatient-apotek-queue-gap-analysis-codex.md` are not in the repository and are no longer indexed in `docs/ARTIFACTS.md`.

---

## Changes applied

1. **Demand source.** Direct Medication Request / DirectRequest / Permintaan Obat Langsung / permintaan obat langsung → Jual Bebas (`JualBebas`). Event `Jual Bebas Accepted`. Decline still creates no record.
2. **Commercial aggregate.** Sales Invoice / `SalesInvoice` / Sales Invoice Item → Invoice / `Invoice` / Invoice Item. Event `Invoice Established`. Indonesian glossary Indonesia-column is Faktur; the identifier is Invoice.
3. **Fulfillment aggregate.** Dispense Order / `DispenseOrder` / Dispense Order Line → Dispensing / `Dispensing` / Dispensing Item. Event `Dispensing Established`. Lifecycle states in §8.4 are unchanged.
4. **Child entities.** Sales Order Line → Sales Order Item. Accepted Medication Line → Accepted Medication Item. Item-level Charge replaces Line-level Charge. `ItemNo` kept. Baris Resep kept as the clinical source term.
5. **Pharmacy working copy.** Prescription Snapshot / Resep Snapshot → Resep Kerja. Glossary entries and domain objects §5.14 / §5.15 (ID §5.15 / §5.16) record Resep Kerja and Jual Bebas as supporting documents, not aggregate roots. Telaah Resep reviews Resep Kerja without changing original-Resep ownership (`BR-APT-002`).
6. **Diagrams.** Screen aggregate map now shows Resep → Resep Kerja → Telaah Resep, Jual Bebas → Sales Order, Sales Order → Invoice and Dispensing. SOP-002 source diagrams use Jual Bebas, Invoice Item, and Dispensing Item. Screen §5.2 classification table lists Resep Kerja and Jual Bebas as supporting documents.
7. **Repositories / tables / classes** in narratives now match persistence: `IInvoiceRepo`, `IDispensingRepo`, `IJualBebasRepo`, `IResepKerjaRepo`, `BILRG_AptInvoice`, `BILRG_AptDispensing`.
8. **ALN-007.** Current-state names updated. A supersession note records that ALN-007 originally aligned informal labels to Dispense Order, and persistence later named the aggregate Dispensing.
9. **Indonesian companions.** Remaining ordinary “baris” wording in SOP-003/004/005 ID and domain-id business rules was replaced with item / Sales Order Item / Covered item where it named pharmacy entities. Baris Resep remains the clinical source term.
10. **Queue-gap index.** Removed the non-existent `outpatient-apotek-queue-gap-analysis.md` and `outpatient-apotek-queue-gap-analysis-codex.md` rows from `docs/ARTIFACTS.md`. The BA-01 “Ratified in” list in `outpatient-apotek-repository-gap-analysis-report.md` no longer cites the missing file; historical evidence footnotes that name the former queue-gap analysis remain as provenance.
11. **Generated SOP pack.** Recompiled `output/markdown/SOP-Apotek-Rawat-Jalan-ID.md` and `output/pdf/SOP-Apotek-Rawat-Jalan-ID.pdf` from the aligned `sop/*-ID.md` sources.

Business rules, lifecycle definitions, ownership boundaries, cardinalities, and previously ratified BA/BC decisions were not rewritten. Only names were synchronized.

---

## Terminology intentionally left unchanged

| Term | Reason |
|---|---|
| `ItemNo` | Explicitly retained as the detail-key name |
| Baris Resep | Clinical source line on the original Resep, not a pharmacy table/class |
| Dispense Authorized | Policy evaluation result; not the Dispensing aggregate |
| Medication Dispense | Accountable supply fact; not the Dispensing aggregate |
| Final Dispense Review / Final Dispense Review Record | Professional review process and its immutable record |
| Dispense Cycle | Fulfillment period/batch, especially unit-dose |
| Unit Dose Dispensing | Care-setting fulfillment mode |
| Dispensing Temporary Unit / Location / Dispensing Temporary Custody | Inventory custody and location vocabulary (ALN-007 / ADR-APT-002); confirmed by architect decision on Issue #1 |
| Physical Dispensing (capability 3.7) / lowercase “dispensing” | Process/activity prose; not the Dispensing aggregate; confirmed by architect decision on Issue #1 |
| Pricing Snapshot | Commercial price basis; not Resep Kerja |
| `Sales Invoice Line` quoted in ALN-003 | Historical rejected prompt phrase; current term is Invoice Item |
| Informal status `Dispensing` as retired in ALN-007 | Remains retired as a queue-like pharmacy status |
| Indonesian operator glosses such as resep kertas, pesanan apotek, faktur, penyiapan obat | SOP-ID writing style; identifiers in backticks remain canonical; confirmed by architect decision on Issue #6 |
| SOP-APT-RJ-002 filenames containing `Permintaan-Langsung` | Kept unchanged by architect decision on Issue #4; body and titles use Jual Bebas |

---

## Remaining cross-document inconsistencies — closed

No items remain open. Issue #2 was already resolved by `APOTEK-QUEUE-MAPPING-ALIGNMENT-REPORT.md`. Issues #1, #3, #4, #5, and #6 were closed by architect decision on 2026-08-17.

| # | Item | Decision | Record |
|---|---|---|---|
| 1 | `Dispensing` name collision | **ACCEPT** | Retain the approved vocabulary split: `Dispensing` = aggregate name; lowercase “dispensing” = process/activity prose; Dispensing Temporary Unit / Location = inventory custody/location terminology. Informal queue-like status `Dispensing` remains retired (ALN-007). |
| 2 | Queue mapping target | **CLOSED** (prior) | Living artifacts use Queue Entry ↔ Resep Kerja or Jual Bebas only. Mapping does not target Sales Order, Invoice, or Dispensing. BA-03 ownership is unchanged. See `APOTEK-QUEUE-MAPPING-ALIGNMENT-REPORT.md`. |
| 3 | Missing queue-gap files | **CLOSE** | `docs/ARTIFACTS.md` no longer indexes `outpatient-apotek-queue-gap-analysis.md` or `outpatient-apotek-queue-gap-analysis-codex.md`. Those files are not in the repository and are not current artifacts. |
| 4 | SOP filename vs body | **ACCEPT** | Keep existing SOP filenames unchanged. No filename renaming is required. Body terminology remains authoritative. |
| 5 | Generated SOP pack | **CLOSE** | Regenerated `output/markdown/SOP-Apotek-Rawat-Jalan-ID.md` and `output/pdf/SOP-Apotek-Rawat-Jalan-ID.pdf` from the updated Indonesian SOP sources so compiled terminology matches the aligned artifacts. |
| 6 | Operator glosses vs identifiers | **ACCEPT** | Continue using Indonesian operational terminology in SOP prose (faktur, pesanan apotek, tugas penyiapan obat, and similar) while retaining canonical technical identifiers (`Invoice`, `Sales Order`, `Dispensing`, and so on) in model references and code-oriented sections. |

---

## Verification

Living Apotek markdown no longer uses Direct Medication Request, `SalesInvoice`, Sales Invoice, `DispenseOrder`, or Dispense Order as current vocabulary, except:

- ALN-007 supersession note explaining the rename
- ALN-003 historical quote of the rejected phrase “Sales Invoice Line”
- this report’s mapping table

The regenerated SOP pack (`output/markdown` and `output/pdf`) matches the aligned Indonesian SOP sources: Jual Bebas, Resep Kerja, Invoice, Dispensing, and Item.

Persistence design already used Jual Bebas, Invoice, Dispensing, Item, and Resep Kerja as the source of the approved vocabulary.
