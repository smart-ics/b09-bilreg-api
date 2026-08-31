# Apotek — `BILRG_AptResepRevisionTask` Consistency Report

```yaml
Artifact-Type: ConsistencyReport
Status: Working
Normative-Level: Historical
Superseded-By: outpatient-apotek-persistence-design.md (PD-11)
Archive-Eligible: Yes
```

**Date:** 2026-08-18  
**Decision:** PD-11 — Remove `BILRG_AptResepRevisionTask`. Source-prescription version change after Resep Kerja intake is not an Apotek business capability.

---

## 1. Conclusion: REMOVE

`BILRG_AptResepRevisionTask` was a technical safeguard around a possible source-version change. It did not represent a documented business requirement.

---

## 2. Investigation answers

### 2.1 Is there an explicit business requirement for Apotek to detect and manage source-prescription change after Resep Kerja exists?

**No numbered Apotek business rule requires it.**

| Artifact | Finding |
|---|---|
| Domain (`BR-APT-*`) | No rule requires source-version detection, a revision worklist, or an accountable revision task. `BR-APT-002` keeps original Resep ownership with the clinical authority. `BR-APT-005` places clarification with the Dokter Penulis Resep **outside the system**. `BR-APT-050` handles later medication replacement by cancelling and **re-reviewing the same original Resep**, without a corrected or replacement Resep. `BR-APT-105` allows re-review of that same Resep while the Registration is active; it is not source-revision detection. |
| Workflow | `WF-APT-RJ-001`–`007` cover queue mapping, intake, Telaah, sale, coverage, multi-demand coordination, and uncollected medication. None is a post-intake source-revision workflow. Clinical Order modification and CPOE lifecycle changes are **explicitly excluded**. |
| SOP APT-RJ-001–007 | No procedure for doctor revision after pharmacy intake, pharmacy review of that revision, or tracking it as an operational task. |
| ADR-APT-001–003 | Queue vs pharmacy state, Stock Ledger boundary, Invoice mutability. None decides prescription-revision tasks. |
| Screen design | Exception Worklist covers No Show, expiry, return, Invoice correction, and Inventory disposition. It does not include source-prescription revision. |
| Domain events | No `Prescription Revision Detected` (or equivalent) event. |
| Open decisions | No open PD/OD that treats revision detection as unresolved business policy. BA-06 had invented the task as an architecture note; that note is withdrawn by PD-11. |

The only affirmative wording was in BA-06 (“source prescription revisions must be detected… create an operational review task”) and in supporting sentences copied into domain, screen, and persistence. That wording protected **silent drift from clinical source changes**. It did not cite SOP, regulatory obligation, or an accountable pharmacy action.

### 2.2 Is there a workflow where a doctor revises after intake, pharmacy must review it, and the revision is an accountable operational task?

**No.**

Documented doctor–pharmacy interaction after intake is:

1. Out-of-system clarification; Telaah stays `Under Review` until the Pharmacist decides (`BR-APT-005`).
2. CPOE may modify an Active Clinical Order (`BR-CPOE-012`–`015`), but outpatient Apotek workflow **excludes** Clinical Order modification.
3. Pharmacist re-review of the **same** original Resep (`BR-APT-105`, `BR-APT-050`).

CPOE records its own Order History. Apotek does not subscribe to that history as a pharmacy task.

### 2.3 Does any business consequence depend on revision detection?

**None is specified.**

The withdrawn table stored `DetectedAt`, `SourceVersionToken`, `TaskStatus`, `ResolvedBy`, `ResolvedAt`, and `ResolutionNote`. It did not define re-review, re-approval, cancellation, re-dispensing, Invoice correction, patient notification, audit obligation, or regulatory action. Resolution did not mutate Resep Kerja. No downstream aggregate was required to consume the task.

### 2.4 If the table is removed, what business capability becomes impossible?

**None that Active artifacts require.**

Pharmacy still:

- creates Resep Kerja at intake from the Prescription Contract;
- processes that snapshot, not live CPOE/legacy rows;
- does not silently rewrite the snapshot from the clinical source;
- performs Telaah, Sales Order, Invoice, and Dispensing on that copy;
- re-reviews the same original Resep while the Registration is active.

What is no longer designed is **watching the source for a later version and queuing a pharmacy task**. That watching was never a documented operational capability.

### 2.5 Was the design solving a business problem or only a technical scenario?

**Technical scenario only:** source data might change after intake.

That possibility is not sufficient. CPOE owns modification of Active orders. Apotek owns an intake snapshot. Silent live-sync remains forbidden without a revision-task table.

---

## 3. What was removed

| Object | Action |
|---|---|
| `BILRG_AptResepRevisionTask` | Removed from write catalog; listed forbidden (PD-11) |
| Identifier prefix `ARV` | Removed |
| ERD `revision_detected` | Removed |
| `SourceVersionToken` on `BILRG_AptResepKerja` | Removed (existed only for detection) |
| `ResepKerjaStatus` `SupersededForReview` | Removed; remaining values `Active`, `Voided` |
| BA-06 “Prescription revision handling” | Withdrawn |
| Domain / screen sentences that source revisions create a review task | Replaced by snapshot non-rewrite wording |

No SQL, DTO, DAL, repository, or application code implemented the table.

---

## 4. Modified artifacts

| Artifact | Classification | Changes |
|---|---|---|
| [`outpatient-apotek-persistence-design.md`](../outpatient-apotek-persistence-design.md) | Active | Closed **PD-11**. Table catalog, omitted/forbidden, identifier prefixes, ERD, §8.1 shape, §15 checklist. |
| [`apotek-domain.md`](../apotek-domain.md) | Active | §5.14 Resep Kerja: intake snapshot is not silently rewritten; no review task. |
| [`apotek-domain-id.md`](../apotek-domain-id.md) | Active | Parallel Indonesian wording. |
| [`outpatient-apotek-screen-and-aggregate-design.md`](../outpatient-apotek-screen-and-aggregate-design.md) | Active | §5.2 Resep Kerja “does not own” column. |
| [`working/outpatient-apotek-repository-gap-analysis-report.md`](./outpatient-apotek-repository-gap-analysis-report.md) | Working | BA-06 decision, rationale, and §9.1 summary. |
| [`docs/ARTIFACTS.md`](../../../ARTIFACTS.md) | Index | This report listed under Apotek Working. |

### Unchanged (already consistent)

| Artifact | Note |
|---|---|
| `outpatient-apotek-workflow.md` / `-id.md` | No revision workflow; CPOE modification already excluded. |
| SOP APT-RJ-001–007 | No revision SOP. |
| ADR-APT-001–003 | No revision-task decision. |

Archived Apotek reports were not edited.

---

## 5. Verification

| Search | Result in Active/Working after PD-11 |
|---|---|
| `BILRG_AptResepRevisionTask` | Persistence only: forbidden (PD-11) and this report |
| `SourceVersionToken` | Persistence only: PD-11 removal statement |
| `SupersededForReview` | Persistence only: PD-11 removal statement |
| Identifier prefix `ARV` | Persistence only: PD-11 result “no `ARV` prefix” |
| Source code / SQL for the table | None |

---

## 6. Cross-cutting consistency matrix

| Concern | Consistent? | Authority |
|---|---|---|
| Table catalog | Yes | Persistence §6.1 has no revision task; §6.2 forbids it |
| ERD | Yes | §7 has no `revision_detected` |
| Aggregate map | Yes | Roots remain Telaah, Sales Order, Invoice, Dispensing; Resep Kerja is still a supporting document |
| Snapshot rule | Yes | Resep Kerja is intake copy; not live-synced; not silently rewritten |
| Re-review | Yes | `BR-APT-105` / `BR-APT-050` on the same original Resep; not a source-version task |
| Clarification | Yes | `BR-APT-005` remains out of system |
| CPOE modification | Yes | Workflow exclusion; CPOE Order History stays in CPOE |
| SOP / worklist | Yes | No orphan revision procedure or Exception Worklist category |

---

## 7. Traceability

```text
Decision absorbed into:
    PD-11 (outpatient-apotek-persistence-design.md)
    Resep Kerja supporting-document text (apotek-domain.md / apotek-domain-id.md)
    Screen aggregate catalog (outpatient-apotek-screen-and-aggregate-design.md)
    BA-06 (outpatient-apotek-repository-gap-analysis-report.md)
```

This report is an Archive Candidate once PD-11 is accepted in architect review.
