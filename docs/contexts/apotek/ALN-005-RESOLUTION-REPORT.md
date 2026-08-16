# ALN-005 Resolution Report

**Issue:** ALN-005 — Queue Completion During No Show Resolution  
**Status:** Resolved  
**Date:** 2026-08-16  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Scope:** Artifact alignment only. No queue architecture redesign. No new queue status. No pharmacy workflow state added to `QueueEntry`.

**Authoritative source:** Business Clarification Decision ratified with this resolution (pre-Pickup-Call No Show handling).

ADR-APT-001 ownership boundaries were not re-analyzed, reinterpreted, replaced, or redesigned.

---

## Locked decision applied

When `WF-APT-RJ-007` (No Show Resolution) is executed before Pickup Call occurs, Apotek **may** complete the associated Queue Entry.

Queue lifecycle remains:

```text
Waiting → In Service → Done
```

No new queue status is introduced.

Queue completion may be triggered by:

1. Pickup Call  
2. No Show Resolution  

Queue `Done` does **not** imply medication handover. Queue `Done` only means the queue service lifecycle has been completed.

Additional alignment facts:

- The Queue Entry may already be `Done` before No Show Resolution (typical after Pickup Call).
- `DoneAt` is recorded when queue completion occurs.
- `DoneAt` is never reversed.
- No pharmacy workflow state is added to QueueEntry.
- Pharmacy Queue Close (`Waiting` → `Withdrawn`) remains a different path and does not apply after `Medication Preparation Started` (`BR-APT-145`).

This decision does not change ADR-APT-001 ownership boundaries: Patient Tracker owns queue identity and lifecycle; Pharmacy owns No Show handling and fulfillment outcomes.

---

## Files modified

| File | Change |
|---|---|
| `apotek-domain.md` | §1.3 queue milestones; `BR-APT-095`; §8.7 handover narrative; `Outpatient No-Show Recorded` event |
| `apotek-domain-id.md` | Companion of the same sections |
| `outpatient-apotek-workflow.md` | `WF-APT-RJ-003` / `004` alternative flows; `WF-APT-RJ-007` inputs, main flow, exceptions, outcomes, events; §8 handoffs; §9 `DoneAt`; §10 traceability |
| `outpatient-apotek-workflow-id.md` | Companion of the same sections |
| `sop/SOP-APT-RJ-007-*-EN.md` and `-ID.md` | Patient Tracker actor; step 12; exceptions 5.3–5.4; completion criterion 6 |
| `sop/SOP-APT-RJ-003-*-EN.md` and `-ID.md` | Exception: Patient does not collect |
| `sop/SOP-APT-RJ-004-*-EN.md` and `-ID.md` | Exception: Patient does not collect |
| `sop/SOP-APT-RJ-005-*-EN.md` and `-ID.md` | Exception: No-Show refers to SOP-007 queue completion |
| `outpatient-apotek-screen-and-aggregate-design.md` | §3.4 Serah Obat note; §4.2 `DoneAt` / `Done` milestone |
| `adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md` | Completion-trigger clarification and Allowed example `Done` + No Show / `Expired`. Ownership lists unchanged. |
| `APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md` | ALN-005 marked resolved; remaining-defect counts updated |
| `docs/ARTIFACTS.md` | Indexed this report |

Unchanged ownership and close path: `BR-APT-081` (`ServedAt` at `Medication Preparation Started`); `BR-APT-143`–`BR-APT-145` (Pharmacy Queue Close from `Waiting` only).

---

## Sections modified

| Artifact | Section | Modification |
|---|---|---|
| Domain | §1.3 | `DoneAt` recorded at queue completion: Pickup Call **or** No Show Resolution while still `In Service`. Lifecycle unchanged. `Done` ≠ handover. |
| Domain | `BR-APT-095` | Both completion triggers; already-`Done` retains `DoneAt`; never reverse; not Pharmacy Queue Close. |
| Domain | §8.7 | No-Show from `Prepared` / Ready for Pickup / Patient Called can complete or retain queue `Done`. |
| Domain | Event catalog | `Outpatient No-Show Recorded` may complete an `In Service` queue. |
| Workflow | `WF-APT-RJ-003`, `WF-APT-RJ-004` | Split “does not collect” into after vs before pickup call. |
| Workflow | `WF-APT-RJ-007` | Input allows `In Service` or `Done`; main-flow step completes `In Service` queue; exception no longer says resolution only after completion. |
| Workflow | §8, §9, §10 | Handoff from No-Show while `In Service`; `DoneAt` timing; Patient Tracker as `WF-APT-RJ-007` external authority. |
| SOP-007 | Actors, step 12, 5.3, 5.4, criterion 6 | Two branches: already `Done` vs still `In Service`. |
| SOP-003 / 004 / 005 | Uncollected exceptions | Point to SOP-007 for both queue states. |
| Screen design | §3.4, §4.2 | Normal pickup still records `DoneAt`; No Show before pickup may also complete the queue. |
| ADR-APT-001 | Decision + Allowed | Completion triggers stated; ownership tables and `AntrianStatusEnum` rule unchanged. |

---

## Alignment changes made

| Before | After |
|---|---|
| `DoneAt` recorded only at pickup call | `DoneAt` recorded when queue completion occurs (Pickup Call **or** No Show Resolution) |
| `WF-APT-RJ-007` assumed queue already complete | Queue may still be `In Service`; Apotek may complete it to `Done` |
| SOP-007 only handled already-`Done` | SOP-007 also defines the pre-Pickup-Call `In Service` path |
| Queue `Done` read as if it implied service/handover end | Queue `Done` only means queue service lifecycle completed |
| Risk of inventing a new status or reversing `DoneAt` | No new status; `DoneAt` never reversed |

Pharmacy terminal outcomes (`Dispense Order` `Expired`, payer-specific Sales Order / invoice consequences) were already aligned and were not changed.

---

## Evidence that pre-Pickup-Call No Show handling is now consistent

| Required statement | Evidence |
|---|---|
| QueueEntry may be completed during No Show Resolution | Domain `BR-APT-095`; workflow `WF-APT-RJ-007` main flow step 2; SOP-007 step 12 and exception 5.4 |
| QueueEntry may already be `Done` before No Show Resolution | `WF-APT-RJ-007` inputs; SOP-007 exception 5.3 |
| `DoneAt` is recorded when queue completion occurs | Domain §1.3; `BR-APT-095`; workflow §9; screen §4.2 |
| `DoneAt` is never reversed | `BR-APT-095`; `BR-APT-142` (unchanged); `WF-APT-RJ-007` exceptions; SOP-007 step 12 |
| No new queue status | Domain §1.3; SOP-007 5.4; ADR-APT-001 canonical statuses unchanged: `Waiting` / `InService` / `Done` / `Withdrawn` |
| No pharmacy workflow state on QueueEntry | ADR-APT-001 “Pharmacy workflow states SHALL NOT be added to `AntrianStatusEnum`”; SOP-007 step 12 |
| Queue `Done` ≠ Medication Handover | Domain §1.3 / §8.7; `BR-APT-095`; SOP-003 / SOP-004 completion criteria; screen §3.4 |
| Pharmacy Queue Close is a different path | `BR-APT-145` unchanged; ADR and `WF-APT-RJ-007` state Close does not apply after `In Service` |

Worked path:

1. `Medication Preparation Started` → queue `In Service`, `ServedAt` recorded (`BR-APT-081`).
2. Dispense Order reaches `Prepared`; pickup call has not occurred.
3. Authorized No Show Resolution (`WF-APT-RJ-007`) runs.
4. Patient Tracker may move the Queue Entry `In Service` → `Done` and record `DoneAt`.
5. Dispense Order becomes `Expired`; stock return Mutasi and payer commercial outcomes proceed as before.
6. Queue `Done` does not record handover.

Worked path after pickup call:

1. Pickup call already completed the queue and recorded `DoneAt`.
2. No Show Resolution retains `DoneAt` and does not reopen the queue.
3. Pharmacy outcomes are the same (`Expired`, return Mutasi, payer-specific commercial close).

---

## Confirmation: ADR-APT-001 ownership boundaries remain unchanged

| ADR-APT-001 rule | Status after ALN-005 |
|---|---|
| Pharmacy workflow states SHALL NOT be added to `AntrianStatusEnum` | Unchanged. No `NoShow`, `Expired`, or `HandedOver` queue status. |
| Canonical statuses remain `Waiting`, `InService`, `Done`, `Withdrawn` | Unchanged. |
| Queue owns number, lifecycle, calling, serving, completion, withdrawal, timestamps | Unchanged. Completion may now be *caused* by Pickup Call or No Show Resolution; ownership of the resulting `Done` / `DoneAt` remains Patient Tracker. |
| Pharmacy owns No Show Handling and fulfillment progress | Unchanged. |
| Modeling remains QueueEntry separate from pharmacy progress | Unchanged. |

The ADR received only a completion-trigger clarification and an Allowed example (`Queue Status = Done` with `Pharmacy Status = No Show / Expired`). That example demonstrates separated concerns; it does not move No Show onto the queue aggregate.

---

## Confirmation: alignment only

This resolution did **not**:

- add a queue status;
- add a pharmacy state to QueueEntry;
- change Pharmacy Queue Close (`Withdrawn` from `Waiting`);
- change `ServedAt` causation (`Medication Preparation Started`);
- change Dispense Order, Sales Order, or stock outcomes for No Show;
- reopen BA-01 through BA-09 or redesign queue architecture.
