# Phase 4 — Return to Waiting Implementation Summary

## Status

Phase 4 is implemented across `b09-bilreg-api` and `c012_myhospital_web`. The only remaining verification is environment-gated: the disposable SQL Server concurrency suite and the authenticated Playwright workflow against representative queue data.

## Codebase assessment

- Already present: `Released` claim state, RowVersion compare-and-swap conventions, current-Loket projection, server-resolved workstation/Loket validation, stable HTTP 409 mapping, post-operation refresh publication, shared frontend request/response schemas, and authoritative conflict recovery.
- Partially present: claim release was coupled to final transitions such as Withdraw, No-Show, and Complete.
- Missing before this change: a claim-only command, audit evidence, API route, frontend mutation and eligibility rules, Current Loket Session action, and focused concurrency coverage.
- Conflict found: the real-SQL test named as a release-versus-recall case actually exercised final Withdraw semantics. It was renamed to describe that behavior and avoid conflating it with the non-final Phase 4 operation.

## Completed work

### Backend

- Added `AdmissionQueueReturnToWaitingCmd`, its handler, and `TryReturnToWaiting`.
- Added authenticated `POST /api/v1/admission-queue/entries/{q}/{n}/return-to-waiting`, reusing `VersionedActorLoketBody` and the existing operation response.
- Implemented one conditional SQL update that requires the requested entry, server-resolved Loket, active Outstanding claim, matching RowVersion, and a still-Waiting queue entry. It releases only the claim; queue status, CallCount, queue label, priority, call timestamps, and AnnouncementVersion are not updated.
- Runs the claim CAS and append-only `BILRG_AuditLog` insertion in one transaction. Audit evidence uses action `RETURN_TO_WAITING`, entity `AdmissionQueueLoketClaim`, entity ID `{AntrianId}:{NoUrut}`, disposition `UnansweredCall`, and JSON context containing Loket, queue identity, Outstanding state, and expected version.
- Publishes the admission-queue refresh hint only after the transaction commits. Every failed predicate returns the existing `AQ_CONCURRENCY_CONFLICT`; conflicts create neither audit evidence nor publication.
- Added the existing shared audit table to queue deployment preflight and disposable-SQL prerequisites.
- Added command, API-contract, migration-manifest, and disposable-SQL race/invariant coverage. The SQL cases include duplicate release, Recall/Start Service races, wrong identity/Loket/version, preservation of call and announcement counters, calling a different entry immediately, and calling the released entry later.

### Frontend

- Added the return-to-waiting endpoint key and `useReturnToWaiting` mutation using the shared versioned body and operation-response schemas.
- Centralized eligibility in `canReturnToWaiting`: it is available only for the configured current Loket's matching Outstanding claim with a current RowVersion and no mutation in progress.
- Added a duplicate-safe Current Loket Session workflow with pending, success, conflict, and non-conflict feedback. Success and HTTP 409 both trigger authoritative worklist/current-Loket refresh. The selected Preview is retained because the entry remains Waiting.
- Added a distinct **Return to Waiting** action beside Recall in desktop and mobile layouts; it remains visually and semantically separate from final No-Show.
- Updated Admisi user documentation and the Unreleased changelog.
- Added service, action-rule, component, and environment-gated browser workflow coverage for Call → Return to Waiting → call another entry → call the original entry again.

## Architectural decisions and deviations

- Reused the generic versioned request and operation-response contracts instead of introducing duplicate Phase 4-only schemas.
- Reused `BILRG_AuditLog`; no new queue-specific audit table or migration is needed. Deployment preflight now fails if the shared table is absent.
- Kept authentication plus server-resolved workstation/Loket ownership. The request cannot authorize an arbitrary Loket, and no new role policy was introduced.
- Fixed the disposition to `UnansweredCall`; no free-text reason prompt was added.
- Kept the action within the existing redesigned Admission Queue enablement boundary; no runtime flag was added.

These are intentional schema and policy reuse decisions consistent with the target design, not functional reductions.

## Verification evidence

- Backend admission-queue regression filter: 70 passed, 0 failed.
- Backend API build: succeeded with 0 warnings and 0 errors.
- Focused frontend Vitest suite: 34 passed across 7 files.
- Frontend application type-check: passed.
- Targeted frontend ESLint and Oxlint: passed with no errors.
- Targeted Prettier: applied successfully.
- Frontend production bundle: completed; only the existing sourcemap, chunk-size, and browsers-data warnings were emitted.
- Playwright collection: the new scenario collected successfully for Chromium, Firefox, and WebKit.
- Repository whitespace checks: passed.
- Disposable-SQL gate invocation: preflight refused execution because `BILREG_AQ_IT_SERVER` and `BILREG_AQ_IT_DATABASE` are not configured; no SQL test body ran.

## Deployment and remaining verification

Deploy the backend first. Its queue preflight must confirm the existing `BILRG_AuditLog` table before enabling the frontend bundle. Deploy the frontend second.

Before production sign-off, run:

1. `AdmissionQueueRealSqlGateTest` against the configured disposable SQL Server database.
2. The authenticated Playwright scenario against an environment with at least two representative Waiting entries and the redesigned queue enabled.

No implementation work is known to remain, and there are no breaking API changes.
