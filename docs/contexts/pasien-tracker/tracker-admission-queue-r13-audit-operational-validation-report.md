# R-13 — Audit and Operational Validation

## Outcome

R-13 is limited to audit compliance and continuous operational validation. No retention job, archive,
purge scheduler, monitoring dashboard, or capacity-planning capability is introduced.

`BILRG_AdmissionQueue_M3_Audit_Alter.sql` adds the project-standard `CrtUser`, `CrtDate`,
`UpdUser`, `UpdDate`, `VodUser`, and `VodDate` fields to existing Queue Session, Queue Entry, and
Service Point tables. It adds the missing void fields to Booking Assistance. New-database scripts are
aligned. Current queue transition SQL writes Queue Entry `UpdUser` and `UpdDate` using the existing
request actor convention; no R-02 identity redesign is introduced.

## Persistence classification

| Persistence type | Retention/state classification | R-13 treatment |
|---|---|---|
| Queue Entry | Permanent historical evidence until retention policy is approved | Never physically delete through V1 workflow |
| Registration Outcome | Permanent final fact | Immutable, one outcome per Queue Entry |
| Redirect provenance | Permanent historical evidence | Preserve source `(AntrianId, NoUrut)` on replacement entry |
| Call history, if added later | Permanent historical evidence | Append-only; no current implementation adds it |
| Current Loket display snapshot | Replaceable latest operational state | Reconstruct from persisted claim/entry state; clients reload it |
| Active Loket claim | Operational latest state | May be updated/released through explicit R-04 transitions; no automatic expiry |

Queue Session and Service Point are supporting durable records and carry standard audit fields. The
Booking Assistance correlation is durable workflow evidence while its `IsActive` flag is replaceable
operational state.

## Continuous operational-validation gate

Each queue slice must add and run tests at the layer it changes:

1. unit tests for invariant and expected-state behavior;
2. real-SQL tests for CAS/unique-index concurrency and rollback of the changed transaction;
3. representative-volume projection tests when a query or index changes; and
4. API conflict tests when transport changes.

The mandatory real-SQL scenarios are: same Loket/two entries, two Loket/one entry, stale claim
version, release versus recall, Redirect rollback, final-outcome rollback, Booking Assistance unique
race, and worklist/current-display representative-volume query plans. They require an installation
with the R-06 through R-13 migrations applied; no connection target is configured in this repository,
so they remain a deployment-gate test suite rather than simulated by unit-test mocks.

The focused Queue transition, outcome, Booking Assistance, and API regression suite passes 21 of 21
tests after the audit-field changes.

## Deferred policy

Queue Entries, Registration Outcomes, redirect provenance, and any future call history remain
indefinitely preserved until the hospital approves a retention policy. Retention, archive, purge,
capacity, dashboard, and monitoring work are deferred to that policy/operations phase.
