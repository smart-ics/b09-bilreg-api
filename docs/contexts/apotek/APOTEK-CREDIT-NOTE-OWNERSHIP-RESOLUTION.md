# PD-07 — Credit Note Ownership Resolution

**Date:** 2026-08-18  
**Task type:** Artifact refactoring. No source code, schema, or API contract was changed.  
**Canonical decision:** [`outpatient-apotek-persistence-design.md`](outpatient-apotek-persistence-design.md) **PD-07**  
**Related:** [`ADR-APT-003`](adr/ADR-APT-003-invoice-mutability-owned-by-tata-rekening.md)

---

## 1. Decision

- Remove `BILRG_AptCreditNote` from the Outpatient Apotek persistence design.
- Credit Note, Refund, Financial Adjustment, and all accountable financial-correction mechanisms are owned by Tata Rekening, not Apotek.
- Apotek may keep correlation/reference information (`TataRekeningCorrectionReff` on Invoice).
- Apotek must not own or persist a Credit Note entity and must not introduce a replacement Apotek financial aggregate.

---

## 2. What changed in the architecture

| Topic | Before (invalid for implementation) | After |
|---|---|---|
| Credit Note table | `BILRG_AptCreditNote` append-only child of Invoice | Forbidden. Listed in persistence §6.2 omitted tables |
| Invoice aggregate | Owned Credit Notes as child facts | Owns sale/charge document only; optional TR correction correlation |
| Invoice lifecycle `Adjusted or Credited` | Compensating-document path implying Apotek CN | Disposition observed after Tata Rekening applies exception correction |
| Integration Task `BillingCredit` `{InvoiceId}:CN{n}` | Fired when Apotek recorded a Credit Note | Removed. Assumed Apotek Credit Note numbers |
| Reporting | Fillfactor / journals mentioned credit notes | Unified reporting remains Query DAL over Invoice ∪ legacy DU; no CN table |
| Unfulfilled Outcome | Optional join to `BILRG_AptCreditNote` | Optional join to Tata Rekening correction identity; KEEP table unchanged |

No new Apotek aggregate was added. Invoice, Sales Order, TelaahResep, and Dispensing remain the only write aggregate roots.

---

## 3. Artifacts modified

| Artifact | Change |
|---|---|
| [`outpatient-apotek-persistence-design.md`](outpatient-apotek-persistence-design.md) | PD-07 closed. Table catalog, ERD, identifiers, Invoice shape, reconstruction, Integration Task catalog, PD-05, indexing notes, architect checklist. Open **PD-08**. |
| [`apotek-domain.md`](apotek-domain.md) / [`apotek-domain-id.md`](apotek-domain-id.md) | Glossary, Invoice aggregate, **BR-APT-027**, **BR-APT-060**, Invoice lifecycle, `Invoice Credited` |
| [`adr/ADR-APT-003-invoice-mutability-owned-by-tata-rekening.md`](adr/ADR-APT-003-invoice-mutability-owned-by-tata-rekening.md) | Exception mechanisms are Tata Rekening-owned; PD-07 consequence |
| [`outpatient-apotek-workflow.md`](outpatient-apotek-workflow.md) / [`outpatient-apotek-workflow-id.md`](outpatient-apotek-workflow-id.md) | Participants, WF-003 correction, WF-007 paid path, Tata Rekening handoff |
| [`outpatient-apotek-screen-and-aggregate-design.md`](outpatient-apotek-screen-and-aggregate-design.md) | Exception worklist, Invoice ownership, Tata Rekening collaborator |
| [`outpatient-apotek-repository-gap-analysis-report.md`](outpatient-apotek-repository-gap-analysis-report.md) | MI-03: financial-correction correlation, not credit-note ownership |
| [`sop/SOP-APT-RJ-003-*`](sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md) EN/ID | Pharmacy does not persist Credit Note |
| [`sop/SOP-APT-RJ-007-*`](sop/SOP-APT-RJ-007-Penanganan-Obat-Tidak-Diambil-EN.md) EN/ID | Display TR outcome; do not persist Credit Note |
| [`ALN-006-RESOLUTION-REPORT.md`](ALN-006-RESOLUTION-REPORT.md) | Post-SO commercial path follows BR-APT-027 / TR ownership |
| [`APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md`](APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md) | Collaborator is Tata Rekening correction, not `BILRG_AptCreditNote`. KEEP unchanged |
| [`APOTEK-INVOICE-MUTABILITY-IMPACT-ANALYSIS.md`](APOTEK-INVOICE-MUTABILITY-IMPACT-ANALYSIS.md) | Historical map; PD-07 supersession banner |
| [`APOTEK-INVOICE-MUTABILITY-ARTIFACT-UPDATE-REPORT.md`](APOTEK-INVOICE-MUTABILITY-ARTIFACT-UPDATE-REPORT.md) | Historical report; §8 superseded |
| [`APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md`](APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md) | Gap / follow-up: TR owns CN vs `FINALIZED`/`LUNAS`; no Apotek `BillingCredit` |
| [`docs/ARTIFACTS.md`](../../ARTIFACTS.md) | Index this resolution |

SOP-004 and SOP-005 already stated that Tata Rekening supplies Credit Note when revision is not permitted. They were left unchanged except as already aligned to BR-APT-027.

ADR-APT-001, ADR-APT-002, and [`APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md`](APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md) had no Credit Note table requirement.

---

## 4. Remaining dependencies that still require architect review

These items are **not** permission to restore `BILRG_AptCreditNote`.

1. **PD-08 — Tata Rekening exception-correction request contract.** Decide whether Apotek emits an Integration Task to *request* Credit Note / Refund / Financial Adjustment, or operators work in Tata Rekening and Apotek only stores returned `TataRekeningCorrectionReff`.
2. **Charge-change after permitted Invoice revision.** `BillingCharge` remains the create path. Task type, idempotency, and reconciliation for an in-place charge change after TR-permitted Invoice rewrite remain an integration-planning item (`BR-APT-028`).
3. **Tata Rekening document shape.** How Tata Rekening persists Credit Note vs Financial Adjustment (SOP-TR-05) vs Cashier Refund, including `FINALIZED` / `LUNAS`, is Tata Rekening-owned and is not specified by Apotek artifacts.
4. **Optional Unfulfilled Outcome correlation.** Additive `DispensingId` / Tata Rekening correction identity on `BILRG_AptUnfulfilledOutcome` remains optional REWORK and is not required for PD-07.

---

## 5. Consistency checks

| Question | Result |
|---|---|
| Does any canonical catalog require `BILRG_AptCreditNote` to exist? | No. Forbidden in §6.2 |
| Does Invoice own Credit Note lifecycle? | No |
| Was a replacement Apotek financial aggregate introduced? | No |
| Is Credit Note still named in Apotek language? | Yes, as a Tata Rekening exception document and observed event |
| Does unified reporting require a Credit Note table? | No |
| Do historical mutability reports still describe the old table? | Yes, as superseded snapshots, not as current design |
