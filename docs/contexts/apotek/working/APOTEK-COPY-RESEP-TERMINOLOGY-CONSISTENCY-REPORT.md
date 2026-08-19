# Apotek Copy Resep Terminology Consistency Report

**Artifact status:** Completed alignment record  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Date:** 2026-08-18  
**Scope:** Replace retired **Salinan Resep** vocabulary with canonical **Copy Resep** across living Apotek architecture artifacts. No business-rule, lifecycle, ownership, cardinality, or architectural-decision change.

**Source of approved vocabulary:** Architect decision 2026-08-18; persistence anchor [`outpatient-apotek-persistence-design.md`](../outpatient-apotek-persistence-design.md).

| Retired (not current vocabulary) | Canonical |
|---|---|
| Salinan Resep | Copy Resep |
| `SalinanResep` / `SalinanResepId` | `CopyResep` / `CopyResepId` |
| `BILRG_AptSalinanResep` / `BILRG_AptSalinanResepItem` | `BILRG_AptCopyResep` / `BILRG_AptCopyResepItem` |
| `ISalinanResepRepo` | `ICopyResepRepo` |
| Prescription Copy (entity label) | Copy Resep |
| NunaId prefix `ASR` (Apt Salinan Resep) | `ACR` (Apt Copy Resep) |

**Intentionally unchanged:** Indonesian prose for **Resep Kerja** that describes the pharmacy operational intake copy (`salinan operasional`, `salinan ini`, `Salinan intake`). That language refers to Resep Kerja, not the Copy Resep supporting document.

**Generic English “copy”** in outcome-analysis prose (e.g. “issue a copy”, “decline with no copy”) remains when it means an optional document instance, not the entity name.

---

## Verification

Full-repository scan (`*.md`) after edits:

- No remaining `Salinan Resep`, `SalinanResep`, `ISalinanResep`, `AptSalinanResep`, or `Prescription Copy` entity labels in Apotek artifacts.
- No remaining `ASR` NunaId prefix for this entity.
- No application source files (`*.cs`, `*.sql`, etc.) contained Salinan identifiers (documentation-only bounded context at this stage).

---

## Modified artifacts

### Living canonical artifacts

| File | Role | Changes |
|---|---|---|
| [`apotek-domain.md`](../apotek-domain.md) | Canonical domain (EN) | Glossary, actors, objects, BR-APT-054/109–115/118; added §5.16 Copy Resep supporting document |
| [`apotek-domain-id.md`](../apotek-domain-id.md) | Indonesian domain companion | Parallel glossary and business rules; added §5.17 Copy Resep |
| [`outpatient-apotek-workflow.md`](../outpatient-apotek-workflow.md) | Canonical workflow (EN) | Stock shortage handling, actor table, WF alternatives, payer paths |
| [`outpatient-apotek-workflow-id.md`](../outpatient-apotek-workflow-id.md) | Indonesian workflow companion | Parallel workflow terminology |
| [`outpatient-apotek-persistence-design.md`](../outpatient-apotek-persistence-design.md) | Persistence design | Entity map, ERD, table catalog, §8.9, repository contract, `CopyResepId` on Unfulfilled Outcome, NunaId `ACR` |
| [`outpatient-apotek-screen-and-aggregate-design.md`](../outpatient-apotek-screen-and-aggregate-design.md) | Screen / aggregate design | Pelayanan Penjualan workbench, Exception Worklist; supporting-object catalog row for Copy Resep |
| [`working/outpatient-apotek-repository-gap-analysis-report.md`](./outpatient-apotek-repository-gap-analysis-report.md) | Repository gap analysis (BA-03) | Partial fulfillment, stock shortage, and BC policy prose |

### SOPs

| File | Changes |
|---|---|
| [`sop/SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md`](../sop/SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md) | Actors, system behavior, verification checklist |
| [`sop/SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-ID.md`](../sop/SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-ID.md) | Parallel Indonesian SOP |
| [`sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md`](../sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md) | Post-SO shortage exception behavior |
| [`sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-ID.md`](../sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-ID.md) | Parallel Indonesian SOP |
| [`sop/SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-EN.md`](../sop/SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-EN.md) | BPJS shortage / outcome behavior |
| [`sop/SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-ID.md`](../sop/SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-ID.md) | Parallel Indonesian SOP |

### Archive alignment records (historical cross-references)

| File | Changes |
|---|---|
| [`archive/ALN-006-RESOLUTION-REPORT.md`](../archive/ALN-006-RESOLUTION-REPORT.md) | Pre- vs post-SO shortage alignment tables |
| [`archive/APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md`](../archive/APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md) | BR-APT-110 cross-artifact note |
| [`archive/APOTEK-INVOICE-MUTABILITY-ARTIFACT-UPDATE-REPORT.md`](../archive/APOTEK-INVOICE-MUTABILITY-ARTIFACT-UPDATE-REPORT.md) | BR-APT-118 impact row |
| [`archive/APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md`](../archive/APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md) | Neighbor-entity analysis, correlation columns, rejection rationale |

### Generated output

| File | Changes |
|---|---|
| [`output/markdown/SOP-Apotek-Rawat-Jalan-ID.md`](../../../output/markdown/SOP-Apotek-Rawat-Jalan-ID.md) | Recompiled Indonesian SOP bundle aligned with SOP-002/003/004 sources |

---

## Artifacts reviewed with no Salinan Resep usage

| Area | Files | Result |
|---|---|---|
| ADR | `adr/ADR-APT-001` through `ADR-APT-003` | No Salinan Resep references |
| SOP index | `sop/DAFTAR-SOP-APT-RJ.md` | No Salinan Resep references |
| Remaining SOPs | SOP-APT-RJ-001, 005, 006, 007 (EN/ID) | No Salinan Resep references |
| Application code | `*.cs`, `*.sql`, `*.ts`, `*.tsx` | No Salinan identifiers |
| Global docs | `docs/ARTIFACTS.md`, `docs/skills/*`, neighbor contexts | No Apotek Salinan Resep references |

---

## Identifier summary (persistence)

| Layer | Canonical form |
|---|---|
| Business term | Copy Resep |
| Identifier / class | `CopyResep`, `CopyResepId` |
| Repository | `ICopyResepRepo` |
| Header table | `BILRG_AptCopyResep` |
| Item table | `BILRG_AptCopyResepItem` |
| Unfulfilled Outcome FK | `CopyResepId` on `BILRG_AptUnfulfilledOutcome` |
| NunaId prefix | `ACR` |

---

## Follow-up (out of scope for this pass)

- Regenerate `output/pdf/SOP-Apotek-Rawat-Jalan-ID.pdf` when the PDF pipeline is run.
- When implementation begins, use `CopyResep` / `BILRG_AptCopyResep*` names in code and migrations; do not introduce `SalinanResep` aliases.
- Add a retired→canonical row for Copy Resep to [`archive/APOTEK-TERMINOLOGY-ALIGNMENT-REPORT.md`](../archive/APOTEK-TERMINOLOGY-ALIGNMENT-REPORT.md) if that record is updated for a future terminology batch.
