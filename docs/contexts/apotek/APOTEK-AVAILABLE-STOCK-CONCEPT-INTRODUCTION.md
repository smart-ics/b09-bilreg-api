# Apotek Available Stock Concept Introduction

**Artifact status:** Concept introduction record  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Date:** 2026-08-17  
**Scope:** Define Available Stock as a fulfillment-planning concept distinct from Current Stock. No Available Stock calculation formula. No change to shortage timing (BC-10), Backorder prohibition, or Stock Ledger movement mapping (BA-09 / ADR-APT-002).

---

## Purpose

Living Apotek artifacts previously used a single quantity idea (`Stock Availability` / “currently available stock”) both for physical inventory and for deciding what could still be placed on a new Sales Order. That collapsed two business questions into one term.

This introduction records the split:

| Concept | Business question |
|---|---|
| Current Stock | How much inventory physically exists? |
| Available Stock | How much inventory can still be promised to a new order? |

**Available Stock ≠ Current Stock.**

---

## Definitions added

### Available Stock

Available Stock represents the quantity that can still be committed to a new Sales Order.

Available Stock is a fulfillment-planning concept used during Sales Order establishment and shortage evaluation.

Available Stock SHALL NOT be considered equivalent to Current Stock.

Identifier form in living artifacts: `Available Stock`. Indonesian companion gloss: **Stok Dapat Dijanjikan**.

### Current Stock

Current Stock represents the current physical inventory recorded by the inventory subsystem.

Current Stock reflects physical inventory state and inventory movements.

Identifier form in living artifacts: `Current Stock`. Indonesian companion gloss: **Stok Saat Ini**.

### Design principle

Available Stock and Current Stock serve different business purposes:

- Current Stock answers: "How much inventory physically exists?"
- Available Stock answers: "How much inventory can still be promised to a new order?"

Therefore:

Available Stock ≠ Current Stock

The exact Available Stock calculation formula is intentionally left undefined at this stage and will be specified in a future inventory-planning design activity.

Canonical rule: `BR-APT-146`.

---

## Terminology changes introduced

| Previous wording | Current wording | Meaning of the change |
|---|---|---|
| `Stock Availability` as Inventory’s “quantity currently available to support fulfillment” | Retired as a standalone quantity concept | The old definition mixed physical inventory with promised-to-order quantity |
| “currently available stock authority” (`BR-APT-117`) | Evaluate **Available Stock**; not equivalent to Current Stock | Shortage planning uses Available Stock |
| Stock Ledger owns “Stock Availability” / “stock quantity” | Stock Ledger owns **Current Stock**, Mutasi, Remove Stock, and movement history | Physical inventory remains Inventory-owned |
| “available stock” in Return to Stock and shortage event prose | **Current Stock** for physical return; **Available Stock** for pre-Sales-Order shortage evaluation | Stops treating returned physical quantity as the planning concept |
| Indonesian “ketersediaan stok” / “stok tersedia” as a single idea | Split into Current Stock and Available Stock | Companion domain and workflow match the English split |

No aggregate, table, or class was renamed. `Sales Order`, `Dispensing`, `Invoice`, Pharmacy Reserve, Stock Mutasi, and Remove Stock are unchanged.

`Stock Availability` remains in the glossary only as a retired umbrella phrase so readers can map old text. New writing shall use Current Stock or Available Stock.

---

## Artifacts updated

### Living canonical artifacts

| File | What was added or clarified |
|---|---|
| `apotek-domain.md` | Glossary entries; Inventory boundary; Sales Order aggregate non-ownership; `BR-APT-008`, `BR-APT-110`, `BR-APT-117`, `BR-APT-146`; Medication Shortage and shortage event wording |
| `apotek-domain-id.md` | Same definitions and rules in the Indonesian companion |
| `outpatient-apotek-workflow.md` | Overview, included/excluded, Stock Ledger handoff, `WF-APT-RJ-002` inputs and Stock Shortage branch, post-establishment exception, `BR-APT-146` reference |
| `outpatient-apotek-workflow-id.md` | Indonesian companion of the same workflow clarifications |
| `outpatient-apotek-screen-and-aggregate-design.md` | Governing principle 9; Telaah Resep and Pelayanan Penjualan workbenches; invariant 12; aggregate-impact row; formula as a non-decision |
| `outpatient-apotek-persistence-design.md` | Do-not-persist Current Stock vs Available Stock; invalid to persist Available Stock; neighbor reuse; checklist item 6; open decision **PD-04** |
| `outpatient-apotek-repository-gap-analysis-report.md` | BC-04 Stock Shortage wording; BC-10 Available Stock ≠ Current Stock; architecture-approval summary |
| `adr/ADR-APT-002-pharmacy-stock-ledger-boundary.md` | Stock Ledger owns Current Stock and does not own Available Stock |
| `docs/ARTIFACTS.md` | Index row for this report |

### Impact analysis

`docs/ARTIFACTS.md` still indexes `outpatient-apotek-stock-shortage-sales-order-impact-analysis.md`. That file is **not in the repository**. The living shortage impact record is BC-10 (and BC-04) in `outpatient-apotek-repository-gap-analysis-report.md`, which was updated instead of inventing a missing investigation document.

ALN-006 remains a historical timing-alignment record. Its BC-10 before/after Sales Order cut is unchanged and was not rewritten.

### Intentionally not updated in this pass

| File | Reason |
|---|---|
| SOP APT-RJ-002–007 | Operator procedures already distinguish shortage before vs after Sales Order establishment. They still say “Stock Availability” in a few actor tables. Align those operator glosses in a later SOP pass if required. |
| `stok-ledger-domain.md` | Inventory remains the Current Stock authority. Available Stock formula is future inventory-planning work, not a Stock Ledger schema change in this pass. |

---

## What did not change

- Shortage timing: Partial Sales Order only **before** Sales Order establishment (`BR-APT-110`); Unfulfilled Medication Outcome **after** (`BR-APT-118`).
- No Backorder, no alternate stock source, no outstanding outpatient demand (BC-10).
- Pharmacy Reserve remains Stock Mutasi Pharmacy Unit → Dispensing Temporary Unit.
- `Prepared` remains a Dispensing state, not an Inventory state.
- Clinical acceptance remains independent of stock facts (`BR-APT-008`).
- No new Apotek aggregate, table, or persisted Available Stock column.

---

## Open questions and future design work

1. **Available Stock formula (required later).** Specify inputs, location scope (Pharmacy Unit vs Dispensing Temporary Unit vs other), whether committed Sales Order / Dispensing quantities reduce Available Stock, batch/expiry rules, and unit of measure. Tracked as persistence open item **PD-04**.
2. **Owning design activity.** Confirm whether the formula is authored in an inventory-planning artifact, a Stock Ledger extension, or a Pharmacy application service that reads Current Stock plus commitments. This introduction does not choose an owner beyond “future inventory-planning design activity.”
3. **Runtime evaluation vs snapshot.** Decide whether Available Stock is evaluated only at Sales Order establishment, also at later shortage detection, and whether any evaluation result is recorded as evidence (still not as Current Stock).
4. **Screen presentation.** If operators see both quantities, the screen design must keep labels distinct. Layout, fields, and badges are not specified here.
5. **SOP operator language.** SOP-002 still says Inventory supplies “Stock Availability.” Decide whether SOP actor tables should say Current Stock, Available Stock, or both.
6. **Missing impact-analysis file.** Either restore `outpatient-apotek-stock-shortage-sales-order-impact-analysis.md` or remove it from `docs/ARTIFACTS.md` so the index matches the repository.
7. **Post-establishment shortage vs Current Stock.** After a Sales Order exists, unfulfillable quantity follows `BR-APT-118`. Whether that later shortage is detected from Current Stock, from execution failure, or from a later Available Stock evaluation is not specified here.

---

## Verification

Living domain, workflow, screen/aggregate, persistence, ADR-APT-002, and BC-10 now state all of the following:

- Available Stock is the quantity that can still be committed to a new Sales Order.
- Current Stock is physical inventory recorded by the inventory subsystem.
- Available Stock ≠ Current Stock.
- The Available Stock formula is undefined and reserved for future inventory-planning design.
- Available Stock is not persisted as Apotek or Stock Ledger truth.
