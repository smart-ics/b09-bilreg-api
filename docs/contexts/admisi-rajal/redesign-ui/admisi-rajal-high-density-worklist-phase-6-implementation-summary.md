# Admisi Rajal High-Density Worklist — Phase 6 Implementation Summary

Date: 2026-07-25

## Scope and outcome

Phase 6 adds queue-aware Registration creation and an explicit completion boundary for an
already-linked Registration. Non-queue Walk-In and Booking callers remain compatible, and the
existing visit, guarantee, eligibility, and patient update dialogs remain intermediate saves.

`NotEstablished` transport is available in the frontend service but intentionally has no operator
action until Operations provides an approved ReasonCode catalog and labels.

## Pre-implementation audit

### Already implemented

- The Admission Queue worklist, workstation selection, Call, Start Service, current-Loket claim,
  optimistic RowVersion, and `Established` / `NotEstablished` backend outcome routes existed.
- Registration creation already accepted queue identity (`AdmissionAntrianId` and
  `AdmissionNoUrut`) and ran its Registration writes inside a transaction.
- The high-density UI already distinguished preview from an authoritative Processing session and
  had separate Registration update dialogs.
- Outcome persistence and queue/current-Loket query invalidation foundations existed.

### Partially implemented

- Queue-linked Walk-In and Booking creation recorded an Established outcome, but used a
  compatibility operation that could release an In Service claim without matching its Loket or
  RowVersion.
- Creation carried queue identity but no expected claim version, so the caller could not prove that
  its Processing session was still current.
- Existing Registration work could be edited but had no explicit, authoritative completion action.
- The session model covered starting/processing/conflict, but not independent completion state.

### Missing

- All-or-none queue context validation and workstation-derived Loket authority on Registration
  creation.
- A single atomic CAS operation covering Established outcome, queue Done state, claim release, and
  the surrounding Registration transaction.
- Queue-aware frontend creation payloads, outcome mutations, completion invalidation, success
  cleanup, focus recovery, conflict recovery, and timeout reconciliation guidance.
- User workflow documentation, changelog notes, contract tests, and rollback coverage.

### Conflicts discovered

- The compatibility finalization path conflicted with the target concurrency boundary because it
  did not constrain claim release by Loket and RowVersion.
- Phase 0's wording that no separate Complete Assistance action was needed conflicted with the
  actual UI: an existing Registration is updated through independent dialogs and has no safe
  consolidated save boundary.
- Exposing `NotEstablished` would require inventing operational ReasonCodes, which conflicts with
  the existing server contract and the stated ownership of that catalog.

## Completed work

### Backend

- Added optional `AdmissionExpectedRowVersion` to Walk-In and Booking requests and enforced the
  all-or-none tuple of queue ID, sequence number, and expected RowVersion.
- Added a server-only Loket property. `RegController` resolves it from authenticated workstation
  headers only when queue context is present; non-queue requests do not invoke workstation
  resolution.
- Queue-linked creation verifies the entry is still In Service, then uses a strict SQL CAS matching
  queue entry, active In Service claim, resolved Loket, and expected RowVersion.
- Registration writes, Established outcome insertion, queue entry completion, booking-assistance
  deactivation, tracker changes, and claim release remain under the existing ambient transaction.
  A failed CAS or duplicate outcome throws a concurrency error so the transaction rolls back.
- Refresh publication occurs only after the transaction scope commits.
- Added unit/API contract coverage for absent, partial, complete, and malformed context plus
  server-derived Loket behavior, and added a real-SQL rollback gate for wrong-Loket completion.

### Frontend

- Extended Walk-In and Booking Zod payloads with all-or-none queue context.
- Queue fields are attached only while the selected worklist item and current-Loket claim are the
  same authoritative In Service session; the current RowVersion is not exposed otherwise.
- Added no-retry Established and NotEstablished mutations. Successful outcomes invalidate Admission
  Queue, Registration, Booking, and Patient query families.
- Queue-originated Walk-In and Booking submission is labelled **Complete Registration** and sends
  the queue-aware creation command.
- Existing Registration keeps its individual update dialogs and gains an explicit
  **Complete Registration** action gated by linked RegId, authoritative Processing context, active
  matching claim/RowVersion, and absence of another submit, void, completion, or open update dialog.
- Added independent completing session state, duplicate-action blocking, 409 conflict recovery,
  indeterminate-error reconciliation messaging, success cleanup, worklist refresh, session
  deselection, and worklist focus restoration.
- Added schema, service, state rendering, and environment-gated Playwright journey coverage.
- Updated the Admisi workflow guide and Unreleased changelog.

## Architectural decisions and deviations

- The existing Registration completion boundary is an explicit outcome command, not any individual
  update dialog. This intentionally supersedes Phase 0 wording because the current UI has no
  consolidated Registration update command.
- No Registration-dialog redesign or consolidated update API was introduced.
- The additional expected RowVersion is additive and required only when queue context is supplied.
  This closes the stale-claim gap while preserving legacy payloads.
- The server never treats a payload Loket as authorization. Registration creation receives the Loket
  through a JSON-ignored command property populated by the controller's workstation resolver.
- Correctable creation validation remains in the mounted Processing workspace. A 409 revokes the
  transient editing state and refetches authority. Other indeterminate errors invalidate and
  reconcile authoritative query state before the operator can retry.
- `NotEstablished` UI and journey are deferred; only the typed client contract is prepared.

## Verification evidence

- Backend solution build: passed; four pre-existing obsolete compatibility-command warnings.
- Backend focused unit/API tests: 34 passed, 0 failed, excluding
  `Category=AdmissionQueueRealSql`.
- Frontend focused Vitest suites: 23 passed after the completion-state rendering change; earlier
  related service/schema suites also passed.
- Frontend targeted Prettier, Oxlint, and ESLint: passed with zero reported errors.
- The repository `pnpm lint` wrapper could not start because pnpm's configured release signature
  could not be verified offline; its Oxlint and ESLint checks were run directly against every
  affected source/test file and passed.
- Frontend Vue TypeScript check: passed.
- Frontend production Vite build: passed; existing sourcemap, browsers-data, and large-chunk warnings
  remain.
- The real-SQL rollback test compiled and correctly fails closed without `BILREG_AQ_IT_SERVER` and
  `BILREG_AQ_IT_DATABASE`; it was not executed against a configured integration database.
- The authenticated Playwright journey is gated by `ADMISI_QUEUE_COMPLETION_E2E=true` and was not
  executed without the required environment/session.

## Remaining Phase 6 work

- Operations must supply the authoritative `NotEstablished` ReasonCode catalog and labels before
  its UI action and E2E journey can be enabled.
- Run the real-SQL rollback/concurrency gate in the designated integration database.
- Run the authenticated queue completion Playwright journeys in a configured application
  environment.

## Recommended commits

### `b09-bilreg-api`

```text
feat(admission-queue): harden queue-linked registration completion

- validate all-or-none queue identity and expected RowVersion
- derive Loket authority from authenticated workstation headers
- atomically establish Registration and release the matching In Service claim
- publish queue refresh only after commit
- add API, concurrency, and rollback coverage
- document the Phase 6 audit and implementation
```

### `c012_myhospital_web`

```text
feat(admisi): add queue-aware registration completion

- attach authoritative queue context to Walk-In and Booking creation
- add explicit Complete Registration for linked registrations
- model completion state, conflict recovery, reconciliation, and focus restoration
- prepare Established and NotEstablished outcome clients
- add schema, service, component, and gated E2E coverage
- update the Admisi workflow and changelog
```
