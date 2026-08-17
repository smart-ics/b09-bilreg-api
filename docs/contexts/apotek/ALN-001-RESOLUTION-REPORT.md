# ALN-001 Resolution Report

**Issue:** ALN-001 — OutpatientQueueMapping Ownership Alignment  
**Status:** Resolved  
**Date:** 2026-08-16  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Scope:** Document alignment only. No architecture redesign. No business-behavior, workflow, or ownership-rule change beyond applying the locked BA-03 decision to stale wording.

**Authoritative source (locked):** `docs/contexts/apotek/outpatient-apotek-repository-gap-analysis-report.md` — BA-03

BA-03 was not re-analyzed, reinterpreted, challenged, replaced, or redesigned.

---

## Locked decision applied (BA-03)

`OutpatientQueueMapping` is not an aggregate root. It is a navigation/association mechanism only. It does not own business lifecycle, workflow state, approval state, operational progress, or transactional consistency. It does not maintain active/inactive relationship state. It does not establish an independent consistency boundary. Queue identity and lifecycle remain owned by Patient Tracker. Medication demand lifecycle remains owned by the corresponding Pharmacy aggregates (`Sales Order`, `Dispensing`, and related roots). The association exists only to answer operational navigation and worklist questions.

The canonical domain (`apotek-domain.md` §5.7, §6.5) already matches that decision: Outpatient Queue Mapping is not an Aggregate Root; Pharmacy Queue Close is an operational fact, not an Aggregate Root and not a queue state.

The Apotek aggregate-root list remains:

- `TelaahResep`
- `SalesOrder`
- `Invoice`
- `Dispensing`

---

## What was changed

| Artifact | Change |
|---|---|
| `outpatient-apotek-screen-and-aggregate-design.md` | Removed `OutpatientQueueMapping` and Pharmacy Queue Close from the aggregate-root catalog. Reclassified mapping as a navigation/association mechanism (BA-03). Reclassified Pharmacy Queue Close as an operational fact/event. Updated diagrams, principles, invariants, and the aggregate-impact matrix so they use the same classification. |
| `APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md` | Marked ALN-001 resolved and removed it from the remaining-defect summary. Did not reopen BA-03. |
| `docs/ARTIFACTS.md` | Indexed this report. |

No code, API, workflow SOP steps, queue milestone causation, payer rules, or domain business rules were changed.

---

## Sections modified in the screen-and-aggregate design

| Section | Modification |
|---|---|
| Header — Related artifacts | Linked the Gap Analysis report as the BA-03 source. |
| §1 Decision Summary | Stated that Outpatient Queue Mapping and Pharmacy Queue Close do not add screens or aggregates. |
| §2 Governing Principles, principle 5 | Replaced “Apotek owns its mapping” with BA-03 language: mapping is navigation/association only; Close is an operational fact/event; Patient Tracker owns queue lifecycle; Pharmacy aggregates own demand lifecycle. |
| §3.5 Patient Medication Journey | Clarified that Queue Mapping in the projection is the same association, not an aggregate. |
| §4.2 Queue milestones | Unchanged. `Withdrawn` remains caused by Pharmacy Queue Close as an Apotek action against Tracker-owned lifecycle. |
| §5.1 Aggregate map | Mapping and Close are drawn as non-aggregate nodes (dotted association / fact), not ownership edges between aggregate roots. |
| §5.2 Aggregate roots and responsibilities | Aggregate-root table now contains only `TelaahResep`, `SalesOrder`, `Invoice`, and `Dispensing`. Mapping and Close moved to a separate classification table. |
| §5.4 Relationships and invariants | Item 6 restates mapping as association only. Item 11 states Close is a fact/event, not an aggregate or queue state. |
| §5.6 Aggregate review matrix | Added BA-03 mapping and Pharmacy Queue Close rows: none added as aggregates. Conclusion lists the four roots only. |

Screen workbench behavior in §3.2 (map demand; close from `Waiting` with mandatory reason; Close unavailable after `In Service`) is unchanged.

---

## Evidence that the updated screen design matches BA-03

| BA-03 statement | Screen-design evidence after alignment |
|---|---|
| `OutpatientQueueMapping` is not an aggregate root. | §5.2 aggregate-root table no longer includes it. Explicit sentence: not an aggregate root. |
| It is a navigation/association mechanism only. | §1, §2 principle 5, §5.1 node label, §5.2 classification table, §5.4 item 6, §5.6 matrix. |
| It does not own lifecycle, workflow state, approval state, operational progress, or transactional consistency. | §5.2 “Does not own” column for mapping copies this boundary. |
| It does not maintain active/inactive relationship state. | §5.2 “Does not own” column. |
| It does not establish an independent consistency boundary. | §5.2 “Does not own” column. |
| Queue identity and lifecycle remain owned by Patient Tracker. | §2 principle 5; §4.1; §5.2; §5.5 Patient Tracker row (unchanged). |
| Medication demand lifecycle remains owned by Pharmacy aggregates (`Sales Order`, `Dispensing`, and related roots). | §2 principle 5; §5.2 four-root list matching domain §6.1–§6.4. |
| Association answers which queue serves which demand, and which demands are associated with a queue entry. | §5.2 mapping responsibility text. |
| Domain: Pharmacy Queue Close is an operational fact, not an Aggregate Root and not a queue state. | §2 principle 5; §5.1; §5.2; §5.4 item 11; §5.6. Tracker still sets `Withdrawn` (§4.2). |

Domain §6.5 and Gap Analysis §9.1 BA-03 conclusion are unchanged and remain consistent with the corrected §5.2.

---

## Confirmation: alignment only

This resolution did **not**:

- change BA-03 or any other Gap Analysis item;
- add, remove, or rename a Pharmacy aggregate root other than removing the incorrect labels;
- change queue milestone causation (`ServedAt` / `In Service` from first `Medication Preparation Started`; `DoneAt` / `Done` from coordinated pickup call; `Withdrawn` from Pharmacy Queue Close);
- change mapping cardinality (one queue entry may associate to multiple independent medication demands);
- change Close eligibility (from `Waiting` only; not after `In Service`; no `ServedAt` / `DoneAt`; no Sales Order / Dispensing / handover establishment);
- change Pelayanan Penjualan, Dispensing, or Serah Obat workbench sequences;
- change payer, invoice, dispense, stock, or handover rules;
- invent a new queue status or a mapping consistency boundary.

ALN-002 through ALN-007 are out of scope and remain as previously reported.
