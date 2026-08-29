# Artifact Cleanup Report

```yaml
Artifact-Type: ReviewReport
Status: Working
Normative-Level: Historical
Archive-Eligible: No
```

**Bounded context:** Apotek (`Pelayanan Obat Pasien`)  
**Date:** 2026-08-18  
**Scope:** `docs/contexts/apotek/` plus the Apotek rows in `docs/ARTIFACTS.md`  
**Skill:** `docs/skills/artifact-management-skill.md`

Moves were executed in this pass. Historical reports are under `archive/`. Remaining open work is under `working/`.

Authority: ADR > Active Design > Working Document > Archived Document.

---

## Layout after cleanup

```text
docs/contexts/apotek/

    apotek-domain.md
    apotek-domain-id.md
    outpatient-apotek-workflow.md
    outpatient-apotek-workflow-id.md
    outpatient-apotek-screen-and-aggregate-design.md
    outpatient-apotek-persistence-design.md

    adr/
        ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md
        ADR-APT-002-pharmacy-stock-ledger-boundary.md
        ADR-APT-003-invoice-mutability-owned-by-tata-rekening.md

    sop/
        DAFTAR-SOP-APT-RJ.md
        SOP-APT-RJ-001-* … SOP-APT-RJ-007-* (EN + ID)

    working/
        outpatient-apotek-repository-gap-analysis-report.md
        APOTEK-AVAILABLE-STOCK-CONCEPT-INTRODUCTION.md
        APOTEK-ARTIFACT-CLEANUP-REPORT.md

    archive/
        ALN-001 … ALN-007 resolution reports
        alignment / terminology / queue-mapping reviews
        invoice-mutability, credit-note, unfulfilled, financial-clearance analyses
```

**Agent read recipe:**

1. `apotek-domain.md` (+ ID if needed)
2. `outpatient-apotek-workflow.md`
3. SOP for the WF under change
4. Screen/aggregate + persistence
5. `adr/ADR-APT-00*`
6. `working/` only for BC-11–13, Available Stock formula (PD-09), implementation gaps

Do not open `archive/` unless reconstructing why a decision was made.

---

## Active

| File | Type |
|---|---|
| `apotek-domain.md` / `apotek-domain-id.md` | Domain |
| `outpatient-apotek-workflow.md` / `outpatient-apotek-workflow-id.md` | Workflow |
| `outpatient-apotek-screen-and-aggregate-design.md` | Screen + aggregate |
| `outpatient-apotek-persistence-design.md` | Persistence (closed PD-* are current; open: PD-03, PD-09, PD-08) |
| `adr/ADR-APT-001` … `003` | ADR |
| `sop/` | SOP index + SOP-APT-RJ-001–007 EN/ID |

---

## Working (kept)

| File | Open items still live here | Already absorbed into Active |
|---|---|---|
| `working/outpatient-apotek-repository-gap-analysis-report.md` | **BC-11** call display wording; **BC-12** permission matrix; **BC-13** physical-prescription policy beyond schema; EC/MI/TD implementation catalog | BA-01–BA-09, BC-01–BC-10, BC-14, ALN-001–007 |
| `working/APOTEK-AVAILABLE-STOCK-CONCEPT-INTRODUCTION.md` | Available Stock formula (**PD-09**); owner of the formula; runtime vs snapshot; SOP actor-table wording | Available Stock ≠ Current Stock in domain, screen non-decision, ADR-APT-002 / BA-09 |

Persistence **PD-04 Call Purpose** (closed) and **PD-01** (closed) do **not** fully close Gap Analysis BC-11 and BC-13. Display wording, authenticity, image retention, and duplicate detection remain business/ops questions.

---

## Move: archived

Reason: accepted decisions were already in ADR / domain / SOP / workflow / screen / persistence. Leaving reports at the context root caused withdrawn freeze, Apotek Credit Note tables, and pre-alignment wording to compete with Active design.

| Move | Reason |
|---|---|
| `archive/ALN-001-RESOLUTION-REPORT.md` | BA-03 absorbed into domain §5.7/§6.5, screen design, gap BA-03 |
| `archive/ALN-002-RESOLUTION-REPORT.md` | BA-08 absorbed into domain, workflow, SOP |
| `archive/ALN-003-RESOLUTION-REPORT.md` | No Billing/Fulfillment Allocation; SO → Invoice / Dispensing |
| `archive/ALN-004-RESOLUTION-REPORT.md` | BA-09 Mutasi / Remove Stock / `Prepared` is Dispensing; ADR-APT-002 |
| `archive/ALN-005-RESOLUTION-REPORT.md` | Queue `Done` from Pickup Call or No Show; ADR-APT-001, WF-007 |
| `archive/ALN-006-RESOLUTION-REPORT.md` | BC-10 / `BR-APT-110` / `BR-APT-118` |
| `archive/ALN-007-RESOLUTION-REPORT.md` | Dispensing lifecycle names; ADR-APT-001 examples |
| `archive/APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md` | Remaining alignment 0; pre-ADR-APT-003 snapshot |
| `archive/APOTEK-TERMINOLOGY-ALIGNMENT-REPORT.md` | Vocabulary now in living files |
| `archive/APOTEK-QUEUE-MAPPING-ALIGNMENT-REPORT.md` | Mapping target now in living files |
| `archive/APOTEK-INVOICE-MUTABILITY-IMPACT-ANALYSIS.md` | Absorbed into **ADR-APT-003**, `BR-APT-027`, **PD-05**. Body still describes freeze as current — highest bias. |
| `archive/APOTEK-INVOICE-MUTABILITY-ARTIFACT-UPDATE-REPORT.md` | Canonical update changelog complete |
| `archive/APOTEK-CREDIT-NOTE-OWNERSHIP-RESOLUTION.md` | **PD-07** closed in persistence; ADR-APT-003 |
| `archive/APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md` | KEEP `BILRG_AptUnfulfilledOutcome` is closed in persistence. Optional REWORK is not required for table existence. |
| `archive/APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md` | BA-08 absorbed. §6 freeze withdrawn. Remaining phrase hygiene (`BR-APT-046` / `BR-APT-118` / SOP-003 / DAFTAR Payment Clearance ≠ `LUNAS`) belongs on Active files, not a living investigation. |

---

## Hygiene done with this cleanup

- `docs/ARTIFACTS.md` Apotek table lists **Active + Working only**. Archive is a one-line note.
- Ghost index row `outpatient-apotek-stock-shortage-sales-order-impact-analysis.md` removed.
- Persistence open Available Stock formula renumbered **PD-04 → PD-09** so it no longer collides with closed **PD-04 Call Purpose**.
- Active links to gap analysis, PD-07 resolution, and ADR-APT-003 related analysis now point at `working/` or `archive/`.

---

## Still not done (Active hygiene, not archive work)

1. Informal *financial clearance* in `BR-APT-046`, `BR-APT-118`, and SOP-003 — replace with Payment/Coverage Clearance / Dispense Authorized wording.
2. `sop/DAFTAR-SOP-APT-RJ.md` maps Payment Clearance → Status Lunas; that collapse is still wrong.
3. ADR-APT-001 filename typo `queu` (TD-10).
4. Persistence header still says “Proposed design for architect review”; closed PD-* are current persistence truth.
