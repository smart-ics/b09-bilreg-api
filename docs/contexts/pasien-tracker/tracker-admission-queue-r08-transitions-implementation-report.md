# R-08 — Queue Operational Transitions

## Transition matrix

| Operation | Queue Entry source | Claim precondition | Queue Entry result | Claim result |
|---|---|---|---|---|
| Call | Waiting | Entry unclaimed; Loket absent or Released | Waiting; CallCount + 1 | Outstanding; AnnouncementVersion + 1 |
| Recall | Waiting | Same entry Outstanding at same Loket and current RowVersion | Waiting; CallCount + 1 | Retained Outstanding; AnnouncementVersion + 1 |
| Start Service | Waiting | Same entry Outstanding at same Loket and current RowVersion | InService; ServedAt set | Retained as InService |
| Withdraw, uncalled | Waiting | No active claim for entry | Withdrawn; reason/actor/time recorded | None |
| Withdraw, called | Waiting | Same entry Outstanding at supplied Loket and current RowVersion | Withdrawn | Released |
| No-Show | Waiting | Same entry Outstanding at supplied Loket and current RowVersion | Withdrawn; reason `NoShow` | Released |
| Redirect, uncalled/called | Waiting | No active claim, or matching Outstanding claim | Origin Withdrawn; new target Priority Waiting entry | Matching claim Released |
| Any operation above | InService, Done, Withdrawn | N/A | Rejected | Unchanged |

`InService` withdrawal is deliberately excluded. The accepted R-04 contract permits Complete or a
controlled rollback from InService; it does not authorize ordinary Withdraw/No-Show/Redirect there.

## Implementation

- Added intention-revealing Entry behaviors for call validation/counting, called service start,
  withdrawal, and redirect provenance.
- Added narrow MediatR commands for Call, Recall, Start Service, Withdraw, No-Show, and Redirect.
- Added intent-specific SQL CAS repository operations. Operational handlers never use aggregate
  `SaveChanges`.
- Queue Entry and current-Loket changes execute inside the existing ambient local transaction.
- Redirect withdraws the origin, releases its matching claim, resolves/creates the target daily
  session, allocates through `ISequencer`, and inserts a Priority replacement with full composite
  provenance in the same transaction. Sequence gaps on rollback remain acceptable.
- CAS misses, stale RowVersion, active-Loket conflicts, active-entry conflicts, and duplicate target
  creation return `AdmissionQueueConcurrencyException`, already mapped to HTTP 409.
- Refresh publishing is invoked only after transaction completion. The registered adapter remains
  a no-op until the approved SignalR transport slice; persisted projections and polling remain truth.

## Legacy compatibility

`AdmissionQueueStartCmd` retains its existing direct Waiting-to-InService behavior and endpoint for
existing consumers, but is marked obsolete. New clients must use explicit Call followed by
`AdmissionQueueStartServiceCmd`. Its semantics were not silently changed.

## Verification scope

Focused tests cover valid explicit Call, stale-CAS conflict/no publish, terminal-state rejection,
No-Show disposition, redirect Priority/provenance, and the existing R-05A through R-07 regression
set. The focused suite passes 34/34. The broader AntrianFeature suite, against an unmigrated test
database, reports the pre-existing 30 AntrianMap failures plus 12 DAL failures caused by the absent
R-06/R-07 columns; it is not valid migration evidence. Real SQL Server integration tests require
applying the R-06/R-07 migrations first and remain
the deployment gate for concurrent claim races and forced rollback after intermediate writes.
