# Admission Officer Workspace — Phase 7 Implementation Summary

**Implemented:** 2026-07-27  
**Phase:** Supervisor Queue Closing backend

## Outcome

`b09-bilreg-api` now provides supervisor-protected Queue Closing preview and close operations for
the exact remaining Waiting entries in one business-date and Service Point scope. Closing uses a
serializable SQL transaction with range/update locks, applies explicit NoShow or Withdraw decisions
atomically, and creates no Queue Session or Registration Outcome.

## Changes

- Added `GET /api/v1/admission-queue/closing-preview` and `POST /api/v1/admission-queue/close`.
- Added exact-set validation, duplicate/invalid decision rejection, Withdraw-reason validation,
  Outstanding RowVersion checks, and an InService all-or-nothing blocker.
- Added one transactional SQL closing repository which locks, reloads, validates, releases matching
  Outstanding claims, transitions entries to Withdrawn, verifies the final Waiting set is empty,
  and rolls back all writes on failure.
- Added per-entry terminal audits and one `ADMISSION_QUEUE_CLOSE` summary audit. No Registration
  Outcome is created for never-presented patients.
- Added the fail-closed `AdmissionQueueSupervisorOperations` policy. It recognizes either
  `AdmissionQueue.SupervisorOperations` or an allowed role from
  `AdmissionQueueApi:SupervisorOperationAllowedRoles` (default `ADM-SPV`), and now protects
  Withdraw, No Show, preview, and close.
- Updated the canonical Admission Queue API contract with route, authorization, concurrency, and
  operational runbook requirements. Queue Closing is enabled by default.

## Validation

| Check | Result |
| --- | --- |
| `dotnet build src\\bilreg\\Bilreg.Api\\Bilreg.Api.csproj --no-restore` | Passed; pre-existing project warnings remain. |
| Focused Admission Queue API/handler tests | 29 passed. One existing `RefreshHub_IsAuthorizedAndExposesStablePath` test failed because the Hub has no reflected `AuthorizeAttribute`; unrelated to Queue Closing. |
| Real-SQL close concurrency suite | Environment-gated; not run locally. |

## Deployment and rollback

Deploy supervisor JWT role/permission mapping before granting operational access. Operations must
stop intake before preview, refresh/review after every conflict, and verify an empty scoped preview
after close. Rollback is a deploy rollback only; successful terminal dispositions are historical
truth and must be corrected with an explicit follow-up action, never automatically reopened.

## Suggested commit

`feat(admission-queue): add atomic supervisor queue closing`
