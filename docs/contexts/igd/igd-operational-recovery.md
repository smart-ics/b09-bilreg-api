# igd-operational-recovery.md — IGD Visit Operational Recovery Procedures

This document describes the manual reconciliation procedures the on-call DBA / operator
should follow when the system reports inconsistencies between `BILRG_PakaiBed`,
`BILRG_BedIgd`, and `BILRG_IgdVisit`.

The reconciliation entry point is the read-only sweep query exposed at:

```
GET /api/BedIgd/pakaiBed/orphan
```

(implemented by `PakaiBedListOrphanQuery`)

## What is an "orphan PakaiBed"?

A `PakaiBed` row is considered orphan when it is still **open**
(`CheckOutDateTime = '3000-01-01'`) but the surrounding aggregates indicate that the
occupancy actually ended:

| OrphanReason         | Condition (PakaiBed open + ...)                                       |
| -------------------- | --------------------------------------------------------------------- |
| `VISIT_NOT_FOUND`    | the referenced `IgdVisitId` no longer exists in `BILRG_IgdVisit`.     |
| `VISIT_VOIDED`       | the visit row has `VodDate <> '3000-01-01'`.                          |
| `VISIT_TERMINAL`     | the visit's `AdministrativeState` is `DISCHARGED` or `REDIRECTED`.    |
| `BED_NOT_FOUND`      | the referenced `BedIgdId` no longer exists in `BILRG_BedIgd`.         |
| `BED_REASSIGNED`     | the bed's `CurrentIgdVisitId` no longer matches the PakaiBed visit.   |
| `BED_NOT_OCCUPIED`   | the bed's `BedState` is not `OCCUPIED`.                               |

These are pathological combinations that should never appear in steady state because
both `BedIgd` and `PakaiBed` are written within the same explicit `TransHelper.NewScope`
transaction by the `IgdVisit` use-cases (Discharge, Void, CheckOut, Redirect cascades).
An orphan therefore indicates one of the following:

1. A historical bug or a manual SQL fix-up that touched only one of the tables.
2. A power loss after the bed was released but before the PakaiBed transaction
   committed (extremely rare, see "Why a sweep is still needed" below).
3. Direct data import / migration that did not respect the aggregate boundaries.

## Why a sweep is still needed

The IGD Visit feature uses **explicit transactions** (`TransHelper.NewScope`) to write
`IgdVisit`, `BedIgd`, and `PakaiBed` together, plus a filtered unique index
(`UQ_BILRG_BedIgd_VisitActive`) to guarantee that the same visit cannot occupy two beds
concurrently. This makes orphans impossible under normal operation.

However, because we deliberately avoid distributed sagas and event sourcing, the only
safety net for the rare unrecoverable failure (DB crash, mid-transaction kill, manual
DML) is this **read-only sweep + manual reconciliation**.

## Reconciliation playbook

> **Scope:** Run during low-traffic window. Always work inside an explicit DB transaction
> and capture the affected rows in a backup table before mutating data.

### Step 1 — Snapshot the orphans

```sql
SELECT * INTO #OrphanSnapshot
FROM (
    -- copy the SELECT used by ListOrphans() in PakaiBedDal.cs
    -- (kept here as a single source of truth for ad-hoc audits)
) src;
```

Or simply call the API:

```
GET /api/BedIgd/pakaiBed/orphan
```

Save the JSON response in the incident ticket.

### Step 2 — Decide per OrphanReason

Use the following table as the **default action**. Always sanity-check against the
incident before applying.

| OrphanReason       | Default action                                                                          |
| ------------------ | --------------------------------------------------------------------------------------- |
| `VISIT_TERMINAL`   | Close the PakaiBed row (`CheckOutDateTime`/`CheckOutUserId` ← visit's discharge audit). |
| `VISIT_VOIDED`     | Close the PakaiBed row (`CheckOutDateTime`/`CheckOutUserId` ← visit's `VodDate`/`VodUser`). |
| `VISIT_NOT_FOUND`  | Close the PakaiBed row (`CheckOutDateTime` ← `CheckInDateTime`, `CheckOutUserId` ← `'SYSTEM'`); investigate why the visit row is missing. |
| `BED_NOT_FOUND`    | Same as `VISIT_NOT_FOUND`. Open a separate ticket for the missing bed master record.    |
| `BED_REASSIGNED`   | Close this PakaiBed row using the *previous* visit's discharge audit (the bed has already been re-occupied by another visit, so the historical row was simply never closed). |
| `BED_NOT_OCCUPIED` | Close this PakaiBed row using `BILRG_BedIgd.UpdDate`/`UpdUser` of the bed's last update.|

### Step 3 — Manual UPDATE template

```sql
BEGIN TRAN;

UPDATE BILRG_PakaiBed
SET CheckOutDateTime = @CheckOutDateTime,
    CheckOutUserId   = @CheckOutUserId
WHERE PakaiBedId = @PakaiBedId
  AND CheckOutDateTime = '3000-01-01';   -- guard: only close if still open

-- inspect, then either COMMIT or ROLLBACK
-- COMMIT;
```

> Never reopen a closed PakaiBed; always create a new one via the application use-case
> (`/api/IgdVisit/{id}/assignBed`) if a re-admission is needed.

### Step 4 — Re-run the sweep

After applying fixes, re-call the endpoint:

```
GET /api/BedIgd/pakaiBed/orphan
```

The response **must** be empty before closing the incident.

## Anti-procedures (do NOT do this)

* Do **not** delete `PakaiBed` rows; they are append-only history and required for
  billing and audit.
* Do **not** mutate `BedIgd` directly to "fix" occupancy — go through the application
  use-cases (`/api/IgdVisit/{id}/checkOut`, `/discharge`, `/void`) so the transaction
  spans both `IgdVisit` and `BedIgd`.
* Do **not** disable the filtered unique index `UQ_BILRG_BedIgd_VisitActive`; it is the
  last-line of defence against double-assignment.
