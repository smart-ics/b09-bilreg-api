# ALN-007 Resolution Report

**Issue:** ALN-007 — Terminology Cleanup and Naming Alignment  
**Status:** Resolved  
**Date:** 2026-08-16  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Scope:** Terminology synchronization only. No business-rule, workflow, aggregate-ownership, or queue-ownership change.

**Authoritative vocabulary:** `apotek-domain.md` Dispense Order lifecycle §8.4; Dispensing Temporary Custody; ADR-APT-001 queue ownership (unchanged).

ADR-APT-001 ownership boundaries were not re-analyzed, reinterpreted, replaced, or redesigned.

---

## What was aligned

Pharmacy example labels in ADR-APT-001 now match the canonical Dispense Order lifecycle:

```text
Established
Awaiting Clearance
Released
Preparing
Prepared
Reviewed
Completed
Cancelled
Expired
Unfulfilled
```

Custody vocabulary remains **Dispensing Temporary Custody**. Informal example names are retired as current vocabulary.

---

## Files modified

| File | Change |
|---|---|
| `adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md` | Context, Pharmacy Owns list, Prevent State Explosion, Allowed/Not Allowed examples, references |
| `outpatient-apotek-repository-gap-analysis-report.md` | BA-09 gap sentence: `in-transit custody` → Dispensing Temporary Custody |
| `APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md` | ALN-007 marked resolved; Business Terminology Aligned; remaining-defect counts updated |
| `docs/ARTIFACTS.md` | Indexed this report |

Unchanged (already using canonical vocabulary): `apotek-domain.md`; `outpatient-apotek-workflow.md`; `outpatient-apotek-screen-and-aggregate-design.md`; `SOP-APT-RJ-007-*-EN.md` and `-ID.md`; ADR-APT-002.

---

## Sections modified in ADR-APT-001

| Section | Modification |
|---|---|
| Context | Informal example list replaced with Dispense Order states from domain §8.4 plus related Pharmacy facts, including Dispensing Temporary Custody |
| Ownership — Pharmacy Owns | Informal labels replaced with Telaah Resep, Sales Order, Sales Invoice, Dispense Authorized, Dispense Order lifecycle, Medication Preparation, Dispensing Temporary Custody, Medication Handover, Pickup Expired, No Show Handling. Ownership of the queue vs Pharmacy split is unchanged |
| Prevent State Explosion | Informal `WaitingPayment` / `Paid` / `Dispensing` / `Dispensed` / `HandedOver` / `NoShow` list replaced with Dispense Order states and Pharmacy facts |
| Allowed | Examples use `Preparing`, `Awaiting Clearance`, `Completed`, `Expired` + No Show. Queue `Done` still does not imply handover; medication may remain in Dispensing Temporary Custody |
| Not Allowed | Forbidden queue statuses are Dispense Order states (`Preparing`, `Awaiting Clearance`, `Completed`, `Expired`), not the retired informal names |
| References | Domain §8.4 and Dispensing Temporary Custody |

---

## Terminology changes applied

| Retired (not current vocabulary) | Canonical |
|---|---|
| Sales Confirmation | Sales Order / Sales Invoice |
| Payment Confirmation / WaitingPayment / Paid | Awaiting Clearance; Payment Clearance / Coverage Clearance / Dispense Authorized as evidence |
| Dispensing (as a queue-like pharmacy status) | `Preparing` (Dispense Order state). “Dispensing lifecycle” remains ordinary domain prose |
| Dispensed | `Prepared` or Medication Dispense, according to the domain fact |
| HandedOver | Medication Handover; Dispense Order `Completed` when handover is recorded |
| In-Transit / in-transit custody | Dispensing Temporary Custody |
| Pickup Expiration (ADR example label) | Pickup Expired (projection category) |
| Prescription Review (ADR example label) | Telaah Resep |

---

## Removed obsolete terms (as current example vocabulary)

These strings are no longer used as pharmacy statuses in ADR-APT-001 Allowed/Not Allowed or Context lists:

- `WaitingPayment`
- `Paid`
- `Dispensing` (as a status)
- `Dispensed`
- `HandedOver`
- `Sales Confirmation`
- `Payment Confirmation`
- In-Transit (live gap-analysis wording)

They may still appear in historical resolution reports as the leftover that was removed.

---

## Evidence that artifacts now use a consistent vocabulary

| Artifact | Dispense Order states | Custody term |
|---|---|---|
| `apotek-domain.md` §8.4 | Canonical lifecycle unchanged | Dispensing Temporary Custody (glossary and `BR-APT-069`) |
| `outpatient-apotek-workflow.md` | Uses `Preparing`, `Prepared`, `Reviewed`, `Completed`, `Expired` | Dispensing Temporary Custody / Dispensing Temporary Unit |
| Screen design §3.3–§3.4 | Dispense Order states; Serah Obat projection categories | Dispensing Temporary Custody |
| SOP-007 | `Prepared`, `Expired` | Dispensing Temporary Custody |
| ADR-APT-001 | Same Dispense Order states in examples | Dispensing Temporary Custody in Context, Pharmacy Owns, Allowed |
| ADR-APT-002 | `Prepared`, Medication Handed Over | Dispensing Temporary Unit / Pharmacy-owned custody |

Worked example after alignment:

```text
Queue Status = InService
Dispense Order = Preparing
```

```text
Queue Status = Done
Dispense Order = Expired
Pharmacy fact = No Show
```

Medication may remain in Dispensing Temporary Custody after queue `Done`. Queue `Done` is still not Medication Handover.

---

## Confirmation: no behavior, state machine, or ownership change

This resolution did **not**:

- change queue statuses (`Waiting`, `InService`, `Done`, `Withdrawn`);
- add a pharmacy state to `AntrianStatusEnum`;
- change Patient Tracker vs Pharmacy ownership in ADR-APT-001;
- change Dispense Order transitions in domain §8.4;
- change `WF-APT-RJ-001`–`007` behavior;
- change aggregate roots (`TelaahResep`, `SalesOrder`, `SalesInvoice`, `DispenseOrder`);
- change BA-09 Mutasi / Remove Stock / return Mutasi stock mapping.

Only example names and leftover In-Transit wording were synchronized to the existing domain vocabulary.
