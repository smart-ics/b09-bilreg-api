# APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md

**Artifact status:** Pre-implementation cross-artifact consistency review  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Review date:** 2026-08-16  
**Review type:** Artifact alignment and business consistency only. Code, repository readiness, architecture quality, implementation complexity, and roadmap sequencing were not evaluated.

**Artifacts reviewed:**

1. `docs/contexts/apotek/apotek-domain.md`
2. `docs/contexts/apotek/outpatient-apotek-workflow.md`
3. `docs/contexts/apotek/outpatient-apotek-screen-and-aggregate-design.md`
4. `docs/contexts/apotek/adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md`
5. `docs/contexts/apotek/sop/SOP-APT-RJ-001-Antrian-dan-Mapping-EN.md`
6. `docs/contexts/apotek/sop/SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md`
7. `docs/contexts/apotek/sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md`
8. `docs/contexts/apotek/sop/SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-EN.md`
9. `docs/contexts/apotek/sop/SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-EN.md`
10. `docs/contexts/apotek/sop/SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-EN.md`
11. `docs/contexts/apotek/sop/SOP-APT-RJ-007-Penanganan-Obat-Tidak-Diambil-EN.md`
12. `docs/contexts/apotek/outpatient-apotek-repository-gap-analysis-report.md`

**Review rule applied:** Previously discussed items marked `Resolved`, `Resolved (Business Clarification)`, or `Resolved (Blocking Architecture)` are not re-opened when the current artifacts already match the ratified decision. An issue is reported only when a contradiction, inconsistent object, or unresolved ambiguity still appears in the current artifacts.

No new business rules were created. No architecture redesign is proposed. Missing behavior was not inferred. Recommended resolutions are limited to aligning stale wording with already-ratified decisions and the canonical English domain.

---

# Executive Summary

The outpatient Apotek artifacts describe one business reality for payer timing, queue ownership, handover gates, mixed coverage, and uncollected-medication resolution. Those sequences are consistent across the canonical domain, the English workflow, ADR-APT-001, and the English SOPs that were updated after the Gap Analysis.

The remaining defects are operational wording leftovers (ALN-006) and informal ADR pharmacy example names (ALN-007). ALN-001 through ALN-005 are closed. BA-09 stock ownership is aligned: Pharmacy Reserve is Stock Mutasi; handover is Remove Stock; No Show is return Mutasi; `Prepared` is a Dispense Order state. Pre-pickup-call No Show Resolution may complete the Queue Entry to `Done` without adding a queue status.

Open Gap Analysis items BC-11, BC-12, and BC-13 are unspecified operational detail (call-display wording, permission matrix, physical-prescription capture fields). They do not contradict other artifacts and are therefore not listed as alignment issues.

* Total Issues Found: 7
* Resolved in this follow-up: 5 (ALN-001, ALN-002, ALN-003, ALN-004, ALN-005)
* Remaining open: 2
* Critical remaining: 0
* High: 0
* Medium: 1
* Low: 1

---

# Alignment Status

| Area                 | Status      |
| -------------------- | ----------- |
| Business Terminology | Not Aligned |
| Workflow             | Aligned     |
| Aggregate Boundaries | Aligned     |
| Queue Ownership      | Aligned     |
| Payer Flows          | Aligned     |
| SOPs                 | Not Aligned |
| Screen Design        | Aligned     |

**Aligned areas, in brief:**

- **Workflow.** Triggers, preconditions, payer sequencing, pickup-call timing, `ServedAt` / `DoneAt` causation (pickup call or No Show Resolution), Final Dispense Review failure, and terminal No-Show commercial outcomes match between the domain and `WF-APT-RJ-001`–`007`.
- **Queue ownership.** Patient Tracker remains the only owner of queue identity and of `Waiting` / `In Service` / `Done` / `Withdrawn`. Pharmacy workflow states are not added to the queue lifecycle. ADR-APT-001 is respected. Queue completion may be triggered by Pickup Call or by No Show Resolution; Queue `Done` does not imply Medication Handover.
- **Payer flows.** General Patient invoice-before-preparation, BPJS invoice-only-at-handover, and mixed coverage as two independent Sales Orders with coordinated pickup are consistent across domain, workflow, SOP-003, SOP-004, SOP-005, and the screen workbenches.

**Not aligned areas, in brief:**

- SOP-002 exception 5.2 still blurs shortage timing (ALN-006). ADR-APT-001 still uses informal pharmacy example state names (ALN-007).

---

# Unresolved Alignment Issues

## ALN-001

### Severity

Critical

### Resolution status

Resolved (2026-08-16) — screen-and-aggregate design aligned to BA-03. See `ALN-001-RESOLUTION-REPORT.md`. BA-03 was not reopened.

### Artifacts Involved

- `apotek-domain.md` §5.7, §6.5
- `outpatient-apotek-screen-and-aggregate-design.md` §5.2
- `outpatient-apotek-repository-gap-analysis-report.md` BA-03

### Description

The canonical domain states that Outpatient Queue Mapping is an active association to an externally owned Pharmacy Queue Entry, not an Aggregate Root, and that Pharmacy Queue Close is an operational fact, not an Aggregate Root.

The screen-and-aggregate design previously placed both in the table titled **Aggregate roots and responsibilities**, with `OutpatientQueueMapping` in the Aggregate-root column.

BA-03 already decided that mapping is a navigation/association mechanism only. Screen design §5.2 now lists only `TelaahResep`, `SalesOrder`, `SalesInvoice`, and `DispenseOrder` as aggregate roots.

### Why It Is A Problem

This was conflicting aggregate ownership. An implementer following the previous screen design would have given mapping its own consistency boundary. That leftover labeling is removed.

### Recommended Resolution

Completed. Edit `outpatient-apotek-screen-and-aggregate-design.md` §5.2 so that the aggregate-root list matches the domain: `TelaahResep`, `SalesOrder`, `SalesInvoice`, and `DispenseOrder` only. Describe Outpatient Queue Mapping and Pharmacy Queue Close as associations or operational facts, not aggregate roots. Do not change the BA-03 decision.

### Related Gap Analysis Item

* Existing Gap — Resolved by artifact alignment. BA-03 remains the locked architectural decision.

---

## ALN-002

### Severity

High

### Resolution status

Resolved (2026-08-16) — screen design, SOP-002 / SOP-003 / SOP-004, Indonesian domain/workflow companions, and SOP glossary aligned to BA-08. See `ALN-002-RESOLUTION-REPORT.md`. BA-08 was not reopened.

### Artifacts Involved

- `apotek-domain.md` ubiquitous language `Dispense Authorized`; `BR-APT-040`, `BR-APT-043`
- `outpatient-apotek-workflow.md` (`Dispense Authorized Evaluated`; no Fulfillment Clearance object)
- `outpatient-apotek-screen-and-aggregate-design.md` §2 principle 8, §3.3, §5.1, §5.3, §5.6
- `SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md` steps 10 and 12
- `SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md` step 7
- `SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-EN.md` actors table and step 3
- `outpatient-apotek-repository-gap-analysis-report.md` BA-08

### Description

BA-08 and the English domain say there is no business concept called Fulfillment Clearance. Preparation authorization is `Dispense Authorized`: a policy evaluation result that must not be persisted as an aggregate, entity, source of truth, or transaction boundary.

The English workflow already followed that language. The screen-and-aggregate design and SOP-002 / SOP-003 / SOP-004 previously named or established `Fulfillment Clearance`. Those leftovers are removed.

SOP-005 already used `Dispense Authorized` correctly and remained the reference pattern.

### Why It Is A Problem

The same authorization gate was described as a non-persisted evaluation in the domain and as a record the system establishes in the screen design and in SOP-003 / SOP-004. That leftover labeling is removed.

### Recommended Resolution

Completed. Replace remaining `Fulfillment Clearance` wording in the screen-and-aggregate design and in SOP-002, SOP-003, and SOP-004 with `Dispense Authorized` as a policy evaluation result. Do not keep a parallel clearance object. Use SOP-005 as the already-corrected pattern. Do not change BA-08.

### Related Gap Analysis Item

* Existing Gap — Resolved by artifact alignment. BA-08 remains the locked architectural/business decision.

---

## ALN-003

### Severity

High

### Resolution status

Resolved (2026-08-16) — Billing Allocation and Fulfillment Allocation removed from SOP-002 / SOP-003 / SOP-004 / SOP-006 and Indonesian companions. See `ALN-003-RESOLUTION-REPORT.md`. No replacement allocation object was added.

### Artifacts Involved

- `apotek-domain.md` §1.4, §5.2–§5.4, §6.2–§6.5, `BR-APT-015`, `BR-APT-021`, `BR-APT-029`
- `outpatient-apotek-workflow.md` `WF-APT-RJ-002` main flow steps 7–8
- `SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md` §1.1 diagram, steps 10–11, completion criterion 2
- `SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md` preconditions 2 and 4, steps 1 and 5
- `SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-EN.md` precondition 2, step 1
- `SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-EN.md` step 2

### Description

The domain and English workflow coordinate commercial and physical fulfillment only through Sales Invoice Items and Dispense Order Lines that reference Sales Order Lines. No Billing Allocation or Fulfillment Allocation object exists in ubiquitous language, aggregates, or workflow steps.

SOP-002 previously defined a data-flow layer through Billing Allocation and Fulfillment Allocation. Those labels are removed. Commercial flow is Sales Order Line → Sales Invoice Item. Fulfillment flow is Sales Order Line → Dispense Order Line.

### Why It Is A Problem

This was unauthorized business structure. The leftover labeling is removed.

### Recommended Resolution

Completed. Remove Billing Allocation and Fulfillment Allocation from SOP-002, SOP-003, SOP-004, and SOP-006. Describe the commercial path as Sales Invoice Items from Sales Order Lines and the fulfillment path as Dispense Order Lines from Sales Order Lines, matching `WF-APT-RJ-002` and the domain. Do not add a new allocation aggregate.

### Related Gap Analysis Item

* Not Found — resolved by artifact alignment to the canonical domain.

---

## ALN-004

### Severity

High

### Resolution status

Resolved (2026-08-16) — leftover Stock Reservation / Inventory Issue / `Stock Reserved` / In-Transit custody labels aligned to BA-09. See `ALN-004-RESOLUTION-REPORT.md`. BA-09 was not reopened.

### Artifacts Involved

- `apotek-domain.md` §5.13, `BR-APT-098`–`BR-APT-104`; domain event catalog
- `outpatient-apotek-workflow.md` (`Pharmacy Reserve` as Stock Mutasi; events `Stock Transferred to Dispensing Temporary Unit`)
- `outpatient-apotek-screen-and-aggregate-design.md` §3.3, §3.4, §5.5
- `SOP-APT-RJ-002` through `SOP-APT-RJ-007` (EN and ID companions)
- `outpatient-apotek-repository-gap-analysis-report.md` BA-09

### Description

BA-09 and the domain rules state that Pharmacy Reserve is implemented only as Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit, that there is no separate `ReserveStock` contract, that Medication Handover requests Remove Stock, and that No-Show resolution requests return Mutasi. `Prepared` is a Dispense Order state, not an Inventory state.

The English workflow already used that language. The domain event catalog, screen design, SOP-002 / SOP-003 / SOP-004 / SOP-006 / SOP-007, and Indonesian companions previously used Stock Reservation, Inventory Issue, `Stock Reserved`, or In-Transit. Those leftovers are removed.

### Why It Is A Problem

This was a leftover Inventory contract. The leftover labeling is removed.

### Recommended Resolution

Completed. Align leftover names with `BR-APT-098`–`BR-APT-104` and with SOP-005 / the English workflow. Domain and workflow event catalogs now share Mutasi / Remove Stock / return Mutasi names. Do not restore a `ReserveStock` or Inventory Issue aggregate. Do not change BA-09.

### Related Gap Analysis Item

* Existing Gap — Resolved by artifact alignment. BA-09 remains the locked architectural decision.

---

## ALN-005

### Severity

Medium

### Resolution status

Resolved (2026-08-16) — pre-Pickup-Call No Show queue completion aligned to the ratified business clarification. See `ALN-005-RESOLUTION-REPORT.md`. ADR-APT-001 ownership boundaries were not changed.

### Artifacts Involved

- `apotek-domain.md` §1.3, §8.7, `BR-APT-095`
- `outpatient-apotek-workflow.md` `WF-APT-RJ-007`
- `SOP-APT-RJ-007-Penanganan-Obat-Tidak-Diambil-EN.md`
- `adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md` (ownership unchanged; completion-trigger clarification only)
- `BR-APT-081`, `BR-APT-145` (unchanged: `ServedAt` at preparation start; Pharmacy Queue Close does not apply after `In Service`)

### Description

The domain pickup lifecycle allows No-Show from `Prepared` before the pickup call. After `Medication Preparation Started`, the queue is already `In Service` and Pharmacy Queue Close is not allowed.

Previous workflow and SOP-007 wording treated No-Show resolution as occurring after queue completion and did not define Patient Tracker recording when uncollected resolution happens while the queue is still `In Service`.

The artifacts now state one queue path: completion may be triggered by Pickup Call or by No Show Resolution; the Queue Entry may already be `Done` before No Show Resolution; `DoneAt` is recorded when completion occurs and is never reversed; no new queue status is introduced; no pharmacy workflow state is added to QueueEntry.

### Why It Is A Problem

This was operational ambiguity, not a second payer or fulfillment rule. Staff following the domain could close uncollected medication before the pickup call while staff following the previous `WF-APT-RJ-007` wording assumed the queue was already complete. That leftover path is now jointly defined.

### Recommended Resolution

Completed. State, in the domain, workflow, SOP-007, and related queue explanations, that when `WF-APT-RJ-007` runs before the pickup call, Apotek may complete the associated Queue Entry (`In Service` → `Done`) and Patient Tracker records `DoneAt`. When the Queue Entry is already `Done`, `DoneAt` is retained. `DoneAt` is never reversed. Queue `Done` does not imply Medication Handover. Do not add a pharmacy state to the queue. Do not invent a new queue status. Do not change ADR-APT-001 ownership.

### Related Gap Analysis Item

* Not Found — BC-01 resolved Pickup Expired versus Dispense Order `Expired`. The pre-pickup-call queue path is now specified by the ALN-005 business clarification.

---

## ALN-006

### Severity

Medium

### Artifacts Involved

- `apotek-domain.md` `BR-APT-110`, `BR-APT-118`
- `outpatient-apotek-workflow.md` `WF-APT-RJ-002` alternative flow versus exception flow
- `SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md` exception 5.2
- `SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md` exception 5.4

### Description

The domain and English workflow distinguish two shortage timings:

- Before Sales Order establishment: include only fulfillable lines in the Sales Order; leave the rest on the Prescription; issue Salinan Resep (`BR-APT-110`).
- After Sales Order establishment or financial clearance: keep the Sales Order; assign an Unfulfilled Medication Outcome and commercial correction when required (`BR-APT-118`). Do not rebuild the Sales Order as a partial order.

`WF-APT-RJ-002` keeps that split.

SOP-002 exception 5.2 is titled “Stock is insufficient after acceptance” and then applies the before-Sales-Order treatment (include only fulfillable lines; unfulfillable lines remain on the Prescription). “After acceptance” is not the same cut as “after Sales Order establishment”. SOP-003 5.4 correctly describes shortage after payment as Unfulfilled Medication Outcome plus financial consequence.

### Why It Is A Problem

An operator using only SOP-002 5.2 after a Sales Order already exists would try to drop lines from that Sales Order. The domain forbids that path and requires an Unfulfilled Medication Outcome instead. The two shortage timings remain defined; SOP-002 blurs the cutover.

### Recommended Resolution

Reword SOP-002 5.2 so it applies only before Sales Order establishment, matching `BR-APT-110` and the `WF-APT-RJ-002` alternative flow. Point post-establishment shortage to SOP-003 5.4 / `BR-APT-118`. Do not add a new shortage reason.

### Related Gap Analysis Item

* Existing Gap But Not Resolved — BC-10 is marked resolved in the domain and workflow; SOP-002 5.2 still collapses the two timings.

---

## ALN-007

### Severity

Low

### Artifacts Involved

- `adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md` example pharmacy states
- `apotek-domain.md` Dispense Order lifecycle and ubiquitous language

### Description

ADR-APT-001 is authoritative for queue ownership and is consistent with the domain on that point. Its illustrative pharmacy labels (`Sales Confirmation`, `Payment Confirmation`, `WaitingPayment`, `Paid`, `Dispensing`, `Dispensed`, `HandedOver`) are not the domain Dispense Order states (`Established`, `Awaiting Clearance`, `Released`, `Preparing`, `Prepared`, `Reviewed`, `Completed`, `Cancelled`, `Expired`, `Unfulfilled`) and are not the Serah Obat projection categories.

SOP-007 and the Dispensing screen now use Dispensing Temporary Custody (ALN-004). The remaining leftover is the ADR example labels.

These are naming leftovers. They do not assign pharmacy states to `AntrianStatusEnum` and do not change payer or handover rules.

### Why It Is A Problem

Readers may treat ADR examples or “In-Transit” as a second pharmacy lifecycle. That is documentation noise, not a second business outcome, as long as implementers follow the domain state machine and ADR-APT-001’s ownership rule.

### Recommended Resolution

In ADR-APT-001, label the pharmacy examples as non-canonical illustrations of ownership, or replace them with domain Dispense Order states. Do not change queue ownership.

### Related Gap Analysis Item

* Existing Gap But Not Resolved — Gap Analysis TD-10 already noted informal ADR pharmacy state names; the ADR examples and In-Transit wording remain.

---

# Final Assessment

**Partially Aligned (Requires Clarification)**

The canonical English domain, English workflow, ADR-APT-001, and the payer SOPs that were fully updated (especially SOP-001 and SOP-005) already describe one outpatient pharmacy business: Tracker-owned queue lifecycle, Apotek-owned fulfillment, General / BPJS / mixed invoice timing, `Dispense Authorized` as evaluation rather than an aggregate, mapping as association rather than an aggregate, Mutasi/Remove Stock as the stock boundary, and manual uncollected resolution after Pickup Expired.

The artifacts are not yet internally consistent enough for implementation planning only because ALN-006 (SOP-002 shortage timing wording) remains, plus informal ADR example names in ALN-007. ALN-001 through ALN-005 are closed. The remaining leftovers are documentation-sync failures against decisions the Gap Analysis already ratified.

This assessment does not reopen BA-01 through BA-09, BC-01 through BC-10, or BC-14. It does not treat open items BC-11, BC-12, or BC-13 as alignment contradictions. After ALN-006 is removed by applying those existing decisions to the stale artifacts, the outpatient Apotek set would be ready to proceed to implementation planning from a business-consistency standpoint.
